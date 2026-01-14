namespace Gomoku.Models.DTO
{
    public record GameSync(IEnumerable<GameMove> MoveHistory,
        PlayerType CurrentTurn, IEnumerable<RuleInfo> Rules,
        Player? BlackPlayer, Player? WhitePlayer);
}
