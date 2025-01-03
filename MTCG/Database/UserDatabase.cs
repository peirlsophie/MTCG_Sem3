using Microsoft.VisualBasic;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCG_Peirl.Models;

namespace MTCG.Database
{
    internal class UserDatabase
    {
        private readonly DatabaseAccess dbAccess;
        public UserDatabase(DatabaseAccess dbAccess)
        {
            this.dbAccess = dbAccess;
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
                string query = "SELECT username FROM users WHERE id = @id;";

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

    }
}
