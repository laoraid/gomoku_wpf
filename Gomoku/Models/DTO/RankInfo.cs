namespace Gomoku.Models.DTO
{
    public class RankInfo
    {
        public int Rank { get; set; }
        public Player Player { get; set; }

        public string Nickname => Player.Nickname;
        public int Wins => Player.Records.Win;
        public int Losses => Player.Records.Loss;
        public int Draws => Player.Records.Draw;

        public double WinRate
        {
            get
            {
                int totalGames = Wins + Losses + Draws;
                return totalGames == 0 ? 0.0 : (double)Wins / totalGames * 100.0;
            }
        }

        public RankInfo(int rank, Player player)
        {
            Rank = rank;
            Player = player;
        }
    }
}
