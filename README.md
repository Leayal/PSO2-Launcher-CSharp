# PSO2-Launcher-CSharp

An alternative game launcher for Phantasy Star Online 2: New Genesis (JP) written in C# (.NET6).

The launcher only targets PSO2 Japan. It may not work for other regions without modifications.

This project is just a hobby one.

# Feature Overviews

- The launcher is designed to be left running for a very long time. Therefore, it is not necessary to close the launcher shortly after using, but it's totally okay to do it, too.
  - If leaving the launcher's window visible on the screen is not a good taste for you, please try `Minimize to tray` feature (read below).
- All functions related to update PSO2 JP client.
  - Check for PSO2 Updates.
  - Perform game client update with some customizable settings (see the UI previews below).
- ~~Installing~~ Deploying PSO2 JP client:
  - This is actually a little different from an installation. There will be no uninstaller created.
  - To remove the deployed game client, you just need to delete the game's directory. That's pretty much all you need to do.
- Launch PSO2 JP game (traditional method and new method supported):
  - Traditional method: launch game without asking for login info. Then player will login in-game.
    - This is the old way to launch PSO2 game before NGS release.
  - New method: Prompt a login with SEGA JP account then use the info to launch the game.
    - If you don't trust this launcher, you shouldn't use this.
    - The login info is not persistently saved anywhere. There is an unrelated feature which will remember the login info in the memory until the launcher exits. However, this is not enabled by default. By default, the launcher will `login-and-forget` for each GameStart.
  - **You can set the default method as you desired in the `Launcher behavior manager` (preview image below).**
