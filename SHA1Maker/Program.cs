using System;
using System.IO;
using System.Security.Cryptography;

namespace SHA1Maker
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir, outputFile, url;
            if (args.Length != 3)
            {
                Console.WriteLine("[0] Directory");
                Console.WriteLine("[1] Output");
                Console.WriteLine("[2] Root URL");
                return;
            }
            else
            {
                dir = args[0];
                outputFile = args[1];
                url = args[2];
            }
            if (Directory.Exists(dir))
            {
                using (var outputFS = File.Create(outputFile))
                using (var writer = new System.Text.Json.Utf8JsonWriter(outputFS))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("rep-version", 1);
                    writer.WriteString("root-url", url);
                    writer.WriteStartObject("files");
                    foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = file.Remove(0, dir.Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        using (var fs = File.OpenRead(file))
                        using (var sha1 = SHA1.Create())
                        {
                            writer.WriteStartObject(relativePath);
                            var sha1hash = Convert.ToHexString(sha1.ComputeHash(fs));
                            writer.WriteString("sha1", sha1hash);
                            writer.WriteEndObject();
                        }
                    }
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.Flush();
                }
            }
            else
            {
                using (var outputFS = File.Create(outputFile))
                using (var writer = new System.Text.Json.Utf8JsonWriter(outputFS))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("rep-version", 1);
                    writer.WriteString("root-url", url);
                    writer.WriteNull("files");
                    writer.WriteEndObject();
                    writer.Flush();
                }
            }
            Console.WriteLine("Done");
        }
    }
}
