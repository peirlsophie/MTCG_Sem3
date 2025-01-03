using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.NewFolder
{
    public class HttpResponse
    {
        public StreamWriter writer;
        public int statusCode;
        public string statusMessage;
        public HttpResponse(StreamWriter writer) 
        {
            this.writer = writer;   
        }

        public void SendResponse() 
        { 

            Console.WriteLine("----------------------------------------");

            // ----- 3. Write the HTTP-Response -----
            var writerAlsoToConsole = new StreamTracer(writer);  
            writer.WriteLine($"HTTP/1.1 {statusCode} {statusMessage}");
            writer.WriteLine("Content-Type: application/json"); 
            writer.WriteLine(); // End of headers

            
            writer.WriteLine("{\"message\": \"" + statusMessage + "\"}");
        }
      
    
    }
}
