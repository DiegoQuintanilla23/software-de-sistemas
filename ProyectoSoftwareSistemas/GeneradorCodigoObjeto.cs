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
        private Dictionary<string, Simbolo> _tabSim;
        private Dictionary<string, Bloque> _tabblk;
        private List<LineaIntermedia> _lineas;
        private Dictionary<string, int> _registros;
        private int? _baseAddress = null;
        private EvaluadorExpresiones _evaluador;

        private Dictionary<string, string> _opcodes;
        public GeneradorCodigoObjeto(Dictionary<string, Simbolo> TABSIM, List<LineaIntermedia> lineas, Dictionary<string, Bloque> tabblk)
        {
            _tabSim = TABSIM;
            _tabblk = tabblk;
            _lineas = lineas;
            _evaluador = new EvaluadorExpresiones(_tabSim, _tabblk);
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

        private bool EsSalto(string opcode)
        {
            return opcode == "J" || opcode == "JEQ" || opcode == "JGT"
                || opcode == "JLT" || opcode == "JSUB";
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

        private string ObtenerBanderasModulo(ResultadoEvaluacion res)
        {
            string banderas = "";

            if (res.EsRelativo)
                banderas += "*R";

            foreach (var kv in res.SimbolosExternos)
            {
                if (kv.Value > 0)
                {
                    for (int i = 0; i < kv.Value; i++) banderas += "*SE";
                }
                else if (kv.Value < 0)
                {
                    for (int i = 0; i < Math.Abs(kv.Value); i++) banderas += "*SE"; // El PDF nuevo usa *SE incluso para restas en el REF3
                }
            }

            return banderas;
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

            var resultado = _evaluador.Evaluar(linea, Convert.ToInt32(linea.ContadorPrograma, 16));

            if (resultado.Error)
            {
                AgregarError(linea, resultado.MensajeError);
                linea.CodigoObjeto = "FFFFFF";
                return;
            }

            int valor = resultado.Valor;

            // Ajuste por bloques relativos reales
            foreach (var kv in resultado.BloquesRelativos)
            {
                string bloque = kv.Key;
                int coef = kv.Value;

                if (_tabblk.ContainsKey(bloque))
                {
                    valor += coef * _tabblk[bloque].DirInicial;
                }
            }

            valor = valor & 0xFFFFFF;
            string objeto = valor.ToString("X6");

            // Agregamos banderas de Modificación (*R, *SE, #SE)
            objeto += ObtenerBanderasModulo(resultado);

            linea.CodigoObjeto = objeto;
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

                // Si es impar - completar con 0 a la izquierda
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

            if (registros.Length >= 1 && _registros.ContainsKey(registros[0].Trim()))
            {
                r1 = _registros[registros[0].Trim()];
            }
            else
            {
                AgregarError(linea, "Error: Registro inválido en primer operando");
                return;
            }

            if (registros.Length == 2)
            {
                string op2 = registros[1].Trim();

                if (linea.CodigoOp == "SHIFTL" || linea.CodigoOp == "SHIFTR")
                {
                    if (!int.TryParse(op2, out r2))
                    {
                        AgregarError(linea, "Error: Segundo operando debe ser numérico");
                        return;
                    }
                    r2 = (r2 - 1) & 0xF; // ← FIX: SIC/XE almacena n-1
                }
                else
                {
                    if (_registros.ContainsKey(op2))
                    {
                        r2 = _registros[op2];
                    }
                    else
                    {
                        AgregarError(linea, "Error: Registro inválido en segundo operando");
                        return;
                    }
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

        private int ObtenerCpAbsoluto(LineaIntermedia linea)
        {
            int cpRelativo = Convert.ToInt32(linea.ContadorPrograma, 16);

            // Buscar el bloque por NumeroBloque
            var bloque = _tabblk.Values.FirstOrDefault(b => b.Numero == linea.NumeroBloque);
            int dirInicial = bloque != null ? bloque.DirInicial : 0;

            return cpRelativo + dirInicial;
        }

        private void GenerarFormato3(LineaIntermedia linea)
        {
            string codop = linea.CodigoOp;

            if (codop == "RSUB")
            {
                linea.CodigoObjeto = "4F0000";
                return;
            }

            int opcode = Convert.ToInt32(_opcodes[codop], 16);
            opcode &= 0xFC;

            string operando = linea.Operador?.Trim().ToUpper() ?? "";

            int n = 1, i = 1, x = 0, b = 0, p = 0, e = 0;

            if (operando.StartsWith("#"))
            {
                n = 0; i = 1;
                operando = operando.Substring(1);
            }
            else if (operando.StartsWith("@"))
            {
                n = 1; i = 0;
                operando = operando.Substring(1);
            }

            if (operando.Contains(",X"))
            {
                x = 1;
                operando = operando.Replace(",X", "");
            }

            opcode |= (n << 1) | i;

            int cpAbsoluto = ObtenerCpAbsoluto(linea); // ← FIX

            var res = _evaluador.Evaluar(
                new LineaIntermedia { Operador = operando },
                cpAbsoluto // ← FIX
            );

            if (res.Error)
            {
                AgregarError(linea, res.MensajeError);

                int flagsError = (x << 3) | (1 << 2) | (1 << 1) | e;

                int codigoError =
                    (opcode << 16) |
                    (flagsError << 12) |
                    0xFFF;

                linea.CodigoObjeto = codigoError.ToString("X6");
                return;
            }

            if (res.SimbolosExternos.Count > 0)
            {
                AgregarError(linea, "Error: Símbolos externos requieren Formato 4 (+)");
                linea.CodigoObjeto = ((opcode << 16) | (0x3 << 12) | 0xFFF).ToString("X6");
                return;
            }

            // ← FIX: solo importa RelCount neto, no cuántos bloques participaron
            bool esDireccionValida =
                res.RelCount == 1 ||
                (res.RelCount == 0 && res.BloquesRelativos.Values.All(v => v == 0));

            if (!esDireccionValida)
            {
                AgregarError(linea, "Expresión no válida como dirección");
                linea.CodigoObjeto = ((opcode << 16) | (0x3 << 12) | 0xFFF).ToString("X6");
                return;
            }

            int direccion = res.Valor;

            foreach (var kv in res.BloquesRelativos)
            {
                if (_tabblk.ContainsKey(kv.Key))
                    direccion += kv.Value * _tabblk[kv.Key].DirInicial;
            }

            int disp = 0;

            if (n == 0 && i == 1 && res.RelCount == 0)
            {
                // Inmediato absoluto
                if (direccion < 0 || direccion > 0xFFF)
                {
                    AgregarError(linea, "Constante fuera de rango");
                    linea.CodigoObjeto = ((opcode << 16) | (0x3 << 12) | 0xFFF).ToString("X6");
                    return;
                }
                disp = direccion;
            }
            else if (res.RelCount == 1)
            {
                int pc = cpAbsoluto + 3; // ← FIX
                int delta = direccion - pc;

                if (delta >= -2048 && delta <= 2047)
                {
                    p = 1;
                    disp = delta & 0xFFF;
                }
                else if (_baseAddress.HasValue)
                {
                    int baseDisp = direccion - _baseAddress.Value;

                    if (baseDisp >= 0 && baseDisp <= 4095)
                    {
                        b = 1;
                        disp = baseDisp;
                    }
                    else
                    {
                        AgregarError(linea, "Fuera de rango");
                        linea.CodigoObjeto = ((opcode << 16) | (0x3 << 12) | 0xFFF).ToString("X6");
                        return;
                    }
                }
                else
                {
                    AgregarError(linea, "No relativo");
                    linea.CodigoObjeto = ((opcode << 16) | (0x3 << 12) | 0xFFF).ToString("X6");
                    return;
                }
            }
            else
            {
                // Absoluto directo
                if (direccion < 0 || direccion > 0xFFF)
                {
                    AgregarError(linea, "Fuera de rango");
                    linea.CodigoObjeto = ((opcode << 16) | (0x3 << 12) | 0xFFF).ToString("X6");
                    return;
                }
                disp = direccion;
            }

            int flags = (x << 3) | (b << 2) | (p << 1) | e;

            int codigo =
                (opcode << 16) |
                (flags << 12) |
                (disp & 0xFFF);

            linea.CodigoObjeto = codigo.ToString("X6");
        }

        private void GenerarFormato4(LineaIntermedia linea)
        {
            string codop = linea.CodigoOp.Substring(1);
            int opcode = Convert.ToInt32(_opcodes[codop], 16);
            opcode &= 0xFC;

            string operando = linea.Operador?.Trim().ToUpper() ?? "";

            int n = 1, i = 1, x = 0, b = 0, p = 0, e = 1;

            if (operando.StartsWith("#"))
            {
                n = 0; i = 1;
                operando = operando.Substring(1);
            }
            else if (operando.StartsWith("@"))
            {
                n = 1; i = 0;
                operando = operando.Substring(1);
            }

            if (operando.Contains(",X"))
            {
                x = 1;
                operando = operando.Replace(",X", "");
            }

            opcode |= (n << 1) | i;

            int cpAbsoluto = ObtenerCpAbsoluto(linea); // ← FIX

            var res = _evaluador.Evaluar(
                new LineaIntermedia { Operador = operando },
                cpAbsoluto // ← FIX
            );

            if (res.Error)
            {
                AgregarError(linea, res.MensajeError);
                linea.CodigoObjeto = ((opcode << 24) | (0x3 << 20) | 0xFFFFF).ToString("X8");
                return;
            }

            // ← FIX: solo importa RelCount neto
            bool esDireccionValida =
                res.RelCount == 1 ||
                (res.RelCount == 0 && res.BloquesRelativos.Values.All(v => v == 0));

            if (!esDireccionValida)
            {
                AgregarError(linea, "Expresión no válida como dirección");
                linea.CodigoObjeto = ((opcode << 24) | (0x3 << 20) | 0xFFFFF).ToString("X8");
                return;
            }

            int direccion = res.Valor;

            foreach (var kv in res.BloquesRelativos)
            {
                if (_tabblk.ContainsKey(kv.Key))
                    direccion += kv.Value * _tabblk[kv.Key].DirInicial;
            }

            if (direccion < 0 || direccion > 0xFFFFF)
            {
                AgregarError(linea, "Fuera de rango");
                linea.CodigoObjeto = ((opcode << 24) | (0x3 << 20) | 0xFFFFF).ToString("X8");
                return;
            }

            int flags = (x << 3) | (b << 2) | (p << 1) | e;

            int codigo =
                (opcode << 24) |
                (flags << 20) |
                (direccion & 0xFFFFF);

            string obj = codigo.ToString("X8");

            // Agregamos banderas de Modificación a las extendidas
            obj += ObtenerBanderasModulo(res);

            linea.CodigoObjeto = obj;
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
                        _baseAddress = _tabSim[linea.Operador].Direccion;

                    linea.CodigoObjeto = "----";
                    continue;
                }

                if (linea.CodigoOp == "NOBASE")
                {
                    _baseAddress = null;
                    linea.CodigoObjeto = "----";
                    continue;
                }

                if (linea.CodigoOp == "END")
                {
                    if (!string.IsNullOrWhiteSpace(linea.Operador))
                    {
                        string simbolo = linea.Operador.Trim().ToUpper();

                        if (!_tabSim.ContainsKey(simbolo))
                        {
                            AgregarError(linea, $"Símbolo no definido: {simbolo}");
                        }
                    }

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
                    if (!string.IsNullOrWhiteSpace(linea.Errores))
                    {
                        linea.CodigoObjeto = "---";
                        continue;
                    }
                    linea.CodigoObjeto = opcode;
                    continue;
                }

                // FORMATO 2
                if (EsFormato2(linea.CodigoOp))
                {
                    if (!string.IsNullOrWhiteSpace(linea.Errores))
                    {
                        linea.CodigoObjeto = "---";
                        continue;
                    }
                    GenerarFormato2(linea, opcode);
                    continue;
                }
                linea.CodigoObjeto = "***";
            }
        }
    }


}