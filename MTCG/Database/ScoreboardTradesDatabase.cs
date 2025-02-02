﻿using MTCG.Businesslogic;
using MTCG_Peirl.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace MTCG.Database
{
    internal class ScoreboardTradesDatabase
    {
        private readonly DatabaseAccess dbAccess;

        public ScoreboardTradesDatabase(DatabaseAccess dbAccess) 
        {
            this.dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));

        }

        public Dictionary<int, int> getScoreboardData()
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = @"SELECT user_id, elo
                                 FROM scoreboard
                                 ORDER BY elo DESC";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        try
                        {
                            Dictionary<int, int> scoreboardData = new Dictionary<int, int>();
                            while (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                int elo = reader.GetInt32(1);
                                scoreboardData[userId] = elo;
                            }
                            return scoreboardData;

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error during transaction: {ex.Message}");
                            throw new Exception("An error occurred while updating scoreboard", ex);
                        }

                    }
                }
            }

        }

        public void updateScoreboard(int user_id, int elo)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string updateScoreboard = @"INSERT INTO scoreboard (user_id, elo)
                                                    VALUES (@user_id, @elo);";

                        using (var command = new NpgsqlCommand(updateScoreboard, connection))
                        {
                            command.Parameters.AddWithValue("user_id", user_id);
                            command.Parameters.AddWithValue("elo", elo);
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

        public void enterTradingDeal(Trade trade, int user_id)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertTradingDeal = @"INSERT INTO trades (Id, user_id, card_id, Type, MinimumDamage)
                                                    VALUES (@Id, @user_id, @card_id, @Type, @MinimumDamage);";

                        using (var command = new NpgsqlCommand(insertTradingDeal, connection))
                        {
                            command.Parameters.AddWithValue("Id", trade.Id);
                            command.Parameters.AddWithValue("user_id", user_id);
                            command.Parameters.AddWithValue("card_id", trade.CardToTrade);
                            command.Parameters.AddWithValue("Type", trade.Type);
                            command.Parameters.AddWithValue("MinimumDamage", trade.MinimumDamage);
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

        public List<Trade> getTradingData()
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = @"SELECT Id, user_id, card_id, Type, MinimumDamage
                                 FROM trades";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        try
                        {
                            List<Trade> tradingData = new List<Trade>();
                            while (reader.Read())
                            {
                                tradingData.Add(new Trade(
                                    Id : reader.GetString(0),
                                    CardToTrade : reader.GetString(2),
                                    Type : reader.GetString(3),
                                    MinimumDamage : reader.GetInt32(4) 
                                )); 
                            }
                                                      
                            return tradingData;

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error during transaction: {ex.Message}");
                            throw new Exception("An error occurred while updating scoreboard", ex);
                        }
                    }
                }
            }
        }

        public bool deleteTrade(string tradeId)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string deleteTradingDeal = @"DELETE FROM trades
                                                     WHERE Id = @Id;";

                        using (var command = new NpgsqlCommand(deleteTradingDeal, connection))
                        {
                            command.Parameters.AddWithValue("Id", tradeId);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {

                        transaction.Rollback();
                        Console.WriteLine($"Error during transaction: {ex.Message}");
                        return false;
                        throw new Exception("An error occurred while deleting the Trade Deal:", ex);
                        
                    }
                }
            }
        }

        public (int userId, string cardId) getOfferingUserIdAndCardID(string tradeId)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = @"SELECT user_id, card_id
                                 FROM trades
                                 WHERE id = @id";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", tradeId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        try
                        {
                            if (reader.Read())
                            {
                               
                                int userId = reader.GetInt32(reader.GetOrdinal("user_id"));
                                string cardId = reader.GetString(reader.GetOrdinal("card_id"));

                                return (userId, cardId);
                            }
                            else
                            {
                                throw new Exception("No user found for this card_id");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error during transaction: {ex.Message}");
                            throw new Exception("An error occurred while getting the user ID and card ID", ex);
                        }
                    }
                }
            }

        }
    }
}
