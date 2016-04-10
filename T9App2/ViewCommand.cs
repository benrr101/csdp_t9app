////////////////////////////////////////////////////////////////////////////
// Command Class
// 
// Description: This class allows the generation of bindable actions using
//              methods as Actions.
// Author: Benjamin Russell (brr1922)
////////////////////////////////////////////////////////////////////////////
using System;
using System.Windows.Input;

namespace T9App2
{
    class ViewCommand : ICommand
    {
        public Action<object> Action { get; set; }

        #region ICommand Interface Implementation

        public bool CanExecute(object param)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object param)
        {
            Action(param);
        }

        #endregion
    }
}
