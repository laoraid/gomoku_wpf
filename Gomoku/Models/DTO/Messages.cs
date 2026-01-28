using Gomoku.Models.Interfaces;

namespace Gomoku.Models.DTO
{
    public record StonePlacedMessage(GameMove Move);
    public record LastStoneCanceledMessage(PlayerType Type, int LeftCancelCount);
    public record TurnChangedMessage(PlayerType Type);
    public record PlaceRejectedMessage(GameMove Move);
    public record TimePassedMessage(PlayerType Type, int Lefttime);

    public record GameEndMessage(bool IsWin, PlayerType Winner, List<GameMove>? Stones, string Reason);
    public record GameResetMessage();
    public record GameStartMessage(bool IsRecordUse, Record? BlackRelativeRecord, Record? WhiteRelativeRecord);

    public record GameJoinMessage(PlayerType Type, Player Player);
    public record GameLeftMessage(PlayerType Type, Player player);
    public record GameSyncMessage(bool IsGameStarted, IEnumerable<GameMove> MoveHistory,
        PlayerType CurrentTurn, IEnumerable<RuleInfo> Rules,
        Player? BlackPlayer, Player? WhitePlayer);

    public record PlayerDisconnectedInternalMessage(Player Player);
    public record PlayerDisconnectedMessage(Player Player);
    public record PlayerConnectedMessage(Player Player);

    public record SessionInitializedMessage(Player Me, IEnumerable<Player> Players);
    public record SessionConnectLostInternalMessage();
    // 서비스 내부용 메시지, 뷰모델은 아래를 구독하여 서비스가 모두 정리한 다음 받도록
    public record SessionConnectLostMessage();

    public record ChatReceivedMessage(Player sender, string Message);

    public record LoginFailedMessage(string Message);
    public record DeleteAccountRejectedMessage(string Message);
    public record CreateAccountRejectedMessage(string Message);

    public record ClientActivatedMessage(IGameClient Client);
    public record ClientDeactivatedMessage();

    public record PlayerNicknameChangedMessage(Player Player, string OldNickname, string NewNickname);
}
