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

        public PlayerType CurrentPlayer
        {
            get;
            private set
            {
                field = value;
                OnTurnChanged?.Invoke(CurrentPlayer);
            }
        }
        public List<PositionData> StoneHistory { get; set; } = new List<PositionData>();
        public List<Rule> Rules { get; set; } = new List<Rule>();
        public int[,] Board { get; set; } = new int[BOARD_SIZE, BOARD_SIZE];

        public int BlackSeconds { get; set; } = 30;
        public int WhiteSeconds { get; set; } = 30;

        public event Action<int, int>? OnTimerTick; // 시간 줄어들때마다 이벤트

        public bool IsGameStarted { get; set; } = false;


        public event Action<PositionData>? OnStonePlaced; // 돌 놓였을때
        public event Action<PlayerType, string>? OnGameEnded; // 게임 종료 시 (Observer = 비김)
        public event Action<PlayerType>? OnTurnChanged; // 바뀐 턴 플레이어
        public event Action? OnGameStarted;
        public event Action? OnGameReset;

        public GomokuManager()
        {
        }

        public void NewSession()
        {
            ResetGame();
            Rules.Clear();
        }

        /// <summary>
        /// 오목 게임 상태를 동기화합니다.
        /// </summary>
        /// <param name="data">동기화할 데이터</param>
        public void SyncState(GameSyncData data)
        {
            Logger.Debug("게임 상태 동기화");

            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                    Board[i, j] = 0;
            }
            StoneHistory.Clear();

            foreach (var ruleinfo in data.SelectedRules)
            {
                Rules.Add(RuleFactory.CreateRule(ruleinfo));
            }

            if (data.MoveHistory.Count > 0)
            {
                StartGame();

                foreach (var place in data.MoveHistory)
                {
                    TryPlaceStone(place);
                    StoneHistory.Add(place);
                }
                CurrentPlayer = data.CurrentTurn;
            }

        }
        /// <summary>
        /// 호출 시 현재 턴 플레이어의 남은 시간을 1 줄입니다.
        /// </summary>
        /// <param name="playerType">줄일 플레이어, 현재 턴이 아닐시 무시됩니다.</param>
        public void Tick(PlayerType playerType)
        {
            if (!IsGameStarted) return;
            if (playerType != CurrentPlayer) return;

            if (playerType == PlayerType.Black) BlackSeconds--;
            else if (playerType == PlayerType.White) WhiteSeconds--;

            OnTimerTick?.Invoke(BlackSeconds, WhiteSeconds);
        }
        /// <summary>
        /// 게임을 강제로 종료합니다. OnGameEnded가 트리거됩니다.
        /// </summary>
        /// <param name="winner">승리자</param>
        /// <param name="reason">승리 이유</param>
        public void ForceGameEnd(PlayerType winner, string reason)
        {
            if (!IsGameStarted) return;
            IsGameStarted = false;
            OnGameEnded?.Invoke(winner, reason);
        }
        /// <summary>
        /// 좌표가 보드 범위를 넘어서는지 확인합니다.
        /// </summary>
        /// <param name="x">x 좌표</param>
        /// <param name="y">y 좌표</param>
        /// <returns>가능하다면 true, 불가능하다면 false</returns>
        public static bool IsValidPos(int x, int y)
        {
            return (0 <= x && x < BOARD_SIZE && 0 <= y && y < BOARD_SIZE);
        }
        /// <summary>
        /// 좌표에 있는 돌을 반환합니다.
        /// </summary>
        /// <param name="x">x 좌표</param>
        /// <param name="y">y 좌표</param>
        /// <returns>0: 없음, 1: 흑돌, 2: 백돌</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int GetStoneAt(int x, int y)
        {
            if (!IsValidPos(x, y))
                throw new ArgumentOutOfRangeException($"보드 범위 초과 {x}, {y}");
            return Board[x, y];
        }

        /// <summary>
        /// 가능하다면 돌을 착수합니다. 승리를 체크하지 않습니다. 착수 불가하다면 예외를 던집니다.
        /// </summary>
        /// <param name="pos">착수할 위치</param>
        /// <returns>착수되었다면 true, 아니면 예외를 던집니다.</returns>
        /// <exception cref="OutOfBoardException">보드의 좌표를 넘어선 착수 시</exception>
        /// <exception cref="AlreadyPlacedException">이미 착수된 좌표에 착수 시</exception>
        /// <exception cref="NotYourTurnException">자신의 턴이 아닐 때 착수 시</exception>
        /// <exception cref="RuleException">룰을 위반하는 착수일시</exception>
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

            if (Board[x, y] != 0)
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

            foreach (var rule in Rules) // 룰 순회하며 체크
            {
                if (!rule.IsValidMove(this, pos))
                {
                    throw new RuleException(rule.ViolationMessage);
                }
            }
            Board[x, y] = playercolor;
            StoneHistory.Add(pos);

            OnStonePlaced?.Invoke(pos);

            CurrentPlayer = (player == PlayerType.Black) ? PlayerType.White : PlayerType.Black;
            // 활성화 플레이어 변경
            BlackSeconds = 30;
            WhiteSeconds = 30;

            return true;
        }
        /// <summary>
        /// 현재 착수로 승리할 수 있는지 체크합니다. 승리한다면, 게임을 종료하고 true를 반환합니다.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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

            for (int i = 0; i < 4; i++)
            {
                int count = 1; // 방금 둔 돌

                for (int j = 1; j < 5; j++)
                {
                    int nx = x + dx[i] * j; // 좌표에 dx * j번째 돌 확인
                    int ny = y + dy[i] * j;
                    if (!IsValidPos(nx, ny) || Board[nx, ny] != color) break; // 다른 돌 있으면 탈출
                    count++;
                }

                for (int j = 1; j < 5; j++) // 역방향
                {
                    int nx = x - dx[i] * j;
                    int ny = y - dy[i] * j;
                    if (!IsValidPos(nx, ny) || Board[nx, ny] != color) break;
                    count++;
                }

                if (count >= 5) // 5개 이상이면 승리
                {
                    OnGameEnded?.Invoke(player, "승리");
                    IsGameStarted = false;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 게임을 리셋합니다. 착수된 돌, 남은 시간 등을 초기화합니다. OnGameReset을 트리거합니다.
        /// </summary>
        public void ResetGame()
        {
            Logger.Debug("게임 리셋됨");
            for (int i = 0; i < BOARD_SIZE; i++) // 보드 초기화
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                    Board[i, j] = 0;
            }


            StoneHistory.Clear();

            BlackSeconds = 30;
            WhiteSeconds = 30;

            OnGameReset?.Invoke();
        }

        /// <summary>
        /// 게임을 리셋하고 시작합니다. OnGameReset과 OnGameStarted가 트리거됩니다.
        /// </summary>
        public void StartGame()
        {
            ResetGame();
            IsGameStarted = true;
            CurrentPlayer = PlayerType.Black; // 선수: 흑돌
            OnGameStarted?.Invoke();
        }
    }
}
