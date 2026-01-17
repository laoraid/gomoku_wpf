using CommunityToolkit.Mvvm.DependencyInjection;
using Gomoku.Services.Interfaces;
using Gomoku.ViewModels;
using MaterialDesignThemes.Wpf;
using System.Collections.Concurrent;

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

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        // 식별자별 다이얼로그 대기 락

        private SemaphoreSlim GetLock(string identifier)
        {
            return _locks.GetOrAdd(identifier, new SemaphoreSlim(1, 1));
        }

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
        public async Task<TDialogVM?> ShowAsync<TDialogVM>(TDialogVM vm,
            DialogSection section) where TDialogVM : class, IDialogViewModel
        {
            string identifier = _sectionMap[section];

            var semaphore = GetLock(identifier);

            await semaphore.WaitAsync();
            // 식별자 별로 다이얼로그 하나씩 띄우도록 대기

            try
            {
                while (DialogHost.IsDialogOpen(identifier))
                {   // 이전 다이얼로그 마무리까지 대기
                    await Task.Delay(100);
                }

                if (vm.CloseRequested)
                    return null;

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
            finally
            {
                semaphore.Release();
            }
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
