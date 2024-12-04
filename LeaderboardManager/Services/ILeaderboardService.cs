using LeaderboardManager.Models;

namespace LeaderboardManager.Services
{
    public interface ILeaderboardService
    {
        Task<List<Score>> GetScores(string tableName);
        Task<Score> AddOrUpdateScore(string tableName, Score score);
        Task UpdateRanks(string tableName);
        Task<LeaderboardInfo> CreateLeaderboard(CreateLeaderboardRequest request);
        Task<List<LeaderboardInfo>> GetLeaderboardsList();
        Task<List<Score>> GetTopScores(string tableName, int count);
        Task<Score> GetScoreByRank(string tableName, int rank);
        Task<List<Score>> GetScoresByDateRange(string tableName, DateTime startDate, DateTime endDate);
        Task DeleteLeaderboard(string tableName);
        Task<bool> ValidateApiKey(string apiKey);
        Task<Score> GetScoreByPlayerName(string tableName, string playerName);  // Cambiado de List<Score> a Score
        Task<bool> DeleteScoreByPlayerName(string tableName, string playerName);
    }
}
