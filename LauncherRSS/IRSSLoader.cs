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
        ICollection<RSSFeed> Items { get; }

        event NotifyCollectionChangedEventHandler ItemsChanged;


    }
}
