using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Reflection;
using System.Net.Http;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    /// <summary>Provides a class to load assemblies which will fetch or parse feeds.</summary>
    /// <remarks>This class is not thread-safe.</remarks>
    public class RSSLoader : IDisposable, IRSSLoader
    {
        private readonly static Type typeofUri = typeof(Uri),
            typeofRSSFeed = typeof(RSSFeedHandler),
            typeofIRSSFeedChannelDownloader = typeof(IRSSFeedChannelDownloader),
            typeofIRSSFeedChannelParser = typeof(IRSSFeedChannelParser),
            typeofIRSSFeedItemCreator = typeof(IRSSFeedItemCreator);
        private readonly static Type[] constructorTarget = { typeofUri };

        private readonly RSSAssemblyLoadContext loadcontext;

        private readonly Dictionary<string, Type> registeredhandlers;
        private readonly Dictionary<string, IRSSFeedChannelDownloader> registereddownloadhandlers;
        private readonly Dictionary<string, IRSSFeedChannelParser> registeredparserhandlers;
        private readonly Dictionary<string, IRSSFeedItemCreator> registeredmakerhandlers;
        private readonly ConcurrentDictionary<string, Assembly> assemblies;
        internal readonly HttpClient webclient;

        public RSSLoader()
        {
            this.webclient = new HttpClient(new SocketsHttpHandler()
            {
                ConnectTimeout = TimeSpan.FromSeconds(5),
                DefaultProxyCredentials = null,
                Credentials = null,
                EnableMultipleHttp2Connections = true,
                AllowAutoRedirect = true,
                UseProxy = false,
                UseCookies = true,
                Proxy = null,
                MaxAutomaticRedirections = 10
            }, true);
            this.loadcontext = new RSSAssemblyLoadContext(Assembly.GetExecutingAssembly().Location);
            this.assemblies = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            this.registeredhandlers = new Dictionary<string, Type>(StringComparer.Ordinal);
            this.registereddownloadhandlers = new Dictionary<string, IRSSFeedChannelDownloader>(StringComparer.Ordinal);
            this.registeredparserhandlers = new Dictionary<string, IRSSFeedChannelParser>(StringComparer.Ordinal);
            this.registeredmakerhandlers = new Dictionary<string, IRSSFeedItemCreator>(StringComparer.Ordinal);
        }

        public IReadOnlyCollection<Type> RegisteredHandlers => this.registeredhandlers.Values;
        public IReadOnlyCollection<IRSSFeedChannelDownloader> RegisteredDownloadHandlers => this.registereddownloadhandlers.Values;
        public IReadOnlyCollection<IRSSFeedChannelParser> RegisteredParserHandlers => this.registeredparserhandlers.Values;
        public IReadOnlyCollection<IRSSFeedItemCreator> RegisteredFeedItemCreatorHandlers => this.registeredmakerhandlers.Values;

        private void LoadFrom(string filename)
        {
            this.assemblies.AddOrUpdate(filename, (path) =>
            {
                if (Shared.FileHelper.IsNotExistsOrZeroLength(path))
                {
                    return null;
                }
                try
                {
                    var assembly = this.loadcontext.LoadFromNativeImagePath(path, path);
                    CreateFromAssemby(assembly);
                    return assembly;
                }
                catch 
                {
                    try
                    {
                        var assembly = this.loadcontext.LoadFromAssemblyPath(path);
                        CreateFromAssemby(assembly);
                        return assembly;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }, (path, asm) =>
            {
                if (asm == null)
                {
                    if (Shared.FileHelper.IsNotExistsOrZeroLength(path))
                    {
                        return null;
                    }
                    try
                    {
                        var assembly = this.loadcontext.LoadFromNativeImagePath(path, path);
                        CreateFromAssemby(assembly);
                        return assembly;
                    }
                    catch
                    {
                        try
                        {
                            var assembly = this.loadcontext.LoadFromAssemblyPath(path);
                            CreateFromAssemby(assembly);
                            return assembly;
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    return asm;
                }
            });
        }

        public void Load(string filename)
        {
            this.LoadFrom(filename);
        }

        public void Load(params string[] filenames)
        {
            foreach (var filename in filenames)
            {
                this.LoadFrom(filename);
            }
        }

        public void Load(IEnumerable<string> filenames)
        {
            foreach (var filename in filenames)
            {
                this.LoadFrom(filename);
            }
        }

        private void CreateFromAssemby(Assembly asm)
        {
            if (asm == null) return;
            var types = asm.GetTypes();
            foreach (var t in types)
            {
                string name = t.FullName;
                if (!t.Equals(typeofRSSFeed) && t.IsAssignableTo(typeofRSSFeed))
                {
                    var constructor = t.GetConstructor(constructorTarget);
                    if (constructor != null)
                    {
                        this.registeredhandlers.Add(name, t);
                    }
                }
                else
                {
                    var interfaces = t.GetInterfaces();
                    object obj = null;
                    if (Array.IndexOf(interfaces, typeofIRSSFeedChannelDownloader) != -1)
                    {
                        var constructor = t.GetConstructor(Type.EmptyTypes);
                        if (constructor != null && constructor.Invoke(null) is IRSSFeedChannelDownloader downloadhandler)
                        {
                            obj = downloadhandler;
                            this.registereddownloadhandlers.Add(name, downloadhandler);
                        }
                    }
                    if (obj == null)
                    {
                        if (Array.IndexOf(interfaces, typeofIRSSFeedChannelParser) != -1)
                        {
                            var constructor = t.GetConstructor(Type.EmptyTypes);
                            if (constructor != null && constructor.Invoke(null) is IRSSFeedChannelParser parserhandler)
                            {
                                obj = parserhandler;
                                this.registeredparserhandlers.Add(name, parserhandler);
                            }
                        }
                    }
                    else if (obj is IRSSFeedChannelParser parser)
                    {
                        this.registeredparserhandlers.Add(name, parser);
                    }

                    if (obj == null)
                    {
                        if (Array.IndexOf(interfaces, typeofIRSSFeedItemCreator) != -1)
                        {
                            var constructor = t.GetConstructor(Type.EmptyTypes);
                            if (constructor != null && constructor.Invoke(null) is IRSSFeedItemCreator creatorhandler)
                            {
                                this.registeredmakerhandlers.Add(name, creatorhandler);
                            }
                        }
                    }
                    else if (obj is IRSSFeedItemCreator creator)
                    {
                        this.registeredmakerhandlers.Add(name, creator);
                    }
                }
            }
        }

        public IRSSFeedChannelDownloader GetDownloadHandlerByTypeName(string name)
        {
            if (this.registereddownloadhandlers.TryGetValue(name, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public IRSSFeedItemCreator GetFeedItemCreatorHandlerByTypeName(string name)
        {
            if (this.registeredmakerhandlers.TryGetValue(name, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public IRSSFeedChannelParser GetParserHandlerByTypeName(string name)
        {
            if (this.registeredparserhandlers.TryGetValue(name, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<IRSSFeedChannelDownloader> GetDownloadHandlerSuggesstion(Uri url)
        {
            foreach (var keypair in this.registereddownloadhandlers)
            {
                var item = keypair.Value;
                if (item.CanHandleDownloadChannel(url))
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<IRSSFeedItemCreator> GetItemCreatorHandlerSuggesstion(Uri url)
        {
            foreach (var keypair in this.registeredmakerhandlers)
            {
                var item = keypair.Value;
                if (item.CanHandleFeedItemCreation(url))
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<IRSSFeedChannelParser> GetParserHandlerSuggesstion(Uri url)
        {
            foreach (var keypair in this.registeredparserhandlers)
            {
                var item = keypair.Value;
                if (item.CanHandleParseFeedData(url))
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<Type> GetRSSFeedHandlerSuggesstion(Uri url)
        {
            foreach (var keypair in this.registeredhandlers)
            {
                var item = keypair.Value;
                if (SupportUriHostAttribute.TryGet(item, out var host))
                {
                    if (!string.Equals(host, url.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }
                if (SupportUriRegexAttribute.TryGet(item, out var regex))
                {
                    if (url.IsAbsoluteUri)
                    {
                        if (!regex.IsMatch(url.AbsoluteUri))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (!regex.IsMatch(url.ToString()))
                        {
                            continue;
                        }
                    }
                }
                yield return item;
            }
        }

        public RSSFeedHandler CreateHandlerFromUri(Uri url)
            => this.CreateHandlerFromUri(url, null, null, null);

        public RSSFeedHandler CreateHandlerFromUri(Uri url, string handlerTypeName)
        {
            if (this.registeredhandlers.TryGetValue(handlerTypeName, out var t))
            {
                var ctor = t.GetConstructor(new Type[] { typeofUri });
                if (ctor != null && ctor.Invoke(new object[] { url }) is RSSFeedHandler handler)
                {
                    handler.loader = this;
                    return handler;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                throw new ArgumentException(nameof(handlerTypeName));
            }
        }

        public Type GetRSSFeedHandlerTypeByTypeName(string name)
        {
            if (this.registeredhandlers.TryGetValue(name, out var t))
            {
                return t;
            }
            else
            {
                return null;
            }
        }

        public RSSFeedHandler CreateHandlerFromUri(Uri url, IRSSFeedChannelDownloader downloadHandler, IRSSFeedChannelParser parser, IRSSFeedItemCreator creator)
        {
            RSSFeedHandler result;
            if (downloadHandler == null && parser == null && creator == null)
            {
                result = new DefaultRSSFeedHandler(url);
            }
            else
            {
                if (downloadHandler == null)
                {
                    downloadHandler = RSSFeedHandler.Default;
                }
                else if (!downloadHandler.CanHandleDownloadChannel(url))
                {
                    throw new ArgumentException(nameof(downloadHandler));
                }
                if (parser == null)
                {
                    parser = RSSFeedHandler.Default;
                }
                else if (!parser.CanHandleParseFeedData(url))
                {
                    throw new ArgumentException(nameof(parser));
                }
                if (creator == null)
                {
                    creator = RSSFeedHandler.Default;
                }
                else if (!creator.CanHandleFeedItemCreation(url))
                {
                    throw new ArgumentException(nameof(creator));
                }

                result = new GenericRSSFeedHandler(url, downloadHandler, parser, creator);
            }

            result.loader = this;

            return result;
        }

        /// <summary>The loader is not unloadable.</summary>
        public void UnloadAll()
        {
            this.registeredhandlers.Clear();
            this.registereddownloadhandlers.Clear();
            this.registeredparserhandlers.Clear();
            this.registeredmakerhandlers.Clear();
            this.assemblies.Clear();
            this.loadcontext.Unload();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.UnloadAll();
            this.webclient.CancelPendingRequests();
            this.webclient.Dispose();
        }

        ~RSSLoader()
        {
            this.Dispose(false);
        }
    }
}
