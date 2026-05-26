using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DorjaModelado.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<Users>> GetAllUsers();
        Task<IEnumerable<Users>> GetAllStudents();
        Task<IEnumerable<Users>> GetAllTeachers();
        Task<Users> GetDetails(int id);
        Task<bool> InsertUsers(Users usuario);
        Task<bool> UpdateUsuarios(Users usuario);
        Task<bool> DeleteUsuarios(Users usuario);
        Task<Users?> GetByEmail(string email);
        Task<Users?> GetByUsername(string username);
        Task<Users?> GetByGoogleId(string googleId);
        Task<bool> UpdatePhotoBlob(int userId, string imageType, byte[] imageData);
        Task<byte[]?> GetPhotoBlob(int userId, string imageType);
    }
}
