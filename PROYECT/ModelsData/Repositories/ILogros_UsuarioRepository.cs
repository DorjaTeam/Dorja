using DorjaModelado;
using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaData.Repositories
{
    public interface ILogros_UsuarioRepository
    {
        Task<IEnumerable<Logros_Usuario>> GetAllLogrosUsuario();
        Task<Logros_Usuario> GetDetails(int id);
        Task<bool> InsertLogrosUsuario(Logros_Usuario logros_Usuario);
        Task<bool> UpdateLogrosUsuario(Logros_Usuario logros_Usuario);
        Task<bool> DeleteLogrosUsuario(Logros_Usuario logros_Usuario);
        Task<IEnumerable<Logros_Usuario>> GetLogrosByUserId(int userId);
        Task<bool> UserHasLogro(int userId, int logroId);
    }
}
