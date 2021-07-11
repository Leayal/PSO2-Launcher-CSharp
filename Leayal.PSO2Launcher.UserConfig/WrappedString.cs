using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2.UserConfig
{
    public class WrappedString
    {
        public ReadOnlyMemory<char> Value { get; }

        public WrappedString(string value)
        {
            this.Value = value.AsMemory();
        }

        public WrappedString(in ReadOnlyMemory<char> value)
        {
            this.Value = value;
        }
    }
}
