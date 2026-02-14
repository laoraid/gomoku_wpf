using Gomoku.Models.Domain;

namespace Gomoku.Services.Applications
{
    /// <summary>
    /// 패킷으로 들어오는 Player 객체는 사본입니다.
    /// 실제로 클라이언트가 관리하는 Player 객체로 변환하기 위한 서비스 클래스입니다.
    /// </summary>
    public interface IPlayerTrackerService
    {
        /// <summary>
        /// Player 객체의 닉네임으로 실제 사용중인 Player 객체를 불러옵니다.
        /// </summary>
        /// <param name="player">찾을 Player 객체</param>
        /// <returns>관리중인 Player 객체</returns>
        Player GetManagedPlayer(Player player);
        /// <summary>
        /// 닉네임으로 실제 사용중인 Player 객체를 불러옵니다.
        /// </summary>
        /// <param name="nickname">찾을 닉네임</param>
        /// <returns>관리중인 Player 객체</returns>
        Player GetManagedPlayer(string nickname);
        /// <summary>
        /// 플레이어를 관리 객체로 추가합니다.
        /// </summary>
        /// <param name="players">추가할 플레이어들의 열거형</param>
        void AddPlayers(IEnumerable<Player> players);
        /// <summary>
        /// 특정 닉네임의 플레이어를 관리 객체에서 삭제합니다.
        /// </summary>
        /// <param name="nickname">삭제할 플레이어의 닉네임</param>
        void RemovePlayer(string nickname);

        /// <summary>
        /// 모든 플레이어를 삭제합니다.
        /// </summary>
        void Clear();
        /// <summary>
        /// 모든 관리중인 플레이어의 열거형
        /// </summary>
        public IEnumerable<Player> AllPlayers { get; }

    }
}
