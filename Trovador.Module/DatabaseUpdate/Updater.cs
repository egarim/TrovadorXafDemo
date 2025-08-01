using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Security.Strategy;
using DevExpress.Xpo;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Trovador.Module.BusinessObjects;
using Trovador.Module.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;

namespace Trovador.Module.DatabaseUpdate;

// For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
public class Updater : ModuleUpdater {
    public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
        base(objectSpace, currentDBVersion) {
    }
    public override void UpdateDatabaseAfterUpdateSchema() {
        base.UpdateDatabaseAfterUpdateSchema();

        // Create ExplanationLevel records from embedded prompt files if they don't exist
        try
        {
            var prompts = Controllers.PromptHelper.GetAllPrompts();
            foreach (var promptKvp in prompts)
            {
                var fileName = promptKvp.Key;
                var promptContent = promptKvp.Value;
                var cleanName = fileName.EndsWith(".txt") ? fileName.Substring(0, fileName.Length - 4) : fileName;
                var existingLevel = ObjectSpace.FirstOrDefault<ExplanationLevel>(el => el.Name == cleanName);
                if (existingLevel == null)
                {
                    var explanationLevel = ObjectSpace.CreateObject<ExplanationLevel>();
                    explanationLevel.Name = cleanName;
                    explanationLevel.Prompt = promptContent;
                }
            }
            ObjectSpace.CommitChanges();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating ExplanationLevels from prompts: {ex.Message}");
        }

        // Create Artists and Artworks from embedded examples if no artworks exist
        try
        {
            var artworkCount = ObjectSpace.GetObjectsCount(typeof(Artwork), null);
            if (artworkCount == 0)
            {
                CreateArtistsAndArtworksFromExamples();
                ObjectSpace.CommitChanges();
                System.Diagnostics.Debug.WriteLine("Successfully created Artists and Artworks from embedded examples.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating Artists and Artworks from examples: {ex.Message}");
        }

        // The code below creates users and roles for testing purposes only.
        // In production code, you can create users and assign roles to them automatically, as described in the following help topic:
        // https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication
#if !RELEASE
        // If a role doesn't exist in the database, create this role
        var defaultRole = CreateDefaultRole();
        var adminRole = CreateAdminRole();

        ObjectSpace.CommitChanges(); //This line persists created object(s);

        UserManager userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();

        // If a user named 'User' doesn't exist in the database, create this user
        if(userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User") == null) {
            // Set a password if the standard authentication type is used
            string EmptyPassword = "";
            _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "User", EmptyPassword, (user) => {
                // Add the Users role to the user
                user.Roles.Add(defaultRole);
            });
        }

        // If a user named 'Admin' doesn't exist in the database, create this user
        if(userManager.FindUserByName<ApplicationUser>(ObjectSpace, "Admin") == null) {
            // Set a password if the standard authentication type is used
            string EmptyPassword = "";
            _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "Admin", EmptyPassword, (user) => {
                // Add the Administrators role to the user
                user.Roles.Add(adminRole);
            });
        }

        ObjectSpace.CommitChanges(); //This line persists created object(s);
#endif
    }

    private void CreateArtistsAndArtworksFromExamples()
    {
        var examples = ExampleHelper.GetAllExamples();
        var artistsCache = new Dictionary<string, Artist>();

        foreach (var example in examples)
        {
            try
            {
                // Get or create artist
                var artist = GetOrCreateArtist(example.ArtistName, artistsCache);
                
                // Create artwork
                var artwork = ObjectSpace.CreateObject<Artwork>();
                artwork.Name = example.ArtworkData.Name;
                artwork.Title = example.ArtworkData.Title;
                artwork.Artist = artist;
                artwork.Year = example.ArtworkData.Year;
                artwork.Medium = example.ArtworkData.Medium;
                artwork.StylePeriod = example.ArtworkData.StylePeriod;
                artwork.Description = example.ArtworkData.Description;
                artwork.ArtworkImage = example.ImageData;

                System.Diagnostics.Debug.WriteLine($"Created artwork: {artwork.Name} by {artist.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating artwork {example.ArtworkData?.Name}: {ex.Message}");
            }
        }
    }

    private Artist GetOrCreateArtist(string artistName, Dictionary<string, Artist> artistsCache)
    {
        if (artistsCache.TryGetValue(artistName, out var cachedArtist))
        {
            return cachedArtist;
        }

        var artist = ObjectSpace.FirstOrDefault<Artist>(a => a.Name == artistName);
        if (artist == null)
        {
            artist = ObjectSpace.CreateObject<Artist>();
            artist.Name = artistName;
            System.Diagnostics.Debug.WriteLine($"Created artist: {artistName}");
        }

        artistsCache[artistName] = artist;
        return artist;
    }

    public override void UpdateDatabaseBeforeUpdateSchema() {
        base.UpdateDatabaseBeforeUpdateSchema();
        //if(CurrentDBVersion < new Version("1.1.0.0") && CurrentDBVersion > new Version("0.0.0.0")) {
        //    RenameColumn("DomainObject1Table", "OldColumnName", "NewColumnName");
        //}
    }
    private PermissionPolicyRole CreateAdminRole() {
        PermissionPolicyRole adminRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
        if(adminRole == null) {
            adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
            adminRole.Name = "Administrators";
            adminRole.IsAdministrative = true;
        }
        return adminRole;
    }
    private PermissionPolicyRole CreateDefaultRole() {
        PermissionPolicyRole defaultRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(role => role.Name == "Default");
        if(defaultRole == null) {
            defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
            defaultRole.Name = "Default";

            defaultRole.AddObjectPermissionFromLambda<ApplicationUser>(SecurityOperations.Read, cm => cm.Oid == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
            defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);
            defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "ChangePasswordOnFirstLogon", cm => cm.Oid == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
            defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "StoredPassword", cm => cm.Oid == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
            defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Deny);
            defaultRole.AddObjectPermission<ModelDifference>(SecurityOperations.ReadWriteAccess, "UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
            defaultRole.AddObjectPermission<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, "Owner.UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
            defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
            defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
        }
        return defaultRole;
    }
}

