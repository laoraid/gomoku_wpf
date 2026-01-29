using Gomoku.Models.Common;

namespace Gomoku.Models.DTO
{
    public record AuthInfo(LoginType LoginType, string UserId, string Password);
}
