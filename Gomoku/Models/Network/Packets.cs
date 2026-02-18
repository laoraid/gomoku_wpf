/*
 * Packets.cs
 * Json으로 직렬화되어 송/수신되는 데이터 클래스
 * 모든 클래스는 GameData를 상속함
 * 
 * 받는 쪽에서 GameData로 받고 실제 하위 클래스로 캐스팅하므로
 * 클래스 추가 시 GameData 위에 JsonDerivedType을 작성할 것
 */
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;
using System.Text.Json.Serialization;

namespace Gomoku.Models.Network
{
    public enum RequestType
    {
        Move, JoinRoom, ExitRoom
    }

    public interface IReadOnlyRequest { }
    public interface IDbRequiredRequest { }

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
    [JsonDerivedType(typeof(RequestGameStartData), typeDiscriminator: nameof(RequestGameStartData))]
    [JsonDerivedType(typeof(GameStartedData), typeDiscriminator: nameof(GameStartedData))]
    [JsonDerivedType(typeof(GameEndedData), typeDiscriminator: nameof(GameEndedData))]
    [JsonDerivedType(typeof(PingData), typeDiscriminator: nameof(PingData))]
    [JsonDerivedType(typeof(PongData), typeDiscriminator: nameof(PongData))]
    [JsonDerivedType(typeof(RequestJoinData), typeDiscriminator: nameof(RequestJoinData))]
    [JsonDerivedType(typeof(CancelLastData), typeDiscriminator: nameof(CancelLastData))]
    [JsonDerivedType(typeof(LoginFailedData), typeDiscriminator: nameof(LoginFailedData))]
    [JsonDerivedType(typeof(RequestCreateAccountData), typeDiscriminator: nameof(RequestCreateAccountData))]
    [JsonDerivedType(typeof(CreateAccountRejectedData), typeDiscriminator: nameof(CreateAccountRejectedData))]
    [JsonDerivedType(typeof(RequestDeleteAccountData), typeDiscriminator: nameof(RequestDeleteAccountData))]
    [JsonDerivedType(typeof(DeleteAccountRejectedData), typeDiscriminator: nameof(DeleteAccountRejectedData))]
    [JsonDerivedType(typeof(ChangeNicknameRequestData), typeDiscriminator: nameof(ChangeNicknameRequestData))]
    [JsonDerivedType(typeof(ChangeNicknameResponseData), typeDiscriminator: nameof(ChangeNicknameResponseData))]
    [JsonDerivedType(typeof(RequestRankingsData), typeDiscriminator: nameof(RequestRankingsData))]
    [JsonDerivedType(typeof(RankingsData), typeDiscriminator: nameof(RankingsData))]
    [JsonDerivedType(typeof(RequestMatchesData), typeDiscriminator: nameof(RequestMatchesData))]
    [JsonDerivedType(typeof(MatchesData), typeDiscriminator: nameof(MatchesData))]
    [JsonDerivedType(typeof(RequestMatchMoveData), typeDiscriminator: nameof(RequestMatchMoveData))]
    [JsonDerivedType(typeof(MatchMoveData), typeDiscriminator: nameof(MatchMoveData))]
    public class GameData
    {
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }

    public class ClientJoinData : GameData // 서버 접속
    {
        public required Player Player { get; set; }

    }

    public class RequestJoinData : GameData, IDbRequiredRequest // 참가 요청 데이터
    {
        public required AuthInfo AuthInfo { get; set; }
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
        public required GameSyncMessage SyncData { get; set; }
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

    public class RequestGameStartData : GameData, IDbRequiredRequest
    {
    }

    public class GameStartedData : GameData
    {
        public required Player BlackPlayer { get; set; }
        public required Player WhitePlayer { get; set; }

        public Record? BlackRelativeRecord { get; set; } = null;
        public Record? WhiteRelativeRecord { get; set; } = null;
    }


    public class GameEndedData : GameData // 게임 종료 알림(브로드캐스트용)
    {
        public required GameEndMessage EndData { get; set; }
    }

    public class PingData : GameData { }

    public class PongData : GameData { }

    public class CancelLastData : GameData // 무르기
    {
        public required PlayerType SenderType { get; set; }
        public required int LeftCancelLastCount { get; set; }
    }

    public class LoginFailedData : GameData // 로그인 실패
    {
        public required string Reason { get; set; }
    }

    public class RequestCreateAccountData : GameData, IDbRequiredRequest // 회원가입 요청
    {
        public required string UserId { get; set; }
        public required string PasswordHashed { get; set; }
        public required string Nickname { get; set; }
    }

    public class CreateAccountRejectedData : GameData // 회원가입 실패
    {
        public required string Reason { get; set; }
    }

    public class RequestDeleteAccountData : GameData, IDbRequiredRequest
    {
        public required string UserId { get; set; }
        public required string PasswordHashed { get; set; }
    }

    public class DeleteAccountRejectedData : GameData
    {
        public required string Reason { get; set; }
    }

    public class ChangeNicknameRequestData : GameData, IDbRequiredRequest
    {
        public required string NewNickname { get; set; }
    }

    public class ChangeNicknameResponseData : ResponseData
    {
        public string OldNickname { get; set; } = "";
        public string NewNickname { get; set; } = "";
        public string Message { get; set; } = string.Empty;
    }

    public class RequestRankingsData : GameData, IReadOnlyRequest
    {
    }

    public class RankingsData : ResponseData
    {
        public List<RankInfo>? Rankings { get; set; } = null;
    }

    public class RequestMatchesData : GameData, IReadOnlyRequest
    {
        public string? PlayerNickname { get; set; } = null;
        public string? BlackPlayerNickname { get; set; } = null;
        public string? WhitePlayerNickname { get; set; } = null;
        public DateTime? from { get; set; } = null;
        public DateTime? to { get; set; } = null;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class MatchesData : ResponseData
    {
        public IEnumerable<MatchInfo>? Matches { get; set; } = null;
    }

    public class RequestMatchMoveData : GameData, IReadOnlyRequest
    {
        public required MatchInfo Match { get; set; }
    }

    public class MatchMoveData : ResponseData
    {
        public IEnumerable<GameMove>? Moves { get; set; } = null;
    }
}
