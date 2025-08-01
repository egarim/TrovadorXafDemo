using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using Trovador.Module.BusinessObjects;

namespace Trovador.Module.Controllers
{

    public class AiEngine
    {
         IChatClient CurrentClient;
         string OpenAiModelId = "gpt-4o";

        public AiEngine()
        {
            CurrentClient = GetChatClientOpenAiImp(Environment.GetEnvironmentVariable("OpenAiTestKey"), OpenAiModelId);
        }
        public async Task<string> Explain(ExplanationLevel explanationLevel, Artwork artwork,string LanguageName)
        {



            var Prompt = ReplacePromptPlaceholders(explanationLevel.Prompt, artwork);

            var message=new ChatMessage(ChatRole.User, Prompt);

            DataContent dataContent = new DataContent(artwork.ArtworkImage, "image/jpg");
            message.Contents.Add(dataContent);
            List<ChatMessage> MessageHistory = new List<ChatMessage>();
            MessageHistory.Add(message);
            MessageHistory.Add(new ChatMessage(ChatRole.User, "Explain this artwork in this language: " + LanguageName));

            var Result = await CurrentClient.GetResponseAsync(MessageHistory);

            return Result.Text;



        }
        
        /// <summary>
        /// Replaces template placeholders in the prompt with actual artwork data.
        /// </summary>
        /// <param name="promptTemplate">The prompt template containing placeholders</param>
        /// <param name="artwork">The artwork object containing the data to replace placeholders with</param>
        /// <returns>The prompt with all placeholders replaced with actual data</returns>
        private static string ReplacePromptPlaceholders(string promptTemplate, Artwork artwork)
        {
            if (string.IsNullOrEmpty(promptTemplate) || artwork == null)
            {
                return promptTemplate ?? string.Empty;
            }

            var processedPrompt = promptTemplate
                .Replace("{{title}}", artwork.Title ?? string.Empty)
                .Replace("{{artist}}", artwork.Artist?.Name ?? string.Empty)
                .Replace("{{year}}", artwork.Year.Year.ToString())
                .Replace("{{medium}}", artwork.Medium ?? string.Empty)
                .Replace("{{style}}", artwork.StylePeriod ?? string.Empty)
                .Replace("{{existing_museum_text}}", artwork.Description ?? string.Empty);

            return processedPrompt;
        }

        private static IChatClient GetChatClientOpenAiImp(string ApiKey, string ModelId)
        {
            OpenAIClient openAIClient = new OpenAIClient(ApiKey);

            return openAIClient.GetChatClient(ModelId).AsIChatClient();
        }
    }

    public class PromptHelper
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static readonly string _promptsNamespace = "Trovador.Module.Prompts";

        /// <summary>
        /// Gets all prompt files from the embedded resources and returns them as a dictionary
        /// where the key is the filename (without path) and the value is the file content.
        /// </summary>
        /// <returns>Dictionary with filename as key and content as value</returns>
        public static Dictionary<string, string> GetAllPrompts()
        {
            var prompts = new Dictionary<string, string>();
            
            // Get all embedded resource names that start with the prompts namespace
            var resourceNames = _assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(_promptsNamespace))
                .ToArray();

            foreach (var resourceName in resourceNames)
            {
                try
                {
                    using var stream = _assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        var content = reader.ReadToEnd();
                        
                        // Extract just the filename from the full resource name
                        var fileName = resourceName.Substring(_promptsNamespace.Length + 1); // +1 for the dot
                        
                        prompts[fileName] = content;
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Error reading embedded resource {resourceName}: {ex.Message}");
                }
            }

            return prompts;
        }

        /// <summary>
        /// Gets a specific prompt by filename.
        /// </summary>
        /// <param name="fileName">The filename of the prompt (e.g., "Expert - deeper context.txt")</param>
        /// <returns>The content of the prompt file, or null if not found</returns>
        public static string? GetPrompt(string fileName)
        {
            var resourceName = $"{_promptsNamespace}.{fileName}";
            
            try
            {
                using var stream = _assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error reading embedded resource {resourceName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets the list of available prompt filenames.
        /// </summary>
        /// <returns>List of prompt filenames</returns>
        public static List<string> GetPromptFileNames()
        {
            return _assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(_promptsNamespace))
                .Select(name => name.Substring(_promptsNamespace.Length + 1))
                .ToList();
        }
    }
}
