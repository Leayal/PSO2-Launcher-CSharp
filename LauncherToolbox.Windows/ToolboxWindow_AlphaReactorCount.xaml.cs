﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Leayal.Shared;
using Leayal.Shared.Windows;

namespace Leayal.PSO2Launcher.Toolbox.Windows
{
    /// <summary>Toolbox window: Alpha Reactor Counter</summary>
    /// <remarks>
    /// <para>This class is standalone and does not require the Launcher's Core. If you want to use this class as standalone tool, feel free to do so.</para>
    /// <para>Please note that you still need to properly setup an <seealso cref="Application"/> for <seealso cref="MetroWindowEx"/> in case of standalone.</para>
    /// <para>If you have an instance of <seealso cref="ToolboxWindow_AlphaReactorCount"/>, please call <seealso cref="DisposeLogWatcherIfCreated"/> when you exit the application.</para>
    /// </remarks>
    public partial class ToolboxWindow_AlphaReactorCount : MetroWindowEx
    {
        private static int Count_LogCategories;
        private static object lockobj_LogCategories;
        private static LogCategories? instance_LogCategories;
        private static bool initialized_LogCategories;

        static ToolboxWindow_AlphaReactorCount()
        {
            Count_LogCategories = 0;
            lockobj_LogCategories = new object();
            instance_LogCategories = null;
            initialized_LogCategories = false;
            Leayal.SharedInterfaces.Compatibility.CompatStockFunc.ProcessShutdown += CompatStockFunc_ProcessShutdown;
        }

        [return: NotNull]
        private static LogCategories InitializeLogWatcher()
        {
            Interlocked.Increment(ref Count_LogCategories);
#nullable disable
            return LazyInitializer.EnsureInitialized(ref instance_LogCategories, ref initialized_LogCategories, ref lockobj_LogCategories, CreateInstance_LogCategories);
#nullable restore
        }

        private static void DeInitializeLogWatcher()
        {
            lock (lockobj_LogCategories)
            {
                var count = Interlocked.Decrement(ref Count_LogCategories);
                if (count == 0)
                {
                    if (instance_LogCategories != null)
                    {
                        instance_LogCategories.Dispose();
                        instance_LogCategories = null;
                    }
                    initialized_LogCategories = false;
                }
                else if (count < 0)
                {
                    Interlocked.Exchange(ref Count_LogCategories, 0);
                }
            }
        }

        private static void CompatStockFunc_ProcessShutdown(object? sender, EventArgs e)
        {
            instance_LogCategories?.Dispose();
        }

        private static LogCategories CreateInstance_LogCategories() => new LogCategories(LogCategories.DefaultLogDirectoryPath);

        // Init the reusable objects (or strings, in this case) to avoid allocations.
        // But allow these objects to be collected by GC when they're no longer in use, by using WeakLazy class.
        // See the window's constructor to see how to use safely in threads.
        private static readonly WeakLazy<string> lazy_AlphaReactor_en = new WeakLazy<string>(() => "Alpha Reactor"),
                                                                    lazy_AlphaReactor_jp = new WeakLazy<string>(() => "アルファリアクター"),
                                                                    lazy_StellarSeed_en1 = new WeakLazy<string>(() => "Stellar Shard"),
                                                                    lazy_StellarSeed_en2 = new WeakLazy<string>(() => "Stellar Seed"),
                                                                    lazy_StellarSeed_jp = new WeakLazy<string>(() => "ステラーシード"),
                                                                    lazy_DateOnlyFormat = new WeakLazy<string>(() => "yyyy-MM-dd"),
                                                                    lazy_ActionPickup = new WeakLazy<string>(() => "[Pickup]"),
                                                                    lazy_ShopAction_SetPrice = new WeakLazy<string>(() => "[DisplayToShop-SetValue]");

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

        private readonly bool shouldDisposeClock, clockInitiallyVisible;
        private readonly ObservableCollection<AccountData> characters;
        private readonly Dictionary<long, AccountData> mapping;
        private readonly NewFileFoundEventHandler Logfiles_NewFileFound;
        private readonly JSTClockTimer timerclock;
        private readonly ClockTickerCallback timerCallback;
        private readonly DelegateSetTime_params @delegateSetTime;
        private readonly DispatcherTimer logcategoryThrottle;
        private readonly LogCategories logCategoriesImpl;

        private readonly string str_AlphaReactor_en, str_AlphaReactor_jp, str_StellarSeed_en1, str_StellarSeed_en2, str_StellarSeed_jp, str_DateOnlyFormat, str_ActionPickup, str_ShopAction_SetPrice;

        private PSO2LogAsyncListener? logreader;
        private CancellationTokenSource? cancelSrcLoad;
        private int logloadingstate;

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
            this.logloadingstate = 0;

