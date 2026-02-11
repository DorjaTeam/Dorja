using DorjaData.Repositories;
using DorjaModelado;
using DorjaModelado.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemasController : ControllerBase
    {
        private readonly ITemasRepository _temasRepository;

        public TemasController(ITemasRepository temasRepository)
        {
            _temasRepository = temasRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTemas()
        {
            return Ok(await _temasRepository.GetAllTemas());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            return Ok(await _temasRepository.GetDetails(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTemas([FromBody] Temas temas)
        {
            if (temas == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _temasRepository.InsertTemas(temas);

            return Created("created", created);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTemas([FromBody] Temas temas)
        {
            if (temas == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _temasRepository.UpdateTemas(temas);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemas(int id)
        {
            var temasToDelete = await _temasRepository.GetDetails(id);
            if (temasToDelete == null)
            {
                return NotFound();
            }
            await _temasRepository.DeleteTemas(new Temas {IdTemas = id });
            return NoContent();
        }
    }
}
