using CommunityToolkit.Mvvm.DependencyInjection;
using Gomoku.Services.Interfaces;
using Gomoku.ViewModels;
using MaterialDesignThemes.Wpf;

namespace Gomoku.Services.Wpf
{
    public enum DialogSection
    {
        // 머티리얼 다이얼로그 띄울 공간, Main이면 창 전체
        // 나중에 채팅창에만 띄운다 하면 Chat 넣고
        // 뷰에서 채팅창을 DialogHost로 감싸고 Identifier 입력하고 그런식으로
        Main
    }
    public class MaterialDialogService : IDialogService, IMessageBoxService
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
            return result != null;
        }

        public async Task<bool> ConfirmAsync(string title, string message, DialogSection section = DialogSection.Main)
        {
            var result = await ShowMaterialDialog(message, title, true, section);
            return result != null;
        }

        public async Task ErrorAsync(string message, DialogSection section = DialogSection.Main)
        {
            await ShowMaterialDialog(message, "오류", true, section);
        }
        public async Task<T?> ShowAsync<T>(T vm, DialogSection section) where T : class, IDialogViewModel
        {
            string identifier = _sectionMap[section];

            Action? closeHandler = null;
            closeHandler = () =>
            {
                vm.RequestClose -= closeHandler;
                if (DialogHost.IsDialogOpen(identifier))
                    DialogHost.Close(identifier);
            };

            vm.RequestClose += closeHandler;

            await DialogHost.Show(vm, identifier);

            return vm.IsConfirmed ? vm : null;
        }

        private async Task<object?> ShowMaterialDialog(string message,
            string title = "알림", bool isConfirm = false, DialogSection section = DialogSection.Main)
        {
            var vm = Ioc.Default.GetRequiredService<MessageDialogViewModel>();

            vm.Title = title;
            vm.Message = message;
            vm.IsConfirmMode = isConfirm;

            return await ShowAsync(vm, section);
        }
    }
}
