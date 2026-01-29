using Gomoku.Models.DTO;

namespace Gomoku.Services.Applications.Command
{
    public interface IServerCommandService
    {
        /// <summary>
        /// 입력받은 텍스트로 명령어를 실행합니다.
        /// </summary>
        /// <param name="text">명령어 실행 문자열(전체 문자열)</param>
        /// <returns>명령 실행 결과</returns>
        Task<CommandResult> ExecuteCommandAsync(string text);
    }
}
