using System;
using System.Collections.Generic;
using System.Linq;

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
        public Dictionary<string, int> SimbolosExternos { get; set; } = new Dictionary<string, int>();
    }

    public class EvaluadorExpresiones
    {
        private Dictionary<string, Simbolo> _tabSim;
        private Dictionary<string, Bloque> _tabblk;
        private string _expr;
        private int _pos;
        private int _cp;
        private string _nombreBloqueActual;

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

            // Capturamos en qué bloque estamos parados actualmente para resolver los asteriscos (*)
            var b = _tabblk.Values.FirstOrDefault(x => x.Numero == linea.NumeroBloque);
            _nombreBloqueActual = b?.Nombre ?? "DEFAULT";

            if (string.IsNullOrEmpty(_expr)) return Error("Expresión vacía");

            // Manejo seguro del contador de programa actual
            if (_expr == "*") return Ok(_cp, true, _nombreBloqueActual);

            var res = ParseExpresion();
            if (res.Error) return res;
            if (_pos < _expr.Length) return Error("Error de sintaxis");

            if (res.RelCount > 1 || res.RelCount < -1) return Error("Expresión relativa inválida");
            res.Tipo = res.EsRelativo ? "R" : "A";
            return res;
        }

        private ResultadoEvaluacion ParseExpresion()
        {
            var left = ParseTermino();

            while (_pos < _expr.Length)
            {
                char op = _expr[_pos];
                if (op != '+' && op != '-') break;
                _pos++;

                var right = ParseTermino();
                if (left.Error) return left;
                if (right.Error) return right;

                int valor = (op == '+') ? left.Valor + right.Valor : left.Valor - right.Valor;
                int relA = left.RelCount;
                int relB = right.RelCount;
                if (op == '-') relB *= -1;

                var nuevo = new ResultadoEvaluacion
                {
                    Valor = valor,
                    RelCount = relA + relB
                };

                // 1. RECUPERAMOS PROPAGACIÓN DE BLOQUES RELATIVOS
                foreach (var kv in left.BloquesRelativos) nuevo.BloquesRelativos[kv.Key] = kv.Value;
                foreach (var kv in right.BloquesRelativos)
                {
                    int signo = (op == '-') ? -1 : 1;
                    if (!nuevo.BloquesRelativos.ContainsKey(kv.Key)) nuevo.BloquesRelativos[kv.Key] = 0;
                    nuevo.BloquesRelativos[kv.Key] += signo * kv.Value;
                }

                // 2. PROPAGACIÓN DE SÍMBOLOS EXTERNOS
                foreach (var kv in left.SimbolosExternos) nuevo.SimbolosExternos[kv.Key] = kv.Value;
                foreach (var kv in right.SimbolosExternos)
                {
                    int signo = (op == '-') ? -1 : 1;
                    if (!nuevo.SimbolosExternos.ContainsKey(kv.Key)) nuevo.SimbolosExternos[kv.Key] = 0;
                    nuevo.SimbolosExternos[kv.Key] += signo * kv.Value;
                }

                left = nuevo;
            }
            return left;
        }

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

                if (left.RelCount != 0 || right.RelCount != 0) return Error("Relativos no permitidos en * o /");
                if (left.SimbolosExternos.Count > 0 || right.SimbolosExternos.Count > 0) return Error("Símbolos externos no permitidos en * o /");

                // Aplicar multiplicadores de inicio de bloque si existen
                int valorLeft = left.Valor;
                foreach (var kv in left.BloquesRelativos)
                    if (_tabblk.ContainsKey(kv.Key)) valorLeft += kv.Value * _tabblk[kv.Key].DirInicial;

                int valorRight = right.Valor;
                foreach (var kv in right.BloquesRelativos)
                    if (_tabblk.ContainsKey(kv.Key)) valorRight += kv.Value * _tabblk[kv.Key].DirInicial;

                int valor = (op == '*') ? valorLeft * valorRight : (valorRight == 0 ? 0 : valorLeft / valorRight);
                if (op == '/' && valorRight == 0) return Error("División entre cero");

                left = Ok(valor, false);
            }
            return left;
        }

        private ResultadoEvaluacion ParseFactor()
        {
            if (_pos >= _expr.Length) return Error("Expresión incompleta");
            char c = _expr[_pos];

            if (c == '+' || c == '-')
            {
                _pos++;
                var factor = ParseFactor();
                if (factor.Error) return factor;
                if (c == '-')
                {
                    factor.Valor = -factor.Valor;
                    factor.RelCount = -factor.RelCount;

                    // Invertir diccionarios algebraicamente
                    var nuevosBloques = new Dictionary<string, int>();
                    foreach (var kv in factor.BloquesRelativos) nuevosBloques[kv.Key] = -kv.Value;
                    factor.BloquesRelativos = nuevosBloques;

                    var nuevosExt = new Dictionary<string, int>();
                    foreach (var kv in factor.SimbolosExternos) nuevosExt[kv.Key] = -kv.Value;
                    factor.SimbolosExternos = nuevosExt;
                }
                return factor;
            }

            if (c == '(')
            {
                _pos++;
                var res = ParseExpresion();
                if (_pos >= _expr.Length || _expr[_pos] != ')') return Error("Paréntesis desbalanceados");
                _pos++;
                return res;
            }

            string token = "";
            while (_pos < _expr.Length && char.IsLetterOrDigit(_expr[_pos])) token += _expr[_pos++];
            if (token == "") return Error("Token inválido");
            return EvaluarToken(token);
        }

        private ResultadoEvaluacion EvaluarToken(string token)
        {
            if (int.TryParse(token, out int num)) return Ok(num, false);

            if (token.EndsWith("H"))
            {
                string hex = token.Substring(0, token.Length - 1);
                if (System.Text.RegularExpressions.Regex.IsMatch(hex, "^[0-9A-F]+$"))
                    return Ok(Convert.ToInt32(hex, 16), false);
            }

            if (token == "*") return Ok(_cp, true, _nombreBloqueActual);

            if (_tabSim.ContainsKey(token))
            {
                var s = _tabSim[token];
                if (s.Tipo == "E")
                {
                    var resE = new ResultadoEvaluacion { Valor = 0, RelCount = 0, Tipo = "E" };
                    resE.SimbolosExternos[token] = 1;
                    return resE;
                }
                return Ok(s.Direccion, s.EsRelativo, s.Bloque);
            }
            return Error($"Símbolo no definido: {token}");
        }

        private ResultadoEvaluacion Ok(int valor, bool relativo, string bloque = null)
        {
            var res = new ResultadoEvaluacion { Valor = valor, RelCount = relativo ? 1 : 0, Tipo = relativo ? "R" : "A" };
            if (relativo && !string.IsNullOrEmpty(bloque)) res.BloquesRelativos[bloque] = 1;
            return res;
        }

        private ResultadoEvaluacion Error(string msg)
        {
            return new ResultadoEvaluacion { Error = true, MensajeError = msg, Valor = -1, Tipo = "A", RelCount = 0 };
        }
    }
}