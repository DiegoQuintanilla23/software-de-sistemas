using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;


namespace ProyectoSoftwareSistemas
{
    public class LineaIntermedia
    {
        public int NumeroLinea { get; set; }
        public int NumeroBloque { get; set; }
        public string ContadorPrograma { get; set; } = "";
        public string Etiqueta { get; set; } = "";
        public string CodigoOp { get; set; } = "";
        public string Operador { get; set; } = "";
        public string Formato { get; set; } = "";
        public string ModoDireccionamiento { get; set; } = "";
        public string Errores { get; set; } = "";
        public string CodigoObjeto { get; set; } = "";
    }
    public class GeneradorArchivoIntermedio
    {
        private SICXEParser.ProgramContext _root;
        private Dictionary<string, Simbolo> TABSIM = new Dictionary<string, Simbolo>();
        private Dictionary<string, Bloque> TABBLK = new Dictionary<string, Bloque>();
        private Bloque bloqueActual;
        private int contadorBloques = 0;

        public GeneradorArchivoIntermedio(SICXEParser.ProgramContext root)
        {
            _root = root;

            TABBLK["DEFAULT"] = new Bloque
            {
                Numero = 0,
                Nombre = "DEFAULT",
                Locctr = 0
            };

            bloqueActual = TABBLK["DEFAULT"];
        }

        private bool EsSalto(string opcode)
        {
            return opcode == "J" || opcode == "JEQ" || opcode == "JGT"
                || opcode == "JLT" || opcode == "JSUB";
        }

