using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpeechControlledTimer
{
    /// <summary>
    /// Generic command to avoid multiple command implementations
    /// Copy & owned from http://msdn.microsoft.com/en-us/magazine/dd419663.aspx#id0090030
    /// </summary>
    public class RelayCommand : IRelayCommand 
    { 
        #region Fields <>---------------------------------------------------------------------------------
        /// <summary>
        /// [Required] delegate which is called if command should be executed
        /// </summary>
        private readonly Action<object> _execute; 
        /// <summary>
        /// [Optional] delegate which is called to verify if command can be executed
        /// </summary>
        private readonly Predicate<object> _canExecute; 
        #endregion 
        
        #region Constructors <>---------------------------------------------------------------------------
        /// <summary>
        /// RelayCommand allows you to inject the command's logic via delegates passed into its constructor.
        /// This approach allows for terse, concise command implementation in ViewModel classes. 
        /// RelayCommand is a simplified variation of the DelegateCommand found in the Microsoft Composite Application Library.
        /// </summary>
        /// <param name="execute">required delegate to handle command</param>
        /// <see cref="http://msdn.microsoft.com/en-us/magazine/dd419663.aspx#id0090030"/>
        public RelayCommand(Action<object> execute) 
            : this(execute, null) { }

        /// <summary>
        /// RelayCommand allows you to inject the command's logic via delegates passed into its constructor. 
        /// This approach allows for terse, concise command implementation in ViewModel classes. 
        /// RelayCommand is a simplified variation of the DelegateCommand found in the Microsoft Composite Application Library.
        /// </summary>
        /// <param name="execute">required delegate to handle command</param>
        /// <param name="canExecute">optional delegate to check if execution is possible</param>
        /// <see cref="http://msdn.microsoft.com/en-us/magazine/dd419663.aspx#id0090030"/>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null) throw new ArgumentNullException("execute"); 
            
            _execute = execute; 
            _canExecute = canExecute;
        } 
        #endregion 
        
        #region ICommand Members <>-----------------------------------------------------------------------

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        } 
        
        public event EventHandler CanExecuteChanged;
        //{
        //    add { CommandManager.RequerySuggested += value; } 
        //    remove { CommandManager.RequerySuggested -= value; }
        //}

        public void Execute(object parameter)
        {
            _execute(parameter);
        } 
        #endregion 

        #region IRelayCommand Members <>------------------------------------------------------------------
        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, EventArgs.Empty);
            }
        }
        #endregion
    }
}

