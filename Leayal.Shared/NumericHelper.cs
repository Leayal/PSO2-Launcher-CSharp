namespace Leayal.Shared
{
    public static class NumericHelper
    {
        public static long ToInt64(this bool value) => value switch { false => 0L, _ => 1L };

        public static int ToInt32(this bool value) => value switch { false => 0, _ => 1 };

        /// <summary>Convert a number to the human-readable file size string.</summary>
        /// <param name="i">The size in byte.</param>
        /// <returns>Returns the human-readable file size for an arbitrary, 64-bit file size </returns>
        /// <remarks>https://www.somacon.com/p576.php</remarks>
        public static string ToHumanReadableFileSize(in long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }
    }
}
