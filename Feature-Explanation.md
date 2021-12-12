# PSO2 Alpha Reactor Counter

- This feature works by parsing the ActionLog file which is dumped by the game itself. The log file can be located in `Documents\SEGA\PHANTASYSTARONLINE2\log_ngs`.
- That also means:
  - If you truncate or modify the log content or delete the log files. The tracking may not yield the correct result.
  - This is absolutely safe as the launcher does not patch or touch anything of the game.
- The JST time is calculated from your local clock. Therefore, if the local time is not correct, calculated JST time won't be, too.
  - I used local time to eliminate the need of Internet.
- Referenced files: [ToolboxWindow_AlphaReactorCount.xaml.cs](LauncherToolbox.Windows/ToolboxWindow_AlphaReactorCount.xaml.cs), [PSO2LogAsyncReader.cs](LauncherToolbox/PSO2LogAsyncReader.cs)

![Dark theme preview](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/toolbox/alphareactorcounter-dark.png)
