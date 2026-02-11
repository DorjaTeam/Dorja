using Microsoft.Data.Sqlite;
using System;

class CheckDatabase
{
    static void Main()
    {
        string connectionString = "Data Source=dorja.db";
        
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        Console.WriteLine("=== CHECKING DATABASE ===\n");
        
        // Check users
        Console.WriteLine("USERS:");
        var usersCmd = connection.CreateCommand();
        usersCmd.CommandText = "SELECT id, username, email FROM users";
        using (var reader = usersCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                Console.WriteLine($"  ID: {reader.GetInt32(0)}, Username: {reader.GetString(1)}, Email: {reader.GetString(2)}");
            }
        }
        
        Console.WriteLine("\nPROBLEMAS:");
        var problemasCmd = connection.CreateCommand();
        problemasCmd.CommandText = "SELECT id, titulo, tema_id FROM problemas ORDER BY id";
        using (var reader = problemasCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                Console.WriteLine($"  ID: {reader.GetInt32(0)}, Titulo: {reader.GetString(1)}, TemaId: {reader.GetInt32(2)}");
            }
        }
        
        Console.WriteLine("\nPROGRESO_PROBLEMA:");
        var progresoCmd = connection.CreateCommand();
        progresoCmd.CommandText = "SELECT id, user_id, problema_id, completado FROM progreso_problema";
        using (var reader = progresoCmd.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                Console.WriteLine("  (No progress records)");
            }
            while (reader.Read())
            {
                Console.WriteLine($"  ID: {reader.GetInt32(0)}, UserId: {reader.GetInt32(1)}, ProblemaId: {reader.GetInt32(2)}, Completado: {reader.GetInt32(3)}");
            }
        }
    }
}
