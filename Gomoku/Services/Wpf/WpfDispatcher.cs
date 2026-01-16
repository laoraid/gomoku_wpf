using Gomoku.Services.Interfaces;
using System.Windows;
using System.Windows.Threading;

namespace Gomoku.Services.Wpf
{
    public class WpfDispatcher : IDispatcher
    {
        private Dispatcher _Dispatcher => Application.Current.Dispatcher;
        public void Invoke(Action action)
        {
            _Dispatcher.Invoke(action);
        }

        public T Invoke<T>(Func<T> func)
        {
            return _Dispatcher.Invoke<T>(func);
        }

        public async Task InvokeAsync(Action action)
        {
            await _Dispatcher.InvokeAsync(action);
        }

        public async Task<T> InvokeAsync<T>(Func<T> func)
        {
            return await _Dispatcher.InvokeAsync<T>(func);
        }
    }
}
