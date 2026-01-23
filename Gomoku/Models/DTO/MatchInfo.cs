namespace Gomoku.Models.DTO
{
    public record MatchInfo(Player BlackPlayer, Player WhitePlayer, PlayerType Winner,
        IEnumerable<GameMove> MoveHistory, DateTime Time);
}
