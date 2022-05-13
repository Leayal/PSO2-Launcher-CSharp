# PSO2 Alpha Reactor & Stellar Seed Counter

- This feature works by parsing the ActionLog file which is dumped by the game itself. The log file can be located in `Documents\SEGA\PHANTASYSTARONLINE2\log_ngs`.
- That also means:
  - If you truncate or modify the log content or delete the log files. The tracking may not yield the correct result.
  - This is absolutely safe as the launcher does not patch or touch anything of the game.
- The JST time is calculated from your local clock. Therefore, if the local time is not correct, calculated JST time won't be, too.
  - I used local time to eliminate the need of Internet.
- The JST clock's state is sync with the launcher's setting.
  - To disable the clock, simply disable in the Launcher Behavior Manager.
  - When disabled, the Counter won't automatically reset the Alpha Reactor's pickup count when the daily spawn reset occurs. You will need to reopen the Counter window to manually reload the count.

## Notes

Sometimes you may see `Stellar Seed`'s count goes beyond 10, or `Alpha Reactor`'s count goes beyond 14. These counts may be from destroying Gold Boxes (Or Yellow Crates, which has a small chance to give these items). Therefore, to confirm whether the tool has problem or not, please check the log file (files with `ActionLogYYYYmmdd_00.txt` format in the location stated above. `YYYY` is the year, `mm` is the month and `dd` is the day, month and day will have prefix 0 if it's below 10. E.g: `ActionLog20220315_00.txt`, `ActionLog20221207_00.txt`).

# Referenced files:

- [ToolboxWindow_AlphaReactorCount.xaml.cs](LauncherToolbox.Windows/ToolboxWindow_AlphaReactorCount.xaml.cs)
- [PSO2LogAsyncListener.cs](LauncherToolbox/PSO2LogAsyncListener.cs)

![Dark theme preview](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/toolbox/alphareactorcounter-dark.png)
