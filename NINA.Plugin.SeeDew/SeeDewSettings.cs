using System;
using System.IO;
using Newtonsoft.Json;
using NINA.Core.Utility;

namespace NINA.Plugin.SeeDew {

    public class SeeDewSettings {
        public double OnBelowThreshold { get; set; } = 3.5;
        public double OffAboveThreshold { get; set; } = 5.0;
        public int PollIntervalMinutes { get; set; } = 1;
        public string DiscordWebhookUrl { get; set; } = "";

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NINA", "SeeDew", "settings.json");

        public static SeeDewSettings Load() {
            try {
                if (File.Exists(SettingsPath))
                    return JsonConvert.DeserializeObject<SeeDewSettings>(File.ReadAllText(SettingsPath)) ?? new SeeDewSettings();
            } catch (Exception ex) {
                Logger.Warning($"[SeeDew] Settings load failed: {ex.Message}");
            }
            return new SeeDewSettings();
        }

        public void Save() {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(this, Formatting.Indented));
            } catch (Exception ex) {
                Logger.Warning($"[SeeDew] Settings save failed: {ex.Message}");
            }
        }
    }
}
