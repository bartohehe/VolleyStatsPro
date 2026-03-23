using System;
using System.IO;
using System.Text.Json;

namespace VolleyStatsPro.Helpers
{
    public class AppSettings
    {
        public string DatabaseDirectory { get; set; } = DefaultDirectory;
        public string Language { get; set; } = "en";

        public static string DefaultDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "VolleyStatsPro");

        public string DatabasePath =>
            Path.Combine(DatabaseDirectory, "volleystats.db");
    }

    public static class SettingsManager
    {
        private static readonly string SettingsFile =
            Path.Combine(AppSettings.DefaultDirectory, "settings.json");

        private static AppSettings _current = Load();
        public static AppSettings Current => _current;

        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* fall back to defaults */ }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(AppSettings.DefaultDirectory);
            File.WriteAllText(SettingsFile,
                JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            _current = settings;
        }
    }
}