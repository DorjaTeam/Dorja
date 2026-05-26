using System.Collections.Generic;
using System.Threading.Tasks;
using DorjaModelado;

namespace DorjaModelado.Repositories
{
    public interface IMensajesContactoRepository
    {
        Task<IEnumerable<MensajeContacto>> GetByTeacher(int teacherId);
        Task<IEnumerable<MensajeContacto>> GetByStudent(int studentId);
        Task<MensajeContacto?> GetById(int id);
        Task<bool> Insert(MensajeContacto mensaje);
    }
}
