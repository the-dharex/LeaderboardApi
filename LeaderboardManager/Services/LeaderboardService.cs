using CsvHelper;
using LeaderboardManager.Models;
using System.Formats.Asn1;
using System.Globalization;
using System.Text.Json;

namespace LeaderboardManager.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly string _baseDirectory;
        private readonly string _configFile;

        public LeaderboardService(IConfiguration configuration)
        {
            _baseDirectory = configuration["CsvDirectory"] ?? "Data/Leaderboards";
            _configFile = Path.Combine(_baseDirectory, "leaderboards.json");
            Directory.CreateDirectory(_baseDirectory);
            InitializeConfigFile();
        }


        private void InitializeConfigFile()
        {
            if (!File.Exists(_configFile))
            {
                File.WriteAllText(_configFile, JsonSerializer.Serialize(new List<LeaderboardInfo>()));
            }
        }

        private async Task<List<LeaderboardInfo>> GetLeaderboardsConfig()
        {
            var json = await File.ReadAllTextAsync(_configFile);
            return JsonSerializer.Deserialize<List<LeaderboardInfo>>(json) ?? new List<LeaderboardInfo>();
        }

        private async Task SaveLeaderboardsConfig(List<LeaderboardInfo> leaderboards)
        {
            await File.WriteAllTextAsync(_configFile, JsonSerializer.Serialize(leaderboards));
        }

        public async Task<LeaderboardInfo> CreateLeaderboard(CreateLeaderboardRequest request)
        {
            var leaderboards = await GetLeaderboardsConfig();

            if (leaderboards.Any(l => l.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Leaderboard name already exists");

            var newLeaderboard = new LeaderboardInfo
            {
                Name = request.Name,
                Description = request.Description,
                ApiKey = Guid.NewGuid().ToString("N"),
                CreationDate = DateTime.UtcNow
            };

            leaderboards.Add(newLeaderboard);
            await SaveLeaderboardsConfig(leaderboards);

            return newLeaderboard;
        }

        public async Task<List<LeaderboardInfo>> GetLeaderboardsList()
        {
            return await GetLeaderboardsConfig();
        }

        public async Task<bool> ValidateApiKey(string apiKey)
        {
            var leaderboards = await GetLeaderboardsConfig();
            return leaderboards.Any(l => l.ApiKey == apiKey);
        }

        public async Task<List<Score>> GetScores(string tableName)
        {
            var filePath = Path.Combine(_baseDirectory, $"{tableName}.csv");
            if (!File.Exists(filePath))
                return new List<Score>();

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<Score>().OrderByDescending(s => s.Points).ToList();
        }

        public async Task<List<Score>> GetTopScores(string tableName, int count)
        {
            var scores = await GetScores(tableName);
            return scores.Take(count).ToList();
        }

        public async Task<Score> GetScoreByRank(string tableName, int rank)
        {
            var scores = await GetScores(tableName);
            return scores.FirstOrDefault(s => s.Rank == rank);
        }

        public async Task<List<Score>> GetScoresByDateRange(string tableName, DateTime startDate, DateTime endDate)
        {
            var scores = await GetScores(tableName);
            return scores.Where(s => s.RecordDate >= startDate && s.RecordDate <= endDate).ToList();
        }

        public async Task<Score> GetScoreByPlayerName(string tableName, string playerName)
        {
            var scores = await GetScores(tableName);
            return scores.FirstOrDefault(s => s.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Score> AddOrUpdateScore(string tableName, Score newScore)
        {
            var scores = await GetScores(tableName);
            var existingScore = scores.FirstOrDefault(s =>
                s.Name.Equals(newScore.Name, StringComparison.OrdinalIgnoreCase));

            if (existingScore != null)
            {
                // Si el jugador ya existe, actualizar solo si la nueva puntuación es mayor
                if (newScore.Points > existingScore.Points)
                {
                    scores.Remove(existingScore);
                    newScore.RecordDate = DateTime.UtcNow;
                    scores.Add(newScore);
                }
                else
                {
                    // Si la nueva puntuación no es mayor, mantener la existente
                    return existingScore;
                }
            }
            else
            {
                // Si es un nuevo jugador, añadir la puntuación
                newScore.RecordDate = DateTime.UtcNow;
                scores.Add(newScore);
            }

            // Guardar los cambios
            var filePath = Path.Combine(_baseDirectory, $"{tableName}.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                await csv.WriteRecordsAsync(scores.OrderByDescending(s => s.Points));
            }

            await UpdateRanks(tableName);
            return newScore;
        }

        public async Task UpdateRanks(string tableName)
        {
            var scores = await GetScores(tableName);
            var rank = 1;
            foreach (var score in scores.OrderByDescending(s => s.Points))
            {
                score.Rank = rank++;
            }

            var filePath = Path.Combine(_baseDirectory, $"{tableName}.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                await csv.WriteRecordsAsync(scores);
            }
        }

        public async Task DeleteLeaderboard(string tableName)
        {
            var leaderboards = await GetLeaderboardsConfig();
            var leaderboard = leaderboards.FirstOrDefault(l => l.Name == tableName);

            if (leaderboard == null)
                throw new InvalidOperationException("Leaderboard not found");

            leaderboards.Remove(leaderboard);
            await SaveLeaderboardsConfig(leaderboards);

            var filePath = Path.Combine(_baseDirectory, $"{tableName}.csv");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public async Task<bool> DeleteScoreByPlayerName(string tableName, string playerName)
        {
            var scores = await GetScores(tableName);
            var score = scores.FirstOrDefault(s =>
                s.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));

            if (score == null)
                return false;

            scores.Remove(score);

            var filePath = Path.Combine(_baseDirectory, $"{tableName}.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                await csv.WriteRecordsAsync(scores);
            }

            await UpdateRanks(tableName);
            return true;
        }
    }
}
