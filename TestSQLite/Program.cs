using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;

class Program {
    static async System.Threading.Tasks.Task Main() {
        var db = new SqliteConnection("Data Source=C:/Users/apoin/Dorja-1/PROYECT/BACK/dorja.db");
        db.Open();
        var sql = "
            DELETE FROM progreso_problema WHERE user_id = @Id;
            DELETE FROM logros_usuario WHERE user_id = @Id;
            DELETE FROM certificados WHERE user_id = @Id;
            DELETE FROM calificaciones WHERE estudiante_id = @Id OR maestro_id = @Id;
            DELETE FROM mensajes_contacto WHERE estudiante_id = @Id OR maestro_id = @Id;
            DELETE FROM users WHERE id = @Id;
        ";
        try {
            var result = await db.ExecuteAsync(sql, new { Id = 999999 });
            Console.WriteLine("Rows affected: " + result);
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
