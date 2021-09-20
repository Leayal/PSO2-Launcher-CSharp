using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection.PortableExecutable;
using Leayal.Shared;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class CustomLibraryModMetadata
    {
        const string Text_Unknown = "<Unknown>", Text_NotGiven = "<None>", Text_Ignored = "<Ignored>";
        
        const string Text_Advice_RemoveIfNotKnown = "If you don't know what {library-mod-name} is or where it's from, please remove it to avoid crashes or bugs caused by this custom library file.";
        private static readonly string Text_Advice_UpdateToLatestPossible = "- If you are using {library-mod-name} and if {library-mod-name} isn't at latest version, please update {library-mod-name} to latest version available to your system to avoid crashes and bugs caused by this graphic mod." + Environment.NewLine + "- " + Text_Advice_RemoveIfNotKnown;

        private static readonly string Text_Advice_RemoveTranslationPatchIfNotUsing = "- If you are a plugin from PSO2 Tweaker's of Arks-Layer, please launch PSO2 Tweaker to update the plugin loader and other plugins to latest version (if it isn't latest yet) to avoid crashes or bugs caused by the plugin(s)."
            + Environment.NewLine + "- If you don't use any plugins coming from PSO2 Tweaker of Arks-Layer, please disable the Plugin Loader in Tweaker's 'Plugin Settings' or remove the file by clicking 'Remove'."
            + Environment.NewLine + "- If you don't know what this file is or where it's from, please remove it to avoid crashes or bugs caused by this patch.";

        const string Text_Advice_RemoveImmediately = "Please remove this file because it will likely cause problem due to wrong target CPU architecture. PSO2 client is 'AMD64' (or 'x64_x86') and it will crash if the game loads this file.";
        const string Text_Advice_RemoveVCRedistImmediately = "Please remove this file and install VC++ Redist properly by using the installation setup from Microsoft Download Center (or Microsoft Support Center) if you haven't installed the VC++ Redist yet.";
        const string Text_Advice_RemoveVCRedistImmediately_Untrusted = "Please remove this file because it's shimming/impersonate VC++ Redist file but it's not digital-trusted.";

        public string Filepath { get; }
        public string ProductName { get; }
        public string Summary { get; }
        public string FileVersion { get; }
        public string ProductVersion { get; }

        public string FileNameOnly => Path.GetFileName(Filepath);

        public string Advice { get; }

        public bool WrongCPUTarget { get; }
        public Machine TargetCPU { get; }

        public bool? IsDigitalSigned { get; }

        public CustomLibraryModMetadata(string filepath)
        {
            this.Filepath = filepath;

            using (var fs = File.OpenRead(filepath))
            {
                using (var pereader = new PEReader(fs, PEStreamOptions.LeaveOpen))
                {
                    this.TargetCPU = pereader.PEHeaders.CoffHeader.Machine;
                }
                if (this.TargetCPU == Machine.Amd64)
                {
                    fs.Position = 0;
                    try
                    {
                        this.IsDigitalSigned = DigitalSignatureHelper.IsSigned(fs);
                    }
                    catch
                    {
                        this.IsDigitalSigned = null;
                    }
                }
                else
                {
                    this.IsDigitalSigned = null;
                }
            }
            if (this.TargetCPU != Machine.Amd64)
            {
                this.WrongCPUTarget = true;
                this.ProductName = Text_Ignored;
                this.Summary = Text_Ignored;
                this.FileVersion = Text_Ignored;
                this.ProductVersion = Text_Ignored;

                this.Advice = Text_Advice_RemoveImmediately;
            }
            else
            {
                this.WrongCPUTarget = false;
                if (IsArksLayerItemTranslationPatch(this.Filepath))
                {
                    this.ProductName = "Item translation patch";
                    this.Summary = "PSO2 Tweaker's item translation patch from Arks-Layer";
                    this.FileVersion = Text_NotGiven;
                    this.ProductVersion = Text_NotGiven;

                    this.IsDigitalSigned = null;

                    this.Advice = Text_Advice_RemoveTranslationPatchIfNotUsing;
                }
                else
                {
                    var info = FileVersionInfo.GetVersionInfo(filepath);
                    this.ProductName = info.ProductName;
                    if (string.IsNullOrWhiteSpace(this.ProductName))
                    {
                        this.ProductName = Text_Unknown; // Path.GetFileNameWithoutExtension(this.ProductName);
                    }
                    this.Summary = info.FileDescription;
                    if (string.IsNullOrWhiteSpace(this.Summary))
                    {
                        this.Summary = Text_NotGiven;
                    }
                    this.FileVersion = info.FileVersion;
                    if (string.IsNullOrEmpty(this.FileVersion))
                    {
                        this.FileVersion = Text_Unknown;
                    }
                    this.ProductVersion = info.ProductVersion;
                    if (string.IsNullOrEmpty(this.ProductVersion))
                    {
                        this.ProductVersion = Text_Unknown;
                    }
                    info = null;

                    if (string.Equals(this.ProductName, "gshade", StringComparison.OrdinalIgnoreCase) || string.Equals(this.ProductName, "reshade", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Advice = Text_Advice_UpdateToLatestPossible.Replace("{library-mod-name}", this.ProductName);
                    }
                    else if (string.Equals(this.ProductName, "Microsoft® Visual Studio®", StringComparison.OrdinalIgnoreCase)
                        && this.Summary.StartsWith("Microsoft®", StringComparison.OrdinalIgnoreCase)
                        && this.Summary.Contains("Runtime", StringComparison.OrdinalIgnoreCase))
                    {
                        if (this.IsDigitalSigned == true)
                        {
                            this.Advice = Text_Advice_RemoveVCRedistImmediately;
                        }
                        else
                        {
                            this.Advice = Text_Advice_RemoveVCRedistImmediately_Untrusted;
                        }
                    }
                    else
                    {
                        this.Advice = Text_Advice_RemoveIfNotKnown.Replace("{library-mod-name}", "this");
                    }
                }
            }
        }

        // 00071E38

        // Hard-coded recognization for Arks-Layer's item translation patch (d3d11.dll)
        private static readonly BoyerMoore utf8bin_itemdatabasepath = new BoyerMoore(Encoding.UTF8.GetBytes(@".\patches\translation_items.bin")),
            utf8bin_noticeloaditemsuccess = new BoyerMoore(Encoding.UTF8.GetBytes("Item translations successfully loaded")),
            utf8bin_attachedtoprocess = new BoyerMoore(Encoding.UTF8.GetBytes("Attached to process")),
            utf8bin_skippingshiminitialization = new BoyerMoore(Encoding.UTF8.GetBytes("Skipping shim initialization")),
            utf8bin_shimd3d11 = new BoyerMoore(Encoding.UTF8.GetBytes("Shimmed d3d11")),
            utf8bin_shimdxgi = new BoyerMoore(Encoding.UTF8.GetBytes("Shimmed dxgi")),
            utf8bin_failedtoinitialized3d11shim = new BoyerMoore(Encoding.UTF8.GetBytes("Failed to initialize d3d11 shim")),
            utf8bin_failedtoinitializedxgishim = new BoyerMoore(Encoding.UTF8.GetBytes("Failed to initialize dxgi shim")),
            utf8bin_pso2classicdll = new BoyerMoore(Encoding.UTF8.GetBytes("pso2classic.dll")),
            utf8bin_pso2rebootdll = new BoyerMoore(Encoding.UTF8.GetBytes("pso2reboot.dll")),
            utf8bin_noticehookthreadending = new BoyerMoore(Encoding.UTF8.GetBytes("Main hook thread ending"));

        private const long WithinFileLength = 1024 * 1024; // 1MB

        public static bool IsArksLayerItemTranslationPatch(string filepath)
        {
            using (var fs = File.OpenRead(filepath))
            {
                if (fs.Length <= WithinFileLength)
                {
                    var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent((int)fs.Length);
                    try
                    {
                        var size = fs.Read(buffer, 0, buffer.Length);
                        var span = new ReadOnlySpan<byte>(buffer, 0, size);
                        int currentIndex = 0;
                        var currentSpan = span;

                        currentIndex = utf8bin_attachedtoprocess.Search(in currentSpan);
                        if (currentIndex == -1) return false;
                        currentSpan = currentSpan.Slice(currentIndex);
                        // currentIndex += utf8bin_attachedtoprocess.PatternLength;

                        currentIndex = utf8bin_failedtoinitialized3d11shim.Search(in currentSpan);
                        if (currentIndex == -1) return false;
                        currentSpan = currentSpan.Slice(currentIndex);
                        // currentIndex += utf8bin_failedtoinitialized3d11shim.PatternLength;

                        currentIndex = utf8bin_shimd3d11.Search(in currentSpan);
                        if (currentIndex == -1) return false;
                        currentSpan = currentSpan.Slice(currentIndex);
                        // currentIndex += utf8bin_shimd3d11.PatternLength;

                        currentIndex = utf8bin_failedtoinitializedxgishim.Search(in currentSpan);
                        if (currentIndex == -1) return false;
                        currentSpan = currentSpan.Slice(currentIndex);
                        // currentIndex += utf8bin_failedtoinitializedxgishim.PatternLength;

                        currentIndex = utf8bin_shimdxgi.Search(in currentSpan);
                        if (currentIndex == -1) return false;
                        currentSpan = currentSpan.Slice(currentIndex);
                        // currentIndex += utf8bin_shimdxgi.PatternLength;

                        currentIndex = utf8bin_skippingshiminitialization.Search(in currentSpan);
                        if (currentIndex == -1) return false;
                        currentSpan = currentSpan.Slice(currentIndex);
                        // currentIndex += utf8bin_skippingshiminitialization.PatternLength;

                        currentIndex = utf8bin_itemdatabasepath.Search(in currentSpan);
                        if (currentIndex == -1) return false;
                        currentSpan = currentSpan.Slice(currentIndex);
                        // currentIndex += utf8bin_itemdatabasepath.PatternLength;

                        currentIndex = utf8bin_noticeloaditemsuccess.Search(in currentSpan);
                        if (currentIndex == -1) return false;
                        currentSpan = currentSpan.Slice(currentIndex);
                        // currentIndex += utf8bin_noticeloaditemsuccess.PatternLength;

                        currentIndex = utf8bin_pso2classicdll.Search(in currentSpan);
                        if (currentIndex == -1) return false;

                        currentIndex = utf8bin_pso2rebootdll.Search(in currentSpan);
                        if (currentIndex == -1) return false;

                        currentIndex = utf8bin_noticehookthreadending.Search(in currentSpan);
                        if (currentIndex == -1) return false;

                        return true;
                    }
                    finally
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(buffer, true);
                    }
                }
            }
            return false;
        }
    }
}
