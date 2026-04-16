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
        private Dictionary<string, Simbolo> _tablaSimbolos;
        private Dictionary<string, Bloque> _tablaBloques;

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

        public GeneradorProgramaObjeto(List<LineaIntermedia> lineas, Dictionary<string, Simbolo> tablaSimbolos, Dictionary<string, Bloque> tabblk)
        {
            _lineas = lineas;
            _tablaSimbolos = tablaSimbolos;
            _tablaBloques = tabblk;

        }

        public List<string> Generar()
        {
            List<string> archivoObjeto = new List<string>();
            List<string> regT = new List<string>();
            List<string> regM = new List<string>();

            string nomPrograma = "PROGRA";
            int direccionInicial = 0;
            int longPrograma = 0;

            var startLine = _lineas.FirstOrDefault(l => l.CodigoOp == "START");
            if (startLine != null)
            {
                if (!string.IsNullOrWhiteSpace(startLine.Etiqueta))
                    nomPrograma = startLine.Etiqueta;

                if (!string.IsNullOrWhiteSpace(startLine.ContadorPrograma))
                    direccionInicial = Convert.ToInt32(startLine.ContadorPrograma, 16);
            }

            nomPrograma = nomPrograma.PadRight(6, ' ').Substring(0, 6);

            int maxFinal = 0;
            foreach (var b in _tablaBloques.Values)
            {
                int fin = b.DirInicial + b.Longitud;
                if (fin > maxFinal)
                    maxFinal = fin;
            }
            longPrograma = maxFinal - direccionInicial;

            string registroH = $"H{nomPrograma}{direccionInicial:X6}{longPrograma:X6}";
            archivoObjeto.Add(registroH);

            string currentTCode = "";
            int currentTStart = -1;
            int bloqueActual = -1;

            Action FlushTRecord = () =>
            {
                if (currentTCode.Length > 0)
                {
                    int length = currentTCode.Length / 2;
                    string regTEntry = $"T{currentTStart:X6}{length:X2}{currentTCode}";
                    regT.Add(regTEntry);
                    currentTCode = "";
                    currentTStart = -1;
                }
            };

            foreach (var linea in _lineas)
            {
                if (linea.CodigoOp == "RESW" || linea.CodigoOp == "RESB" || linea.CodigoOp == "ORG")
                {
                    FlushTRecord();
                    bloqueActual = -1;
                    continue;
                }

                string objeto = linea.CodigoObjeto;

                if (string.IsNullOrWhiteSpace(objeto) || objeto.StartsWith("-") || objeto.StartsWith("***"))
                    continue;

                bool requiereM = objeto.EndsWith("*");
                if (requiereM)
                    objeto = objeto.TrimEnd('*');

                int cp = Convert.ToInt32(linea.ContadorPrograma, 16);
                var bloque = _tablaBloques.Values.FirstOrDefault(b => b.Numero == linea.NumeroBloque);
                int dirInicial = bloque != null ? bloque.DirInicial : 0;

                int direccionAbsoluta = cp + dirInicial;

                // corte por cambio de bloque
                if (bloqueActual != linea.NumeroBloque)
                {
                    FlushTRecord();
                    currentTStart = direccionAbsoluta;
                    bloqueActual = linea.NumeroBloque;
                }

                if (currentTStart == -1)
                    currentTStart = direccionAbsoluta;

                // corte por tamaño
                if (currentTCode.Length + objeto.Length > 60)
                {
                    FlushTRecord();
                    currentTStart = direccionAbsoluta;
                }

                currentTCode += objeto;

                if (requiereM)
                {
                    if (linea.CodigoOp == "WORD")
                    {
                        regM.Add($"M{direccionAbsoluta:X6}06+{nomPrograma}");
                    }
                    else
                    {
                        regM.Add($"M{(direccionAbsoluta + 1):X6}05+{nomPrograma}");
                    }
                }
            }

            FlushTRecord();

            int direccionEjecucion = direccionInicial;
            var endLine = _lineas.FirstOrDefault(l => l.CodigoOp == "END");

            if (endLine != null && !string.IsNullOrWhiteSpace(endLine.Operador))
            {
                string simbolo = endLine.Operador.Trim().ToUpper();

                if (_tablaSimbolos.ContainsKey(simbolo))
                {
                    var simb = _tablaSimbolos[simbolo];

                    direccionEjecucion = simb.Direccion;

                    if (_tablaBloques.ContainsKey(simb.Bloque))
                        direccionEjecucion += _tablaBloques[simb.Bloque].DirInicial;
                }
                else
                {
                    direccionEjecucion = 0xFFFFFF;
                    //AgregarError(endLine, $"Símbolo no definido: {simbolo}");
                }
            }
            else
            {
                var primeraInstruccion = _lineas.FirstOrDefault(l =>
                    !string.IsNullOrWhiteSpace(l.CodigoObjeto) &&
                    !l.CodigoObjeto.StartsWith("-") &&
                    !l.CodigoObjeto.StartsWith("***") &&
                    EsInstruccion(l.CodigoOp)
                );

                if (primeraInstruccion != null)
                {
                    int cp = Convert.ToInt32(primeraInstruccion.ContadorPrograma, 16);
                    var bloque = _tablaBloques.Values.FirstOrDefault(b => b.Numero == primeraInstruccion.NumeroBloque);
                    int dirInicial = bloque != null ? bloque.DirInicial : 0;

                    direccionEjecucion = cp + dirInicial;
                }
            }

            string regE = $"E{direccionEjecucion:X6}";

            archivoObjeto.AddRange(regT);
            archivoObjeto.AddRange(regM);
            archivoObjeto.Add(regE);

            return archivoObjeto;
        }
    }
}
