namespace Gomoku.Models
{
    public class Record(int win, int loss, int draw)
    {
        public int Win { get; set; } = win;
        public int Loss { get; set; } = loss;
        public int Draw { get; set; } = draw;
    }
    public class Player
    {
        public string Nickname { get; set; } = "익명";
        public PlayerType Type { get; set; } = PlayerType.Observer;

        public Record Records { get; set; } = new Record(0, 0, 0);

        public Player() { }

        public Player(string nickname, PlayerType type, Record records)
        {
            Nickname = nickname;
            Type = type;
            Records = records;
        }
    }
}
