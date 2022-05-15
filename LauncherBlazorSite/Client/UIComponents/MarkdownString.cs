using System.IO;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace LauncherBlazorSite.Client.UIComponents
{
    public class MarkdownString : ComponentBase
    {
        /// <summary>
        /// Gets or sets the path to the Markdown file.
        /// </summary>
        [Parameter]
        public string? Markdown { get; set; }

        private MarkupString _markupString = new MarkupString();

        /// <inheritdoc/>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);
            builder.AddContent(0, _markupString);
        }

        /// <inheritdoc/>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (string.IsNullOrWhiteSpace(this.Markdown))
            {
                _markupString = new MarkupString(this.Markdown ?? string.Empty);
            }
            else
            {
                _markupString = new MarkupString(Markdig.Markdown.ToHtml(this.Markdown, new MarkdownPipelineBuilder().UseEmojiAndSmiley().UseAdvancedExtensions().Build()));
            }
        }
    }
}
