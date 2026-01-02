using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Gomoku.Models
{
    public enum PlayerType
    {
        Observer = 0,
        Black = 1,
        White = 2
    }
    public class GomokuManager
    {
        public const int BOARD_SIZE = 15;

        private int[,] _board = new int[BOARD_SIZE, BOARD_SIZE];

        public PlayerType CurrentPlayer { get; 
            private set
            {
                field = value;
                OnTurnChanged?.Invoke(CurrentPlayer);
            }
        }
        public List<PositionData> StoneHistory { get; set; } = new List<PositionData>();
        public List<Rule> Rules { get; set; } = new List<Rule>();
        public int[,] Board { get => _board; }

        public int BlackSeconds { get; set; } = 30;
        public int WhiteSeconds { get; set; } = 30;

        public event Action<int, int>? OnTimerTick; // 시간 줄어들때마다 이벤트

        public bool IsGameStarted { get; set; } = false;


        public event Action<PositionData>? OnStonePlaced; // 돌 놓였을때
        public event Action<PlayerType, string>? OnGameEnded; // 게임 종료 시 (Observer = 비김)
        public event Action<PlayerType>? OnTurnChanged; // 바뀐 턴 플레이어
        public event Action OnGameStarted;
        public event Action? OnGameReset;

        public GomokuManager()
        {
        }

        public void SyncState(GameSyncData data)
        {
            ResetGame();
            Rules = data.Rules;

            if (data.MoveHistory.Count > 0)
            {
                StartGame();

                foreach (var place in data.MoveHistory)
                {
                    TryPlaceStone(place);
                }
                CurrentPlayer = data.CurrentTurn;
            }

        }

        public void Tick(PlayerType playerType)
        {
            if (!IsGameStarted) return;
            if (playerType != CurrentPlayer) return;

            if (playerType == PlayerType.Black) BlackSeconds--;
            else if (playerType == PlayerType.White) WhiteSeconds--;

            OnTimerTick?.Invoke(BlackSeconds, WhiteSeconds);
        }

        public void ForceGameEnd(PlayerType winner, string reason)
        {
            if (!IsGameStarted) return;
            IsGameStarted = false;
            OnGameEnded?.Invoke(winner, reason);
        }

        private bool IsValidPos(int x, int y)
        {
            return (0 <= x && x < BOARD_SIZE && 0 <= y && y < BOARD_SIZE);
        }

        public int GetStoneAt(int x, int y)
        {
            if (!IsValidPos(x,y))
                throw new ArgumentOutOfRangeException($"보드 범위 초과 {x}, {y}");
            return _board[x, y];
        }

        public bool TryPlaceStone(PositionData pos)
        {
            int x = pos.X;
            int y = pos.Y;
            PlayerType player = pos.Player;

            if (!IsValidPos(x, y))
            {
                Logger.Debug($"불가능한 착수 : {x}, {y}");
                throw new OutOfBoardException("보드 범위를 벗어났습니다.");
            }

            if (_board[x,y] != 0)
            {
                Logger.Debug($"이미 돌 있음 : {x} , {y}");
                throw new AlreadyPlacedException("이미 돌이 착수된 곳입니다.");
            }
            if (player != CurrentPlayer)
            {
                Logger.Debug($"턴 아님 : {player}");
                throw new NotYourTurnException("당신의 턴이 아닙니다.");
            }

            int playercolor = (int)player;

            foreach (var rule in Rules)
            {
                if (!rule.IsVaildMove(this, pos))
                {
                    throw new RuleException(rule.violation_message);
                }
            }
            _board[x, y] = playercolor;
            StoneHistory.Add(pos);

            OnStonePlaced?.Invoke(pos);

            CurrentPlayer = (player == PlayerType.Black) ? PlayerType.White : PlayerType.Black;
            // 활성화 플레이어 변경
            BlackSeconds = 30;
            WhiteSeconds = 30;

            return true;
        }

        public bool CheckWin(PositionData data)
        {
            var player = data.Player;
            int x = data.X;
            int y = data.Y;

            if (player == PlayerType.Observer)
                throw new InvalidOperationException("관전자는 돌을 둘 수 없습니다.");

            int color = (int)player;

            // 가로 - 세로 | 대각선 \ 반대대각선 /
            int[] dx = { 1, 0, 1, 1 };
            int[] dy = { 0, 1, 1, -1 };

            for (int i=0; i<4; i++)
            {
                int count = 1; // 방금 둔 돌

                for(int j=1; j<5; j++)
                {
                    int nx = x + dx[i] * j; // 좌표에 dx * j번째 돌 확인
                    int ny = y + dy[i] * j;
                    if (!IsValidPos(nx, ny) || _board[nx, ny] != color) break;
                    count++;
                }

                for(int j=1; j<5; j++) // 역방향
                {
                    int nx = x - dx[i] * j;
                    int ny = y - dy[i] * j;
                    if (!IsValidPos(nx, ny) || _board[nx, ny] != color) break;
                    count++;
                }

                if (count >= 5)
                {
                    OnGameEnded?.Invoke(player, "승리");
                    IsGameStarted = false;
                    return true;
                }
            }
            return false;
        }

        public void ResetGame()
        {
            for (int i = 0; i < BOARD_SIZE; i++) // 보드 초기화
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                    _board[i, j] = 0;
            }


            StoneHistory.Clear();

            BlackSeconds = 30;
            WhiteSeconds = 30;

            OnGameReset?.Invoke();
        }
        public void StartGame()
        {
            ResetGame();
            OnGameStarted?.Invoke();
            IsGameStarted = true;
            CurrentPlayer = PlayerType.Black; // 선수: 흑돌
        }
    }
}
