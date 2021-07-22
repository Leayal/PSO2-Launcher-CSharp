using Leayal.PSO2Launcher.Core.Classes.PSO2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    public delegate void ButtonScanFixGameDataClickRoutedEventHander(object sender, ButtonScanFixGameDataClickRoutedEventArgs e);
    public class ButtonScanFixGameDataClickRoutedEventArgs : RoutedEventArgs
    {
        public GameClientSelection SelectedMode { get; }

        public ButtonScanFixGameDataClickRoutedEventArgs(GameClientSelection mode, RoutedEvent e) : base(e)
        {
            this.SelectedMode = mode;
        }
    }
}
