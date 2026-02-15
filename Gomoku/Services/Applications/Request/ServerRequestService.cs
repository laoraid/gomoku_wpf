/*
 * ServerRequestService.cs
 * 서버와 통신하여 정보를 요청할때 사용하는 서비스 클래스입니다.
 * 
 * 매치 정보, 착수 히스토리, 랭킹
 * 패킷은 IMessenger를 통해 들어오므로 IRecipeint를 구현하여 사용합니다.
 */
using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Common;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;
using Gomoku.Models.Network;

namespace Gomoku.Services.Applications.Request
{
    public class ServerRequestService : IServerRequestService,
        IRecipient<ClientActivatedMessage>,
        IRecipient<ClientDeactivatedMessage>,
        IRecipient<RankingsData>,
        IRecipient<MatchesData>,
        IRecipient<MatchMoveData>
    {
        private IGameClient? _client;

        private TaskCompletionSource<IEnumerable<RankInfo>>? _rankingsTcs;
        private TaskCompletionSource<IEnumerable<MatchInfo>>? _matchesTcs;
        private TaskCompletionSource<IEnumerable<GameMove>>? _movesTcs;

        public ServerRequestService(IMessenger messenger)
        {
            messenger.RegisterAll(this);
        }
        public async Task<IEnumerable<GameMove>> RequestMatchMovesAsync(MatchInfo match)
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");

            if (_movesTcs != null)
                throw new InvalidOperationException("이미 착수 정보를 요청 중입니다.");

            var request = new RequestMatchMoveData { Match = match };

            try
            {
                _movesTcs = new TaskCompletionSource<IEnumerable<GameMove>>();
                await _client.SendDataAsync(request);
                return await _movesTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch { throw; }
            finally
            {
                _movesTcs = null;
            }
        }

        public async Task<IEnumerable<MatchInfo>> RequestSearchMatchesAsync(
            string? PlayerNickname = null,
            string? BlackPlayerNickname = null,
            string? WhitePlayerNickname = null,
            DateTime? from = null,
            DateTime? to = null,
            int PageNumber = 1,
            int PageSize = 20)
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");

            if (_matchesTcs != null)
                throw new InvalidOperationException("이미 매치 정보를 요청 중입니다.");

            var request = new RequestMatchesData
            {
                PlayerNickname = PlayerNickname,
                BlackPlayerNickname = BlackPlayerNickname,
                WhitePlayerNickname = WhitePlayerNickname,
                from = from,
                to = to,
                PageNumber = PageNumber,
                PageSize = PageSize
            };

            try
            {
                _matchesTcs = new TaskCompletionSource<IEnumerable<MatchInfo>>();
                await _client.SendDataAsync(request);
                return await _matchesTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch { throw; }
            finally
            {
                _matchesTcs = null;
            }
        }


        public async Task<IEnumerable<RankInfo>> RequestRankingsAsync()
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");

            if (_rankingsTcs != null)
                throw new InvalidOperationException("이미 랭킹 정보를 요청 중입니다.");

            try
            {
                _rankingsTcs = new TaskCompletionSource<IEnumerable<RankInfo>>();
                await _client.SendDataAsync(new RequestRankingsData());

                return await _rankingsTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex) // when (ex is TimeoutException || ex is ServerException)
            {
                // TODO: 예외 처리
                return Array.Empty<RankInfo>();
            }
            finally
            {
                _rankingsTcs = null;
            }
        }

        public void Receive(ClientActivatedMessage message)
        {
            _client = message.Client;
        }

        public void Receive(ClientDeactivatedMessage message)
        {
            _client = null;
        }

        public void Receive(RankingsData message)
        {
            if (message.Accepted && message.Rankings != null)
            {
                _rankingsTcs?.TrySetResult(message.Rankings);
            }
            else
            {
                _rankingsTcs?.TrySetException(new ServerException("랭킹 정보를 가져오지 못했습니다."));
            }
        }

        public void Receive(MatchesData message)
        {
            if (message.Accepted && message.Matches != null)
            {
                _matchesTcs?.TrySetResult(message.Matches);
            }
            else
            {
                _matchesTcs?.TrySetException(new ServerException("매치 정보를 가져오지 못했습니다."));
            }
        }

        public void Receive(MatchMoveData message)
        {
            if (message.Accepted && message.Moves != null)
            {
                _movesTcs?.TrySetResult(message.Moves);
            }
            else
            {
                _movesTcs?.TrySetException(new ServerException("착수 정보를 가져오지 못했습니다."));
            }
        }
    }
}
