using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Windows.Ink;

namespace Gomoku.Models
{
    public class GameServer
    {
        private TcpListener _listener;

        private List<NetworkSession> _sessions = new List<NetworkSession>();

        private GomokuManager manager = new GomokuManager();

        private object _handlelock = new object();

        private NetworkSession? _blackPlayer;
        private NetworkSession? _whitePlayer;
        private System.Timers.Timer _gametimer = new System.Timers.Timer(1000);
        private System.Timers.Timer _heartbeattimer = new System.Timers.Timer(5000);

        public GameServer()
        {
            _gametimer.Elapsed += SetTimer;
            manager.OnGameEnded += async (winner, reason) => // 게임 종료 시에 모든 클라에게 결과 방송
            {
                _gametimer.Stop();
                GameEndData enddata = new GameEndData()
                {
                    Winner = winner,
                    Reason = reason
                };

                await Broadcast(enddata);
            };
        }

        public void StartGame()
        {
            manager.StartGame();
            _gametimer.Start();
        }

        public async void SetTimer(object? sender, ElapsedEventArgs e)
        {
            TimePassedData? timepasspacket = null;
            lock (_handlelock) // 초마다 시간 까는 타이머, 다까졌으면 게임 종료, 아니면 시간 패킷 전송
            {
                if (!manager.IsGameStarted) return;

                manager.Tick(manager.CurrentPlayer);

                if (manager.CurrentPlayer == PlayerType.Black)
                {
                    if (manager.BlackSeconds <= 0)
                    {
                        manager.ForceGameEnd(PlayerType.White, "시간 초과");
                        return;
                    }
                }
                else if (manager.CurrentPlayer == PlayerType.White)
                {
                    if (manager.WhiteSeconds <= 0)
                    {
                        manager.ForceGameEnd(PlayerType.Black, "시간 초과");
                        return;
                    }
                }

                timepasspacket = new TimePassedData()
                {
                    CurrentLeftTimeSeconds = manager.CurrentPlayer == PlayerType.Black ? manager.BlackSeconds : manager.WhiteSeconds,
                    Player = manager.CurrentPlayer
                };
            }

            if(timepasspacket != null)
                await Broadcast(timepasspacket);
        }

        public async Task StartAsync(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            Logger.System($"서버 시작 됨. 포트 : {port}");

            _ = Task.Run(AccpetClientsAsync); // 비동기적으로 클라이언트 수락 시작

            _heartbeattimer.Elapsed += async (s, e) => // 핑 송신 및 오래된 세션 정리
            {
                await Broadcast(new PingData());

                List<NetworkSession> sessionToDisconnect = new List<NetworkSession>();

                lock(_handlelock)
                {
                    var now = DateTime.Now;

                    foreach (var session in _sessions)
                    {
                        if ((now - session.LastActiveTime).TotalSeconds > 15) // 오래 응답 없는 세션
                            sessionToDisconnect.Add(session);
                    }
                }

                foreach(var session in sessionToDisconnect)
                {
                    session.Disconnect();
                    await Broadcast(new ClientExitData() { Nickname = session.Nickname });
                }
            };

            _heartbeattimer.Start();
        }

        public void StopServer()
        {
            _listener.Stop();
            _heartbeattimer.Stop();
            _gametimer.Stop();
        }

        private async Task AccpetClientsAsync()
        {
            try
            {
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
            catch (Exception ex)
            {
                Logger.Error($"클라이언트 연결 수락 중 오류 발생 : {ex.Message}");
                StopServer();
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
                        if (!manager.IsGameStarted) return;
                        try
                        {
                            manager.TryPlaceStone(positionData);
                            _gametimer.Stop();
                            broadcast_res.Add(positionData); // catch 안되면 돌 둔것
                            if(!manager.CheckWin(positionData))
                            {
                                _gametimer.Start();
                            }
                        }
                        catch (InvalidPlaceException)
                        {
                            ResponseData response = new PlaceResponseData()
                            {
                                Accepted = false,
                                Position = positionData,
                            };
                            responses.Add(response);
                        }
                        break;
                    case ClientJoinData joinData: // 클라이언트 최초 접속시
                        string finalnickname = GenerateUniqueNickname(session, joinData.Nickname);
                        session.Nickname = finalnickname;

                        var res = new ClientJoinResponseData() // 접속 확인 응답
                        {
                            Accepted = true,
                            ConfirmedNickname = finalnickname,
                            Users = _sessions.Select((s) => s.Nickname).ToList()
                        };

                        responses.Add(res);

                        var join_broadcast = new ClientJoinData() // 모두에게 접속했다고 방송
                        {
                            Nickname = finalnickname
                        };

                        broadcast_res.Add(join_broadcast);

                        var syncdata = new GameSyncData() // 게임 진행 데이터 전송
                        {
                            CurrentTurn = manager.CurrentPlayer,
                            MoveHistory = manager.StoneHistory,
                            Rules = manager.Rules
                        };

                        responses.Add(syncdata);

                        // 게임 참가자 정보 전송
                        if(_blackPlayer != null)
                        {
                            responses.Add(new GameJoinData()
                            {
                                Nickname = _blackPlayer.Nickname,
                                Type = PlayerType.Black
                            });
                        }

                        if(_whitePlayer != null)
                        {
                            responses.Add(new GameJoinData()
                            {
                                Nickname = _whitePlayer.Nickname,
                                Type = PlayerType.White
                            });
                        }

                        break;

                    case GameJoinData joindata:
                        if (_blackPlayer == session || _whitePlayer == session)
                            // 이미 흑백 들어간 사람이라면
                            break;
                        if (joindata.Type == PlayerType.Black)
                            _blackPlayer = session;
                        else
                            _whitePlayer = session;

                        broadcast_res.Add(joindata);
                        break;

                    case GameLeaveData leaveData:
                        if(_blackPlayer != session && _whitePlayer != session)
                            // 안들어간 사람이 나가기 요청한거라면
                            break;

                        PlayerType winner;

                        if (leaveData.Type == PlayerType.Black)
                        {
                            _blackPlayer = null;
                            winner = PlayerType.White;
                        }
                        else
                        {
                            _whitePlayer = null;
                            winner = PlayerType.Black;
                        }

                        manager.ForceGameEnd(winner, "게임 나감");

                        broadcast_res.Add(leaveData);
                        break;
                    case GameStartData gamestartdata:
                        if (_blackPlayer != session) // 흑 플레이어가 요청한게 아니라면
                            break;

                        broadcast_res.Add(gamestartdata);
                        StartGame();
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

        private string GenerateUniqueNickname(NetworkSession client, string nickname)
        {
            nickname = nickname.Trim().Replace(" ", ""); // 공백 제거
            if (string.IsNullOrEmpty(nickname)) nickname = "익명";
            int count = 0;
            foreach (var session in _sessions)
            {
                if(client == session) continue; // 자기 자신은 제외
                var nicknamesplit = session.Nickname.Split(' '); 
                // "익명 (2)" 의 경우 앞의 "익명" 이 현재 닉네임과 중복되는지 체크

                if(nicknamesplit[0] == nickname)
                    count++;
            }

            if (count == 0)
                return nickname; // 중복 없으면 닉네임 그대로

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

            if(manager.IsGameStarted)
            {
                if (session == _blackPlayer || session == _whitePlayer)
                { // 게임 참가자가 나간거라면?
                    var winner = (session == _blackPlayer) ? PlayerType.White : PlayerType.Black;
                    manager.ForceGameEnd(winner, "게임 나감");
                }
            }
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
