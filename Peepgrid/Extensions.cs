using Kieker.DllImport;
using System.Drawing;

namespace Kieker
{
    public static class ExtensionsClass
    {
        public static string Max(this string s, int length)
        {
            return length <= s.Length ? s.Substring(0, length) : s;
        }

        /// <summary>
        /// Returns this String with a maximum length of <i>length</i> characters.
        /// </summary>
        /// <param name="s">this</param>
        /// <param name="length">max length</param>
        /// <param name="append">string to append if the string had to be truncated</param>
        /// <returns>The string with the given string to append in case it had to be truncated.</returns>
        public static string Max(this string s, int length, string append)
        {
            if (length <= s.Length)
            {
                return s.Substring(0, length) + (append != null ? append : "");
            }
            else
            {
                return s;
            }
        }

        public static Rectangle ToRectangle(this Rect rect)
        {
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        public static Rect ToRect(this Rectangle rect)
        {
            return new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public static Rectangle GetNormalized(this Rectangle rect)
        {
            return new Rectangle(0, 0, rect.Width, rect.Height);
        }
    }
}