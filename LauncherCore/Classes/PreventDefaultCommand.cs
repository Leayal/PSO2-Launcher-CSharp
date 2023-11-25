using System;
using System.Windows.Input;

namespace Leayal.PSO2Launcher.Core.Classes
{
    sealed class PreventDefaultCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { }
    }
}
