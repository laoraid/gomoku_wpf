using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Services.Wpf;

namespace Gomoku.ViewModels
{
    /// <summary>
    /// 모든 뷰모델의 베이스 뷰모델입니다.
    /// </summary>
    /// <param name="dispatcher"></param>
    public class ViewModelBase(IDispatcher dispatcher) : ObservableValidator
    {
        /// <summary>
        /// UI 스레드에서의 작업을 위한 Dispather
        /// </summary>
        protected IDispatcher _dispatcher = dispatcher;
        /// <summary>
        /// 메서드와 파라미터 메시지를 UI 스레드에서 작업합니다. IRecipient를 구현할때 사용합니다.
        /// </summary>
        /// <typeparam name="T">메서드의 파라미터 메시지</typeparam>
        /// <param name="action">수행할 메서드</param>
        /// <param name="data">메서드에 전달될 메시지</param>
        protected void ReceiveInvoke<T>(Action<T> action, T data)
        {
            _dispatcher.Invoke(() => { action(data); });
        }

        /// <summary>
        /// 메서드를 UI 스레드에서 작업합니다.
        /// </summary>
        /// <param name="action">수행할 메서드</param>
        protected void ReceiveInvoke(Action action)
        {
            _dispatcher.Invoke(action);
        }
    }
}
