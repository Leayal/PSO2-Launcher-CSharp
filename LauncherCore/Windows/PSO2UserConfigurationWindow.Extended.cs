using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;
using System.Windows.Input;
using Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig;
using System.Windows;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.PSO2Launcher.Core.Classes;
using System.Globalization;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class PSO2UserConfigurationWindow
    {
        private static readonly ScreenResolution[] CommonResolutions = new ScreenResolution[]
        {
            new ScreenResolution(1280, 720, KnownRatio._16_9), new ScreenResolution(1280, 800, KnownRatio._16_10), new ScreenResolution(1366, 768, KnownRatio._16_9), new ScreenResolution(1440, 900, KnownRatio._16_10),
            new ScreenResolution(1600, 900, KnownRatio._16_9), new ScreenResolution(1680, 1050, KnownRatio._16_10), new ScreenResolution(1920, 1080, KnownRatio._16_9), new ScreenResolution(1920, 1200, KnownRatio._16_10),
            new ScreenResolution(1920, 1440, KnownRatio._4_3), new ScreenResolution(2560, 1440, KnownRatio._16_9), new ScreenResolution(2560, 1600, KnownRatio._16_10), new ScreenResolution(2560, 1920, KnownRatio._4_3),

            // This is madness
            new ScreenResolution(3840, 2160, KnownRatio._16_9)
        };

        private void HandleSpecialConfigurationDom(PropertyInfo item)
        {
            if (item.PropertyType == typeof(MonitorCountWrapper))
            {
                if (!CategoryAttribute.TryGetCategoryName(item, out var categoryName))
                {
                    categoryName = string.Empty;
                }
                if (!EnumDisplayNameAttribute.TryGetDisplayName(item, out var displayName))
                {
                    displayName = item.Name;
                }
                var option = new MonitorCountOptionDOM(item.Name, displayName);
                option.Reload(this._configR);
                option.Slider.ValueChanged += this.SliderMonitorNo_ValueChanged;
                if (!this.listOfOptions.TryGetValue(categoryName, out var opts))
                {
                    opts = new List<OptionDOM>();
                    this.listOfOptions.Add(categoryName, opts);
                }
                opts.Add(option);
            }
            else if (item.PropertyType == typeof(ScreenResolution))
            {
                if (!CategoryAttribute.TryGetCategoryName(item, out var categoryName))
                {
                    categoryName = string.Empty;
                }
                if (!EnumDisplayNameAttribute.TryGetDisplayName(item, out var displayName))
                {
                    displayName = item.Name;
                }
                var option = new ResolutionOptionDOM(item.Name, displayName);
                option.Reload(this._configR);
                option.ComboBox.SelectedValueChanged += this.OptionResolution_ValueChanged;
                if (!this.listOfOptions.TryGetValue(categoryName, out var opts))
                {
                    opts = new List<OptionDOM>();
                    this.listOfOptions.Add(categoryName, opts);
                }
                opts.Add(option);
            }

        }

        private void SliderMonitorNo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
        {
            if (sender is WeirdSlider slider && slider.Tag is MonitorCountOptionDOM dom)
            {
                var t = this._configR.GetType();
                var prop = t.GetProperty(dom.Name);
                if (prop != null)
                {
                    prop.SetValue(this._configR, new MonitorCountWrapper(e.NewValue));
                }
            }
        }

        private void OptionResolution_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (sender is ResolutionComboBox combobox && combobox.Tag is ResolutionOptionDOM dom)
            {
                var t = this._configR.GetType();
                var prop = t.GetProperty(dom.Name);
                if (prop != null)
                {
                    var res = combobox.SelectedResolution;
                    prop.SetValue(this._configR, res);
                }
            }
        }

