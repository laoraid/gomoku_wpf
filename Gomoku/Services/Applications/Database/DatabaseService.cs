/*
 * DatabaseService.cs
 * 데이터베이스와의 조회, 저장, 삭제, 수정을 수행합니다.
 */
using Gomoku.Helpers;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Microsoft.Data.Sqlite;

namespace Gomoku.Services.Applications.Database
{
    public static class Schema
    {
        public static class Users
        {
            public const string Table = nameof(Users);
            public const string Id = nameof(Id);
            public const string UserId = nameof(UserId);
            public const string Nickname = nameof(Nickname);
            public const string PasswordHash = nameof(PasswordHash);
        }

        public static class Matches
        {
            public const string Table = nameof(Matches);
            public const string Id = nameof(Id);
            public const string BlackPlayerId = nameof(BlackPlayerId);
            public const string WhitePlayerId = nameof(WhitePlayerId);
            public const string WinnerType = nameof(WinnerType);
            public const string Reason = nameof(Reason);
            public const string MatchTime = nameof(MatchTime);
        }

        public static class MatchMoves
        {
            public const string Table = nameof(MatchMoves);
            public const string Id = nameof(Id);
            public const string MatchId = nameof(MatchId);
            public const string MoveNumber = nameof(MoveNumber);
            public const string X = nameof(X);
            public const string Y = nameof(Y);
            public const string PlayerType = nameof(PlayerType);
        }

        public static class UserRecord
        {
            public const string Table = nameof(UserRecord);
            public const string Id = nameof(Id);
            public const string Win = nameof(Win);
            public const string Loss = nameof(Loss);
            public const string Draw = nameof(Draw);
        }
    }
    // TODO: Dapper 알아보기

    /// <summary>
    /// SQLite 데이터베이스를 사용하여 조회, 저장, 수정, 삭제를 수행합니다.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly string _dbString;

        public DatabaseService() : this("Data Source=Server.db;Foreign Keys=True;")
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
                usersTableCommand.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS {Schema.Users.Table} (
                    {Schema.Users.Id} INTEGER PRIMARY KEY AUTOINCREMENT,
                    {Schema.Users.UserId} TEXT NOT NULL UNIQUE,
                    {Schema.Users.Nickname} TEXT NOT NULL UNIQUE,
                    {Schema.Users.PasswordHash} TEXT NOT NULL
                    );";
                // 아이디, 계정아이디, 비밀번호, 승리, 패배, 무승부
                usersTableCommand.ExecuteNonQuery();

                var matchTableCommand = db.CreateCommand();
                matchTableCommand.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS {Schema.Matches.Table} (
                    {Schema.Matches.Id} INTEGER PRIMARY KEY AUTOINCREMENT,
                    {Schema.Matches.BlackPlayerId} INTEGER NOT NULL,
                    {Schema.Matches.WhitePlayerId} INTEGER NOT NULL,
                    {Schema.Matches.WinnerType} INTEGER NOT NULL,
                    {Schema.Matches.Reason} TEXT NOT NULL,
                    {Schema.Matches.MatchTime} TEXT NOT NULL,
                    FOREIGN KEY ({Schema.Matches.BlackPlayerId}) REFERENCES {Schema.Users.Table}({Schema.Users.Id}),
                    FOREIGN KEY ({Schema.Matches.WhitePlayerId}) REFERENCES {Schema.Users.Table}({Schema.Users.Id})
                    );";
                // id, 흑id, 백id, 승자(0:무승부, 1:흑, 2:백), 승리이유, 매치시간
                matchTableCommand.ExecuteNonQuery();

