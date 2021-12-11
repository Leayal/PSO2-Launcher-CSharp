using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class IsNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return true;
            else if (value is string str && str.Length == 0) return true;
            else if (value is int i && i == 0) return true;
            else if (value is long l && l == 0) return true;
            else if (value is double d && d == 0) return true;
            else if (value is float f && f == 0) return true;
            else if (value is byte b && b == 0) return true;
            else if (value is decimal de && de == 0) return true;
            else if (value is short s && s == 0) return true;
            else if (value is sbyte sb && sb == 0) return true;
            else if (value is ulong ul && ul == 0) return true;
            else if (value is uint ui && ui == 0) return true;
            else if (value is ushort us && us == 0) return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                if (b)
                {
                    switch (Type.GetTypeCode(targetType))
                    {
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Decimal:
                        case TypeCode.Double:
                        case TypeCode.Single:
                            return 0;
                        case TypeCode.Char:
                            return char.MinValue;
                        default:
                            return null;
                    }
                }
            }
            return value;
        }
    }
}
