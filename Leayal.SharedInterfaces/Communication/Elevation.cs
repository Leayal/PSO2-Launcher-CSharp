using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Leayal.SharedInterfaces.Communication
{
    public class BootstrapElevation : RestartDataObj<BootstrapElevation>
    {
        public readonly Dictionary<string, string> EnvironmentVars;

        public string Arguments { get; set; }
        public string Filename { get; set; }
        public string WorkingDirectory { get; set; }

        public int LingerTime { get; set; }

        public readonly List<string> ArgumentList;

        public BootstrapElevation()
        {
            this.EnvironmentVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.ArgumentList = new List<string>();
            this.Arguments = null;
            this.Filename = null;
            this.WorkingDirectory = null;
            this.LingerTime = 2000;
        }

        public override void WriteJsonValueTo(Utf8JsonWriter writer)
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

        public override BootstrapElevation DeserializeJson(JsonElement element)
        {
            var result = new BootstrapElevation();
            if (element.TryGetProperty("argument", out var prop_arg) && prop_arg.ValueKind == JsonValueKind.String)
            {
                result.Arguments = prop_arg.GetString();
            }

            if (element.TryGetProperty("arguments", out var prop_args) && prop_args.ValueKind == JsonValueKind.Array)
            {
                using (var walker = prop_args.EnumerateArray())
                {
                    while (walker.MoveNext())
                    {
                        result.ArgumentList.Add(walker.Current.GetString());
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
                        result.EnvironmentVars.Add(item.Name, item.Value.GetString());
                    }
                }
            }

            if (element.TryGetProperty("workingdir", out var prop_workingdir) && prop_workingdir.ValueKind == JsonValueKind.String)
            {
                result.WorkingDirectory = prop_workingdir.GetString();
            }

            if (element.TryGetProperty("filename", out var prop_filename) && prop_filename.ValueKind == JsonValueKind.String)
            {
                result.Filename = prop_filename.GetString();
            }

            if (element.TryGetProperty("lingertime", out var prop_lingertime) && prop_lingertime.ValueKind == JsonValueKind.Number)
            {
                result.LingerTime = prop_lingertime.GetInt32();
            }

            // this.LingerTime

            return result;
        }
    }
}
