using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Communication.GameLauncher
{
    public interface IWPFApp
    {
        void Run(string[] args);
    }

    class GameLauncherLoadContext : AssemblyLoadContext
    {
        private readonly string _entryDll;
        private IWPFApp _app;
        private AssemblyDependencyResolver _resolver;

        public GameLauncherLoadContext(string dllpath) : base("LauncherCore", false)
        {
            this._entryDll = dllpath;
            this._resolver = new AssemblyDependencyResolver(dllpath);

            // this.Resolving += GameLauncherLoadContext_Resolving;
            // Why is this AssemblyDependencyResolver not used in the LoadContext by default?

            // ControlzEx loaded, but MahApps doesn't
            // Do I have to load it manually by myself?
        }

        public IWPFApp EntryPoint
        {
            get
            {
                if (this._app == null)
                {
                    var asm = this.LoadFromAssemblyPath(this._entryDll);
                    this._app = (IWPFApp)asm.CreateInstance("Leayal.PSO2Launcher.Core.GameLauncher");
                }
                return this._app;
            }
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = this._resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = this._resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }

        private static Assembly GameLauncherLoadContext_Resolving(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            if (arg1 is GameLauncherLoadContext gamelauncherLoadContext)
            {
                return gamelauncherLoadContext.Load(arg2);
            }
            else
            {
                return arg1.LoadFromAssemblyName(arg2);
            }
        }
    }
}
