using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    sealed class EmptyPatchListException : Exception
    {
        public EmptyPatchListException() : base("Failed to update the game client due to the patchlist info is empty. Please check if your Internet connectivity behaves normally") { }
    }
}
