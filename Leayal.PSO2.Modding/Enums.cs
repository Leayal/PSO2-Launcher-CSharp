namespace Leayal.PSO2.Modding
{
    /// <summary>FileSystem hint</summary>
    public enum FileSystemType
    {
        /// <summary>Unknown FileSystem.</summary>
        Unspecified,
        /// <summary>A directory contains modded files.</summary>
        Win32_LooseFile,
        /// <summary>A zip archive contains modded files.</summary>
        ZipArchive
    }
}
