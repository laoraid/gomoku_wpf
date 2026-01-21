using Gomoku.ViewModels;

namespace Gomoku.Models.Interfaces
{
    public interface IViewModelFactory
    {
        T Create<T>() where T : ViewModelBase;
    }
}
