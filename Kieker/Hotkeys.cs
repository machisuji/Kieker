using Gma.UserActivityMonitor;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;

namespace Kieker
{
    public class Hotkeys
    {
        private static Hotkeys instance = new Hotkeys();

        private ICollection<Keys> activeKeys = new List<Keys>();

        private IDictionary<PickyTuple<Keys>, Func<Keys[], bool>> actions = 
            new Dictionary<PickyTuple<Keys>, Func<Keys[], bool>>();
        private ICollection<Func<Keys[], bool>> unselectiveActions = 
            new List<Func<Keys[], bool>>();

        private Hotkeys()
        {
            HookManager.KeyDown += new System.Windows.Forms.KeyEventHandler(HookManager_KeyDown);
            HookManager.KeyUp += new System.Windows.Forms.KeyEventHandler(HookManager_KeyUp);
        }

        /// <summary>
        /// Adds the given action under the specified hotkey.
        /// The return value of the lambda determines whether or not the
        /// event is marked as handled or not, true meaning it has been handled
        /// and false meaning not.
        /// </summary>
        /// <param name="hotkey"></param>
        /// <param name="action"></param>
        public static void Put(Func<Keys[], bool> action, params Keys[] hotkey)
        {
            PickyTuple<Keys> key = new PickyTuple<Keys>(hotkey);
            if (instance.actions.ContainsKey(key))
            {
                instance.actions.Remove(key);
            }
            instance.actions.Add(key, action);
        }

        /// <summary>
        /// Adds an action that gets executed no matter what combination of keys are pressed.
        /// </summary>
        /// <param name="action"></param>
        public static void Add(Func<Keys[], bool> action)
        {
            instance.unselectiveActions.Add(action);
        }

        /// <summary>
        /// Removes actions added through #Add(Func)
        /// </summary>
        /// <param name="action"></param>
        public static void Remove(Func<Keys[], bool> action)
        {
            instance.unselectiveActions.Remove(action);
        }

        public static ICollection<Keys> GetActiveKeys()
        {
            return instance.activeKeys;
        }

        public static bool Equal(Keys[] a, Keys[] b)
        {
            return new PickyTuple<Keys>(a).Equals(new PickyTuple<Keys>(b));
        }

        public static String ToString(params Keys[] keys)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (Keys key in keys)
            {
                if (first) first = false;
                else sb.Append(" + ");
                sb.Append(key);
            }
            return sb.ToString();
        }

        public static Keys[] Parse(String name)
        {
            String[] names = name.Split('+');
            List<Keys> keys = new List<Keys>();
            foreach (String value in names)
            {
                Keys key = (Keys)Enum.Parse(typeof(Keys), value.Trim());
                keys.Add(key);
            }
            return new PickyTuple<Keys>(keys).Values;
        }

        void HookManager_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (!activeKeys.Contains(e.KeyCode))
            {
                activeKeys.Add(e.KeyCode);
            }
            PickyTuple<Keys> activeHotkey = new PickyTuple<Keys>(activeKeys);
            foreach (PickyTuple<Keys> hotkey in actions.Keys)
            {
                if (hotkey.Equals(activeHotkey))
                {
                    Func<Keys[], bool> action = actions[hotkey];
                    if (action != null)
                    {
                        e.Handled |= action.Invoke(activeHotkey.Values);
                    }
                }
            }
            foreach (Func<Keys[], bool> action in unselectiveActions)
            {
                e.Handled |= action.Invoke(activeHotkey.Values);
            }
        }

        void HookManager_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (activeKeys.Contains(e.KeyCode))
                activeKeys.Remove(e.KeyCode);
        }
    }

    struct PickyTuple<T> : IEquatable<PickyTuple<T>>
    {
        readonly T[] values;

        public PickyTuple(params T[] values)
        {
            this.values = values;
        }

        public PickyTuple(ICollection<T> values)
        {
            this.values = new T[values.Count];
            int index = 0;
            foreach (T value in values)
            {
                this.values[index++] = value;
            }
        }

        public T[] Values
        {
            get { return values; }
        }

        public override int GetHashCode()
        {
            int hash = values.Length;
            foreach (T value in values)
            {
                hash ^= value.GetHashCode();
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((PickyTuple<T>)obj);
        }

        /// <summary>
        /// Checks whether two PickyTuples contain the same Keys independently from their order.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(PickyTuple<T> other)
        {
            T[] these = this.values;
            T[] those = other.values;
            if (these.Length != those.Length)
            {
                return false;
            }
            else
            {
                foreach (T value in these)
                {
                    int matches = 0;
                    foreach (T otherValue in those)
                    {
                        if (value.Equals(otherValue))
                        {
                            ++matches;
                        }
                    }
                    if (matches != 1) return false;
                }
            }
            return true;
        }
    }
}