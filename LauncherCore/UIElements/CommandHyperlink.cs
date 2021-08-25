using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    class CommandHyperlink : Hyperlink
    {
        public readonly static DependencyProperty CustomClickHandlerProperty = DependencyProperty.Register("CustomClickHandler", typeof(bool), typeof(CommandHyperlink), new PropertyMetadata(false));
        public bool CustomClickHandler
        {
            get => (bool)this.GetValue(CustomClickHandlerProperty);
            set => this.SetValue(CustomClickHandlerProperty, value);
        }

        public CommandHyperlink() : base()
        {
            this.CustomClickHandler = false;
        }

        protected override void OnClick()
        {
            if (this.CustomClickHandler)
            {
                base.OnClick();
            }
            else if (this.NavigateUri != null)
            {
                App.Current.ExecuteCommandUrl(this.NavigateUri);
            }
        }
    }
}
