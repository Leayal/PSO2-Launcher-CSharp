﻿using Leayal.PSO2.UserConfig;
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
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.Highlighting;

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
        private readonly Dictionary<string, List<OptionDOM>> listOfOptions;

        private static readonly Lazy<IHighlightingDefinition> highlighter_dark = new Lazy<IHighlightingDefinition>(() =>
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Leayal.PSO2Launcher.Core.Resources.SyntaxHighlightRuleDarkTheme.xml"))
            {
                if (stream != null)
                {
                    using (var xmlr = System.Xml.XmlReader.Create(stream))
                    {
                        return ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(xmlr, HighlightingManager.Instance);
                    }
                }
                return null;
            }
        }),
            highlighter_light = new Lazy<IHighlightingDefinition>(() =>
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Leayal.PSO2Launcher.Core.Resources.SyntaxHighlightRuleLightTheme.xml"))
                {
                    if (stream != null)
                    {
                        using (var xmlr = System.Xml.XmlReader.Create(stream))
                        {
                            return ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(xmlr, HighlightingManager.Instance);
                        }
                    }
                }
                return null;
            });

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
            this.listOfOptions = new Dictionary<string, List<OptionDOM>>(6, StringComparer.OrdinalIgnoreCase);
            InitializeComponent();
            this.Box_ManualConfig.TextArea.Options.EnableHyperlinks = false;
            this.Box_ManualConfig.TextArea.Options.EnableEmailHyperlinks = false;
            SearchPanel.Install(this.Box_ManualConfig.TextArea);

            this.Box_ManualConfig.InputBindings.Add(new KeyBinding() { CommandTarget = this.Box_ManualConfig, Command = KeyCommandGoTo.Default, CommandParameter = this, Key = Key.G, Modifiers = ModifierKeys.Control });

            // this.Box_ManualConfig.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("JavaScript");
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            // var t = this._configR.GetType();
            this.OptionsItems.Children.Clear();
            this.OptionsItems.RowDefinitions.Clear();
            var props = this._configR.GetType().GetProperties();
            Type t_bool = typeof(bool),
                t_int = typeof(int);
            this.listOfOptions.Clear();
            int gridX = 0;

            // Hackish. Append the resolution selecting first.


            for (int i = 0; i < props.Length; i++)
            {
                var t = props[i];
                var propT = t.PropertyType;
                if (propT == t_bool || propT == t_int || propT.IsEnum)
                {
                    if (!CategoryAttribute.TryGetCategoryName(t, out var categoryName))
                    {
                        categoryName = string.Empty;
                    }
                    OptionDOM opt;
                    FrameworkElement slider;
                    if (!EnumDisplayNameAttribute.TryGetDisplayName(t, out var displayName))
                    {
                        displayName = t.Name;
                    }
                    if (propT == t_bool)
                    {
                        var _opt = new BooleanIntOptionDOM(t.Name, displayName);
                        _opt.CheckBox.ValueChanged += this.OptionSlider_ValueChanged;
                        slider = _opt.CheckBox;
                        opt = _opt;
                    }
                    else if (propT == t_int)
                    {
                        if (!ValueRangeAttribute.TryGetRange(t, out var min, out var max))
                        {
                            min = 0;
                            max = 100;
                        }
                        var _opt = new IntOptionDOM(t.Name, min, max, displayName);
                        _opt.Slider.ValueChanged += this.OptionSlider_ValueChanged;
                        slider = _opt.Slider;
                        opt = _opt;
                    }
                    else
                    {
                        var _opt = new EnumOptionDOM(t.Name, displayName, propT);
                        _opt.Slider.ValueChanged += this.OptionSlider_ValueChanged;
                        slider = _opt.Slider;
                        opt = _opt;
                    }

                    this.OptionsItems.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    opt.Reload(this._configR);
                    if (!this.listOfOptions.TryGetValue(categoryName, out var opts))
                    {
                        opts = new List<OptionDOM>();
                        this.listOfOptions.Add(categoryName, opts);
                    }
                    opts.Add(opt);
                    gridX++;
                }
            }

            this.OptionsTab.ItemsSource = this.listOfOptions.Keys;
            this.OptionsTab.SelectedIndex = 0;
        }

        private void OptionsTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0)
            {
                if (e.AddedItems[0] is string selected)
                {
                    this.OptionsItems.Children.Clear();
                    this.OptionsItems.RowDefinitions.Clear();
                    if (this.listOfOptions.TryGetValue(selected, out var list))
                    {
                        var gridX = 0;
                        for (int i = 0; i < list.Count; i++)
                        {
                            OptionDOM opt = list[i];

                            // list[i];
                            this.OptionsItems.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                            var text = new TextBlock() { Text = opt.DisplayName };
                            Grid.SetRow(text, gridX);
                            Grid.SetRow(opt.ValueController, gridX);
                            Grid.SetColumn(opt.ValueController, 1);

                            this.OptionsItems.Children.Add(text);
                            this.OptionsItems.Children.Add(opt.ValueController);

                            gridX++;
                        }
                    }
                }
            }
        }

        private void OptionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
        {
            if (sender is WeirdSlider slider)
            {
                var t = this._configR.GetType();
                if (slider.Tag is OptionDOM dom)
                {
                    var prop = t.GetProperty(dom.Name);
                    if (prop != null)
                    {
                        if (slider.Tag is BooleanIntOptionDOM)
                        {
                            bool b = e.NewValue != 0;
                            prop.SetValue(this._configR, b);
                        }
                        else if (slider.Tag is EnumOptionDOM)
                        {
                            prop.SetValue(this._configR, e.NewValue);
                        }
                    }
                }
            }
            else if (sender is WeirdValueSlider valueslider)
            {
                var t = this._configR.GetType();
                if (valueslider.Tag is IntOptionDOM dom)
                {
                    var prop = t.GetProperty(dom.Name);
                    if (prop != null)
                    {
                        prop.SetValue(this._configR, e.NewValue);
                    }
                }
            }
        }

        bool flag_shouldreload = true;
        private void MetroTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Contains(this.TabAdvanced))
            {
                // var d = this.Box_ManualConfig.Document;
                // var range = new TextRange(d.ContentStart, d.ContentEnd);
                try
                {
                    var conf = UserConfig.Parse(this.Box_ManualConfig.Text);
                    _ = conf.ToString();
                    this._conf = conf;
                    this._configR = new PSO2RebootUserConfig(conf);
                    foreach (var opts in this.listOfOptions)
                    {
                        foreach (var opt in opts.Value)
                        {
                            opt.Reload(this._configR);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.flag_shouldreload = false;
                    this.TabAdvanced.IsSelected = true;
                    this.flag_shouldreload = true;
                    if (Debugger.IsAttached)
                    {
                        throw;
                    }
                    else
                    {
                        MessageBox.Show(this, "Invalid configuration string. Message:\r\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.TabAdvanced.IsSelected)
                {
                    // var d = this.Box_ManualConfig.Document;
                    // var range = new TextRange(d.ContentStart, d.ContentEnd);
                    var conf = UserConfig.Parse(this.Box_ManualConfig.Text);
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
            this.Box_ManualConfig.Clear();
            this.Box_ManualConfig.Text = this._conf.ToString();
            /*
            var oldsrc = this.Box_ManualConfig.SourceTextBox;
            var newsrc = new FastColoredTextBoxNS.FastColoredTextBox();
            newsrc.Text = this._conf.ToString();
            newsrc.ClearUndo();
            this.Box_ManualConfig.SourceTextBox = newsrc;
            if (oldsrc != null)
            {
                if (oldsrc != this.Box_ManualConfig)
                {
                    oldsrc.Clear();
                    oldsrc.Dispose();
                }
            }
            */
        }

        protected override void OnThemeRefresh()
        {
            /*
            var brush_fore = this.Foreground.Clone();
            if (brush_fore.CanFreeze && !brush_fore.IsFrozen) brush_fore.Freeze();
            this.Box_ManualConfig.Foreground = brush_fore;
            var brush_back = this.Background.Clone();
            if (brush_back.CanFreeze && !brush_back.IsFrozen) brush_back.Freeze();
            this.Box_ManualConfig.Background = brush_back;
            */

            IHighlightingDefinition highlightingDefinition;
            if (App.Current.IsLightMode)
            {
                highlightingDefinition = highlighter_light.Value;
            }
            else
            {
                highlightingDefinition = highlighter_dark.Value;
            }

            this.Box_ManualConfig.SyntaxHighlighting = highlightingDefinition;
        }

        private static System.Drawing.Color WPFColorToWFColor(Color color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

        abstract class OptionDOM
        {
            public string Name { get; }

            public string DisplayName { get; }

            protected OptionDOM(string name, string displayname)
            {
                this.Name = name;
                this.DisplayName = displayname;
            }

            public abstract void Reload(PSO2RebootUserConfig conf);

            public abstract FrameworkElement ValueController { get; }
        }

        class BooleanIntOptionDOM : OptionDOM
        {
            public readonly WeirdSlider CheckBox;
            private static readonly IReadOnlyDictionary<int, string> lookupDictionary = new Dictionary<int, string>(2)
            {
                { 0, "Off" },
                { 1, "On" }
            };

            public BooleanIntOptionDOM(string name, string displayName) : base(name, displayName) 
            {
                this.CheckBox = new WeirdSlider() { Tag = this, Name = "PSO2GameOption_" + name, ItemsSource = lookupDictionary };
            }

            public override void Reload(PSO2RebootUserConfig conf)
            {
                var prop = conf.GetType().GetProperty(this.Name);
                if (prop != null)
                {
                    this.CheckBox.Value = Convert.ToInt32(prop.GetValue(conf), CultureInfo.InvariantCulture.NumberFormat);
                }
            }

            public override FrameworkElement ValueController => this.CheckBox;
        }

        class IntOptionDOM : OptionDOM
        {
            public readonly WeirdValueSlider Slider;

            public IntOptionDOM(string name, in int min, in int max, string displayName) : base(name, displayName)
            {
                this.Slider = new WeirdValueSlider() { Tag = this, Name = "PSO2GameOption_" + name };
                this.Slider.slider.Minimum = min;
                this.Slider.slider.Maximum = max;
            }

            public override void Reload(PSO2RebootUserConfig conf)
            {
                var prop = conf.GetType().GetProperty(this.Name);
                if (prop != null)
                {
                    var safe_val = Math.Clamp(Convert.ToInt32(prop.GetValue(conf), CultureInfo.InvariantCulture.NumberFormat), Convert.ToInt32(this.Slider.slider.Minimum), Convert.ToInt32(this.Slider.slider.Maximum));
                    this.Slider.Value = safe_val;
                }
            }

            public override FrameworkElement ValueController => this.Slider;
        }

        class EnumOptionDOM : OptionDOM
        {
            public readonly WeirdSlider Slider;

            public EnumOptionDOM(string name, string displayName, Type type) : base(name, displayName)
            {
                this.Slider = new WeirdSlider() { Tag = this, Name = "PSO2GameOption_" + name };
                var mems = Enum.GetNames(type);
                var d = new Dictionary<int, string>(mems.Length);
                for (int i = 0; i < mems.Length; i++)
                {
                    string memName = mems[i];
                    var mem = type.GetMember(memName)[0];
                    if (!EnumVisibleInOptionAttribute.TryGetIsVisible(mem, out var visible) || visible)
                    {
                        var val = Convert.ToInt32(Enum.Parse(type, memName));
                        if (!EnumDisplayNameAttribute.TryGetDisplayName(mem, out var displayname))
                        {
                            displayname = memName;
                        }
                        d.Add(val, displayname);
                    }
                }
                this.Slider.ItemsSource = d;
            }

            public override void Reload(PSO2RebootUserConfig conf)
            {
                var t = conf.GetType();
                var prop = t.GetProperty(this.Name);
                if (prop != null)
                {
                    var val = prop.GetValue(conf);
                    var converted = Convert.ToInt32(val);
                    this.Slider.Value = converted;
                }
            }

            public override FrameworkElement ValueController => this.Slider;
        }
    }
}
