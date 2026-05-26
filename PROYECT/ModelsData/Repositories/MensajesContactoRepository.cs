using Dapper;
using DorjaData;
using DorjaModelado;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaModelado.Repositories
{
    public class MensajesContactoRepository : IMensajesContactoRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public MensajesContactoRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<IEnumerable<MensajeContacto>> GetByTeacher(int teacherId)
        {
            using var db = dbConnection();
            var sql = @"SELECT id as Id, estudiante_id as EstudianteId, maestro_id as MaestroId,
                        nombre_estudiante as NombreEstudiante, correo_estudiante as CorreoEstudiante,
                        asunto as Asunto, mensaje as Mensaje, fecha_envio as FechaEnvio,
                        tiene_pdf as TienePdf, pdf_base64 as PdfBase64
                        FROM mensajes_contacto
                        WHERE maestro_id = @TeacherId
                        ORDER BY fecha_envio DESC";
            return await db.QueryAsync<MensajeContacto>(sql, new { TeacherId = teacherId });
        }

        public async Task<IEnumerable<MensajeContacto>> GetByStudent(int studentId)
        {
            using var db = dbConnection();
            var sql = @"SELECT id as Id, estudiante_id as EstudianteId, maestro_id as MaestroId,
                        nombre_estudiante as NombreEstudiante, correo_estudiante as CorreoEstudiante,
                        asunto as Asunto, mensaje as Mensaje, fecha_envio as FechaEnvio,
                        tiene_pdf as TienePdf, pdf_base64 as PdfBase64
                        FROM mensajes_contacto
                        WHERE estudiante_id = @StudentId
                        ORDER BY fecha_envio DESC";
            return await db.QueryAsync<MensajeContacto>(sql, new { StudentId = studentId });
        }

        public async Task<MensajeContacto?> GetById(int id)
        {
            using var db = dbConnection();
            var sql = @"SELECT id as Id, estudiante_id as EstudianteId, maestro_id as MaestroId,
                        nombre_estudiante as NombreEstudiante, correo_estudiante as CorreoEstudiante,
                        asunto as Asunto, mensaje as Mensaje, fecha_envio as FechaEnvio,
                        tiene_pdf as TienePdf, pdf_base64 as PdfBase64
                        FROM mensajes_contacto
                        WHERE id = @Id";
            return await db.QueryFirstOrDefaultAsync<MensajeContacto>(sql, new { Id = id });
        }

        public async Task<bool> Insert(MensajeContacto mensaje)
        {
            using var db = dbConnection();
            var sql = @"INSERT INTO mensajes_contacto (estudiante_id, maestro_id, nombre_estudiante, correo_estudiante, asunto, mensaje, fecha_envio, tiene_pdf, pdf_base64)
                        VALUES (@EstudianteId, @MaestroId, @NombreEstudiante, @CorreoEstudiante, @Asunto, @Mensaje, @FechaEnvio, @TienePdf, @PdfBase64)";
            
            if (string.IsNullOrEmpty(mensaje.FechaEnvio))
            {
                mensaje.FechaEnvio = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }

            var result = await db.ExecuteAsync(sql, new
            {
                mensaje.EstudianteId,
                mensaje.MaestroId,
                mensaje.NombreEstudiante,
                mensaje.CorreoEstudiante,
                mensaje.Asunto,
                mensaje.Mensaje,
                mensaje.FechaEnvio,
                mensaje.TienePdf,
                mensaje.PdfBase64
            });
            return result > 0;
        }
    }
}
