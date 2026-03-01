using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ClosedXML.Excel;
using System.IO;

namespace ProyectoSoftwareSistemas
{
    public class LineaIntermedia
    {
        public int NumeroLinea { get; set; }
        public string ContadorPrograma { get; set; } = "";
        public string Etiqueta { get; set; } = "";
        public string CodigoOp { get; set; } = "";
        public string Operador { get; set; } = "";
        public string Formato { get; set; } = "";
        public string ModoDireccionamiento { get; set; } = "";
        public string Errores { get; set; } = "";
    }
    public class GeneradorArchivoIntermedio
    {
        private SICXEParser.ProgramContext _root;
        private Dictionary<string, string> TABSIM = new Dictionary<string, string>();

        public GeneradorArchivoIntermedio(SICXEParser.ProgramContext root)
        {
            _root = root;
        }

        public List<LineaIntermedia> GenerarLineas()
        {
            var lista = new List<LineaIntermedia>();
            int contadorPrograma = 0;

            foreach (var linea in _root.line())
            {
                var nueva = new LineaIntermedia();
                if (linea.statement() == null)
                {
                    nueva.Errores = "Error de sintaxis";
                    lista.Add(nueva);
                    continue;
                }
                nueva.ContadorPrograma = contadorPrograma.ToString("X4");

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
                        nueva.CodigoOp = "+" + stmt.extendedInstr().f3().OPCODE_F3().GetText();
                        nueva.Formato = "4";
                        if (!hayErrorSintactico)
                            contadorPrograma += 4;

                        if (stmt.extendedInstr().f3().f3Operands() != null)
                        {
                            nueva.Operador = stmt.extendedInstr().f3().f3Operands().GetText();
                            ProcesarModoDireccionamiento(nueva, stmt.extendedInstr().f3().f3Operands());
                        }
                    }
                    // FORMATO 3 normal
                    else if (stmt.instruction()?.f3() != null)
                    {
                        nueva.CodigoOp = stmt.instruction().f3().OPCODE_F3().GetText();
                        nueva.Formato = "3";
                        if (!hayErrorSintactico)
                            contadorPrograma += 3;

                        if (stmt.instruction().f3().f3Operands() != null)
                        {
                            nueva.Operador = stmt.instruction().f3().f3Operands().GetText();
                            ProcesarModoDireccionamiento(nueva, stmt.instruction().f3().f3Operands());
                        }
                    }
                    // FORMATO 2
                    else if (stmt.instruction()?.f2() != null)
                    {
                        nueva.CodigoOp = stmt.instruction().f2().OPCODE_F2().GetText();
                        nueva.Operador = stmt.instruction().f2().GetText()
                                            .Replace(nueva.CodigoOp, "");

                        nueva.Formato = "2";
                        nueva.ModoDireccionamiento = "--";
                        if (!hayErrorSintactico)
                            contadorPrograma += 2;
                    }
                    // FORMATO 1
                    else if (stmt.instruction()?.f1() != null)
                    {
                        var f1 = stmt.instruction().f1();

                        nueva.CodigoOp = f1.OPCODE_F1().GetText();
                        nueva.Formato = "1";
                        nueva.ModoDireccionamiento = "-";

                        // Si existe value → es error
                        if (f1.value() != null)
                        {
                            nueva.Operador = f1.value().GetText();
                            nueva.Errores = "Error: Sintaxis";
                            hayErrorSintactico = true;
                            insertarEnTabSim = false;
                        }

                        if (!hayErrorSintactico)
                            contadorPrograma += 1;
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
                                contadorPrograma = inicio;
                                nueva.ContadorPrograma = contadorPrograma.ToString("X4");
                            }
                        }

                        else if (nueva.CodigoOp == "WORD")
                        {
                            if (!hayErrorSintactico)
                                contadorPrograma += 3;
                        }