        public List<LineaIntermedia> GenerarLineas()
        {
            var lista = new List<LineaIntermedia>();
            //int contadorPrograma = 0;
            Stack<int> pilaORG = new Stack<int>();
            var evaluador = new EvaluadorExpresiones(TABSIM, TABBLK);

            foreach (var linea in _root.line())
            {
                var nueva = new LineaIntermedia();
                int locctrAntes = bloqueActual.Locctr;
                //nueva.NumeroBloque = 0;
                if (linea.statement() == null)
                {
                    nueva.Errores = "Error de sintaxis";
                    lista.Add(nueva);
                    continue;
                }
                nueva.ContadorPrograma = locctrAntes.ToString("X4");
                nueva.NumeroBloque = bloqueActual.Numero;

                nueva.NumeroLinea = linea.Start.Line;

                if (linea.label() != null)
                    nueva.Etiqueta = linea.label().GetText();

                bool hayErrorSintactico = false;
                bool hayErrorSemantico = false;
                bool insertarEnTabSim = false;

                if (!string.IsNullOrEmpty(nueva.Etiqueta))
                {
                    bool esStart = linea.statement().directive() != null && linea.statement().directive().DIRECTIVE().GetText() == "START";
                    if (TABSIM.ContainsKey(nueva.Etiqueta))
                    {
                        nueva.Errores = "Error: Símbolo duplicado";
                        hayErrorSemantico = true;
                    }
                    else
                    {
                        if(!esStart)
                            insertarEnTabSim = true;
                    }
                }

                if (linea.statement() != null)
                {
                    var stmt = linea.statement();

                    // EXTENDIDA (+)
                    if (stmt.extendedInstr() != null)
                    {
                        string opcode = stmt.extendedInstr().f3().OPCODE_F3().GetText();

                        nueva.CodigoOp = "+" + opcode;
                        nueva.Formato = "4";

                        if (!hayErrorSintactico)
                            bloqueActual.Locctr += 4;

                        if (stmt.extendedInstr().f3().f3Operands() != null)
                        {
                            var ops = stmt.extendedInstr().f3().f3Operands();

                            nueva.Operador = ops.GetText();
                            ProcesarModoDireccionamiento(nueva, ops);

                            // VALIDACIÓN AQUÍ
                            // bool esIndexado = ops.indexedOperand() != null;

                            // if (EsSalto(opcode) && esIndexado)
                            // {
                            // nueva.Errores = "Error: no existe combinación MD";
                            // }
                        }
                    }
                    // FORMATO 3 normal
                    else if (stmt.instruction()?.f3() != null)
                    {
                        nueva.CodigoOp = stmt.instruction().f3().OPCODE_F3().GetText();
                        nueva.Formato = "3";
                        if (!hayErrorSintactico)
                            bloqueActual.Locctr += 3;

                        if (stmt.instruction().f3().f3Operands() != null)
                        {
                            nueva.Operador = stmt.instruction().f3().f3Operands().GetText();
                            ProcesarModoDireccionamiento(nueva, stmt.instruction().f3().f3Operands());
                        }
                    }
                    // FORMATO 2
                    else if (stmt.instruction()?.f2() != null)
                    {
                        var f2 = stmt.instruction().f2();

                        nueva.CodigoOp = f2.Start.Text;
                        nueva.Formato = "2";
                        nueva.ModoDireccionamiento = "--";

                        if (f2.f2_oneReg() != null)
                        {
                            var ctx = f2.f2_oneReg();

                            if (ctx.REG() == null)
                            {
                                nueva.Errores = "Error: Falta registro";
                                hayErrorSintactico = true;
                            }
                            else
                            {
                                nueva.Operador = ctx.REG().GetText();
                            }
                        }
                        else if (f2.f2_twoReg() != null)
                        {
                            var ctx = f2.f2_twoReg();

                            if (ctx.REG().Length < 2)
                            {
                                nueva.Operador = ctx.GetText().Replace(nueva.CodigoOp, "");
                                nueva.Errores = "Error: Falta registro";
                                hayErrorSintactico = true;
                            }
                            else if (ctx.REG().Length > 2)
                            {
                                nueva.Operador = ctx.GetText().Replace(nueva.CodigoOp, "");
                                nueva.Errores = "Error: Demasiados registros";
                                hayErrorSintactico = true;
                            }
                            else
                            {
                                var r1 = ctx.REG(0).GetText();
                                var r2 = ctx.REG(1).GetText();

                                nueva.Operador = r1 + "," + r2;
                            }
                        }
                        else if (f2.f2_regNum() != null)
                        {
                            var r = f2.f2_regNum().REG().GetText();
                            var n = f2.f2_regNum().NUMBER().GetText();

                            nueva.Operador = r + "," + n;

                            int valor = int.Parse(n);
                            if (valor < 0 || valor > 15)
                            {
                                nueva.Errores = "Error: Número fuera de rango";
                                hayErrorSintactico = true;
                            }
                        }
                        else if (f2.f2_num() != null)
                        {
                            var n = f2.f2_num().NUMBER().GetText();
                            nueva.Operador = n;

                            int valor = int.Parse(n);
                            if (valor < 0 || valor > 15)
                            {
                                nueva.Errores = "Error: Número fuera de rango";
                                hayErrorSintactico = true;
                            }
                        }

                        if (!hayErrorSintactico)
                            bloqueActual.Locctr += 2;
                    }
                    // FORMATO 1
                    else if (stmt.instruction()?.f1() != null)
                    {
                        var f1 = stmt.instruction().f1();

                        nueva.CodigoOp = f1.OPCODE_F1().GetText();
                        nueva.Formato = "1";
                        nueva.ModoDireccionamiento = "-";

                        // Si existe value - es error
                        if (f1.f3Operands() != null)
                        {
                            nueva.Operador = f1.f3Operands().GetText();
                            nueva.Errores = "Error: Sintaxis";
                            hayErrorSintactico = true;
                            insertarEnTabSim = false;
                        }
                        if (f1.value() != null)
                        {
                            nueva.Operador = f1.value().GetText();
                            nueva.Errores = "Error: Sintaxis";
                            hayErrorSintactico = true;
                            insertarEnTabSim = false;
                        }

                        if (!hayErrorSintactico)
                            bloqueActual.Locctr += 1;
                    }
                    // DIRECTIVAS
                    else if (stmt.directive() != null)
                    {
                        nueva.CodigoOp = stmt.directive().GetChild(0).GetText();

                        if (stmt.directive().ChildCount > 1)
                            nueva.Operador = stmt.directive().GetChild(1).GetText();

                        nueva.Formato = "-";
                        nueva.ModoDireccionamiento = "---";

                        if (nueva.CodigoOp == "START")
                        {
                            if (int.TryParse(nueva.Operador, System.Globalization.NumberStyles.HexNumber, null, out int inicio))
                            {
                                bloqueActual.Locctr = inicio;
                                nueva.ContadorPrograma = locctrAntes.ToString("X4");
                            }
                        }

                        else if (nueva.CodigoOp == "WORD")
                        {
                            if (!hayErrorSintactico)
                                bloqueActual.Locctr += 3;
                        }

                        else if (nueva.CodigoOp == "RESB")
                        {
                            if (TryParseNumero(nueva.Operador, out int valor))
                            {
                                if (!hayErrorSintactico)
                                    bloqueActual.Locctr += valor;
                            }
                            else
                            {
                                nueva.Errores = "Error: Operando inválido en RESB";
                                hayErrorSintactico = true;
                                insertarEnTabSim = false;
                            }
                        }

                        else if (nueva.CodigoOp == "RESW")
                        {
                            if (TryParseNumero(nueva.Operador, out int valor))
                            {
                                if (!hayErrorSintactico)
                                    bloqueActual.Locctr += valor * 3;
                            }
                            else
                            {
                                nueva.Errores = "Error: Operando inválido en RESW";
                                hayErrorSintactico = true;
                                insertarEnTabSim = false;
                            }
                        }

                        else if (nueva.CodigoOp == "BYTE")
                        {
                            string op = nueva.Operador.ToUpper();

                            if (op.StartsWith("C'") && op.EndsWith("'"))
                            {
                                int longitud = op.Length - 3;

                                if (!hayErrorSintactico)
                                    bloqueActual.Locctr += longitud;
                            }
                            else if (op.StartsWith("X'") && op.EndsWith("'"))
                            {
                                string contenido = op.Substring(2, op.Length - 3); // quitar X' '

                                // Validar que solo tenga hex válidos
                                if (!System.Text.RegularExpressions.Regex.IsMatch(contenido, "^[0-9A-F]+$"))
                                {
                                    nueva.Errores = "Error: Literal hexadecimal inválido";
                                    hayErrorSintactico = true;
                                    insertarEnTabSim = false;
                                }

                                // Si es impar, completar con 0 a la izquierda
                                if (contenido.Length % 2 != 0)
                                {
                                    contenido = "0" + contenido;
                                    op = "X'" + contenido + "'";
                                }

                                if (!hayErrorSintactico)
                                    bloqueActual.Locctr += contenido.Length / 2;
                            }
                            else
                            {
                                nueva.Errores = "Error: Sintaxis";
                                hayErrorSintactico = true;
                                insertarEnTabSim = false;
                            }
                        }
                        else if (nueva.CodigoOp == "EQU")
                        {
                            if (string.IsNullOrEmpty(nueva.Etiqueta))
                            {
                                nueva.Errores = "Error: EQU requiere etiqueta";
                                hayErrorSemantico = true;
                                insertarEnTabSim = false;
                            }
                            else
                            {
                                var res = evaluador.Evaluar(nueva, bloqueActual.Locctr);

                                // VALIDAR BLOQUES SOLO EN EQU
                                var (bloques, hayRel, hayAbs) = ObtenerInfoExpresion(nueva.Operador);

                                // 1. Diferentes bloques
                                if (bloques.Count > 1)
                                {
                                    nueva.Errores = "Error: Símbolos de diferentes bloques en EQU";
                                    hayErrorSemantico = true;
                                }

                                // 2. Mezcla inválida (según tu práctica)
                                if (hayRel && hayAbs)
                                {
                                    if (string.IsNullOrEmpty(nueva.Errores))
                                        nueva.Errores = "Error: Símbolos de diferentes bloques en EQU";
                                    else
                                        nueva.Errores += " | Error: Símbolos de diferentes bloques en EQU";

                                    hayErrorSemantico = true;
                                }

                                if (res.Error || hayErrorSemantico)
                                {
                                    // Acumular error
                                    if (string.IsNullOrEmpty(nueva.Errores))
                                        nueva.Errores = res.MensajeError;
                                    else
                                        nueva.Errores += " | " + res.MensajeError;

                                    hayErrorSemantico = true;

                                    // IMPORTANTE: insertar con FFFF tipo A
                                    TABSIM[nueva.Etiqueta] = new Simbolo
                                    {
                                        Nombre = nueva.Etiqueta,
                                        Direccion = 0xFFFF,
                                        Tipo = "A",
                                        EsRelativo = false,
                                        Bloque = bloqueActual.Nombre
                                    };
                                }
                                else
                                {
                                    TABSIM[nueva.Etiqueta] = new Simbolo
                                    {
                                        Nombre = nueva.Etiqueta,
                                        Direccion = res.Valor,
                                        Tipo = res.Tipo,
                                        EsRelativo = res.EsRelativo,
                                        Bloque = bloqueActual.Nombre
                                    };
                                }

                                insertarEnTabSim = false;
                            }
                        }
                        else if (nueva.CodigoOp == "ORG")
                        {
                            // ORG sin operando - regresar
                            if (string.IsNullOrWhiteSpace(nueva.Operador))
                            {
                                if (pilaORG.Count > 0)
                                {
                                    bloqueActual.Locctr = pilaORG.Pop();
                                    nueva.ContadorPrograma = locctrAntes.ToString("X4");
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(nueva.Errores))
                                        nueva.Errores = "Error: ORG sin valor previo";
                                    else
                                        nueva.Errores += " | Error: ORG sin valor previo";

                                    hayErrorSemantico = true;
                                }
                            }
                            else
                            {
                                var res = evaluador.Evaluar(nueva, bloqueActual.Locctr);

                                if (res.Error)
                                {
                                    if (string.IsNullOrEmpty(nueva.Errores))
                                        nueva.Errores = res.MensajeError;
                                    else
                                        nueva.Errores += " | " + res.MensajeError;

                                    hayErrorSemantico = true;
                                }
                                else
                                {
                                    // Guardar PC actual
                                    pilaORG.Push(bloqueActual.Locctr);

                                    bloqueActual.Locctr = res.Valor;
                                    nueva.ContadorPrograma = locctrAntes.ToString("X4");
                                }
                            }

                            insertarEnTabSim = false;
                        }
                        else if (nueva.CodigoOp == "USE")
                        {
                            string nombre = string.IsNullOrWhiteSpace(nueva.Operador) ? "DEFAULT" : nueva.Operador;

                            if (!TABBLK.ContainsKey(nombre))
                            {
                                TABBLK[nombre] = new Bloque
                                {
                                    Numero = ++contadorBloques,
                                    Nombre = nombre,
                                    Locctr = 0
                                };
                            }

                            bloqueActual = TABBLK[nombre];

                            // actualizar valores de la línea DESPUÉS del cambio de bloque
                            nueva.NumeroBloque = bloqueActual.Numero;
                            nueva.ContadorPrograma = bloqueActual.Locctr.ToString("X4");

                            insertarEnTabSim = false;
                        }
                        else if (nueva.CodigoOp == "END")
                        {
                            // Forzar que END esté en el bloque 0 (DEFAULT)
                            if (TABBLK.ContainsKey("DEFAULT"))
                            {
                                bloqueActual = TABBLK["DEFAULT"];
                            }

                            nueva.NumeroBloque = bloqueActual.Numero;
                            nueva.ContadorPrograma = bloqueActual.Locctr.ToString("X4");

                            insertarEnTabSim = false;
                        }

                    }
                    else
                    {
                        nueva.Errores = "Error: Instrucción no existe";
                        hayErrorSintactico = true;
                        insertarEnTabSim = false;
                    }
                }

                if (!hayErrorSemantico && insertarEnTabSim)
                {
                    TABSIM[nueva.Etiqueta] = new Simbolo
                    {
                        Nombre = nueva.Etiqueta,
                        Direccion = locctrAntes,
                        Tipo = "R",
                        EsRelativo = true,
                        Bloque = bloqueActual.Nombre
                    };
                }

                lista.Add(nueva);
            }

