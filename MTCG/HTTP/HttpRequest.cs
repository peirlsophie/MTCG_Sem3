using MTCG.Backend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.NewFolder
{
    public class HttpRequest
    {
        public StreamReader reader;
        public string Method { get; set; }
        public string Path { get; set; }
        public string HttpVersion { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string Content { get; set; }
       

        public HttpRequest(StreamReader reader)
        {
            this.reader = reader;
        }

        public void processRequest()
        {
            string? line;

            // 1.1 first line in HTTP contains the method, path, and HTTP version
            line = reader.ReadLine();
            if (line != null)
            {
                Console.WriteLine(line);
                // extract method, path, and HTTP version
                var firstLineParts = line.Split(' ');  
                if (firstLineParts.Length == 3)
                {
                    Method = firstLineParts[0];       
                    Path = firstLineParts[1];         
                    HttpVersion = firstLineParts[2];  
                }
                else
                {
                    throw new InvalidOperationException("Invalid HTTP request line.");
                }
            }

            // 1.2 read the HTTP-headers (in HTTP after the first line, until the empty line)
            int content_length = 0; // we need the content_length later, to be able to read the HTTP-content
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (line == "")
                {
                    break;  // empty line indicates the end of the HTTP-headers
                }

                // Parse the header
                var parts = line.Split(':');
                if (parts.Length == 2 && parts[0].Trim() == "Content-Length")
                {
                    content_length = int.Parse(parts[1].Trim());
                }

                // Store headers in dictionary
                if (parts.Length == 2)
                {
                    Headers[parts[0].Trim()] = parts[1].Trim();
                }
            }

            // 1.3 read the body if existing
            if (content_length > 0)
            {
                var data = new StringBuilder(content_length);
                char[] chars = new char[1024];
                int bytesReadTotal = 0;
                while (bytesReadTotal < content_length)
                {
                    var bytesRead = reader.Read(chars, 0, chars.Length);
                    bytesReadTotal += bytesRead;
                    if (bytesRead == 0)
                        break;
                    data.Append(chars, 0, bytesRead);
                }
                Content = data.ToString(); // Set the content to the body of the request
            }
        }


    }
}