- Minimize to tray (or minimize to `Notification Area`, or `Windows's clock area` on the taskbar):
  - Double-click on the launcher's icon in the Notification Area to restore the launcher from tray.
  - There is a quick menu if you right-click the launcher's icon in the Notification Area. The quick menu contains quick `most used functions`:
    - Game start (using default method which is set by user).
      - Game start without SEGA login (Bypass user's setting).
      - Game start with SEGA login (Bypass user's setting).
      - Forget SEGA Login (Forget the SEGA login if user logged in with `remember the login info`).
    - Check for PSO2 updates.
- Configure PSO2 Client's options.
  - Classic options are planned.
- Customizable RSS Feeds:
  - Add or remove RSS Feeds by URLs (When adding, the `Default` Base Handler or `Leayal.PSO2Launcher.RSS.Handlers.WordpressRSSFeed` Base Handler should be sufficient for most cases).
  - Plugin system is available to add more handler to handle special RSS Feeds.
  - `Deferred Refresh` setting per-feed allows the user to customize which feed will be actively refreshed: If enabled, this `deferred refresh` will stop any unfocused RSS feeds (in UI) from refreshing the feed data, only when they're focused (or being in the UI view) that they start to refresh data.
- \[BETA\] PSO2 Client Troubleshooting:
  - The feature is currently in beta, but it's not changing anything to the system or any files so it's perfectly safe to try. Any technical advices, fixes and explanations are welcomed to share, but please do so by [Creating issues](https://github.com/Leayal/PSO2-Launcher-CSharp/issues) with `label` including `enhancement`. The more you explain about how and why the problem is there, and how the fix works is valuable information.
  - Main purpose is to help player who has technical issue(s) with the game client. However, anything outside of the game client is not mentioned, such as Internet's problem, damaged OS's important files, etc...
  - Most of results and words are "advices", it is not a 'must' to follow accordingly unless stated.
- Convenient Utilities/Toolbox:
  - Currently, the toolbox has only `PSO2 Alpha Reactor Counter` which helps user to keep track of the amount of Alpha Reactor that user has picked up in-game. [How does it work?](Feature-Explanation.md#PSO2-Alpha-Reactor-Counter)
  - The toolbox is accessible via the main menu and via the quick menu if you minimize the launcher to tray.
- Minimal JST clock:
  - The clock can be enabled/disabled in Launcher Behavior Manager dialog.
  - When enabled, it will be visible at the bottom of the launcher's window and at the context menu of the Tray Icon.
  - When disabled, it will be hidden in all places and the clock instance will be stopped to save CPU. Though, the amount is very insignificant.
- Minimal compatibility with [PSO2 Tweaker](https://arks-layer.com/):
  - **It is disabled by default**. To enable, please press `Launcher Options` and then select `Manage launcher's compatibility` in the dropdown.
  - Update Tweaker's hash cache (which is used to speed up game client updating progress) when updating the game client with this launcher. However, you should **NOT** using both to update the game at the same time.
  - Allow user to launch the game via PSO2 Tweaker instead of launching the game client directly. This also allows Tweaker to manage Patches and perform its workaround magics. But you shouldn't exit this launcher before Tweaker is closed in order to avoid corrupting Tweaker's config. To enable this launching game method, you can the setting in Launcher's behavior or in the GameStart's dropdown menu.

### Launcher arguments:

- `--tray`: Launch the launcher and minimize to tray immediately.

# UI Previews

- Theming: User can let the launcher sync with Windows 10's `Dark|Light App Mode` setting or manually select a theme that you prefer.

|                                       |                                                          Dark theme                                                          |                                                          Light theme                                                           |
| :------------------------------------ | :--------------------------------------------------------------------------------------------------------------------------: | :----------------------------------------------------------------------------------------------------------------------------: |
| Main menu                             |             ![Dark theme mainmenu](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/mainmenu-dark.png)             |             ![Light theme mainmenu](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/mainmenu-light.png)             |
| Main menu (RSS Feeds)                 |           ![Dark theme mainmenu](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/mainmenu-rss-dark.png)           |           ![Light theme mainmenu](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/mainmenu-rss-light.png)           |
| Main menu (Console log)               |     ![Dark theme mainmenu console](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/mainmenu-console-dark.png)     |     ![Light theme mainmenu console](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/mainmenu-console-light.png)     |
| Dialog: Launcher Behavior Manager     |             ![Dark theme behavior](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/behavior-dark.png)             |             ![Light theme behavior](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/behavior-light.png)             |
| Dialog: PSO2 Data Manager             |           ![Dark theme data manager](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/data-mgr-dark.png)           |           ![Light theme data manager](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/data-mgr-light.png)           |
| Dialog: RSS Feeds Manager             |         ![Dark theme behavior](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/rss-feed-manager-dark.png)         |         ![Light theme behavior](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/rss-feed-manager-light.png)         |
| Dialog: Launcher Theming Manager      |             ![Dark theme behavior](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/thememgr-dark.png)             |             ![Light theme behavior](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/thememgr-light.png)             |
| Dialog: PSO2 Configuration            |       ![Dark theme PSO2 User Config](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/pso2options-dark.png)        |       ![Light theme PSO2 User Config](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/pso2options-light.png)        |
| Dialog: PSO2 Configuration (Advanced) | ![Dark theme PSO2 Advanced User Config](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/pso2options-adv-dark.png) | ![Light theme PSO2 Advanced User Config](https://leayal.github.io/PSO2-Launcher-CSharp/imgs/preview/pso2options-adv-light.png) |

# Download Releases

- If you are interested in using this launcher, please check out the [Release section of this repository](https://github.com/Leayal/PSO2-Launcher-CSharp/releases/).
- If you are curious about what changes are made (or `change log`), please check out the [Git commit log](https://github.com/Leayal/PSO2-Launcher-CSharp/commits/main):
  - "Checkpoint" commits are non-release commits. Their main purpose is to make a revertible point (in case I mess things up).
  - Anything else is self-explained, such as `minor updates`, `bugfixes` and `feature update`. They are the releases which are published and so they contain short description to summarize the changes. If you are curious about what's the changes, these commits are what you're probably looking for.

# Development

As of writing this `README.md`, I am using: `Visual Studio 2022` (stable channel) with `.NET desktop development` bundle. Specifically, the launcher was written with `C# 9.0` of `.NET6`.

- Any IDEs supporting `.NET6 SDK` development should be usable to develop or compile this launcher.
