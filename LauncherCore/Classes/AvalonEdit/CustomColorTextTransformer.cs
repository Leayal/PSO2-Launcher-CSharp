using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    class CustomColorTextTransformer : DocumentColorizingTransformer
    {
        private readonly TextTransformCollection Items;

        public void Clear()
        {
            this.Items.Clear();
        }

        public CustomColorTextTransformer() : base()
        {
            this.Items = new TextTransformCollection();
        }

        /// <summary>Add a text styling to the text transformer.</summary>
        /// <param name="transformData">The transformation data to for text styling.</param>
        public void Add(TextTransformData transformData)
        {
            this.Items.Add(transformData);
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            var itemCount = this.Items.Count;
            if (itemCount != 0)
            {
                var startOffset = line.Offset;
                var lineEndOffset = startOffset + line.Length;
                for (int i = (startOffset == 0 ? 0 : this.Items.FindFirstIndexGreaterThanOrEqualTo(startOffset)); i < itemCount; i++)
                {
                    var transformData = this.Items.Values[i];
                    var item_absoluteOffsetStart = transformData.InlinePlacement.AbsoluteOffset;
                    var item_absoluteOffsetEnd = item_absoluteOffsetStart + transformData.InlinePlacement.Length;
                    if (startOffset <= item_absoluteOffsetStart && item_absoluteOffsetEnd <= lineEndOffset)
                    {
                        this.ChangeLinePart(item_absoluteOffsetStart, item_absoluteOffsetEnd, transformData.ApplyChanges);
                    }
                }
            }
        }
    }
}
