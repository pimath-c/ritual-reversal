// Desenho 2D para as telas (o SVG do navegador): uma tela de pixels com retângulos, círculos e linhas suavizados, nas
// unidades do viewBox (y para baixo), que vira uma Texture2D. Serve à planta fixa (milhares de peças, desenhada uma vez)
// e à linha do tempo da noite. Para o que muda a cada quadro, os traços do IMGUI (Tracos.cs).
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public class Desenho
    {
        readonly int L, A; readonly float k; readonly Color[] px;
        public Desenho(float largura, float altura, float pixelsPorUnidade, Color fundo)
        {
            k = pixelsPorUnidade; L = Mathf.CeilToInt(largura * k); A = Mathf.CeilToInt(altura * k); px = new Color[L * A];
            for (int i = 0; i < px.Length; i++) px[i] = fundo;
        }
        public static Color Hex(string h, float a = 1) { Color c; if (!ColorUtility.TryParseHtmlString(h, out c)) c = Color.magenta; c.a = a; return c; }

        void Pinta(int x, int y, Color c, float cob)
        {
            if (x < 0 || y < 0 || x >= L || y >= A || cob <= 0) return; float a = c.a * Mathf.Min(1, cob); int i = (A - 1 - y) * L + x; var d = px[i];
            float ao = a + d.a * (1 - a); if (ao <= 0) return;
            px[i] = new Color((c.r * a + d.r * d.a * (1 - a)) / ao, (c.g * a + d.g * d.a * (1 - a)) / ao, (c.b * a + d.b * d.a * (1 - a)) / ao, ao);
        }
        // retângulo com a cobertura exata nas bordas
        public void Ret(float x, float y, float w, float h, Color c)
        {
            float x0 = x * k, y0 = y * k, x1 = (x + w) * k, y1 = (y + h) * k;
            for (int j = Mathf.FloorToInt(y0); j < Mathf.CeilToInt(y1); j++)
            {
                float cy = Mathf.Clamp01(Mathf.Min(j + 1, y1) - Mathf.Max(j, y0)); if (cy <= 0) continue;
                for (int i = Mathf.FloorToInt(x0); i < Mathf.CeilToInt(x1); i++) Pinta(i, j, c, cy * Mathf.Clamp01(Mathf.Min(i + 1, x1) - Mathf.Max(i, x0)));
            }
        }
        // contorno de retângulo (stroke do SVG: metade para dentro, metade para fora)
        public void RetBorda(float x, float y, float w, float h, float e, Color c)
        {
            Ret(x - e / 2, y - e / 2, w + e, e, c); Ret(x - e / 2, y + h - e / 2, w + e, e, c); Ret(x - e / 2, y + e / 2, e, h - e, c); Ret(x + w - e / 2, y + e / 2, e, h - e, c);
        }
        public void Circulo(float cx, float cy, float r, Color c)
        {
            float X = cx * k, Y = cy * k, R = r * k;
            for (int j = Mathf.FloorToInt(Y - R - 1); j <= Mathf.CeilToInt(Y + R + 1); j++)
                for (int i = Mathf.FloorToInt(X - R - 1); i <= Mathf.CeilToInt(X + R + 1); i++)
                {
                    float d = Mathf.Sqrt((i + .5f - X) * (i + .5f - X) + (j + .5f - Y) * (j + .5f - Y)); Pinta(i, j, c, Mathf.Clamp01(R - d + .5f));
                }
        }
        // linha grossa com pontas redondas; traco/vao > 0 desenham tracejado (stroke-dasharray), contado ao longo da polilinha
        public void Polilinha(IList<Vector2> p, float largura, Color c, float traco = 0, float vao = 0)
        {
            if (p == null || p.Count < 2) return; float meia = largura * k / 2, s0 = 0;
            // cada pixel pinta uma vez só por polilinha (as juntas não escurecem): a maior cobertura vence
            var cob = new Dictionary<int, float>();
            for (int n = 0; n + 1 < p.Count; n++)
            {
                Vector2 a = p[n] * k, b = p[n + 1] * k, ab = b - a; float len = ab.magnitude;
                int x0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - meia - 1), x1 = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + meia + 1), y0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - meia - 1), y1 = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + meia + 1);
                for (int j = y0; j <= y1; j++) for (int i = x0; i <= x1; i++)
                    {
                        if (i < 0 || j < 0 || i >= L || j >= A) continue;
                        var q = new Vector2(i + .5f, j + .5f); float t = len > 0 ? Mathf.Clamp(Vector2.Dot(q - a, ab) / (len * len), 0, 1) : 0;
                        if (traco > 0) { float s = (s0 + t * len) / k, per = traco + vao; if (s % per > traco) continue; }
                        float v = Mathf.Clamp01(meia - Vector2.Distance(q, a + ab * t) + .5f); if (v <= 0) continue;
                        int key = j * L + i; float o; if (!cob.TryGetValue(key, out o) || v > o) cob[key] = v;
                    }
                s0 += len;
            }
            foreach (var kv in cob) Pinta(kv.Key % L, kv.Key / L, c, kv.Value);
        }
        public Texture2D Textura()
        {
            var t = new Texture2D(L, A, TextureFormat.RGBA32, false); t.SetPixels(px); t.Apply(false, true); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; return t;
        }
    }

    // a camada fixa da planta do navegador (plantaFixa), num viewBox de 540 × 380 desenhado a 2 pixels por unidade
    public static class PlantaFixa
    {
        public const float L = 540, A = 380;
        static Texture2D tex;
        public static float S { get { return Mathf.Min(500f / (float)(Mapa.HX * 2), 350f / (float)(Mapa.HZ * 2)); } }
        public static float X(double x) { return 270 + (float)x * S; }
        public static float Z(double z) { return 190 + (float)z * S; }
        static readonly Dictionary<string, string> COR_PECA = new Dictionary<string, string> {
            {"catedral","#6a645a"},{"fachada","#6a645a"},{"torre_oeste","#6a645a"},{"torre_leste","#5a554c"},{"abside","#6a645a"},{"jube","#5a554c"},{"claustro","#5a554c"},
            {"parapeito","#4a4438"},{"penhasco","#4e4a40"},{"ruina","#4e4e46"},{"pilar_ruina","#5e5e55"},{"estatua","#8a8a80"},{"sebe","#3a5a3c"},{"ossos","#8a7f68"},{"menir","#77746a"},
            {"carvalho","#4a3a2e"},{"tenda","#5a4c34"},{"madeira","#6a5040"},{"mausoleu","#58524a"},{"torre","#6a645a"},{"limite","#2b3328"} };

        static string texMapa;
        public static Texture2D Textura()
        {
            if (!MapaVisual.Carregado) { var ta = Resources.Load<TextAsset>("mapa"); if (ta != null) MapaVisual.Carregar(ta.text); }
            if (tex != null && texMapa == MapaVisual.ID) return tex;
            Mapa.Usar(MapaVisual.ID); texMapa = MapaVisual.ID;   // a escala (S) e os limites vêm do mapa da simulação
            if (MapaVisual.Asilo) return tex = TexturaAsilo();
            float S = PlantaFixa.S; var D = new Desenho(L, A, 2, new Color(0, 0, 0, 0)); System.Func<string, Color> H = h => Desenho.Hex(h);
            D.Ret(X(-Mapa.HX), Z(-Mapa.HZ), (float)Mapa.HX * 2 * S, (float)Mapa.HZ * 2 * S, H("#0d130f")); D.RetBorda(X(-Mapa.HX), Z(-Mapa.HZ), (float)Mapa.HX * 2 * S, (float)Mapa.HZ * 2 * S, 2, H("#3a4a36"));
            foreach (var c in MapaVisual.CLAREIRAS) D.Circulo(X(c.x), Z(c.y), c.z * S, H("#18211a"));
            foreach (var T in MapaVisual.TRILHAS) { var p = new List<Vector2>(); foreach (var q in T.pts) p.Add(new Vector2(X(q.x), Z(q.y))); D.Polilinha(p, T.w * S, H("#3b3326")); }
            foreach (var q in MapaVisual.ARVORES) D.Circulo(X(q.x), Z(q.z), Mathf.Max(1, (q.tipo == 0 ? 4 : q.tipo == 1 ? 2.6f : 1.2f) * S), H(q.tipo == 2 ? "#2a2620" : "#1a261b"));
            // v6: o desfiladeiro (preto), o promontório, a cruz da catedral e as travessias
            { var Df = MapaVisual.DESFILADEIRO; var P = MapaVisual.PROMONTORIO; D.Ret(X(Df.x1), Z(Df.z1), (Df.x2 - Df.x1) * S, (Df.z2 - Df.z1) * S, H("#030405")); D.Ret(X(P.x1), Z(P.z1), (P.x2 - P.x1) * S, (P.z2 - P.z1) * S, H("#1b1d20")); }
            for (int i = Mapa.ZONAS.Count - 1; i >= 0; i--) { var Zn = Mapa.ZONAS[i]; if (Zn.name == "Catedral" || Zn.name == "Claustro") D.Ret(X(Zn.x1), Z(Zn.z1), (float)(Zn.x2 - Zn.x1) * S, (float)(Zn.z2 - Zn.z1) * S, H(Zn.name == "Claustro" ? "#1d2620" : "#23252b")); }
            D.Circulo(X(0), Z(-85), 11 * S, H("#23252b"));
            foreach (var T in MapaVisual.TRAVESSIAS)
                if (T.eixo == "z") D.Ret(X(T.a), Z(T.de), (T.b - T.a) * S, (T.ate - T.de) * S, H("#4a4438"));
                else D.Ret(X(T.de), Z(T.a), (T.ate - T.de) * S, (T.b - T.a) * S, H(T.nome == "Ponte Caída" ? "#4a3a28" : "#4a4438"));
            foreach (var p in MapaVisual.PEWS) D.Ret(X(Mathf.Min(p.x, p.y)), Z(Mathf.Min(p.z, p.w)), Mathf.Abs(p.y - p.x) * S, Mathf.Abs(p.w - p.z) * S, H("#2a2119"));
            foreach (var b in MapaVisual.WALLS)
            {
                if (b.kind == "arvore_f" || b.kind == "abismo") continue;
                if (b.kind == "espinheiro") { D.Circulo(X((b.x1 + b.x2) / 2), Z((b.z1 + b.z2) / 2), Mathf.Max(1, (b.x2 - b.x1) * .6f * S), H("#15201a")); continue; }
                bool low = !b.tall; string c; if (!COR_PECA.TryGetValue(b.kind, out c)) c = low ? (b.kind == "sebe" ? "#27402a" : "#3a3630") : "#58524a";
                D.Ret(X(b.x1), Z(b.z1), Mathf.Max(low ? 1.2f : 1.5f, (b.x2 - b.x1) * S), Mathf.Max(low ? 1.2f : 1.5f, (b.z2 - b.z1) * S), H(c));
            }
            foreach (var b in Mapa.TALL)   // os pilares (estão na colisão alta, mas não entre as peças; o mapa.json arredonda as peças a 0,01)
            {
                bool tem = false; foreach (var q in MapaVisual.WALLS) if (Mathf.Abs(q.x1 - (float)b.x1) < .006f && Mathf.Abs(q.z1 - (float)b.z1) < .006f && Mathf.Abs(q.x2 - (float)b.x2) < .006f && Mathf.Abs(q.z2 - (float)b.z2) < .006f) { tem = true; break; }
                if (!tem) D.Ret(X(b.x1), Z(b.z1), Mathf.Max(1.5f, (float)(b.x2 - b.x1) * S), Mathf.Max(1.5f, (float)(b.z2 - b.z1) * S), H("#58524a"));
            }
            return tex = D.Textura();
        }
        // a planta do Asilo Blackwater (plantaAsilo do navegador): terreno, lago, salas cobertas e abertas, água, paredes,
        // móveis e os quadros de força; os nomes das salas vão por cima no IMGUI (PartidaPlanta)
        static readonly Dictionary<string, string> COR_ASILO = new Dictionary<string, string> {
            {"parede","#0e0e10"},{"muro","#2a2824"},{"lapide","#77736a"},{"canteiro","#2c3a22"},{"coluna_coreto","#8a8478"},{"caldeira","#6a3a2a"},{"carvao","#111"} };
        static Texture2D TexturaAsilo()
        {
            float S = PlantaFixa.S; var D = new Desenho(L, A, 2, new Color(0, 0, 0, 0)); System.Func<string, Color> H = h => Desenho.Hex(h);
            System.Action<float, float, float, float, string> ret = (x1, x2, z1, z2, c) => D.Ret(X(x1), Z(z1), Mathf.Max(1, (x2 - x1) * S), Mathf.Max(1, (z2 - z1) * S), H(c));
            float hx = (float)Mapa.HX, hz = (float)Mapa.HZ;
            ret(-hx, hx, -hz, hz, "#141a14"); D.RetBorda(X(-hx), Z(-hz), hx * 2 * S, hz * 2 * S, 2, H("#3a4a36"));
            ret(-hx, hx, -hz, -66, "#0b1418");   // o lago
            foreach (var E in MapaVisual.ESPACOS) ret(E.x1, E.x2, E.z1, E.z2, E.interno ? "#3b3a3c" : "#1f2a1e");
            foreach (var q in MapaVisual.AGUA) ret(q.x1, q.x2, q.z1, q.z2, "#16303a");
            foreach (var b in MapaVisual.WALLS) { if (b.kind == "lago") continue; string c; if (!COR_ASILO.TryGetValue(b.kind, out c)) c = "#57493a"; ret(b.x1, b.x2, b.z1, b.z2, c); }
            foreach (var q in MapaVisual.QUADROS) { D.Ret(X(q.x) - 4, Z(q.z) - 4, 8, 8, H("#24282c")); D.RetBorda(X(q.x) - 4, Z(q.z) - 4, 8, 8, 1.2f, H("#e8c050")); }
            return D.Textura();
        }

    }
}
