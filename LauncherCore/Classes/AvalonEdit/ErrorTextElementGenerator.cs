using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Input;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    class ErrorTextElementGenerator : VisualLineElementGenerator
    {
        private readonly HashSet<Placements> Items;

        public readonly CustomErrorTextElementColorizer Colorizer;

        /// <summary>
        /// Creates a new LinkElementGenerator.
        /// </summary>
        public ErrorTextElementGenerator() : base()
        {
            Colorizer = new CustomErrorTextElementColorizer(this);
            Items = new HashSet<Placements>();
        }

        public void Add(Placements placement) => this.Items.Add(placement);

        public void Clear()
        {
            this.Items.Clear();
            this.Colorizer.ClearCache();
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
            foreach (var info in this.Items)
            {
                if (startOffset <= info.Offset && info.Offset + info.Length <= endOffset)
                {
                    return info.Offset;
                }
            }

            return -1;
        }

        /// <inheritdoc/>
        public override VisualLineElement? ConstructElement(int offset) => null;

        public class CustomErrorTextElementColorizer : DocumentColorizingTransformer
        {
            private readonly ErrorTextElementGenerator generator;
            private readonly Dictionary<System.Windows.Media.Typeface, System.Windows.Media.Typeface> cachedTypeFace;

            public System.Windows.Media.Brush ForegroundBrush { get; set; }

            public void ClearCache() => this.cachedTypeFace.Clear();

            public CustomErrorTextElementColorizer(ErrorTextElementGenerator parent) : base()
            {
                this.cachedTypeFace = new Dictionary<System.Windows.Media.Typeface, System.Windows.Media.Typeface>();
                this.ForegroundBrush = System.Windows.Media.Brushes.Red;
                this.generator = parent;
            }

            protected override void ColorizeLine(ICSharpCode.AvalonEdit.Document.DocumentLine line)
            {
                var currentIndex = line.Offset;
                var absolutelength = line.EndOffset;
                foreach (var info in generator.Items)
                {
                    if (currentIndex <= info.Offset && info.Offset + info.Length <= absolutelength)
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

                props.SetForegroundBrush(ForegroundBrush);
                var typeface = props.Typeface;
                if (!this.cachedTypeFace.TryGetValue(typeface, out var cachedBoldFace))
                {
                    cachedBoldFace = new System.Windows.Media.Typeface(typeface.FontFamily, typeface.Style, FontWeights.Bold, typeface.Stretch);
                    this.cachedTypeFace.Add(typeface, cachedBoldFace);
                }
                props.SetTypeface(cachedBoldFace);
                // props.SetTextDecorations(System.Windows.TextDecorations.Underline);
                // props.SetFontHintingEmSize(1d);
                // props.SetFontRenderingEmSize(1d);
            }
        }
    }
}
