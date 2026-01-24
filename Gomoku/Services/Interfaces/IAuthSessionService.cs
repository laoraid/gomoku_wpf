using Gomoku.Models.DTO;

namespace Gomoku.Services.Interfaces
{
    public interface IAuthSessionService
    {
        Task<bool> StartSessionAsync(ConnectionOption option);
        Task StopSessionAsync();
        Task RequestCreateAccountAsync(string userid, string password, string nickname);
        Task RequestLoginAsync(string userid, string password);

        Task RequestGuestLoginAsync();
        Task RequestDeleteAccountAsync(string userid, string password);
    }
}
