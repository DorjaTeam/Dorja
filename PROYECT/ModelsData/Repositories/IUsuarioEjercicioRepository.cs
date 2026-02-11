using DorjaModelado;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public interface IUsuarioEjercicioRepository
    {
        Task<IEnumerable<UsuarioEjercicio>> GetByUserId(int userId);
        Task<UsuarioEjercicio> GetByUserAndProblema(int userId, int problemaId, string parametrosUnicos = "");
        Task<IEnumerable<UsuarioEjercicio>> GetByUserAndSection(int userId, int sectionId);
        Task<bool> InsertUsuarioEjercicio(UsuarioEjercicio usuarioEjercicio);
        Task<bool> UpdateUsuarioEjercicio(UsuarioEjercicio usuarioEjercicio);
        Task<bool> HasUserSeenProblema(int userId, int problemaId, string parametrosUnicos = "");
    }
}

