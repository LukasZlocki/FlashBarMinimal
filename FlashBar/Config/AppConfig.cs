using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlashBar.Config;

public sealed class AppConfig
{
    public string Hotkey { get; set; } = "Alt+Space";

    public bool ScanStartMenu { get; set; } = true;

    public List<FolderEntry> Folders { get; set; } = [];
}

public sealed class FolderEntry
{
    public string Path { get; set; } = "";

    public bool Recursive { get; set; }
}

public sealed class AppEntry
{
    public string? Name { get; set; }

    public string Path { get; set; } = "";

    public string Args { get; set; } = "";
}

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    WriteIndented = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(List<AppEntry>))]
internal sealed partial class AppConfigJsonContext : JsonSerializerContext
{
}
