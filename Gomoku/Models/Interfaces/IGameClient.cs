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
        Task<bool> ConnectAsync(string ip, int port, string nickname, CancellationToken cts);

        Player? Me { get; }

        bool HasOpponent { get; }

        string MessageToken { get; }
    }
}
