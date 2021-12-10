# PSO2 Alpha Reactor Counter

- This feature works by parsing the ActionLog file which is dumped by the game itself. The log file can be located in `Documents\SEGA\PHANTASYSTARONLINE2\log_ngs`.
- That also means:
  - If you truncate the log content or delete the log files. The tracking may not yield the correct result.
  - This is absolutely safe as the launcher does not patch or touch anything of the game.
- Referenced files: [ToolboxWindow_AlphaReactorCount.xaml.cs](LauncherCore/Windows/ToolboxWindow_AlphaReactorCount.xaml.cs), [PSO2LogAsyncReader.cs](LauncherToolbox/PSO2LogAsyncReader.cs)
