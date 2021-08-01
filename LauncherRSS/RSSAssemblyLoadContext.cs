using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Leayal.SharedInterfaces;

namespace Leayal.PSO2Launcher.RSS
{
    class RSSAssemblyLoadContext : AssemblyLoadContext
    {
        private static readonly AssemblyLoadContext defaultone = AssemblyLoadContext.Default;
        private static readonly Assembly currentAsm = Assembly.GetExecutingAssembly();
        private readonly AssemblyDependencyResolver resolver;

        public RSSAssemblyLoadContext(bool unloadable) : base(null, unloadable)
        {
            string path = currentAsm.Location;
            if (string.IsNullOrEmpty(path))
            {
                path = RuntimeValues.RootDirectory;
            }
            this.resolver = new AssemblyDependencyResolver(path);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (AssemblyName.ReferenceMatchesDefinition(currentAsm.GetName(), assemblyName))
            {
                return currentAsm;
            }
            foreach (var asm in defaultone.Assemblies)
            {
                if (AssemblyName.ReferenceMatchesDefinition(asm.GetName(), assemblyName))
                {
                    return asm;
                }
            }
            var path = this.resolver.ResolveAssemblyToPath(assemblyName);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            else
            {
                return this.LoadFromAssemblyPath(path);
            }
        }

        

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = this.resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (string.IsNullOrEmpty(path))
            {
                return IntPtr.Zero;
            }
            else
            {
                return this.LoadUnmanagedDllFromPath(path);
            }
        }
    }
}
