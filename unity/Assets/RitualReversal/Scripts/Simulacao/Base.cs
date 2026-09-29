// Base do porte de shared/sim.js: contas que dão o mesmo resultado, bit a bit, que o JavaScript do navegador
// (motor V8), e um leitor de JSON sem depender do Unity. Com isso o C# e o protótipo podem rodar a mesma partida
// lado a lado e ser comparados passo a passo (tools/comparar-cs.js).
// C# puro: nada aqui usa UnityEngine, então também compila e roda fora do Unity (testes com o Mono).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RitualReversal.Simulacao
{
    public static partial class JS
    {
        // Math.random. Nos testes, JS e C# recebem o mesmo gerador com semente (Mulberry32).
        public static Func<double> Random = Padrao;
        static readonly System.Random sistema = new System.Random();
        static double Padrao() { return sistema.NextDouble(); }

        // rand(a, b) do protótipo
        public static double Rand(double a, double b) { return a + Random() * (b - a); }

        public static Func<double> Mulberry32(uint semente)
        {
            uint s = semente;
            return () =>
            {
                s += 0x6D2B79F5u;
                uint t = s;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            };
        }

        // Math.hypot do V8: divide pelo maior valor e soma com compensação de Kahan.
        // Math.Sqrt(x*x + z*z) às vezes difere no último bit, e isso bastaria para as partidas se separarem.
        public static double Hypot(double a, double b)
        {
            double x = Math.Abs(a), y = Math.Abs(b);
            bool nan = double.IsNaN(a) || double.IsNaN(b);
            double max = 0; if (!double.IsNaN(x) && x > max) max = x; if (!double.IsNaN(y) && y > max) max = y;
            if (double.IsPositiveInfinity(max)) return max;
            if (nan) return double.NaN;
            if (max == 0) return 0;
            double soma = 0, comp = 0, n, parcela, prev;
            n = x / max; parcela = n * n - comp; prev = soma + parcela; comp = (prev - soma) - parcela; soma = prev;
            n = y / max; parcela = n * n - comp; prev = soma + parcela; comp = (prev - soma) - parcela; soma = prev;
            return Math.Sqrt(soma) * max;
        }

        public static double Hypot(double a, double b, double c)
        {
            double x = Math.Abs(a), y = Math.Abs(b), z = Math.Abs(c);
            bool nan = double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(c);
            double max = 0; if (!double.IsNaN(x) && x > max) max = x; if (!double.IsNaN(y) && y > max) max = y; if (!double.IsNaN(z) && z > max) max = z;
            if (double.IsPositiveInfinity(max)) return max;
            if (nan) return double.NaN;
            if (max == 0) return 0;
            double soma = 0, comp = 0, n, parcela, prev;
            n = x / max; parcela = n * n - comp; prev = soma + parcela; comp = (prev - soma) - parcela; soma = prev;
            n = y / max; parcela = n * n - comp; prev = soma + parcela; comp = (prev - soma) - parcela; soma = prev;
            n = z / max; parcela = n * n - comp; prev = soma + parcela; comp = (prev - soma) - parcela; soma = prev;
            return Math.Sqrt(soma) * max;
        }

        // Math.round: o meio arredonda para cima (2,5 -> 3; -2,5 -> -2). O Math.Round do C# arredonda para o par.
        public static double Round(double v) { double c = Math.Ceiling(v); return c - 0.5 > v ? c - 1 : c; }
        public static double R2(double v) { return Round(v * 100) / 100; }
        public static double Clamp(double v, double a, double b) { return Math.Max(a, Math.Min(b, v)); }
        public static double Lerp(double a, double b, double t) { return a + (b - a) * t; }
        public static int Floor(double v) { return (int)Math.Floor(v); }

        // número -> texto como o JS faz nas chaves ('r' + x + ',' + z). Só precisa ser igual para números iguais.
        public static string Num(double v)
        {
            if (v == 0) return "0";
            if (v == Math.Floor(v) && Math.Abs(v) < 1e15) return ((long)v).ToString(CultureInfo.InvariantCulture);
            return v.ToString("R", CultureInfo.InvariantCulture);
        }

        // Array.prototype.sort do V8 (TimSort). Com até 63 itens ele faz uma corrida inicial e inserção binária;
        // reproduzir a mesma sequência de comparações importa quando o comparador sorteia (sort(() => random - .5)).
        public static void Sort<T>(List<T> a, Func<T, T, double> cmp)
        {
            int n = a.Count; if (n < 2) return;
            Func<T, T, double> c = (x, y) => { double r = cmp(x, y); return double.IsNaN(r) ? 0 : r; };
            if (n >= 64) { OrdenarEstavel(a, c); return; }
            int corrida = 2;
            double ordem = c(a[1], a[0]); bool desc = ordem < 0; T ant = a[1];
            for (int i = 2; i < n; i++)
            {
                T atual = a[i]; ordem = c(atual, ant);
                if (desc) { if (ordem >= 0) break; } else { if (ordem < 0) break; }
                ant = atual; corrida++;
            }
            if (desc) a.Reverse(0, corrida);
            for (int ini = corrida; ini < n; ini++)
            {
                int esq = 0, dir = ini; T pivo = a[ini];
                while (esq < dir) { int meio = esq + ((dir - esq) >> 1); if (c(pivo, a[meio]) < 0) dir = meio; else esq = meio + 1; }
                for (int p = ini; p > esq; p--) a[p] = a[p - 1];
                a[esq] = pivo;
            }
        }

        static void OrdenarEstavel<T>(List<T> a, Func<T, T, double> c)
        {
            var idx = new List<KeyValuePair<int, T>>(); for (int i = 0; i < a.Count; i++) idx.Add(new KeyValuePair<int, T>(i, a[i]));
            idx.Sort((p, q) => { double r = c(p.Value, q.Value); return r < 0 ? -1 : r > 0 ? 1 : p.Key.CompareTo(q.Key); });
            for (int i = 0; i < a.Count; i++) a[i] = idx[i].Value;
        }

        // arr.sort(cmp)[0] sem alterar a lista original (o JS usa filter(...).sort(...)[0], que já trabalha numa cópia)
        public static T Primeiro<T>(List<T> a, Func<T, T, double> cmp) where T : class
        {
            if (a.Count == 0) return null; var copia = new List<T>(a); Sort(copia, cmp); return copia[0];
        }
    }

    // Leitor de JSON mínimo: objetos viram Dictionary<string, object>, listas viram List<object>, números viram double.
    public static class Json
    {
        public static object Ler(string s) { int i = 0; var v = Valor(s, ref i); return v; }

        static void Espaco(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Valor(string s, ref int i)
        {
            Espaco(s, ref i);
            char ch = s[i];
            if (ch == '{')
            {
                var o = new Dictionary<string, object>(); i++; Espaco(s, ref i);
                if (s[i] == '}') { i++; return o; }
                while (true)
                {
                    Espaco(s, ref i); string k = Texto(s, ref i); Espaco(s, ref i); i++; // ':'
                    o[k] = Valor(s, ref i); Espaco(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return o; // '}'
                }
            }
            if (ch == '[')
            {
                var l = new List<object>(); i++; Espaco(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Valor(s, ref i)); Espaco(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return l; // ']'
                }
            }
            if (ch == '"') return Texto(s, ref i);
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int ini = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(ini, i - ini), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static string Texto(string s, ref int i)
        {
            var sb = new StringBuilder(); i++; // '"'
            while (s[i] != '"')
            {
                char ch = s[i++];
                if (ch != '\\') { sb.Append(ch); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                    default: sb.Append(e); break;
                }
            }
            i++; return sb.ToString();
        }

        // atalhos para ler a árvore
        public static Dictionary<string, object> O(object v) { return (Dictionary<string, object>)v; }
        public static List<object> L(object v) { return (List<object>)v; }
        public static double D(object v) { return (double)v; }
        public static string S(object v) { return (string)v; }
    }
}
