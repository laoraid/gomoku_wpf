using Gomoku.Models.Common;
using Gomoku.Models.DTO;

namespace Gomoku.Services.Applications.Auth
{
    public interface IAuthSessionService
    {
        Task<bool> StartSessionAsync(ConnectionOption option);
        Task StopSessionAsync();
        Task<AuthResult> RequestCreateAccountAsync(string userid, string password, string nickname);
        Task<AuthResult> RequestLoginAsync(string userid, string password);

        Task RequestGuestLoginAsync();
        Task RequestDeleteAccountAsync(string userid, string password);

        Task<IEnumerable<RankInfo>> RequestRankingsAsync();
    }
}
