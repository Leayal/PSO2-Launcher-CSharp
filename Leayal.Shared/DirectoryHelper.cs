using System;
using System.Collections.Generic;
using System.IO;

namespace Leayal.Shared
{
    public static class DirectoryHelper
    {
        public static bool IsDirectoryExistsAndNotEmpty(string path, bool includingFolder = false)
        {
            if (Directory.Exists(path))
            {
                IEnumerable<string> walk = includingFolder ? Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories) : Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
                using (var walker = walk.GetEnumerator())
                {
                    walk = null;
                    if (walker.MoveNext())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
