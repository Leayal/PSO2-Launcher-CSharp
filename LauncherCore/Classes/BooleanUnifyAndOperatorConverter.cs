﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class BooleanUnifyAndOperatorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = true;
            foreach (var item in values)
            {
                if (item is bool b)
                {
                    result = result && b;
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
