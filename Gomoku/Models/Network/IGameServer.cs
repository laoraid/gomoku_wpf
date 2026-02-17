using Gomoku.Models.Common;
using Gomoku.Models.Domain;

namespace Gomoku.Models.Network
{
    public interface IGameServer : IAsyncDisposable
    {
        /// <summary>
        /// 서버가 가동중인지에 대한 여부
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 비동기적으로 서버를 시작합니다.
        /// </summary>
        /// <param name="option">서버 연결 옵션</param>
        /// <returns></returns>
        Task StartAsync(ConnectionOption option);

        /// <summary>
        /// 게임을 시작합니다.
        /// </summary>
        void StartGame();

        /// <summary>
        /// 룰을 추가합니다.
        /// </summary>
        /// <param name="rule">추가할 룰</param>
        void AddRule(Rule rule);
    }
}
