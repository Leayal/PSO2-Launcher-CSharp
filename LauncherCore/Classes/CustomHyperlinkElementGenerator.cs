using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Input;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class CustomHyperlinkElementGenerator : VisualLineElementGenerator
    {
        public readonly struct Placements
        {
            public readonly int Offset, Length; // Line

            public Placements(in int offset, in int length)
            {
                // this.Line = line;
                this.Offset = offset;
                this.Length = length;
            }
        }

        public readonly Dictionary<Placements, Uri> Items;

        public readonly CustomHyperlinkElementColorizer Colorizer;

        /// <summary>
        /// Creates a new LinkElementGenerator.
        /// </summary>
        public CustomHyperlinkElementGenerator() : base()
        {
            this.Colorizer = new CustomHyperlinkElementColorizer(this);
            this.Items = new Dictionary<Placements, Uri>();
        }

        /// <inheritdoc/>
        public override int GetFirstInterestedOffset(int startOffset)
        {
            // var beginning = CurrentContext.VisualLine.FirstDocumentLine;
            // var firstOffset = beginning.Offset;
            // var currentLineNumber = beginning.LineNumber;
            int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
            // var segment = CurrentContext.GetText(startOffset, endOffset - startOffset);
            // var span = segment.Text.AsSpan().Slice(segment.Offset, segment.Count);
            foreach (var item in this.Items)
            {
                var info = item.Key;
                if (startOffset <= info.Offset && (info.Offset + info.Length) <= endOffset)
                {
                    return info.Offset;
                }
            }

            return -1;
        }

        /// <inheritdoc/>
        public override VisualLineElement ConstructElement(int offset)
        {
            // var beginning = CurrentContext.VisualLine.FirstDocumentLine;
            // var firstOffset = beginning.Offset;
            // var currentLineNumber = beginning.LineNumber;
            // int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
            // var segment = CurrentContext.GetText(offset, endOffset - offset);
            // var span = segment.Text.AsSpan().Slice(segment.Offset, segment.Count);
            foreach (var item in this.Items)
            {
                var info = item.Key;
                if (offset == info.Offset)
                {
                    return new VisualLineLinkTextEx(this, CurrentContext.VisualLine, item.Key.Length, item.Value);
                }
            }
            return null;
        }

        public event Action<VisualLineLinkTextEx> LinkClicked;

        public class VisualLineLinkTextEx : VisualLineText
        {
            private readonly CustomHyperlinkElementGenerator linked;
            private int state;

            public readonly Uri NavigateUri;

            public VisualLineLinkTextEx(CustomHyperlinkElementGenerator generator, VisualLine line, int length, Uri url) : base(line, length)
            {
                this.NavigateUri = url;
                this.linked = generator;
                this.state = 0;
            }

            protected override void OnQueryCursor(QueryCursorEventArgs e)
            {
                e.Handled = true;
                e.Cursor = Cursors.Hand;
                // base.OnQueryCursor(e);
            }

            protected override void OnMouseDown(MouseButtonEventArgs e)
            {
                if (this.NavigateUri == null) return;
                if (e.ChangedButton == MouseButton.Left & !e.Handled && e.LeftButton == MouseButtonState.Pressed)
                {
                    Interlocked.CompareExchange(ref this.state, 1, 0);
                    e.Handled = true;
                }
                // base.OnMouseDown(e);
            }

            protected override void OnMouseUp(MouseButtonEventArgs e)
            {
                if (this.NavigateUri == null)
                {
                    Interlocked.Exchange(ref this.state, 0);
                    return;
                }
                if (e.ChangedButton == MouseButton.Left && !e.Handled && e.LeftButton == MouseButtonState.Released)
                {
                    e.Handled = true;
                    if (Interlocked.CompareExchange(ref this.state, 0, 1) == 1)
                    {
                        this.linked.LinkClicked?.Invoke(this);
                    }
                }
                // base.OnMouseUp(e);
            }
        }

        public class CustomHyperlinkElementColorizer : DocumentColorizingTransformer
        {
            private readonly CustomHyperlinkElementGenerator generator;

            public System.Windows.Media.Brush ForegroundBrush { get; set; }

            public CustomHyperlinkElementColorizer(CustomHyperlinkElementGenerator parent) : base() 
            {
                this.generator = parent;
            }

            protected override void ColorizeLine(ICSharpCode.AvalonEdit.Document.DocumentLine line)
            {
                var currentIndex = line.Offset;
                var absolutelength = line.EndOffset;
                foreach (var item in this.generator.Items)
                {
                    var info = item.Key;
                    if (currentIndex <= info.Offset && (info.Offset + info.Length) <= absolutelength)
                    {
                        currentIndex = info.Offset + info.Length;
                        ChangeLinePart(info.Offset, currentIndex, ApplyChanges);
                    }
                    if (currentIndex >= absolutelength)
                    {
                        break;
                    }
                }
            }

            private void ApplyChanges(VisualLineElement element)
            {
                // This is where you do anything with the line
                var props = element.TextRunProperties;

                props.SetForegroundBrush(this.ForegroundBrush);
                props.SetTextDecorations(System.Windows.TextDecorations.Underline);
                // props.SetFontHintingEmSize(1d);
                // props.SetFontRenderingEmSize(1d);
            }
        }
    }
}
