using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Interfaces
{
    /// <summary>Represent a main program.</summary>
    public interface ILauncherProgram
    {
        /// <summary>Gets a value determines whether the program has WPF components.</summary>
        bool HasWPF { get; }

        /// <summary>Gets a value determines whether the program has Windows Forms components.</summary>
        bool HasWinForm { get; }

        /// <summary>Occurs when the program is loaded but it hasn't run yet.</summary>
        event EventHandler Initialized;

        /// <summary>Run the program without any arguments.</summary>
        /// <remarks>This is the same as calling <seealso cref="Run(string[])"/> with an empty array.</remarks>
        void Run() => this.Run(Array.Empty<string>());

        /// <summary>Run the program with arguments.</summary>
        void Run(string[] args);

        /// <summary>Exit the program and return the exit code.</summary>
        /// <returns>An internet which is exit code.</returns>
        int Exit();
    }
}
