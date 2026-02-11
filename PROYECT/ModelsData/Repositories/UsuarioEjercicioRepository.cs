using Dapper;
using DorjaModelado;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public class UsuarioEjercicioRepository : IUsuarioEjercicioRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public UsuarioEjercicioRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<IEnumerable<UsuarioEjercicio>> GetByUserId(int userId)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, user_id as UserId, problema_id as ProblemaId, 
                       syllabus_section_id as SyllabusSectionId, fecha_asignado as FechaAsignado, 
                       fecha_completado as FechaCompletado, completado as Completado, 
                       es_unico as EsUnico, parametros_unicos as ParametrosUnicos
                       FROM usuario_ejercicio WHERE user_id = @UserId";
            return await db.QueryAsync<UsuarioEjercicio>(sql, new { UserId = userId });
        }

        public async Task<UsuarioEjercicio> GetByUserAndProblema(int userId, int problemaId, string parametrosUnicos = "")
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, user_id as UserId, problema_id as ProblemaId, 
                       syllabus_section_id as SyllabusSectionId, fecha_asignado as FechaAsignado, 
                       fecha_completado as FechaCompletado, completado as Completado, 
                       es_unico as EsUnico, parametros_unicos as ParametrosUnicos
                       FROM usuario_ejercicio 
                       WHERE user_id = @UserId AND problema_id = @ProblemaId AND parametros_unicos = @ParametrosUnicos";
            return await db.QueryFirstOrDefaultAsync<UsuarioEjercicio>(sql, new { 
                UserId = userId, 
                ProblemaId = problemaId, 
                ParametrosUnicos = parametrosUnicos ?? "" 
            });
        }

        public async Task<IEnumerable<UsuarioEjercicio>> GetByUserAndSection(int userId, int sectionId)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, user_id as UserId, problema_id as ProblemaId, 
                       syllabus_section_id as SyllabusSectionId, fecha_asignado as FechaAsignado, 
                       fecha_completado as FechaCompletado, completado as Completado, 
                       es_unico as EsUnico, parametros_unicos as ParametrosUnicos
                       FROM usuario_ejercicio 
                       WHERE user_id = @UserId AND syllabus_section_id = @SectionId";
            return await db.QueryAsync<UsuarioEjercicio>(sql, new { UserId = userId, SectionId = sectionId });
        }

        public async Task<bool> InsertUsuarioEjercicio(UsuarioEjercicio usuarioEjercicio)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO usuario_ejercicio (user_id, problema_id, syllabus_section_id, fecha_asignado, 
                       fecha_completado, completado, es_unico, parametros_unicos)
                       VALUES (@UserId, @ProblemaId, @SyllabusSectionId, @FechaAsignado, 
                       @FechaCompletado, @Completado, @EsUnico, @ParametrosUnicos)";
            var result = await db.ExecuteAsync(sql, usuarioEjercicio);
            return result > 0;
        }

        public async Task<bool> UpdateUsuarioEjercicio(UsuarioEjercicio usuarioEjercicio)
        {
            var db = dbConnection();
            var sql = @"UPDATE usuario_ejercicio SET 
                       fecha_completado = @FechaCompletado, completado = @Completado
                       WHERE id = @Id";
            var result = await db.ExecuteAsync(sql, usuarioEjercicio);
            return result > 0;
        }

        public async Task<bool> HasUserSeenProblema(int userId, int problemaId, string parametrosUnicos = "")
        {
            var db = dbConnection();
            var sql = @"SELECT COUNT(*) FROM usuario_ejercicio 
                       WHERE user_id = @UserId AND problema_id = @ProblemaId AND parametros_unicos = @ParametrosUnicos";
            var count = await db.ExecuteScalarAsync<int>(sql, new { 
                UserId = userId, 
                ProblemaId = problemaId, 
                ParametrosUnicos = parametrosUnicos ?? "" 
            });
            return count > 0;
        }
    }
}

