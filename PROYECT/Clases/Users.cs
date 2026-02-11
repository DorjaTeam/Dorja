using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaModelado
{
    public class Users
    {
        public int Id { get; set; }  // coincidencia exacta con la columna 'id'
        public string Username { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ProfilePhotoPath { get; set; } = string.Empty;
        public string CoverPhotoPath { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }
        public DateTime? UltimaConexion { get; set; }  // Nullable porque puede ser NULL en DB

        public int PuntosTotales { get; set; }
        public int NivelActual { get; set; }
    }
}
