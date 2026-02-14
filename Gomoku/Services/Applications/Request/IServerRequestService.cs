using Gomoku.Models.DTO;

namespace Gomoku.Services.Applications.Request
{
    public interface IServerRequestService
    {
        /// <summary>
        /// 필터에 맞는 특정한 매치를 요청합니다.
        /// PlayerNickname과 (BlackPlayerNickname, WhitePlayerNickname) 은 같이 사용할 수 없음
        /// </summary>
        /// <param name="PlayerNickname">흑, 백 상관없이 불러올 닉네임</param>
        /// <param name="BlackPlayerNickname">흑 닉네임</param>
        /// <param name="WhitePlayerNickname">백 닉네임</param>
        /// <param name="from">시작 날짜</param>
        /// <param name="to">~까지 날짜</param>
        /// <param name="PageNumber">불러올 페이지 넘버</param>
        /// <param name="PageSize">한 페이지의 사이즈</param>
        /// <returns></returns>
        Task<IEnumerable<MatchInfo>> RequestSearchMatchesAsync(
            string? PlayerNickname = null,
            string? BlackPlayerNickname = null,
            string? WhitePlayerNickname = null,
            DateTime? from = null,
            DateTime? to = null,
            int PageNumber = 1,
            int PageSize = 20);

        /// <summary>
        /// MatchInfo의 착수 히스토리를 요청합니다.
        /// </summary>
        /// <param name="match">착수 히스토리를 요청할 매치</param>
        /// <returns>GameMove의 IEnumerable (착수 순서대로 오름차순 정렬되어 있음)</returns>
        Task<IEnumerable<GameMove>> RequestMatchMovesAsync(MatchInfo match);

        /// <summary>
        /// 랭킹을 요청합니다.
        /// </summary>
        /// <returns>랭킹 정보 목록</returns>
        /// <exception cref="InvalidOperationException">클라이언트가 초기화되지 않았을때</exception>
        Task<IEnumerable<RankInfo>> RequestRankingsAsync();
    }
}
