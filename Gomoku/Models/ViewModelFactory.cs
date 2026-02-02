using Gomoku.Models.Interfaces;
using Gomoku.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gomoku.Models
{

    public class ViewModelFactory(IServiceProvider provider) : IViewModelFactory
    {
        public T Create<T>() where T : ViewModelBase => provider.GetRequiredService<T>();
        public T Create<T>(params object[] parameters) where T : ViewModelBase
            => ActivatorUtilities.CreateInstance<T>(provider, parameters);
    }
}
