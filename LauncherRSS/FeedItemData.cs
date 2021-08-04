using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    /// <summary>Parsed feed item from a RSS feed channel.</summary>
    public readonly struct FeedItemData
    {
        /// <summary>The item's title.</summary>
        /// <remarks>This will also be used as display name.</remarks>
        public readonly string Title { get; }
        /// <summary>The item's short description.</summary>
        /// <remarks>This will be used as the item's tooltip.</remarks>
        public readonly string? Description { get; }
        /// <summary>The URL to the article on the site.</summary>
        public readonly string Link { get; }
        /// <summary>The date and/or time when the article is published.</summary>
        /// <remarks>Can be null, when null, it means the feed data didn't give this information.</remarks>
        public readonly DateTime? PublishDate { get; }

        /// <summary>Create a new data item.</summary>
        /// <param name="title">The item's title.</param>
        /// <param name="description">The item's short description.</param>
        /// <param name="link">The URL to the article on the site.</param>
        /// <param name="publishdate">The date and/or time when the article is published.</param>
        public FeedItemData(string title, string? description, string link, DateTime? publishdate)
        {
            this.Title = title;
            this.Description = description;
            this.Link = link;
            this.PublishDate = publishdate;
        }

        /// <summary>Create a new data item.</summary>
        /// <param name="title">The item's title.</param>
        /// <param name="description">The item's short description.</param>
        /// <param name="link">The URL to the article on the site.</param>
        public FeedItemData(string title, string? description, string link)
            : this(title, description, link, null) { }
    }
}
