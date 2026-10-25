// O visual do Asilo Blackwater (client/asilo.js): pisos de ladrilho e de tábua, paredes de reboco descascado com lambri,
// tetos e telhados, portas de correr, lâmpadas penduradas por ala (que apagam com o quadro de força), quadros de força,
// móveis, o terreno (horta, gramado com o coreto, cemitério), a margem e o lago. A colisão, as portas e a luz vêm da
// simulação (Mapa, mapaAsilo do sim.js); aqui só a aparência. Mesmas medidas, cores e ordem de sorteio do navegador.
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        // o que se mexe no asilo: folhas das portas, lâmpadas (por ala) e quadros de força
        class FolhaAsilo { public Transform piv; public float s, x0, z0, lf; }
        class PortaAsilo { public PortaV P; public bool temFace; public List<FolhaAsilo> folhas = new List<FolhaAsilo>(); public float ang = 1; }
        class LampAsilo { public Material lamp, brilho; public Fonte fonte; public float fase; }
        class QuadroAsilo { public QuadroV q; public Transform alav; public Material luz; }
        List<PortaAsilo> portasAsilo; Dictionary<string, List<LampAsilo>> lampAsilo; List<QuadroAsilo> quadrosAsilo;
        public bool Asilo { get { return portasAsilo != null; } }

        const float ALT_ASILO = 4.4f;   // pé-direito: o teto fica a 4,4 m; as paredes da simulação têm 4,5
        static double sAsilo = 4711; static float RA() { sAsilo = (sAsilo * 16807) % 2147483647; return (float)(sAsilo / 2147483647); }   // sorteio próprio (o R() do asilo.js)
        static float SmA(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }

        // ---------------- materiais ----------------
        Dictionary<string, Material> MA;
        void MateriaisAsilo()
        {
            var n = new Ruido(9091);
            var reboco = Pbr.Criar(Tex.Tam(256), (u, v) =>
            {
                float f = n.Fbm(u * 5, v * 5, 5, 5), desc = SmA(.62f, .7f, n.Fbm(u * 3 + 7, v * 3, 3, 4)), mancha = SmA(.5f, .9f, n.Fbm(u * 2, v * 9 + 3, 2, 3)) * (1 - v) * .5f;
                float k = .85f + .18f * (f - .5f) - mancha * .4f; var o = new Pbr.Saida { h = f * .3f - desc * .5f };
                if (desc > .5f) { o.r = 120 * k; o.g = 84 * k; o.b = 64 * k; o.ro = .95f; } else { o.r = 150 * k; o.g = 160 * k; o.b = 140 * k; o.ro = .9f; }   // verde-hospital, descascando no tijolo
                return o;
            }, 1.6f).Texturas();
            var lambri = Pbr.Criar(Tex.Tam(256), (u, v) =>
            {
                int T = 8, tx = Mathf.FloorToInt(u * T), ty = Mathf.FloorToInt(v * T); float lu = u * T - tx, lv = v * T - ty, e = Mathf.Min(Mathf.Min(lu, 1 - lu), Mathf.Min(lv, 1 - lv)), f = n.Fbm(u * 9, v * 9, 9, 3), rachado = SmA(.7f, .75f, n.Fbm(u * 6 + 1, v * 6, 6, 3));
                float junta = e < .05f ? .45f : 1, k = (.78f + .2f * f) * junta * (1 - rachado * .3f);
                return new Pbr.Saida { h = junta < 1 ? -.4f : f * .1f, r = 72 * k, g = 92 * k, b = 84 * k, ro = junta < 1 ? .95f : .35f };   // azulejo verde-escuro
            }, 1.2f).Texturas();
            var ladrilho = Pbr.Criar(Tex.Tam(512), (u, v) =>
            {
                int T = 10, tx = Mathf.FloorToInt(u * T), ty = Mathf.FloorToInt(v * T); float lu = u * T - tx, lv = v * T - ty, e = Mathf.Min(Mathf.Min(lu, 1 - lu), Mathf.Min(lv, 1 - lv)), f = n.Fbm(u * 8, v * 8, 8, 4), suj = SmA(.45f, .85f, n.Fbm(u * 2 + 4, v * 2, 2, 4));
                bool claro = (tx + ty) % 2 == 0; float junta = e < .04f ? .5f : 1, k = (claro ? 1 : .42f) * (.82f + .2f * f) * junta * (1 - suj * .35f);
                return new Pbr.Saida { h = junta < 1 ? -.3f : f * .08f, r = 196 * k, g = 190 * k, b = 172 * k, ro = junta < 1 ? .95f : .4f + suj * .4f };   // xadrez gasto
            }, 1).Texturas();
            var tabua = Pbr.Criar(Tex.Tam(256), (u, v) =>
            {
                int tb = Mathf.FloorToInt(u * 6); float lu = u * 6 - tb, f = n.Fbm(u * 3, v * 24 + tb * 7, 3, 4), junta = lu < .04f ? .5f : 1, k = (.65f + .3f * f) * junta;
                return new Pbr.Saida { h = junta < 1 ? -.3f : f * .2f, r = 82 * k, g = 58 * k, b = 40 * k, ro = .75f };
            }, 1.3f).Texturas();
            var teto = Pbr.Criar(Tex.Tam(256), (u, v) => { float f = n.Fbm(u * 4, v * 4, 4, 5), mofo = SmA(.55f, .8f, n.Fbm(u * 2 + 9, v * 2, 2, 4)), k = .6f + .2f * f - mofo * .35f; return new Pbr.Saida { h = f * .2f, r = 120 * k, g = 120 * k, b = 112 * k, ro = .95f }; }, 1).Texturas();
            var grama = Pbr.Criar(Tex.Tam(256), (u, v) => { float f = n.Fbm(u * 10, v * 10, 10, 5), t = SmA(.5f, .8f, n.Fbm(u * 3 + 2, v * 3, 3, 4)), k = .55f + .3f * f; return new Pbr.Saida { h = f * .3f, r = (46 + 40 * t) * k, g = (62 + 20 * t) * k, b = (34 + 12 * t) * k, ro = 1 }; }, 1.4f).Texturas();
            var terra = Pbr.Criar(Tex.Tam(256), (u, v) => { float f = n.Fbm(u * 8, v * 8, 8, 5), k = .6f + .3f * f; return new Pbr.Saida { h = f * .4f, r = 64 * k, g = 48 * k, b = 34 * k, ro = 1 }; }, 1.6f).Texturas();
            var cascalho = Pbr.Criar(Tex.Tam(256), (u, v) => { float f = n.Fbm(u * 20, v * 20, 20, 4), k = .55f + .4f * f; return new Pbr.Saida { h = f * .6f, r = 96 * k, g = 92 * k, b = 86 * k, ro = 1 }; }, 2.2f).Texturas();
            Func<Pbr, float, bool, Material> pbr = (p, relevoK, dupla) => Mat.Padrao(new Mat.Opcoes { cor = Color.white, mapa = p.mapa, relevo = p.relevo, relevoK = relevoK, duplaFace = dupla });
            MA = new Dictionary<string, Material>
            {
                { "reboco", pbr(reboco, .6f, false) }, { "lambri", pbr(lambri, 1, false) }, { "ladrilho", pbr(ladrilho, .6f, false) }, { "tabua", pbr(tabua, 1, false) },
                { "teto", pbr(teto, 1, true) }, { "telhado", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x2a2622), mapa = tabua.mapa }) },
                { "grama", pbr(grama, .6f, false) }, { "terra", pbr(terra, .6f, false) }, { "cascalho", pbr(cascalho, 1, false) },
                { "ferro", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x2c2e30), metal = .8f, rugosidade = .55f, relevo = Tex.metal.relevo }) },
                { "ferrugem", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x5a3424), metal = .5f, rugosidade = .9f }) },
                { "colchao", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x8e8a78), mapa = Tex.cloth.mapa, relevo = Tex.cloth.relevo }) },
                { "porcelana", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0xd6d2c4), rugosidade = .35f }) },
                { "concreto", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x6a6862), mapa = reboco.mapa }) },
                { "carvao", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x111012), rugosidade = .8f }) },
                { "agua", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x0b1416), metal = .6f, rugosidade = .08f, transparente = true, opacidade = .92f }) },
                { "lago", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x05090b), metal = .75f, rugosidade = .12f }) },
                { "porta", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x7a5c40), mapa = Tex.wood.mapa, relevo = Tex.wood.relevo, rugosidade = .9f }) },
                { "cupula", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x2e3532), metal = .6f, rugosidade = .5f }) },
                { "cupulaDentro", Mat.Brilho(Mat.Hex(0x7a6648), null, Mat.Mistura.Alfa, false, true, 0, true, 2450) },   // o esmalte de dentro, sem luz
                { "visor", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x1a2224), metal = .4f, rugosidade = .2f }) },
                { "planta", Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x2c3a22) }) },
            };
            facetados.Add(MA["carvao"]); facetados.Add(MA["planta"]);
            foreach (var kv in MA) kv.Value.name = "asilo " + kv.Key;
        }

        // piso: plano entre x1..x2, z1..z2 na altura y, textura a cada S metros; teto = o mesmo virado para baixo
        void PisoA(float x1, float x2, float z1, float z2, Material m, float S, float y, bool teto = false)
        {
            var g = Geo.Plano(x2 - x1, z2 - z1).EscalarUV((x2 - x1) / S, (z2 - z1) / S).GirarX(-PI / 2); if (teto) g.GirarX(PI);
            Est(g, m, (x1 + x2) / 2, y, (z1 + z2) / 2, !teto);
        }
        void CxA(float x1, float x2, float y1, float y2, float z1, float z2, Material m, float S = 2, bool sombra = true) { Caixa(x1, x2, y1, y2, z1, z2, m, S, sombra); }
        Material A(string k) { return MA[k]; }

        // ---------------- montagem (no lugar das peças, bancos e da v6 da catedral) ----------------
        void ConstruirAsilo()
        {
            sAsilo = 4711; MateriaisAsilo(); Mapa.Usar(MapaVisual.ID);   // a face livre das portas consulta a colisão do asilo
            var E = MapaVisual.ESPACOS; var W = MapaVisual.WALLS;
            // pisos, tetos e telhados
            foreach (var e in E)
            {
                if (e.interno)
                {
                    Material mat = A("ladrilho"); float S = 4;
                    if (System.Text.RegularExpressions.Regex.IsMatch(e.nome, "Capela|Saguão|Arquivo|Sala de Visitas|Sala de Estar")) { mat = A("tabua"); S = 3; }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(e.nome, "Caldeiras|Lavanderia|Depósito|Rouparia|Oficina|Escada")) { mat = A("concreto"); S = 4; }
                    PisoA(e.x1, e.x2, e.z1, e.z2, mat, S, .01f);
                    PisoA(e.x1, e.x2, e.z1, e.z2, A("teto"), 5, ALT_ASILO, true);
                    CxA(e.x1 - .6f, e.x2 + .6f, ALT_ASILO + .02f, ALT_ASILO + .45f, e.z1 - .6f, e.z2 + .6f, A("telhado"), 4);   // telhado por cima, cobre o topo das paredes
                }
                else
                {
                    bool pier = e.nome.Contains("Píer"); var mat = e.nome.Contains("Horta") ? A("terra") : System.Text.RegularExpressions.Regex.IsMatch(e.nome, "Margem|Trilha|Píer") ? A("cascalho") : A("grama");
                    PisoA(e.x1, e.x2, e.z1, e.z2, pier ? A("tabua") : mat, pier ? 2 : 6, .005f);
                }
            }
            // vãos: chão do vão e, nas passagens internas, verga por cima da abertura
            foreach (var v in MapaVisual.VAOS)
            {
                bool dentro = E.Exists(e => e.interno && v.x1 < e.x2 + .7f && v.x2 > e.x1 - .7f && v.z1 < e.z2 + .7f && v.z2 > e.z1 - .7f);
                PisoA(v.x1, v.x2, v.z1, v.z2, dentro ? A("ladrilho") : A("cascalho"), 4, .012f);
                if (dentro) { float largo = Mathf.Max(v.x2 - v.x1, v.z2 - v.z1); CxA(v.x1, v.x2, largo > 3.5f ? 3.2f : 2.75f, 4.5f, v.z1, v.z2, A("reboco"), 2); }
            }
            // paredes
            foreach (var w in W)
            {
                if (w.kind == "parede")
                {
                    CxA(w.x1, w.x2, 0, w.h, w.z1, w.z2, A("reboco"), 3);
                    CxA(w.x1 - .03f, w.x2 + .03f, 0, 1.25f, w.z1 - .03f, w.z2 + .03f, A("lambri"), 1.2f, false);          // lambri de azulejo até 1,25 m
                    CxA(w.x1 - .05f, w.x2 + .05f, 1.25f, 1.33f, w.z1 - .05f, w.z2 + .05f, A("ferrugem"), 2, false);     // friso
                }
                else if (w.kind == "muro") { CxA(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("wall"), 4); CxA(w.x1 - .12f, w.x2 + .12f, w.h, w.h + .25f, w.z1 - .12f, w.z2 + .12f, M("wallDark"), 4, false); }
            }
            // o lago Blackwater e o píer
            Est(Geo.Plano(600, 260).GirarX(-PI / 2), A("lago"), 0, -.35f, -66 - 130, false);
            Est(Geo.Caixa(220, .4f, .6f), M("wallDark"), 0, -.1f, -66.2f);
            for (float z = -71.5f; z <= -66.5f; z += 1) foreach (float x in new[] { -2.2f, 2.2f }) Est(Geo.Cilindro(.12f, .14f, 1.4f, 6), M("wood"), x, -.5f, z);

            // portas de correr encostadas na parede (asilo.js): aberta, a folha fica ao lado do vão na face da parede; fechada, cobre
            // o vão. A face é a primeira (z1/x1, depois z2/x2) com espaço livre ao longo da folha aberta; sem nenhuma, entra na
            // parede. Batente nas duas faces e trilho de ferro na face da folha.
            portasAsilo = new List<PortaAsilo>(); var gPortas = Grupo(raiz, "Portas do asilo");
            foreach (var P in MapaVisual.PORTAS_DIN)
            {
                bool ax = P.eixo == "x"; float larg = ax ? P.x2 - P.x1 : P.z2 - P.z1; int nf = larg > 3.2f ? 2 : 1; float lf = larg / nf, esp = .1f, alt = Mathf.Min(2.7f, ALT_ASILO - .3f);
                float a1 = ax ? P.x1 : P.z1, a2 = ax ? P.x2 : P.z2, f1 = ax ? P.z1 : P.x1, f2 = ax ? P.z2 : P.x2;
                Func<float, float, Vector2> pt = (a, f) => ax ? new Vector2(a, f) : new Vector2(f, a);
                Func<float, float, bool> faceLivre = (face, n) =>
                {
                    for (int k = 0; k < nf; k++) for (float t = .3f; t < lf - .2f; t += .5f) { float a = k == 0 ? a1 - t : a2 + t; var q = pt(a, face + n * .3f); if (!Mapa.Livre(q.x, q.y, .1)) return false; }
                    return true;
                };
                bool temFace = true; float fc = f1, fn = -1;
                if (!faceLivre(f1, -1)) { if (faceLivre(f2, 1)) { fc = f2; fn = 1; } else temFace = false; }
                float plano = temFace ? fc + fn * (esp / 2 + .03f) : (f1 + f2) / 2;
                Action<float, float, float, float, float, float, Material> cx = (xa, xb, ya, yb, za, zb, m) => CxA(Mathf.Min(xa, xb), Mathf.Max(xa, xb), ya, yb, Mathf.Min(za, zb), Mathf.Max(za, zb), m, 1);
                foreach (var fcN in new[] { new Vector2(f1, -1), new Vector2(f2, 1) })
                {
                    float c = fcN.x + fcN.y * .04f;
                    foreach (float a in new[] { a1, a2 }) { var q = pt(a, c); cx(q.x - (ax ? .08f : .05f), q.x + (ax ? .08f : .05f), 0, alt + .12f, q.y - (ax ? .05f : .08f), q.y + (ax ? .05f : .08f), M("wood")); }
                    { var q1 = pt(a1 - .08f, c - .05f); var q2 = pt(a2 + .08f, c + .05f); cx(q1.x, q2.x, alt, alt + .14f, q1.y, q2.y, M("wood")); }
                }
                if (temFace) { var q1 = pt(a1 - lf - .1f, plano - .02f); var q2 = pt(a2 + (nf == 2 ? lf : 0) + .1f, plano + .02f); cx(q1.x, q2.x, alt + .14f, alt + .2f, q1.y, q2.y, A("ferro")); }
                var D = new PortaAsilo { P = P, temFace = temFace };
                for (int k = 0; k < nf; k++)
                {
                    var piv = Grupo(gPortas, "porta " + P.i); var p0 = pt(k == 0 ? a1 : a2, plano); piv.localPosition = new Vector3(p0.x, 0, p0.y);
                    float s = k == 0 ? 1 : -1;
                    var f = Malha(Geo.Caixa(ax ? lf : esp, alt, ax ? esp : lf, 1.5f), A("porta"), piv, "folha"); f.transform.localPosition = ax ? new Vector3(s * lf / 2, alt / 2, 0) : new Vector3(0, alt / 2, s * lf / 2);
                    foreach (float lado in new[] { -1f, 1f }) { float o = lado * (esp / 2 + .03f); var mac = Malha(Geo.Esfera(.05f, 8, 6), A("ferro"), piv, "maçaneta", false); mac.transform.localPosition = ax ? new Vector3(s * (lf - .15f), 1.05f, o) : new Vector3(o, 1.05f, s * (lf - .15f)); }
                    var vis = Malha(Geo.Caixa(ax ? lf * .45f : esp + .02f, .32f, ax ? esp + .02f : lf * .45f, 1), A("visor"), piv, "visor", false); vis.transform.localPosition = new Vector3(ax ? s * lf / 2 : 0, 1.75f, ax ? 0 : s * lf / 2);
                    var faixa = Malha(Geo.Caixa(ax ? lf : esp + .01f, .1f, ax ? esp + .01f : lf, 1), A("ferrugem"), piv, "chapa", false); faixa.transform.localPosition = new Vector3(ax ? s * lf / 2 : 0, .35f, ax ? 0 : s * lf / 2);
                    D.folhas.Add(new FolhaAsilo { piv = piv, s = s, x0 = piv.localPosition.x, z0 = piv.localPosition.z, lf = lf });
                }
                portasAsilo.Add(D);
            }

            // lâmpadas por ala (cada uma com o seu material, para apagar e tremer sozinha) e quadros de força
            lampAsilo = new Dictionary<string, List<LampAsilo>> { { "oeste", new List<LampAsilo>() }, { "centro", new List<LampAsilo>() }, { "leste", new List<LampAsilo>() } };
            var gLamp = Grupo(raiz, "Lâmpadas do asilo");
            foreach (var c in MapaVisual.CANDLES)
            {
                if (c.tipo != "lampada") continue; float x = c.x, z = c.z;
                Est(Geo.Cilindro(.012f, .012f, .7f, 4), A("ferro"), x, ALT_ASILO - .35f, z, false);
                Est(Geo.Cone(.42f, .28f, 12, 1, true), A("cupula"), x, ALT_ASILO - .78f, z, false);
                Est(Geo.Cone(.41f, .27f, 12, 1, true), A("cupulaDentro"), x, ALT_ASILO - .78f, z, false);
                var ml = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0xfff2d0), rugosidade = .3f, emissao = Mat.Hex(0xffd890), emissaoK = 2.2f }); ml.name = "lâmpada";
                var lamp = Malha(Geo.Esfera(.1f, 10, 8), ml, gLamp, "lâmpada", false); lamp.transform.localPosition = new Vector3(x, ALT_ASILO - .86f, z);
                var mb = Mat.Brilho(Mat.Hex(0xffe0a0, .4f), Tex.Brilho(), Mat.Mistura.Aditiva, true); mb.name = "brilho da lâmpada";
                var br = Sprite(gLamp, mb, .55f, .55f, "brilho"); br.transform.localPosition = new Vector3(x, ALT_ASILO - .9f, z);
                var fonte = new Fonte { x = x, z = z, y = ALT_ASILO - 1.7f, cor = 0xffdca0, i = 1.3f, alc = 13 }; FONTES.Add(fonte);   // abaixo da cúpula, longe do teto
                if (c.setor != null && lampAsilo.ContainsKey(c.setor)) lampAsilo[c.setor].Add(new LampAsilo { lamp = ml, brilho = mb, fonte = fonte, fase = RA() * 10 });
            }
            quadrosAsilo = new List<QuadroAsilo>();
            foreach (var q in MapaVisual.QUADROS)
            {
                var g = Grupo(raiz, "quadro de força " + q.setor); g.localPosition = new Vector3(q.x, 0, q.z);
                Malha(Geo.Caixa(.7f, .9f, .22f, 1), A("ferro"), g, "caixa").transform.localPosition = new Vector3(0, 1.45f, 0);
                var alav = Malha(Geo.Caixa(.06f, .36f, .06f), A("ferrugem"), g, "alavanca", false); alav.transform.localPosition = new Vector3(.2f, 1.5f, .14f);
                var mluz = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x30ff60), emissao = Mat.Hex(0x30ff60), emissaoK = 2 }); mluz.name = "luz do quadro";
                Malha(Geo.Esfera(.05f, 8, 6), mluz, g, "luz", false).transform.localPosition = new Vector3(-.2f, 1.75f, .12f);
                Malha(Geo.Cilindro(.03f, .03f, ALT_ASILO - 1.9f, 5), A("ferro"), g, "cabo", false).transform.localPosition = new Vector3(0, 1.9f + (ALT_ASILO - 1.9f) / 2, 0);
                quadrosAsilo.Add(new QuadroAsilo { q = q, alav = alav.transform, luz = mluz });
            }

            // móveis (a caixa da simulação é a colisão; aqui o que se vê)
            foreach (var w in W) MovelAsilo(w);
            // o teto do coreto: chapéu de ferro sobre as seis colunas, e o piso
            Est(Geo.Cone(6.6f, 2.2f, 6, 1), A("ferro"), 0, 3.4f + 1.1f, 46); CxA(-5.8f, 5.8f, 0, .3f, 40.2f, 51.8f, A("concreto"), 3, false);
            // tanques da hidroterapia: água rasa com borda
            foreach (var q in MapaVisual.AGUA)
            {
                CxA(q.x1, q.x2, .02f, .22f, q.z1, q.z2, A("agua"), 2, false);
                CxA(q.x1 - .2f, q.x2 + .2f, 0, .3f, q.z1 - .2f, q.z1, A("porcelana"), 1, false); CxA(q.x1 - .2f, q.x2 + .2f, 0, .3f, q.z2, q.z2 + .2f, A("porcelana"), 1, false);
                CxA(q.x1 - .2f, q.x1, 0, .3f, q.z1, q.z2, A("porcelana"), 1, false); CxA(q.x2, q.x2 + .2f, 0, .3f, q.z1, q.z2, A("porcelana"), 1, false);
            }
            // grade de ferro em cima dos muros do terreno: lanças a cada 30 cm
            foreach (var w in W)
            {
                if (w.kind != "muro" || w.h >= 3.5f) continue; bool ax = w.x2 - w.x1 > w.z2 - w.z1; float L = ax ? w.x2 - w.x1 : w.z2 - w.z1; if (L > 40) continue;
                for (float t = .15f; t < L; t += .3f) { float x = ax ? w.x1 + t : (w.x1 + w.x2) / 2, z = ax ? (w.z1 + w.z2) / 2 : w.z1 + t; Est(Geo.Cilindro(.015f, .015f, .8f, 4), A("ferro"), x, w.h + .65f, z, false); }
            }
        }

        void MovelAsilo(PecaV w)
        {
            float cx = (w.x1 + w.x2) / 2, cz = (w.z1 + w.z2) / 2;
            switch (w.kind)
            {
                case "leito":
                    {
                        bool ax = w.x2 - w.x1 > w.z2 - w.z1; float L = ax ? w.x2 - w.x1 : w.z2 - w.z1, P = ax ? w.z2 - w.z1 : w.x2 - w.x1;
                        Action<float, float, float, float, float, float, Material> B = (dx, y1, y2, dz, lx, lz, mat) =>
                        { float x = ax ? cx + dx : cx + dz, z = ax ? cz + dz : cz + dx, sx = ax ? lx : lz, sz = ax ? lz : lx; CxA(x - sx / 2, x + sx / 2, y1, y2, z - sz / 2, z + sz / 2, mat, 1); };
                        B(0, .42f, .6f, 0, L - .1f, P - .1f, A("colchao")); B(0, .36f, .42f, 0, L, P, A("ferro"));
                        foreach (float s in new[] { -1f, 1f }) { B(s * (L / 2 - .03f), 0, s < 0 ? 1.05f : .8f, 0, .05f, P, A("ferro")); foreach (float t in new[] { -1f, 1f }) B(s * (L / 2 - .03f), 0, .4f, t * (P / 2 - .03f), .05f, .05f, A("ferro")); }
                        B(-L / 2 + .4f, .6f, .72f, 0, .5f, P * .7f, A("colchao")); break;
                    }
                case "banco":
                    CxA(w.x1, w.x2, .42f, .5f, w.z1, w.z2, M("wood"), 2); CxA(w.x1, w.x2, .5f, w.h, w.z2 - .1f, w.z2, M("wood"), 2);
                    CxA(w.x1 + .1f, w.x1 + .2f, 0, .42f, w.z1, w.z2, M("wood"), 2); CxA(w.x2 - .2f, w.x2 - .1f, 0, .42f, w.z1, w.z2, M("wood"), 2); break;
                case "mesa":
                    CxA(w.x1, w.x2, w.h - .07f, w.h, w.z1, w.z2, M("wood"), 2);
                    foreach (var p in new[] { new Vector2(w.x1 + .1f, w.z1 + .1f), new Vector2(w.x2 - .1f, w.z1 + .1f), new Vector2(w.x1 + .1f, w.z2 - .1f), new Vector2(w.x2 - .1f, w.z2 - .1f) }) CxA(p.x - .05f, p.x + .05f, 0, w.h - .07f, p.y - .05f, p.y + .05f, M("wood"), 1);
                    break;
                case "tanque": CxA(w.x1, w.x2, 0, w.h, w.z1, w.z2, A("concreto"), 2); CxA(w.x1 + .12f, w.x2 - .12f, w.h - .05f, w.h + .01f, w.z1 + .12f, w.z2 - .12f, A("agua"), 1, false); break;
                case "bancada": CxA(w.x1, w.x2, w.h - .08f, w.h, w.z1, w.z2, M("wood"), 2); CxA(w.x1, w.x2, 0, w.h - .08f, w.z1 + .1f, w.z2 - .1f, M("wood"), 2); break;
                case "maca":
                    CxA(w.x1, w.x2, .8f, .88f, w.z1, w.z2, A("colchao"), 1); CxA(w.x1 + .1f, w.x2 - .1f, .72f, .8f, w.z1 + .1f, w.z2 - .1f, A("ferro"), 1);
                    foreach (var p in new[] { new Vector2(w.x1 + .15f, w.z1 + .15f), new Vector2(w.x2 - .15f, w.z1 + .15f), new Vector2(w.x1 + .15f, w.z2 - .15f), new Vector2(w.x2 - .15f, w.z2 - .15f) }) CxA(p.x - .03f, p.x + .03f, 0, .72f, p.y - .03f, p.y + .03f, A("ferro"), 1);
                    break;
                case "cadeira_eletrica":
                    CxA(cx - .45f, cx + .45f, .45f, .55f, cz - .45f, cz + .45f, M("wood"), 1); CxA(cx - .45f, cx + .45f, .55f, 1.7f, cz + .35f, cz + .45f, M("wood"), 1);
                    CxA(cx - .5f, cx - .4f, .55f, .85f, cz - .45f, cz + .45f, M("wood"), 1); CxA(cx + .4f, cx + .5f, .55f, .85f, cz - .45f, cz + .45f, M("wood"), 1);
                    CxA(cx - .2f, cx + .2f, 1.7f, 1.85f, cz + .2f, cz + .45f, A("ferro"), 1); CxA(cx - .4f, cx + .4f, 0, .45f, cz - .4f, cz + .4f, M("wood"), 1); break;
                case "banheira":
                    {
                        bool ax = w.x2 - w.x1 > w.z2 - w.z1; CxA(w.x1, w.x2, .12f, w.h, w.z1, w.z2, A("porcelana"), 1); CxA(w.x1 + .1f, w.x2 - .1f, w.h - .18f, w.h + .005f, w.z1 + .1f, w.z2 - .1f, A("agua"), 1, false);
                        foreach (float s in new[] { -1f, 1f }) foreach (float t in new[] { -1f, 1f })
                            { float x = ax ? (s < 0 ? w.x1 + .15f : w.x2 - .15f) : cx + t * .3f, z = ax ? cz + t * .3f : (s < 0 ? w.z1 + .15f : w.z2 - .15f); CxA(x - .05f, x + .05f, 0, .12f, z - .05f, z + .05f, A("ferro"), 1); }
                        break;
                    }
                case "caldeira":
                    {
                        float r = Mathf.Min(w.x2 - w.x1, w.z2 - w.z1) / 2;
                        Est(Geo.Cilindro(r, r, w.h - .4f, 16), A("ferrugem"), cx, .4f + (w.h - .4f) / 2, cz);
                        CxA(w.x1, w.x2, 0, .4f, w.z1, w.z2, A("concreto"), 2); Est(Geo.Cilindro(.22f, .22f, ALT_ASILO - w.h, 8), A("ferro"), cx, w.h + (ALT_ASILO - w.h) / 2, cz);
                        foreach (float y in new[] { .9f, 1.9f, 2.9f }) Est(Geo.Toro(r + .02f, .05f, 6, 20), A("ferro"), TRS(cx, y, cz, PI / 2, 0, 0));   // cintas rebitadas
                        var boca = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x200800), emissao = Mat.Hex(0xff5010), emissaoK = .8f }); boca.name = "boca da fornalha";
                        Est(Geo.Circulo(.35f, 12), boca, cx, 1.1f, cz + r + .01f, false);
                        for (int k = -1; k <= 1; k++) Est(Geo.Caixa(.04f, .62f, .04f, 1), A("ferro"), cx + k * .15f, 1.1f, cz + r + .04f, false);   // grade da fornalha
                        FONTES.Add(new Fonte { x = cx, z = cz + r + .6f, y = 1.1f, cor = 0xff6a20, i = 1.8f, alc = 9 }); break;
                    }
                case "carvao":
                    for (int k = 0; k < 7; k++) { float rr = .25f + RA() * .25f, x = w.x1 + RA() * (w.x2 - w.x1), y = .15f + RA() * w.h * .6f, z = w.z1 + RA() * (w.z2 - w.z1); Est(Geo.Dodecaedro(rr), A("carvao"), x, y, z); }
                    break;
                case "balcao": CxA(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("wood"), 2); CxA(w.x1 - .05f, w.x2 + .05f, w.h, w.h + .06f, w.z1 - .08f, w.z2 + .08f, M("wood"), 2); break;
                case "coluna_coreto": Est(Geo.Cilindro(.13f, .16f, w.h, 8), A("ferro"), cx, w.h / 2, cz); break;
                case "canteiro":
                    CxA(w.x1, w.x2, 0, w.h * .6f, w.z1, w.z2, A("terra"), 2);
                    for (float x = w.x1 + .3f; x < w.x2 - .2f; x += .55f) { float h = .4f + RA() * .3f, z = cz + (RA() - .5f) * .4f; Est(Geo.Cone(.16f, h, 5), A("planta"), x, w.h * .6f + .2f, z); }
                    break;
                case "lapide":
                    {
                        CxA(w.x1, w.x2, 0, w.h - .2f, w.z1, w.z2, M("lapide"), 1);
                        Est(Geo.Cilindro((w.x2 - w.x1) / 2, (w.x2 - w.x1) / 2, w.z2 - w.z1, 10, 1, false, 0, PI), M("lapide"), TRS(cx, w.h - .2f, cz, PI / 2, 0, PI / 2));
                        if (RA() < .4f) Est(Geo.Caixa(.06f, .5f, .06f), A("ferro"), cx, w.h + .25f, cz);
                        break;
                    }
            }
        }

        // ---------------- a cada quadro ----------------
        // portas: a folha corre para dentro da parede quando a porta está aberta; energia por ala: lâmpadas apagadas na ala sem
        // luz (com um tremeluzir nos 3 s finais do apagão) e uma ou outra falhando de vez em quando; quadros: alavanca e luz
        void AtualizarAsilo(Jogo g, float dt, float agora)
        {
            if (!Asilo) return;
            foreach (var D in portasAsilo)
            {
                float alvo = g != null && g.portas != null && D.P.i < g.portas.Count && !g.portas[D.P.i] ? 0 : 1; D.ang += (alvo - D.ang) * Mathf.Min(1, dt * 7);
                foreach (var f in D.folhas) { float d = -f.s * (f.lf + (D.temFace ? .06f : -.03f * f.lf)) * D.ang; var p = f.piv.localPosition; if (D.P.eixo == "x") p.x = f.x0 + d; else p.z = f.z0 + d; f.piv.localPosition = p; }
            }
            float ms = agora * 1000;   // o "agora" do navegador é em milissegundos
            foreach (var kv in lampAsilo)
            {
                double fim; float rest = g != null && g.energia != null && g.energia.TryGetValue(kv.Key, out fim) ? (float)Math.Max(0, fim - g.t) : 0; bool apagada = rest > 0, tremendo = apagada && rest < 3;
                foreach (var L in kv.Value)
                {
                    float k = apagada ? (tremendo && Mathf.Sin(ms * .03f + L.fase * 7) > .3f ? .6f : 0) : 1;
                    if (!apagada && Mathf.Sin(ms * .0023f + L.fase) > .995f) k = .35f;
                    EmissaoA(L.lamp, SRGB(0xffd890) * 2.2f * k); Mat.Opacidade(L.brilho, .4f * k); L.fonte.i = 1.3f * k;
                }
            }
            foreach (var qd in quadrosAsilo)
            {
                double fim; bool ap = g != null && g.energia != null && g.energia.TryGetValue(qd.q.setor, out fim) && fim > g.t;
                qd.alav.localRotation = Q(0, 0, ap ? .9f : 0); int cor = ap ? 0xff3020 : 0x30ff60; Mat.Cor(qd.luz, Mat.Hex(cor)); EmissaoA(qd.luz, SRGB(cor) * 2);
            }
        }
        // emissão como a do Mat.Padrao (cor do three em sRGB, convertida)
        static void EmissaoA(Material m, Color c) { m.SetColor("_EmissionColor", Mat.Lin(c)); m.EnableKeyword("_EMISSION"); }
        static Color SRGB(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f); }
    }
}
