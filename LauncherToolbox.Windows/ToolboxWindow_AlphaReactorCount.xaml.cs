﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Leayal.Shared;
using Leayal.Shared.Windows;

namespace Leayal.PSO2Launcher.Toolbox.Windows
{
    /// <summary>Toolbox window: Alpha Reactor Counter</summary>
    /// <remarks>
    /// <para>This class is standalone and does not require the Launcher's Core. If you want to use this class as standalone tool, feel free to do so.</para>
    /// <para>Please note that you still need to properly setup an <seealso cref="Application"/> for <seealso cref="MetroWindowEx"/> in case of standalone.</para>
    /// </remarks>
    public partial class ToolboxWindow_AlphaReactorCount : MetroWindowEx
    {
        private static Lazy<LogCategories> PSO2LogWatcher = new Lazy<LogCategories>(() => new LogCategories(Application.Current.Dispatcher));

        /// <summary>Dispose the log watcher if it has been created. Otherwise does nothing</summary>
        public static void DisposeLogWatcherIfCreated()
        {
            try
            {
                if (PSO2LogWatcher.IsValueCreated)
                {
                    PSO2LogWatcher.Value.Dispose();
                }
            }
            catch { } // Silent everything as we will terminate the process anyway.
        }
        
        private static readonly DependencyPropertyKey CurrentTimePropertyKey = DependencyProperty.RegisterReadOnly("CurrentTime", typeof(DateTime), typeof(ToolboxWindow_AlphaReactorCount), new PropertyMetadata(DateTime.MinValue));
        private static readonly DependencyPropertyKey IsBeforeResetPropertyKey = DependencyProperty.RegisterReadOnly("IsBeforeReset", typeof(bool?), typeof(ToolboxWindow_AlphaReactorCount), new PropertyMetadata(null));

        /// <summary><seealso cref="DependencyProperty"/> for <seealso cref="CurrentTime"/> property.</summary>
        public static readonly DependencyProperty CurrentTimeProperty = CurrentTimePropertyKey.DependencyProperty;

        /// <summary><seealso cref="DependencyProperty"/> for <seealso cref="IsBeforeReset"/> property.</summary>
        public static readonly DependencyProperty IsBeforeResetProperty = IsBeforeResetPropertyKey.DependencyProperty;

        /// <summary><seealso cref="DependencyProperty"/> for <seealso cref="IsClockVisible"/> property.</summary>
        private static readonly DependencyProperty IsClockVisibleProperty = DependencyProperty.Register("IsClockVisible", typeof(bool), typeof(ToolboxWindow_AlphaReactorCount), new PropertyMetadata(false, (obj, e) =>
        {
            if (obj is ToolboxWindow_AlphaReactorCount window && e.NewValue is bool b)
            {
                if (b)
                {
                    window.timerclock.Register(window.timerCallback);
                }
                else
                {
                    window.timerclock.Unregister(window.timerCallback);
                }
            }
        }));

        /// <summary>Gets a <seealso cref="DateTime"/> of the current time.</summary>
        /// <remarks>This is not high-resolution and therefore may not be accurate.</remarks>
        public DateTime CurrentTime => (DateTime)this.GetValue(CurrentTimeProperty);

        /// <summary>Gets or sets a boolean determines whether the clock will be displayed.</summary>
        /// <remarks>If the clock is hidden, the clock's instance will be deactivated. Thus, saving CPU. Though it's not significant anyway.</remarks>
        public bool IsClockVisible
        {
            get => (bool)this.GetValue(IsClockVisibleProperty);
            set => this.SetValue(IsClockVisibleProperty, value);
        }

        /// <summary>Determines whether the current time is before or after the daily reset.</summary>
        /// <remarks>
        /// <para><c>null</c> - Right at daily reset.</para>
        /// <para><c>true</c> - Before daily reset.</para>
        /// <para><c>false</c> - After daily reset.</para>
        /// </remarks>
        public bool? IsBeforeReset => (bool?)this.GetValue(IsBeforeResetProperty);

