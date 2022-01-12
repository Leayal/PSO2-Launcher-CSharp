using System;
using System.Collections.Generic;

namespace Leayal.PSO2Launcher.Interfaces
{
    /// <summary>Represent a main program.</summary>
    public interface ILauncherProgram
    {
        /// <summary>Gets the launcher's model version.</summary>
        /// <remarks>Primary to determine which features and codeflow to implement.</remarks>
        static int PSO2LauncherModelVersion { get; } = LauncherController.PSO2LauncherModelVersion;

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

        /// <summary>Occurs after the program exits and unloads.</summary>
        event EventHandler Exited;
    }
}
