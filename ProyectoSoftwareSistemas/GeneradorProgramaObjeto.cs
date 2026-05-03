using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoSoftwareSistemas
{
    public class GeneradorProgramaObjeto
    {
        private List<LineaIntermedia> _lineas;
        private List<Seccion> _secciones;

        private bool EsFormato1(string codop) => new[] { "FIX", "FLOAT", "HIO", "NORM", "SIO", "TIO" }.Contains(codop);
        private bool EsFormato2(string codop) => new[] { "ADDR", "CLEAR", "COMPR", "DIVR", "MULR", "RMO", "SHIFTL", "SHIFTR", "SUBR", "SVC", "TIXR" }.Contains(codop);
        private bool EsFormato3(string codop) => new[] { "ADD", "ADDF", "AND", "COMP", "COMPF", "DIV", "DIVF", "J", "JEQ", "JGT", "JLT", "JSUB", "LDA", "LDB", "LDCH", "LDF", "LDL", "LDS", "LDT", "LDX", "LPS", "MUL", "MULF", "OR", "RD", "RSUB", "STA", "STB", "STCH", "STF", "STI", "STL", "STS", "STSW", "STT", "STX", "SUB", "SUBF", "TD", "TIX", "WD" }.Contains(codop);

        private bool EsFormato4(string codop)
        {
            if (string.IsNullOrWhiteSpace(codop)) return false;
            return codop.StartsWith("+") && EsFormato3(codop.Substring(1));
        }

        private bool EsInstruccion(string codop)
        {
            if (string.IsNullOrWhiteSpace(codop)) return false;
            codop = codop.TrimStart('+');
            return EsFormato1(codop) || EsFormato2(codop) || EsFormato3(codop);
        }

        public GeneradorProgramaObjeto(List<LineaIntermedia> lineas, List<Seccion> secciones)
        {
            _lineas = lineas;
            _secciones = secciones;
        }

        public List<string> Generar()
        {
            List<string> archivoObjeto = new List<string>();

            int seccionIdx = 0;
            var tabSim = _secciones[seccionIdx].TABSIM;
            var tabBlk = _secciones[seccionIdx].TABBLK;
            var evaluador = new EvaluadorExpresiones(tabSim, tabBlk);

            // FIX: Obtener el nombre correcto de la sección (Prioriza la etiqueta de START)
            string nomPrograma = _secciones[seccionIdx].Nombre;
            if (seccionIdx == 0)
            {
                var startLine = _lineas.FirstOrDefault(l => l.CodigoOp == "START");
                if (startLine != null && !string.IsNullOrWhiteSpace(startLine.Etiqueta))
                    nomPrograma = startLine.Etiqueta.Trim();
            }
            nomPrograma = nomPrograma.PadRight(6, ' ').Substring(0, 6);
            int dirInicial = 0;

            // FIX: Unificamos D y R en una sola lista para intercalarlos fielmente al código fuente
            List<string> regDR = new List<string>();
            List<string> regT = new List<string>();
            List<string> regM = new List<string>();

            string currentTCode = "";
            int currentTStart = -1;
            int bloqueActual = -1;

            Action FlushTRecord = () =>
            {
                if (currentTCode.Length > 0)
                {
                    int length = currentTCode.Length / 2;
                    regT.Add($"T{currentTStart:X6}{length:X2}{currentTCode}");
                    currentTCode = "";
                    currentTStart = -1;
                }
            };

            Action CerrarSeccion = () =>
            {
                FlushTRecord();

                int maxFinal = tabBlk.Values.Max(b => b.DirInicial + b.Longitud);
                string registroH = $"H{nomPrograma}{dirInicial:X6}{(maxFinal - dirInicial):X6}";

                archivoObjeto.Add(registroH);
                archivoObjeto.AddRange(regDR);
                archivoObjeto.AddRange(regT);
                archivoObjeto.AddRange(regM);

                if (seccionIdx == 0)
                {
                    var endLine = _lineas.FirstOrDefault(l => l.CodigoOp == "END");
                    int dirEjecucion = dirInicial;

                    if (endLine != null && !string.IsNullOrWhiteSpace(endLine.Operador))
                    {
                        string sim = endLine.Operador.Trim().ToUpper();
                        if (tabSim.ContainsKey(sim))
                        {
                            dirEjecucion = tabSim[sim].Direccion;
                            if (tabBlk.ContainsKey(tabSim[sim].Bloque))
                                dirEjecucion += tabBlk[tabSim[sim].Bloque].DirInicial;
                        }
                    }
                    else
                    {
                        var pri = _lineas.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l.CodigoObjeto) && !l.CodigoObjeto.StartsWith("-") && !l.CodigoObjeto.StartsWith("*") && EsInstruccion(l.CodigoOp));
                        if (pri != null)
                        {
                            int cp = Convert.ToInt32(pri.ContadorPrograma, 16);
                            var b = tabBlk.Values.FirstOrDefault(bk => bk.Numero == pri.NumeroBloque);
                            dirEjecucion = cp + (b != null ? b.DirInicial : 0);
                        }
                    }
                    archivoObjeto.Add($"E{dirEjecucion:X6}");
                }
                else
                {
                    archivoObjeto.Add("E"); // Módulos secundarios llevan E vacío
                }

                regDR.Clear(); regT.Clear(); regM.Clear();
            };

            foreach (var linea in _lineas)
            {
                if (linea.CodigoOp == "START")
                {
                    if (!string.IsNullOrWhiteSpace(linea.Operador) && int.TryParse(linea.Operador, System.Globalization.NumberStyles.HexNumber, null, out int inicio))
                        dirInicial = inicio;
                    continue;
                }

                if (linea.CodigoOp == "CSECT")
                {
                    CerrarSeccion();

                    seccionIdx++;
                    if (seccionIdx < _secciones.Count)
                    {
                        tabSim = _secciones[seccionIdx].TABSIM;
                        tabBlk = _secciones[seccionIdx].TABBLK;
                        evaluador = new EvaluadorExpresiones(tabSim, tabBlk);
                        nomPrograma = _secciones[seccionIdx].Nombre.PadRight(6, ' ').Substring(0, 6);
                        dirInicial = 0;
                        bloqueActual = -1;
                    }
                    continue;
                }

                // ==============================
                // FIX: REGISTROS D y R INTERCALADOS
                // ==============================
                if (linea.CodigoOp == "EXTDEF")
                {
                    string currentD = "D";
                    var simbolos = linea.Operador.Split(',');
                    foreach (var s in simbolos)
                    {
                        string sim = s.Trim().ToUpper();
                        if (tabSim.ContainsKey(sim))
                        {
                            int dir = tabSim[sim].Direccion;
                            if (tabBlk.ContainsKey(tabSim[sim].Bloque))
                                dir += tabBlk[tabSim[sim].Bloque].DirInicial;

                            if (currentD.Length + 12 > 73)
                            {
                                regDR.Add(currentD);
                                currentD = "D";
                            }
                            currentD += $"{sim.PadRight(6, ' ').Substring(0, 6)}{dir:X6}";
                        }
                    }
                    if (currentD.Length > 1) regDR.Add(currentD);
                    continue;
                }

                if (linea.CodigoOp == "EXTREF")
                {
                    string currentR = "R";
                    var simbolos = linea.Operador.Split(',');
                    foreach (var s in simbolos)
                    {
                        string sim = s.Trim().ToUpper();
                        if (currentR.Length + 6 > 73)
                        {
                            regDR.Add(currentR);
                            currentR = "R";
                        }
                        currentR += sim.PadRight(6, ' ').Substring(0, 6);
                    }
                    if (currentR.Length > 1) regDR.Add(currentR);
                    continue;
                }

                // FIX: Ignorar EQU y USE para evitar que rompan los Registros T
                if (linea.CodigoOp == "RESW" || linea.CodigoOp == "RESB" || linea.CodigoOp == "ORG")
                {
                    FlushTRecord();
                    bloqueActual = -1;
                    continue;
                }
                if (linea.CodigoOp == "USE" || linea.CodigoOp == "EQU")
                {
                    continue;
                }

                string objetoOriginal = linea.CodigoObjeto;
                if (string.IsNullOrWhiteSpace(objetoOriginal) || objetoOriginal.StartsWith("-") || objetoOriginal.StartsWith("***"))
                    continue;

                string objeto = "";
                foreach (char c in objetoOriginal)
                {
                    if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                        objeto += c;
                    else
                        break;
                }

                if (objeto.Length == 0) continue;

                int cp = Convert.ToInt32(linea.ContadorPrograma, 16);
                var bloque = tabBlk.Values.FirstOrDefault(b => b.Numero == linea.NumeroBloque);
                int dirInicialBloque = bloque != null ? bloque.DirInicial : 0;
                int dirAbsoluta = cp + dirInicialBloque;

                if (bloqueActual != linea.NumeroBloque)
                {
                    FlushTRecord();
                    currentTStart = dirAbsoluta;
                    bloqueActual = linea.NumeroBloque;
                }

                if (currentTStart == -1) currentTStart = dirAbsoluta;

                if (currentTCode.Length + objeto.Length > 60)
                {
                    FlushTRecord();
                    currentTStart = dirAbsoluta;
                }

                currentTCode += objeto;

                bool esFormato4 = EsFormato4(linea.CodigoOp);
                bool esWord = linea.CodigoOp == "WORD";

                if ((esFormato4 || esWord) && !string.IsNullOrWhiteSpace(linea.Operador))
                {
                    int lengthM = esWord ? 6 : 5;
                    int dirM = esWord ? dirAbsoluta : dirAbsoluta + 1;

                    string opLimpio = linea.Operador.TrimStart('#', '@').Replace(",X", "");
                    var res = evaluador.Evaluar(new LineaIntermedia { Operador = opLimpio }, dirAbsoluta);

                    if (!res.Error)
                    {
                        // FIX: Orden idéntico al PDF -> Primero SE, luego Locales
                        foreach (var kv in res.SimbolosExternos)
                        {
                            string signo = kv.Value > 0 ? "+" : "-";
                            string nombreSE = kv.Key.PadRight(6, ' ').Substring(0, 6);
                            for (int i = 0; i < Math.Abs(kv.Value); i++)
                            {
                                regM.Add($"M{dirM:X6}{lengthM:D2}{signo}{nombreSE}");
                            }
                        }

                        if (res.EsRelativo)
                        {
                            regM.Add($"M{dirM:X6}{lengthM:D2}+{nomPrograma}");
                        }
                    }
                }
            }

            CerrarSeccion();
            return archivoObjeto;
        }
    }
}