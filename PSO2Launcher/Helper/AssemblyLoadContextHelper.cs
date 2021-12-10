using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Leayal.PSO2Launcher.Helper
{
    public static class AssemblyLoadContextHelper
    {
        public static Assembly FromFileWithNative(this AssemblyLoadContext context, string path)
        {
            try
            {
                return context.LoadFromNativeImagePath(path, path);
            }
            catch (FileLoadException)
            {
                return context.LoadFromAssemblyPath(path);
            }
            catch (BadImageFormatException)
            {
                return context.LoadFromAssemblyPath(path);
            }
        }
    }
}
