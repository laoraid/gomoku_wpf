using System.Net.Sockets;

namespace Gomoku.Models.Interfaces
{
    public interface INetworkSessionFactory
    {
        INetworkSession Create(TcpClient tcpclient);
    }
}
