﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Exports colored text as RTF
    /// </summary>
    /// <remarks>At this time only TextStyle renderer is supported. Other styles are not exported.</remarks>
    public class ExportToRTF
    {
        /// <summary>
        /// Includes line numbers
        /// </summary>
        public bool IncludeLineNumbers { get; set; }
        /// <summary>
        /// Use original font
        /// </summary>
        public bool UseOriginalFont { get; set; }

        private FastColoredTextBox? tb;
        private readonly Dictionary<Color, int> colorTable = new Dictionary<Color, int>();

        public ExportToRTF()
        {
            UseOriginalFont = true;
        }

        public string GetRtf(FastColoredTextBox tb)
        {
            this.tb = tb;
            Range sel = new Range(tb);
            sel.SelectAll();
            return GetRtf(sel);
        }

        public string GetRtf(Range r)
        {
            tb = r.Tb;
            var styles = new Dictionary<StyleIndex, object?>();
            var sb = new StringBuilder();
            var tempSB = new StringBuilder();
            var currentStyleId = StyleIndex.None;
            r.Normalize();
            int currentLine = r.Start.iLine;
            styles[currentStyleId] = null;
            colorTable.Clear();
            //
            var lineNumberColor = GetColorTableNumber(r.Tb.LineNumberColor);

            if (IncludeLineNumbers)
                _ = tempSB.AppendFormat(@"{{\cf{1} {0}}}\tab", currentLine + 1, lineNumberColor);
            //
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
                        _ = tempSB.AppendLine(@"\line");
                        if (IncludeLineNumbers)
                            _ = tempSB.AppendFormat(@"{{\cf{1} {0}}}\tab", i + 2, lineNumberColor);
                    }
                    currentLine = p.iLine;
                }
                switch (c.c)
                {
                    case '\\':
                        _ = tempSB.Append(@"\\");
                        break;
                    case '{':
                        _ = tempSB.Append(@"\{");
                        break;
                    case '}':
                        _ = tempSB.Append(@"\}");
                        break;
                    default:
                        var ch = c.c;
                        var code = (int)ch;
                        if(code < 128)
                            _ = tempSB.Append(c.c);
                        else
                            _ = tempSB.AppendFormat(@"{{\u{0}}}", code);
                        break;
                }
            }
            Flush(sb, tempSB, currentStyleId);
           
            //build color table
            var list = new SortedList<int, Color>();
            foreach (var pair in colorTable)
                list.Add(pair.Value, pair.Key);

            tempSB.Length = 0;
            _ = tempSB.AppendFormat(@"{{\colortbl;");

            foreach (var pair in list)
                _ = tempSB.Append(GetColorAsString(pair.Value) + ";");
            _ = tempSB.AppendLine("}");

            //
            if (UseOriginalFont)
            {
                _ = sb.Insert(0, $@"{{\fonttbl{{\f0\fmodern {tb.Font.Name};}}}}{{\fs{(int)(2 * tb.Font.SizeInPoints)} ");
                _ = sb.AppendLine(@"}");
            }

            _ = sb.Insert(0, tempSB.ToString());

            _ = sb.Insert(0, @"{\rtf1\ud\deff0");
            _ = sb.AppendLine(@"}");

            return sb.ToString();
        }

        private RTFStyleDescriptor GetRtfDescriptor(StyleIndex styleIndex)
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
            return !hasTextStyle ? tb.DefaultStyle.GetRTF() : textStyle.GetRTF();
        }

        public static string GetColorAsString(Color color)
        {
            if (color == Color.Transparent)
                return "";
            return string.Format(@"\red{0}\green{1}\blue{2}", color.R, color.G, color.B);
        }

        private void Flush(StringBuilder sb, StringBuilder tempSB, StyleIndex currentStyle)
        {
            //find textRenderer
            if (tempSB.Length == 0)
                return;

            var desc = GetRtfDescriptor(currentStyle);
            var cf = GetColorTableNumber(desc.ForeColor);
            var cb = GetColorTableNumber(desc.BackColor);
            var tags = new StringBuilder();
            if (cf >= 0)
                _ = tags.AppendFormat(@"\cf{0}", cf);
            if (cb >= 0)
                _ = tags.AppendFormat(@"\highlight{0}", cb);
            if(!string.IsNullOrEmpty(desc.AdditionalTags))
                _ = tags.Append(desc.AdditionalTags.Trim());

            if(tags.Length > 0)
                _ = sb.AppendFormat(@"{{{0} {1}}}", tags, tempSB.ToString());
            else
                _ = sb.Append(tempSB);
            tempSB.Length = 0;
        }

        private int GetColorTableNumber(Color color)
        {
            if (color.A == 0)
                return -1;

            if (!colorTable.ContainsKey(color))
                colorTable[color] = colorTable.Count + 1;

            return colorTable[color];
        }
    }

    public class RTFStyleDescriptor
    {
        public Color ForeColor { get; set; }
        public Color BackColor { get; set; }
        public string? AdditionalTags { get; set; }
    }
}
