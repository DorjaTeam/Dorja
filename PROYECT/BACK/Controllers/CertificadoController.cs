using DorjaData.Repositories;
using DorjaModelado;
using Microsoft.AspNetCore.Mvc;

namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CertificadoController : ControllerBase
    {
        private readonly ICertificadosRepository _certificadosRepository;

        public CertificadoController(ICertificadosRepository certificadosRepository)
        {
            _certificadosRepository = certificadosRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCertificados()
        {
            return Ok(await _certificadosRepository.GetAllCertificados());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            return Ok(await _certificadosRepository.GetDetails(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNiveles([FromBody] Certificados certificados)
        {
            if (certificados == null)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _certificadosRepository.InsertCertificados(certificados);
            return Created("created", created);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateNiveles([FromBody] Certificados certificados)
        {
            if (certificados == null)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _certificadosRepository.UpdateCertificados(certificados);
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteNiveles(int id)
        {
            await _certificadosRepository.DeleteCertificados(new Certificados { idCertificados = id });
            return NoContent();
        }
    }
}