        private PSO2LogAsyncReader? logreader;
        private readonly bool shouldDisposeClock, clockInitiallyVisible;
        private readonly ObservableCollection<AccountData> characters;
        private readonly Dictionary<long, AccountData> mapping;
        private readonly Action<LogCategories, List<string>> Logfiles_NewFileFound;
        private readonly JSTClockTimer timerclock;
        private readonly ClockTickerCallback timerCallback;
        private readonly DelegateSetTime_params @delegateSetTime;

        /// <summary>Creates a new window.</summary>
        public ToolboxWindow_AlphaReactorCount() : this(null) { }

        /// <summary>Creates a new window with the given icon and a new clock instance.</summary>
        /// <param name="icon">The bitmap contains the window's icon you want to set.</param>
        public ToolboxWindow_AlphaReactorCount(BitmapSource? icon) : this(icon, null, true, true) { }

        /// <summary>Creates a new window with the given icon and a new clock instance.</summary>
        /// <param name="icon">The bitmap contains the window's icon you want to set.</param>
        /// <param name="clockInitiallyVisible">A boolean determines whether the timer will be in active and displayed in the UI when the window is shown.</param>
        public ToolboxWindow_AlphaReactorCount(BitmapSource? icon, bool clockInitiallyVisible) : this(icon, null, true, clockInitiallyVisible) { }

        /// <summary>Creates a new window with the given icon</summary>
        /// <param name="icon">The bitmap contains the window's icon you want to set.</param>
        /// <param name="clock">The timer clock which will be used to display the JST time on the UI.</param>
        /// <param name="disposeTimer">A boolean determines whether the timer should be disposed when the window is closed.</param>
        /// <param name="clockInitiallyVisible">A boolean determines whether the timer will be in active and displayed in the UI when the window is shown.</param>
        public ToolboxWindow_AlphaReactorCount(BitmapSource? icon, JSTClockTimer? clock, bool disposeTimer, bool clockInitiallyVisible) : base()
        {
            this.clockInitiallyVisible = clockInitiallyVisible;
            this.mapping = new Dictionary<long, AccountData>();
            this.characters = new ObservableCollection<AccountData>();
            this.SetTime(TimeZoneHelper.ConvertTimeToLocalJST(DateTime.UtcNow));
            this.Logfiles_NewFileFound = new Action<LogCategories, List<string>>(this.Logfiles_OnNewFileFound);
            this.timerCallback = new ClockTickerCallback(this.OnClockTicked);
            this.@delegateSetTime = new DelegateSetTime_params(this);
            if (clock == null)
            {
                clock = new JSTClockTimer();
                this.shouldDisposeClock = true;
            }
            else
            {
                this.shouldDisposeClock = disposeTimer;
            }
            this.timerclock = clock;
            InitializeComponent();
            if (icon is not null)
            {
                this.Icon = icon;
            }
        }

        /// <inheritdoc/>
        protected override Task OnCleanupBeforeClosed()
        {
            this.IsClockVisible = false;
            if (this.shouldDisposeClock)
            {
                this.timerclock.Dispose();
            }
            PSO2LogWatcher.Value.StopWatching(this.Logfiles_NewFileFound);
            try
            {
                this.CloseCurrentLog();
            }
            catch { }
            return Task.CompletedTask;
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            var logfiles = PSO2LogWatcher.Value;
            logfiles.StartWatching(this.Logfiles_NewFileFound);
            this.IsClockVisible = this.clockInitiallyVisible;
        }

        private void Logfiles_OnNewFileFound(object sender, List<string> ev)
        {
            if (ev == PSO2LogWatcher.Value.ActionLog)
            {
                this.SelectNewestLog(ev);
            }
        }

        private void OnClockTicked(in DateTime oldTime, in DateTime newTime)
        {
            if (TimeZoneHelper.IsBeforePSO2GameResetTime(in oldTime) && !TimeZoneHelper.IsBeforePSO2GameResetTime(in newTime))
            {
                this.Dispatcher.InvokeAsync(new Action(this.ForceSelectNewestActionLog));
            }
            else
            {
                this.@delegateSetTime.arg = newTime;
                this.Dispatcher.InvokeAsync(this.@delegateSetTime.Invoke);
            }
        }

