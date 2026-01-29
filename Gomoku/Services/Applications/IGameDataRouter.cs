using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Network;

namespace Gomoku.Services.Applications
{
    public interface IGameDataRouter : IRecipient<GameData>
    {
    }
}
