using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Leayal.PSO2Launcher.Classes
{
    class BootstrapUpdaterAssemblyLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver? solver;
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

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var fullname = assemblyName.FullName;
            foreach (var asm in this.Assemblies)
            {
                if (string.Equals(asm.FullName, fullname, StringComparison.Ordinal))
                {
                    return asm;
                }
            }

            if (this.solver is not null)
            {
                var path = this.solver.ResolveAssemblyToPath(assemblyName);
                if (path != null)
                {
                    using (var fs = File.OpenRead(path))
                    {
                        return base.LoadFromStream(fs);
                    }
                }
            }
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            if (this.solver is not null)
            {
                var path = this.solver.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (path != null)
                {
                    return base.LoadUnmanagedDll(path);
                }
            }
            return IntPtr.Zero;
        }
    }
}
