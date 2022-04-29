using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    /// <summary>File checking/scanning profiles (or presets) for <see cref="GameClientUpdater"/>.</summary>
    [Flags]
    public enum FileScanFlags
    {
        /// <summary>Full scan?</summary>
        /// <remarks>When used for PSO2Classic's profile. This value means using the same value from PSO2 Reboot.</remarks>
        None = 0,

        /// <summary>Only missing files are deemed to be in need of download.</summary>
        MissingFilesOnly = 1 << 0,

        /// <summary>Only local files, which have mismatched file size, are deemed to be in need of download.</summary>
        /// <remarks>
        /// <para>This also includes <see cref="MissingFilesOnly"/>.</para>
        /// <para>This take precede over <see cref="MD5HashMismatch"/>. In case both flags are used together, <b>only files with matched size will be hash-compare.</b></para>
        /// </remarks>
        FileSizeMismatch = 1 << 1,

        /// <summary>Only local files, which have mismatched MD5 hash, are deemed to be in need of download.</summary>
        /// <remarks>
        /// <para>This also includes <see cref="MissingFilesOnly"/>.</para>
        /// <para>In case this flag is used together with <see cref="FileSizeMismatch"/>, <b>only files with matched size will be hash-compare.</b></para>
        /// </remarks>
        MD5HashMismatch = 1 << 2,

        /// <summary>Bypass the cached hashes and perform a hash compute from local file regardless.</summary>
        /// <remarks>
        /// <para>This will ignore the existing records in the cache and generate anew.</para>
        /// <para>This flag will take precede over <seealso cref="CacheOnly"/>.</para>
        /// </remarks>
        ForceRefreshCache = 1 << 3,

        [EnumDisplayName("Cache only [Not recommended: Very unreliable] (Only use the info from the cache to check. Ignore all physical file checks on disk)")]
        /// <summary>Only use the data info from the hash cache.</summary>
        /// <remarks>
        /// <para>Very fast, however this will <b>not</b> do sanity check whether the actual file is existed or matched the written state in the cache.</para>
        /// <para>This flag cannot be used together with <seealso cref="ForceRefreshCache"/>. If you do, <seealso cref="ForceRefreshCache"/> takes precede.</para>
        /// </remarks>
        CacheOnly = 1 << 4,

        // Preset below

        [EnumDisplayName("Prefer speed [Not recommended: Unreliable] (Check missing and compare the cached MD5-hash if the file is already existed)")]
        /// <summary>This should be very fast. But unreliable.</summary>
        /// <remarks>Should only be used for advanced users. Or those who know they're having missing files only.</remarks>
        FastCheck = MissingFilesOnly | MD5HashMismatch,

        [EnumDisplayName("Balanced [Recommended] (Check missing files, incorrect size, and compare file hash in case size is correct)")]
        /// <summary>This should be fast. But somewhat unreliable. However, should be good enough.</summary>
        Balanced = FileSizeMismatch | MD5HashMismatch,

        [EnumDisplayName("Prefer accurate [Very slow but extremely reliable] (Check missing files, and compare file hash regardless)")]
        /// <summary>This should be very slowest. But reliable. As all files are going to be hash-compared regardless.</summary>
        HighAccuracy = MD5HashMismatch | ForceRefreshCache
    }

    public enum GameClientSelection
    {
        [EnumDisplayName("Download NGS files which are needed for NGS prologue only")]
        /// <summary>Download NGS files which are needed for NGS prologue only.</summary>
        /// <remarks>While playing prologue mode, which is singleplay, the game client will download the rest by itself.</remarks>
        NGS_Prologue_Only,

        [EnumDisplayName("Download all NGS files which are necessary to play the NGS game")]
        /// <summary>Download all NGS files which are necessary to play the NGS game.</summary>
        /// <remarks>You won't be able to switch to PSO2 classic.</remarks>
        NGS_Only,

        [EnumDisplayName("Download all NGS and Classic files which are necessary to play both")]
        /// <summary>Download all NGS and Classic files which are necessary to play both.</summary>
        NGS_AND_CLASSIC,

        [EnumVisibleInOption(false)]
        /// <summary>Internal use only. Check the files which should always be checked before starting game.</summary>
        Always_Only,
        
        [EnumVisibleInOption(false)]
        /// <summary>Internal use only. Check the classic files only.</summary>
        Classic_Only,

        [EnumVisibleInOption(false)]
        /// <summary>Internal use only. Follow the settings.</summary>
        Auto
    }
}