            int dir = 0;

            foreach (var b in TABBLK.Values.OrderBy(x => x.Numero))
            {
                b.Longitud = b.Locctr;
                b.DirInicial = dir;
                dir += b.Longitud;
            }

            return lista;
        }

        private (HashSet<string> bloques, bool hayRel, bool hayAbs) ObtenerInfoExpresion(string expr)
        {
            var bloques = new HashSet<string>();
            bool hayRel = false;
            bool hayAbs = false;

            if (string.IsNullOrWhiteSpace(expr))
                return (bloques, hayRel, hayAbs);

            expr = expr.ToUpper().Replace(" ", "");

            var tokens = System.Text.RegularExpressions.Regex.Split(expr, @"[^A-Z0-9]+");

            foreach (var token in tokens)
            {
                if (string.IsNullOrEmpty(token))
                    continue;

                if (int.TryParse(token, out _))
                {
                    hayAbs = true;
                    continue;
                }

                if (token.EndsWith("H"))
                {
                    hayAbs = true;
                    continue;
                }

                if (token == "*")
                {
                    hayRel = true;
                    bloques.Add(bloqueActual.Nombre);
                    continue;
                }

                if (TABSIM.ContainsKey(token))
                {
                    var s = TABSIM[token];

                    if (s.EsRelativo)
                    {
                        hayRel = true;
                        bloques.Add(s.Bloque);
                    }
                    else
                    {
                        hayAbs = true;
                    }
                }
            }

            return (bloques, hayRel, hayAbs);
        }


