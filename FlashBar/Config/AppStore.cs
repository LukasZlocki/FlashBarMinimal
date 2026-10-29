using System.Text.Json;

namespace FlashBar.Config;

public static class AppStore
{
    public static string DefaultPath { get; } = Path.Combine(AppContext.BaseDirectory, "apps.json");

    private static string s_path = DefaultPath;
    private static bool s_readable;

    public static List<AppEntry> Apps { get; private set; } = [];

    // Returns an error message when the file exists but cannot be read.
    public static string? Load(string path)
    {
        s_path = path;

        if (!File.Exists(path))
        {
            s_readable = true;
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            Apps = JsonSerializer.Deserialize(stream, AppConfigJsonContext.Default.ListAppEntry) ?? [];
            s_readable = true;
            return null;
        }
        catch (JsonException ex)
        {
            return $"Invalid apps file:\n{path}\n\n{ex.Message}\n\nNew apps will not be saved until this file is fixed.";
        }
        catch (IOException ex)
        {
            return $"Could not read apps file:\n{path}\n\n{ex.Message}\n\nNew apps will not be saved until this file is fixed.";
        }
    }

    // Adds the app and saves the file. Does nothing if the same path is already stored.
    public static void Add(AppEntry app)
    {
        if (Apps.Any(a => a.Path.Equals(app.Path, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        if (!s_readable)
        {
            throw new InvalidOperationException("The apps file could not be read, so the app was not saved.");
        }

        var updated = new List<AppEntry>(Apps) { app };
        Write(updated);
        Apps = updated;
    }

    private static void Write(List<AppEntry> apps)
    {
        string? directory = Path.GetDirectoryName(s_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temporary file first so a failed write never leaves a half-written apps.json.
        string temp = s_path + ".tmp";
        using (FileStream stream = File.Create(temp))
        {
            JsonSerializer.Serialize(stream, apps, AppConfigJsonContext.Default.ListAppEntry);
        }

        File.Move(temp, s_path, overwrite: true);
    }
}
