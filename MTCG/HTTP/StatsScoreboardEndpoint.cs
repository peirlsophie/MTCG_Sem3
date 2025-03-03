using MTCG.Database;
using MTCG.NewFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.HTTP
{
    public class StatsScoreboardEndpoint
    {
        private readonly DatabaseAccess dbAccess;
        private readonly UserDatabase userDatabase;
        private readonly PackagesEndpoint packagesEndpoint;
        private readonly ScoreboardTradesDatabase scoreboardTradesDatabase;
        public StatsScoreboardEndpoint(DatabaseAccess dbAccess) 
        {
            this.userDatabase = new UserDatabase(dbAccess);
            this.packagesEndpoint = new PackagesEndpoint(dbAccess);
            this.scoreboardTradesDatabase = new ScoreboardTradesDatabase(dbAccess);
            this.dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));
        }

        public async Task handleStatsScoreboardRequests(HttpRequest request, HttpResponse response)
        {
            if (request.Method == "GET" && request.Path == "/stats")
            {
                //show user stats
                showUserStats(request, response);
            }
            else if (request.Method == "GET" && request.Path == "/scoreboard")
            {
                //show scoreboard
                showScoreboard(request, response);
            }
            else
            {
                Console.WriteLine($"{request.Method} + {request.Path}");
                response.statusCode = 400;
                response.statusMessage = $"HTTP {response.statusCode} Bad request";
            }

        }

        public void showUserStats(HttpRequest request, HttpResponse response)
        {
            try
            {
                string username = packagesEndpoint.extractUsernameFromToken(request);
                var userStats = userDatabase.getUserStats(username);

                if (userStats == null)
                {
                    response.statusCode = 401;
                    response.statusMessage = $"HTTP {response.statusCode} No stats found";
                    return;
                }
                response.statusCode = 200;
                response.statusMessage = $"HTTP {response.statusCode} Stats for player {username}, Elo: {userStats[0]}, Games played: {userStats[1]}, Wins: {userStats[2]}, Losses: {userStats[3]}";
            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = $"HTTP {response.statusCode} Server error: " + ex.Message;
            }
        }

        public void showScoreboard(HttpRequest request, HttpResponse response)
        {
            try
            {
                var scoreboardData = scoreboardTradesDatabase.getScoreboardData();
                int rank = 1;
                var outputText = new System.Text.StringBuilder();

                if (scoreboardData == null)
                {
                    response.statusCode = 401;
                    response.statusMessage = "No stats found";
                    return;
                }
                foreach (KeyValuePair<int, int> kvp in scoreboardData)
                {
                    string username = userDatabase.getUsernameById(kvp.Key);
                    outputText.AppendLine($"\nRank: {rank}, Username: {username}, Elo-Score: {kvp.Value}");
                    rank++;
                }
                string output = outputText.ToString();
                response.statusCode = 200;
                response.statusMessage = $"HTTP {response.statusCode} Scoreboard : {output}";
            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = "Server error: " + ex.Message;
            }
        }



    }
}
