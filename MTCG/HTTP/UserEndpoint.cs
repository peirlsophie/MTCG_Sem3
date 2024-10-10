using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MTCG.NewFolder;
using MTCG_Peirl.Models;

namespace MTCG.Backend
{
    internal class UserEndpoint
    {
        public Dictionary<string, string> users = new Dictionary<string, string>();
        private static int tokenCounter = 0;

        public UserEndpoint() 
        {
        }

        public void HandleRequest(HttpRequest request, HttpResponse response)
        {

            
 


            if(request.Method == "POST" && request.Path == "/users")
            {
                RegisterUser(request, response);
            }
            else if(request.Method == "POST" && request.Path == "/sessions")
            {
                LoginUser(request, response); 
            }
            else
            {
                Console.WriteLine($"{request.Method} + {request.Path}");
                response.statusCode = 400;
                response.statusMessage = "Bad request 2";
            }
        }

        private void RegisterUser(HttpRequest request, HttpResponse response)
        {
            // extract username and password from request
            var userData = JsonSerializer.Deserialize<User>(request.Content);
            Console.WriteLine($"{request.Content} + {userData.Username} + {userData.Password}");



            if (userData == null || string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                response.statusCode = 400;
                response.statusMessage = "Invalid input";
                return;
            }

            // Check if the user already exists
            if (users.ContainsKey(userData.Username))
            {
                response.statusCode = 409; 
                response.statusMessage = "User already exists";
                return;
            }
            else {
                // Add the new user to the in-memory store
                foreach (var user in users)
                {
                    Console.WriteLine($"Username: {user.Key}, Password: {user.Value}");
                }
                this.users.Add(userData.Username,userData.Password); 
                response.statusCode = 201; 
            response.statusMessage = $"User created HTTP {response.statusCode}";
            }
        }

        private void LoginUser(HttpRequest request, HttpResponse response)
        {
            // extract username and password
            var userData = JsonSerializer.Deserialize<User>(request.Content);

            if (userData == null || string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                response.statusCode = 400;
                response.statusMessage = "Invalid input";
                return;
            }

            // Validate username and password
            if (users.TryGetValue(userData.Username, out string storedPassword) && storedPassword == userData.Password)
            {
                // Generate a token for the user
                string token = $"{userData.Username}-mtcgToken{tokenCounter++}";

                
                response.statusCode = 200; 
                response.statusMessage = "Login successful " + token;
               
            }
            else
            {
                response.statusCode = 401; 
                response.statusMessage = "Login failed";
            }
        }
    }



}
