namespace Gomoku.Models.Network
{
    public interface INetworkService
    {
        bool IsConnected { get; }

        void Disconnect();
    }
}