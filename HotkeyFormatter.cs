using System.Collections.Generic;
using System.Windows.Forms;

namespace Cropalicious
{
    internal static class HotkeyFormatter
    {
        public static string Format(Keys modifiers, Keys key)
        {
            var parts = new List<string>();

            if ((modifiers & Keys.Control) != 0) parts.Add("Ctrl");
            if ((modifiers & Keys.Alt) != 0) parts.Add("Alt");
            if ((modifiers & Keys.Shift) != 0) parts.Add("Shift");

            parts.Add(FormatKey(key));
            return string.Join("+", parts);
        }

        public static bool IsModifierKey(Keys key)
        {
            return key is Keys.ControlKey
                or Keys.LControlKey
                or Keys.RControlKey
                or Keys.ShiftKey
                or Keys.LShiftKey
                or Keys.RShiftKey
                or Keys.Menu
                or Keys.LMenu
                or Keys.RMenu
                or Keys.LWin
                or Keys.RWin;
        }

        private static string FormatKey(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
                return key.ToString();

            if (key >= Keys.D0 && key <= Keys.D9)
                return ((int)key - (int)Keys.D0).ToString();

            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return $"Num {(int)key - (int)Keys.NumPad0}";

            return key switch
            {
                Keys.Return => "Enter",
                Keys.Escape => "Esc",
                Keys.Back => "Backspace",
                Keys.Space => "Space",
                Keys.Prior => "Page Up",
                Keys.Next => "Page Down",
                Keys.Up => "Up",
                Keys.Down => "Down",
                Keys.Left => "Left",
                Keys.Right => "Right",
                Keys.Oemtilde => "`",
                Keys.OemMinus => "-",
                Keys.Oemplus => "=",
                Keys.OemOpenBrackets => "[",
                Keys.OemCloseBrackets => "]",
                Keys.OemPipe => "\\",
                Keys.OemSemicolon => ";",
                Keys.OemQuotes => "'",
                Keys.Oemcomma => ",",
                Keys.OemPeriod => ".",
                Keys.OemQuestion => "/",
                _ => key.ToString()
            };
        }
    }
}
