using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    class ShowLocalFileHyperlink : Hyperlink
    {
        public ShowLocalFileHyperlink() : base()
        {
        }

        public ShowLocalFileHyperlink(Inline inline) : base(inline)
        {
        }

        public ShowLocalFileHyperlink(Inline inline, TextPointer insertPosition) : base(inline, insertPosition)
        {
        }

        public ShowLocalFileHyperlink(TextPointer start, TextPointer end) : base(start, end)
        {
        }

        protected override void OnClick()
        {
            if (this.NavigateUri != null && this.NavigateUri.IsFile)
            {
                Leayal.Shared.WindowsExplorerHelper.SelectPathInExplorer(this.NavigateUri.LocalPath);
            }
        }
    }
}
