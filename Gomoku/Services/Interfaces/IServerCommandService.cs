using Gomoku.Models.DTO;

namespace Gomoku.Services.Interfaces
{
    public interface IServerCommandService
    {
        Task<CommandResult> ExecuteCommandAsync(string text);
    }
}
