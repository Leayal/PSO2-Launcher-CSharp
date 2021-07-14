using System.Text;
using System.Drawing;
using System.Collections.Generic;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Exports colored text as HTML
    /// </summary>
    /// <remarks>At this time only TextStyle renderer is supported. Other styles is not exported.</remarks>
    public class ExportToHTML
    {
        private string lineNumbersCSS = "<style type=\"text/css\"> .lineNumber{font-family : monospace; font-size : small; font-style : normal; font-weight : normal; color : Teal; background-color : ThreedFace;} </style>";

        /// <summary>
        /// Use nbsp; instead space
        /// </summary>
        public bool UseNbsp { get; set; }
        /// <summary>
        /// Use nbsp; instead space in beginning of line
        /// </summary>
        public bool UseForwardNbsp { get; set; }
        /// <summary>
        /// Use original font
        /// </summary>
        public bool UseOriginalFont { get; set; }
        /// <summary>
        /// Use style tag instead style attribute
        /// </summary>
        public bool UseStyleTag { get; set; }
        /// <summary>
        /// Use 'br' tag instead of '\n'
        /// </summary>
        public bool UseBr { get; set; }
        /// <summary>
        /// Includes line numbers
        /// </summary>
        public bool IncludeLineNumbers { get; set; }
        public string LineNumbersCSS { get => lineNumbersCSS; set => lineNumbersCSS = value; }

        private FastColoredTextBox? tb;

        public ExportToHTML()
        {
            UseNbsp = true;
            UseOriginalFont = true;
            UseStyleTag = true;
            UseBr = true;
        }

        public string GetHtml(FastColoredTextBox tb)
        {
            this.tb = tb;
            Range sel = new Range(tb);
            sel.SelectAll();
            return GetHtml(sel);
        }
        
        public string GetHtml(Range r)
        {
            tb = r.Tb;
            Dictionary<StyleIndex, object?> styles = new Dictionary<StyleIndex, object?>();
            StringBuilder sb = new StringBuilder();
            StringBuilder tempSB = new StringBuilder();
            StyleIndex currentStyleId = StyleIndex.None;
            r.Normalize();
            int currentLine = r.Start.iLine;
            styles[currentStyleId] = null;
            //
            if (UseOriginalFont)
                _ = sb.AppendFormat("<font style=\"font-family: {0}, monospace; font-size: {1}pt; line-height: {2}px;\">",
                                                r.Tb.Font.Name, r.Tb.Font.SizeInPoints, r.Tb.CharHeight);

            //
            if (IncludeLineNumbers)
                _ = tempSB.AppendFormat("<span class=lineNumber>{0}</span>  ", currentLine + 1);
            //
            bool hasNonSpace = false;
            foreach (Place p in r)
            {
                Char c = r.Tb[p.iLine][p.iChar];
                if (c.style != currentStyleId)
                {
                    Flush(sb, tempSB, currentStyleId);
                    currentStyleId = c.style;
                    styles[currentStyleId] = null;
                }

                if (p.iLine != currentLine)
                {
                    for (int i = currentLine; i < p.iLine; i++)
                    {
                        _ = tempSB.Append(UseBr ? "<br>" : "\r\n");
                        if (IncludeLineNumbers)
                            _ = tempSB.AppendFormat("<span class=lineNumber>{0}</span>  ", i + 2);
                    }
                    currentLine = p.iLine;
                    hasNonSpace = false;
                }
                switch (c.c)
                {
                    case ' ':
                        if ((hasNonSpace || !UseForwardNbsp) && !UseNbsp)
                            goto default;

                        _ = tempSB.Append("&nbsp;");
                        break;
                    case '<':
                        _ = tempSB.Append("&lt;");
                        break;
                    case '>':
                        _ = tempSB.Append("&gt;");
                        break;
                    case '&':
                        _ = tempSB.Append("&amp;");
                        break;
                    default:
                        hasNonSpace = true;
                        _ = tempSB.Append(c.c);
                        break;
                }
            }
            Flush(sb, tempSB, currentStyleId);

            if (UseOriginalFont)
                _ = sb.Append("</font>");

            //build styles
            if (UseStyleTag)
            {
                tempSB.Length = 0;
                _ = tempSB.Append("<style type=\"text/css\">");
                foreach (var styleId in styles.Keys)
                    _ = tempSB.AppendFormat(".fctb{0}{{ {1} }}\r\n", GetStyleName(styleId), GetCss(styleId));
                _ = tempSB.Append("</style>");

                _ = sb.Insert(0, tempSB.ToString());
            }

            if (IncludeLineNumbers)
                _ = sb.Insert(0, LineNumbersCSS);

            return sb.ToString();
        }

        private string GetCss(StyleIndex styleIndex)
        {
            List<Style> styles = new List<Style>();
            //find text renderer
            TextStyle? textStyle = null;
            int mask = 1;
            bool hasTextStyle = false;
            for (int i = 0; i < tb.Styles.Length; i++)
            {
                if (tb.Styles[i] != null && ((int)styleIndex & mask) != 0)
                if (tb.Styles[i].IsExportable)
                {
                    var style = tb.Styles[i];
                    styles.Add(style);

                    bool isTextStyle = style is TextStyle;
                    if (isTextStyle)
                        if (!hasTextStyle || tb.AllowSeveralTextStyleDrawing)
                        {
                            hasTextStyle = true;
                            textStyle = style as TextStyle;
                        }
                }
                mask <<= 1;
            }
            //add TextStyle css
            string result = "";
            
            if (!hasTextStyle)
            {
                //draw by default renderer
                result = tb.DefaultStyle.GetCSS();
            }
            else
            {
                result = ((TextStyle?)null).GetCSS();
            }
            //add no TextStyle css
            foreach(var style in styles)
//            if (style != textStyle)
            if(style is not TextStyle)
                result += style.GetCSS();

            return result;
        }

        public static string GetColorAsString(Color color)
        {
            if(color==Color.Transparent)
                return "";
            return string.Format("#{0:x2}{1:x2}{2:x2}", color.R, color.G, color.B);
        }

        private static string GetStyleName(StyleIndex styleIndex)
        {
            return styleIndex.ToString().Replace(" ", "").Replace(",", "");
        }

        private void Flush(StringBuilder sb, StringBuilder tempSB, StyleIndex currentStyle)
        {
            //find textRenderer
            if (tempSB.Length == 0)
                return;
            if (UseStyleTag)
                _ = sb.AppendFormat("<font class=fctb{0}>{1}</font>", GetStyleName(currentStyle), tempSB.ToString());
            else
            {
                string css = GetCss(currentStyle);
                if(css!="")
                    _ = sb.AppendFormat("<font style=\"{0}\">", css);
                _ = sb.Append(tempSB);
                if (css != "")
                    _ = sb.Append("</font>");
            }
            tempSB.Length = 0;
        }
    }
}
