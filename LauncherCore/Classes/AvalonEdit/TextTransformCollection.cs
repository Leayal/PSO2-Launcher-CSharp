using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    class TextTransformCollection : SortedList<int, TextTransformData>
    {
        public void Add(TextTransformData data)
        {
            base.Add(data.InlinePlacement.AbsoluteOffset, data);
        }
    }
}
