using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Leayal.Shared;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>The clock displays time in Japan Standard Time.</summary>
    /// <remarks>The JST time is converted from your local clock and time zone.</remarks>
    public class JSTClockTimer : ClockTicker
    {
        /// <summary>Creates a new instance of this class.</summary>
        public JSTClockTimer() : base() { }

        /// <inheritdoc/>
        protected override void InvokeCallbacks(in DateTime oldTime, in DateTime newTime)
        {
            // Convert local time to JST.
            base.InvokeCallbacks(TimeZoneHelper.ConvertTimeToLocalJST(in oldTime), TimeZoneHelper.ConvertTimeToLocalJST(in newTime));
        }
    }
}
