using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class LanguageLoader
    {
        private readonly Dictionary<string, string> _pairs;


        public LanguageFile CurrentFile { get; set; }

        public LanguageLoader()
        {
            this._pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);


        }

        public void LoadFile(string file)
        {
            if (File.Exists(file))
            {
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                using (var sr = new StreamReader(fs))
                {
                    while (!sr.EndOfStream)
                    {
                        int character = sr.Read();
                        if (character == -1)
                        {
                            // EOF
                        }
                        else
                        {
                            character = sr.Read();
                            char c = (char)character;
                            switch (c)
                            {
                                case '=':

                                    break;
                                case '\n':
                                    break;
                                case '\r':
                                    break;
                                case '[':
                                    break;
                                case ']':
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new FileNotFoundException("");
            }
        }
    }
}
