// As peças das telas do navegador para o IMGUI, desenhadas em retângulos calculados como o CSS faz (sem o GUILayout):
// button.go (degradê carmim com as pontas chanfradas do clip-path), .go.gold, .go.ghost, os botões de opção (.dif, .set
// .opts, .labRow, .repCtl: borda fina, dourada quando escolhidos), o link sublinhado (.lk), o campo (input.txt), a caixinha
// (checkbox com accent dourado), o fundo da tela (.screen e o degradê lateral do título) e a placa do painel (.panel.plate).
// Cada tela mede antes de desenhar (medindo = true: nada é desenhado nem clicado), para centrar e rolar como o navegador.
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        bool medindo;
        static readonly Color BORDA_OP = new Color(58 / 255f, 65 / 255f, 73 / 255f), FUNDO_OP = new Color(14 / 255f, 16 / 255f, 19 / 255f, .7f);

        // ---------- texturas dos botões ----------
        readonly Dictionary<string, GUIStyle> estilosBotao = new Dictionary<string, GUIStyle>();
        static Texture2D TexBotao(Color cima, Color baixo, Color borda, bool chanfro, bool bordaSoEmbaixo = false)
        {
            int Wt = 64, Ht = 44; var t = new Texture2D(Wt, Ht, TextureFormat.RGBA32, false); var p = new Color[Wt * Ht];
            for (int y = 0; y < Ht; y++) for (int x = 0; x < Wt; x++)
                {
                    // y = 0 é a linha de baixo; v de cima para baixo
                    float v = 1 - (y + .5f) / Ht; Color c = Color.Lerp(cima, baixo, v); bool dentro = true;
                    if (chanfro) { float meio = Mathf.Abs(v - .5f) * 2, X = x + .5f, Xd = Wt - x - .5f; dentro = X >= 10 * meio && Xd >= 10 * meio; }
                    bool naBorda = bordaSoEmbaixo ? y == 0 : (y == 0 || y == Ht - 1 || (!chanfro && (x == 0 || x == Wt - 1)));
                    p[y * Wt + x] = !dentro ? new Color(0, 0, 0, 0) : naBorda ? borda : c;
                }
            t.SetPixels(p); t.Apply(false, true); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; return t;
        }
        // retângulo de cantos redondos (border-radius r) com borda de 1 px, para esticar em nove partes (borda r + 1)
        static readonly Dictionary<string, Texture2D> arredondadas = new Dictionary<string, Texture2D>();
        static Texture2D TexArredondada(Color fundo, Color borda, int r)
        {
            string k = ColorUtility.ToHtmlStringRGBA(fundo) + ColorUtility.ToHtmlStringRGBA(borda) + r; Texture2D t; if (arredondadas.TryGetValue(k, out t)) return t;
            int N = 2 * r + 4; t = new Texture2D(N, N, TextureFormat.RGBA32, false); var p = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    // distância (com sinal) ao contorno do retângulo arredondado, no centro do pixel; cobertura de 1 px de largura
                    float cx = Mathf.Clamp(x + .5f, r, N - r), cy = Mathf.Clamp(y + .5f, r, N - r), dx = x + .5f - cx, dy = y + .5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - r;   // < 0 dentro do canto; nas retas, d = -distância à borda
                    if (x + .5f > r && x + .5f < N - r || y + .5f > r && y + .5f < N - r) d = -Mathf.Min(Mathf.Min(x + .5f, N - x - .5f), Mathf.Min(y + .5f, N - y - .5f));
                    float fora = Mathf.Clamp01(d + .5f), naBorda = Mathf.Clamp01(1.5f + d) * (1 - fora);   // a faixa de 1 px junto do contorno
                    float dentro = 1 - fora - naBorda;
                    // cor sem pré-multiplicar: média das duas pesada pelo alfa de cada uma
                    float wa = naBorda * borda.a, wb = dentro * fundo.a, ws = wa + wb;
                    p[y * N + x] = ws > 0 ? new Color((borda.r * wa + fundo.r * wb) / ws, (borda.g * wa + fundo.g * wb) / ws, (borda.b * wa + fundo.b * wb) / ws, ws) : new Color(0, 0, 0, 0);
                }
            t.SetPixels(p); t.Apply(false, true); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; arredondadas[k] = t; return t;
        }
        // tipo: go, gold, ghost, op, opSel, lab, labSel, lk, set, setSel, rep, repOn
        GUIStyle EstiloBotao(string tipo)
        {
            GUIStyle s; if (estilosBotao.TryGetValue(tipo, out s)) return s;
            // "x|mudo": o mesmo botão sem o texto (quem chama escreve por cima); "nada": área clicável sem desenho
            if (tipo.EndsWith("|mudo") || tipo == "nada")
            {
                s = tipo == "nada" ? new GUIStyle(Css("400", 15, BONE)) : new GUIStyle(EstiloBotao(tipo.Substring(0, tipo.Length - 5)));
                if (tipo == "nada") s.normal.background = s.hover.background = s.active.background = s.focused.background = null;
                var cl = new Color(0, 0, 0, 0); s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = cl; estilosBotao[tipo] = s; return s;
            }
            int tam = 20; string peso = "600"; Color txt = Color.white; RectOffset pad; RectOffset borda; Texture2D tex; var al = TextAnchor.MiddleCenter;
            switch (tipo)
            {
                case "go": tex = TexBotao(Hx("#b3263f"), Hx("#6e1224"), Hx("#e0435e"), true); pad = new RectOffset(29, 29, 11, 11); borda = new RectOffset(10, 10, 1, 1); break;
                case "gold": tex = TexBotao(Hx("#b88d3e"), Hx("#6d4f1c"), Hx("#f3d58e"), true); pad = new RectOffset(29, 29, 11, 11); borda = new RectOffset(10, 10, 1, 1); break;
                case "ghost": tex = TexBotao(new Color(20 / 255f, 23 / 255f, 27 / 255f, .85f), new Color(20 / 255f, 23 / 255f, 27 / 255f, .85f), Hx("#4a525c"), true); pad = new RectOffset(29, 29, 11, 11); borda = new RectOffset(10, 10, 1, 1); txt = BONE; break;
                // .loja .item .go: 17 px, padding 8 × 18 (dourado com o preço; fantasma com o motivo do bloqueio)
                case "lojaGold": tex = TexBotao(Hx("#b88d3e"), Hx("#6d4f1c"), Hx("#f3d58e"), true); pad = new RectOffset(19, 19, 9, 9); borda = new RectOffset(10, 10, 1, 1); tam = 17; break;
                case "lojaGhost": tex = TexBotao(new Color(20 / 255f, 23 / 255f, 27 / 255f, .85f), new Color(20 / 255f, 23 / 255f, 27 / 255f, .85f), Hx("#4a525c"), true); pad = new RectOffset(19, 19, 9, 9); borda = new RectOffset(10, 10, 1, 1); txt = BONE; tam = 17; break;
                case "ghostP": tex = TexBotao(new Color(20 / 255f, 23 / 255f, 27 / 255f, .85f), new Color(20 / 255f, 23 / 255f, 27 / 255f, .85f), Hx("#4a525c"), true); pad = new RectOffset(17, 17, 6, 6); borda = new RectOffset(10, 10, 1, 1); txt = BONE; tam = 15; break;
                // .slot: 19 px à esquerda, fundo escuro, borda #3a4149 (a sua vaga, ouro); a do bot com o texto apagado
                case "slot": case "slotBot": case "slotMe": tex = TexBotao(new Color(14 / 255f, 16 / 255f, 19 / 255f, .8f), new Color(14 / 255f, 16 / 255f, 19 / 255f, .8f), tipo == "slotMe" ? GOLD_HI : BORDA_OP, false); pad = new RectOffset(13, 13, 11, 11); borda = new RectOffset(1, 1, 1, 1); tam = 19; peso = "400"; txt = tipo == "slotBot" ? MUDO : BONE; al = TextAnchor.MiddleLeft; break;
                // #arsenal .abas button: fundo quase transparente, borda ouro .35, cantos de 6 (a escolhida em ouro); .skin: cantos de 8
                // a vitrine das animações (o painel do arma.js): fundo #2a1d18, borda #6b4a2a, cantos de 5, padding 4 × 7 (9 no tocar)
                case "vit": case "vitPlay": tex = TexArredondada(Hx("#2a1d18"), Hx("#6b4a2a"), 5); pad = tipo == "vitPlay" ? new RectOffset(10, 10, 5, 5) : new RectOffset(8, 8, 5, 5); borda = new RectOffset(6, 6, 6, 6); tam = 13; peso = "sys"; txt = Hx("#eadfca"); break;
                case "arsAba": case "arsAbaSel": { bool sl = tipo == "arsAbaSel"; tex = TexArredondada(sl ? new Color(210 / 255f, 173 / 255f, 98 / 255f, .18f) : new Color(1, 1, 1, .04f), sl ? GOLD : new Color(210 / 255f, 173 / 255f, 98 / 255f, .35f), 6); pad = new RectOffset(7, 7, 9, 9); borda = new RectOffset(7, 7, 7, 7); tam = 13; peso = "sys"; txt = sl ? GOLD_HI : BONE; break; }
                case "arsSkin": case "arsSkinSel": { bool sl = tipo == "arsSkinSel"; tex = TexArredondada(sl ? new Color(210 / 255f, 173 / 255f, 98 / 255f, .08f) : new Color(1, 1, 1, .02f), sl ? GOLD : new Color(232 / 255f, 224 / 255f, 208 / 255f, .14f), 8); pad = new RectOffset(13, 13, 11, 11); borda = new RectOffset(9, 9, 9, 9); tam = 13; peso = "sys"; break; }
                case "lk": tex = TexBotao(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), new Color(.91f, .878f, .816f, .3f), false, true); pad = new RectOffset(0, 0, 2, 3); borda = new RectOffset(0, 0, 0, 1); tam = 18; peso = "400"; txt = BONE; break;
                default:
                    bool sel = tipo.EndsWith("Sel") || tipo == "repOn"; var fundo = tipo.StartsWith("choice") ? new Color(14 / 255f, 16 / 255f, 19 / 255f, .8f) : FUNDO_OP; tex = TexBotao(fundo, fundo, sel ? GOLD_HI : BORDA_OP, false); borda = new RectOffset(1, 1, 1, 1); peso = "400"; txt = sel ? GOLD_HI : BONE;
                    if (tipo.StartsWith("lab")) { tam = 14; pad = new RectOffset(10, 10, 4, 4); } else if (tipo.StartsWith("set")) { tam = 16; pad = new RectOffset(1, 1, 7, 7); } else if (tipo.StartsWith("rep")) { tam = 15; pad = new RectOffset(10, 10, 5, 5); } else { tam = 15; pad = new RectOffset(13, 13, 4, 4); }
                    break;
            }
            s = new GUIStyle(Css(peso, tam, txt, al)) { padding = pad, border = borda, wordWrap = false };
            s.normal.background = tex; s.hover.background = tex; s.active.background = tex; s.focused.background = tex;
            s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = txt;
            if (tipo == "lk") { s.hover.textColor = GOLD_HI; }
            if (tipo == "arsSkin") s.hover.background = TexArredondada(new Color(1, 1, 1, .02f), new Color(210 / 255f, 173 / 255f, 98 / 255f, .6f), 8);   // .skin:hover
            estilosBotao[tipo] = s; return s;
        }
        float LarguraBotao(string txt, string tipo) { return Larg(txt, EstiloBotao(tipo)); }   // o CalcSize já soma o padding
        float AlturaBotao(string tipo) { var s = EstiloBotao(tipo); return Mathf.Round(AlturaNormal(s)) + s.padding.vertical; }
        // o botão num retângulo, com o foco do controle e a contagem do Bt (PartidaNavPad)
        bool BotaoCss(Rect r, string txt, string tipo, bool ativo = true)
        {
            if (medindo) { if (ativo && !primeiroFoco.HasValue) primeiroFoco = r; return false; }
            // button.go:disabled tem opacity .35: o IMGUI já pinta o desativado com metade do alfa, o GUI.color tira o resto
            int i = padContagem++; var e0 = GUI.enabled; var c0 = GUI.color; GUI.enabled = e0 && ativo; if (!ativo) GUI.color = new Color(c0.r, c0.g, c0.b, c0.a * .7f);
            bool b = BotaoNitido(r, txt, EstiloBotao(tipo)); GUI.enabled = e0; GUI.color = c0;
            if (b) cliqueNaPassada = true;
            if (padNav && i == padFoco && Event.current.type == EventType.Repaint)
            {
                Moldura(new Rect(r.x - 5, r.y - 5, r.width + 10, r.height + 10), GOLD_HI, 2);   // outline 2px com offset 3px
                if (padConfirmar && padConfirmarQuadro == Time.frameCount && ativo) { padConfirmar = false; cliqueNaPassada = true; return true; }
            }
            return b && ativo;
        }
        // button.go.desafioBtn: o texto à esquerda e uma linha menor embaixo
        bool BotaoDuasLinhas(Rect r, string tit, string sub, string tipo)
        {
            if (medindo) return false; bool b = BotaoCss(r, tit, tipo + "|mudo");
            Linha(r.x + 29, r.y + 11, r.width - 58, 24, tit, Css("600", 20, tipo == "ghost" ? BONE : Color.white));
            Linha(r.x + 29, r.y + 11 + 24 + 2, r.width - 58, LN(13f), sub, Css("400", 13, MUDO));
            return b;
        }
        // input.txt
        GUIStyle sCampoCss; readonly Dictionary<int, GUIStyle> camposCss = new Dictionary<int, GUIStyle>();
        string CampoCss(Rect r, string valor, int max, string nome, string dica = null)
        {
            if (medindo) { if (!primeiroFoco.HasValue) primeiroFoco = r; return valor; }
            if (sCampoCss == null)
            {
                sCampoCss = new GUIStyle(Css("400", 20, BONE)) { padding = new RectOffset(11, 11, 9, 9), border = new RectOffset(1, 1, 1, 1), clipping = TextClipping.Clip };
                var t = TexBotao(Hx("#0f1215"), Hx("#0f1215"), Hx("#4a525c"), false); var tf = TexBotao(Hx("#0f1215"), Hx("#0f1215"), GOLD, false);
                sCampoCss.normal.background = sCampoCss.hover.background = t; sCampoCss.focused.background = tf; sCampoCss.normal.textColor = sCampoCss.focused.textColor = sCampoCss.hover.textColor = BONE;
            }
            // esticado numa .linha (align-items: stretch), o texto do campo fica no meio da altura
            int pv = Mathf.RoundToInt((r.height - 2 - 24) / 2) + 1; GUIStyle st;
            if (!camposCss.TryGetValue(pv, out st)) { st = new GUIStyle(sCampoCss) { padding = new RectOffset(11, 11, pv, pv) }; camposCss[pv] = st; }
            var v = CampoNitido(r, valor, max, st, nome);
            if (string.IsNullOrEmpty(v) && dica != null) Linha(r.x + 11, r.y + pv, r.width - 22, 24, dica, Css("400", 20, new Color(.46f, .46f, .46f)));
            return v;
        }
        // a caixinha (accent dourado) com o rótulo; clicar em qualquer parte troca
        bool CaixinhaCss(float x, float y, string txt, bool v, GUIStyle st)
        {
            if (medindo) return v; var r = new Rect(x, y + 3, 13, 13);
            if (v) { Caixa(r, GOLD); Caixa(new Rect(r.x + 3, r.y + 6, 2, 4), Hx("#050506")); Caixa(new Rect(r.x + 5, r.y + 8, 5, 2), Hx("#050506")); Caixa(new Rect(r.x + 8, r.y + 3, 2, 6), Hx("#050506")); }
            else { Caixa(r, Hx("#ffffff", .9f)); Moldura(r, Hx("#767676")); }
            Linha(x + 13 + 6, y, 600, 18, txt, st);
            var toda = new Rect(x, y, 13 + 6 + Larg(txt, st), 19); if (GUI.Button(toda, "", GUIStyle.none)) { if (som != null) som.Tocar("click"); return !v; }
            return v;
        }

        // ---------- o fundo e o painel ----------
        Texture2D texFundoTela, texFundoTitulo;
        // .screen: radial-gradient(ellipse at 50% 38%, rgba(40,14,24,.55), rgba(3,3,4,.94) 70%)
        void FundoTela()
        {
            if (medindo) return;
            if (texFundoTela == null)
            {
                int N = 96; texFundoTela = new Texture2D(N, N, TextureFormat.RGBA32, false); var p = new Color[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                    {
                        // a elipse vai até o canto mais longe (de 50%, 38%: o de baixo); 70% dela já é a cor de fora
                        float dx = ((x + .5f) / N - .5f) / .5f, dy = (1 - (y + .5f) / N - .38f) / .62f, d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                        p[y * N + x] = Color.Lerp(new Color(40 / 255f, 14 / 255f, 24 / 255f, .55f), new Color(3 / 255f, 3 / 255f, 4 / 255f, .94f), Mathf.Clamp01(d / .7f));
                    }
                texFundoTela.SetPixels(p); texFundoTela.Apply(false, true); texFundoTela.wrapMode = TextureWrapMode.Clamp;
            }
            GUI.DrawTexture(new Rect(0, 0, W, H), texFundoTela);
        }
        // #scr.inicial: linear-gradient(90deg, .94 0%, .72 38%, .15 70%, 0 100%)
        void FundoTitulo()
        {
            if (medindo) return;
            if (texFundoTitulo == null)
            {
                int n = 128; texFundoTitulo = new Texture2D(n, 1, TextureFormat.RGBA32, false); var p = new Color[n];
                for (int i = 0; i < n; i++) { float f = (i + .5f) / n, a = f < .38f ? Mathf.Lerp(.94f, .72f, f / .38f) : f < .7f ? Mathf.Lerp(.72f, .15f, (f - .38f) / .32f) : Mathf.Lerp(.15f, 0, (f - .7f) / .3f); p[i] = new Color(3 / 255f, 3 / 255f, 6 / 255f, a); }
                texFundoTitulo.SetPixels(p); texFundoTitulo.Apply(false, true); texFundoTitulo.wrapMode = TextureWrapMode.Clamp;
            }
            GUI.DrawTexture(new Rect(0, 0, W, H), texFundoTitulo);
        }
        // .panel.plate: o degradê do HUD com a borda bronze e os cantos cortados
        void PainelCss(Rect r) { if (!medindo) Placa(r); }

        // ---------- texto nas telas (h2, h3, p, .lede, .fine) ----------
        // devolvem a altura usada; com medindo, só medem
        float H2(float x, float y, float w, string txt, TextAnchor al = TextAnchor.UpperLeft) { var st = Css("serif", 32, BONE, al); float h = AlturaNormal(st); if (!medindo) Linha(x, y, w, h, txt, st); return h + 8; }
        float H3(float x, float y, float w, string txt, Color? cor = null) { var st = Css("serif", 21, cor ?? BONE); float h = AlturaNormal(st); if (!medindo) Linha(x, y + 14, w, h, txt, st); return 14 + h + 6; }
        // o .panel p: 18 px, linha de 1,45, no máximo 64ch de largura (a caixa fica à esquerda, como a margem 6px 0)
        // fine: o .fine (margem 12 auto 0: a caixa de 64ch fica no meio e o texto centrado)
        float P(float x, float y, float w, string txt, int tam = 18, Color? cor = null, float lh = 1.45f, TextAnchor al = TextAnchor.UpperLeft, float maxCh = 64, bool fine = false)
        {
            var st = Css("400", tam, cor ?? Hx("#cfc7b8"), al); float l = tam * lh, wb = maxCh > 0 ? Mathf.Min(w, Larg("0", st) * maxCh) : w;
            if (fine) { x += (w - wb) / 2; y += 12; }
            float h = medindo ? AlturaParagrafo(txt, st, wb, l) : Paragrafo(x, y, wb, l, txt, st); return h + (fine ? 12 : 0);
        }
        float Ps(float x, float y, float w, string txt, GUIStyle st, float lh) { if (medindo) return AlturaParagrafo(txt, st, w, lh); return Paragrafo(x, y, w, lh, txt, st); }
    }
}
