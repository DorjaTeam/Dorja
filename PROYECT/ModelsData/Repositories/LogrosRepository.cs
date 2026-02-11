using Dapper;
using DorjaModelado;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public class LogrosRepository : ILogrosRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public LogrosRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<IEnumerable<Logros>> GetAllLogros()
        {
            var db = dbConnection();
            var sql = "SELECT * FROM logros";
            return await db.QueryAsync<Logros>(sql);
        }

        public async Task<Logros> GetDetails(int id)
        {
            var db = dbConnection();
            var sql = "SELECT * FROM logros WHERE id = @Id";
            return await db.QueryFirstOrDefaultAsync<Logros>(sql, new { Id = id });
        }

        public async Task<bool> InsertLogros(Logros logros)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO logros (nombre, descripcion, iconoPhoto) 
                        VALUES (@Nombre, @Descripcion, @iconoPhoto)";
            var result = await db.ExecuteAsync(sql, logros);
            return result > 0;
        }

        public async Task<bool> UpdateLogros(Logros logros)
        {
            var db = dbConnection();
            var sql = @"UPDATE logros SET 
                            nombre = @Nombre,
                            descripcion = @Descripcion,
                            iconoPhoto = @iconoPhoto
                        WHERE id = @Id";
            var result = await db.ExecuteAsync(sql, logros);
            return result > 0;
        }

        public async Task<bool> DeleteLogros(Logros logros)
        {
            var db = dbConnection();
            var sql = "DELETE FROM logros WHERE id = @Id";
            var result = await db.ExecuteAsync(sql, logros);
            return result > 0;
        }

        public async Task<Logros> GetLogroByNombre(string nombre)
        {
            var db = dbConnection();
            var sql = "SELECT * FROM logros WHERE nombre = @Nombre";
            return await db.QueryFirstOrDefaultAsync<Logros>(sql, new { Nombre = nombre });
        }
    }
}
