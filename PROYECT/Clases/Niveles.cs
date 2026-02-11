using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaModelado
{
    public class Niveles
    {
        public int IdNiveles { get; set; }
        public string NombreNivel { get; set; } = string.Empty;
        public string DescripcionNivel { get; set; } = string.Empty;
        public string dificultad { get; set; } = string.Empty;
        public int orden { get; set; }
        public int puntosRequeridos { get; set; }
    }
}
