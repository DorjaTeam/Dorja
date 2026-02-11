using DorjaModelado;
using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaData.Repositories
{
    public interface INivelesRepository
    {
        Task<IEnumerable<Niveles>> GetAllNiveles();
        Task<Niveles> GetDetails(int id);
        Task<bool> InsertNiveles(Niveles niveles);
        Task<bool> UpdateNiveles(Niveles niveles);
        Task<bool> DeleteNiveles(Niveles niveles);
    }
}
