using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaModelado
{
    public class Problema
    {
        public int Id { get; set; }                  // id INT AUTO_INCREMENT PRIMARY KEY
        public int TemaId { get; set; }             // tema_id INT (FK)
        public string Titulo { get; set; } = string.Empty;        // titulo VARCHAR(150) NOT NULL
        public string Descripcion { get; set; } = string.Empty;  // descripcion TEXT
        public string Ejemplo { get; set; } = string.Empty;      // ejemplo TEXT
        public string Dificultad { get; set; } = string.Empty;   // dificultad VARCHAR(50)
        public string CodigoInicial { get; set; } = string.Empty; // codigo_inicial TEXT
        public string Solucion { get; set; } = string.Empty;     // solucion TEXT
        public int Orden { get; set; }                            // orden INT
        public bool Locked { get; set; } = true;                 // locked BOOLEAN DEFAULT TRUE
        public int PuntosOtorgados { get; set; } = 10;       // puntos_otorgados INT DEFAULT 10
    }
}
