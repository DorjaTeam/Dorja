using System;
using System.Collections.Generic;

namespace DorjaModelado
{
    public class SyllabusSection
    {
        public int Id { get; set; }
        public int? ParentSectionId { get; set; } // For subsections (2.1, 2.2, etc.)
        public string Codigo { get; set; } = string.Empty; // "2", "2.1", "2.2", etc.
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Orden { get; set; } // Order within parent or root
        public int NivelId { get; set; } // Which level this belongs to
        public bool RequiereCompletarAnterior { get; set; } = true;
    }
}

