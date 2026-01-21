namespace Gomoku.Models.DTO
{
    public record StonePlacedMessage(GameMove Move);
    public record LastStoneCanceledMessage(PlayerType Type, int LeftCancelCount);
    public record TurnChangedMessage(PlayerType Type);
    public record PlaceRejectedMessage(GameMove Move);
    public record TimePassedMessage(PlayerType Type, int Lefttime);

    public record GameEndMessage(bool IsWin, PlayerType Winner, List<GameMove>? Stones, string Reason);
    public record GameResetMessage();
    public record GameStartMessage();

    public record GameJoinMessage(PlayerType Type, Player Player);
    public record GameLeftMessage(PlayerType Type, Player player);
    public record GameSyncMessage(bool IsGameStarted, IEnumerable<GameMove> MoveHistory,
        PlayerType CurrentTurn, IEnumerable<RuleInfo> Rules,
        Player? BlackPlayer, Player? WhitePlayer);

    public record PlayerDisconnectedMessage(Player Player);
    public record PlayerConnectedMessage(Player Player);

    public record SessionInitializedMessage(Player Me, IEnumerable<Player> Players);
    public record SessionConnectLostMessage();

    public record ChatReceivedMessage(Player sender, string Message);

}
