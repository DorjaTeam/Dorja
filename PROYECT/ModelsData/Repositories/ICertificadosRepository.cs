using DorjaModelado;
using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaData.Repositories
{
    public interface ICertificadosRepository
    {
        Task<IEnumerable<Certificados>> GetAllCertificados();
        Task<Certificados> GetDetails(int id);
        Task<bool> InsertCertificados(Certificados certificados);
        Task<bool> UpdateCertificados(Certificados certificados);
        Task<bool> DeleteCertificados(Certificados certificados);
    }
}
