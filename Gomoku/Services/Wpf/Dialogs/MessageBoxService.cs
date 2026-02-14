/*
 * MessageBoxService.cs
 * 윈도우의 메시지박스를 띄우는 IMessageBoxService의 구현 클래스
 */
using System.Windows;

namespace Gomoku.Services.Wpf.Dialogs
{
    public class MessageBoxService : WpfServiceBase, IMessageBoxService
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
}
