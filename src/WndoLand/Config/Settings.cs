using System.Text.Json;
using System.Text.Json.Serialization;

namespace WndoLand
{
    internal class Settings
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static readonly Color DefaultBorderColor = Color.FromArgb(0x4F, 0xC3, 0xF7);

        [JsonPropertyName("border_color")]
        public string BorderColor { get; set; } = "#4FC3F7";

        [JsonPropertyName("border_thickness")]
        public int BorderThickness { get; set; } = 2;

        [JsonPropertyName("strip_title_bars")]
        public bool StripTitleBars { get; set; } = true;

        [JsonPropertyName("gap")]
        public int Gap { get; set; } = 0;

        [JsonIgnore]
        public Color BorderColorValue
        {
            get
            {
                try { return ColorTranslator.FromHtml(BorderColor); }
                catch { return DefaultBorderColor; }
            }
        }

        public static Settings Current { get; private set; } = Load();

        private static Settings Load()
        {
            try
            {
                if (File.Exists(AppPaths.SettingsFile))
                    return JsonSerializer.Deserialize<Settings>(File.ReadAllText(AppPaths.SettingsFile)) ?? new Settings();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Couldn't read {AppPaths.SettingsFile}, using defaults");
            }
            return new Settings();
        }

        public void Save()
        {
            File.WriteAllText(AppPaths.SettingsFile, JsonSerializer.Serialize(this, JsonOptions));
            Current = this;
            Log.Info($"Saved settings: color={BorderColor} thickness={BorderThickness} strip={StripTitleBars} gap={Gap}");
        }
    }
}
