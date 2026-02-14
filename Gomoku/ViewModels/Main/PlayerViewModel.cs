using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Models.Domain;

namespace Gomoku.ViewModels
{
    /// <summary>
    /// Player 의 상태를 바인딩하는 뷰모델
    /// 
    /// 내부 Player가 변경되면 UpdateFromModel을 호출하여 바인딩된 속성에 알립니다.
    /// </summary>
    public partial class PlayerViewModel : ObservableObject
    {
        private Player _player;

        [ObservableProperty]
        private string _nickname;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBlack))]
        [NotifyPropertyChangedFor(nameof(IsWhite))]
        private PlayerType _type;

        [ObservableProperty]
        private int _remainingTime = 30;

        [ObservableProperty]
        private int _win = 0;

        [ObservableProperty]
        private int _loss = 0;

        [ObservableProperty]
        private int _draw = 0;

        [ObservableProperty]
        private int _leftCancelLast = 3;

        public bool IsBlack => Type == PlayerType.Black;
        public bool IsWhite => Type == PlayerType.White;

        public PlayerViewModel(Player player)
        {
            _player = player;
            Nickname = player.Nickname;
            Type = player.Type;

            Win = player.Records.Win;
            Loss = player.Records.Loss;
            LeftCancelLast = player.LeftCancelLast;
        }
        public void UpdateFromModel()
        {
            Type = _player.Type;
            Nickname = _player.Nickname;
            Win = _player.Records.Win;
            Loss = _player.Records.Loss;
            Draw = _player.Records.Draw;
            LeftCancelLast = _player.LeftCancelLast;
        }
        public Player ToModel() => _player;
    }
}
