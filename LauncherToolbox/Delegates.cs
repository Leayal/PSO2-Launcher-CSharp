using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>Callback which be invoked when a log data is found.</summary>
    /// <param name="data">The log data.</param>
    public delegate void DataReceivedCallback(in PSO2LogData data);

    /// <summary>The event handler which be invoked when a log data is found.</summary>
    /// <param name="sender">The event's invoker.</param>
    /// <param name="data">The log data.</param>
    public delegate void DataReceivedEventHandler(PSO2LogAsyncListener? sender, in PSO2LogData data);
}
