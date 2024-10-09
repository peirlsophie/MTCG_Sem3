using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.NewFolder
{
    internal class HttpResponse
    {
        public StreamWriter writer;
        public int statusCode;
        public string statusMessage;
        public HttpResponse(StreamWriter writer) 
        {
            this.writer = writer;   
        }

        public void SendResponse() 
        { // statt hardgecoded http/1.0 usw 

            Console.WriteLine("----------------------------------------");

            // ----- 3. Write the HTTP-Response -----
            var writerAlsoToConsole = new StreamTracer(writer);  // we use ai simple helper-class StreamTracer to write the HTTP-Response to the client and to the console
            writer.WriteLine($"HTTP/1.1 {statusCode} {statusMessage}");
            writer.WriteLine("Content-Type: application/json"); // Set appropriate content type
            writer.WriteLine(); // End of headers

            // You can add a body here if needed
            writer.WriteLine("{\"message\": \"" + statusMessage + "\"}");
        }
      
    
    }
}
