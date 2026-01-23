using Gomoku.Models;
using Gomoku.Models.DTO;

namespace Gomoku.Services.Interfaces
{
    public interface IDatabaseService
    {
        Task<Player> TryLoginAsync(string id, string pw);
        Task<Player> CreateAccountAsync(string id, string pw);

        Task SaveMatchAsync(MatchInfo match);

        Task<Record> GetPlayerRecordsAsync(Player player);
        Task<IEnumerable<MatchInfo>> GetPlayerMachHistoriesAsync(Player player);
    }
}
