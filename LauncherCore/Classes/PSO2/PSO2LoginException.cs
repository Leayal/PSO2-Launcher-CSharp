using System;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public sealed class PSO2LoginException : Exception
    {
        public int ErrorCode { get; }

        public PSO2LoginException(int resultCode) : base()
        {
            this.ErrorCode = resultCode;
        }
    }
}
