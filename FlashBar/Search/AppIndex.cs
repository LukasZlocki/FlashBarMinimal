using FlashBar.Config;

namespace FlashBar.Search;

internal sealed record SearchEntry(string Name, string Target, string Arguments);

internal static class AppIndex
{
    public static List<SearchEntry> Build(AppConfig config)
    {
        var entries = new List<SearchEntry>();

        if (config.ScanStartMenu)
        {
            AddDirectory(entries, Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), recursive: true);
            AddDirectory(entries, Environment.GetFolderPath(Environment.SpecialFolder.Programs), recursive: true);
        }

        foreach (FolderEntry folder in config.Folders)
        {
            AddDirectory(entries, Expand(folder.Path), folder.Recursive);
        }

        foreach (AppEntry app in config.Apps)
        {
            string target = Expand(app.Path);
            if (string.IsNullOrWhiteSpace(target))
            {
                continue;
            }

            string name = string.IsNullOrWhiteSpace(app.Name)
                ? Path.GetFileNameWithoutExtension(target)
                : app.Name;
            entries.Add(new SearchEntry(name, target, app.Args));
        }

        entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        // Same name and target can appear in several Start Menu folders.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        entries.RemoveAll(e => !seen.Add(e.Name + "\u0001" + e.Target));

        return entries;
    }

    public static List<SearchEntry> Filter(List<SearchEntry> entries, string query, int limit)
    {
        var result = new List<SearchEntry>(limit);
        if (string.IsNullOrWhiteSpace(query))
        {
            foreach (SearchEntry e in entries)
            {
                if (result.Count >= limit) break;
                result.Add(e);
            }
            return result;
        }

        string q = query.Trim();

        foreach (SearchEntry e in entries)
        {
            if (result.Count >= limit) break;
            if (e.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase)) result.Add(e);
        }

        foreach (SearchEntry e in entries)
        {
            if (result.Count >= limit) break;
            if (e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !e.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(e);
            }
        }

        return result;
    }

    private static void AddDirectory(List<SearchEntry> entries, string directory, bool recursive)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System,
        };

        foreach (string file in Directory.EnumerateFiles(directory, "*.*", options))
        {
            string ext = Path.GetExtension(file);
            if (!ext.Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entries.Add(new SearchEntry(Path.GetFileNameWithoutExtension(file), file, ""));
        }
    }

    private static string Expand(string path) =>
        string.IsNullOrWhiteSpace(path) ? "" : Environment.ExpandEnvironmentVariables(path.Trim());
}
