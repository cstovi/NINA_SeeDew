using System;
using System.IO;
using Newtonsoft.Json;

namespace NINA.Plugin.SeeDew {

    public class SeeDewSettings {
        public double OnBelowThreshold { get; set; } = 3.5;
        public double OffAboveThreshold { get; set; } = 5.0;
        public int PollIntervalMinutes { get; set; } = 1;
        public string DiscordWebhookUrl { get; set; } = "";

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NINA", "Plugins", "SeeDew", "settings.json");

        public static SeeDewSettings Load() {
            try {
                if (File.Exists(SettingsPath))
                    return JsonConvert.DeserializeObject<SeeDewSettings>(File.ReadAllText(SettingsPath)) ?? new SeeDewSettings();
            } catch { }
            return new SeeDewSettings();
        }

        public void Save() {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(this, Formatting.Indented));
            } catch { }
        }
    }
}
