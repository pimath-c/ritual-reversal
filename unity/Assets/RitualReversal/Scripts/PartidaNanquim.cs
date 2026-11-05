// v6.3, direção Nanquim (o mesmo do index.html): as peças que o CSS faz com clip-path, -webkit-text-stroke e SVG, e o
// movimento da interface. Tudo é desenhado por cima do que já existia: a pincelada (o path do .pincel, rasterizado uma vez),
// o texto só no contorno (a segunda palavra do título), o recorte que revela da esquerda para a direita (@keyframes revelar e
// pintar), o carimbo (scale 1,14 → 1) e os relógios das animações que começam num evento (a tinta do título uma vez, o
// tranco da munição, o tremor da vida, a linha nova do feed). "Clarões e tremor: reduzidos" (rr-suave) desliga o movimento.
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        // ---------- relógios das animações ----------
        readonly Dictionary<string, float> inicioAnim = new Dictionary<string, float>();
        static bool animacaoNaFoto;   // tools/cs/TesteTelas.cs: as fotos mostram o fim das animações
        // marca o começo de uma animação (como o marcar() do navegador, que põe a classe de novo)
        void Marcar(string k) { if (!SuaveUI()) inicioAnim[k] = Time.unscaledTime; }
        // de 0 a 1 entre atraso e atraso + dur desde a marca; 1 sem marca, depois do fim ou com o movimento desligado
        float Anim(string k, float dur, float atraso = 0)
        {
            float t0; if (SuaveUI() || animacaoNaFoto || !inicioAnim.TryGetValue(k, out t0)) return 1;
            return Mathf.Clamp01((Time.unscaledTime - t0 - atraso) / dur);
        }
        bool Animando(string k, float ate) { float t0; return !SuaveUI() && inicioAnim.TryGetValue(k, out t0) && Time.unscaledTime - t0 < ate; }
        static float Sai(float t) { return 1 - (1 - t) * (1 - t) * (1 - t); }   // ease-out

        // ---------- recorte: só o retângulo r aparece (o clip-path: inset dos @keyframes revelar e digitar) ----------
        // k: a fração revelada; com k >= 1 desenha sem recorte (o caminho de sempre)
        void Recorte(Rect r, System.Action desenho, float k = 0)
        {
            if (medindo) return;   // quem chama desenha com Linha, que não olha o medindo
            if (k >= 1) { desenho(); return; }
            if (r.width <= 0) return;
            Grupo(r); var m0 = GUI.matrix; GUI.matrix = m0 * Matrix4x4.TRS(new Vector3(-r.x, -r.y, 0), Quaternion.identity, Vector3.one);
            // dentro do grupo, o pivô do texto nítido conta a origem do grupo: tira-se de volta a mesma origem
            recortes[recortes.Count - 1] -= r.position;
            try { desenho(); } finally { GUI.matrix = m0; FimGrupo(); }
        }

        // ---------- a pincelada (o path do .pincel do navegador, viewBox 700 × 40) ----------
        static Texture2D texPincel;
        static Texture2D Pincel()
        {
            if (texPincel != null) return texPincel;
            // os pontos do contorno: as curvas de Bézier cúbicas do path, 24 passos cada
            var P = new List<Vector2>(); System.Action<Vector2, Vector2, Vector2, Vector2> c = (a, b, cc, d) =>
            {
                for (int i = 1; i <= 24; i++) { float t = i / 24f, u = 1 - t; P.Add(u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * cc + t * t * t * d); }
            };
            var p0 = new Vector2(4, 22); P.Add(p0);
            c(p0, new Vector2(120, 10), new Vector2(260, 30), new Vector2(420, 16));
            c(new Vector2(420, 16), new Vector2(540, 6), new Vector2(620, 24), new Vector2(696, 14));
            P.Add(new Vector2(696, 26));
            c(new Vector2(696, 26), new Vector2(610, 38), new Vector2(520, 22), new Vector2(410, 32));
            c(new Vector2(410, 32), new Vector2(260, 44), new Vector2(120, 26), new Vector2(4, 32));
            int Wt = 700, Ht = 40, SS = 4; var px = new Color[Wt * Ht];
            for (int y = 0; y < Ht; y++)
                for (int s = 0; s < SS; s++)
                {
                    float yy = y + (s + .5f) / SS; var xs = new List<float>();
                    for (int i = 0; i < P.Count; i++) { var a = P[i]; var b = P[(i + 1) % P.Count]; if ((a.y <= yy) != (b.y <= yy)) xs.Add(a.x + (yy - a.y) / (b.y - a.y) * (b.x - a.x)); }
                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                        for (int x = Mathf.Max(0, Mathf.FloorToInt(xs[k])); x < Mathf.Min(Wt, Mathf.CeilToInt(xs[k + 1])); x++)
                        {
                            float cob = Mathf.Clamp01(Mathf.Min(x + 1, xs[k + 1]) - Mathf.Max(x, xs[k])) / SS;
                            int idx = (Ht - 1 - y) * Wt + x; px[idx] = new Color(1, 1, 1, Mathf.Clamp01(px[idx].a + cob));
                        }
                }
            texPincel = new Texture2D(Wt, Ht, TextureFormat.RGBA32, false); texPincel.SetPixels(px); texPincel.Apply(false, true);
            texPincel.wrapMode = TextureWrapMode.Clamp; texPincel.filterMode = FilterMode.Bilinear; return texPincel;
        }
        // a pincelada esticada em r (preserveAspectRatio none), pintada da esquerda até a fração k
        void DesenharPincel(Rect r, Color cor, float k = 1)
        {
            if (medindo || k <= 0) return; var c0 = GUI.color; GUI.color = cor;
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, r.width * k, r.height), Pincel(), new Rect(0, 0, k, 1)); GUI.color = c0;
        }

        // ---------- texto só no contorno (-webkit-text-stroke: 2px; color: transparent) ----------
        // o IMGUI não tem contorno: o texto na cor do traço deslocado em volta (raio e) e, por cima, na cor do fundo
        void TextoContorno(float x, float y, float w, float lh, string txt, GUIStyle st, Color traco, Color fundo, float e = 1)
        {
            if (medindo) return; var sT = new GUIStyle(st); sT.normal.textColor = traco; var sF = new GUIStyle(st); sF.normal.textColor = fundo;
            for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6; Linha(x + Mathf.Cos(a) * e, y + Mathf.Sin(a) * e, w, lh, txt, sT); }
            Linha(x, y, w, lh, txt, sF);
        }

        // ---------- carimbo: scale 1,14 → ,99 → 1 com um leve deslocamento (@keyframes carimbo) ----------
        // devolve a escala e o alfa do instante t (0 a 1)
        static void Carimbo(float t, out float escala, out float alfa, out Vector2 desloc)
        {
            desloc = Vector2.zero;
            if (t < .6f) { float u = t / .6f; escala = Mathf.Lerp(1.14f, .99f, u); alfa = u; }
            else if (t < .8f) { float u = (t - .6f) / .2f; escala = Mathf.Lerp(.99f, 1, u); alfa = 1; desloc = new Vector2(2 * u, -u); }
            else { float u = (t - .8f) / .2f; escala = 1; alfa = 1; desloc = new Vector2(2 * (1 - u), -(1 - u)); }
        }
        void ComCarimbo(string k, float dur, float atraso, float px, float py, System.Action desenho)
        {
            if (medindo) return; float t = Anim(k, dur, atraso); if (t >= 1) { desenho(); return; }
            float s, a; Vector2 d; Carimbo(t, out s, out a, out d); var c0 = GUI.color; GUI.color = new Color(c0.r, c0.g, c0.b, c0.a * a);
            try { ComTransformacao(px, py, px + d.x, py + d.y, s, desenho); } finally { GUI.color = c0; }
        }
        // entrar: opacidade 0 → 1 e 8 px de baixo para cima
        void ComEntrada(string k, float dur, float atraso, System.Action<float> desenho)
        {
            float t = Sai(Anim(k, dur, atraso)); if (t >= 1 || medindo) { desenho(0); return; }
            var c0 = GUI.color; GUI.color = new Color(c0.r, c0.g, c0.b, c0.a * t); try { desenho(8 * (1 - t)); } finally { GUI.color = c0; }
        }
    }
}
