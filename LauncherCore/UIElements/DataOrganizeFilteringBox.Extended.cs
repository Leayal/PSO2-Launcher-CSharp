using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    partial class DataOrganizeFilteringBox
    {
        public enum ClientType
        {
            Both,
            NGS,
            Classic
        }

        public enum SizeComparisonType
        {
            Bigger,
            Smaller
        }

        public enum SizeComparisonScale
        {
            B,
            KB,
            MB,
            GB
        }
    }
}
