using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DorjaModelado
{
    public class Users
    {
        public int Id { get; set; }  // coincidencia exacta con la columna 'id'
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string ApellidoMaterno { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        
        public string Password { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string ProfilePhotoPath { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string CoverPhotoPath { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? GoogleId { get; set; }

        public DateTime FechaRegistro { get; set; }
        public DateTime? UltimaConexion { get; set; }  // Nullable porque puede ser NULL en DB

        public int PuntosTotales { get; set; }
        public int NivelActual { get; set; }

        /// <summary>
        /// Rol del usuario: "estudiante" (default) o "maestro"
        /// </summary>
        [Required]
        [RegularExpression("^(estudiante|maestro)$", ErrorMessage = "El rol debe ser estudiante o maestro")]
        public string Rol { get; set; } = "estudiante";
    }
}