                        else if (nueva.CodigoOp == "RESB")
                        {
                            if (TryParseNumero(nueva.Operador, out int valor))
                            {
                                if (!hayErrorSintactico)
                                    contadorPrograma += valor;
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
                                    contadorPrograma += valor * 3;
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
                                    contadorPrograma += longitud;
                            }
                            else if (op.StartsWith("X'") && op.EndsWith("'"))
                            {
                                int longitud = op.Length - 3;

                                if (longitud % 2 != 0)
                                {
                                    nueva.Errores = "Error: Sintaxis";
                                    hayErrorSintactico = true;
                                    insertarEnTabSim = false;
                                }
                                else
                                {
                                    if (!hayErrorSintactico)
                                        contadorPrograma += longitud / 2;
                                }
                            }
                            else
                            {
                                nueva.Errores = "Error: Sintaxis";
                                hayErrorSintactico = true;
                                insertarEnTabSim = false;
                            }
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
                    TABSIM[nueva.Etiqueta] = nueva.ContadorPrograma;
                }

                lista.Add(nueva);
            }

            return lista;
        }

        private bool TryParseNumero(string texto, out int valor)
        {
            valor = 0;

            texto = texto.Trim().ToUpper();

            // Si termina en H → hexadecimal
            if (texto.EndsWith("H"))
            {
                string hex = texto.Substring(0, texto.Length - 1);

                return int.TryParse(hex,
                                    System.Globalization.NumberStyles.HexNumber,
                                    null,
                                    out valor);
            }

            // Si empieza con 0x → hexadecimal
            if (texto.StartsWith("0X"))
            {
                return int.TryParse(texto.Substring(2),
                                    System.Globalization.NumberStyles.HexNumber,
                                    null,
                                    out valor);
            }

            // Si no → decimal normal
            return int.TryParse(texto, out valor);
        }

        public void GenerarExcel(List<LineaIntermedia> lineas, string nombreArchivo)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ArchivoIntermedio");

            worksheet.Cell(1, 1).Value = "Numero de Linea";
            worksheet.Cell(1, 2).Value = "Contador de Programa";
            worksheet.Cell(1, 3).Value = "Etiqueta";
            worksheet.Cell(1, 4).Value = "CodigoOp";
            worksheet.Cell(1, 5).Value = "Operador";
            worksheet.Cell(1, 6).Value = "Formato de Instruccion";
            worksheet.Cell(1, 7).Value = "Modo de Direccionamiento";
            worksheet.Cell(1, 8).Value = "Errores";

            int fila = 2;

            foreach (var l in lineas)
            {
                worksheet.Cell(fila, 1).Value = l.NumeroLinea;
                worksheet.Cell(fila, 2).Value = l.ContadorPrograma;   // ← CP
                worksheet.Cell(fila, 3).Value = l.Etiqueta;
                worksheet.Cell(fila, 4).Value = l.CodigoOp;
                worksheet.Cell(fila, 5).Value = l.Operador;
                worksheet.Cell(fila, 6).Value = l.Formato;
                worksheet.Cell(fila, 7).Value = l.ModoDireccionamiento;
                worksheet.Cell(fila, 8).Value = l.Errores;            // ← ERRORES

                fila++;
            }

            string nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
            string nuevoNombre = nombreSinExtension + "_ArchivoIntermedio.xlsx";
            string carpetaRaiz = Directory.GetCurrentDirectory();
            carpetaRaiz = carpetaRaiz + "\\output\\";
            string rutaFinal = Path.Combine(carpetaRaiz, nuevoNombre);

            workbook.SaveAs(rutaFinal);
            GeneradorCodigoObjeto codObjGen = new GeneradorCodigoObjeto(TABSIM, rutaFinal);
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

            Console.WriteLine("{0,-20} {1,-10}", "Símbolo", "Dirección");
            Console.WriteLine(new string('-', 30));

            foreach (var simbolo in TABSIM)
            {
                Console.WriteLine("{0,-20} {1,-10}", simbolo.Key, simbolo.Value);
            }

            Console.WriteLine("\n========================================\n");
        }
    }
}