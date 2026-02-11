using System;
using System.Collections.Generic;
using System.Text;

namespace DorjaModelado
{
    public class Certificados
    {
        public int idCertificados {  get; set; }
        public int Id_User { get; set; } //FK
        public int Nivel_Id { get; set; } //FK
        public string rutaPDF { get; set; } = string.Empty;
        public DateTime fechaGenerado { get; set; }
    }
}
