﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Leayal.PSO2.UserConfig
{
    /// <summary>
    /// Provides instances to parse, manipulate and write user configuration file for PSO2 game.
    /// </summary>
    /// <remarks>
    /// This is a complete rework of my work in the past. Even so, it's still a mess.
    /// </remarks>
    public class UserConfig : ConfigToken
    {
        public string Name { get; }

        public UserConfig(string name) : base()
        {
            this.Name = name;
        }

        public void SaveAs(string filepath)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.UTF8))
            {
                var sb = new StringBuilder();
                this.WriteValueTo(sb);
                sw.WriteLine(sb.ToString());
                sw.Flush();
            }
        }

        protected override void WriteValueTo(StringBuilder sb, int depth)
        {
            sb.Append($"{this.Name} = ");
            base.WriteValueTo(sb, 1);
        }

        public static UserConfig Parse(string content) => Parse(content.AsMemory());

        public static UserConfig FromFile(string filepath) => Parse(Leayal.Shared.FileHelper.ReadAllTexts(filepath));

        private static UserConfig Parse(ReadOnlyMemory<char> jsonData)
        {
            int pos1 = 0;
            var data = jsonData.Span;
            ReadOnlyMemory<char> tmpBuffer = ReadOnlyMemory<char>.Empty, currentProperty = ReadOnlyMemory<char>.Empty;
            List<ReadOnlyMemory<char>> r_currentPath = new List<ReadOnlyMemory<char>>();
            UserConfig root = null;
            ConfigToken token = null;

            for (int currentPos = 0; currentPos < data.Length; currentPos++)
            {
                ref readonly var c = ref data[currentPos];
                switch (c)
                {
                    case '\0':
                        break;
                    case '\n':
                        break;
                    case '\r':
                        break;
                    case '=':
                        tmpBuffer = jsonData.Slice(pos1, currentPos - pos1);
                        pos1 = currentPos + 1;
                        if (!tmpBuffer.IsEmpty && !tmpBuffer.Span.IsWhiteSpace())
                        {
                            currentProperty = tmpBuffer.Trim();
                        }
                        break;
                    case '{':
                        if (!currentProperty.IsEmpty && !currentProperty.Span.IsWhiteSpace())
                        {
                            r_currentPath.Add(currentProperty);
                            if (root == null)
                            {
                                root = new UserConfig(new string(currentProperty.Span));
                                currentProperty = ReadOnlyMemory<char>.Empty;
                                token = root;
                            }
                            else
                            {
                                token = SelectOrCreate(root, r_currentPath);
                                currentProperty = ReadOnlyMemory<char>.Empty;
                            }
                        }
                        pos1 = currentPos + 1;
                        break;
                    case '}':
                        r_currentPath.RemoveAt(r_currentPath.Count - 1);
                        token = SelectOrCreate(root, r_currentPath);
                        pos1 = currentPos + 1;
                        break;
                    case ',':
                        if (!currentProperty.IsEmpty && !currentProperty.Span.IsWhiteSpace())
                        {
                            tmpBuffer = jsonData.Slice(pos1, currentPos - pos1);
                            var val = tmpBuffer.Trim();
                            var spanVal = val.Span;
                            if (long.TryParse(spanVal, System.Globalization.NumberStyles.Integer, null, out var number))
                            {
                                token[in currentProperty] = number;
                            }
                            else if (double.TryParse(spanVal, System.Globalization.NumberStyles.Float, null, out var _floatnumber))
                            {
                                token[in currentProperty] = _floatnumber;
                            }
                            else if (bool.TryParse(spanVal, out var _boolean))
                            {
                                token[in currentProperty] = _boolean;
                            }
                            else if (spanVal[0] == '"' && spanVal[spanVal.Length - 1] == '"')
                            {
                                token[in currentProperty] = new string(tmpBuffer.Slice(1, tmpBuffer.Length - 2).Span);
                            }
                            else
                            {
                                token[in currentProperty] = val;
                            }
                            currentProperty = ReadOnlyMemory<char>.Empty;
                        }
                        pos1 = currentPos + 1;
                        break;
                }
            }
            return root;
        }

        private static ConfigToken SelectOrCreate(UserConfig root, List<ReadOnlyMemory<char>> path)
        {
            ConfigToken result = root;
            ReadOnlyMemory<char> name;
            for (int i = 1; i < path.Count; i++)
            {
                name = path[i];
                if (result[name] == null)
                {
                    result[name] = new ConfigToken();
                }
                result = (ConfigToken)result[in name];
            }
            return result;
        }
    }
}
