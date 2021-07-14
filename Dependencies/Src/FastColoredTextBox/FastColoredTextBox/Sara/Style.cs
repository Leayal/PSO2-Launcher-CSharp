using System.Drawing;

// ReSharper disable once CheckNamespace
namespace FastColoredTextBoxNS
{
    public partial class TextStyle
    {
        private void DrawSara(Graphics gr, Range range, Line line, Font f, float x, float y, float dx)
        {
            var end = range.End.iChar + line.TotalHiddenCharacters;
            for (var i = range.Start.iChar; i < end; i++)
            {
                if (line.HideCharacter(i)) continue;

                // If the TotalHiddenCharacters takes us beyond the actual lines available then break out. - Sara
                if (line.Count <= i) break;

                //draw char
                gr.DrawString(line[i].c.ToString(), f, ForeBrush, x, y, StringFormat);
                x += dx;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            ForeBrush?.Dispose();
            BackgroundBrush?.Dispose();
        }
    }

    public partial class SelectionStyle
    {
        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            BackgroundBrush?.Dispose();
        }

    }

    public partial class MarkerStyle
    {
        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            BackgroundBrush?.Dispose();
        }
    }
    public partial class ShortcutStyle
    {
        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            borderPen?.Dispose();
        }
    }
}
