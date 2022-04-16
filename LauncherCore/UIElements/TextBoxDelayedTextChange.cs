using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    class TextBoxDelayedTextChange : TextBox
    {
        private bool _isDelayTextChangeEvent;
        private readonly DispatcherTimer _delayTextChanged;
        private TextChangedEventArgs _e;
        private string _textBeforeFiringEvent;

        public string TextBeforeDelay => this._textBeforeFiringEvent;
        public int DelayTimeTextChanged
        {
            get => Convert.ToInt32(this._delayTextChanged.Interval.TotalMilliseconds);
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                this._delayTextChanged.Interval = TimeSpan.FromMilliseconds(value);
            }
        }
        public bool IsDelayTextChangeEvent
        {
            get => this._isDelayTextChangeEvent;
            set
            {
                if (value != this._isDelayTextChangeEvent)
                {
                    if (this._isDelayTextChangeEvent == true)
                    {
                        this._isDelayTextChangeEvent = value;
                        this.RaiseDelayedTextChangedEvent();
                    }
                    else
                    {
                        this._isDelayTextChangeEvent = value;
                    }
                }
            }
        }

        public TextBoxDelayedTextChange() : base()
        {
            this._e = null;
            this._textBeforeFiringEvent = string.Empty;
            this._delayTextChanged = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, this.DelayTextChanged_Tick, this.Dispatcher);
            this._delayTextChanged.Stop();
            this.IsDelayTextChangeEvent = true;
            this.Style = (System.Windows.Style)App.Current.TryFindResource(typeof(TextBox));
        }

        private void DelayTextChanged_Tick(object sender, EventArgs e)
            => this.RaiseDelayedTextChangedEvent();

        private void RaiseDelayedTextChangedEvent()
        {
            this._delayTextChanged.Stop();
            var ev = Interlocked.Exchange(ref this._e, null);
            if (ev != null)
            {
                base.OnTextChanged(ev);
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (this.IsDelayTextChangeEvent)
            {
                this._delayTextChanged.Stop();
                if (Interlocked.Exchange(ref this._e, e) == null)
                {
                    this._textBeforeFiringEvent = this.Text;
                }
                this._delayTextChanged.Start();
            }
            else
            {
                base.OnTextChanged(e);
            }
        }
    }
}
