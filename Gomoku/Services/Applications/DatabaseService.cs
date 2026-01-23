using Gomoku.Helpers;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Interfaces;
using Microsoft.Data.Sqlite;

namespace Gomoku.Services.Applications
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _dbString;

        public DatabaseService() : this("Data Source=Server.db")
        {
        }

        public DatabaseService(string dbstring) // 테스트용 db 파일 이름 변경
        {
            _dbString = dbstring;
            Initialize();
        }

        private void Initialize()
        {
            using (var db = new SqliteConnection(_dbString))
            {
                db.Open();

                var pragmaCommand = db.CreateCommand();
                pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCommand.ExecuteNonQuery();
                // 이거 안하면 외래키 사용 불가

                var usersTableCommand = db.CreateCommand();
                usersTableCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Win INTEGER NOT NULL,
                    Loss INTEGER NOT NULL,
                    Draw INTEGER NOT NULL
                    );";
                usersTableCommand.ExecuteNonQuery();

                var matchTableCommand = db.CreateCommand();
                matchTableCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Matches (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BlackPlayerId INTEGER NOT NULL,
                    WhitePlayerId INTEGER NOT NULL,
                    WinnerType INTEGER NOT NULL,
                    Reason TEXT NOT NULL,
                    MatchTime TEXT NOT NULL,
                    FOREIGN KEY (BlackPlayerId) REFERENCES Users(Id),
                    FOREIGN KEY (WhitePlayerId) REFERENCES Users(Id)
                    );";
                matchTableCommand.ExecuteNonQuery();

                var matchMovesTableCommand = db.CreateCommand();
                matchMovesTableCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS MatchMoves (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MatchId INTEGER NOT NULL,
                    MoveNumber INTEGER NOT NULL,
                    X INTEGER NOT NULL,
                    Y INTEGER NOT NULL,
                    PlayerType INTEGER NOT NULL,
                    FOREIGN KEY (MatchId) REFERENCES Matches(Id) ON DELETE CASCADE
                    );"; // 매치 삭제되면 이것도 삭제
                matchMovesTableCommand.ExecuteNonQuery();

                var guestCommand = db.CreateCommand();
                guestCommand.CommandText = @"
                    INSERT INTO Users (Id, UserId, PasswordHash, Win, Loss, Draw)
                    VALUES (1, 'Guest', 'None', 0, 0, 0)
                    ON CONFLICT(Id) DO UPDATE SET
                        UserId = 'Guest',
                        PasswordHash = 'None',
                        Win = 0,
                        Loss = 0,
                        Draw = 0;";
                guestCommand.ExecuteNonQuery();
                // 아이디 1이면 게스트 계정

            }
        }

        public async Task<Player> CreateAccountAsync(string id, string hashedpw)
        {
            using (var db = new SqliteConnection(_dbString))
            {
                try
                {
                    await db.OpenAsync();
                    var cmd = db.CreateCommand();

                    cmd.CommandText = @"INSERT INTO Users (UserId, PasswordHash, Win, Loss, Draw)
                                    VALUES (@id, @hashedpw, 0, 0, 0)
                                    RETURNING Id;"; // 방금 들어간 계정 가져오기
                    cmd.Parameters.AddWithValue("@id", id);
                    hashedpw = HashHelper.SHA256Hash(hashedpw);
                    cmd.Parameters.AddWithValue("@hashedpw", hashedpw);


                    var result = await cmd.ExecuteScalarAsync();

                    if (result != null)
                    {
                        int newid = Convert.ToInt32(result);
                        return new Player(newid, id, "", PlayerType.Observer);
                    }

                    throw new Exception("Result is null");
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // 19: 제약조건 위반(중복 등)
                {
                    throw new IdDuplicateException("이미 존재하는 아이디입니다.");
                }
                catch (Exception e)
                {
                    Logger.Error($"DB 오류 발생 : {e.Message}");
                    throw;
                }
            }
        }
        public async Task<IEnumerable<MatchInfo>> GetPlayerMatchHistoriesAsync(Player player)
        {
            if (player.Id == 1)
                throw new GuestPlayerException("게스트 플레이어는 전적이 없습니다.");

            Dictionary<int, MatchInfo> matches = new();
            // 매치Id : 정보 딕셔너리

            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();

                var matchcmd = db.CreateCommand();
                matchcmd.CommandText = @"
                        SELECT m.Id, m.WinnerType, m.MatchTime, m.Reason,
                            m.BlackPlayerId, b.UserId, 
                            m.WhitePlayerId, w.UserId
                        FROM Matches m
                        JOIN Users b On m.BlackPlayerId = b.Id
                        JOIN Users w On m.WhitePlayerId = w.Id
                        WHERE m.BlackPlayerId = @id OR m.WhitePlayerId = @id
                        ORDER BY m.MatchTime DESC;";
                matchcmd.Parameters.AddWithValue("@id", player.Id);

                using (var reader = await matchcmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int matchId = reader.GetInt32(0);
                        string reason = reader.GetString(3);
                        var black = new MatchPlayerInfo(reader.GetInt32(4), reader.GetString(5));
                        var white = new MatchPlayerInfo(reader.GetInt32(6), reader.GetString(7));

                        matches[matchId] = new MatchInfo(black, white, (PlayerType)reader.GetInt32(1),
                            reason, new List<GameMove>(), DateTime.Parse(reader.GetString(2)));
                        // 착수 히스토리는 일단 빈 상태로 추가
                    }
                }

                var movecmd = db.CreateCommand();
                movecmd.CommandText = @"
                    SELECT mm.MatchId, mm.X, mm.Y, mm.MoveNumber, mm.PlayerType
                    FROM MatchMoves mm
                    JOIN Matches m ON mm.MatchId = m.Id
                    WHERE m.BlackPlayerId = @id OR m.WhitePlayerId = @id
                    ORDER BY mm.MatchId, mm.MoveNumber ASC;";
                movecmd.Parameters.AddWithValue("@id", player.Id);

                using (var reader = await movecmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int matchId = reader.GetInt32(0);
                        int x = reader.GetInt32(1);
                        int y = reader.GetInt32(2);
                        int movenumber = reader.GetInt32(3);
                        PlayerType playerType = (PlayerType)reader.GetInt32(4);

                        ((List<GameMove>)matches[matchId].MoveHistory).Add(new GameMove(x, y, movenumber, playerType));
                    }
                }

                return matches.Values;
            }
        }

        public async Task<Record> GetPlayerRecordsAsync(Player player)
        {
            if (player.Id == 1)
                throw new GuestPlayerException("게스트 플레이어는 전적이 없습니다.");

            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();
                var cmd = db.CreateCommand();

                cmd.CommandText = @"SELECT Win, Loss, Draw FROM Users WHERE id = @id;";
                cmd.Parameters.AddWithValue("@id", player.Id);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync()) // 데이터 있으면
                    {
                        int win = reader.GetInt32(0);
                        int loss = reader.GetInt32(1);
                        int draw = reader.GetInt32(2);

                        return new Record(win, loss, draw);
                    }
                    else
                        throw new AccountNotExistException("플레이어를 찾을 수 없습니다.");
                }
            }
        }

        public async Task SaveMatchAsync(MatchInfo match)
        {
            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();

                // 트랜잭션 시작, 착수 저장할때 실패하면 다 취소하도록
                using (var transaction = await db.BeginTransactionAsync())
                {
                    try
                    {
                        var matchcmd = db.CreateCommand();
                        matchcmd.Transaction = transaction as SqliteTransaction;
                        matchcmd.CommandText = @"
                        INSERT INTO Matches (BlackPlayerId, WhitePlayerId, WinnerType, Reason, MatchTime)
                        VALUES (@bId, @wId, @reason, @winner, @time)                        
                        RETURNING Id;";

                        matchcmd.Parameters.AddWithValue("@bId", match.BlackPlayer.Id);
                        matchcmd.Parameters.AddWithValue("@wId", match.WhitePlayer.Id);
                        matchcmd.Parameters.AddWithValue("@reason", match.Reason);
                        matchcmd.Parameters.AddWithValue("@winner", (int)match.Winner);
                        matchcmd.Parameters.AddWithValue("@time", match.Time.ToString("O"));

                        int matchid = Convert.ToInt32(await matchcmd.ExecuteScalarAsync());
                        // 매치 생성

                        foreach (var move in match.MoveHistory)
                        {
                            var movecmd = db.CreateCommand();
                            movecmd.Transaction = transaction as SqliteTransaction;
                            movecmd.CommandText = @"
                            INSERT INTO MatchMoves (MatchId, MoveNumber, X, Y, PlayerType)
                            VALUES (@matchId, @moveNumber, @x, @y, @playerType);";

                            movecmd.Parameters.AddWithValue("@matchId", matchid);
                            movecmd.Parameters.AddWithValue("@moveNumber", move.MoveNumber);
                            movecmd.Parameters.AddWithValue("@x", move.X);
                            movecmd.Parameters.AddWithValue("@y", move.Y);
                            movecmd.Parameters.AddWithValue("@playerType", (int)move.PlayerType);

                            await movecmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        // 오류 시 취소
                        throw;
                    }
                }
            }
        }

        public async Task<Player> TryLoginAsync(string id, string pw)
        {
            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();
                var cmd = db.CreateCommand();

                cmd.CommandText = "SELECT Id, UserId, PasswordHash, Win, Loss, Draw FROM Users WHERE Userid = @id;";
                cmd.Parameters.AddWithValue("@id", id);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync()) // 다음 읽기, 근데 아이디는 유일하므로 없으면 false
                    {
                        string dbpwd = reader.GetString(2);

                        if (dbpwd == pw) // 비밀번호 일치
                        {
                            return new Player(
                                reader.GetInt32(0),     // Id
                                reader.GetString(1),    // 계정ID
                                "",                     // 닉네임
                                PlayerType.Observer,
                                new Record(
                                    reader.GetInt32(3),     // 승
                                    reader.GetInt32(4),     // 패
                                    reader.GetInt32(5)));   // 무승부
                        }
                        else
                        {
                            throw new PasswordWrongException("패스워드가 틀립니다.");
                        }
                    }
                    throw new AccountNotExistException("계정이 존재하지 않습니다.");
                }
            }
        }
    }
}
