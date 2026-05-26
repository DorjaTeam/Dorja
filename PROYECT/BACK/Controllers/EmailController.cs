using Microsoft.AspNetCore.Mvc;
using DorjaModelado;
using DorjaModelado.Repositories;
using BACK.Services;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BACK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IMensajesContactoRepository _mensajesContactoRepository;
        private readonly IUserRepository _userRepository;

        // In-memory rate limiting dictionary: userId -> last sent time
        private static readonly ConcurrentDictionary<int, DateTime> _rateLimits = new();
        private static readonly TimeSpan RateLimitPeriod = TimeSpan.FromSeconds(30);

        public EmailController(
            IEmailService emailService,
            IMensajesContactoRepository mensajesContactoRepository,
            IUserRepository userRepository)
        {
            _emailService = emailService;
            _mensajesContactoRepository = mensajesContactoRepository;
            _userRepository = userRepository;
        }

        // POST api/Email/send
        [HttpPost("send")]
        public async Task<IActionResult> SendContactEmail([FromBody] SendEmailRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Datos inválidos" });

            if (request.EstudianteId <= 0 || request.MaestroId <= 0)
                return BadRequest(new { success = false, message = "IDs de estudiante y maestro son requeridos" });

            if (string.IsNullOrEmpty(request.Asunto) || string.IsNullOrEmpty(request.Mensaje))
                return BadRequest(new { success = false, message = "El asunto y el mensaje son requeridos" });

            // In-memory rate limiting check
            if (_rateLimits.TryGetValue(request.EstudianteId, out var lastSent))
            {
                var timeSinceLast = DateTime.UtcNow - lastSent;
                if (timeSinceLast < RateLimitPeriod)
                {
                    var waitSecs = Math.Ceiling((RateLimitPeriod - timeSinceLast).TotalSeconds);
                    return StatusCode(429, new { 
                        success = false, 
                        message = $"Por favor espera {waitSecs} segundos antes de enviar otro mensaje académico." 
                    });
                }
            }

            try
            {
                // Verify student exists
                var student = await _userRepository.GetDetails(request.EstudianteId);
                if (student == null)
                    return NotFound(new { success = false, message = "Estudiante no encontrado" });

                var studentName = $"{student.Nombre} {student.ApellidoPaterno} {student.ApellidoMaterno}".Trim();
                var studentEmail = student.Email;

                // Verify teacher exists
                var teacher = await _userRepository.GetDetails(request.MaestroId);
                if (teacher == null)
                    return NotFound(new { success = false, message = "Profesor no encontrado" });

                if (teacher.Rol?.ToLower() != "maestro")
                    return BadRequest(new { success = false, message = "El usuario seleccionado no es un maestro" });

                // Update rate limit timestamp
                _rateLimits[request.EstudianteId] = DateTime.UtcNow;

                // Handle attachment if base64 pdf is supplied
                byte[]? pdfBytes = null;
                string? attachmentName = null;
                bool tienePdf = false;

                if (!string.IsNullOrEmpty(request.PdfBase64))
                {
                    try
                    {
                        var base64Data = request.PdfBase64;
                        if (base64Data.Contains(","))
                        {
                            base64Data = base64Data.Split(',')[1];
                        }
                        pdfBytes = Convert.FromBase64String(base64Data);
                        attachmentName = $"Certificado_{student.Nombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
                        tienePdf = true;
                    }
                    catch (Exception pdfEx)
                    {
                        Console.WriteLine($"⚠️ Warning parsing PDF Base64: {pdfEx.Message}");
                        // Continue sending email even if PDF attachment parsing fails
                    }
                }

                // Academic HTML Email body template
                string formattedBody = $@"
                <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(73,41,164,0.08); border: 1px solid #eef2f6;'>
                    <div style='background: linear-gradient(135deg, #1a0f3c, #4929a4); padding: 40px 20px; text-align: center;'>
                        <div style='width: 64px; height: 64px; background: linear-gradient(135deg, #4929a4, #8a5df5); border: 1px solid rgba(255,255,255,0.2); border-radius: 16px; display: inline-flex; align-items: center; justify-content: center; color: white; font-size: 28px; font-weight: 800; margin-bottom: 20px; box-shadow: 0 8px 16px rgba(0,0,0,0.2);'>D</div>
                        <h2 style='margin: 0; color: #ffffff; font-size: 26px; font-weight: 800; letter-spacing: -0.5px;'>Mensaje Académico</h2>
                    </div>
                    <div style='padding: 40px 30px; color: #334155;'>
                        <p style='font-size: 16px; margin-bottom: 20px;'><strong>Estimado(a) Prof. {teacher.Nombre} {teacher.ApellidoPaterno},</strong></p>
                        <p style='font-size: 15px; line-height: 1.6; color: #475569; margin-bottom: 24px;'>Has recibido una nueva consulta de un estudiante a través de la plataforma Dorja.</p>
                        
                        <div style='background-color: #f8fafc; border-radius: 12px; padding: 20px; margin-bottom: 24px; border: 1px solid #e2e8f0;'>
                            <h3 style='margin-top: 0; margin-bottom: 12px; font-size: 14px; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;'>Detalles del Estudiante</h3>
                            <ul style='list-style-type: none; padding-left: 0; margin: 0;'>
                                <li style='margin-bottom: 8px;'><strong style='color: #1a0f3c;'>Nombre:</strong> {studentName}</li>
                                <li><strong style='color: #1a0f3c;'>Correo:</strong> {studentEmail}</li>
                            </ul>
                        </div>
                        
                        <h3 style='margin-top: 0; margin-bottom: 12px; font-size: 16px; color: #1a0f3c;'>Asunto: {request.Asunto}</h3>
                        
                        <div style='background-color: #f1f5f9; border-left: 4px solid #4929a4; padding: 20px; margin: 15px 0; border-radius: 0 8px 8px 0; font-size: 15px; line-height: 1.6; color: #334155; white-space: pre-line;'>
                            {request.Mensaje}
                        </div>
                        
                        {(tienePdf ? "<div style='margin-top: 24px; padding: 16px; background-color: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 8px; color: #166534; font-size: 14px;'>📎 <em>Se ha adjuntado el certificado de finalización de curso del estudiante a este correo.</em></div>" : "")}
                    </div>
                    <div style='background-color: #f8fafc; padding: 24px; text-align: center; border-top: 1px solid #eef2f6;'>
                        <p style='margin: 0; color: #94a3b8; font-size: 13px;'>Este es un mensaje generado automáticamente por la plataforma educativa Dorja.</p>
                        <p style='margin: 8px 0 0 0; color: #cbd5e1; font-size: 12px;'>© 2026 Dorja. Todos los derechos reservados.</p>
                    </div>
                </div>";

                // Save message to database
                var dbMessage = new MensajeContacto
                {
                    EstudianteId = request.EstudianteId,
                    MaestroId = request.MaestroId,
                    NombreEstudiante = studentName,
                    CorreoEstudiante = studentEmail,
                    Asunto = request.Asunto,
                    Mensaje = request.Mensaje,
                    FechaEnvio = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TienePdf = tienePdf ? 1 : 0,
                    PdfBase64 = request.PdfBase64
                };

                await _mensajesContactoRepository.Insert(dbMessage);

                // Send actual email using MailKit
                await _emailService.SendEmailAsync(
                    teacher.Email, 
                    $"{teacher.Nombre} {teacher.ApellidoPaterno}", 
                    $"[Dorja] {request.Asunto}", 
                    formattedBody, 
                    pdfBytes, 
                    attachmentName
                );

                return Ok(new { success = true, message = "El mensaje ha sido enviado exitosamente." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending email endpoint: {ex.Message}");
                return StatusCode(500, new { success = false, message = $"Error al procesar el envío: {ex.Message}" });
            }
        }

        // GET api/Email/messages/{teacherId}
        [HttpGet("messages/{teacherId}")]
        public async Task<IActionResult> GetTeacherMessages(int teacherId)
        {
            try
            {
                var teacher = await _userRepository.GetDetails(teacherId);
                if (teacher == null)
                    return NotFound(new { success = false, message = "Profesor no encontrado" });

                if (teacher.Rol?.ToLower() != "maestro")
                    return Forbid();

                var messages = await _mensajesContactoRepository.GetByTeacher(teacherId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener mensajes: {ex.Message}" });
            }
        }

        // Request Class Model
        public class SendEmailRequest
        {
            public int EstudianteId { get; set; }
            public string NombreEstudiante { get; set; } = string.Empty;
            public string CorreoEstudiante { get; set; } = string.Empty;
            public int MaestroId { get; set; }
            public string Asunto { get; set; } = string.Empty;
            public string Mensaje { get; set; } = string.Empty;
            public string? PdfBase64 { get; set; }
        }
    }
}
