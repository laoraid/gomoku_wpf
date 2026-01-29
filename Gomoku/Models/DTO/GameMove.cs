using Gomoku.Models.Domain;

namespace Gomoku.Models.DTO
{
    public record GameMove(int X, int Y, int MoveNumber, PlayerType PlayerType);
}
