namespace Gomoku.Models
{
    public record Record(int winCount, int LossCount, int DrawCount);
    public class Player
    {
        public string Nickname { get; set; } = "익명";
        public PlayerType Type { get; set; } = PlayerType.Observer;

        public Record Record { get; set; } = new Record(0, 0, 0);

        public Player() { }

        public Player(string nickname, PlayerType type, Record record)
        {
            Nickname = nickname;
            Type = type;
            Record = record;
        }
    }
}
