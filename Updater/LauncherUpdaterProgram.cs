using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Windows.Forms;
using Leayal.PSO2Launcher.Helper;
using Leayal.PSO2Launcher.Interfaces;
using Leayal.PSO2Launcher.Updater.Forms;
using Leayal.SharedInterfaces.Communication;

namespace Leayal.PSO2Launcher.Updater
{
    public sealed class LauncherUpdaterProgram : LauncherProgram
    {
        private const int WM_SYSCOMMAND = 0x0112, SC_RESTORE = 0xF120;

        private readonly ApplicationContext bootstrapContext;

        public LauncherUpdaterProgram() : base()
        {
            this.bootstrapContext = new ApplicationContext(new Bootstrap());
        }

        protected override int OnExit()
        {
            if (this.bootstrapContext.MainForm is Form form)
            {
                form.Close();
            }
            // this.bootstrapContext.ExitThread();
            return 0;
        }

        protected override void OnFirstInstance(string[] args)
        {
            this.OnInitialized();
            bool canContinueCheckingUpdate = true;
            foreach (var arg in args)
            {
                if (string.Equals(arg, "--no-self-update", StringComparison.OrdinalIgnoreCase))
                {
                    canContinueCheckingUpdate = false;
                }
            }
            try
            {
                if (canContinueCheckingUpdate)
                {
                    Application.Run(this.bootstrapContext);
                }
                else
                {
                    var rootDirectory = LauncherController.RootDirectory;
                    var asmPath = Path.GetFullPath(Path.Combine("bin", "LauncherCoreNew.dll"), rootDirectory);
                    object? launcherObj = File.Exists(asmPath) ?
                        (Program.TryLoadLauncherAssembly(asmPath) ?? Program.TryLoadLauncherAssembly(Path.GetFullPath(Path.Combine("bin", "LauncherCore.dll"), rootDirectory)))
                        : Program.TryLoadLauncherAssembly(Path.GetFullPath(Path.Combine("bin", "LauncherCore.dll"), rootDirectory));
                    if (launcherObj is not null)
                    {
                        if (launcherObj is ILauncherProgram class_gameLauncherNew)
                        {
                            LauncherController.Current.SwitchProgram(class_gameLauncherNew);
                        }
                        else if (launcherObj is IWPFApp class_gameLauncher)
                        {
                            // Support old models in hackish way.
                            LauncherController.Current.SwitchProgram(new Classes.BackwardCompatibility.OldPSO2LauncherIWPFApp(class_gameLauncher));
                        }
                    }
                }
            }
            finally
            {
                this.bootstrapContext.Dispose();
            }
        }

        protected override void OnSubsequentInstance(string[] args)
        {
            if (this.bootstrapContext is ApplicationContext context && context.MainForm is Bootstrap bootstrap && !bootstrap.IsDisposed)
            {
                bootstrap.BeginInvoke(this.BringMainWindowForeground);
            }
        }

        private void BringMainWindowForeground()
        {
            if (this.bootstrapContext is ApplicationContext context && context.MainForm is Bootstrap bootstrap && !bootstrap.IsDisposed)
            {
                if (bootstrap.WindowState == FormWindowState.Minimized)
                {
                    var a = Message.Create(bootstrap.Handle, WM_SYSCOMMAND, new IntPtr(SC_RESTORE), IntPtr.Zero);
                    bootstrap.SendNativeMessage(ref a);
                    // mainform.Show();
                }
                bootstrap.Activate();
            }
        }
    }
}
