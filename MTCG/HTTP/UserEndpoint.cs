using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MTCG.Database;
using MTCG.HTTP;
using MTCG.NewFolder;
using MTCG_Peirl.Models;
using Npgsql;

namespace MTCG.Backend
{
    public class UserEndpoint
    {
        private static int tokenCounter = 0;
        private readonly UserDatabase userDatabase;
        private readonly PackagesEndpoint packagesEndpoint;
        
        private readonly DatabaseAccess dbAccess;


        public UserEndpoint(DatabaseAccess dbAccess)
        {
            this.userDatabase = new UserDatabase(dbAccess);
            this.packagesEndpoint = new PackagesEndpoint(dbAccess);
            this.dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));

        }

        public async Task HandleUserRequest(HttpRequest request, HttpResponse response)
        {
            string[] pathSegments = request.Path.Trim('/').Split('/');

            if (request.Method == "POST" && request.Path == "/users")
            {
                RegisterUser(request, response);
            }
            else if (request.Method == "POST" && request.Path == "/sessions")
            {
                LoginUser(request, response);
            }
            else if (request.Method == "PUT" && pathSegments.Length == 2 && pathSegments[0] == "users" )
            {
                EditUser(request, response, pathSegments[1]);
            }
            else if (request.Method == "GET" && pathSegments.Length == 2 && pathSegments[0] == "users")
            {
                ShowUserData(request, response, pathSegments[1]);
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
        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashBytes); // Storing hashed password as base64 string
            }
        }


        public string LoginUser(HttpRequest request, HttpResponse response)
        {
            var userData = JsonSerializer.Deserialize<User>(request.Content);

            if (userData == null || string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                response.statusCode = 400;
                response.statusMessage = $"HTTP {response.statusCode} Invalid input";
                return "";
            }

            string storedHash = userDatabase.GetStoredPasswordHash(userData.Username);

            if (storedHash == null)
            {
                response.statusCode = 401; 
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

        public void EditUser(HttpRequest request, HttpResponse response, string pathname)
        {
            try
            {
                var userData = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Content);

                if (userData == null ||
                    !userData.ContainsKey("Name") ||
                    !userData.ContainsKey("Bio") ||
                    !userData.ContainsKey("Image"))
                {
                    response.statusCode = 400;
                    response.statusMessage = "Invalid input - Missing required fields.";
                    return;
                }

                string username = userData["Name"];
                string bio = userData["Bio"];
                string image = userData["Image"];

                //check name vom path gegen token!! noch erledigen
                string tokenUsername = packagesEndpoint.extractUsernameFromToken(request);

                // check token authorization
                var token = request.Headers["Authorization"];
                if (string.IsNullOrEmpty(token) || !token.Contains("Bearer") || !userDatabase.UserExists(pathname))
                {
                    response.statusCode = 401;
                    response.statusMessage = "Unauthorized: Missing or invalid token";
                    return;
                }

                if (tokenUsername != pathname)
                {
                    response.statusCode = 403;
                    response.statusMessage = "Unauthorized: Cannot edit another users profile";
                    return;
                }

                userDatabase.changeUserBioAndImage(tokenUsername, username, bio, image);

                response.statusCode = 200;
                response.statusMessage = $"User profile updated successfully for {username}";
            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = "An error occurred while updating the user: " + ex.Message;
            }
        }

        public void ShowUserData(HttpRequest request, HttpResponse response, string pathname)
        {
            try
            {
                string username = packagesEndpoint.extractUsernameFromToken(request);
                Console.WriteLine($"username is:{username}");
                if (username == null || !userDatabase.UserExists(pathname))
                {
                    response.statusCode = 401;
                    response.statusMessage = $"HTTP {response.statusCode} Unauthorized";
                    return;
                }
                var userdata = userDatabase.getUserData(username);
                                    
                response.statusCode = 200;
                response.statusMessage = $"HTTP {response.statusCode} Name: {userdata[0]} , Bio: {userdata[1]} , Image: {userdata[2]} ";
            }
            catch (Exception ex)
            {
                response.statusCode = 500;
                response.statusMessage = "An error occurred while getting the userdata " + ex.Message;
            }
        }
    }
}
