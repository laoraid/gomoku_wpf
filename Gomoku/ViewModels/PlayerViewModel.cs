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
        private PlayerType _type;

        [ObservableProperty]
        private int _remainingTime = 30;

        [ObservableProperty]
        private int _win = 0;

        [ObservableProperty]
        private int _loss = 0;

        [ObservableProperty]
        private int _draw = 0;

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
            _player.Records.Win++;
        }

        public void AddLoss()
        {
            Loss++;
            _player.Records.Loss++;
        }

        public void AddDraw()
        {
            Draw++;
            _player.Records.Draw++;
        }

        public void UpdateFromModel(Player player)
        {
            if (player.Nickname == Nickname)
                Type = player.Type;
        }

        public Player ToModel() => new Player { Nickname = Nickname, Type = Type };
    }
}
