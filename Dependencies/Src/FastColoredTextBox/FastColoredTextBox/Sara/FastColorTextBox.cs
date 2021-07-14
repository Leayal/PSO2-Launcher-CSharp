using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

// ReSharper disable once CheckNamespace
namespace FastColoredTextBoxNS
{
    public partial class FastColoredTextBox
    {
        private void CtorSara()
        {
            timer.Tag = "SelectionDelayedTimer";
            AutoCompleteBrackets = true;
        }

        private int currentPenSize = 3;

        /// <summary>
        /// 
        /// </summary>
        public int CurrentPenSize
        {
            get => currentPenSize;
            set
            {
                currentPenSize = value;
                Invalidate();
            }
        }

        private static void DrawHighlight(Line line, Graphics gr, int x, int y, int width, int height)
        {
            if (line.PropertyBackColor != Color.Transparent)
                gr.FillRectangle(new SolidBrush(line.PropertyBackColor), new Rectangle(x, y, width, height));
            if (line.DocumentMapOverlayBackColor != Color.Transparent)
                gr.FillRectangle(new SolidBrush(line.DocumentMapOverlayBackColor), new Rectangle(x, y, 50, height));
            if (line.PatternOverlayBackColor != Color.Transparent)
                gr.FillRectangle(new SolidBrush(line.PatternOverlayBackColor), new Rectangle(x, y + height, width, 1));
        }

        /// <summary>
        /// Event handler
        /// </summary>
        public delegate void PaintLineFullAccessEventHandler(Line line, int iLine);

        /// <summary>
        /// It occurs when line background is painting.  Provides the option to change the background color of the line.
        /// </summary>
        [Browsable(true)]
        [Description(
            "It occurs when line background is painting.  Provides the option to change the background color of the line.")]
        public event PaintLineFullAccessEventHandler? PaintLineFullAccess;

        /// <summary>
        /// Clears the LineData object within the Lines - Sara
        /// </summary>
        public void ClearCache()
        {
            foreach (Line line in lines)
            {
                line.LineData = null;
                line.HiddenText.Clear();
            }
        }
        /// <summary>
        /// Added path for debugging purposes - Travis
        /// </summary>
        public string? DocumentPath { get; set; }
        /// <summary>
        /// Exposes the Line object to the consumer - Sara
        /// </summary>
        protected virtual void SaraOnPaintLineFullAccess(Line line, int iLine)
        {
            PaintLineFullAccess?.Invoke(line, iLine);
        }
        /// <summary>
        /// When True the SelectedChangedDelay will be fired
        /// </summary>
        public bool SelectionChangedDelayedEnabled { get; set; }

        private void NewMethod(PaintEventArgs e, int y)
        {
            // Adding Documentation Marker - Sara
            var size = CharHeight - 1;
            e.Graphics.FillRectangle(new SolidBrush(Color.Red), new RectangleF(1, y, 2, size));
            // I moved the line to the left, but wanted to leave the old code for reference in case I need to move it back - Travis
            //                    e.Graphics.FillRectangle(new SolidBrush(Color.Red), new RectangleF(LeftIndent - minLeftIndent - 2, y, 2, size));
        }

        private void NewMethod1(PaintEventArgs e, Line line, int iLine, Rectangle textAreaRect, int y, LineInfo lineInfo)
        {
            SaraOnPaintLineFullAccess(line, iLine);
            //draw current line background
            if (CurrentLineColor != Color.Transparent && iLine == Selection.Start.iLine)
                if (Selection.IsEmpty)
                    e.Graphics.DrawRectangle(new Pen(CurrentLineColor, CurrentPenSize), // currentLineBrush,
                        new Rectangle(textAreaRect.Left, y, textAreaRect.Width, CharHeight));

            //draw line background
            var height = CharHeight * lineInfo.WordWrapStringsCount - 2;
            var width = textAreaRect.Width;
            if (lineInfo.VisibleState == VisibleState.Visible)
                DrawHighlight(line, e.Graphics, textAreaRect.Left, y + 2, width, height);
        }


    }
}
