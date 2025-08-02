using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Trovador.Module.BusinessObjects;

namespace Trovador.Module.Controllers
{
    public class ArtWorkController : ViewController
    {
        SingleChoiceAction Language;
        SimpleAction GenerateDescriptions;
        SimpleAction CreateNewVersion;
        SimpleAction UploadToIpfs;
        string SelectedLanguage;
        private readonly HttpClient _httpClient;

        public ArtWorkController() : base()
        {
            this.TargetObjectType = typeof(Artwork);
            this.TargetViewType = ViewType.DetailView;
            
            // Initialize HttpClient
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

            // Target required Views (use the TargetXXX properties) and create their Actions.
            GenerateDescriptions = new SimpleAction(this, "Generate Descriptions", "View");
            GenerateDescriptions.Execute += GenerateDescriptions_Execute;

            CreateNewVersion = new SimpleAction(this, "Create New Version", "Edit");
            CreateNewVersion.Execute += CreateNewVersion_Execute;

            UploadToIpfs = new SimpleAction(this, "Upload to IPFS", "Tools");
            UploadToIpfs.Execute += UploadToIpfs_Execute;

            Language = new SingleChoiceAction(this, "Language", "View");
            Language.ItemType = SingleChoiceActionItemType.ItemIsMode;
            Language.Execute += Language_Execute;
            // Create some items
            //Language.Items.Add(new ChoiceActionItem("MyItem1", "My Item 1", 1));
            Language.Items.Add(new ChoiceActionItem("Russian", "Russian", 9));
            Language.Items.Add(new ChoiceActionItem("English", "English", 1));
            Language.Items.Add(new ChoiceActionItem("Spanish", "Spanish", 2));
            Language.Items.Add(new ChoiceActionItem("French", "French", 3));
            Language.Items.Add(new ChoiceActionItem("German", "German", 4));
            Language.Items.Add(new ChoiceActionItem("Italian", "Italian", 5));
            Language.Items.Add(new ChoiceActionItem("Portuguese", "Portuguese", 6));
            Language.Items.Add(new ChoiceActionItem("Chinese", "Chinese", 7));
            Language.Items.Add(new ChoiceActionItem("Japanese", "Japanese", 8));
        }

        private void Language_Execute(object sender, SingleChoiceActionExecuteEventArgs e)
        {
            SelectedLanguage = e.SelectedChoiceActionItem.Caption;
            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112738/).
        }

