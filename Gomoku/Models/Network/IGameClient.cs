using Gomoku.Models.Domain;
using Gomoku.Models.DTO;

namespace Gomoku.Models.Network
{
    public interface IGameClient : INetworkService
    {
        Task SendPlaceAsync(GameMove move);
        Task SendChatAsync(string message);
        Task SendJoinGameAsync(PlayerType type);
        Task SendLeaveGameAsync();
        Task SendGameStartAsync();
        Task CancelLastStoneAsync(int LeftCancelCount);

        /// <summary>
        /// Ip 주소와 포트로 서버에 접속합니다.
        /// </summary>
        /// <param name="ip">아이피 주소</param>
        /// <param name="port">포트</param>
        /// <param name="cts">취소 토큰</param>
        /// <returns></returns>
        Task<bool> ConnectAsync(string ip, int port, CancellationToken cts);

        /// <summary>
        /// 인증 정보를 송신합니다.
        /// </summary>
        /// <param name="authInfo">인증 정보</param>
        /// <returns></returns>
        Task SendAuthAsync(AuthInfo authInfo);
        Task SendCreateAccountAsync(string username, string password, string nickname);

        /// <summary>
        /// 데이터를 비동기적으로 전송합니다.
        /// </summary>
        /// <param name="data">보낼 게임 데이터</param>
        /// <returns></returns>
        Task SendDataAsync(GameData data);

        /// <summary>
        /// 클라이언트의 Player 객체 상태
        /// </summary>
        Player? Me { get; }

        /// <summary>
        /// 상대방이 있는지 여부
        /// </summary>
        bool HasOpponent { get; }

        /// <summary>
        /// IMessenger 버스에서 사용할 메시지 토큰입니다.
        /// </summary>
        string MessageToken { get; }

        /// <summary>
        /// 인증되었는지에 대한 여부
        /// </summary>
        bool IsAuthenticated { get; }
    }
}
