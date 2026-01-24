using Gomoku.Models;
using Gomoku.Models.DTO;

namespace Gomoku.Services.Interfaces
{
    public interface IDatabaseService
    {
        Task<Player> TryLoginAsync(string id, string pw);
        Task<Player> CreateAccountAsync(string id, string pw);
        Task DeleteAccountAsync(string id, string pw);

        Task SaveMatchAsync(MatchInfo match);

        Task<Record> GetPlayerRecordsAsync(Player player);
        Task<IEnumerable<MatchInfo>> GetPlayerMatchHistoriesAsync(Player player);
        Task<(Record BlackRecord, Record WhiteRecord)> GetRelativeRecordsAsync(Player black, Player white);
    }
}
