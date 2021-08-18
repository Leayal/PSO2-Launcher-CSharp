using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Leayal.PSO2.Installer;
using Leayal.PSO2Launcher.Core.Classes;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for PSO2TroubleshootingWindow.xaml
    /// </summary>
    public partial class PSO2TroubleshootingWindow : MetroWindowEx
    {
        private readonly static Lazy<PSO2TroubleshootingAnswer> RootAnswer;

        static PSO2TroubleshootingWindow()
        {
            RootAnswer = new Lazy<PSO2TroubleshootingAnswer>(() =>
            {
                return new PSO2TroubleshootingAnswer("root", "What sort of problem you're having with the game?", string.Empty, new List<PSO2TroubleshootingAnswer>(1)
                {
                    new PSO2TroubleshootingAnswer("diag_requirements", "I have the PSO2 JP game client. But nothing happens when I try to start the game.", "I have the PSO2 JP game client. However, when I tried to start the game with the official game launcher, PSO2 Tweaker or this game launcher, nothing happened.", null),
                    new PSO2TroubleshootingAnswer("diag_black_screen_stuck_startup", "I have the PSO2 JP game client. The game can be launched and I can hear background music but it's stuck with the black screen forever.", "The game can be launched but it's stuck with black screen while the background music is playing.", null)
                });
            });
        }

        private readonly static DependencyPropertyKey IsInLoadingPropertyKey = DependencyProperty.RegisterReadOnly("IsInLoading", typeof(bool), typeof(PSO2TroubleshootingWindow), new PropertyMetadata(true, (obj, e) =>
        {
            if (obj is PSO2TroubleshootingWindow window)
            {
                window.CoerceValue(IsInResultProperty);
            }
        }));
        public readonly static DependencyProperty IsInLoadingProperty = IsInLoadingPropertyKey.DependencyProperty;
        public bool IsInLoading => (bool)this.GetValue(IsInLoadingProperty);

        private readonly static DependencyPropertyKey IsInResultPropertyKey = DependencyProperty.RegisterReadOnly("IsInResult", typeof(bool), typeof(PSO2TroubleshootingWindow), new PropertyMetadata(false, null, (obj, val) =>
        {
            if (obj is PSO2TroubleshootingWindow presenter)
            {
                if (!presenter.IsInLoading)
                {
                    return val;
                }
            }
            return false;
        }));
        public readonly static DependencyProperty IsInResultProperty = IsInResultPropertyKey.DependencyProperty;
        public bool IsInResult => (bool)this.GetValue(IsInResultProperty);

        public PSO2TroubleshootingWindow()
        {
            InitializeComponent();

            this.AnswerSelectionList.AnswersSource = RootAnswer.Value;
        }

        // Select category
        // -> Verify component(s) or environment(s) to filter answers
        // -> Asks what happened (Gets user select answer(s))
        // -> Filter fix suggestion(s)
        // -> Begin giving fix suggestion(s) or auto-fix (if user allows, but this probably requires admin privileges).

        // Hah!! Too much work.
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (!this.AnswerSelectionList.CanGoBack)
            {
                SystemCommands.CloseWindow(this);
            }
            else
            {
                this.SetValue(IsInResultPropertyKey, false);
                this.AnswerSelectionList.GoBackPreviousAnswer();
            }
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsInResult)
            {
                SystemCommands.CloseWindow(this);
            }
            else
            {
                var selecteditem = this.AnswerSelectionList.SelectedItem;
                if (selecteditem is PSO2TroubleshootingAnswer answer)
                {
                    this.AnswerSelectionList.CurrentAnswer = answer;
                }
            }
        }

        private async void AnswerSelectionList_CurrentAnswerChanged(object sender, RoutedEventArgs e)
        {
            this.SetValue(IsInLoadingPropertyKey, true);
            try
            {
                var answer = this.AnswerSelectionList.CurrentAnswer;
                if (answer != null)
                {
                    if (answer.Name == "diag_requirements")
                    {
                        await this.DiagnoseRequirements();
                    }
                    else if (answer.Name == "diag_black_screen_stuck_startup")
                    {
                        this.DiagnoseGameFiles();
                    }
                    // answer.
                }
            }
            finally
            {
                this.SetValue(IsInLoadingPropertyKey, false);
            }
        }

        private void DiagnoseGameFiles()
        {
            this.ResultBox.Document.Blocks.Clear();
            this.ResultBox.Document.Blocks.Add(new Paragraph(new Run("It seems that the game has missing or damaged data file(s). I recommend you to scan for missing or damaged files to see whether all files are okay.")));

            this.SetValue(IsInResultPropertyKey, true);
        }

        private async Task DiagnoseRequirements()
        {
            var hasDirectX = await Task.Run(Requirements.HasDirectX11);
            var vc14_x64 = await Task.Run(() => Requirements.GetVC14RedistVersion(true));
            var vc14_x86 = await Task.Run(() => Requirements.GetVC14RedistVersion(false));

            var list = new List<Paragraph>(4);
            bool isOkay = true;

            Hyperlink link;

            var paragraph = new Paragraph(new Bold(new Run("> DirectX11 (From DirectX Runtime Redistribution June 2010):")));
            if (!hasDirectX)
            {
                // https://www.microsoft.com/en-us/download/details.aspx?id=8109
                isOkay = false;
                paragraph.Inlines.Add(new Run("Not Installed") { Foreground = Brushes.Red });
                link = new Hyperlink(new Run("(Download DirectX setup from Microsoft)")) { NavigateUri = new Uri("https://www.microsoft.com/en-us/download/details.aspx?id=8109") };
                paragraph.Inlines.Add(link);
                link.Click += Hyperlink_Clicked;
            }
            else
            {
                paragraph.Inlines.Add(new Run("Installed") { Foreground = Brushes.Green });
            }
            list.Add(paragraph);

            paragraph = new Paragraph(new Bold(new Run("> Visual C++ 2015~2019 (x64):")));
            switch (vc14_x64)
            {
                case VCRedistVersion.None:
                    isOkay = false;
                    paragraph.Inlines.Add(new Run("Not Installed") { Foreground = Brushes.Red });
                    link = new Hyperlink(new Run("(Download VC2019 from Microsoft)")) { NavigateUri = new Uri("https://aka.ms/vs/16/release/vc_redist.x64.exe") };
                    paragraph.Inlines.Add(link);
                    link.Click += Hyperlink_Clicked;
                    break;
                case VCRedistVersion.VC2019:
                    paragraph.Inlines.Add(new Run("VC++ 2019 Installed") { Foreground = Brushes.Green });
                    break;
                default:
                    isOkay = false;
                    paragraph.Inlines.Add(new Run($"VC++ {vc14_x64.ToString()} Installed") { Foreground = Brushes.Yellow });
                    link = new Hyperlink(new Run("(Recommended to update to VC2019 from Microsoft)")) { NavigateUri = new Uri("https://aka.ms/vs/16/release/vc_redist.x64.exe") };
                    paragraph.Inlines.Add(link);
                    link.Click += Hyperlink_Clicked;
                    break;
            }
            list.Add(paragraph);

            paragraph = new Paragraph(new Bold(new Run("> Visual C++ 2015~2019 (x86):")));
            switch (vc14_x86)
            {
                case VCRedistVersion.None:
                    isOkay = false;
                    paragraph.Inlines.Add(new Run("Not Installed") { Foreground = Brushes.Red });
                    link = new Hyperlink(new Run("(Download VC2019 from Microsoft)")) { NavigateUri = new Uri("https://aka.ms/vs/16/release/vc_redist.x86.exe") };
                    paragraph.Inlines.Add(link);
                    link.Click += Hyperlink_Clicked;
                    break;
                case VCRedistVersion.VC2019:
                    paragraph.Inlines.Add(new Run("VC++ 2019 Installed") { Foreground = Brushes.Green });
                    break;
                default:
                    isOkay = false;
                    paragraph.Inlines.Add(new Run($"VC++ {vc14_x86.ToString()} Installed") { Foreground = Brushes.Yellow });
                    link = new Hyperlink(new Run("(Recommended to update to VC2019 from Microsoft)")) { NavigateUri = new Uri("https://aka.ms/vs/16/release/vc_redist.x86.exe") };
                    paragraph.Inlines.Add(link);
                    link.Click += Hyperlink_Clicked;
                    break;
            }
            list.Add(paragraph);

            if (isOkay)
            {
                list.Add(new Paragraph(new Run("All requirements for the game appears to be installed. I recommend you to scan for missing or damaged files to see whether all files are okay.")));
            }
            else
            {
                list.Add(new Paragraph(new Run("Some requirements for the game appears to be missing or not up-to-date. I recommend you to install latest version. You can click on the link(s) above to download from Microsoft's server.")));
            }

            this.ResultBox.Document.Blocks.Clear();
            this.ResultBox.Document.Blocks.AddRange(list);

            this.SetValue(IsInResultPropertyKey, true);
        }

        private static void Hyperlink_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link)
            {
                var url = link.NavigateUri;
                if (url != null)
                {
                    if (url.IsAbsoluteUri)
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), "\"" + url.AbsoluteUri + "\"")?.Dispose();
                            }
                            catch
                            {

                            }
                        });
                    }
                }
            }
        }
    }
}
