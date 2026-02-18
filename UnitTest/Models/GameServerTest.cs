using Gomoku.Models;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Models.Network;
using Gomoku.Services.Applications.Database;
using NSubstitute;
using System.Net.Sockets;

namespace UnitTest.Models
{
    [TestClass]
    public class GameServerTest
    {
        private GameServer _server = null!;
        private INetworkSessionFactory _subSessionFactory = null!;
        private INetworkSession _subSession = null!;
        private IDatabaseService _databaseService = null!;

        [TestInitialize]
        public void Setup()
        {
            _subSession = Substitute.For<INetworkSession>();
            _subSession.SessionId.Returns(Guid.NewGuid().ToString());

            _subSessionFactory = Substitute.For<INetworkSessionFactory>();
            _subSessionFactory.Create(Arg.Any<TcpClient>()).Returns(_subSession);
            // 가짜 팩토리
            _databaseService = Substitute.For<IDatabaseService>();

            _server = new GameServer(_subSessionFactory, _databaseService);
            _server._connectionOption = new ConnectionOption("", 1234, LoginType.Guest,
                DoubleThreeRuleType.BothForbidden, ConnectionType.Server, CancellationToken.None, 3);
        }

        [TestMethod]
        public async Task ProcessDataAsnyc_JoinData_Nickname_not_duplicate()
        {
            var joindata = new RequestJoinData { AuthInfo = new AuthInfo(LoginType.Guest, "", "") };

            var sentPackets = new List<GameData>();
            var session = Substitute.For<INetworkSession>();

            var player = _server.AddSession(session);

            await session.SendAsync(Arg.Do<GameData>(p => sentPackets.Add(p)));

            await _server.ProcessDataAsync(session, joindata);
            await Task.Delay(100);

            Assert.IsTrue(sentPackets.Any(p => p is ClientJoinResponseData));
            // 참가 요청에 대한 응답 메시지 받았는가?

            Assert.AreEqual("Guest", player.Nickname);
            // 닉네임 Guest인가? (중복 안되었으니 그대로여야 함)

            var response = (ClientJoinResponseData)sentPackets.First(p => p is ClientJoinResponseData);
            Assert.IsTrue(response.Accepted);
            Assert.AreEqual("Guest", response.Me.Nickname);
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

            await _server.ProcessDataAsync(s1, new RequestJoinData { AuthInfo = new AuthInfo(LoginType.Guest, "", "") });
            await _server.ProcessDataAsync(s2, new RequestJoinData { AuthInfo = new AuthInfo(LoginType.Guest, "", "") });

            await _server.ProcessDataAsync(s3, new RequestJoinData { AuthInfo = new AuthInfo(LoginType.Guest, "", "") });
            await Task.Delay(100);

            await s1.Received().SendAsync(Arg.Is<ClientJoinData>(p => p.Player.Nickname == "Guest (2)"));
            await s2.Received().SendAsync(Arg.Is<ClientJoinData>(p => p.Player.Nickname == "Guest (2)"));
            // 새 세션 접속 시 브로드캐스트 받았는지 체크

            await _server.ProcessDataAsync(s1, new ChatData { Message = "안녕", Sender = new Player { Nickname = "Guest" } });
            await Task.Delay(100);

            await s1.Received().SendAsync(Arg.Is<ChatData>(p => p.Message == "안녕"));
            await s2.Received().SendAsync(Arg.Is<ChatData>(p => p.Message == "안녕"));
            await s3.Received().SendAsync(Arg.Is<ChatData>(p => p.Message == "안녕"));
        }

        [TestMethod]
        public void GenerateUniqueNickname_Not_Duplicate()
        {
            var newclient = Substitute.For<INetworkSession>();

            string result = _server.GenerateGuestNickname(newclient);

            Assert.AreEqual("Guest", result);
        }

        public static IEnumerable<object[]> ExistNames()
        {
            yield return new object[] { 1, "Guest (1)" };
            yield return new object[] { 2, "Guest (2)" };
            yield return new object[] { 3, "Guest (3)" };
        }

        [TestMethod]
        [DynamicData(nameof(ExistNames))]
        public async Task GenerateUniqueNickname_When_Duplicate(int existGuest, string expected)
        {
            for (int i = 0; i < existGuest; i++)
            {
                var tempsession = Substitute.For<INetworkSession>();
                var p = _server.AddSession(tempsession);
                await _server.ProcessDataAsync(tempsession, new RequestJoinData { AuthInfo = new AuthInfo(LoginType.Guest, "", "") });
            }

            await Task.Delay(100);
            // 비동기 작업 완료 짧은 대기
            var newsession = Substitute.For<INetworkSession>();

            string result = _server.GenerateGuestNickname(newsession);

            Assert.AreEqual(expected, result);
        }
    }
}
