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
    public class CardPackagesDatabase
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
                try
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
                            Console.WriteLine($"Database transaction failed: {ex.Message}");
                            throw new Exception("Database error occurred while saving the package.", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database connection failed: {ex.Message}");
                    throw new Exception("Failed to connect to the database.", ex);
                }
            }
        }


        public int countAvailablePackages()
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM packages WHERE is_purchased = @is_purchased;";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("is_purchased", false);
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public void updatePurchasedPackage(string username)
        {
            using (var connection = dbAccess.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {

                        string selectQuery = @"
                        SELECT id FROM packages 
                        WHERE is_purchased = @is_purchased 
                        LIMIT 1 
                        FOR UPDATE SKIP LOCKED;";

                        int packageId;
                        using (var selectCommand = new NpgsqlCommand(selectQuery, connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("is_purchased", false);
                            var packageIdObj = selectCommand.ExecuteScalar();
                            if (packageIdObj == null)
                            {
                                Console.WriteLine("No available packages found.");
                                transaction.Rollback();
                                return;
                            }
                            packageId = Convert.ToInt32(packageIdObj);
                        }


                        int userId = findUserIdByName(username);


                        string updateQuery = @"
                        UPDATE packages 
                        SET is_purchased = @is_purchased, buyer_id = @buyer_id 
                        WHERE id = @id;";

                        using (var updateCommand = new NpgsqlCommand(updateQuery, connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("is_purchased", true);
                            updateCommand.Parameters.AddWithValue("buyer_id", userId);
                            updateCommand.Parameters.AddWithValue("id", packageId);
                            updateCommand.ExecuteNonQuery();
                        }

                        var packageData = GetCardsFromPackage(packageId, connection);
                        saveCardsToStack(packageData, userId);


                       transaction.Commit();
                        Console.WriteLine($"Package {packageId} successfully purchased by user {username}.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Transaction failed: {ex.Message}");
                        throw;
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


        public bool saveDeckConfig(List<string> cardIds, int userid)
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
                            string addCardsInDeck = "INSERT INTO decks (user_id, card_id) VALUES (@user_id, @card_id);";
                            using (var command = new NpgsqlCommand(addCardsInDeck, connection, transaction))
                            {
                                command.Parameters.AddWithValue("user_id", userid);
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
                        Console.WriteLine("Transaction committed successfully.");

                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error while saving cards to stack: {ex.Message}");
                        return false; 
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
                            string markAsInDeck = "UPDATE cards SET in_deck = @in_deck WHERE id = @id;";
                            using (var command = new NpgsqlCommand(markAsInDeck, connection, transaction))
                            {
                                command.Parameters.AddWithValue("in_deck", false);
                                command.Parameters.AddWithValue("id", cardId);

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

        public async Task saveTradedCardInStack(int userId, string cardId)
        {
            using (var connection = dbAccess.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        string addCardInStack = "INSERT INTO stacks (user_id, card_id) VALUES (@user_id, @card_id);";
                            using (var command = new NpgsqlCommand(addCardInStack, connection, transaction))
                            {
                                command.Parameters.AddWithValue("user_id", userId);
                                command.Parameters.AddWithValue("card_id", cardId);

                                await command.ExecuteNonQueryAsync();
                            }
                        await transaction.CommitAsync();
                    }
                    catch(Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Error while saving card to stack: {ex.Message}");
                    }
                }
            }
        }

        public async Task deleteTradedCardFromStack(int userId, string cardId)
        {
            using (var connection = dbAccess.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        string deleteCardFromStack = "DELETE FROM stacks WHERE user_id = @user_id AND card_id = @card_id;";
                        using (var command = new NpgsqlCommand(deleteCardFromStack, connection, transaction))
                        {
                            command.Parameters.AddWithValue("user_id", userId);
                            command.Parameters.AddWithValue("card_id", cardId);

                            await command.ExecuteNonQueryAsync();
                        }
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Error while deleting card from stack: {ex.Message}");
                    }
                }
            }
        }

    }

}
