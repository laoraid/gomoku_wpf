namespace Gomoku.Models.Common
{
    /// <summary>
    /// 인증 타입, 접속 시에 로그인 할건지 계정 생성 할것인지 선택할때 사용합니다.
    /// </summary>
    public enum AuthType
    {
        Login, CreateAccount
    }

    /// <summary>
    /// 로그인 타입, 접속 시 로그인 모드인지 게스트 모드인지 선택할때 사용합니다.
    /// </summary>
    public enum LoginType
    {
        Login, Guest
    }

}
