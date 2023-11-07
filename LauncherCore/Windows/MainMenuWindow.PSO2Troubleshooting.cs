using Leayal.PSO2Launcher.Core.Classes;
using Leayal.Shared.Windows;
using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.SharedInterfaces;
using System.Diagnostics;
using Microsoft.Win32;
using System.ComponentModel;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.AvalonEdit;
using System.Windows.Media;
using System.Windows.Documents;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private void TabMainMenu_ButtonPSO2Troubleshooting_Clicked(object sender, RoutedEventArgs e)
        {
            var dialog = new PSO2TroubleshootingWindow(this.config_main)
            {
                Owner = this
            };
            dialog.ShowDialog();
        }

        private async void TabMainMenu_ButtonRemoveWellbiaACClicked(object sender, RoutedEventArgs e)
        {
            const string WellbiaUninstallerSender = "Wellbia Anti-cheat uninstaller";
            if (sender is TabMainMenu tab)
            {
                tab.ButtonRemoveWellbiaACClicked -= this.TabMainMenu_ButtonRemoveWellbiaACClicked;

                var lines = new System.Collections.Generic.List<Inline>(UacHelper.IsCurrentProcessElevated ? 12 : 16)
                {
                    new Run("Are you sure you want to \"purge clean\" Wellbia's anti-cheat from your system?"),
                    new LineBreak(),
                    new Run("*Please DO NOT use this function while there are any other games using XignCode or Wellbia's stuffs, it may lead to unexpected behaviors such as false-positive bans on those other games.") { FontSize = SystemFonts.MessageFontSize + 3 },
                    new LineBreak(),
                    new Run("Only use this function when you made sure no games using Wellbia's stuffs is running, if you're unsure about this, please abort by pressing 'No'.*") { FontSize = SystemFonts.MessageFontSize + 3 },
                    new LineBreak(),
                    new LineBreak(),
                    new Run("Notes:"),
                    new LineBreak(),
                    new Run("- The anti-cheat will be reinstalled again if you start the game with Wellbia anti-cheat next time."),
                    new LineBreak(),
                    new Run("- Please press 'OK' button on the XignCode uninstaller's dialog showing a successful uninstallation message.")
                };

                if (!UacHelper.IsCurrentProcessElevated)
                {
                    lines.Add(new LineBreak());
                    lines.Add(new Run("- "));
                    lines.Add(new Run("This uninstallation process requires Administration privileges:") { FontSize = SystemFonts.MessageFontSize + 3 });
                    lines.Add(new Run(" If you proceed, the launcher will launch Command-Prompt as Administration to execute a script which will clean Wellbia's stuffs up, please allow Command-Prompt to be run as Admin if Windows shows a dialog asks you allow or not."));
                    lines.Add(new LineBreak());
                    lines.Add(new Run("  + Alternatively, you can launch this launcher as Admin and use this function again for more verbose uninstallation messages."));
                    lines.Add(new LineBreak());
                    lines.Add(new Run("- "));
                    lines.Add(new Run("PLEASE DO NOT CLOSE THE COMMAND-PROMPT WINDOW:") { FontSize = SystemFonts.MessageFontSize + 3 });
                    lines.Add(new Run(" The window will automatically close itself once the uninstallation process is complete."));
                }

                if (Prompt_Generic.Show(this, lines, "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    tab.ButtonRemoveWellbiaACClicked += this.TabMainMenu_ButtonRemoveWellbiaACClicked;
                    return;
                }

                // ....A bit messy, but overlapping the CancellationSource of GameUpdater
                var newCancelSrc = CancellationTokenSource.CreateLinkedTokenSource(this.cancelAllOperation.Token);
                var theExistingCancelSrc = Interlocked.CompareExchange(ref this.cancelSrc_gameupdater, newCancelSrc, null);

                bool isOverlapping = false;

                if (theExistingCancelSrc == null)
                {
                    theExistingCancelSrc = newCancelSrc;
                }
                else if (theExistingCancelSrc != newCancelSrc)
                {
                    isOverlapping = true;
                    newCancelSrc.Dispose();
                }
                
                try
                {
                  
                    this.TabGameClientUpdateProgressBar.IsIndetermined = true;
                    this.TabGameClientUpdateProgressBar.IsSelected = true;

                    await Task.Run(async () =>
                    {
                        var cancel_token = theExistingCancelSrc.Token;
                        string filename_xignCodeUninstaller = string.Empty;
                        try
                        {
                            this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, (console, writer, offset, mainmenu) => {
                                writer.Write("Downloading Wellbia XignCode Uninstaller from ");
                                mainmenu.ConsoleLogHelper_WriteHyperLink(writer, "Wellbia's FAQ site", StaticResources.WellbiaFAQWebsite, ConsoleLog_OpenLinkWithDefaultBrowser);
                                writer.Write("...");
                            }, this);
                            using (var contentStream = await this.pso2HttpClient.DownloadWellbiaUninstaller(cancel_token))
                            {
                                if (cancel_token.IsCancellationRequested) return;

                                using (var zipArchive = new ZipArchive(contentStream, ZipArchiveMode.Read, false))
                                {
                                    foreach (var entry in zipArchive.Entries)
                                    {
                                        if (entry != null && entry.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                        {
                                            filename_xignCodeUninstaller = Path.GetFullPath(entry.Name, RuntimeValues.RootDirectory);

                                            this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, (console, writer, offset, arg) => {
                                                var (mainmenu, filename_xignCodeUninstaller) = arg;
                                                writer.Write("Writing downloaded file to ");
                                                mainmenu.ConsoleLogHelper_WriteHyperLink(writer, "xuninstaller.exe", new Uri(filename_xignCodeUninstaller), mainmenu.ConsoleLog_SelectLocalPathLinkInExplorer);
                                                writer.Write(".");
                                            }, (this, filename_xignCodeUninstaller));

                                            entry.ExtractToFile(filename_xignCodeUninstaller, true);
                                            break;
                                        }
                                    }
                                    if (string.IsNullOrEmpty(filename_xignCodeUninstaller)) throw new Exception("Failed to download XignCode uninstaller.");
                                }
                            }

                            if (UacHelper.IsCurrentProcessElevated)
                            {
                                static void InvokeProcess(string filename, params string[] args)
                                {
                                    using (var proc = new Process())
                                    {
                                        proc.StartInfo.FileName = filename;
                                        proc.StartInfo.UseShellExecute = false;
                                        proc.StartInfo.CreateNoWindow = true;
                                        if (args != null && args.Length != 0)
                                        {
                                            var argList = proc.StartInfo.ArgumentList;
                                            for (int i = 0; i < args.Length; i++)
                                                argList.Add(args[i]);
                                        }
                                        proc.Start();
                                        proc.WaitForExit();
                                    }
                                }
                                this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, (console, writer, offset, arg) => {
                                    var (mainmenu, filename_xignCodeUninstaller) = arg;
                                    writer.Write("Launching ");
                                    mainmenu.ConsoleLogHelper_WriteHyperLink(writer, "xuninstaller.exe", new Uri(filename_xignCodeUninstaller), mainmenu.ConsoleLog_SelectLocalPathLinkInExplorer);
                                    writer.Write(" to uninstall XignCode anti-cheat from your system. ");

                                    var absoluteOffsetStart = writer.InsertionOffset;
                                    writer.Write("Please press 'OK' when the uninstaller shows a dialog saying that XignCode is successfully uninstalled or the launcher will wait forever for it.");
                                    var absoluteOffsetEnd = writer.InsertionOffset;
                                    mainmenu.consolelog_textcolorizer.Add(new TextStaticTransformData(absoluteOffsetStart, absoluteOffsetEnd - absoluteOffsetStart, Brushes.Gold, Brushes.DarkGoldenrod)
                                    {
                                        Typeface = mainmenu.consolelog_boldTypeface
                                    });
                                }, (this, filename_xignCodeUninstaller));
                                InvokeProcess(filename_xignCodeUninstaller);

                                this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, "Attempting deleting 'ucldr_PSO2_JP' service of XignCode if it still remains...");
                                InvokeProcess("sc", "delete", "ucldr_PSO2_JP");
                                this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, "Attempting deleting 'xhunter1' service of XignCode if it still remains...");
                                InvokeProcess("sc", "delete", "xhunter1");
                                try
                                {
                                    var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                                    File.Delete(Path.GetFullPath("xhunter1.sys", windir));
                                    File.Delete(Path.GetFullPath("xhunters.log", windir));

                                    static void AttemptDeleteAPath(string path)
                                    {
                                        try
                                        {
                                            var dir_attr = File.GetAttributes(path);
                                            if ((dir_attr & FileAttributes.ReadOnly) != 0)
                                            {
                                                // Shouldn't be here anyway
                                                File.SetAttributes(path, dir_attr & ~FileAttributes.ReadOnly);
                                            }
                                            Directory.Delete(path, true);
                                        }
                                        catch (FileNotFoundException) { /* Do nothing */ }
                                        catch (DirectoryNotFoundException) { /* Do nothing */ }
                                        catch (PathTooLongException) { /* Do nothing */ }
                                        catch (IOException)
                                        {
                                            if (File.Exists(path))
                                            {
                                                // It's a file.
                                                var attr = File.GetAttributes(path);
                                                if ((attr & FileAttributes.ReadOnly) != 0)
                                                {
                                                    File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
                                                }
                                                File.Delete(path);
                                            }
                                            else
                                            {
                                                // There's at least one file has "Read-Only" attribute within the directory.
                                                foreach (var filesysteminfo in Directory.EnumerateFileSystemEntries(path, "*", new EnumerationOptions()
                                                {
                                                    RecurseSubdirectories = true,
                                                    MaxRecursionDepth = 10,
                                                    ReturnSpecialDirectories = false
                                                }))
                                                {
                                                    var attr = File.GetAttributes(filesysteminfo);
                                                    if ((attr & FileAttributes.ReadOnly) != 0)
                                                    {
                                                        File.SetAttributes(filesysteminfo, attr & ~FileAttributes.ReadOnly);
                                                    }
                                                }

                                                // Attempt to delete again, but will throw error if it fails, for this time.
                                                Directory.Delete(path, true);
                                            }
                                        }
                                    }

                                    AttemptDeleteAPath(Path.GetFullPath("WELLBIA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
                                    AttemptDeleteAPath(Path.GetFullPath("Wellbia.com", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));

                                    this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, "Cleaned up left-over files of XignCode.");
                                }
                                catch (Exception ex)
                                {
                                    this.CreateNewErrorLineInConsoleLog(WellbiaUninstallerSender, "Failed to clean up left-over files of XignCode.", "Error while cleaning up XignCode files", ex);
                                }
                                try
                                {
                                    Registry.LocalMachine.DeleteSubKeyTree(Path.Join("SYSTEM", "ControlSet001", "Services", "ucldr_PSO2_JP"), false);
                                    this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, "Cleaned up registry of XignCode.");
                                }
                                catch (Exception ex)
                                {
                                    this.CreateNewErrorLineInConsoleLog(WellbiaUninstallerSender, "Failed to clean up registry entries of XignCode.", "Error while cleaning up XignCode registry entries", ex);
                                }
                            }
                            else
                            {
                                var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                                // 'Runas' the command prompt and executing cmd-line.
                                string batch_script_content = "@start \"XignCode Uninstaller\" /WAIT /B \"" + filename_xignCodeUninstaller + "\"" + Environment.NewLine
                                + "@sc delete ucldr_PSO2_JP" + Environment.NewLine
                                + "@sc delete xhunter1" + Environment.NewLine
                                + "@reg delete \"" + Path.Join("HKLM", "SYSTEM", "ControlSet001", "Services", "ucldr_PSO2_JP") + "\" /va /f"+ Environment.NewLine
                                + "@del /F /Q \"" + Path.GetFullPath("xhunter1.sys", windir) + "\"" + Environment.NewLine
                                + "@del /F /Q \"" + Path.GetFullPath("xhunters.log", windir) + "\"" + Environment.NewLine
                                + "@rmdir /S /Q \"" + Path.GetFullPath("WELLBIA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + "\"" + Environment.NewLine
                                + "@rmdir /S /Q \"" + Path.GetFullPath("Wellbia.com", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) + "\"" + Environment.NewLine;

                                var batchFilename = Path.ChangeExtension(filename_xignCodeUninstaller, "bat");
                                try
                                {
                                    this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, (console, writer, offset, arg) => {
                                        var (mainmenu, batchFilename) = arg;
                                        writer.Write("Writing cleanup script ");
                                        mainmenu.ConsoleLogHelper_WriteHyperLink(writer, Path.GetFileName(batchFilename), new Uri(batchFilename), mainmenu.ConsoleLog_SelectLocalPathLinkInExplorer);
                                        writer.Write(" for Command-Prompt to execute...");
                                    }, (this, batchFilename));
                                    File.WriteAllText(batchFilename, batch_script_content, System.Text.Encoding.ASCII);
                                    using (var proc = new Process())
                                    {
                                        proc.StartInfo.FileName = Path.GetFullPath("cmd.exe", Environment.GetFolderPath(Environment.SpecialFolder.System));
                                        proc.StartInfo.UseShellExecute = true;
                                        proc.StartInfo.Verb = "runas";
                                        proc.StartInfo.Arguments = @$"/C ""call ""{batchFilename}""""";
                                        this.CreateNewWarnLineInConsoleLog(WellbiaUninstallerSender, "Please DO NOT close the Command-Prompt's window while it's executing. And please press 'OK' when the uninstaller shows a dialog saying that XignCode is successfully uninstalled or the launcher will wait forever for it.");
                                        this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, (console, writer, offset, mainmenu) => {
                                            var absoluteOffsetStart1 = writer.InsertionOffset;
                                            writer.Write("Please allow Command-Prompt (or Windows Command Processor) to be run as Admin.");
                                            var absoluteOffsetEnd1 = writer.InsertionOffset;
                                            mainmenu.consolelog_textcolorizer.Add(new TextStaticTransformData(absoluteOffsetStart1, absoluteOffsetEnd1 - absoluteOffsetStart1, Brushes.Gold, Brushes.DarkGoldenrod)
                                            {
                                                Typeface = this.consolelog_boldTypeface
                                            });
                                        }, this);
                                        proc.Start();
                                        proc.WaitForExit();
                                    }
                                }
                                finally
                                {
                                    File.Delete(batchFilename);
                                }
                            }
                        }
                        finally
                        {
                            if (!string.IsNullOrEmpty(filename_xignCodeUninstaller)) File.Delete(filename_xignCodeUninstaller);
                        }
                    });
                    this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, "Wellbia XignCode uninstallation process is completed.");
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, "User cancelled the uninstallation process by disallowing Command-Prompt to be run as Admin.");
                }
                catch (Exception ex)
                {
                    this.CreateNewErrorLineInConsoleLog(WellbiaUninstallerSender, "Wellbia XignCode uninstallation process is completed with error(s).", "Error while uninstalling Wellbia XignCode", ex);
                }
                finally
                {
                    this.CreateNewLineInConsoleLog(WellbiaUninstallerSender, "Cleaned up uninstaller's files.");
                    tab.ButtonRemoveWellbiaACClicked += this.TabMainMenu_ButtonRemoveWellbiaACClicked;
                    if (!isOverlapping)
                    {
                        Interlocked.Exchange(ref this.cancelSrc_gameupdater, null);
                        theExistingCancelSrc.Dispose();
                        this.TabMainMenu.IsSelected = true;
                        this.TabGameClientUpdateProgressBar.IsIndetermined = false;
                    }
                }
            }
        }
    }
}
