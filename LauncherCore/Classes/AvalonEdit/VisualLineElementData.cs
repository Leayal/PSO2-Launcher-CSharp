using ICSharpCode.AvalonEdit.Rendering;
using System;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    abstract class VisualLineElementData
    {
        public readonly int AbsoluteOffset;

        protected VisualLineElementData(int absoluteOffset)
        {
            this.AbsoluteOffset = absoluteOffset;
        }

        public abstract VisualLineText Construct(VisualLine line);
    }
}
