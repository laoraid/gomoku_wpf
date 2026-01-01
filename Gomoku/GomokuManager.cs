using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Gomoku
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

        private PlayerType _currentPlayer = PlayerType.Black;
        private List<PositionData> _stoneHistory = new List<PositionData>(); // 돌 놓은 기록
        private List<Rule> _rules = new List<Rule>(); // 룰 리스트
       

        public PlayerType CurrentPlayer { get => _currentPlayer; }
        public List<PositionData> StoneHistory { get => _stoneHistory; }
        public List<Rule> Rules { get => _rules; }
        public int[,] Board { get => _board; }


        public event Action<PositionData>? OnStonePlaced; // 돌 놓였을때
        public event Action<PlayerType>? OnGameEnded; // 게임 종료 시 (Observer = 비김)
        public event Action<PlayerType>? OnTurnChanged; // 바뀐 턴 플레이어
        public event Action? OnGameReset;

        public GomokuManager()
        {
            ResetGame();
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
            if (player != _currentPlayer)
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
            _stoneHistory.Add(pos);

            OnStonePlaced?.Invoke(pos);

            _currentPlayer = (player == PlayerType.Black) ? PlayerType.White : PlayerType.Black;
            // 활성화 플레이어 변경
            OnTurnChanged?.Invoke(_currentPlayer);

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
                    OnGameEnded?.Invoke(player);
                    return true;
                }
            }
            return false;
        }

        public void ResetGame()
        {
            for(int i=0; i < BOARD_SIZE; i++) // 보드 초기화
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                    _board[i, j] = 0;
            }

            _currentPlayer = PlayerType.Black; // 선수: 흑돌
            _stoneHistory.Clear();

            OnGameReset?.Invoke();
        }
    }
}
