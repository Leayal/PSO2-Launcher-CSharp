using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public class CommandElevateProcess : CommandPacket
    {
        public CommandElevateProcess() : base("ElevateProcess")
        {
            this.EnvironmentVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.ArgumentList = new List<string>();
            this.Arguments = null;
            this.Filename = null;
            this.WorkingDirectory = null;
            this.LingerTime = 2000;
        }

        public readonly Dictionary<string, string> EnvironmentVars;

        public string Arguments { get; set; }
        public string Filename { get; set; }
        public string WorkingDirectory { get; set; }

        public int LingerTime { get; set; }

        public readonly List<string> ArgumentList;

        public override void WriteDataToJson(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteStartObject("environment");
            foreach (var item in this.EnvironmentVars)
            {
                writer.WriteString(item.Key, item.Value);
            }
            writer.WriteEndObject();

            writer.WriteStartArray("arguments");
            foreach (var item in this.ArgumentList)
            {
                writer.WriteStringValue(item);
            }
            writer.WriteEndArray();

            if (this.Arguments == null)
            {
                writer.WriteNull("argument");
            }
            else
            {
                writer.WriteString("argument", this.Arguments);
            }

            if (this.WorkingDirectory == null)
            {
                writer.WriteNull("workingdir");
            }
            else
            {
                writer.WriteString("workingdir", this.WorkingDirectory);
            }

            if (this.Filename == null)
            {
                writer.WriteNull("filename");
            }
            else
            {
                writer.WriteString("filename", this.Filename);
            }

            writer.WriteNumber("lingertime", this.LingerTime);

            writer.WriteEndObject();
        }

        public static bool TryDecodeData(ReadOnlyMemory<byte> raw, out CommandElevateProcess command)
        {
            var result = new CommandElevateProcess();

            if (result.Decode(raw))
            {
                command = result;
                return true;
            }
            else
            {
                command = default;
                return false;
            }
        }

        public override void DecodeData(in JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return;
            if (element.TryGetProperty("argument", out var prop_arg) && prop_arg.ValueKind == JsonValueKind.String)
            {
                this.Arguments = prop_arg.GetString();
            }

            if (element.TryGetProperty("arguments", out var prop_args) && prop_args.ValueKind == JsonValueKind.Array)
            {
                using (var walker = prop_args.EnumerateArray())
                {
                    while (walker.MoveNext())
                    {
                        this.ArgumentList.Add(walker.Current.GetString());
                    }
                }
            }

            if (element.TryGetProperty("environment", out var prop_envir) && prop_envir.ValueKind == JsonValueKind.Object)
            {
                using (var walker = prop_envir.EnumerateObject())
                {
                    while (walker.MoveNext())
                    {
                        var item = walker.Current;
                        this.EnvironmentVars.Add(item.Name, item.Value.GetString());
                    }
                }
            }

            if (element.TryGetProperty("workingdir", out var prop_workingdir) && prop_workingdir.ValueKind == JsonValueKind.String)
            {
                this.WorkingDirectory = prop_workingdir.GetString();
            }

            if (element.TryGetProperty("filename", out var prop_filename) && prop_filename.ValueKind == JsonValueKind.String)
            {
                this.Filename = prop_filename.GetString();
            }

            if (element.TryGetProperty("lingertime", out var prop_lingertime) && prop_lingertime.ValueKind == JsonValueKind.Number)
            {
                this.LingerTime = prop_lingertime.GetInt32();
            }
        }

        public async Task<ResponseElevateProcess> Execute()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = this.Filename;

                    if (this.ArgumentList != null && this.ArgumentList.Count != 0)
                    {
                        for (int i = 0; i < this.ArgumentList.Count; i++)
                        {
                            process.StartInfo.ArgumentList.Add(this.ArgumentList[i]);
                        }
                    }
                    if (!string.IsNullOrEmpty(this.Arguments))
                    {
                        process.StartInfo.Arguments = this.Arguments;
                    }

                    if (this.EnvironmentVars != null && this.EnvironmentVars.Count != 0)
                    {
                        process.StartInfo.UseShellExecute = false;
                        var env = process.StartInfo.Environment;
                        foreach (var item in this.EnvironmentVars)
                        {
                            if (env.ContainsKey(item.Key))
                            {
                                env[item.Key] = item.Value;
                            }
                            else
                            {
                                env.Add(item.Key, item.Value);
                            }
                        }
                    }
                    else
                    {
                        process.StartInfo.UseShellExecute = true;
                    }

                    process.StartInfo.Verb = "runas";
                    process.StartInfo.WorkingDirectory = this.WorkingDirectory;

                    process.Start();

                    if (this.LingerTime > 0)
                    {
                        CancellationTokenSource cancelSrc = new CancellationTokenSource(this.LingerTime);
                        var token = cancelSrc.Token;
                        await process.WaitForExitAsync(token);
                    }
                    if (process.HasExited)
                    {
                        return new ResponseElevateProcess(true, null, 0, process.ExitCode);
                    }
                    else
                    {
                        return new ResponseElevateProcess(true, null, 0, null);
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResponseElevateProcess(false, ex.Message, ex.HResult, 0);
            }
        }
    }
}
