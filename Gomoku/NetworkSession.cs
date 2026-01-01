using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Gomoku
{
    public class NetworkSession
    {
        private readonly TcpClient _client;
        private readonly StreamWriter _writer;
        private readonly StreamReader _reader;

        public int SessionId { get; }

        public event Action<NetworkSession, GameData>
    }
}
