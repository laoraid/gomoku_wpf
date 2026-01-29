using Gomoku.Models.DTO;

namespace Gomoku.Services.Applications.Command
{
    public interface IServerCommandService
    {
        Task<CommandResult> ExecuteCommandAsync(string text);
    }
}
