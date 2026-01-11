using Gomoku.Models;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class GameServerTest
    {
        private GameServer _server;
        private INetworkSessionFactory _subSessionFactory;
        private INetworkSession _subSession;

        [TestInitialize]
        public void Setup()
        {
            _subSession = Substitute.For<INetworkSession>();
            _subSession.Nickname.Returns("익명");
            _subSession.SessionId.Returns(Guid.NewGuid().ToString());

            _subSessionFactory = Substitute.For<INetworkSessionFactory>();
            _subSessionFactory.Create(Arg.Any<TcpClient>()).Returns(_subSession);
            // 가짜 팩토리

            _server = new GameServer(_subSessionFactory);
        }

        [TestMethod]
        public async Task ProcessDataAsnyc_JoinData_Nickname_not_duplicate()
        {
            var joindata = new ClientJoinData { Nickname = "이름1" };

            var sentPackets = new List<GameData>();
            await _subSession.SendAsync(Arg.Do<GameData>(p => sentPackets.Add(p)));

            await _server.ProcessDataAsync(_subSession, joindata);

            Assert.IsTrue(sentPackets.Any(p => p is ClientJoinResponseData));
            // 참가 요청에 대한 응답 메시지 받았는가?

            Assert.AreEqual("이름1", _subSession.Nickname);
            // 닉네임 그대로인가? (중복 안되었으니 그대로여야 함)

            var response = (ClientJoinResponseData)sentPackets.First(p => p is ClientJoinResponseData);
            Assert.IsTrue(response.Accepted);
            Assert.AreEqual("이름1", response.ConfirmedNickname);
            // 응답 데이터 확인
        }

        [TestMethod]
        public async Task Broadcast_Test()
        {
            var s1 = Substitute.For<INetworkSession>();
            var s2 = Substitute.For<INetworkSession>();
            var s3 = Substitute.For<INetworkSession>();

            s1.Nickname.Returns("익명1");
            s2.Nickname.Returns("익명2");
            s3.Nickname.Returns("익명3");

            _server.SessionAdd(s1);
            _server.SessionAdd(s2);
            _server.SessionAdd(s3);

            await _server.ProcessDataAsync(s1, new ClientJoinData { Nickname = "익명1" });
            await _server.ProcessDataAsync(s2, new ClientJoinData { Nickname = "익명2" });

            await _server.ProcessDataAsync(s3, new ClientJoinData { Nickname = "익명3" });

            await s1.Received().SendAsync(Arg.Is<ClientJoinData>(p => p.Nickname == "익명3"));
            await s2.Received().SendAsync(Arg.Is<ClientJoinData>(p => p.Nickname == "익명3"));
            // 새 세션 접속 시 브로드캐스트 받았는지 체크

            await _server.Broadcast(new ChatData { Message = "안녕", SenderNickname = "익명1" });

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
        public void GenerateUniqueNickname_When_Duplicate(string[] existNames, string input, string expected)
        {
            foreach (string name in existNames)
            {
                var tempsession = Substitute.For<INetworkSession>();
                tempsession.Nickname.Returns(name);
                _server.SessionAdd(tempsession);
            }

            var newsession = Substitute.For<INetworkSession>();

            string result = _server.GenerateUniqueNickname(newsession, input);

            Assert.AreEqual(expected, result);
        }
    }
}
