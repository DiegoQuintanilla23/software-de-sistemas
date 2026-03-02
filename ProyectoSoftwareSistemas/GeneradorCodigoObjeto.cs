using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoSoftwareSistemas
{
    public class GeneradorCodigoObjeto
    {
        private List<LineaIntermedia> _lineas;
        private Dictionary<string, string> _tabsim;
        private int _baseReg = -1;

        // Diccionario de OpCodes (Ejemplo simplificado, debe expandirse con la tabla completa de SIC/XE)
        private Dictionary<string, int> _opcodes = new Dictionary<string, int>
        {
            { "LDA", 0x00 }, { "LDX", 0x04 }, { "LDL", 0x08 }, { "STA", 0x0C },
            { "STX", 0x10 }, { "STL", 0x14 }, { "ADD", 0x18 }, { "SUB", 0x1C },
            { "MUL", 0x20 }, { "DIV", 0x24 }, { "COMP", 0x28 }, { "J", 0x3C },
            { "JEQ", 0x30 }, { "JGT", 0x34 }, { "JLT", 0x38 }, { "JSUB", 0x48 },
            { "RSUB", 0x4C }, { "LDCH", 0x50 }, { "STCH", 0x54 }, { "LDB", 0x68 },
            { "LDT", 0x74 }, { "CLEAR", 0xB4 }, { "TIXR", 0xB8 }
        };

        public string RutaFinal { get; }

        public GeneradorCodigoObjeto(List<LineaIntermedia> lineas, Dictionary<string, string> tabsim)
        {
            _lineas = lineas;
            _tabsim = tabsim;
        }

        public GeneradorCodigoObjeto(Dictionary<string, string> tABSIM, string rutaFinal)
        {
            _tabsim = tABSIM;
            RutaFinal = rutaFinal;
        }

        public void Generar()
        {
            foreach (var linea in _lineas)
            {
                // Si la línea ya tiene errores de la primera pasada (como Símbolo duplicado), la saltamos
                if (!string.IsNullOrEmpty(linea.Errores)) continue;

                try
                {
                    // Manejo de Directivas que afectan el Código Objeto
                    if (linea.Formato == "-")
                    {
                        ProcesarDirectiva(linea);
                        continue;
                    }

                    // Manejo de Instrucciones por Formato
                    switch (linea.Formato)
                    {
                        case "1":
                            GenerarFormato1(linea);
                            break;
                        case "2":
                            GenerarFormato2(linea);
                            break;
                        case "3":
                        case "4":
                            GenerarFormato3o4(linea);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    linea.Errores = "Error en generación: " + ex.Message;
                }
            }
        }

        private void ProcesarDirectiva(LineaIntermedia linea)
        {
            if (linea.CodigoOp == "BYTE")
            {
                string op = linea.Operador.ToUpper();
                if (op.StartsWith("C'"))
                {
                    string contenido = op.Substring(2, op.Length - 3);
                    foreach (char c in contenido)
                        linea.CodigoObjeto += ((int)c).ToString("X2"); // Corregido: CodigoObjeto
                }
                else if (op.StartsWith("X'"))
                {
                    linea.CodigoObjeto = op.Substring(2, op.Length - 3); // Corregido: CodigoObjeto
                }
            }
            else if (linea.CodigoOp == "WORD")
            {
                if (int.TryParse(linea.Operador, out int val))
                    linea.CodigoObjeto = val.ToString("X6"); // Corregido: CodigoObjeto
            }
            else if (linea.CodigoOp == "BASE")
            {
                if (_tabsim.ContainsKey(linea.Operador))
                {
                    _baseReg = Convert.ToInt32(_tabsim[linea.Operador], 16);
                }
                else
                {
                    // Registramos un error semántico en la segunda pasada
                    linea.Errores = "Error: Símbolo BASE no encontrado";
                }
            }
        }

        private void GenerarFormato1(LineaIntermedia linea)
        {
            string op = linea.CodigoOp;
            if (_opcodes.ContainsKey(op))
                linea.CodigoObjeto = _opcodes[op].ToString("X2"); // Corregido: CodigoObjeto
            else
                linea.Errores = "Error: Mnemónico no reconocido";
        }

        private void GenerarFormato2(LineaIntermedia linea)
        {
            // Lógica simplificada de registros (A=0, X=1, L=2, B=3, S=4, T=5, F=6)
            // Aquí se debería parsear el operador "R1, R2" en hexadecimal real
            linea.CodigoObjeto = "F2_OBJ"; // Corregido: CodigoObjeto (cambia F2_OBJ por tu cálculo de bytes real)
        }

        private void GenerarFormato3o4(LineaIntermedia linea)
        {
            string mnem = linea.CodigoOp.Replace("+", "");

            if (!_opcodes.ContainsKey(mnem))
            {
                linea.Errores = "Error: Mnemónico no reconocido";
                return;
            }

            int opCode = _opcodes[mnem];
            int n = 1, i = 1, x = 0, b = 0, p = 0, e = (linea.Formato == "4") ? 1 : 0;

            // Determinar n e i basado en el modo
            if (linea.ModoDireccionamiento == "Inmediato") { n = 0; i = 1; }
            else if (linea.ModoDireccionamiento == "Indirecto") { n = 1; i = 0; }

            // En tu implementación final, aquí calcularás el desplazamiento (Desp) restando
            // la dirección del símbolo destino (sacada de _tabsim) menos el PC.

            // Si el operador es un símbolo pero no está en la tabla, lanzamos error:
            if (!string.IsNullOrEmpty(linea.Operador) && !linea.Operador.StartsWith("#") && !linea.Operador.StartsWith("@") && !_tabsim.ContainsKey(linea.Operador.Replace(",X", "")))
            {
                // Omitimos números puros para no marcarlos como error
                if (!int.TryParse(linea.Operador, out _))
                {
                    linea.Errores = "Error: Símbolo no encontrado";
                }
            }

            // Solo para que no salga vacío mientras implementas el cálculo de binarios:
            linea.CodigoObjeto = opCode.ToString("X2") + (e == 1 ? "000000" : "0000"); // Corregido: CodigoObjeto
        }
    }
}