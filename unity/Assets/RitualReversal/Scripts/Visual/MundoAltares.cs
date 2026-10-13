// Altares do protótipo (buildAltars / syncAltars): degrau, laje de mármore, encosto com runa, velas, o círculo de
// runas no chão (gira e muda de cor com o estado), o anel dourado do selamento, o feixe de luz do ritual revelado,
// brasas subindo, a luz do altar e as runas que a lanterna do Caçador revela num altar consagrado.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        public class AltarV
        {
            public Transform g, circulo, selo, feixe; public Material mCirculo, mSelo, mFeixe, mEncosto, mRunas, mBrasas; public Light luz;
            public Mesh brasas; public Vector3[] ep; public float[] es; public List<Vector3> ev = new List<Vector3>(); public float opC = .05f, opF, opE, opR, escF = 1;
        }
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
                Est(Geo.Caixa(2.6f, .95f, 1.3f, 2), M("marble"), G * M4.T(0, .725f, 0));
                Est(Geo.Caixa(2.9f, .12f, 1.55f, 2), M("marble"), G * M4.T(0, 1.26f, 0));
                Est(Geo.Caixa(1, 3.4f, .35f, 2), M("wall"), G * M4.T(0, 1.7f, .95f * sgn));
                for (int k = 0; k < 5; k++) { float h = rand(.15f, .35f); Est(Geo.Cilindro(.04f, .045f, h, 6), M("wax"), G * M4.T(-1 + k * .5f, 1.32f + h / 2, rand(-.4f, .4f)), false); }
                // partes que mudam
                A.mEncosto = Mat.Brilho(Mat.Hex(0x3a2a4a), Tex.Runa()); var bp = Malha(Geo.Plano(.85f, .85f), A.mEncosto, A.g, "runa do encosto", false); Pos(bp, 0, 2.5f, .77f * sgn); if (z > 0) Rot(bp, 0, PI, 0);
                A.mCirculo = Mat.Brilho(Mat.Hex(0x6b3a8a, .05f), Tex.Runa(), Mat.Mistura.Aditiva, false, false); A.circulo = Malha(Geo.Plano((float)CFG.circleR * 2.15f, (float)CFG.circleR * 2.15f), A.mCirculo, A.g, "círculo", false).transform; A.circulo.localPosition = new Vector3(0, .04f, 0); A.circulo.localRotation = Q(-PI / 2, 0, 0);
                A.mSelo = Mat.Brilho(Mat.Hex(0xf3d58e, 0), Tex.Runa(), Mat.Mistura.Aditiva, false, false); A.selo = Malha(Geo.Plano((float)CFG.circleR * 2.15f, (float)CFG.circleR * 2.15f), A.mSelo, A.g, "selo", false).transform; A.selo.localPosition = new Vector3(0, .06f, 0); A.selo.localRotation = Q(-PI / 2, 0, 0);
                A.mFeixe = Mat.Brilho(Mat.Hex(0xe0435e, 0), Tex.Feixe(), Mat.Mistura.Aditiva, false, false); A.feixe = Malha(Geo.Cilindro(.55f, .9f, 40, 16, 1, true), A.mFeixe, A.g, "feixe", false).transform; A.feixe.localPosition = new Vector3(0, 20, 0);
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
            }
        }

        static Color HexA(int hex, float a) { return Mat.Hex(hex, a); }
        // mesma lógica do syncAltars do protótipo; 'eu' é o jogador (para a lanterna), 'cam' a posição da câmera no protótipo
        public void SyncAltars(Jogo g, string papel, Ator eu, Vector3 cam, float lookYaw, float dt)
        {
            float t = Time.time;
            for (int i = 0; i < g.altars.Count && i < AV.Count; i++)
            {
                var A = g.altars[i]; var O = AV[i]; string st = A.state;
                if (papel == "H" && st == "awake") { string k = g.know[A.i]; if (!(k == "desperto" || k == "confirmado" || k == "chamariz" || k == "ativo")) st = "dormant"; }
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
            }
        }
    }
}
