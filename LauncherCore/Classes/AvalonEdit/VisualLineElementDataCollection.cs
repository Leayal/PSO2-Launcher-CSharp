using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    class VisualLineElementDataCollection : SortedList<int, VisualLineElementData>
    {
        public void Add(VisualLineElementData data)
        {
            base.Add(data.AbsoluteOffset, data);
        }
    }
}
