using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Models;

namespace Gomoku.ViewModels
{
    public partial class PlayerViewModel : ViewModelBase
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

        public bool IsBlack => Type == PlayerType.Black;
        public bool IsWhite => Type == PlayerType.White;

        public PlayerViewModel(Player player)
        {
            _player = player;
            Nickname = player.Nickname;
            Type = player.Type;

            Win = player.Records.Win;
            Loss = player.Records.Loss;
        }
        public void UpdateFromModel()
        {
            Type = _player.Type;
            Nickname = _player.Nickname;
            Win = _player.Records.Win;
            Loss = _player.Records.Loss;
            Draw = _player.Records.Draw;
        }
        public Player ToModel() => _player;
    }
}
