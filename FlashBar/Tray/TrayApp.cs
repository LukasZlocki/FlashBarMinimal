using System.Runtime.InteropServices;
using FlashBar.Config;
using FlashBar.Search;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace FlashBar.Tray;

internal static unsafe class TrayApp
{
    private const uint TrayCallbackMessage = WM_APP + 1;
    private const uint TrayIconId = 1;
    private const uint MenuExitId = 1;
    private const int HotkeyId = 1;

    private const string WindowClassName = "FlashBarMinimalHostWindow";

    // Message-only parent, same as HWND_MESSAGE (-3) in the Win32 headers.
    private static readonly HWND s_messageOnlyParent = new(new IntPtr(-3));

    private static HWND s_hwnd;

    public static AppConfig Config { get; private set; } = new();

    public static void Run(AppConfig config)
    {
        Config = config;

        HINSTANCE hInstance = GetModuleHandle((PCWSTR)null);

        fixed (char* className = WindowClassName)
        {
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = &WndProc,
                hInstance = hInstance,
                lpszClassName = new PCWSTR(className),
            };
            RegisterClassEx(wc);

            // Message-only window: never visible, no taskbar entry, receives tray and hotkey messages.
            s_hwnd = CreateWindowEx(
                0,
                new PCWSTR(className),
                new PCWSTR(className),
                0,
                0, 0, 0, 0,
                s_messageOnlyParent,
                default,
                hInstance,
                null);
        }

        AddTrayIcon();
        SearchWindow.Create(hInstance);
        RegisterSearchHotkey(config.Hotkey);

        while (GetMessage(out MSG msg, default, 0, 0))
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
        }
    }

    private static void RegisterSearchHotkey(string text)
    {
        Hotkey.Hotkey? hotkey = Hotkey.HotkeyParser.Parse(text);
        if (hotkey is not { } hk)
        {
            ShowError($"Invalid hotkey \"{text}\". Search is disabled. Example: \"Alt+Space\".");
            return;
        }

        if (!RegisterHotKey(s_hwnd, HotkeyId, (HOT_KEY_MODIFIERS)hk.Modifiers, hk.VirtualKey))
        {
            ShowError($"Hotkey \"{text}\" is already used by another application. Search is disabled.");
        }
    }

    private static void ShowError(string message) =>
        MessageBox(HWND.Null, message, "FlashBar", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONWARNING);

    private static void AddTrayIcon()
    {
        var nid = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = s_hwnd,
            uID = TrayIconId,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP,
            uCallbackMessage = TrayCallbackMessage,
            hIcon = LoadIcon(default, IDI_APPLICATION),
            szTip = "FlashBar",
        };
        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, nid);
    }

    private static void RemoveTrayIcon()
    {
        var nid = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = s_hwnd,
            uID = TrayIconId,
        };
        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, nid);
    }

    private static void ShowContextMenu(HWND hwnd)
    {
        System.Drawing.Point point;
        GetCursorPos(out point);
        SetForegroundWindow(hwnd);

        HMENU menu = CreatePopupMenu();
        fixed (char* exitText = "Exit")
        {
            AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, MenuExitId, new PCWSTR(exitText));
        }

        int command = TrackPopupMenu(
            menu,
            TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD,
            point.X,
            point.Y,
            0,
            hwnd,
            null).Value;

        DestroyMenu(menu);

        if (command == MenuExitId)
        {
            DestroyWindow(hwnd);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case TrayCallbackMessage when lParam.Value == WM_RBUTTONUP:
                ShowContextMenu(hwnd);
                return new LRESULT(0);

            case WM_HOTKEY when wParam.Value == HotkeyId:
                SearchWindow.Toggle(Config);
                return new LRESULT(0);

            case WM_DESTROY:
                UnregisterHotKey(hwnd, HotkeyId);
                RemoveTrayIcon();
                PostQuitMessage(0);
                return new LRESULT(0);
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
