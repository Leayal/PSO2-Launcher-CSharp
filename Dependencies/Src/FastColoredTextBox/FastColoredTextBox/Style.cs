using System.Drawing;
using System;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Style of chars
    /// </summary>
    /// <remarks>This is base class for all text and design renderers</remarks>
    public abstract class Style : IDisposable
    {
        /// <summary>
        /// This style is exported to outer formats (HTML for example)
        /// </summary>
        public virtual bool IsExportable { get; set; }
        /// <summary>
        /// Occurs when user click on StyleVisualMarker joined to this style 
        /// </summary>
        public event EventHandler<VisualMarkerEventArgs>? VisualMarkerClick;

        /// <summary>
        /// Constructor
        /// </summary>
        public Style()
        {
            IsExportable = true;
        }

        /// <summary>
        /// Renders given range of text
        /// </summary>
        /// <param name="gr">Graphics object</param>
        /// <param name="position">Position of the range in absolute control coordinates</param>
        /// <param name="range">Rendering range of text</param>
        public abstract void Draw(Graphics gr, Point position, Range range);

        /// <summary>
        /// Occurs when user click on StyleVisualMarker joined to this style 
        /// </summary>
        public virtual void OnVisualMarkerClick(FastColoredTextBox tb, VisualMarkerEventArgs args)
        {
            VisualMarkerClick?.Invoke(tb, args);
        }

        /// <summary>
        /// Shows VisualMarker
        /// Call this method in Draw method, when you need to show VisualMarker for your style
        /// </summary>
        protected virtual void AddVisualMarker(FastColoredTextBox tb, StyleVisualMarker marker)
        {
            tb.AddVisualMarker(marker);
        }

        public static Size GetSizeOfRange(Range range)
        {
            return new Size((range.End.iChar - range.Start.iChar) * range.Tb.CharWidth, range.Tb.CharHeight);
        }

        public static GraphicsPath GetRoundedRectangle(Rectangle rect, int d)
        {
            GraphicsPath gp = new GraphicsPath();

            gp.AddArc(rect.X, rect.Y, d, d, 180, 90);
            gp.AddArc(rect.X + rect.Width - d, rect.Y, d, d, 270, 90);
            gp.AddArc(rect.X + rect.Width - d, rect.Y + rect.Height - d, d, d, 0, 90);
            gp.AddArc(rect.X, rect.Y + rect.Height - d, d, d, 90, 90);
            gp.AddLine(rect.X, rect.Y + rect.Height - d, rect.X, rect.Y + d / 2);

            return gp;
        }

        public virtual void Dispose()
        {
            ;
        }

        /// <summary>
        /// Returns CSS for export to HTML
        /// </summary>
        /// <returns></returns>
        public virtual string GetCSS()
        {
            return "";
        }

        /// <summary>
        /// Returns RTF descriptor for export to RTF
        /// </summary>
        /// <returns></returns>
        public virtual RTFStyleDescriptor GetRTF()
        {
            return new RTFStyleDescriptor();
        }
    }

    /// <summary>
    /// Style for chars rendering
    /// This renderer can draws chars, with defined fore and back colors
    /// </summary>
    public partial class TextStyle : Style
    {
        public Brush? ForeBrush { get; set; }
        public Brush? BackgroundBrush { get; set; }
        public FontStyle FontStyle { get; set; }
        public StringFormat? StringFormat { get => stringFormat; set => stringFormat = value; }

        //public readonly Font Font;
        private StringFormat? stringFormat;

        public TextStyle(Brush? foreBrush, Brush? backgroundBrush, FontStyle fontStyle)
        {
            ForeBrush = foreBrush;
            BackgroundBrush = backgroundBrush;
            FontStyle = fontStyle;
            StringFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            //draw background
            if (BackgroundBrush != null)
                gr.FillRectangle(BackgroundBrush, position.X, position.Y, (range.End.iChar - range.Start.iChar) * range.Tb.CharWidth, range.Tb.CharHeight);
            //draw chars
            using var f = new Font(range.Tb.Font, FontStyle);
            Line line = range.Tb[range.Start.iLine];
            float dx = range.Tb.CharWidth;
            float y = position.Y + range.Tb.LineInterval / 2;
            float x = position.X - range.Tb.CharWidth / 3;

            if (ForeBrush == null)
                ForeBrush = new SolidBrush(range.Tb.ForeColor);

            if (range.Tb.ImeAllowed)
            {
                //IME mode
                for (int i = range.Start.iChar; i < range.End.iChar; i++)
                {
                    SizeF size = FastColoredTextBox.GetCharSize(f, line[i].c);

                    var gs = gr.Save();
                    float k = size.Width > range.Tb.CharWidth + 1 ? range.Tb.CharWidth / size.Width : 1;
                    gr.TranslateTransform(x, y + (1 - k) * range.Tb.CharHeight / 2);
                    gr.ScaleTransform(k, (float)Math.Sqrt(k));
                    gr.DrawString(line[i].c.ToString(), f, ForeBrush, 0, 0, StringFormat);
                    gr.Restore(gs);
                    x += dx;
                }
            }
            else
            {
                //classic mode 
                DrawSara(gr, range, line, f, x, y, dx);
            }
        }

        public override string GetCSS()
        {
            string result = "";

            if (BackgroundBrush is SolidBrush backgroundBrush)
            {
                var s =  ExportToHTML.GetColorAsString(backgroundBrush.Color);
                if (s != "")
                    result += "background-color:" + s + ";";
            }
            if (ForeBrush is SolidBrush foreBrush)
            {
                var s = ExportToHTML.GetColorAsString(foreBrush.Color);
                if (s != "")
                    result += "color:" + s + ";";
            }
            if ((FontStyle & FontStyle.Bold) != 0)
                result += "font-weight:bold;";
            if ((FontStyle & FontStyle.Italic) != 0)
                result += "font-style:oblique;";
            if ((FontStyle & FontStyle.Strikeout) != 0)
                result += "text-decoration:line-through;";
            if ((FontStyle & FontStyle.Underline) != 0)
                result += "text-decoration:underline;";

            return result;
        }

        public override RTFStyleDescriptor GetRTF()
        {
            var result = new RTFStyleDescriptor();

            if (BackgroundBrush is SolidBrush backgroundBrush)
                result.BackColor = backgroundBrush.Color;
            
            if (ForeBrush is SolidBrush foreBrush)
                result.ForeColor = foreBrush.Color;
            
            if ((FontStyle & FontStyle.Bold) != 0)
                result.AdditionalTags += @"\b";
            if ((FontStyle & FontStyle.Italic) != 0)
                result.AdditionalTags += @"\i";
            if ((FontStyle & FontStyle.Strikeout) != 0)
                result.AdditionalTags += @"\strike";
            if ((FontStyle & FontStyle.Underline) != 0)
                result.AdditionalTags += @"\ul";

            return result;
        }
    }

    /// <summary>
    /// Renderer for folded block
    /// </summary>
    public class FoldedBlockStyle : TextStyle
    {
        public FoldedBlockStyle(Brush foreBrush, Brush? backgroundBrush, FontStyle fontStyle):
            base(foreBrush, backgroundBrush, fontStyle)
        {
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            if (range.End.iChar > range.Start.iChar)
            {
                base.Draw(gr, position, range);

                int firstNonSpaceSymbolX = position.X;
                
                //find first non space symbol
                for (int i = range.Start.iChar; i < range.End.iChar; i++)
                    if (range.Tb[range.Start.iLine][i].c != ' ')
                        break;
                    else
                        firstNonSpaceSymbolX += range.Tb.CharWidth;

                //create marker
                range.Tb.AddVisualMarker(new FoldedAreaMarker(range.Start.iLine, new Rectangle(firstNonSpaceSymbolX, position.Y, position.X + (range.End.iChar - range.Start.iChar) * range.Tb.CharWidth - firstNonSpaceSymbolX, range.Tb.CharHeight)));
            }
            else
            {
                //draw '...'
                using(Font f = new Font(range.Tb.Font, FontStyle))
                    gr.DrawString("...", f, ForeBrush!, range.Tb.LeftIndent, position.Y - 2);
                //create marker
                range.Tb.AddVisualMarker(new FoldedAreaMarker(range.Start.iLine, new Rectangle(range.Tb.LeftIndent + 2, position.Y, 2 * range.Tb.CharHeight, range.Tb.CharHeight)));
            }
        }
    }

    /// <summary>
    /// Renderer for selected area
    /// </summary>
    public partial class SelectionStyle : Style
    {
        public Brush BackgroundBrush{ get; set;}
        public Brush? ForegroundBrush { get; private set; }

        public override bool IsExportable
        {
            get{return false;}  set{}
        }

        public SelectionStyle(Brush backgroundBrush, Brush? foregroundBrush = null)
        {
            BackgroundBrush = backgroundBrush;
            ForegroundBrush = foregroundBrush;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            //draw background
            if (BackgroundBrush != null)
            {
                gr.SmoothingMode = SmoothingMode.None;
                var rect = new Rectangle(position.X, position.Y, (range.End.iChar - range.Start.iChar) * range.Tb.CharWidth, range.Tb.CharHeight);
                if (rect.Width == 0)
                    return;
                gr.FillRectangle(BackgroundBrush, rect);
                //
                if (ForegroundBrush != null)
                {
                    //draw text
                    gr.SmoothingMode = SmoothingMode.AntiAlias;

                    var r = new Range(range.Tb, range.Start.iChar, range.Start.iLine,
                                      Math.Min(range.Tb[range.End.iLine].Count, range.End.iChar), range.End.iLine);
                    using var style = new TextStyle(ForegroundBrush, null, FontStyle.Regular);
                    style.Draw(gr, new Point(position.X, position.Y - 1), r);
                }
            }
        }
    }

    /// <summary>
    /// Marker style
    /// Draws background color for text
    /// </summary>
    public partial class MarkerStyle : Style
    {
        public Brush BackgroundBrush{get;set;}

        public MarkerStyle(Brush backgroundBrush)
        {
            BackgroundBrush = backgroundBrush;
            IsExportable = true;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            //draw background
            if (BackgroundBrush != null)
            {
                Rectangle rect = new Rectangle(position.X, position.Y, (range.End.iChar - range.Start.iChar) * range.Tb.CharWidth, range.Tb.CharHeight);
                if (rect.Width == 0)
                    return;
                gr.FillRectangle(BackgroundBrush, rect);
            }
        }

        public override string GetCSS()
        {
            string result = "";

            if (BackgroundBrush is SolidBrush backgroundBrush)
            {
                var s = ExportToHTML.GetColorAsString(backgroundBrush.Color);
                if (s != "")
                    result += "background-color:" + s + ";";
            }

            return result;
        }
    }

    /// <summary>
    /// Draws small rectangle for popup menu
    /// </summary>
    public partial class ShortcutStyle : Style
    {
        public Pen borderPen;

        public ShortcutStyle(Pen borderPen)
        {
            this.borderPen = borderPen;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            //get last char coordinates
            Point p = range.Tb.PlaceToPoint(range.End);
            //draw small square under char
            Rectangle rect = new Rectangle(p.X - 5, p.Y + range.Tb.CharHeight - 2, 4, 3);
            gr.FillPath(Brushes.White, GetRoundedRectangle(rect, 1));
            gr.DrawPath(borderPen, GetRoundedRectangle(rect, 1));
            //add visual marker for handle mouse events
            AddVisualMarker(range.Tb, new StyleVisualMarker(new Rectangle(p.X-range.Tb.CharWidth, p.Y, range.Tb.CharWidth, range.Tb.CharHeight), this));
        }
    }

    /// <summary>
    /// This style draws a wavy line below a given text range.
    /// </summary>
    /// <remarks>Thanks for Yallie</remarks>
    public class WavyLineStyle : Style
    {
        private Pen Pen { get; set; }

        public WavyLineStyle(int alpha, Color color)
        {
            Pen = new Pen(Color.FromArgb(alpha, color));
        }

        public override void Draw(Graphics gr, Point pos, Range range)
        {
            var size = GetSizeOfRange(range);
            var start = new Point(pos.X, pos.Y + size.Height - 1);
            var end = new Point(pos.X + size.Width, pos.Y + size.Height - 1);
            DrawWavyLine(gr, start, end);
        }

        private void DrawWavyLine(Graphics graphics, Point start, Point end)
        {
            if (end.X - start.X < 2)
            {
                graphics.DrawLine(Pen, start, end);
                return;
            }

            var offset = -1;
            var points = new List<Point>();

            for (int i = start.X; i <= end.X; i += 2)
            {
                points.Add(new Point(i, start.Y + offset));
                offset = -offset;
            }

            graphics.DrawLines(Pen, points.ToArray());
        }

        public override void Dispose()
        {
            base.Dispose();

            if (Pen != null)
                Pen.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// This style is used to mark range of text as ReadOnly block
    /// </summary>
    /// <remarks>You can inherite this style to add visual effects of readonly text</remarks>
    public class ReadOnlyStyle : Style
    {
        public ReadOnlyStyle()
        {
            IsExportable = false;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            //
        }
    }
}
