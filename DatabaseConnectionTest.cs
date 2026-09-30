using MySqlConnector;
using System;

namespace ExpenseTrackerApp
{
    public class DatabaseConnectionTest
    {
        public static void TestConnection()
        {
            string connectionString = "Server=localhost;Port=3306;Database=ExpenseTrackerDB;Uid=root;Pwd=#Root@1337#;SslMode=none;";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    Console.WriteLine("Opening database connection...");
                    connection.Open();
                    Console.WriteLine("✅ Database connection successful!");

                    // Test a simple query
                    using (var command = new MySqlCommand("SELECT DATABASE();", connection))
                    {
                        var result = command.ExecuteScalar();
                        Console.WriteLine($"Connected to database: {result}");
                    }

                    // Test creating a simple table
                    using (var command = new MySqlCommand(@"
                        CREATE TABLE IF NOT EXISTS test_table (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            message VARCHAR(100)
                        );", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("✅ Test table created successfully!");
                    }

                    // Clean up
                    using (var command = new MySqlCommand("DROP TABLE IF EXISTS test_table;", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("✅ Test table cleaned up!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database connection failed: {ex.Message}");
                Console.WriteLine($"Full error: {ex}");
            }
        }
    }
}
