using System;
using System.IO;
using System.Text.Json;

namespace TransApp;

public class AppConfig
{
    public string FromLanguage { get; set; } = "auto";
    public string ToLanguage { get; set; } = "zh-TW";
    public double FontSize { get; set; } = 18;
    public double Opacity { get; set; } = 0.8;
}

public static class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    public static AppConfig Current { get; private set; } = new();

    static ConfigService()
    {
        Load();
    }

    public static void Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                Current = JsonSerializer.Deserialize<AppConfig>(json) ?? new();
            }
            catch { Current = new(); }
        }
    }

    public static void Save()
    {
        var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
