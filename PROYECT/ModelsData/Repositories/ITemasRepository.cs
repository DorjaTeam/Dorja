using DorjaModelado;
using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaData.Repositories
{
    public interface ITemasRepository
    {
        Task<IEnumerable<Temas>> GetAllTemas();
        Task<Temas> GetDetails(int id);
        Task<bool> InsertTemas(Temas temas);
        Task<bool> UpdateTemas(Temas temas);
        Task<bool> DeleteTemas(Temas temas);
    }
}
