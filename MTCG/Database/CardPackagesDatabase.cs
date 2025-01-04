using MTCG_Peirl.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Database
{
    internal class CardPackagesDatabase
    {
        private readonly DatabaseAccess dbAccess;

        public CardPackagesDatabase(DatabaseAccess dbAccess)
        {
            this.dbAccess = dbAccess;
        }

        public void savePackageToDatabase(List<Card> packageData)
        {
            if (packageData == null || packageData.Count != 5)
            {
                throw new ArgumentException("A package must contain exactly 5 cards.");
            }

            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Save each card to the database
                        foreach (var card in packageData)
                        {
                            string insertCardSql = @"
                            INSERT INTO cards (id, name, damage, element_type, card_type)
                            VALUES (@id, @name, @damage, @element_type, @card_type);";

                            using (var command = new NpgsqlCommand(insertCardSql, connection))
                            {
                                command.Parameters.AddWithValue("id", card.Id);
                                command.Parameters.AddWithValue("name", card.Name);
                                command.Parameters.AddWithValue("damage", card.Damage);
                                command.Parameters.AddWithValue("element_type", card.ElementType.ToString());
                                command.Parameters.AddWithValue("card_type", card.CardType);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Insert the package with the provided card IDs
                        string insertPackageSql = @"
                        INSERT INTO packages (card1_id, card2_id, card3_id, card4_id, card5_id, is_purchased)
                        VALUES (@card1_id, @card2_id, @card3_id, @card4_id, @card5_id, false);";

                        using (var command = new NpgsqlCommand(insertPackageSql, connection))
                        {
                            command.Parameters.AddWithValue("card1_id", packageData[0].Id);
                            command.Parameters.AddWithValue("card2_id", packageData[1].Id);
                            command.Parameters.AddWithValue("card3_id", packageData[2].Id);
                            command.Parameters.AddWithValue("card4_id", packageData[3].Id);
                            command.Parameters.AddWithValue("card5_id", packageData[4].Id);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during transaction: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public int countAvailablePackages()
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM packages WHERE is_purchased = false;";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public void updatePurchasedPackage(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();

                // Find id of an available package
                string selectQuery = "SELECT id FROM packages WHERE is_purchased = false LIMIT 1;";

                using (var selectCommand = new NpgsqlCommand(selectQuery, connection))
                {
                    var packageIdObj = selectCommand.ExecuteScalar();
                    if (packageIdObj != null)
                    {
                        int packageId = Convert.ToInt32(packageIdObj);

                        // Mark the package as purchased
                        string updateQuery = "UPDATE packages SET is_purchased = true WHERE id = @id;";
                        using (var updateCommand = new NpgsqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("id", packageId);
                            updateCommand.ExecuteNonQuery();
                        }

                        // Get user ID based on username
                        int userId = findUserIdByName(username);

                        // Assign the buyer to the package
                        string assignBuyerQuery = "UPDATE packages SET buyer_id = @buyer_id WHERE id = @id;";
                        using (var assignBuyerCommand = new NpgsqlCommand(assignBuyerQuery, connection))
                        {
                            assignBuyerCommand.Parameters.AddWithValue("buyer_id", userId);
                            assignBuyerCommand.Parameters.AddWithValue("id", packageId);
                            assignBuyerCommand.ExecuteNonQuery();
                        }

                        // Retrieve package cards and save them to the stack
                        var packageData = GetCardsFromPackage(packageId, connection);
                        saveCardsToStack(packageData, userId);

                        Console.WriteLine($"Package {packageId} purchased by user {username}.");
                    }
                    else
                    {
                        Console.WriteLine("No available packages found.");
                    }
                }
            }
        }

        private List<Card> GetCardsFromPackage(int packageId, NpgsqlConnection connection)
        {
            string query = @"
            SELECT c.id, c.name, c.damage, c.element_type, c.card_type
            FROM packages p
            JOIN cards c ON c.id IN (p.card1_id, p.card2_id, p.card3_id, p.card4_id, p.card5_id)
            WHERE p.id = @packageId;";

            var cards = new List<Card>();

            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("packageId", packageId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cards.Add(new Card(
                            id: reader.GetString(0),
                            name: reader.GetString(1),
                            damage: reader.GetDouble(2),
                            elementType: Enum.Parse<ElementType>(reader.GetString(3)),
                            cardType: reader.GetString(4)
                        ));
                    }
                }
            }

            return cards;
        }

        public void saveCardsToStack(List<Card> packageData, int userId)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var card in packageData)
                    {
                        string query = "INSERT INTO stacks (user_id, card_id) VALUES (@user_id, @card_id);";
                        using (var command = new NpgsqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("user_id", userId);
                            command.Parameters.AddWithValue("card_id", card.Id);
                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        public int findUserIdByName(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                if (dbAccess == null)
                {
                    throw new InvalidOperationException("DatabaseAccess has not been initialized properly.");
                }

                connection.Open();
                string query = "SELECT id FROM users WHERE username = @username;";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public List<string> findOwnedCardIdsInStacks(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                int userId = findUserIdByName(username);
                string query = "SELECT card_id FROM stacks WHERE user_id = @user_id;";
                var cardIds = new List<string>();

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("user_id", userId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cardIds.Add(reader.GetString(0));
                        }
                    }
                }

                return cardIds;
            }
        }




        public List<string> findOwnedCardIdsInDecks(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                int userId = findUserIdByName(username);
                string query = "SELECT card_id FROM decks WHERE user_id = @user_id;";
                var cardIds = new List<string>();

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("user_id", userId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cardIds.Add(reader.GetString(0));
                        }
                    }
                }

                return cardIds;
            }

        }


        public List<string> getCardNames(List<string> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
            {
                return new List<string>(); // Return an empty list if no card IDs are provided
            }

            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();

                // Use IN clause to fetch card names for the given card IDs
                string getCardNamesQuery = @"
            SELECT name 
            FROM cards 
            WHERE id = ANY(@cardIds);";

                var cardNames = new List<string>();

                using (var command = new NpgsqlCommand(getCardNamesQuery, connection))
                {
                    command.Parameters.AddWithValue("cardIds", cardIds.ToArray());
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cardNames.Add(reader.GetString(0)); // Read card names
                        }
                    }
                }

                return cardNames;
            }
        }


        public bool saveDeckConfig(List<string> cardIds, string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                int userId = findUserIdByName(username);
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var cardId in cardIds)
                        {
                            string addCardsInDeck = "INSERT INTO decks (user_id, card_id) VALUES (@user_id, @card_id);";
                            using (var command = new NpgsqlCommand(addCardsInDeck, connection, transaction))
                            {
                                command.Parameters.AddWithValue("user_id", userId);
                                command.Parameters.AddWithValue("card_id", cardId);

                                Console.WriteLine("Executing SQL: " + addCardsInDeck);
                                foreach (NpgsqlParameter param in command.Parameters)
                                {
                                    Console.WriteLine($"{param.ParameterName}: {param.Value}");
                                }

                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        markCardAsInDeck(cardIds);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error while saving cards to stack: {ex.Message}");
                        throw;

                    }

                }
            }
        }

        public void markCardAsInDeck(List<string> cardIds)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var cardId in cardIds)
                        {
                            string markAsInDeck = "UPDATE cards SET in_deck = @in_deck WHERE card_id = @card_id;";
                            using (var command = new NpgsqlCommand(markAsInDeck, connection, transaction))
                            {
                                command.Parameters.AddWithValue("in_deck", false);
                                command.Parameters.AddWithValue("card_id", cardId);

                                Console.WriteLine("Executing SQL: " + markAsInDeck);
                                foreach (NpgsqlParameter param in command.Parameters)
                                {
                                    Console.WriteLine($"{param.ParameterName}: {param.Value}");
                                }

                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error while marking card as in_deck: {ex.Message}");
                        throw;

                    }

                }
            }
        }

        public int checkDeckSize(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                int userId = findUserIdByName(username);
                string query = "SELECT COUNT(*) FROM decks WHERE user_id = @user_id;";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("user_id", userId);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }

        }


        public Card getCardFromDeckForBattle(string cardId)
        {


            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT id, name, damage, element_type, card_type
                    FROM cards 
                    WHERE id = @id;";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("id", cardId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Card(
                                id: reader.GetString(0),
                                name: reader.GetString(1),
                                damage: reader.GetDouble(2),
                                elementType: Enum.Parse<ElementType>(reader.GetString(3)),
                                cardType: reader.GetString(4)
                            );
                        }
                        return null;
                    }
                }
            }

        }
    }

}
