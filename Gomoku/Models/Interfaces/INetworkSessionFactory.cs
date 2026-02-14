using Gomoku.Models.Network;
using System.Net.Sockets;

namespace Gomoku.Models.Interfaces
{
    /// <summary>
    /// 네트워크세션을 생성하는 팩토리 클래스
    /// </summary>
    public interface INetworkSessionFactory
    {
        /// <summary>
        /// TcpClient로 INetworkSession을 생성합니다.
        /// </summary>
        /// <param name="tcpclient">TCP 클라이언트</param>
        /// <returns></returns>
        INetworkSession Create(TcpClient tcpclient);
    }
}
