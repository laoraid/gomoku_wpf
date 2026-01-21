using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Services.Interfaces;

namespace Gomoku.ViewModels
{
    public class ViewModelBase(IDispatcher dispatcher) : ObservableValidator
    {
        protected IDispatcher _dispatcher = dispatcher;
        protected void ReceiveInvoke<T>(Action<T> action, T data)
        {
            _dispatcher.Invoke(() => { action(data); });
        }

        protected void ReceiveInvoke(Action action)
        {
            _dispatcher.Invoke(action);
        }
    }
}
