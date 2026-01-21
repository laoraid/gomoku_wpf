namespace Gomoku.Models.Interfaces
{
    public interface IGameServer
    {
        bool IsRunning { get; }

        Task StartAsync(int port);
        void StopServer();

        void StartGame();
        void AddRule(Rule rule);

        Task Broadcast(GameData data);
    }
}
