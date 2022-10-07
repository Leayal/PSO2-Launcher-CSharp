using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2.Installer
{
    /// <summary>Visual C++ 2015-2019 Redist.</summary>
    /// <remarks><para>As 2017 and 2019 are <b>binary-compatible</b> with 2015. 2015 can be in-place replaced with either the two.</para></remarks>
    public enum VCRedistVersion
    {
        /// <summary>Not installed.</summary>
        None,

        /// <summary>Visual C++ 2015 Redist installed.</summary>
        VC2015,

        /// <summary>Visual C++ 2015-2017 Redist installed.</summary>
        VC2017,

        /// <summary>Visual C++ 2015-2019 Redist installed.</summary>
        VC2019,

        /// <summary>Visual C++ 2015-2022 Redist installed.</summary>
        VC2022,

        /// <summary>Visual C++ 2022+ Redist installed.</summary>
        NewerThanExpected
    }
}
