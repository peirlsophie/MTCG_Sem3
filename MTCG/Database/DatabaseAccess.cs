using System;
using Npgsql;

public class DatabaseAccess
{
    private string connectionString;

    public DatabaseAccess()
    {
        connectionString = $"Host=localhost;Port=5433;Database=mctg;Username=postgres;Password=Wanda0402";
    }

    public void Connect()
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Console.WriteLine("Connection to the database established successfully.");

                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }

    public NpgsqlConnection GetConnection()
    { 
        return new NpgsqlConnection(connectionString); 
    }

    public void ExecuteScriptToCreateTables(string scriptPath)
    {
        string script = File.ReadAllText(scriptPath);
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open(); using (var command = new NpgsqlCommand(script, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    public void ExecuteScriptToDropAllTables(string scriptPath)
    {
        string script = File.ReadAllText(scriptPath);
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open(); using (var command = new NpgsqlCommand(script, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}
