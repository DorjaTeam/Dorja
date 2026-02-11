using Dapper;
using DorjaModelado;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaData.Repositories
{
    public class CertificadoRepository : ICertificadosRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public CertificadoRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }


        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<bool> DeleteCertificados(Certificados certificados)
        {
            var db = dbConnection();
            var sql = "DELETE FROM certificados WHERE id = @idCertificados";
            var result = await db.ExecuteAsync(sql, certificados);
            return result > 0;
        }

        public async Task<IEnumerable<Certificados>> GetAllCertificados()
        {
            var db = dbConnection();
            var sql = @"SELECT id as idCertificados, user_id as Id_User, nivel_id as Nivel_Id, 
                       rutaPDF as rutaPDF, fechaGenerado as fechaGenerado 
                       FROM certificados";
            return await db.QueryAsync<Certificados>(sql);
        }

        public async Task<Certificados> GetDetails(int id)
        {
            var db = dbConnection();
            var sql = @"SELECT id as idCertificados, user_id as Id_User, nivel_id as Nivel_Id, 
                       rutaPDF as rutaPDF, fechaGenerado as fechaGenerado 
                       FROM certificados WHERE id = @Id";
            return await db.QueryFirstOrDefaultAsync<Certificados>(sql, new { Id = id });
        }

        public async Task<bool> InsertCertificados(Certificados certificados)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO certificados (user_id, nivel_id, rutaPDF, fechaGenerado) 
                        VALUES (@Id_User, @Nivel_Id, @rutaPDF, @fechaGenerado)";
            var result = await db.ExecuteAsync(sql, certificados);
            return result > 0;
        }

        public async Task<bool> UpdateCertificados(Certificados certificados)
        {
            var db = dbConnection();
            var sql = @"UPDATE certificados SET 
                            user_id = @Id_User,
                            nivel_id = @Nivel_Id,
                            rutaPDF = @rutaPDF,
                            fechaGenerado = @fechaGenerado
                        WHERE id = @idCertificados";
            var result = await db.ExecuteAsync(sql, certificados);
            return result > 0;
        }
    }
}
