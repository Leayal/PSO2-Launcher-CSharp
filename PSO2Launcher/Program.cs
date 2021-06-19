using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;

namespace Leayal.PSO2Launcher
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);
            
            var controller = new SingleAppController();
            controller.Run(args);
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
        }
    }
}
