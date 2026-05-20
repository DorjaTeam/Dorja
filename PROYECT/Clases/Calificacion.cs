using System;

namespace DorjaModelado
{
    public class Calificacion
    {
        public int Id { get; set; }

        /// <summary>ID del maestro que asigna la calificación</summary>
        public int MaestroId { get; set; }

        /// <summary>ID del estudiante que recibe la calificación</summary>
        public int EstudianteId { get; set; }

        /// <summary>Valor numérico de la calificación (ej. 0-10 o 0-100)</summary>
        public double Valor { get; set; }

        /// <summary>Comentario opcional del maestro</summary>
        public string Comentario { get; set; } = string.Empty;

        /// <summary>Fecha en que se asignó o actualizó la calificación</summary>
        public DateTime Fecha { get; set; }
    }
}