        private bool TryParseNumero(string texto, out int valor)
        {
            valor = 0;

            texto = texto.Trim().ToUpper();

            // Si termina en H - hexadecimal
            if (texto.EndsWith("H"))
            {
                string hex = texto.Substring(0, texto.Length - 1);

                return int.TryParse(hex,
                                    System.Globalization.NumberStyles.HexNumber,
                                    null,
                                    out valor);
            }

            // Si empieza con 0x - hexadecimal
            if (texto.StartsWith("0X"))
            {
                return int.TryParse(texto.Substring(2),
                                    System.Globalization.NumberStyles.HexNumber,
                                    null,
                                    out valor);
            }

            // Si no - decimal normal
            return int.TryParse(texto, out valor);
        }

        public void GenerarExcel(List<LineaIntermedia> lineas, List<string> programaObjeto, string nombreArchivo)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ArchivoIntermedio");

            worksheet.Cell(1, 1).Value = "Num. Linea";
            worksheet.Cell(1, 2).Value = "Num. Bloque";
            worksheet.Cell(1, 3).Value = "P.C.";
            worksheet.Cell(1, 4).Value = "Etiq.";
            worksheet.Cell(1, 5).Value = "CodOp";
            worksheet.Cell(1, 6).Value = "Op";
            worksheet.Cell(1, 7).Value = "Frmt. Inst";
            worksheet.Cell(1, 8).Value = "M.D.";
            worksheet.Cell(1, 9).Value = "Errores";
            worksheet.Cell(1, 10).Value = "C.O.";

