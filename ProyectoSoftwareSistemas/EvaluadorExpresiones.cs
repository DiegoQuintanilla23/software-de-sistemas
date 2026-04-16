using System;
using System.Collections.Generic;

namespace ProyectoSoftwareSistemas
{
    public class ResultadoEvaluacion
    {
        public int Valor { get; set; }

        public int RelCount { get; set; } = 0;

        public string Tipo { get; set; } = "A";

        public bool EsRelativo => RelCount == 1;

        public bool Error { get; set; } = false;
        public string MensajeError { get; set; } = "";
        public Dictionary<string, int> BloquesRelativos { get; set; } = new Dictionary<string, int>();
    }

    public class EvaluadorExpresiones
    {
        private Dictionary<string, Simbolo> _tabSim;
        private Dictionary<string, Bloque> _tabblk;
        private string _expr;
        private int _pos;
        private int _cp;

        public EvaluadorExpresiones(Dictionary<string, Simbolo> tabSim, Dictionary<string, Bloque> tabblk)
        {
            _tabSim = tabSim;
            _tabblk = tabblk;
        }

        public ResultadoEvaluacion Evaluar(LineaIntermedia linea, int contadorPrograma)
        {
            _expr = linea.Operador?.Replace(" ", "").ToUpper();
            _pos = 0;
            _cp = contadorPrograma;

            if (string.IsNullOrEmpty(_expr))
                return Error("Expresión vacía");

            if (_expr == "*")
                return Ok(_cp, true);

            var res = ParseExpresion();

            if (res.Error) return res;

            if (_pos < _expr.Length)
                return Error("Error de sintaxis");

            // VALIDACIÓN FINAL DE RELATIVOS
            if (res.RelCount > 1 || res.RelCount < -1)
                return Error("Expresión relativa inválida");

            res.Tipo = res.EsRelativo ? "R" : "A";

            return res;
        }


        // + -
        private ResultadoEvaluacion ParseExpresion()
        {
            var left = ParseTermino();

            while (_pos < _expr.Length)
            {
                char op = _expr[_pos];

                if (op != '+' && op != '-')
                    break;

                _pos++;

                var right = ParseTermino();

                if (left.Error) return left;
                if (right.Error) return right;

                int valor = (op == '+')
                    ? left.Valor + right.Valor
                    : left.Valor - right.Valor;

                int relA = left.RelCount;
                int relB = right.RelCount;

                if (op == '-')
                    relB *= -1;

                int suma = relA + relB;

                var nuevo = new ResultadoEvaluacion
                {
                    Valor = valor,
                    RelCount = suma
                };

                // combinar bloques
                foreach (var kv in left.BloquesRelativos)
                {
                    nuevo.BloquesRelativos[kv.Key] = kv.Value;
                }

                foreach (var kv in right.BloquesRelativos)
                {
                    int signo = (op == '-') ? -1 : 1;

                    if (!nuevo.BloquesRelativos.ContainsKey(kv.Key))
                        nuevo.BloquesRelativos[kv.Key] = 0;

                    nuevo.BloquesRelativos[kv.Key] += signo * kv.Value;
                }

                left = nuevo;
            }

            return left;
        }


        // * /
        private ResultadoEvaluacion ParseTermino()
        {
            var left = ParseFactor();

            while (_pos < _expr.Length)
            {
                char op = _expr[_pos];
                if (op != '*' && op != '/') break;
                _pos++;

                var right = ParseFactor();
                if (left.Error) return left;
                if (right.Error) return right;

                if (left.RelCount != 0 || right.RelCount != 0)
                    return Error("Relativos no permitidos en * o /");

                // ← NUEVO: aplicar ajustes de bloque antes de operar
                int valorLeft = left.Valor;
                foreach (var kv in left.BloquesRelativos)
                    if (_tabblk.ContainsKey(kv.Key))
                        valorLeft += kv.Value * _tabblk[kv.Key].DirInicial;

                int valorRight = right.Valor;
                foreach (var kv in right.BloquesRelativos)
                    if (_tabblk.ContainsKey(kv.Key))
                        valorRight += kv.Value * _tabblk[kv.Key].DirInicial;

                int valor;
                if (op == '*')
                    valor = valorLeft * valorRight;
                else
                {
                    if (valorRight == 0) return Error("División entre cero");
                    valor = valorLeft / valorRight;
                }

                left = Ok(valor, false); // BloquesRelativos ya consumidos, resultado absoluto
            }

            return left;
        }

        private ResultadoEvaluacion ParseFactor()
        {
            if (_pos >= _expr.Length)
                return Error("Expresión incompleta");

            char c = _expr[_pos];

            // signo unario
            if (c == '+' || c == '-')
            {
                _pos++;
                var factor = ParseFactor();

                if (factor.Error) return factor;

                if (c == '-')
                    factor.Valor = -factor.Valor;

                return factor;
            }

            // paréntesis
            if (c == '(')
            {
                _pos++;
                var res = ParseExpresion();

                if (_pos >= _expr.Length || _expr[_pos] != ')')
                    return Error("Paréntesis desbalanceados");

                _pos++;
                return res;
            }

            // token
            string token = "";

            while (_pos < _expr.Length &&
                   char.IsLetterOrDigit(_expr[_pos]))
            {
                token += _expr[_pos++];
            }

            if (token == "")
                return Error("Token inválido");

            return EvaluarToken(token);
        }

        
        private ResultadoEvaluacion EvaluarToken(string token)
        {
            // número decimal
            if (int.TryParse(token, out int num))
                return Ok(num, false);

            // hexadecimal
            if (token.EndsWith("H"))
            {
                string hex = token.Substring(0, token.Length - 1);

                if (System.Text.RegularExpressions.Regex.IsMatch(hex, "^[0-9A-F]+$"))
                {
                    return Ok(Convert.ToInt32(hex, 16), false);
                }
            }

            // *
            if (token == "*")
                return Ok(_cp, true);

            // símbolo
            if (_tabSim.ContainsKey(token))
            {
                var s = _tabSim[token];

                int valor = s.Direccion;

                return Ok(valor, s.EsRelativo, s.Bloque);
            }

            return Error($"Símbolo no definido: {token}");
        }


        private ResultadoEvaluacion Ok(int valor, bool relativo, string bloque = null)
        {
            var res = new ResultadoEvaluacion
            {
                Valor = valor,
                RelCount = relativo ? 1 : 0,
                Tipo = relativo ? "R" : "A"
            };

            if (relativo && bloque != null)
            {
                res.BloquesRelativos[bloque] = 1;
            }

            return res;
        }

        private ResultadoEvaluacion Error(string msg)
        {
            return new ResultadoEvaluacion
            {
                Error = true,
                MensajeError = msg,
                Valor = -1,
                Tipo = "A",
                RelCount = 0
            };
        }
    }
}