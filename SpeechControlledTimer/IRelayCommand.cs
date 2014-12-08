using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpeechControlledTimer
{
    public interface IRelayCommand : ICommand
    {
        void RaiseCanExecuteChanged();
    }
}