            int fila = 2;

            foreach (var l in lineas)
            {
                worksheet.Cell(fila, 1).Value = l.NumeroLinea;
                worksheet.Cell(fila, 2).Value = l.NumeroBloque;
                worksheet.Cell(fila, 3).Value = l.ContadorPrograma;
                worksheet.Cell(fila, 4).Value = l.Etiqueta;
                worksheet.Cell(fila, 5).Value = l.CodigoOp;
                worksheet.Cell(fila, 6).Value = l.Operador;
                worksheet.Cell(fila, 7).Value = l.Formato;
                worksheet.Cell(fila, 8).Value = l.ModoDireccionamiento;
                worksheet.Cell(fila, 9).Value = l.Errores;
                worksheet.Cell(fila, 10).Value = l.CodigoObjeto;
                fila++;
            }

            //Hoja 2, progama objeto
            if (programaObjeto != null && programaObjeto.Count > 0)
            {
                var wsObjeto = workbook.Worksheets.Add("ProgramaObjeto");

                int filaObj = 1;
                foreach (string registro in programaObjeto)
					{
                    wsObjeto.Cell(filaObj, 1).Value = registro;
                    filaObj++;
                }

                wsObjeto.Column(1).AdjustToContents();
            }

            var wsBloques = workbook.Worksheets.Add("TABBLK");

