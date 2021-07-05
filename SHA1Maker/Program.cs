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

            using (var outputFS = File.Create(outputFile))
            using (var writer = new System.Text.Json.Utf8JsonWriter(outputFS))
            {
                writer.WriteStartObject();
                writer.WriteNumber("rep-version", 2);
                var baseUrl = new Uri(url);
                writer.WriteString("root-url", (new Uri(baseUrl, "files")).AbsoluteUri);
                writer.WriteString("root-url-critical", baseUrl.AbsoluteUri);
                var currentProbe = Path.Combine(dir, "files");
                if (Directory.Exists(currentProbe))
                {
                    writer.WriteStartObject("files");
                    foreach (var file in Directory.EnumerateFiles(currentProbe, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = file.Remove(0, currentProbe.Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                        using (var fs = File.OpenRead(file))
                        using (var sha1 = SHA1.Create())
                        {
                            writer.WriteStartObject(relativePath);
                            writer.WriteNumber("size", fs.Length);
                            var sha1hash = Convert.ToHexString(sha1.ComputeHash(fs));
                            writer.WriteString("sha1", sha1hash);
                            writer.WriteEndObject();
                        }
                    }
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WriteNull("files");
                }
                currentProbe = Path.Combine(dir, "critical-files");
                if (Directory.Exists(currentProbe))
                {
                    writer.WriteStartObject("critical-files");
                    string entryFilePath = Path.Combine(dir, "PSO2LeaLauncher.dll");
                    if (File.Exists(entryFilePath))
                    {
                        var relativePath = entryFilePath.Remove(0, dir.Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                        using (var fs = File.OpenRead(entryFilePath))
                        using (var sha1 = SHA1.Create())
                        {
                            writer.WriteStartObject(relativePath);
                            writer.WriteNumber("size", fs.Length);
                            writer.WriteBoolean("entry", true);
                            var sha1hash = Convert.ToHexString(sha1.ComputeHash(fs));
                            writer.WriteString("sha1", sha1hash);
                            writer.WriteEndObject();
                        }
                    }
                    foreach (var file in Directory.EnumerateFiles(currentProbe, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = file.Remove(0, dir.Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                        using (var fs = File.OpenRead(file))
                        using (var sha1 = SHA1.Create())
                        {
                            writer.WriteStartObject(relativePath);
                            writer.WriteNumber("size", fs.Length);
                            var sha1hash = Convert.ToHexString(sha1.ComputeHash(fs));
                            writer.WriteString("sha1", sha1hash);
                            writer.WriteEndObject();
                        }
                    }
                    writer.WriteEndObject();
                }
                else
                {
                    string entryFilePath = Path.Combine(dir, "PSO2LeaLauncher.dll");
                    if (File.Exists(entryFilePath))
                    {
                        writer.WriteStartObject("critical-files");
                        var relativePath = entryFilePath.Remove(0, dir.Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                        using (var fs = File.OpenRead(entryFilePath))
                        using (var sha1 = SHA1.Create())
                        {
                            writer.WriteStartObject(relativePath);
                            writer.WriteNumber("size", fs.Length);
                            writer.WriteBoolean("entry", true);
                            var sha1hash = Convert.ToHexString(sha1.ComputeHash(fs));
                            writer.WriteString("sha1", sha1hash);
                            writer.WriteEndObject();
                        }
                        writer.WriteEndObject();
                    }
                    else
                    {
                        writer.WriteNull("critical-files");
                    }
                }
                writer.WriteEndObject();
                writer.Flush();
            }
            Console.WriteLine("Done");
        }
    }
}
