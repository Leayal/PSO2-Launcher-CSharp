using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Leayal.PSO2Launcher.Helper
{
    public static class QuickFile
    {
        public static IEnumerable<string> OpenTextFileToReadByLine(string filename, Encoding encoding)
        {
            using (var sr = new StreamReader(filename, encoding))
                foreach (var line in EnumerableLines(sr))
                    yield return line;
        }

        public static IEnumerable<string> OpenTextFileToReadByLine(string filename)
        {
            using (var sr = new StreamReader(filename))
                foreach (var line in EnumerableLines(sr))
                    yield return line;
        }

        public static IEnumerable<string> OpenTextFileToReadByLine(string filename, bool detectingEncodingByteOrderMask)
        {
            using (var sr = new StreamReader(filename, detectingEncodingByteOrderMask))
                foreach (var line in EnumerableLines(sr))
                    yield return line;
        }

        public static IEnumerable<string> EnumerableLines(TextReader textReader)
        {
            var line = textReader.ReadLine();
            while (line != null)
            {
                yield return line;
                line = textReader.ReadLine();
            }
        }

        public static string ReadFirstLine(string filename, bool detectEncodingByteOrderMask)
        {
            using (var sr = new StreamReader(filename, detectEncodingByteOrderMask))
                return sr.ReadLine();
        }

        public static string ReadFirstLine(string filename)
        {
            using (var sr = new StreamReader(filename))
                return sr.ReadLine();
        }

        public static string ReadFirstLine(string filename, Encoding encoding)
        {
            using (var sr = new StreamReader(filename, encoding))
                return sr.ReadLine();
        }
    }
}
