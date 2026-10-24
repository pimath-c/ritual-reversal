// Altares do protótipo (buildAltars / syncAltars): degrau, laje de mármore, encosto com runa, velas, o círculo de
// runas no chão (gira e muda de cor com o estado), o anel dourado do selamento, o feixe de luz do ritual revelado,
// brasas subindo, a luz do altar e as runas que a lanterna do Caçador revela num altar consagrado.
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;
using Random = UnityEngine.Random;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        public class AltarV
        {
            public Transform g, circulo, selo, feixe; public Material mCirculo, mSelo, mFeixe, mEncosto, mRunas, mBrasas; public Light luz;
            public Mesh brasas; public Vector3[] ep; public float[] es; public List<Vector3> ev = new List<Vector3>(); public float opC = .05f, opF, opE, opR, escF = 1;
            // v5.8: oito runas em volta do círculo (acendem, pulsam, tremem sob o selo, convergem no Farol, estouram na Fenda)
            public RunaR[] ru = new RunaR[8]; public string rvSt; public float rvProg, rvSeal, contest; public bool somSelo;
            public VelaR[] velas; // v6: o relógio de velas (MundoReagentes.cs)
            public Transform dial; public Material mDial; public float dialOp, dialFl; // v5.6: o mostrador no chão (Resources/RitualReversalMostrador.shader)
        }
        public class RunaR { public GameObject go; public Material m; public float bx, bz, flash, choque, op; public float? acende, converge, explode; }
        public Action<float> AoTremor; Vector3 camRit;   // empurrão de câmera nos grandes momentos, se o jogador está perto
        public readonly List<AltarV> AV = new List<AltarV>();

        void BuildAltars()
        {
            var pai = Grupo(raiz, "Altares");
            foreach (var d in Mapa.ALTARS)
            {
                float x = (float)d.x, z = (float)d.z, sgn = Mathf.Sign(z == 0 ? 1 : z); var A = new AltarV(); AV.Add(A);
                A.g = Grupo(pai, "Altar " + d.name); A.g.localPosition = new Vector3(x, 0, z);
                var G = M4.T(x, 0, z);
                // partes paradas
                Est(Geo.Caixa(4.4f, .25f, 2.8f, 2), M("wall"), G * M4.T(0, .125f, 0));
                var MA = ModeloGLB.Carregar("altar");   // o altar gerado (ModeloGLB.cs), a frente virada para a nave
                if (MA != null) Est(MA.geo, MA.mat, G * M4.RY(sgn > 0 ? PI : 0));
                else { Est(Geo.Caixa(2.6f, .95f, 1.3f, 2), M("marble"), G * M4.T(0, .725f, 0)); Est(Geo.Caixa(2.9f, .12f, 1.55f, 2), M("marble"), G * M4.T(0, 1.26f, 0)); }
                Est(Geo.Caixa(1, 3.4f, .35f, 2), M("wall"), G * M4.T(0, 1.7f, .95f * sgn));
                for (int k = 0; k < 5; k++) { float h = rand(.15f, .35f); Est(Geo.Cilindro(.04f, .045f, h, 6), M("wax"), G * M4.T(-1 + k * .5f, 1.32f + h / 2, rand(-.4f, .4f)), false); }
                // partes que mudam
                A.mEncosto = Mat.Brilho(Mat.Hex(0x3a2a4a), Tex.Runa()); var bp = Malha(Geo.Plano(.85f, .85f), A.mEncosto, A.g, "runa do encosto", false); Pos(bp, 0, 2.5f, .77f * sgn); if (z > 0) Rot(bp, 0, PI, 0);
                A.mCirculo = Mat.Brilho(Mat.Hex(0x6b3a8a, .05f), Tex.Runa(), Mat.Mistura.Aditiva, false, false); A.circulo = Malha(Geo.Plano((float)CFG.circleR * 2.15f, (float)CFG.circleR * 2.15f), A.mCirculo, A.g, "círculo", false).transform; A.circulo.localPosition = new Vector3(0, .04f, 0); A.circulo.localRotation = Q(-PI / 2, 0, 0);
                A.mSelo = Mat.Brilho(Mat.Hex(0xf3d58e, 0), Tex.Runa(), Mat.Mistura.Aditiva, false, false); A.selo = Malha(Geo.Plano((float)CFG.circleR * 2.15f, (float)CFG.circleR * 2.15f), A.mSelo, A.g, "selo", false).transform; A.selo.localPosition = new Vector3(0, .06f, 0); A.selo.localRotation = Q(-PI / 2, 0, 0);
                A.mFeixe = Mat.Brilho(Mat.Hex(0xe0435e, 0), Tex.Feixe(), Mat.Mistura.Aditiva, false, false); A.feixe = Malha(Geo.Cilindro(.55f, .9f, 40, 16, 1, true), A.mFeixe, A.g, "feixe", false).transform; A.feixe.localPosition = new Vector3(0, 20, 0);
                // mostrador no chão: a borda no raio do círculo, o progresso (com os pontos de controle) e o selo
                { var sh = Shader.Find("RitualReversal/Mostrador"); if (sh != null && sh.isSupported)
                  { float Rc = (float)CFG.circleR, lado = 2 * (Rc + .6f); A.mDial = new Material(sh) { name = "mostrador", renderQueue = 3003 };
                    A.mDial.SetFloat("_R", Rc); A.mDial.SetFloat("_Lado", lado); A.mDial.SetVector("_Cp", new Vector4((float)CFG.cps[0], (float)CFG.cps[1], 0, 0));
                    A.dial = Malha(Geo.Plano(lado, lado), A.mDial, A.g, "mostrador", false).transform; A.dial.localPosition = new Vector3(0, .07f, 0); A.dial.localRotation = Q(-PI / 2, 0, 0); A.dial.gameObject.SetActive(false); } }
                A.luz = LuzPonto(A.g, 0x6b3a8a, 0, 20, 2, "luz do altar"); A.luz.transform.localPosition = new Vector3(0, 2.4f, 0); A.luz.enabled = false;
                // brasas
                int EN = 70; A.ep = new Vector3[EN]; A.es = new float[EN];
                for (int k = 0; k < EN; k++) { float a = rand(0, PI * 2), r = rand(.5f, 3); A.ep[k] = new Vector3(Mathf.Cos(a) * r, rand(0, 4), Mathf.Sin(a) * r); A.es[k] = rand(.6f, 1.8f); }
                A.mBrasas = Mat.Brilho(Mat.Hex(0xff4a66, 0), Tex.Brilho(), Mat.Mistura.Aditiva, true);
                var gb = new Geo(); foreach (var q in A.ep) gb.Juntar(Geo.Sprite(q, .11f, .11f)); A.brasas = gb.ToMesh("brasas"); A.brasas.MarkDynamic();
                var eb = new GameObject("brasas"); eb.transform.SetParent(A.g, false); eb.AddComponent<MeshFilter>().sharedMesh = A.brasas; var rb = eb.AddComponent<MeshRenderer>(); rb.sharedMaterial = A.mBrasas; rb.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // runas espalhadas (a lanterna revela)
                A.mRunas = Mat.Brilho(Mat.Hex(0xf0d8ff, 0), Tex.Runa(), Mat.Mistura.Aditiva, true); var gr = new Geo();
                for (int k = 0; k < 9; k++) { float a = rand(0, PI * 2), r = rand(2, 6.5f); gr.Juntar(Geo.Sprite(new Vector3(Mathf.Cos(a) * r, .08f, Mathf.Sin(a) * r), .5f, .5f)); }
                Malha(gr, A.mRunas, A.g, "runas", false);
                float R = (float)CFG.circleR;
                for (int k = 0; k < 8; k++)
                {
                    float an = k / 8f * PI * 2 + .2f; var mr = Pecas.Aditivo(0xff3a5a, Tex.Runa(), true, 0); var sp = Sprite(A.g, mr, .5f, .5f, "runa do ritual");
                    var ru = new RunaR { go = sp, m = mr, bx = Mathf.Cos(an) * (R + .25f), bz = Mathf.Sin(an) * (R + .25f) }; sp.transform.localPosition = new Vector3(ru.bx, .4f, ru.bz); sp.SetActive(false); A.ru[k] = ru;
                }
                A.velas = CriarVelas(A.g);
            }
        }

        // ---------- o ritual como evento (mesmo desenho do navegador; tudo sai do estado que o servidor manda) ----------
        void Coluna(float x, float z, int cor, float dur, float larg)
        {
            GarantirAtores(); var m = Mat.Brilho(Mat.Hex(cor, 0), Tex.Feixe(), Mat.Mistura.Aditiva, false, false); var go = Malha(Geo.Cilindro(.5f, 1.1f, 16, 18, 1, true), m, atoresRaiz, "coluna", false);
            go.transform.localPosition = new Vector3(x, 8, z);
            NovoEfeito(go, dur, (o, k) => { float s = larg * (.6f + k * .9f); o.transform.localScale = new Vector3(s, .4f + k * .8f, s); Mat.Opacidade(m, (k < .12f ? k / .12f : 1 - (k - .12f) / .88f) * .85f); }, m);
        }
        bool Perto(float x, float z, float r) { return (camRit.x - x) * (camRit.x - x) + (camRit.z - z) * (camRit.z - z) < r * r; }
        void RitualIniciado(int i)
        {
            var A = AV[i]; float x = A.g.localPosition.x, z = A.g.localPosition.z, R = (float)CFG.circleR; for (int k = 0; k < 8; k++) A.ru[k].acende = .15f + k * .1f; // acendem em volta, uma de cada vez
            GarantirAtores(); AnelChao(x, z, R + 1.5f, 0xff3a5a, 1.1f, Tex.Runa()); AnelChao(x, z, 7, 0xff6a7a, .7f); Coluna(x, z, 0xff2a4a, 1.4f, .7f);
            Particulas(x, 1.3f, z, 40, 0xff3a5a, 2.4f, 1.2f, -1.2f, 1.4f); Clarao(x, 2, z, 0xff3050, 5); if (AoSom != null) AoSom("ritual_inicio", x, z); if (AoTremor != null && Perto(x, z, 20)) AoTremor(.6f);
        }
        void RitualMarco(int i, float c)
        {
            var A = AV[i]; float x = A.g.localPosition.x, z = A.g.localPosition.z; foreach (var r in A.ru) r.flash = Mathf.Max(r.flash, .9f);
            AnelChao(x, z, (float)CFG.circleR + .8f + c * 3, 0xff3a5a, .8f, Tex.Runa()); Particulas(x, 1.2f, z, 14 + (int)(c * 30), 0xff4a66, 1.6f, 1.1f, -2, 1.8f); Clarao(x, 1.6f, z, 0xff3050, 2 + c * 4);
            if (AoSom != null) AoSom("ritual_marco", x, z);
        }
        public void RitualSelado(int i)
        {
            if (i < 0 || i >= AV.Count) return; var A = AV[i]; float x = A.g.localPosition.x, z = A.g.localPosition.z;
            foreach (var r in A.ru) { r.converge = 0; r.flash = 1.2f; r.go.SetActive(true); }   // as runas se fecham no altar
            PiscarMostrador(A, 0xf3d58e);
            GarantirAtores(); Coluna(x, z, 0xf3d58e, 1.8f, 1.2f); AnelChao(x, z, 8, 0xf3d58e, 1); AnelChao(x, z, (float)CFG.circleR + 2, 0xffe6a8, 1.3f, Tex.Runa());
            Particulas(x, 1.2f, z, 70, 0xf3d58e, 3.2f, 1.5f, -1.5f, 2.4f); Clarao(x, 2.4f, z, 0xffd08a, 8); if (AoTremor != null && Perto(x, z, 25)) AoTremor(1.4f);
        }
        public void RitualFenda(int i)
        {
            if (i < 0 || i >= AV.Count) return; var A = AV[i]; float x = A.g.localPosition.x, z = A.g.localPosition.z;
            foreach (var r in A.ru) { r.explode = 0; r.flash = 1.4f; r.go.SetActive(true); }    // as runas estouram para fora
            PiscarMostrador(A, 0xff3a5a);
            GarantirAtores(); Coluna(x, z, 0xb01830, 2.2f, 1.5f); AnelChao(x, z, 10, 0xff2a40, 1.2f); AnelChao(x, z, (float)CFG.circleR + 3, 0x9a40ff, 1.5f, Tex.Runa());
            Particulas(x, .6f, z, 80, 0x5a0a14, 2.6f, 2.2f, -1, 2.6f); Particulas(x, 1, z, 40, 0xb050ff, 2, 1.6f, -.8f, 1.6f); Clarao(x, 2, z, 0xff2040, 9); if (AoTremor != null && Perto(x, z, 30)) AoTremor(2);
        }
        public void RitualInterrompido(int i, string tipo)
        {
            if (i < 0 || i >= AV.Count || AV[i].rvSt != "active") return; var A = AV[i]; float x = A.g.localPosition.x, z = A.g.localPosition.z;
            foreach (var r in A.ru) { r.choque = .4f; r.flash = Mathf.Max(r.flash, .6f); }
            PiscarMostrador(A, tipo == "selo" ? 0xff3a5a : 0xf3d58e);
            GarantirAtores(); Particulas(x, 1, z, 26, tipo == "selo" ? 0xf3d58e : 0xff3a5a, 4, .5f, 6, 1); AnelChao(x, z, (float)CFG.circleR + .5f, tipo == "selo" ? 0xffe0a0 : 0xff3a5a, .35f);
        }
        static readonly Color CARMIM_R = Mat.Hex(0xff3a5a), OURO_R = Mat.Hex(0xf3d58e);
        void AnimarRitual(int i, Altar Al, AltarV A, string st, string papel, float dt, float t)
        {
            float P = (float)Al.prog; bool ativo = st == "active"; float x = A.g.localPosition.x, z = A.g.localPosition.z;
            if (ativo && A.rvSt != null && A.rvSt != "active") RitualIniciado(i);           // quem entra no meio do ritual não ganha a fanfarra
            if (ativo && A.rvSt == "active") foreach (float c in new[] { .25f, .5f, .75f }) if (A.rvProg < c && P >= c) RitualMarco(i, c);
            bool sobe = ativo && Al.seal > A.rvSeal + 1e-5f; A.contest = Mathf.Lerp(A.contest, sobe ? 1 : 0, Mathf.Min(1, dt * (sobe ? 8 : 1.5f)));
            if (sobe && !A.somSelo && papel == "H") { A.somSelo = true; if (AoSom != null) AoSom("selando_perto", x, z); } if (A.contest < .05f) A.somSelo = false;
            A.rvSt = st; A.rvProg = ativo ? P : 0; A.rvSeal = ativo ? (float)Al.seal : 0;
            int acesas = ativo ? 2 + Mathf.FloorToInt(P * 6.01f) : st == "farol" ? 8 : 0; float ouro = Mathf.Min(1, A.contest * .6f + (ativo ? (float)Al.seal : 0) * .8f);
            for (int k = 0; k < 8; k++)
            {
                var u = A.ru[k];
                if (u.acende.HasValue) { u.acende -= dt; if (u.acende <= 0) { u.acende = null; u.flash = 1.3f; Particulas(x + u.bx, .5f, z + u.bz, 6, 0xff3a5a, 1, .7f, -1.5f, .8f); } }
                float alvo = k < acesas ? (st == "farol" ? .35f : (.45f + .4f * P) * (.82f + .18f * Mathf.Sin(t * (2.5f + P * 6) + k * 1.7f))) : 0; if (u.acende.HasValue) alvo = 0;
                u.flash = Mathf.Max(0, u.flash - dt * 2.2f); u.choque = Mathf.Max(0, u.choque - dt * 1.2f);
                float ox = u.bx, oz = u.bz, y = .4f + Mathf.Sin(t * 1.6f + k) * .06f * (ativo ? 1 + P : 0), esc = .5f + .2f * P;
                if (u.converge.HasValue) { u.converge += dt; float q = Suave(Mathf.Clamp01(u.converge.Value / .55f)); ox *= 1 - q; oz *= 1 - q; y = .4f + q * .95f; esc = .5f + .4f * q; alvo = 1; if (u.converge > 1.4f) u.converge = null; }
                if (u.explode.HasValue) { u.explode += dt; float q = Mathf.Clamp01(u.explode.Value / .9f); ox *= 1 + q * 2.5f; oz *= 1 + q * 2.5f; y = .4f + q * 2.2f; alvo = 1 - q; if (q >= 1) u.explode = null; }
                float j = A.contest * .09f + u.choque * .35f; if (j > 0) { ox += Random.Range(-j, j); oz += Random.Range(-j, j); y += Random.Range(-j, j) * .5f; }
                u.go.transform.localPosition = new Vector3(ox, y, oz); u.go.transform.localScale = new Vector3(esc, esc, 1);
                u.op = Mathf.Lerp(u.op, Mathf.Clamp(alvo + u.flash, 0, 1.4f), .25f); u.go.SetActive(u.op > .01f);
                var cor = Color.Lerp(CARMIM_R, OURO_R, st == "farol" || u.converge.HasValue ? 1 : ouro); cor.a = Mathf.Min(1, u.op); Mat.Cor(u.m, cor); u.m.SetFloat("_Giro", t * (.4f + P) + k);
            }
            if (ativo)
            {
                float R = (float)CFG.circleR;
                if (Random.value < dt * (3 + P * 16)) { float a = Random.value * PI * 2, r = R * (.4f + Random.value * .7f); Jato(x + Mathf.Cos(a) * r, .15f, z + Mathf.Sin(a) * r, new Vector3(-Mathf.Cos(a) * .3f, 1, -Mathf.Sin(a) * .3f), 1, 0xff4a66, .9f + P, .05f, 1.4f, -.6f); }
                if (A.contest > .3f && Random.value < dt * 16) { float a = Random.value * PI * 2; Jato(x + Mathf.Cos(a) * R, .3f, z + Mathf.Sin(a) * R, new Vector3(-Mathf.Cos(a), .6f, -Mathf.Sin(a)), 2, 0xf3d58e, 2.2f, .3f, .45f, 3); }
            }
        }
        static float Suave(float t) { return t * t * (3 - 2 * t); }
        // EVP (só para quem ouviu): as vozes sobem do altar em espiral
        // investigação: sinal que descarta (o anel apaga em cinza), resíduo de reagente (brasas carmins) e Chamariz desfeito (cinza)
        public void MarcaInvestigacao(int i, string k)
        {
            if (i < 0 || i >= AV.Count) return; GarantirAtores(); float x = AV[i].g.localPosition.x, z = AV[i].g.localPosition.z, R = (float)CFG.circleR;
            if (k == "descartado") AnelChao(x, z, R + .6f, 0x8aa0b8, .9f);
            else if (k == "confirmado") { Particulas(x, 1.3f, z, 30, 0xff3a5a, .9f, 1.6f, -.6f, .8f); AnelChao(x, z, R + 1, 0xff6a7a, .9f); }
            else if (k == "chamariz") { Particulas(x, 1.6f, z, 40, 0x6a6a72, .8f, 2, 1.2f, .2f); AnelChao(x, z, R + .6f, 0x9a9aa8, .8f); }
        }
        public void RevelarEVP(float x, float z) { GarantirAtores(); AnelChao(x, z, 2.4f, 0x9a6aff, 1.1f); for (int k = 0; k < 3; k++) Particulas(x, .4f + k * .5f, z, 12, 0xb08aff, .6f, 1.4f, -1.4f, 1.2f); }

        static Color HexA(int hex, float a) { return Mat.Hex(hex, a); }
        // mesma lógica do syncAltars do protótipo; 'eu' é o jogador (para a lanterna), 'cam' a posição da câmera no protótipo
        public void SyncAltars(Jogo g, string papel, Ator eu, Vector3 cam, float lookYaw, float dt)
        {
            float t = Time.time; camRit = cam;
            for (int i = 0; i < g.altars.Count && i < AV.Count; i++)
            {
                var A = g.altars[i]; var O = AV[i]; string st = A.state;
                { string k = g.know[A.i]; bool ve = k == "desperto" || k == "confirmado" || k == "chamariz" || k == "ativo";
                  if (papel == "H" && st == "active" && !A.localized) st = ve ? "awake" : "dormant"; if (papel == "H" && st == "awake" && !ve) st = "dormant"; } // como o servidor mostra à caça
                float op = .05f, li = 0, beam = 0, emb = 0, spin = .05f; int col = 0x6b3a8a, lc = 0x6b3a8a, bcol = 0xe0435e;
                if (st == "awake") { op = .18f + .06f * Mathf.Sin(t * 2 + i); li = .9f + .2f * Mathf.Sin(t * 3); col = 0x8a4ad0; lc = 0x7a3ab0; if (papel == "C" && !A.chosen) col = 0x555566; }
                if (st == "active") { float pulse = A.prog >= .25 ? (.5f + .5f * Mathf.Sin(t * 1.3f * PI * 2)) : .5f; op = .9f + .1f * pulse; col = 0xff5577; lc = 0xff2040; li = 1.4f + 1.6f * (float)A.prog + pulse * .8f; spin = .4f + (float)A.prog * 1.6f; emb = .9f; if (A.localized) beam = .22f + .1f * pulse; if (A.grande) { beam = .55f + .3f * pulse; bcol = 0xff2a3a; li *= 1.6f; spin *= 1.8f; } }
                if (st == "fenda") { op = .35f; col = 0x6a1a8a; li = .8f + .4f * Mathf.Sin(t * 7) * Random.value; lc = 0x5a1080; beam = .1f; bcol = 0x7a2aa0; emb = .4f; spin = -.2f; }
                if (st == "farol") { op = .7f; col = 0xf3d58e; li = 3.2f; lc = 0xffd08a; beam = .28f; bcol = 0xf3d58e; spin = .08f; }
                O.opC = Mathf.Lerp(O.opC, op, .1f); Mat.Cor(O.mCirculo, HexA(col, O.opC));
                float li0 = O.luz.enabled ? O.luz.intensity / IntensidadeUnity(1, 20, 2) : 0; float liN = Mathf.Lerp(li0, li, .12f);
                O.luz.intensity = IntensidadeUnity(liN, 20, 2); O.luz.enabled = liN > .02f && Qualidade.nivel >= 2; O.luz.color = Mat.Hex(lc);
                O.circulo.localRotation = O.circulo.localRotation * Quaternion.AngleAxis(spin * dt * Mathf.Rad2Deg, Vector3.forward);
                O.opF = Mathf.Lerp(O.opF, beam, .08f); Mat.Cor(O.mFeixe, HexA(bcol, O.opF)); O.feixe.gameObject.SetActive(O.opF > .005f);
                float lg = A.grande && st == "active" ? 2.4f : 1; O.escF = Mathf.Lerp(O.escF, lg, .05f); O.feixe.localScale = new Vector3(O.escF, 1, O.escF);
                Mat.Cor(O.mEncosto, Mat.Hex(st == "active" ? 0xff3050 : st == "farol" ? 0xf3d58e : st == "awake" ? 0x8a4ad0 : 0x2a2030));
                float sv = st == "active" ? (float)A.seal : 0; Mat.Cor(O.mSelo, HexA(0xf3d58e, sv > 0 ? .9f : 0)); float sc = Mathf.Max(.05f, 1 - sv * .95f); O.selo.localScale = new Vector3(sc, sc, 1);
                O.selo.localRotation = O.selo.localRotation * Quaternion.AngleAxis(-dt * 1.5f * Mathf.Rad2Deg, Vector3.forward);
                O.opE = Mathf.Lerp(O.opE, emb, .08f); Mat.Cor(O.mBrasas, HexA(st == "fenda" ? 0x9a4ad0 : 0xff4a66, O.opE));
                if (O.opE > .02f)
                {
                    O.ev.Clear(); for (int k = 0; k < O.ep.Length; k++) { float y = O.ep[k].y + dt * O.es[k] * (st == "active" ? 1 + (float)A.prog * 2 : .5f); if (y > 5) y = 0; O.ep[k].y = y; for (int c = 0; c < 4; c++) O.ev.Add(O.ep[k]); }
                    O.brasas.SetVertices(O.ev); O.brasas.RecalculateBounds();
                }
                float rv = 0;
                if (papel == "H" && eu != null && eu.lantern && !A.decoy && (st == "awake" || st == "active") && g.know[i] != "chamariz")
                {
                    float d = Mathf.Sqrt(((float)A.x - cam.x) * ((float)A.x - cam.x) + ((float)A.z - cam.z) * ((float)A.z - cam.z));
                    if (d < 17) { float fx = -Mathf.Sin(lookYaw), fz = -Mathf.Cos(lookYaw), cos = (((float)A.x - cam.x) * fx + ((float)A.z - cam.z) * fz) / Mathf.Max(d, .1f); rv = Mathf.Clamp01((cos - .8f) * 5) * (1 - d / 17); }
                }
                O.opR = Mathf.Lerp(O.opR, rv * .9f, .2f); Mat.Cor(O.mRunas, HexA(0xf0d8ff, O.opR));
                AnimarRitual(i, A, O, st, papel, dt, t); AnimarVelas(g, A, O, st, dt, t);
                if (O.mDial != null)
                {
                    bool ve2 = st == "active"; O.dialOp = Mathf.Lerp(O.dialOp, ve2 ? 1 : 0, .12f); O.dialFl = Mathf.Max(0, O.dialFl - Mathf.Max(dt, .016f) * 1.4f);
                    O.mDial.SetFloat("_Op", O.dialOp); O.mDial.SetFloat("_Prog", ve2 ? (float)A.prog : 0); O.mDial.SetFloat("_Selo", ve2 ? (float)A.seal : 0); O.mDial.SetFloat("_Fl", O.dialFl);
                    O.dial.gameObject.SetActive(O.dialOp > .01f);
                }
            }
        }
        // a onda do mostrador (Fenda, Farol, interrupção): cor de tela, sem conversão
        void PiscarMostrador(AltarV A, int hex) { if (A.mDial == null) return; A.dialFl = 1; A.mDial.SetVector("_CorF", new Vector4(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1)); }
    }
}
