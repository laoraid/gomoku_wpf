namespace Gomoku.Models.Network
{
    public interface INetworkSession
    {
        string SessionId { get; set; }
        DateTime LastActiveTime { get; set; }
        bool IsConnected { get; }
        bool IsAuthenticated { get; set; }

        event Action<INetworkSession, GameData> OnDataReceived;
        event Action<INetworkSession> OnDisconnected;

        Task SendAsync(GameData data);
        void Disconnect();

    }
}
