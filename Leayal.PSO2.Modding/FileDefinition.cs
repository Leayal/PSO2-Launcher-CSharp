using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2.Modding
{
    /// <summary>Contains informations about the file in a mod.</summary>
    public readonly struct FileDefinition
    {
        // <summary>The relative filename in the mod package.</summary>
        // public readonly string FilenameInMod;

        /// <summary>The relative filename in the game directory.</summary>
        /// <remarks>Starts from folder containing pso2.exe file, aka "pso2_bin" directory.</remarks>
        public readonly string FilenameInGame;

        /// <summary>The MD5 checksum of the targeting file.</summary>
        /// <remarks>
        /// <para>This is to check whether the target file is as expected. Which ensures the mod's compatibility.</para>
        /// <para>In case it's an empty string or <see langword="null"/>, the checker will skip this compatibility check for the file <b>and unconditionally allow using the mod</b>.</para>
        /// <para>The string should be all in UPPERCASE.</para>
        /// </remarks>
        public readonly string? TargetFileMD5;

        /// <summary>Constructor.</summary>
        internal FileDefinition(string filenameInGame, string? targetFileMD5)
        {
            this.FilenameInGame = filenameInGame;
            // this.FilenameInMod = filenameInMod;
            this.TargetFileMD5 = targetFileMD5;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            var hashcode = new HashCode();
            // hashcode.Add(this.FilenameInMod, StringComparer.OrdinalIgnoreCase);
            hashcode.Add(this.FilenameInGame, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(this.TargetFileMD5))
            {
                hashcode.Add(this.TargetFileMD5, StringComparer.OrdinalIgnoreCase);
            }
            return hashcode.ToHashCode();
        }

        /// <inheritdoc/>
        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is FileDefinition filedef)
            {
                return this.Equals(in filedef);
            }
            return false;
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The <see cref="FileDefinition"/> to compare with the current one.</param>
        /// <returns><see langword="true" /> if the specified <see cref="FileDefinition"/> is equal to the current one; otherwise, <see langword="false" />.</returns>
        public readonly bool Equals([NotNullWhen(true)] in FileDefinition obj) => (this.GetHashCode() == obj.GetHashCode());
    }
}
