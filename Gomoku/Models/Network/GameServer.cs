/*
 * GameServer.cs
 * 실제 클라이언트와의 통신, 패킷 유효성 검사, 게임 진행 담당
 */
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Models.Network;
using Gomoku.Services.Applications.Database;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Timers;

namespace Gomoku.Models
{
    // TODO: 리팩토링 필요함 게임 진행/수신 패킷 처리/송,수신/세션-플레이어 목록 관리 로 나누기
    public partial class GameServer : IGameServer
    {
        private TcpListener? _listener;

        private Channel<(INetworkSession, GameData)> _sendChannel = Channel.CreateUnbounded<(INetworkSession, GameData)>();
        // 패킷 보내기 채널

        private Task? _sendTask;
        private Task? _acceptTask;

        private readonly ConcurrentDictionary<INetworkSession, Player> _sessions = new();

        private readonly CancellationTokenSource _ServerCts = new();

        private readonly GomokuManager manager = new GomokuManager();

        private readonly object _gameLock = new object();

        private readonly INetworkSessionFactory _sessionFactory;
        private readonly IDatabaseService _databaseService;

        private INetworkSession? _blackPlayer;
        private INetworkSession? _whitePlayer;
        private readonly System.Timers.Timer _gametimer = new System.Timers.Timer(1000);
        private readonly System.Timers.Timer _heartbeattimer = new System.Timers.Timer(5000);

        public bool IsRunning => _listener != null;

        internal ConnectionOption? _connectionOption;

        public GameServer(INetworkSessionFactory sessionFactory, IDatabaseService databaseService)
        {
            _sessionFactory = sessionFactory;
            _databaseService = databaseService;

            _gametimer.Elapsed += GameTimerElapsed;
            manager.GameEnded += async (gameend) => // 게임 종료 시에 모든 클라에게 결과 방송
            {
                _gametimer.Stop();
                GameEndData enddata = new GameEndData()
                {
                    EndData = gameend
                };

                var black = GetPlayerOrNull(_blackPlayer)!;
                var white = GetPlayerOrNull(_whitePlayer)!;

                if (gameend.Winner == PlayerType.Black)
                {
                    black.Records.Win += 1;
                    white.Records.Loss += 1;
                }
                else if (gameend.Winner == PlayerType.White)
                {
                    white.Records.Win += 1;
                    black.Records.Loss += 1;
                }
                else if (gameend.Winner == PlayerType.Observer)
                {
                    black.Records.Draw += 1;
                    white.Records.Draw += 1;
                }

                if (black.Id != 1 || white.Id != 1)
                {   // 둘 다 게스트인 경우 저장 안함
                    var blackinfo = new MatchPlayerInfo(black.Id, black.Nickname);
                    var whiteinfo = new MatchPlayerInfo(white.Id, white.Nickname);

                    var moves = manager.Board.GetHistory() ?? Enumerable.Empty<GameMove>();

                    var matchinfo = new MatchInfo(blackinfo, whiteinfo, gameend.Winner, gameend.Reason, moves, DateTime.Now);

                    await _databaseService.SaveMatchAsync(matchinfo).ConfigureAwait(false);
                    // 매치 정보 저장
                    Logger.Info($"매치 정보 저장 완료. {matchinfo.BlackPlayer.Nickname} vs {matchinfo.WhitePlayer.Nickname}");
                }

                AddBroadcast(enddata);
            };

            _heartbeattimer.Elapsed += async (s, e) => // 핑 송신 및 오래된 세션 정리
            {
                AddBroadcast(new PingData());

                List<KeyValuePair<INetworkSession, Player>> sessionToDisconnect = [];

                var now = DateTime.Now;

                foreach (var sessionplayer in _sessions)
                {
                    if ((now - sessionplayer.Key.LastActiveTime).TotalSeconds > 15) // 오래 응답 없는 세션 
                        sessionToDisconnect.Add(sessionplayer);
                }

                foreach (var sessionplayer in sessionToDisconnect)
                {
                    DisconnectSession(sessionplayer.Key);
                }
            };

            _sendTask = StartSenderAsync(_ServerCts.Token);
        }