                var matchMovesTableCommand = db.CreateCommand();
                matchMovesTableCommand.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS {Schema.MatchMoves.Table} (
                    {Schema.MatchMoves.Id} INTEGER PRIMARY KEY AUTOINCREMENT,
                    {Schema.MatchMoves.MatchId} INTEGER NOT NULL,
                    {Schema.MatchMoves.MoveNumber} INTEGER NOT NULL,
                    {Schema.MatchMoves.X} INTEGER NOT NULL,
                    {Schema.MatchMoves.Y} INTEGER NOT NULL,
                    {Schema.MatchMoves.PlayerType} INTEGER NOT NULL,
                    FOREIGN KEY ({Schema.MatchMoves.MatchId}) REFERENCES {Schema.Matches.Table}({Schema.Matches.Id}) ON DELETE CASCADE
                    );"; // 매치 삭제되면 이것도 삭제
                // id, 매치id, 착수순서(1~), X좌표, Y좌표, 돌 색상
                matchMovesTableCommand.ExecuteNonQuery();

                var recordviewcmd = db.CreateCommand();
                recordviewcmd.CommandText = $@"
                    CREATE VIEW IF NOT EXISTS {Schema.UserRecord.Table} AS
                    SELECT
                        u.{Schema.Users.Id},
                        COUNT(CASE WHEN 
                                    (
                                        m.{Schema.Matches.BlackPlayerId} = u.{Schema.Users.Id} 
                                        AND 
                                        {Schema.Matches.WinnerType} = 1
                                    ) 
                                    OR 
                                    (
                                        m.{Schema.Matches.WhitePlayerId} = u.{Schema.Users.Id} 
                                        AND 
                                        {Schema.Matches.WinnerType} = 2
                                    ) THEN 1 END
                            ) AS {Schema.UserRecord.Win},
                        COUNT(CASE WHEN
                                    (
                                        m.{Schema.Matches.BlackPlayerId} = u.{Schema.Users.Id} 
                                        AND 
                                        {Schema.Matches.WinnerType} = 2
                                    ) 
                                    OR
                                    (
                                        m.{Schema.Matches.WhitePlayerId} = u.{Schema.Users.Id} 
                                        AND 
                                        {Schema.Matches.WinnerType} = 1
                                    ) THEN 1 END
                                ) AS {Schema.UserRecord.Loss},
                        COUNT(CASE WHEN {Schema.Matches.WinnerType} = 0 THEN 1 END) AS {Schema.UserRecord.Draw}
                    FROM {Schema.Users.Table} u
                    LEFT JOIN {Schema.Matches.Table} m
                    ON 
                        m.{Schema.Matches.BlackPlayerId} = u.{Schema.Users.Id}
                        OR
                        m.{Schema.Matches.WhitePlayerId} = u.{Schema.Users.Id}
                    GROUP BY u.{Schema.Users.Id};";
                recordviewcmd.ExecuteNonQuery();
                // 흑이면서 승리가자 흑 또는 백이면서 승리자가 백인 갯수 = win
                // 흑이면서 승리자가 백 또는 백이면서 승리자가 흑인 갯수 = loss
                // 승리자가 0(무승부)인 갯수 = draw 

                var guestCommand = db.CreateCommand();
                guestCommand.CommandText = $@"
                    INSERT INTO {Schema.Users.Table}
                        ({Schema.Users.Id}, {Schema.Users.UserId}, {Schema.Users.Nickname}, {Schema.Users.PasswordHash})
                    VALUES (1, 'Guest', 'Guest', 'None')
                    ON CONFLICT({Schema.Users.Id}) DO UPDATE SET
                        {Schema.Users.UserId} = 'Guest',
                        {Schema.Users.Nickname} = 'Guest',
                        {Schema.Users.PasswordHash} = 'None';";
                guestCommand.ExecuteNonQuery();
                // 아이디 1이면 게스트 계정

