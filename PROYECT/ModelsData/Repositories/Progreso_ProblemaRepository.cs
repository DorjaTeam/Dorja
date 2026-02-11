using DorjaModelado;
using DorjaData;
using Microsoft.Data.Sqlite;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public class Progreso_ProblemaRepository : IProgreso_ProblemaRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public Progreso_ProblemaRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<IEnumerable<Progreso_Problema>> GetAllProgreso_Problemas()
        {
            var db = dbConnection();
            var sql = "SELECT * FROM progreso_problema";
            return await db.QueryAsync<Progreso_Problema>(sql);
        }

        public async Task<Progreso_Problema> GetDetails(int id)
        {
            var db = dbConnection();
            // Mapeo explícito para asegurar que los campos se mapeen correctamente
            var sql = @"SELECT 
                            id as Id,
                            user_id as UserId,
                            problema_id as ProblemaId,
                            completado as Completado,
                            puntuacion as Puntuacion,
                            intentos as Intentos,
                            ultimo_codigo as UltimoCodigo,
                            fecha_completado as FechaCompletado
                        FROM progreso_problema 
                        WHERE id = @Id";
            return await db.QueryFirstOrDefaultAsync<Progreso_Problema>(sql, new { Id = id });
        }

        public async Task<Progreso_Problema> GetByUserAndProblema(int userId, int problemaId)
        {
            var db = dbConnection();
            // Mapeo explícito para asegurar que los campos se mapeen correctamente
            var sql = @"SELECT 
                            id as Id,
                            user_id as UserId,
                            problema_id as ProblemaId,
                            completado as Completado,
                            puntuacion as Puntuacion,
                            intentos as Intentos,
                            ultimo_codigo as UltimoCodigo,
                            fecha_completado as FechaCompletado
                        FROM progreso_problema 
                        WHERE user_id = @UserId AND problema_id = @ProblemaId";
            return await db.QueryFirstOrDefaultAsync<Progreso_Problema>(sql, new { UserId = userId, ProblemaId = problemaId });
        }

        public async Task<IEnumerable<Progreso_Problema>> GetByUserId(int userId)
        {
            var db = dbConnection();
            // Mapeo explícito para asegurar que los campos se mapeen correctamente
            var sql = @"SELECT 
                            id as Id,
                            user_id as UserId,
                            problema_id as ProblemaId,
                            completado as Completado,
                            puntuacion as Puntuacion,
                            intentos as Intentos,
                            ultimo_codigo as UltimoCodigo,
                            fecha_completado as FechaCompletado
                        FROM progreso_problema 
                        WHERE user_id = @UserId";
            return await db.QueryAsync<Progreso_Problema>(sql, new { UserId = userId });
        }

        public async Task<bool> InsertProgreso_Problemas(Progreso_Problema progreso_problema)
        {
            try
            {
                using var db = dbConnection();
                await db.OpenAsync();
                
                // First verify that the problem exists
                var verifyProblemSql = "SELECT COUNT(*) FROM problemas WHERE id = @ProblemaId";
                var problemExists = await db.ExecuteScalarAsync<int>(verifyProblemSql, new { ProblemaId = progreso_problema.ProblemaId });
                
                if (problemExists == 0)
                {
                    Console.WriteLine($"⚠️ WARNING: Cannot insert progress - Problema {progreso_problema.ProblemaId} does not exist");
                    return false;
                }
                
                // Verify user exists
                var verifyUserSql = "SELECT COUNT(*) FROM users WHERE id = @UserId";
                var userExists = await db.ExecuteScalarAsync<int>(verifyUserSql, new { UserId = progreso_problema.UserId });
                
                if (userExists == 0)
                {
                    Console.WriteLine($"⚠️ WARNING: Cannot insert progress - User {progreso_problema.UserId} does not exist");
                    return false;
                }
                
                // Try with foreign keys enabled first
                await db.ExecuteAsync("PRAGMA foreign_keys = ON");
                
                var sql = @"INSERT INTO progreso_problema 
                            (user_id, problema_id, completado, puntuacion, intentos, ultimo_codigo, fecha_completado) 
                            VALUES 
                            (@UserId, @ProblemaId, @Completado, @Puntuacion, @Intentos, @UltimoCodigo, @FechaCompletado)";
                var result = await db.ExecuteAsync(sql, progreso_problema);
                
                if (result > 0)
                {
                    // Verify the insert was successful by querying back
                    var verifySql = "SELECT COUNT(*) FROM progreso_problema WHERE user_id = @UserId AND problema_id = @ProblemaId";
                    var count = await db.ExecuteScalarAsync<int>(verifySql, new { UserId = progreso_problema.UserId, ProblemaId = progreso_problema.ProblemaId });
                    return count > 0;
                }
                
                return false;
            }
            catch (Exception ex) when (ex.Message.Contains("FOREIGN KEY") || ex.Message.Contains("constraint") || ex.Message.Contains("SQLITE_CONSTRAINT"))
            {
                Console.WriteLine($"⚠️ WARNING: Foreign key constraint violation when inserting progress:");
                Console.WriteLine($"   UserId: {progreso_problema.UserId}, ProblemaId: {progreso_problema.ProblemaId}");
                Console.WriteLine($"   Error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ERROR inserting progress: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateProgreso_Problemas(Progreso_Problema progreso_problema)
        {
            try
            {
                using var db = dbConnection();
                await db.OpenAsync();
                
                // First verify that the progress record exists
                var verifySql = "SELECT COUNT(*) FROM progreso_problema WHERE id = @Id";
                var exists = await db.ExecuteScalarAsync<int>(verifySql, new { Id = progreso_problema.Id });
                
                if (exists == 0)
                {
                    Console.WriteLine($"⚠️ WARNING: Cannot update progress - Progress record {progreso_problema.Id} does not exist");
                    return false;
                }
                
                // Verify that the problem still exists
                var verifyProblemSql = "SELECT COUNT(*) FROM problemas WHERE id = @ProblemaId";
                var problemExists = await db.ExecuteScalarAsync<int>(verifyProblemSql, new { ProblemaId = progreso_problema.ProblemaId });
                
                if (problemExists == 0)
                {
                    Console.WriteLine($"⚠️ WARNING: Cannot update progress - Problema {progreso_problema.ProblemaId} no longer exists");
                    return false;
                }
                
                await db.ExecuteAsync("PRAGMA foreign_keys = ON");
                
                var sql = @"UPDATE progreso_problema SET
                                user_id = @UserId,
                                problema_id = @ProblemaId,
                                completado = @Completado,
                                puntuacion = @Puntuacion,
                                intentos = @Intentos,
                                ultimo_codigo = @UltimoCodigo,
                                fecha_completado = @FechaCompletado
                            WHERE id = @Id";
                var result = await db.ExecuteAsync(sql, progreso_problema);
                
                if (result > 0)
                {
                    // Verify the update was successful
                    var verifyUpdateSql = "SELECT COUNT(*) FROM progreso_problema WHERE id = @Id AND problema_id = @ProblemaId";
                    var count = await db.ExecuteScalarAsync<int>(verifyUpdateSql, new { Id = progreso_problema.Id, ProblemaId = progreso_problema.ProblemaId });
                    return count > 0;
                }
                
                return false;
            }
            catch (Exception ex) when (ex.Message.Contains("FOREIGN KEY") || ex.Message.Contains("constraint") || ex.Message.Contains("SQLITE_CONSTRAINT"))
            {
                Console.WriteLine($"⚠️ WARNING: Foreign key constraint violation when updating progress:");
                Console.WriteLine($"   Id: {progreso_problema.Id}, UserId: {progreso_problema.UserId}, ProblemaId: {progreso_problema.ProblemaId}");
                Console.WriteLine($"   Error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ERROR updating progress: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProgreso_Problemas(int id)
        {
            var db = dbConnection();
            var sql = "DELETE FROM progreso_problema WHERE id = @Id";
            var result = await db.ExecuteAsync(sql, new { Id = id });
            return result > 0;
        }
    }
}
