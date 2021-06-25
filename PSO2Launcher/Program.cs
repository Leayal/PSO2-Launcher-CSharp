using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;
using System.Reflection;
using Leayal.SharedInterfaces.Communication;
using System.ComponentModel;

namespace Leayal.PSO2Launcher
{
    static class Program
    {
        private static bool flag_reload, flag_switchtoWPF;
        private static SingleAppController _appController;
        public static SingleAppController AppController => _appController;
        private static IWPFApp wpfController;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            flag_reload = true;
            flag_switchtoWPF = false;
            _appController = null;

            if (args != null && args.Length == 2)
            {
                if (string.Equals(args[0], "--launch-elevated", StringComparison.OrdinalIgnoreCase))
                {
                    var memoryId = args[1];
                    try
                    {
                        RestartObj<BootstrapElevation> data = null;
                        try
                        {
                            using (var mmf = MemoryMappedFile.OpenExisting(memoryId))
                            using (var dataStream = mmf.CreateViewStream())
                            {
                                var bytes = new byte[dataStream.Length];
                                dataStream.Read(bytes);
                                data = RestartObj<BootstrapElevation>.DeserializeJson(bytes);
                            }
                        }
                        catch
                        {
                            data = null;
                            // data = RestartObj<BootstrapElevation>.DeserializeJson(File.ReadAllBytes(@"E:\All Content\VB_Project\visual studio 2019\PSO2-Launcher-CSharp\Test\testelevation.json"));
                        }

                        if (data == null)
                        {
                            Environment.Exit(-1);
                        }
                        else
                        {
                            using (var process = new Process())
                            {
                                process.StartInfo.FileName = data.DataObj.Filename;
                                if (!string.IsNullOrEmpty(data.DataObj.WorkingDirectory))
                                {
                                    process.StartInfo.WorkingDirectory = data.DataObj.WorkingDirectory;
                                }

                                if (!string.IsNullOrEmpty(data.DataObj.Arguments))
                                {
                                    process.StartInfo.Arguments = data.DataObj.Arguments;
                                }

                                if (data.DataObj.ArgumentList.Count != 0)
                                {
                                    foreach (var item in data.DataObj.ArgumentList)
                                    {
                                        process.StartInfo.ArgumentList.Add(item);
                                    }
                                }

                                if (data.DataObj.EnvironmentVars.Count != 0)
                                {
                                    foreach (var item in data.DataObj.EnvironmentVars)
                                    {
                                        if (process.StartInfo.EnvironmentVariables.ContainsKey(item.Key))
                                        {
                                            process.StartInfo.EnvironmentVariables[item.Key] = item.Value;
                                        }
                                        else
                                        {
                                            process.StartInfo.EnvironmentVariables.Add(item.Key, item.Value);
                                        }
                                    }
                                }
                                process.StartInfo.Verb = "runas";
                                process.StartInfo.UseShellExecute = false;

                                process.Start();

                                if (data.DataObj.LingerTime != 0)
                                {
                                    process.WaitForExit(data.DataObj.LingerTime);
                                }
                                // Require shell =false
                            }
                            Environment.Exit(0);
                        }
                    }
                    catch (Win32Exception ex)
                    {
                        Environment.Exit(ex.NativeErrorCode);
                    }
                    catch (FileNotFoundException)
                    {
                        // Derp, what happen.
                    }
                }
                else if (string.Equals(args[0], "--restart-update", StringComparison.OrdinalIgnoreCase))
                {
                    var memoryId = args[1];
                    try
                    {
                        if (EventWaitHandle.TryOpenExisting($"{memoryId}-wait", out var waithandle))
                        {
                            RestartObj<BootstrapUpdater_CheckForUpdates> data = null;
                            try
                            {
                                using (var mmf = MemoryMappedFile.OpenExisting(memoryId))
                                using (var dataStream = mmf.CreateViewStream())
                                {
                                    var bytes = new byte[dataStream.Length];
                                    dataStream.Read(bytes);
                                    data = RestartObj<BootstrapUpdater_CheckForUpdates>.DeserializeJson(bytes);
                                }
                            }
                            catch
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
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                while (flag_reload)
                {
                    flag_reload = false;

                    if (flag_switchtoWPF && wpfController != null)
                    {
                        wpfController.Run(args);
                    }
                    else
                    {
                        _appController = new SingleAppController();
                        _appController.Run(args);
                    }
                }
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
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

        public static void SwitchToWPF(IWPFApp applicationBase)
        {
            flag_switchtoWPF = true;
            flag_reload = true;
            wpfController = applicationBase;
            _appController?.CloseMainForm();
        }

        // private static readonly ConcurrentDictionary<string, Assembly> 
        
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // I start to wonder why I'm using this old way....
            // So load context doesn't do my stuff well?
            // Definitely I used it wrongly or something.


            // Extending probing path: \bin\*;
            var filename = GetFilenameFromAssemblyFullname(args.Name);
            
            if (!filename.EndsWith(".resources", StringComparison.Ordinal))
            {
                var filepath = Path.GetFullPath(Path.Combine("bin", filename + ".dll"), AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
                if (File.Exists(filepath))
                {
                    return Assembly.LoadFrom(filepath);
                }
            }
            return null;
        }

        private static string GetFilenameFromAssemblyFullname(string fullname)
        {
            var index = fullname.IndexOf(',');
            if (index == -1)
            {
                return fullname;
            }
            else
            {
                return fullname.Substring(0, index);
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
