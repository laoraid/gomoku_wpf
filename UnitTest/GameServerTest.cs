using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using NSubstitute;
using System.Net.Sockets;

namespace UnitTest
{
    [TestClass]
    public class GameServerTest
    {
        private GameServer _server = null!;
        private INetworkSessionFactory _subSessionFactory = null!;
        private INetworkSession _subSession = null!;

        [TestInitialize]
        public void Setup()
        {
            _subSession = Substitute.For<INetworkSession>();
            _subSession.SessionId.Returns(Guid.NewGuid().ToString());

            _subSessionFactory = Substitute.For<INetworkSessionFactory>();
            _subSessionFactory.Create(Arg.Any<TcpClient>()).Returns(_subSession);
            // 가짜 팩토리

            _server = new GameServer(_subSessionFactory);
            _server._connectionOption = new ConnectionOption("", 1234, "테스트",
                DoubleThreeRuleType.BothForbidden, ConnectionType.Server, CancellationToken.None, 3);
        }

        [TestMethod]
        public async Task ProcessDataAsnyc_JoinData_Nickname_not_duplicate()
        {
            var joindata = new RequestJoinData { Nickname = "이름1" };

            var sentPackets = new List<GameData>();
            var session = Substitute.For<INetworkSession>();

            var player = _server.AddSession(session);

            await session.SendAsync(Arg.Do<GameData>(p => sentPackets.Add(p)));

            _server.ProcessData(session, joindata);
            await Task.Delay(50);

            Assert.IsTrue(sentPackets.Any(p => p is ClientJoinResponseData));
            // 참가 요청에 대한 응답 메시지 받았는가?

            Assert.AreEqual("이름1", player.Nickname);
            // 닉네임 그대로인가? (중복 안되었으니 그대로여야 함)

            var response = (ClientJoinResponseData)sentPackets.First(p => p is ClientJoinResponseData);
            Assert.IsTrue(response.Accepted);
            Assert.AreEqual("이름1", response.Me.Nickname);
            // 응답 데이터 확인
        }

        [TestMethod]
        public async Task Broadcast_Test()
        {
            var s1 = Substitute.For<INetworkSession>();
            var s2 = Substitute.For<INetworkSession>();
            var s3 = Substitute.For<INetworkSession>();

            _server.AddSession(s1);
            _server.AddSession(s2);
            _server.AddSession(s3);

            _server.ProcessData(s1, new RequestJoinData { Nickname = "익명1" });
            _server.ProcessData(s2, new RequestJoinData { Nickname = "익명2" });

            _server.ProcessData(s3, new RequestJoinData { Nickname = "익명3" });
            await Task.Delay(50);

            await s1.Received().SendAsync(Arg.Is<ClientJoinData>(p => p.Player.Nickname == "익명3"));
            await s2.Received().SendAsync(Arg.Is<ClientJoinData>(p => p.Player.Nickname == "익명3"));
            // 새 세션 접속 시 브로드캐스트 받았는지 체크

            _server.ProcessData(s1, new ChatData { Message = "안녕", Sender = new Player { Nickname = "익명1" } });
            await Task.Delay(50);

            await s1.Received().SendAsync(Arg.Is<ChatData>(p => p.Message == "안녕"));
            await s2.Received().SendAsync(Arg.Is<ChatData>(p => p.Message == "안녕"));
            await s3.Received().SendAsync(Arg.Is<ChatData>(p => p.Message == "안녕"));
        }

        [TestMethod]
        [DataRow(" ", "익명")]
        [DataRow("중 간 공 백", "중간공백")]
        [DataRow("player", "player")]
        public void GenerateUniqueNickname_Not_Duplicate(string input, string expected)
        {
            var newclient = Substitute.For<INetworkSession>();

            string result = _server.GenerateUniqueNickname(newclient, input);

            Assert.AreEqual(expected, result);
        }

        public static IEnumerable<object[]> ExistNames()
        {
            yield return new object[] { new string[] { "익명" }, "익명", "익명 (1)" };
            yield return new object[] { new string[] { "익명", "익명 (2)" }, "익명", "익명 (1)" };
            yield return new object[] { new string[] { "익명", "익명 (1)", "익명 (2)" }, "익명", "익명 (3)" };
        }

        [TestMethod]
        [DynamicData(nameof(ExistNames))]
        public async Task GenerateUniqueNickname_When_Duplicate(string[] existNames, string input, string expected)
        {
            foreach (string name in existNames)
            {
                var tempsession = Substitute.For<INetworkSession>();
                var p = _server.AddSession(tempsession);
                p.Nickname = name;
            }

            var newsession = Substitute.For<INetworkSession>();

            string result = _server.GenerateUniqueNickname(newsession, input);

            Assert.AreEqual(expected, result);
        }
    }
}
