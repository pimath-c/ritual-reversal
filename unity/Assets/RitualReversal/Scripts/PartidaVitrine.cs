// Vitrine das animações (F9 numa partida local; o painel() do arma.js): escolhe a arma e o clipe, a velocidade, a vista
// e a luz, e arrasta a linha do tempo. Não é um menu do jogo: é uma ferramenta para olhar as animações sem jogar uma
// noite. O painel tem as medidas do #vitrineArma (290 px no canto de cima à direita, system-ui de 13 px com linha de 1,35,
// botões de 25 px com 2 px de margem). No navegador o painel só recebe cliques depois de soltar o mouse (Esc); aqui abrir
// já solta o cursor, e a pausa não aparece enquanto ele está aberto. A noite fica parada (como na pausa); a primeira
// pessoa continua animando.
using UnityEngine;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        bool vitrine; string vitArma = "carabina", vitClipe = "recarga", vitVista; bool vitVazia = true, vitEstudio, vitRepetir = true; double vitVel = 1; double? vitT;
        static readonly string[][] VIT_ARMAS = { new[] { "carabina", "Carabina" }, new[] { "revolver", "Revólver" }, new[] { "cetro", "Cetro" } };
        static readonly string[][] VIT_CLIPES = { new[] { "pronta", "Pronta" }, new[] { "sacar", "Sacar" }, new[] { "recarga:1", "Recarga vazia" }, new[] { "recarga:0", "Recarga tática" }, new[] { "inspecao", "Inspeção" },
            new[] { "ajuste", "Ajuste de pegada" }, new[] { "corrida", "Corrida" }, new[] { "selar", "Selar / canalizar" }, new[] { "abaixada", "Interagir" }, new[] { "tiro", "Atirar" } };
        static readonly string[][] VIT_VEL = { new[] { "1", "1×" }, new[] { ".5", "½×" }, new[] { ".25", "¼×" }, new[] { ".1", "1/10×" } };
        static readonly string[][] VIT_VISTAS = { new[] { "", "Jogador" }, new[] { "lado", "Lado" }, new[] { "cima", "Cima" }, new[] { "frente", "Frente" }, new[] { "direita", "Direita" }, new[] { "peca", "Peça (portinhola, tambor)" } };
        static readonly string[][] VIT_LUZ = { new[] { "0", "Do jogo" }, new[] { "1", "Estúdio" } };

        // F9: abre (soltando o cursor) ou fecha (devolvendo a arma, a luz e a câmera ao jogo)
        void AlternarVitrine()
        {
            if (fp == null || fp.arma == null) return;
            vitrine = !vitrine;
            if (vitrine) { vitT = null; AplicarVitrine(true); Travar(false); }
            else FecharVitrine(true);
        }
        void FecharVitrine(bool travar)
        {
            vitrine = false; if (fp != null && fp.arma != null) { fp.arma.Forcar(null); fp.arma.LuzEstudio(false); fp.arma.Vista(cam, null, fp.fovBase); }
            if (travar && g != null && g.phase == "play") Travar(true);
        }
        // o aplicarVitrine do arma.js: reiniciar troca o clipe; senão só muda as opções do que está tocando
        void AplicarVitrine(bool reiniciar)
        {
            if (fp == null || fp.arma == null) return;
            var o = new ArmaFP.Forca { arma = vitArma, clipe = vitClipe, vazia = vitVazia, n = 3, carga = vitVazia ? 0 : 4, vel = vitVel, vista = vitVista, estudio = vitEstudio, repetir = vitRepetir, t = vitT, fervor = 100 };
            var f = fp.arma.forca;
            if (reiniciar || f == null) fp.arma.Forcar(o);
            else { f.arma = o.arma; f.clipe = o.clipe; f.vazia = o.vazia; f.n = o.n; f.carga = o.carga; f.vel = o.vel; f.vista = o.vista; f.estudio = o.estudio; f.repetir = o.repetir; f.t = o.t; f.fervor = o.fervor; }
        }
        void AtirarVitrine() { fp.arma.DisparoForcado(vitArma); if (fp.arma.EstadoAtual != "pronta") { vitClipe = "pronta"; AplicarVitrine(true); } }
        // depois da câmera do quadro (como o camVista do arma.js): a vista escolhida e a luz de estúdio
        void CameraVitrine()
        {
            if (!vitrine || fp == null || fp.arma == null) return;
            fp.arma.LuzEstudio(vitEstudio); cam.fieldOfView = fp.arma.Vista(cam, vitVista, fp.fovBase);
        }

        void PainelVitrine()
        {
            if (fp == null || fp.arma == null) return;
            var cor = Hx("#eadfca"); var stR = Css("sys", 13, Hx("#c9a36a")); float lh = 13 * 1.35f;
            float w = 290, x0 = W - 12 - w, y0 = 12, cx = x0 + 13, cw = w - 26;
            // a altura: o título, cinco grupos e a linha do tempo (medidos antes de desenhar a placa)
            medindo = true; float h = CorpoVitrine(cx, y0 + 13, cw, cor, stR, lh) - y0 + 10 + 1; medindo = false;
            if (sPlacaVit == null) { sPlacaVit = new GUIStyle { border = new RectOffset(9, 9, 9, 9) }; sPlacaVit.normal.background = TexArredondada(new Color(12 / 255f, 9 / 255f, 14 / 255f, .9f), Hx("#6b4a2a"), 8); }
            GUI.Box(new Rect(x0, y0, w, h), GUIContent.none, sPlacaVit);
            CorpoVitrine(cx, y0 + 13, cw, cor, stR, lh);
        }
        GUIStyle sPlacaVit;
        // devolve o y do fim do conteúdo
        float CorpoVitrine(float x, float y, float w, Color cor, GUIStyle stR, float lh)
        {
            // <b style="font-size:14px">: a linha cresce com a meia entrelinha do b (14 × 1,35)
            var stB = Css("sys", 14, cor, TextAnchor.UpperLeft, FontStyle.Bold); float lhB = 14 * 1.35f, bas = y + Mathf.Floor((lhB - LNsis(14)) / 2) + Ar(14 * subSis);
            if (!medindo) { NaBase(x, bas, w, "Vitrine das animações", stB); NaBase(x + Larg("Vitrine das animações", stB) + LargEspaco(Css("sys", 13, cor)), bas, w, "(F9 fecha)", Css("sys", 13, new Color(cor.r, cor.g, cor.b, .6f))); }
            y += lhB;
            y = GrupoVitrine(x, y, w, "Arma", VIT_ARMAS, stR, lh, v => { vitArma = v; if (v == "cetro" && vitClipe == "recarga") vitClipe = "pronta"; AplicarVitrine(true); });
            y = GrupoVitrine(x, y, w, "Clipe", VIT_CLIPES, stR, lh, v =>
            {
                if (v == "tiro") { AtirarVitrine(); return; }
                var p = v.Split(':'); string c = p[0]; vitClipe = c == "selar" && vitArma == "cetro" ? "canalizar" : c; if (p.Length > 1) vitVazia = p[1] == "1";
                if (c == "recarga" && vitArma == "cetro") vitClipe = "inspecao"; vitT = null; AplicarVitrine(true);
            });
            y = GrupoVitrine(x, y, w, "Velocidade", VIT_VEL, stR, lh, v => { vitVel = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); AplicarVitrine(false); });
            y = GrupoVitrine(x, y, w, "Vista", VIT_VISTAS, stR, lh, v => { vitVista = v == "" ? null : v; AplicarVitrine(false); });
            y = GrupoVitrine(x, y, w, "Luz", VIT_LUZ, stR, lh, v => { vitEstudio = v == "1"; AplicarVitrine(false); });
            // a linha do tempo: o rótulo com o tempo do clipe, o range (22 px, margem 2) e a fileira do tocar com o repetir
            y += 8; double t = fp.arma.TempoClipe, dur = fp.arma.DuracaoClipe;
            if (!medindo)
            {
                Linha(x, y, w, lh, "Linha do tempo", stR);
                if (dur > 0) Linha(x + Larg("Linha do tempo", stR) + LargEspaco(stR), y, w, lh, t.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " / " + dur.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s", Css("sys", 13, cor));
            }
            y += Mathf.Floor(lh * 64) / 64 + 3;
            var rr = new Rect(x + 2, y + 2, w, 22); float f = vitT.HasValue ? (float)vitT.Value : dur > 0 ? Mathf.Clamp01((float)(t / dur)) : 0;
            if (!medindo)
            {
                Range(rr, f);
                var c0 = GUI.color; GUI.color = new Color(1, 1, 1, 0); float nv = GUI.HorizontalSlider(rr, f * 1000, 0, 1000); GUI.color = c0;
                if (Mathf.Abs(nv - f * 1000) > .5f) { vitT = nv / 1000; AplicarVitrine(false); }
            }
            y += 28.6f;
            float hb = AlturaBotao("vitPlay"), wb = LarguraBotao("▶ tocar", "vitPlay");
            if (BotaoCss(new Rect(x + 2, y + 2, wb, hb), "▶ tocar", "vitPlay")) { vitT = null; if (fp.arma.forca != null) fp.arma.forca.t = null; AplicarVitrine(true); }
            if (!medindo)
            {
                // <label style="margin-left:8px"><input type=checkbox> repetir</label>: a caixinha azul do Chromium (13 px, margem 3 3 3 4)
                float xc = x + 2 + wb + 2 + 8 + 4, yc = y + 6; var rc = new Rect(xc, yc, 13, 13);
                if (vitRepetir) { CaixaRedonda(rc, Hx("#0075ff"), Hx("#0075ff"), 2); Tracos.Seg(new Vector2(xc + 3, yc + 6.5f), new Vector2(xc + 5.5f, yc + 9), 1.6f, Color.white); Tracos.Seg(new Vector2(xc + 5.5f, yc + 9), new Vector2(xc + 10, yc + 3.5f), 1.6f, Color.white); }
                else CaixaRedonda(rc, Color.white, Hx("#767676"), 2);
                // o texto na linha de base do botão (margem 2, borda 1, padding 4, a subida da fonte)
                var stC = Css("sys", 13, cor); float xt = xc + 13 + 3 + LargEspaco(stC); NaBase(xt, y + 2 + 5 + Ar(13 * subSis), 100, "repetir", stC);
                if (GUI.Button(new Rect(xc - 4, y, xt + Larg("repetir", stC) - xc + 4, hb + 4), "", GUIStyle.none)) { vitRepetir = !vitRepetir; AplicarVitrine(false); }
            }
            return y + hb + 4;
        }
        // um rótulo em ouro (margem 8 em cima, 3 embaixo) e os botões em linha, quebrando na largura (cada um com 2 px de margem)
        float GrupoVitrine(float x, float y, float w, string rotulo, string[][] itens, GUIStyle stR, float lh, System.Action<string> clica)
        {
            y += 8; if (!medindo) Linha(x, y, w, lh, rotulo, stR); y += Mathf.Floor(lh * 64) / 64 + 3;
            float xx = x, hb = AlturaBotao("vit"), lin = hb + 4;
            foreach (var it in itens)
            {
                float bw = LarguraBotao(it[1], "vit");
                if (xx > x && xx + bw + 4 > x + w) { xx = x; y += lin; }
                if (BotaoCss(new Rect(xx + 2, y + 2, bw, hb), it[1], "vit")) clica(it[0]);
                xx += bw + 4;
            }
            return y + lin;
        }
        // o input range do Chromium com accent-color #d2ad62: trilho de 8 px (borda #858585, fundo #3b3b3b, o feito em ouro)
        // e o polegar de 16 px
        void Range(Rect r, float f)
        {
            float cy = r.y + r.height / 2, xi = r.x + 8, xf = r.xMax - 8, xp = Mathf.Lerp(xi, xf, Mathf.Clamp01(f)); var ouro = Hx("#d2ad62");
            var tr = new Rect(r.x + 1, cy - 4, r.width - 2, 8);
            CaixaRedonda(tr, Hx("#3b3b3b"), Hx("#858585"), 4); CaixaRedonda(new Rect(tr.x, tr.y, Mathf.Max(8, xp - tr.x), 8), ouro, ouro, 4);
            Tracos.Disco(xp, cy, 8, ouro);
        }
        // um retângulo de cantos redondos em nove partes (a textura do TexArredondada esticada só no meio)
        readonly System.Collections.Generic.Dictionary<Texture2D, GUIStyle> caixasRedondas = new System.Collections.Generic.Dictionary<Texture2D, GUIStyle>();
        void CaixaRedonda(Rect r, Color fundo, Color borda, int raio)
        {
            if (Event.current.type != EventType.Repaint) return;
            var t = TexArredondada(fundo, borda, raio); GUIStyle s; if (!caixasRedondas.TryGetValue(t, out s)) { s = new GUIStyle { border = new RectOffset(raio + 1, raio + 1, raio + 1, raio + 1) }; s.normal.background = t; caixasRedondas[t] = s; }
            GUI.Box(r, GUIContent.none, s);
        }
    }
}
