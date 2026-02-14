/*
 * WpfDispather.cs
 * 다른 스레드에서 UI 스레드를 변경할 때 사용하는 Dispather 클래스
 * 
 * UI 변경은 UI 스레드에서 작업하도록 보장하기 위하여 Invoke, InvokeAsync를 사용합니다.
 */
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
