using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Services.Applications.Database;
using Microsoft.Data.Sqlite;

namespace UnitTest
{
    [TestClass]
    public class DatabaseServiceTest
    {
        private string testDBFile = null!;
        private IDatabaseService _service = null!;
        private SqliteConnection _connection = null!;

        [TestInitialize]
        public void Setup()
        {
            testDBFile = $"test_{Guid.NewGuid():N}.db";
            string connectstring = $"Data Source={testDBFile};Foreign Keys=True;Mode=Memory;Cache=Shared";
            // 파일 I/O 대신 메모리에서 테스트

            _connection = new SqliteConnection(connectstring);
            _connection.Open();
            // 연결을 유지해야 안사라짐

            _service = new DatabaseService(connectstring);
        }

        [TestCleanup]
        public void Clean()
        {
            _connection.Close();
            _connection.Dispose();
        }

        [TestMethod]
        public async Task CreateAccount_Test()
        {
            string id = "testuser";
            string pwd = "password123";

            var player = await _service.CreateAccountAsync(id, pwd, "1번닉네임");

            Assert.IsNotNull(player);
            Assert.AreEqual(id, player.AccountId);
            Assert.IsGreaterThan(0, player.Id);

            bool exthrow = false;
            try
            {
                var player2 = await _service.CreateAccountAsync(id, pwd, "2번닉네임");
            }
            catch (IdDuplicateException)
            {
                exthrow = true;
            }
            Assert.IsTrue(exthrow);
            // 아이디 중복시에

            await Assert.ThrowsAsync<NicknameDuplicateException>(() => _service.CreateAccountAsync("uniqueid", "1234", "1번닉네임"));
            // 닉네임 중복시에

        }

        [TestMethod]
        public async Task GetPlayerRecords_Test()
        {
            string id1 = "user1";
            string pwd1 = "1234";
            string id2 = "user2";
            string pwd2 = "12345";

            var player1 = await _service.CreateAccountAsync(id1, pwd1, "닉1");
            var player2 = await _service.CreateAccountAsync(id2, pwd2, "닉2");

            var blackinfo = new MatchPlayerInfo(player1.Id, player1.AccountId);
            var whiteinfo = new MatchPlayerInfo(1, "Guest");

            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.Black, "흑승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.White, "백승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.Observer, "무승부", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.Black, "흑승리", [], DateTime.Now));

            Record player1r = await _service.GetPlayerRecordsAsync(player1);

            Assert.AreEqual(2, player1r.Win);
            Assert.AreEqual(1, player1r.Loss);
            Assert.AreEqual(1, player1r.Draw);
            // 매치 저장된대로 전적 나와야 함 

            Record player2r = await _service.GetPlayerRecordsAsync(player2);

