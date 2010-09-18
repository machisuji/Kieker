using Kieker.DllImport;
using System.Drawing;
using System;
using System.Threading;

namespace Kieker
{
    public static class ExtensionsClass
    {
        public static void Fork(this Action action)
        {
            Thread thread = new Thread(new ThreadStart(action));
            thread.IsBackground = true;
            thread.Start();
        }

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

        public static Rectangle GetScaled(this Rectangle rect, double factor)
        {
            int width = (int)Math.Round(rect.Width * factor, 0);
            int height = (int)Math.Round(rect.Height * factor, 0);

            return new Rectangle(rect.X, rect.Y, width, height);
        }

        public static Rectangle GetExpanded(this Rectangle rect, int dw, int dh)
        {
            return new Rectangle(
                rect.X - (dw / 2), rect.Y - (dh / 2),
                rect.Width + dw, rect.Height + dh);
        }

        public static int Area(this Rectangle rect)
        {
            return rect.Width * rect.Height;
        }

        /// <summary>
        /// Takes a linear value and eases it in using a function raising x to the given power.
        /// For instance a quadratic function if the value for power is 2.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="power"></param>
        /// <returns></returns>
        public static double EaseIn(this double x, int power)
        {
            return Power.EaseIn(x, power);
        }

        /// <summary>
        /// Takes a linear value and eases it out using a function raising x to the given power.
        /// For instance a quadratic function if the value for power is 2.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="power"></param>
        /// <returns></returns>
        public static double EaseOut(this double x, int power)
        {
            return Power.EaseOut(x, power);
        }

        /// <summary>
        /// Takes a linear value and eases it in and out using a function raising x to the given power.
        /// For instance a quadratic function if the value for power is 2.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="power"></param>
        /// <returns></returns>
        public static double EaseInOut(this double x, int power)
        {
            return Power.EaseInOut(x, power);
        }
    }

    static class Power
    {
        public static float EaseIn(double s, int power)
        {
            return (float)Math.Pow(s, power);
        }
        public static float EaseOut(double s, int power)
        {
            var sign = power % 2 == 0 ? -1 : 1;
            return (float)(sign * (Math.Pow(s - 1, power) + sign));
        }
        public static float EaseInOut(double s, int power)
        {
            s *= 2;
            if (s < 1) return EaseIn(s, power) / 2;
            var sign = power % 2 == 0 ? -1 : 1;
            return (float)(sign / 2.0 * (Math.Pow(s - 2, power) + sign * 2));
        }
    }
}