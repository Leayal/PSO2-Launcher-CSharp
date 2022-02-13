using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public class ExAssemblyLoadContext : AssemblyLoadContext
    {
        private static readonly Assembly CurrentAsm = Assembly.GetExecutingAssembly();
        private readonly AssemblyDependencyResolver solver;
        private readonly bool loadWithoutLock;

        public ExAssemblyLoadContext(string entryPath) : this(entryPath, false) { }

        public ExAssemblyLoadContext(string entryPath, bool unloadable) : this(entryPath, unloadable, false) { }

        public ExAssemblyLoadContext(string entryPath, bool unloadable, bool loadWithoutLock) : base(null, unloadable)
        {
            this.loadWithoutLock = loadWithoutLock;
            this.solver = new AssemblyDependencyResolver(entryPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (AssemblyName.ReferenceMatchesDefinition(assemblyName, CurrentAsm.GetName())) return CurrentAsm;
            var path = this.solver.ResolveAssemblyToPath(assemblyName);
            if (path != null)
            {
                if (this.loadWithoutLock)
                {
                    var b = File.Exists(path) ? File.ReadAllBytes(path) : null;
                    if (b != null && b.Length != 0)
                    {
                        var m = new MemoryStream(b, false);
                        m.Position = 0;
                        return base.LoadFromStream(m);
                    }
                }
                else
                {
                    return base.LoadFromNativeImagePath(path, path);
                }
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = this.solver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (path != null)
            {
                return base.LoadUnmanagedDll(path);
            }
            return IntPtr.Zero;
        }
    }
}
