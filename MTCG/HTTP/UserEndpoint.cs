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

            
        //method
        //content
        //path
        // wenn post dann methode registeruser
        // wenn get login


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
            // Deserialize the request content to extract the username and password
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
                response.statusCode = 409; // Conflict
                response.statusMessage = "User already exists";
                return;
            }
            else {
                // Add the new user to the in-memory store
                foreach (var user in users)
                {
                    Console.WriteLine($"Username: {user.Key}, Password: {user.Value}");
                }
                this.users.Add(userData.Username,userData.Password); // You might want to hash the password in a real application
                response.statusCode = 201; // Created
            response.statusMessage = $"User created HTTP {response.statusCode}";
            }
        }

        private void LoginUser(HttpRequest request, HttpResponse response)
        {
            // Deserialize the request content to extract the username and password
            var userData = JsonSerializer.Deserialize<User>(request.Content);

            if (userData == null || string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                response.statusCode = 400;
                response.statusMessage = "Invalid input";
                return;
            }

            // Validate the username and password
            if (users.TryGetValue(userData.Username, out string storedPassword) && storedPassword == userData.Password)
            {
                // Generate a token for the user (this is just a simple example)
                string token = $"{userData.Username}-mtcgToken{tokenCounter++}";

                // Here you would normally return the token in the response body as well
                response.statusCode = 200; // OK
                response.statusMessage = "Login successful " + token;
                // Send the token in the response headers or body as needed
            }
            else
            {
                response.statusCode = 401; // Unauthorized
                response.statusMessage = "Login failed";
            }
        }
    }



}