#nullable disable
        class KeyCommandGoTo : ICommand
        {
            public readonly static KeyCommandGoTo Default = new KeyCommandGoTo();

            private readonly ConcurrentDictionary<PSO2UserConfigurationWindow, TextBoxGoToForm> _opended;
            
            private KeyCommandGoTo()
            {
                this._opended = new ConcurrentDictionary<PSO2UserConfigurationWindow, TextBoxGoToForm>();
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => (parameter is PSO2UserConfigurationWindow);

            public void Execute(object parameter)
            {
                if (parameter is PSO2UserConfigurationWindow window)
                {
                    var editor = window.Box_ManualConfig;

                    var dialog = this._opended.GetOrAdd(window, (key) =>
                    {
                        var duh = new TextBoxGoToForm();
                        duh.Owner = key;
                        duh.Closed += (sender, e) =>
                        {
                            this._opended.TryRemove(key, out _);
                        };
                        duh.LineJumpRequest += this.Duh_LineJumpRequest;
                        return duh;
                    });

                    if (dialog.Visibility == System.Windows.Visibility.Visible)
                    {
                        dialog.Activate();
                    }
                    else
                    {
                        dialog.Show();
                    }
                }
            }

            private void Duh_LineJumpRequest(object sender, System.Windows.RoutedPropertyChangedEventArgs<int> e)
            {
                if (sender is TextBoxGoToForm window && window.Owner is PSO2UserConfigurationWindow parent)
                {
                    var textbox = parent.Box_ManualConfig;
                    // textbox.ScrollToLine(Math.Clamp(e.NewValue, 0, textbox.LineCount));
                    textbox.TextArea.Caret.Line = Math.Clamp(e.NewValue, 0, textbox.LineCount);
                    textbox.TextArea.Caret.BringCaretToView();
                }
            }
        }
#nullable restore

        abstract class OptionDOM
        {
            public string Name { get; }

            public string DisplayName { get; }

            protected OptionDOM(string name, string displayname)
            {
                this.Name = name;
                this.DisplayName = displayname;
            }

            public abstract void Reload(PSO2FacadeUserConfig conf);

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

            public override void Reload(PSO2FacadeUserConfig conf)
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

            public override void Reload(PSO2FacadeUserConfig conf)
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

        class MonitorCountOptionDOM : OptionDOM
        {
            public readonly WeirdSlider Slider;

            public MonitorCountOptionDOM(string name, string displayName) : base(name, displayName)
            {
                // System.Windows.Forms.SystemInformation.MonitorCount
                var monitorCount = System.Windows.Forms.SystemInformation.MonitorCount; // ScreenInformation.GetMonitorCount();
                var dictionary = new Dictionary<int, string>(monitorCount);
                for (int i = 0; i < monitorCount; i++)
                {
                    dictionary.Add(i, $"Display No.{i + 1}");
                }
                this.Slider = new WeirdSlider() { Tag = this, Name = "PSO2GameOption_" + name, ItemsSource = (IReadOnlyDictionary<int, string>)dictionary };
            }

            public override FrameworkElement ValueController => this.Slider;

            public override void Reload(PSO2FacadeUserConfig conf)
            {
                var prop = conf.GetType().GetProperty(this.Name);
                if (prop != null && prop.GetValue(conf) is MonitorCountWrapper wrapper)
                {
                    var safe_val = Math.Clamp(wrapper.DisplayNo, Convert.ToInt32(this.Slider.Minimum), Convert.ToInt32(this.Slider.Maximum));
                    this.Slider.Value = safe_val;
                }
            }
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

            public override void Reload(PSO2FacadeUserConfig conf)
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

        class ResolutionOptionDOM : OptionDOM
        {
            public readonly ResolutionComboBox ComboBox;

            public ResolutionOptionDOM(string name, string displayName) : base(name, displayName)
            {
                this.ComboBox = new ResolutionComboBox() { Tag = this, Name = "PSO2GameOption_" + name };
                this.ComboBox.ItemsSource = CommonResolutions;
            }

            public override void Reload(PSO2FacadeUserConfig conf)
            {
                var t = conf.GetType();
                var prop = t.GetProperty(this.Name);
                if (prop != null)
                {
                    if (prop.GetValue(conf) is ScreenResolution res)
                    {
                        this.ComboBox.SelectedResolution = res;
                    }
                }
            }

            public override FrameworkElement ValueController => this.ComboBox;
        }
    }
}