            // Init and fork the reference locally to keep the object alive.
            // Since WeakReference isn't thread-safe. Fork on UI thread and use it until we don't need it anymore should make it safe regardless of thread.
            this.str_AlphaReactor_en = lazy_AlphaReactor_en.Value;
            this.str_AlphaReactor_jp = lazy_AlphaReactor_jp.Value;
            this.str_StellarSeed_en1 = lazy_StellarSeed_en1.Value;
            this.str_StellarSeed_en2 = lazy_StellarSeed_en2.Value;
            this.str_StellarSeed_jp = lazy_StellarSeed_jp.Value;
            this.str_DateOnlyFormat = lazy_DateOnlyFormat.Value;
            this.str_ActionPickup = lazy_ActionPickup.Value;
            this.str_ShopAction_SetPrice = lazy_ShopAction_SetPrice.Value;

            this.logCategoriesImpl = InitializeLogWatcher();
            this.SetTime(TimeZoneHelper.ConvertTimeToLocalJST(DateTime.UtcNow));
            this.Logfiles_NewFileFound = new NewFileFoundEventHandler(this.Logfiles_OnNewFileFound);
            this.timerCallback = new ClockTickerCallback(this.OnClockTicked);
            this.@delegateSetTime = new DelegateSetTime_params(this);
            this.logcategoryThrottle = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, OnThrottleEventInvocation, this.Dispatcher) { Tag = this, IsEnabled = false };

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
            this.logCategoriesImpl.StopWatching(this.Logfiles_NewFileFound);
            DeInitializeLogWatcher();
            try
            {
                this.CloseLog();
            }
            catch { }
            try
            {
                this.cancelSrcLoad?.Cancel();
            }
            catch { }
            return Task.CompletedTask;
        }

        private async void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            this.IsClockVisible = this.clockInitiallyVisible;
            await this.logCategoriesImpl.StartWatching(this.Logfiles_NewFileFound);
        }

        private void Logfiles_OnNewFileFound(LogCategories sender, NewFileFoundEventArgs e)
        {
            if (e.CategoryName == null || e.CategoryName == LogCategories.ActionLog)
            {
                lock (this.logcategoryThrottle)
                {
                    this.logcategoryThrottle.Stop();
                    this.logcategoryThrottle.Start();
                }
            }
        }

        private void OnClockTicked(in DateTime oldTime, in DateTime newTime)
        {
            if (TimeZoneHelper.IsBeforePSO2GameResetTime(in oldTime) && !TimeZoneHelper.IsBeforePSO2GameResetTime(in newTime))
            {
                this.Dispatcher.InvokeAsync(this.ForceSelectNewestActionLog);
            }
            else
            {
                this.@delegateSetTime.arg = newTime;
                this.Dispatcher.InvokeAsync(this.@delegateSetTime.Invoke);
            }
        }

        private async void ForceSelectNewestActionLog() => await this.SelectNewestLog(LogCategories.ActionLog, true);

        /// <summary>Select the newest log file from the given category.</summary>
        /// <param name="categoryName">The name of log category to get the log file.</param>
        /// <param name="force">Determines whether a force reload the log file is required.</param>
        public async Task SelectNewestLog(string categoryName, bool force = false)
        {
            if (Interlocked.CompareExchange(ref this.logloadingstate, 1, 0) == 0)
            {
                try
                {
                    string? filepath = this.logCategoriesImpl.SelectLog(categoryName, -1);
                    var currentLogPath = string.Empty;
                    while (currentLogPath != filepath)
                    {
                        currentLogPath = filepath;
                        if (string.IsNullOrEmpty(filepath))
                        {
                            this.CloseLog();
                        }
                        else
                        {
                            if (!force && this.logreader is not null && string.Equals(Path.IsPathFullyQualified(filepath) ? filepath : Path.GetFullPath(filepath), this.logreader.Fullpath, StringComparison.OrdinalIgnoreCase)) return;
                            if (File.Exists(filepath))
                            {
                                this.CloseLog();
                                this.RefreshDate();
                                this.LoadLog(filepath);
                                string? secondLast = this.logCategoriesImpl.SelectLog(categoryName, -2);
                                if (!string.IsNullOrEmpty(secondLast) && File.Exists(secondLast))
                                {
                                    using (var cancel = new CancellationTokenSource())
                                    {
                                        try
                                        {
                                            this.cancelSrcLoad?.Cancel();
                                        }
                                        catch (ObjectDisposedException) { }
                                        catch (InvalidOperationException) { }
                                        this.cancelSrcLoad = cancel;
                                        try
                                        {
                                            await PSO2LogAsyncListener.FetchAllData(secondLast, this.cancelSrcLoad.Token, this.ConvertDataReceivedCallbackToEventHandler);
                                        }
                                        catch (ObjectDisposedException) { }
                                        catch (InvalidOperationException) { }
                                        finally
                                        {
                                            this.cancelSrcLoad = null;
                                        }
                                    }
                                }
                            }
                        }

                        filepath = this.logCategoriesImpl.SelectLog(categoryName, -1);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref this.logloadingstate, 0);
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

        private void ConvertDataReceivedCallbackToEventHandler(in PSO2LogData data)
        {
            this.Logreader_DataReceived(null, in data);
        }

        private bool IsAlphaReactor(ReadOnlySpan<char> itemname)
            => (MemoryExtensions.Equals(itemname, this.str_AlphaReactor_en, StringComparison.OrdinalIgnoreCase)
                || MemoryExtensions.Equals(itemname, this.str_AlphaReactor_jp, StringComparison.OrdinalIgnoreCase));

        private bool IsStellarSeed(ReadOnlySpan<char> itemname)
        {
            if (MemoryExtensions.Equals(itemname, this.str_StellarSeed_jp, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                // Compare to "Stellar Seed"
                if (MemoryExtensions.Equals(itemname, this.str_StellarSeed_en1, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else
                {
                    // Compare to "Stella Seed"
                    var something = this.str_StellarSeed_en1.AsSpan();
                    if ((something.Length - 1) == itemname.Length && MemoryExtensions.Equals(itemname.Slice(0, 6), something.Slice(0, 6), StringComparison.OrdinalIgnoreCase) && MemoryExtensions.Equals(itemname.Slice(6), something.Slice(7), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        // Compare to "Stellar Shard"
                        if (MemoryExtensions.Equals(itemname, this.str_StellarSeed_en2, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                        else
                        {
                            // Compare to "Stella Shard"
                            something = this.str_StellarSeed_en2.AsSpan();
                            if ((something.Length - 1) == itemname.Length && MemoryExtensions.Equals(itemname.Slice(0, 6), something.Slice(0, 6), StringComparison.OrdinalIgnoreCase) && MemoryExtensions.Equals(itemname.Slice(6), something.Slice(7), StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }
        }

        private void Logreader_DataReceived(PSO2LogAsyncListener? arg1, in PSO2LogData arg2)
        {
            var datas = arg2.GetDataColumns();
            // All string creations below (aka the "new string()") will allocate a new char[] buffer and copies all the chars to the new buffer.
            // Thus, all strings below can be used beyond the event's invocation.
            if (MemoryExtensions.Equals(datas[2].Span, this.str_ActionPickup, StringComparison.OrdinalIgnoreCase))
            {
                bool isAlphaReactor = this.IsAlphaReactor(datas[5].Span);
                bool isStellaSeed = this.IsStellarSeed(datas[5].Span);
                if (isAlphaReactor || isStellaSeed)
                {
                    var dateonly = DateOnly.ParseExact(datas[0].Span.Slice(0, 10), this.str_DateOnlyFormat);
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
                    if (long.TryParse(datas[3].Span, out var playeridNumber) || long.TryParse(datas[datas.Count - 3].Span, out playeridNumber))
                    {
                        if (dateonly_logtime_jst == dateonly_currenttime_jst)
                        {
                            this.AddOrModifyCharacterData(playeridNumber, new string(datas[4].Span), isAlphaReactor ? 1 : 0, isStellaSeed ? 1 : 0);
                        }
                        else
                        {
                            this.AddOrModifyCharacterData(playeridNumber, new string(datas[4].Span));
                        }
                    }
                }
                else
                {
                    if (long.TryParse(datas[3].Span, out var playeridNumber) || long.TryParse(datas[datas.Count - 3].Span, out playeridNumber))
                    {
                        this.AddOrModifyCharacterData(playeridNumber, new string(datas[4].Span));
                    }
                }
            }
            else if (MemoryExtensions.Equals(datas[2].Span, this.str_ShopAction_SetPrice, StringComparison.OrdinalIgnoreCase))
            {
                // return;
                // This has no character/account information in the line. So skip it.
            }
            else if (long.TryParse(datas[datas.Count - 3].Span, out var playerIdNumber))
            {
                this.AddOrModifyCharacterData(playerIdNumber, new string(datas[4].Span));
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

        private void LoadLog(string filepath)
        {
            this.logreader = new PSO2LogAsyncListener(filepath);
            this.logreader.DataReceived += this.Logreader_DataReceived;
            this.logreader.StartReceiving();
        }

        /// <summary>Close the current opening log file if there is one. Otherwise does nothing.</summary>
        public void CloseLog()
        {
            if (this.Dispatcher.CheckAccess())
            {
                foreach (var character in this.characters)
                {
                    character.AlphaReactorCount = 0;
                    character.StellarSeedCount = 0;
                }
                if (this.logreader is not null)
                {
                    this.logreader.DataReceived -= this.Logreader_DataReceived;
                    this.logreader.Dispose();
                }
            }
            else
            {
                this.Dispatcher.Invoke(this.CloseLog);
            }
        }

        private static void OnThrottleEventInvocation(object? sender, EventArgs e)
        {
            if (sender is DispatcherTimer timer && timer.Tag is ToolboxWindow_AlphaReactorCount window)
            {
                timer.Stop();
                _ = window.SelectNewestLog(LogCategories.ActionLog);
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
    }
}
