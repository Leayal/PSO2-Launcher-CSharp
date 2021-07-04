using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    class UnexpectedDataFormatException : Exception
    {
        public UnexpectedDataFormatException() : base("Server replies with unexpected data. As such, you shouldn't use this function for now.") { }
    }
}
