using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher
{
    class BootstrapUpdaterAssemblyLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver solver;
        public BootstrapUpdaterAssemblyLoadContext() : base("BootstrapUpdater", true) { }

        public Assembly Init(string entryPath)
        {
            this.solver = new AssemblyDependencyResolver(entryPath);
            Assembly result;
            using (var fs = File.OpenRead(entryPath))
            {
                result = this.LoadFromStream(fs);
            }

            foreach (var reference in result.GetReferencedAssemblies())
            {
                this.Load(new AssemblyName(reference.FullName));
            }

            return result;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var fullname = assemblyName.FullName;
            foreach (var asm in this.Assemblies)
            {
                if (string.Equals(asm.FullName, fullname, StringComparison.Ordinal))
                {
                    return asm;
                }
            }
            var path = this.solver.ResolveAssemblyToPath(assemblyName);
            if (path != null)
            {
                using (var fs = File.OpenRead(path))
                {
                    return base.LoadFromStream(fs);
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
