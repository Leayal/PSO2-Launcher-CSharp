using System.IO;

namespace Leayal.Shared
{
    public static class DirectoryHelper
    {
        public static bool IsDirectoryExistsAndNotEmpty(string path, bool includingFolder = false)
        {
            if (Directory.Exists(path))
            {
                return IsDirectoryNotEmpty(path);
            }
            return false;
        }

        public static bool IsDirectoryNotEmpty(string path, bool includingFolders = false)
        {
            using (var walker = (includingFolders ? Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories) : Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)).GetEnumerator())
            {
                if (walker.MoveNext())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
