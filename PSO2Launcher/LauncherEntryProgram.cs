using System;
using System.Windows.Forms;
using Leayal.PSO2Launcher.Interfaces;
using Leayal.PSO2Launcher.Forms;

namespace Leayal.PSO2Launcher
{
    class LauncherEntryProgram : LauncherProgram
    {
        private const int WM_SYSCOMMAND = 0x0112, SC_RESTORE = 0xF120;

        private Bootstrap? mainForm;
        public LauncherEntryProgram() : base(true, false)
        {
            this.mainForm = null;
        }

        protected override int OnExit()
        {
            if (this.mainForm != null)
            {
                this.mainForm.Close();
                this.mainForm = null;
            }
            return 0;
        }

        protected override void OnFirstInstance(string[] args)
        {
            if (this.mainForm == null || this.mainForm is null || this.mainForm.IsDisposed)
            {
                this.mainForm = new Bootstrap();
            }
            this.OnInitialized();
            Application.Run(this.mainForm);
        }

        protected override void OnSubsequentInstance(string[] args)
        {
            if (this.mainForm is Bootstrap mainform && !mainform.IsDisposed)
            {
                if (mainform.WindowState == FormWindowState.Minimized)
                {
                    var a = Message.Create(mainform.Handle, WM_SYSCOMMAND, new IntPtr(SC_RESTORE), IntPtr.Zero);
                    mainform.SendNativeMessage(ref a);
                    // mainform.Show();
                }
                mainform.Activate();
            }
        }

        private static void MainWindow_FormClosed(object? sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            if (sender is Bootstrap form)
            {
                form.FormClosed -= MainWindow_FormClosed;
                form.Dispose();
            }
        }
    }
}
