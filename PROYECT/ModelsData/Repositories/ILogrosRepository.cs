using DorjaModelado;
using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaData.Repositories
{
    public interface ILogrosRepository
    {
        Task<IEnumerable<Logros>> GetAllLogros();
        Task<Logros> GetDetails(int id);
        Task<bool> InsertLogros(Logros logros);
        Task<bool> UpdateLogros(Logros logros);
        Task<bool> DeleteLogros(Logros logros);
        Task<Logros> GetLogroByNombre(string nombre);
    }
}