            wsBloques.Cell(1, 1).Value = "No. Bloque";
            wsBloques.Cell(1, 2).Value = "Nombre";
            wsBloques.Cell(1, 3).Value = "Longitud";
            wsBloques.Cell(1, 4).Value = "Dir Inicial";

            int filaB = 2;

            foreach (var b in TABBLK.Values.OrderBy(x => x.Numero))
            {
                wsBloques.Cell(filaB, 1).Value = b.Numero;
                wsBloques.Cell(filaB, 2).Value = b.Nombre;
                wsBloques.Cell(filaB, 3).Value = b.Longitud.ToString("X4");
                wsBloques.Cell(filaB, 4).Value = b.DirInicial.ToString("X4");
                filaB++;
            }

            wsBloques.Columns().AdjustToContents();

            // Hoja TABSIM
            var wsSim = workbook.Worksheets.Add("TABSIM");

            wsSim.Cell(1, 1).Value = "Simbolo";
            wsSim.Cell(1, 2).Value = "Direccion";
            wsSim.Cell(1, 3).Value = "Tipo";
            wsSim.Cell(1, 4).Value = "Relativo";
            wsSim.Cell(1, 5).Value = "Bloque";

            int filaS = 2;

            foreach (var s in TABSIM.Values)
            {
                wsSim.Cell(filaS, 1).Value = s.Nombre;
                wsSim.Cell(filaS, 2).Value = s.Direccion.ToString("X4");
                wsSim.Cell(filaS, 3).Value = s.Tipo;
                wsSim.Cell(filaS, 4).Value = s.EsRelativo ? "Sí" : "No";
                wsSim.Cell(filaS, 5).Value = s.Bloque;
                filaS++;
            }

            wsSim.Columns().AdjustToContents();

            string nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
            string nuevoNombre = nombreSinExtension + "_ArchivoIntermedio.xlsx";
            string carpetaRaiz = Directory.GetCurrentDirectory();
            carpetaRaiz = carpetaRaiz + "\\output\\";
            string rutaFinal = Path.Combine(carpetaRaiz, nuevoNombre);

            workbook.SaveAs(rutaFinal);
            var psi = new ProcessStartInfo
            {
                FileName = rutaFinal,
                UseShellExecute = true
            };

            Process.Start(psi);
        }

        private void ProcesarModoDireccionamiento(LineaIntermedia nueva, SICXEParser.F3OperandsContext ops)
        {
            if (ops.immediateOperand() != null)
            {
                nueva.ModoDireccionamiento = "Inmediato";
            }
            else if (ops.indirectOperand() != null)
            {
                nueva.ModoDireccionamiento = "Indirecto";
            }
            else if (ops.indexedOperand() != null)
            {
                nueva.ModoDireccionamiento = "Indexado";
            }
            else if (ops.simpleOperand() != null)
            {
                nueva.ModoDireccionamiento = "Simple";
            }
        }

        public void ImprimirTABSIM()
        {
            Console.WriteLine("\n========== TABLA DE SÍMBOLOS ==========\n");

            if (TABSIM.Count == 0)
            {
                Console.WriteLine("TABSIM vacía.");
                return;
            }

            // Encabezados
            Console.WriteLine("{0,-15} {1,-10} {2,-10} {3,-10} {4,-10}",
                "Símbolo", "Dirección", "Tipo", "Relativo", "Bloque");

            Console.WriteLine(new string('-', 60));

            foreach (var simbolo in TABSIM.Values)
            {
                Console.WriteLine("{0,-15} {1,-10:X4} {2,-10} {3,-10} {4,-10}",
                    simbolo.Nombre,
                    simbolo.Direccion,
                    simbolo.Tipo,
                    simbolo.EsRelativo ? "Sí" : "No",
                    simbolo.Bloque);
            }

            Console.WriteLine("\n========================================\n");
        }

        public Dictionary<string, Simbolo> GetTabSim()
        {
            return this.TABSIM;
        }

        public Dictionary<string, Bloque> GetTabBlk()
        {
            return TABBLK;
        }
    }
}