using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Models;

namespace Gomoku.ViewModels
{
    public partial class PlayerViewModel : ViewModelBase
    {
        private Player _player;

        [ObservableProperty]
        private string _nickname;

        partial void OnNicknameChanged(string value) => _player.Nickname = value;

        [ObservableProperty]
        private PlayerType _type;

        partial void OnTypeChanged(PlayerType value) => _player.Type = value;

        [ObservableProperty]
        private int _remainingTime = 30;

        [ObservableProperty]
        private int _win = 0;

        partial void OnWinChanged(int value) => _player.Records.Win = value;

        [ObservableProperty]
        private int _loss = 0;

        partial void OnLossChanged(int value) => _player.Records.Loss = value;

        [ObservableProperty]
        private int _draw = 0;

        partial void OnDrawChanged(int value) => _player.Records.Draw = value;

        public PlayerViewModel(Player player)
        {
            _player = player;
            Nickname = player.Nickname;
            Type = player.Type;

            Win = player.Records.Win;
            Loss = player.Records.Loss;
        }

        public void AddWin()
        {
            Win++;
        }

        public void AddLoss()
        {
            Loss++;
        }

        public void AddDraw()
        {
            Draw++;
        }
        public Player ToModel() => _player;
    }
}