        private async void GameTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            TimePassedData? timepasspacket = null;
            lock (_gameLock) // 초마다 시간 까는 타이머, 다까졌으면 게임 종료, 아니면 시간 패킷 전송
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
                    PlayerType = manager.CurrentPlayer
                };
            }

            if (timepasspacket != null)
                AddBroadcast(timepasspacket);
        }

        #region 연결 끊김 처리
        private void HandleClientDisconnected(INetworkSession session)
        {
            if (!IsRunning) return; // 서버 종료 중에는 연결 끊김 신호 안보냄

            Logger.System($"클라이언트 연결 끊김 세션 ID : {session.SessionId}");
            DisconnectSession(session);
        }
        private void DisconnectSession(INetworkSession session)
        {
            lock (_gameLock)
            {
                // 게임 종료 처리
                if (manager.IsGameStarted)
                {
                    if (session == _blackPlayer || session == _whitePlayer)
                    { // 게임 참가자가 나간거라면?
                        var winner = (session == _blackPlayer) ? PlayerType.White : PlayerType.Black;
                        Logger.Info("게임 참가자 나감. 게임 종료 처리");
                        manager.ForceGameEnd(winner, "게임 나감");
                    }
                }

                if (session == _blackPlayer)
                    _blackPlayer = null;
                else if (session == _whitePlayer)
                    _whitePlayer = null;
            }

            if (!_sessions.TryRemove(session, out var player)) return;
            // 제거 시도

            session.Disconnect();
            // 세션 연결 종료

            if (session.IsAuthenticated)
                AddBroadcast(new ClientExitData() { Player = player });
            // 퇴장 알림
        }
        #endregion

        #region 송신
        private void AddUnicast(INetworkSession target, GameData data)
        {
            _sendChannel.Writer.TryWrite((target, data));
        }

        private void AddBroadcast(GameData data)
        {
            foreach (var session in _sessions.Keys)
            {
                if (data is not PingData && !session.IsAuthenticated)
                    continue;
                _sendChannel.Writer.TryWrite((session, data));
            }
        }

        private async Task StartSenderAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var (target, data) in _sendChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    try
                    {
                        await target.SendAsync(data).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"전송 중 오류 발생: {e.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
        #endregion
        public void StartGame()
        {
            manager.StartGame();
            _gametimer.Start();
        }

        public void AddRule(Rule rule)
        {
            manager.Rules.Add(rule);
        }
        private Player? GetPlayerOrNull(INetworkSession? session)
        {
            if (session == null) return null;

            if (_sessions.TryGetValue(session, out Player? value))
                return value;

            return null;
        }
        internal string GenerateGuestNickname(INetworkSession client)
        {
            string nickname = "Guest";
            string pattern = $@"^{nickname}\s\((\d+)\)$";
            // 닉네임 (숫자) 형태

            var usedNumbers = new HashSet<int>();
            bool isBaseNameUsed = false;

            foreach (var sessionplayer in _sessions)
            {
                if (client == sessionplayer.Key) continue; // 자기 자신은 제외

                if (sessionplayer.Value.Nickname == nickname)
                    isBaseNameUsed = true; // 이름 이미 사용 중
                else
                {
                    Match match = Regex.Match(sessionplayer.Value.Nickname, pattern);

                    if (match.Success) // 패턴과 일치하면
                    {
                        if (int.TryParse(match.Groups[1].Value, out int num))
                            usedNumbers.Add(num); // 집합에 숫자 추가
                    }
                }
            }

            if (!isBaseNameUsed) return nickname; // 이름 사용 안하고 있으니

            int nicknum = 1;

            while (usedNumbers.Contains(nicknum)) // 집합에서 숫자 찾으면 1증가 (안쓰는 가장 작은 숫자 찾기)
                nicknum++;

            return $"{nickname} ({nicknum})";
        }

        public async ValueTask DisposeAsync()
        {
            _ServerCts.Cancel();
            _heartbeattimer.Stop();
            _gametimer.Stop();

            try { _listener?.Stop(); } catch { }
            _listener = null;

            _heartbeattimer.Dispose();
            _gametimer.Dispose();

            foreach (var sessionplayer in _sessions)
            {
                sessionplayer.Key.Disconnect();
            }

            _sendChannel.Writer.TryComplete();

            if (_sendTask != null)
                await _sendTask;

            if (_acceptTask != null)
                await _acceptTask;

            _ServerCts.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
