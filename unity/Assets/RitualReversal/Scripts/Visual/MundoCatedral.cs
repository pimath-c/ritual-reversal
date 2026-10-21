// A catedral viva do protótipo (buildCatedralViva, v6): lustres de ferro com velas, velas no chão, estandartes, a cor
// dos vitrais derramada no piso, tapetes gastos, hera nas brechas e os corvos pousados. Nada disso mexe na colisão.
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

        void BuildCatedralViva()
        {
            // v6: a cruz latina no promontório. A arquitetura vem de MundoV6.cs; aqui fica o que dá vida: lustres e velas,
            // estandartes, tapetes gastos nos eixos de passagem, a cor dos vitrais no chão, hera e corvos.
            double sd = 606; Func<float> R = () => { sd = (sd * 16807) % 2147483647; return (float)(sd / 2147483647); };
            var chamas = new List<Vector3>();
            // ---------- lustres de ferro com velas: a nave, o cruzeiro, os braços do transepto, o coro e a Sala do Capítulo ----------
            float[,] LUSTRES = { { 0, -43, 22 }, { 0, -49, 22 }, { 0, -55, 22 }, { 0, -61, 22 }, { 0, -71, 30 }, { -30, -71, 20 }, { -12, -55, 11 }, { 12, -49, 11 }, { 0, -82, 20 }, { -61, -55, 10 }, { -21, -84, 9 }, { 0, -36, 16 } };
            for (int li = 0; li < LUSTRES.GetLength(0); li++)
            {
                float x = LUSTRES[li, 0], z = LUSTRES[li, 1], yc = LUSTRES[li, 2], y = yc - 4, len = Math.Max(2, yc + 3 - y), R1 = yc > 25 ? 2.1f : 1.25f;
                Est(Geo.Cilindro(.03f, .03f, len, 4), M("metal"), x, y + len / 2, z, false);
                Est(Geo.Toro(R1, .06f, 6, 28), M("metal"), TRS(x, y, z, PI / 2, 0, 0), false); Est(Geo.Toro(R1 * .56f, .05f, 6, 18), M("metal"), TRS(x, y - .4f, z, PI / 2, 0, 0), false);
                for (int k = 0; k < 4; k++) Est(Geo.Cilindro(.025f, .025f, R1 * 1.1f, 4), M("metal"), TRS(x, y + .35f, z, 0, k * PI / 4, PI / 2 - .5f), false);
                int n = yc > 25 ? 14 : 8;
                for (int k = 0; k < n; k++) { float a = (float)k / n * PI * 2, cx = x + Mathf.Cos(a) * R1, cz = z + Mathf.Sin(a) * R1, h = .18f + ((k * 7) % 3) * .06f; Est(Geo.Cilindro(.04f, .045f, h, 6), M("wax"), cx, y + .06f + h / 2, cz, false); chamas.Add(new Vector3(cx, y + .12f + h, cz)); }
                FONTES.Add(new Fonte { x = x, z = z, y = y - .5f, cor = 0xffa050, i = yc > 25 ? 2.1f : 1.5f, alc = yc > 25 ? 22 : 16 });
            }
            // velas no chão: aos pés dos pilares da nave, em volta dos altares do promontório e na beira do jubé
            Action<float, float, int, float> grupoVelas = (x, z, n, raio) =>
            {
                for (int k = 0; k < n; k++) { float a = R() * 6.283f, r = R() * raio, cx = x + Mathf.Cos(a) * r, cz = z + Mathf.Sin(a) * r, h = .12f + R() * .42f, rr = .035f + R() * .03f; Est(Geo.Cilindro(rr, rr * 1.1f, h, 6), M("wax"), cx, h / 2, cz, false); chamas.Add(new Vector3(cx, h + .06f, cz)); }
            };
            foreach (var p in MapaVisual.PILLARS) { int n = 4 + (int)Math.Floor(R() * 4); grupoVelas(p.x + (p.x > 0 ? -1.1f : 1.1f), p.y + .9f, n, .4f); }
            foreach (var A in Mapa.ALTARS) if (Mapa.ZoneAt(A.x, A.z) != "Floresta") for (int k = 0; k < 6; k++) { float a = k / 6f * PI * 2 + .3f; grupoVelas((float)A.x + Mathf.Cos(a) * 3.6f, (float)A.z + Mathf.Sin(a) * 3.6f, 3, .3f); }
            foreach (float x in new[] { -10.5f, -7.5f, 7.5f, 10.5f }) grupoVelas(x, -76.6f, 5, .35f);
            { var mat = Mat.Brilho(Mat.Hex(0xffc070), Tex.Chama(), Mat.Mistura.Aditiva, true); mat.name = "chamaVela"; foreach (var c in chamas) SpriteEst(c.x, c.y, c.z, .24f, .24f, mat); }
            // ---------- estandartes pendurados no clerestório e no transepto (sem o vento do protótipo: pendurados parados) ----------
            {
                var geo = new Geo();
                Action<float, float, float, float, float, float, int> ban = (x, z, ry, topo, alt, larg, k) =>
                {
                    var g = Geo.Plano(larg, alt, 1, 6); for (int i = 0; i < g.uv.Count; i++) g.uv[i] = new Vector2(g.uv[i].x * .5f + (k % 2) * .5f, g.uv[i].y);
                    geo.Juntar(g.Mover(0, -alt / 2, 0).GirarY(ry).Mover(x, topo, z));
                    Est(Geo.Cilindro(.05f, .05f, larg + .4f, 6), M("metal"), TRS(x, topo + .05f, z, 0, ry, PI / 2));
                };
                int kb = 0; foreach (float z in new float[] { -43, -49, -55, -61 }) foreach (int s in new[] { -1, 1 }) ban(s * 7.3f, z, s * PI / 2, ALT.lateral + 11, 6.5f, 2.2f, kb++);
                foreach (float x in new float[] { -38, -24 }) ban(x, -64.7f, PI, ALT.transepto - 4, 7, 2.4f, kb++);
                ban(0, -32.8f, PI, ALT.lateral + 2, 5, 2.6f, kb++);
                var mat = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = Tex.Estandarte(), recorte = true, corte = .5f, duplaFace = true, rugosidade = .95f }); mat.name = "estandarte"; Est(geo, mat, M4.I, false);
            }
            // ---------- a cor dos vitrais derramada no chão, sob as janelas das naves laterais ----------
            {
                var geo = new Geo();
                foreach (int s in new[] { -1, 1 }) foreach (float z in new float[] { -37, -43, -49, -55, -61 }) { if ((s > 0 && z == -49) || (s < 0 && z == -55)) continue; geo.Juntar(Geo.Plano(2.6f, 6).GirarX(-PI / 2).Mover(s * 12.6f, .035f, z)); }
                var mt = Mat.Brilho(new Color(1, 1, 1, .18f), Tex.Vitral(false), Mat.Mistura.Aditiva, false, true, 0, false, 2600); mt.name = "pocaVitral"; pocas.Add(mt); Est(geo, mt, M4.I, false);
                var mr = Mat.Brilho(new Color(1, 1, 1, .18f), Tex.Vitral(true), Mat.Mistura.Aditiva, false, true, 0, false, 2600); mr.name = "pocaRosacea"; pocas.Add(mr);
                Est(Geo.Circulo(3.4f, 32), mr, TRS(0, .035f, -37, -PI / 2, 0, 0), false);
            }
            // ---------- tapetes gastos nos eixos de passagem ----------
            {
                var geo = new Geo();
                Action<float, float, float, float, float> tapete = (x1, z1, x2, z2, larg) =>
                {
                    float L = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1)); var g = Geo.Plano(larg, L); for (int i = 0; i < g.uv.Count; i++) g.uv[i] = new Vector2(g.uv[i].x, g.uv[i].y * L / 4);
                    geo.Juntar(g.GirarX(-PI / 2).GirarY(Mathf.Atan2(x2 - x1, z2 - z1)).Mover((x1 + x2) / 2, .022f, (z1 + z2) / 2));
                };
                tapete(0, -33, 0, -62, 2.2f); tapete(-40, -71, 40, -71, 2.2f); tapete(0, -79, 0, -84, 2); tapete(-60, -49, -60, -61, 1.8f);
                var mat = Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = Tex.Tapete(), rugosidade = 1 }); mat.name = "tapete"; Est(geo, mat, M4.I, false);
            }
            // ---------- hera subindo pela capela desabada, pela torre partida, pelo claustro e pelas ruínas do mosteiro ----------
            {
                var geo = new Geo(); Action<float, float, float, float, float> planta = (x, z, ry, alt, larg) => geo.Juntar(Geo.Plano(larg, alt).Mover(0, alt / 2, 0).GirarY(ry).Mover(x, 0, z));
                float[,] lugares = { { 44.6f, -66, PI / 2 }, { 44.6f, -76, PI / 2 }, { 30, -63.4f, 0 }, { 36, -63.4f, 0 }, { 28.6f, -36, PI / 2 }, { 28.6f, -41, PI / 2 }, { -44.6f, -48, -PI / 2 }, { -44.6f, -60, -PI / 2 }, { -28, -43.4f, 0 }, { -16.6f, -62, PI / 2 },
                    { -70.6f, -50, -PI / 2 }, { -51.4f, -60, PI / 2 }, { 16.6f, -50, PI / 2 }, { 30.6f, -86, PI / 2 } };
                for (int i = 0; i < lugares.GetLength(0); i++) { float alt = 4 + R() * 6, larg = 2.5f + R() * 1.5f; planta(lugares[i, 0], lugares[i, 1], lugares[i, 2], alt, larg); }
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
