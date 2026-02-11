using Dapper;
using DorjaData;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DorjaModelado.Repositories
{
    public class UsersRepository : IUserRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public UsersRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        public async Task<IEnumerable<Users>> GetAllUsers()
        {
            var db = dbConnection();
            var sql = @"SELECT id, username, nombre, apellidoPaterno, apellidoMaterno,
                        email, password, fechaRegistro, ultimaConexion, puntosTotales, nivelActual,
                        COALESCE(profilePhotoPath, '') as ProfilePhotoPath, 
                        COALESCE(coverPhotoPath, '') as CoverPhotoPath
                        FROM users";

            var users = await db.QueryAsync<Users>(sql, new { });
            
            // Ensure photo paths are never null
            foreach (var user in users)
            {
                if (user.ProfilePhotoPath == null) user.ProfilePhotoPath = string.Empty;
                if (user.CoverPhotoPath == null) user.CoverPhotoPath = string.Empty;
            }
            
            return users;
        }

        public async Task<Users> GetDetails(int id)
        {
            var db = dbConnection();
            var sql = @"SELECT id, username, nombre, apellidoPaterno, apellidoMaterno,
                       email, password, fechaRegistro, ultimaConexion, puntosTotales, nivelActual,
                       COALESCE(profilePhotoPath, '') as ProfilePhotoPath, 
                       COALESCE(coverPhotoPath, '') as CoverPhotoPath
                       FROM users
                       WHERE id = @id";

            var user = await db.QueryFirstOrDefaultAsync<Users>(sql, new { id });
            
            // Ensure photo paths are never null
            if (user != null)
            {
                if (user.ProfilePhotoPath == null) user.ProfilePhotoPath = string.Empty;
                if (user.CoverPhotoPath == null) user.CoverPhotoPath = string.Empty;
            }
            
            return user;
        }

        public async Task<bool> InsertUsers(Users usuario)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO users (username, nombre, apellidoPaterno, apellidoMaterno,
                                   email, password, fechaRegistro, ultimaConexion, puntosTotales, nivelActual,
                                   profilePhotoPath, coverPhotoPath)
                        VALUES (@Username, @Nombre, @ApellidoPaterno, @ApellidoMaterno,
                                @Email, @Password, @FechaRegistro, @UltimaConexion, @PuntosTotales, @NivelActual,
                                @ProfilePhotoPath, @CoverPhotoPath)";

            var result = await db.ExecuteAsync(sql, usuario);
            return result > 0;
        }

        public async Task<bool> UpdateUsuarios(Users usuario)
        {
            // Ensure photo paths are never null before updating
            if (usuario.ProfilePhotoPath == null) usuario.ProfilePhotoPath = string.Empty;
            if (usuario.CoverPhotoPath == null) usuario.CoverPhotoPath = string.Empty;
            
            var db = dbConnection();
            var sql = @"UPDATE users SET
                        username = @Username,
                        nombre = @Nombre,
                        apellidoPaterno = @ApellidoPaterno,
                        apellidoMaterno = @ApellidoMaterno,
                        email = @Email,
                        password = @Password,
                        fechaRegistro = @FechaRegistro,
                        ultimaConexion = @UltimaConexion,
                        puntosTotales = @PuntosTotales,
                        nivelActual = @NivelActual,
                        profilePhotoPath = @ProfilePhotoPath,
                        coverPhotoPath = @CoverPhotoPath
                        WHERE id = @Id";

            var result = await db.ExecuteAsync(sql, usuario);
            return result > 0;
        }

        public async Task<bool> DeleteUsuarios(Users usuario)
        {
            var db = dbConnection();
            var sql = @"DELETE FROM users WHERE id = @Id";

            var result = await db.ExecuteAsync(sql, new { usuario.Id });
            return result > 0;
        }

        public async Task<Users?> GetByEmail(string email)
        {
            var db = dbConnection();
            var sql = @"SELECT id, username, nombre, apellidoPaterno, apellidoMaterno,
                        email, password, fechaRegistro, ultimaConexion, puntosTotales, nivelActual,
                        COALESCE(profilePhotoPath, '') as ProfilePhotoPath, 
                        COALESCE(coverPhotoPath, '') as CoverPhotoPath
                        FROM users WHERE email = @Email";

            var user = await db.QueryFirstOrDefaultAsync<Users>(sql, new { Email = email });
            
            // Ensure photo paths are never null
            if (user != null)
            {
                if (user.ProfilePhotoPath == null) user.ProfilePhotoPath = string.Empty;
                if (user.CoverPhotoPath == null) user.CoverPhotoPath = string.Empty;
            }
            
            return user;
        }

        public async Task<Users?> GetByUsername(string username)
        {
            var db = dbConnection();
            var sql = @"SELECT id, username, nombre, apellidoPaterno, apellidoMaterno,
                        email, password, fechaRegistro, ultimaConexion, puntosTotales, nivelActual,
                        COALESCE(profilePhotoPath, '') as ProfilePhotoPath, 
                        COALESCE(coverPhotoPath, '') as CoverPhotoPath
                        FROM users WHERE username = @Username";

            var user = await db.QueryFirstOrDefaultAsync<Users>(sql, new { Username = username });
            
            // Ensure photo paths are never null
            if (user != null)
            {
                if (user.ProfilePhotoPath == null) user.ProfilePhotoPath = string.Empty;
                if (user.CoverPhotoPath == null) user.CoverPhotoPath = string.Empty;
            }
            
            return user;
        }

        public async Task<Users?> ValidateLogin(string email, string passwordHash)
        {
            var db = dbConnection();
            var sql = @"SELECT id, username, email, password
                        FROM users
                        WHERE email = @Email AND password = @Password";

            return await db.QueryFirstOrDefaultAsync<Users>(sql, new { Email = email, Password = passwordHash });
        }

        // Methods for BLOB image storage
        public async Task<bool> UpdatePhotoBlob(int userId, string imageType, byte[] imageData)
        {
            var db = dbConnection();
            var columnName = imageType == "profile" ? "profilePhotoBlob" : "coverPhotoBlob";
            var sql = $@"UPDATE users SET {columnName} = @ImageData WHERE id = @UserId";

            var result = await db.ExecuteAsync(sql, new { UserId = userId, ImageData = imageData });
            return result > 0;
        }

        public async Task<byte[]?> GetPhotoBlob(int userId, string imageType)
        {
            var db = dbConnection();
            // Validate imageType to prevent SQL injection (though it's already validated in controller)
            var columnName = imageType == "profile" ? "profilePhotoBlob" : "coverPhotoBlob";
            var sql = $@"SELECT {columnName} FROM users WHERE id = @UserId";

            // Use QueryFirstOrDefaultAsync with proper type handling for BLOB
            var result = await db.QueryFirstOrDefaultAsync<byte[]>(sql, new { UserId = userId });
            return result;
        }
    }
}
