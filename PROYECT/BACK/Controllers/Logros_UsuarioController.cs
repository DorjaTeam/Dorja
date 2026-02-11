using DorjaData.Repositories;
using DorjaModelado;
using Microsoft.AspNetCore.Mvc;

namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Logros_UsuarioController : ControllerBase
    {
        private readonly ILogros_UsuarioRepository  _logros_UsuarioRepository;

        public Logros_UsuarioController(ILogros_UsuarioRepository logros_UsuarioRepository)
        {
            _logros_UsuarioRepository = logros_UsuarioRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLogros_Usuario()
        {
            return Ok(await _logros_UsuarioRepository.GetAllLogrosUsuario());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            return Ok(await _logros_UsuarioRepository.GetDetails(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNiveles([FromBody] Logros_Usuario logros_usuario)
        {
            if (logros_usuario == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var created = await _logros_UsuarioRepository.InsertLogrosUsuario(logros_usuario);
            return Created("created", created);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateNiveles([FromBody] Logros_Usuario logros_Usuario)
        {
            if (logros_Usuario == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _logros_UsuarioRepository.UpdateLogrosUsuario(logros_Usuario);
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteNiveles(int id)
        {
            await _logros_UsuarioRepository.DeleteLogrosUsuario(new Logros_Usuario { id = id });
            return NoContent();
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetLogrosByUserId(int userId)
        {
            var logros = await _logros_UsuarioRepository.GetLogrosByUserId(userId);
            return Ok(logros);
        }

        [HttpPost("grant")]
        public async Task<IActionResult> GrantLogro([FromBody] GrantLogroRequest request)
        {
            if (request == null || request.UserId <= 0 || request.LogroId <= 0)
            {
                return BadRequest(new { message = "UserId y LogroId son requeridos" });
            }

            // Check if user already has this logro
            var hasLogro = await _logros_UsuarioRepository.UserHasLogro(request.UserId, request.LogroId);
            if (hasLogro)
            {
                return Ok(new { message = "El usuario ya tiene este logro", alreadyHas = true });
            }

            // Grant the logro
            var logroUsuario = new Logros_Usuario
            {
                Id_Usuario = request.UserId,
                Id_Logro = request.LogroId,
                Fecha_Obtencion = DateTime.Now
            };

            var created = await _logros_UsuarioRepository.InsertLogrosUsuario(logroUsuario);
            
            if (!created)
            {
                return StatusCode(500, new { message = "Error al otorgar el logro" });
            }

            return Ok(new { message = "Logro otorgado exitosamente", alreadyHas = false });
        }

        public class GrantLogroRequest
        {
            public int UserId { get; set; }
            public int LogroId { get; set; }
        }
    }
}
