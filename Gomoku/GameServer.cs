using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Ink;

namespace Gomoku
{
    public class GameServer
    {
        private TcpListener _listener;

        private List<NetworkSession> _sessions = new List<NetworkSession>();

        private GomokuManager manager = new GomokuManager();

        private object _handlelock = new object();

        public async Task StartAsync(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            Logger.System($"서버 시작 됨. 포트 : {port}");

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();

                var newSession = new NetworkSession(client);

                newSession.OnDataReceived += HandleDataReceived;
                newSession.OnDisconnected += HandleClientDisconnected;

                _sessions.Add(newSession);
                Logger.System($"새 클라이언트 연결됨. 세션 ID : {newSession.SessionId}");
            }
        }

        private async void HandleDataReceived(NetworkSession session, GameData data)
        {
            Logger.Debug($"데이터 수신. 세션 ID : {session.SessionId}, 데이터 타입 : {data.GetType().Name}");
            List<GameData> responses = new List<GameData>();
            List<GameData> broadcast_res = new List<GameData>();

            lock (_handlelock)
            {
                switch (data) // 데이터 분기 처리 (서버)
                {
                    case ChatData chatData:
                        chatData.SenderNickname = session.Nickname; // 닉네임 바꿔서 패킷 전송해도 그냥 서버에서 저장된 닉네임으로
                        broadcast_res.Add(chatData);
                        break;
                    case PositionData positionData:
                        try
                        {
                            manager.TryPlaceStone(positionData);
                            if(manager.CheckWin(positionData))
                            {
                                GameEndData enddata = new GameEndData()
                                {
                                    Winner = positionData.Player
                                };

                                broadcast_res.Add(enddata);
                            }
                        }
                        catch(InvalidPlaceException ex)
                        {
                            ResponseData response = new PlaceResponseData()
                            {
                                Accepted = false,
                                Position = positionData,
                            };
                            responses.Add(response);
                        }
                        break;
                    case ClientJoinData joinData:
                        string finalnickname = GenerateUniqueNickname(joinData.Nickname);
                        session.Nickname = finalnickname;

                        var res = new ClientJoinResponseData()
                        {
                            Accepted = true,
                            ConfirmedNickname = finalnickname                            
                        };

                        responses.Add(res);
                        broadcast_res.Add(res);

                        var syncdata = new GameSyncData()
                        {
                            CurrentTurn = manager.CurrentPlayer,
                            MoveHistory = manager.StoneHistory
                        };

                        responses.Add(syncdata);


                        break;
                    default:
                        Logger.Error($"알 수 없는 데이터 타입 수신. 세션 ID : {session.SessionId}");
                        break;
                }
            }

            if(responses.Count > 0)
            {
                foreach (var response in responses)
                    await session.SendAsync(response);
            }

            if(broadcast_res.Count > 0)
            {
                foreach (var response in broadcast_res)
                    await Broadcast(response);
            }

        }

        private string GenerateUniqueNickname(string nickname)
        {
            nickname = nickname.Trim().Replace(" ", ""); // 공백 제거
            if (string.IsNullOrEmpty(nickname)) nickname = "익명";
            int count = 0;
            foreach (var session in _sessions)
            {
                var nicknamesplit = session.Nickname.Split(' '); 
                // "익명 (2)" 의 경우 앞의 "익명" 이 현재 닉네임과 중복되는지 체크

                if(nicknamesplit[0] == nickname)
                    count++;
            }

            return nickname + $" ({count})"; // 닉네임 (5) 형태
        }

        private async void HandleClientDisconnected(NetworkSession session)
        {
            lock (_handlelock)
            {
                _sessions.Remove(session);
            }
            Logger.System($"클라이언트 연결 끊김. 세션 ID : {session.SessionId}");

            await Broadcast(new ClientExitData() {Nickname = session.Nickname});
        }

        public async Task Broadcast(GameData data)
        {
            List<NetworkSession> targetSessions;

            lock(_handlelock)
            { // 브로드캐스트 도중 세션 종료된 경우 보호
                targetSessions = new List<NetworkSession>(_sessions);
            }

            foreach (var session in targetSessions)
            {
                await session.SendAsync(data);
            }
        }

        public async Task BroadcastChat(string msg)
        {
            var chatdata = new ChatData
            {
                Message = msg
            };

            await Broadcast(chatdata);
        }
    }
}
