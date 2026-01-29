using Gomoku.Models.Domain;

namespace Gomoku.Models.DTO
{
    public record MatchPlayerInfo(int Id, string UserId);
    public record MatchInfo(MatchPlayerInfo BlackPlayer, MatchPlayerInfo WhitePlayer, PlayerType Winner,
        string Reason, IEnumerable<GameMove> MoveHistory, DateTime Time);
}
