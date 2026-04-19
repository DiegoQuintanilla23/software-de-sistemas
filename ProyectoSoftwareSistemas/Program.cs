using System;
using System.Windows.Forms;
using Antlr4.Runtime;

namespace ProyectoSoftwareSistemas
{
    // Si tus listeners siguen aquí, el using de Antlr4 es obligatorio.

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InterfazSicXE());
        }
    }
}