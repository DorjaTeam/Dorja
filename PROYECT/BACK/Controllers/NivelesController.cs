using DorjaData.Repositories;
using DorjaModelado;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class NivelesController : ControllerBase
    {
        private readonly INivelesRepository _nivelesRepository;

        public NivelesController(INivelesRepository nivelesRepository)
        {
            _nivelesRepository = nivelesRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllNiveles()
        {
            return Ok(await _nivelesRepository.GetAllNiveles());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            return Ok(await _nivelesRepository.GetDetails(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNiveles([FromBody] Niveles nivel)
        {
            if (nivel == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var created = await _nivelesRepository.InsertNiveles(nivel);
            return Created("created", created);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateNiveles([FromBody] Niveles nivel)
        {
            if (nivel == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _nivelesRepository.UpdateNiveles(nivel);
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteNiveles(int id)
        {
            await _nivelesRepository.DeleteNiveles(new Niveles {IdNiveles = id });
            return NoContent();
        }
    }
}
