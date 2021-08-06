using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    public interface IRSSLoader
    {
        /// <summary>All found subclass types of <seealso cref="RSSFeedHandler"/> from assemblies.</summary>
        IReadOnlyCollection<Type> RegisteredHandlers { get; }

        /// <summary>All found class types which implements the <seealso cref="IRSSFeedChannelDownloader"/> interface from assemblies.</summary>
        IReadOnlyCollection<IRSSFeedChannelDownloader> RegisteredDownloadHandlers { get; }

        /// <summary>All found class types which implements the <seealso cref="IRSSFeedChannelParser"/> interface from assemblies.</summary>
        IReadOnlyCollection<IRSSFeedChannelParser> RegisteredParserHandlers { get; }

        /// <summary>All found class types which implements the <seealso cref="IRSSFeedItemCreator"/> interface from assemblies.</summary>
        IReadOnlyCollection<IRSSFeedItemCreator> RegisteredFeedItemCreatorHandlers { get; }

        IEnumerable<Type> GetRSSFeedHandlerSuggesstion(Uri url);

        IEnumerable<IRSSFeedChannelDownloader> GetDownloadHandlerSuggesstion(Uri url);

        IEnumerable<IRSSFeedItemCreator> GetItemCreatorHandlerSuggesstion(Uri url);

        IEnumerable<IRSSFeedChannelParser> GetParserHandlerSuggesstion(Uri url);

        Type GetRSSFeedHandlerTypeByTypeName(string name);

        IRSSFeedChannelDownloader GetDownloadHandlerByTypeName(string name);

        IRSSFeedItemCreator GetFeedItemCreatorHandlerByTypeName(string name);

        IRSSFeedChannelParser GetParserHandlerByTypeName(string name);

        RSSFeedHandler CreateHandlerFromUri(Uri url, string handlerTypeName);

        /// <summary>Create a default handler from the given <seealso cref="Uri"/></summary>
        /// <param name="url">The feed channel.</param>
        RSSFeedHandler CreateHandlerFromUri(Uri url);

        /// <summary>Create a generic handler from the given <seealso cref="Uri"/> with given handlers.</summary>
        /// <param name="url">The feed channel.</param>
        /// <param name="downloadHandler">The feed download handler to handle downloading the feed data.</param>
        /// <param name="parser">The parser handler to handle parsing the feed data.</param>
        /// <param name="creator">The feed item creator handler</param>
        RSSFeedHandler CreateHandlerFromUri(Uri url, IRSSFeedChannelDownloader downloadHandler, IRSSFeedChannelParser parser, IRSSFeedItemCreator creator);

        /// <summary>Load an assembly from the path and search for all <seealso cref="RSSFeedHandler"/>-inherited classes.</summary>
        /// <param name="filename">Path to the assembly</param>
        void Load(string filename);

        /// <summary>Load an assembly from the paths and search for all <seealso cref="RSSFeedHandler"/>-inherited classes.</summary>
        /// <param name="filenames">Paths to the assembly</param>
        void Load(params string[] filenames);

        /// <summary>Load an assembly from the paths and search for all <seealso cref="RSSFeedHandler"/>-inherited classes.</summary>
        /// <param name="filenames">Paths to the assembly</param>
        void Load(IEnumerable<string> filenames);
    }
}
