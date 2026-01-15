namespace Gomoku.Models.DTO
{
    public record GameEnd(bool IsWin, PlayerType Winner, List<GameMove>? Stones, string Reason);
}
