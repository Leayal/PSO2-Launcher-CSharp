using System;
using System.Globalization;
using System.Windows.Data;

namespace Leayal.PSO2Launcher.Core.Classes
{
    sealed class BooleanUnifyOrOperatorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;
            foreach (var item in values)
            {
                if (item is bool b)
                {
                    if (b)
                    {
                        return true;
                    }
                }
            }
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                if (targetTypes != null && targetTypes.Length != 0)
                {
                    var result = new object[targetTypes.Length];
                    Array.Fill(result, b);
                    return result;
                }
            }
            return null;
        }
    }
}
