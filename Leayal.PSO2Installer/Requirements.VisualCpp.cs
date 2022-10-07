using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Leayal.PSO2.Installer
{
    public static partial class Requirements
    {
        // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64
        // HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\X64

        // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\DevDiv\VC\Servicing\14.0\RuntimeAdditional
        // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\DevDiv\VC\Servicing\14.0\RuntimeMinimum

        // HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Installer\Dependencies\Microsoft.VS.VC_RuntimeAdditionalVSU_amd64,v14
        // HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Installer\Dependencies\Microsoft.VS.VC_RuntimeMinimumVSU_amd64,v14

        public static VCRedistVersion GetVC14RedistVersion(bool x64)
        {
            using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, x64 ? RegistryView.Registry64 : RegistryView.Registry32))
            {
                return GetVC14RedistVersion(hive, x64);
            }
        }

        private static VCRedistVersion GetVC14RedistVersion(RegistryKey key, bool is_x64)
        {
            using (var subkey = key.OpenSubKey(Path.Combine("SOFTWARE", "Microsoft", "VisualStudio", "14.0", "VC", "Runtimes", is_x64 ? "X64" : "X86"), false))
            {
                if (subkey == null)
                {
                    return VCRedistVersion.None;
                }
                else
                {
                    var obj_major = subkey.GetValue("Major");
                    if (obj_major is int major && major == 14)
                    {
                        var obj_minor = subkey.GetValue("Minor");
                        if (obj_minor is int minor)
                        {
                            if (minor < 10)
                            {
                                return VCRedistVersion.VC2015;
                            }
                            else if (minor < 20)
                            {
                                // Expect 16 ~ 16
                                return VCRedistVersion.VC2017;
                            }
                            else if (minor < 30)
                            {
                                // Expect 20 ~ 29
                                return VCRedistVersion.VC2019;
                            }
                            else if (minor < 40)
                            {
                                // Expect 30~39
                                return VCRedistVersion.VC2022;
                            }
                            else
                            {
                                // 40+
                                return VCRedistVersion.NewerThanExpected;
                            }
                        }
                        else
                        {
							// It's unlikely possible to reach here. But handle it anyway.
                            return VCRedistVersion.VC2015;
                        }
                    }
                    else
                    {
                        return VCRedistVersion.None;
                    }
                }
            }
        }
    }
}
