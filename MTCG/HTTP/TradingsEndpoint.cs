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
using Azure;
using Microsoft.Extensions.FileSystemGlobbing.Internal;


namespace MTCG.HTTP
{
    public class TradingsEndpoint
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

        public async Task handleTradingsRequests(HttpRequest request, HttpResponse response)
        {
            string[] pathSegments = request.Path.Trim('/').Split('/');

            if (request.Method == "POST" && request.Path == "/tradings")
            {
                //create trading deal
                createTradingDeals(request, response);
            }
            else if(request.Method == "POST" && pathSegments.Length == 2 && pathSegments[0] == "tradings")
            {
                //trade
                tradingTransaction(request, response, pathSegments[1]);
            }
            else if (request.Method == "GET" && request.Path == "/tradings")
            {
                //check trading deals
                checkTradingDeals(request, response);
            }
            else if (request.Method == "DELETE" && pathSegments.Length == 2 && pathSegments[0] == "tradings")
            {
                //delete trading deals
                deleteTradingDeals(request, response, pathSegments[1]);

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
                    response.statusMessage = $"HTTP {response.statusCode} \nTrading Offers: ";
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
                response.statusMessage = $"HTTP {response.statusCode} Trading Offers: {output}";

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
            if (cardIds.Contains(card_id))
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

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var tradingData = JsonSerializer.Deserialize<Trade>(request.Content, options);
                if (tradingData == null)
                {
                    response.statusCode = 400;
                    response.statusMessage = "No data";
                    return;
                }
                tradesDatabase.enterTradingDeal(tradingData, user_id);

                response.statusCode = 201;
                response.statusMessage = $"HTTP {response.statusCode}";

            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = ex.Message;
            }
        }

        public void deleteTradingDeals(HttpRequest request, HttpResponse response, string tradeId)
        {
            try
            {
                string username = packagesEndpoint.extractUsernameFromToken(request);
                int user_id = cardPackagesDatabase.findUserIdByName(username);

                if(tradesDatabase.deleteTrade(tradeId))
                {
                    response.statusCode = 202;
                    response.statusMessage = $"HTTP {response.statusCode} trading deal deleted.";
                }
                else
                {
                    response.statusCode = 402;
                    response.statusMessage = $"HTTP {response.statusCode} trading deal could not be deleted.";
                }
            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = ex.Message;
            }
        }
        public void tradingTransaction(HttpRequest request, HttpResponse response, string tradeId)
        {
            try
            {
                var offeredCardId = JsonSerializer.Deserialize<string>(request.Content);
                if (offeredCardId == null)
                {
                    response.statusCode = 400;
                    response.statusMessage = "No card offered";
                    return;
                }
                string username = packagesEndpoint.extractUsernameFromToken(request);
                int user_id = cardPackagesDatabase.findUserIdByName(username);

                var cardIdsOfOwnedCardsInStack = cardPackagesDatabase.findOwnedCardIdsInStacks(username);

                
                if(cardIdsOfOwnedCardsInStack.Contains(offeredCardId))
                {
                    response.statusCode = 403;
                    response.statusMessage = $"HTTP {response.statusCode} You can not trade with yourself.";
                }
                else if(checkIfCardInDeck(username,offeredCardId))
                {
                    response.statusCode = 403;
                    response.statusMessage = $"HTTP {response.statusCode} Cards in deck must not be used for trading.";
                }
                var tradingDealUserId = tradesDatabase.getOfferingUserIdAndCardID(tradeId);

                //switches the cards in stacks
                cardPackagesDatabase.saveTradedCardInStack(tradingDealUserId.userId, offeredCardId);
                cardPackagesDatabase.saveTradedCardInStack(user_id, tradingDealUserId.cardId);
                //deletes the traded cards from previous owners stacks
                cardPackagesDatabase.deleteTradedCardFromStack(user_id, offeredCardId);
                cardPackagesDatabase.deleteTradedCardFromStack(tradingDealUserId.userId, tradingDealUserId.cardId);
            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = ex.Message;
            }
        }
    }
}
