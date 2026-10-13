// O mundo visual do protótipo (buildWorld): piso, paredes com a aparência de cada tipo de peça (PECAS), tetos e
// telhados, pilares, bancos, vitrais com raios de luz, portas com arcos, velas, fogueira, braseiros e lanternas,
// reagentes, poeira, luzes e céu. Tudo nas coordenadas do protótipo, sob a raiz espelhada (1, 1, -1).
// As peças paradas são fundidas por material e por região de 64 m (fundirEstaticos): poucas chamadas de desenho.
// A floresta está em MundoFloresta.cs, a catedral viva em MundoCatedral.cs e os altares em MundoAltares.cs.
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public partial class Mundo : MonoBehaviour
    {
        public static Mundo Atual;
        public Transform raiz; // espelhada
        public readonly Dictionary<string, Material> MAT = new Dictionary<string, Material>();
        readonly HashSet<Material> facetados = new HashSet<Material>();
        class Balde { public Material m; public bool sombra; public Geo g = new Geo(); }
        readonly Dictionary<string, Balde> baldes = new Dictionary<string, Balde>();
        const float PI = (float)Math.PI;

        public static Mundo Construir()
        {
            if (!MapaVisual.Carregado) { var t = Resources.Load<TextAsset>("mapa"); if (t != null) MapaVisual.Carregar(t.text); }
            var go = new GameObject("Mundo (visual do protótipo)"); var m = go.AddComponent<Mundo>(); m.raiz = go.transform; go.transform.localScale = new Vector3(1, 1, -1);
            Atual = m; m.Montar(); return m;
        }

        void Montar()
        {
            Tex.Construir(); CriarMateriais(); BuildWorld(); BuildFloresta(); BuildCatedralViva(); BuildAltars(); BuildEfeitos(); Fundir();
        }

        // ---------- ajudantes ----------
        static float vs = 31; static float vr() { vs = (float)(((double)vs * 16807) % 2147483647); return (float)((double)vs / 2147483647); }
        static float rand(float a, float b) { return UnityEngine.Random.Range(a, b); }
        public static Quaternion Q(float x, float y, float z) { return Quaternion.AngleAxis(x * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(y * Mathf.Rad2Deg, Vector3.up) * Quaternion.AngleAxis(z * Mathf.Rad2Deg, Vector3.forward); }
        static M4 TRS(float px, float py, float pz, float rx = 0, float ry = 0, float rz = 0, float sx = 1, float sy = 1, float sz = 1) { return M4.TRS(px, py, pz, rx, ry, rz, sx, sy, sz); }

        // peça parada: vai para o balde do seu material (e região)
        public void Est(Geo g, Material m, M4 t, bool sombra = true)
        {
            if (g == null || g.p.Count == 0 || m == null) return;
            if (facetados.Contains(m)) g = g.Facetar();
            var c = t.Ponto(g.p[0]); string k = m.GetInstanceID() + "|" + Mathf.FloorToInt(c.x / 64) + "|" + Mathf.FloorToInt(c.z / 64) + "|" + sombra;
            Balde b; if (!baldes.TryGetValue(k, out b)) { b = new Balde { m = m, sombra = sombra }; baldes[k] = b; }
            b.g.Juntar(g, t);
        }
        public void Est(Geo g, Material m, float x, float y, float z, bool sombra = true) { Est(g, m, M4.T(x, y, z), sombra); }
        // meshBox do protótipo: caixa entre x1..x2, y1..y2, z1..z2 com textura a cada S metros
        public void Caixa(float x1, float x2, float y1, float y2, float z1, float z2, Material m, float S = 4, bool sombra = true)
        { Est(Geo.Caixa(x2 - x1, y2 - y1, z2 - z1, S), m, M4.T((x1 + x2) / 2, (y1 + y2) / 2, (z1 + z2) / 2), sombra); }
        public void SpriteEst(float x, float y, float z, float sx, float sy, Material m) { Est(Geo.Sprite(Vector3.zero, sx, sy), m, M4.T(x, y, z), false); }

        void Fundir()
        {
            var pai = new GameObject("Estático (fundido)").transform; pai.SetParent(raiz, false);
            foreach (var b in baldes.Values)
            {
                if (b.g.p.Count == 0) continue;
                if (b.m.shader == Mat.ShaderBrilho && b.g.cor == null) b.g.Pintar(Color.white);
                var go = new GameObject(b.m.name); go.transform.SetParent(pai, false); go.isStatic = true;
                go.AddComponent<MeshFilter>().sharedMesh = b.g.ToMesh(b.m.name);
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = b.m; r.shadowCastingMode = b.sombra ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            baldes.Clear();
        }

        // objeto que se mexe (fora dos baldes)
        public static GameObject Malha(Geo g, Material m, Transform pai, string nome = "malha", bool sombra = true)
        {
            var go = new GameObject(nome); go.transform.SetParent(pai, false);
            if (m != null && m.shader == Mat.ShaderBrilho && g.cor == null) g = g.Copia().Pintar(Color.white);
            go.AddComponent<MeshFilter>().sharedMesh = g.ToMesh(nome); var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = sombra ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off; return go;
        }
        public static Transform Grupo(Transform pai, string nome = "grupo") { var t = new GameObject(nome).transform; t.SetParent(pai, false); return t; }
        public static void Pos(Component c, float x, float y, float z) { c.transform.localPosition = new Vector3(x, y, z); }
        public static void Pos(GameObject c, float x, float y, float z) { c.transform.localPosition = new Vector3(x, y, z); }
        public static void Rot(GameObject c, float x, float y, float z) { c.transform.localRotation = Q(x, y, z); }
        public static void Esc(GameObject c, float x, float y, float z) { c.transform.localScale = new Vector3(x, y, z); }
        public static GameObject Sprite(Transform pai, Material m, float sx, float sy, string nome = "sprite")
        { var go = Malha(Geo.Sprite(Vector3.zero, 1, 1), m, pai, nome, false); go.transform.localScale = new Vector3(sx, sy, 1); return go; }

        // intensidade de uma luz pontual do three (sem física: some linearmente até 'distância', com expoente 'decaimento')
        // para o URP (inverso do quadrado): as duas batem a um terço do alcance
        public static float IntensidadeUnity(float i, float dist, float dec) { float d = dist / 3f; return i * Mathf.Pow(2f / 3f, dec) * d * d; }
        public static Light LuzPonto(Transform pai, int cor, float i, float dist, float dec = 2, string nome = "luz")
        {
            var go = new GameObject(nome); go.transform.SetParent(pai, false); var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = Mat.Hex(cor); l.range = dist;
            l.intensity = IntensidadeUnity(i, dist, dec); l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel; return l;
        }

        // ---------- materiais (buildWorld) ----------
        Material M(string k) { return MAT[k]; }
        void CriarMateriais()
        {
            MAT["floor"] = Mat.Pbr(Tex.floor, 0xffffff, 1, new Vector2(24, 15), .55f);
            MAT["wall"] = Mat.Pbr(Tex.wall, 0xffffff, 1, null, .55f); MAT["wallDark"] = Mat.Pbr(Tex.wall, 0x6a6a74, 1, null, .55f);
            MAT["wood"] = Mat.Pbr(Tex.wood); MAT["marble"] = Mat.Pbr(Tex.marble);
            MAT["metal"] = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = Tex.metal.mapa, relevo = Tex.metal.relevo, metal = .85f, rugosidade = 1 });
            MAT["wax"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0xe8dcc0), rugosidade = .6f, emissao = Mat.Hex(0x3a2a10), emissaoK = .4f });
            MAT["casca"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x6a6258), mapa = Tex.wood.mapa, relevo = Tex.wood.relevo, repetir = new Vector2(2, 5) });
            MAT["estatua"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x8e8c84), relevo = Tex.floor.relevo }); facetados.Add(MAT["estatua"]);
            MAT["madeiraCorte"] = Mat.Padrao(0x8a6a48);
            MAT["rocha"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x5c5e56), relevo = Tex.floor.relevo, repetir = new Vector2(1.5f, 1.5f) }); facetados.Add(MAT["rocha"]);
            MAT["pedraVelha"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x6a6a66), relevo = Tex.floor.relevo, repetir = new Vector2(1.5f, 1.5f) }); facetados.Add(MAT["pedraVelha"]);
            MAT["lona"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x5a4c34), mapa = Tex.cloth.mapa, relevo = Tex.cloth.relevo, duplaFace = true });
            MAT["tabuas"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x6a5040), mapa = Tex.wood.mapa, relevo = Tex.wood.relevo });
            MAT["lapide"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x8a8a86), mapa = Tex.marble.mapa, relevo = Tex.marble.relevo });
            MAT["copaCarvalho"] = Mat.Padrao(0x1c241a); facetados.Add(MAT["copaCarvalho"]);
            MAT["preto"] = Mat.Brilho(Mat.Hex(0x020103), null, Mat.Mistura.Alfa, false, true, 0, true, 2000);
            MAT["sebe"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x223020), mapa = Tex.cloth.mapa, relevo = Tex.leather.relevo });
            MAT["agua"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x05080c), rugosidade = .05f, metal = .6f });
            MAT["cascaClaustro"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x2a221c), mapa = Tex.wood.mapa, relevo = Tex.wood.relevo });
            foreach (var kv in MAT) kv.Value.name = kv.Key;
        }

        // ---------- PECAS: a aparência de cada tipo de peça (a colisão continua a caixa da simulação) ----------
        void Peca(PecaV w)
        {
            float x = (w.x1 + w.x2) / 2, z = (w.z1 + w.z2) / 2, L = w.x2 - w.x1, D = w.z2 - w.z1;
            switch (w.kind)
            {
                case "arvore_f": case "espinheiro": return; // floresta
                case "ruina":
                    {
                        bool aoX = L >= D; float LL = Math.Max(L, D); int n = Math.Max(1, (int)JS.Round(LL / 1.3));
                        for (int k = 0; k < n; k++)
                        {
                            float a = (float)k / n * LL, b = (float)(k + 1) / n * LL, h = w.h * (k == 0 || k == n - 1 ? .55f + vr() * .35f : .7f + vr() * .3f);
                            if (aoX) Caixa(w.x1 + a, w.x1 + b, 0, h, w.z1, w.z2, M("pedraVelha"), 3); else Caixa(w.x1, w.x2, 0, h, w.z1 + a, w.z1 + b, M("pedraVelha"), 3);
                        }
                        for (int k = 0; k < Math.Ceiling(LL / 3); k++)
                        {
                            var g = Geo.Dodecaedro(.35f + vr() * .35f); float t = vr() * LL, lado = vr() < .5f ? -1 : 1, px, pz;
                            if (aoX) { px = w.x1 + t; pz = z + lado * (1 + vr()); } else { px = x + lado * (1 + vr()); pz = w.z1 + t; }
                            float rx = vr() * 3, ry = vr() * 3; Est(g, M("pedraVelha"), TRS(px, .25f, pz, rx, ry, 0));
                        }
                        return;
                    }
                case "pilar_ruina":
                    Est(Geo.Cilindro(.55f, .62f, w.h, 8), M("pedraVelha"), x, w.h / 2, z);
                    Caixa(x - .8f, x + .8f, 0, .6f, z - .8f, z + .8f, M("pedraVelha"), 2); Caixa(x - .75f, x + .75f, w.h - .2f, w.h + .3f, z - .75f, z + .75f, M("pedraVelha"), 2); return;
                case "estatua": Estatua(w, x, z); return;
                case "rocha":
                    {
                        float ry = vr() * 3; Est(Geo.Dodecaedro(.5f, 1), M("rocha"), TRS(x, w.h * .42f, z, 0, ry, 0, L * 1.05f, w.h * 1.5f, D * 1.05f));
                        if (vr() < .6f) { float px = x + (vr() - .5f) * L, pz = z + (vr() - .5f) * D; float a = vr(), b = vr() * 3, c = vr(); Est(Geo.Dodecaedro(.5f), M("rocha"), TRS(px, w.h * .25f, pz, a, b, c, L * .5f, w.h * .8f, D * .5f)); }
                        return;
                    }
                case "tronco": case "raiz":
                    {
                        bool aoX = L >= D; float len = Math.Max(L, D), gr = w.kind == "tronco" ? 1.3f : 1;
                        Est(Geo.Cilindro(.42f * gr, .5f * gr, len, 10), M("casca"), TRS(x, .45f * gr, z, aoX ? 0 : PI / 2, 0, aoX ? PI / 2 : 0));
                        for (int k = 0; k < 3; k++) { float t = (vr() - .5f) * len * .8f; var gh = Geo.Cilindro(.05f, .12f, 1 + vr(), 5); float rx = vr() - .5f, rz = vr() - .5f; Est(gh, M("casca"), TRS(x + (aoX ? t : 0), .8f, z + (aoX ? 0 : t), rx, 0, rz)); }
                        foreach (int e in new[] { -1, 1 }) Est(Geo.Circulo(.44f, 10), M("madeiraCorte"), TRS(x + (aoX ? e * len / 2 : 0), .45f * gr, z + (aoX ? 0 : e * len / 2), 0, aoX ? e * PI / 2 : (e > 0 ? 0 : PI), 0, gr, gr, gr));
                        return;
                    }
                case "menir":
                    {
                        float r = L / 2, rx = (vr() - .5f) * .12f, ry = vr() * 6, rz = (vr() - .5f) * .12f;
                        Est(Geo.Cilindro(r * .75f, r * 1.25f, w.h, 5, 3), M("pedraVelha"), TRS(x, w.h / 2, z, rx, ry, rz));
                        Est(Geo.Cone(r * .8f, .7f, 5), M("pedraVelha"), TRS(x, w.h + .3f, z, 0, ry, 0)); return;
                    }
                case "tenda":
                    Est(Geo.Cone(.5f, 1, 4, 1, true), M("lona"), TRS(x, w.h / 2, z, 0, PI / 4, 0, L * 1.35f, w.h, D * 1.35f));
                    Est(Geo.Cilindro(.05f, .05f, w.h + .5f, 5), M("wood"), x, (w.h + .5f) / 2, z); return;
                case "caixote": Caixa(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("wood"), 1); return;
                case "carvalho":
                    {
                        float r = Math.Min(L, D) / 2; Est(Geo.Cilindro(r * .8f, r * 1.25f, w.h, 14, 4), M("casca"), x, w.h / 2, z);
                        Est(Geo.Circulo(1, 16), M("preto"), TRS(x, 1.7f, w.z1 - .02f, 0, PI, 0, .9f, 1.5f, 1), false);
                        for (int k = 0; k < 7; k++)
                        {
                            float a = k / 7f * PI * 2 + .3f, len = 5 + vr() * 3, gy = w.h * .75f + vr() * 2, gz = .9f + vr() * .4f;
                            Est(Geo.Cilindro(.25f, .7f, len, 7), M("casca"), TRS(x, gy, z, 0, a, gz) * M4.T(0, len / 2, 0));
                        }
                        for (int k = 0; k < 9; k++) { float a = vr() * 6.28f, rr = 3 + vr() * 6, raio = 4 + vr() * 2.5f, py = w.h + 2 + vr() * 4; Est(Geo.Icosaedro(raio, 1), M("copaCarvalho"), TRS(x + Mathf.Cos(a) * rr, py, z + Mathf.Sin(a) * rr, 0, 0, 0, 1, .7f, 1)); }
                        return;
                    }
                case "madeira": Caixa(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("tabuas"), 2); return;
                case "lapide":
                    {
                        float rx = (vr() - .5f) * .2f, ry = (vr() - .5f) * .25f, rz = (vr() - .5f) * .15f; var G = TRS(x, 0, z, rx, ry, rz);
                        Est(Geo.Caixa(L * .8f, w.h * .75f, D, 1), M("lapide"), G * M4.T(0, w.h * .375f, 0));
                        Est(Geo.Cilindro(L * .4f, L * .4f, D, 12, 1, false, 0, PI), M("lapide"), G * TRS(0, w.h * .75f, 0, PI / 2, 0, PI / 2));
                        Est(Geo.Caixa(L * 1.1f, .16f, .9f, 1), M("lapide"), G * M4.T(0, .08f, -.55f)); return;
                    }
                case "mausoleu":
                    Caixa(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("wallDark"), 3);
                    Est(Geo.Cone(L * .78f, 2.4f, 4), M("wallDark"), TRS(x, w.h + 1.2f, z, 0, PI / 4, 0));
                    Est(Geo.Plano(1.4f, 2.4f), M("preto"), TRS(x, 1.2f, w.z1 - .02f, 0, PI, 0), false);
                    Est(Geo.Caixa(.12f, 1, .12f), M("metal"), x, w.h + 2.9f, z); Est(Geo.Caixa(.5f, .1f, .1f), M("metal"), x, w.h + 3.1f, z); return;
                case "marco":
                    Est(Geo.Caixa(.25f, w.h + .8f, .25f), M("wood"), x, (w.h + .8f) / 2, z);
                    foreach (var q in new[] { new Vector3(.4f, 2.6f, .5f), new Vector3(-.3f, 2.2f, -1.1f), new Vector3(.2f, 1.8f, 2) }) Est(Geo.Caixa(1.4f, .28f, .06f), M("wood"), TRS(x + Mathf.Cos(q.z) * q.x, q.y, z + Mathf.Sin(q.z) * q.x, 0, q.z, 0));
                    Est(Geo.Dodecaedro(.7f), M("rocha"), TRS(x, .3f, z, 0, 0, 0, 1, .5f, 1)); return;
                case "santuario":
                    {
                        Caixa(w.x1, w.x2, 0, w.h * .55f, w.z1, w.z2, M("pedraVelha"), 2);
                        float rz = (vr() - .5f) * .2f; Est(Geo.Caixa(1.1f, 1.1f, .5f, 2), M("pedraVelha"), TRS(x, w.h * .55f + .55f, z + .1f, 0, 0, rz));
                        Est(Geo.Cone(.9f, .6f, 4), M("wallDark"), TRS(x, w.h * .55f + 1.35f, z + .1f, 0, PI / 4, 0));
                        Est(Geo.Cilindro(.05f, .05f, .25f, 6), M("wax"), x, w.h * .55f + .2f, z - .1f); return;
                    }
                case "limite": Caixa(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("wall")); Caixa(w.x1 - .15f, w.x2 + .15f, w.h, w.h + .3f, w.z1 - .15f, w.z2 + .15f, M("wallDark"), 4, false); return;
                case "torre":
                    {
                        float H = 30; Caixa(w.x1, w.x2, 0, H, w.z1, w.z2, M("wall")); Caixa(w.x1 - .3f, w.x2 + .3f, H, H + .6f, w.z1 - .3f, w.z2 + .3f, M("wallDark"));
                        foreach (var q in new[] { new Vector3(0, -1, 0), new Vector3(0, 1, PI), new Vector3(-1, 0, PI / 2), new Vector3(1, 0, -PI / 2) })
                            Est(Geo.Plano(1.6f, 3.4f), M("preto"), TRS(x + q.x * (L / 2 + .02f), H - 3, z + q.y * (D / 2 + .02f), 0, q.z + PI, 0), false);
                        Est(Geo.Cone(L * .72f, 12, 4), M("wallDark"), TRS(x, H + 6.6f, z, 0, PI / 4, 0)); return;
                    }
                case "escombro": Escombro(w.x1, w.x2, w.z1, w.z2, w.h); return;
                case "coluna":
                    Est(Geo.Cilindro(.66f, .78f, w.h, 20), M("wall"), x, w.h / 2, z); Est(Geo.Cilindro(.95f, 1.05f, .5f, 20), M("marble"), x, .25f, z);
                    Est(Geo.Cone(.68f, .7f, 9), M("wall"), TRS(x + .1f, w.h + .2f, z, 0, 0, .5f, 1, .5f, 1));
                    Escombro(x - 1.6f, x + 1.6f, z - 1.6f, z + 1.6f, .6f); return;
                case "coluna_caida":
                    {
                        bool aoX = L >= D; float len = Math.Max(L, D);
                        Est(Geo.Cilindro(.64f, .72f, len * .94f, 20), M("wall"), TRS(x, .66f, z, aoX ? 0 : PI / 2, .04f, aoX ? PI / 2 : 0));
                        Est(Geo.Caixa(1.6f, 1.3f, 1.6f), M("wallDark"), TRS(aoX ? w.x2 - .6f : x, .65f, aoX ? z : w.z2 - .6f, .2f, .3f, .1f));
                        if (aoX) Escombro(w.x1 - 1, w.x1 + 1.5f, z - 1.4f, z + 1.4f, .5f); else Escombro(w.x1 - 1.4f, w.x2 + 1.4f, w.z1 - 1, w.z1 + 1.5f, .5f);
                        return;
                    }
                case "sebe":
                    {
                        Est(Geo.Caixa(L, w.h * .85f, D), M("sebe"), x, w.h * .425f, z);
                        float LL = Math.Max(L, D); int n = (int)Math.Ceiling(LL / .9f);
                        for (int k = 0; k < n; k++)
                        {
                            float t = (k + .5f) / n, r = .45f + vr() * .35f, py = w.h * .8f + vr() * .2f;
                            Est(Geo.Esfera(r, 10, 8), M("sebe"), Mathf.Lerp(w.x1 + .4f, w.x2 - .4f, L >= D ? t : .5f), py, Mathf.Lerp(w.z1 + .4f, w.z2 - .4f, L < D ? t : .5f));
                        }
                        return;
                    }
                case "poco":
                    {
                        float r = Math.Min(L, D) / 2;
                        Est(Geo.Cilindro(r, r * 1.05f, w.h, 28, 1, true), M("wall"), x, w.h / 2, z);
                        Est(Geo.Toro(r - .15f, .22f, 10, 32), M("marble"), TRS(x, w.h, z, PI / 2, 0, 0));
                        Est(Geo.Circulo(r - .3f, 28), M("agua"), TRS(x, w.h - .35f, z, -PI / 2, 0, 0), false);
                        foreach (int sx in new[] { -1, 1 }) Est(Geo.Caixa(.25f, 3.2f, .25f), M("wood"), x + sx * (r - .2f), 1.6f, z);
                        Est(Geo.Cilindro(.1f, .1f, r * 2, 10), M("wood"), TRS(x, 3, z, 0, 0, PI / 2)); Est(Geo.Cilindro(.22f, .18f, .35f, 12), M("wood"), x + .3f, 2.2f, z); return;
                    }
                case "arvore":
                    {
                        Est(Geo.Cilindro(.22f, .5f, w.h, 12), M("cascaClaustro"), TRS(x, w.h / 2, z, 0, 0, .06f));
                        for (int k = 0; k < 5; k++) { float len = 1.8f + vr(), ang = .7f + vr() * .5f, giro = k * 1.3f + vr(); Galho(x, w.h * (.45f + k * .1f), z, len, .14f, ang, giro, 2); }
                        return;
                    }
                case "ossos": Ossos(w, x, z); return;
            }
            var mat = w.kind == "tumba" ? M("marble") : w.kind == "cripta" ? M("wallDark") : w.kind == "biombo" ? M("wood") : M("wall");
            Caixa(w.x1, w.x2, 0, w.h, w.z1, w.z2, mat, w.kind == "tumba" ? 2 : 4);
            if (w.h > 2.2f && w.kind != "biombo") Caixa(w.x1 - .1f, w.x2 + .1f, w.h, w.h + .35f, w.z1 - .1f, w.z2 + .1f, M("wallDark"), 4, false);
        }

        void Escombro(float x1, float x2, float z1, float z2, float h)
        {
            float L = x2 - x1, D = z2 - z1; int n = Math.Max(3, (int)JS.Round(L * D / 1.1f));
            for (int k = 0; k < n; k++)
            {
                float r = .35f + vr() * .55f; var g = Geo.Dodecaedro(r);
                float px = x1 + r * .6f + vr() * (L - r * 1.2f), py = Math.Min(h, r * .7f + vr() * h * .6f), pz = z1 + r * .6f + vr() * (D - r * 1.2f);
                float a = vr() * 3, b = vr() * 3, c = vr() * 3, sy = .55f + vr() * .5f, sz = 1 + vr() * .4f;
                Est(g, M("wall"), TRS(px, py, pz, a, b, c, 1, sy, sz));
            }
        }

        void Galho(float px, float py, float pz, float len, float r, float ang, float giro, int prof)
        {
            var q = TRS(px, py, pz, 0, giro, ang);
            Est(Geo.Cilindro(r * .4f, r, len, 8), M("cascaClaustro"), q * M4.T(0, len / 2, 0));
            if (prof > 0) { var ponta = q.Ponto(new Vector3(0, len, 0)); for (int k = 0; k < 2; k++) { float a2 = ang + (k == 1 ? .5f : -.4f) + vr() * .3f, g2 = giro + vr() * 1.5f; Galho(ponta.x, ponta.y, ponta.z, len * .6f, r * .5f, a2, g2, prof - 1); } }
        }

        void Estatua(PecaV w, float x, float z)
        {
            var MT = M("estatua"); int pose = w.pose; bool ajoelha = pose == 1 || pose == 3; float alt = ajoelha ? .62f : 1;
            float gx = pose == 0 ? .12f : 0, gz = pose == 3 ? .1f : 0; var G = TRS(x, 0, z, gx, w.rot, gz);
            Est(Geo.Torno(new float[,] { { .42f, 0 }, { .38f, .3f }, { .3f, .8f * alt + .2f }, { .25f, 1.25f * alt + .1f }, { .14f, 1.42f * alt + .1f }, { 0, 1.45f * alt + .1f } }, 9), MT, G);
            float cabRx = pose == 0 ? .5f : pose == 1 ? .7f : pose == 2 ? -.35f : .55f;
            Est(Geo.Esfera(.17f, 8, 6), MT, G * TRS(0, 1.62f * alt + .1f, -.03f, cabRx, 0, 0, 1, 1.15f, 1));
            Est(Geo.Cone(.22f, .42f, 8), MT, G * TRS(0, 1.72f * alt + .12f, .03f, -.25f, 0, 0));
            Action<float, float, float> braco = (sx, rx, rz) => Est(Geo.Cilindro(.065f, .08f, .62f, 6), MT, G * TRS(sx * .26f, 1.3f * alt + .1f, 0, rx, 0, rz) * M4.T(0, -.31f, 0));
            if (pose == 0) { braco(-1, -.9f, .15f); braco(1, -.9f, -.15f); }
            else if (pose == 1) { braco(-1, -1.3f, .1f); braco(1, -1.3f, -.1f); }
            else if (pose == 2) { braco(-1, .2f, .3f); braco(1, -2.9f, -.2f); }
            else { braco(-1, -2.4f, .55f); braco(1, -2.4f, -.55f); }
            for (int k = 0; k < 3; k++) { var g = Geo.Dodecaedro(.12f + vr() * .12f); float px = (vr() - .5f) * 1.2f, pz = (vr() - .5f) * 1.2f; Est(g, MT, x + px, .08f, z + pz); }
        }

        void Ossos(PecaV w, float x, float z)
        {
            var tons = new[] { 0xa89878, 0xbcae8e, 0x8e8066, 0xc8bb9c };
            var mats = new Material[4]; for (int i = 0; i < 4; i++) { string k = "osso" + i; if (!MAT.ContainsKey(k)) { MAT[k] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(tons[i]), rugosidade = .7f, relevo = Tex.wall.relevo }); MAT[k].name = k; } mats[i] = MAT[k]; }
            Caixa(w.x1 + .35f, w.x2 - .35f, 0, w.h * .92f, w.z1 + .35f, w.z2 - .35f, M("wallDark"), 3);
            var cranio = Geo.Esfera(.2f, 7, 5); var olho = Geo.Esfera(.05f, 4, 3); var longo = Geo.Cilindro(.045f, .05f, 1, 8);
            int camada = 0;
            for (float y = .24f; y < w.h * .95f; y += .44f, camada++)
            {
                float desl = (camada % 2) * .25f, falha = .12f + .1f * Mathf.Sin(camada * 2.1f);
                var lados = new object[][] { new object[] { "z", w.z1, w.x1, w.x2 }, new object[] { "z", w.z2, w.x1, w.x2 }, new object[] { "x", w.x1, w.z1, w.z2 }, new object[] { "x", w.x2, w.z1, w.z2 } };
                foreach (var L in lados)
                {
                    string lado = (string)L[0]; float fixo = (float)L[1], de = (float)L[2], ate = (float)L[3];
                    for (float t = de + .28f + desl; t < ate - .25f; t += .5f)
                    {
                        if (vr() < falha) continue;
                        float sc = .8f + vr() * .45f; var mt = mats[(int)Math.Floor(vr() * 4)]; float px = lado == "z" ? t : fixo, pz = lado == "z" ? fixo : t;
                        float cx = px + (vr() - .5f) * .1f, cy = y + (vr() - .5f) * .08f, cz = pz + (vr() - .5f) * .1f, a = (vr() - .5f) * .5f, b = (vr() - .5f) * .9f, c = (vr() - .5f) * .4f;
                        Est(cranio, mt, TRS(cx, cy, cz, a, b, c, sc, sc * .88f, sc * 1.1f));
                        if (vr() < .6f)
                        {
                            float nx = lado == "x" ? Mathf.Sign(px - x) : 0, nz = lado == "z" ? Mathf.Sign(pz - z) : 0;
                            foreach (float o in new[] { -.07f, .07f }) Est(olho, M("preto"), px + nx * .17f * sc + (lado == "z" ? o * sc : 0), y + .03f, pz + nz * .17f * sc + (lado == "x" ? o * sc : 0), false);
                        }
                    }
                    if (camada % 2 == 1)
                    {
                        var mt = mats[(int)Math.Floor(vr() * 4)]; float len = (ate - de) * (.5f + vr() * .4f);
                        float bx = lado == "z" ? (de + ate) / 2 + (vr() - .5f) : fixo, bz = lado == "z" ? fixo : (de + ate) / 2 + (vr() - .5f); float rz = lado == "z" ? PI / 2 + (vr() - .5f) * .15f : (vr() - .5f) * .15f;
                        Est(longo, mt, TRS(bx, y + .24f, bz, lado == "x" ? PI / 2 : 0, 0, rz, 1, len, 1));
                    }
                }
            }
            float mn = Math.Min(w.x2 - w.x1, w.z2 - w.z1);
            for (int k = 0; k < 14; k++) { var mt = mats[(int)Math.Floor(vr() * 4)]; float a = vr() * 6.28f, r = vr() * mn * .4f, py = w.h * .92f + vr() * .35f, ra = vr() * 3, rb = vr() * 3, rc = vr() * 3; Est(cranio, mt, TRS(x + Mathf.Cos(a) * r, py, z + Mathf.Sin(a) * r, ra, rb, rc)); }
            for (int k = 0; k < 9; k++)
            {
                var mt = mats[(int)Math.Floor(vr() * 4)]; int lado = (int)Math.Floor(vr() * 4); float t = vr(), px, pz;
                if (lado < 2) { px = Mathf.Lerp(w.x1, w.x2, t); pz = lado == 0 ? w.z1 - .5f - vr() * .8f : w.z2 + .5f + vr() * .8f; }
                else { px = lado == 2 ? w.x1 - .5f - vr() * .8f : w.x2 + .5f + vr() * .8f; pz = Mathf.Lerp(w.z1, w.z2, t); }
                float ra = vr() * 3, rb = vr() * 3, rc = vr() * 3; Est(cranio, mt, TRS(px, .17f, pz, ra, rb, rc));
            }
        }

        // ---------- buildWorld ----------
        public readonly List<Fonte> FONTES = new List<Fonte>();
        public class Fonte { public float x, y, z, i, alc; public int cor; }
        public Material winMat, roseMat, winFora; public Color winCor, roseCor, winForaCor;
        public class Reag { public GameObject jar, halo; public float x; }
        public readonly List<Reag> reagentes = new List<Reag>();

        void BuildWorld()
        {
            var CT = MapaVisual.CATEDRAL; var cl0 = MapaVisual.CLAUSTRO;
            Est(Geo.Plano(CT.width, CT.height), M("floor"), TRS(CT.center.x, 0, CT.center.y, -PI / 2, 0, 0));
            var tetoMat = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x2a2a32), mapa = Tex.wall.mapa, relevo = Tex.wall.relevo, duplaFace = true }); tetoMat.name = "teto";
            Action<float, float, float, float> teto = (x1, x2, z1, z2) => Est(Geo.Plano(x2 - x1, z2 - z1), tetoMat, TRS((x1 + x2) / 2, 15, (z1 + z2) / 2, PI / 2, 0, 0));
            teto(CT.xMin - 1, CT.xMax + 1, CT.yMin - 1, cl0.yMin); teto(CT.xMin - 1, CT.xMax + 1, cl0.yMax, CT.yMax + 1); teto(CT.xMin - 1, cl0.xMin, cl0.yMin, cl0.yMax); teto(cl0.xMax, CT.xMax + 1, cl0.yMin, cl0.yMax);
            // telhados de duas águas nas quatro alas
            var telha = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x3a3438), mapa = Tex.wall.mapa, relevo = Tex.wall.relevo, duplaFace = true }); telha.name = "telha";
            var empena = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = Tex.wall.mapa, relevo = Tex.wall.relevo, duplaFace = true }); empena.name = "empena";
            Action<float, float, float, float, bool> telhado = (x1, x2, z1, z2, aoLongoX) =>
            {
                float larg = (aoLongoX ? z2 - z1 : x2 - x1) / 2, comp = aoLongoX ? x2 - x1 : z2 - z1, alt = larg * .62f, lado = Mathf.Sqrt(alt * alt + larg * larg), inc = Mathf.Atan2(alt, larg);
                var G = TRS((x1 + x2) / 2, 15, (z1 + z2) / 2, 0, aoLongoX ? 0 : PI / 2, 0);
                foreach (int sz in new[] { -1, 1 }) Est(Geo.Plano(comp, lado), telha, G * TRS(0, alt / 2, sz * larg / 2, -PI / 2 + sz * inc, 0, 0));
                var tri = new List<Vector2> { new Vector2(-larg, 0), new Vector2(larg, 0), new Vector2(0, alt) };
                foreach (int sx in new[] { -1, 1 }) Est(Geo.Forma(tri), empena, G * TRS(sx * comp / 2, 0, 0, 0, PI / 2, 0));
            };
            telhado(CT.xMin - 1, CT.xMax + 1, CT.yMin - 1, cl0.yMin, true); telhado(CT.xMin - 1, CT.xMax + 1, cl0.yMax, CT.yMax + 1, true);
            telhado(CT.xMin - 1, cl0.xMin, cl0.yMin, cl0.yMax, false); telhado(cl0.xMax, CT.xMax + 1, cl0.yMin, cl0.yMax, false);
            // piso do Claustro
            var piso = Mat.Pbr(Tex.floor, 0x6a6256, 1, new Vector2(14, 7)); piso.name = "pisoClaustro";
            Est(Geo.Plano(cl0.width, cl0.height), piso, TRS(cl0.center.x, .02f, cl0.center.y, -PI / 2, 0, 0));
            foreach (var w in MapaVisual.WALLS) Peca(w);
            // pilares
            var pg = Geo.Cilindro(.72f, .85f, 15, 14).EscalarUV(1.2f, 3.6f); var baseG = Geo.Cilindro(1.05f, 1.15f, .7f, 14); var capG = Geo.Cilindro(1.15f, .8f, .8f, 14);
            foreach (var p in MapaVisual.PILLARS) { Est(pg, M("wall"), p.x, 7.5f, p.y); Est(baseG, M("marble"), p.x, .35f, p.y); Est(capG, M("wallDark"), p.x, 12.9f, p.y); }
            foreach (var P in MapaVisual.PEWS)
                for (float x = P.x + .3f; x < P.y - .5f; x += 3.1f) foreach (float zz in new[] { P.z + .4f, P.z + 2.1f })
                    {
                        if (zz > P.w - .5f) continue;
                        Caixa(x, x + 2.7f, .42f, .52f, zz, zz + .55f, M("wood"), 2); Caixa(x, x + 2.7f, .52f, 1, zz + .5f, zz + .62f, M("wood"), 2);
                        Caixa(x, x + .1f, 0, .95f, zz, zz + .6f, M("wood"), 2); Caixa(x + 2.6f, x + 2.7f, 0, .95f, zz, zz + .6f, M("wood"), 2);
                    }
            // vitrais e raios de luz
            winCor = Mat.Lin(new Color(1.35f, 1.35f, 1.6f)); roseCor = Mat.Lin(new Color(1.5f, 1.4f, 1.6f)); winForaCor = Mat.Lin(new Color(.55f, .5f, .7f));
            winMat = Mat.Brilho(winCor, Tex.Vitral(false), Mat.Mistura.Alfa, false, false, .1f, true, 2460); winMat.name = "vitral";
            roseMat = Mat.Brilho(roseCor, Tex.Vitral(true), Mat.Mistura.Alfa, false, false, .1f, true, 2460); roseMat.name = "rosacea";
            winFora = Mat.Brilho(winForaCor, Tex.Vitral(false), Mat.Mistura.Alfa, false, false, .1f, true, 2460); winFora.name = "vitralFora";
            var shaftMat = Mat.Brilho(Mat.Hex(0x8fa6d8, .085f), Tex.RaioLuz(), Mat.Mistura.Aditiva, false, false); shaftMat.name = "raioVitral";
            var janelas = new List<float>(); for (float x = CT.xMin + 14; x <= CT.xMax - 14; x += 16) janelas.Add(x);
            Func<float, int, bool> noVao = (x, sz) => { foreach (var p in MapaVisual.PORTAIS) if (p.eixo == "x" && Math.Sign(p.fixo) == sz && x > p.a - 3.5f && x < p.b + 3.5f) return true; return sz > 0 && Math.Abs(x) < 17; };
            foreach (float x in janelas) foreach (int sz in new[] { -1, 1 })
                {
                    if (noVao(x, sz)) continue;
                    Est(Geo.Plano(3, 7), winMat, TRS(x, 8, sz * (CT.yMax - .06f), 0, sz > 0 ? PI : 0, 0), false);
                    Est(Geo.Plano(3, 7), winFora, TRS(x, 8, sz * (CT.yMax + 1.06f), 0, sz < 0 ? PI : 0, 0), false);
                    var v = new Vector3(0, 7.5f, -sz * 11).normalized; var q = Quaternion.FromToRotation(new Vector3(0, -1, 0), -v);
                    var G = M4Q(new Vector3(x, 8 - 3.75f, sz * (CT.yMax - 5.6f)), q);
                    Est(Geo.Plano(3, 13.8f), shaftMat, G, false); Est(Geo.Plano(3, 13.8f), shaftMat, G * M4.RY(PI / 2), false);
                }
            foreach (int sx in new[] { -1, 1 }) foreach (int lado in new[] { 0, 1 }) Est(Geo.Circulo(3.6f, 40), lado == 1 ? winFora : roseMat, TRS(sx * (CT.xMax + (lado == 1 ? 1.06f : -.06f)), 10.2f, 0, 0, (lado == 1 ? 1 : -1) * sx * PI / 2, 0), false);
            // portas: lintel e batentes; as brechas ficam com a borda quebrada
            foreach (var P in MapaVisual.PORTAIS)
            {
                bool ax = P.eixo == "x"; float f = P.fixo;
                Action<float, float, float, float, float, float, Material> box = (a, b, y1, y2, e1, e2, m) => { if (ax) Caixa(a, b, y1, y2, f + e1, f + e2, m); else Caixa(f + e1, f + e2, y1, y2, a, b, m); };
                if (P.brecha) { for (int k = 0; k < 4; k++) { float a = P.a + (P.b - P.a) * k / 4, b = a + (P.b - P.a) / 4; box(a, b, 8 + vr() * 3, 14, -.5f, .5f, M("wall")); } continue; }
                box(P.a, P.b, 5.6f, 14, -.5f, .5f, M("wall")); box(P.a - .5f, P.b + .5f, 5.3f, 5.7f, -.8f, .8f, M("wallDark"));
                foreach (float e in new[] { P.a - .45f, P.b - .05f }) box(e, e + .5f, 0, 5.6f, -.75f, .75f, M("wallDark"));
                float c = (P.a + P.b) / 2; Est(Geo.Toro((P.b - P.a) / 2, .28f, 8, 20, PI), M("wallDark"), ax ? TRS(c, 5.6f, f) : TRS(f, 5.6f, c, 0, PI / 2, 0));
            }
            // velas, fogueira, braseiros e lanternas
            var chama = Mat.Brilho(Mat.Hex(0xffc070), Tex.Chama(), Mat.Mistura.Aditiva, true); chama.name = "chama";
            var chamaVerm = Mat.Brilho(Mat.Hex(0xff5a4a), Tex.Chama(), Mat.Mistura.Aditiva, true); chamaVerm.name = "chamaVermelha";
            var vidro = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0xffc070), rugosidade = .4f, emissao = Mat.Hex(0xff9030), emissaoK = 1.6f }); vidro.name = "lanternaVidro";
            var metalDupla = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = Tex.metal.mapa, relevo = Tex.metal.relevo, metal = .85f, duplaFace = true }); metalDupla.name = "metalDupla";
            foreach (var cd in MapaVisual.CANDLES)
            {
                float x = cd.x, z = cd.z; var fonte = new Fonte { x = x, z = z, y = 2.1f, cor = 0xff8a36, i = 1.6f, alc = 14 };
                if (cd.tipo == "fogueira")
                {
                    for (int k = 0; k < 6; k++) Est(Geo.Cilindro(.09f, .11f, 1.3f, 6), M("wood"), TRS(x, .2f, z, PI / 2 - .35f, k / 6f * PI * 2, 0));
                    for (int k = 0; k < 9; k++) { float a = k / 9f * PI * 2; Est(Geo.Dodecaedro(.22f), M("wall"), x + Mathf.Cos(a) * .85f, .1f, z + Mathf.Sin(a) * .85f); }
                    for (int k = 0; k < 4; k++) SpriteEst(x + rand(-.25f, .25f), .7f + k * .12f, z + rand(-.25f, .25f), .7f, 1.1f, chama);
                    fonte.y = 1.4f; fonte.cor = 0xff7a2a; fonte.i = 2.6f; fonte.alc = 20;
                }
                else if (cd.tipo == "braseiro")
                {
                    for (int k = 0; k < 3; k++) { float a = k / 3f * PI * 2; Est(Geo.Cilindro(.04f, .05f, 1.3f, 5), M("metal"), TRS(x + Mathf.Cos(a) * .3f, .62f, z + Mathf.Sin(a) * .3f, Mathf.Sin(a) * .25f, 0, -Mathf.Cos(a) * .25f)); }
                    Est(Geo.Cilindro(.55f, .3f, .4f, 10, 1, true), metalDupla, x, 1.3f, z);
                    for (int k = 0; k < 3; k++) SpriteEst(x + rand(-.2f, .2f), 1.75f, z + rand(-.2f, .2f), .55f, .9f, chamaVerm);
                    fonte.y = 2; fonte.cor = 0xff4a3a; fonte.i = 2.2f; fonte.alc = 16;
                }
                else if (cd.tipo == "lanterna")
                {
                    Est(Geo.Caixa(.14f, 2.4f, .14f), M("wood"), x, 1.2f, z); Est(Geo.Caixa(.6f, .08f, .08f), M("wood"), x + .25f, 2.35f, z);
                    Est(Geo.Cilindro(.14f, .18f, .34f, 6), vidro, x + .5f, 2.1f, z); SpriteEst(x + .5f, 2.1f, z, .5f, .5f, chama);
                    fonte.x = x + .5f; fonte.y = 2.1f; fonte.cor = 0xffb060; fonte.i = 1.7f; fonte.alc = 13;
                }
                else
                {
                    Est(Geo.Cilindro(.06f, .08f, 1.5f, 6), M("metal"), x, .75f, z); Est(Geo.Cilindro(.55f, .35f, .08f, 12), M("metal"), x, 1.5f, z);
                    for (int i = 0; i < 5; i++) { float a = i / 5f * PI * 2, cx = x + Mathf.Cos(a) * .32f, cz = z + Mathf.Sin(a) * .32f, h = rand(.18f, .38f); Est(Geo.Cilindro(.045f, .05f, h, 6), M("wax"), cx, 1.54f + h / 2, cz); SpriteEst(cx, 1.6f + h, cz, .16f, .26f, chama); }
                }
                FONTES.Add(fonte);
            }
            // reagentes: pedestal parado; o frasco e o halo aparecem e somem
            var jarMat = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x2a1016), rugosidade = .3f, metal = .2f, emissao = Mat.Hex(0xb0142e), emissaoK = 1.4f }); jarMat.name = "frasco";
            var haloR = Mat.Brilho(Mat.Hex(0xff3355, .55f), Tex.Brilho(), Mat.Mistura.Aditiva, true); haloR.name = "haloReagente";
            var jarG = Geo.Cilindro(.13f, .16f, .34f, 10);
            foreach (var R in Mapa.REAG)
            {
                float x = (float)R.x, z = (float)R.z; Est(Geo.Caixa(.7f, .9f, .7f, 1.5f), M("wall"), x, .45f, z);
                var g = Grupo(raiz, "Reagente"); g.localPosition = new Vector3(x, 0, z);
                var jar = Malha(jarG, jarMat, g, "frasco"); Pos(jar, 0, 1.08f, 0);
                var halo = Sprite(g, haloR, 1.1f, 1.1f, "halo"); Pos(halo, 0, 1.1f, 0);
                reagentes.Add(new Reag { jar = jar, halo = halo, x = x });
            }
            // poeira no ar
            { int N = Qualidade.nivel == 0 ? 500 : Qualidade.nivel <= 1 ? 1200 : 2400; var dust = Mat.Brilho(Mat.Hex(0xcdbb96, .5f), Tex.Brilho(), Mat.Mistura.Aditiva, true); dust.name = "poeira"; var rnd = new System.Random(9);
                for (int i = 0; i < N; i++) SpriteEst((float)(rnd.NextDouble() * 2 - 1) * MapaVisual.HX, .3f + (float)rnd.NextDouble() * 11.7f, (float)(rnd.NextDouble() * 2 - 1) * MapaVisual.HZ, .05f, .05f, dust); }
        }
        public static M4 M4Q(Vector3 p, Quaternion q)
        {
            float x = q.x, y = q.y, z = q.z, w = q.w;
            var r = new M4 { a = 1 - 2 * (y * y + z * z), b = 2 * (x * y - z * w), c = 2 * (x * z + y * w), e = 2 * (x * y + z * w), f = 1 - 2 * (x * x + z * z), g = 2 * (y * z - x * w), i = 2 * (x * z - y * w), j = 2 * (y * z + x * w), k = 1 - 2 * (x * x + y * y) };
            r.d = p.x; r.h = p.y; r.l = p.z; return r;
        }
    }
}
