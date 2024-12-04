using LeaderboardManager.Services;

namespace LeaderboardManager.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILeaderboardService leaderboardService)
        {
            // Skip authentication for leaderboard creation endpoint
            if (context.Request.Path.Equals("/api/leaderboard/create", StringComparison.OrdinalIgnoreCase) &&
                context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is missing");
                return;
            }

            var isValid = await leaderboardService.ValidateApiKey(apiKey);
            if (!isValid)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            var leaderboards = await leaderboardService.GetLeaderboardsList();
            var leaderboard = leaderboards.FirstOrDefault(l => l.ApiKey == apiKey);
            context.Items["TableName"] = leaderboard?.Name;

            await _next(context);
        }
    }
}