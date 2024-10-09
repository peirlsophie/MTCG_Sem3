using System;
using System.Net;

namespace MTCG.Backend
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define the IP address and port for the server
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // Localhost
            int port = 10001; // Port number

            // Create and run the HTTP server
            HttpServer server = new HttpServer(ipAddress, port);
            server.Run(); // This will block until the application is closed
        }
    }
}
