using System.Runtime.InteropServices;
using FlashBar.Config;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace FlashBar.Search;

internal static unsafe class SearchWindow
{
    private const string WindowClassName = "FlashBarSearchWindow";
    private const int WindowWidth = 560;
    private const int QueryHeight = 40;
    private const int RowHeight = 28;
    private const int VisibleRows = 8;
    private const int TextPadding = 10;
    private const int CornerRadius = 16;

    private const uint BackgroundColor = 0x2D2D2D;
    private const uint SelectionColor = 0x505050;
    private const uint TextColor = 0xE6E6E6;
    private const uint HintTextColor = 0x969696;
    private const string FontFaceName = "Consolas";
    private const int FontHeight = -16;
    private const int FontWeightNormal = 400;

    private static HBRUSH s_backgroundBrush;
    private static HBRUSH s_selectionBrush;
    private static DeleteObjectHFONTSafeHandle s_font = null!;
    private static HWND s_hwnd;
    private static List<SearchEntry> s_index = [];
    private static List<SearchEntry> s_results = [];
    private static string s_query = "";
    private static int s_selected;
    private static bool s_visible;

    public static void Create(HINSTANCE hInstance)
    {
        s_backgroundBrush = CreateSolidBrush(new COLORREF(BackgroundColor));
        s_selectionBrush = CreateSolidBrush(new COLORREF(SelectionColor));
        s_font = CreateFont(FontHeight, 0, 0, 0, FontWeightNormal, 0, 0, 0,
            FONT_CHARSET.DEFAULT_CHARSET, (uint)FONT_OUTPUT_PRECISION.OUT_DEFAULT_PRECIS,
            (uint)FONT_CLIP_PRECISION.CLIP_DEFAULT_PRECIS, FONT_QUALITY.CLEARTYPE_QUALITY,
            (uint)FONT_PITCH.FIXED_PITCH | (uint)FONT_FAMILY.FF_MODERN, FontFaceName);

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

            s_hwnd = CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_TOPMOST,
                new PCWSTR(className),
                new PCWSTR(className),
                WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_BORDER,
                0, 0, WindowWidth, QueryHeight + RowHeight * VisibleRows,
                default,
                default,
                hInstance,
                null);
        }

        if (s_hwnd == HWND.Null)
        {
            MessageBox(HWND.Null, "Could not create the search window.", "FlashBar", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
        }
    }

    public static void Toggle(AppConfig config)
    {
        if (s_visible)
        {
            Hide();
        }
        else
        {
            Show(config);
        }
    }

    private static void Show(AppConfig config)
    {
        s_index = AppIndex.Build(config);
        s_query = "";
        s_selected = 0;
        Refresh();

        int screenW = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
        int screenH = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
        int height = QueryHeight + RowHeight * VisibleRows;
        MoveWindow(s_hwnd, (screenW - WindowWidth) / 2, (screenH - height) / 2, WindowWidth, height, true);
        ApplyRoundedCorners(WindowWidth, height);

        s_visible = true;
        ShowWindow(s_hwnd, SHOW_WINDOW_CMD.SW_SHOW);
        SetForegroundWindow(s_hwnd);
    }

    private static void ApplyRoundedCorners(int width, int height)
    {
        // The system owns the region after SetWindowRgn, so it is not deleted here.
        HRGN region = CreateRoundRectRgn(0, 0, width + 1, height + 1, CornerRadius, CornerRadius);
        SetWindowRgn(s_hwnd, region, true);
    }

    private static void Hide()
    {
        s_visible = false;
        ShowWindow(s_hwnd, SHOW_WINDOW_CMD.SW_HIDE);
    }

    private static void Refresh()
    {
        s_results = AppIndex.Filter(s_index, s_query, VisibleRows);
        if (s_selected >= s_results.Count)
        {
            s_selected = Math.Max(0, s_results.Count - 1);
        }
        InvalidateRect(s_hwnd, (RECT*)null, true);
    }

    private static void Launch()
    {
        if (s_results.Count == 0)
        {
            return;
        }

        SearchEntry entry = s_results[s_selected];
        Hide();

        fixed (char* verb = "open")
        fixed (char* file = entry.Target)
        fixed (char* args = entry.Arguments)
        {
            ShellExecute(
                HWND.Null,
                new PCWSTR(verb),
                new PCWSTR(file),
                entry.Arguments.Length > 0 ? new PCWSTR(args) : default,
                default(PCWSTR),
                SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        }
    }

    private static void Paint(HWND hwnd)
    {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hwnd, out ps);

        GetClientRect(hwnd, out RECT client);
        FillRect(hdc, &client, s_backgroundBrush);

        SelectObject(hdc, new HFONT(s_font.DangerousGetHandle()));
        SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);

        var queryRect = new RECT { left = 0, top = 0, right = client.right, bottom = QueryHeight };
        string queryText = s_query;
        COLORREF textColor = new(TextColor);
        if (queryText.Length == 0)
        {
            queryText = "Type an app name...";
            textColor = new(HintTextColor);
        }
        DrawLine(hdc, queryText, queryRect, textColor);

        for (int i = 0; i < s_results.Count; i++)
        {
            var row = new RECT
            {
                left = 0,
                top = QueryHeight + i * RowHeight,
                right = client.right,
                bottom = QueryHeight + (i + 1) * RowHeight,
            };

            bool selected = i == s_selected;
            if (selected)
            {
                FillRect(hdc, &row, s_selectionBrush);
            }

            COLORREF color = new(TextColor);
            DrawLine(hdc, s_results[i].Name, row, color);
        }

        EndPaint(hwnd, ps);
    }

    private static void DrawLine(HDC hdc, string text, RECT rect, COLORREF color)
    {
        SetTextColor(hdc, color);
        rect.left += TextPadding;
        rect.right -= TextPadding;
        fixed (char* p = text)
        {
            DrawText(hdc, new PCWSTR(p), text.Length, ref rect,
                DRAW_TEXT_FORMAT.DT_SINGLELINE | DRAW_TEXT_FORMAT.DT_VCENTER | DRAW_TEXT_FORMAT.DT_LEFT | DRAW_TEXT_FORMAT.DT_END_ELLIPSIS);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case WM_PAINT:
                Paint(hwnd);
                return new LRESULT(0);

            case WM_CHAR:
                HandleChar((char)wParam.Value);
                return new LRESULT(0);

            case WM_KEYDOWN:
                HandleKey((VIRTUAL_KEY)wParam.Value);
                return new LRESULT(0);

            case WM_ACTIVATE when (wParam.Value & 0xFFFF) == WA_INACTIVE:
                if (s_visible)
                {
                    Hide();
                }
                return new LRESULT(0);
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private static void HandleChar(char ch)
    {
        if (ch == '\b')
        {
            if (s_query.Length > 0)
            {
                s_query = s_query[..^1];
                s_selected = 0;
                Refresh();
            }
        }
        else if (ch >= ' ')
        {
            s_query += ch;
            s_selected = 0;
            Refresh();
        }
    }

    private static void HandleKey(VIRTUAL_KEY key)
    {
        switch (key)
        {
            case VIRTUAL_KEY.VK_ESCAPE:
                Hide();
                break;
            case VIRTUAL_KEY.VK_RETURN:
                Launch();
                break;
            case VIRTUAL_KEY.VK_DOWN:
                if (s_selected < s_results.Count - 1)
                {
                    s_selected++;
                    InvalidateRect(s_hwnd, (RECT*)null, true);
                }
                break;
            case VIRTUAL_KEY.VK_UP:
                if (s_selected > 0)
                {
                    s_selected--;
                    InvalidateRect(s_hwnd, (RECT*)null, true);
                }
                break;
        }
    }
}
