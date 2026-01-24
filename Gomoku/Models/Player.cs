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
        public int Id { get; set; } = 1;
        public string AccountId { get; set; } = "Guest";
        public string Nickname { get; set; } = "익명";
        public PlayerType Type { get; set; } = PlayerType.Observer;

        public Record Records { get; set; } = new Record(0, 0, 0);

        public int LeftCancelLast { get; set; } = 3;

        public Player() { }

        public Player(int id, string accountId, string nickname, PlayerType type, Record? records = null)
        {
            Id = id;
            AccountId = accountId;
            Nickname = nickname;
            Type = type;
            Records = records ?? new Record(0, 0, 0);
        }
    }
}
