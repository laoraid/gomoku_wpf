using Gomoku.Models.DTO;

namespace Gomoku.Services.Applications.Request
{
    public interface IServerRequestService
    {
        Task<IEnumerable<MatchInfo>> RequestSearchMatchesAsync(
            string? PlayerNickname = null,
            string? BlackPlayerNickname = null,
            string? WhitePlayerNickname = null,
            DateTime? from = null,
            DateTime? to = null,
            int PageNumber = 1,
            int PageSize = 20);

        Task<IEnumerable<GameMove>> RequestMatchMovesAsync(MatchInfo match);

        /// <summary>
        /// 랭킹을 요청합니다.
        /// </summary>
        /// <returns>랭킹 정보 목록</returns>
        /// <exception cref="InvalidOperationException">클라이언트가 초기화되지 않았을때</exception>
        Task<IEnumerable<RankInfo>> RequestRankingsAsync();
    }
}
