using Kieker.DllImport;
using System.Drawing;
using System;
using System.Threading;
using System.Collections.Generic;

namespace Kieker
{
    public static class ExtensionsClass
    {
        public static ICollection<T> Join<T>(this ICollection<T> these, ICollection<T> those)
        {
            ICollection<T> all = new List<T>(these);
            foreach (T item in those) all.Add(item);
            return all;
        }

        public static void MoveToEnd<T>(this ICollection<T> items, T item)
        {
            items.Remove(item);
            items.Add(item);
        }

        /// <summary>
        /// Tries to find an item fulfilling the specified predicate.
        /// If there is no such item, one is created (using the default constructor)
        /// and added to the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="predicate"></param>
        /// <returns>The found or created item.</returns>
        public static T FindOrCreate<T>(this ICollection<T> items, Predicate<T> predicate)
        {
            T item = default(T);
            foreach (T ci in items)
            {
                if (predicate.Invoke(ci))
                    item = ci;
            }
            if (item == null)
            {
                item = Activator.CreateInstance<T>();
                items.Add(item);
            }
            return item;
        }

        /// <summary>
        /// Tries to find an item fulfilling the specified predicate.
        /// If there is no such item, one is created (using the given function)
        /// and added to the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="predicate"></param>
        /// <param name="creation"></param>
        /// <returns>The found or created item.</returns>
        public static T FindOrCreate<T>(this ICollection<T> items, Predicate<T> predicate, Func<T> creation)
        {
            return FindOrCreateIf<T>(items, predicate, creation, t => true);
        }

        public static T FindOrCreateIf<T>(this ICollection<T> items, Predicate<T> find, Func<T> create,
            Predicate<T> providedThat)
        {
            foreach (T item in items)
            {
                if (find.Invoke(item))
                    return item;
            }
            T newItem = create.Invoke();
            if (providedThat.Invoke(newItem))
                items.Add(newItem);
            return newItem;
        }

        public static void Fork(this Action action, String threadName)
        {
            Thread thread = new Thread(new ThreadStart(action));
            if (threadName != null)
            {
                thread.Name = threadName;
            }
            thread.IsBackground = true;
            thread.Start();
        }

        public static void Fork(this Action action)
        {
            Fork(action, null);
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

        public static Rectangle ToRectangle(this RECT rect)
        {
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        public static RECT ToRect(this Rectangle rect)
        {
            return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
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