using Gomoku.Models.DTO;

namespace Gomoku.Models.Domain
{
    /// <summary>
    /// 오목 보드의 상태를 관리하는 클래스입니다.
    /// </summary>
    public class GomokuBoard
    {
        private readonly GameMove?[,] _board;

        private List<GameMove> _StoneHistory { get; } = new();

        public GomokuBoard(int width, int height)
        {
            _board = new GameMove[width, height];
        }

        public int this[int x, int y]
        {
            get => (int?)(_board[x, y]?.PlayerType) ?? 0;

            set
            {
                if (value == 0)
                {
                    if (_board[x, y] != null)
                    {
                        _StoneHistory.Remove(_board[x, y]!);
                        _board[x, y] = null;
                    }
                }
                else
                {
                    var move = new GameMove(x, y, _StoneHistory.Count + 1, (PlayerType)value);
                    _board[x, y] = move;
                    _StoneHistory.Add(move);
                }
            }
        }

        public GameMove? GetGameMove(int x, int y) => _board[x, y];

        public GameMove? GetLastStonePos()
        {
            if (Count == 0) return null;
            return _StoneHistory.Last();
        }

        public IReadOnlyList<GameMove> GetHistory() => _StoneHistory;
        public int Count => _StoneHistory.Count;
        public void Clear()
        {
            Array.Clear(_board, 0, _board.Length);
            _StoneHistory.Clear();
        }

        internal void CancelLastStone()
        {
            var last = GetLastStonePos() ?? throw new InvalidOperationException("마지막 돌이 없음");

            _board[last.X, last.Y] = null;
            _StoneHistory.RemoveAt(_StoneHistory.Count - 1);
        }
    }
}
