using Leayal.PSO2Launcher.Core.Classes;
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
    public sealed class ButtonScanFixGameDataClickRoutedEventArgs : RoutedEventArgs
    {
        public GameClientSelection SelectedMode { get; }

        public ButtonScanFixGameDataClickRoutedEventArgs(GameClientSelection mode, RoutedEvent e) : base(e)
        {
            this.SelectedMode = mode;
        }
    }

    public delegate void ChangeDefaultGameStartStyleEventHandler(object sender, ChangeDefaultGameStartStyleEventArgs e);
    public sealed class ChangeDefaultGameStartStyleEventArgs : RoutedEventArgs
    {
        public GameStartStyle SelectedStyle { get; }

        public ChangeDefaultGameStartStyleEventArgs(GameStartStyle style, RoutedEvent e) : base(e)
        {
            this.SelectedStyle = style;
        }
    }

    public delegate void GameStartRequestEventHandler(object sender, GameStartStyleEventArgs e);
    public sealed class GameStartStyleEventArgs : RoutedEventArgs
    {
        public GameStartStyle SelectedStyle { get; }

        public GameStartStyleEventArgs(GameStartStyle style, RoutedEvent e) : base(e)
        {
            this.SelectedStyle = style;
        }
    }
}
