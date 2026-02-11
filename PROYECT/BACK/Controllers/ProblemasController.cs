using DorjaData.Repositories;
using DorjaModelado;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProblemasController: ControllerBase
    {
        private readonly IProblemaRepository _problemaRepository;

        public ProblemasController(IProblemaRepository nivelesRepository)
        {
            _problemaRepository = nivelesRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProblems() 
        {

            return Ok(await _problemaRepository.GetAllProblemas());
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetProblemCount()
        {
            var allProblems = await _problemaRepository.GetAllProblemas();
            var problemList = allProblems.ToList();
            return Ok(new { 
                total = problemList.Count,
                problems = problemList.Take(50).Select(p => new { 
                    id = p.Id, 
                    temaId = p.TemaId, 
                    titulo = p.Titulo 
                })
            });
        }

        // IMPORTANT: Esta ruta debe ir ANTES de [HttpGet("{id}")] para evitar conflictos de enrutamiento
        [HttpGet("tema/{temaId}/random")]
        public async Task<IActionResult> GetRandomProblemasByTema(int temaId, [FromQuery] int count = 10, [FromQuery] int? userId = null)
        {
            try
            {
                Console.WriteLine($"🔄 GET /Problemas/tema/{temaId}/random?count={count}&userId={userId}");
                
                // Get all problems for this topic first to check availability
                var allProblemas = await _problemaRepository.GetProblemasByTema(temaId);
                var allProblemasList = allProblemas.ToList();
                Console.WriteLine($"📊 Total de problemas disponibles para tema {temaId}: {allProblemasList.Count} (solicitados: {count})");
                
                var problemas = await _problemaRepository.GetProblemasRandomByTema(temaId, count, userId);
                var problemasList = problemas.ToList();
                
                Console.WriteLine($"✅ Devolviendo {problemasList.Count} problemas aleatorios para tema {temaId} (solicitados: {count})");
                
                // Log problem IDs for debugging
                if (problemasList.Count > 0)
                {
                    var ids = string.Join(", ", problemasList.Select(p => p.Id));
                    Console.WriteLine($"📋 IDs de problemas devueltos: {ids}");
                }
                else
                {
                    Console.WriteLine($"⚠️ No se devolvieron problemas. Posibles razones:");
                    Console.WriteLine($"   - No hay problemas disponibles para tema {temaId}");
                    Console.WriteLine($"   - Todos los problemas ya están completados (userId: {userId})");
                    Console.WriteLine($"   - Todos los problemas están bloqueados");
                }
                
                return Ok(problemasList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener problemas aleatorios: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Error al obtener problemas aleatorios: {ex.Message}", stackTrace = ex.StackTrace });
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id) 
        { 
            var problema = await _problemaRepository.GetDetails(id);
            if (problema == null)
            {
                // Get all problems to help debug
                var allProblems = await _problemaRepository.GetAllProblemas();
                var problemList = allProblems.ToList();
                var problemIds = string.Join(", ", problemList.Take(20).Select(p => p.Id));
                return NotFound(new { 
                    message = $"Problema con ID {id} no encontrado",
                    totalProblems = problemList.Count,
                    availableIds = problemIds
                });
            }
            return Ok(problema);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProblema([FromBody] Problema problema)
        {
            if(problema == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid) 
            {
                return BadRequest(ModelState);
            }

            var created = await _problemaRepository.InsertProblemas(problema);
            if (!created)
            {
                return StatusCode(500, new { message = "Error al crear el problema" });
            }
            return Created("created", problema); 
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProblemas([FromBody] Problema problema)
        {
            if (problema == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _problemaRepository.UpdateProblemas(problema);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblema(int id)
        {
            var temasToDelete = await _problemaRepository.GetDetails(id);
            if (temasToDelete == null)
            {
                return NotFound();
            }
            await _problemaRepository.DeleteProblemas(new Problema { Id = id });
            return NoContent();
        }
    }
}
