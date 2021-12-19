using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Toolbox.Windows
{
    /// <summary>Provides handler for the <seealso cref="LogCategories.StartWatching(Action{LogCategories, List{string}})"/> method.</summary>
    delegate void NewFileFoundEventHandler(LogCategories sender, NewFileFoundEventArgs e);

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
