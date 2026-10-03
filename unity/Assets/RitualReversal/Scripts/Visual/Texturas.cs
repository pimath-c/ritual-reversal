// Texturas do protótipo, geradas por código como lá: ruído com a mesma semente (makeNoise), as mesmas receitas de
// piso, parede, madeira, mármore, metal, pano, couro e chão da mata (pbr: cor + relevo), e uma pequena "tela" que
// imita o canvas 2D do navegador para as texturas desenhadas (runas, vitrais, estandartes, tapete, hera, musgo...).
// A parte de cálculo (Ruido, Tela, Pbr) é C# puro; só Tex2D() cria a textura do Unity.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal.Visual
{
    // makeNoise(seed) do protótipo: Park-Miller, grade de 256 × 256 valores e ruído de valor periódico
    public class Ruido
    {
        double s; readonly float[] g = new float[65536];
        public Ruido(int semente) { s = semente; for (int i = 0; i < 65536; i++) g[i] = (float)R(); }
        public double R() { s = (s * 16807) % 2147483647; return s / 2147483647; }
        float H(int a, int b, int P) { return g[(((a % P) + P) % P) * 256 + (((b % P) + P) % P)]; }
        public float Vn(float x, float y, int P)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y); float xf = x - xi, yf = y - yi, u = xf * xf * (3 - 2 * xf), w = yf * yf * (3 - 2 * yf);
            return Lerp(Lerp(H(xi, yi, P), H(xi + 1, yi, P), u), Lerp(H(xi, yi + 1, P), H(xi + 1, yi + 1, P), u), w);
        }
        public float Fbm(float x, float y, int P, int oct)
        {
            float a = 0, amp = .5f, f = 1, n = 0;
            for (int o = 0; o < oct; o++) { a += amp * Vn(x * f, y * f, Math.Min(256, (int)(P * f))); n += amp; amp *= .5f; f *= 2; }
            return a / n;
        }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
    }

    // resultado de pbr(): cor (sRGB) e mapa de normais, em linhas de cima para baixo como o canvas
    public class Pbr
    {
        public int tam; public Color32[] cor, normal; public float rugosidade;
        public Texture2D mapa, relevo;
        public struct Saida { public float h, r, g, b, ro; }
        public static Pbr Criar(int tam, Func<float, float, Saida> fn, float normalK)
        {
            var P = new Pbr { tam = tam, cor = new Color32[tam * tam], normal = new Color32[tam * tam] }; var H = new float[tam * tam]; double ro = 0;
            for (int y = 0; y < tam; y++) for (int x = 0; x < tam; x++)
                {
                    var o = fn((float)x / tam, (float)y / tam); int i = y * tam + x; H[i] = o.h;
                    P.cor[i] = new Color32(B(o.r), B(o.g), B(o.b), 255); ro += o.ro;
                }
            for (int y = 0; y < tam; y++) for (int x = 0; x < tam; x++)
                {
                    int xl = (x - 1 + tam) % tam, xr = (x + 1) % tam, yu = (y - 1 + tam) % tam, yd = (y + 1) % tam;
                    float dx = (H[y * tam + xr] - H[y * tam + xl]) * normalK, dy = (H[yd * tam + x] - H[yu * tam + x]) * normalK, l = (float)Math.Sqrt(dx * dx + dy * dy + 1);
                    P.normal[y * tam + x] = new Color32(B((-dx / l * .5f + .5f) * 255), B((dy / l * .5f + .5f) * 255), B((1 / l * .5f + .5f) * 255), 255);
                }
            P.rugosidade = (float)(ro / (tam * tam)); return P;
        }
        static byte B(float v) { return (byte)Math.Max(0, Math.Min(255, (int)v)); }
        public Pbr Texturas()
        {
            if (mapa == null) { mapa = Tela.Tex2D(tam, tam, cor, true, "pbr"); relevo = Tela.Tex2D(tam, tam, normal, false, "pbr_normal"); }
            return this;
        }
    }

    // Canvas 2D mínimo: cores em 0..1, origem no canto de cima, caminhos com retas, curvas, arcos e elipses,
    // preencher, contornar, recorte (clip) e gradientes. Suficiente para as texturas desenhadas do protótipo.
    public class Tela
    {
        public readonly int W, H; public readonly Color[] px; float[] mascara;
        public Color preencher = Color.white, contorno = Color.white; public float largura = 1, alfaGlobal = 1;
        public Func<float, float, Color> gradiente; // se definido, substitui 'preencher' ao preencher
        readonly List<List<Vector2>> caminho = new List<List<Vector2>>();
        readonly Stack<float[]> pilha = new Stack<float[]>();
        public Tela(int w, int h) { W = w; H = h; px = new Color[w * h]; }

        public static Color Cor(string css)
        {
            css = css.Trim();
            if (css.StartsWith("#")) { int v = Convert.ToInt32(css.Substring(1), 16); return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1); }
            int i0 = css.IndexOf('('), i1 = css.LastIndexOf(')'); var p = css.Substring(i0 + 1, i1 - i0 - 1).Split(',');
            float r = float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture) / 255f, g = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture) / 255f, b = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture) / 255f;
            float a = p.Length > 3 ? float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture) : 1; return new Color(r, g, b, a);
        }
        public static Color Hex(int v, float a = 1) { return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, a); }
        public static Func<float, float, Color> Linear(float x0, float y0, float x1, float y1, params object[] paradas)
        {
            var st = Paradas(paradas); float dx = x1 - x0, dy = y1 - y0, L2 = dx * dx + dy * dy;
            return (x, y) => Amostra(st, L2 > 0 ? ((x - x0) * dx + (y - y0) * dy) / L2 : 0);
        }
        public static Func<float, float, Color> Radial(float cx, float cy, float r0, float r1, params object[] paradas)
        {
            var st = Paradas(paradas); return (x, y) => Amostra(st, ((float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r0) / Math.Max(1e-6f, r1 - r0));
        }
        static List<KeyValuePair<float, Color>> Paradas(object[] p) { var l = new List<KeyValuePair<float, Color>>(); for (int i = 0; i + 1 < p.Length; i += 2) l.Add(new KeyValuePair<float, Color>(Convert.ToSingle(p[i]), p[i + 1] is Color ? (Color)p[i + 1] : Cor((string)p[i + 1]))); return l; }
        static Color Amostra(List<KeyValuePair<float, Color>> st, float t)
        {
            t = Mathf.Clamp01(t); if (t <= st[0].Key) return st[0].Value; if (t >= st[st.Count - 1].Key) return st[st.Count - 1].Value;
            for (int i = 1; i < st.Count; i++) if (t <= st[i].Key) { float k = (t - st[i - 1].Key) / Math.Max(1e-6f, st[i].Key - st[i - 1].Key); return Color.Lerp(st[i - 1].Value, st[i].Value, k); }
            return st[st.Count - 1].Value;
        }

        void Misturar(int x, int y, Color c, float cobertura)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return; int i = y * W + x; float a = c.a * cobertura * alfaGlobal; if (mascara != null) a *= mascara[i]; if (a <= 0) return;
            var d = px[i]; float oa = a + d.a * (1 - a); if (oa <= 0) { px[i] = new Color(0, 0, 0, 0); return; }
            px[i] = new Color((c.r * a + d.r * d.a * (1 - a)) / oa, (c.g * a + d.g * d.a * (1 - a)) / oa, (c.b * a + d.b * d.a * (1 - a)) / oa, oa);
        }
        public void Limpar() { for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0); }
        public void Retangulo(float x, float y, float w, float h)
        {
            int x0 = (int)Math.Floor(x), x1 = (int)Math.Ceiling(x + w), y0 = (int)Math.Floor(y), y1 = (int)Math.Ceiling(y + h);
            for (int yy = Math.Max(0, y0); yy < Math.Min(H, y1); yy++) for (int xx = Math.Max(0, x0); xx < Math.Min(W, x1); xx++)
                {
                    float cx = Math.Min(xx + 1, x + w) - Math.Max(xx, x), cy = Math.Min(yy + 1, y + h) - Math.Max(yy, y); if (cx <= 0 || cy <= 0) continue;
                    Misturar(xx, yy, gradiente != null ? gradiente(xx + .5f, yy + .5f) : preencher, Math.Min(1, cx) * Math.Min(1, cy));
                }
        }
        public void ContornoRetangulo(float x, float y, float w, float h) { Iniciar(); Mover(x, y); Linha(x + w, y); Linha(x + w, y + h); Linha(x, y + h); Fechar(); Contornar(); }

        // ---------- caminhos ----------
        public void Iniciar() { caminho.Clear(); }
        List<Vector2> Atual { get { if (caminho.Count == 0) caminho.Add(new List<Vector2>()); return caminho[caminho.Count - 1]; } }
        public void Mover(float x, float y) { caminho.Add(new List<Vector2> { new Vector2(x, y) }); }
        public void Linha(float x, float y) { Atual.Add(new Vector2(x, y)); }
        public void Curva(float cx, float cy, float x, float y) { var l = Atual; var a = l.Count > 0 ? l[l.Count - 1] : new Vector2(cx, cy); for (int s = 1; s <= 14; s++) { float t = s / 14f; l.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * new Vector2(cx, cy) + t * t * new Vector2(x, y)); } }
        public void Arco(float cx, float cy, float r, float a0, float a1) { Elipse(cx, cy, r, r, 0, a0, a1); }
        public void Elipse(float cx, float cy, float rx, float ry, float rot, float a0, float a1)
        {
            var l = Atual; float cr = (float)Math.Cos(rot), sr = (float)Math.Sin(rot); if (a1 - a0 > Math.PI * 2) a1 = a0 + (float)Math.PI * 2;
            int n = Math.Max(8, (int)(Math.Max(rx, ry) * Math.Abs(a1 - a0) / 2)); n = Math.Min(n, 256);
            for (int s = 0; s <= n; s++) { float a = a0 + (a1 - a0) * s / n; float ex = rx * (float)Math.Cos(a), ey = ry * (float)Math.Sin(a); l.Add(new Vector2(cx + ex * cr - ey * sr, cy + ex * sr + ey * cr)); }
        }
        public void Fechar() { var l = Atual; if (l.Count > 0) l.Add(l[0]); }

        // preenchimento com 4 × 4 amostras por pixel (regra par-ímpar)
        public void Preencher() { var c = CoberturaCaminho(); for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { float k = c[y * W + x]; if (k > 0) Misturar(x, y, gradiente != null ? gradiente(x + .5f, y + .5f) : preencher, k); } }
        float[] CoberturaCaminho()
        {
            var cob = new float[W * H]; var arestas = new List<Vector4>();
            foreach (var l in caminho) for (int i = 0; i < l.Count; i++) { var a = l[i]; var b = l[(i + 1) % l.Count]; if (a.y != b.y) arestas.Add(new Vector4(a.x, a.y, b.x, b.y)); }
            if (arestas.Count == 0) return cob;
            float ymin = float.MaxValue, ymax = float.MinValue; foreach (var e in arestas) { ymin = Math.Min(ymin, Math.Min(e.y, e.w)); ymax = Math.Max(ymax, Math.Max(e.y, e.w)); }
            var xs = new List<float>();
            for (int y = Math.Max(0, (int)Math.Floor(ymin)); y < Math.Min(H, (int)Math.Ceiling(ymax)); y++)
                for (int sy = 0; sy < 4; sy++)
                {
                    float yy = y + (sy + .5f) / 4; xs.Clear();
                    foreach (var e in arestas) { if ((yy >= e.y && yy < e.w) || (yy >= e.w && yy < e.y)) xs.Add(e.x + (yy - e.y) / (e.w - e.y) * (e.z - e.x)); }
                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                    {
                        float x0 = xs[k], x1 = xs[k + 1];
                        for (int x = Math.Max(0, (int)Math.Floor(x0)); x < Math.Min(W, (int)Math.Ceiling(x1)); x++) { float c = Math.Min(x + 1, x1) - Math.Max(x, x0); if (c > 0) cob[y * W + x] += c / 4; }
                    }
                }
            for (int i = 0; i < cob.Length; i++) cob[i] = Math.Min(1, cob[i]);
            return cob;
        }
        // contorno com pontas e juntas redondas
        public void Contornar()
        {
            float r = largura / 2; var cob = new float[W * H];
            foreach (var l in caminho)
                for (int i = 0; i + 1 < l.Count; i++)
                {
                    var a = l[i]; var b = l[i + 1];
                    int x0 = Math.Max(0, (int)Math.Floor(Math.Min(a.x, b.x) - r - 1)), x1 = Math.Min(W - 1, (int)Math.Ceiling(Math.Max(a.x, b.x) + r + 1));
                    int y0 = Math.Max(0, (int)Math.Floor(Math.Min(a.y, b.y) - r - 1)), y1 = Math.Min(H - 1, (int)Math.Ceiling(Math.Max(a.y, b.y) + r + 1));
                    var ab = b - a; float L2 = ab.sqrMagnitude;
                    for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                        {
                            var p = new Vector2(x + .5f, y + .5f); float t = L2 > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / L2) : 0; float d = (p - (a + ab * t)).magnitude;
                            float c = Mathf.Clamp01(r + .5f - d); if (c > cob[y * W + x]) cob[y * W + x] = c;
                        }
                }
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { float k = cob[y * W + x]; if (k > 0) Misturar(x, y, contorno, k); }
        }
        // recorte: tudo que vier depois só pinta dentro do caminho atual (até Restaurar)
        public void Recortar() { pilha.Push(mascara); var c = CoberturaCaminho(); if (mascara != null) for (int i = 0; i < c.Length; i++) c[i] *= mascara[i]; mascara = c; }
        public void Restaurar() { mascara = pilha.Count > 0 ? pilha.Pop() : null; }
        // globalCompositeOperation='destination-out' com um gradiente horizontal (usado no raio de luz)
        public void ApagarPorAlfa(Func<float, float, float> alfa) { for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { int i = y * W + x; var c = px[i]; c.a *= 1 - Mathf.Clamp01(alfa(x + .5f, y + .5f)); px[i] = c; } }

        public Texture2D Textura(bool repetir = false, string nome = "tela")
        {
            var cs = new Color32[W * H]; for (int i = 0; i < cs.Length; i++) cs[i] = px[i];
            var t = Tex2D(W, H, cs, true, nome); t.wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; return t;
        }
        // linhas de cima para baixo (canvas) viram de baixo para cima (Unity), como o flipY das texturas do three
        public static Texture2D Tex2D(int w, int h, Color32[] linhas, bool srgb, string nome)
        {
            var inv = new Color32[w * h]; for (int y = 0; y < h; y++) Array.Copy(linhas, y * w, inv, (h - 1 - y) * w, w);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true, !srgb) { name = nome, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            t.SetPixels32(inv); t.Apply(true, false); return t;
        }
    }

    // As texturas do protótipo (buildTextures, buildTexturasExtras, buildFloresta e as desenhadas no canvas)
    public static class Tex
    {
        static float Sm(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }
        static float L(float a, float b, float t) { return a + (b - a) * t; }
        static Pbr.Saida S(float h, float r, float g, float b, float ro) { return new Pbr.Saida { h = h, r = r, g = g, b = b, ro = ro }; }
        public static Pbr floor, wall, wood, marble, metal, cloth, leather, chao;
        public static int Tam(int t) { return Qualidade.nivel <= 1 ? Math.Max(128, t >> 1) : t; }

        public static void Construir()
        {
            if (floor != null) return;
            var n = new Ruido(1337); var tone = new float[64]; for (int i = 0; i < 64; i++) tone[i] = .78f + (float)n.R() * .3f;
            floor = Pbr.Criar(Tam(512), (u, v) =>
            {
                int T = 4, tx = (int)Math.Floor(u * T), ty = (int)Math.Floor(v * T); float lu = u * T - tx, lv = v * T - ty, e = Math.Min(Math.Min(lu, 1 - lu), Math.Min(lv, 1 - lv));
                float f = n.Fbm(u * 8, v * 8, 8, 5), f2 = n.Fbm(u * 3 + 7, v * 3 + 3, 3, 4), crack = Math.Abs(n.Fbm(u * 16 + 2, v * 16, 16, 3) - .5f) < .006f ? .6f : 0, gr = Sm(.015f, .05f, e), k = tone[(tx * 7 + ty * 3) % 64] * (.72f + .5f * f) * (.75f + .45f * f2);
                return S(gr * (.55f + .35f * f) - crack * .25f, 78 * k * (.35f + .65f * gr) * (1 - crack * .5f), 72 * k * (.35f + .65f * gr) * (1 - crack * .5f), 66 * k * (.38f + .62f * gr) * (1 - crack * .5f), L(.95f, .55f + .3f * f, gr));
            }, 2.2f);
            wall = Pbr.Criar(Tam(512), (u, v) =>
            {
                int rows = 6, ry = (int)Math.Floor(v * rows); float lv = v * rows - ry, off = (ry % 2) * .5f; int cols = 3, cx = (int)Math.Floor(u * cols + off); float lu = u * cols + off - cx, e = Math.Min(Math.Min(lu, 1 - lu), Math.Min(lv * .5f, (1 - lv) * .5f));
                float f = n.Fbm(u * 10, v * 10, 10, 5), moss = Sm(.55f, .75f, n.Fbm(u * 4 + 9, v * 4, 4, 4)) * Sm(.3f, 1, v), bev = Sm(0, .05f, e), t = .8f + .35f * tone[(((cx * 5 + ry * 11) % 64) + 64) % 64] - .2f, k = (.45f + .55f * bev) * t * (.75f + .45f * f);
                return S(bev * .7f + f * .3f, L(86, 48, moss * .6f) * k, L(80, 56, moss * .6f) * k, L(74, 44, moss * .6f) * k, .85f + .1f * f);
            }, 2.6f);
            wood = Pbr.Criar(Tam(256), (u, v) => { float w = n.Fbm(u * 2, v * 6, 2, 4), g = .5f + .5f * (float)Math.Sin((u * 28 + w * 7) * Math.PI), f = n.Fbm(u * 20, v * 4, 16, 3), k = .6f + .4f * g; return S(g * .4f + f * .3f, 70 * k + 20 * f, 42 * k + 10 * f, 26 * k + 6 * f, .6f + .25f * f); }, 1.5f);
            marble = Pbr.Criar(Tam(256), (u, v) => { float f = n.Fbm(u * 4, v * 4, 4, 5), vein = (float)Math.Pow(1 - Math.Abs(Math.Sin((u * 3 + v * 2 + f * 4) * Math.PI)), 10), k = .78f + .2f * f - vein * .55f; return S(f * .2f - vein * .2f, 200 * k, 194 * k, 186 * k, .28f + .2f * f + vein * .3f); }, 1.2f);
            metal = Pbr.Criar(Tam(256), (u, v) =>
            {
                float f = n.Fbm(u * 8, v * 8, 8, 5), scr = Math.Abs(Math.Sin(u * 60 + n.Fbm(u * 3, v * 3, 3, 2) * 20)) > .985 ? 1 : 0, pat = Sm(.55f, .7f, n.Fbm(u * 5 + 3, v * 5, 5, 4)), k = .8f + .3f * f + scr * .3f;
                return S(f * .3f + scr * .15f, L(120, 58, pat) * k, L(84, 78, pat) * k, L(48, 60, pat) * k, .35f + .3f * f + pat * .3f);
            }, 1.2f);
            var n2 = new Ruido(4242);
            cloth = Pbr.Criar(Tam(256), (u, v) => { float w = (float)(Math.Sin(u * 180 * Math.PI) * Math.Sin(v * 180 * Math.PI)), f = n2.Fbm(u * 6, v * 6, 6, 4), gasto = Sm(.55f, .85f, n2.Fbm(u * 3 + 5, v * 3, 3, 4)), k = .8f + .1f * w + .3f * (f - .5f) - gasto * .2f; float c = Mathf.Clamp(240 * k, 0, 255); return S(.5f + .3f * w + f * .25f, c, c, c, .95f); }, 1.6f);
            leather = Pbr.Criar(Tam(256), (u, v) => { float f = n2.Fbm(u * 14, v * 14, 14, 4), vinco = (float)Math.Pow(Math.Abs(n2.Fbm(u * 4 + 2, v * 4, 4, 3) - .5f) * 2, 6), k = .82f + .25f * (f - .5f) - vinco * .3f; float c = Mathf.Clamp(235 * k, 0, 255); return S(f * .4f - vinco * .3f, c, c, c, .55f + .3f * f); }, 1.8f);
            var n3 = new Ruido(9091);
            chao = Pbr.Criar(Tam(512), (u, v) =>
            {
                float f = n3.Fbm(u * 8, v * 8, 8, 5), fol = Sm(.52f, .66f, n3.Fbm(u * 16 + 9, v * 16 + 2, 16, 3)), musgo = Sm(.5f, .72f, n3.Fbm(u * 4 + 5, v * 4 + 1, 4, 4)), gr = n3.Fbm(u * 32, v * 32, 32, 2);
                float k = .62f + .5f * f; return S(f * .45f + fol * .35f + gr * .2f, L(L(58, 40, musgo), 84, fol * .7f) * k, L(L(46, 54, musgo), 56, fol * .7f) * k, L(L(36, 32, musgo), 30, fol * .7f) * k, .96f);
            }, 2.4f);
            foreach (var p in new[] { floor, wall, wood, marble, metal, cloth, leather, chao }) p.Texturas();
        }

        // ---------- desenhadas ----------
        static double sd; static float R() { sd = (sd * 16807) % 2147483647; return (float)(sd / 2147483647); }
        const float PI = (float)Math.PI;
        static Texture2D _rune, _flame, _glow, _beam, _shaft, _win, _rose, _estandarte, _tapete, _hera, _musgo, _samambaia, _pegada;

        public static Texture2D Runa()
        {
            if (_rune != null) return _rune; var g = new Tela(512, 512); float c = 256; g.contorno = Color.white; g.preencher = Color.white;
            Action<float, float> anel = (r, w) => { g.largura = w; g.Iniciar(); g.Arco(c, c, r, 0, PI * 2); g.Contornar(); };
            anel(246, 7); anel(226, 2.5f); anel(170, 4); anel(152, 1.5f); anel(60, 3);
            sd = 7;
            for (int i = 0; i < 28; i++)
            {
                float a = i / 28f * PI * 2, ox = c + (float)Math.Cos(a) * 198, oy = c + (float)Math.Sin(a) * 198, rot = a + PI / 2, cr = (float)Math.Cos(rot), sr = (float)Math.Sin(rot);
                Func<float, float, Vector2> T = (x, y) => new Vector2(ox + x * cr - y * sr, oy + x * sr + y * cr);
                g.largura = 3.6f; g.Iniciar(); int k = (int)Math.Floor(R() * 4); var p = T(-8, -12); g.Mover(p.x, p.y);
                Action<float, float> ln = (x, y) => { var q = T(x, y); g.Linha(q.x, q.y); }; Action<float, float> mv = (x, y) => { var q = T(x, y); g.Mover(q.x, q.y); };
                if (k == 0) { ln(8, 12); mv(8, -12); ln(-8, 12); }
                else if (k == 1) { ln(0, 12); ln(8, -12); mv(-6, 0); ln(6, 0); }
                else if (k == 2) { mv(0, -13); ln(0, 13); mv(-8, -6); ln(8, 6); }
                else { for (int s = 0; s <= 12; s++) { float an = PI * 1.5f * s / 12; ln(9 * (float)Math.Cos(an), 9 * (float)Math.Sin(an)); } mv(0, -13); ln(0, 4); }
                g.Contornar();
            }
            g.largura = 3; g.Iniciar(); for (int i = 0; i <= 7; i++) { float a = i * (PI * 2 * 3 / 7) - PI / 2, x = c + (float)Math.Cos(a) * 150, y = c + (float)Math.Sin(a) * 150; if (i > 0) g.Linha(x, y); else g.Mover(x, y); } g.Contornar();
            g.largura = 5; g.Iniciar(); g.Mover(c - 52, c); g.Curva(c, c - 44, c + 52, c); g.Curva(c, c + 44, c - 52, c); g.Contornar();
            g.Iniciar(); g.Arco(c, c - 8, 14, 0, PI * 2); g.Preencher();
            return _rune = g.Textura(false, "runa");
        }
        public static Texture2D RadialTex(Color dentro, Color fora, string nome) { var g = new Tela(128, 128); g.gradiente = Tela.Radial(64, 64, 0, 64, 0f, dentro, 1f, fora); g.Retangulo(0, 0, 128, 128); return g.Textura(false, nome); }
        public static Texture2D Chama() { return _flame ?? (_flame = RadialTex(new Color(1, 240 / 255f, 200 / 255f, 1), new Color(1, 120 / 255f, 20 / 255f, 0), "chama")); }
        public static Texture2D Brilho() { return _glow ?? (_glow = RadialTex(new Color(1, 1, 1, 1), new Color(1, 1, 1, 0), "brilho")); }
        public static Texture2D Feixe()
        {
            if (_beam != null) return _beam; var g = new Tela(8, 256); g.gradiente = Tela.Linear(0, 256, 0, 0, 0f, "rgba(255,255,255,.9)", .35f, "rgba(255,255,255,.35)", 1f, "rgba(255,255,255,0)"); g.Retangulo(0, 0, 8, 256);
            return _beam = g.Textura(false, "feixe");
        }
        public static Texture2D RaioLuz()
        {
            if (_shaft != null) return _shaft; var g = new Tela(64, 256); g.gradiente = Tela.Linear(0, 0, 0, 256, 0f, "rgba(255,255,255,.95)", 1f, "rgba(255,255,255,0)"); g.Retangulo(0, 0, 64, 256);
            var h = Tela.Linear(0, 0, 64, 0, 0f, "rgba(0,0,0,1)", .2f, "rgba(0,0,0,0)", .8f, "rgba(0,0,0,0)", 1f, "rgba(0,0,0,1)"); g.ApagarPorAlfa((x, y) => h(x, y).a);
            return _shaft = g.Textura(false, "raio");
        }
        public static Texture2D Vitral(bool rosa)
        {
            if (rosa && _rose != null) return _rose; if (!rosa && _win != null) return _win;
            int W = 256, H = rosa ? 256 : 512; var g = new Tela(W, H); var pal = new[] { "#1d3f8f", "#5a2a91", "#8f1d3a", "#1f6b7a", "#c99a2e", "#2a2f8f", "#3d1768" };
            g.Iniciar(); if (rosa) g.Arco(W / 2f, H / 2f, W / 2f - 4, 0, PI * 2); else { g.Mover(8, H); g.Linha(8, H * .32f); g.Curva(10, 40, W / 2f, 4); g.Curva(W - 10, 40, W - 8, H * .32f); g.Linha(W - 8, H); g.Fechar(); }
            g.Recortar(); sd = 11; int cel = rosa ? 20 : 28;
            for (int y = 0; y < H; y += cel) for (int x = 0; x < W; x += cel) { g.preencher = Tela.Cor(pal[(int)Math.Floor(R() * pal.Length)]); g.alfaGlobal = .75f + R() * .25f; g.Retangulo(x, y, cel, cel); }
            g.alfaGlobal = 1; g.contorno = Tela.Cor("#07070a"); g.largura = 3;
            for (int y = 0; y <= H; y += cel) { g.Iniciar(); g.Mover(0, y + (R() - .5f) * 4); g.Linha(W, y + (R() - .5f) * 4); g.Contornar(); }
            for (int x = 0; x <= W; x += cel) { g.Iniciar(); g.Mover(x, 0); g.Linha(x + (R() - .5f) * 6, H); g.Contornar(); }
            if (rosa) { g.largura = 5; for (int i = 0; i < 12; i++) { float a = i / 12f * PI * 2; g.Iniciar(); g.Mover(W / 2f, H / 2f); g.Linha(W / 2f + (float)Math.Cos(a) * W, H / 2f + (float)Math.Sin(a) * W); g.Contornar(); } foreach (float rr in new float[] { 30, 70, 110 }) { g.Iniciar(); g.Arco(W / 2f, H / 2f, rr, 0, PI * 2); g.Contornar(); } }
            else { g.largura = 7; g.Iniciar(); g.Mover(W / 2f, H * .18f); g.Linha(W / 2f, H); g.Contornar(); g.Iniciar(); g.Arco(W / 2f, H * .3f, 34, 0, PI * 2); g.Contornar(); }
            g.Restaurar(); var t = g.Textura(false, rosa ? "rosacea" : "vitral"); if (rosa) _rose = t; else _win = t; return t;
        }
        public static Texture2D Estandarte()
        {
            if (_estandarte != null) return _estandarte; int W = 256, H = 256; var c = new Tela(W, H); sd = 21;
            Action<float, string, string, Action<float, float>> faixa = (x0, fundo, ouro, desenho) =>
            {
                c.Iniciar(); c.Mover(x0 + 6, 0); c.Linha(x0 + 122, 0); c.Linha(x0 + 122, H - 44);
                for (int k = 0; k <= 8; k++) c.Linha(x0 + 122 - k * 14.5f, H - 44 + (k % 2 == 1 ? 36 : 12) + R() * 8); c.Linha(x0 + 6, H - 44); c.Fechar(); c.Recortar();
                c.gradiente = null; c.preencher = Tela.Cor(fundo); c.Retangulo(x0, 0, 128, H);
                for (int y = 0; y < H; y += 3) { c.preencher = new Color(0, 0, 0, .05f + .05f * (float)Math.Sin(y * .7)); c.Retangulo(x0, y, 128, 1); }
                c.contorno = Tela.Cor(ouro); c.largura = 5; c.ContornoRetangulo(x0 + 13, 8, 96, H - 60); c.largura = 2; c.ContornoRetangulo(x0 + 19, 14, 84, H - 72);
                c.preencher = Tela.Cor(ouro); desenho(x0 + 64, 96);
                for (int k = 0; k < 34; k++) { c.preencher = new Color(12 / 255f, 6 / 255f, 6 / 255f, R() * .3f); c.Iniciar(); c.Arco(x0 + R() * 128, R() * H, 2 + R() * 9, 0, 7); c.Preencher(); }
                c.Restaurar();
            };
            faixa(0, "#781226", "#d9b262", (x, y) =>
            {
                c.largura = 5; c.Iniciar(); c.Mover(x - 34, y); c.Curva(x, y - 30, x + 34, y); c.Curva(x, y + 30, x - 34, y); c.Contornar(); c.Iniciar(); c.Arco(x, y, 11, 0, 7); c.Preencher();
                c.largura = 3; for (int k = 0; k < 8; k++) { float a = k / 8f * PI * 2; c.Iniciar(); c.Mover(x + (float)Math.Cos(a) * 44, y + (float)Math.Sin(a) * 44); c.Linha(x + (float)Math.Cos(a) * 56, y + (float)Math.Sin(a) * 56); c.Contornar(); }
            });
            faixa(128, "#1b2a5e", "#d9b262", (x, y) =>
            {
                c.largura = 4; c.Iniciar(); c.Arco(x, y - 12, 26, 0, 7); c.Contornar(); c.Retangulo(x - 5, y - 56, 10, 112); c.Retangulo(x - 30, y - 20, 60, 10);
                for (int k = 0; k < 12; k++) { float a = k / 12f * PI * 2; c.Iniciar(); c.Mover(x + (float)Math.Cos(a) * 32, y - 12 + (float)Math.Sin(a) * 32); c.Linha(x + (float)Math.Cos(a) * 40, y - 12 + (float)Math.Sin(a) * 40); c.Contornar(); }
            });
            return _estandarte = c.Textura(false, "estandarte");
        }
        public static Texture2D Tapete()
        {
            if (_tapete != null) return _tapete; int W = 64, H = 256; var c = new Tela(W, H); var rnd = new System.Random(3);
            c.preencher = Tela.Cor("#5c1018"); c.Retangulo(0, 0, W, H);
            c.preencher = Tela.Cor("#b8893c"); c.Retangulo(3, 0, 4, H); c.Retangulo(W - 7, 0, 4, H); c.preencher = Tela.Cor("#3a0a10"); c.Retangulo(9, 0, 2, H); c.Retangulo(W - 11, 0, 2, H);
            for (int y = 0; y < H; y += 32) { c.preencher = Tela.Cor("#8a6a2c"); c.Iniciar(); c.Mover(W / 2f, y + 4); c.Linha(W / 2f + 12, y + 16); c.Linha(W / 2f, y + 28); c.Linha(W / 2f - 12, y + 16); c.Fechar(); c.Preencher(); c.preencher = Tela.Cor("#5c1018"); c.Retangulo(W / 2f - 3, y + 13, 6, 6); }
            for (int k = 0; k < 60; k++) { c.preencher = new Color(20 / 255f, 10 / 255f, 8 / 255f, (float)rnd.NextDouble() * .35f); c.Retangulo((float)rnd.NextDouble() * W, (float)rnd.NextDouble() * H, 2 + (float)rnd.NextDouble() * 8, 2 + (float)rnd.NextDouble() * 14); }
            return _tapete = c.Textura(true, "tapete");
        }
        public static Texture2D Hera()
        {
            if (_hera != null) return _hera; int W = 128, H = 256; var c = new Tela(W, H); sd = 77;
            for (int v = 0; v < 5; v++)
            {
                float x = 20 + R() * 88, y = H; c.contorno = new Color(60 / 255f, 70 / 255f, 40 / 255f); c.largura = 2; c.Iniciar(); c.Mover(x, y);
                var pts = new List<Vector2>(); float lim = H * (.1f + R() * .4f); while (y > lim) { x += (R() - .5f) * 16; y -= 10; c.Linha(x, y); pts.Add(new Vector2(x, y)); } c.Contornar();
                foreach (var p in pts) for (int k = 0; k < 2; k++) { c.preencher = new Color((50 + (int)(R() * 30)) / 255f, (80 + (int)(R() * 40)) / 255f, (36 + (int)(R() * 20)) / 255f); c.Iniciar(); c.Elipse(p.x + (R() - .5f) * 16, p.y + (R() - .5f) * 8, 5 + R() * 4, 4 + R() * 2, R() * 3, 0, 7); c.Preencher(); }
            }
            return _hera = c.Textura(false, "hera");
        }
        public static Texture2D Musgo()
        {
            if (_musgo != null) return _musgo; int W = 64, H = 256; var c = new Tela(W, H); sd = 5;
            for (int k = 0; k < 26; k++)
            {
                float x = R() * W, len = H * (.3f + R() * .7f); var grad = Tela.Linear(0, 0, 0, len, 0f, "rgba(150,160,125,1)", .7f, "rgba(120,135,100,.8)", 1f, "rgba(100,115,85,0)");
                c.largura = 1.2f + R() * 2.6f; c.Iniciar(); c.Mover(x, 0); float xx = x; var pts = new List<Vector2> { new Vector2(x, 0) }; for (float y = 0; y <= len; y += 10) { xx += (R() - .5f) * 3.5f; c.Linha(xx, y); pts.Add(new Vector2(xx, y)); }
                // o contorno com gradiente vertical: pinta em faixas
                for (int s = 0; s + 1 < pts.Count; s++) { c.contorno = grad(pts[s].x, pts[s].y); c.Iniciar(); c.Mover(pts[s].x, pts[s].y); c.Linha(pts[s + 1].x, pts[s + 1].y); c.Contornar(); }
            }
            return _musgo = c.Textura(false, "musgo");
        }
        public static Texture2D Samambaia()
        {
            if (_samambaia != null) return _samambaia; int W = 128, H = 256; var c = new Tela(W, H); c.preencher = c.contorno = new Color(92 / 255f, 118 / 255f, 70 / 255f); c.largura = 3;
            c.Iniciar(); c.Mover(W / 2f, H); c.Curva(W / 2f + 8, H / 2f, W / 2f, 4); c.Contornar();
            for (float y = H - 14; y > 10; y -= 9)
            {
                float t = 1 - y / H, len = (W / 2f - 8) * (.35f + .65f * (float)Math.Sin(Math.PI * Math.Min(1, (1 - t) * .95f + .05f))), xc = W / 2f + 4 * (1 - Math.Abs(y - H / 2f) / (H / 2f));
                foreach (int sg in new[] { -1, 1 }) { c.Iniciar(); c.Elipse(xc + sg * len / 2, y - sg * 2, len / 2, 3.4f, sg * .25f, 0, PI * 2); c.Preencher(); }
            }
            return _samambaia = c.Textura(false, "samambaia");
        }
        public static Texture2D Pegada()
        {
            if (_pegada != null) return _pegada; var c = new Tela(64, 64); c.preencher = Color.white;
            c.Iniciar(); c.Elipse(32, 22, 11, 15, 0, 0, PI * 2); c.Preencher(); c.Iniciar(); c.Elipse(32, 48, 8, 10, 0, 0, PI * 2); c.Preencher();
            return _pegada = c.Textura(false, "pegada");
        }
        public static Texture2D Gradiente(string nome, params object[] paradas) { var c = new Tela(8, 256); c.gradiente = Tela.Linear(0, 0, 0, 256, paradas); c.Retangulo(0, 0, 8, 256); return c.Textura(false, nome); }
    }
}
