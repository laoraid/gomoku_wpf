using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models;

namespace Gomoku.Services.Interfaces
{
    public interface IGameDataRouter : IRecipient<GameData>
    {
    }
}
