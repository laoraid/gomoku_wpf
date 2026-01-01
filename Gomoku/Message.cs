using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Gomoku
{
    public enum RequestType
    {
        Move, JoinRoom, ExitRoom
    }
    [JsonDerivedType(typeof(GameData), typeDiscriminator: "GameData")]
    [JsonDerivedType(typeof(ClientJoinData), typeDiscriminator: "ClientJoinData")]
    [JsonDerivedType(typeof(ClientExitData), typeDiscriminator: "ClientExitData")]
    [JsonDerivedType(typeof(PositionData), typeDiscriminator: "PositionData")]
    [JsonDerivedType(typeof(ChatData), typeDiscriminator: "ChatData")]
    [JsonDerivedType(typeof(ResponseData), typeDiscriminator: "ResponseData")]
    [JsonDerivedType(typeof(GameSyncData), typeDiscriminator: "GameSyncData")]
    [JsonDerivedType(typeof(TimePassedData), typeDiscriminator: "TimePassedData")]
    [JsonDerivedType(typeof(GameJoinData), typeDiscriminator: "GameJoinData")]
    [JsonDerivedType(typeof(GameLeaveData), typeDiscriminator: "GameLeaveData")]
    [JsonDerivedType(typeof(GameStartData), typeDiscriminator: "GameStartData")]
    public abstract class GameData
    {
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }

    public class ClientJoinData : GameData // 서버 접속
    {
        public string Nickname { get; set; } = "익명";

    }

    public class ClientExitData : GameData // 서버 퇴장(연결 끊김) - 클라이언트가 보내고 , 서버가 브로드캐스트용으로도 사용
    {
        public string Nickname { get; set; } = "익명";
    }

    public class PositionData : GameData // 착수 데이터 - ResponseData로 응답
    {
        public int X { get; set; }
        public int Y { get; set; }

        public int MoveNumber { get; set; } // 착수 순서 번호

        public PlayerType Player { get; set; }
    }

    public class ChatData : GameData // 채팅 메시지 * 클라이언트가 보내고, 서버가 브로드캐스트용으로도 사용
    {
        public string SenderNickname { get; set; } = "익명";
        public string Message { get; set; } = string.Empty;
    }

    public class ResponseData : GameData // 서버가 클라이언트 요청에 대한 응답
    {
        public bool Accepted { get; set; }
        public string Message { get; set; } = string.Empty;

        public RequestType TargetRequest { get; set; }
    }

    public class GameSyncData : GameData // 클라이언트 접속 시 게임 상태 동기화용
    {
        public List<PositionData> MoveHistory { get; set; } = new List<PositionData>();
        public PlayerType CurrentTurn { get; set; }
    }

    public class TimePassedData : GameData // 게임 진행 중 시간 경과 알림용(브로드캐스트용)
    {
        public PlayerType Player { get; set; }
        public int CurrentLeftTimeSeconds { get; set; }
    }

    public class GameJoinData : GameData // 게임 참가(흑 또는 백)
    {
        public PlayerType Type { get; set; }
        public string Nickname { get; set; } = "익명";
    }

    public class GameLeaveData : GameData // 게임 나감(관전자 전환)
    {
        public PlayerType Type { get; set; }
        public string Nickname { get; set; } = "익명";
    }

    public class GameStartData : GameData
    {

    }

    public class GameEndData : GameData // 게임 종료 알림(브로드캐스트용)
    {
        public PlayerType Winner { get; set; }
    }
}
