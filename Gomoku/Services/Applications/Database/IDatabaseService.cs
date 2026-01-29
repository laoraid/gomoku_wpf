using Gomoku.Models.Domain;
using Gomoku.Models.DTO;

namespace Gomoku.Services.Applications.Database
{
    public enum RecordUpdateType
    {
        Win, Loss, Draw
    }

    public interface IDatabaseService
    {
        /// <summary>
        /// 로그인을 시도합니다.
        /// </summary>
        /// <param name="id">아이디</param>
        /// <param name="pw">패스워드</param>
        /// <returns>자신의 Player 객체</returns>
        /// <exception cref="PasswordWrongException">패스워드가 틀렸을때</exception>
        /// <exception cref="AccountNotExistException">계정이 존재하지 않을때</exception>
        Task<Player> TryLoginAsync(string id, string pw);

        /// <summary>
        /// 계정을 새로 만듭니다.
        /// </summary>
        /// <param name="id">만들 계정 아이디</param>
        /// <param name="pw">패스워드</param>
        /// <param name="nickname">닉네임</param>
        /// <returns></returns>
        /// <exception cref="IdDuplicateException">아이디가 이미 있을때</exception>
        /// <exception cref="NicknameDuplicateException">닉네임이 이미 있을때</exception>
        Task<Player> CreateAccountAsync(string id, string pw, string nickname);

        /// <summary>
        /// 계정을 삭제합니다.
        /// </summary>
        /// <param name="userid">삭제할 계정 아이디</param>
        /// <param name="pw">비밀번호</param>
        /// <returns></returns>
        /// <exception cref="PasswordWrongException">패스워드가 틀렸을떄</exception>
        /// <exception cref="AccountNotExistException">계정이 존재하지 않을때</exception>
        Task DeleteAccountAsync(string id, string pw);

        /// <summary>
        /// 매치 정보를 저장합니다.
        /// </summary>
        /// <param name="match">매치 정보</param>
        /// <returns></returns>
        Task SaveMatchAsync(MatchInfo match);

        /// <summary>
        /// 플레이어 전적을 가져옵니다.
        /// </summary>
        /// <param name="player">전적을 가져올 플레이어</param>
        /// <returns></returns>
        /// <exception cref="GuestPlayerException">게스트 플레이어의 전적을 가져오려 할 때</exception>
        /// <exception cref="AccountNotExistException">계정이 존재하지 않을 때</exception>
        Task<Record> GetPlayerRecordsAsync(Player player);


        /// <summary>
        /// 플레이어의 매치 히스토리를 가져옵니다.
        /// </summary>
        /// <param name="player">매치 히스토리를 가져올 플레이어</param>
        /// <returns></returns>
        /// <exception cref="GuestPlayerException">게스트 플레이어의 매치 히스토리를 가져오려 할 때</exception>
        Task<IEnumerable<MatchInfo>> GetPlayerMatchHistoriesAsync(Player player);

        /// <summary>
        /// 상대 전적을 가져옵니다.
        /// </summary>
        /// <param name="black">흑 플레이어</param>
        /// <param name="white">백 플레이어</param>
        /// <returns>흑 플레이어의 상대전적, 백 플레이어의 상대전적</returns>
        /// <exception cref="GuestPlayerException">게스트 플레이어와의 전적을 가져오려 할때</exception>
        Task<(Record BlackRecord, Record WhiteRecord)> GetRelativeRecordsAsync(Player black, Player white);

        /// <summary>
        /// 닉네임을 변경합니다.
        /// </summary>
        /// <param name="userid">닉네임을 바꿀 계정 아이디</param>
        /// <param name="newnickname">새로운 닉네임</param>
        /// <returns></returns>
        /// <exception cref="AccountNotExistException">계정이 존재하지 않을때</exception>
        /// <exception cref="NicknameDuplicateException">닉네임이 중복될때</exception>
        Task<bool> ChangeNicknameAsync(string userid, string newnickname);


        /// <summary>
        /// 플레이어 랭킹 정보를 가져옵니다. (최대 10개)
        /// </summary>
        /// <returns>랭킹 정보 리스트</returns>
        Task<IEnumerable<RankInfo>> GetPlayerRanksAsync();
    }
}
