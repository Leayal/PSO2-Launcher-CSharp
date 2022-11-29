using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text.Json;

namespace Leayal.PSO2.Modding
{
    /// <summary>Contains informations about the file in a mod.</summary>
    public class IndividualFileDefinition
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

        /// <summary>A list of files to be repacked into ICE archive (which will be read by the game) if <seealso cref="IsRepack"/> is <see langword="true"/></summary>
        /// <remarks>If this field is <see langword="null"/>, same meaning as <seealso cref="IsRepack"/> be <see langword="false"/>.</remarks>
        public readonly string[]? FilesToRepack;

        /// <summary>Whether this mod file is a repack from many files.</summary>
        /// <remarks><seealso cref="FilesToRepack"/>contains the lists of files to be repacked.</remarks>
        public readonly bool IsRepack;

        /// <summary>Constructor.</summary>
        internal IndividualFileDefinition(string filenameInGame, string? targetFileMD5) : this(filenameInGame, targetFileMD5, null, false) { }

        /// <summary>Constructor.</summary>
        internal IndividualFileDefinition(string filenameInGame, string? targetFileMD5, string[]? filesToRepack, bool isRepack)
        {
            this.FilenameInGame = filenameInGame;
            // this.FilenameInMod = filenameInMod;
            this.TargetFileMD5 = targetFileMD5;
            this.FilesToRepack = filesToRepack;
            this.IsRepack = isRepack;
        }

        internal static bool TryParse(string relativeFilepathInModArchive, in JsonElement jsondata, [NotNullWhen(true)] out IndividualFileDefinition? obj)
        {
            if (jsondata.ValueKind == JsonValueKind.String)
            {
                obj = new IndividualFileDefinition(jsondata.GetString() ?? relativeFilepathInModArchive, null);
                return true;
            }
            else if (jsondata.ValueKind == JsonValueKind.Array)
            {
                var arraylength = jsondata.GetArrayLength();

                foreach (var arrayItem in jsondata.EnumerateArray())
                {

                }
                obj = new IndividualFileDefinition(relativeFilepathInModArchive, null, null, true);
                return true;
            }
            else if (jsondata.ValueKind == JsonValueKind.Object)
            {
                string? targetPath, targetMd5 = null;
                if (jsondata.TryGetProperty("target-path", out var prop_targetPath) && prop_targetPath.ValueKind == JsonValueKind.String)
                {
                    targetPath = prop_targetPath.GetString();
                    if (string.IsNullOrEmpty(targetPath))
                    {
                        targetPath = relativeFilepathInModArchive;
                    }
                }
                else
                {
                    targetPath = relativeFilepathInModArchive;
                }
                if (jsondata.TryGetProperty("target-md5", out var prop_targetMd5) && prop_targetMd5.ValueKind == JsonValueKind.String)
                {
                    targetMd5 = prop_targetMd5.GetString();
                    if (string.IsNullOrWhiteSpace(targetMd5))
                    {
                        targetMd5 = null;
                    }
                }
                obj = new IndividualFileDefinition(targetPath, targetMd5);
                return true;
            }

            obj = null;
            return false;
        }
    }
}
