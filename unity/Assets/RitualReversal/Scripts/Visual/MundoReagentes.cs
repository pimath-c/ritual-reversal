// v6 no Unity (client/index.html): reagentes com forma própria por subárea da mata e que respondem à coleta e à
// queima, o relógio de velas em volta de cada altar e o fecho da execução (coluna de luz da caça, garras de sombra do
// culto, a marca que fica 40 s no chão). Tudo sai do estado que o servidor manda; nada aqui é regra.
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;
using Random = UnityEngine.Random;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        // ================= REAGENTES =================
        // Cada subárea da mata tem o seu reagente. O brilho vermelho continua: é a leitura de "aqui há reagente" para os dois
        // lados. Quem coleta ou queima segura E, e o modelo responde ao progresso (a raiz sobe do barro, as velas apagam...).
        public static readonly string[] REAG_TIPOS = { "sal", "sal", "mandragora", "mandragora", "cinza", "cera", "flor", "flor", "corvo", "corvo" };
        public static readonly string[] REAG_TIPOS_ASILO = { "mandragora", "flor", "cera", "cera", "sal", "cinza", "corvo", "corvo", "sal", "cinza" };   // o REAG_TIPOS_ASILO do navegador
        public class Parte { public Transform m; public Vector3 r, p; public float h; }
        public class Reag
        {
            public string tipo, acao = ""; public Transform g, raiz, tampa; public float x, z, k, kAlvo, cresce = 1; public bool tinha = true, somCol;
            public readonly List<Parte> partes = new List<Parte>(); public readonly List<Transform> chamas = new List<Transform>(); public readonly List<Material> mChamas = new List<Material>();
            public GameObject halo, fogo; public Material mHalo, mFogo;
        }
        public readonly List<Reag> reagentes = new List<Reag>();
        readonly Dictionary<string, Material> matReag = new Dictionary<string, Material>();
        Material Std(int cor, float r = .8f, float metal = 0, int emis = 0, float emisK = 1, bool dupla = false)
        {
            string k = cor + "|" + r + "|" + metal + "|" + emis + "|" + emisK + "|" + dupla; Material m;
            if (!matReag.TryGetValue(k, out m)) { m = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(cor), rugosidade = r, metal = metal, emissao = emis != 0 ? Mat.Hex(emis) : Color.black, emissaoK = emisK, duplaFace = dupla }); m.name = "reagente"; matReag[k] = m; }
            return m;
        }
        static Transform ParteR(Geo g, Material m, Transform pai, float px, float py, float pz, float rx = 0, float ry = 0, float rz = 0, float sx = 1, float sy = 1, float sz = 1, bool sombra = false)
        {
            var o = Malha(g, m, pai, "parte", sombra); o.transform.localPosition = new Vector3(px, py, pz); o.transform.localRotation = Q(rx, ry, rz); o.transform.localScale = new Vector3(sx, sy, sz); return o.transform;
        }
        Reag ModeloReagente(string tipo, float x, float z)
        {
            var P = new Reag { tipo = tipo, x = x, z = z }; P.g = Grupo(raiz, "Reagente (" + tipo + ")"); P.g.localPosition = new Vector3(x, 0, z); P.g.localRotation = Q(0, (x * 7.3f + z * 3.1f) % 6.28f, 0);
            var g = P.g;
            if (tipo == "sal")
            {
                ParteR(Geo.Dodecaedro(.42f), M("rocha"), g, 0, .12f, 0, 0, 0, 0, 1.2f, .55f, 1, true);
                var cr = Std(0x121216, .12f, .35f, 0x2a0610, .6f);
                for (int k = 0; k < 7; k++) { float a = k / 7f * 6.28f; var c = ParteR(Geo.Octaedro(.07f), cr, g, Mathf.Cos(a) * .18f, .3f, Mathf.Sin(a) * .14f, Mathf.Sin(a) * .6f, a, Mathf.Cos(a) * .6f, .7f, 2.4f + (k % 3) * .6f, .7f, true); P.partes.Add(new Parte { m = c, r = new Vector3(Mathf.Sin(a) * .6f, a, Mathf.Cos(a) * .6f), p = c.localPosition }); }
            }
            else if (tipo == "mandragora")
            {
                ParteR(Geo.Circulo(.55f, 18), Std(0x1c140c, .35f), g, 0, .015f, 0, -PI / 2, 0, 0);
                var raizG = Grupo(g, "raiz"); var pele = Std(0x8a6a48, .85f); ParteR(Geo.Cilindro(.06f, .025f, .36f, 7), pele, raizG, 0, -.1f, 0);
                foreach (int s in new[] { -1, 1 }) { ParteR(Geo.Cone(.025f, .22f, 6), pele, raizG, s * .04f, -.32f, 0, 0, 0, s * .35f + PI); ParteR(Geo.Cone(.018f, .14f, 5), pele, raizG, s * .06f, -.06f, 0, 0, 0, s * 1.1f + PI); }
                raizG.localPosition = new Vector3(0, -.05f, 0); P.raiz = raizG; var folha = Std(0x2a4020, .7f, 0, 0, 1, true);
                for (int k = 0; k < 7; k++) { float a = k / 7f * 6.28f; var r = new Vector3(-.6f * Mathf.Cos(a), -a + PI / 2, 0); var f = ParteR(Geo.Esfera(.16f, 8, 5), folha, g, Mathf.Cos(a) * .13f, .14f, Mathf.Sin(a) * .13f, r.x, r.y, r.z, .45f, .08f, 1.15f); P.partes.Add(new Parte { m = f, r = r, p = f.localPosition }); }
            }
            else if (tipo == "cinza")
            {
                var barro = Std(0x6a3e26, .75f);
                ParteR(Geo.Torno(new float[,] { { 0, 0 }, { .12f, .01f }, { .17f, .08f }, { .16f, .2f }, { .1f, .26f }, { .11f, .29f }, { 0, .29f } }, 14), barro, g, 0, 0, 0, 0, 0, 0, 1, 1, 1, true);
                P.tampa = ParteR(Geo.Cilindro(.12f, .115f, .03f, 14), barro, g, 0, .3f, 0, 0, 0, 0, 1, 1, 1, true);
                ParteR(Geo.Esfera(.2f, 10, 6, 0, 6.28f, 0, 1.2f), Std(0x6a6862, 1), g, .3f, 0, .1f, 0, 0, 0, 1, .35f, 1);
                for (int k = 0; k < 5; k++) ParteR(Geo.Cilindro(.012f, .015f, .16f, 5), Std(0xcab894, .7f), g, -.25f + k * .07f, .02f, .22f - k * .05f, PI / 2, 0, k, 1, 1, 1, true);
            }
            else if (tipo == "cera")
            {
                ParteR(Geo.Caixa(.7f, .08f, .45f), M("pedraVelha"), g, 0, .04f, 0, 0, 0, 0, 1, 1, 1, true); var cera = Std(0x14100e, .55f);
                for (int k = 0; k < 5; k++)
                {
                    float h = .12f + ((k * 37) % 5) * .04f, vx = -.25f + k * .12f, vz = k % 2 == 1 ? .08f : -.06f; var v = ParteR(Geo.Cilindro(.022f, .026f, h, 8), cera, g, vx, .08f + h / 2, vz, 0, 0, 0, 1, 1, 1, true); P.partes.Add(new Parte { m = v, h = h });
                    var mc = Mat.Brilho(Mat.Hex(0xffb060, .9f), Tex.Chama(), Mat.Mistura.Aditiva, true); var ch = Sprite(g, mc, .06f, .1f, "chama"); ch.transform.localPosition = new Vector3(vx, .08f + h + .04f, vz); P.chamas.Add(ch.transform); P.mChamas.Add(mc);
                }
            }
            else if (tipo == "flor")
            {
                ParteR(Geo.Esfera(.5f, 12, 6, 0, 6.28f, 0, 1.1f), Std(0x2a2418, 1), g, 0, 0, 0, 0, 0, 0, 1, .25f, 1, true);
                var petala = Std(0xe8e2d6, .6f, 0, 0x6a6a70, .5f, true); var haste = Std(0x3a4a2a, .8f);
                for (int k = 0; k < 3; k++)
                {
                    var fl = Grupo(g, "flor"); fl.localPosition = new Vector3(-.15f + k * .15f, 0, (k - 1) * .08f); ParteR(Geo.Cilindro(.008f, .01f, .34f, 5), haste, fl, 0, .17f, 0);
                    var cab = Grupo(fl, "cabeça"); cab.localPosition = new Vector3(0, .35f, 0);
                    for (int j = 0; j < 5; j++) ParteR(Geo.Esfera(.045f, 6, 4), petala, cab, Mathf.Cos(j * 1.26f) * .04f, 0, Mathf.Sin(j * 1.26f) * .04f, 0, -j * 1.26f, 0, .5f, .12f, 1);
                    P.partes.Add(new Parte { m = cab });
                }
            }
            else
            {
                ParteR(Geo.Dodecaedro(.3f), M("rocha"), g, 0, .08f, 0, 0, 0, 0, 1, .45f, 1, true); var pena = Std(0x0c0c10, .45f, .2f, 0, 1, true);
                for (int k = 0; k < 6; k++) { var r = new Vector3(0, 0, -.5f + k * .2f); var f = ParteR(Geo.Cone(.03f, .32f, 4), pena, g, -.04f + k * .016f, .22f, 0, r.x, r.y, r.z, 1, 1, .25f); P.partes.Add(new Parte { m = f, r = r }); }
                ParteR(Geo.Toro(.035f, .006f, 5, 12), Std(0x8a6a40, .9f), g, 0, .12f, 0, PI / 2, 0, 0, 1, 1, 1, true);
                ParteR(Geo.Esfera(.045f, 8, 6), Std(0xd8ccb0, .6f), g, .16f, .16f, .04f, 0, 0, 0, 1, 1, 1, true); ParteR(Geo.Cone(.018f, .09f, 5), Std(0x2a2620, .5f), g, .22f, .15f, .04f, 0, 0, -PI / 2, 1, 1, 1, true);
            }
            P.mHalo = Mat.Brilho(Mat.Hex(0xff3355, .5f), Tex.Brilho(), Mat.Mistura.Aditiva, true); P.halo = Sprite(g, P.mHalo, .85f, .85f, "halo"); P.halo.transform.localPosition = new Vector3(0, .45f, 0);
            P.mFogo = Mat.Brilho(Mat.Hex(0xffa040, 0), Tex.Chama(), Mat.Mistura.Aditiva, true); P.fogo = Sprite(g, P.mFogo, .7f, 1, "fogo"); P.fogo.transform.localPosition = new Vector3(0, .4f, 0);
            return P;
        }
        // quem está coletando ou queimando cada reagente (o E segurado do snapshot), e o quanto já foi
        void AtualizarReagentes(Jogo g, float dt)
        {
            float agora = Time.time; var alvo = new Dictionary<int, KeyValuePair<float, string>>();
            foreach (var a in g.actors)
            {
                var h = a.hold; if (h == null || string.IsNullOrEmpty(h.key) || h.max <= 0) continue; bool col = h.key.StartsWith("col"), bur = h.key.StartsWith("burn"); if (!col && !bur) continue;
                int i; if (int.TryParse(h.key.Substring(col ? 3 : 4), out i)) alvo[i] = new KeyValuePair<float, string>((float)(h.t / h.max), col ? "col" : "burn");
            }
            for (int i = 0; i < g.reagents.Count && i < reagentes.Count; i++)
            {
                var R = g.reagents[i]; var P = reagentes[i]; KeyValuePair<float, string> A; bool temA = alvo.TryGetValue(i, out A);
                if (P.tinha && !R.has) FimReagente(P); else if (!P.tinha && R.has) { P.cresce = 0; P.k = 0; }
                P.tinha = R.has; P.g.gameObject.SetActive(R.has); if (!R.has) continue;
                P.cresce = Mathf.Min(1, P.cresce + dt / 1.2f); float esc = .05f + .95f * (1 - Mathf.Pow(1 - P.cresce, 3)); P.g.localScale = new Vector3(esc, esc, esc);
                P.kAlvo = temA ? A.Key : 0; if (temA) P.acao = A.Value; P.k += (P.kAlvo - P.k) * Mathf.Min(1, dt * 10); float k = P.k, col = P.acao == "col" ? k : 0, fogo = P.acao == "burn" ? k : 0;
                Mat.Opacidade(P.mHalo, (.4f + .12f * Mathf.Sin(agora * 3 + P.x)) * (1 - k * .6f)); Mat.Opacidade(P.mFogo, fogo * .95f * (.8f + .2f * Mathf.Sin(agora * 23))); P.fogo.transform.localScale = new Vector3(.5f + fogo * .6f, .7f + fogo * .9f, 1);
                if (fogo > 0 && Random.value < dt * 20 * fogo) Particulas(P.x + Random.Range(-.2f, .2f), .3f, P.z + Random.Range(-.2f, .2f), 1, 0x4a4038, .6f, 1.4f, -1.2f, .3f);
                if (fogo > 0 && Random.value < dt * 3.5f && AoSom != null) AoSom("estalo_fogo", P.x, P.z);
                if (col > .02f && !P.somCol) { P.somCol = true; if (AoSom != null) AoSom("folhas", P.x, P.z); } else if (col <= .02f) P.somCol = false;
                string t = P.tipo;
                if (t == "sal") for (int j = 0; j < P.partes.Count; j++) { var p = P.partes[j]; float s = Mathf.Clamp01(col * 7 - j); p.m.localRotation = Q(p.r.x * (1 + s * .8f), p.r.y, p.r.z * (1 + s * .8f)); p.m.localPosition = new Vector3(p.p.x, p.p.y + s * .06f, p.p.z); p.m.gameObject.SetActive(s < .98f); }
                else if (t == "mandragora") { P.raiz.localPosition = new Vector3(0, -.05f + col * .32f, 0); P.raiz.localRotation = Q(0, 0, Mathf.Sin(agora * 17) * .06f * col); foreach (var p in P.partes) { p.m.localRotation = Q(p.r.x * (1 + col * .9f), p.r.y, p.r.z); p.m.localPosition = new Vector3(p.p.x, p.p.y - col * .05f, p.p.z); } }
                else if (t == "cinza") { P.tampa.localPosition = new Vector3(0, .3f + col * .06f, 0); P.tampa.localRotation = Q(0, 0, col * .5f); if (col > .3f && Random.value < dt * 8) Particulas(P.x, .35f, P.z, 1, 0x8a8680, .3f, 1, -.3f, .15f); }
                else if (t == "cera") for (int j = 0; j < P.chamas.Count; j++) { float apaga = Mathf.Clamp01(col * 5 - j); Mat.Opacidade(P.mChamas[j], (1 - apaga) * (.75f + .25f * Mathf.Sin(agora * 19 + j))); P.chamas[j].localScale = new Vector3(.06f, .1f * (1 - apaga * .7f), 1); }
                else if (t == "flor") foreach (var p in P.partes) { float s = 1 - col * .75f; p.m.localScale = new Vector3(s, s, s); }
                else for (int j = 0; j < P.partes.Count; j++) { var p = P.partes[j]; p.m.localRotation = Q(p.r.x, p.r.y, p.r.z + Mathf.Sin(agora * 21 + j) * .15f * col); var lp = p.m.localPosition; p.m.localPosition = new Vector3(lp.x, .22f + col * .08f, lp.z); }
            }
        }
        // o instante em que o reagente sai do mundo: cada um some do seu jeito (e o fogo deixa fumaça)
        void FimReagente(Reag P)
        {
            float x = P.x, z = P.z; bool fogo = P.acao == "burn"; int cor;
            switch (P.tipo) { case "sal": cor = 0x1a1a22; break; case "mandragora": cor = 0x3a2a18; break; case "cinza": cor = 0x8a8680; break; case "cera": cor = 0x2a2018; break; case "flor": cor = 0xe8e2d6; break; case "corvo": cor = 0x0c0c10; break; default: cor = 0x444444; break; }
            GarantirAtores();
            if (fogo) { Particulas(x, .4f, z, 40, 0xffa040, 2, .9f, -1.4f, .3f); Particulas(x, .4f, z, 30, 0x2a2622, 1, 2.2f, -.8f, .4f); }
            else { Particulas(x, .25f, z, 26, cor, 1.4f, 1.1f, 2.2f, .25f); if (P.tipo == "mandragora" && (camRit.x - x) * (camRit.x - x) + (camRit.z - z) * (camRit.z - z) < 400 && AoSom != null) AoSom("evp", x, z); /* a raiz grita, para quem está perto */ }
            P.k = 0; P.kAlvo = 0;
        }

        // ================= RELÓGIO DE VELAS =================
        // Oito velas na borda do círculo: cada uma é um oitavo do progresso. As de antes da última etapa garantida queimam
        // vermelho-escuro (aquele progresso não volta), as outras laranja (o selo pode apagá-las). Selado: todas douradas.
        // Fenda: derretem, chama roxa. Quem não sabe do ritual vê o que o servidor mostra.
        public class VelaR { public Transform v, c, ch; public Material m; public float h, a, acesa; public Color cor; }
        static readonly Color VELA_ATUAL = Mat.Hex(0xffa050), VELA_GARANTIDA = Mat.Hex(0xc0182a), VELA_CONSAGRADA = Mat.Hex(0x9a5aff), VELA_FAROL = Mat.Hex(0xffe2a0), VELA_FENDA = Mat.Hex(0x8a2aff);
        Material mCeraVela;
        VelaR[] CriarVelas(Transform g)
        {
            if (mCeraVela == null) { mCeraVela = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0xd8ccb0), rugosidade = .6f }); mCeraVela.name = "ceraVela"; }
            var r = new VelaR[8]; float R = (float)CFG.circleR * .94f;
            for (int k = 0; k < 8; k++)
            {
                float a = k / 8f * PI * 2 + PI / 8, h = .34f + (k % 3) * .07f; var v = Grupo(g, "vela"); v.localPosition = new Vector3(Mathf.Cos(a) * R, 0, Mathf.Sin(a) * R);
                var c = Malha(Geo.Cilindro(.045f, .055f, h, 8), mCeraVela, v, "cera", true).transform; c.localPosition = new Vector3(0, h / 2, 0);
                Pos(Malha(Geo.Cilindro(.09f, .11f, .03f, 10), M("wallDark"), v, "base", false), 0, .015f, 0);
                var m = Mat.Brilho(Mat.Hex(0xffa050, 0), Tex.Chama(), Mat.Mistura.Aditiva, true); var ch = Sprite(v, m, .09f, .16f, "chama").transform; ch.localPosition = new Vector3(0, h + .07f, 0);
                r[k] = new VelaR { v = v, c = c, ch = ch, m = m, h = h, a = a, cor = VELA_ATUAL };
            }
            return r;
        }
        void AnimarVelas(Jogo g, Altar A, AltarV O, string st, float dt, float t)
        {
            if (O.velas == null) return; int n = O.velas.Length; float cp = (float)A.cp; bool canal = false;
            foreach (var a in g.actors) if (a.team == "C" && a.st == "alive" && !a.hidden && Mathf.Sqrt((float)((a.x - A.x) * (a.x - A.x) + (a.z - A.z) * (a.z - A.z))) < CFG.circleR) canal = true;
            for (int k = 0; k < n; k++)
            {
                var V2 = O.velas[k]; float alvo = 0, inclina = 0, derrete = 1; var cor = VELA_ATUAL;
                if (st == "awake") { alvo = k == 0 ? .55f : 0; cor = VELA_CONSAGRADA; }
                else if (st == "active") { alvo = Mathf.Clamp01((float)A.prog * n - k); cor = (k + 1f) / n <= cp + 1e-3f ? VELA_GARANTIDA : VELA_ATUAL; if (canal) inclina = .6f; if (A.seal > 0) alvo *= 1 - (float)A.seal * .35f; }
                else if (st == "farol") { alvo = 1; cor = VELA_FAROL; }
                else if (st == "fenda") { alvo = .7f; cor = VELA_FENDA; derrete = .35f; }
                V2.acesa += (alvo - V2.acesa) * Mathf.Min(1, dt * (alvo > V2.acesa ? 3 : 6)); V2.cor = Color.Lerp(V2.cor, cor, Mathf.Min(1, dt * 4));
                float fl = V2.acesa, tr = .85f + .15f * Mathf.Sin(t * (17 + k) + k * 2.1f) * Mathf.Sin(t * 7.3f + k);
                var c = V2.cor; c.a = fl * tr; Mat.Cor(V2.m, c); V2.ch.gameObject.SetActive(c.a > .005f);
                V2.ch.localScale = new Vector3(.09f * (.5f + fl * .5f), .16f * (.4f + fl * .6f) * tr * (st == "farol" ? 1.5f : 1), 1);
                // canalizando: as chamas se deitam para o altar
                var sy = V2.c.localScale.y; V2.ch.localPosition = new Vector3(-Mathf.Cos(V2.a) * inclina * .06f, V2.h * sy + .07f, -Mathf.Sin(V2.a) * inclina * .06f); V2.m.SetFloat("_Giro", inclina * .5f * Mathf.Sin(V2.a + PI / 2));
                sy += (derrete - sy) * Mathf.Min(1, dt * .8f); V2.c.localScale = new Vector3(1, sy, 1); V2.c.localPosition = new Vector3(0, V2.h * sy / 2, 0);
            }
        }

        // ================= EXECUÇÃO =================
        // Quem está executando quem sai do E segurado que o snapshot manda ('exe' + id da vítima). Com isso: a pose de quem
        // executa (Boneco.Animar), o que a vítima vê (PartidaVisual) e o fecho que todos por perto veem quando a morte chega.
        public class Exec { public int por; public string team; public float k, t; }
        public readonly Dictionary<int, Exec> EXE = new Dictionary<int, Exec>();
        public int fxExecucoes;
        public Action<Exec> AoExecutar;   // o jogador executou alguém (a primeira pessoa reage)
        public int euId = -1;
        void LerExecucoes(Jogo g)
        {
            float agora = Time.time;
            foreach (var a in g.actors) { var h = a.hold; if (h == null || string.IsNullOrEmpty(h.key) || !h.key.StartsWith("exe") || h.max <= 0) continue; int id; if (int.TryParse(h.key.Substring(3), out id)) EXE[id] = new Exec { por = a.id, team = a.team, k = (float)(h.t / h.max), t = agora }; }
            var velhos = new List<int>(); foreach (var kv in EXE) if (agora - kv.Value.t > 3) velhos.Add(kv.Key); foreach (var k in velhos) EXE.Remove(k);
        }
        // recente: visto há menos de 2,5 s (um quadro travado não pode engolir o fecho)
        public Exec ExecRecente(int id) { Exec E; return EXE.TryGetValue(id, out E) && Time.time - E.t < 2.5f ? E : null; }
        static float CurvaE(float u) { return 1 - Mathf.Pow(1 - Mathf.Clamp01(u), 3); }
        // Caçador executa Cultista: coluna de luz pálida, o sinal queimado no chão (40 s) e brasas.
        // Culto executa Caçador: garras de sombra saem do chão e puxam o corpo, um círculo de sangue fica no lugar.
        public void FxExecucao(float x, float z, string vitima, Exec E)
        {
            GarantirAtores(); fxExecucoes++; bool caca = vitima == "C", meu = E != null && E.por == euId;
            if (caca)
            {
                var m = Mat.Brilho(Mat.Hex(0xffe6b0, 0), null, Mat.Mistura.Aditiva, false, false); var col = Malha(Geo.Cilindro(.55f, .9f, 16, 20, 1, true), m, atoresRaiz, "coluna da execução", false); col.transform.localPosition = new Vector3(x, 8, z);
                NovoEfeito(col, 1.5f, (o, k) => { Mat.Opacidade(m, Mathf.Sin(Mathf.Min(1, k * 1.4f) * PI) * .8f); float r = 1 - k * .55f; o.transform.localScale = new Vector3(r, 1, r); }, m);
                Particulas(x, .4f, z, 60, 0xffd890, 2.6f, 1.6f, -2.2f, .5f); Particulas(x, .2f, z, 30, 0x3a2a1a, 1, 2.4f, -.6f, .6f);
                if (AoSom != null) { AoSom("sino", x, z); AoSom("selo_quebra", x, z); }
            }
            else
            {
                var garras = Grupo(atoresRaiz, "garras da execução"); garras.localPosition = new Vector3(x, 0, z); var mt = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x140508), rugosidade = .6f, emissao = Mat.Hex(0x3a0610), emissaoK = .8f });
                var cs = new List<Transform>();
                for (int k = 0; k < 7; k++) { float a = k / 7f * PI * 2 + Random.Range(-.2f, .2f); var c = Malha(Geo.Cone(.09f, 1.9f, 6), mt, garras, "garra", true).transform; c.localPosition = new Vector3(Mathf.Cos(a) * .75f, 0, Mathf.Sin(a) * .75f); c.localRotation = Q(Mathf.Sin(a) * .5f, 0, -Mathf.Cos(a) * .5f); cs.Add(c); }
                NovoEfeito(garras.gameObject, 1.8f, (o, k) => { float sobe = k < .25f ? CurvaE(k / .25f) : 1 - CurvaE((k - .25f) / .75f); foreach (var c in cs) { var p = c.localPosition; c.localPosition = new Vector3(p.x, -1 + sobe * 1.25f, p.z); c.localScale = new Vector3(1, .3f + sobe * .9f, 1); } });
                Particulas(x, .5f, z, 50, 0x8a0a1a, 2, 1.4f, -.4f, .6f); Particulas(x, .3f, z, 24, 0x050203, 1.2f, 2, -.3f, .4f);
                if (AoSom != null) { AoSom("canal_quebra", x, z); AoSom("assombro", x, z); AoSom("sussurro", x, z); }
            }
            StartCoroutine(Depois(.15f, () => { if (AoSom != null) AoSom("queda_corpo", x, z); }));
            // a marca que fica no chão: quem passar depois lê que ali alguém foi executado
            var mm = Mat.Brilho(Mat.Hex(caca ? 0x1a1208 : 0x3a0208, 0), Tex.Runa(), Mat.Mistura.Alfa, false, true, 0, false, 2990); var marca = Malha(Geo.Circulo(1.5f, 32), mm, atoresRaiz, "marca da execução", false);
            marca.transform.localPosition = new Vector3(x, .03f, z); marca.transform.localRotation = Q(-PI / 2, 0, 0);
            NovoEfeito(marca, 40, (o, k) => Mat.Opacidade(mm, (k < .02f ? k / .02f : 1 - k * k * k) * .75f), mm);
            if (caca)
            {
                var mb = Mat.Brilho(Mat.Hex(0xffc060, 0), Tex.Runa(), Mat.Mistura.Aditiva, false, false, 0, false, 2995); var br = Malha(Geo.Circulo(1.6f, 32), mb, atoresRaiz, "sinal queimado", false);
                br.transform.localPosition = new Vector3(x, .05f, z); br.transform.localRotation = Q(-PI / 2, 0, 0);
                NovoEfeito(br, 2.4f, (o, k) => { Mat.Opacidade(mb, (1 - k) * .9f); mb.SetFloat("_Giro", k * .6f); }, mb);
            }
            if (meu && AoExecutar != null) AoExecutar(E);
        }
    }
}
