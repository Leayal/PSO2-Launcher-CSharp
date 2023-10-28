using Leayal.PSO2Launcher.Core.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for EnumComboBox.xaml
    /// </summary>
    public partial class EnumComboBox : ComboBox
    {
        public EnumComboBox()
        {
            InitializeComponent();
        }

        public class ValueDOM<T> where T : Enum
        {
            public string Name { get; }

            public T Value { get; }

            public ValueDOM(T value) : this(value, EnumDisplayNameAttribute.TryGetDisplayName(value, out var name) ? name : value.ToString()) { }

            public ValueDOM(T value, string displayName)
            {
                this.Name = displayName;
                this.Value = value;
            }
        }

        public readonly struct ValueDOMNumber
        {
            public string Name { get; }

            public int Value { get; }

            public ValueDOMNumber(string displayName, int value)
            {
                this.Name = displayName;
                this.Value = value;
            }
        }

        public static Dictionary<T, ValueDOM<T>> EnumToDictionary<T>(bool includeInvisible) where T : struct, Enum
        {
            var _enum = Enum.GetNames<T>();
            return EnumToDictionary<T>(includeInvisible, _enum);
        }

        public static Dictionary<T, ValueDOM<T>> EnumToDictionary<T>(bool includeInvisible, T[] values) where T : struct, Enum
        {
            var strs = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                strs[i] = values[i].ToString();
            }
            return EnumToDictionary<T>(includeInvisible, strs);
        }

        public static Dictionary<T, ValueDOM<T>> EnumToDictionary<T>() where T : struct, Enum => EnumToDictionary<T>(false);

        public static Dictionary<T, ValueDOM<T>> EnumToDictionary<T>(T[] values) where T : struct, Enum => EnumToDictionary<T>(false, values);

        public static Dictionary<T, ValueDOM<T>> EnumToDictionary<T>(bool includeInvisible, params string[] names) where T : struct, Enum
        {
            var _list = new Dictionary<T, ValueDOM<T>>(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                var member = Enum.Parse<T>(names[i]);
                if (includeInvisible || !EnumVisibleInOptionAttribute.TryGetIsVisible(member, out var isVisible) || isVisible)
                {
                    _list.Add(member, new ValueDOM<T>(member));
                }
            }
            return _list;
        }
    }
}
