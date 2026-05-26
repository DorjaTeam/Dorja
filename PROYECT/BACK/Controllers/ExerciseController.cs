using BACK.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExerciseController : ControllerBase
    {
        private readonly ExerciseService _exerciseService;

        public ExerciseController(ExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        /// <summary>
        /// Gets a random problem for the user based on their current level
        /// </summary>
        [HttpGet("random/{userId}")]
        public async Task<IActionResult> GetRandomProblem(int userId)
        {
            try
            {
                var problema = await _exerciseService.GetRandomProblemForUser(userId);
                return Ok(problema);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Validates a solution by executing the code and comparing outputs
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateSolution([FromBody] ValidateSolutionRequest request)
        {
            if (request == null || request.UserId <= 0 || request.ProblemaId <= 0 || string.IsNullOrEmpty(request.Codigo))
            {
                return BadRequest(new { message = "Datos inválidos" });
            }

            try
            {
                var result = await _exerciseService.ValidateSolution(
                    request.UserId, 
                    request.ProblemaId, 
                    request.Codigo, 
                    request.Language ?? "python",
                    request.TiempoInvertido,
                    request.Errores,
                    request.IntentosFallidos
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class ValidateSolutionRequest
    {
        public int UserId { get; set; }
        public int ProblemaId { get; set; }
        public string Codigo { get; set; } = "";
        public string? Language { get; set; }
        public int TiempoInvertido { get; set; } = 0;
        public int Errores { get; set; } = 0;
        public int IntentosFallidos { get; set; } = 0;
    }
}
