using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MTCG.NewFolder;
using System.Security.Cryptography.X509Certificates;
using MTCG.HTTP;
using MTCG.Businesslogic;
using MTCG.Database;

namespace MTCG.Backend
{
    public class HttpServer
    {
        private readonly TcpListener httpServer;

        private readonly DatabaseAccess dbAccess;

        private readonly UserEndpoint userEndpoint; 
        private readonly PackagesEndpoint packagesEndpoint;
        private readonly CardsDeckEndpoint cardsDeckEndpoint;
        private readonly BattlesEndpoint battlesEndpoint;
        private readonly UserDatabase userDatabase;
        private readonly CardPackagesDatabase cardPackagesDb;
        private readonly StatsScoreboardEndpoint statsScoreboardDb;
        private readonly TradingsEndpoint tradingsEndpoint;

        public int statusCode;
        public string statusMessage { get; set; }
        public string Path { get; set; }

        public HttpServer(IPAddress address, int port)
        {
            this.httpServer = new TcpListener(address, port);
            this.statusMessage = string.Empty; // Initialisierung
            this.Path = string.Empty; // Initialisierung

            dbAccess = new DatabaseAccess();
            userEndpoint = new UserEndpoint(dbAccess);
            packagesEndpoint = new PackagesEndpoint(dbAccess);
            cardsDeckEndpoint = new CardsDeckEndpoint(dbAccess);
            battlesEndpoint = new BattlesEndpoint(dbAccess);
            userDatabase = new UserDatabase(dbAccess);
            cardPackagesDb = new CardPackagesDatabase(dbAccess);
            //Battle battle = new Battle(dbAccess);
            statsScoreboardDb = new StatsScoreboardEndpoint(dbAccess);
            tradingsEndpoint = new TradingsEndpoint(dbAccess);

            try
            {
                dbAccess.ExecuteScriptToDropAllTables("C:/Users/Sophie/Documents/FH/Semester3_WS24_25/SoftwareEngLabor/MTCG/MTCG/drop_tables.txt");
                dbAccess.ExecuteScriptToCreateTables("C:/Users/Sophie/Documents/FH/Semester3_WS24_25/SoftwareEngLabor/MTCG/MTCG/sql_script.txt");
                Console.WriteLine("Database tables successfully created");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize database tables: {ex.Message}");
            }

        }

        public void Run()
        {
            httpServer.Start();

            while (true)
            {
                var clientSocket = httpServer.AcceptTcpClient();
                HandleEndpoints(clientSocket);
                
            }
        }

        public async Task HandleEndpoints(TcpClient clientSocket)
        {
            using var reader = new StreamReader(clientSocket.GetStream());
            var request = new HttpRequest(reader);
            request.processRequest();
            using var writer = new StreamWriter(clientSocket.GetStream()) { AutoFlush = true };
            var response = new HttpResponse(writer);

            string[] pathSegments = request.Path.Trim('/').Split('/');

            //checking for endpoint path
            if (request.Path == "/users" || request.Path == "/sessions" || (pathSegments[0] == "users" && pathSegments.Length == 2))
            {
               await userEndpoint.HandleUserRequest(request, response);
            }
            else if (request.Path == "/packages" || request.Path == "/transactions/packages")
            {
               await packagesEndpoint.handlePackageRequests(request, response);
            }
            else if (request.Path == "/cards" || request.Path == "/deck")
            {
                await cardsDeckEndpoint.handleCardDeckRequests(request, response);
            }
            else if(request.Path == "/battles")
            {
                await battlesEndpoint.handleBattlesRequests(request, response);
            }
            else if (request.Path == "/stats" || request.Path == "/scoreboard")
            {
                await statsScoreboardDb.handleStatsScoreboardRequests(request, response);
            }
            else if(request.Path == "/tradings")
            {
                await tradingsEndpoint.handleTradingsRequests(request, response);
            }
            else
            {
                Console.WriteLine($"{request.Method} + {request.Path}");
                response.statusCode = 404; // Not Found
                response.statusMessage = "Endpoint not found";
                response.SendResponse(); 
                return;
            }

            response.SendResponse();

        }
    }
}
