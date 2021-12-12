using System;
using System.Windows;
using Leayal.Shared.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for Prompt_PSO2BinIsNotSet.xaml
    /// </summary>
    public partial class Prompt_PSO2BinIsNotSet : MetroWindowEx
    {
        public Prompt_PSO2BinIsNotSet()
        {
            InitializeComponent();
        }

        private void ButtonSelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            this.CustomDialogResult = false;
            this.DialogResult = false;
        }

        private void ButtonDeployRequest_Click(object sender, RoutedEventArgs e)
        {
            this.CustomDialogResult = true;
            this.DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = null;
        }
    }
}
