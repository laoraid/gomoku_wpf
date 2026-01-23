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
        public Task<IEnumerable<MatchInfo>> GetPlayerMachHistoriesAsync(Player player)
        {
            throw new NotImplementedException();
        }

        public async Task<Record> GetPlayerRecordsAsync(Player player)
        {
            if (player.Id == -1)
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

        public Task SaveMatchAsync(MatchInfo match)
        {
            throw new NotImplementedException();
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
