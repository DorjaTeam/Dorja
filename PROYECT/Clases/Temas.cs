using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaModelado
{
    public class Temas
    {
        public int IdTemas { get; set; }
        public int IdNivel { get; set; } // FK

        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Orden { get; set; }

        public bool Locked { get; set; }   
        public int PuntosRequeridos { get; set; }
    }

}
