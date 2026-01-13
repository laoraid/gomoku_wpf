using Gomoku.Controls;
using Gomoku.Services.Interfaces;
using Gomoku.ViewModels;
using Gomoku.Views;
using MaterialDesignThemes.Wpf;
using System.Windows;

namespace Gomoku.Services.Wpf
{


    public class DialogService : WpfServiceBase, IDialogService
    {
        public async Task AlertAsync(string message, DialogSection section = DialogSection.Main)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(ActiveWindow, message, "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        public async Task<bool> ConfirmAsync(string title, string message, DialogSection section = DialogSection.Main)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(ActiveWindow, message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            });
        }

        public async Task<bool> CautionAsync(string title, string message, DialogSection section = DialogSection.Main)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(ActiveWindow, message, title,
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
            });
        }

        public async Task ErrorAsync(string message, DialogSection section = DialogSection.Main)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(ActiveWindow, message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    public enum DialogSection
    {
        // 머티리얼 다이얼로그 띄울 공간, Main이면 창 전체
        // 나중에 채팅창에만 띄운다 하면 Chat 넣고
        // 뷰에서 채팅창을 DialogHost로 감싸고 Identifier 입력하고 그런식으로
        Main
    }
    public class MaterialDialogService : WpfServiceBase, IDialogService
    {
        private readonly Dictionary<DialogSection, string> _sectionMap = new Dictionary<DialogSection, string>()
        {
            { DialogSection.Main, "MainDialogHost" },
        };

        public async Task AlertAsync(string message, DialogSection section = DialogSection.Main)
        {
            await ShowMaterialDialog(message, "알림", false, section);
        }

        public async Task<bool> CautionAsync(string title, string message, DialogSection section = DialogSection.Main)
        {
            var result = await ShowMaterialDialog(message, title, true, section);
            return result is bool b && b;
        }

        public async Task<bool> ConfirmAsync(string title, string message, DialogSection section = DialogSection.Main)
        {
            var result = await ShowMaterialDialog(message, title, true, section);
            return result is bool b && b;
        }

        public async Task ErrorAsync(string message, DialogSection section = DialogSection.Main)
        {
            await ShowMaterialDialog(message, "오류", true, section);
        }

        private async Task<object?> ShowMaterialDialog(string message,
            string title = "알림", bool isConfirm = false, DialogSection section = DialogSection.Main)
        {
            string identifier = _sectionMap[section];
            var vm = new MessageDialogViewModel
            {
                Title = title,
                Message = message,
                IsConfirm = isConfirm
            };

            var dialog = new MessageDialog { DataContext = vm };
            return await DialogHost.Show(dialog, identifier);
        }
    }

    public class WindowService : WpfServiceBase, IWindowService
    {
        private readonly Dictionary<Type, Type> _windowMap = new Dictionary<Type, Type>()
        { // 뷰모델: 뷰 매핑
            { typeof(ConnectViewModel), typeof(ConnectWindow) },
            { typeof(InformationViewModel), typeof(InformationWindow) },
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
