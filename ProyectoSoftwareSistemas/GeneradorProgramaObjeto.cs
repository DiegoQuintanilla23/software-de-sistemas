using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoSoftwareSistemas
{
    public class GeneradorProgramaObjeto
    {
        private List<LineaIntermedia> _lineas;
        private Dictionary<string, string> _tablaSimbolos;

        private bool EsFormato1(string codop)
        {
            return new[] { "FIX", "FLOAT", "HIO", "NORM", "SIO", "TIO" }
                .Contains(codop);
        }

        private bool EsFormato2(string codop)
        {
            return new[]
            {
                "ADDR", "CLEAR", "COMPR", "DIVR",
                "MULR", "RMO", "SHIFTL", "SHIFTR",
                "SUBR", "SVC", "TIXR"
            }.Contains(codop);
        }

        private bool EsFormato3(string codop)
        {
            return new[]
            {
                "ADD", "ADDF", "AND", "COMP", "COMPF",
                "DIV", "DIVF", "J", "JEQ", "JGT", "JLT", "JSUB",
                "LDA", "LDB", "LDCH", "LDF", "LDL", "LDS",
                "LDT", "LDX", "LPS",
                "MUL", "MULF", "OR",
                "RD", "RSUB",
                "STA", "STB", "STCH", "STF", "STI", "STL",
                "STS", "STSW", "STT", "STX",
                "SUB", "SUBF",
                "TD", "TIX", "WD"
            }.Contains(codop);
        }

        private bool EsInstruccion(string codop)
        {
            if (string.IsNullOrWhiteSpace(codop))
                return false;

            codop = codop.TrimStart('+');

            return EsFormato1(codop) || EsFormato2(codop) || EsFormato3(codop);
        }

        public GeneradorProgramaObjeto(List<LineaIntermedia> lineas, Dictionary<string, string> tablaSimbolos)
        {
            _lineas = lineas;
            _tablaSimbolos = tablaSimbolos;
        }

        public List<string> Generar()
        {
            List<string> archivoObjeto = new List<string>();
            List<string> regT = new List<string>();
            List<string> regM = new List<string>();

            string nomPrograma = "PROGRA";
            int direccionInicial = 0;
            //contador de programa - direccion inicial
            int longPrograma = 0;

            var startLine = _lineas.FirstOrDefault(l => l.CodigoOp == "START");
            if (startLine != null)
            {
                if(!string.IsNullOrWhiteSpace(startLine.Etiqueta))
                    nomPrograma = startLine.Etiqueta;
                if (!string.IsNullOrWhiteSpace(startLine.ContadorPrograma))
                    direccionInicial = Convert.ToInt32(startLine.ContadorPrograma, 16);
            }


            //ajustar el nombre del programa a 6 caracteres
            nomPrograma = nomPrograma.PadRight(6, ' ').Substring(0, 6);

            //calcular la longitud del programa
            var lastLine = _lineas.LastOrDefault(l => !string.IsNullOrWhiteSpace(l.ContadorPrograma));
            if(lastLine != null)
            {
                int direccionFinal = Convert.ToInt32(lastLine.ContadorPrograma, 16);
                longPrograma = direccionFinal - direccionInicial;
            }

            string registroH = $"H{nomPrograma}{direccionInicial:X6}{longPrograma:X6}";
            archivoObjeto.Add(registroH);

            //Registros de texto (T) y modificacion (M)
            string currentTCode= "";
            int currentTStart = -1;

            Action FlushTRecord = () =>
            {
                if(currentTCode.Length > 0)
                {
                    int length = currentTCode.Length / 2;
                    string regTEntry = $"T{currentTStart:X6}{length:X2}{currentTCode}";
                    regT.Add(regTEntry);
                    currentTCode = "";
                    currentTStart = -1;
                }
            };

            foreach(var linea in _lineas)
            {
                if(linea.CodigoOp == "RESW" || linea.CodigoOp == "RESB" || linea.CodigoOp == "ORG")
                {
                    FlushTRecord();
                    continue;
                }

                string objeto = linea.CodigoObjeto;

                //Ignorar lineas sin codigo objeto
                if(string.IsNullOrWhiteSpace(objeto) || objeto.StartsWith("-") || objeto.StartsWith("***"))
                    continue;

                //detectar si requiere un registro de modificacion
                bool requiereM = objeto.EndsWith("*");
                if(requiereM)
                    objeto = objeto.TrimEnd('*');

                if(currentTStart == -1)
                    currentTStart = Convert.ToInt32(linea.ContadorPrograma, 16);

                //Un registro de texto no puede exceder los 30 bytes (60 caracteres hex)
                if(currentTCode.Length + objeto.Length > 60)
                {
                    FlushTRecord();
                    currentTStart = Convert.ToInt32(linea.ContadorPrograma, 16);
                }

                currentTCode += objeto;

                //Si es formato 4, agregar un registro de modificacion
                if(requiereM)
                {
                    //La modificacion empieza en el medio byte 3 (Dirreccion + 1)
                    int mAddr = Convert.ToInt32(linea.ContadorPrograma, 16) + 1;
                    regM.Add($"M{mAddr:X6}05+{nomPrograma}");
                }
            }

            //Vaciar cualquier registro de texto pendiente
            FlushTRecord();

            //Registro de fin (E)
            int direccionEjecucion = direccionInicial;
            var endLine = _lineas.FirstOrDefault(l => l.CodigoOp == "END");
            if(endLine != null && !string.IsNullOrWhiteSpace(endLine.Operador))
            {
                if(_tablaSimbolos.ContainsKey(endLine.Operador))
                    direccionEjecucion = Convert.ToInt32(_tablaSimbolos[endLine.Operador], 16);
            }
            else
            {
                // Buscar la primera instrucción válida con código objeto
                var primeraInstruccion = _lineas.FirstOrDefault(l =>
                    !string.IsNullOrWhiteSpace(l.CodigoObjeto) &&
                    !l.CodigoObjeto.StartsWith("-") &&
                    !l.CodigoObjeto.StartsWith("***") &&
                    EsInstruccion(l.CodigoOp)
                );

                if (primeraInstruccion != null)
                    direccionEjecucion = Convert.ToInt32(primeraInstruccion.ContadorPrograma, 16);
            }

            string regE = $"E{direccionEjecucion:X6}";

            //ensablar el archivo final
            archivoObjeto.AddRange(regT);
            archivoObjeto.AddRange(regM);
            archivoObjeto.Add(regE);

            return archivoObjeto;
        }
    }
}
