using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Leayal.PSO2.Modding.Cache;
using System.Reflection.Metadata;

namespace Leayal.PSO2.Modding
{
    /// <summary>Contains mod's infomation.</summary>
    public sealed class ModDefinition
    {
        private static readonly ReadOnlyDictionary<string, IndividualFileDefinition> EmptyFileList = new ReadOnlyDictionary<string, IndividualFileDefinition>(new Dictionary<string, IndividualFileDefinition>(0));

        /// <summary>The name of the modification package.</summary>
        public readonly string ModName;

        /// <summary>Is the mod in use or not.</summary>
        public bool IsEnabled { get; set; }

        /// <summary>Description of the modification package.</summary>
        public readonly string? Description;

        /// <summary>The directory path of the modification package.</summary>
        public readonly string ModDirectory;

        /// <summary>Gets the list of modded files.</summary>
        public readonly ReadOnlyDictionary<string, IndividualFileDefinition> Files;
        
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
                    Dictionary<string, IndividualFileDefinition> list = ((root.TryGetProperty("file-count", out var prop_fileCount) && prop_fileCount.ValueKind == JsonValueKind.Number) ?
                        new Dictionary<string, IndividualFileDefinition>(prop_fileCount.GetInt32()) : new Dictionary<string, IndividualFileDefinition>());

                    foreach (var obj in prop_files.EnumerateObject())
                    {
                        var relativeFilepathInModArchive = obj.Name;
                        var obj_val = obj.Value;
                        if (IndividualFileDefinition.TryParse(relativeFilepathInModArchive, in obj_val, out var fileDef))
                        {
                            list.Add(relativeFilepathInModArchive, fileDef);
                        }  
                    }
                    this.Files = new ReadOnlyDictionary<string, IndividualFileDefinition>(list);
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
