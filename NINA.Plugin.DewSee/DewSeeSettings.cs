using System;
using System.IO;
using Newtonsoft.Json;

namespace NINA.Plugin.DewSee {

    public class DewSeeSettings {
        public double OnBelowThreshold { get; set; } = 3.5;
        public double OffAboveThreshold { get; set; } = 5.0;
        public int PollIntervalMinutes { get; set; } = 1;
        public string DiscordWebhookUrl { get; set; } = "";

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NINA", "Plugins", "DewSee", "settings.json");

        public static DewSeeSettings Load() {
            try {
                if (File.Exists(SettingsPath))
                    return JsonConvert.DeserializeObject<DewSeeSettings>(File.ReadAllText(SettingsPath)) ?? new DewSeeSettings();
            } catch { }
            return new DewSeeSettings();
        }

        public void Save() {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(this, Formatting.Indented));
            } catch { }
        }
    }
}
