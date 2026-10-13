// Personagens do protótipo (makeCharacter / animateActor): Cultistas de túnica esfarrapada com capuz, olhos em brasa
// e cetro de vértebras; Caçadores de casaco, chapéu, bandoleira, lamparina e carabina ou revólver. As peças de cada
// junta são fundidas por material (poucas chamadas de desenho) e a animação é procedural: tudo sai do estado que a
// simulação manda (posição, mira, estado, recarga), com morte que tomba para o lado do tiro e se dissolve em brasa/luz.
// Coordenadas e giros do protótipo; o boneco vive sob a raiz espelhada do Mundo.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    // junta peças paradas por (junta, material) e cria uma malha para cada par
    public class Kit
    {
        readonly Dictionary<Transform, Dictionary<Material, Geo>> d = new Dictionary<Transform, Dictionary<Material, Geo>>();
        public readonly List<Renderer> renderers = new List<Renderer>();
        public void Por(Transform j, Material m, Geo g, M4 t)
        {
            Dictionary<Material, Geo> pm; if (!d.TryGetValue(j, out pm)) { pm = new Dictionary<Material, Geo>(); d[j] = pm; }
            Geo acc; if (!pm.TryGetValue(m, out acc)) { acc = new Geo(); pm[m] = acc; }
            acc.Juntar(g, t);
        }
        public void Por(Transform j, Material m, Geo g, float x, float y, float z, float rx = 0, float ry = 0, float rz = 0, float sx = 1, float sy = 1, float sz = 1)
        { Por(j, m, g, M4.TRS(x, y, z, rx, ry, rz, sx, sy, sz)); }
        public void Fechar(bool sombra = true)
        {
            foreach (var kv in d) foreach (var km in kv.Value)
                {
                    var go = Mundo.Malha(km.Value, km.Key, kv.Key, km.Key.name, sombra && km.Key.shader != Mat.ShaderBrilho);
                    renderers.Add(go.GetComponent<Renderer>());
                }
            d.Clear();
        }
    }

    public static class Pecas
    {
        const float PI = Mathf.PI;
        public static Material Pano(int cor, float r = .95f) { return Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(cor), mapa = Tex.cloth.mapa, relevo = Tex.cloth.relevo, rugosidade = r }); }
        public static Material Couro(int cor) { return Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(cor), mapa = Tex.leather.mapa, relevo = Tex.leather.relevo, rugosidade = .68f }); }
        public static Material Metal(int cor, float r = .35f, float m = .9f) { return Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(cor), relevo = Tex.metal.relevo, rugosidade = r, metal = m }); }
        public static Material Madeira(int cor, float r) { return Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(cor), mapa = Tex.wood.mapa, relevo = Tex.wood.relevo, rugosidade = r }); }
        public static Material Osso() { return Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0xd8caa6), relevo = Tex.wall.relevo, rugosidade = .62f }); }
        public static Material Liso(int cor, float r) { return Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(cor), rugosidade = r }); }
        // MeshBasicMaterial: cor pura, sem luz (olhos, rosto sob o capuz, brasas)
        public static Material Basico(int cor, float a = 1) { var m = Mat.Brilho(Mat.Hex(cor, a), null, Mat.Mistura.Alfa, false, true, 0, a >= 1, a >= 1 ? 2000 : 3000); m.name = "básico"; return m; }
        public static Material Aditivo(int cor, Texture2D tex, bool billboard, float a = 1) { return Mat.Brilho(Mat.Hex(cor, a), tex, Mat.Mistura.Aditiva, billboard); }

        // Barra esfarrapada: desloca os vértices de baixo com ondas irregulares.
        public static Geo Esfarrapar(Geo g, float amp, float seed)
        {
            float mn = 1e9f; foreach (var q in g.p) mn = Mathf.Min(mn, q.y);
            for (int i = 0; i < g.p.Count; i++)
            {
                var q = g.p[i]; if (q.y >= mn + .05f) continue; float a = Mathf.Atan2(q.z, q.x);
                q.y += (Mathf.Sin(a * 7 + seed) * .5f + Mathf.Sin(a * 13 + seed * 2.3f) * .3f + Mathf.Sin(a * 29 + seed) * .2f) * amp; g.p[i] = q;
            }
            return g.NormaisDasFaces();
        }
        // Peça lateral extrudada com bisel (coronhas, armações, empunhaduras). x do perfil = frente da arma (-z), y = cima.
        public static Geo Lateral(float[][] pts, float prof, float bisel) { return Geo.Extrusao(Geo.Contorno(pts), prof, bisel).Mover(0, 0, -prof / 2).GirarY(PI / 2); }
        public static Geo CilZ(float r1, float r2, float len, int segs = 20) { return Geo.Cilindro(r1, r2, len, segs).GirarX(PI / 2); }
        static float[] P(params float[] v) { return v; }

        public class Arma { public Transform g; public Vector3 boca; }
        // Soldado: carabina de alavanca, nogueira e aço escurecido
        public static Arma Carabina(Kit k, Transform pai)
        {
            var g = Mundo.Grupo(pai, "carabina"); Material aco = Metal(0x34373c, .4f), latao = Metal(0xb58c48, .28f, 1), mad = Madeira(0x7a4626, .5f);
            k.Por(g, aco, Lateral(new[] { P(-.06f, -.034f), P(.1f, -.034f), P(.1f, .05f), P(.03f, .062f), P(-.05f, .058f), P(-.06f, .04f) }, .042f, .006f), M4.I);
            k.Por(g, aco, CilZ(.015f, .017f, .64f), 0, .036f, -.42f);
            k.Por(g, aco, CilZ(.011f, .011f, .52f, 16), 0, .004f, -.36f);
            k.Por(g, mad, Lateral(new[] { P(.1f, -.028f), P(.38f, -.022f), P(.4f, -.008f, .39f, .014f), P(.1f, .018f) }, .04f, .007f), M4.I);
            k.Por(g, mad, Lateral(new[] { P(-.06f, .04f), P(-.2f, .026f), P(-.43f, .034f), P(-.46f, .03f, -.465f, 0), P(-.46f, -.105f), P(-.43f, -.125f, -.39f, -.12f), P(-.2f, -.068f), P(-.06f, -.034f) }, .044f, .009f), M4.I);
            foreach (float z in new[] { -.3f, -.62f }) k.Por(g, latao, Geo.Toro(.021f, .005f, 8, 20), 0, .02f, z);
            k.Por(g, aco, Geo.Toro(.036f, .0065f, 8, 24, PI * 1.35f), 0, -.062f, .015f, PI * .55f, PI / 2, 0);
            k.Por(g, latao, Geo.Caixa(.006f, .028f, .008f), 0, -.045f, 0, .3f);
            k.Por(g, aco, Geo.Caixa(.012f, .03f, .018f), 0, .07f, .045f, -.5f);
            k.Por(g, latao, Geo.Caixa(.006f, .018f, .012f), 0, .058f, -.73f);
            k.Por(g, aco, Geo.Caixa(.03f, .014f, .01f), 0, .06f, -.13f);
            return new Arma { g = g, boca = new Vector3(0, .036f, -.755f) };
        }
        // Exorcista: revólver de prata gravada, cabo escuro com cruz
        public static Arma Revolver(Kit k, Transform pai)
        {
            var g = Mundo.Grupo(pai, "revólver"); Material prata = Metal(0xc8ccd2, .22f, 1), escuro = Metal(0x1c1d20, .45f), mad = Madeira(0x2e1a12, .45f);
            k.Por(g, prata, Lateral(new[] { P(-.05f, -.022f), P(.075f, -.022f), P(.085f, .042f), P(-.02f, .05f), P(-.05f, .03f) }, .03f, .005f), M4.I);
            k.Por(g, prata, CilZ(.013f, .014f, .2f, 20), 0, .03f, -.17f);
            k.Por(g, prata, Geo.Caixa(.012f, .012f, .2f), 0, .045f, -.17f);
            k.Por(g, prata, CilZ(.036f, .036f, .066f, 28), 0, .014f, -.035f);
            for (int i = 0; i < 6; i++) { float a = i / 6f * PI * 2; k.Por(g, escuro, CilZ(.006f, .006f, .068f, 8), Mathf.Cos(a) * .034f, .014f + Mathf.Sin(a) * .034f, -.035f); }
            k.Por(g, mad, Lateral(new[] { P(-.05f, .012f), P(.012f, -.015f), P(0, -.1f), P(-.012f, -.14f, -.05f, -.135f), P(-.075f, -.07f), P(-.07f, -.005f) }, .034f, .007f), M4.I);
            foreach (float sx in new[] { -1f, 1f }) { k.Por(g, prata, Geo.Caixa(.002f, .05f, .009f), sx * .021f, -.07f, .04f); k.Por(g, prata, Geo.Caixa(.002f, .009f, .03f), sx * .021f, -.058f, .04f); }
            k.Por(g, prata, Geo.Toro(.025f, .005f, 8, 20, PI * 1.2f), 0, -.036f, 0, PI * .6f, PI / 2, 0);
            k.Por(g, escuro, Geo.Caixa(.01f, .03f, .016f), 0, .058f, .035f, -.6f);
            k.Por(g, prata, Geo.Caixa(.004f, .014f, .01f), 0, .05f, -.265f);
            return new Arma { g = g, boca = new Vector3(0, .03f, -.28f) };
        }

        public class Cetro { public Transform g, cab; public GameObject brasa, halo, runa; public Material mHalo, mRuna; }
        // Cultistas: cetro de vértebras com uma brasa presa em garras de osso
        public static Cetro CetroOsso(Kit k, Transform pai, int cor)
        {
            var C = new Cetro(); var g = C.g = Mundo.Grupo(pai, "cetro"); Material osso = Osso(), ferro = Metal(0x2a2226, .5f, .8f);
            var pts = new List<Vector3>(); for (int i = 0; i <= 14; i++) { float t = i / 14f; pts.Add(new Vector3(Mathf.Sin(t * 9) * .012f, t * 1f, Mathf.Cos(t * 7) * .01f)); }
            var curva = new Geo.Curva(pts);
            k.Por(g, osso, Geo.Tubo(curva, 56, .016f, 12), M4.I);
            for (int i = 1; i < 8; i++) { var p = curva.Ponto(i / 8.6f); k.Por(g, osso, Geo.Toro(.02f, .0075f, 10, 18), p.x, p.y, p.z, PI / 2); }
            k.Por(g, ferro, Geo.Cone(.02f, .07f, 12), pts[0].x, -.03f, pts[0].z, PI);
            var cab = C.cab = Mundo.Grupo(g, "cabeça"); cab.localPosition = curva.Ponto(1);
            for (int i = 0; i < 4; i++)
            {
                float a = i / 4f * PI * 2 + .4f; var c = new Geo.Curva(new[] { Vector3.zero, new Vector3(Mathf.Cos(a) * .05f, .04f, Mathf.Sin(a) * .05f), new Vector3(Mathf.Cos(a) * .072f, .11f, Mathf.Sin(a) * .072f), new Vector3(Mathf.Cos(a) * .03f, .18f, Mathf.Sin(a) * .03f) });
                k.Por(cab, osso, Geo.Tubo(c, 20, .0075f, 8), M4.I); k.Por(cab, osso, Geo.Cone(.008f, .03f, 8), Mathf.Cos(a) * .025f, .195f, Mathf.Sin(a) * .025f);
            }
            k.Por(cab, ferro, Geo.Toro(.052f, .006f, 8, 28), 0, .035f, 0, PI / 2);
            C.brasa = Mundo.Malha(Geo.Esfera(.036f, 24, 18), Basico(cor), cab, "brasa", false); Mundo.Pos(C.brasa, 0, .1f, 0);
            C.mHalo = Aditivo(cor, Tex.Brilho(), true, .8f); C.halo = Mundo.Sprite(cab, C.mHalo, .3f, .3f, "halo"); Mundo.Pos(C.halo, 0, .1f, 0);
            C.mRuna = Aditivo(0xffd6ff, Tex.Runa(), true, .75f); C.runa = Mundo.Sprite(cab, C.mRuna, .11f, .11f, "runa"); Mundo.Pos(C.runa, 0, .1f, 0);
            for (int i = 0; i < 3; i++)
            {
                float a = i / 3f * PI * 2; k.Por(cab, ferro, Geo.Cilindro(.002f, .002f, .09f, 4), Mathf.Cos(a) * .05f, -.01f, Mathf.Sin(a) * .05f);
                k.Por(cab, osso, Geo.Esfera(.009f, 10, 8), Mathf.Cos(a) * .05f, -.06f, Mathf.Sin(a) * .05f, 0, 0, 0, 1, 2.2f, 1);
            }
            return C;
        }
        public static Geo Torno(params float[] xy) { var p = new float[xy.Length / 2, 2]; for (int i = 0; i < xy.Length / 2; i++) { p[i, 0] = xy[i * 2]; p[i, 1] = xy[i * 2 + 1]; } return Geo.Torno(p, 28); }
    }

    public class Boneco
    {
        const float PI = Mathf.PI;
        public class Junta { public Transform t; public Vector3 r, p; public void Aplicar() { t.localRotation = Mundo.Q(r.x, r.y, r.z); } }
        public Transform g, bodyT; public Junta body, torso, head, legL, legR, armL, armR, gun; Junta[] juntas;
        public Transform robe, aba; public float robeZ, abaX;
        public GameObject rune, flash; public Material mRune, mFlash;
        public Pecas.Cetro cetro;
        public string team; public float escala = 1;
        readonly List<Material> pisca = new List<Material>(); readonly List<Color> piscaE = new List<Color>();
        public readonly List<Material> todas = new List<Material>(); readonly List<Renderer> rends = new List<Renderer>(); readonly List<Material[]> originais = new List<Material[]>();
        readonly List<GameObject> sprites = new List<GameObject>();
        // estado da animação
        public float phase = Random.value * 6, spd, kick, cast, flinch, dieT = -1, bodyY; public float? px, pz; public string prevSt = "alive"; public Vector2? golpe;
        bool deitado; string eixo; float sinal, torcao;
        // marcas de estado e runas em órbita
        GameObject atord, cego, runaM, orbita; Material mAtord, mCego, mRunaM; readonly List<GameObject> orb = new List<GameObject>(); readonly List<Material> mOrb = new List<Material>();
        Material mVeu; bool veuOn; float fadeAtual = 1;

        Junta J(Transform pai, float x, float y, float z, string nome) { var t = Mundo.Grupo(pai, nome); t.localPosition = new Vector3(x, y, z); return new Junta { t = t }; }
        Material Pisca(Material m) { pisca.Add(m); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); piscaE.Add(Color.black); return m; }

        public Boneco(Transform pai, string team, string cls)
        {
            this.team = team; var k = new Kit();
            g = Mundo.Grupo(pai, team + " " + cls); bodyT = Mundo.Grupo(g, "corpo"); body = new Junta { t = bodyT };
            if (team == "C")
            {
                bool guard = cls == "guardiao";
                var tunica = Pisca(Pecas.Pano(guard ? 0x40101a : 0x581320)); tunica.SetFloat("_Cull", 0);
                var escuro = Pecas.Pano(0x2c0c14); escuro.SetFloat("_Cull", 0); var pele = Pecas.Liso(0x2e1c1e, .8f); var corda = Pecas.Couro(0x8a6a44);
                legL = J(bodyT, -.12f, .62f, 0, "perna E"); legR = J(bodyT, .12f, .62f, 0, "perna D");
                foreach (var L in new[] { legL, legR }) { k.Por(L.t, escuro, Geo.Cilindro(.062f, .055f, .55f, 14), 0, -.3f, 0); k.Por(L.t, escuro, Geo.Esfera(.07f, 16, 12), 0, -.6f, -.04f, 0, 0, 0, 1, .6f, 1.7f); }
                robe = Mundo.Grupo(bodyT, "túnica"); robe.localPosition = new Vector3(0, .1f, 0);
                k.Por(robe, tunica, Pecas.Esfarrapar(Pecas.Torno(.52f, 0, .49f, .12f, .44f, .35f, .38f, .6f, .33f, .82f, .31f, .94f, 0, .95f), .07f, guard ? 2 : 5), M4.I);
                k.Por(bodyT, corda, Geo.Toro(.315f, .024f, 10, 32), 0, 1, 0, PI / 2);
                foreach (float dx in new[] { -.08f, .05f }) k.Por(bodyT, corda, Geo.Cilindro(.012f, .01f, .3f, 8), dx, .84f, -.3f, .12f);
                torso = J(bodyT, 0, 1.02f, 0, "tronco");
                k.Por(torso.t, tunica, Pecas.Torno(.31f, 0, .33f, .12f, .34f, .26f, .3f, .36f, .18f, .42f, 0, .43f), M4.I);
                k.Por(torso.t, escuro, Pecas.Torno(.36f, .28f, .38f, .34f, .32f, .42f, .22f, .46f, .13f, .46f), M4.I);
                k.Por(torso.t, Pecas.Metal(0xb58c48, .3f, 1), Geo.Octaedro(.035f, 1), 0, .2f, -.33f, 0, 0, 0, .8f, 1.2f, .5f);
                head = J(torso.t, 0, .44f, 0, "cabeça");
                k.Por(head.t, tunica, Pecas.Torno(.2f, -.1f, .25f, .02f, .265f, .16f, .235f, .3f, .17f, .42f, .09f, .52f, .03f, .6f, 0, .62f), 0, 0, .02f, -.12f);
                k.Por(head.t, Pecas.Basico(0x020103), Geo.Esfera(.15f, 20, 16), 0, .1f, -.15f, 0, 0, 0, 1, 1.15f, .6f);
                var olho = Pecas.Basico(0xff2a44); foreach (float sx in new[] { -1f, 1f }) k.Por(head.t, olho, Geo.Esfera(.024f, 10, 8), sx * .055f, .12f, -.235f);
                armL = J(torso.t, -.34f, .32f, 0, "braço E"); armR = J(torso.t, .34f, .32f, 0, "braço D");
                foreach (var A in new[] { armL, armR }) { k.Por(A.t, tunica, Geo.Cilindro(.075f, .15f, .62f, 18, 1, true), 0, -.3f, 0); k.Por(A.t, pele, Geo.Esfera(.052f, 14, 10), 0, -.6f, 0); }
                if (guard)
                {
                    var osso = Pecas.Osso();
                    foreach (var par in new[] { new KeyValuePair<Junta, float>(armL, -1), new KeyValuePair<Junta, float>(armR, 1) })
                    {
                        var A = par.Key; float sx = par.Value;
                        k.Por(A.t, osso, Geo.Esfera(.14f, 20, 14, 0, PI * 2, 0, PI * .55f), 0, .02f, 0, 0, 0, 0, 1.05f, .85f, 1.15f);
                        for (int i = 0; i < 3; i++) k.Por(A.t, osso, Geo.Cone(.032f, .26f, 14), sx * (.04f + i * .035f), .12f + i * .02f, .03f - i * .04f, 0, 0, -sx * (.35f + i * .25f));
                    }
                    for (int i = 0; i < 4; i++) k.Por(torso.t, osso, Geo.Toro(.26f - i * .015f, .014f, 8, 24, PI * .7f), 0, .1f + i * .075f, -.05f, -.2f, 0, PI * 1.15f);
                    escala = 1.12f;
                }
                else { mRune = Pecas.Aditivo(0x9a4ad0, Tex.Runa(), false); rune = Mundo.Malha(Geo.Plano(.3f, .3f), mRune, torso.t, "runa", false); Mundo.Pos(rune, 0, .08f, -.345f); Mundo.Rot(rune, 0, PI, 0); }
                gun = J(armR.t, 0, -.6f, -.02f, "mão");
                cetro = Pecas.CetroOsso(k, gun.t, guard ? 0xff4a5a : 0xb050ff); cetro.g.localPosition = new Vector3(0, -.36f, 0);
                sprites.Add(cetro.halo); sprites.Add(cetro.runa);
            }
            else
            {
                bool exo = cls == "exorcista";
                var casaco = Pisca(Pecas.Pano(exo ? 0x34343c : 0x384858)); casaco.SetFloat("_Cull", 0); var couro = Pecas.Couro(0x4a3220); var calca = Pecas.Pano(0x22262c); var pele = Pecas.Liso(0x8a6450, .78f);
                var ouro = Pecas.Metal(0xb58c48, .3f, 1);
                legL = J(bodyT, -.12f, .9f, 0, "perna E"); legR = J(bodyT, .12f, .9f, 0, "perna D");
                foreach (var L in new[] { legL, legR })
                {
                    k.Por(L.t, calca, Geo.Cilindro(.085f, .066f, .66f, 16), 0, -.33f, 0); k.Por(L.t, couro, Geo.Cilindro(.07f, .08f, .26f, 16), 0, -.72f, 0);
                    k.Por(L.t, couro, Geo.Esfera(.075f, 16, 10), 0, -.85f, -.045f, 0, 0, 0, 1, .55f, 1.75f);
                }
                aba = Mundo.Grupo(bodyT, "aba"); aba.localPosition = new Vector3(0, .4f, 0);
                k.Por(aba, casaco, Pecas.Esfarrapar(Pecas.Torno(.4f, 0, .38f, .18f, .35f, .4f, .32f, .52f, 0, .53f), .025f, 3), M4.I);
                torso = J(bodyT, 0, .9f, 0, "tronco");
                k.Por(torso.t, casaco, Pecas.Torno(.31f, 0, .33f, .14f, .355f, .38f, .365f, .56f, .31f, .69f, .17f, .76f, 0, .77f), M4.I);
                k.Por(torso.t, casaco, Pecas.Torno(.2f, .64f, .25f, .7f, .27f, .8f, .24f, .83f), M4.I);
                k.Por(torso.t, couro, Geo.Toro(.335f, .028f, 10, 32), 0, .03f, 0, PI / 2);
                k.Por(torso.t, ouro, Geo.Caixa(.07f, .055f, .02f), 0, .03f, -.34f);
                k.Por(torso.t, couro, Geo.Toro(.4f, .026f, 8, 32, PI * .95f), 0, .36f, 0, 0, PI / 2, .75f);
                for (int i = 0; i < 6; i++) { float a = .9f + i * .16f; k.Por(torso.t, ouro, Geo.Cilindro(.011f, .011f, .05f, 10), Mathf.Cos(a) * .3f - .02f, .36f + Mathf.Sin(a) * .22f, -.34f, 0, 0, a); }
                k.Por(torso.t, Pecas.Metal(0x2a2a2e, .5f), Geo.Cilindro(.05f, .05f, .13f, 12, 1, true), -.37f, .02f, -.08f);
                k.Por(torso.t, Pecas.Basico(0xffd58a), Geo.Esfera(.035f, 12, 10), -.37f, .02f, -.08f);
                head = J(torso.t, 0, .8f, 0, "cabeça");
                k.Por(head.t, pele, Geo.Cilindro(.07f, .08f, .1f, 14), 0, -.05f, 0);
                k.Por(head.t, pele, Geo.Esfera(.15f, 24, 18), 0, .1f, 0, 0, 0, 0, 1, 1.12f, 1);
                Material orbitaM = Pecas.Liso(0x3a2418, .9f), sobr = Pecas.Liso(0x2a1c14, 1);
                foreach (float sx in new[] { -1f, 1f })
                {
                    k.Por(head.t, orbitaM, Geo.Esfera(.034f, 12, 8), sx * .055f, .13f, -.128f, 0, 0, 0, 1.2f, .8f, .5f);
                    k.Por(head.t, sobr, Geo.Caixa(.06f, .012f, .02f), sx * .055f, .165f, -.136f, 0, 0, sx * -.18f);
                }
                k.Por(head.t, pele, Geo.Cone(.022f, .06f, 10), 0, .1f, -.155f, -PI / 2 + .35f);
                k.Por(head.t, Pecas.Pano(exo ? 0xe6ddd0 : 0x5a3c2a), Geo.Cilindro(.155f, .165f, .13f, 24), 0, .02f, -.005f);
                Material pupila = Pecas.Basico(0x0d0a08), reflexo = Pecas.Basico(0xfff2d8);
                foreach (float sx in new[] { -1f, 1f }) { k.Por(head.t, pupila, Geo.Esfera(.016f, 10, 8), sx * .055f, .13f, -.146f); k.Por(head.t, reflexo, Geo.Esfera(.005f, 6, 4), sx * .055f + .006f, .137f, -.16f); }
                var chapeu = Pecas.Couro(0x4a3220); chapeu.SetFloat("_Cull", 0);
                k.Por(head.t, chapeu, Pecas.Torno(0, 0, .43f, -.012f, .445f, .012f, .22f, .036f, 0, .04f), 0, .22f, 0);
                k.Por(head.t, chapeu, Pecas.Torno(.185f, 0, .2f, .1f, .19f, .17f, .12f, .21f, 0, .2f), 0, .24f, 0);
                k.Por(head.t, Pecas.Pano(0x1a1a1a), Geo.Toro(.195f, .014f, 8, 28), 0, .265f, 0, PI / 2);
                armL = J(torso.t, -.37f, .66f, 0, "braço E"); armR = J(torso.t, .37f, .66f, 0, "braço D");
                foreach (var A in new[] { armL, armR }) { k.Por(A.t, casaco, Geo.Cilindro(.085f, .07f, .58f, 16), 0, -.29f, 0); k.Por(A.t, couro, Geo.Esfera(.062f, 14, 10), 0, -.6f, 0); }
                if (exo)
                {
                    Material alvo = Pecas.Pano(0xeee6d8), dour = Pecas.Metal(0xd4b060, .3f, 1), conta = Pecas.Metal(0x3a2a20, .5f, .2f);
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        k.Por(torso.t, alvo, Geo.Caixa(.085f, .86f, .012f), sx * .11f, .36f, -.36f);
                        k.Por(torso.t, dour, Geo.Caixa(.012f, .07f, .014f), sx * .11f, .1f, -.37f); k.Por(torso.t, dour, Geo.Caixa(.045f, .012f, .014f), sx * .11f, .115f, -.37f);
                    }
                    for (int i = 0; i < 12; i++) { float a = PI * (.15f + i * .058f); k.Por(torso.t, conta, Geo.Esfera(.013f, 8, 6), Mathf.Cos(a) * .2f, .66f - Mathf.Sin(a) * .2f, -.24f - Mathf.Sin(a) * .06f); }
                }
                else k.Por(armL.t, couro, Geo.Esfera(.15f, 20, 14, 0, PI * 2, 0, PI * .5f), 0, .02f, 0, 0, 0, 0, 1.05f, .7f, 1.1f);
                gun = J(armR.t, 0, -.6f, -.02f, "mão"); gun.r = new Vector3(-PI / 2, 0, 0); gun.Aplicar();
                var ar = exo ? Pecas.Revolver(k, gun.t) : Pecas.Carabina(k, gun.t); ar.g.localPosition = new Vector3(0, 0, .02f);
                mFlash = Pecas.Aditivo(0xffd08a, Tex.Chama(), true, 0); flash = Mundo.Sprite(ar.g, mFlash, .5f, .5f, "clarão"); flash.transform.localPosition = ar.boca; sprites.Add(flash);
            }
            k.Fechar();
            juntas = new[] { body, torso, head, legL, legR, armL, armR, gun };
            g.localScale = Vector3.one * escala;
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                rends.Add(r); originais.Add(r.sharedMaterials);
                foreach (var m in r.sharedMaterials) if (m != null && m.shader == Mat.Lit && !todas.Contains(m)) todas.Add(m);
            }
            // marcas de estado sobre o personagem: quem vê entende o que a habilidade fez
            mAtord = Pecas.Aditivo(0xf3d58e, null, false); atord = Mundo.Malha(Geo.Toro(.26f, .035f, 8, 28), mAtord, g, "atordoado", false); atord.SetActive(false);
            mCego = Pecas.Aditivo(0xffffff, Tex.Brilho(), true); cego = Mundo.Sprite(g, mCego, .9f, .9f, "cego"); cego.SetActive(false);
            mRunaM = Pecas.Aditivo(0xc070ff, Tex.Runa(), true); runaM = Mundo.Sprite(g, mRunaM, 1.6f, 1.6f, "runa"); runaM.SetActive(false);
            if (team == "C")
            {
                orbita = Mundo.Grupo(g, "órbita").gameObject;
                for (int i = 0; i < 5; i++) { var m = Pecas.Aditivo(0xd070ff, Tex.Runa(), true); mOrb.Add(m); orb.Add(Mundo.Sprite(orbita.transform, m, .3f, .3f, "runa")); }
                orbita.SetActive(false);
            }
        }

        public void Destruir() { Object.Destroy(g.gameObject); }

        void Fade(float f)
        {
            if (Mathf.Abs(f - fadeAtual) < .001f) return; fadeAtual = f;
            foreach (var m in todas) Mat.Transparencia(m, f < 1, f);
        }
        // Véu: silhueta escura e translúcida
        public void Veu(float op)
        {
            if (op >= 1 && !veuOn) return;
            bool on = op < 1;
            if (mVeu == null) mVeu = Mat.Brilho(Mat.Hex(0x23232f), null, Mat.Mistura.Alfa, false, true, 0, false, 3000);
            if (on != veuOn)
            {
                for (int i = 0; i < rends.Count; i++)
                {
                    if (rends[i] == null) continue;
                    if (on) { var ms = new Material[originais[i].Length]; for (int j = 0; j < ms.Length; j++) ms[j] = mVeu; rends[i].sharedMaterials = ms; } else rends[i].sharedMaterials = originais[i];
                }
                foreach (var s in sprites) if (s != null) s.SetActive(!on);
            }
            Mat.Opacidade(mVeu, op); veuOn = on;
        }

        static float Suave(float t) { return t * t * (3 - 2 * t); }
        static float Queda(float t) { if (t < .72f) { float u = t / .72f; return u * u; } float v = (t - .72f) / .28f; return 1 - Mathf.Sin(v * PI) * .07f * (1 - v); }
        static float Lerp(float a, float b, float k) { return a + (b - a) * k; }

        // animateActor: tudo em coordenadas do protótipo; 'altares' para o cultista saber se está canalizando
        public void Animar(Mundo W, Ator a, float x, float z, float yaw, float dt, float t, List<Altar> altares)
        {
            if (px == null) { px = x; pz = z; }
            float v = Mathf.Sqrt((x - px.Value) * (x - px.Value) + (z - pz.Value) * (z - pz.Value)) / Mathf.Max(dt, 1e-3f); px = x; pz = z;
            spd = Lerp(spd, Mathf.Min(v, 8), .18f);
            string st = a.st;
            if (st == "dead" && prevSt != "dead") { dieT = 0; deitado = prevSt == "down"; }
            if (st != "dead" && dieT >= 0) { dieT = -1; Fade(1); }
            prevSt = st;
            kick = Mathf.Max(0, kick - dt * 6); cast = Mathf.Max(0, cast - dt * 3); flinch = Mathf.Max(0, flinch - dt * 5);
            var fe = flinch > 0 ? Mat.Hex(0x6a0a18) * (flinch * .75f) : Color.black; foreach (var m in pisca) m.SetColor("_EmissionColor", fe);
            g.localPosition = new Vector3(x, 0, z); g.localRotation = Mundo.Q(0, yaw, 0); g.gameObject.SetActive(true);
            float topo = (a.team == "C" ? 2.1f : 2.3f) / escala;
            bool vivo = a.st == "alive";
            atord.SetActive(a.stunT > 0 && vivo); if (atord.activeSelf) { atord.transform.localPosition = new Vector3(0, topo, 0); atord.transform.localRotation = Mundo.Q(PI / 2, 0, t * 6); Mat.Opacidade(mAtord, Mathf.Min(1, (float)a.stunT * 2)); }
            cego.SetActive(a.blindT > 0 && vivo); if (cego.activeSelf) { cego.transform.localPosition = new Vector3(0, topo - .45f, -.2f); Mat.Opacidade(mCego, Mathf.Min(1, (float)a.blindT) * (.7f + .3f * Mathf.Sin(t * 20))); }
            runaM.SetActive(a.runeT > 0 && vivo); if (runaM.activeSelf) { runaM.transform.localPosition = new Vector3(0, .08f, 0); mRunaM.SetFloat("_Giro", t * 1.5f); Mat.Opacidade(mRunaM, .5f + .2f * Mathf.Sin(t * 4)); if (Random.value < dt * 20) W.Particulas(x + Random.Range(-.3f, .3f), .2f, z + Random.Range(-.3f, .3f), 1, 0xb050ff, .3f, .9f, -1.5f, .3f); }
            if (cetro != null) { float pulso = .85f + .15f * Mathf.Sin(t * 5 + a.id); Mat.Opacidade(cetro.mHalo, (.55f + cast * .45f) * pulso); cetro.mRuna.SetFloat("_Giro", cetro.mRuna.GetFloat("_Giro") + dt * (1.5f + cast * 9)); }
            var B = body;
            // ---------- morte ----------
            if (st == "dead")
            {
                if (dieT == 0)
                {
                    float lx = 0, lz = 1; if (golpe.HasValue) { float dx = golpe.Value.x - x, dz = golpe.Value.y - z, c = Mathf.Cos(yaw), sn = Mathf.Sin(yaw); lx = dx * c - dz * sn; lz = dx * sn + dz * c; }
                    if (Mathf.Abs(lz) >= Mathf.Abs(lx) * .8f) { eixo = "x"; sinal = lz < 0 ? 1 : -1; } else { eixo = "z"; sinal = lx > 0 ? 1 : -1; }
                    torcao = Random.Range(-.5f, .5f);
                }
                dieT += dt; float tt = dieT;
                if (deitado)
                {
                    float esp = Mathf.Sin(tt * 18) * Mathf.Exp(-tt * 5) * .25f;
                    B.r = new Vector3(-1.42f + esp * .3f, 0, esp * .2f); bodyY = .08f; torso.r = new Vector3(esp, 0, 0); head.r = new Vector3(.5f - tt * .3f > .1f ? .5f - tt * .3f : .1f, 0, esp);
                    armL.r = new Vector3(Lerp(armL.r.x, 2.9f, .08f), 0, .4f); armR.r = new Vector3(Lerp(armR.r.x, 2.6f, .08f), 0, -.5f); legL.r.x = Lerp(legL.r.x, 0, .1f); legR.r.x = Lerp(legR.r.x, 0, .1f);
                }
                else
                {
                    float k1 = Suave(Mathf.Clamp01(tt / .3f)), k2 = Queda(Mathf.Clamp01((tt - .18f) / .72f)), q = 1.52f * sinal * k2;
                    B.r = new Vector3(eixo == "x" ? q : 0, torcao * k2, eixo == "z" ? q : 0); bodyY = -.04f * k2 - .24f * k1 * (1 - k2);
                    legL.r = new Vector3(-.95f * k1 * (1 - k2 * .75f), 0, 0); legR.r = new Vector3((-.5f * k1 + .18f * k2) * (1 - k2 * .4f), 0, 0);
                    torso.r = new Vector3((eixo == "x" ? -sinal * .35f : .25f) * k1 * (1 - k2 * .5f), 0, 0);
                    float chic = Mathf.Sin(Mathf.Min(tt, 1.3f) * 10) * Mathf.Exp(-tt * 4) * .45f;
                    head.r = new Vector3(.3f * k1 * sinal + chic, 0, chic * .6f);
                    float solto = 1 - k2, fim = sinal > 0 ? 2.3f : -.5f;
                    armL.r = new Vector3(-1.3f * k1 * solto + fim * k2, 0, .45f + .65f * k2); armR.r = new Vector3(-1.0f * k1 * solto + (fim + .15f) * k2, 0, -.45f - .75f * k2);
                    if (cetro != null) gun.r.x = -1.2f * k2;
                }
                if (mFlash != null) Mat.Opacidade(mFlash, 0);
                if (tt > 1.7f)
                {
                    Fade(Mathf.Clamp01(1 - (tt - 1.7f) / 1.1f));
                    if (Random.value < dt * 45) W.Particulas(x + Random.Range(-.35f, .35f), .1f + Random.value * .45f, z + Random.Range(-.35f, .35f), 1, a.team == "C" ? 0xb050ff : 0xffdc9a, .25f, 1.2f, -1.6f, .4f);
                }
                if (tt > 2.9f) g.gameObject.SetActive(false);
                if (orbita != null) orbita.SetActive(false);
                Aplicar(); return;
            }
            // ---------- vivo ou caído: calcula a pose alvo e chega nela suavemente ----------
            foreach (var j in juntas) j.p = j.r;
            float pb = bodyY;
            B.r = Vector3.zero; bodyY = 0; torso.r = Vector3.zero; head.r = Vector3.zero; legL.r = Vector3.zero; legR.r = Vector3.zero;
            gun.r = new Vector3(cetro != null ? 0 : -PI / 2, 0, 0);
            float gy = 0; robeZ = 0; abaX = 0;
            if (st == "down")
            {
                B.r.x = -1.42f; bodyY = .08f; float c = Mathf.Sin(t * 2.2f + a.id);
                armL.r = new Vector3(2.4f + c * .35f, 0, .15f); armR.r = new Vector3(2.4f - c * .35f, 0, -.15f); legL.r.x = c * .2f; legR.r.x = -c * .2f; head.r.x = .5f;
                if (mFlash != null) Mat.Opacidade(mFlash, 0);
                if (orbita != null) orbita.SetActive(false);
            }
            else
            {
                bool moving = spd > .5f; float pitch = (float)a.pitch;
                if (moving) phase += dt * (3.2f + spd * 1.35f);
                float amp = moving ? Mathf.Min(.75f, spd * .12f) : 0, sw = Mathf.Sin(phase) * amp;
                legL.r.x = sw; legR.r.x = -sw;
                gy = moving ? Mathf.Abs(Mathf.Sin(phase)) * .05f : 0;
                torso.r.x = pitch * .3f + Mathf.Sin(t * 1.7f + a.id) * .015f - (moving ? .08f : 0) - flinch * .25f;
                torso.r.z = Mathf.Sin(phase) * amp * .08f; head.r.x = pitch * .4f;
                abaX = -sw * .06f; robeZ = Mathf.Sin(phase) * amp * .05f;
                if (a.team == "H")
                {
                    float mira = PI / 2 + pitch * .7f;
                    if (a.sealing >= 0) { bodyY = -.38f; legL.r.x = -1.5f; legR.r.x = .25f; torso.r.x = -.35f; armR.r = new Vector3(1.1f, 0, 0); armL.r = new Vector3(1.2f + Mathf.Sin(t * 3) * .08f, 0, .2f); }
                    else if (a.reloadT > 0) { float p = 1 - (float)a.reloadT / 1.6f; armR.r = new Vector3(.9f + Mathf.Sin(p * PI) * .4f, 0, -.3f); armL.r = new Vector3(.9f + Mathf.Sin(p * PI * 3) * .4f, 0, .4f); gun.r.z = Mathf.Sin(p * PI) * .8f; }
                    else { armR.r = new Vector3(mira - kick * .45f, 0, 0); armL.r = new Vector3(mira - .1f - kick * .3f, 0, .45f); torso.r.x += kick * .08f; }
                    if (mFlash != null) Mat.Opacidade(mFlash, kick > .7f ? 1 : 0);
                }
                else
                {
                    bool noCirculo = false; if (altares != null) foreach (var Al in altares) if (Al.state == "active" && Mathf.Sqrt((float)((Al.x - x) * (Al.x - x) + (Al.z - z) * (Al.z - z))) <= CFG.circleR) noCirculo = true;
                    orbita.SetActive(noCirculo && !moving);
                    if (orbita.activeSelf)
                    {
                        for (int i = 0; i < orb.Count; i++) { float an = t * 1.4f + i / 5f * PI * 2; orb[i].transform.localPosition = new Vector3(Mathf.Cos(an) * .95f, 1.1f + Mathf.Sin(t * 2 + i) * .18f, Mathf.Sin(an) * .95f); mOrb[i].SetFloat("_Giro", an * 2); Mat.Opacidade(mOrb[i], .55f + .35f * Mathf.Sin(t * 3 + i)); }
                        if (Random.value < dt * 14) W.Particulas(x + Random.Range(-.4f, .4f), .25f, z + Random.Range(-.4f, .4f), 1, 0xb050ff, .2f, 1.3f, -1.8f, .2f);
                    }
                    if (noCirculo && !moving) { float s2 = Mathf.Sin(t * 2.5f + a.id) * .15f; armL.r = new Vector3(2.7f + s2, 0, .35f); armR.r = new Vector3(2.7f - s2, 0, -.35f); gun.r.x = -2.7f; head.r.x = .35f; }
                    else if (cast > 0) { float ang = PI / 2 + pitch * .7f + cast * .2f; armR.r = new Vector3(ang, 0, 0); gun.r.x = -ang - 1.05f; armL.r = new Vector3(-sw * .8f, 0, .1f); }
                    else { armL.r = new Vector3(-sw * .9f, 0, .08f); armR.r = new Vector3(.35f + sw * .5f, 0, -.08f); gun.r.x = -.35f; }
                    if (mRune != null) Mat.Cor(mRune, Mat.Hex(a.runeT > 0 ? 0xffd0ff : 0x9a4ad0));
                }
                if (a.stunT > 0) { torso.r.z = Mathf.Sin(t * 14) * .12f; head.r.z = Mathf.Sin(t * 11) * .2f; }
            }
            float kR = 1 - Mathf.Exp(-dt * 16), kB = 1 - Mathf.Exp(-dt * 9);
            foreach (var j in juntas) { float kk = j == body ? kB : kR; j.r = j.p + (j.r - j.p) * kk; }
            bodyY = pb + (bodyY - pb) * kB;
            g.localPosition = new Vector3(x, gy, z);
            Aplicar();
        }
        void Aplicar()
        {
            foreach (var j in juntas) j.Aplicar(); bodyT.localPosition = new Vector3(0, bodyY, 0);
            if (robe != null) robe.localRotation = Mundo.Q(0, 0, robeZ); if (aba != null) aba.localRotation = Mundo.Q(abaX, 0, 0);
        }
    }
}
