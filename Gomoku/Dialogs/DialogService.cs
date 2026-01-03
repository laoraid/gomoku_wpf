using Gomoku.Models;
using Gomoku.ViewModels;
using Gomoku.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Gomoku.Dialogs
{
    public class DialogService : WpfServiceBase, IDialogService
    {
        public void Alert(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(ActiveWindow, message, "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        public bool Confirm(string title, string message)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                return MessageBox.Show(ActiveWindow, message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            });
        }
    }

    public class WindowService : WpfServiceBase, IWindowService
    {
        private readonly Dictionary<Type, Type> _windowMap = new Dictionary<Type, Type>()
        { // 뷰모델: 뷰 매핑
            { typeof(ConnectViewModel), typeof(ConnectWindow) },
        };

        T? IWindowService.ShowDialog<T>(T viewModel) where T : class
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                if (!_windowMap.TryGetValue(typeof(T), out var windowType))
                {
                    throw new Exception("알 수 없는 뷰모델");
                }

                var win = (Window)Activator.CreateInstance(windowType)!;

                win.DataContext = viewModel;
                win.Owner = ActiveWindow;
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                Action closeHandler = null!; // 확인 버튼같은거 클릭시 창 종료되도록
                closeHandler = () =>
                {
                    viewModel.RequestClose -= closeHandler;
                    win.DialogResult = true;
                    win.Close();
                };
                viewModel.RequestClose += closeHandler;

                bool? result = win.ShowDialog();

                return (result == true && viewModel.IsConfirmed) ? viewModel : null; //확인 버튼 눌렀을때만 뷰모델 반환
            });
        }
    }
}
