using Gomoku.Services.Wpf.Dialogs;
using Gomoku.ViewModels;
using Gomoku.Views;
using Gomoku.Views.Windows;
using System.Windows;

namespace Gomoku.Services.Wpf.Window
{
    public class WindowService : WpfServiceBase, IWindowService
    {
        private readonly Dictionary<Type, Type> _windowMap = new Dictionary<Type, Type>()
        { // 뷰모델: 뷰 매핑
            { typeof(ConnectViewModel), typeof(ConnectWindow) },
            { typeof(InformationViewModel), typeof(InformationWindow) },
            { typeof(RankingViewModel), typeof(RankingWindow) },
            { typeof(MatchViewModel), typeof(MatchWindow) }
        };

        T? IWindowService.ShowDialog<T>(T viewModel) where T : class
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                if (!_windowMap.TryGetValue(typeof(T), out var windowType))
                {
                    throw new Exception("알 수 없는 뷰모델");
                }

                var win = (System.Windows.Window)Activator.CreateInstance(windowType)!;

                win.DataContext = viewModel;
                win.Owner = ActiveWindow;
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                Action closeHandler = null!; // 확인 버튼같은거 클릭시 창 종료되도록
                closeHandler = () =>
                {
                    viewModel.RequestClose -= closeHandler;
                    win.DialogResult = true;
                };
                viewModel.RequestClose += closeHandler;
                // 뷰모델의 RequestClose 이벤트에 핸들러 등록

                win.Closing += (_, _) =>
                {
                    if (viewModel is DialogViewModelBase dialogVM)
                    {
                        if (!dialogVM.IsConfirmed && !dialogVM.CloseRequested)
                        {
                            // 확인 안하고 창 닫으려 할때
                            viewModel.RequestClose -= closeHandler; // 중복호출 방지
                            dialogVM.CancelCommand.Execute(null);
                        }
                    }
                };

                bool? result = win.ShowDialog();

                return (result == true && viewModel.IsConfirmed) ? viewModel : null; //확인 버튼 눌렀을때만 뷰모델 반환
            });
        }
    }
}
