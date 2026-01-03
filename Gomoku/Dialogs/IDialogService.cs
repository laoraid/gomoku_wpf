using Gomoku.Models;
using Gomoku.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Gomoku.Dialogs
{
    public abstract class WpfServiceBase
    {
        protected static Window ActiveWindow =>
            Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) ?? Application.Current.MainWindow;

    }
    public interface IDialogService
    {
        bool Confirm(string title, string message);
        void Alert(string message);
    }

    public interface IDialogViewModel
    {
        public bool IsConfirmed { get; }
        event Action? RequestClose;
    }

    public interface IWindowService
    {
        T? ShowDialog<T>(T viewModel) where T : class, IDialogViewModel;
    }
}
