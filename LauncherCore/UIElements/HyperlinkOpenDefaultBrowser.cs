using System;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    class HyperlinkOpenDefaultBrowser : Hyperlink
    {
        public HyperlinkOpenDefaultBrowser() : base() { }

        public HyperlinkOpenDefaultBrowser(Inline inline) : base(inline) { }

        public HyperlinkOpenDefaultBrowser(Inline inline, TextPointer insertPosition) : base(inline, insertPosition) { }

        public HyperlinkOpenDefaultBrowser(TextPointer start, TextPointer end) : base(start, end) { }

        protected override void OnClick()
        {
            var uri = this.NavigateUri;
            if (uri != null && uri.IsAbsoluteUri)
            {
                Task.Factory.StartNew(StartUrl, uri);
            }
        }

        private static void StartUrl(object? obj)
        {
            if (obj == null) return;
            try
            {
                Shared.Windows.WindowsExplorerHelper.OpenUrlWithDefaultBrowser((Uri)obj);
            }
            catch { }
        }
    }
}
