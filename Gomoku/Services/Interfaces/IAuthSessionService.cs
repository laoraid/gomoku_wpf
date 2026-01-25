using Gomoku.Models.DTO;

namespace Gomoku.Services.Interfaces
{
    public interface IAuthSessionService
    {
        Task<bool> StartSessionAsync(ConnectionOption option);
        Task StopSessionAsync();
        Task<AuthResult> RequestCreateAccountAsync(string userid, string password, string nickname);
        Task<AuthResult> RequestLoginAsync(string userid, string password);

        Task RequestGuestLoginAsync();
        Task RequestDeleteAccountAsync(string userid, string password);
    }
}
