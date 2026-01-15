namespace Gomoku.Models.DTO
{
    public record GameEnd(bool IsWin, PlayerType Winner, List<(int x, int y)>? Stones, string Reason);
}
