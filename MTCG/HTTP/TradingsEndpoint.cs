using Azure.Core;
using MTCG.Database;
using MTCG.NewFolder;
using MTCG_Peirl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using MTCG.Businesslogic;


namespace MTCG.HTTP
{
    internal class TradingsEndpoint
    {
        private readonly DatabaseAccess dbAccess;
        private readonly ScoreboardTradesDatabase tradesDatabase;
        private readonly UserDatabase userDatabase;
        private readonly CardPackagesDatabase cardPackagesDatabase;
        private readonly PackagesEndpoint packagesEndpoint;

        public TradingsEndpoint(DatabaseAccess dbAccess)
        {
            this.tradesDatabase = new ScoreboardTradesDatabase(dbAccess);
            this.packagesEndpoint = new PackagesEndpoint(dbAccess);
            this.cardPackagesDatabase = new CardPackagesDatabase(dbAccess);
            this.userDatabase = new UserDatabase(dbAccess);
            this.dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));

        }

        public void handleTradingsRequests(HttpRequest request, HttpResponse response)
        {
            if (request.Method == "POST" && request.Path == "/tradings")
            {
                //create trading deal
                createTradingDeals(request, response);

            }
            else if(request.Method == "GET" && request.Path == "/tradings")
            {
                //check trading deals
                checkTradingDeals(request, response);
            }
            else if(request.Method == "DELETE" && request.Path == "/tradings")
            {
                //delete trading deals

            }
            else
            {
                Console.WriteLine($"{request.Method} + {request.Path}");
                response.statusCode = 400;
                response.statusMessage = $"HTTP {response.statusCode} Bad request";
            }
        }

        public void checkTradingDeals(HttpRequest request, HttpResponse response)
        {
            try
            {
                string username = packagesEndpoint.extractUsernameFromToken(request);

                var tradingData = tradesDatabase.getTradingData();
                var outputText = new System.Text.StringBuilder();

                if (tradingData == null || tradingData.Count == 0) 
                { 
                    response.statusCode = 200; 
                    response.statusMessage = "No trading deals found"; 
                    return; 
                }
                foreach (Trade trade in tradingData)
                {
                    
                    bool inDeck = checkIfCardInDeck(username, trade.CardToTrade);
                    if (inDeck)
                    {
                        response.statusCode = 400;
                        response.statusMessage = $"HTTP {response.statusCode} Offering cards in deck is not permitted.";
                        continue;
                    }
                    outputText.AppendLine($"\nTradingID: {trade.Id}, Username: {username} offers CardID: {trade.CardToTrade}, Requirement Type: {trade.Type}, Minimum Damage: {trade.MinimumDamage}");
                    
                }
                string output = outputText.ToString();
                response.statusCode = 200;
                response.statusMessage = $"HTTP {response.statusCode} Trading Offers : {output}";

            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = ex.Message;

            }
        }
        public bool checkIfCardInDeck(string username, string card_id)
        {
            var cardIds = cardPackagesDatabase.findOwnedCardIdsInDecks(username);
            if(cardIds.Contains(card_id))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
       
        public void createTradingDeals(HttpRequest request, HttpResponse response)
        {
            try
            {
                string username = packagesEndpoint.extractUsernameFromToken(request);
                int user_id = cardPackagesDatabase.findUserIdByName(username);

                var tradingData = JsonSerializer.Deserialize<Trade>(request.Content);
                if (tradingData == null) 
                { 
                    response.statusCode = 400; 
                    response.statusMessage = "No data"; 
                    response.SendResponse(); 
                    return; 
                }
                Console.WriteLine($"Trade details before entering into DB: {tradingData.Id}, {user_id}, {tradingData.CardToTrade}, {tradingData.Type}, {tradingData.MinimumDamage}");
                tradesDatabase.enterTradingDeal(tradingData,user_id);
                response.statusCode = 201;
                response.statusMessage = $"HTTP {response.statusCode}";

            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = ex.Message;

            }
        }






        



    }
}
