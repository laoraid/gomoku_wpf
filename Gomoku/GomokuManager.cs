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
        private List<PositionData> _stoneHistory = new List<PositionData>();
        private List<Rule> _rules = new List<Rule>();
       

        public PlayerType CurrentPlayer { get => _currentPlayer; }
        public List<PositionData> StoneHistory { get => _stoneHistory; }
        public List<Rule> Rules { get => _rules; }
        public int[,] Board { get => _board; }


        public event Action<PositionData>? OnStonePlaced; // x, y, player type
        public event Action<PlayerType>? OnGameEnded; // winner
        public event Action<PlayerType>? OnTurnChanged; // current player
        public event Action? OnGameReset;

        public GomokuManager()
        {
            ResetGame();
        }

        private bool IsVaildPos(int x, int y)
        {
            return (0 <= x && x < BOARD_SIZE && 0 <= y && y < BOARD_SIZE);
        }

        public int GetStoneAt(int x, int y)
        {
            if (IsVaildPos(x,y))
                throw new ArgumentOutOfRangeException($"보드 범위 초과 {x}, {y}");
            return _board[x, y];
        }

        public bool TryPlaceStone(PositionData pos)
        {
            int x = pos.X;
            int y = pos.Y;
            PlayerType player = pos.Player;

            if (!IsVaildPos(x, y))
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

            OnStonePlaced?.Invoke(new PositionData() { X=x, Y=y, MoveNumber=_stoneHistory.Count });

            _currentPlayer = (player == PlayerType.Black) ? PlayerType.White : PlayerType.Black;
            // 활성화 플레이어 변경
            OnTurnChanged?.Invoke(_currentPlayer);

            return true;
        }

        public void CheckWin(int x, int y, PlayerType player)
        {
            if (player != PlayerType.Observer)
            {
                // TODO : 승리 확인
                OnGameEnded?.Invoke(player);
            }

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

        public bool CheckWin(PositionData pos)
        {
            throw new NotImplementedException();
        }
    }
}
