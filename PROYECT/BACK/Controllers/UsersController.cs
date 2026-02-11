using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DorjaModelado.Repositories;
using DorjaModelado;
using DorjaData.Repositories;
using System.Text;
using System.Linq;


namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _usersRepository;
        private readonly ILogrosRepository _logrosRepository;
        private readonly ILogros_UsuarioRepository _logrosUsuarioRepository;
        private readonly IProblemaRepository _problemaRepository;
        private readonly IProgreso_ProblemaRepository _progresoProblemaRepository;

        public UsersController(
            IUserRepository usersRepository, 
            ILogrosRepository logrosRepository, 
            ILogros_UsuarioRepository logrosUsuarioRepository,
            IProblemaRepository problemaRepository,
            IProgreso_ProblemaRepository progresoProblemaRepository)
        {
            _usersRepository = usersRepository;
            _logrosRepository = logrosRepository;
            _logrosUsuarioRepository = logrosUsuarioRepository;
            _problemaRepository = problemaRepository;
            _progresoProblemaRepository = progresoProblemaRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            return Ok(await _usersRepository.GetAllUsers());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            return Ok(await _usersRepository.GetDetails(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateUsuers([FromBody] Users usuario)
        {
            if (usuario == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _usersRepository.InsertUsers(usuario);

            return Created("created", created);
        }


        [HttpPut]
        public async Task<IActionResult> UpdateUsers([FromBody] Users usuario)
        {
            if (usuario == null)
            {
                return BadRequest(new { message = "Datos inválidos" });
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get current user data from database to preserve fields not being updated
            var currentUser = await _usersRepository.GetDetails(usuario.Id);
            if (currentUser == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Merge: use provided values, fall back to current database values
            // This ensures we don't lose data like photo paths, password, etc.
            var userToUpdate = new Users
            {
                Id = usuario.Id,
                Username = !string.IsNullOrWhiteSpace(usuario.Username) ? usuario.Username : currentUser.Username,
                Email = !string.IsNullOrWhiteSpace(usuario.Email) ? usuario.Email : currentUser.Email,
                Nombre = !string.IsNullOrWhiteSpace(usuario.Nombre) ? usuario.Nombre : currentUser.Nombre,
                ApellidoPaterno = !string.IsNullOrWhiteSpace(usuario.ApellidoPaterno) ? usuario.ApellidoPaterno : currentUser.ApellidoPaterno,
                ApellidoMaterno = !string.IsNullOrWhiteSpace(usuario.ApellidoMaterno) ? usuario.ApellidoMaterno : currentUser.ApellidoMaterno,
                Password = !string.IsNullOrWhiteSpace(usuario.Password) ? usuario.Password : currentUser.Password,
                FechaRegistro = usuario.FechaRegistro != default ? usuario.FechaRegistro : currentUser.FechaRegistro,
                UltimaConexion = usuario.UltimaConexion ?? currentUser.UltimaConexion,
                PuntosTotales = usuario.PuntosTotales != default ? usuario.PuntosTotales : currentUser.PuntosTotales,
                NivelActual = usuario.NivelActual != default ? usuario.NivelActual : currentUser.NivelActual,
                // CRITICAL: Preserve photo paths from database unless explicitly provided
                ProfilePhotoPath = !string.IsNullOrWhiteSpace(usuario.ProfilePhotoPath) ? usuario.ProfilePhotoPath : currentUser.ProfilePhotoPath,
                CoverPhotoPath = !string.IsNullOrWhiteSpace(usuario.CoverPhotoPath) ? usuario.CoverPhotoPath : currentUser.CoverPhotoPath
            };

            var updated = await _usersRepository.UpdateUsuarios(userToUpdate);
            if (!updated)
            {
                return StatusCode(500, new { message = "Error al actualizar el usuario" });
            }
            
            // Return the updated user data
            var updatedUser = await _usersRepository.GetDetails(usuario.Id);
            return Ok(updatedUser);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUsers(int id)
        {
            await _usersRepository.DeleteUsuarios(new Users { Id = id });

            return NoContent();
        }

        // --------------------------  SIGNUP  ----------------------------

        [HttpPost("signup")]

        public async Task<IActionResult> Signup([FromBody] Users users)
        {
            if (users == null)
            {
                return BadRequest(new { message = "Datos invalidos" });
            }

            if (string.IsNullOrWhiteSpace(users.Email) ||
               string.IsNullOrWhiteSpace(users.Password) ||
               string.IsNullOrWhiteSpace(users.Username))
            {
                return BadRequest(new { message = "Email, Username y Password son obligatorios" });
            }

            // Check if email already exists
            var existingByEmail = await _usersRepository.GetByEmail(users.Email);
            if (existingByEmail != null)
            {
                return Conflict(new { message = "El email ya está registrado" });
            }

            // Check if username already exists
            var existingByUsername = await _usersRepository.GetByUsername(users.Username);
            if (existingByUsername != null)
            {
                return Conflict(new { message = "El nombre de usuario ya está en uso" });
            }

            // Set default values for new user
            users.Password = HashPassword(users.Password);
            users.FechaRegistro = DateTime.Now;
            users.UltimaConexion = null; // Will be set on first login
            users.PuntosTotales = 0;
            users.NivelActual = 1; // Start at level 1
            users.ProfilePhotoPath = string.Empty;
            users.CoverPhotoPath = string.Empty;

            var created = await _usersRepository.InsertUsers(users);

            if (!created)
            {
                return StatusCode(500, new { message = "Error al registrar el usuario" });
            }

            // Get the created user to get the ID
            var createdUser = await _usersRepository.GetByEmail(users.Email);
            
            // Grant "Crear cuenta" achievement
            if (createdUser != null)
            {
                await GrantLogroIfNotExists(createdUser.Id, "Crear cuenta");
            }

            return Ok(new { 
                message = "Usuario registrado correctamente",
                userId = createdUser?.Id,
                achievementGranted = createdUser != null
            });
        }

        // --------------------------  LOGIN  ----------------------------

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Datos inválidos" });
            }

            if ((string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Username)) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email/Username y Password son obligatorios" });
            }

            // Try to find user by email or username
            Users? existing = null;
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                existing = await _usersRepository.GetByEmail(request.Email);
            }
            
            if (existing == null && !string.IsNullOrWhiteSpace(request.Username))
            {
                existing = await _usersRepository.GetByUsername(request.Username);
            }

            if (existing == null)
            {
                return Unauthorized(new { message = "Email/Username o contraseña incorrectos" });
            }

            // Hashear la contraseña ingresada para compararla con la almacenada
            var hashedInputPassword = HashPassword(request.Password);

            if (existing.Password != hashedInputPassword)
            {
                return Unauthorized(new { message = "Email/Username o contraseña incorrectos" });
            }

            // Aquí podrías generar un token JWT (si quieres seguridad avanzada)
            return Ok(new
            {
                message = "Inicio de sesión exitoso",
                user = new
                {
                    existing.Id,
                    existing.Username,
                    existing.Email,
                    existing.Nombre,
                    existing.ApellidoPaterno,
                    existing.ApellidoMaterno
                }
            });
        }

        // Helper class for login request
        public class LoginRequest
        {
            public string? Username { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
        }

        // --------------------------  IMAGE UPLOAD  ----------------------------

        [HttpPost("{userId}/upload-image")]
        public async Task<IActionResult> UploadImage(int userId, [FromForm] IFormFile file, [FromForm] string imageType)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No se proporcionó ningún archivo" });
            }

            if (string.IsNullOrWhiteSpace(imageType) || (imageType != "profile" && imageType != "cover"))
            {
                return BadRequest(new { message = "Tipo de imagen inválido. Debe ser 'profile' o 'cover'" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Tipo de archivo no permitido. Solo se permiten imágenes (JPG, PNG, GIF)" });
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "El archivo es demasiado grande. El tamaño máximo es 5MB" });
            }

            try
            {
                // Get user to update
                var user = await _usersRepository.GetDetails(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                // Ensure photo paths are not null
                if (user.ProfilePhotoPath == null) user.ProfilePhotoPath = string.Empty;
                if (user.CoverPhotoPath == null) user.CoverPhotoPath = string.Empty;

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "users", userId.ToString());
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generate unique filename
                var fileName = $"{imageType}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, fileName);
                var relativePath = $"/uploads/users/{userId}/{fileName}";

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update user record - only update the path for the image type being uploaded
                // The other photo path is already loaded from the database and will be preserved
                if (imageType == "profile")
                {
                    user.ProfilePhotoPath = relativePath;
                    // CoverPhotoPath is already set from GetDetails, so it will be preserved
                }
                else
                {
                    user.CoverPhotoPath = relativePath;
                    // ProfilePhotoPath is already set from GetDetails, so it will be preserved
                }

                // Log before update for debugging
                Console.WriteLine($"Updating user {userId}: ProfilePhotoPath='{user.ProfilePhotoPath}', CoverPhotoPath='{user.CoverPhotoPath}'");

                var updateResult = await _usersRepository.UpdateUsuarios(user);
                
                if (!updateResult)
                {
                    Console.WriteLine($"ERROR: Failed to update user {userId} in database");
                    return StatusCode(500, new { message = "Error al actualizar el registro del usuario en la base de datos" });
                }

                // Verify the update by getting the user again
                var updatedUser = await _usersRepository.GetDetails(userId);
                var savedPath = imageType == "profile" ? updatedUser?.ProfilePhotoPath : updatedUser?.CoverPhotoPath;

                // Log after update for debugging
                Console.WriteLine($"After update - User {userId}: ProfilePhotoPath='{updatedUser?.ProfilePhotoPath}', CoverPhotoPath='{updatedUser?.CoverPhotoPath}'");
                Console.WriteLine($"Saved path for {imageType}: '{savedPath}'");

                if (string.IsNullOrEmpty(savedPath))
                {
                    Console.WriteLine($"WARNING: Path was not saved correctly for {imageType} image");
                }

                // Grant "Personalizar perfil" achievement if profile photo was uploaded
                bool achievementGranted = false;
                if (imageType == "profile" && !string.IsNullOrEmpty(savedPath))
                {
                    achievementGranted = await GrantLogroIfNotExists(userId, "Personalizar perfil");
                }

                return Ok(new
                {
                    success = true,
                    message = "Imagen subida exitosamente",
                    path = relativePath,
                    savedPath = savedPath, // Return the saved path for verification
                    achievementGranted = achievementGranted
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al subir la imagen: {ex.Message}" });
            }
        }

        // --------------------------  IMAGE UPLOAD AS BLOB (Database Storage)  ----------------------------

        [HttpPost("{userId}/upload-image-blob")]
        public async Task<IActionResult> UploadImageAsBlob(int userId, [FromForm] IFormFile file, [FromForm] string imageType)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No se proporcionó ningún archivo" });
            }

            if (string.IsNullOrWhiteSpace(imageType) || (imageType != "profile" && imageType != "cover"))
            {
                return BadRequest(new { message = "Tipo de imagen inválido. Debe ser 'profile' o 'cover'" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Tipo de archivo no permitido. Solo se permiten imágenes (JPG, PNG, GIF)" });
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "El archivo es demasiado grande. El tamaño máximo es 5MB" });
            }

            try
            {
                // Get user to verify existence
                var user = await _usersRepository.GetDetails(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                // Read image data into byte array
                byte[] imageData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                }

                // Save to database as BLOB
                var updateResult = await _usersRepository.UpdatePhotoBlob(userId, imageType, imageData);
                
                if (!updateResult)
                {
                    return StatusCode(500, new { message = "Error al guardar la imagen en la base de datos" });
                }

                // Grant "Personalizar perfil" achievement if profile photo was uploaded
                bool achievementGranted = false;
                if (imageType == "profile")
                {
                    achievementGranted = await GrantLogroIfNotExists(userId, "Personalizar perfil");
                }

                return Ok(new
                {
                    success = true,
                    message = "Imagen guardada exitosamente en la base de datos",
                    storageType = "blob",
                    size = imageData.Length,
                    achievementGranted = achievementGranted
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al subir la imagen: {ex.Message}" });
            }
        }

        [HttpGet("{userId}/image-blob")]
        public async Task<IActionResult> GetImageBlob(int userId, [FromQuery] string imageType)
        {
            if (string.IsNullOrWhiteSpace(imageType) || (imageType != "profile" && imageType != "cover"))
            {
                return BadRequest(new { message = "Tipo de imagen inválido. Debe ser 'profile' o 'cover'" });
            }

            try
            {
                var imageData = await _usersRepository.GetPhotoBlob(userId, imageType);
                
                if (imageData == null || imageData.Length == 0)
                {
                    return NotFound(new { message = "Imagen no encontrada en la base de datos" });
                }

                // Determine content type based on image data
                string contentType = "image/jpeg"; // Default
                if (imageData.Length > 4)
                {
                    // Check for PNG signature
                    if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
                    {
                        contentType = "image/png";
                    }
                    // Check for GIF signature
                    else if (imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46)
                    {
                        contentType = "image/gif";
                    }
                }

                return File(imageData, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al recuperar la imagen: {ex.Message}" });
            }
        }

        // --------------------------  HASH  ----------------------------
        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // --------------------------  ACHIEVEMENTS  ----------------------------
        private async Task<bool> GrantLogroIfNotExists(int userId, string logroNombre)
        {
            try
            {
                // Get logro by name
                var logro = await _logrosRepository.GetLogroByNombre(logroNombre);
                if (logro == null)
                {
                    Console.WriteLine($"Logro '{logroNombre}' no encontrado");
                    return false;
                }

                // Check if user already has this logro
                var hasLogro = await _logrosUsuarioRepository.UserHasLogro(userId, logro.Id);
                if (hasLogro)
                {
                    return false; // Already has it
                }

                // Grant the logro
                var logroUsuario = new Logros_Usuario
                {
                    Id_Usuario = userId,
                    Id_Logro = logro.Id,
                    Fecha_Obtencion = DateTime.Now
                };

                var created = await _logrosUsuarioRepository.InsertLogrosUsuario(logroUsuario);
                return created;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error granting logro '{logroNombre}' to user {userId}: {ex.Message}");
                return false;
            }
        }

        // --------------------------  STATISTICS  ----------------------------

        [HttpGet("{userId}/stats")]
        public async Task<IActionResult> GetUserStats(int userId)
        {
            try
            {
                // Verify user exists
                var user = await _usersRepository.GetDetails(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                // Update last connection time if it's a different day
                var today = DateTime.Today;
                if (user.UltimaConexion == null || user.UltimaConexion.Value.Date < today)
                {
                    user.UltimaConexion = DateTime.Now;
                    await _usersRepository.UpdateUsuarios(user);
                    // Reload user to get updated UltimaConexion
                    user = await _usersRepository.GetDetails(userId);
                }

                // Calculate streak: consecutive days with activity
                var streak = await CalculateStreak(userId, user);

                // Calculate exercise completion percentage
                var completionPercentage = await CalculateCompletionPercentage(userId);

                return Ok(new
                {
                    streak = streak,
                    completionPercentage = completionPercentage
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting stats for user {userId}: {ex.Message}");
                return StatusCode(500, new { message = $"Error al obtener estadísticas: {ex.Message}" });
            }
        }

        private async Task<int> CalculateStreak(int userId, Users user)
        {
            try
            {
                var today = DateTime.Today;

                // Get all completed exercises with their completion dates
                var progresos = await _progresoProblemaRepository.GetByUserId(userId);
                var completedProgresos = progresos.Where(p => p.Completado && p.FechaCompletado.HasValue).ToList();

                // Get unique dates when exercises were completed
                var activityDates = completedProgresos
                    .Select(p => p.FechaCompletado.Value.Date)
                    .Distinct()
                    .ToHashSet();

                // Include today if user has activity today (even if no exercises completed)
                // This counts just using the app as activity
                if (user.UltimaConexion != null && user.UltimaConexion.Value.Date == today)
                {
                    activityDates.Add(today);
                }

                // If no activity at all, return 0
                if (activityDates.Count == 0)
                {
                    return 0;
                }

                // Calculate consecutive days from today backwards
                int streak = 0;
                var currentDate = today;

                // Count consecutive days backwards
                while (activityDates.Contains(currentDate))
                {
                    streak++;
                    currentDate = currentDate.AddDays(-1);
                }

                return streak;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating streak for user {userId}: {ex.Message}");
                return 0;
            }
        }

        private async Task<double> CalculateCompletionPercentage(int userId)
        {
            try
            {
                // Get total number of problems
                var allProblemas = await _problemaRepository.GetAllProblemas();
                var totalProblemas = allProblemas.Count();

                if (totalProblemas == 0)
                {
                    return 0;
                }

                // Get completed exercises for this user
                var progresos = await _progresoProblemaRepository.GetByUserId(userId);
                var completedCount = progresos.Count(p => p.Completado);

                // Calculate percentage
                var percentage = (double)completedCount / totalProblemas * 100;
                return Math.Round(percentage, 1); // Round to 1 decimal place
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating completion percentage for user {userId}: {ex.Message}");
                return 0;
            }
        }
    }
}