            Assert.AreEqual(0, player2r.Win);
            Assert.AreEqual(0, player2r.Loss);
            Assert.AreEqual(0, player2r.Draw);
            // 매치가 없으니 다 0이어야 함
        }

        [TestMethod]
        public async Task PlayerMatch_Test()
        {
            string id1 = "winner1";
            string pwd1 = "asdf";
            string id2 = "loser1";
            string pwd2 = "123123";

            var player1 = await _service.CreateAccountAsync(id1, pwd1, "닉1");
            var player2 = await _service.CreateAccountAsync(id2, pwd2, "닉2");

            List<GameMove> moves =
            [
                new GameMove(0, 0, 1, PlayerType.Black),
                new GameMove(0, 1, 2, PlayerType.White),
                new GameMove(0, 2, 3, PlayerType.Black),
                new GameMove(1, 0, 4, PlayerType.White),
            ];

            var blackinfo = new MatchPlayerInfo(player1.Id, player1.AccountId);
            var whiteinfo = new MatchPlayerInfo(player2.Id, player2.AccountId);

            MatchInfo matchInfo = new MatchInfo(blackinfo, whiteinfo, PlayerType.Black, "그냥", moves, DateTime.Now);

            await _service.SaveMatchAsync(matchInfo);
            // 매치 저장

            var dbmatches = await _service.GetPlayerMatchHistoriesAsync(player1);

            Assert.AreEqual(1, dbmatches.Count());

            var dbmoves = dbmatches.First().MoveHistory.ToList();

            Assert.AreEqual(0, dbmoves[0].X);
            Assert.AreEqual(0, dbmoves[0].Y);
            Assert.AreEqual(1, dbmoves[0].MoveNumber);
            Assert.AreEqual(1, dbmoves[1].Y);
            Assert.AreEqual(2, dbmoves[2].Y);
            Assert.AreEqual(1, dbmoves[3].X);

            Assert.AreEqual(PlayerType.Black, dbmatches.First().Winner);
            Assert.AreEqual("그냥", dbmatches.First().Reason);
        }

        [TestMethod]
        public async Task Login_Test()
        {
            string id1 = "myid";
            string pwd1 = "1234";

            string id2 = "myid2";
            string pwd2 = "mypassword";

            await _service.CreateAccountAsync(id1, pwd1, "닉1");
            await _service.CreateAccountAsync(id2, pwd2, "닉2");

            var player1 = await _service.TryLoginAsync(id1, pwd1);
            var player2 = await _service.TryLoginAsync(id2, pwd2);

            Assert.IsNotNull(player1);
            Assert.IsNotNull(player2);

            Assert.AreEqual(id1, player1.AccountId);
            Assert.AreEqual(id2, player2.AccountId);

            var blackinfo = new MatchPlayerInfo(player1.Id, player1.AccountId);
            var whiteinfo = new MatchPlayerInfo(1, "Guest");

            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.Black, "흑승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.White, "백승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.Observer, "무승부", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.Black, "흑승리", [], DateTime.Now));

            var replayer1 = await _service.TryLoginAsync(id1, pwd1);

            Assert.AreEqual(2, replayer1.Records.Win);
            Assert.AreEqual(1, replayer1.Records.Loss);
            Assert.AreEqual(1, replayer1.Records.Draw);
            // 매치 종료 후 다시 로그인 시 제대로 전적 불러오는지
        }

        [TestMethod]
        public async Task Account_Delete_Test()
        {
            string id1 = "id";
            string pwd1 = "pwd";
            string id2 = "id2";
            string pwd2 = "pwd2";

            var player1 = await _service.CreateAccountAsync(id1, pwd1, "닉1");
            var player2 = await _service.CreateAccountAsync(id2, pwd2, "닉2");

            List<GameMove> moves =
            [
                new GameMove(0, 0, 1, PlayerType.Black),
                new GameMove(0, 1, 2, PlayerType.White),
                new GameMove(0, 2, 3, PlayerType.Black),
                new GameMove(1, 0, 4, PlayerType.White),
            ];

            var blackinfo = new MatchPlayerInfo(player1.Id, player1.AccountId);
            var whiteinfo = new MatchPlayerInfo(player2.Id, player2.AccountId);

            MatchInfo matchInfo = new MatchInfo(blackinfo, whiteinfo, PlayerType.Black, "그냥", moves, DateTime.Now);

            await _service.SaveMatchAsync(matchInfo);
            // 매치 저장

            await _service.DeleteAccountAsync(id1, pwd1);
            // 플레이어1 삭제

            var dbmatches = (await _service.GetPlayerMatchHistoriesAsync(player2)).ToList();

            Assert.HasCount(1, dbmatches);
            Assert.AreEqual(2, dbmatches[0].BlackPlayer.Id);
            // 삭제된 계정 id 가 2(탈퇴한 계정)인지
            Assert.AreEqual("(탈퇴한 계정)", dbmatches[0].BlackPlayer.UserId);

            await Assert.ThrowsAsync<AccountNotExistException>(() => _service.TryLoginAsync(id1, pwd1));
            // 삭제한 계정으로 로그인 시도시 없는 계정이라고 나오는지?
        }

        [TestMethod]
        public async Task GetRelativeRecord_Test()
        {
            string id1 = "id";
            string pwd1 = "pwd";
            string id2 = "id2";
            string pwd2 = "pwd2";

            var player1 = await _service.CreateAccountAsync(id1, pwd1, "닉1");
            var player2 = await _service.CreateAccountAsync(id2, pwd2, "닉2");

            var blackinfo = new MatchPlayerInfo(player1.Id, player1.AccountId);
            var whiteinfo = new MatchPlayerInfo(player2.Id, player2.AccountId);

            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.Black, "흑승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.White, "백승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.Observer, "무승부", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(blackinfo, whiteinfo, PlayerType.Black, "흑승리", [], DateTime.Now));

            (var blackrecord, var whiterecord) = await _service.GetRelativeRecordsAsync(player1, player2);

            Assert.AreEqual(2, blackrecord.Win);
            Assert.AreEqual(1, whiterecord.Win);
            Assert.AreEqual(1, blackrecord.Loss);
            Assert.AreEqual(2, whiterecord.Loss);
            Assert.AreEqual(1, blackrecord.Draw);
            Assert.AreEqual(1, whiterecord.Draw);
        }

        [TestMethod]
        public async Task ChangeNickname_Test()
        {
            string id1 = "id";
            string pwd1 = "pwd";
            string id2 = "id2";
            string pwd2 = "pwd2";

            var player1 = await _service.CreateAccountAsync(id1, pwd1, "닉1");
            var player2 = await _service.CreateAccountAsync(id2, pwd2, "닉2");

            var blackinfo = new MatchPlayerInfo(player1.Id, player1.AccountId);
            var whiteinfo = new MatchPlayerInfo(player2.Id, player2.AccountId);

            bool result = await _service.ChangeNicknameAsync(id1, "새로운닉네임");
            Assert.IsTrue(result);

            await Assert.ThrowsAsync<NicknameDuplicateException>(() => _service.ChangeNicknameAsync(id1, "닉2"));
            await Assert.ThrowsAsync<AccountNotExistException>(() => _service.ChangeNicknameAsync("없는계정id", "하이"));
        }

        [TestMethod]
        public async Task GetPlayerRanks_Test()
        {
            string id1 = "id";
            string pwd1 = "pwd";
            string id2 = "id2";
            string pwd2 = "pwd2";
            string id3 = "id3";
            string pwd3 = "pwd3";

            var player1 = await _service.CreateAccountAsync(id1, pwd1, "닉1");
            var player2 = await _service.CreateAccountAsync(id2, pwd2, "닉2");
            var player3 = await _service.CreateAccountAsync(id3, pwd3, "닉3");

            var p1info = new MatchPlayerInfo(player1.Id, player1.AccountId);
            var p2info = new MatchPlayerInfo(player2.Id, player2.AccountId);

            await _service.SaveMatchAsync(new MatchInfo(p1info, p2info, PlayerType.Black, "흑승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(p1info, p2info, PlayerType.White, "백승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(p1info, p2info, PlayerType.Observer, "무승부", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(p1info, p2info, PlayerType.Black, "흑승리", [], DateTime.Now));

            var p3info = new MatchPlayerInfo(player3.Id, player3.AccountId);
            var guestinfo = new MatchPlayerInfo(1, "Guest");

            await _service.SaveMatchAsync(new MatchInfo(p3info, guestinfo, PlayerType.Black, "흑승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(p3info, guestinfo, PlayerType.Black, "흑승리", [], DateTime.Now));
            await _service.SaveMatchAsync(new MatchInfo(p3info, guestinfo, PlayerType.White, "게스트승리", [], DateTime.Now));

            var ranks = (await _service.GetPlayerRanksAsync()).ToList();

            Assert.AreEqual(id1, ranks[0].AccountId);
            // 플레이어1이 1등
            Assert.AreEqual(id3, ranks[1].AccountId);
            // 플레이어3이 2등
            Assert.AreEqual(id2, ranks[2].AccountId);
            // 플레이어2가 3등

            Assert.HasCount(3, ranks);
            // 게스트 미포함
        }
    }
}
