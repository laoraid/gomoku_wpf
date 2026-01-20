using Gomoku.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gomoku.Models
{
    public interface IViewModelFactory
    {
        T Create<T>() where T : ViewModelBase;
    }

    public class ViewModelFactory(IServiceProvider provider) : IViewModelFactory
    {
        public T Create<T>() where T : ViewModelBase => provider.GetRequiredService<T>();
    }
}
