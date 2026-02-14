using Gomoku.Models.Interfaces;
using Gomoku.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gomoku.Models
{
    /// <summary>
    /// 뷰모델을 생성하는 클래스
    /// 모든 뷰모델은 DI에 등록되어야 함
    /// </summary>
    /// <param name="provider"></param>
    public class ViewModelFactory(IServiceProvider provider) : IViewModelFactory
    {
        public T Create<T>() where T : ViewModelBase => provider.GetRequiredService<T>();
        public T Create<T>(params object[] parameters) where T : ViewModelBase
            => ActivatorUtilities.CreateInstance<T>(provider, parameters);
    }
}
