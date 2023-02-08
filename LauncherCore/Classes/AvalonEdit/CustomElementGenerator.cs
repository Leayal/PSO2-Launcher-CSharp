using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Input;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    class CustomElementGenerator : VisualLineElementGenerator
    {
        private readonly VisualLineElementDataCollection Items;

        /// <summary>
        /// Creates a new LinkElementGenerator.
        /// </summary>
        public CustomElementGenerator() : base()
        {
            this.Items = new VisualLineElementDataCollection();
        }

        public void Clear()
        {
            this.Items.Clear();
        }

        /// <summary>Add a text element to the document.</summary>
        /// <param name="visualLineData">The visual line data to for text element creation.</param>
        public void Add(VisualLineElementData visualLineData)
        {
            this.Items.Add(visualLineData);
        }

        /// <inheritdoc/>
        public override int GetFirstInterestedOffset(int startOffset)
        {
            var itemCount = this.Items.Count;
            if (itemCount != 0)
            {
                for (int i = (startOffset == 0 ? 0 : this.Items.FindFirstIndexGreaterThanOrEqualTo(startOffset)); i < itemCount; i++)
                {
                    var interest = this.Items.Values[i];
                    var absoluteOffsetOfInterest = interest.AbsoluteOffset;
                    if (absoluteOffsetOfInterest >= startOffset)
                    {
                        return absoluteOffsetOfInterest;
                    }
                }
            }

            return -1;
        }

        /// <inheritdoc/>
        public override VisualLineElement? ConstructElement(int offset)
        {
            var itemCount = this.Items.Count;
            if (itemCount != 0)
            {
                for (int i = (offset == 0 ? 0 : this.Items.FindFirstIndexGreaterThanOrEqualTo(offset)); i < itemCount; i++)
                {
                    var elementData = this.Items.Values[i];
                    var absoluteOffsetOfInterest = elementData.AbsoluteOffset;
                    if (absoluteOffsetOfInterest == offset)
                    {
                        return elementData.Construct(this.CurrentContext.VisualLine);
                    }
                }
            }
            return null;
        }
    }
}
