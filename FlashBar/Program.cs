using FlashBar.Config;
using FlashBar.Tray;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace FlashBar;

internal static class Program
{
    private static void Main()
    {
        PInvoke.SetProcessDPIAware();

        ConfigLoadResult result = ConfigLoader.Load(ConfigLoader.DefaultPath);

        if (result.Error is { } error)
        {
            PInvoke.MessageBox(HWND.Null, error, "FlashBar", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
        }

        if (AppStore.Load(AppStore.DefaultPath) is { } appsError)
        {
            PInvoke.MessageBox(HWND.Null, appsError, "FlashBar", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
        }

        TrayApp.Run(result.Config);
    }
}
