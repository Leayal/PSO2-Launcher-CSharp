using ICSharpCode.AvalonEdit.Rendering;
using System;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    abstract class TextTransformData
    {
        public readonly Placement InlinePlacement;

        protected TextTransformData(in Placement placement)
        {
            if (placement.Length <= 0 || placement.AbsoluteOffset < 0) throw new ArgumentOutOfRangeException(nameof(placement));
            this.InlinePlacement = placement;
        }

        public abstract Action<VisualLineElement> ApplyChanges { get; }
    }
}