        private void ForceSelectNewestActionLog() => this.SelectNewestLog(PSO2LogWatcher.Value.ActionLog, true);

        /// <summary>Select the newest log file from the given category.</summary>
        /// <param name="logcategory">The log category to get the log file.</param>
        /// <param name="force">Determines whether a force reload the log file is required.</param>
        public void SelectNewestLog(List<string> logcategory, bool force = false) => this.SelectLog(logcategory, -1, force);

        /// <summary>Select a certain log file from the given category.</summary>
        /// <param name="logcategory">The log category to get the log file.</param>
        /// <param name="index">The index of the log file from the category.</param>
        /// <param name="force">Determines whether a force reload the log file is required.</param>
        public void SelectLog(List<string> logcategory, int index, bool force = false)
        {
            string filepath;
            int count;
            lock (logcategory)
            {
                count = logcategory.Count;
                if (count != 0)
                {
                    if (index == -1)
                    {
                        filepath = logcategory[count - 1];
                    }
                    else if (index < 0)
                    {
                        count = 0;
                        filepath = string.Empty;
                    }
                    else
                    {
                        filepath = logcategory[index];
                    }
                }
                else
                {
                    filepath = string.Empty;
                }
            }
            if (count == 0)
            {
                this.CloseCurrentLog();
            }
            else if (!string.IsNullOrEmpty(filepath))
            {
                if (!force && this.logreader is not null && string.Equals(Path.IsPathFullyQualified(filepath) ? filepath : Path.GetFullPath(filepath), this.logreader.Fullpath, StringComparison.OrdinalIgnoreCase)) return;
                if (File.Exists(filepath))
                {
                    this.CloseCurrentLog();
                    this.RefreshDate();
                    this.logreader = new PSO2LogAsyncReader(filepath);
                    this.logreader.DataReceived += this.Logreader_DataReceived;
                    this.logreader.StartReceiving();
                }
            }
        }

        private void SetTime(DateTime datetime)
        {
            this.SetValue(CurrentTimePropertyKey, datetime);
            this.SetValue(IsBeforeResetPropertyKey, TimeZoneHelper.IsBeforePSO2GameResetTime(in datetime));
        }

        /// <summary>Force refresh the displayed <seealso cref="DateTime"/> on the UI.</summary>
        /// <remarks>This is totally not required and not useful in most cases.</remarks>
        public void RefreshDate()
        {
            var today = TimeZoneHelper.ConvertTimeToLocalJST(DateTime.Now);
            if (this.Dispatcher.CheckAccess())
            {
                this.SetTime(today);
            }
            else
            {
                this.@delegateSetTime.arg = today;
                this.Dispatcher.Invoke(this.@delegateSetTime.Invoke);
            }
        }