        private async void GenerateDescriptions_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            IEnumerable<ExplanationLevel> Levels = this.ObjectSpace.CreateCollection(typeof(ExplanationLevel)).Cast<ExplanationLevel>();
            Artwork artwork = this.View.CurrentObject as Artwork; 
            AiEngine AiEngine = new AiEngine();
            foreach (ExplanationLevel explanationLevel in Levels)
            {
                var text = await AiEngine.Explain(explanationLevel, artwork, SelectedLanguage);
                ArtworkDescription description = this.ObjectSpace.CreateObject<ArtworkDescription>();
                description.Artwork = artwork;
                description.Language = SelectedLanguage;
                description.ExplanationLevel = explanationLevel;
                description.Text = text;
            }
            this.ObjectSpace.CommitChanges();
            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112737/).
        }

        private void CreateNewVersion_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            Artwork artwork = this.View.CurrentObject as Artwork;
            if (artwork == null) return;

            try
            {
                var newVersion = this.ObjectSpace.CreateObject<ArtworkVersion>();
                newVersion.Artwork = artwork;
                newVersion.CreatedBy = SecuritySystem.CurrentUserName ?? "System";
                newVersion.Description = "New version";

                // Copy current image if exists
                if (artwork.ArtworkImage != null)
                {
                    newVersion.ImageData = artwork.ArtworkImage;
                }

                // Mark as current version
                newVersion.IsCurrentVersion = true;

                this.ObjectSpace.CommitChanges();
                Application.ShowViewStrategy.ShowMessage("New version created successfully.", InformationType.Success);
            }
            catch (Exception ex)
            {
                Application.ShowViewStrategy.ShowMessage($"Error creating new version: {ex.Message}", InformationType.Error);
            }
        }

        private async void UploadToIpfs_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            Artwork artwork = this.View.CurrentObject as Artwork;
            
            // Check if IPFS is enabled via ApplicationSettings
            var settings = GetApplicationSettings();
            if (!settings.IpfsEnabled)
            {
                Application.ShowViewStrategy.ShowMessage("IPFS is disabled in application settings. Please enable it in Administration > Application Settings.", InformationType.Warning);
                return;
            }

            var currentVersion = artwork?.ArtworkVersions?.FirstOrDefault(v => v.IsCurrentVersion);

            if (currentVersion?.ImageData == null)
            {
                Application.ShowViewStrategy.ShowMessage("No current version with image data found.", InformationType.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(currentVersion.IpfsHash))
            {
                var publicUrl = GetPublicIpfsUrl(currentVersion.IpfsHash);
                Application.ShowViewStrategy.ShowMessage($"Current version is already uploaded to IPFS.\nHash: {currentVersion.IpfsHash}\nPublic URL: {publicUrl}", InformationType.Info);
                return;
            }

            try
            {
                // Check IPFS availability
                if (!await IsIpfsAvailableAsync())
                {
                    Application.ShowViewStrategy.ShowMessage("IPFS service is not available. Please check your IPFS node is running.", InformationType.Error);
                    return;
                }

                currentVersion.IpfsUploadStatus = IpfsUploadStatus.Uploading;
                this.ObjectSpace.CommitChanges();

                var fileName = $"{artwork.Name}_v{currentVersion.VersionNumber}.jpg";
                var ipfsHash = await UploadToIpfsAsync(currentVersion.ImageData, fileName);

                currentVersion.IpfsHash = ipfsHash;
                currentVersion.IpfsUploadDate = DateTime.Now;
                currentVersion.IpfsUploadStatus = IpfsUploadStatus.Uploaded;

                this.ObjectSpace.CommitChanges();
                
                var publicUrl = GetPublicIpfsUrl(ipfsHash);
                
                // Verify the upload
                var verificationResult = await VerifyIpfsUpload(ipfsHash);
                var verificationMessage = verificationResult ? "✓ Verified in local node" : "⚠ Could not verify in local node";
                
                Application.ShowViewStrategy.ShowMessage($"Successfully uploaded to IPFS!\nHash: {ipfsHash}\nPublic URL: {publicUrl}\nLocal Gateway: http://localhost:8080/ipfs/{ipfsHash}\n{verificationMessage}", InformationType.Success);
            }
            catch (Exception ex)
            {
                if (currentVersion != null)
                {
                    currentVersion.IpfsUploadStatus = IpfsUploadStatus.Failed;
                    this.ObjectSpace.CommitChanges();
                }
                Application.ShowViewStrategy.ShowMessage($"Failed to upload to IPFS: {ex.Message}", InformationType.Error);
            }
        }

        private ApplicationSettings GetApplicationSettings()
        {
            return ApplicationSettings.GetInstance(this.ObjectSpace);
        }

        private async Task<bool> IsIpfsAvailableAsync()
        {
            try
            {
                return true;
                //var settings = GetApplicationSettings();
                //var response = await _httpClient.GetAsync($"{settings.IpfsNodeUrl}/api/v0/version");
                //return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> UploadToIpfsAsync(byte[] imageData, string fileName)
        {
            try
            {
                var settings = GetApplicationSettings();
                using var content = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(imageData);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(fileContent, "file", fileName);

                // Add API key header if provided (for hosted services like Pinata)
                if (!string.IsNullOrEmpty(settings.IpfsApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.IpfsApiKey);
                }

                var pinParam = settings.IpfsAutoPin ? "?pin=true" : "";
                var response = await _httpClient.PostAsync($"{settings.IpfsNodeUrl}/api/v0/add{pinParam}", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"IPFS upload failed with status {response.StatusCode}: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Parse the JSON response to get the hash
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var hash = jsonDoc.RootElement.GetProperty("Hash").GetString();

                if (string.IsNullOrEmpty(hash))
                {
                    throw new InvalidOperationException("IPFS returned empty hash");
                }

                return hash;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to upload to IPFS: {ex.Message}", ex);
            }
        }

        private string GetPublicIpfsUrl(string hash)
        {
            var settings = GetApplicationSettings();
            var gatewayUrl = settings.IpfsGatewayUrl ?? "https://ipfs.io/ipfs/";
            return $"{gatewayUrl.TrimEnd('/')}/{hash}";
        }

        private async Task<bool> VerifyIpfsUpload(string hash)
        {
            try
            {
                var settings = GetApplicationSettings();
                // Try to get object stats to verify the file exists
                var response = await _httpClient.GetAsync($"{settings.IpfsNodeUrl}/api/v0/object/stat?arg={hash}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> GetIpfsPinStatus(string hash)
        {
            try
            {
                var settings = GetApplicationSettings();
                var response = await _httpClient.GetAsync($"{settings.IpfsNodeUrl}/api/v0/pin/ls?arg={hash}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return content.Contains(hash) ? "Pinned" : "Not Pinned";
                }
                return "Unknown";
            }
            catch
            {
                return "Error checking pin status";
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            this.Language.SelectedItem = this.Language.Items.FirstOrDefault();
            if (this.Language.SelectedItem != null)
            {
                this.SelectedLanguage = this.Language.SelectedItem.Caption;
            }
            UpdateActionAvailability();
            // Perform various tasks depending on the target View.
        }

        private void UpdateActionAvailability()
        {
            Artwork artwork = this.View?.CurrentObject as Artwork;
            var settings = GetApplicationSettings();

            CreateNewVersion.Enabled["HasArtwork"] = artwork != null;
            
            var currentVersion = artwork?.ArtworkVersions?.FirstOrDefault(v => v.IsCurrentVersion);
            UploadToIpfs.Enabled["HasCurrentVersionAndIpfsEnabled"] = 
                currentVersion?.ImageData != null && settings.IpfsEnabled;
        }

        protected override void OnDeactivated()
        {
            // Unsubscribe from previously subscribed events and release other references and resources.
            base.OnDeactivated();
        }

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            UpdateActionAvailability();
            // Access and customize the target View control.
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
