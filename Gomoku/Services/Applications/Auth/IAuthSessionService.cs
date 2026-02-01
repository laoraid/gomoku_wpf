using Gomoku.Models.Common;
using Gomoku.Models.DTO;

namespace Gomoku.Services.Applications.Auth
{
    public interface IAuthSessionService
    {
        /// <summary>
        /// option으로 세션을 시작합니다.
        /// </summary>
        /// <param name="option"></param>
        /// <returns>성공 여부</returns>
        Task<bool> StartSessionAsync(ConnectionOption option);

        /// <summary>
        /// 세션을 종료합니다.
        /// </summary>
        /// <returns></returns>
        Task StopSessionAsync();

        /// <summary>
        /// 아이디, 패스워드, 닉네임을 이용해 계정을 생성 요청합니다.
        /// </summary>
        /// <param name="userid">아이디</param>
        /// <param name="password">비밀번호</param>
        /// <param name="nickname">사용할 닉네임</param>
        /// <returns>계정 생성 결과</returns>
        /// <exception cref="InvalidOperationException">클라이언트가 초기화되지 않았을때</exception>
        Task<AuthResult> RequestCreateAccountAsync(string userid, string password, string nickname);

        /// <summary>
        /// 아이디, 패스워드로 로그인을 요청합니다.
        /// </summary>
        /// <param name="userid">아이디</param>
        /// <param name="password">비밀번호</param>
        /// <returns>로그인 결과</returns>
        Task<AuthResult> RequestLoginAsync(string userid, string password);

        /// <summary>
        /// 게스트로 로그인을 요청합니다.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">클라이언트가 초기화되지 않았을때</exception>
        Task RequestGuestLoginAsync();

        /// <summary>
        /// 아이디, 패스워드로 계정 삭제를 요청합니다.
        /// </summary>
        /// <param name="userid">아이디</param>
        /// <param name="password">비밀번호</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">클라이언트가 초기화되지 않았을때</exception>
        Task RequestDeleteAccountAsync(string userid, string password);


    }
}
