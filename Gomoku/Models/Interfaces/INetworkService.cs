namespace Gomoku.Models.Interfaces
{
    public interface INetworkService
    {
        bool IsConnected { get; }

        void Disconnect();
    }
}