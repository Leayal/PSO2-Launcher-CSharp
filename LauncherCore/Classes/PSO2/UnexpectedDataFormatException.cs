using System;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    sealed class UnexpectedDataFormatException : Exception
    {
        public UnexpectedDataFormatException() : base("Server replies with unexpected data. As such, you shouldn't use this function for now.") { }
    }
}
