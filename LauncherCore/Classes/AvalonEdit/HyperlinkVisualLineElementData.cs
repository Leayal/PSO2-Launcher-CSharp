using ICSharpCode.AvalonEdit.Rendering;
using System;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    sealed class HyperlinkVisualLineElementData : VisualLineElementData
    {
        public readonly Uri NavigateUri;
        public readonly string Caption;

        public HyperlinkVisualLineElementData(int relativeOffset, string caption, Uri url) : base(relativeOffset)
        {
            this.NavigateUri = url;
            this.Caption = caption;
        }

        public override VisualLineText Construct(VisualLine line) => new HyperlinkVisualLineText(this, line, this.Caption.Length);

        public event Action<HyperlinkVisualLineElementData>? LinkClicked;

        public void OnLinkClicked()
        {
            this.LinkClicked?.Invoke(this);
        }
    }
}
