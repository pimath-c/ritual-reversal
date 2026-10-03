// A floresta do protótipo (buildFloresta): chão da mata com trilhas e clareiras pintadas, árvores em três formas
// (anciã, alta e morta: tronco torneado e torcido, raízes, galhos, copa e musgo pendurado), samambaias, moitas,
// espinheiros, arcos de pedra, raios de luar nas clareiras, telhado da cabana e as runas do Círculo de Pedras.
// O chão usa a cor pintada como textura base e a textura da mata como detalhe repetido (o three usava cor por vértice).
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        void BuildFloresta()
        {
            var n = new Ruido(9091);
            float Mg = 40, LX = MapaVisual.HX * 2 + Mg * 2, LZ = MapaVisual.HZ * 2 + Mg * 2;
            // ---------- chão: cor pintada (trilha, clareira, mata, fora do muro) numa textura de 2 px por metro ----------
            int TW = Mathf.CeilToInt(LX * 2), TH = Mathf.CeilToInt(LZ * 2); var tr = new float[TW * TH]; var cl = new float[TW * TH];
            Func<int, float> PX = i => -LX / 2 + (i + .5f) * LX / TW; Func<int, float> PZ = j => -LZ / 2 + (j + .5f) * LZ / TH;
            foreach (var T in MapaVisual.TRILHAS) for (int s = 1; s < T.pts.Count; s++)
                {
                    Vector2 A = T.pts[s - 1], B = T.pts[s]; float ext = T.w / 2 + .8f;
                    int i0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(A.x, B.x) - ext + LX / 2) * TW / LX)), i1 = Mathf.Min(TW - 1, Mathf.CeilToInt((Mathf.Max(A.x, B.x) + ext + LX / 2) * TW / LX));
                    int j0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(A.y, B.y) - ext + LZ / 2) * TH / LZ)), j1 = Mathf.Min(TH - 1, Mathf.CeilToInt((Mathf.Max(A.y, B.y) + ext + LZ / 2) * TH / LZ));
                    var AB = B - A; float L2 = AB.sqrMagnitude;
                    for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++)
                        {
                            var P = new Vector2(PX(i), PZ(j)); float t = L2 > 0 ? Mathf.Clamp01(Vector2.Dot(P - A, AB) / L2) : 0; float d = (P - (A + AB * t)).magnitude;
                            float v = Mathf.Clamp01((T.w / 2 + .8f - d) / 1.6f); if (v > tr[j * TW + i]) tr[j * TW + i] = v;
                        }
                }
            foreach (var C in MapaVisual.CLAREIRAS)
            {
                float ext = C.z + 3; int i0 = Mathf.Max(0, Mathf.FloorToInt((C.x - ext + LX / 2) * TW / LX)), i1 = Mathf.Min(TW - 1, Mathf.CeilToInt((C.x + ext + LX / 2) * TW / LX));
                int j0 = Mathf.Max(0, Mathf.FloorToInt((C.y - ext + LZ / 2) * TH / LZ)), j1 = Mathf.Min(TH - 1, Mathf.CeilToInt((C.y + ext + LZ / 2) * TH / LZ));
                for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++) { float d = Mathf.Sqrt((PX(i) - C.x) * (PX(i) - C.x) + (PZ(j) - C.y) * (PZ(j) - C.y)); float v = Mathf.Clamp01((C.z + 3 - d) / 6); if (v > cl[j * TW + i]) cl[j * TW + i] = v; }
            }
            var px = new Color32[TW * TH];
            for (int j = 0; j < TH; j++) for (int i = 0; i < TW; i++)
                {
                    float x = PX(i), z = PZ(j), trv = tr[j * TW + i], clv = cl[j * TW + i];
                    float fora = Mathf.Clamp01(Mathf.Max(Mathf.Abs(x) - MapaVisual.HX, Mathf.Abs(z) - MapaVisual.HZ) / 12), var_ = .85f + .3f * n.Fbm(x * .05f + 50, z * .05f + 50, 64, 3);
                    float r = .62f * var_, g = .66f * var_, b = .6f * var_;
                    r = Mathf.Lerp(r, .95f, clv * .5f); g = Mathf.Lerp(g, 1, clv * .5f); b = Mathf.Lerp(b, .78f, clv * .5f);
                    r = Mathf.Lerp(r, 1.25f, trv); g = Mathf.Lerp(g, 1.02f, trv); b = Mathf.Lerp(b, .78f, trv);
                    r *= 1 - fora * .55f; g *= 1 - fora * .55f; b *= 1 - fora * .5f;
                    // a textura de detalhe (x2) dobra: a cor base vai pela metade. Linha j (z crescente) é gravada de cima para baixo, como no canvas
                    px[j * TW + i] = new Color32((byte)Mathf.Clamp(r * 127.5f, 0, 255), (byte)Mathf.Clamp(g * 127.5f, 0, 255), (byte)Mathf.Clamp(b * 127.5f, 0, 255), 255);
                }
            var pint = Tela.Tex2D(TW, TH, px, true, "chaoPintado"); pint.wrapMode = TextureWrapMode.Clamp;
            var chaoMat = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = pint, rugosidade = 1 }); chaoMat.name = "chaoMata";
            chaoMat.SetTexture("_DetailAlbedoMap", Tex.chao.mapa); chaoMat.SetTextureScale("_DetailAlbedoMap", new Vector2(LX / 7, LZ / 7)); chaoMat.SetFloat("_DetailAlbedoMapScale", 1);
            if (Qualidade.nivel >= 2) { chaoMat.SetTexture("_DetailNormalMap", Tex.chao.relevo); chaoMat.SetTextureScale("_DetailNormalMap", new Vector2(LX / 7, LZ / 7)); chaoMat.SetFloat("_DetailNormalMapScale", .8f); }
            chaoMat.EnableKeyword("_DETAIL_MULX2");
            var chao = Malha(Geo.Plano(LX, LZ), chaoMat, raiz, "Chão da mata", false); Pos(chao, 0, -.04f, 0); Rot(chao, -PI / 2, 0, 0);
            chao.GetComponent<MeshRenderer>().receiveShadows = true;

            // ---------- árvores ----------
            double sd = 5150; Func<float> R = () => { sd = (sd * 16807) % 2147483647; return (float)(sd / 2147483647); };
            float[] RREF = { 1.2f, .7f, .58f }; float[,] RFAIXA = { { .95f, 1.45f }, { .55f, .85f }, { .45f, .7f } };
            var lista = new List<ArvoreV>(MapaVisual.ARVORES);
            int extra = Qualidade.nivel == 0 ? 140 : Qualidade.nivel <= 1 ? 260 : 420;
            for (int t = 0; t < 9000 && lista.Count < MapaVisual.ARVORES.Count + extra; t++)
            {
                float x = (R() * 2 - 1) * (MapaVisual.HX + 32), z = (R() * 2 - 1) * (MapaVisual.HZ + 32);
                if (Math.Abs(x) < MapaVisual.HX + 2 && Math.Abs(z) < MapaVisual.HZ + 2) continue; float u = R(); int tipo = u < .3f ? 0 : u < .8f ? 1 : 2;
                float r = RFAIXA[tipo, 0] + R() * (RFAIXA[tipo, 1] - RFAIXA[tipo, 0]), s = .85f + R() * .3f, rot = R() * 6.28f, inc = (R() - .5f) * .08f;
                lista.Add(new ArvoreV { x = x, z = z, r = r, tipo = tipo, s = s, rot = rot, inc = inc });
            }
            var modelos = Arvores.Modelos();
            var folhaV = new Material[3, 6];
            var cascaM = M("casca");
            var musgoMat = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x9aa888), mapa = Tex.Musgo(), recorte = true, corte = .38f, duplaFace = true }); musgoMat.name = "musgo";
            for (int ti = 0; ti < 3; ti++)
            {
                var T = modelos[ti]; int i = 0;
                foreach (var q in lista)
                {
                    if (q.tipo != ti) continue;
                    float k = q.r / RREF[ti]; var MM = M4.T(q.x, 0, q.z) * M4.Euler(q.inc, q.rot, q.inc * .5f) * M4.S(k, k * q.s, k);
                    Est(T.casca, cascaM, MM); Est(T.musgo, musgoMat, MM, false);
                    if (T.copa != null)
                    {
                        int var_ = i % 6; if (folhaV[ti, var_] == null)
                        {
                            var c = new Color(T.cor.x * (.75f + ((i * 13) % 10) / 18f), T.cor.y * (.8f + ((i * 7) % 10) / 20f), T.cor.z * (.8f + ((i * 29) % 10) / 22f));
                            folhaV[ti, var_] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Lin(c), rugosidade = 1 }); folhaV[ti, var_].name = "copa" + ti + "_" + var_; facetados.Add(folhaV[ti, var_]);
                        }
                        Est(T.copa, folhaV[ti, var_], MM);
                    }
                    i++;
                }
            }
            // ---------- samambaias e montes de musgo ----------
            var samMat = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = Tex.Samambaia(), recorte = true, corte = .4f, duplaFace = true, rugosidade = .9f }); samMat.name = "samambaia";
            var samGeo = new Geo(); for (int k = 0; k < 7; k++) samGeo.Juntar(Geo.Plano(.55f, 1.6f).Mover(0, .8f, 0).GirarX(.75f + (k % 3) * .18f).GirarY(k / 7f * PI * 2));
            var moitaGeo = Geo.Unir(new[] { Arvores.Tufo(0, .18f, 0, .75f, .38f), Arvores.Tufo(.55f, .12f, .3f, .5f, .4f), Arvores.Tufo(-.45f, .1f, -.35f, .45f, .4f) });
            var samV = new Material[6]; var moitaV = new Material[6]; int ia = 0;
            foreach (var q in MapaVisual.ARBUSTOS)
            {
                int var_ = ia % 6; var MM = M4.T(q.x, 0, q.z) * M4.RY(q.rot) * M4.S(q.s, q.s * (q.tipo == 1 ? 1 : .9f), q.s);
                if (q.tipo == 1) { if (moitaV[var_] == null) { moitaV[var_] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Lin(new Color(.11f, .15f * (.85f + ((ia * 11) % 10) / 30f), .08f)) }); moitaV[var_].name = "moita" + var_; facetados.Add(moitaV[var_]); } Est(moitaGeo, moitaV[var_], MM, false); }
                else { if (samV[var_] == null) { samV[var_] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Lin(new Color(.42f + ((ia * 7) % 10) / 60f, .55f + ((ia * 3) % 10) / 50f, .36f)), mapa = Tex.Samambaia(), recorte = true, corte = .4f, duplaFace = true, rugosidade = .9f }); samV[var_].name = "samambaia" + var_; } Est(samGeo, samV[var_], MM, false); }
                ia++;
            }
            // ---------- espinheiros ----------
            {
                var espinhos = new List<Geo>(); for (int k = 0; k < 9; k++) espinhos.Add(Geo.Cone(.035f, 1, 3).Mover(0, .5f, 0).GirarZ(-(.7f + (k % 3) * .3f)).GirarY(k / 9f * PI * 2 + .3f).Mover(0, .5f + (k % 4) * .35f, 0));
                var espGeo = Geo.Unir(new[] { Arvores.Tufo(0, .85f, 0, 1, .8f), Arvores.Tufo(.62f, .55f, .3f, .72f, .85f), Arvores.Tufo(-.5f, .65f, -.3f, .8f, .85f), espinhos[0], espinhos[1], espinhos[2], espinhos[3], espinhos[4] });
                var espV = new Material[6]; int i = 0;
                foreach (var w in MapaVisual.WALLS)
                {
                    if (w.kind != "espinheiro") continue; float x = (w.x1 + w.x2) / 2, z = (w.z1 + w.z2) / 2, r = (w.x2 - w.x1) / 2 / .9f; int var_ = i % 6;
                    if (espV[var_] == null) { espV[var_] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Lin(new Color(.075f + ((i * 3) % 10) / 300f, .09f + ((i * 7) % 10) / 260f, .05f)) }); espV[var_].name = "espinheiro" + var_; facetados.Add(espV[var_]); }
                    Est(espGeo, espV[var_], M4.T(x, 0, z) * M4.RY(w.rot) * M4.S(r, r * (1 + ((i * 7) % 10) / 25f), r), false); i++;
                }
            }
            // ---------- arcos de pedra sobre as trilhas ----------
            for (int k = 0; k < MapaVisual.ARCOS.Count; k++)
            {
                var A = MapaVisual.ARCOS[k]; float x1 = A.x, z1 = A.y, x2 = A.z, z2 = A.w, cx = (x1 + x2) / 2, cz = (z1 + z2) / 2, vao = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1)); bool inteiro = k % 3 != 1;
                Est(Geo.Toro(vao / 2, .42f, 6, 18, inteiro ? PI : PI * .42f), M("pedraVelha"), TRS(cx, 7.3f, cz, 0, -Mathf.Atan2(z2 - z1, x2 - x1), 0));
                if (!inteiro) Est(Geo.Dodecaedro(.7f), M("pedraVelha"), TRS(x2 + (cx - x2) * .3f, .45f, z2 + (cz - z2) * .3f + .8f, 1, 2, .5f));
            }
            // ---------- raios de luar nas clareiras ----------
            {
                var raio = Mat.Brilho(Mat.Hex(0x9fb4e0, .07f), Tex.RaioLuz(), Mat.Mistura.Aditiva, false, false); raio.name = "raioLuar";
                var dir = new Vector3(18, 40, 26).normalized; var qd = Quaternion.FromToRotation(Vector3.up, dir);
                foreach (var C in MapaVisual.CLAREIRAS) for (int k = 0; k < 3; k++)
                    {
                        float a = R() * 6.28f, rr = R() * C.z * .55f, L = 30, larg = 2 + R() * 2.5f;
                        var G = M4Q(new Vector3(C.x + Mathf.Cos(a) * rr, 0, C.y + Mathf.Sin(a) * rr) + dir * L / 2, qd);
                        Est(Geo.Plano(larg, L), raio, G, false); Est(Geo.Plano(larg, L), raio, G * M4.RY(PI / 2), false);
                    }
            }
            // ---------- telhado da cabana do Ermitão ----------
            {
                float x1 = 1e9f, x2 = -1e9f, z1 = 1e9f, z2 = -1e9f; foreach (var w in MapaVisual.WALLS) if (w.kind == "madeira") { x1 = Math.Min(x1, w.x1); x2 = Math.Max(x2, w.x2); z1 = Math.Min(z1, w.z1); z2 = Math.Max(z2, w.z2); }
                if (x1 < x2) { float cx = (x1 + x2) / 2, cz = (z1 + z2) / 2; Est(Geo.Cone(8.2f, 3, 4), M("tabuas"), TRS(cx, 5.1f, cz, 0, PI / 4, 0, 1, 1, .85f)); Est(Geo.Caixa(.8f, 2.2f, .8f, 1), M("wall"), cx - 3, 5.2f, cz + 2.5f); }
            }
            // ---------- runas no Círculo de Pedras ----------
            { var sc = Mapa.SPAWN["C"]; var runa = Mat.Brilho(Mat.Hex(0xa3203a, .22f), Tex.Runa(), Mat.Mistura.Aditiva); runa.name = "runaCirculo"; Est(Geo.Plano(12, 12), runa, TRS((float)sc.x, .03f, (float)sc.z, -PI / 2, 0, 0), false); }
        }

    }
}
