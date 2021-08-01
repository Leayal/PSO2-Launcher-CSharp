using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    public interface IRSSLoader
    {
        /// <summary>All found <seealso cref="RSSFeed"/> from assemblies.</summary>
        ICollection<RSSFeed> Items { get; }

        event NotifyCollectionChangedEventHandler ItemsChanged;

        /// <summary>Load an assembly from the path and search for all <seealso cref="RSSFeed"/>-inherited classes.</summary>
        /// <param name="filename">Path to the assembly</param>
        IReadOnlyList<RSSFeed> Load(string filename);

        /// <summary>Load an assembly from the paths and search for all <seealso cref="RSSFeed"/>-inherited classes.</summary>
        /// <param name="filenames">Paths to the assembly</param>
        IReadOnlyList<RSSFeed> Load(params string[] filenames);

        /// <summary>Load an assembly from the paths and search for all <seealso cref="RSSFeed"/>-inherited classes.</summary>
        /// <param name="filenames">Paths to the assembly</param>
        IReadOnlyList<RSSFeed> Load(IEnumerable<string> filenames);
    }
}
