using System;
using System.ComponentModel.DataAnnotations;

namespace DorjaModelado
{
    public class MensajeContacto
    {
        public int Id { get; set; }
        
        [Required]
        public int EstudianteId { get; set; }
        
        [Required]
        public int MaestroId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string NombreEstudiante { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string CorreoEstudiante { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string Asunto { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(5000)]
        public string Mensaje { get; set; } = string.Empty;
        
        public string FechaEnvio { get; set; } = string.Empty;
        
        public int TienePdf { get; set; }
        
        public string? PdfBase64 { get; set; }
    }
}
