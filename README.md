# FlashBarMinimal

A lightweight, resident app launcher for Windows. Press the hotkey, type an app name, and press Enter to launch it.

## Build

Requires the .NET 10 SDK and the "Desktop development with C++" workload (needed for Native AOT linking).

```powershell
dotnet publish FlashBar\FlashBar.csproj -c Release -r win-x64
```

The self-contained native executable is written to `FlashBar\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\FlashBar.exe`.

## Configuration

The app reads its settings from `%APPDATA%\FlashBarMinimal\config.json`. See [flashbar.sample.json](flashbar.sample.json) for a complete example.

| Field | Type | Default | Description |
|---|---|---|---|
| `hotkey` | string | `"Alt+Space"` | Global activation hotkey. Win+Space is reserved by Windows and can't be used. |
| `scanStartMenu` | bool | `true` | Index shortcuts from the per-user and all-users Start Menu folders. |
| `folders` | array | `[]` | Extra folders to scan for `.exe` and `.lnk` files. |
| `folders[].path` | string | required | Folder path. Backslashes must be escaped in JSON (`C:\\Tools`). |
| `folders[].recursive` | bool | `false` | Whether to include subfolders. |
| `apps` | array | `[]` | Individual executables to add. |
| `apps[].name` | string | file name without extension | Name shown in search results. |
| `apps[].path` | string | required | Full path to the executable. |
| `apps[].args` | string | `""` | Optional command-line arguments. |

If the file is missing, the app starts with the defaults. Invalid JSON should be reported, not silently ignored.

Config changes take effect after restarting FlashBar. The search index is rebuilt each time the search window opens.

## Usage

- Press **Alt+Space** to open the search box. FlashBar runs in the background with a tray icon.
- Type to filter. Prefix matches are listed before substring matches.
- **Up** / **Down** select a result, **Enter** launches it, **Esc** closes the box.
- The box also closes when it loses focus.
- Right-click the tray icon and choose Exit to quit.

Known limitations:

- **Alt+Space** replaces the standard window system menu shortcut in every application while FlashBar runs.
- Paste (Ctrl+V) is not supported, because the search box is custom-drawn.
- The hotkey must not be used by another application. If it is, FlashBar shows an error and search is disabled.
