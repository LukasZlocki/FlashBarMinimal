using System.Text.Json;

namespace FlashBar.Config;

public sealed record ConfigLoadResult(AppConfig Config, string? Error);

public static class ConfigLoader
{
    public static string DefaultPath { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FlashBarMinimal",
        "config.json");

    public static ConfigLoadResult Load(string path)
    {
        if (!File.Exists(path))
        {
            return new ConfigLoadResult(new AppConfig(), null);
        }

        try
        {
            using var stream = File.OpenRead(path);
            var config = JsonSerializer.Deserialize(stream, AppConfigJsonContext.Default.AppConfig);
            return new ConfigLoadResult(config ?? new AppConfig(), null);
        }
        catch (JsonException ex)
        {
            return new ConfigLoadResult(new AppConfig(), $"Invalid config file:\n{path}\n\n{ex.Message}\n\nDefault settings will be used.");
        }
        catch (IOException ex)
        {
            return new ConfigLoadResult(new AppConfig(), $"Could not read config file:\n{path}\n\n{ex.Message}\n\nDefault settings will be used.");
        }
    }
}
