using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    sealed class TextCustomTransformData : TextTransformData
    {
        private readonly Action<TextCustomTransformData, VisualLineElement> _applyChangeCallback;
        public readonly Dictionary<string, object> Properties;

        public TextCustomTransformData(int absoluteOffset, int length, Action<TextCustomTransformData, VisualLineElement> applyChangeCallback) : this(new Placement(in absoluteOffset, in length), applyChangeCallback) { }

        public TextCustomTransformData(in Placement placement, Action<TextCustomTransformData, VisualLineElement> applyChangeCallback) : base(in placement)
        {
            if (applyChangeCallback == null) throw new ArgumentNullException(nameof(applyChangeCallback));
            this._applyChangeCallback = applyChangeCallback;
            this.Properties = new Dictionary<string, object>();
        }

        public override Action<VisualLineElement> ApplyChanges => this.InternalApplyChanges;

        private void InternalApplyChanges(VisualLineElement element) => this._applyChangeCallback.Invoke(this, element);
    }
}
