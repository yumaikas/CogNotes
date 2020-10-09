using System;
using System.Windows.Input;

namespace Cognotes
{
    // Using Cmd rather than Command for cuteness points in this code.
    public class Cmd : ICommand
    {
        // Code copied from https://stackoverflow.com/a/12423962/823592
        Func<bool> can_exec;
        Action exec;
        public Cmd(Action exec, Func<bool> can_exec)
        {
            this.exec = exec;
            this.can_exec = can_exec;
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            // If we've not defined how to execute, we shouldn't be able to.
            return can_exec?.Invoke() ?? false; 
        }

        public void Execute(object parameter)
        {
            exec?.Invoke();
        }
    }
}
