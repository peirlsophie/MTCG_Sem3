using MTCG.Businesslogic;
using MTCG.Database;
using MTCG.NewFolder;
using MTCG_Peirl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.HTTP
{
    internal class BattlesEndpoint
    {
        private readonly DatabaseAccess dbAccess;
        private readonly UserDatabase userDatabase;
        private readonly CardPackagesDatabase cardPackagesDb;
        private readonly PackagesEndpoint packagesEndpoint;
        private readonly ScoreboardTradesDatabase scoreboardTradesDb;
        public BattlesEndpoint(DatabaseAccess dbAccess) 
        {
            this.cardPackagesDb = new CardPackagesDatabase(dbAccess);
            this.userDatabase = new UserDatabase(dbAccess);
            this.packagesEndpoint = new PackagesEndpoint(dbAccess);
            this.scoreboardTradesDb = new ScoreboardTradesDatabase(dbAccess);
            this.dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));

        }

        public void handleBattlesRequests(HttpRequest request, HttpResponse response)
        {
            if (request.Method == "POST" && request.Path == "/battles")
            {
                //handle battle logic
                startBattle(request, response);
                
            }
            else
            {
                Console.WriteLine($"{request.Method} + {request.Path}");
                response.statusCode = 400;
                response.statusMessage = $"HTTP {response.statusCode} Bad request";
            }
        }

        public void startBattle(HttpRequest request, HttpResponse response)
        {
            try
            {
                int roundCounter = 0;
                int player1WinCount = 0;
                int player2WinCount = 0;

                var username = packagesEndpoint.extractUsernameFromToken(request);
                Battle battle = new Battle(dbAccess);

                battle.startBattle(username);
                response.statusCode = 200;
                response.statusMessage = $"HTTP {response.statusCode} Battle started successfully.";

                List<string> players = userDatabase.showPlayersInBattle();
               
                List<User> playerObjects = battle.createPlayerObjects(players[0], players[1]);


                User player1 = playerObjects[0];
                User player2 = playerObjects[1];

                while (roundCounter < 10)
                {
                    if (player1.ownedCards.Count == 0 || player2.ownedCards.Count == 0)
                    {
                        Console.WriteLine("A player's deck is empty, stopping the battle.");
                        break;
                    }
                    roundCounter++;
                    Console.WriteLine($"Starting round {roundCounter}");

                    User winner = battle.playBattleRound(player1, player2);

                    if (winner == player1)
                    {
                        player1WinCount++;
                        Console.WriteLine($"{player1.Username} wins round {roundCounter}");
                    }
                    else if (winner == player2)
                    {
                        player2WinCount++;
                        Console.WriteLine($"{player2.Username} wins round {roundCounter}");
                    }
                    else
                    {
                        Console.WriteLine($"Round {roundCounter} was a draw.");
                    }
                }


                if (player1WinCount > player2WinCount)
                {
                    response.statusCode = 200;
                    response.statusMessage = $"{player1.Username} wins the battle with {player1WinCount} rounds won!";
                    eloCalcUsers(player1, player2);
                }
                else if (player2WinCount > player1WinCount)
                {
                    response.statusCode = 200;
                    response.statusMessage = $"{player2.Username} wins the battle with {player2WinCount} rounds won!";
                    eloCalcUsers(player2, player1);
                    Console.WriteLine($"Player1 stats: gamesplayed {player1.games_played}, wins:{player1.Wins}, losses: {player1.Losses}, elo: {player1.ELO}");
                    Console.WriteLine($"Player2 stats: gamesplayed {player2.games_played}, wins:{player2.Wins}, losses: {player2.Losses}, elo: {player2.ELO}");
                }
                else
                {
                    response.statusCode = 200;
                    response.statusMessage = "The battle ended in a draw!";

                }

                Console.WriteLine($"kommst hierher beide usernames:{player1.Wins}, {player1.Losses},{player2.Wins}, {player2.Losses}");
                userDatabase.changeUserStats(player1);
                userDatabase.changeUserStats(player2);
                var userId1 = cardPackagesDb.findUserIdByName(player1.Username);
                var userId2 = cardPackagesDb.findUserIdByName(player2.Username);
                Console.WriteLine($"kommst hierher beide userids:{userId1}, {userId2}, {player1.ELO}, {player2.ELO}");

                scoreboardTradesDb.updateScoreboard(userId1, player1.ELO);
                scoreboardTradesDb.updateScoreboard(userId2, player2.ELO);
                Console.WriteLine($"db insert funktioniert?");


            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = ex.Message;

            }
        }


        public void eloCalcUsers(User winner, User loser)
        {
            winner.Wins++;
            winner.games_played++;
            winner.ELO += 3;

            loser.Losses++;
            loser.games_played++;
            loser.ELO -= 5;
        }
    }
}
