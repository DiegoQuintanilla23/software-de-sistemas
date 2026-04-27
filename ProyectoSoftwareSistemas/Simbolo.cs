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

        // R = relativo, A = absoluto, E = externo
        public string Tipo { get; set; } = "R";

        public string Bloque { get; set; } = "DEFAULT";
    }
}
