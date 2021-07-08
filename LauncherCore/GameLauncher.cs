using Leayal.SharedInterfaces;
using Leayal.SharedInterfaces.Communication;
using System;
using System.Diagnostics;
using System.IO;

namespace Leayal.PSO2Launcher.Core
{
    public class GameLauncher : IWPFApp
    {
        private readonly App _app;

        public GameLauncher()
        {
            // var adminClient = new Leayal.PSO2Launcher.AdminProcess.AdminClient();
            // this.isLightMode = false;
            this._app = new App();
        }

        public void Run(string[] args)
        {
            if (Debugger.IsAttached)
            {
                this._app.Run();
            }
            else
            {
                try
                {
                    this._app.Run();
                }
                catch (Exception ex)
                {
                    using (var sw = new StreamWriter(Path.Combine(RuntimeValues.RootDirectory, "unhandled_error_wpf.txt"), true, System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine();
                        sw.WriteLine();
                        sw.WriteLine(ex.ToString());
                        sw.Flush();
                    }
                }
            }
        }


    }
}
