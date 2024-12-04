using LeaderboardManager.Models;
using LeaderboardManager.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LeaderboardManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;

        public LeaderboardController(ILeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<LeaderboardInfo>> CreateLeaderboard(CreateLeaderboardRequest request)
        {
            try
            {
                var leaderboard = await _leaderboardService.CreateLeaderboard(request);
                return Ok(leaderboard);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("list")]
        public async Task<ActionResult<List<LeaderboardInfo>>> GetLeaderboards()
        {
            var leaderboards = await _leaderboardService.GetLeaderboardsList();
            return Ok(leaderboards);
        }

        [HttpGet]
        public async Task<ActionResult<List<Score>>> GetScores()
        {
            var tableName = HttpContext.Items["TableName"] as string;
            var scores = await _leaderboardService.GetScores(tableName);
            return Ok(scores);
        }

        [HttpGet("top/{count}")]
        public async Task<ActionResult<List<Score>>> GetTopScores(int count)
        {
            var tableName = HttpContext.Items["TableName"] as string;
            var scores = await _leaderboardService.GetTopScores(tableName, count);
            return Ok(scores);
        }

        [HttpGet("rank/{rank}")]
        public async Task<ActionResult<Score>> GetScoreByRank(int rank)
        {
            var tableName = HttpContext.Items["TableName"] as string;
            var score = await _leaderboardService.GetScoreByRank(tableName, rank);
            return score != null ? Ok(score) : NotFound();
        }

        [HttpGet("daterange")]
        public async Task<ActionResult<List<Score>>> GetScoresByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var tableName = HttpContext.Items["TableName"] as string;
            var scores = await _leaderboardService.GetScoresByDateRange(tableName, startDate, endDate);
            return Ok(scores);
        }

        [HttpPost]
        public async Task<ActionResult<Score>> AddOrUpdateScore(Score score)
        {
            if (string.IsNullOrWhiteSpace(score.Name))
                return BadRequest("Player name cannot be empty");

            if (score.ExtraInfo?.Length > 255)
                return BadRequest("ExtraInfo cannot exceed 255 characters");

            var tableName = HttpContext.Items["TableName"] as string;

            try
            {
                var result = await _leaderboardService.AddOrUpdateScore(tableName, score);
                var isUpdate = result.Points == score.Points;

                if (isUpdate)
                    return Ok(new
                    {
                        message = "Score updated successfully",
                        score = result
                    });
                else
                    return Ok(new
                    {
                        message = "Existing higher score maintained",
                        score = result
                    });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing score: {ex.Message}");
            }
        }

        [HttpDelete]
        public async Task<ActionResult> DeleteLeaderboard()
        {
            try
            {
                var tableName = HttpContext.Items["TableName"] as string;
                await _leaderboardService.DeleteLeaderboard(tableName);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }


        [HttpGet("player/{playerName}")]
        public async Task<ActionResult<Score>> GetScoreByPlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return BadRequest("Player name cannot be empty");

            var tableName = HttpContext.Items["TableName"] as string;
            var score = await _leaderboardService.GetScoreByPlayerName(tableName, playerName);

            if (score == null)
                return NotFound($"No score found for player: {playerName}");
            Console.WriteLine(JsonSerializer.Serialize(score)); // Inspeccionar el formato

            return Ok(score);
        }

        [HttpDelete("player/{playerName}")]
        public async Task<ActionResult> DeleteScoreByPlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return BadRequest("Player name cannot be empty");

            var tableName = HttpContext.Items["TableName"] as string;
            var deleted = await _leaderboardService.DeleteScoreByPlayerName(tableName, playerName);

            if (!deleted)
                return NotFound($"No score found for player: {playerName}");

            return Ok($"Score for player {playerName} has been deleted");
        }
    }
}