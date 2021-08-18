using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Leayal.PSO2Launcher.Core.Classes;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for TroubleshootingAnswerPresenter.xaml
    /// </summary>
    public partial class TroubleshootingAnswerPresenter : ListView
    {
        public readonly static DependencyProperty AnswersSourceProperty = DependencyProperty.Register("AnswersSource", typeof(PSO2TroubleshootingAnswer), typeof(TroubleshootingAnswerPresenter), new PropertyMetadata(null, (obj, e) =>
        {
            if (obj is TroubleshootingAnswerPresenter presenter)
            {
                presenter.RestartQuestioning();
            }
        }));
        public PSO2TroubleshootingAnswer AnswersSource
        {
            get => (PSO2TroubleshootingAnswer)this.GetValue(AnswersSourceProperty);
            set => this.SetValue(AnswersSourceProperty, value);
        }

        private readonly static DependencyPropertyKey CurrentAnswerTitlePropertyKey = DependencyProperty.RegisterReadOnly("CurrentAnswerTitle", typeof(string), typeof(TroubleshootingAnswerPresenter), new PropertyMetadata(string.Empty, (obj, e) =>
        {
            if (obj is TroubleshootingAnswerPresenter presenter)
            {
                presenter.RaiseEvent(new RoutedEventArgs(CurrentAnswerTitleChangedEvent));
            }
        }));
        public readonly static DependencyProperty CurrentAnswerTitleProperty = CurrentAnswerTitlePropertyKey.DependencyProperty;
        public string CurrentAnswerTitle => (string)this.GetValue(CurrentAnswerTitleProperty);
        public readonly static RoutedEvent CurrentAnswerTitleChangedEvent = EventManager.RegisterRoutedEvent("CurrentAnswerTitleChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TroubleshootingAnswerPresenter));
        public event RoutedEventHandler CurrentAnswerTitleChanged
        {
            add => this.AddHandler(CurrentAnswerTitleChangedEvent, value);
            remove => this.RemoveHandler(CurrentAnswerTitleChangedEvent, value);
        }

        private readonly static DependencyPropertyKey CurrentAnswerTooltipTextPropertyKey = DependencyProperty.RegisterReadOnly("CurrentAnswerTooltipText", typeof(string), typeof(TroubleshootingAnswerPresenter), new PropertyMetadata(string.Empty, (obj, e) =>
        {
            if (obj is TroubleshootingAnswerPresenter presenter)
            {
                presenter.RaiseEvent(new RoutedEventArgs(CurrentAnswerTooltipTextChangedEvent));
            }
        }));
        public readonly static DependencyProperty CurrentAnswerTooltipTextProperty = CurrentAnswerTooltipTextPropertyKey.DependencyProperty;
        public string CurrentAnswerTooltipText => (string)this.GetValue(CurrentAnswerTooltipTextProperty);
        public readonly static RoutedEvent CurrentAnswerTooltipTextChangedEvent = EventManager.RegisterRoutedEvent("CurrentAnswerTooltipTextChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TroubleshootingAnswerPresenter));
        public event RoutedEventHandler CurrentAnswerTooltipTextChanged
        {
            add => this.AddHandler(CurrentAnswerTooltipTextChangedEvent, value);
            remove => this.RemoveHandler(CurrentAnswerTooltipTextChangedEvent, value);
        }

        private readonly static DependencyProperty CurrentAnswerProperty = DependencyProperty.Register("CurrentAnswer", typeof(PSO2TroubleshootingAnswer), typeof(TroubleshootingAnswerPresenter), new PropertyMetadata(null, (obj, e) =>
        {
            if (obj is TroubleshootingAnswerPresenter presenter)
            {
                if (e.OldValue is PSO2TroubleshootingAnswer oldanswer)
                {
                    foreach (var oldone in oldanswer)
                    {
                        oldone.Selected -= presenter.OnSelectAnswer;
                    }
                }
                if (e.NewValue is PSO2TroubleshootingAnswer newanswer)
                {
                    presenter.SetValue(CurrentAnswerTitlePropertyKey, newanswer.Title);
                    presenter.SetValue(CurrentAnswerTooltipTextPropertyKey, newanswer.TooltipText);

                    foreach (var newone in newanswer)
                    {
                        newone.Selected += presenter.OnSelectAnswer;
                    }
                    presenter.ItemsSource = newanswer;
                    presenter.answersstack.Push(newanswer);
                    presenter.SetValue(CanGoBackPropertyKey, presenter.answersstack.Count > 1);
                }
                presenter.RaiseEvent(new RoutedEventArgs(CurrentAnswerChangedEvent));
            }
        }));
        public PSO2TroubleshootingAnswer CurrentAnswer
        {
            get => (PSO2TroubleshootingAnswer)this.GetValue(CurrentAnswerProperty);
            set => this.SetValue(CurrentAnswerProperty, value);
        }
        public readonly static RoutedEvent CurrentAnswerChangedEvent = EventManager.RegisterRoutedEvent("CurrentAnswerChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TroubleshootingAnswerPresenter));
        public event RoutedEventHandler CurrentAnswerChanged
        {
            add => this.AddHandler(CurrentAnswerChangedEvent, value);
            remove => this.RemoveHandler(CurrentAnswerChangedEvent, value);
        }

        private readonly static DependencyPropertyKey CanGoBackPropertyKey = DependencyProperty.RegisterReadOnly("CanGoBack", typeof(bool), typeof(TroubleshootingAnswerPresenter), new PropertyMetadata(false));
        public readonly static DependencyProperty CanGoBackProperty = CanGoBackPropertyKey.DependencyProperty;
        public bool CanGoBack => (bool)this.GetValue(CanGoBackProperty);

        private readonly Action<PSO2TroubleshootingAnswer> OnSelectAnswer;
        private readonly Stack<PSO2TroubleshootingAnswer> answersstack;

        public TroubleshootingAnswerPresenter()
        {
            this.answersstack = new Stack<PSO2TroubleshootingAnswer>();
            this.OnSelectAnswer = new Action<PSO2TroubleshootingAnswer>(selectedanswer =>
            {
                this.SelectedItem = selectedanswer;
            });
            InitializeComponent();
        }

        public void RestartQuestioning()
        {
            this.answersstack.Clear();
            var answer = this.AnswersSource;
            if (answer != null)
            {
                this.CurrentAnswer = answer;
            }
        }

        public void GoBackPreviousAnswer()
        {
            // Yes, pop twice
            if (this.answersstack.TryPop(out var currentanswer) && this.answersstack.TryPop(out var previousanswer))
            {
                this.CurrentAnswer = previousanswer;
            }
            else
            {
                this.RestartQuestioning();
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radiobtn)
            {
                var datacontext = radiobtn.DataContext;

                if (datacontext is PSO2TroubleshootingAnswer answer)
                {
                    answer.Select();
                }

                foreach (var item in this.Items)
                {
                    if (item is RadioButton otherradiobtn)
                    {
                        if (otherradiobtn != radiobtn)
                        {
                            otherradiobtn.IsChecked = false;
                        }
                    }
                }
            }
        }
    }
}
