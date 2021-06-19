using Leayal.PSO2Launcher.Communication.GameLauncher;
using System;

namespace Leayal.PSO2Launcher.Core
{
    public class GameLauncher : IWPFApp
    {
        private readonly App _app;

        public GameLauncher()
        {
            this._app = new App();
        }

        public void Run(string[] args)
        {
            this._app.Run();
        }
    }
}
