namespace Gomoku.Models.Network
{
    public interface INetworkSession
    {
        /// <summary>
        /// 세션을 구분할 수 있는 구분자 string.
        /// </summary>
        string SessionId { get; set; }
        /// <summary>
        /// 마지막 통신 시간입니다.
        /// </summary>
        DateTime LastActiveTime { get; set; }

        /// <summary>
        /// 연결되었는지에 대한 여부
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 인증되었는지에 대한 여부
        /// </summary>
        bool IsAuthenticated { get; set; }

        /// <summary>
        /// 패킷을 수신하였을때 트리거되는 이벤트입니다.
        /// INetworkSession : 수신자
        /// GameData : 수신한 데이터
        /// </summary>
        event Action<INetworkSession, GameData> OnDataReceived;

        /// <summary>
        /// 연결이 해제되었을때 트리거되는 이벤트입니다.
        /// INetworkSession: 연결 해제된 세션
        /// </summary>
        event Action<INetworkSession> OnDisconnected;

        /// <summary>
        /// 비동기적으로 데이터를 송신합니다.
        /// </summary>
        /// <param name="data">보낼 데이터</param>
        /// <returns></returns>
        Task SendAsync(GameData data);

        /// <summary>
        /// 연결을 해제합니다.
        /// </summary>
        void Disconnect();

    }
}
