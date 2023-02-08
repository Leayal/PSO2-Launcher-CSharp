using System;
using System.Threading;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Rendering;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    class HyperlinkVisualLineText : VisualLineText
    {
        private readonly HyperlinkVisualLineElementData linked;
        private int state;

        public HyperlinkVisualLineText(HyperlinkVisualLineElementData generator, VisualLine line, int length) : base(line, length)
        {
            this.linked = generator;
            this.state = 0;
        }

        protected override void OnQueryCursor(QueryCursorEventArgs e)
        {
            if (this.linked.NavigateUri == null) return;
            e.Handled = true;
            e.Cursor = Cursors.Hand;
            // base.OnQueryCursor(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (this.linked.NavigateUri == null) return;
            if (e.ChangedButton == MouseButton.Left & !e.Handled && e.LeftButton == MouseButtonState.Pressed)
            {
                Interlocked.CompareExchange(ref state, 1, 0);
                e.Handled = true;
            }
            // base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (this.linked.NavigateUri == null)
            {
                Interlocked.Exchange(ref state, 0);
                return;
            }
            if (e.ChangedButton == MouseButton.Left && !e.Handled && e.LeftButton == MouseButtonState.Released)
            {
                e.Handled = true;
                if (Interlocked.CompareExchange(ref state, 0, 1) == 1)
                {
                    this.linked.OnLinkClicked();
                }
            }
            // base.OnMouseUp(e);
        }
    }
}
