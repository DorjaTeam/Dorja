using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaModelado.Repositories
{
    public interface ICalificacionesRepository
    {
        Task<IEnumerable<Calificacion>> GetByEstudiante(int estudianteId);
        Task<IEnumerable<Calificacion>> GetByMaestro(int maestroId);
        Task<Calificacion?> GetByMaestroAndEstudiante(int maestroId, int estudianteId);
        Task<bool> Upsert(Calificacion calificacion);
        Task<bool> Delete(int id);
    }
}
