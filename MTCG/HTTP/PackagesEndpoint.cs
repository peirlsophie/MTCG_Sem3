using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Core;
using MTCG.Backend;
using MTCG.Database;
using MTCG.NewFolder;
using MTCG_Peirl.Models;
using Npgsql;

namespace MTCG.HTTP
{
    public class PackagesEndpoint
    {
        private readonly DatabaseAccess dbAccess;
        private readonly CardPackagesDatabase cardPackagesDb;
        private readonly UserDatabase userDatabase;
        public PackagesEndpoint(DatabaseAccess dbAccess)
        {
            this.cardPackagesDb = new CardPackagesDatabase(dbAccess);
            this.userDatabase = new UserDatabase(dbAccess);
            this.dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));
        }

        public async Task handlePackageRequests(HttpRequest request, HttpResponse response)
        {
            if (request.Method == "POST" && request.Path == "/packages")
            {
                createPackages(request, response);
            }
            else if (request.Method == "POST" && request.Path == "/transactions/packages")
            {
                purchasePackages(request, response);
            }
            else
            {
                Console.WriteLine($"{request.Method} + {request.Path}");
                response.statusCode = 400;
                response.statusMessage = $"HTTP {response.statusCode} Bad request";
            }

        }

        public void createPackages(HttpRequest request, HttpResponse response)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                        new CardConverter()
                    }
                };
                var packageData = JsonSerializer.Deserialize<List<Card>>(request.Content, options);
                if (packageData == null || packageData.Count != 5)
                {
                    response.statusCode = 400;
                    response.statusMessage = $"HTTP {response.statusCode} Invalid package data";
                    return;
                }
                // Save the package to the database
                cardPackagesDb.savePackageToDatabase(packageData);
                response.statusCode = 201; 
                response.statusMessage = $"HTTP {response.statusCode} Package created successfully";
            }
            catch (JsonException ex)
            {
                response.statusCode = 400;
                response.statusMessage = $"HTTP {response.statusCode} Invalid JSON format: " + ex.Message;
            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = $"HTTP {response.statusCode} Server error: " + ex.Message;
            }
        }

              
        
        public void purchasePackages(HttpRequest request, HttpResponse response)
        {
            try
            {
                if (request == null || response == null)
                {
                    Console.WriteLine("Request or Response is null.");
                    return;
                }
                var username = extractUsernameFromToken(request);

                if (username == null)
                {
                    response.statusCode = 400;
                    response.statusMessage = $"HTTP {response.statusCode} Invalid input";
                    return;
                }
                int availablePackages = cardPackagesDb.countAvailablePackages();
                var availableCoins = userDatabase.checkAvailableCoins(username);

                if (availablePackages > 0)
                {
                    if (availableCoins >= 5)
                    {
                        userDatabase.decreaseCoins(username);
                        cardPackagesDb.updatePurchasedPackage(username);
                        response.statusCode = 201; 
                        response.statusMessage = $"HTTP {response.statusCode} Package purchased successfully.";
                    }
                    else
                    {
                        response.statusCode = 401;
                        response.statusMessage = $"HTTP {response.statusCode} Not enough money";

                    }
                }
                else
                {
                    response.statusCode = 402;
                    response.statusMessage = $"HTTP {response.statusCode} No packages available";
                }
            }
            catch(Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = $"HTTP {response.statusCode} Server error: " + ex.Message;
            }

        }

        public string extractUsernameFromToken(HttpRequest request)
        {
            var authHeader = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader))
            {
                return null;
            }
            var token = authHeader.Split(" ")[1];
            int index = token.IndexOf("-mtcg");
            if (index == -1)
            {
                return null; 
            }
            string username = token.Substring(0, index);

            return username;

        }
    
    }
}
