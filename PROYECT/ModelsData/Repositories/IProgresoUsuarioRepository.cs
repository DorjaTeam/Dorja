using DorjaModelado;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public interface IProgresoUsuarioRepository
    {
        Task<ProgresoUsuario> GetByUserId(int userId);
        Task<bool> InsertProgresoUsuario(ProgresoUsuario progreso);
        Task<bool> UpdateProgresoUsuario(ProgresoUsuario progreso);
        Task<bool> CanUserAccessSection(int userId, int sectionId);
    }
}

