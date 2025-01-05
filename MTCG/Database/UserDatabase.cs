using Microsoft.VisualBasic;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCG_Peirl.Models;
using System.Numerics;

namespace MTCG.Database
{
    internal class UserDatabase
    {
        private readonly DatabaseAccess dbAccess;
        public UserDatabase(DatabaseAccess dbAccess)
        {
            this.dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));

        }

        public bool UserExists(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM users WHERE username = @username";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    int userCount = Convert.ToInt32(command.ExecuteScalar());
                    return userCount > 0;
                }
            }
        }

        public void decreaseCoins(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string updateCoins = @"UPDATE users SET coins = coins - @amount WHERE username = @username;";

                        using (var command = new NpgsqlCommand(updateCoins, connection))
                        {
                            command.Parameters.AddWithValue("amount", 5);
                            command.Parameters.AddWithValue("username", username);

                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during transaction: {ex.Message}");
                        throw new Exception("An error occurred while performing the transaction", ex);
                    }

                }

            }
        }

        public int checkAvailableCoins(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = "SELECT coins FROM users WHERE username = @username";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    var result = command.ExecuteScalar();
                    
                    if (result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        public string GetStoredPasswordHash(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = "SELECT password FROM users WHERE username = @username";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    var result = command.ExecuteScalar();
                    return result?.ToString(); // Return null if no user found
                }
            }
        }

        public void insertUserToDatabase(string username, string hashedPassword)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertUserSql = @"
                            INSERT INTO users (username, password, coins, elo, games_played, wins, losses)
                            VALUES (@username, @password, @coins, @elo, @games_played, @wins, @losses);";
                        using (var command = new NpgsqlCommand(insertUserSql, connection))
                        {
                            command.Parameters.AddWithValue("username", username);
                            command.Parameters.AddWithValue("password", hashedPassword); // Store the hashed password
                            command.Parameters.AddWithValue("coins", 20);
                            command.Parameters.AddWithValue("elo", 100);
                            command.Parameters.AddWithValue("games_played", 0);
                            command.Parameters.AddWithValue("wins", 0);
                            command.Parameters.AddWithValue("losses", 0);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during user insert: {ex.Message}");
                        throw new Exception("An error occurred while saving the user to the database.", ex);
                    }
                }
            }
        }

        public void enterPlayersInBattle(int userId)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Lock the row for the first available battle to avoid simultaneous matches
                        string checkForOpponentQuery = @"
                        SELECT id, player1 
                        FROM battles 
                        WHERE player2 IS NULL 
                        LIMIT 1 
                        FOR UPDATE SKIP LOCKED;";

                        int battleId = -1;
                        int opponent = -1;

                        using (var command = new NpgsqlCommand(checkForOpponentQuery, connection, transaction))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    battleId = reader.GetInt32(0);
                                    opponent = reader.GetInt32(1);
                                }
                            }
                        }

                        // If an opponent is available, update the battle record
                        if (battleId != -1)
                        {
                            string startBattleQuery = @"
                        UPDATE battles 
                        SET player2 = @player2, status = 'in_progress' 
                        WHERE id = @id;";

                            using (var updateCommand = new NpgsqlCommand(startBattleQuery, connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("player2", userId);
                                updateCommand.Parameters.AddWithValue("id", battleId);
                                updateCommand.ExecuteNonQuery();
                            }
                            //Console.WriteLine($"Battle started between {opponent} and {userId}!");
                        }
                        else
                        {
                            // If no opponent available, create a new battle with the player as player1
                            string insertBattleQuery = "INSERT INTO battles (player1, status) VALUES (@player1, 'waiting');";
                            using (var insertCommand = new NpgsqlCommand(insertBattleQuery, connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("player1", userId);
                                insertCommand.ExecuteNonQuery();
                            }
                            Console.WriteLine($"{userId} is waiting for an opponent.");
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during battle initiation: {ex.Message}");
                    }
                }
            }
        }

        public void markBattleAsFinished(int id1, int id2)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string updateCoins = @"UPDATE battles
                                               SET status = @status WHERE player1 = @player1 AND player2 = @player2;";

                        using (var command = new NpgsqlCommand(updateCoins, connection))
                        {
                            command.Parameters.AddWithValue("status", "completed");
                            command.Parameters.AddWithValue("player1", id1);
                            command.Parameters.AddWithValue("player2", id2);

                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during transaction: {ex.Message}");
                        throw new Exception("An error occurred while performing the transaction", ex);
                    }
                }
            }
        }



        public List<string> showPlayersInBattle()
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = "SELECT player1, player2 FROM battles WHERE status = @status;";
                var players = new List<string>();

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("status", "in_progress");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())  
                        {
                            // Get player 1
                            int player1Id = reader.GetInt32(0); 
                            string player1Name = getUsernameById(player1Id);
                            players.Add(player1Name);

                            // Get player 2 (if exists)
                            if (!reader.IsDBNull(1)) 
                            {
                                int player2Id = reader.GetInt32(1); 
                                string player2Name = getUsernameById(player2Id);
                                players.Add(player2Name);
                            }
                        }
                    }
                }

                return players;
            }
        }

        public User getUserObjectById(int id)
        {
            string query = @"
            SELECT username, password, coins, highscore, elo, games_played, wins, losses
            FROM users 
            WHERE id = @id;";

            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User(
                                username: reader.GetString(0),
                                password: reader.GetString(1),
                                coins: reader.GetInt32(2),
                                highscore: reader.GetInt32(3),
                                elo: reader.GetInt32(4),
                                games_played: reader.GetInt32(5),
                                wins: reader.GetInt32(6),
                                losses: reader.GetInt32(7)
                            );

                        }
                        return null;
                    }
                }
            }

        }    



        public string getUsernameById(int id)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = @"SELECT username FROM users WHERE id = @id;";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("id", id);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetString(0); 
                        }
                        else
                        {
                            return null;  
                        }
                    }
                }
            }
        }

        public void changeUserStats(User player)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string updateStats = @"UPDATE users 
                                             SET elo = @elo, games_played = @games_played, wins = @wins, losses = @losses 
                                             WHERE username = @username;";

                        using (var command = new NpgsqlCommand(updateStats, connection))
                        {
                            command.Parameters.AddWithValue("elo", player.ELO);
                            command.Parameters.AddWithValue("games_played", player.games_played);
                            command.Parameters.AddWithValue("wins", player.Wins);
                            command.Parameters.AddWithValue("losses", player.Losses);
                            command.Parameters.AddWithValue("username", player.Username);

                            command.ExecuteReader();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during transaction: {ex.Message}");
                        throw new Exception("An error occurred while changing the userstats", ex);
                    }

                }

            }
        }

        public void changeUserBioAndImage(string username,string name, string bio, string image)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    Console.WriteLine($"before enter in db username is:{username}");
                    Console.WriteLine($"before enter in db bio is:{bio}");
                    Console.WriteLine($"before enter in db image is:{image}");
                    try
                    {
                        string updateBioImage = @"UPDATE users 
                                             SET name = @name, bio = @bio, image = @image
                                             WHERE username = @username;";

                        using (var command = new NpgsqlCommand(updateBioImage, connection))
                        {
                            command.Parameters.AddWithValue("name", name);
                            command.Parameters.AddWithValue("bio", bio);
                            command.Parameters.AddWithValue("image", image);
                            command.Parameters.AddWithValue("username", username);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during transaction: {ex.Message}");
                        throw new Exception("An error occurred while changing the userstats", ex);
                    }

                }
            }
        }

        public List<string> getUserData(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = @"SELECT name, bio, image
                                 FROM users 
                                 WHERE username = @username;";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            List<string> userData = new List<string>
                            {
                                reader.IsDBNull(0) ? "N/A" : reader.GetString(0),
                                reader.IsDBNull(1) ? "N/A" : reader.GetString(1),
                                reader.IsDBNull(2) ? "N/A" : reader.GetString(2)
                            };

                            return userData;
                        }
                        else
                        {
                            return new List<string>();
                        }
                    }
                }
            }

        }
        public List<int> getUserStats(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = @"SELECT elo, games_played, wins, losses
                                 FROM users 
                                 WHERE username = @username;";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            List<int> userStats = new List<int>
                            {
                                reader.GetInt32(0),
                                reader.GetInt32(1),
                                reader.GetInt32(2),
                                reader.GetInt32(3)
                            };

                            return userStats;
                        }
                        else
                        {
                            return new List<int>();
                        }
                    }
                }
            }

        }

    }
}
