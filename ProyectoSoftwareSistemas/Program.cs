using System;
using System.Windows.Forms;
using Antlr4.Runtime;

namespace ProyectoSoftwareSistemas
{
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