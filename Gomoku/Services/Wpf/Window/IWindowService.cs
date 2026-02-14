using Gomoku.ViewModels.Dialogs;

namespace Gomoku.Services.Wpf.Window
{
    public interface IWindowService
    {
        /// <summary>
        /// 새 창을 다이얼로그로 띄웁니다.
        /// </summary>
        /// <typeparam name="T">띄울 뷰모델</typeparam>
        /// <param name="viewModel">뷰모델</param>
        /// <returns>확인 버튼 클릭 시 뷰모델, 아니면 null</returns>
        T? ShowDialog<T>(T viewModel) where T : class, IDialogViewModel;
    }
}
