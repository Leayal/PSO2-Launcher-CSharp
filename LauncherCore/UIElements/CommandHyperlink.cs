using System.Windows;
using System.Windows.Documents;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    sealed class CommandHyperlink : Hyperlink
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

        public CommandHyperlink(Inline inline) : base(inline) 
        {
            this.CustomClickHandler = false;
        }

        public CommandHyperlink(Inline inline, TextPointer insertPosition) : base(inline, insertPosition) 
        {
            this.CustomClickHandler = false;
        }

        public CommandHyperlink(TextPointer start, TextPointer end) : base(start, end) 
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
