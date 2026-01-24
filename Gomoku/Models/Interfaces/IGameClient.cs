using Gomoku.Models.DTO;

namespace Gomoku.Models.Interfaces
{
    public interface IGameClient : INetworkService
    {
        Task SendPlaceAsync(GameMove move);
        Task SendChatAsync(string message);
        Task SendJoinGameAsync(PlayerType type);
        Task SendLeaveGameAsync();
        Task SendGameStartAsync();
        Task CancelLastStoneAsync(int LeftCancelCount);
        Task<bool> ConnectAsync(string ip, int port, CancellationToken cts);

        Task SendAuthAsync(AuthInfo authInfo);
        Task SendCreateAccountAsync(string username, string password, string nickname);

        Player? Me { get; }

        bool HasOpponent { get; }

        string MessageToken { get; }
    }
}
