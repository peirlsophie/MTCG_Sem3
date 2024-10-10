using System;
using System.Net;

namespace MTCG.Backend
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); 
            int port = 10001; 

            // Create server
            HttpServer server = new HttpServer(ipAddress, port);
            server.Run(); 
        }
    }
}
