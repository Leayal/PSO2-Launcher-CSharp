using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
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
        private static readonly Lazy<LogCategories> PSO2LogWatcher = new Lazy<LogCategories>(() => new LogCategories(Application.Current.Dispatcher));
        /// <summary>The reset time in local (JST time). Please note that that this is the fixed hour of the day.</summary>
        private static readonly TimeOnly DailyResetTime = new TimeOnly(4, 0);

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

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty CurrentTimeProperty = CurrentTimePropertyKey.DependencyProperty;

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty IsBeforeResetProperty = IsBeforeResetPropertyKey.DependencyProperty;

        /// <summary>Gets a <seealso cref="DateTime"/> of the current time.</summary>
        /// <remarks>This is not high-resolution and therefore may not be accurate.</remarks>
        public DateTime CurrentTime => (DateTime)this.GetValue(CurrentTimeProperty);

        /// <summary>Determines whether the current time is before or after the daily reset.</summary>
        /// <remarks>
        /// <para><c>null</c> - Right at daily reset.</para>
        /// <para><c>true</c> - Before daily reset.</para>
        /// <para><c>false</c> - After daily reset.</para>
        /// </remarks>
        public bool? IsBeforeReset => (bool?)this.GetValue(IsBeforeResetProperty);

        private PSO2LogAsyncReader? logreader;
        private readonly ObservableCollection<AccountData> characters;
        private readonly Dictionary<long, AccountData> mapping;
        private readonly Action<LogCategories, List<string>> Logfiles_NewFileFound;
        private readonly LogCategories.ClockTickedCallback Clock_Tick;

        /// <summary>Creates a new window.</summary>
        public ToolboxWindow_AlphaReactorCount() : this(null) { }

        /// <summary>Creates a new window with the given icon</summary>
        /// <param name="icon">The bitmap contains the window's icon you want to set.</param>
        public ToolboxWindow_AlphaReactorCount(BitmapSource? icon) : base()
        {
            this.mapping = new Dictionary<long, AccountData>();
            this.characters = new ObservableCollection<AccountData>();
            this.SetTime(TimeZoneHelper.ConvertTimeToLocalJST(DateTime.UtcNow));
            this.Logfiles_NewFileFound = new Action<LogCategories, List<string>>(this.Logfiles_OnNewFileFound);
            this.Clock_Tick = new LogCategories.ClockTickedCallback(this.OnClockTicked);
            InitializeComponent();
            if (icon is not null)
            {
                this.Icon = icon;
            }
        }

        /// <inheritdoc/>
        protected override Task OnCleanupBeforeClosed()
        {
            var logfiles = PSO2LogWatcher.Value;
            logfiles.StopWatching(this.Logfiles_NewFileFound, this.Clock_Tick);
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
            logfiles.StartWatching(this.Logfiles_NewFileFound, this.Clock_Tick);
            this.SelectNewestLog(logfiles.ActionLog);
        }

        private void Logfiles_OnNewFileFound(object sender, List<string> ev)
        {
            if (ev == PSO2LogWatcher.Value.ActionLog)
            {
                this.SelectNewestLog(ev);
            }
        }

        private void OnClockTicked(in DateTime time, in bool isResetting)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.SetTime(time);
            }
            else
            {
                if (!isResetting)
                {
                    this.Dispatcher.BeginInvoke(new Action<DateTime>(this.SetTime), new object[] { time });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(new Action<List<string>, bool>(this.SelectNewestLog), new object[] { PSO2LogWatcher.Value.ActionLog, true });
                }
            }
        }

        /// <summary>Select the newest log file from the given category.</summary>
        /// <param name="logcategory">The log category to get the log file.</param>
        /// <param name="force">Determines whether a force reload the log file is required.</param>
        public void SelectNewestLog(List<string> logcategory, bool force = false) => this.SelectLog(logcategory, logcategory.Count - 1, force);

        /// <summary>Select a certain log file from the given category.</summary>
        /// <param name="logcategory">The log category to get the log file.</param>
        /// <param name="index">The index of the log file from the category.</param>
        /// <param name="force">Determines whether a force reload the log file is required.</param>
        public void SelectLog(List<string> logcategory, int index, bool force = false)
        {
            if (logcategory.Count == 0 || index < 0)
            {
                this.CloseCurrentLog();
            }
            else
            {
                var filepath = logcategory[index];
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
            this.SetValue(IsBeforeResetPropertyKey, TimeOnly.FromDateTime(datetime) < DailyResetTime);
        }

        /// <summary>Force refresh the displayed <seealso cref="DateTime"/> on the UI.</summary>
        /// <remarks>This is totally not required and not useful in most cases.</remarks>
        public void RefreshDate()
        {
            var today = TimeZoneHelper.ConvertTimeToLocalJST(DateTime.UtcNow);
            if (this.Dispatcher.CheckAccess())
            {
                this.SetTime(today);
            }
            else
            {
                this.Dispatcher.Invoke(new Action<DateTime>(this.SetTime), new object[] { today });
            }
        }

        private void Logreader_DataReceived(PSO2LogAsyncReader arg1, LogReaderDataReceivedEventArgs arg2)
        {
            var datas = arg2.GetDatas();
            if (datas[2].Span.Equals("[Pickup]", StringComparison.OrdinalIgnoreCase) && (datas[5].Span.Equals("Alpha Reactor", StringComparison.OrdinalIgnoreCase) || datas[5].Span.Equals("アルファリアクター", StringComparison.OrdinalIgnoreCase)))
            {
                var dateonly = DateOnly.ParseExact(datas[0].Span.Slice(0, 10), "yyyy-MM-dd");
                var timeonly = TimeOnly.Parse(datas[0].Span.Slice(11));
                var logtime = new DateTime(dateonly.Year, dateonly.Month, dateonly.Day, timeonly.Hour, timeonly.Minute, timeonly.Second, DateTimeKind.Local);

                // Consider push the datetime before reset to become previous day.
                var logtime_jst = AdjustToGameResetTime(TimeZoneHelper.ConvertTimeToLocalJST(logtime));
                var currenttime_jst = AdjustToGameResetTime(TimeZoneHelper.ConvertTimeToLocalJST(DateTime.Now));

                // Consider push the datetime before reset to become previous day.
                var dateonly_logtime_jst = DateOnly.FromDateTime(logtime_jst);
                var dateonly_currenttime_jst = DateOnly.FromDateTime(currenttime_jst);

                if (dateonly_logtime_jst != dateonly_currenttime_jst)
                {
                    this.RefreshDate();
                }
                if (dateonly_logtime_jst == dateonly_currenttime_jst)
                {
                    this.IncreaseCharacterAlphaCount(long.Parse(datas[3].Span), new string(datas[4].Span));
                }
            }
            else
            {
                this.AddCharacter(long.Parse(datas[3].Span), new string(datas[4].Span));
            }
        }

        private void AddCharacter(long charId, string charName)
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
            }
            else
            {
                this.Dispatcher.Invoke(new Action<long, string>(this.AddCharacter), new object[] { charId, charName });
            }
        }

        private void IncreaseCharacterAlphaCount(long charId, string charName)
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
                data.AlphaReactorCount++;
            }
            else
            {
                this.Dispatcher.Invoke(new Action<long, string>(this.IncreaseCharacterAlphaCount), new object[] { charId, charName });
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
