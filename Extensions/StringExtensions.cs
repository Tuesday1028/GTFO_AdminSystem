using BepInEx;

namespace Hikaria.AdminSystem.Extensions
{
    public static class StringExtensions
    {
        public static string FormatInLength(this string str, int length)
        {
            if (str.Length == length)
            {
                return str;
            }
            else if (str.Length < length)
            {
                return str.PadRight(length);
            }
            else
            {
                return str.Substring(0, length);
            }
        }
    }
}
