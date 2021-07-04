using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public class PSO2LoginException : Exception
    {
        public int ErrorCode { get; }

        public PSO2LoginException(int resultCode) : base()
        {
            this.ErrorCode = resultCode;
        }
    }
}
