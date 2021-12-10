using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Interfaces
{
    /// <summary>Represent to interact with subprocess.</summary>
    public interface ISubProcessExtension
    {
        bool InitializeProcess(string[] args);
    }
}
