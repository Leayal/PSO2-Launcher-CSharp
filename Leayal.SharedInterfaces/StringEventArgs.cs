using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.SharedInterfaces
{
    public class StringEventArgs : EventArgs
    {
        public string Data { get; }

        public StringEventArgs(string data)
        {
            this.Data = data;
        }
    }
}
