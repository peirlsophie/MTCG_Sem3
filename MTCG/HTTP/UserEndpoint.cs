using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MTCG.Database;
using MTCG.NewFolder;
using MTCG_Peirl.Models;
using Npgsql;

namespace MTCG.Backend
{
    internal class UserEndpoint
    {
        private static int tokenCounter = 0;
        private readonly UserDatabase userDatabase;

        private readonly DatabaseAccess dbAccess;


        public UserEndpoint(DatabaseAccess dbAccess)
        {
            this.userDatabase = new UserDatabase(dbAccess);
            this.dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));

        }

        public void HandleUserRequest(HttpRequest request, HttpResponse response)
        {

            if (request.Method == "POST" && request.Path == "/users")
            {
                RegisterUser(request, response);
            }
            else if (request.Method == "POST" && request.Path == "/sessions")
            {
                LoginUser(request, response);
            }
            else
            {
                Console.WriteLine($"{request.Method} + {request.Path}");
                response.statusCode = 400;
                response.statusMessage = $"HTTP {response.statusCode} Bad request 2";
            }
        }

        public void RegisterUser(HttpRequest request, HttpResponse response)
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
            if (userDatabase.UserExists(userData.Username))
            {
                response.statusCode = 409;
                response.statusMessage = $"HTTP {response.statusCode} User already exists";
                return;
            }
            else
            {
                string hashpw = HashPassword(userData.Password);
                try
                {
                    userDatabase.insertUserToDatabase(userData.Username, hashpw);
                    response.statusCode = 201;
                    response.statusMessage = $"User created HTTP {response.statusCode}";
                    
                }
                catch (Exception ex)
                {
                    response.statusCode = 500; // Internal server error
                    response.statusMessage = "An error occurred while creating the user: " + ex.Message;
                }
            }
        }
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashBytes); // Storing hashed password as base64 string
            }
        }


        private string LoginUser(HttpRequest request, HttpResponse response)
        {
            var userData = JsonSerializer.Deserialize<User>(request.Content);

            if (userData == null || string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                response.statusCode = 400;
                response.statusMessage = $"HTTP {response.statusCode} Invalid input";
                return "";
            }

            // Retrieve the stored hashed password from the database
            string storedHash = userDatabase.GetStoredPasswordHash(userData.Username);

            if (storedHash == null)
            {
                response.statusCode = 401; // Unauthorized
                response.statusMessage = $"HTTP {response.statusCode} Login failed: User not found";
                return "";
            }

            // Hash the provided password and compare it with the stored hash
            string hashedPassword = HashPassword(userData.Password);

            if (storedHash == hashedPassword)
            {
                // Generate a token for the user (or other login success logic)
                string token = $"{userData.Username}-mtcgToken{tokenCounter++}";
                response.statusCode = 200;
                response.statusMessage = $"HTTP {response.statusCode} Login successful "+ token;
                return token;
            }
            else
            {
                response.statusCode = 401; // Unauthorized
                response.statusMessage = $"HTTP {response.statusCode} Login failed: Incorrect password";
                return "";
            }
        }

    }
}
