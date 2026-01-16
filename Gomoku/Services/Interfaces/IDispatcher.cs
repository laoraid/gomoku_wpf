namespace Gomoku.Services.Interfaces
{
    public interface IDispatcher
    {
        Task InvokeAsync(Action action);
        void Invoke(Action action);

        Task<T> InvokeAsync<T>(Func<T> func);
        T Invoke<T>(Func<T> func);
    }
}
