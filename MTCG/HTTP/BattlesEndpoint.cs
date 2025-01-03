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
        public BattlesEndpoint(DatabaseAccess dbAccess) 
        {
            this.cardPackagesDb = new CardPackagesDatabase(dbAccess);
            this.userDatabase = new UserDatabase(dbAccess);
            this.packagesEndpoint = new PackagesEndpoint(dbAccess);
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
                var username = packagesEndpoint.extractUsernameFromToken(request);
                Battle battle = new Battle(dbAccess);
                Console.WriteLine("kommt es hierher / vor Battle Initialisierung");

                battle.startBattle(username);
                response.statusCode = 200;
                response.statusMessage = "Battle started successfully.";

                List<string> players = userDatabase.showPlayersInBattle();

                //Console.WriteLine($"Player1: {players[0]} vs Player2: {players[1]}");

                List<User> playerObjects =  battle.createPlayerObjects(players[0],players[1]);

                User player1 = playerObjects[0];
                User player2 = playerObjects[1];

                List<Card> cards = battle.choosePlayingCards(player1.Username, player2.Username);

                Card card1 = cards[0];
                Card card2 = cards[1];



            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage= ex.Message;
            
            }


        }

    }
}
