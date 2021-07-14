using System;
using System.Collections.Generic;
using System.Globalization;
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
            var t = typeof(T);
            if (Enum.IsDefined(t, i))
            {
                @enum = (T)Enum.ToObject(t, value);
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
            var t = typeof(TEnum);
            var primitiveT = Enum.GetUnderlyingType(t);
            if (primitiveT == typeof(int))
            {
                GetMinAndMax<TEnum, int>(out var min, out var max);
                var i = Math.Clamp(Convert.ToInt32(value, CultureInfo.InvariantCulture.NumberFormat), min.HasValue ? min.Value : int.MinValue, max.HasValue ? max.Value : int.MaxValue);
                @enum = (TEnum)Enum.ToObject(t, i);
                return true;
            }
            else if (primitiveT == typeof(long))
            {
                GetMinAndMax<TEnum, long>(out var min, out var max);
                var i = Math.Clamp(Convert.ToInt64(value, CultureInfo.InvariantCulture.NumberFormat), min.HasValue ? min.Value : long.MinValue, max.HasValue ? max.Value : long.MaxValue);
                @enum = (TEnum)Enum.ToObject(t, i);
                return true;
            }
            else if (primitiveT == typeof(short))
            {
                GetMinAndMax<TEnum, short>(out var min, out var max);
                var i = Math.Clamp(Convert.ToInt16(value, CultureInfo.InvariantCulture.NumberFormat), min.HasValue ? min.Value : short.MinValue, max.HasValue ? max.Value : short.MaxValue);
                @enum = (TEnum)Enum.ToObject(t, i);
                return true;
            }
            else if (primitiveT == typeof(ushort))
            {
                GetMinAndMax<TEnum, ushort>(out var min, out var max);
                var i = Math.Clamp(Convert.ToUInt16(value, CultureInfo.InvariantCulture.NumberFormat), min.HasValue ? min.Value : ushort.MinValue, max.HasValue ? max.Value : ushort.MaxValue);
                @enum = (TEnum)Enum.ToObject(t, i);
                return true;
            }
            else if (primitiveT == typeof(ulong))
            {
                GetMinAndMax<TEnum, ulong>(out var min, out var max);
                var i = Math.Clamp(Convert.ToUInt64(value, CultureInfo.InvariantCulture.NumberFormat), min.HasValue ? min.Value : ulong.MinValue, max.HasValue ? max.Value : ulong.MaxValue);
                @enum = (TEnum)Enum.ToObject(t, i);
                return true;
            }
            else if (primitiveT == typeof(uint))
            {
                GetMinAndMax<TEnum, uint>(out var min, out var max);
                var i = Math.Clamp(Convert.ToUInt32(value, CultureInfo.InvariantCulture.NumberFormat), min.HasValue ? min.Value : uint.MinValue, max.HasValue ? max.Value : uint.MaxValue);
                @enum = (TEnum)Enum.ToObject(t, i);
                return true;
            }
            else if (primitiveT == typeof(byte))
            {
                GetMinAndMax<TEnum, byte>(out var min, out var max);
                var i = Math.Clamp(Convert.ToByte(value, CultureInfo.InvariantCulture.NumberFormat), min.HasValue ? min.Value : byte.MinValue, max.HasValue ? max.Value : byte.MaxValue);
                @enum = (TEnum)Enum.ToObject(t, i);
                return true;
            }
            @enum = default;
            return false;
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
