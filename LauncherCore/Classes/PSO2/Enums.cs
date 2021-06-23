using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    /// <summary>File checking/scanning profiles (or presets) for <see cref="GameClientUpdater"/>.</summary>
    [Flags]
    enum FileScanFlags
    {
        /// <summary>Full scan?</summary>
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
        /// </remarks>
        ForceRefreshCache = 1 << 3,

        // Preset below

        /// <summary>This should be very fast. But unreliable.</summary>
        /// <remarks>Should only be used for advanced users. Or those who know they're having missing files only.</remarks>
        FastCheck = MissingFilesOnly | FileSizeMismatch,

        /// <summary>This should be fast. But somewhat unreliable. However, should be good enough.</summary>
        Balanced = FileSizeMismatch | MD5HashMismatch,

        /// <summary>This should be very slowest. But reliable. As all files are going to be hash-compared regardless.</summary>
        HighAccuracy = MD5HashMismatch | ForceRefreshCache
    }

    enum GameClientSelection
    {
        /// <summary>Download NGS files which are needed for NGS prologue only.</summary>
        /// <remarks>While playing prologue mode, which is singleplay, the game client will download the rest by itself.</remarks>
        NGS_Prologue_Only,

        /// <summary>Download all NGS files which are necessary to play the game.</summary>
        /// <remarks>You won't be able to switch to PSO2 classic.</remarks>
        NGS_Only,

        /// <summary>Download all NGS files and Classic files which are necessary to play both.</summary>
        NGS_AND_CLASSIC
    }
}
