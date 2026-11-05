// As peças das telas do navegador para o IMGUI, desenhadas em retângulos calculados como o CSS faz (sem o GUILayout).
// v6.3, direção Nanquim: button.go é texto (Big Shoulders em maiúsculas, vermelho; .gold em osso; .ghost em mono, minúsculas,
// apagado) com um traço que corre por baixo no cursor e no foco; as opções (.dif, .set .opts, .labRow, .repCtl) são texto
// mono, a escolhida entre colchetes; o link (.lk) em mono; o campo (input.txt) só com a linha de baixo; a caixinha
// (checkbox com accent dourado), o fundo da tela (.screen e o degradê lateral do título) e a placa do painel (.panel.plate).
// Cada tela mede antes de desenhar (medindo = true: nada é desenhado nem clicado), para centrar e rolar como o navegador.
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        bool medindo;
        static readonly Color SANGUE = new Color(.769f, .086f, .11f);   // --sangue #c4161c
        static bool SuaveUI() { return PlayerPrefs.GetInt("rr-suave", 0) == 1; }
        static readonly Color BORDA_OP = new Color(38 / 255f, 38 / 255f, 38 / 255f), FUNDO_OP = new Color(1, 1, 1, .025f);

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
                // button.go: Big Shoulders 800 de 24 px, maiúsculas, vermelho, padding 6 × 0 (7 embaixo), sem fundo
                case "go": tex = null; pad = new RectOffset(0, 0, 6, 7); borda = new RectOffset(0, 0, 0, 0); peso = "serif"; tam = 24; txt = SANGUE; break;
                case "gold": tex = null; pad = new RectOffset(0, 0, 6, 7); borda = new RectOffset(0, 0, 0, 0); peso = "serif"; tam = 24; txt = BONE; break;
                // .go.ghost: mono 14, minúsculas, apagado (osso no cursor), padding 4 × 0 (5 embaixo)
                case "ghost": case "ghostP": tex = null; pad = new RectOffset(0, 0, 4, 5); borda = new RectOffset(0, 0, 0, 0); peso = "mono"; tam = 14; txt = MUDO; break;
                // .loja .item .go: 17 px, padding 8 × 18 (o preço em osso; o motivo do bloqueio em mono)
                case "lojaGold": tex = null; pad = new RectOffset(18, 18, 8, 8); borda = new RectOffset(0, 0, 0, 0); peso = "serif"; tam = 17; txt = BONE; break;
                case "lojaGhost": tex = null; pad = new RectOffset(18, 18, 8, 8); borda = new RectOffset(0, 0, 0, 0); peso = "mono"; tam = 17; txt = MUDO; break;
                // .slot: 19 px à esquerda, fundo escuro, borda #3a4149 (a sua vaga, ouro); a do bot com o texto apagado
                case "slot": case "slotBot": case "slotMe": tex = TexBotao(FUNDO_OP, FUNDO_OP, tipo == "slotMe" ? BONE : BORDA_OP, false); pad = new RectOffset(13, 13, 11, 11); borda = new RectOffset(1, 1, 1, 1); tam = 19; peso = "400"; txt = tipo == "slotBot" ? MUDO : BONE; al = TextAnchor.MiddleLeft; break;
                // #arsenal .abas button: fundo quase transparente, borda ouro .35, cantos de 6 (a escolhida em ouro); .skin: cantos de 8
                // a vitrine das animações (o painel do arma.js): fundo #2a1d18, borda #6b4a2a, cantos de 5, padding 4 × 7 (9 no tocar)
                case "vit": case "vitPlay": tex = TexArredondada(Hx("#2a1d18"), Hx("#6b4a2a"), 5); pad = tipo == "vitPlay" ? new RectOffset(10, 10, 5, 5) : new RectOffset(8, 8, 5, 5); borda = new RectOffset(6, 6, 6, 6); tam = 13; peso = "sys"; txt = Hx("#eadfca"); break;
                case "arsAba": case "arsAbaSel": { bool sl = tipo == "arsAbaSel"; tex = TexArredondada(sl ? new Color(210 / 255f, 173 / 255f, 98 / 255f, .18f) : new Color(1, 1, 1, .04f), sl ? GOLD : new Color(210 / 255f, 173 / 255f, 98 / 255f, .35f), 6); pad = new RectOffset(7, 7, 9, 9); borda = new RectOffset(7, 7, 7, 7); tam = 13; peso = "sys"; txt = sl ? GOLD_HI : BONE; break; }
                case "arsSkin": case "arsSkinSel": { bool sl = tipo == "arsSkinSel"; tex = TexArredondada(sl ? new Color(210 / 255f, 173 / 255f, 98 / 255f, .08f) : new Color(1, 1, 1, .02f), sl ? GOLD : new Color(232 / 255f, 224 / 255f, 208 / 255f, .14f), 8); pad = new RectOffset(13, 13, 11, 11); borda = new RectOffset(9, 9, 9, 9); tam = 13; peso = "sys"; break; }
                // .lk: mono 13, minúsculas, apagado; no cursor, osso com o traço vermelho
                case "lk": tex = null; pad = new RectOffset(0, 0, 2, 2); borda = new RectOffset(0, 0, 0, 0); tam = 13; peso = "mono"; txt = MUDO; break;
                default:
                    bool sel = tipo.EndsWith("Sel") || tipo == "repOn";
                    if (tipo.StartsWith("choice"))
                    {   // .choice: a caixa fica (fundo quase nada, borda #262626; a escolhida em osso)
                        tex = TexBotao(FUNDO_OP, FUNDO_OP, sel ? BONE : BORDA_OP, false); borda = new RectOffset(1, 1, 1, 1); peso = "400"; txt = BONE; tam = 15; pad = new RectOffset(13, 13, 4, 4); break;
                    }
                    // .dif button, .set .opts button, .labRow, .repCtl: mono 13 sem caixa; a escolhida em osso, entre colchetes
                    tex = null; borda = new RectOffset(0, 0, 0, 0); peso = "mono"; tam = 13; txt = sel ? BONE : Hx("#6e6a62"); pad = new RectOffset(0, 0, 2, 2);
                    break;
            }
            s = new GUIStyle(Css(peso, tam, txt, al)) { padding = pad, border = borda, wordWrap = false };
            s.normal.background = tex; s.hover.background = tex; s.active.background = tex; s.focused.background = tex;
            s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = txt;
            if (tipo == "lk" || tipo == "ghost" || tipo == "ghostP" || tipo == "lojaGhost" || tex == null && txt != BONE && txt != SANGUE) { s.hover.textColor = BONE; }
            if (tipo == "arsSkin") s.hover.background = TexArredondada(new Color(1, 1, 1, .02f), new Color(210 / 255f, 173 / 255f, 98 / 255f, .6f), 8);   // .skin:hover
            estilosBotao[tipo] = s; return s;
        }
        // o texto como o CSS mostra: maiúsculas nos .go (text-transform), minúsculas nos fantasmas e nos links, a opção escolhida
        // entre colchetes (::before e ::after)
        static string TextoBotao(string txt, string tipo)
        {
            if (string.IsNullOrEmpty(txt)) return txt; string t = tipo.EndsWith("|mudo") ? tipo.Substring(0, tipo.Length - 5) : tipo;
            switch (t)
            {
                case "go": case "gold": case "lojaGold": return txt.ToUpper(new System.Globalization.CultureInfo("pt-BR"));
                case "ghost": case "ghostP": case "lojaGhost": case "lk": return txt.ToLower(new System.Globalization.CultureInfo("pt-BR"));
            }
            if (!t.StartsWith("choice") && !t.StartsWith("slot") && !t.StartsWith("vit") && !t.StartsWith("ars") && (t.EndsWith("Sel") || t == "repOn")) return "[" + txt + "]";
            return txt;
        }
        // o traço que corre por baixo (button.go::after, .lk::after): na cor do texto nos .go, vermelho nos fantasmas e nos links
        static bool TemTraco(string tipo) { return tipo == "go" || tipo == "gold" || tipo == "ghost" || tipo == "ghostP" || tipo == "lk" || tipo == "lojaGold" || tipo == "lojaGhost"; }
        readonly Dictionary<string, float> tracos = new Dictionary<string, float>();
        void Traco(Rect r, string tipo, bool ligado)
        {
            if (!TemTraco(tipo)) return; string k = tipo + "|" + r.x.ToString("0") + "|" + r.y.ToString("0"); float v; tracos.TryGetValue(k, out v);
            v = Mathf.MoveTowards(v, ligado ? 1 : 0, Time.unscaledDeltaTime / .12f); if (SuaveUI()) v = ligado ? 1 : 0; tracos[k] = v; if (v <= 0) return;
            var c = tipo == "go" ? SANGUE : tipo == "gold" || tipo == "lojaGold" ? BONE : SANGUE; float y = tipo == "lk" ? r.yMax : r.yMax - 2;
            Caixa(new Rect(r.x, y, r.width * v, 2), c);
        }
        float LarguraBotao(string txt, string tipo) { return Larg(TextoBotao(txt, tipo), EstiloBotao(tipo)); }   // o CalcSize já soma o padding
        float AlturaBotao(string tipo) { var s = EstiloBotao(tipo); return Mathf.Round(AlturaNormal(s)) + s.padding.vertical; }
        // o botão num retângulo, com o foco do controle e a contagem do Bt (PartidaNavPad)
        bool BotaoCss(Rect r, string txt, string tipo, bool ativo = true)
        {
            if (medindo) { if (ativo && !primeiroFoco.HasValue) primeiroFoco = r; return false; }
            // button.go:disabled tem opacity .35: o IMGUI já pinta o desativado com metade do alfa, o GUI.color tira o resto
            int i = padContagem++; var e0 = GUI.enabled; var c0 = GUI.color; GUI.enabled = e0 && ativo; if (!ativo) GUI.color = new Color(c0.r, c0.g, c0.b, c0.a * .7f);
            bool b = BotaoNitido(r, TextoBotao(txt, tipo), EstiloBotao(tipo)); GUI.enabled = e0; GUI.color = c0;
            if (b) cliqueNaPassada = true;
            bool foco = padNav && i == padFoco;
            if (Event.current.type == EventType.Repaint && ativo) Traco(r, tipo, foco || r.Contains(Event.current.mousePosition));
            if (foco && Event.current.type == EventType.Repaint)
            {
                if (!TemTraco(tipo)) Moldura(new Rect(r.x - 5, r.y - 5, r.width + 10, r.height + 10), BONE, 1);   // as opções sem traço: um contorno fino
                if (padConfirmar && padConfirmarQuadro == Time.frameCount && ativo) { padConfirmar = false; cliqueNaPassada = true; return true; }
            }
            return b && ativo;
        }
        // button.go.desafioBtn: o texto à esquerda e uma linha menor embaixo
        bool BotaoDuasLinhas(Rect r, string tit, string sub, string tipo)
        {
            if (medindo) return false; bool b = BotaoCss(r, tit, tipo + "|mudo");
            // .desafioBtn é um .go.ghost: o título em mono 14 e a linha de baixo em mono 13
            Linha(r.x, r.y + 4, r.width, LN(14f), TextoBotao(tit, tipo), Css("mono", 14, tipo == "ghost" ? MUDO : SANGUE));
            Linha(r.x, r.y + 4 + LN(14f) + 2, r.width, LN(13f), TextoBotao(sub, tipo), Css("mono", 13, MUDO));
            return b;
        }
        // input.txt
        GUIStyle sCampoCss; readonly Dictionary<int, GUIStyle> camposCss = new Dictionary<int, GUIStyle>();
        string CampoCss(Rect r, string valor, int max, string nome, string dica = null)
        {
            if (medindo) { if (!primeiroFoco.HasValue) primeiroFoco = r; return valor; }
            if (sCampoCss == null)
            {
                // input.txt: mono 16, sem fundo, só a linha de baixo (osso no foco)
                sCampoCss = new GUIStyle(Css("mono", 16, BONE)) { padding = new RectOffset(2, 2, 6, 6), border = new RectOffset(0, 0, 0, 1), clipping = TextClipping.Clip };
                var t = TexBotao(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), Hx("#6e6a62"), false, true); var tf = TexBotao(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), BONE, false, true);
                sCampoCss.normal.background = sCampoCss.hover.background = t; sCampoCss.focused.background = tf; sCampoCss.normal.textColor = sCampoCss.focused.textColor = sCampoCss.hover.textColor = BONE;
            }
            // esticado numa .linha (align-items: stretch), o texto do campo fica no meio da altura
            int pv = Mathf.RoundToInt((r.height - 1 - 21) / 2); GUIStyle st;
            if (!camposCss.TryGetValue(pv, out st)) { st = new GUIStyle(sCampoCss) { padding = new RectOffset(2, 2, pv, pv) }; camposCss[pv] = st; }
            var v = CampoNitido(r, valor, max, st, nome);
            if (string.IsNullOrEmpty(v) && dica != null) Linha(r.x + 2, r.y + pv, r.width - 4, 24, dica, Css("mono", 16, new Color(.46f, .46f, .46f)));
            return v;
        }
        // a caixinha (accent dourado) com o rótulo; clicar em qualquer parte troca
        bool CaixinhaCss(float x, float y, string txt, bool v, GUIStyle st)
        {
            if (medindo) return v; var r = new Rect(x, y + 3, 13, 13);
            if (v) { Caixa(r, GOLD); Caixa(new Rect(r.x + 3, r.y + 6, 2, 4), Hx("#0a0a0a")); Caixa(new Rect(r.x + 5, r.y + 8, 5, 2), Hx("#0a0a0a")); Caixa(new Rect(r.x + 8, r.y + 3, 2, 6), Hx("#0a0a0a")); }
            else { Caixa(r, Hx("#ffffff", .9f)); Moldura(r, Hx("#767676")); }
            Linha(x + 13 + 6, y, 600, 18, txt, st);
            var toda = new Rect(x, y, 13 + 6 + Larg(txt, st), 19); if (GUI.Button(toda, "", GUIStyle.none)) { if (som != null) som.Tocar("click"); return !v; }
            return v;
        }

        // ---------- o fundo e o painel ----------
        Texture2D texFundoTitulo;
        // .screen (Nanquim): preto chapado rgba(10,10,10,.94)
        void FundoTela()
        {
            if (medindo) return;
            Caixa(new Rect(0, 0, W, H), new Color(10 / 255f, 10 / 255f, 10 / 255f, .94f));
        }
        // #scr.inicial (Nanquim): linear-gradient(90deg, .97 0%, .86 40%, .25 72%, 0 100%) de #0a0a0a
        void FundoTitulo()
        {
            if (medindo) return;
            if (texFundoTitulo == null)
            {
                int n = 128; texFundoTitulo = new Texture2D(n, 1, TextureFormat.RGBA32, false); var p = new Color[n];
                for (int i = 0; i < n; i++) { float f = (i + .5f) / n, a = f < .4f ? Mathf.Lerp(.97f, .86f, f / .4f) : f < .72f ? Mathf.Lerp(.86f, .25f, (f - .4f) / .32f) : Mathf.Lerp(.25f, 0, (f - .72f) / .28f); p[i] = new Color(10 / 255f, 10 / 255f, 10 / 255f, a); }
                texFundoTitulo.SetPixels(p); texFundoTitulo.Apply(false, true); texFundoTitulo.wrapMode = TextureWrapMode.Clamp;
            }
            GUI.DrawTexture(new Rect(0, 0, W, H), texFundoTitulo);
        }
        // .panel.plate (Nanquim): sem placa, o texto direto no fundo
        void PainelCss(Rect r) { }

        // ---------- texto nas telas (h2, h3, p, .lede, .fine) ----------
        // devolvem a altura usada; com medindo, só medem
        // .panel h2/h3 (Nanquim): Big Shoulders 800 em maiúsculas, 44 e 22 px, linha de .95
        static string Maiusc(string t) { return t == null ? t : t.ToUpper(new System.Globalization.CultureInfo("pt-BR")); }
        float H2(float x, float y, float w, string txt, TextAnchor al = TextAnchor.UpperLeft) { var st = Css("serif", 44, BONE, al); float h = Mathf.Round(44 * .95f); if (!medindo) Linha(x, y, w, h, Maiusc(txt), st); return h + 8; }
        float H3(float x, float y, float w, string txt, Color? cor = null) { var st = Css("serif", 22, cor ?? BONE); float h = Mathf.Round(22 * .95f); if (!medindo) Linha(x, y + 18, w, h, Maiusc(txt), st); return 18 + h + 6; }
        // o .panel p: 18 px, linha de 1,45, no máximo 64ch de largura (a caixa fica à esquerda, como a margem 6px 0)
        // fine: o .fine (margem 12 auto 0: a caixa de 64ch fica no meio e o texto centrado)
        float P(float x, float y, float w, string txt, int tam = 18, Color? cor = null, float lh = 1.45f, TextAnchor al = TextAnchor.UpperLeft, float maxCh = 64, bool fine = false)
        {
            if (fine) { tam = 13; }   // .fine: mono 13
            var st = Css(fine ? "mono" : "400", tam == 18 ? 17 : tam, cor ?? Hx("#c9c3b8"), al); tam = st.fontSize; float l = tam * lh, wb = maxCh > 0 ? Mathf.Min(w, Larg("0", st) * maxCh) : w;
            if (fine) { x += (w - wb) / 2; y += 12; }
            float h = medindo ? AlturaParagrafo(txt, st, wb, l) : Paragrafo(x, y, wb, l, txt, st); return h + (fine ? 12 : 0);
        }
        float Ps(float x, float y, float w, string txt, GUIStyle st, float lh) { if (medindo) return AlturaParagrafo(txt, st, w, lh); return Paragrafo(x, y, w, lh, txt, st); }
    }
}
