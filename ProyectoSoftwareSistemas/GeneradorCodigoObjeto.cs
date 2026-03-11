using DocumentFormat.OpenXml.ExtendedProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoSoftwareSistemas
{
    public class GeneradorCodigoObjeto
    {
        private Dictionary<string, string> _tabSim;
        private List<LineaIntermedia> _lineas;
        private Dictionary<string, int> _registros;
        private int? _baseAddress = null;

        private Dictionary<string, string> _opcodes;
        public GeneradorCodigoObjeto(Dictionary<string, string> TABSIM, List<LineaIntermedia> lineas)
        {
            _tabSim = TABSIM;
            _lineas = lineas;
            InicializarOpcodes();
            InicializarRegistros();
        }

        private void InicializarRegistros()
        {
            _registros = new Dictionary<string, int>
            {
                { "A", 0 },
                { "X", 1 },
                { "L", 2 },
                { "B", 3 },
                { "S", 4 },
                { "T", 5 },
                { "F", 6 },
                { "PC", 8 },
                { "SW", 9 }
            };
        }

        private void InicializarOpcodes()
        {
            _opcodes = new Dictionary<string, string>
            {
                { "ADD", "18" },
                { "ADDF", "58" },
                { "ADDR", "90" },
                { "AND", "40" },
                { "CLEAR", "B4" },
                { "COMP", "28" },
                { "COMPF", "88" },
                { "COMPR", "A0" },
                { "DIV", "24" },
                { "DIVF", "64" },
                { "DIVR", "9C" },
                { "FIX", "C4" },
                { "FLOAT", "C0" },
                { "HIO", "F4" },
                { "J", "3C" },
                { "JEQ", "30" },
                { "JGT", "34" },
                { "JLT", "38" },
                { "JSUB", "48" },
                { "LDA", "00" },
                { "LDB", "68" },
                { "LDCH", "50" },
                { "LDF", "70" },
                { "LDL", "08" },
                { "LDS", "6C" },
                { "LDT", "74" },
                { "LDX", "04" },
                { "LPS", "D0" },
                { "MUL", "20" },
                { "MULF", "60" },
                { "MULR", "98" },
                { "NORM", "C8" },
                { "OR", "44" },
                { "RD", "D8" },
                { "RMO", "AC" },
                { "RSUB", "4C" },
                { "SHIFTL", "A4" },
                { "SHIFTR", "A8" },
                { "SIO", "F0" },
                { "SSK", "EC" },
                { "STA", "0C" },
                { "STB", "78" },
                { "STCH", "54" },
                { "STF", "80" },
                { "STI", "D4" },
                { "STL", "14" },
                { "STS", "7C" },
                { "STSW", "E8" },
                { "STT", "84" },
                { "STX", "10" },
                { "SUB", "1C" },
                { "SUBF", "5C" },
                { "SUBR", "94" },
                { "SVC", "B0" },
                { "TD", "E0" },
                { "TIO", "F8" },
                { "TIX", "2C" },
                { "TIXR", "B8" },
                { "WD", "DC" }
            };
        }
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

        private bool EsFormato4(string codop)
        {
            if (string.IsNullOrWhiteSpace(codop))
                return false;

            if (!codop.StartsWith("+"))
                return false;

            string baseCodop = codop.Substring(1);

            return EsFormato3(baseCodop);
        }

        private bool EsDirectivaSinCodigo(string codop)
        {
            return new[]
            {
                "START",
                "END",
                "BASE",
                "NOBASE",
                "RESW",
                "RESB",
                "ORG",
                "EQU"
            }.Contains(codop);
        }

        private void GenerarWord(LineaIntermedia linea)
        {
            if (string.IsNullOrWhiteSpace(linea.Operador))
            {
                AgregarError(linea, "Error: WORD requiere un operando");
                return;
            }

            string operando = linea.Operador.Trim().ToUpper();

            try
            {
                int valor;

                // Caso hexadecimal con H al final
                if (operando.EndsWith("H"))
                {
                    operando = operando.Substring(0, operando.Length - 1);
                    valor = Convert.ToInt32(operando, 16);
                }
                else
                {
                    // Caso decimal
                    valor = Convert.ToInt32(operando);
                }

                // WORD es 3 bytes → 6 dígitos hex
                linea.CodigoObjeto = valor.ToString("X6");
            }
            catch
            {
                AgregarError(linea, "Error: Operando inválido en WORD");
            }
        }

        private void GenerarByte(LineaIntermedia linea)
        {
            if (string.IsNullOrWhiteSpace(linea.Operador))
            {
                AgregarError(linea, "Error: BYTE requiere un operando");
                return;
            }

            string op = linea.Operador.Trim().ToUpper();

            // CASO C'EOF'
            if (op.StartsWith("C'") && op.EndsWith("'"))
            {
                string contenido = op.Substring(2, op.Length - 3);

                StringBuilder resultado = new StringBuilder();

                foreach (char c in contenido)
                {
                    resultado.Append(((int)c).ToString("X2"));
                }

                linea.CodigoObjeto = resultado.ToString();
                return;
            }

            // CASO X'F1'
            if (op.StartsWith("X'") && op.EndsWith("'"))
            {
                string contenido = op.Substring(2, op.Length - 3);

                // Validar hexadecimal
                if (!System.Text.RegularExpressions.Regex.IsMatch(contenido, "^[0-9A-F]+$"))
                {
                    AgregarError(linea, "Error: Literal hexadecimal inválido");
                    return;
                }

                // Si es impar → completar con 0 a la izquierda
                if (contenido.Length % 2 != 0)
                {
                    contenido = "0" + contenido;
                }

                linea.CodigoObjeto = contenido;
                return;
            }

            AgregarError(linea, "Error: Formato inválido en BYTE");
        }

        private void GenerarFormato2(LineaIntermedia linea, string opcode)
        {
            if (string.IsNullOrWhiteSpace(linea.Operador))
            {
                AgregarError(linea, "Error: Formato 2 requiere operandos");
                return;
            }

            var registros = linea.Operador.Split(',');

            int r1 = 0;
            int r2 = 0;

            // Validar r1
            if (registros.Length >= 1 && _registros.ContainsKey(registros[0].Trim()))
            {
                r1 = _registros[registros[0].Trim()];
            }
            else
            {
                AgregarError(linea, "Error: Registro inválido en primer operando");
                return;
            }

            // Validar r2 si existe
            if (registros.Length == 2)
            {
                if (_registros.ContainsKey(registros[1].Trim()))
                {
                    r2 = _registros[registros[1].Trim()];
                }
                else
                {
                    AgregarError(linea, "Error: Registro inválido en segundo operando");
                    return;
                }
            }
            else if (registros.Length > 2)
            {
                AgregarError(linea, "Error: Demasiados operandos para formato 2");
                return;
            }

            string byte2 = r1.ToString("X") + r2.ToString("X");

            linea.CodigoObjeto = opcode + byte2;
        }

        private void GenerarFormato3(LineaIntermedia linea)
        {
            string codop = linea.CodigoOp;
            // Caso especial RSUB
            if (codop == "RSUB")
            {
                linea.CodigoObjeto = "4F0000";
                return;
            }
            string opcodeHex = _opcodes[codop];
            int opcode = Convert.ToInt32(opcodeHex, 16);

            // limpiar últimos 2 bits
            opcode = opcode & 0xFC;

            string operando = linea.Operador?.Trim() ?? "";

            if (operando.Contains("@") && operando.Contains("#"))
            {
                AgregarError(linea, "Error: Modo de direccionamiento inválido");
            }

            int n = 1, i = 1, x = 0, b = 0, p = 0, e = 0;

            // Determinar n/i
            if (operando.StartsWith("#"))
            {
                n = 0;
                i = 1;
                operando = operando.Substring(1);
            }
            else if (operando.StartsWith("@"))
            {
                n = 1;
                i = 0;
                operando = operando.Substring(1);
            }

            // Determinar x
            if (operando.Contains(",X"))
            {
                x = 1;
                operando = operando.Replace(",X", "");
            }

            // Agregar n/i al opcode
            opcode = opcode | (n << 1) | i;

            int direccionSimbolo = 0;

            // Si es número inmediato
            if (int.TryParse(operando, out int valorInmediato))
            {
                direccionSimbolo = valorInmediato;
                p = 0;
                b = 0;
            }
            else
            {
                if (!_tabSim.ContainsKey(operando))
                {
                    AgregarError(linea, "Error: Símbolo no encontrado");

                    b = 1;
                    p = 1;

                    int flagsError = (x << 3) | (b << 2) | (p << 1) | e;

                    int codigoError =
                        (opcode << 16) |
                        (flagsError << 12) |
                        0xFFF;   // -1 en 12 bits

                    linea.CodigoObjeto = codigoError.ToString("X6");
                    return;
                }

                direccionSimbolo = Convert.ToInt32(_tabSim[operando], 16);

                int pc = Convert.ToInt32(linea.ContadorPrograma, 16) + 3;
                int disp = direccionSimbolo - pc;

                if (disp >= -2048 && disp <= 2047)
                {
                    p = 1;
                    direccionSimbolo = disp & 0xFFF;
                }
                else if (_baseAddress.HasValue)
                {
                    int dispBase = direccionSimbolo - _baseAddress.Value;

                    // Intentar BASE relative
                    if (dispBase >= 0 && dispBase <= 4095)
                    {
                        b = 1;
                        direccionSimbolo = dispBase & 0xFFF;
                    }
                    else
                    {
                        AgregarError(linea, "Error: Operando fuera de rango");

                        b = 1;
                        p = 1;

                        int flagsError = (x << 3) | (b << 2) | (p << 1) | e;

                        int codigoError =
                            (opcode << 16) |
                            (flagsError << 12) |
                            0xFFF;

                        linea.CodigoObjeto = codigoError.ToString("X6");
                        return;
                    }
                }
                else
                {
                    AgregarError(linea, "Error: No es relativo a PC ni a BASE");

                    b = 1;
                    p = 1;

                    int flagsError = (x << 3) | (b << 2) | (p << 1) | e;

                    int codigoError =
                        (opcode << 16) |
                        (flagsError << 12) |
                        0xFFF;

                    linea.CodigoObjeto = codigoError.ToString("X6");
                    return;
                }
            }

            int flags = (x << 3) | (b << 2) | (p << 1) | e;

            int codigoFinal =
                (opcode << 16) |
                (flags << 12) |
                (direccionSimbolo & 0xFFF);

            linea.CodigoObjeto = codigoFinal.ToString("X6");
        }
        private void GenerarFormato4(LineaIntermedia linea)
        {
            string codop = linea.CodigoOp.Substring(1); // quitar '+'
            string opcodeHex = _opcodes[codop];
            int opcode = Convert.ToInt32(opcodeHex, 16);

            // limpiar últimos 2 bits
            opcode = opcode & 0xFC;

            string operando = linea.Operador?.Trim() ?? "";

            int n = 1, i = 1, x = 0, b = 0, p = 0, e = 1; // e SIEMPRE 1

            // Validación modo inválido
            if (operando.Contains("@") && operando.Contains("#"))
            {
                AgregarError(linea, "Modo de direccionamiento inválido");

                b = 1;
                p = 1;

                int flagsError = (x << 3) | (b << 2) | (p << 1) | e;

                int codigoError =
                    (opcode << 24) |
                    (flagsError << 20) |
                    0xFFFFF; // -1 en 20 bits

                linea.CodigoObjeto = codigoError.ToString("X8");
                return;
            }

            // Determinar n/i
            if (operando.StartsWith("#"))
            {
                n = 0;
                i = 1;
                operando = operando.Substring(1);
            }
            else if (operando.StartsWith("@"))
            {
                n = 1;
                i = 0;
                operando = operando.Substring(1);
            }

            // Determinar x
            if (operando.Contains(",X"))
            {
                x = 1;
                operando = operando.Replace(",X", "");
            }

            opcode = opcode | (n << 1) | i;

            int direccion = 0;

            // Inmediato numérico
            if (int.TryParse(operando, out int valorInmediato))
            {
                direccion = valorInmediato;

                if (direccion < 0 || direccion > 0xFFFFF)
                {
                    AgregarError(linea, "Operando fuera de rango");

                    b = 1;
                    p = 1;

                    int flagsError = (x << 3) | (b << 2) | (p << 1) | e;

                    int codigoError =
                        (opcode << 24) |
                        (flagsError << 20) |
                        0xFFFFF;

                    linea.CodigoObjeto = codigoError.ToString("X8");
                    return;
                }
            }
            else if (operando.EndsWith("H"))
            {
                try
                {
                    string hex = operando.Substring(0, operando.Length - 1);
                    direccion = Convert.ToInt32(hex, 16);

                    if (direccion < 0 || direccion > 0xFFFFF)
                    {
                        AgregarError(linea, "Operando fuera de rango");

                        b = 1;
                        p = 1;

                        int flagsError = (x << 3) | (b << 2) | (p << 1) | e;

                        int codigoError =
                            (opcode << 24) |
                            (flagsError << 20) |
                            0xFFFFF;

                        linea.CodigoObjeto = codigoError.ToString("X8");
                        return;
                    }
                }
                catch
                {
                    AgregarError(linea, "Número hexadecimal inválido");
                    return;
                }
            }
            else
            {
                if (!_tabSim.ContainsKey(operando))
                {
                    AgregarError(linea, "Símbolo no encontrado");

                    b = 1;
                    p = 1;

                    int flagsError = (x << 3) | (b << 2) | (p << 1) | e;

                    int codigoError =
                        (opcode << 24) |
                        (flagsError << 20) |
                        0xFFFFF;

                    linea.CodigoObjeto = codigoError.ToString("X8");
                    return;
                }

                direccion = Convert.ToInt32(_tabSim[operando], 16);
            }

            int flags = (x << 3) | (b << 2) | (p << 1) | e;

            int codigoFinal =
                (opcode << 24) |
                (flags << 20) |
                (direccion & 0xFFFFF);

            string objeto = codigoFinal.ToString("X8");

            // Detectar relocación
            bool esNumero = int.TryParse(operando, out _);

            if (!esNumero &&
                _tabSim.ContainsKey(operando))
            {
                objeto += "*";
            }

            linea.CodigoObjeto = objeto;
        }

        private void AgregarError(LineaIntermedia linea, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(linea.Errores))
                linea.Errores = mensaje;
            else
                linea.Errores += " | " + mensaje;
        }

        public void Generar()
        {
            foreach (var linea in _lineas)
            {
                if (linea.CodigoOp == "BASE")
                {
                    if (_tabSim.ContainsKey(linea.Operador))
                        _baseAddress = Convert.ToInt32(_tabSim[linea.Operador], 16);

                    linea.CodigoObjeto = "----";
                    continue;
                }

                if (linea.CodigoOp == "NOBASE")
                {
                    _baseAddress = null;
                    linea.CodigoObjeto = "----";
                    continue;
                }

                if (EsDirectivaSinCodigo(linea.CodigoOp))
                {
                    linea.CodigoObjeto = "----";
                    continue;
                }

                // DIRECTIVA WORD
                if (linea.CodigoOp == "WORD")
                {
                    GenerarWord(linea);
                    continue;
                }

                // DIRECTIVA BYTE
                if (linea.CodigoOp == "BYTE")
                {
                    GenerarByte(linea);
                    continue;
                }

                // FORMATO 4
                if (EsFormato4(linea.CodigoOp))
                {
                    GenerarFormato4(linea);
                    continue;
                }

                // FORMATO 3
                if (EsFormato3(linea.CodigoOp))
                {
                    GenerarFormato3(linea);
                    continue;
                }

                if (!_opcodes.ContainsKey(linea.CodigoOp))
                {
                    linea.CodigoObjeto = "----";
                    continue;
                }

                string opcode = _opcodes[linea.CodigoOp];

                // FORMATO 1
                if (EsFormato1(linea.CodigoOp))
                {
                    linea.CodigoObjeto = opcode;
                    continue;
                }

                // FORMATO 2
                if (EsFormato2(linea.CodigoOp))
                {
                    GenerarFormato2(linea, opcode);
                    continue;
                }
                linea.CodigoObjeto = "***";
            }
        }
    }


}