using DorjaModelado;
using Microsoft.Data.Sqlite;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public class NivelesRepository : INivelesRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        // Constructor correcto
        public NivelesRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<IEnumerable<Niveles>> GetAllNiveles()
        {
            var db = dbConnection();
            var sql = @"SELECT id as IdNiveles, nombre as NombreNivel, descripcion as DescripcionNivel, 
                       dificultad as dificultad, orden as orden, puntosRequeridos as puntosRequeridos 
                       FROM niveles";
            return await db.QueryAsync<Niveles>(sql);
        }

        public async Task<Niveles> GetDetails(int id)
        {
            var db = dbConnection();
            var sql = @"SELECT id as IdNiveles, nombre as NombreNivel, descripcion as DescripcionNivel, 
                       dificultad as dificultad, orden as orden, puntosRequeridos as puntosRequeridos 
                       FROM niveles WHERE id = @Id";
            return await db.QueryFirstOrDefaultAsync<Niveles>(sql, new { Id = id });
        }

        public async Task<bool> InsertNiveles(Niveles niveles)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO niveles (nombre, descripcion, dificultad, orden, puntosRequeridos) 
                        VALUES (@NombreNivel, @DescripcionNivel, @dificultad, @orden, @puntosRequeridos)";
            var result = await db.ExecuteAsync(sql, niveles);
            return result > 0;
        }

        public async Task<bool> UpdateNiveles(Niveles niveles)
        {
            var db = dbConnection();
            var sql = @"UPDATE niveles SET 
                            nombre = @NombreNivel,
                            descripcion = @DescripcionNivel,
                            dificultad = @dificultad,
                            orden = @orden,
                            puntosRequeridos = @puntosRequeridos
                        WHERE id = @IdNiveles";
            var result = await db.ExecuteAsync(sql, niveles);
            return result > 0;
        }

        public async Task<bool> DeleteNiveles(Niveles niveles)
        {
            var db = dbConnection();
            var sql = "DELETE FROM niveles WHERE id = @IdNiveles";
            var result = await db.ExecuteAsync(sql, niveles);
            return result > 0;
        }
    }
}
