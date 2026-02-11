using Dapper;
using DorjaModelado;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaData.Repositories
{
    public class Logros_UsuarioRepository : ILogros_UsuarioRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public Logros_UsuarioRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }
        public  async Task<bool> DeleteLogrosUsuario(Logros_Usuario logros_Usuario)
        {
            var db = dbConnection();
            var sql = @"DELETE FROM logros_usuario WHERE id = @id";

            var result = await db.ExecuteAsync(sql, new { id = logros_Usuario.id });
            return result > 0;
        }

        public Task<IEnumerable<Logros_Usuario>> GetAllLogrosUsuario()
        {
            var db = dbConnection();
            var sql = @"SELECT id, user_id as Id_Usuario, logro_id as Id_Logro, fechaDesbloqueo as Fecha_Obtencion
                        FROM logros_usuario";

            return db.QueryAsync<Logros_Usuario>(sql, new { });
        }

        public async Task<Logros_Usuario> GetDetails(int id)
        {
            var db = dbConnection();
            var sql = @"SELECT id, user_id as Id_Usuario, logro_id as Id_Logro, fechaDesbloqueo as Fecha_Obtencion
                        FROM logros_usuario
                        WHERE id = @id";

            return await db.QueryFirstOrDefaultAsync<Logros_Usuario>(sql, new { id });
        }

        // AQUI TENGO DUDAS DE SI INSERTAR LOS ID DE LOS USUARIOS Y DE LOS LOGROS
        public async Task<bool> InsertLogrosUsuario(Logros_Usuario logros_Usuario)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO logros_usuario (user_id, logro_id, fechaDesbloqueo)
                VALUES (@Id_Usuario, @Id_Logro, @Fecha_Obtencion)";

            var result = await db.ExecuteAsync(sql, logros_Usuario);

            return result > 0;
        }

        // AQUI TENGO DUDAS DE SI ACTUALIZAR LOS ID DE LOS USUARIOS Y DE LOS LOGROS
        public async Task<bool> UpdateLogrosUsuario(Logros_Usuario logros_Usuario)
        {
            var db = dbConnection();
            var sql = @"UPDATE logros_usuario SET
                    user_id = @Id_Usuario,
                    logro_id = @Id_Logro,
                    fechaDesbloqueo = @Fecha_Obtencion
                WHERE id = @id";

            var result = await db.ExecuteAsync(sql, logros_Usuario);
            return result > 0; // true si actualizó
        }

        public async Task<IEnumerable<Logros_Usuario>> GetLogrosByUserId(int userId)
        {
            var db = dbConnection();
            var sql = @"SELECT id, user_id as Id_Usuario, logro_id as Id_Logro, fechaDesbloqueo as Fecha_Obtencion
                        FROM logros_usuario
                        WHERE user_id = @userId";
            return await db.QueryAsync<Logros_Usuario>(sql, new { userId });
        }

        public async Task<bool> UserHasLogro(int userId, int logroId)
        {
            var db = dbConnection();
            var sql = @"SELECT COUNT(*) FROM logros_usuario 
                        WHERE user_id = @userId AND logro_id = @logroId";
            var count = await db.QueryFirstOrDefaultAsync<int>(sql, new { userId, logroId });
            return count > 0;
        }
    }
}
