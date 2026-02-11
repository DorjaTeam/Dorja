using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DorjaModelado
{
    public class Progreso_Problema
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProblemaId { get; set; }
        public bool Completado { get; set; } = false;
        public int Puntuacion { get; set; } = 0;
        public int Intentos { get; set; } = 0;
        public string UltimoCodigo { get; set; } = string.Empty;
        public DateTime? FechaCompletado { get; set; }
    }
}
