using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.ComponentModel;

namespace FloWin.Common
{
    #region No parameter
    public sealed class DelegateCommand : ICommand, INotifyPropertyChanged
    {
        private Action _execute;
        private Func<bool> _canExecute;
        private string _label;
        public string Label
        {
            get { return _label; }
            set { _label = value; RaisePropertyChanged("Label"); }
        }

        public DelegateCommand(string label, Action execute) : this(label, execute, () => true) {}
        public DelegateCommand(string label, Action execute, Func<bool> canExecute)
        {
            Label = label;
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute()
        {
            var d = _canExecute;
            if (d != null) return d();
            else return false;
        }
        public void Execute()
        {
            var d = _execute;
            if (d != null) d();
        }

        bool ICommand.CanExecute(object parameter) { return CanExecute(); }
        void ICommand.Execute(object parameter) { Execute(); }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(params string[] names)
        {
            var h = PropertyChanged;
            if (h == null) return;
            foreach (var name in names)
            {
                h(this, new PropertyChangedEventArgs(name));
            }
        }
    }
    #endregion

    #region Parameter
    public sealed class DelegateCommand<T> : ICommand
    {
        private Action<T> _execute;
        private Func<T, bool> _canExecute;
        private string _label;
        public string Label
        {
            get { return _label; }
            set { _label = value; RaisePropertyChanged("Label"); }
        }

        private static readonly bool IS_VALUE_TYPE;
        static DelegateCommand()
        {
            IS_VALUE_TYPE = typeof(T).IsValueType;
        }

        public DelegateCommand(string label, Action<T> execute) : this(label, execute, _ => true) {}
        public DelegateCommand(string label, Action<T> execute, Func<T, bool> canExecute)
        {
            Label = label;
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(T parameter)
        {
            var d = _canExecute;
            if (d != null) return d(parameter);
            else return false;
        }
        public void Execute(T parameter)
        {
            var d = _execute;
            if (d != null) d(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return CanExecute(Cast(parameter));
        }
        public void Execute(object parameter)
        {
            Execute(Cast(parameter));
        }

        private T Cast(object parameter)
        {
            if (parameter == null && IS_VALUE_TYPE) return default(T);
            return (T)parameter;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(params string[] names)
        {
            var h = PropertyChanged;
            if (h == null) return;
            foreach (var name in names)
            {
                h(this, new PropertyChangedEventArgs(name));
            }
        }
    }
    #endregion
}
