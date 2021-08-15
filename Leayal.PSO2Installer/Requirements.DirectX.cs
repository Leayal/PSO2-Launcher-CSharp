using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.CompilerServices;

namespace Leayal.PSO2.Installer
{
    public static partial class Requirements
    {
        // HRESULT WINAPI D3DX11CheckVersion(UINT D3DSdkVersion, UINT D3DX11SdkVersion);
        // D3DX11_SDK_VERSION 43
        // D3D11_SDK_VERSION = 7
        // D3D_SDK_VERSION 32

        /// <summary>Check if DirectX11 (From June 2010) runtime is installed.</summary>
        public static bool HasDirectX11()
        {
            var dllpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "d3dx11_43.dll");
            if (File.Exists(dllpath) && NativeLibrary.TryLoad(dllpath, out var libraryHandle))
            {
                try
                {
                    if (NativeLibrary.TryGetExport(libraryHandle, "D3DX11CheckVersion", out var hMethod))
                    {
                        var method = Marshal.GetDelegateForFunctionPointer<D3DX11CheckVersion>(hMethod);
                        var hresult = method.Invoke(7, 43);
                        return (hresult == 0);
                    }
                }
                finally
                {
                    NativeLibrary.Free(libraryHandle);
                }
            }
            return false;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate uint D3DX11CheckVersion(uint D3D11_SDK_VERSION, uint D3DX11_SDK_VERSION);
    }
}