                var deletedAccountCommand = db.CreateCommand();
                deletedAccountCommand.CommandText = $@"
                INSERT INTO {Schema.Users.Table}
                        ({Schema.Users.Id}, {Schema.Users.UserId}, {Schema.Users.Nickname}, {Schema.Users.PasswordHash})
                VALUES (2, '(탈퇴한 계정)', '(탈퇴한 계정)', 'None')
                ON CONFLICT({Schema.Users.Id}) DO UPDATE SET
                    {Schema.Users.UserId} = '(탈퇴한 계정)',
                    {Schema.Users.Nickname} = '(탈퇴한 계정)',
                    {Schema.Users.PasswordHash} = 'None';";
                deletedAccountCommand.ExecuteNonQuery();
                // 아이디 2는 탈퇴한 계정용
            }
        }

        public async Task<Player> CreateAccountAsync(string id, string hashedpw, string nickname)
        {
            // TODO: Guest 닉네임 불가, 아이디,닉네임에 공백 불가, 특수문자 불가 등 처리
            using (var db = new SqliteConnection(_dbString))
            {
                try
                {
                    await db.OpenAsync();
                    var cmd = db.CreateCommand();

                    cmd.CommandText = $@"
                        INSERT INTO {Schema.Users.Table} 
                        ({Schema.Users.UserId}, {Schema.Users.Nickname}, {Schema.Users.PasswordHash})
                        VALUES (@id, @nickname, @hashedpw)
                        RETURNING {Schema.Users.Id};";
                    // 방금 생성된 계정 id 가져오기

                    cmd.Parameters.AddWithValue("@id", id);
                    hashedpw = HashHelper.SHA256Hash(hashedpw);
                    cmd.Parameters.AddWithValue("@hashedpw", hashedpw);
                    cmd.Parameters.AddWithValue("@nickname", nickname);

                    var result = await cmd.ExecuteScalarAsync();

                    if (result != null)
                    {
                        int newid = Convert.ToInt32(result);
                        return new Player(newid, id, nickname, PlayerType.Observer);
                    }

                    throw new Exception("Result is null");
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // 19: 제약조건 위반(중복 등)
                {
                    if (ex.Message.Contains("Users.UserId"))
                        throw new IdDuplicateException("이미 존재하는 아이디입니다.");
                    else if (ex.Message.Contains("Users.Nickname"))
                        throw new NicknameDuplicateException("닉네임이 중복됩니다.");
                    else
                        throw;
                }
                catch (Exception e)
                {
                    Logger.Error($"DB 오류 발생 : {e.Message}");
                    throw;
                }
            }
        }

        public async Task<IEnumerable<MatchInfo>> GetMatchesAsync(
            string? PlayerNickname = null,
            string? BlackPlayerNickname = null,
            string? WhitePlayerNickname = null,
            DateTime? from = null,
            DateTime? to = null,
            int PageNumber = 1,
            int PageSize = 20)
        {
            if (BlackPlayerNickname == "Guest" || WhitePlayerNickname == "Guest")
                throw new GuestPlayerException("게스트 플레이어의 매치는 조회할 수 없습니다.");

            if (PlayerNickname != null && (BlackPlayerNickname != null || WhitePlayerNickname != null))
                throw new ArgumentException("PlayerNickname으로 검색하면 흑이나 백 플레이어로 검색할 수 없습니다.");

            using var db = new SqliteConnection(_dbString);

            await db.OpenAsync();
            var cmd = db.CreateCommand();

            var whereClauses = new List<string>();
            // 조건 저장 리스트

            if (!string.IsNullOrEmpty(PlayerNickname))
            {
                whereClauses.Add($"b.{Schema.Users.Nickname} = @Nick OR w.{Schema.Users.Nickname} = @Nick");
                cmd.Parameters.AddWithValue("@Nick", PlayerNickname);

            }

            if (!string.IsNullOrEmpty(BlackPlayerNickname))
            {
                whereClauses.Add($"b.{Schema.Users.Nickname} = @blackNick");
                cmd.Parameters.AddWithValue("@blackNick", BlackPlayerNickname);
            }

            if (!string.IsNullOrEmpty(WhitePlayerNickname))
            {
                whereClauses.Add($"w.{Schema.Users.Nickname} = @whiteNick");
                cmd.Parameters.AddWithValue("@whiteNick", WhitePlayerNickname);
            }

            if (from != null)
            {
                whereClauses.Add($"{Schema.Matches.MatchTime} >= @from");
                cmd.Parameters.AddWithValue("@from", from.Value.ToString("O"));
            }

            if (to != null)
            {
                whereClauses.Add($"{Schema.Matches.MatchTime} <= @to");
                cmd.Parameters.AddWithValue("@to", to.Value.ToString("O"));
            }

            string whereClause = whereClauses.Count > 0
                ? "WHERE " + string.Join(" AND ", whereClauses)
                : string.Empty;
            // where 절 생성

            cmd.CommandText = $@"
                SELECT 
                    m.{Schema.Matches.Id},
                    m.{Schema.Matches.BlackPlayerId},
                    m.{Schema.Matches.WhitePlayerId},
                    m.{Schema.Matches.WinnerType},
                    m.{Schema.Matches.Reason},
                    m.{Schema.Matches.MatchTime},
                    b.{Schema.Users.Nickname} AS BlackNickname,
                    w.{Schema.Users.Nickname} AS WhiteNickname
                FROM {Schema.Matches.Table} AS m
                JOIN {Schema.Users.Table} AS b ON m.{Schema.Matches.BlackPlayerId} = b.{Schema.Users.Id}
                JOIN {Schema.Users.Table} AS w ON m.{Schema.Matches.WhitePlayerId} = w.{Schema.Users.Id}
                {whereClause}
                ORDER BY {Schema.Matches.MatchTime} DESC
                LIMIT @limit OFFSET @offset;";

            cmd.Parameters.AddWithValue("@limit", PageSize);
            cmd.Parameters.AddWithValue("@offset", (PageNumber - 1) * PageSize);

            List<MatchInfo> matches = new();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int matchId = reader.GetInt32(0);
                int blackId = reader.GetInt32(1);
                int whiteId = reader.GetInt32(2);
                PlayerType winner = (PlayerType)reader.GetInt32(3);
                string reason = reader.GetString(4);
                DateTime matchTime = DateTime.Parse(reader.GetString(5));
                var blackPlayerInfo = new MatchPlayerInfo(blackId, reader.GetString(6));
                var whitePlayerInfo = new MatchPlayerInfo(whiteId, reader.GetString(7));

                var match = new MatchInfo(blackPlayerInfo, whitePlayerInfo,
                    winner, reason, null, matchTime, matchId);
                matches.Add(match);
            }
            return matches;
        }


        public async Task<IEnumerable<GameMove>> GetMatchMovesAsync(MatchInfo match)
        {
            if (match.Id <= 0)
                throw new ArgumentException("매치 ID가 유효하지 않습니다.");

            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();
                var movecmd = db.CreateCommand();
                movecmd.CommandText = $@"
                    SELECT 
                        {Schema.MatchMoves.X}, 
                        {Schema.MatchMoves.Y}, 
                        {Schema.MatchMoves.MoveNumber}, 
                        {Schema.MatchMoves.PlayerType}
                    FROM {Schema.MatchMoves.Table}
                    WHERE {Schema.MatchMoves.MatchId} = @matchId
                    ORDER BY {Schema.MatchMoves.MatchId}, {Schema.MatchMoves.MoveNumber} ASC;";
                // 착수 히스토리 불러오기, 해당하는 matchid만, 착수순서 오름차순
                movecmd.Parameters.AddWithValue("@matchId", match.Id);

                List<GameMove> moves = new();

                using (var reader = await movecmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int x = reader.GetInt32(0);
                        int y = reader.GetInt32(1);
                        int movenumber = reader.GetInt32(2);
                        PlayerType playerType = (PlayerType)reader.GetInt32(3);

                        var move = new GameMove(x, y, movenumber, playerType);
                        moves.Add(move);
                    }
                }
                return moves;
            }
        }

        public async Task<Record> GetPlayerRecordsAsync(Player player)
        {
            if (player.Id == 1)
                throw new GuestPlayerException("게스트 플레이어는 전적이 없습니다.");

            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();

                var userfindcmd = db.CreateCommand();
                userfindcmd.CommandText = $@"SELECT {Schema.Users.Id} FROM {Schema.Users.Table} WHERE {Schema.Users.Id} = @id;";
                // 플레이어 존재 확인
                userfindcmd.Parameters.AddWithValue("@id", player.Id);
                var exists = await userfindcmd.ExecuteScalarAsync();

                if (exists == null) throw new AccountNotExistException("플레이어를 찾을 수 없습니다.");

                var recordcmd = db.CreateCommand();

                recordcmd.CommandText = $@"
                    SELECT {Schema.UserRecord.Win}, {Schema.UserRecord.Loss}, {Schema.UserRecord.Draw} 
                    FROM {Schema.UserRecord.Table}
                    WHERE Id = @id;";

                recordcmd.Parameters.AddWithValue("@id", player.Id);

                using (var reader = await recordcmd.ExecuteReaderAsync())
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
                        matchcmd.CommandText = $@"
                            INSERT INTO {Schema.Matches.Table} 
                                (
                                    {Schema.Matches.BlackPlayerId}, 
                                    {Schema.Matches.WhitePlayerId}, 
                                    {Schema.Matches.WinnerType}, 
                                    {Schema.Matches.Reason}, 
                                    {Schema.Matches.MatchTime}
                                )
                            VALUES 
                                (@bId, @wId, @winner, @reason, @time)                         
                            RETURNING {Schema.Matches.Id};";

                        matchcmd.Parameters.AddWithValue("@bId", match.BlackPlayer.Id);
                        matchcmd.Parameters.AddWithValue("@wId", match.WhitePlayer.Id);
                        matchcmd.Parameters.AddWithValue("@reason", match.Reason);
                        matchcmd.Parameters.AddWithValue("@winner", (int)match.Winner);
                        matchcmd.Parameters.AddWithValue("@time", match.Time.ToString("O"));

                        int matchid = Convert.ToInt32(await matchcmd.ExecuteScalarAsync());
                        // 매치 생성

                        if (match.MoveHistory != null)
                        {
                            foreach (var move in match.MoveHistory)
                            {
                                var movecmd = db.CreateCommand();
                                movecmd.Transaction = transaction as SqliteTransaction;
                                movecmd.CommandText = $@"
                                INSERT INTO {Schema.MatchMoves.Table} 
                                    (
                                        {Schema.MatchMoves.MatchId}, 
                                        {Schema.MatchMoves.MoveNumber}, 
                                        {Schema.MatchMoves.X}, 
                                        {Schema.MatchMoves.Y}, 
                                        {Schema.MatchMoves.PlayerType}
                                    )
                                VALUES 
                                    (@matchId, @moveNumber, @x, @y, @playerType);";

                                movecmd.Parameters.AddWithValue("@matchId", matchid);
                                movecmd.Parameters.AddWithValue("@moveNumber", move.MoveNumber);
                                movecmd.Parameters.AddWithValue("@x", move.X);
                                movecmd.Parameters.AddWithValue("@y", move.Y);
                                movecmd.Parameters.AddWithValue("@playerType", (int)move.PlayerType);

                                await movecmd.ExecuteNonQueryAsync();
                            }
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

        public async Task<Player> TryLoginAsync(string userid, string pw)
        {
            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();
                var cmd = db.CreateCommand();

                cmd.CommandText = $@"
                    SELECT u.{Schema.Users.Id}, u.{Schema.Users.UserId}, 
                        u.{Schema.Users.Nickname}, u.{Schema.Users.PasswordHash},
                        r.{Schema.UserRecord.Win}, r.{Schema.UserRecord.Loss}, r.{Schema.UserRecord.Draw}
                    FROM {Schema.Users.Table} u
                    JOIN {Schema.UserRecord.Table} r ON r.{Schema.UserRecord.Id} = u.{Schema.Users.Id}
                    WHERE {Schema.Users.UserId} = @userid;";
                cmd.Parameters.AddWithValue("@userid", userid);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync()) // 다음 읽기, 근데 아이디는 유일하므로 없으면 false
                    {
                        string dbpwd = reader.GetString(3);
                        pw = HashHelper.SHA256Hash(pw);

                        if (dbpwd == pw) // 비밀번호 일치
                        {
                            return new Player(
                                reader.GetInt32(0),     // Id
                                reader.GetString(1),    // 계정ID
                                reader.GetString(2),    // 닉네임
                                PlayerType.Observer,
                                new Record(
                                    reader.GetInt32(4),     // 승
                                    reader.GetInt32(5),     // 패
                                    reader.GetInt32(6)));   // 무승부
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

        public async Task DeleteAccountAsync(string userid, string pw)
        {
            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();

                var passwordcheckcmd = db.CreateCommand();
                passwordcheckcmd.CommandText = $@"SELECT {Schema.Users.PasswordHash} FROM {Schema.Users.Table} 
                                                  WHERE @userid = {Schema.Users.UserId}";
                passwordcheckcmd.Parameters.AddWithValue("@userid", userid);
                var dbpwd = await passwordcheckcmd.ExecuteScalarAsync();

                if (dbpwd != null)
                {
                    var dbpwdstr = dbpwd.ToString();
                    if (HashHelper.SHA256Hash(pw) != dbpwdstr)
                        throw new PasswordWrongException("패스워드가 틀립니다.");
                }
                else
                    throw new AccountNotExistException("계정이 존재하지 않습니다.");

                using (var transaction = await db.BeginTransactionAsync())
                {
                    try
                    {
                        var cmd = db.CreateCommand();
                        cmd.Transaction = transaction as SqliteTransaction;
                        cmd.CommandText = $@"
                            UPDATE {Schema.Matches.Table}
                            SET {Schema.Matches.BlackPlayerId} = 2
                            WHERE 
                                {Schema.Matches.BlackPlayerId} = (SELECT {Schema.Users.Id} FROM {Schema.Users.Table}
                                                                  WHERE {Schema.Users.UserId} = @userid);

                            UPDATE {Schema.Matches.Table} 
                            SET {Schema.Matches.WhitePlayerId} = 2
                            WHERE
                                {Schema.Matches.WhitePlayerId} = (SELECT {Schema.Users.Id} FROM {Schema.Users.Table}
                                                                  WHERE {Schema.Users.UserId} = @userid);

                            DELETE FROM {Schema.Users.Table} WHERE {Schema.Users.UserId} = @userid;
                            
                            DELETE FROM {Schema.Matches.Table} 
                            WHERE {Schema.Matches.BlackPlayerId} = 2 AND {Schema.Matches.WhitePlayerId} = 2;";
                        // 매치 먼저 삭제된 계정으로 바꾸고 계정 삭제

                        cmd.Parameters.AddWithValue("@userid", userid);

                        await cmd.ExecuteNonQueryAsync();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<(Record BlackRecord, Record WhiteRecord)> GetRelativeRecordsAsync(Player black, Player white)
        {
            if (black.Id == 1 || white.Id == 1)
                throw new GuestPlayerException("게스트 플레이어와의 전적은 없습니다.");

            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();

                var matchescmd = db.CreateCommand();
                matchescmd.CommandText = $@"
                    SELECT COUNT(CASE WHEN 
                                    (
                                        {Schema.Matches.BlackPlayerId} = @bId AND {Schema.Matches.WinnerType} = 1
                                    ) 
                                    OR 
                                    (
                                        {Schema.Matches.WhitePlayerId} = @bId AND {Schema.Matches.WinnerType} = 2
                                    ) THEN 1 END
                                ) as Win,
                           COUNT(CASE WHEN 
                                    (
                                        {Schema.Matches.BlackPlayerId} = @bId AND {Schema.Matches.WinnerType} = 2
                                    ) 
                                    OR 
                                    (
                                        {Schema.Matches.WhitePlayerId} = @bId AND {Schema.Matches.WinnerType} = 1
                                    ) THEN 1 END
                                ) as Loss,
                           COUNT(CASE WHEN {Schema.Matches.WinnerType} = 0 THEN 1 END) as Draw
                    FROM {Schema.Matches.Table}
                    WHERE ({Schema.Matches.BlackPlayerId} = @bId AND {Schema.Matches.WhitePlayerId} = @wId)
                       OR ({Schema.Matches.BlackPlayerId} = @wId AND {Schema.Matches.WhitePlayerId} = @bId);";

                // Win : 흑 플레이어가 이전에 흑이면서 흑이 승리한 판 또는 백이면서 백이 승리한 판
                // Loss : 흑 플레이어가 이전에 흑이면서 백이 승리한 판 또는 백이면서 흑이 승리한 판
                // Draw : WinnerType이 0인 판

                matchescmd.Parameters.AddWithValue("@bId", black.Id);
                matchescmd.Parameters.AddWithValue("@wId", white.Id);

                int win = 0, loss = 0, draw = 0;

                using (var reader = await matchescmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        win += reader.GetInt32(0);
                        loss += reader.GetInt32(1);
                        draw += reader.GetInt32(2);
                    }
                }

                var blackrecord = new Record(win, loss, draw);
                var whiterecord = new Record(loss, win, draw);

                return (blackrecord, whiterecord);
            }
        }

        public async Task<bool> ChangeNicknameAsync(string userid, string newnickname)
        {
            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();

                var cmd = db.CreateCommand();
                cmd.CommandText = $@"
                    UPDATE {Schema.Users.Table} 
                    SET {Schema.Users.Nickname} = @nick
                    WHERE {Schema.Users.UserId} = @userid;";
                cmd.Parameters.AddWithValue("@nick", newnickname);
                cmd.Parameters.AddWithValue("@userid", userid);
                try
                {
                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows <= 0)
                        throw new AccountNotExistException("계정이 존재하지 않습니다.");

                    return true;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                {
                    throw new NicknameDuplicateException("이미 존재하는 닉네임입니다.");
                }
            }
        }

        public async Task<IEnumerable<RankInfo>> GetPlayerRanksAsync()
        {
            using (var db = new SqliteConnection(_dbString))
            {
                await db.OpenAsync();
                var ranks = new List<RankInfo>();

                var cmd = db.CreateCommand();
                cmd.CommandText = $@"
                    SELECT  
                            RANK() OVER (
                                ORDER BY 
                                    r.{Schema.UserRecord.Win} DESC,
                                    r.{Schema.UserRecord.Loss} ASC,
                                    r.{Schema.UserRecord.Draw} DESC
                            ) AS Rank,
                            u.{Schema.Users.Id},
                            u.{Schema.Users.UserId}, 
                            u.{Schema.Users.Nickname},
                            r.{Schema.UserRecord.Win}, r.{Schema.UserRecord.Loss}, r.{Schema.UserRecord.Draw}
                    FROM {Schema.Users.Table} u
                    JOIN {Schema.UserRecord.Table} r ON r.{Schema.UserRecord.Id} = u.{Schema.Users.Id}
                    WHERE u.{Schema.Users.Id} <> 1 AND u.{Schema.Users.Id} <> 2
                    ORDER BY Rank ASC
                    LIMIT 10;";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Player p = new Player();
                        int rank = reader.GetInt32(0);
                        p.Id = reader.GetInt32(1);
                        p.AccountId = reader.GetString(2);
                        p.Nickname = reader.GetString(3);
                        Record r = new Record(reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6));
                        p.Records = r;

                        ranks.Add(new RankInfo(rank, p));
                    }
                }

                return ranks;
            }

        }
    }
}
