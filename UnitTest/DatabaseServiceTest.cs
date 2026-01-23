using Gomoku.Models;
using Gomoku.Services.Applications;
using Gomoku.Services.Interfaces;
using Microsoft.Data.Sqlite;

namespace UnitTest
{
    [TestClass]
    public class DatabaseServiceTest
    {
        private readonly string testDBFile = "TestDB.db";
        private readonly IDatabaseService _service;

        public DatabaseServiceTest()
        {
            _service = new DatabaseService($"Data Source={testDBFile}");
        }

        [TestCleanup]
        public void Clean()
        {
            if (File.Exists(testDBFile))
            {
                SqliteConnection.ClearAllPools();
                // 연결 해제
                File.Delete(testDBFile);
            }
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
    }
}
