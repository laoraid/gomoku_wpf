using Gomoku.Models.Domain;
using Gomoku.Models.DTO;

namespace Gomoku.Services.Applications.Game
{
    public interface IGameSessionService
    {
        public Player? BlackPlayer { get; }
        public Player? WhitePlayer { get; }
        public Player? Me { get; }

        bool IsSessionAlive { get; }

        bool IsGameStarted { get; }
        PlayerType CurrentTurn { get; }
        bool IsMyTurn { get; }
        bool IsOpponentTurn { get; }
        bool CanCancelLast { get; }

        string RulesInfo { get; }

        int StoneCount { get; }

        GameMove? LastStone { get; }

        /// <summary>
        /// 금수의 좌표를 모두 불러옵니다.
        /// </summary>
        /// <param name="player">플레이어 타입(흑, 백)</param>
        /// <returns>금수 좌표 tuple의 리스트</returns>
        List<(int x, int y)> GetAllForbiddenPositions(PlayerType player);
        /// <summary>
        /// 돌을 착수합니다.
        /// </summary>
        /// <param name="move">착수할 GameMove 데이터 클래스</param>
        /// <returns></returns>
        Task PlaceStoneAsync(GameMove move);
        /// <summary>
        /// 채팅을 전송합니다.
        /// </summary>
        /// <param name="message">전송할 메시지</param>
        /// <returns></returns>
        Task SendChatAsync(string message);
        /// <summary>
        /// 흑 또는 백에 참가합니다.
        /// </summary>
        /// <param name="type">참가할 진영</param>
        /// <returns></returns>
        Task JoinGameAsync(PlayerType type);
        /// <summary>
        /// 참가한 게임에서 퇴장합니다. (연결 종료 아님)
        /// </summary>
        /// <returns></returns>
        Task LeaveGameAsync();
        /// <summary>
        /// 게임 시작 명령을 내립니다. (흑만 사용 가능)
        /// </summary>
        /// <returns></returns>
        Task StartGameAsync();

        /// <summary>
        /// 무르기를 요청합니다.
        /// </summary>
        /// <returns></returns>
        Task<bool> CancelLastStoneAsync();
    }
}