// Helper class for working with embedded example resources
public class ExampleHelper
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static readonly string _examplesNamespace = "Trovador.Module.Examples";

    public class ArtworkData
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateOnly Year { get; set; }
        public string Medium { get; set; } = string.Empty;
        public string StylePeriod { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ExampleArtwork
    {
        public string ArtistName { get; set; } = string.Empty;
        public string ArtworkName { get; set; } = string.Empty;
        public ArtworkData ArtworkData { get; set; } = new();
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
    }

    public static List<ExampleArtwork> GetAllExamples()
    {
        var examples = new List<ExampleArtwork>();
        
        // Get all embedded resource names that start with the examples namespace
        var resourceNames = _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(_examplesNamespace))
            .ToArray();

        // Group resources by artist/artwork combination
        var groupedResources = resourceNames
            .GroupBy(GetArtistArtworkKey)
            .Where(g => !string.IsNullOrEmpty(g.Key));

        foreach (var group in groupedResources)
        {
            try
            {
                var parts = group.Key.Split('|');
                if (parts.Length != 2) continue;

                var artistName = FormatArtistName(parts[0]);
                var artworkName = parts[1];

                var descriptionResource = group.FirstOrDefault(r => r.EndsWith("Description.json"));
                var imageResource = group.FirstOrDefault(r => r.EndsWith("image.jpg"));

                if (descriptionResource != null && imageResource != null)
                {
                    var artworkData = LoadArtworkData(descriptionResource);
                    var imageData = LoadImageData(imageResource);

                    if (artworkData != null && imageData != null)
                    {
                        examples.Add(new ExampleArtwork
                        {
                            ArtistName = artistName,
                            ArtworkName = artworkName,
                            ArtworkData = artworkData,
                            ImageData = imageData
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing example group {group.Key}: {ex.Message}");
            }
        }

        return examples;
    }

    private static string GetArtistArtworkKey(string resourceName)
    {
        try
        {
            // Extract path after namespace: Examples.ArtistName.ArtworkName.filename
            var pathPart = resourceName.Substring(_examplesNamespace.Length + 1);
            var parts = pathPart.Split('.');
            
            if (parts.Length >= 3)
            {
                var artistName = parts[0];
                var artworkName = parts[1];
                return $"{artistName}|{artworkName}";
            }
        }
        catch
        {
            // Ignore malformed resource names
        }
        
        return string.Empty;
    }

    private static string FormatArtistName(string folderName)
    {
        // Convert folder names like "ReneMagritte" to "René Magritte"
        return folderName switch
        {
            "ReneMagritte" => "René Magritte",
            "Michelangelo" => "Michelangelo",
            _ => folderName
        };
    }

    private static ArtworkData? LoadArtworkData(string resourceName)
    {
        try
        {
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var jsonContent = reader.ReadToEnd();
                
                var jsonDoc = JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                var artworkData = new ArtworkData
                {
                    Name = GetStringProperty(root, "name"),
                    Title = GetStringProperty(root, "title"),
                    Medium = GetStringProperty(root, "medium"),
                    StylePeriod = GetStringProperty(root, "stylePeriod"),
                    Description = GetStringProperty(root, "description")
                };

                // Parse year
                var yearString = GetStringProperty(root, "year");
                if (DateTime.TryParse(yearString, out var yearDate))
                {
                    artworkData.Year = DateOnly.FromDateTime(yearDate);
                }

                return artworkData;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading artwork data from {resourceName}: {ex.Message}");
        }

        return null;
    }

    private static byte[]? LoadImageData(string resourceName)
    {
        try
        {
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading image data from {resourceName}: {ex.Message}");
        }

        return null;
    }

    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetString() ?? string.Empty;
        }
        return string.Empty;
    }
}
