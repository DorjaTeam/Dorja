using DorjaData.Repositories;
using DorjaModelado;
using Microsoft.AspNetCore.Mvc;

namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogrosController : ControllerBase
    {
        private readonly ILogrosRepository _logrosRepository;

        public LogrosController(ILogrosRepository logrosRepository)
        {
            _logrosRepository = logrosRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLogros()
        {
            return Ok(await _logrosRepository.GetAllLogros());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            return Ok(await _logrosRepository.GetDetails(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNiveles([FromBody] Logros logros)
        {
            if (logros == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var created = await _logrosRepository.InsertLogros(logros);
            return Created("created", created);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateNiveles([FromBody] Logros logros)
        {
            if (logros == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _logrosRepository.UpdateLogros(logros);
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteNiveles(int id)
        {
            await _logrosRepository.DeleteLogros(new Logros { Id = id });
            return NoContent();
        }

        [HttpGet("by-name/{nombre}")]
        public async Task<IActionResult> GetLogroByNombre(string nombre)
        {
            var logro = await _logrosRepository.GetLogroByNombre(nombre);
            if (logro == null)
            {
                return NotFound(new { message = "Logro no encontrado" });
            }
            return Ok(logro);
        }
    }
}
