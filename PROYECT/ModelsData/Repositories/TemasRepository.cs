using Dapper;
using DorjaModelado;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaData.Repositories
{
    public class TemasRepository : ITemasRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public TemasRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<bool> DeleteTemas(Temas temas)
        {
            var db = dbConnection();
            var sql = @"DELETE FROM temas WHERE id = @IdTemas";

            var result = await db.ExecuteAsync(sql, new { IdTemas = temas.IdTemas });
            return result > 0;
        }

        public Task<IEnumerable<Temas>> GetAllTemas()
        {
            var db = dbConnection();
            var sql = @"SELECT id as IdTemas, nivel_id as IdNivel, titulo as Titulo, descripcion as Descripcion, orden as Orden,
                        locked as Locked, puntos_requeridos as PuntosRequeridos
                        FROM temas";

            return db.QueryAsync<Temas>(sql, new { });
        }

        public async Task<Temas> GetDetails(int id)
        {
            var db = dbConnection();
            var sql = @"SELECT id as IdTemas, nivel_id as IdNivel, titulo as Titulo, descripcion as Descripcion, orden as Orden,
                       locked as Locked, puntos_requeridos as PuntosRequeridos
                FROM temas
                WHERE id = @id";

            return await db.QueryFirstOrDefaultAsync<Temas>(sql, new { id });
        }

        public async Task<bool> InsertTemas(Temas temas)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO temas (nivel_id, titulo, descripcion, locked,
                       orden, puntos_requeridos)
                VALUES (@IdNivel, @Titulo, @Descripcion, @Locked,
                        @Orden, @PuntosRequeridos)";

            var result = await db.ExecuteAsync(sql, temas);

            return result > 0;
        }

        public async Task<bool> UpdateTemas(Temas temas)
        {
            var db = dbConnection();
            var sql = @"UPDATE temas SET
                    nivel_id = @IdNivel,
                    titulo = @Titulo,
                    descripcion = @Descripcion,
                    locked = @Locked,
                    orden = @Orden,
                    puntos_requeridos = @PuntosRequeridos
                WHERE id = @IdTemas";

            var result = await db.ExecuteAsync(sql, temas);
            return result > 0; // true si actualizó
        }
    }
}
