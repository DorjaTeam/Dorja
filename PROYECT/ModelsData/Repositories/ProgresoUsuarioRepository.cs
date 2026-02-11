using Dapper;
using DorjaModelado;
using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public class ProgresoUsuarioRepository : IProgresoUsuarioRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public ProgresoUsuarioRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<ProgresoUsuario> GetByUserId(int userId)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, user_id as UserId, syllabus_section_id as SyllabusSectionId, 
                       ejercicios_completados_en_seccion as EjerciciosCompletadosEnSeccion, 
                       ejercicios_requeridos_en_seccion as EjerciciosRequeridosEnSeccion, 
                       ultima_actualizacion as UltimaActualizacion, 
                       seccion_completada as SeccionCompletada
                       FROM progreso_usuario WHERE user_id = @UserId";
            var progreso = await db.QueryFirstOrDefaultAsync<ProgresoUsuario>(sql, new { UserId = userId });
            
            // If no progress exists, create initial progress for first section
            if (progreso == null)
            {
                var firstSectionSql = @"SELECT id FROM syllabus_sections WHERE codigo = '2.1' ORDER BY id LIMIT 1";
                var firstSectionId = await db.ExecuteScalarAsync<int?>(firstSectionSql);
                
                if (firstSectionId.HasValue)
                {
                    progreso = new ProgresoUsuario
                    {
                        UserId = userId,
                        SyllabusSectionId = firstSectionId.Value,
                        EjerciciosCompletadosEnSeccion = 0,
                        EjerciciosRequeridosEnSeccion = 3,
                        UltimaActualizacion = DateTime.UtcNow,
                        SeccionCompletada = false
                    };
                    await InsertProgresoUsuario(progreso);
                }
            }
            
            return progreso;
        }

        public async Task<bool> InsertProgresoUsuario(ProgresoUsuario progreso)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO progreso_usuario (user_id, syllabus_section_id, ejercicios_completados_en_seccion, 
                       ejercicios_requeridos_en_seccion, ultima_actualizacion, seccion_completada)
                       VALUES (@UserId, @SyllabusSectionId, @EjerciciosCompletadosEnSeccion, 
                       @EjerciciosRequeridosEnSeccion, @UltimaActualizacion, @SeccionCompletada)";
            var result = await db.ExecuteAsync(sql, progreso);
            return result > 0;
        }

        public async Task<bool> UpdateProgresoUsuario(ProgresoUsuario progreso)
        {
            var db = dbConnection();
            var sql = @"UPDATE progreso_usuario SET 
                       syllabus_section_id = @SyllabusSectionId, 
                       ejercicios_completados_en_seccion = @EjerciciosCompletadosEnSeccion, 
                       ejercicios_requeridos_en_seccion = @EjerciciosRequeridosEnSeccion, 
                       ultima_actualizacion = @UltimaActualizacion, 
                       seccion_completada = @SeccionCompletada
                       WHERE user_id = @UserId";
            var result = await db.ExecuteAsync(sql, progreso);
            return result > 0;
        }

        public async Task<bool> CanUserAccessSection(int userId, int sectionId)
        {
            var db = dbConnection();
            
            // Get the section
            var sectionSql = @"SELECT parent_section_id, requiere_completar_anterior, orden 
                              FROM syllabus_sections WHERE id = @SectionId";
            var section = await db.QueryFirstOrDefaultAsync<dynamic>(sectionSql, new { SectionId = sectionId });
            
            if (section == null) return false;
            
            // If section doesn't require completing previous, allow access
            if (section.requiere_completar_anterior == 0) return true;
            
            // Get user's current progress
            var progreso = await GetByUserId(userId);
            if (progreso == null) return false;
            
            // Get all sections with same parent, ordered
            int? parentId = section.parent_section_id;
            var siblingSectionsSql = @"SELECT id, orden FROM syllabus_sections 
                                      WHERE parent_section_id = @ParentId OR (parent_section_id IS NULL AND @ParentId IS NULL)
                                      ORDER BY orden";
            var siblings = await db.QueryAsync<dynamic>(siblingSectionsSql, new { ParentId = parentId });
            
            // Check if user has completed all previous sections
            foreach (var sibling in siblings)
            {
                if (sibling.orden >= section.orden) break;
                
                // Check if this previous section is completed
                var checkSql = @"SELECT COUNT(*) FROM usuario_ejercicio ue
                                INNER JOIN progreso_usuario pu ON ue.user_id = pu.user_id
                                WHERE ue.user_id = @UserId AND ue.syllabus_section_id = @SectionId 
                                AND ue.completado = 1
                                AND (SELECT COUNT(*) FROM usuario_ejercicio 
                                     WHERE user_id = @UserId AND syllabus_section_id = @SectionId AND completado = 1) 
                                >= (SELECT ejercicios_requeridos_en_seccion FROM progreso_usuario WHERE user_id = @UserId)";
                var completed = await db.ExecuteScalarAsync<int>(checkSql, new { 
                    UserId = userId, 
                    SectionId = sibling.id 
                });
                
                if (completed == 0) return false;
            }
            
            return true;
        }
    }
}

