// O mundo visual do protótipo (buildWorld): paredes com a aparência de cada tipo de peça (PECAS), bancos, velas,
// fogueira, braseiros e lanternas, reagentes, poeira, luzes e céu. A catedral da v6, o desfiladeiro e as pontes ficam em
// MundoV6.cs (client/mundo.js). Tudo nas coordenadas do protótipo, sob a raiz espelhada (1, 1, -1).
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

        public static Mundo Construir(string mapa = null)
        {
            // a planta visual do mapa pedido (sem pedido, a que estiver carregada; a catedral de saída)
            if (mapa != null) MapaVisual.Usar(mapa); else if (!MapaVisual.Carregado) MapaVisual.Usar("catedral");
            var go = new GameObject("Mundo (visual do protótipo)"); var m = go.AddComponent<Mundo>(); m.raiz = go.transform; go.transform.localScale = new Vector3(1, 1, -1);
            Atual = m; m.Montar(); return m;
        }

        void Montar()
        {
            // o asilo (MundoAsilo.cs) não tem a mata nem a catedral viva, como no boot() do navegador
            bool asilo = MapaVisual.Asilo;
            // altares, reagentes, mercadores e zonas vêm do mapa da simulação (Mapa): ele passa a ser o mesmo do mundo montado.
            // Sem isso, voltar do Asilo para a catedral no título montava a catedral com os altares e reagentes do Asilo.
            Mapa.Usar(MapaVisual.ID);
            Tex.Construir(); CriarMateriais(); BuildWorld(); if (!asilo) { BuildFloresta(); BuildCatedralViva(); } BuildAltars(); BuildEfeitos(); Fundir();
        }

        // ---------- ajudantes ----------
        static float vs = 31; static float vr() { vs = (float)(((double)vs * 16807) % 2147483647); return (float)((double)vs / 2147483647); }
        static float rand(float a, float b) { return UnityEngine.Random.Range(a, b); }
        public static Quaternion Q(float x, float y, float z) { return Quaternion.AngleAxis(x * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(y * Mathf.Rad2Deg, Vector3.up) * Quaternion.AngleAxis(z * Mathf.Rad2Deg, Vector3.forward); }
        static M4 TRS(float px, float py, float pz, float rx = 0, float ry = 0, float rz = 0, float sx = 1, float sy = 1, float sz = 1) { return M4.TRS(px, py, pz, rx, ry, rz, sx, sy, sz); }

        // peça parada: vai para o balde do seu material (e região)
        readonly System.Collections.Generic.Dictionary<Material, int> idMat = new System.Collections.Generic.Dictionary<Material, int>();
        public void Est(Geo g, Material m, M4 t, bool sombra = true)
        {
            if (g == null || g.p.Count == 0 || m == null) return;
            if (facetados.Contains(m)) g = g.Facetar();
            // número próprio de cada material (GetInstanceID virou erro no Unity 6.x)
            int idm; if (!idMat.TryGetValue(m, out idm)) { idm = idMat.Count; idMat[m] = idm; }
            var c = t.Ponto(g.p[0]); string k = idm + "|" + Mathf.FloorToInt(c.x / 64) + "|" + Mathf.FloorToInt(c.z / 64) + "|" + sombra;
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
            // v6.4b: fotos novas (casca, pedra velha, lápide); sem elas, o que era antes
            var fCasca = Pbr.Foto("casca"); var fPedra = Pbr.Foto("pedraVelha"); var fLapide = Pbr.Foto("lapide");
            MAT["casca"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x6a6258), mapa = (fCasca ?? Tex.wood).mapa, relevo = (fCasca ?? Tex.wood).relevo, repetir = fCasca != null ? new Vector2(1, 3) : new Vector2(2, 5) });
            MAT["estatua"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x8e8c84), relevo = Tex.floor.relevo }); facetados.Add(MAT["estatua"]);
            MAT["madeiraCorte"] = Mat.Padrao(0x8a6a48);
            // v6: rocha de verdade (Tex.rocha), sem as juntas das lajes do piso que antes apareciam nas pedras
            MAT["rocha"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x5c5e56), relevo = Tex.rocha.relevo, repetir = new Vector2(1.2f, 1.2f) }); facetados.Add(MAT["rocha"]);
            // a pedra velha com foto (em cinza, média 200): a cor sobe na mesma conta (255/200) para o brilho não mudar
            MAT["pedraVelha"] = fPedra != null ? Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x878782), mapa = fPedra.mapa, relevo = fPedra.relevo })
                : Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x6a6a66), relevo = Tex.rocha.relevo, repetir = new Vector2(1.2f, 1.2f) }); facetados.Add(MAT["pedraVelha"]);
            MAT["lona"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x5a4c34), mapa = Tex.cloth.mapa, relevo = Tex.cloth.relevo, duplaFace = true });
            MAT["tabuas"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x6a5040), mapa = Tex.wood.mapa, relevo = Tex.wood.relevo });
            MAT["lapide"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x8a8a86), mapa = (fLapide ?? Tex.marble).mapa, relevo = (fLapide ?? Tex.marble).relevo });
            MAT["copaCarvalho"] = Mat.Padrao(0x1c241a); facetados.Add(MAT["copaCarvalho"]);
            MAT["preto"] = Mat.Brilho(Mat.Hex(0x020103), null, Mat.Mistura.Alfa, false, true, 0, true, 2000);
            MAT["sebe"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x223020), mapa = Tex.cloth.mapa, relevo = Tex.leather.relevo });
            MAT["agua"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x05080c), rugosidade = .05f, metal = .6f });
            MAT["cascaClaustro"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x2a221c), mapa = Tex.wood.mapa, relevo = Tex.wood.relevo });
            MAT["lajes"] = Mat.Pbr(Tex.floor, 0xffffff, 1, null, .55f); // o piso com a repetição na geometria (pisos da v6)
            MAT["metalDupla"] = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = Tex.metal.mapa, relevo = Tex.metal.relevo, metal = .85f, duplaFace = true });
            foreach (var kv in MAT) kv.Value.name = kv.Key;
            CriarMateriaisV6();
        }

        // ---------- PECAS: a aparência de cada tipo de peça (a colisão continua a caixa da simulação) ----------
        void Peca(PecaV w)
        {
            if (PecaV6(w)) return; // as peças da v6 (client/mundo.js) valem antes das antigas
            float x = (w.x1 + w.x2) / 2, z = (w.z1 + w.z2) / 2, L = w.x2 - w.x1, D = w.z2 - w.z1;
            switch (w.kind)
            {
                case "arvore_f": case "espinheiro": return; // floresta
                case "pilar_ruina":
                    Est(Geo.Cilindro(.55f, .62f, w.h, 8), M("pedraVelha"), x, w.h / 2, z);
                    Caixa(x - .8f, x + .8f, 0, .6f, z - .8f, z + .8f, M("pedraVelha"), 2); Caixa(x - .75f, x + .75f, w.h - .2f, w.h + .3f, z - .75f, z + .75f, M("pedraVelha"), 2); return;
                case "estatua": Estatua(w, x, z); return;
                case "rocha":
                    {
                        // v6.4: a rocha e a pedra escaneadas (CC0, Resources/Modelos) esticadas na caixa da peça; sem elas, os dodecaedros
                        var MR = ModeloGLB.Carregar("rocha"); var MP = ModeloGLB.Carregar("pedra");
                        float ry = vr() * 3;
                        if (MR != null) Est(MR.geo, MR.mat, TRS(x, -w.h * .08f, z, 0, ry, 0, L * 1.05f, w.h * 1.2f, D * 1.05f));
                        else Est(Geo.Dodecaedro(.5f, 1), M("rocha"), TRS(x, w.h * .42f, z, 0, ry, 0, L * 1.05f, w.h * 1.5f, D * 1.05f));
                        if (vr() < .6f)
                        {
                            float px = x + (vr() - .5f) * L, pz = z + (vr() - .5f) * D; float a = vr(), b = vr() * 3, c = vr();
                            if (MP != null) Est(MP.geo, MP.mat, TRS(px, -.05f, pz, a * .3f, b, c * .3f, L * .5f, w.h * .65f, D * .5f));
                            else Est(Geo.Dodecaedro(.5f), M("rocha"), TRS(px, w.h * .25f, pz, a, b, c, L * .5f, w.h * .8f, D * .5f));
                        }
                        return;
                    }
                case "tronco": case "raiz":
                    {
                        bool aoX = L >= D; float len = Math.Max(L, D), gr = w.kind == "tronco" ? 1.3f : 1;
                        var MT = ModeloGLB.Carregar("tronco");
                        if (MT != null)
                        {   // v6.4: o tronco morto escaneado (CC0), deitado na caixa; os mesmos sorteios dos galhos de antes
                            Est(MT.geo, MT.mat, TRS(x, -.05f, z, 0, aoX ? 0 : PI / 2, 0, len * 1.05f, 1.05f * gr, 1.05f * gr));
                            for (int k = 0; k < 12; k++) vr();
                            return;
                        }
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
                case "caixote":
                    {
                        var MC = ModeloGLB.Carregar("caixote");   // v6.4: o caixote escaneado (CC0)
                        if (MC == null) { Caixa(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("wood"), 1); return; }
                        bool aoX = L >= D; Est(MC.geo, MC.mat, TRS(x, 0, z, 0, aoX ? 0 : PI / 2, 0, Math.Max(L, D), w.h, Math.Min(L, D))); return;
                    }
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
            var ME = ModeloGLB.Carregar("escombro");
            if (ME != null)
            {   // v6.4b: a pilha de pedras escaneada (CC0) na caixa; os mesmos sorteios de antes
                bool aoX = L >= D; Est(ME.geo, ME.mat, TRS((x1 + x2) / 2, 0, (z1 + z2) / 2, 0, aoX ? 0 : PI / 2, 0, Math.Max(L, D), Math.Max(.5f, h), Math.Min(L, D)));
                for (int k = 0; k < n * 9; k++) vr(); return;
            }
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
            var MT = M("estatua"); var MS = ModeloGLB.Carregar("estatua"); int pose = w.pose; bool ajoelha = MS == null && (pose == 1 || pose == 3); float alt = ajoelha ? .62f : 1;
            // v6: pedestal de pedra. A caixa da estátua é alta (bloqueia visão e tiro até 2,4 m); sem pedestal, a figura ajoelhada
            // ficava abaixo do olho e dava para ver por cima do que bloqueia (tools/alturas.js)
            float ped = ajoelha ? 1.05f : .7f; Caixa(x - .42f, x + .42f, 0, ped, z - .42f, z + .42f, M("pedraVelha"), 1.5f); Caixa(x - .5f, x + .5f, ped - .08f, ped, z - .5f, z + .5f, M("pedraVelha"), 1.5f);
            if (MS != null) Est(MS.geo, MS.mat, TRS(x, ped, z, 0, w.rot, 0));   // o monge gerado, em pé, para todas as poses
            else EstatuaFigura(MT, pose, alt, x, ped, z, w.rot);
            for (int k = 0; k < 3; k++) { var g = Geo.Dodecaedro(.12f + vr() * .12f); float px = (vr() - .5f) * 1.4f, pz = (vr() - .5f) * 1.4f; Est(g, MT, x + px, .08f, z + pz); }
        }

        void EstatuaFigura(Material MT, int pose, float alt, float x, float ped, float z, float rot)
        {
            float gx = pose == 0 ? .12f : 0, gz = pose == 3 ? .1f : 0; var G = TRS(x, ped, z, gx, rot, gz);
            Est(Geo.Torno(new float[,] { { .42f, 0 }, { .38f, .3f }, { .3f, .8f * alt + .2f }, { .25f, 1.25f * alt + .1f }, { .14f, 1.42f * alt + .1f }, { 0, 1.45f * alt + .1f } }, 9), MT, G);
            float cabRx = pose == 0 ? .5f : pose == 1 ? .7f : pose == 2 ? -.35f : .55f;
            Est(Geo.Esfera(.17f, 8, 6), MT, G * TRS(0, 1.62f * alt + .1f, -.03f, cabRx, 0, 0, 1, 1.15f, 1));
            Est(Geo.Cone(.22f, .42f, 8), MT, G * TRS(0, 1.72f * alt + .12f, .03f, -.25f, 0, 0));
            Action<float, float, float> braco = (sx, rx, rz) => Est(Geo.Cilindro(.065f, .08f, .62f, 6), MT, G * TRS(sx * .26f, 1.3f * alt + .1f, 0, rx, 0, rz) * M4.T(0, -.31f, 0));
            if (pose == 0) { braco(-1, -.9f, .15f); braco(1, -.9f, -.15f); }
            else if (pose == 1) { braco(-1, -1.3f, .1f); braco(1, -1.3f, -.1f); }
            else if (pose == 2) { braco(-1, .2f, .3f); braco(1, -2.9f, -.2f); }
            else { braco(-1, -2.4f, .55f); braco(1, -2.4f, -.55f); }
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

        void BuildWorld()
        {
            // v6: pisos, paredes, telhados, janelas e portas da catedral vêm de MundoV6.cs (client/mundo.js); o asilo, de MundoAsilo.cs
            if (MapaVisual.Asilo) ConstruirAsilo();
            else { foreach (var w in MapaVisual.WALLS) Peca(w);
            foreach (var P in MapaVisual.PEWS)
                for (float x = P.x + .3f; x < P.y - .5f; x += 3.1f) foreach (float zz in new[] { P.z + .4f, P.z + 2.1f })
                    {
                        if (zz > P.w - .5f) continue;
                        Caixa(x, x + 2.7f, .42f, .52f, zz, zz + .55f, M("wood"), 2); Caixa(x, x + 2.7f, .52f, 1, zz + .5f, zz + .62f, M("wood"), 2);
                        Caixa(x, x + .1f, 0, .95f, zz, zz + .6f, M("wood"), 2); Caixa(x + 2.6f, x + 2.7f, 0, .95f, zz, zz + .6f, M("wood"), 2);
                    }
            ConstruirV6(); }
            // velas, fogueira, braseiros e lanternas (as lâmpadas do asilo são do MundoAsilo)
            var chama = Mat.Brilho(Mat.Hex(0xffc070), Tex.Chama(), Mat.Mistura.Aditiva, true); chama.name = "chama";
            var chamaVerm = Mat.Brilho(Mat.Hex(0xff5a4a), Tex.Chama(), Mat.Mistura.Aditiva, true); chamaVerm.name = "chamaVermelha";
            var vidro = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0xffc070), rugosidade = .4f, emissao = Mat.Hex(0xff9030), emissaoK = 1.6f }); vidro.name = "lanternaVidro";
            var metalDupla = M("metalDupla");
            foreach (var cd in MapaVisual.CANDLES)
            {
                if (cd.tipo == "lampada") continue;
                float x = cd.x, z = cd.z; var fonte = new Fonte { x = x, z = z, y = 2.1f, cor = 0xff8a36, i = 1.6f, alc = 14 };
                if (cd.tipo == "fogueira")
                {
                    for (int k = 0; k < 6; k++) Est(Geo.Cilindro(.09f, .11f, 1.3f, 6), M("wood"), TRS(x, .2f, z, PI / 2 - .35f, k / 6f * PI * 2, 0));
                    var MF = ModeloGLB.Carregar("fogueira");   // v6.4b: o anel de pedras escaneado (CC0)
                    if (MF != null) Est(MF.geo, MF.mat, TRS(x, 0, z, 0, 0, 0, 2.1f, .42f, 2.1f), false);
                    else for (int k = 0; k < 9; k++) { float a = k / 9f * PI * 2; Est(Geo.Dodecaedro(.22f), M("wall"), x + Mathf.Cos(a) * .85f, .1f, z + Mathf.Sin(a) * .85f); }
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
                    var ML = ModeloGLB.Carregar("lanterna");   // v6.4b: a lanterna de madeira escaneada (CC0), com a chama dentro
                    if (ML != null) { Est(Geo.Cilindro(.05f, .05f, .12f, 6), vidro, x + .5f, 2.1f, z); Est(ML.geo, ML.mat, TRS(x + .5f, 1.89f, z, 0, 0, 0, .32f, .44f, .32f)); }
                    else Est(Geo.Cilindro(.14f, .18f, .34f, 6), vidro, x + .5f, 2.1f, z);
                    SpriteEst(x + .5f, 2.1f, z, .5f, .5f, chama);
                    fonte.x = x + .5f; fonte.y = 2.1f; fonte.cor = 0xffb060; fonte.i = 1.7f; fonte.alc = 13;
                }
                else
                {
                    Est(Geo.Cilindro(.06f, .08f, 1.5f, 6), M("metal"), x, .75f, z); Est(Geo.Cilindro(.55f, .35f, .08f, 12), M("metal"), x, 1.5f, z);
                    for (int i = 0; i < 5; i++) { float a = i / 5f * PI * 2, cx = x + Mathf.Cos(a) * .32f, cz = z + Mathf.Sin(a) * .32f, h = rand(.18f, .38f); Est(Geo.Cilindro(.045f, .05f, h, 6), M("wax"), cx, 1.54f + h / 2, cz); SpriteEst(cx, 1.6f + h, cz, .16f, .26f, chama); }
                }
                FONTES.Add(fonte);
            }
            // v6: reagentes com forma própria por subárea da mata (MundoReagentes.cs)
            for (int i = 0; i < Mapa.REAG.Count; i++) reagentes.Add(ModeloReagente(i < (MapaVisual.Asilo ? REAG_TIPOS_ASILO : REAG_TIPOS).Length ? (MapaVisual.Asilo ? REAG_TIPOS_ASILO : REAG_TIPOS)[i] : "sal", (float)Mapa.REAG[i].x, (float)Mapa.REAG[i].z));
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
