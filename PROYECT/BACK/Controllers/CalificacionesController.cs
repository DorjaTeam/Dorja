using Microsoft.AspNetCore.Mvc;
using DorjaModelado;
using DorjaModelado.Repositories;
using System;
using System.Threading.Tasks;

namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalificacionesController : ControllerBase
    {
        private readonly ICalificacionesRepository _calificacionesRepository;
        private readonly IUserRepository _usersRepository;

        public CalificacionesController(
            ICalificacionesRepository calificacionesRepository,
            IUserRepository usersRepository)
        {
            _calificacionesRepository = calificacionesRepository;
            _usersRepository = usersRepository;
        }

        // GET api/Calificaciones/estudiante/{estudianteId}
        [HttpGet("estudiante/{estudianteId}")]
        public async Task<IActionResult> GetByEstudiante(int estudianteId)
        {
            try
            {
                var calificaciones = await _calificacionesRepository.GetByEstudiante(estudianteId);
                return Ok(calificaciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al obtener calificaciones: {ex.Message}" });
            }
        }

        // GET api/Calificaciones/maestro/{maestroId}
        [HttpGet("maestro/{maestroId}")]
        public async Task<IActionResult> GetByMaestro(int maestroId)
        {
            try
            {
                var calificaciones = await _calificacionesRepository.GetByMaestro(maestroId);
                return Ok(calificaciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al obtener calificaciones: {ex.Message}" });
            }
        }

        // GET api/Calificaciones/maestro/{maestroId}/estudiante/{estudianteId}
        [HttpGet("maestro/{maestroId}/estudiante/{estudianteId}")]
        public async Task<IActionResult> GetByMaestroAndEstudiante(int maestroId, int estudianteId)
        {
            try
            {
                var calificacion = await _calificacionesRepository.GetByMaestroAndEstudiante(maestroId, estudianteId);
                if (calificacion == null)
                    return NotFound(new { message = "No se encontró calificación para este par maestro-estudiante" });
                return Ok(calificacion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // POST api/Calificaciones
        [HttpPost]
        public async Task<IActionResult> SaveCalificacion([FromBody] CalificacionRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Datos inválidos" });

            if (request.MaestroId <= 0 || request.EstudianteId <= 0)
                return BadRequest(new { message = "IDs de maestro y estudiante son requeridos" });

            if (request.Valor < 0 || request.Valor > 100)
                return BadRequest(new { message = "El valor de la calificación debe estar entre 0 y 100" });

            try
            {
                // Verify the maestro has the correct role
                var maestro = await _usersRepository.GetDetails(request.MaestroId);
                if (maestro == null)
                    return NotFound(new { message = "Maestro no encontrado" });

                if (maestro.Rol?.ToLower() != "maestro")
                    return Forbid();

                // Verify the student exists
                var estudiante = await _usersRepository.GetDetails(request.EstudianteId);
                if (estudiante == null)
                    return NotFound(new { message = "Estudiante no encontrado" });

                var calificacion = new Calificacion
                {
                    MaestroId = request.MaestroId,
                    EstudianteId = request.EstudianteId,
                    Valor = request.Valor,
                    Comentario = request.Comentario ?? string.Empty,
                    Fecha = DateTime.Now
                };

                var success = await _calificacionesRepository.Upsert(calificacion);

                if (!success)
                    return StatusCode(500, new { message = "Error al guardar la calificación" });

                return Ok(new
                {
                    message = "Calificación guardada correctamente",
                    calificacion = new
                    {
                        calificacion.MaestroId,
                        calificacion.EstudianteId,
                        calificacion.Valor,
                        calificacion.Comentario,
                        calificacion.Fecha
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al guardar la calificación: {ex.Message}" });
            }
        }

        // DELETE api/Calificaciones/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCalificacion(int id)
        {
            try
            {
                var success = await _calificacionesRepository.Delete(id);
                if (!success)
                    return NotFound(new { message = "Calificación no encontrada" });
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // Helper class for request body
        public class CalificacionRequest
        {
            public int MaestroId { get; set; }
            public int EstudianteId { get; set; }
            public double Valor { get; set; }
            public string? Comentario { get; set; }
        }
    }
}
