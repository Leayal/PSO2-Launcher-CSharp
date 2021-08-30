using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    public class HandlerNotRegisteredException : Exception
    {
        public string TargetHandlerName { get; }
        public HandlerNotRegisteredException(string handlername) : base($"The target handler '{handlername}' is not found in the loader.") 
        {
            this.TargetHandlerName = handlername;
        }
    }
}
