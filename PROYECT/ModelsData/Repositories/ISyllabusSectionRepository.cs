using DorjaModelado;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public interface ISyllabusSectionRepository
    {
        Task<IEnumerable<SyllabusSection>> GetAllSections();
        Task<SyllabusSection> GetByCodigo(string codigo);
        Task<SyllabusSection> GetDetails(int id);
        Task<IEnumerable<SyllabusSection>> GetByParentId(int? parentId);
        Task<bool> InsertSection(SyllabusSection section);
        Task<bool> UpdateSection(SyllabusSection section);
    }
}

