namespace Gomoku.Models.DTO
{
    public record GameSync(bool IsGameStarted, IEnumerable<GameMove> MoveHistory,
        PlayerType CurrentTurn, IEnumerable<RuleInfo> Rules,
        Player? BlackPlayer, Player? WhitePlayer);
}
