using Dapper;
using DorjaModelado;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public class SyllabusSectionRepository : ISyllabusSectionRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public SyllabusSectionRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<IEnumerable<SyllabusSection>> GetAllSections()
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, parent_section_id as ParentSectionId, codigo as Codigo, 
                       titulo as Titulo, descripcion as Descripcion, orden as Orden, 
                       nivel_id as NivelId, requiere_completar_anterior as RequiereCompletarAnterior
                       FROM syllabus_sections ORDER BY orden";
            return await db.QueryAsync<SyllabusSection>(sql);
        }

        public async Task<SyllabusSection> GetByCodigo(string codigo)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, parent_section_id as ParentSectionId, codigo as Codigo, 
                       titulo as Titulo, descripcion as Descripcion, orden as Orden, 
                       nivel_id as NivelId, requiere_completar_anterior as RequiereCompletarAnterior
                       FROM syllabus_sections WHERE codigo = @Codigo";
            return await db.QueryFirstOrDefaultAsync<SyllabusSection>(sql, new { Codigo = codigo });
        }

        public async Task<SyllabusSection> GetDetails(int id)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, parent_section_id as ParentSectionId, codigo as Codigo, 
                       titulo as Titulo, descripcion as Descripcion, orden as Orden, 
                       nivel_id as NivelId, requiere_completar_anterior as RequiereCompletarAnterior
                       FROM syllabus_sections WHERE id = @Id";
            return await db.QueryFirstOrDefaultAsync<SyllabusSection>(sql, new { Id = id });
        }

        public async Task<IEnumerable<SyllabusSection>> GetByParentId(int? parentId)
        {
            var db = dbConnection();
            if (parentId.HasValue)
            {
                var sql = @"SELECT id as Id, parent_section_id as ParentSectionId, codigo as Codigo, 
                           titulo as Titulo, descripcion as Descripcion, orden as Orden, 
                           nivel_id as NivelId, requiere_completar_anterior as RequiereCompletarAnterior
                           FROM syllabus_sections WHERE parent_section_id = @ParentId ORDER BY orden";
                return await db.QueryAsync<SyllabusSection>(sql, new { ParentId = parentId.Value });
            }
            else
            {
                var sql = @"SELECT id as Id, parent_section_id as ParentSectionId, codigo as Codigo, 
                           titulo as Titulo, descripcion as Descripcion, orden as Orden, 
                           nivel_id as NivelId, requiere_completar_anterior as RequiereCompletarAnterior
                           FROM syllabus_sections WHERE parent_section_id IS NULL ORDER BY orden";
                return await db.QueryAsync<SyllabusSection>(sql);
            }
        }

        public async Task<bool> InsertSection(SyllabusSection section)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO syllabus_sections (parent_section_id, codigo, titulo, descripcion, orden, nivel_id, requiere_completar_anterior)
                       VALUES (@ParentSectionId, @Codigo, @Titulo, @Descripcion, @Orden, @NivelId, @RequiereCompletarAnterior)";
            var result = await db.ExecuteAsync(sql, section);
            return result > 0;
        }

        public async Task<bool> UpdateSection(SyllabusSection section)
        {
            var db = dbConnection();
            var sql = @"UPDATE syllabus_sections SET 
                       parent_section_id = @ParentSectionId, codigo = @Codigo, titulo = @Titulo, 
                       descripcion = @Descripcion, orden = @Orden, nivel_id = @NivelId, 
                       requiere_completar_anterior = @RequiereCompletarAnterior
                       WHERE id = @Id";
            var result = await db.ExecuteAsync(sql, section);
            return result > 0;
        }
    }
}

