using System;
using System.Collections.Generic;

namespace ProyectoSoftwareSistemas
{
    public class ResultadoEvaluacion
    {
        public int Valor { get; set; }
        public string Tipo { get; set; } = "A";
        public bool EsRelativo { get; set; } = false;

        public bool Error { get; set; } = false;
        public string MensajeError { get; set; } = "";
    }

    public class EvaluadorExpresiones
    {
        private Dictionary<string, Simbolo> _tabSim;
        private string _expr;
        private int _pos;
        private int _cp;

        public EvaluadorExpresiones(Dictionary<string, Simbolo> tabSim)
        {
            _tabSim = tabSim;
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

                int relA = left.EsRelativo ? 1 : 0;
                int relB = right.EsRelativo ? 1 : 0;

                if (op == '-')
                    relB *= -1;

                int suma = relA + relB;

                if (suma == 0)
                    left = Ok(valor, false);
                else if (suma == 1)
                    left = Ok(valor, true);
                else
                    return Error("Expresión relativa inválida");
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

                if (op != '*' && op != '/')
                    break;

                _pos++;

                var right = ParseFactor();

                if (left.Error) return left;
                if (right.Error) return right;

                // - Regla SIC/XE
                if (left.EsRelativo || right.EsRelativo)
                    return Error("Relativos no permitidos en * o /");

                int valor;

                if (op == '*')
                    valor = left.Valor * right.Valor;
                else
                {
                    if (right.Valor == 0)
                        return Error("División entre cero");

                    valor = left.Valor / right.Valor;
                }

                left = Ok(valor, false);
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
                try
                {
                    return Ok(Convert.ToInt32(token.Replace("H", ""), 16), false);
                }
                catch
                {
                    return Error("Hex inválido");
                }
            }

            // *
            if (token == "*")
                return Ok(_cp, true);

            // símbolo
            if (_tabSim.ContainsKey(token))
            {
                var s = _tabSim[token];
                return Ok(s.Direccion, s.EsRelativo);
            }

            return Error($"Símbolo no definido: {token}");
        }

        
        private ResultadoEvaluacion Ok(int valor, bool relativo)
        {
            return new ResultadoEvaluacion
            {
                Valor = valor,
                EsRelativo = relativo,
                Tipo = relativo ? "R" : "A"
            };
        }

        private ResultadoEvaluacion Error(string msg)
        {
            return new ResultadoEvaluacion
            {
                Error = true,
                MensajeError = msg,
                Valor = -1,
                Tipo = "A",
                EsRelativo = false
            };
        }
    }
}