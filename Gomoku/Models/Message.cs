using Gomoku.Models.DTO;
using System.Text.Json.Serialization;

namespace Gomoku.Models
{
    public enum RequestType
    {
        Move, JoinRoom, ExitRoom
    }
    [JsonDerivedType(typeof(GameData), typeDiscriminator: nameof(GameData))]
    [JsonDerivedType(typeof(ClientJoinData), typeDiscriminator: nameof(ClientJoinData))]
    [JsonDerivedType(typeof(ClientExitData), typeDiscriminator: nameof(ClientExitData))]
    [JsonDerivedType(typeof(PositionData), typeDiscriminator: nameof(PositionData))]
    [JsonDerivedType(typeof(ChatData), typeDiscriminator: nameof(ChatData))]
    [JsonDerivedType(typeof(ResponseData), typeDiscriminator: nameof(ResponseData))]
    [JsonDerivedType(typeof(PlaceResponseData), typeDiscriminator: nameof(PlaceResponseData))]
    [JsonDerivedType(typeof(ClientJoinResponseData), typeDiscriminator: nameof(ClientJoinResponseData))]
    [JsonDerivedType(typeof(GameSyncData), typeDiscriminator: nameof(GameSyncData))]
    [JsonDerivedType(typeof(TimePassedData), typeDiscriminator: nameof(TimePassedData))]
    [JsonDerivedType(typeof(GameJoinData), typeDiscriminator: nameof(GameJoinData))]
    [JsonDerivedType(typeof(GameLeaveData), typeDiscriminator: nameof(GameLeaveData))]
    [JsonDerivedType(typeof(GameStartData), typeDiscriminator: nameof(GameStartData))]
    [JsonDerivedType(typeof(GameEndData), typeDiscriminator: nameof(GameEndData))]
    [JsonDerivedType(typeof(PingData), typeDiscriminator: nameof(PingData))]
    [JsonDerivedType(typeof(PongData), typeDiscriminator: nameof(PongData))]
    [JsonDerivedType(typeof(RequestJoinData), typeDiscriminator: nameof(RequestJoinData))]
    public class GameData
    {
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }

    public class ClientJoinData : GameData // 서버 접속
    {
        public required Player Player { get; set; }

    }

    public class RequestJoinData : GameData // 참가 요청 데이터
    {
        public string Nickname { get; set; } = "익명";
    }

    public class ClientExitData : GameData // 서버 퇴장(연결 끊김) - 클라이언트가 보내고 , 서버가 브로드캐스트용으로도 사용
    {
        public required Player Player { get; set; }
    }

    public class PositionData : GameData // 착수 데이터 - ResponseData로 응답
    {
        public required GameMove Move { get; set; }
    }

    public class ChatData : GameData // 채팅 메시지 * 클라이언트가 보내고, 서버가 브로드캐스트용으로도 사용
    {
        public required Player Sender { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ResponseData : GameData // 서버가 클라이언트 요청에 대한 응답
    {
        public bool Accepted { get; set; }
    }

    public class PlaceResponseData : ResponseData // 착수 요청 응답 - 불가할때만 보냄
    {
        public required PositionData Position { get; set; }
    }

    public class ClientJoinResponseData : ResponseData // 클라이언트 연결 요청 응답
    {
        public required Player Me { get; set; }
        public required List<Player> Users { get; set; }
    }

    public class GameSyncData : GameData // 클라이언트 접속 시 게임 상태 동기화용
    {
        public required GameSync SyncData { get; set; }
    }

    public class TimePassedData : GameData // 게임 진행 중 시간 경과 알림용(브로드캐스트용)
    {
        public PlayerType PlayerType { get; set; }
        public int CurrentLeftTimeSeconds { get; set; }
    }

    public class GameJoinData : GameData // 게임 참가(흑 또는 백)
    {
        public PlayerType Type { get; set; }
        public required Player Player { get; set; }
    }

    public class GameLeaveData : GameData // 게임 나감(관전자 전환)
    {
        public PlayerType Type { get; set; }
        public required Player Player { get; set; }
    }

    public class GameStartData : GameData
    {

    }

    public class GameEndData : GameData // 게임 종료 알림(브로드캐스트용)
    {
        public PlayerType Winner { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class PingData : GameData { }

    public class PongData : GameData { }
}
