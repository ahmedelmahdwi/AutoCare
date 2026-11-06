using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels adhering to Clean MVVM principles.
    /// Provides standardized property change notifications, asynchronous task execution, and busy state management.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private bool _isLoading;
        private string _busyMessage = "";
        private string _errorMessage = "";

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Indicates if an asynchronous or background operation is currently running.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Human-readable message displayed while IsLoading is true.
        /// </summary>
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        /// <summary>
        /// Error message if the last operation failed.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Sets field and raises PropertyChanged if value changed.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises PropertyChanged event.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Executes an asynchronous operation safely with automatic IsLoading and BusyMessage tracking.
        /// </summary>
        protected async Task RunAsync(Func<Task> action, string busyMessage = "")
        {
            if (IsLoading) return;

            try
            {
                BusyMessage = busyMessage ?? "";
                ErrorMessage = "";
                IsLoading = true;
                await action();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Data.AppLogger.Error($"RunAsync error in {GetType().Name}: {ex.Message}", ex);
            }
            finally
            {
                IsLoading = false;
                BusyMessage = "";
            }
        }

        /// <summary>
        /// Dispatches an action onto the UI thread safely.
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                    action();
                else
                    Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
