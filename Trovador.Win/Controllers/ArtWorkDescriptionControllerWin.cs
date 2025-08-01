using DevExpress.ExpressApp.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trovador.Module.BusinessObjects;
using Trovador.Module.Controllers;
using System.Speech.Synthesis;
using System.Globalization;
using DevExpress.ExpressApp;

namespace Trovador.Win.Controllers
{
    public class ArtWorkDescriptionControllerWin : ArtWorkDescriptionController
    {
        private SpeechSynthesizer speechSynthesizer;

        public ArtWorkDescriptionControllerWin()
        {
            speechSynthesizer = new SpeechSynthesizer();
        }

        protected override void ToAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            ArtworkDescription artworkDescription = View.CurrentObject as ArtworkDescription;
            
            if (artworkDescription == null)
            {
                Application.ShowViewStrategy.ShowMessage("No artwork description selected.", InformationType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(artworkDescription.Text))
            {
                Application.ShowViewStrategy.ShowMessage("No text to speak.", InformationType.Warning);
                return;
            }

            try
            {
                // Stop any current speech
                speechSynthesizer.SpeakAsyncCancelAll();

                // Set voice based on language if available
                SetVoiceByLanguage(artworkDescription.Language);

                // Speak the text asynchronously
                speechSynthesizer.SpeakAsync(artworkDescription.Text);
            }
            catch (Exception ex)
            {
                Application.ShowViewStrategy.ShowMessage($"Error playing audio: {ex.Message}", InformationType.Error);
            }
        }

        // Helper method to display available voices (you can call this for debugging)
        public void ShowAvailableVoices()
        {
            var voices = speechSynthesizer.GetInstalledVoices();
            var voiceInfo = string.Join("\n", voices.Select(v => 
                $"Name: {v.VoiceInfo.Name}\n" +
                $"Culture: {v.VoiceInfo.Culture.Name}\n" +
                $"Gender: {v.VoiceInfo.Gender}\n" +
                $"Age: {v.VoiceInfo.Age}\n" +
                $"Enabled: {v.Enabled}\n" +
                "---"));
            
            Application.ShowViewStrategy.ShowMessage($"Available Voices:\n\n{voiceInfo}", InformationType.Info);
        }

        private void SetVoiceByLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return;

            try
            {
                // Get all installed voices
                var voices = speechSynthesizer.GetInstalledVoices();

                // Debug: Log available voices (remove this in production)
                System.Diagnostics.Debug.WriteLine($"Available voices ({voices.Count}):");
                foreach (var voice in voices)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {voice.VoiceInfo.Name} ({voice.VoiceInfo.Culture.Name}) - {voice.VoiceInfo.Gender}");
                }

                // Language mapping for common languages
                var languageMappings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    { "English", new[] { "en-US", "en-GB", "en" } },
                    { "Spanish", new[] { "es-ES", "es-MX", "es" } },
                    { "French", new[] { "fr-FR", "fr-CA", "fr" } },
                    { "German", new[] { "de-DE", "de" } },
                    { "Italian", new[] { "it-IT", "it" } },
                    { "Portuguese", new[] { "pt-PT", "pt-BR", "pt" } },
                    { "Russian", new[] { "ru-RU", "ru" } },
                    { "Chinese", new[] { "zh-CN", "zh-TW", "zh" } },
                    { "Japanese", new[] { "ja-JP", "ja" } }
                };

                // Try to find a voice for the specified language
                if (languageMappings.TryGetValue(language, out var cultureCodes))
                {
                    foreach (var cultureCode in cultureCodes)
                    {
                        var voice = voices.FirstOrDefault(v => 
                            v.VoiceInfo.Culture.Name.StartsWith(cultureCode, StringComparison.OrdinalIgnoreCase) ||
                            v.VoiceInfo.Culture.TwoLetterISOLanguageName.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));

                        if (voice != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Selected voice: {voice.VoiceInfo.Name} for language: {language}");
                            speechSynthesizer.SelectVoice(voice.VoiceInfo.Name);
                            return;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"No voice found for language: {language}, using default voice");

                // If no specific voice found, try direct culture lookup
                try
                {
                    var culture = CultureInfo.GetCultureInfo(language);
                    var voice = voices.FirstOrDefault(v => v.VoiceInfo.Culture.Equals(culture));
                    if (voice != null)
                    {
                        speechSynthesizer.SelectVoice(voice.VoiceInfo.Name);
                    }
                }
                catch
                {
                    // If culture lookup fails, use default voice
                }
            }
            catch
            {
                // If voice selection fails, continue with default voice
            }
        }

        protected override void OnDeactivated()
        {
            // Stop any ongoing speech when the controller is deactivated
            speechSynthesizer?.SpeakAsyncCancelAll();
            base.OnDeactivated();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                speechSynthesizer?.SpeakAsyncCancelAll();
                speechSynthesizer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
