using System;
using System.Windows.Forms;
using Leayal.PSO2Launcher.Interfaces;
using Leayal.PSO2Launcher.Helper;
using Leayal.PSO2Launcher.Classes;
using System.IO;
using System.Reflection;

namespace Leayal.PSO2Launcher
{
    class LauncherEntryProgram : LauncherProgram
    {
        private ExAssemblyLoadContext? assemblycontext;
        private LauncherProgram? realprogram;

        public LauncherProgram? UnderlyingProgram => this.realprogram;

        public LauncherEntryProgram() : base(true, false)
        {
            this.assemblycontext = null;
        }

        protected override int OnExit() => this.realprogram?.Exit() ?? 0;

        protected override void OnFirstInstance(string[] args)
        {
            if (this.assemblycontext == null)
            {
                try
                {
                    var assemblyPath = Path.Combine(Program.RootDirectory, "bin", "BootstrapUpdater.dll");
                    this.assemblycontext = new ExAssemblyLoadContext(assemblyPath, true, true);
                    this.assemblycontext.SetProfileOptimizationRoot(Path.Combine(Program.RootDirectory, "data", "optimization"));
                    this.assemblycontext.StartProfileOptimization(null);
                    var assembly = this.assemblycontext.FromFileWithNative(assemblyPath);
                    if (assembly.CreateInstance("Leayal.PSO2Launcher.Updater.LauncherUpdaterProgram") is LauncherProgram realstuff)
                    {
                        this.realprogram = realstuff;
                        realstuff.Run(args);
                    }
                }
                finally
                {
                    this.assemblycontext?.Unload();
                }
            }
        }

        protected override void OnSubsequentInstance(string[] args)
        {
            this.realprogram?.Run(args);
        }
    }
}
