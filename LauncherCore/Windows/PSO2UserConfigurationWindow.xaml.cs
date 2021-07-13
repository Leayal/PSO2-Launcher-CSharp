using Leayal.PSO2.UserConfig;
using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig;
using Leayal.PSO2Launcher.Core.UIElements;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for DataManagerWindow.xaml
    /// </summary>
    public partial class PSO2UserConfigurationWindow : MetroWindowEx
    {
        private readonly string path_conf;
        private UserConfig _conf;
        private PSO2RebootUserConfig _configR;
        
        public PSO2UserConfigurationWindow()
        {
            this.path_conf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SEGA", "PHANTASYSTARONLINE2", "user.pso2");
            if (File.Exists(this.path_conf))
            {
                this._conf = UserConfig.FromFile(this.path_conf);
            }
            else
            {
                this._conf = new UserConfig("Ini");
            }

            this._configR = new PSO2RebootUserConfig(this._conf);
            InitializeComponent();
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            var t = this._configR.GetType();
            var props = t.GetProperties();
            var t_bool = typeof(bool);
            for (int i = 0; i < props.Length; i++)
            {
                var propT = props[i].PropertyType;
                if (propT == t_bool)
                {

                }
                else if (propT.IsEnum)
                {
                    var vals = propT.GetEnumValues();
                }
            }
        }

        bool flag_shouldreload = true;
        private void MetroTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Contains(this.TabAdvanced))
            {
                var d = this.Box_ManualConfig.Document;
                var range = new TextRange(d.ContentStart, d.ContentEnd);
                try
                {
                    var conf = UserConfig.Parse(range.Text);
                    _ = conf.ToString();
                    this._conf = conf;
                }
                catch
                {
                    this.flag_shouldreload = false;
                    this.TabAdvanced.IsSelected = true;
                    this.flag_shouldreload = true;
                    MessageBox.Show(this, "Invalid configuration string", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (e.AddedItems.Contains(this.TabAdvanced))
            {
                if (this.flag_shouldreload)
                {
                    this.ReloadConfigFromLoadedConfig();
                }
            }
        }

        private void TabSimple_Selected(object sender, RoutedEventArgs e)
        {
        }

        private void TabAdvanced_Selected(object sender, RoutedEventArgs e)
        {
            
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.TabAdvanced.IsSelected)
                {
                    var d = this.Box_ManualConfig.Document;
                    var range = new TextRange(d.ContentStart, d.ContentEnd);
                    var conf = UserConfig.Parse(range.Text);
                    conf.SaveAs(this.path_conf);
                }
                else
                {
                    this._conf.SaveAs(this.path_conf);
                }

                this.DialogResult = true;
                SystemCommands.CloseWindow(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            SystemCommands.CloseWindow(this);
        }

        private static Dictionary<T, EnumComboBox.ValueDOM<T>> EnumToDictionary<T>() where T : struct, Enum
        {
            var _enum = Enum.GetNames<T>();
            return EnumToDictionary<T>(_enum);
        }

        private static Dictionary<T, EnumComboBox.ValueDOM<T>> EnumToDictionary<T>(T[] values) where T : struct, Enum
        {
            var strs = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                strs[i] = values[i].ToString();
            }
            return EnumToDictionary<T>(strs);
        }

        private static Dictionary<T, EnumComboBox.ValueDOM<T>> EnumToDictionary<T>(params string[] names) where T : struct, Enum
        {
            var _list = new Dictionary<T, EnumComboBox.ValueDOM<T>>(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                var member = Enum.Parse<T>(names[i]);
                if (!EnumVisibleInOptionAttribute.TryGetIsVisible(member, out var isVisible) || isVisible)
                {
                    _list.Add(member, new EnumComboBox.ValueDOM<T>(member));
                }
            }
            return _list;
        }

        private void ButtonUndoAllChanges_Click(object sender, RoutedEventArgs e) => this.ReloadConfigFromLoadedConfig();

        private void ReloadConfigFromLoadedConfig()
        {
            var b = this.Box_ManualConfig.Document.Blocks;
            b.Clear();
            b.Add(new Paragraph(new Run(this._conf.ToString())));
        }

        abstract class OptionDOM
        {
            public string Name { get; }

            protected OptionDOM(string name)
            {
                this.Name = name;
            }

            public abstract FrameworkElement ValueController { get; }
        }


    }
}
