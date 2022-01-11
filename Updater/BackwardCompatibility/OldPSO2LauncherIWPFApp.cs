using System;
using System.Windows.Forms;
using Leayal.PSO2Launcher.Interfaces;
using Leayal.SharedInterfaces.Communication;

namespace Leayal.PSO2Launcher.Classes.BackwardCompatibility
{
    public class OldPSO2LauncherIWPFApp : LauncherProgram
    {
        private readonly IWPFApp app;

        public OldPSO2LauncherIWPFApp(IWPFApp app) : base(true, true) 
        {
            this.app = app;
        }

        protected override int OnExit()
        {
            Application.Exit();
            return 0;
        }

        protected override void OnFirstInstance(string[] args)
        {
            this.app.Run(args);
        }

        protected override void OnSubsequentInstance(string[] args)
        { }
    }
}
