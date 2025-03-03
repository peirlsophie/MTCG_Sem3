using Azure;
using Azure.Core;
using MTCG.Database;
using MTCG.NewFolder;
using MTCG_Peirl.Models;
using System;
using System.Text.Json;


using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.HTTP
{
    public class CardsDeckEndpoint
    {
        private readonly DatabaseAccess dbAccess;
        private readonly UserDatabase userDatabase;
        private readonly CardPackagesDatabase cardPackagesDb;
        public CardsDeckEndpoint(DatabaseAccess dbAccess) 
        {
            this.cardPackagesDb = new CardPackagesDatabase(dbAccess);
            this.userDatabase = new UserDatabase(dbAccess);
            this.dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));
        }

        public async Task handleCardDeckRequests(HttpRequest request, HttpResponse response)
        {
            if (request.Method == "GET" && request.Path == "/cards")
            {
                //show a list of all owned cards/stack
                showStack(request, response);
            }
            else if (request.Method == "GET" && request.Path == "/deck")
            {
                //show deck
                showDeck(request, response);
            }
            else if(request.Method == "PUT" && request.Path == "/deck")
            {
                //configure deck
                configureDeck(request, response);
            }
            else
            {
                Console.WriteLine($"{request.Method} + {request.Path}");
                response.statusCode = 400;
                response.statusMessage = $"HTTP {response.statusCode} Bad request";
            }

        }

        public string extractUsernameFromToken(HttpRequest request)
        {
            try
            {
                var authHeader = request.Headers["Authorization"];
                if (string.IsNullOrEmpty(authHeader))
                {
                    throw new ArgumentException("Authorization header is missing");
                }

                var token = authHeader.Split(" ")[1];
                int index = token.IndexOf("-mtcg");
                Console.WriteLine("index: " + index);
                if (index == -1)
                {
                    throw new ArgumentException("Invalid token format");
                }
                string username = token.Substring(0, index);

                return username;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return "";
            }
        }


        public void showStack(HttpRequest request, HttpResponse response)
        {
            try
            {
                string username = extractUsernameFromToken(request);

                if (username == null)
                {
                    response.statusCode = 401;
                    response.statusMessage = $"HTTP {response.statusCode} Unauthorized";
                    return;
                }

                var cardIds = cardPackagesDb.findOwnedCardIdsInStacks(username);
                if (cardIds.Count == 0)
                {
                    response.statusCode = 200;
                    response.statusMessage= $"HTTP {response.statusCode} Cards found for user {username}:[]";
                    return;
                }
                var cardNames = cardPackagesDb.getCardNames(cardIds);
                string cardList = string.Join(", ", cardNames);
                response.statusCode = 200;
                response.statusMessage = $"HTTP {response.statusCode} Cards found for user {username}:[{cardList}]";
  
            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = $"HTTP {response.statusCode} Server error: " + ex.Message;
            }
        }

        public void showDeck(HttpRequest request, HttpResponse response)
        {
            try
            {
                string username = extractUsernameFromToken(request);
                Console.WriteLine("problem mit extraction?");
                if (username == null)
                {
                    response.statusCode = 401;
                    response.statusMessage = $"HTTP {response.statusCode} Unauthorized";
                    return;
                }

                var cardIds = cardPackagesDb.findOwnedCardIdsInDecks(username);
                if (cardIds.Count == 0)
                {
                    response.statusCode = 200;
                    response.statusMessage = $"HTTP {response.statusCode} Deck for user {username}:[]";
                    return;
                }
                var cardNames = cardPackagesDb.getCardNames(cardIds);

                string cardList = string.Join(", ", cardNames);
                response.statusCode = 200;
                response.statusMessage = $"HTTP {response.statusCode} Deck for user {username}:[{cardList}]";

            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = $"HTTP {response.statusCode} Server error: " + ex.Message;
            }


        }

        public void configureDeck(HttpRequest request, HttpResponse response)
        {
            try
            {
                string username = extractUsernameFromToken(request);
                int userId = cardPackagesDb.findUserIdByName(username);
                if (username == null)
                {
                    response.statusCode = 401;
                    response.statusMessage = $"HTTP {response.statusCode} Unauthorized";
                    return;
                }
                List<string> cardIds;
                try
                {
                    cardIds = JsonSerializer.Deserialize<List<string>>(request.Content);
                }
                catch (JsonException)
                {
                    response.statusCode = 400;
                    response.statusMessage = "HTTP 400 Bad Request: Invalid JSON format.";
                    return;
                }
                if(cardIds.Count != 4)
                {
                    response.statusCode = 405;
                    response.statusMessage = $"HTTP {response.statusCode} Bad request ";
                }
                else
                {
                    if(cardPackagesDb.checkDeckSize(username) == 4)
                    {
                        response.statusCode = 405;
                        response.statusMessage = $"HTTP {response.statusCode} Deck already configured ";
                        showDeck(request, response);
                        
                    }
                    else if(cardPackagesDb.saveDeckConfig(cardIds, userId))
                    {
                        response.statusCode = 200;
                        response.statusMessage = $"HTTP {response.statusCode} deck configured for {username}.";
                        cardPackagesDb.markCardAsInDeck(cardIds);
                    }
                    else
                    {
                        response.statusCode = 500;
                        response.statusMessage = $"HTTP {response.statusCode} Failed to update the deck.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = $"HTTP {response.statusCode} Server error: " + ex.Message;
            }
        }
    }
}
