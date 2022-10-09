using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Leayal.PSO2.Modding.Cache;

namespace Leayal.PSO2.Modding
{
    /// <summary>Contains mod's infomation.</summary>
    public sealed class ModDefinition
    {
        private static readonly ReadOnlyDictionary<string, FileDefinition> EmptyFileList = new ReadOnlyDictionary<string, FileDefinition>(new Dictionary<string, FileDefinition>(0));

        /// <summary>The name of the modification package.</summary>
        public readonly string ModName;

        /// <summary>Is the mod in use or not.</summary>
        public bool IsEnabled { get; set; }

        /// <summary>Description of the modification package.</summary>
        public readonly string? Description;

        /// <summary>The directory path of the modification package.</summary>
        public readonly string ModDirectory;

        /// <summary>Gets the list of modded files.</summary>
        public readonly ReadOnlyDictionary<string, FileDefinition> Files;
        
        /// <summary>Constructor.</summary>
        /// <param name="mod_directory"></param>
        public ModDefinition(string mod_directory)
        {
            this.ModDirectory = (Path.IsPathFullyQualified(mod_directory) ? mod_directory : Path.GetFullPath(mod_directory));
            string? modname = null;
            var metadataJsonPath = Path.Combine(mod_directory, "metadata.json");
            using (var fs = File.OpenRead(metadataJsonPath))
            using (var metadataDocument = JsonDocument.Parse(fs))
            {
                var root = metadataDocument.RootElement;
                if (root.TryGetProperty("name", out var prop_name) && prop_name.ValueKind == JsonValueKind.String)
                {
                    modname = prop_name.GetString();
                }

                if (root.TryGetProperty("description", out var prop_description) && prop_description.ValueKind == JsonValueKind.String)
                {
                    this.Description = prop_description.GetString();
                }

                if (root.TryGetProperty("files", out var prop_files) && prop_files.ValueKind == JsonValueKind.Object)
                {
                    Dictionary<string, FileDefinition> list = ((root.TryGetProperty("file-count", out var prop_fileCount) && prop_fileCount.ValueKind == JsonValueKind.Number) ?
                        new Dictionary<string, FileDefinition>(prop_fileCount.GetInt32()) : new Dictionary<string, FileDefinition>());

                    foreach (var obj in prop_files.EnumerateObject())
                    {
                        var relativeFilepath = obj.Name;
                        var obj_val = obj.Value;
                        if (obj_val.ValueKind == JsonValueKind.Object)
                        {
                            string? targetPath, targetMd5 = null;
                            if (obj_val.TryGetProperty("target-path", out var prop_targetPath) && prop_targetPath.ValueKind == JsonValueKind.String)
                            {
                                targetPath = prop_targetPath.GetString();
                                if (string.IsNullOrEmpty(targetPath))
                                {
                                    targetPath = relativeFilepath;
                                }
                            }
                            else
                            {
                                targetPath = relativeFilepath;
                            }
                            if (obj_val.TryGetProperty("target-md5", out var prop_targetMd5) && prop_targetMd5.ValueKind == JsonValueKind.String)
                            {
                                targetMd5 = prop_targetMd5.GetString();
                                if (string.IsNullOrWhiteSpace(targetMd5))
                                {
                                    targetMd5 = null;
                                }
                            }
                            list.Add(relativeFilepath, new FileDefinition(targetPath, targetMd5));
                        }  
                    }
                    this.Files = new ReadOnlyDictionary<string, FileDefinition>(list);
                }
                else
                {
                    this.Files = EmptyFileList;
                }
            }

            if (string.IsNullOrEmpty(modname))
            {
                if (mod_directory.AsSpan().IndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) == -1)
                {
                    modname = mod_directory;
                }
                else
                {
                    modname = Path.GetFileName(mod_directory) ?? string.Empty;
                }
            }

            this.ModName = modname;
        }
    }
}
