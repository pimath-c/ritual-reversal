// A catedral viva do protótipo (buildCatedralViva): nervuras das abóbadas, pilastras e cornijas, arcada cega do
// Claustro, estandartes, lustres de ferro com velas, velas no chão, a cor dos vitrais derramada no piso, tapetes
// gastos, hera nas brechas e os corvos pousados. Nada disso mexe na colisão.
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        public readonly List<Material> pocas = new List<Material>(); // manchas coloridas dos vitrais no chão (o Grande Ritual acende)
        public class Corvo { public float px, py, pz, yaw, x, y, z, vx, vy, vz, t, volta, bico; public string estado = "pousado"; public Transform corpo, asaE, asaD; }
        public readonly List<Corvo> CORVOS = new List<Corvo>();
        class Face { public string tipo, eixo; public float fixo, a, b; public int n; }

        void BuildCatedralViva()
        {
            var CT = MapaVisual.CATEDRAL; var cl = MapaVisual.CLAUSTRO;
            float CTx1 = CT.xMin, CTx2 = CT.xMax, CTz1 = CT.yMin, CTz2 = CT.yMax, clx1 = cl.xMin, clx2 = cl.xMax, clz1 = cl.yMin, clz2 = cl.yMax;
            // ---------- nervuras das abóbadas ----------
            Func<float, float, float, Geo> arco = (w, alto, tubo) => { float s = alto, Ra = (w * w / 4 + s * s) / (2 * s), f = Mathf.Asin(Mathf.Min(1, (w / 2) / Ra)); return Geo.Toro(Ra, tubo, 5, 24, 2 * f).GirarZ(PI / 2 - f).Mover(0, -Ra, 0); };
            Action<float, float, float, bool> nerv = (w, x, z, girar) => Est(arco(w, 4.6f, .26f), M("wallDark"), TRS(x, 14.7f, z, 0, girar ? PI / 2 : 0, 0), false);
            for (float x = CTx1 + 8; x <= CTx2 - 8; x += 10) { nerv(clz1 - CTz1, x, (CTz1 + clz1) / 2, true); nerv(CTz2 - clz2, x, (CTz2 + clz2) / 2, true); }
            for (float z = clz1 + 6; z <= clz2 - 6; z += 10) { nerv(clx1 - CTx1, (CTx1 + clx1) / 2, z, false); nerv(CTx2 - clx2, (CTx2 + clx2) / 2, z, false); }
            Caixa(CTx1, CTx2, 14.35f, 14.75f, (CTz1 + clz1) / 2 - .2f, (CTz1 + clz1) / 2 + .2f, M("wallDark"), 4, false); Caixa(CTx1, CTx2, 14.35f, 14.75f, (CTz2 + clz2) / 2 - .2f, (CTz2 + clz2) / 2 + .2f, M("wallDark"), 4, false);
            Caixa((CTx1 + clx1) / 2 - .2f, (CTx1 + clx1) / 2 + .2f, 14.35f, 14.75f, CTz1, CTz2, M("wallDark"), 4, false); Caixa((CTx2 + clx2) / 2 - .2f, (CTx2 + clx2) / 2 + .2f, 14.35f, 14.75f, CTz1, CTz2, M("wallDark"), 4, false);

            // ---------- faces das paredes ----------
            var faces = new List<Face>(); Func<float, float, bool> quase = (u, v) => Math.Abs(u - v) < .01f;
            foreach (var w in MapaVisual.WALLS)
            {
                if (w.kind != "pedra") continue;
                if (quase(w.h, 14))
                {
                    if (quase(w.z2 - w.z1, 1) && quase(w.z1, CTz1 - 1)) faces.Add(new Face { tipo = "fora", eixo = "x", fixo = CTz1, n = 1, a = w.x1, b = w.x2 });
                    else if (quase(w.z2 - w.z1, 1) && quase(w.z1, CTz2)) faces.Add(new Face { tipo = "fora", eixo = "x", fixo = CTz2, n = -1, a = w.x1, b = w.x2 });
                    else if (quase(w.x2 - w.x1, 1) && quase(w.x1, CTx1 - 1)) faces.Add(new Face { tipo = "fora", eixo = "z", fixo = CTx1, n = 1, a = w.z1, b = w.z2 });
                    else if (quase(w.x2 - w.x1, 1) && quase(w.x1, CTx2)) faces.Add(new Face { tipo = "fora", eixo = "z", fixo = CTx2, n = -1, a = w.z1, b = w.z2 });
                }
                else if (quase(w.h, 9))
                {
                    float cz = (w.z1 + w.z2) / 2, cx = (w.x1 + w.x2) / 2;
                    if (quase(w.z2 - w.z1, 1) && (quase(cz, clz1) || quase(cz, clz2)) && w.x1 >= clx1 - .6f && w.x2 <= clx2 + .6f) { int sg = Math.Sign(cz); faces.Add(new Face { tipo = "corredor", eixo = "x", fixo = cz + sg * .5f, n = sg, a = w.x1, b = w.x2 }); faces.Add(new Face { tipo = "patio", eixo = "x", fixo = cz - sg * .5f, n = -sg, a = w.x1, b = w.x2 }); }
                    else if (quase(w.x2 - w.x1, 1) && (quase(cx, clx1) || quase(cx, clx2)) && w.z1 >= clz1 - .6f && w.z2 <= clz2 + .6f) { int sg = Math.Sign(cx); faces.Add(new Face { tipo = "corredor", eixo = "z", fixo = cx + sg * .5f, n = sg, a = w.z1, b = w.z2 }); faces.Add(new Face { tipo = "patio", eixo = "z", fixo = cx - sg * .5f, n = -sg, a = w.z1, b = w.z2 }); }
                }
            }
            Action<Face, float, float, float, float, float, Material, float> naFace = (F, u1, u2, y1, y2, prof, mat, S) =>
            {
                float f1 = F.fixo, f2 = F.fixo + F.n * prof;
                if (F.eixo == "x") Caixa(u1, u2, y1, y2, Math.Min(f1, f2), Math.Max(f1, f2), mat, S); else Caixa(Math.Min(f1, f2), Math.Max(f1, f2), y1, y2, u1, u2, mat, S);
            };
            Func<Face, float, float, float, Vector3> posFace = (F, u, y, d) => F.eixo == "x" ? new Vector3(u, y, F.fixo + F.n * d) : new Vector3(F.fixo + F.n * d, y, u);
            Func<Face, float> giroFace = F => F.eixo == "x" ? (F.n > 0 ? 0 : PI) : (F.n > 0 ? PI / 2 : -PI / 2);
            var bandeiras = new List<object[]>(); var velasChao = new List<Vector3>();
            foreach (var F in faces)
            {
                float L = F.b - F.a; if (L < 2) continue;
                if (F.tipo == "fora")
                {
                    naFace(F, F.a, F.b, 10, 10.4f, .32f, M("wallDark"), 4); naFace(F, F.a, F.b, 13.3f, 13.6f, .26f, M("wallDark"), 4); naFace(F, F.a, F.b, 0, .6f, .22f, M("wallDark"), 4);
                    float passo = F.eixo == "x" ? 16 : 8, base_ = F.eixo == "x" ? -90 : -52;
                    for (float u = base_; u <= (F.eixo == "x" ? 90 : 52); u += passo)
                    {
                        if (u < F.a + .9f || u > F.b - .9f) continue;
                        naFace(F, u - .55f, u + .55f, 0, 13.3f, .34f, M("wall"), 4); naFace(F, u - .75f, u + .75f, 0, .9f, .42f, M("wallDark"), 2); naFace(F, u - .7f, u + .7f, 9.4f, 10, .42f, M("wallDark"), 2);
                        if (((int)Math.Round((u - base_) / passo)) % 2 == 0) velasChao.Add(posFace(F, u + 1.3f, 0, .8f));
                    }
                    if (F.eixo == "z") foreach (float u in new float[] { -48, -32, -16, 16, 32, 48 }) if (u > F.a + 1.2f && u < F.b - 1.2f) bandeiras.Add(new object[] { F, u, 12.4f, 6.2f, 2.1f });
                }
                else if (F.tipo == "corredor")
                {
                    naFace(F, F.a, F.b, 7.4f, 7.75f, .28f, M("wallDark"), 4); naFace(F, F.a, F.b, 0, .5f, .2f, M("wallDark"), 4);
                    for (float u = -52; u <= 52; u += 8)
                    {
                        if (u < F.a + .8f || u > F.b - .8f) continue; naFace(F, u - .45f, u + .45f, 0, 8.9f, .3f, M("wall"), 4);
                        if (((int)Math.Round((u + 52) / 8)) % 2 == 1 && u + 4 < F.b - 1 && u + 4 > F.a + 1) bandeiras.Add(new object[] { F, u + 4, 8.3f, 4.4f, 1.6f });
                    }
                }
                else
                {
                    naFace(F, F.a, F.b, 6.9f, 7.3f, .3f, M("wallDark"), 4); var col = Geo.Cilindro(.28f, .32f, 5.2f, 10);
                    for (float u = -54; u <= 54; u += 5)
                    {
                        if (u < F.a + .5f || u > F.b - .5f) continue; var p = posFace(F, u, 2.6f, .05f); Est(col, M("marble"), p.x, p.y, p.z);
                        var pc = posFace(F, u, 5.3f, .1f); Est(Geo.Caixa(.8f, .3f, .5f, 1), M("wallDark"), TRS(pc.x, pc.y, pc.z, 0, giroFace(F), 0));
                        if (u + 5 <= F.b - .5f) { var pa = posFace(F, u + 2.5f, 5.3f, .12f); Est(Geo.Toro(2.5f, .2f, 5, 14, PI), M("wallDark"), TRS(pa.x, pa.y, pa.z, 0, giroFace(F), 0)); }
                    }
                }
            }
            // estandartes (sem o vento do protótipo: pendurados parados)
            {
                var geo = new Geo(); int k = 0;
                foreach (var B in bandeiras)
                {
                    var F = (Face)B[0]; float u = (float)B[1], topo = (float)B[2], alt = (float)B[3], larg = (float)B[4]; int var_ = k % 2;
                    var g = Geo.Plano(larg, alt, 1, 6); for (int i = 0; i < g.uv.Count; i++) g.uv[i] = new Vector2(g.uv[i].x * .5f + var_ * .5f, g.uv[i].y);
                    var p = posFace(F, u, topo, .4f); geo.Juntar(g.Mover(0, -alt / 2, 0).GirarY(giroFace(F)).Mover(p.x, p.y, p.z));
                    var pb = posFace(F, u, topo + .05f, .4f); Est(Geo.Cilindro(.05f, .05f, larg + .4f, 6), M("metal"), TRS(pb.x, pb.y, pb.z, 0, giroFace(F), PI / 2)); k++;
                }
                if (geo.p.Count > 0) { var mat = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = Tex.Estandarte(), recorte = true, corte = .5f, duplaFace = true, rugosidade = .95f }); mat.name = "estandarte"; Est(geo, mat, M4.I, false); }
            }
            // ---------- lustres com velas ----------
            var chamas = new List<Vector3>();
            float[,] LUSTRES = { { -30, -44 }, { 30, -45 }, { 0, -37 }, { -65, -47 }, { 64, -47 }, { -76, -18 }, { -76, 18 }, { 76, -22 }, { 76, 22 }, { -30, 44 }, { 28, 44 }, { 0, 43 }, { -66, 49 }, { 66, 50 }, { -20, -32 }, { 20, -32 } };
            for (int li = 0; li < LUSTRES.GetLength(0); li++)
            {
                float x = LUSTRES[li, 0], z = LUSTRES[li, 1];
                Est(Geo.Cilindro(.03f, .03f, 6.4f, 4), M("metal"), x, 11.8f, z, false);
                Est(Geo.Toro(1.25f, .06f, 6, 24), M("metal"), TRS(x, 8.5f, z, PI / 2, 0, 0)); Est(Geo.Toro(.7f, .05f, 6, 18), M("metal"), TRS(x, 8.1f, z, PI / 2, 0, 0));
                for (int k = 0; k < 4; k++) Est(Geo.Cilindro(.025f, .025f, 1.4f, 4), M("metal"), TRS(x, 8.5f + .35f, z, 0, k * PI / 4, PI / 2 - .5f), false);
                for (int k = 0; k < 8; k++) { float a = k / 8f * PI * 2, cx = x + Mathf.Cos(a) * 1.25f, cz = z + Mathf.Sin(a) * 1.25f, h = .18f + ((k * 7) % 3) * .06f; Est(Geo.Cilindro(.04f, .045f, h, 6), M("wax"), cx, 8.56f + h / 2, cz, false); chamas.Add(new Vector3(cx, 8.62f + h, cz)); }
                FONTES.Add(new Fonte { x = x, z = z, y = 8, cor = 0xffa050, i = 1.5f, alc = 17 });
            }
            double sd = 606; Func<float> R = () => { sd = (sd * 16807) % 2147483647; return (float)(sd / 2147483647); };
            Action<float, float, int, float> grupoVelas = (x, z, n, raio) =>
            {
                for (int k = 0; k < n; k++) { float a = R() * 6.283f, r = R() * raio, cx = x + Mathf.Cos(a) * r, cz = z + Mathf.Sin(a) * r, h = .12f + R() * .42f, rr = .035f + R() * .03f; Est(Geo.Cilindro(rr, rr * 1.1f, h, 6), M("wax"), cx, h / 2, cz, false); chamas.Add(new Vector3(cx, h + .06f, cz)); }
            };
            foreach (var p in velasChao) grupoVelas(p.x, p.z, 4 + (int)Math.Floor(R() * 4), .45f);
            foreach (var A in Mapa.ALTARS) if (MapaVisual.NaCatedral((float)A.x, (float)A.z)) for (int k = 0; k < 6; k++) { float a = k / 6f * PI * 2 + .3f; grupoVelas((float)A.x + Mathf.Cos(a) * 3.6f, (float)A.z + Mathf.Sin(a) * 3.6f, 3, .3f); }
            { var mat = Mat.Brilho(Mat.Hex(0xffc070), Tex.Chama(), Mat.Mistura.Aditiva, true); mat.name = "chamaVela"; foreach (var c in chamas) SpriteEst(c.x, c.y, c.z, .24f, .24f, mat); }
            // ---------- cor dos vitrais no chão ----------
            {
                var geo = new Geo();
                for (float x = CTx1 + 14; x <= CTx2 - 14; x += 16) foreach (int sz in new[] { -1, 1 })
                    {
                        bool vao = false; foreach (var p in MapaVisual.PORTAIS) if (p.eixo == "x" && Math.Sign(p.fixo) == sz && x > p.a - 3.5f && x < p.b + 3.5f) vao = true; if (vao || (sz > 0 && Math.Abs(x) < 17)) continue;
                        geo.Juntar(Geo.Plano(3.4f, 7.5f).GirarX(-PI / 2).Mover(x, .035f, sz * (CTz2 - 9)));
                    }
                var mt = Mat.Brilho(new Color(1, 1, 1, .2f), Tex.Vitral(false), Mat.Mistura.Aditiva, false, true, 0, false, 2600); mt.name = "pocaVitral"; pocas.Add(mt); Est(geo, mt, M4.I, false);
                var mr = Mat.Brilho(new Color(1, 1, 1, .2f), Tex.Vitral(true), Mat.Mistura.Aditiva, false, true, 0, false, 2600); mr.name = "pocaRosacea"; pocas.Add(mr);
                foreach (int sx in new[] { -1, 1 }) Est(Geo.Circulo(3.4f, 32), mr, TRS(sx * (CTx2 - 10), .035f, 0, -PI / 2, 0, 0), false);
            }
            // ---------- tapetes gastos ----------
            {
                var geo = new Geo();
                Action<float, float, float, float, float> tapete = (x1, z1, x2, z2, larg) =>
                {
                    float L = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1)); var g = Geo.Plano(larg, L); for (int i = 0; i < g.uv.Count; i++) g.uv[i] = new Vector2(g.uv[i].x, g.uv[i].y * L / 4);
                    geo.Juntar(g.GirarX(-PI / 2).GirarY(Mathf.Atan2(x2 - x1, z2 - z1)).Mover((x1 + x2) / 2, .022f, (z1 + z2) / 2));
                };
                tapete(-46, -31, 46, -31, 2.4f); tapete(-44, 32, 44, 32, 2.4f); tapete(-88, -21, -88, 21, 2.4f); tapete(88, -21, 88, 21, 2.4f); tapete(-95, 0, -71, 0, 2.2f); tapete(78, 0, 95, 0, 2.2f); tapete(0, 50.5f, 0, 57.5f, 2.6f);
                var mat = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = Tex.Tapete(), rugosidade = 1 }); mat.name = "tapete"; Est(geo, mat, M4.I, false);
            }
            // ---------- hera ----------
            {
                var geo = new Geo(); Action<float, float, float, float, float> planta = (x, z, ry, alt, larg) => geo.Juntar(Geo.Plano(larg, alt).Mover(0, alt / 2, 0).GirarY(ry).Mover(x, 0, z));
                float[,] lugares = { { 95.9f, -26, -PI / 2 }, { 95.9f, -18, -PI / 2 }, { -26, 57.9f, PI }, { -18, 57.9f, PI }, { 46, -28.6f, 0 }, { 52, -28.6f, 0 }, { 57, -33.6f, 0 }, { 50.6f, -40, PI / 2 }, { -95.9f, 26, PI / 2 }, { -95.9f, 32, PI / 2 }, { -50.6f, -44, PI / 2 } };
                for (int i = 0; i < lugares.GetLength(0); i++) { float alt = 4 + R() * 5, larg = 2.5f + R() * 1.5f; planta(lugares[i, 0], lugares[i, 1], lugares[i, 2], alt, larg); }
                foreach (var F in faces) if (F.tipo == "patio") for (int k = 0; k < 4; k++) { float u = F.a + 1 + R() * (F.b - F.a - 2); var p = posFace(F, u, 0, .06f); float alt = 2 + R() * 4.5f, larg = 1.6f + R() * 1.5f; planta(p.x, p.z, giroFace(F), alt, larg); }
                var mat = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x9ab080), mapa = Tex.Hera(), recorte = true, corte = .45f, duplaFace = true }); mat.name = "hera"; Est(geo, mat, M4.I, false);
            }
            // ---------- corvos ----------
            {
                var tipos = new Dictionary<string, float> { { "tumba", 1.1f }, { "coluna_caida", 1.35f }, { "lapide", 1.1f }, { "estatua", 2.25f }, { "pilar_ruina", 7.8f }, { "poco", 1.1f }, { "ossos", 4.5f }, { "tronco", 1.2f }, { "menir", -1 }, { "mausoleu", 5 } };
                int k = 0; foreach (var w in MapaVisual.WALLS)
                {
                    float ty; if (!tipos.TryGetValue(w.kind, out ty)) continue; if ((k++) % 3 != 0) continue;
                    CORVOS.Add(new Corvo { px = (w.x1 + w.x2) / 2, py = ty < 0 ? w.h : ty, pz = (w.z1 + w.z2) / 2, yaw = R() * 6.28f, t = R() * 5 }); if (CORVOS.Count >= 40) break;
                }
                var corpo = Geo.Unir(new[] { Geo.Esfera(.12f, 8, 6).Escalar(1, .85f, 1.8f).Mover(0, .15f, 0), Geo.Esfera(.075f, 8, 6).Mover(0, .25f, -.17f), Geo.Cone(.024f, .1f, 5).GirarX(-PI / 2).Mover(0, .24f, -.27f),
                    Geo.Caixa(.1f, .02f, .18f).GirarX(.25f).Mover(0, .16f, .24f), Geo.Cilindro(.008f, .008f, .12f, 3).Mover(-.04f, .05f, 0), Geo.Cilindro(.008f, .008f, .12f, 3).Mover(.04f, .05f, 0) });
                var asa = Geo.Plano(.36f, .16f).Mover(.18f, 0, .02f).GirarX(-PI / 2);
                var mat = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x0b0b10), rugosidade = .45f, metal = .1f, duplaFace = true }); mat.name = "corvo";
                var pai = Grupo(raiz, "Corvos"); var mc = corpo.ToMesh("corvo"); var ma = asa.ToMesh("asa");
                foreach (var C in CORVOS)
                {
                    C.x = C.px; C.y = C.py; C.z = C.pz;
                    C.corpo = Grupo(pai, "corvo"); var mf = C.corpo.gameObject.AddComponent<MeshFilter>(); mf.sharedMesh = mc; C.corpo.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
                    foreach (int lado in new[] { 1, -1 })
                    {
                        var a = Grupo(C.corpo, lado > 0 ? "asaD" : "asaE"); a.gameObject.AddComponent<MeshFilter>().sharedMesh = ma; a.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
                        a.localPosition = new Vector3(lado * .06f, .2f, -.02f); if (lado > 0) C.asaD = a; else C.asaE = a;
                    }
                }
            }
        }
    }
}
