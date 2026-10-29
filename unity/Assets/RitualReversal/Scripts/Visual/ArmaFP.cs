// Primeira pessoa da v5.9/v6 no Unity: o client/arma.js inteiro. Mãos com esqueleto (dedos de três falanges e polegar),
// braços por IK de dois ossos, clipes com chaves em quadros de referência (corpo, arma, alavanca, guindaste, tambor),
// mistura entre estados, camadas por cima (coice e ciclo de cada arma, respiração, passo, tranco), peças que se mexem
// (alavanca, portinhola, ferrolho, cão, guindaste, extrator, tambor), cartuchos que trocam de pai, municiador, cápsulas e
// cartuchos que caem no chão, e o cetro com as runas em órbita, as vértebras acesas e o fio da canalização. As skins
// (client/skins.js) ficam em SkinsFP.cs.
// A árvore de objetos, as malhas, os materiais e os clipes já preparados saem do jogo oficial: node
// tools/exportar-primeira-pessoa.js grava Resources/primeira-pessoa.json e primeira-pessoa-geo.bytes. Aqui roda só o
// motor, com as contas do three.js em double (V3d, Qd, Md), nas coordenadas do three sob a raiz espelhada da câmera.
// node tools/unity-fp.js compara, nó a nó, as poses daqui com as do navegador.
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    // ---------------------------------------------------------------------------------------------------------------------
    // Matemática do three.js r128 (mesmas fórmulas, em double)
    public struct V3d
    {
        public double x, y, z;
        public V3d(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        public static V3d De(object o) { var l = Json.L(o); return new V3d(Json.D(l[0]), Json.D(l[1]), Json.D(l[2])); }
        public static V3d operator +(V3d a, V3d b) { return new V3d(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static V3d operator -(V3d a, V3d b) { return new V3d(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static V3d operator -(V3d a) { return new V3d(-a.x, -a.y, -a.z); }
        public static V3d operator *(V3d a, double k) { return new V3d(a.x * k, a.y * k, a.z * k); }
        public double Len { get { return Math.Sqrt(x * x + y * y + z * z); } }
        public double Dot(V3d b) { return x * b.x + y * b.y + z * b.z; }
        public V3d Cross(V3d b) { return new V3d(y * b.z - z * b.y, z * b.x - x * b.z, x * b.y - y * b.x); }
        public V3d Norm() { double l = Len; return this * (1 / (l != 0 ? l : 1)); }   // divideScalar(length()||1)
        public V3d Lerp(V3d b, double t) { return new V3d(x + (b.x - x) * t, y + (b.y - y) * t, z + (b.z - z) * t); }
        public V3d Rot(Qd q)
        {
            double ix = q.w * x + q.y * z - q.z * y, iy = q.w * y + q.z * x - q.x * z, iz = q.w * z + q.x * y - q.y * x, iw = -q.x * x - q.y * y - q.z * z;
            return new V3d(ix * q.w + iw * -q.x + iy * -q.z - iz * -q.y, iy * q.w + iw * -q.y + iz * -q.x - ix * -q.z, iz * q.w + iw * -q.z + ix * -q.y - iy * -q.x);
        }
        public V3d Mat(Md m)
        {
            var e = m.e; double w = 1 / (e[3] * x + e[7] * y + e[11] * z + e[15]);
            return new V3d((e[0] * x + e[4] * y + e[8] * z + e[12]) * w, (e[1] * x + e[5] * y + e[9] * z + e[13]) * w, (e[2] * x + e[6] * y + e[10] * z + e[14]) * w);
        }
        public Vector3 F { get { return new Vector3((float)x, (float)y, (float)z); } }
        public static V3d DeF(Vector3 v) { return new V3d(v.x, v.y, v.z); }
    }

    public struct Qd
    {
        public double x, y, z, w;
        public Qd(double x, double y, double z, double w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static readonly Qd I = new Qd(0, 0, 0, 1);
        public static Qd De(object o) { var l = Json.L(o); return new Qd(Json.D(l[0]), Json.D(l[1]), Json.D(l[2]), Json.D(l[3])); }
        public static Qd Mul(Qd a, Qd b)
        {
            return new Qd(a.x * b.w + a.w * b.x + a.y * b.z - a.z * b.y, a.y * b.w + a.w * b.y + a.z * b.x - a.x * b.z,
                a.z * b.w + a.w * b.z + a.x * b.y - a.y * b.x, a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        }
        public Qd Inv() { return new Qd(-x, -y, -z, w); }
        public Qd Normalizar() { double l = Math.Sqrt(x * x + y * y + z * z + w * w); return l == 0 ? I : new Qd(x / l, y / l, z / l, w / l); }
        public Qd Slerp(Qd b, double t)
        {
            if (t == 0) return this; if (t == 1) return b;
            double cos = w * b.w + x * b.x + y * b.y + z * b.z; Qd o;
            if (cos < 0) { o = new Qd(-b.x, -b.y, -b.z, -b.w); cos = -cos; } else o = b;
            if (cos >= 1) return this;
            double sqrSin = 1 - cos * cos;
            if (sqrSin <= 2.220446049250313e-16) { double s = 1 - t; return new Qd(s * x + t * o.x, s * y + t * o.y, s * z + t * o.z, s * w + t * o.w).Normalizar(); }
            double sin = Math.Sqrt(sqrSin), meio = Math.Atan2(sin, cos), rA = Math.Sin((1 - t) * meio) / sin, rB = Math.Sin(t * meio) / sin;
            return new Qd(x * rA + o.x * rB, y * rA + o.y * rB, z * rA + o.z * rB, w * rA + o.w * rB);
        }
        public static Qd Euler(double ex, double ey, double ez, string ordem = "XYZ")
        {
            double c1 = Math.Cos(ex / 2), c2 = Math.Cos(ey / 2), c3 = Math.Cos(ez / 2), s1 = Math.Sin(ex / 2), s2 = Math.Sin(ey / 2), s3 = Math.Sin(ez / 2);
            if (ordem == "YXZ") return new Qd(s1 * c2 * c3 + c1 * s2 * s3, c1 * s2 * c3 - s1 * c2 * s3, c1 * c2 * s3 - s1 * s2 * c3, c1 * c2 * c3 + s1 * s2 * s3);
            return new Qd(s1 * c2 * c3 + c1 * s2 * s3, c1 * s2 * c3 - s1 * c2 * s3, c1 * c2 * s3 + s1 * s2 * c3, c1 * c2 * c3 - s1 * s2 * s3);
        }
        public static Qd Eixo(V3d e, double a) { double s = Math.Sin(a / 2); return new Qd(e.x * s, e.y * s, e.z * s, Math.Cos(a / 2)); }
        public static Qd DeVetores(V3d de, V3d para)
        {
            double r = de.Dot(para) + 1; Qd q;
            if (r < 1e-6) { r = 0; q = Math.Abs(de.x) > Math.Abs(de.z) ? new Qd(-de.y, de.x, 0, r) : new Qd(0, -de.z, de.y, r); }
            else q = new Qd(de.y * para.z - de.z * para.y, de.z * para.x - de.x * para.z, de.x * para.y - de.y * para.x, r);
            return q.Normalizar();
        }
        public static Qd DeMatriz(double[] te)
        {
            double m11 = te[0], m12 = te[4], m13 = te[8], m21 = te[1], m22 = te[5], m23 = te[9], m31 = te[2], m32 = te[6], m33 = te[10], tr = m11 + m22 + m33;
            if (tr > 0) { double s = .5 / Math.Sqrt(tr + 1); return new Qd((m32 - m23) * s, (m13 - m31) * s, (m21 - m12) * s, .25 / s); }
            if (m11 > m22 && m11 > m33) { double s = 2 * Math.Sqrt(1 + m11 - m22 - m33); return new Qd(.25 * s, (m12 + m21) / s, (m13 + m31) / s, (m32 - m23) / s); }
            if (m22 > m33) { double s = 2 * Math.Sqrt(1 + m22 - m11 - m33); return new Qd((m12 + m21) / s, .25 * s, (m23 + m32) / s, (m13 - m31) / s); }
            { double s = 2 * Math.Sqrt(1 + m33 - m11 - m22); return new Qd((m13 + m31) / s, (m23 + m32) / s, .25 * s, (m21 - m12) / s); }
        }
        public Quaternion F { get { return new Quaternion((float)x, (float)y, (float)z, (float)w); } }
        public static Qd DeF(Quaternion q) { return new Qd(q.x, q.y, q.z, q.w); }
    }

    // matriz 4×4 em colunas (elements do three)
    public class Md
    {
        public readonly double[] e = new double[16];
        public static Md Id() { var m = new Md(); m.e[0] = m.e[5] = m.e[10] = m.e[15] = 1; return m; }
        public static Md De(object o) { var l = Json.L(o); var m = new Md(); for (int i = 0; i < 16; i++) m.e[i] = Json.D(l[i]); return m; }
        public static Md Compor(V3d p, Qd q, V3d s)
        {
            var m = new Md(); var t = m.e; double x2 = q.x + q.x, y2 = q.y + q.y, z2 = q.z + q.z, xx = q.x * x2, xy = q.x * y2, xz = q.x * z2, yy = q.y * y2, yz = q.y * z2, zz = q.z * z2, wx = q.w * x2, wy = q.w * y2, wz = q.w * z2;
            t[0] = (1 - (yy + zz)) * s.x; t[1] = (xy + wz) * s.x; t[2] = (xz - wy) * s.x; t[3] = 0;
            t[4] = (xy - wz) * s.y; t[5] = (1 - (xx + zz)) * s.y; t[6] = (yz + wx) * s.y; t[7] = 0;
            t[8] = (xz + wy) * s.z; t[9] = (yz - wx) * s.z; t[10] = (1 - (xx + yy)) * s.z; t[11] = 0;
            t[12] = p.x; t[13] = p.y; t[14] = p.z; t[15] = 1; return m;
        }
        public static Md Translacao(V3d p) { var m = Id(); m.e[12] = p.x; m.e[13] = p.y; m.e[14] = p.z; return m; }
        public static Md GiroZ(double a) { var m = Id(); double c = Math.Cos(a), s = Math.Sin(a); m.e[0] = c; m.e[4] = -s; m.e[1] = s; m.e[5] = c; return m; }
        public static Md Giro(Qd q) { return Compor(new V3d(0, 0, 0), q, new V3d(1, 1, 1)); }
        // this × b
        public Md Mul(Md b)
        {
            var o = new Md(); var a = e; var B = b.e;
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) o.e[j * 4 + i] = a[i] * B[j * 4] + a[4 + i] * B[j * 4 + 1] + a[8 + i] * B[j * 4 + 2] + a[12 + i] * B[j * 4 + 3];
            return o;
        }
        public double Det()
        {
            var t = e; double n11 = t[0], n12 = t[4], n13 = t[8], n14 = t[12], n21 = t[1], n22 = t[5], n23 = t[9], n24 = t[13], n31 = t[2], n32 = t[6], n33 = t[10], n34 = t[14], n41 = t[3], n42 = t[7], n43 = t[11], n44 = t[15];
            return n41 * (+n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34) + n42 * (+n11 * n23 * n34 - n11 * n24 * n33 + n14 * n21 * n33 - n13 * n21 * n34 + n13 * n24 * n31 - n14 * n23 * n31)
                + n43 * (+n11 * n24 * n32 - n11 * n22 * n34 - n14 * n21 * n32 + n12 * n21 * n34 + n14 * n22 * n31 - n12 * n24 * n31) + n44 * (-n13 * n22 * n31 - n11 * n23 * n32 + n11 * n22 * n33 + n13 * n21 * n32 - n12 * n21 * n33 + n12 * n23 * n31);
        }
        public void Decompor(out V3d p, out Qd q, out V3d s)
        {
            double sx = new V3d(e[0], e[1], e[2]).Len, sy = new V3d(e[4], e[5], e[6]).Len, sz = new V3d(e[8], e[9], e[10]).Len; if (Det() < 0) sx = -sx;
            p = new V3d(e[12], e[13], e[14]); var m = (double[])e.Clone(); double ix = 1 / sx, iy = 1 / sy, iz = 1 / sz;
            m[0] *= ix; m[1] *= ix; m[2] *= ix; m[4] *= iy; m[5] *= iy; m[6] *= iy; m[8] *= iz; m[9] *= iz; m[10] *= iz;
            q = Qd.DeMatriz(m); s = new V3d(sx, sy, sz);
        }
        public Qd Rotacao() { double sx = 1 / new V3d(e[0], e[1], e[2]).Len, sy = 1 / new V3d(e[4], e[5], e[6]).Len, sz = 1 / new V3d(e[8], e[9], e[10]).Len; var m = new double[16]; m[0] = e[0] * sx; m[1] = e[1] * sx; m[2] = e[2] * sx; m[4] = e[4] * sy; m[5] = e[5] * sy; m[6] = e[6] * sy; m[8] = e[8] * sz; m[9] = e[9] * sz; m[10] = e[10] * sz; m[15] = 1; return Qd.DeMatriz(m); }
    }

    public class ArmaFP
    {
        const double PI = Math.PI, TAU = Math.PI * 2;
        const int ND = 8;
        static double Cl(double v, double a, double b) { return v < a ? a : v > b ? b : v; }
        static double Mix(double a, double b, double t) { return a + (b - a) * t; }
        static readonly V3d UM = new V3d(1, 1, 1);
        // curvas de tempo de cada trecho (a chave diz como se chega nela)
        public static double Curva(string e, double u)
        {
            switch (e)
            {
                case "l": return u;
                case "s": return u * u * (3 - 2 * u);
                case "w": return u < .5 ? 4 * u * u * u : 1 - Math.Pow(-2 * u + 2, 3) / 2;
                case "o": return 1 - Math.Pow(1 - u, 3);
                case "q": return 1 - Math.Pow(1 - u, 5);
                case "i": return u * u * u;
                case "b": { double c = 1.6, c3 = c + 1; return 1 + c3 * Math.Pow(u - 1, 3) + c * Math.Pow(u - 1, 2); }
                case "h": return u < 1 ? 0 : 1;
            }
            return u;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Clipes (já preparados no navegador) e poses
        class Ch { public double t; public string e = "s"; }
        class ChA : Ch { public V3d p; public Qd q; }
        class ChM : Ch { public string f; public V3d p; public Qd q; public float[] dd; public bool reta; }
        class ChV : Ch { public double v; }
        class ChP : Ch { public string m; public double s; }
        class ChC : Ch { public V3d p; }
        class Ev { public double t; public string tipo; public object a1, a2; }
        class Clipe
        {
            public string nome; public double dur; public bool laco; public List<ChA> a = new List<ChA>(); public List<ChM> D = new List<ChM>(), E = new List<ChM>();
            public List<KeyValuePair<string, List<ChV>>> c = new List<KeyValuePair<string, List<ChV>>>(); public List<ChC> cam = new List<ChC>();
            public List<KeyValuePair<string, List<ChP>>> pr = new List<KeyValuePair<string, List<ChP>>>(); public List<Ev> ev = new List<Ev>();
        }
        class PM { public string f = "arma"; public V3d p; public Qd q = Qd.I; public float[] dd = new float[ND]; }
        class Pr { public string m; public double s; }
        class Pose
        {
            public V3d ap; public Qd aq = Qd.I; public PM D = new PM(), E = new PM(); public Dictionary<string, double> c = new Dictionary<string, double>(); public V3d cam;
            public Dictionary<string, Pr> pr = new Dictionary<string, Pr>();
            public PM Mao(string h) { return h == "D" ? D : E; }
        }
        static Pose Copiar(Pose o, Pose s)
        {
            o.ap = s.ap; o.aq = s.aq;
            foreach (var h in new[] { "D", "E" }) { PM a = o.Mao(h), b = s.Mao(h); a.f = b.f; a.p = b.p; a.q = b.q; Array.Copy(b.dd, a.dd, ND); }
            o.c.Clear(); foreach (var kv in s.c) o.c[kv.Key] = kv.Value; o.cam = s.cam;
            o.pr.Clear(); foreach (var kv in s.pr) o.pr[kv.Key] = new Pr { m = kv.Value.m, s = kv.Value.s }; return o;
        }
        static double Cv(Dictionary<string, double> c, string k) { double v; return c.TryGetValue(k, out v) ? v : 0; }

        static void Trecho<T>(List<T> L, double t, out int i, out int j, out double u) where T : Ch
        {
            if (t <= L[0].t) { i = j = 0; u = 0; return; }
            if (t >= L[L.Count - 1].t) { i = j = L.Count - 1; u = 0; return; }
            i = 0; while (i < L.Count - 1 && L[i + 1].t <= t) i++; j = i + 1; var k = L[j]; double d = k.t - L[i].t;
            u = Curva(k.e, (t - L[i].t) / (d != 0 ? d : 1));
        }
        // Catmull-Rom entre as chaves (arcos naturais), com o tempo já curvado
        static V3d Cr(V3d p0, V3d p1, V3d p2, V3d p3, double u)
        {
            double u2 = u * u, u3 = u2 * u;
            return new V3d(.5 * (2 * p1.x + (-p0.x + p2.x) * u + (2 * p0.x - 5 * p1.x + 4 * p2.x - p3.x) * u2 + (-p0.x + 3 * p1.x - 3 * p2.x + p3.x) * u3),
                .5 * (2 * p1.y + (-p0.y + p2.y) * u + (2 * p0.y - 5 * p1.y + 4 * p2.y - p3.y) * u2 + (-p0.y + 3 * p1.y - 3 * p2.y + p3.y) * u3),
                .5 * (2 * p1.z + (-p0.z + p2.z) * u + (2 * p0.z - 5 * p1.z + 4 * p2.z - p3.z) * u2 + (-p0.z + 3 * p1.z - 3 * p2.z + p3.z) * u3));
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Rig: a arma e as mãos de um lado (caça ou culto)
        public class Mao { public Transform raiz, pinca; public Transform[][] segs = new Transform[4][]; public double[] abre = new double[4]; public Transform[] pol = new Transform[3]; public bool esq; public Transform luz; public Material mLuz; public float luzOp; }
        public class Braco { public Transform sup, ant, cot; }
        public class Arma
        {
            public string tipo; public Transform g; public Md base_; public V3d boca;
            // carabina
            public Transform alav, cao, port, ferr, camara, c0, c1; public V3d alavPos, ferrPos, entrada, dirEntrada, janela;
            // revólver
            public Transform tambor, guin, ext, mun; public V3d PIVO, tamborPos, extPos; public Transform[] munC; public Transform[] vivo, vazio; public Material cruzMat, prata;
            public float[] cruzCor = { .9f, .75f, .35f };
            // cetro
            public Transform cab, brasa, halo, runa; public Material[] vert; public Material mBrasa, mHalo, mRuna; public Color corOriginal, corBrasa;
            public float runaGiro, gotasT, gotaAcc, respiraT;
            // skin
            public SkinsFP.Skin skin; public readonly List<Material> papeis = new List<Material>(); public readonly List<GameObject> acess = new List<GameObject>();
        }
        public class Rig { public string nome; public Transform corpo, raiz; public Arma arma; public Mao D, E; public Braco bD, bE; public V3d ombroD, ombroE, poloD, poloE; public double[] ossoD, ossoE, bracoD, bracoE; public Mao M(string h) { return h == "D" ? D : E; } }

        // ---------------------------------------------------------------------------------------------------------------
        // Estado (o VM do index.html que a primeira pessoa usa: tempo desde o tiro, pausa de impacto, sigilo)
        public double tT = 9, pausa, cast, fase; public bool somFeito = true; public int vmTiros;
        public bool ok; public Transform vmH, vmC, flash; public Material mFlash; public float flashOp;
        public Arma carabina, revolver, cetro; Transform[] runas; Material[] mRunas; Transform orb; Material mOrb;
        public readonly Dictionary<string, Rig> rigs = new Dictionary<string, Rig>();
        readonly Dictionary<string, Clipe> clipes = new Dictionary<string, Clipe>();
        V3d PALMA_CENTRO, MUN_NA_MAO; Qd qPinca, qMunMao; readonly Dictionary<string, double> PADRAO_C = new Dictionary<string, double>();
        class Perfil { public double ataque, recuoZ, sobe, giro, mola, amort, c0, c1, somT; public string som; public bool capsula; }
        readonly Dictionary<string, Perfil> PERFIL = new Dictionary<string, Perfil>();
        public SkinsFP skins; public Transform raizMundo; public Transform vm; Transform[] T; Dictionary<string, object> D_;
        public Func<string, float, int, float, bool> AoTocar;   // nome, volume, máximo de vozes, variação
        public Action AoCapsula;                                 // cápsula pela janela da carabina (PrimeiraPessoa)
        public Func<int> Nivel = () => Qualidade.nivel;
        public float camFx = 1;

        class Est { public string nome = ""; public Clipe C; public double t, dur; }
        public class Forca { public string arma, clipe; public bool vazia, repetir, estudio; public int n = 3; public int? carga; public double? t; public double vel = 1; public double? fervor; public string vista; }
        public Forca forca;
        Est est = new Est(); Pose de, saida, base_; double fade, fadeDur; string arma; Rig R;
        bool visivel; double tUlt, parado, calmo, proxAjuste = 8, proxInspecao = 22; bool pedidoInspecao; V3d jolt, joltV; public V3d camJolt;
        bool recarga; bool recargaVazia; int carga = 8; double reloadAnt; int tiros; double ultTiro = -9; bool camaraCheia = true;
        int runasVisiveis; readonly double?[] runaNasce = new double?[7]; double? tUltRuna; double giroT; double brilho; bool queimando; bool tamborEjetado;
        LineRenderer fio; Material mFio;
        readonly Pose _pose = Nova(), _de = Nova(), _out = Nova();
        static Pose Nova() { return new Pose(); }

        public class Ctx
        {
            public double agora, agK, execK, colK; public bool andando, correndo, canalizando, queimando, temAltar; public float altarX, altarZ;
            public float bx, by, bz, brx, bry, brz;   // corpo: balanço do passo, atraso do mouse e do passo (index.html)
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Montagem a partir da exportação
        public ArmaFP(Transform vm)
        {
            this.vm = vm;
            var tx = Resources.Load<TextAsset>("primeira-pessoa"); var tb = Resources.Load<TextAsset>("primeira-pessoa-geo");
            if (tx == null || tb == null) { Debug.LogError("Ritual Reversal: faltam Resources/primeira-pessoa.json e primeira-pessoa-geo.bytes (node tools/exportar-primeira-pessoa.js)"); return; }
            D_ = Json.O(Json.Ler(tx.text)); var bin = tb.bytes;
            var geos = Json.L(D_["geometrias"]); var malhas = new Mesh[geos.Count];
            Func<int, Mesh> malha = k =>
            {
                if (malhas[k] != null) return malhas[k];
                var G = Json.O(geos[k]); int v = (int)Json.D(G["v"]), t = (int)Json.D(G["t"]), o = (int)Json.D(G["ini"]); var g = new Geo();
                for (int i = 0; i < v; i++)
                {
                    int p = o + i * 12, n = o + v * 12 + i * 12, u = o + v * 24 + i * 8;
                    g.V(new Vector3(BitConverter.ToSingle(bin, p), BitConverter.ToSingle(bin, p + 4), BitConverter.ToSingle(bin, p + 8)),
                        new Vector3(BitConverter.ToSingle(bin, n), BitConverter.ToSingle(bin, n + 4), BitConverter.ToSingle(bin, n + 8)), new Vector2(BitConverter.ToSingle(bin, u), BitConverter.ToSingle(bin, u + 4)));
                }
                int ti = o + v * 32; for (int i = 0; i < t; i += 3) g.Tri((int)BitConverter.ToUInt32(bin, ti + i * 4), (int)BitConverter.ToUInt32(bin, ti + i * 4 + 4), (int)BitConverter.ToUInt32(bin, ti + i * 4 + 8));
                return malhas[k] = g.ToMesh("fp " + k);
            };
            skins = new SkinsFP(Json.O(D_["skins"]), this);
            var mats = Json.L(D_["materiais"]); var M = new Material[mats.Count];
            for (int i = 0; i < mats.Count; i++) M[i] = CriarMaterial(Json.O(mats[i]));
            malhasF = malha; matsF = M;
            T = Arvore(Json.L(D_["nos"]), null, malha, M);
            // papéis
            var P = Json.O(D_["papeis"]);
            vmH = T[I(P["vmH"])]; vmC = T[I(P["vmC"])]; flash = T[I(P["flash"])]; mFlash = Rend(flash).sharedMaterial;
            var lr = Json.L(P["runas"]); runas = new Transform[lr.Count]; mRunas = new Material[lr.Count]; for (int i = 0; i < lr.Count; i++) { runas[i] = T[I(lr[i])]; mRunas[i] = Rend(runas[i]).sharedMaterial; }
            orb = T[I(P["orb"])]; mOrb = Rend(orb).sharedMaterial;
            var pc = Json.O(P["carabina"]); carabina = new Arma
            {
                tipo = "carabina", g = T[I(pc["g"])], base_ = Md.De(pc["base"]), alav = T[I(pc["alav"])], cao = T[I(pc["cao"])], port = T[I(pc["port"])], ferr = T[I(pc["ferr"])],
                camara = T[I(pc["camara"])], boca = V3d.De(pc["boca"]), janela = V3d.De(pc["janela"]), entrada = V3d.De(pc["entrada"]), dirEntrada = V3d.De(pc["dirEntrada"])
            };
            carabina.alavPos = PosNo(I(pc["alav"])); carabina.ferrPos = PosNo(I(pc["ferr"]));
            var pv = Json.O(P["revolver"]); revolver = new Arma
            {
                tipo = "revolver", g = T[I(pv["g"])], base_ = Md.De(pv["base"]), tambor = T[I(pv["tambor"])], guin = T[I(pv["guin"])], ext = T[I(pv["ext"])], cao = T[I(pv["cao"])],
                mun = T[I(pv["mun"])], PIVO = V3d.De(pv["PIVO"]), boca = V3d.De(pv["boca"]), cruzMat = M[I(pv["cruzMat"])], prata = M[I(pv["prata"])]
            };
            revolver.tamborPos = PosNo(I(pv["tambor"])); revolver.extPos = PosNo(I(pv["ext"]));
            var cams = Json.L(pv["camaras"]); revolver.vivo = new Transform[cams.Count]; revolver.vazio = new Transform[cams.Count];
            for (int i = 0; i < cams.Count; i++) { var c = Json.O(cams[i]); revolver.vivo[i] = T[I(c["vivo"])]; revolver.vazio[i] = T[I(c["vazio"])]; }
            var mc = Json.L(pv["munC"]); revolver.munC = new Transform[mc.Count]; for (int i = 0; i < mc.Count; i++) revolver.munC[i] = T[I(mc[i])];
            var px = Json.O(P["cetro"]); cetro = new Arma { tipo = "cetro", g = T[I(px["g"])], base_ = Md.De(px["base"]), cab = T[I(px["cab"])], brasa = T[I(px["brasa"])], halo = T[I(px["halo"])], runa = T[I(px["runa"])] };
            cetro.mBrasa = Rend(cetro.brasa).sharedMaterial; cetro.mHalo = Rend(cetro.halo).sharedMaterial; cetro.mRuna = Rend(cetro.runa).sharedMaterial;
            var lv = Json.L(px["vert"]); cetro.vert = new Material[lv.Count]; for (int i = 0; i < lv.Count; i++) cetro.vert[i] = Rend(T[I(lv[i])]).sharedMaterial;
            cetro.corOriginal = cetro.corBrasa = HexLinear((int)Json.D(px["corOriginal"]));
            foreach (var A in new[] { carabina, revolver, cetro }) skins.Preparar(A);
            // rigs
            var RG = Json.O(D_["rigs"]); var maoCache = new Dictionary<int, Mao>();   // a carabina e o revólver usam as mesmas mãos
            foreach (var kv in RG)
            {
                var r = Json.O(kv.Value); var maos = Json.O(r["maos"]); var br = Json.O(r["bracos"]); var om = Json.O(r["ombro"]); var po = Json.O(r["polo"]); var os = Json.O(r["osso"]); var bc = Json.O(r["braco"]);
                Func<object, Mao> mao = o =>
                {
                    var m = Json.O(o); Mao x; if (maoCache.TryGetValue(I(m["raiz"]), out x)) return x;
                    x = new Mao { raiz = T[I(m["raiz"])], pinca = T[I(m["pinca"])], esq = (bool)m["esq"] }; maoCache[I(m["raiz"])] = x; var dd = Json.L(m["dedos"]);
                    for (int k = 0; k < 4; k++) { var dk = Json.O(dd[k]); var sg = Json.L(dk["segs"]); x.segs[k] = new[] { T[I(sg[0])], T[I(sg[1])], T[I(sg[2])] }; x.abre[k] = Json.D(dk["abre"]); }
                    var pl = Json.L(m["pol"]); for (int k = 0; k < 3; k++) x.pol[k] = T[I(pl[k])]; return x;
                };
                Func<object, Braco> braco = o => { var b = Json.O(o); return new Braco { sup = T[I(b["sup"])], ant = T[I(b["ant"])], cot = T[I(b["cot"])] }; };
                Func<object, double[]> dv = o => { var l = Json.L(o); return new[] { Json.D(l[0]), Json.D(l[1]) }; };
                var rg = new Rig
                {
                    nome = kv.Key, corpo = T[I(r["corpo"])], raiz = T[I(r["raiz"])], arma = kv.Key == "carabina" ? carabina : kv.Key == "revolver" ? revolver : cetro,
                    D = mao(maos["D"]), E = mao(maos["E"]), bD = braco(br["D"]), bE = braco(br["E"]), ombroD = V3d.De(om["D"]), ombroE = V3d.De(om["E"]), poloD = V3d.De(po["D"]), poloE = V3d.De(po["E"]),
                    ossoD = dv(os["D"]), ossoE = dv(os["E"]), bracoD = dv(bc["D"]), bracoE = dv(bc["E"])
                };
                rigs[kv.Key] = rg;
            }
            // a caça usa as mesmas mãos na carabina e no revólver: os pinos de luz e os cartuchos são de cada mão
            foreach (var kv in Json.O(D_["clipes"])) clipes[kv.Key] = LerClipe(Json.O(kv.Value));
            var cte = Json.O(D_["cte"]);
            PALMA_CENTRO = V3d.De(cte["PALMA_CENTRO"]); MUN_NA_MAO = V3d.De(cte["MUN_NA_MAO"]); qPinca = Qd.De(cte["qPinca"]); qMunMao = Qd.De(cte["qMunMao"]);
            foreach (var kv in Json.O(cte["PADRAO_C"])) PADRAO_C[kv.Key] = Json.D(kv.Value);
            if (cte.ContainsKey("ZC_REV")) ZC_REV = Json.D(cte["ZC_REV"]);
            foreach (var kv in Json.O(D_["perfil"]))
            {
                var p = Json.O(kv.Value); var ci = Json.L(p["ciclo"]); object v;
                PERFIL[kv.Key] = new Perfil
                {
                    ataque = Json.D(p["ataque"]), recuoZ = Json.D(p["recuoZ"]), sobe = Json.D(p["sobe"]), giro = Json.D(p["giro"]), mola = Json.D(p["mola"]), amort = Json.D(p["amort"]), c0 = Json.D(ci[0]), c1 = Json.D(ci[1]),
                    som = p.TryGetValue("som", out v) ? (string)v : null, somT = p.TryGetValue("somT", out v) ? Json.D(v) : 0, capsula = p.TryGetValue("capsula", out v) && (bool)v
                };
            }
            foreach (var a in new[] { "carabina", "revolver", "cetro" }) Vestir(a, skins.Escolhida(a));
            Shader.SetGlobalFloat("_RRLeitura", .2f); Shader.SetGlobalFloat("_RRRim", .16f);
            vmH.gameObject.SetActive(false); vmC.gameObject.SetActive(false);
            ok = true;
        }
        static int I(object o) { return (int)Json.D(o); }
        static Renderer Rend(Transform t) { return t.GetComponent<Renderer>(); }
        V3d PosNo(int i) { return V3d.De(Json.O(Json.L(D_["nos"])[i])["p"]); }
        public static Color CorHex(int hex) { return Mat.Hex(hex); }
        // cor do three como está na memória (linear); Mat.Lin converte para o material
        public static Color HexLinear(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1); }
        public static Color CorLin(object o, float a = 1) { var l = Json.L(o); return Mat.Lin(new Color((float)Json.D(l[0]), (float)Json.D(l[1]), (float)Json.D(l[2]), a)); }

        // monta uma árvore exportada (nós com o pai antes dos filhos); raiz: pai dos nós sem pai (null = vm)
        internal Transform[] Arvore(List<object> nos, Transform raiz, Func<int, Mesh> malha, Material[] M)
        {
            var T = new Transform[nos.Count];
            for (int i = 0; i < nos.Count; i++)
            {
                var n = Json.O(nos[i]); int pai = I(n["pai"]); string k = Json.S(n["k"]);
                var go = new GameObject(Json.S(n["n"])); go.transform.SetParent(pai >= 0 ? T[pai] : raiz ?? vm, false);
                go.transform.localPosition = V3d.De(n["p"]).F; go.transform.localRotation = Qd.De(n["q"]).F; go.transform.localScale = V3d.De(n["s"]).F;
                if (k == "m" || k == "sp")
                {
                    Mesh me = k == "m" ? malha(I(n["geo"])) : (malhaSprite ?? (malhaSprite = Geo.Sprite(Vector3.zero, 1, 1).ToMesh("sprite")));
                    go.AddComponent<MeshFilter>().sharedMesh = me; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = M[I(n["mat"])];
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                }
                if (!(bool)n["vis"]) go.SetActive(false);
                T[i] = go.transform;
            }
            return T;
        }
        static Mesh malhaSprite;
        internal Func<int, Mesh> Malhas { get { return malhasF; } }
        Func<int, Mesh> malhasF; Material[] matsF;
        internal Material[] Mats { get { return matsF; } }

        // material da primeira pessoa: luz de leitura e detalhe das skins (RitualReversal/PrimeiraPessoa); sem o shader, URP/Lit
        static Shader shaderFP; static bool procurouFP;
        public static Material MaterialFP(Color cor, Texture2D mapa, Texture2D relevo, Vector2 rep, float rug, float met, Color emis, float emisI, bool dupla, float relevoK = 1)
        {
            if (!procurouFP) { procurouFP = true; shaderFP = Shader.Find("RitualReversal/PrimeiraPessoa"); if (shaderFP != null && !shaderFP.isSupported) shaderFP = null; }
            if (shaderFP == null) return Mat.Padrao(new Mat.Opcoes { cor = cor, mapa = mapa, relevo = relevo, repetir = rep, rugosidade = rug, metal = met, emissao = emis, emissaoK = emisI, duplaFace = dupla, relevoK = relevoK });
            var m = new Material(shaderFP) { name = "primeira pessoa" };
            m.SetColor("_BaseColor", cor); if (mapa != null) m.SetTexture("_BaseMap", mapa); m.SetTextureScale("_BaseMap", rep);
            if (relevo != null) { m.SetTexture("_BumpMap", relevo); m.SetFloat("_TemRelevo", 1); m.SetFloat("_BumpScale", relevoK); }
            m.SetFloat("_Rug", rug); m.SetFloat("_Metal", met); m.SetColor("_Emissao", emis); m.SetFloat("_EmissaoI", emisI); m.SetFloat("_Cull", dupla ? 0 : 2);
            return m;
        }
        static Texture2D TexDe(object nome)
        {
            var s = nome as string; if (string.IsNullOrEmpty(s)) return null;
            switch (s)
            {
                case "RT.glow": return Tex.Brilho(); case "RT.flame": return Tex.Chama(); case "RT.rune": return Tex.Runa();
            }
            var p = s.Split('.'); Pbr P = p[0] == "wood" ? Tex.wood : p[0] == "metal" ? Tex.metal : p[0] == "cloth" ? Tex.cloth : p[0] == "leather" ? Tex.leather : p[0] == "wall" ? Tex.wall : p[0] == "floor" ? Tex.floor : p[0] == "marble" ? Tex.marble : null;
            if (P == null) return null; return p[1] == "map" ? P.mapa : p[1] == "normalMap" ? P.relevo : null;
        }
        Material CriarMaterial(Dictionary<string, object> m)
        {
            string k = Json.S(m["k"]); float op = (float)Json.D(m["opac"]); object v;
            if (k == "sprite")
            {
                var s = Mat.Brilho(CorLin(m["cor"], op), TexDe(m["mapa"]), Json.S(m["mistura"]) == "aditiva" ? Mat.Mistura.Aditiva : Mat.Mistura.Alfa, true, true, 0, false, 3000);
                if (m.TryGetValue("giro", out v) && s.HasProperty("_Giro")) s.SetFloat("_Giro", (float)Json.D(v)); return s;
            }
            if (k == "basico") return Mat.Brilho(CorLin(m["cor"], op), null, Mat.Mistura.Alfa, false, true, 0, op >= 1, op >= 1 ? 2000 : 3000);
            Vector2 rep = Vector2.one; if (m.TryGetValue("rep", out v) && v != null) { var l = Json.L(v); rep = new Vector2((float)Json.D(l[0]), (float)Json.D(l[1])); }
            else if (m.TryGetValue("nrep", out v) && v != null) { var l = Json.L(v); rep = new Vector2((float)Json.D(l[0]), (float)Json.D(l[1])); }
            float nk = 1; if (m.TryGetValue("nesc", out v) && v != null) nk = (float)Json.D(Json.L(v)[0]);
            var emis = Json.L(m["emis"]); var cor = CorLin(m["cor"]);
            var mat = MaterialFP(cor, TexDe(m["mapa"]), TexDe(m["normal"]), rep, (float)Json.D(m["rug"]), (float)Json.D(m["met"]), CorLin(m["emis"]), (float)Json.D(m["emisI"]), I(m["lado"]) == 2, nk);
            mat.name = "fp " + (m.TryGetValue("papel", out v) && v != null ? (string)v : Json.S(m["nome"]));
            skins.Registrar(mat, m);
            return mat;
        }
        Clipe LerClipe(Dictionary<string, object> o)
        {
            var C = new Clipe { nome = Json.S(o["nome"]), dur = Json.D(o["dur"]), laco = (bool)o["laco"] };
            foreach (var x in Json.L(o["a"])) { var k = Json.O(x); C.a.Add(new ChA { t = Json.D(k["t"]), p = V3d.De(k["p"]), q = Qd.De(k["q"]), e = Json.S(k["e"]) }); }
            Func<object, ChM> mao = x => { var k = Json.O(x); var dd = Json.L(k["dd"]); var f = new float[ND]; for (int i = 0; i < ND; i++) f[i] = (float)Json.D(dd[i]); return new ChM { t = Json.D(k["t"]), f = Json.S(k["f"]), p = V3d.De(k["p"]), q = Qd.De(k["q"]), dd = f, e = Json.S(k["e"]), reta = (bool)k["reta"] }; };
            foreach (var x in Json.L(o["D"])) C.D.Add(mao(x)); foreach (var x in Json.L(o["E"])) C.E.Add(mao(x));
            foreach (var kv in Json.O(o["c"])) { var L = new List<ChV>(); foreach (var x in Json.L(kv.Value)) { var k = Json.O(x); L.Add(new ChV { t = Json.D(k["t"]), v = Json.D(k["v"]), e = Json.S(k["e"]) }); } C.c.Add(new KeyValuePair<string, List<ChV>>(kv.Key, L)); }
            foreach (var x in Json.L(o["cam"])) { var k = Json.O(x); C.cam.Add(new ChC { t = Json.D(k["t"]), p = V3d.De(k["p"]), e = Json.S(k["e"]) }); }
            foreach (var kv in Json.O(o["pr"]))
            {
                var L = new List<ChP>(); foreach (var x in Json.L(kv.Value)) { var k = Json.O(x); var vv = Json.O(k["v"]); object s; L.Add(new ChP { t = Json.D(k["t"]), m = Json.S(vv["m"]), s = vv.TryGetValue("s", out s) && s != null ? Json.D(s) : 0, e = Json.S(k["e"]) }); }
                C.pr.Add(new KeyValuePair<string, List<ChP>>(kv.Key, L));
            }
            foreach (var x in Json.L(o["ev"])) { var l = Json.L(x); C.ev.Add(new Ev { t = Json.D(l[0]), tipo = Json.S(l[1]), a1 = l.Count > 2 ? l[2] : null, a2 = l.Count > 3 ? l[3] : null }); }
            C.ev.Sort((a, b) => a.t.CompareTo(b.t));
            return C;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Amostrar, quadros de referência e mistura
        Md QuadroArma(Rig R, Pose pose) { return Md.Compor(pose.ap, pose.aq, UM).Mul(R.arma.base_); }
        Md Quadro(Rig R, Pose pose, string nome)
        {
            if (nome == "corpo") return Md.Id();
            var o = QuadroArma(R, pose); if (nome == "arma") return o;
            var W = R.arma; var c = pose.c;
            if (nome == "alavanca") { var P = W.alavPos; var q1 = Qd.Eixo(new V3d(1, 0, 0), Cv(c, "alav") * .95 * .82); return o.Mul(Md.Translacao(P)).Mul(Md.Giro(q1)).Mul(Md.Translacao(-P)); }
            if (nome == "guindaste" || nome == "tambor")
            {
                o = o.Mul(Md.Translacao(W.PIVO)).Mul(Md.GiroZ(Cv(c, "guin") * 1.42)); if (nome == "guindaste") return o;
                return o.Mul(Md.Translacao(W.tamborPos));
            }
            return o;
        }
        void MaoNoCorpo(Rig R, Pose pose, string f, V3d p, Qd q, out V3d P, out Qd Q)
        {
            if (f == "corpo") { P = p; Q = q; return; }
            var m = Quadro(R, pose, f).Mul(Md.Compor(p, q, UM)); V3d s; m.Decompor(out P, out Q, out s);
        }
        Pose Amostrar(Rig R, Clipe C, double t, Pose b, Pose o)
        {
            Copiar(o, b); int i, j; double u;
            if (C.a.Count > 0)
            {
                var L = C.a; Trecho(L, t, out i, out j, out u);
                if (i == j) { o.ap = L[i].p; o.aq = L[i].q; } else { o.ap = Cr(L[Math.Max(0, i - 1)].p, L[i].p, L[j].p, L[Math.Min(L.Count - 1, j + 1)].p, u); o.aq = L[i].q.Slerp(L[j].q, u); }
            }
            foreach (var kv in C.c) { var L = kv.Value; Trecho(L, t, out i, out j, out u); o.c[kv.Key] = Mix(L[i].v, L[j].v, u); }
            foreach (var kv in C.pr) { var L = kv.Value; Trecho(L, t, out i, out j, out u); ChP A = L[i], B = L[j]; o.pr[kv.Key] = new Pr { m = u >= 1 ? B.m : A.m, s = Mix(A.s, B.s, u) }; }
            if (C.cam.Count > 0) { Trecho(C.cam, t, out i, out j, out u); o.cam = C.cam[i].p.Lerp(C.cam[j].p, u); }
            foreach (var h in new[] { "D", "E" })
            {
                var L = h == "D" ? C.D : C.E; if (L.Count == 0) continue; Trecho(L, t, out i, out j, out u); ChM A = L[i], B = L[j]; var H = o.Mao(h);
                for (int n = 0; n < ND; n++) H.dd[n] = (float)Mix(A.dd[n], B.dd[n], u);
                if (i == j || A.f == B.f)
                {
                    H.f = A.f; if (i == j) { H.p = A.p; H.q = A.q; continue; }
                    if (B.reta) H.p = A.p.Lerp(B.p, u);
                    else { var p0 = i > 0 && L[i - 1].f == A.f ? L[i - 1].p : A.p; var p3 = j < L.Count - 1 && L[j + 1].f == A.f ? L[j + 1].p : B.p; H.p = Cr(p0, A.p, B.p, p3, u); }
                    H.q = A.q.Slerp(B.q, u); continue;
                }
                // quadros diferentes: as duas chaves no corpo, com as peças deste instante; a mão viaja solta
                V3d PA, PB; Qd QA, QB; MaoNoCorpo(R, o, A.f, A.p, A.q, out PA, out QA); MaoNoCorpo(R, o, B.f, B.p, B.q, out PB, out QB);
                H.f = "corpo"; H.p = PA.Lerp(PB, u); H.q = QA.Slerp(QB, u);
            }
            return o;
        }
        // mistura de duas poses (troca de estado). Mãos no mesmo quadro misturam por dentro dele; senão, no corpo.
        Pose Misturar(Rig R, Pose A, Pose B, double w, Pose o)
        {
            o.ap = A.ap.Lerp(B.ap, w); o.aq = A.aq.Slerp(B.aq, w);
            o.c.Clear(); foreach (var k in A.c.Keys) o.c[k] = 0; foreach (var k in B.c.Keys) o.c[k] = 0;
            foreach (var k in new List<string>(o.c.Keys)) { double pa, pb, pd; PADRAO_C.TryGetValue(k, out pd); if (!A.c.TryGetValue(k, out pa)) pa = pd; if (!B.c.TryGetValue(k, out pb)) pb = pd; o.c[k] = Mix(pa, pb, w); }
            o.cam = A.cam.Lerp(B.cam, w); o.pr.Clear(); foreach (var kv in (w < .5 ? A.pr : B.pr)) o.pr[kv.Key] = new Pr { m = kv.Value.m, s = kv.Value.s };
            foreach (var h in new[] { "D", "E" })
            {
                PM a = A.Mao(h), b = B.Mao(h), H = o.Mao(h); for (int n = 0; n < ND; n++) H.dd[n] = (float)Mix(a.dd[n], b.dd[n], w);
                if (a.f == b.f) { H.f = a.f; H.p = a.p.Lerp(b.p, w); H.q = a.q.Slerp(b.q, w); }
                else { V3d PA, PB; Qd QA, QB; MaoNoCorpo(R, A, a.f, a.p, a.q, out PA, out QA); MaoNoCorpo(R, B, b.f, b.p, b.q, out PB, out QB); H.f = "corpo"; H.p = PA.Lerp(PB, w); H.q = QA.Slerp(QB, w); }
            }
            return o;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Troca de estado
        Clipe ClipeDe(string nome) { Clipe C; return clipes.TryGetValue(arma + "_" + nome, out C) ? C : null; }
        Clipe Recarga(bool vazia, int n) { return arma == "carabina" ? clipes["carabina_recarga_" + (vazia ? 1 : 0) + "_" + n] : clipes["revolver_recarga_" + (vazia ? 1 : 0)]; }
        void Entrar(string nome, Clipe C, double fade_, double t = 0)
        {
            if (C == null) { C = ClipeDe("pronta"); nome = "pronta"; }
            if (saida != null) Copiar(_de, saida); de = saida != null ? _de : null; fade = 0; fadeDur = saida != null ? fade_ : 0;
            est = new Est { nome = nome, C = C, t = t, dur = C.dur };
        }
        string EstadoDesejado(Ator m, Ctx ctx)
        {
            if (recarga) return "recarga";
            bool H = m.team == "H";
            if (H && m.sealing >= 0) return "selar";
            if (m.hold != null && !string.IsNullOrEmpty(m.hold.key)) return m.hold.key.StartsWith("exe") ? "executar" : !H && m.hold.key.StartsWith("col") && ClipeDe("coletar") != null ? "coletar" : "abaixada";
            if (!H && ctx.canalizando) return "canalizar";
            if (ctx.correndo && ctx.agora - ultTiro > .45) return "corrida";
            return "pronta";
        }
        // eventos do clipe (sons, cápsulas no mundo, municiador caindo, empurrões de câmera)
        void Evento(Ev E)
        {
            switch (E.tipo)
            {
                case "som": Tocar((string)E.a1, E.a2 == null ? .6f : (float)Json.D(E.a2), 2, .035f); return;
                case "cam": { var l = Json.L(E.a1); double f = camFx; joltV = joltV + new V3d(Json.D(l[0]), Json.D(l[1]), Json.D(l[2])) * (f * 30); return; }
                case "camara": camaraCheia = true; return;
                case "ejetar": EjetarTambor(E.a1 is bool && (bool)E.a1); return;
                case "soltar": SoltarMuniciador(); return;
                case "skin": if (R != null) skins.Gancho(R.arma, (string)E.a1); return;
            }
        }
        public bool Tocar(string nome, float vol, int max = 2, float varia = .035f) { return AoTocar != null && AoTocar(nome, vol, max, varia); }

        // ---------------------------------------------------------------------------------------------------------------
        // Aplicação: pose → objetos
        static void PorE(Transform t, double x, double y, double z, string ordem = "XYZ") { t.localRotation = Qd.Euler(x, y, z, ordem).F; }
        void Aplicar(Rig R, Pose pose, double[] tremor)
        {
            R.raiz.localPosition = pose.ap.F; R.raiz.localRotation = pose.aq.F;
            var W = R.arma; var c = pose.c; double cv;
            if (W.tipo == "carabina")
            {
                PorE(W.alav, Cv(c, "alav") * .95, 0, 0); PorE(W.cao, c.TryGetValue("caoAbs", out cv) ? cv : -.5, 0, 0); PorE(W.port, 0, -Cv(c, "port") * .48, 0);
                W.ferr.localPosition = new V3d(W.ferrPos.x, W.ferrPos.y, .018 + Cl(Cv(c, "alav") / .7, 0, 1) * .036).F; W.camara.gameObject.SetActive(Cv(c, "alav") > .28 && camaraCheia);
            }
            else if (W.tipo == "revolver")
            {
                PorE(W.guin, 0, 0, Cv(c, "guin") * 1.42); W.ext.localPosition = new V3d(W.extPos.x, W.extPos.y, Cv(c, "ext") * .03).F; PorE(W.tambor, 0, 0, Cv(c, "tambor")); PorE(W.cao, c.TryGetValue("caoAbs", out cv) ? cv : -.6, 0, 0);
                var cc = W.cruzCor; float kz = (float)Cv(c, "cruz"), b = (float)Cv(c, "brilho");
                SkinsFP.Emissao(W.cruzMat, new Color(cc[0] * kz, cc[1] * kz, cc[2] * kz)); SkinsFP.Emissao(W.prata, new Color(.5f * b, .42f * b, .2f * b));
            }
            // mãos e braços
            foreach (var h in new[] { "D", "E" })
            {
                var M = R.M(h); var H = pose.Mao(h); V3d PW; Qd QW; MaoNoCorpo(R, pose, H.f, H.p, H.q, out PW, out QW);
                // a chave dá o centro da palma; o punho fica atrás dele
                var raizP = PW - PALMA_CENTRO.Rot(QW); M.raiz.localPosition = raizP.F; M.raiz.localRotation = QW.F; PosarDedos(M, H.dd, h == "E" ? tremor : null);
                var B = h == "D" ? R.bD : R.bE; var os = h == "D" ? R.ossoD : R.ossoE; var br = h == "D" ? R.bracoD : R.bracoE; var SH = h == "D" ? R.ombroD : R.ombroE;
                // o antebraço termina um pouco atrás do punho (a manga cobre a luva)
                var PO = new V3d(0, 0, .004).Rot(QW) + raizP; var EL = Ik(SH, PO, os[0], os[1], h == "D" ? R.poloD : R.poloE);
                Tubo(B.sup, SH, EL, br[0]); Tubo(B.ant, EL, PO, br[1]); B.cot.localPosition = EL.F; float s = (float)(br[1] * 1.02); B.cot.localScale = new Vector3(s, s, s);
            }
        }
        static void PosarDedos(Mao M, float[] dd, double[] tremor)
        {
            for (int k = 0; k < 4; k++)
            {
                double c = Cl(dd[k] + (tremor != null ? tremor[k] : 0), -.15, 1.1);
                PorE(M.segs[k][0], -c * 1.3, M.abre[k] * (.3 + dd[7] * 1.6), 0, "YXZ"); PorE(M.segs[k][1], -c * 1.55, 0, 0); PorE(M.segs[k][2], -c * 1.05, 0, 0);
            }
            double a = dd[4], o = dd[5], d = dd[6]; PorE(M.pol[0], -.2 - o * .75, .55 + a * .75, -.5 - o * .4, "YXZ"); PorE(M.pol[1], -d * .75, 0, 0); PorE(M.pol[2], -d * .95, 0, 0);
        }
        static readonly V3d UP = new V3d(0, 1, 0);
        static void Tubo(Transform m, V3d P, V3d Q, double r1) { var v = Q - P; double L = v.Len; m.localPosition = P.F; m.localRotation = Qd.DeVetores(UP, v * (1 / (L != 0 ? L : 1))).F; m.localScale = new Vector3((float)r1, (float)L, (float)r1); }
        // IK de dois ossos: ombro S, punho W, ossos a e b; o cotovelo fica no plano que contém o polo
        static V3d Ik(V3d S, V3d W, double a, double b, V3d polo)
        {
            var v2 = W - S; double L = v2.Len; var dir = v2 * (1 / (L != 0 ? L : 1)); L = Cl(L, Math.Abs(a - b) + 1e-4, a + b - 1e-4);
            double cA = (a * a + L * L - b * b) / (2 * a * L), sA = Math.Sqrt(Math.Max(0, 1 - cA * cA)); var v3 = (polo - dir * polo.Dot(dir)).Norm();
            return S + dir * (a * cA) + v3 * (a * sA);
        }
        // objetos que mudam de pai conforme a pose (cartuchos da carabina, municiador e câmaras do revólver)
        void AplicarObjetos(Rig R, Pose pose)
        {
            var W = R.arma;
            if (W.tipo == "carabina")
            {
                for (int i = 0; i < 2; i++)
                {
                    string k = "c" + i; Pr pr; pose.pr.TryGetValue(k, out pr); var o = i == 0 ? W.c0 : W.c1;
                    if (o == null) { o = Clonar(W.camara, W.g); o.gameObject.name = k; o.gameObject.SetActive(false); if (i == 0) W.c0 = o; else W.c1 = o; }
                    if (pr == null || pr.m == "oculto") { o.gameObject.SetActive(false); continue; }
                    o.gameObject.SetActive(true); var pai = pr.m == "mao" ? R.D.pinca : W.g; if (o.parent != pai) o.SetParent(pai, false);
                    if (pr.m == "mao") { o.localPosition = Vector3.zero; o.localRotation = qPinca.F; }
                    else { o.localPosition = (W.entrada + W.dirEntrada * (pr.s * .065)).F; o.localRotation = Qd.Euler(0, .3, 0).F; o.gameObject.SetActive(pr.s < 1); }
                }
            }
            else if (W.tipo == "revolver")
            {
                Pr pm, pc; pose.pr.TryGetValue("mun", out pm); pose.pr.TryGetValue("cam", out pc);
                if (pm != null && pm.m != "oculto")
                {
                    W.mun.gameObject.SetActive(true); bool naMao = pm.m != "tambor"; var pai = naMao ? R.D.pinca : W.tambor; if (W.mun.parent != pai) W.mun.SetParent(pai, false);
                    foreach (var c in W.munC) c.gameObject.SetActive(pm.m != "mao_vazio");
                    if (naMao) { W.mun.localPosition = MUN_NA_MAO.F; W.mun.localRotation = qMunMao.F; } else { W.mun.localPosition = new Vector3(0, 0, (float)ZMun(pm.s)); W.mun.localRotation = Quaternion.identity; }
                }
                else W.mun.gameObject.SetActive(false);
                string e = pc != null ? pc.m : "antigas";
                for (int i = 0; i < W.vivo.Length; i++)
                {
                    bool vivo, vazio;
                    if (e == "novas") { vivo = true; vazio = false; } else if (e == "vazias") { vivo = vazio = false; } else { vivo = i < carga; vazio = !vivo; }
                    W.vivo[i].gameObject.SetActive(vivo); W.vazio[i].gameObject.SetActive(vazio);
                }
            }
        }
        // municiador no eixo do tambor: s=0 com as pontas encostando atrás das câmaras, s=1 com os aros rentes à face traseira
        static double ZC_REV = .0335;   // a face de trás do tambor (v6.5: a do revólver de verdade, exportada em cte.ZC_REV)
        static double ZMun(double s) { return ZC_REV + .004 + (1 - s) * .0365; }
        // cópia de uma peça (mesmas malhas e materiais)
        internal static Transform Clonar(Transform o, Transform pai)
        {
            var go = new GameObject(o.gameObject.name); var t = go.transform; t.SetParent(pai, false); t.localPosition = o.localPosition; t.localRotation = o.localRotation; t.localScale = o.localScale;
            var mf = o.GetComponent<MeshFilter>(); if (mf != null) { go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = o.GetComponent<MeshRenderer>().sharedMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            for (int i = 0; i < o.childCount; i++) { var f = o.GetChild(i); var c = Clonar(f, t); c.gameObject.SetActive(f.gameObject.activeSelf); }
            return t;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Camadas por cima do clipe: coice e ciclo mecânico (PERFIL_ARMA), dedos que acompanham, cão e tambor
        static double Mola(double t, double w, double z) { return Math.Exp(-z * w * t) * Math.Cos(w * Math.Sqrt(1 - z * z) * t); }
        static double Coice(double t, Perfil P) { return t < 0 ? 0 : t < P.ataque ? t / P.ataque : Mola(t - P.ataque, P.mola, P.amort); }
        static double CicloDe(double t, Perfil P) { return t < P.c0 || t > P.c1 ? 0 : Math.Sin(Math.PI * (t - P.c0) / (P.c1 - P.c0)); }
        static double Suave(double t) { return t * t * (3 - 2 * t); }
        void CamadaTiro(Pose pose)
        {
            var W = R.arma; bool cetroA = W.tipo == "cetro", exo = W.tipo == "revolver"; var Pf = PERFIL[cetroA ? "cetro" : exo ? "revolver" : "carabina"];
            double kc = Coice(tT, Pf), cy = CicloDe(tT, Pf), f = Math.Max(.35, camFx);
            pose.aq = Qd.Mul(Qd.Euler(kc * Pf.sobe * f, 0, (kc * Pf.giro - cy * (cetroA ? 0 : exo ? .06 : .2)) * f), pose.aq);
            pose.ap = new V3d(pose.ap.x + cy * .008, pose.ap.y - cy * .022 * (cetroA ? 0 : 1), pose.ap.z + kc * Pf.recuoZ * f);
            if (W.tipo == "carabina")
            {
                pose.c["alav"] = Cv(pose.c, "alav") + cy; pose.c["caoAbs"] = tT < .03 ? -.15 : -.5 - cy * .35;
                // a direita aperta a alça e a abre: dedos fecham um pouco na descida
                pose.D.dd[1] += (float)(cy * .08); pose.D.dd[2] += (float)(cy * .06);
            }
            else if (exo)
            {
                double a = Cl((tT - Pf.c0) / (Pf.c1 - Pf.c0), 0, 1), cao;
                pose.c["tambor"] = (tiros - 1 + Suave(a)) * PI / 4 + Cv(pose.c, "giro") * PI * 2 / 8 * 3;
                // o polegar sobe no cão e o puxa para trás (o cão cai no tiro e é armado no ciclo)
                pose.c["caoAbs"] = (tT < .04 ? -.15 : -.6 - cy * .45) - (1 - (pose.c.TryGetValue("cao", out cao) ? cao : 1)) * -.45;
                pose.D.dd[4] += (float)(cy * .35); pose.D.dd[6] -= (float)(cy * .25); pose.D.dd[5] -= (float)(cy * .35);
            }
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Quadro a quadro (quadroAnim)
        public void Quadro(double dtF, Ator m, Ctx ctx)
        {
            if (!ok) return; double dt = dtF, agora = ctx.agora; tUlt = agora;
            string qual = m.team == "C" ? "cetro" : m.cls == "exorcista" ? "revolver" : "carabina"; var F = forca;
            string a = F != null && F.arma != null ? F.arma : qual; bool vivo = F != null || m.st == "alive";
            if (a != arma) { arma = a; R = rigs[a]; saida = null; base_ = null; Entrar("pronta", ClipeDe("pronta"), 0); if (vivo) IniciarSacar(); }
            vmH.gameObject.SetActive(vivo && R.corpo == vmH); vmC.gameObject.SetActive(vivo && R.corpo == vmC); carabina.g.gameObject.SetActive(a == "carabina"); revolver.g.gameObject.SetActive(a == "revolver");
            if (!vivo) { visivel = false; recarga = false; return; }
            if (!visivel) { visivel = true; IniciarSacar(); }
            // munição atual (o revólver mostra nas câmaras quantos cartuchos e quantas cápsulas há)
            if (!recarga) { carga = F != null && F.carga != null ? F.carga.Value : m.ammo; camaraCheia = carga > 0; }
            // recarga: começa quando o servidor diz (reloadT > 0) e dura o que o servidor diz (só na subida de reloadT)
            if (F == null)
            {
                if (m.reloadT > 0 && !(reloadAnt > 0) && !recarga && m.team == "H") IniciarRecarga(m.ammo, m.reloadT); reloadAnt = m.reloadT;
                if (recarga && m.reloadT <= 0 && est.nome == "recarga" && est.t < est.dur - .12) est.t = Math.Max(est.t, est.dur - .12);
            }
            // estado desejado (fora dos clipes que terminam sozinhos)
            string em = est.nome; bool fixo = em == "sacar" || em == "recarga" || em == "inspecao" || em == "ajuste" || em == "gesto";
            if (F == null)
            {
                string d = EstadoDesejado(m, ctx);
                if (fixo) { if (em != "recarga" && em != "sacar" && d != "pronta") Entrar(d, ClipeDe(d), .16); }
                else if (d != em && d != "recarga") Entrar(d, ClipeDe(d), d == "corrida" ? .2 : em == "corrida" ? .22 : .18);
                // ocioso: ajuste de pegada e inspeção (só parado, fora de combate, sem nada para fazer)
                if (est.nome == "pronta")
                {
                    parado = ctx.andando ? 0 : parado + dt; calmo += dt;
                    if (pedidoInspecao || (calmo > proxInspecao && parado > 2.5)) { pedidoInspecao = false; calmo = 0; proxInspecao = 28 + UnityEngine.Random.value * 20; Entrar("inspecao", ClipeDe("inspecao"), .18); }
                    else if (calmo > proxAjuste && calmo < proxInspecao - 2) { proxAjuste = calmo + 7 + UnityEngine.Random.value * 7; Entrar("ajuste", ClipeDe("ajuste"), .12); }
                }
            }
            else if (F.clipe != null && em != F.clipe) ForcarEstado(F);
            // avança o tempo do clipe e dispara os eventos que ficaram para trás
            var E = est; var C = E.C; double vel = F != null ? F.vel : 1, tAnt = E.t;
            if (F != null && F.t != null) E.t = F.t.Value * E.dur; else E.t += dt * vel;
            if (C.laco) E.t %= C.dur; else if (F == null || F.t == null) foreach (var ev in C.ev) if (ev.t > tAnt && ev.t <= E.t) Evento(ev);
            if (!C.laco && E.t >= C.dur && !(F != null && F.t != null))
            {
                if (F != null && F.repetir) E.t = 0;
                else { if (E.nome == "recarga") recarga = false; if (F != null) F.clipe = "pronta"; string d = F != null ? "pronta" : EstadoDesejado(m, ctx); Entrar(d, ClipeDe(d), .12); }
            }
            // pose do clipe, mistura com a anterior
            if (base_ == null) base_ = Amostrar(R, ClipeDe("pronta"), 0, Nova(), Nova());
            Amostrar(R, est.C, Math.Min(est.t, est.C.dur), base_, _pose);
            if (de != null && fade < fadeDur) { fade += dt * vel; Misturar(R, de, _pose, Curva("s", Cl(fade / fadeDur, 0, 1)), _out); } else Copiar(_out, _pose);
            if (saida == null) saida = Nova(); Copiar(saida, _out);
            // camadas: tiro, vida (respiração e tremor sutil), corrida no passo
            if (pausa > 0) pausa = Math.Max(0, pausa - dt); else tT += dt;
            {
                var Pf = PERFIL[a];
                if (!somFeito && tT >= Pf.somT) { somFeito = true; if (Pf.som != null && m.team == "H") Tocar(Pf.som, .5f, 2, .05f); if (Pf.capsula && m.team == "H" && AoCapsula != null) AoCapsula(); }
            }
            var P2 = _out; CamadaTiro(P2);
            if (est.nome == "corrida") { double f = fase; P2.ap = new V3d(P2.ap.x + Math.Cos(f) * .012, P2.ap.y + Math.Sin(f * 2) * .012, P2.ap.z); P2.aq = Qd.Mul(Qd.Euler(Math.Sin(f * 2) * .03, 0, Math.Sin(f) * .06), P2.aq); }
            double resp = Math.Sin(agora * 1.6) * .0035, oscil = Math.Sin(agora * .71 + 1.3) * .0025;
            P2.ap = new V3d(P2.ap.x + oscil * .6, P2.ap.y + resp, P2.ap.z); P2.aq = Qd.Mul(Qd.Euler(resp * .6, oscil * .4, oscil * .5), P2.aq);
            // brilho de acerto confirmado (prata, latão, brasa): a arma reage quando o servidor confirma
            brilho = Math.Max(0, brilho - dt * 5); P2.c["brilho"] = brilho;
            AplicarObjetos(R, P2);
            queimando = ctx.queimando; double tr = est.nome == "selar" ? .03 : est.nome == "executar" ? .012 + .06 * ctx.execK : 0;
            double[] tremor = tr != 0 ? new[] { Math.Sin(agora * 31) * tr, Math.Sin(agora * 27 + 1) * tr, Math.Sin(agora * 29 + 2) * tr, Math.Sin(agora * 33 + 3) * tr } : null;
            if (est.nome == "coletar")
            {
                double k = ctx.colK; var Em = P2.E; if (Em.f == "corpo") Em.p = new V3d(Em.p.x + Math.Sin(agora * 29) * .006 * k, Em.p.y + k * .28, Em.p.z + k * .05);
                for (int n = 0; n < 4; n++) Em.dd[n] = (float)Math.Min(1, Em.dd[n] + k * .4);
            }
            if (est.nome == "executar" && ctx.execK > .6) { double k = (ctx.execK - .6) * .006; P2.ap = new V3d(P2.ap.x + Math.Sin(agora * 37) * k, P2.ap.y + Math.Sin(agora * 41 + 1) * k, P2.ap.z); }
            Aplicar(R, P2, tremor);
            LuzNaMao(R, agora);
            // corpo: balanço do passo, atraso do mouse, atraso ao começar a andar, tranco de golpe (mola)
            joltV = (joltV + jolt * (-dt * 160)) * Math.Exp(-dt * 16); jolt = jolt + joltV * dt;
            var cor = R.corpo; bool h = cor == vmH; double ag = ctx.agK;
            cor.localPosition = new V3d((h ? .2 : .26) + ctx.bx - ag * .012, (h ? -.17 : -.12) + ctx.by - ag * .022, (h ? -.42 : -.5) + ctx.bz + ag * .018).F;
            cor.localRotation = Qd.Euler(ctx.brx + jolt.x * 1.5 + ag * .04, ctx.bry, ctx.brz + jolt.z * 1.5 + ag * .07).F;
            camJolt = jolt * .5;
            if (a == "cetro") { CetroVida(P2, m, dt, agora, ctx); RastroBrasa(); }
            flash.localPosition = (a == "revolver" ? revolver.boca.Mat(revolver.base_) : carabina.boca).F;
            skins.Quadro((float)dt, new[] { carabina, revolver, cetro });
        }
        void LuzNaMao(Rig R, double agora)
        {
            var M = R.E;
            if (M.luz == null)
            {
                M.mLuz = Mat.Brilho(Mat.Hex(R.corpo == vmH ? 0xffd890 : 0xff3a5a, 0), Tex.Brilho(), Mat.Mistura.Aditiva, true);
                M.luz = Mundo.Malha(Geo.Sprite(Vector3.zero, 1, 1), M.mLuz, M.raiz, "luz na mão", false).transform; M.luz.localPosition = (PALMA_CENTRO + new V3d(0, -.03, -.01)).F;
            }
            bool on = est.nome == "selar" || est.nome == "canalizar" || est.nome == "gesto" || est.nome == "executar" || queimando;
            double alvo = on ? (.55 + .25 * Math.Sin(agora * 9) + .1 * Math.Sin(agora * 23)) : 0;
            M.luzOp += (float)((alvo - M.luzOp) * .2); Mat.Opacidade(M.mLuz, M.luzOp); float sc = .09f + M.luzOp * .07f; M.luz.localScale = new Vector3(sc, sc, sc); M.luz.gameObject.SetActive(M.luzOp > .01f);
        }
        void IniciarSacar() { var C = ClipeDe("sacar"); if (C != null) { saida = null; Entrar("sacar", C, 0); camaraCheia = true; } }
        void IniciarRecarga(int ammo, double reloadT)
        {
            if (!ok || R == null || arma == "cetro") return; bool vazia = ammo <= 0; int falta = 8 - ammo;
            var C = Recarga(vazia, vazia ? 3 : Math.Max(1, Math.Min(3, falta)));
            recarga = true; recargaVazia = vazia; carga = ammo; tamborEjetado = false; if (vazia) camaraCheia = false;
            // a recarga dura o que o servidor manda: se o aviso chegou atrasado, o clipe começa adiantado
            Entrar("recarga", C, .1, Cl(1.6 - reloadT, 0, .5));
        }
        void ForcarEstado(Forca F)
        {
            string nome = F.clipe; Clipe C;
            if (nome == "recarga" && arma != "cetro") { C = Recarga(F.vazia, F.vazia ? 3 : F.n); recarga = true; recargaVazia = F.vazia; carga = F.vazia ? 0 : (F.carga ?? 5); if (F.vazia) camaraCheia = false; }
            else C = ClipeDe(nome) ?? ClipeDe("pronta");
            saida = null; Entrar(nome, C, 0);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Cetro: runas que nascem da brasa quando o fervor volta, vértebras acesas, onda do lançamento, fio da canalização
        void CetroVida(Pose P2, Ator m, double dt, double agora, Ctx ctx)
        {
            var W = cetro; var c = P2.c; double kC = tT; double cs = cast;
            int cargas = (int)Math.Floor((forca != null && forca.fervor != null ? forca.fervor.Value : m.fervor) / CFG.sigilCusto);
            if (cargas > runasVisiveis && est.nome != "sacar")
            {
                for (int k = runasVisiveis; k < cargas && k < 7; k++) runaNasce[k] = agora;
                if (tUltRuna == null || agora - tUltRuna.Value > .3) { Tocar("runa_volta", .4f, 1); tUltRuna = agora; }
            }
            runasVisiveis = cargas;
            // a órbita anda por um tempo próprio: o frenesi acelera e a parada congela as runas no ar (inspeção do cetro)
            giroT += dt * 1.5 * (1 + cs * 6) * (1 + Cv(c, "frenesi") * 5) * (1 - Cv(c, "para")); double giro = giroT, orbita = Cv(c, "orbita");
            double recolhe = kC < .06 ? kC / .06 : Math.Max(0, 1 - (kC - .06) / .22), raioR = .085 * (1 - .8 * recolhe) * (1 - orbita * .25) + .03 * CicloDe(kC, PERFIL["cetro"]);
            for (int k = 0; k < runas.Length; k++)
            {
                bool on = k < cargas; runas[k].gameObject.SetActive(on); if (!on) continue;
                double nasce = runaNasce[k] != null ? Cl((agora - runaNasce[k].Value) / .4, 0, 1) : 1, e = Curva("o", nasce);
                double an = giro * (1 - orbita * .6) + k / 7.0 * TAU + (1 - e) * 3, rr = raioR * e;
                runas[k].localPosition = new V3d(Math.Cos(an) * rr, .1 + Math.Sin(an * 2 + k) * .012, Math.Sin(an) * rr).F; mRunas[k].SetFloat("_Giro", (float)(an * 2));
                float s = (float)(.035 * (.4 + .6 * e) * (1 + (1 - e) * .8)); runas[k].localScale = new Vector3(s, s, s);
            }
            // vértebras: acendem de baixo para cima ao sacar; no lançamento uma onda sobe pela espinha em 80 ms
            double ac; if (!c.TryGetValue("acende", out ac)) ac = 1; int n = W.vert.Length; double oi = Cv(c, "ondaInv");
            for (int i = 0; i < n; i++)
            {
                double u = i / (double)(n - 1), b = Cl(ac * 1.4 - u * .9, 0, 1) * .25, onda = Math.Max(0, 1 - Math.Abs(kC * 12.5 - u * 1) * 3) * (kC < .15 ? 1 : 0), can = est.nome == "canalizar" ? .35 + .25 * Math.Sin(agora * 6 - u * 5) : 0;
                double inv = oi != 0 ? Math.Max(0, 1 - Math.Abs(oi * 1.3 - (1 - u) - .15) * 4) * 1.4 : 0;   // a luz descendo a espinha, de cima para baixo
                SkinsFP.EmissaoI(W.vert[i], (float)(b + onda * 1.6 + can + inv + brilho * .8));
            }
            double br; if (!c.TryGetValue("brasa", out br)) br = 1;
            float bs = (float)(Math.Max(.001, br) * (1 + cs * .5 + recolhe * .6 + Math.Sin(agora * 6) * .05 + brilho * .5)); W.brasa.localScale = new Vector3(bs, bs, bs);
            W.runaGiro += (float)(dt * (1.5 + cs * 12)); W.mRuna.SetFloat("_Giro", W.runaGiro);
            Mat.Opacidade(mOrb, (float)Math.Min(1, (.3 + .5 * (m.fervor / 100) + cs * .5) * Math.Min(1, br) + brilho * .4));
            // fio de luz da brasa até o altar enquanto canaliza
            if (est.nome == "canalizar" && ctx.temAltar)
            {
                if (fio == null)
                {
                    var go = new GameObject("fio da canalização"); fio = go.AddComponent<LineRenderer>(); mFio = Mat.Brilho(Mat.Hex(0xff4a7a, .6f), null, Mat.Mistura.Aditiva);
                    fio.sharedMaterial = mFio; fio.positionCount = 2; fio.startWidth = fio.endWidth = .006f; fio.useWorldSpace = true;
                }
                fio.SetPosition(0, W.brasa.position); fio.SetPosition(1, new Vector3(ctx.altarX, 1.05f + (float)Math.Sin(agora * 3) * .05f, -ctx.altarZ));
                Mat.Opacidade(mFio, (float)(.35 + .25 * Math.Sin(agora * 9))); fio.gameObject.SetActive(true);
            }
            else if (fio != null) fio.gameObject.SetActive(false);
        }
        void RastroBrasa()
        {
            if (cast < .45 || R == null || R.corpo != vmC || raizMundo == null) return;
            var p = raizMundo.InverseTransformPoint(cetro.brasa.position); var m = Mat.Brilho(Mat.Lin(new Color(cetro.corBrasa.r, cetro.corBrasa.g, cetro.corBrasa.b, .7f)), Tex.Brilho(), Mat.Mistura.Aditiva, true);
            var sp = Mundo.Malha(Geo.Sprite(Vector3.zero, 1, 1), m, raizMundo, "rastro da brasa", false).transform; sp.localPosition = p; sp.localScale = new Vector3(.07f, .07f, .07f);
            Efeito(sp, m, .2f, (o, u) => { Mat.Opacidade(m, .7f * (1 - u)); float s = .07f * (1 - u * .6f); o.localScale = new Vector3(s, s, s); });
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Efeitos no mundo (cápsulas e cartuchos do tambor, municiador, fumaça, rastro, gotas): sob a raiz do mundo, nas
        // coordenadas do three
        class Ef { public Transform t; public Material m; public float dur, k; public Action<Transform, float> fn; }
        readonly List<Ef> efeitos = new List<Ef>();
        internal void Efeito(Transform t, Material m, float dur, Action<Transform, float> fn) { efeitos.Add(new Ef { t = t, m = m, dur = dur, fn = fn }); }
        public void AtualizarEfeitos(float dt)
        {
            for (int i = efeitos.Count - 1; i >= 0; i--)
            {
                var e = efeitos[i]; e.k += dt / e.dur; if (e.k >= 1) { if (e.t != null) UnityEngine.Object.Destroy(e.t.gameObject); if (e.m != null) UnityEngine.Object.Destroy(e.m); efeitos.RemoveAt(i); continue; }
                e.fn(e.t, e.k);
            }
        }
        // câmera do three a partir da do Unity (espelho em z: (x, y, −z); giro (−x, −y, z, w))
        Md CameraThree() { var c = vm.parent; var p = c.position; var q = c.rotation; return Md.Compor(new V3d(p.x, p.y, -p.z), new Qd(-q.x, -q.y, q.z, q.w), UM); }
        // matriz no mundo do three de uma peça da primeira pessoa (sobe pelos pais até a raiz espelhada da câmera)
        Md MundoThree(Transform o)
        {
            var M = Md.Id(); for (var t = o; t != null && t != vm; t = t.parent) M = Md.Compor(V3d.DeF(t.localPosition), Qd.DeF(t.localRotation), V3d.DeF(t.localScale)).Mul(M);
            return CameraThree().Mul(M);
        }
        void ParaMundo(Transform obj, V3d vel, V3d giro, float vida, double quica, float camY)
        {
            if (raizMundo == null) return;
            var o = Clonar(obj, raizMundo); o.gameObject.SetActive(true); for (int i = 0; i < o.childCount; i++) o.GetChild(i).gameObject.SetActive(true);
            V3d mp, ms; Qd mq; MundoThree(obj).Decompor(out mp, out mq, out ms); o.localPosition = mp.F; o.localRotation = mq.F; o.localScale = ms.F;
            double chao = Math.Max(.015, camY - 1.67 + .015); float tAnt = 0;
            Efeito(o, null, vida, (ob, k) =>
            {
                double d = Math.Min(.05, (k - tAnt) * vida); tAnt = k; if (d <= 0) return;
                vel = new V3d(vel.x, vel.y - 9.8 * d, vel.z); var p = V3d.DeF(ob.localPosition) + vel * d; ob.localPosition = p.F;
                ob.localRotation = ob.localRotation * Qd.Euler(giro.x * d, 0, 0).F * Qd.Euler(0, 0, giro.z * d).F;
                if (p.y < chao) { ob.localPosition = new Vector3((float)p.x, (float)chao, (float)p.z); vel = vel * quica; vel = new V3d(vel.x, Math.Abs(vel.y) * .5, vel.z); giro = giro * .5; }
            });
        }
        public float camY;
        void EjetarTambor(bool vazia)
        {
            var W = revolver; if (!W.g.gameObject.activeInHierarchy || raizMundo == null) return; var dirW = new V3d(0, 0, 1).Rot(MundoThree(W.tambor).Rotacao());
            for (int i = 0; i < W.vivo.Length; i++)
            {
                var obj = W.vivo[i].gameObject.activeSelf ? W.vivo[i] : W.vazio[i].gameObject.activeSelf ? W.vazio[i] : null; if (obj == null) continue;
                var v = dirW * (1.1 + UnityEngine.Random.value * .6) + new V3d((UnityEngine.Random.value - .5) * .6, .2, (UnityEngine.Random.value - .5) * .6);
                ParaMundo(obj, v, new V3d((UnityEngine.Random.value - .5) * 30, 0, (UnityEngine.Random.value - .5) * 30), 1.8f, .32, camY); W.vivo[i].gameObject.SetActive(false); W.vazio[i].gameObject.SetActive(false);
            }
            tamborEjetado = true;
        }
        void SoltarMuniciador()
        {
            var W = revolver; if (!W.mun.gameObject.activeInHierarchy || !W.g.gameObject.activeInHierarchy || raizMundo == null) return;
            foreach (var c in W.munC) c.gameObject.SetActive(false);
            var c3 = vm.parent.rotation; var v = new V3d(.3, -.6, .4).Rot(new Qd(-c3.x, -c3.y, c3.z, c3.w)); ParaMundo(W.mun, v, new V3d(6, 0, 9), 1.4f, .25, camY);
            foreach (var c in W.munC) c.gameObject.SetActive(true); W.mun.gameObject.SetActive(false);
        }
        // um fiapo pequeno na boca do cano (nada nos níveis Baixo e Mínimo)
        void Fumaca()
        {
            if (R == null || R.corpo != vmH || Nivel() < 2 || raizMundo == null) return;
            var p = raizMundo.InverseTransformPoint(flash.position); var m = Mat.Brilho(Mat.Hex(0x8a8478, 0), Tex.Brilho(), Mat.Mistura.Alfa, true);
            var sp = Mundo.Malha(Geo.Sprite(Vector3.zero, 1, 1), m, raizMundo, "fumaça", false).transform; sp.localPosition = p;
            var v = new Vector3((UnityEngine.Random.value - .5f) * .12f, .14f + UnityEngine.Random.value * .08f, (UnityEngine.Random.value - .5f) * .12f);
            Efeito(sp, m, .9f, (o, u) => { float t = Mathf.Max(0, u * .9f); o.localPosition += v / 60; float s = .04f + t * .16f; o.localScale = new Vector3(s, s, s); Mat.Opacidade(m, Mathf.Max(0, .18f * (1 - t / .85f)) * (t > 0 ? 1 : 0)); });
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Entradas vindas do jogo
        public void Tiro(bool exo)
        {
            ultTiro = tUlt; calmo = 0; tiros++; flashOp = 1; Mat.Opacidade(mFlash, 1);
            // cada disparo tem um clarão diferente (rotação e tamanho) e deixa um fiapo de fumaça na boca do cano
            if (mFlash.HasProperty("_Giro")) mFlash.SetFloat("_Giro", UnityEngine.Random.value * (float)TAU);
            float sc = (exo ? .36f : .3f) * (.8f + UnityEngine.Random.value * .4f); flash.localScale = new Vector3(sc * (1 + UnityEngine.Random.value * .3f), sc, 1);
            Fumaca(); if (EmRepouso()) Entrar("pronta", ClipeDe("pronta"), .07);
        }
        bool EmRepouso() { return R != null && (est.nome == "sacar" || est.nome == "inspecao" || est.nome == "ajuste" || est.nome == "corrida"); }
        public void Sigilo() { ultTiro = tUlt; calmo = 0; if (EmRepouso()) Entrar("pronta", ClipeDe("pronta"), .07); }
        public void Acerto(bool escudo, bool derrubou) { brilho = derrubou ? 1 : escudo ? .25 : .6; calmo = 0; }
        public void Golpe(float lado)
        {
            calmo = 0; double f = camFx; joltV = joltV + new V3d((UnityEngine.Random.value - .5) * .5 * f, -.3 * f, lado * .6 * f);
            if ((est.nome == "inspecao" || est.nome == "ajuste") && R != null) Entrar("pronta", ClipeDe("pronta"), .1);
        }
        public void Hab() { calmo = 0; if ((est.nome == "pronta" || est.nome == "ajuste" || est.nome == "inspecao" || est.nome == "corrida") && R != null && ClipeDe("gesto") != null) Entrar("gesto", ClipeDe("gesto"), .08); }
        public void PedirInspecao() { if (est.nome == "pronta") pedidoInspecao = true; }
        // o golpe final da execução (quando o servidor confirma a morte): tranco para a frente, pausa curta e o brilho de acerto
        public void Executou() { double f = camFx; joltV = joltV + new V3d(0, -.3 * f, -.5 * f); pausa = .1; brilho = 1; }
        // um tiro ou sigilo só de aparência (vitrine e Arsenal): coice, ciclo, clarão e som, sem nada no servidor
        public void DisparoForcado(string a)
        {
            a = a ?? arma; tT = 0; somFeito = false; vmTiros++; tiros++;
            if (a == "cetro") { cast = 1; Tocar("sigilo", .6f, 2); } else { flashOp = 1; Mat.Opacidade(mFlash, 1); Tocar(a == "revolver" ? "tiro_revolver" : "tiro_carabina", .7f, 2); }
        }
        public void Forcar(Forca o) { forca = o; if (o != null) { if (o.arma != null && o.arma != arma) arma = null; est.nome = "_"; } }
        // luz de estúdio (vitrine e Arsenal): a própria luz de leitura, mais forte
        public void LuzEstudio(bool on) { Shader.SetGlobalFloat("_RRLeitura", on ? 1.3f : .2f); Shader.SetGlobalFloat("_RRRim", on ? .4f : .16f); }
        public void Esconder() { if (!ok) return; vmH.gameObject.SetActive(false); vmC.gameObject.SetActive(false); visivel = false; recarga = false; if (fio != null) fio.gameObject.SetActive(false); }
        public string EstadoAtual { get { return est.nome; } }
        public double TempoClipe { get { return est.t; } }
        public double DuracaoClipe { get { return est.dur; } }
        public Transform JanelaCarabina(out Vector3 p) { p = carabina.janela.F; return carabina.g; }
        // troca de skin (Arsenal): grava a escolha e veste a arma na hora
        public bool TrocarSkin(string a, string id) { if (!skins.Escolher(a, id)) return false; Vestir(a, id); return true; }
        void Vestir(string a, string id)
        {
            var A = a == "carabina" ? carabina : a == "revolver" ? revolver : cetro; skins.Aplicar(A, a, id);
            if (a == "cetro") { var c = A.corBrasa; var claro = Color.Lerp(c, Color.white, .5f); foreach (var m in mRunas) Mat.Cor(m, Mat.Lin(new Color(claro.r, claro.g, claro.b, .9f))); Mat.Cor(A.mRuna, Mat.Lin(new Color(claro.r, claro.g, claro.b, .75f))); }
        }
        // decaimento do clarão (o index.html faz: opacidade − dt·14)
        public void Clarao(float dt) { if (!ok) return; flashOp = Mathf.Max(0, flashOp - dt * 14); Mat.Opacidade(mFlash, flashOp); }
        // vistas da vitrine e do Arsenal (camVista do arma.js): uma câmera presa à do jogador, de outro ângulo. Aqui a própria
        // câmera vai para lá e a raiz da primeira pessoa faz o caminho inverso, então a arma fica onde estava em relação ao
        // jogador. p e alvo no espaço da câmera do three; fov da vista.
        static readonly Dictionary<string, double[]> VISTAS = new Dictionary<string, double[]>
        {
            { "ladoPerto", new[] { -.4, -.06, -.36, .1, -.21, -.44, 40 } }, { "cimaPerto", new[] { .1, .2, -.38, .1, -.2, -.42, 40 } }, { "lado", new[] { -.62, -.12, -.36, .12, -.2, -.42, 50 } },
            { "cima", new[] { .12, .45, -.4, .12, -.2, -.42, 50 } }, { "frente", new[] { .18, -.12, -1.15, .12, -.2, -.42, 50 } }, { "direita", new[] { .75, -.1, -.4, .12, -.2, -.42, 50 } }, { "perto", new[] { -.05, -.06, -.12, .15, -.22, -.45, 62 } }
        };
        public float Vista(Camera cam, string nome, float fovJogo)
        {
            double[] v; V3d p, t; double fov;
            if (nome == "peca")
            {
                // perto da peça que trabalha (a vitrine): a portinhola da carabina ou o tambor do revólver, medidos no espaço da
                // câmera com a primeira pessoa no lugar de sempre
                vm.localPosition = Vector3.zero; vm.localRotation = Quaternion.identity;
                bool rev = arma == "revolver"; var g = rev ? revolver.g : carabina.g; var ct0 = cam.transform;
                Vector3 alvo = ct0.InverseTransformPoint(rev ? revolver.tambor.position : g.TransformPoint(carabina.entrada.F)), de = ct0.InverseTransformPoint(g.TransformPoint(rev ? new Vector3(-.16f, .07f, .12f) : new Vector3(.17f, .09f, .06f)));
                p = new V3d(de.x, de.y, -de.z); t = new V3d(alvo.x, alvo.y, -alvo.z); fov = 38;
            }
            else if (nome == null || !VISTAS.TryGetValue(nome, out v)) { vm.localPosition = Vector3.zero; vm.localRotation = Quaternion.identity; return fovJogo; }
            else { p = new V3d(v[0], v[1], v[2]); t = new V3d(v[3], v[4], v[5]); fov = v[6]; }
            var up = nome.StartsWith("cima") ? new V3d(0, 0, -1) : new V3d(0, 1, 0);
            // Matrix4.lookAt do three para câmera: z aponta do alvo para o olho
            var z = (p - t).Norm(); var x = up.Cross(z).Norm(); var y = z.Cross(x); var m = new double[16]; m[0] = x.x; m[1] = x.y; m[2] = x.z; m[4] = y.x; m[5] = y.y; m[6] = y.z; m[8] = z.x; m[9] = z.y; m[10] = z.z; m[15] = 1;
            var Rt = Qd.DeMatriz(m); var Ri = Rt.Inv(); var pi = -(p.Rot(Ri));
            var ct = cam.transform; var cr = ct.rotation; ct.position = ct.position + cr * new Vector3((float)p.x, (float)p.y, -(float)p.z); ct.rotation = cr * new Quaternion(-(float)Rt.x, -(float)Rt.y, (float)Rt.z, (float)Rt.w);
            vm.localRotation = new Quaternion(-(float)Ri.x, -(float)Ri.y, (float)Ri.z, (float)Ri.w); vm.localPosition = new Vector3((float)pi.x, (float)pi.y, -(float)pi.z);
            return (float)fov;
        }
        // nós na ordem da exportação (tools/unity-fp.js)
        public Transform[] Nos { get { return T; } }
    }
}
