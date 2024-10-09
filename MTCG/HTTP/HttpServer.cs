using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MTCG.NewFolder;
using System.Security.Cryptography.X509Certificates;

namespace MTCG.Backend
{
    public class HttpServer
    {
        private readonly TcpListener httpServer;
        public int statusCode;
        public string statusMessage { get; set; }
        public string Path { get; set; }
        UserEndpoint userEndpoint = new UserEndpoint();

        public HttpServer(IPAddress address, int port)
        {
            this.httpServer = new TcpListener(address, port);
            this.statusMessage = string.Empty; // Initialisierung
            this.Path = string.Empty; // Initialisierung
            
    }

        public void Run()
        {
            httpServer.Start();

            while (true)
            {
                var clientSocket = httpServer.AcceptTcpClient();
                HandleUser(clientSocket);
            }
        }

        public void HandleUser(TcpClient clientSocket)
        {
            using var reader = new StreamReader(clientSocket.GetStream());
            var request = new HttpRequest(reader);
            request.processRequest();
            using var writer = new StreamWriter(clientSocket.GetStream()) { AutoFlush = true };
            var response = new HttpResponse(writer);
            //if user 
            if (request.Path == "/users" || request.Path == "/sessions")
            {

                userEndpoint.HandleRequest(request, response);
            }
            else
            {
                Console.WriteLine($"{request.Method} + {request.Path}");
                response.statusCode = 404; // Not Found
                response.statusMessage = "Endpoint not found";
                response.SendResponse(); // Send response for not found
                return;
            }

            response.SendResponse();
        }
    }
}