        private void Logreader_DataReceived(PSO2LogAsyncReader arg1, LogReaderDataReceivedEventArgs arg2)
        {
            var datas = arg2.GetDatas();
            if (datas[2].Span.Equals("[Pickup]", StringComparison.OrdinalIgnoreCase))
            {
                bool isAlphaReactor = (datas[5].Span.Equals("Alpha Reactor", StringComparison.OrdinalIgnoreCase) || datas[5].Span.Equals("アルファリアクター", StringComparison.OrdinalIgnoreCase));
                bool isStellaSeed = (datas[5].Span.Equals("Stellar Shard", StringComparison.OrdinalIgnoreCase) || datas[5].Span.Equals("Stellar Seed", StringComparison.OrdinalIgnoreCase) || datas[5].Span.Equals("ステラーシード", StringComparison.OrdinalIgnoreCase));
                if (isAlphaReactor || isStellaSeed)
                {
                    var dateonly = DateOnly.ParseExact(datas[0].Span.Slice(0, 10), "yyyy-MM-dd");
                    var timeonly = TimeOnly.Parse(datas[0].Span.Slice(11));
                    var logtime = new DateTime(dateonly.Year, dateonly.Month, dateonly.Day, timeonly.Hour, timeonly.Minute, timeonly.Second, DateTimeKind.Local);

                    // Consider push the datetime before reset to become previous day.
                    logtime = TimeZoneHelper.ConvertTimeToLocalJST(logtime);
                    var logtime_jst = TimeZoneHelper.AdjustToPSO2GameResetTime(in logtime);
                    logtime = TimeZoneHelper.ConvertTimeToLocalJST(DateTime.Now);
                    var currenttime_jst = TimeZoneHelper.AdjustToPSO2GameResetTime(in logtime);

                    // Consider push the datetime before reset to become previous day.
                    var dateonly_logtime_jst = DateOnly.FromDateTime(logtime_jst);
                    var dateonly_currenttime_jst = DateOnly.FromDateTime(currenttime_jst);

                    if (dateonly_logtime_jst != dateonly_currenttime_jst)
                    {
                        this.RefreshDate();
                    }
                    if (dateonly_logtime_jst == dateonly_currenttime_jst)
                    {
                        this.AddOrModifyCharacterData(long.Parse(datas[3].Span), new string(datas[4].Span), isAlphaReactor ? 1 : 0, isStellaSeed ? 1 : 0);
                    }
                    else
                    {
                        this.AddOrModifyCharacterData(long.Parse(datas[3].Span), new string(datas[4].Span));
                    }
                }
                else
                {
                    this.AddOrModifyCharacterData(long.Parse(datas[3].Span), new string(datas[4].Span));
                }
            }
            else
            {
                this.AddOrModifyCharacterData(long.Parse(datas[3].Span), new string(datas[4].Span));
            }
        }

        private void AddOrModifyCharacterData(long charId, string charName, int alphaReactorIncrement = 0, int stellarSeedIncrement = 0)
        {
            if (this.Dispatcher.CheckAccess())
            {
                if (!this.mapping.TryGetValue(charId, out var data))
                {
                    data = new AccountData(charId);
                    this.mapping.Add(charId, data);
                    this.characters.Add(data);
                    if (this.characters.Count == 1)
                    {
                        this.AccountSelector.SelectedIndex = 0;
                    }
                }
                data.AddName(charName);
                if (alphaReactorIncrement != 0)
                {
                    data.AlphaReactorCount += alphaReactorIncrement;
                }
                if (stellarSeedIncrement != 0)
                {
                    data.StellarSeedCount += stellarSeedIncrement;
                }
            }
            else
            {
                this.Dispatcher.Invoke((Action<long, string, int, int>)this.AddOrModifyCharacterData, new object[] { charId, charName, alphaReactorIncrement, stellarSeedIncrement });
            }
        }

        private void AccountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems is not null && e.AddedItems.Count != 0)
            {
                this.AlphaCounter.DataContext = e.AddedItems[0];
            }
            else
            {
                this.AlphaCounter.DataContext = null;
            }
        }

        private void AccountSelector_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox box)
            {
                box.ItemsSource = this.characters;
            }
        }

        class DelegateSetTime_params
        {
            public DateTime arg;
            public readonly ToolboxWindow_AlphaReactorCount window;

            public DelegateSetTime_params(ToolboxWindow_AlphaReactorCount w)
            {
                this.window = w;
            }

            public void Invoke()
            {
                this.window.SetTime(arg);
            }
        }

        /// <summary>Close the current opening log file if there is one. Otherwise does nothing.</summary>
        public void CloseCurrentLog()
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.characters.Clear();
                this.mapping.Clear();
                if (this.logreader is not null)
                {
                    this.logreader.DataReceived -= this.Logreader_DataReceived;
                    this.logreader.Dispose();
                }
            }
            else
            {
                this.Dispatcher.Invoke(this.CloseCurrentLog);
            }
        }
    }
}
