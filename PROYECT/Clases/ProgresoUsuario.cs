using System;

namespace DorjaModelado
{
    // Tracks user's current position in the syllabus
    public class ProgresoUsuario
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SyllabusSectionId { get; set; } // Current section (e.g., 2.1)
        public int EjerciciosCompletadosEnSeccion { get; set; } = 0;
        public int EjerciciosRequeridosEnSeccion { get; set; } = 3; // Minimum exercises to complete before moving on
        public DateTime UltimaActualizacion { get; set; }
        public bool SeccionCompletada { get; set; } = false;
    }
}

