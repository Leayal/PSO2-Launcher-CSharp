using System;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>Provides handler for the <seealso cref="LogCategories.StartWatching(NewFileFoundEventHandler)"/> method.</summary>
    public delegate void NewFileFoundEventHandler(LogCategories sender, NewFileFoundEventArgs e);

    /// <summary>Provides data for the <seealso cref="NewFileFoundEventHandler"/> handler.</summary>
    public class NewFileFoundEventArgs : EventArgs
    {
        /// <summary>The name of the log category which was found.</summary>
        /// <remarks>When null, indicating that a refresh has happened.</remarks>
        public readonly string? CategoryName;

        /// <summary>Creates a new instance of this event.</summary>
        /// <param name="name">The category name of the log. Can be null.</param>
        public NewFileFoundEventArgs(string? name) : base()
        {
            this.CategoryName = name;
        }
    }
}
