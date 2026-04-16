using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoSoftwareSistemas
{
    public class Bloque
    {
        public int Numero { get; set; }
        public string Nombre { get; set; }
        public int Locctr { get; set; } = 0;     // contador interno
        public int Longitud { get; set; } = 0;
        public int DirInicial { get; set; } = 0;
    }
}
