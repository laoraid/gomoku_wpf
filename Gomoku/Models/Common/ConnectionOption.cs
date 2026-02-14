using Gomoku.Models.Domain;

namespace Gomoku.Models.Common
{
    /// <summary>
    /// 연결 옵션. 서버 모드, 클라이언트 모드, 혼자두기 모드
    /// </summary>
    public enum ConnectionType
    {
        Server, Client, Single
    }

    /// <summary>
    /// 연결 옵션 데이터 클래스입니다.
    /// </summary>
    /// <param name="Ip">아이피 주소</param>
    /// <param name="port">포트</param>
    /// <param name="LoginType">로그인 타입</param>
    /// <param name="DoubleThreeRuleType">쌍삼 룰</param>
    /// <param name="ConnectionType">연결 타입</param>
    /// <param name="CancellationToken">취소 토큰</param>
    /// <param name="LeftCancelCount">무르기 가능 횟수</param>
    public record ConnectionOption(string Ip, int port, LoginType LoginType,
        DoubleThreeRuleType DoubleThreeRuleType,
        ConnectionType ConnectionType, CancellationToken CancellationToken, int LeftCancelCount);
}
