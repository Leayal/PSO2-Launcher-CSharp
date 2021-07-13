using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    public static class EnumHelper
    {
        public static bool Clamp<T>(int value, int min, int max, out T @enum) where T : struct, Enum
        {
            var i = Math.Clamp(value, min, max);
            if (Enum.IsDefined(typeof(T), i))
            {
                @enum = Unsafe.As<int, T>(ref i);
                return true;
            }
            else
            {
                @enum = default;
                return false;
            }
        }

        public static bool Clamp<TEnum>(object value, out TEnum @enum) where TEnum : struct, Enum
        {
            // GetMinAndMax<TEnum, long>(out var min, out var max);
            // var i = Math.Clamp(value, min.HasValue ? min.Value : long.MinValue, max.HasValue ? max.Value : long.MaxValue);
            if (Enum.IsDefined(typeof(TEnum), value))
            {
                @enum = Unsafe.As<object, TEnum>(ref value);
                return true;
            }
            else
            {
                @enum = default;
                return false;
            }
        }

        public static void GetMinAndMax<TEnum, T>(out T? min, out T? max) where TEnum : struct, Enum where T : struct
        {
            T v_min = default, v_max = default;
            var comparer = Comparer<T>.Default;
            bool hasMin = false, hasMax = false;
            foreach (var x in Enum.GetValues<TEnum>().Cast<T>())
            {
                if (hasMin)
                {
                    if (comparer.Compare(x, v_min) < 0) v_min = x;
                }
                else
                {
                    v_min = x;
                    hasMin = true;
                }
                if (hasMax)
                {
                    if (comparer.Compare(x, v_max) > 0) v_max = x;
                }
                else
                {
                    v_max = x;
                    hasMax = true;
                }
            }

            if (hasMax)
            {
                max = v_max;
            }
            else
            {
                max = null;
            }
            if (hasMin)
            {
                min = v_min;
            }
            else
            {
                min = null;
            }
        }
    }
}
