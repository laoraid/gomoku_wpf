using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Applications;
using Gomoku.Services.Interfaces;
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

            var player = await _service.CreateAccountAsync(id, pwd);

            Assert.IsNotNull(player);
            Assert.AreEqual(id, player.AccountId);
            Assert.IsGreaterThan(0, player.Id);

            bool exthrow = false;
            try
            {
                var player2 = await _service.CreateAccountAsync(id, pwd);
            }
            catch (IdDuplicateException)
            {
                exthrow = true;
            }

            Assert.IsTrue(exthrow);
        }

        [TestMethod]
        public async Task GetPlayerRecords_Test()
        {
            string id1 = "user1";
            string pwd1 = "1234";
            string id2 = "user2";
            string pwd2 = "12345";

            var player1 = await _service.CreateAccountAsync(id1, pwd1);
            var player2 = await _service.CreateAccountAsync(id2, pwd2);

            // TODO: 매치 업데이트 하기

            Record player1r = await _service.GetPlayerRecordsAsync(player1);

            Assert.AreEqual(0, player1r.Win);
            Assert.AreEqual(0, player1r.Loss);
            Assert.AreEqual(0, player1r.Draw);
        }

        [TestMethod]
        public async Task PlayerMatch_Test()
        {
            string id1 = "winner1";
            string pwd1 = "asdf";
            string id2 = "loser1";
            string pwd2 = "123123";

            var player1 = await _service.CreateAccountAsync(id1, pwd1);
            var player2 = await _service.CreateAccountAsync(id2, pwd2);

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
        }

        [TestMethod]
        public async Task Login_Test()
        {
            string id1 = "myid";
            string pwd1 = "1234";

            string id2 = "myid2";
            string pwd2 = "mypassword";

            await _service.CreateAccountAsync(id1, pwd1);
            await _service.CreateAccountAsync(id2, pwd2);

            var player1 = await _service.TryLoginAsync(id1, pwd1);
            var player2 = await _service.TryLoginAsync(id2, pwd2);

            Assert.IsNotNull(player1);
            Assert.IsNotNull(player2);

            Assert.AreEqual(id1, player1.AccountId);
            Assert.AreEqual(id2, player2.AccountId);


        }

        [TestMethod]
        public async Task Account_Delete_Test()
        {
            string id1 = "id";
            string pwd1 = "pwd";
            string id2 = "id2";
            string pwd2 = "pwd2";

            var player1 = await _service.CreateAccountAsync(id1, pwd1);
            var player2 = await _service.CreateAccountAsync(id2, pwd2);

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
            // 삭제된 계정 id 가 2인지
            Assert.AreEqual("(탈퇴한 계정)", dbmatches[0].BlackPlayer.UserId);
        }
    }
}
