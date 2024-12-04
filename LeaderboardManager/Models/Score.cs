namespace LeaderboardManager.Models
{
    public class Score
    {
        public int Rank { get; set; }
        public string Name { get; set; }
        public int Points { get; set; }
        public string ExtraInfo { get; set; }
        public DateTime RecordDate { get; set; }
    }
}
