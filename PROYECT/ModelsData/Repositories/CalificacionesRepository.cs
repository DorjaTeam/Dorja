using Dapper;
using DorjaData;
using DorjaModelado;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaModelado.Repositories
{
    public class CalificacionesRepository : ICalificacionesRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public CalificacionesRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<IEnumerable<Calificacion>> GetByEstudiante(int estudianteId)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, maestro_id as MaestroId, estudiante_id as EstudianteId,
                        valor as Valor, COALESCE(comentario, '') as Comentario, fecha as Fecha
                        FROM calificaciones
                        WHERE estudiante_id = @EstudianteId
                        ORDER BY fecha DESC";
            return await db.QueryAsync<Calificacion>(sql, new { EstudianteId = estudianteId });
        }

        public async Task<IEnumerable<Calificacion>> GetByMaestro(int maestroId)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, maestro_id as MaestroId, estudiante_id as EstudianteId,
                        valor as Valor, COALESCE(comentario, '') as Comentario, fecha as Fecha
                        FROM calificaciones
                        WHERE maestro_id = @MaestroId
                        ORDER BY fecha DESC";
            return await db.QueryAsync<Calificacion>(sql, new { MaestroId = maestroId });
        }

        public async Task<Calificacion?> GetByMaestroAndEstudiante(int maestroId, int estudianteId)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, maestro_id as MaestroId, estudiante_id as EstudianteId,
                        valor as Valor, COALESCE(comentario, '') as Comentario, fecha as Fecha
                        FROM calificaciones
                        WHERE maestro_id = @MaestroId AND estudiante_id = @EstudianteId
                        ORDER BY fecha DESC
                        LIMIT 1";
            return await db.QueryFirstOrDefaultAsync<Calificacion>(sql,
                new { MaestroId = maestroId, EstudianteId = estudianteId });
        }

        public async Task<bool> Upsert(Calificacion calificacion)
        {
            var db = dbConnection();
            
            // Check if a calificacion already exists for this maestro-estudiante pair
            var existing = await GetByMaestroAndEstudiante(calificacion.MaestroId, calificacion.EstudianteId);
            
            if (existing != null)
            {
                // Update existing
                var updateSql = @"UPDATE calificaciones SET
                                  valor = @Valor,
                                  comentario = @Comentario,
                                  fecha = @Fecha
                                  WHERE maestro_id = @MaestroId AND estudiante_id = @EstudianteId";
                calificacion.Fecha = DateTime.Now;
                var updated = await db.ExecuteAsync(updateSql, new
                {
                    calificacion.Valor,
                    calificacion.Comentario,
                    Fecha = calificacion.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
                    calificacion.MaestroId,
                    calificacion.EstudianteId
                });
                return updated > 0;
            }
            else
            {
                // Insert new
                var insertSql = @"INSERT INTO calificaciones (maestro_id, estudiante_id, valor, comentario, fecha)
                                  VALUES (@MaestroId, @EstudianteId, @Valor, @Comentario, @Fecha)";
                calificacion.Fecha = DateTime.Now;
                var inserted = await db.ExecuteAsync(insertSql, new
                {
                    calificacion.MaestroId,
                    calificacion.EstudianteId,
                    calificacion.Valor,
                    calificacion.Comentario,
                    Fecha = calificacion.Fecha.ToString("yyyy-MM-dd HH:mm:ss")
                });
                return inserted > 0;
            }
        }

        public async Task<bool> Delete(int id)
        {
            var db = dbConnection();
            var sql = "DELETE FROM calificaciones WHERE id = @Id";
            var result = await db.ExecuteAsync(sql, new { Id = id });
            return result > 0;
        }
    }
}
