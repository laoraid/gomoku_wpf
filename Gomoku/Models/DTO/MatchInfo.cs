using Gomoku.Models.Domain;

namespace Gomoku.Models.DTO
{
    public record MatchPlayerInfo(int Id, string Nickname);
    public record MatchInfo(MatchPlayerInfo BlackPlayer, MatchPlayerInfo WhitePlayer, PlayerType Winner,
        string Reason, IEnumerable<GameMove>? MoveHistory, DateTime Time, int Id = -1);
}
