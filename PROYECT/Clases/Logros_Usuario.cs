using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaModelado
{
    public class Logros_Usuario
    {
        public int id { get; set; } // PK
        public int Id_Usuario { get; set; } // FK Usuario
        public int Id_Logro { get; set; } // FK Logro
        public DateTime Fecha_Obtencion { get; set; } // Fecha en que se obtuvo el logro
    }
}
