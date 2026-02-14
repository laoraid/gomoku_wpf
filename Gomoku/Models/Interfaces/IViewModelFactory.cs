using Gomoku.ViewModels;

namespace Gomoku.Models.Interfaces
{
    /// <summary>
    /// 뷰모델을 생성하는 팩토리 클래스
    /// </summary>
    public interface IViewModelFactory
    {
        /// <summary>
        /// 뷰모델을 생성합니다.
        /// </summary>
        /// <typeparam name="T">뷰모델</typeparam>
        /// <returns>생성된 뷰모델</returns>
        T Create<T>() where T : ViewModelBase;
        /// <summary>
        /// 파라미터가 필요한 뷰모델을 생성합니다.
        /// </summary>
        /// <typeparam name="T">뷰모델</typeparam>
        /// <param name="parameters">뷰모델이 필요한 파라미터(DI 컨테이너에서 주입되는건 제외)</param>
        /// <returns>생성된 뷰모델</returns>
        T Create<T>(params object[] parameters) where T : ViewModelBase;
    }
}
