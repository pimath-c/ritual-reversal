// Geometria no estilo do three.js (o mesmo desenho que o protótipo usa): caixa, plano, círculo, anel, cilindro, cone,
// esfera, poliedros, toro, torno (lathe), tubo, extrusão e forma plana, com as mesmas medidas e os mesmos parâmetros.
// Tudo é montado nas coordenadas do protótipo (mão direita); o mundo inteiro fica sob uma raiz com escala (1, 1, -1),
// que leva ao Unity (x, y, -z). As faces são orientadas pelas normais (a frente fica para fora), então o mesmo código
// vale nos dois lados do espelho. Só ToMesh() usa a API do Unity; o resto é conta pura (dá para testar fora do Unity).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal.Visual
{
    // matriz 4x4 própria (três primeiras linhas; a quarta é 0 0 0 1), para não depender das funções nativas do Unity
    public struct M4
    {
        public float a, b, c, d, e, f, g, h, i, j, k, l; // [a b c d; e f g h; i j k l]
        public static M4 I { get { return new M4 { a = 1, f = 1, k = 1 }; } }
        public static M4 T(float x, float y, float z) { var m = I; m.d = x; m.h = y; m.l = z; return m; }
        public static M4 S(float x, float y, float z) { return new M4 { a = x, f = y, k = z }; }
        public static M4 RX(float t) { float s = (float)Math.Sin(t), co = (float)Math.Cos(t); return new M4 { a = 1, f = co, g = -s, j = s, k = co }; }
        public static M4 RY(float t) { float s = (float)Math.Sin(t), co = (float)Math.Cos(t); return new M4 { a = co, c = s, f = 1, i = -s, k = co }; }
        public static M4 RZ(float t) { float s = (float)Math.Sin(t), co = (float)Math.Cos(t); return new M4 { a = co, b = -s, e = s, f = co, k = 1 }; }
        // rotação de Euler na ordem XYZ do three.js (R = Rx · Ry · Rz)
        public static M4 Euler(float x, float y, float z) { return RX(x) * RY(y) * RZ(z); }
        public static M4 operator *(M4 p, M4 q)
        {
            return new M4
            {
                a = p.a * q.a + p.b * q.e + p.c * q.i, b = p.a * q.b + p.b * q.f + p.c * q.j, c = p.a * q.c + p.b * q.g + p.c * q.k, d = p.a * q.d + p.b * q.h + p.c * q.l + p.d,
                e = p.e * q.a + p.f * q.e + p.g * q.i, f = p.e * q.b + p.f * q.f + p.g * q.j, g = p.e * q.c + p.f * q.g + p.g * q.k, h = p.e * q.d + p.f * q.h + p.g * q.l + p.h,
                i = p.i * q.a + p.j * q.e + p.k * q.i, j = p.i * q.b + p.j * q.f + p.k * q.j, k = p.i * q.c + p.j * q.g + p.k * q.k, l = p.i * q.d + p.j * q.h + p.k * q.l + p.l
            };
        }
        public Vector3 Ponto(Vector3 v) { return new Vector3(a * v.x + b * v.y + c * v.z + d, e * v.x + f * v.y + g * v.z + h, i * v.x + j * v.y + k * v.z + l); }
        public Vector3 Direcao(Vector3 v) { return new Vector3(a * v.x + b * v.y + c * v.z, e * v.x + f * v.y + g * v.z, i * v.x + j * v.y + k * v.z); }
        // normais: inversa transposta da parte 3x3
        public Vector3 Normal(Vector3 v)
        {
            float A = f * k - g * j, B = -(e * k - g * i), C = e * j - f * i, D = -(b * k - c * j), E = a * k - c * i, F = -(a * j - b * i), G = b * g - c * f, H = -(a * g - c * e), K = a * f - b * e;
            var n = new Vector3(A * v.x + B * v.y + C * v.z, D * v.x + E * v.y + F * v.z, G * v.x + H * v.y + K * v.z);
            float det = a * A + b * B + c * C; if (det < 0) n = -n;
            float m = (float)Math.Sqrt(n.x * n.x + n.y * n.y + n.z * n.z); return m > 1e-12f ? n / m : n;
        }
        // composição como Object3D do three: posição, rotação de Euler XYZ e escala
        public static M4 TRS(float px, float py, float pz, float rx, float ry, float rz, float sx, float sy, float sz) { return T(px, py, pz) * Euler(rx, ry, rz) * S(sx, sy, sz); }
    }

    public class Geo
    {
        public List<Vector3> p = new List<Vector3>(), n = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
        public List<Color> cor; // cor por vértice (opcional)
        public List<Vector2> uv2; // canto dos sprites (shader RitualReversal/Brilho com _Billboard)
        const float PI = (float)Math.PI;

        public int V(Vector3 pos, Vector3 nor, Vector2 u) { p.Add(pos); n.Add(nor); uv.Add(u); return p.Count - 1; }
        public void Tri(int a, int b, int c) { t.Add(a); t.Add(b); t.Add(c); }
        static float Sin(double x) { return (float)Math.Sin(x); }
        static float Cos(double x) { return (float)Math.Cos(x); }

        // ---------- transformações (como geometry.translate/rotateX/... do three) ----------
        public Geo Aplicar(M4 m) { for (int i = 0; i < p.Count; i++) { p[i] = m.Ponto(p[i]); n[i] = m.Normal(n[i]); } return this; }
        public Geo Mover(float x, float y, float z) { return Aplicar(M4.T(x, y, z)); }
        public Geo GirarX(float a) { return Aplicar(M4.RX(a)); }
        public Geo GirarY(float a) { return Aplicar(M4.RY(a)); }
        public Geo GirarZ(float a) { return Aplicar(M4.RZ(a)); }
        public Geo Escalar(float x, float y, float z) { return Aplicar(M4.S(x, y, z)); }
        public Geo Juntar(Geo o, M4? m = null)
        {
            int b = p.Count; var M = m ?? M4.I;
            for (int i = 0; i < o.p.Count; i++) { p.Add(M.Ponto(o.p[i])); n.Add(M.Normal(o.n[i])); uv.Add(o.uv[i]); }
            if (o.cor != null || cor != null) { if (cor == null) { cor = new List<Color>(); for (int i = 0; i < b; i++) cor.Add(Color.white); } for (int i = 0; i < o.p.Count; i++) cor.Add(o.cor != null ? o.cor[i] : Color.white); }
            if (o.uv2 != null || uv2 != null) { if (uv2 == null) { uv2 = new List<Vector2>(); for (int i = 0; i < b; i++) uv2.Add(Vector2.zero); } for (int i = 0; i < o.p.Count; i++) uv2.Add(o.uv2 != null ? o.uv2[i] : Vector2.zero); }
            bool espelha = M.a * (M.f * M.k - M.g * M.j) - M.b * (M.e * M.k - M.g * M.i) + M.c * (M.e * M.j - M.f * M.i) < 0;
            for (int i = 0; i < o.t.Count; i += 3) { if (espelha) { t.Add(o.t[i] + b); t.Add(o.t[i + 2] + b); t.Add(o.t[i + 1] + b); } else { t.Add(o.t[i] + b); t.Add(o.t[i + 1] + b); t.Add(o.t[i + 2] + b); } }
            return this;
        }
        public static Geo Unir(IEnumerable<Geo> gs) { var r = new Geo(); foreach (var g in gs) if (g != null) r.Juntar(g); return r; }
        public Geo Copia() { var g = new Geo(); g.Juntar(this); return g; }
        public Geo EscalarUV(float su, float sv) { for (int i = 0; i < uv.Count; i++) uv[i] = new Vector2(uv[i].x * su, uv[i].y * sv); return this; }
        public Geo Pintar(Color c) { cor = new List<Color>(); for (int i = 0; i < p.Count; i++) cor.Add(c); return this; }

        // Frente de cada triângulo = lado para onde a normal dos vértices aponta (a mesma regra do three e do Unity).
        public Geo Orientar()
        {
            for (int i = 0; i < t.Count; i += 3)
            {
                int a = t[i], b = t[i + 1], c = t[i + 2];
                var f = Vector3.Cross(p[b] - p[a], p[c] - p[a]); var m = n[a] + n[b] + n[c];
                if (f.x * m.x + f.y * m.y + f.z * m.z < 0) { t[i + 1] = c; t[i + 2] = b; }
            }
            return this;
        }
        // normais pela média das faces (computeVertexNormals), sem mexer na orientação
        public Geo NormaisDasFaces()
        {
            var acc = new Vector3[p.Count];
            for (int i = 0; i < t.Count; i += 3) { var f = Vector3.Cross(p[t[i + 1]] - p[t[i]], p[t[i + 2]] - p[t[i]]); acc[t[i]] += f; acc[t[i + 1]] += f; acc[t[i + 2]] += f; }
            for (int i = 0; i < p.Count; i++) { var v = acc[i]; float m = v.magnitude; n[i] = m > 1e-12f ? v / m : Vector3.up; }
            return this;
        }
        // facetado (flatShading): cada triângulo com vértices próprios e a normal da face, virada para fora do centro
        public Geo Facetar(bool paraFora = true)
        {
            var g = new Geo(); Vector3 centro = Vector3.zero; foreach (var q in p) centro += q; if (p.Count > 0) centro /= p.Count;
            for (int i = 0; i < t.Count; i += 3)
            {
                Vector3 A = p[t[i]], B = p[t[i + 1]], C = p[t[i + 2]]; var f = Vector3.Cross(B - A, C - A); float m = f.magnitude; if (m < 1e-12f) continue; f /= m;
                if (paraFora && Vector3.Dot(f, (A + B + C) / 3 - centro) < 0) { var tmp = B; B = C; C = tmp; f = -f; }
                int k = g.V(A, f, uv[t[i]]); g.V(B, f, uv[t[i + 1]]); g.V(C, f, uv[t[i + 2]]); g.Tri(k, k + 1, k + 2);
            }
            return g;
        }

        public Mesh ToMesh(string nome = "geo")
        {
            var m = new Mesh { name = nome };
            if (p.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(p); m.SetNormals(n); m.SetUVs(0, uv); if (uv2 != null) m.SetUVs(1, uv2); if (cor != null) m.SetColors(cor); m.SetTriangles(t, 0);
            m.RecalculateBounds(); return m;
        }

        // sprite: quadrado de sx × sy metros virado para a câmera, centrado em c (precisa do shader de brilho)
        public static Geo Sprite(Vector3 c, float sx, float sy, Color? cor = null)
        {
            var g = new Geo { uv2 = new List<Vector2>(), cor = new List<Color>() }; var k = cor ?? Color.white;
            float[,] q = { { -.5f, -.5f }, { .5f, -.5f }, { .5f, .5f }, { -.5f, .5f } };
            for (int i = 0; i < 4; i++) { g.V(c, Vector3.back, new Vector2(q[i, 0] + .5f, q[i, 1] + .5f)); g.uv2.Add(new Vector2(q[i, 0] * sx, q[i, 1] * sy)); g.cor.Add(k); }
            g.Tri(0, 1, 2); g.Tri(0, 2, 3); return g;
        }

        // ---------- primitivas (mesmos parâmetros do three.js) ----------
        // caixa centrada; S > 0 faz a textura repetir a cada S metros (boxGeo do protótipo)
        public static Geo Caixa(float w, float h, float d, float S = 0)
        {
            var g = new Geo();
            Action<Vector3, Vector3, Vector3, float, float> face = (nor, U, Vv, du, dv) =>
            {
                float su = S > 0 ? du / S : 1, sv = S > 0 ? dv / S : 1; var c = new Vector3(nor.x * w / 2, nor.y * h / 2, nor.z * d / 2);
                int k = g.V(c - U - Vv, nor, new Vector2(0, 0)); g.V(c + U - Vv, nor, new Vector2(su, 0)); g.V(c + U + Vv, nor, new Vector2(su, sv)); g.V(c - U + Vv, nor, new Vector2(0, sv));
                g.Tri(k, k + 1, k + 2); g.Tri(k, k + 2, k + 3);
            };
            face(Vector3.right, new Vector3(0, 0, -d / 2), new Vector3(0, h / 2, 0), d, h); face(Vector3.left, new Vector3(0, 0, d / 2), new Vector3(0, h / 2, 0), d, h);
            face(Vector3.up, new Vector3(w / 2, 0, 0), new Vector3(0, 0, -d / 2), w, d); face(Vector3.down, new Vector3(w / 2, 0, 0), new Vector3(0, 0, d / 2), w, d);
            face(Vector3.forward, new Vector3(w / 2, 0, 0), new Vector3(0, h / 2, 0), w, h); face(Vector3.back, new Vector3(-w / 2, 0, 0), new Vector3(0, h / 2, 0), w, h);
            return g.Orientar();
        }
        // plano XY virado para +Z
        public static Geo Plano(float w, float h, int sw = 1, int sh = 1)
        {
            var g = new Geo();
            for (int j = 0; j <= sh; j++) for (int i = 0; i <= sw; i++) g.V(new Vector3(-w / 2 + w * i / sw, h / 2 - h * j / sh, 0), Vector3.forward, new Vector2((float)i / sw, 1 - (float)j / sh));
            for (int j = 0; j < sh; j++) for (int i = 0; i < sw; i++) { int a = i + (sw + 1) * j, b = i + (sw + 1) * (j + 1), c = i + 1 + (sw + 1) * (j + 1), d = i + 1 + (sw + 1) * j; g.Tri(a, b, d); g.Tri(b, c, d); }
            return g.Orientar();
        }
        public static Geo Circulo(float r, int segs, float ini = 0, float arco = 2 * PI)
        {
            var g = new Geo(); int c = g.V(Vector3.zero, Vector3.forward, new Vector2(.5f, .5f));
            for (int s = 0; s <= segs; s++) { float a = ini + (float)s / segs * arco; g.V(new Vector3(r * Cos(a), r * Sin(a), 0), Vector3.forward, new Vector2((Cos(a) + 1) / 2, (Sin(a) + 1) / 2)); }
            for (int s = 1; s <= segs; s++) g.Tri(c, s, s + 1);
            return g.Orientar();
        }
        public static Geo Anel(float ri, float re, int segs, float ini = 0, float arco = 2 * PI)
        {
            var g = new Geo();
            for (int s = 0; s <= segs; s++) { float a = ini + (float)s / segs * arco; foreach (float r in new[] { ri, re }) g.V(new Vector3(r * Cos(a), r * Sin(a), 0), Vector3.forward, new Vector2((r * Cos(a) / re + 1) / 2, (r * Sin(a) / re + 1) / 2)); }
            for (int s = 0; s < segs; s++) { int a = s * 2; g.Tri(a, a + 1, a + 3); g.Tri(a, a + 3, a + 2); }
            return g.Orientar();
        }
        // cilindro no eixo Y, centrado (CylinderGeometry: raio de cima, raio de baixo, altura, segmentos...)
        public static Geo Cilindro(float rTopo, float rBase, float h, int segs = 8, int segH = 1, bool aberto = false, float ini = 0, float arco = 2 * PI)
        {
            var g = new Geo(); float inc = (rBase - rTopo) / h; var idx = new int[segH + 1, segs + 1];
            for (int y = 0; y <= segH; y++)
            {
                float v = (float)y / segH, r = v * (rBase - rTopo) + rTopo;
                for (int x = 0; x <= segs; x++)
                {
                    float u = (float)x / segs, th = u * arco + ini, s = Sin(th), c = Cos(th);
                    var nor = new Vector3(s, inc, c).normalized; idx[y, x] = g.V(new Vector3(r * s, -v * h + h / 2, r * c), nor, new Vector2(u, 1 - v));
                }
            }
            for (int x = 0; x < segs; x++) for (int y = 0; y < segH; y++) { int a = idx[y, x], b = idx[y + 1, x], c = idx[y + 1, x + 1], d = idx[y, x + 1]; g.Tri(a, b, d); g.Tri(b, c, d); }
            if (!aberto)
            {
                foreach (bool topo in new[] { true, false })
                {
                    float r = topo ? rTopo : rBase; if (r <= 0) continue; float sy = topo ? 1 : -1; var nor = new Vector3(0, sy, 0);
                    int c0 = g.V(new Vector3(0, h / 2 * sy, 0), nor, new Vector2(.5f, .5f)); int b0 = g.p.Count;
                    for (int x = 0; x <= segs; x++) { float th = (float)x / segs * arco + ini; g.V(new Vector3(r * Sin(th), h / 2 * sy, r * Cos(th)), nor, new Vector2(Cos(th) * .5f + .5f, Sin(th) * .5f * sy + .5f)); }
                    for (int x = 0; x < segs; x++) g.Tri(c0, b0 + x, b0 + x + 1);
                }
            }
            return g.Orientar();
        }
        public static Geo Cone(float r, float h, int segs = 8, int segH = 1, bool aberto = false) { return Cilindro(0, r, h, segs, segH, aberto); }
        public static Geo Esfera(float r, int ws = 8, int hs = 6, float phiIni = 0, float phiArco = 2 * PI, float thIni = 0, float thArco = PI)
        {
            var g = new Geo(); var idx = new int[hs + 1, ws + 1];
            for (int y = 0; y <= hs; y++)
            {
                float v = (float)y / hs;
                for (int x = 0; x <= ws; x++)
                {
                    float u = (float)x / ws; var q = new Vector3(-r * Cos(phiIni + u * phiArco) * Sin(thIni + v * thArco), r * Cos(thIni + v * thArco), r * Sin(phiIni + u * phiArco) * Sin(thIni + v * thArco));
                    idx[y, x] = g.V(q, r > 0 ? q.normalized : Vector3.up, new Vector2(u, 1 - v));
                }
            }
            for (int y = 0; y < hs; y++) for (int x = 0; x < ws; x++)
                {
                    int a = idx[y, x + 1], b = idx[y, x], c = idx[y + 1, x], d = idx[y + 1, x + 1];
                    if (y != 0 || thIni > 0) g.Tri(a, b, d); if (y != hs - 1 || thIni + thArco < PI) g.Tri(b, c, d);
                }
            return g.Orientar();
        }
        public static Geo Toro(float R, float tubo, int segR = 8, int segT = 6, float arco = 2 * PI)
        {
            var g = new Geo(); var idx = new int[segR + 1, segT + 1];
            for (int j = 0; j <= segR; j++) for (int i = 0; i <= segT; i++)
                {
                    float u = (float)i / segT * arco, v = (float)j / segR * PI * 2;
                    var q = new Vector3((R + tubo * Cos(v)) * Cos(u), (R + tubo * Cos(v)) * Sin(u), tubo * Sin(v)); var c = new Vector3(R * Cos(u), R * Sin(u), 0);
                    idx[j, i] = g.V(q, (q - c).normalized, new Vector2((float)i / segT, (float)j / segR));
                }
            for (int j = 1; j <= segR; j++) for (int i = 1; i <= segT; i++) { int a = idx[j, i - 1], b = idx[j - 1, i - 1], c = idx[j - 1, i], d = idx[j, i]; g.Tri(a, b, d); g.Tri(b, c, d); }
            return g.Orientar();
        }
        // perfil (raio, altura) girado em volta do eixo Y (LatheGeometry). A normal sai do perfil (dy, -dr).
        public static Geo Torno(float[,] perfil, int segs = 12, float ini = 0, float arco = 2 * PI)
        {
            int np = perfil.GetLength(0); var g = new Geo(); var n2 = new Vector2[np];
            for (int j = 0; j < np; j++)
            {
                Vector2 soma = Vector2.zero;
                if (j > 0) { float dr = perfil[j, 0] - perfil[j - 1, 0], dy = perfil[j, 1] - perfil[j - 1, 1]; soma += new Vector2(dy, -dr).normalized; }
                if (j < np - 1) { float dr = perfil[j + 1, 0] - perfil[j, 0], dy = perfil[j + 1, 1] - perfil[j, 1]; soma += new Vector2(dy, -dr).normalized; }
                n2[j] = soma.sqrMagnitude > 1e-12f ? soma.normalized : Vector2.up;
            }
            var idx = new int[segs + 1, np];
            for (int i = 0; i <= segs; i++)
            {
                float ph = ini + (float)i / segs * arco, s = Sin(ph), c = Cos(ph);
                for (int j = 0; j < np; j++) idx[i, j] = g.V(new Vector3(perfil[j, 0] * s, perfil[j, 1], perfil[j, 0] * c), new Vector3(n2[j].x * s, n2[j].y, n2[j].x * c), new Vector2((float)i / segs, (float)j / (np - 1)));
            }
            for (int i = 0; i < segs; i++) for (int j = 0; j < np - 1; j++) { int a = idx[i, j], b = idx[i + 1, j], c = idx[i + 1, j + 1], d = idx[i, j + 1]; g.Tri(a, b, d); g.Tri(b, c, d); }
            return g.Orientar();
        }

        // ---------- poliedros (PolyhedronGeometry), sempre facetados ----------
        static readonly float FI = (1 + (float)Math.Sqrt(5)) / 2;
        static Geo Poliedro(float[] v, int[] idx, float r, int detalhe)
        {
            var tris = new List<Vector3>();
            for (int i = 0; i < idx.Length; i += 3)
            {
                Vector3 A = new Vector3(v[idx[i] * 3], v[idx[i] * 3 + 1], v[idx[i] * 3 + 2]), B = new Vector3(v[idx[i + 1] * 3], v[idx[i + 1] * 3 + 1], v[idx[i + 1] * 3 + 2]), C = new Vector3(v[idx[i + 2] * 3], v[idx[i + 2] * 3 + 1], v[idx[i + 2] * 3 + 2]);
                int cols = detalhe + 1; var grade = new List<List<Vector3>>();
                for (int a = 0; a <= cols; a++)
                {
                    var linha = new List<Vector3>(); Vector3 aj = Vector3.Lerp(A, C, (float)a / cols), bj = Vector3.Lerp(B, C, (float)a / cols); int rows = cols - a;
                    for (int b = 0; b <= rows; b++) linha.Add(b == 0 && a == cols ? aj : Vector3.Lerp(aj, bj, rows == 0 ? 0 : (float)b / rows)); grade.Add(linha);
                }
                for (int a = 0; a < cols; a++) for (int b = 0; b < 2 * (cols - a) - 1; b++)
                    {
                        int k = b / 2;
                        if (b % 2 == 0) { tris.Add(grade[a][k + 1]); tris.Add(grade[a + 1][k]); tris.Add(grade[a][k]); }
                        else { tris.Add(grade[a][k + 1]); tris.Add(grade[a + 1][k + 1]); tris.Add(grade[a + 1][k]); }
                    }
            }
            var g = new Geo();
            for (int i = 0; i < tris.Count; i++) { var q = tris[i].normalized * r; float u = Mathf.Atan2(q.z, -q.x) / (2 * PI) + .5f, vv = Mathf.Asin(Mathf.Clamp(q.y / r, -1, 1)) / PI + .5f; g.V(q, q.normalized, new Vector2(u, vv)); }
            for (int i = 0; i < tris.Count; i += 3) g.Tri(i, i + 1, i + 2);
            return g.Facetar();
        }
        public static Geo Icosaedro(float r, int detalhe = 0)
        {
            float t = FI; var v = new float[] { -1, t, 0, 1, t, 0, -1, -t, 0, 1, -t, 0, 0, -1, t, 0, 1, t, 0, -1, -t, 0, 1, -t, t, 0, -1, t, 0, 1, -t, 0, -1, -t, 0, 1 };
            var idx = new[] { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
            return Poliedro(v, idx, r, detalhe);
        }
        public static Geo Octaedro(float r, int detalhe = 0)
        {
            var v = new float[] { 1, 0, 0, -1, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 1, 0, 0, -1 };
            var idx = new[] { 0, 2, 4, 0, 4, 3, 0, 3, 5, 0, 5, 2, 1, 2, 5, 1, 5, 3, 1, 3, 4, 1, 4, 2 };
            return Poliedro(v, idx, r, detalhe);
        }
        public static Geo Dodecaedro(float r, int detalhe = 0)
        {
            float t = FI, s = 1 / t;
            var v = new float[] { -1, -1, -1, -1, -1, 1, -1, 1, -1, -1, 1, 1, 1, -1, -1, 1, -1, 1, 1, 1, -1, 1, 1, 1, 0, -s, -t, 0, -s, t, 0, s, -t, 0, s, t, -s, -t, 0, -s, t, 0, s, -t, 0, s, t, 0, -t, 0, -s, t, 0, -s, -t, 0, s, t, 0, s };
            var idx = new[] { 3, 11, 7, 3, 7, 15, 3, 15, 13, 7, 19, 17, 7, 17, 6, 7, 6, 15, 17, 4, 8, 17, 8, 10, 17, 10, 6, 8, 0, 16, 8, 16, 2, 8, 2, 10, 0, 12, 1, 0, 1, 18, 0, 18, 16,
                6, 10, 2, 6, 2, 13, 6, 13, 15, 2, 16, 18, 2, 18, 3, 2, 3, 13, 18, 1, 9, 18, 9, 11, 18, 11, 3, 4, 14, 12, 4, 12, 0, 4, 0, 8, 11, 9, 5, 11, 5, 19, 11, 19, 7, 19, 5, 14, 19, 14, 4, 19, 4, 17, 1, 12, 14, 1, 14, 5, 1, 5, 9 };
            return Poliedro(v, idx, r, detalhe);
        }

        // ---------- curvas, tubo, forma plana e extrusão ----------
        // CatmullRomCurve3 do three (centrípeta, tensão 0,5)
        public class Curva
        {
            readonly List<Vector3> pts;
            public Curva(IList<Vector3> pontos) { pts = new List<Vector3>(pontos); }
            public Vector3 Ponto(float tt)
            {
                int l = pts.Count; float pp = (l - 1) * tt; int ip = (int)Math.Floor(pp); float w = pp - ip;
                if (w == 0 && ip == l - 1) { ip = l - 2; w = 1; }
                Vector3 p0 = ip > 0 ? pts[ip - 1] : 2 * pts[0] - pts[1], p1 = pts[ip], p2 = pts[Math.Min(ip + 1, l - 1)], p3 = ip + 2 < l ? pts[ip + 2] : 2 * pts[l - 1] - pts[l - 2];
                float d01 = Mathf.Pow((p1 - p0).sqrMagnitude, .25f), d12 = Mathf.Pow((p2 - p1).sqrMagnitude, .25f), d23 = Mathf.Pow((p3 - p2).sqrMagnitude, .25f);
                if (d12 < 1e-4f) d12 = 1; if (d01 < 1e-4f) d01 = d12; if (d23 < 1e-4f) d23 = d12;
                return new Vector3(Cr(p0.x, p1.x, p2.x, p3.x, d01, d12, d23, w), Cr(p0.y, p1.y, p2.y, p3.y, d01, d12, d23, w), Cr(p0.z, p1.z, p2.z, p3.z, d01, d12, d23, w));
            }
            static float Cr(float x0, float x1, float x2, float x3, float dt0, float dt1, float dt2, float w)
            {
                float t1 = (x1 - x0) / dt0 - (x2 - x0) / (dt0 + dt1) + (x2 - x1) / dt1, t2 = (x2 - x1) / dt1 - (x3 - x1) / (dt1 + dt2) + (x3 - x2) / dt2;
                t1 *= dt1; t2 *= dt1; float c0 = x1, c1 = t1, c2 = -3 * x1 + 3 * x2 - 2 * t1 - t2, c3 = 2 * x1 - 2 * x2 + t1 + t2;
                return c0 + c1 * w + c2 * w * w + c3 * w * w * w;
            }
        }
        public static Geo Tubo(Curva curva, int segs, float r, int segR = 8)
        {
            var g = new Geo(); var P = new Vector3[segs + 1]; for (int i = 0; i <= segs; i++) P[i] = curva.Ponto((float)i / segs);
            var T = new Vector3[segs + 1]; for (int i = 0; i <= segs; i++) T[i] = (P[Math.Min(i + 1, segs)] - P[Math.Max(i - 1, 0)]).normalized;
            // referenciais por transporte paralelo
            var N = new Vector3[segs + 1]; var B = new Vector3[segs + 1];
            var aux = Math.Abs(T[0].x) < .9f ? Vector3.right : Vector3.up; N[0] = Vector3.Cross(T[0], Vector3.Cross(aux, T[0])).normalized; B[0] = Vector3.Cross(T[0], N[0]);
            for (int i = 1; i <= segs; i++)
            {
                var ax = Vector3.Cross(T[i - 1], T[i]); N[i] = N[i - 1];
                if (ax.magnitude > 1e-6f) { ax.Normalize(); float th = Mathf.Acos(Mathf.Clamp(Vector3.Dot(T[i - 1], T[i]), -1, 1)); N[i] = Rodrigues(N[i - 1], ax, th); }
                B[i] = Vector3.Cross(T[i], N[i]);
            }
            var idx = new int[segs + 1, segR + 1];
            for (int i = 0; i <= segs; i++) for (int j = 0; j <= segR; j++)
                {
                    float v = (float)j / segR * PI * 2; var nor = (-Cos(v) * N[i] + Sin(v) * B[i]).normalized;
                    idx[i, j] = g.V(P[i] + r * nor, nor, new Vector2((float)i / segs, (float)j / segR));
                }
            for (int j = 1; j <= segs; j++) for (int i = 1; i <= segR; i++) { int a = idx[j - 1, i - 1], b = idx[j, i - 1], c = idx[j, i], d = idx[j - 1, i]; g.Tri(a, b, d); g.Tri(b, c, d); }
            return g.Orientar();
        }
        static Vector3 Rodrigues(Vector3 v, Vector3 k, float th) { float c = Cos(th), s = Sin(th); return v * c + Vector3.Cross(k, v) * s + k * Vector3.Dot(k, v) * (1 - c); }

        // Contorno 2D: pontos [x,y] (reta) ou [cx,cy,x,y] (curva quadrática), como Shape.lineTo/quadraticCurveTo
        public static List<Vector2> Contorno(float[][] pts, int passos = 12)
        {
            var o = new List<Vector2> { new Vector2(pts[0][0], pts[0][1]) };
            for (int i = 1; i < pts.Length; i++)
            {
                var q = pts[i];
                if (q.Length == 4) { var a = o[o.Count - 1]; var c = new Vector2(q[0], q[1]); var b = new Vector2(q[2], q[3]); for (int s = 1; s <= passos; s++) { float tt = (float)s / passos; o.Add((1 - tt) * (1 - tt) * a + 2 * (1 - tt) * tt * c + tt * tt * b); } }
                else o.Add(new Vector2(q[0], q[1]));
            }
            if ((o[0] - o[o.Count - 1]).sqrMagnitude < 1e-10f) o.RemoveAt(o.Count - 1);
            return o;
        }
        // triangulação por orelhas (polígono simples)
        public static List<int> Triangular(List<Vector2> poly)
        {
            var res = new List<int>(); int n = poly.Count; if (n < 3) return res;
            float area = 0; for (int i = 0; i < n; i++) { var a = poly[i]; var b = poly[(i + 1) % n]; area += a.x * b.y - b.x * a.y; }
            var V = new List<int>(); for (int i = 0; i < n; i++) V.Add(area > 0 ? i : n - 1 - i);
            int guarda = 0;
            while (V.Count > 3 && guarda++ < 10000)
            {
                bool cortou = false;
                for (int i = 0; i < V.Count; i++)
                {
                    int ia = V[(i + V.Count - 1) % V.Count], ib = V[i], ic = V[(i + 1) % V.Count]; Vector2 A = poly[ia], B = poly[ib], C = poly[ic];
                    if ((B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x) <= 1e-12f) continue;
                    bool dentro = false;
                    foreach (int k in V) { if (k == ia || k == ib || k == ic) continue; if (NoTri(poly[k], A, B, C)) { dentro = true; break; } }
                    if (dentro) continue;
                    res.Add(ia); res.Add(ib); res.Add(ic); V.RemoveAt(i); cortou = true; break;
                }
                if (!cortou) break;
            }
            if (V.Count == 3) { res.Add(V[0]); res.Add(V[1]); res.Add(V[2]); }
            return res;
        }
        static bool NoTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y), d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y), d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0; return !(neg && pos);
        }
        // forma plana no plano XY virada para +Z (ShapeGeometry)
        public static Geo Forma(List<Vector2> poly)
        {
            var g = new Geo(); foreach (var q in poly) g.V(new Vector3(q.x, q.y, 0), Vector3.forward, q);
            var tr = Triangular(poly); for (int i = 0; i < tr.Count; i += 3) g.Tri(tr[i], tr[i + 1], tr[i + 2]);
            return g.Orientar();
        }
        // extrusão do contorno em Z (de 0 a prof), com um chanfro simples de largura 'bisel' nas bordas das tampas
        public static Geo Extrusao(List<Vector2> poly, float prof, float bisel = 0)
        {
            var g = new Geo(); int n = poly.Count; var tr = Triangular(poly);
            float area = 0; for (int i = 0; i < n; i++) { var a = poly[i]; var b = poly[(i + 1) % n]; area += a.x * b.y - b.x * a.y; } float sinal = area > 0 ? 1 : -1;
            // tampas (um pouco para fora, como o bisel do three)
            foreach (float z in new[] { -bisel, prof + bisel })
            {
                var nor = new Vector3(0, 0, z > prof / 2 ? 1 : -1); int b0 = g.p.Count;
                foreach (var q in poly) g.V(new Vector3(q.x, q.y, z), nor, q);
                for (int i = 0; i < tr.Count; i += 3) g.Tri(b0 + tr[i], b0 + tr[i + 1], b0 + tr[i + 2]);
            }
            // lados
            for (int i = 0; i < n; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % n]; var e = b - a; var nor = new Vector3(e.y, -e.x, 0).normalized * sinal; float L = e.magnitude;
                int k = g.V(new Vector3(a.x, a.y, -bisel), nor, new Vector2(0, 0)); g.V(new Vector3(b.x, b.y, -bisel), nor, new Vector2(L, 0)); g.V(new Vector3(b.x, b.y, prof + bisel), nor, new Vector2(L, prof)); g.V(new Vector3(a.x, a.y, prof + bisel), nor, new Vector2(0, prof));
                g.Tri(k, k + 1, k + 2); g.Tri(k, k + 2, k + 3);
            }
            return g.Orientar();
        }
    }
}
