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
            if (sender is TabMainMenu tab)
            {
                tab.ButtonRemoveWellbiaACClicked -= this.TabMainMenu_ButtonRemoveWellbiaACClicked;

                string theQuestion = "Are you sure you want to \"purge clean\" Wellbia's anti-cheat from your system?" + Environment.NewLine
                    + "(The anti-cheat will be reinstalled again if you start the game with Wellbia anti-cheat next time)";
                if (Prompt_Generic.Show(this, UacHelper.IsCurrentProcessElevated ?
                   theQuestion + Environment.NewLine + Environment.NewLine
                   + "(Please press 'OK' button on the XignCode uninstaller's dialog showing a successful uninstallation message)"
                    : theQuestion + Environment.NewLine + "(If you agree, the launcher will launch Command-Prompt as Administration to clean Wellbia's stuffs up, please allow Command-Prompt to be launched as Admin)" + Environment.NewLine + Environment.NewLine
                    + "(PLEASE DO NOT CLOSE THE COMMAND-PROMPT WINDOW. ONLY PRESS 'OK' button on the XignCode uninstaller's dialog showing a successful uninstallation message)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
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
                                InvokeProcess(filename_xignCodeUninstaller);
                                InvokeProcess("sc", "delete", "ucldr_PSO2_JP");
                                InvokeProcess("sc", "delete", "xhunter1");
                                try
                                {
                                    File.Delete(Path.GetFullPath("xhunter1.sys", Environment.GetFolderPath(Environment.SpecialFolder.Windows)));
                                    File.Delete(Path.GetFullPath("xhunters.log", Environment.GetFolderPath(Environment.SpecialFolder.Windows)));

                                    Directory.Delete(Path.GetFullPath("WELLBIA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)), true);
                                    Directory.Delete(Path.GetFullPath("Wellbia.com", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)), true);
                                }
                                catch { }
                                Registry.LocalMachine.DeleteSubKeyTree(Path.Join("SYSTEM", "ControlSet001", "Services", "ucldr_PSO2_JP"), false);
                            }
                            else
                            {
                                // 'Runas' the command prompt and executing cmd-line.
                                string batch_script_content = "@start \"XignCode Uninstaller\" /WAIT /B \"" + filename_xignCodeUninstaller + "\"" + Environment.NewLine
                                + "@sc delete ucldr_PSO2_JP" + Environment.NewLine
                                + "@sc delete xhunter1" + Environment.NewLine
                                + "@reg delete \"" + Path.Join("HKLM", "SYSTEM", "ControlSet001", "Services", "ucldr_PSO2_JP") + "\" /va /f"+ Environment.NewLine
                                + "@del /F /Q \"" + Path.GetFullPath("xhunter1.sys", Environment.GetFolderPath(Environment.SpecialFolder.Windows)) + "\"" + Environment.NewLine
                                + "@del /F /Q \"" + Path.GetFullPath("xhunters.log", Environment.GetFolderPath(Environment.SpecialFolder.Windows)) + "\"" + Environment.NewLine
                                + "@rmdir /S /Q \"" + Path.GetFullPath("WELLBIA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + "\"" + Environment.NewLine
                                + "@rmdir /S /Q \"" + Path.GetFullPath("Wellbia.com", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) + "\"" + Environment.NewLine;

                                var batchFilename = Path.ChangeExtension(filename_xignCodeUninstaller, "bat");
                                try
                                {
                                    File.WriteAllText(batchFilename, batch_script_content, System.Text.Encoding.ASCII);
                                    using (var proc = new Process())
                                    {
                                        proc.StartInfo.FileName = Path.GetFullPath("cmd.exe", Environment.GetFolderPath(Environment.SpecialFolder.System));
                                        proc.StartInfo.UseShellExecute = true;
                                        proc.StartInfo.Verb = "runas";
                                        proc.StartInfo.Arguments = @$"/C ""call ""{batchFilename}""""";
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
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    // User cancelled UAC prompt, just go ahead like nothing happened.
                }
                finally
                {
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
