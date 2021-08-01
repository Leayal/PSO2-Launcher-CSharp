using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Loader;
using System.Reflection.PortableExecutable;
using System.Xml;
using System.Net.Http;

namespace Leayal.PSO2Launcher.RSS
{
    /// <summary>Provides a class to load assemblies which will fetch or parse feeds.</summary>
    /// <remarks>This class is not thread-safe.</remarks>
    public class RSSLoader : IDisposable, IRSSLoader
    {
        private readonly static Type typeofRSSFeed = typeof(RSSFeed);
        private readonly static Type typeofIRSSLoader = typeof(IRSSLoader);

        private readonly ObservableCollection<RSSFeed> collection;
        private readonly RSSAssemblyLoadContext assemblies;
        private readonly bool unloadable;
        private readonly HttpClient webclient;

        public RSSLoader() : this(false) { }

        public RSSLoader(bool unloadable)
        {
            this.unloadable = unloadable;
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
            this.assemblies = new RSSAssemblyLoadContext(unloadable);
            this.collection = new ObservableCollection<RSSFeed>();
            this.collection.CollectionChanged += Collection_CollectionChanged;
        }

        private void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.ItemsChanged?.Invoke(this, e);
        }

        public ICollection<RSSFeed> Items => this.collection;

        public event NotifyCollectionChangedEventHandler ItemsChanged;

        private Assembly LoadFrom(string filename)
        {
            Assembly asm;
            try
            {
                asm = this.assemblies.LoadFromNativeImagePath(filename, filename);
            }
            catch (BadImageFormatException)
            {
                asm = this.assemblies.LoadFromAssemblyPath(filename);
            }
            return asm;
        }

        public IReadOnlyList<RSSFeed> Load(string filename)
        {
            return CreateFromAssemby(this.LoadFrom(filename));
        }

        public IReadOnlyList<RSSFeed> Load(params string[] filenames)
        {
            var asms = new List<Assembly>(filenames.Length);
            foreach (var filename in filenames)
            {
                asms.Add(this.LoadFrom(filename));
            }

            if (asms.Count != 0)
            {
                return this.CreateFromAssemblies(asms);
            }
            else
            {
                return new ReadOnlyCollection<RSSFeed>(Array.Empty<RSSFeed>());
            }
        }

        public IReadOnlyList<RSSFeed> Load(IEnumerable<string> filenames)
        {
            var asms = new List<Assembly>();
            foreach (var filename in filenames)
            {
                asms.Add(this.LoadFrom(filename));
            }

            if (asms.Count != 0)
            {
                return this.CreateFromAssemblies(asms);
            }
            else
            {
                return new ReadOnlyCollection<RSSFeed>(Array.Empty<RSSFeed>());
            }
        }

        private IReadOnlyList<RSSFeed> CreateFromAssemblies(List<Assembly> list)
        {
            var results = new List<RSSFeed>();
            foreach (var asm in list)
            {
                results.AddRange(CreateFromAssemby(asm));
            }
            if (results.Count != 0)
            {
                return new ReadOnlyCollection<RSSFeed>(results);
            }
            else
            {
                return new ReadOnlyCollection<RSSFeed>(Array.Empty<RSSFeed>());
            }
        }

        private IReadOnlyList<RSSFeed> CreateFromAssemby(Assembly asm)
        {
            var feeds = new List<RSSFeed>();
            var types = asm.GetTypes();
            var targetConstructor = new object[] { (IRSSLoader)this };
            var targetConstructorTypes = new Type[] { typeofIRSSLoader };
            foreach (var t in types)
            {
                if (t.IsSubclassOf(typeofRSSFeed))
                {
                    var constructor = t.GetConstructor(targetConstructorTypes);
                    if (constructor != null)
                    {
                        var obj = constructor.Invoke(targetConstructor);
                        if (obj is RSSFeed feed)
                        {
                            feeds.Add(feed);
                            feed.webClient = this.webclient;
                            this.collection.Add(feed);
                        }
                        continue;
                    }
                    else
                    {
                        constructor = t.GetConstructor(Type.EmptyTypes);
                        if (constructor != null)
                        {
                            var obj = constructor.Invoke(null);
                            if (obj is RSSFeed feed)
                            {
                                feeds.Add(feed);
                                feed.webClient = this.webclient;
                                this.collection.Add(feed);
                            }
                            continue;
                        }
                    }
                }
            }
            if (feeds.Count != 0)
            {
                return new ReadOnlyCollection<RSSFeed>(feeds);
            }
            else
            {
                return new ReadOnlyCollection<RSSFeed>(Array.Empty<RSSFeed>());
            }
        }

        /// <summary>The loader is not unloadable.</summary>
        public void Unload()
        {
            if (!this.unloadable) throw new InvalidOperationException();
            this.assemblies.Unload();
        }

        public void Dispose()
        {
            if (this.unloadable)
            {
                this.assemblies.Unload();
            }
        }
    }
}
