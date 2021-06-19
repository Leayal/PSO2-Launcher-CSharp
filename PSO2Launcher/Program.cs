using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;

namespace Leayal.PSO2Launcher
{
    static class Program
    {
        private static bool flag_reload;
        private static SingleAppController _appController;
        public static SingleAppController AppController => _appController;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            flag_reload = true;
            _appController = null;

            if (args != null && args.Length == 2 && string.Equals(args[0], "--restart-update", StringComparison.OrdinalIgnoreCase))
            {
                var memoryId = args[1];
                try
                {
                    if (EventWaitHandle.TryOpenExisting($"{memoryId}-wait", out var waithandle))
                    {
                        Communication.RestartObj<Communication.BootstrapUpdater.BootstrapUpdater_CheckForUpdates> data = null;
                        try
                        {
                            using (var mmf = MemoryMappedFile.OpenExisting(memoryId))
                            using (var dataStream = mmf.CreateViewStream())
                            {
                                var bytes = new byte[dataStream.Length];
                                dataStream.Read(bytes);
                                data = Communication.RestartObj<Communication.BootstrapUpdater.BootstrapUpdater_CheckForUpdates>.DeserializeJson(bytes);
                            }
                        }
                        catch (Exception)
                        {
                            data = null;
                        }
                        finally
                        {
                            waithandle.Dispose();
                        }
                        
                        if (data == null)
                        {
                            Environment.Exit(-1);
                        }
                        else
                        {
                            if (data.DataObj != null && data.DataObj.RestartMoveItems != null && data.DataObj.RestartMoveItems.Count != 0)
                            {
                                foreach (var item in data.DataObj.RestartMoveItems)
                                {
                                    File.Move(item.Value, item.Key, true);
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(data.ParentFilename))
                            {
                                // Replace files

                                using (var process = new Process())
                                {
                                    process.StartInfo.FileName = data.ParentFilename;
                                    if (data.ParentParams != null && data.ParentParams.Count != 0)
                                    {
                                        foreach (var arg in data.ParentParams)
                                        {
                                            process.StartInfo.ArgumentList.Add(arg);
                                        }
                                    }
                                    process.StartInfo.UseShellExecute = false;

                                    process.Start();
                                    process.WaitForExit(500);
                                }
                            }
                            Environment.Exit(0);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // Derp, what happen.
                }
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);

            while (flag_reload)
            {
                flag_reload = false;
                _appController = new SingleAppController();
                _appController.Run(args);
            }
        }

        public static void Reload()
        {
            if (_appController != null)
            {
                flag_reload = true;
                _appController.CloseMainForm();
            }
        }
    }

    class SingleAppController : WindowsFormsApplicationBase
    {
        public SingleAppController() : base(AuthenticationMode.Windows)
        {
            this.IsSingleInstance = true;
            this.EnableVisualStyles = true;
            this.ShutdownStyle = ShutdownMode.AfterMainFormCloses;
        }

        protected override void OnCreateMainForm()
        {
            this.MainForm = new Bootstrap();
        }

        public void CloseMainForm() => this.MainForm?.Close();
    }
}
