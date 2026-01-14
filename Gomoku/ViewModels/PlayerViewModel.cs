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

        public PlayerViewModel(Player player)
        {
            _player = player;
            Nickname = player.Nickname;
            Type = player.Type;
        }

        public Player ToModel() => new Player { Nickname = Nickname, Type = Type };
    }
}
