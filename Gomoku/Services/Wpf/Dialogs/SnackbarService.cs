/*
 * SnackbarService.cs
 * 머티리얼 디자인 스타일의 하단 알림 스낵바를 띄우는 서비스 클래스
 * SnackbarMessageQueue를 사용하여 여러 스낵바를 순서대로 출력합니다.
 */
using MaterialDesignThemes.Wpf;

namespace Gomoku.Services.Wpf.Dialogs
{
    public class SnackbarService : ISnackbarService
    {
        private readonly SnackbarMessageQueue _messageQueue;

        public SnackbarService()
        {
            _messageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));
        }

        public object MessageQueue => _messageQueue;
        // object 로 하는 이유: 단위테스트 시에 UI 라이브러리랑 종속성 제거하기
        public void Show(string message, string? buttonContent = null, Action? actionhandler = null)
        {
            if (buttonContent != null)
            { // 이러는 이유: 스낵바에 버튼 놓고 아무 액션도 안넣으면 그냥 바로 스낵바 닫힘
                if (actionhandler == null)
                    actionhandler = () => { };
                _messageQueue.Enqueue(message, buttonContent, actionhandler);
            }
            else
                _messageQueue.Enqueue(message);
        }
    }
}
