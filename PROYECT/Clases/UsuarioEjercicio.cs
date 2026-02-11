using System;

namespace DorjaModelado
{
    // Tracks which exercises each user has seen/attempted to prevent repeats
    public class UsuarioEjercicio
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProblemaId { get; set; }
        public int SyllabusSectionId { get; set; } // Which section this exercise belongs to
        public DateTime FechaAsignado { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public bool Completado { get; set; } = false;
        public bool EsUnico { get; set; } = true; // Ensures this exercise is unique to this user
        public string ParametrosUnicos { get; set; } = string.Empty; // For parameterized exercises
    }
}

