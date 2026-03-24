using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoSoftwareSistemas
{
    public class Simbolo
    {
        public string Nombre { get; set; }
        public int Direccion { get; set; }
        public bool EsRelativo { get; set; } = true;
        public string Tipo { get; set; } = "R"; // R = relativo, A = absoluto
        public string Bloque { get; set; } = "DEFAULT";
    }
}
