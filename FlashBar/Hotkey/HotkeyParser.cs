namespace FlashBar.Hotkey;

internal readonly record struct Hotkey(uint Modifiers, uint VirtualKey);

internal static class HotkeyParser
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;

    // Returns null when the text is not a valid hotkey.
    public static Hotkey? Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        uint modifiers = ModNoRepeat;
        uint? key = null;

        foreach (string raw in text.Split('+'))
        {
            string part = raw.Trim();
            switch (part.ToUpperInvariant())
            {
                case "ALT":
                    modifiers |= ModAlt;
                    break;
                case "CTRL":
                case "CONTROL":
                    modifiers |= ModControl;
                    break;
                case "SHIFT":
                    modifiers |= ModShift;
                    break;
                case "WIN":
                    modifiers |= ModWin;
                    break;
                default:
                    if (key is not null || !TryParseKey(part, out uint vk))
                    {
                        return null;
                    }
                    key = vk;
                    break;
            }
        }

        return key is { } k ? new Hotkey(modifiers, k) : null;
    }

    private static bool TryParseKey(string part, out uint vk)
    {
        vk = 0;
        string upper = part.ToUpperInvariant();

        if (upper == "SPACE")
        {
            vk = 0x20;
            return true;
        }

        if (upper.Length == 1 && (char.IsAsciiLetterUpper(upper[0]) || char.IsAsciiDigit(upper[0])))
        {
            vk = upper[0];
            return true;
        }

        if (upper.Length >= 2 && upper[0] == 'F' && int.TryParse(upper.AsSpan(1), out int f) && f is >= 1 and <= 24)
        {
            vk = (uint)(0x70 + f - 1);
            return true;
        }

        return false;
    }
}
