// As telas de painel do navegador (.screen com a placa .panel.plate no meio): a tela genérica que mede, centra e rola, os
// cartões de escolha (.choice) e a entrada da noite (introScreen: arquétipo, feitiço e Estou pronto).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        Vector2 rolPainel; string painelChave;
        // .screen + .panel.plate: largura min(800, tela − 32), padding 26 × 40 × 24, no meio; o corpo recebe (x, y, largura)
        // e devolve a altura do conteúdo. Quando não cabe, a tela inteira rola
        Rect? primeiroFoco;   // o primeiro botão ativo ou campo da tela, achado na passada que mede
        void PainelTela(string chave, float largura, System.Func<float, float, float, float> corpo)
        {
            bool nova = painelChave != chave; if (nova) { painelChave = chave; rolPainel = Vector2.zero; }
            FundoTela(); float w = Mathf.Min(largura, W - 32), wc = w - 82;
            primeiroFoco = null; medindo = true; float hc = corpo(0, 0, wc); medindo = false; float h = 1 + 26 + hc + 24 + 1;
            if (h + 32 <= H) { var r = new Rect((W - w) / 2, (H - h) / 2, w, h); PainelCss(r); corpo(r.x + 41, r.y + 27, wc); return; }
            // o setScreen do navegador foca o primeiro botão (ou campo) da tela nova, e o focus() o traz para a vista: no meio
            // da tela se estava fora dela, sem passar do fim da rolagem
            if (nova && primeiroFoco.HasValue)
            {
                var f = primeiroFoco.Value; float fy = 16 + 1 + 26 + f.y;
                if (fy < rolPainel.y || fy + f.height > rolPainel.y + H) rolPainel.y = Mathf.Clamp(fy + f.height / 2 - H / 2, 0, h + 32 - H);
            }
            rolPainel = GUI.BeginScrollView(new Rect(0, 0, W, H), rolPainel, new Rect(0, 0, W - 20, h + 32), false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            var rr = new Rect((W - 20 - w) / 2, 16, w, h); PainelCss(rr); corpo(rr.x + 41, rr.y + 27, wc); GUI.EndScrollView();
        }

        // .choices: cartões em 2 ou 3 colunas, 14 px entre eles; o conteúdo no meio da altura (é um <button>)
        float Cartoes(float x, float y, float w, int colunas, IList<string[]> itens, System.Func<int, bool> sel, System.Action<int> clica, bool ativo)
        {
            float cw = (w - 14 * (colunas - 1)) / colunas, yy = y + 10; var stB = Css("serif", 22, BONE); var stS = Css("400", 16, Hx("#c9c1b2")); var stM = Css("400", 14, MUDO);
            for (int i0 = 0; i0 < itens.Count; i0 += colunas)
            {
                float hl = 0; var alts = new List<float>();
                for (int i = i0; i < Mathf.Min(itens.Count, i0 + colunas); i++) { float a = AlturaNormal(stB) + 4 + AlturaParagrafo(itens[i][1], stS, cw - 30, 16 * 1.35f) + (itens[i][2] != "" ? 6 + LN(14f) : 0); alts.Add(a); hl = Mathf.Max(hl, a); }
                float hc = hl + 24 + 2;
                for (int i = i0; i < Mathf.Min(itens.Count, i0 + colunas); i++)
                {
                    var r = new Rect(x + (i - i0) * (cw + 14), yy, cw, hc); bool s = sel(i);
                    var e0 = GUI.enabled; if (!ativo) GUI.enabled = false;
                    if (BotaoCss(r, itens[i][0], (s ? "choiceSel" : "choice") + "|mudo", ativo) && ativo) clica(i);
                    if (!medindo)
                    {
                        if (s) Moldura(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), GOLD_HI);   // o inset 0 0 0 1px
                        float cy = r.y + 1 + 12 + (hl - alts[i - i0]) / 2, cx = r.x + 15;
                        Linha(cx, cy, cw - 30, AlturaNormal(stB), itens[i][0], stB); cy += AlturaNormal(stB) + 4;
                        cy += Paragrafo(cx, cy, cw - 30, 16 * 1.35f, itens[i][1], stS) + 6; if (itens[i][2] != "") Linha(cx, cy, cw - 30, LN(14f), itens[i][2], stM);
                    }
                    GUI.enabled = e0;
                }
                yy += hc + 14;
            }
            return yy - 14 - y;
        }

        // .btnrow: botões no meio, 12 px entre eles (e entre as linhas, quando quebra), 18 px acima
        float FileiraBotoes(float x, float y, float w, params object[] bs)   // (texto, tipo, ativo, ação) em sequência
        {
            var L = new List<object[]>(); for (int i = 0; i + 3 < bs.Length; i += 4) L.Add(new[] { bs[i], bs[i + 1], bs[i + 2], bs[i + 3] });
            var linhas = new List<List<object[]>> { new List<object[]>() }; float acc = 0;
            foreach (var b in L) { float bw = LarguraBotao((string)b[0], (string)b[1]), add = (linhas[linhas.Count - 1].Count > 0 ? 12 : 0) + bw; if (acc + add > w && linhas[linhas.Count - 1].Count > 0) { linhas.Add(new List<object[]>()); acc = 0; add = bw; } linhas[linhas.Count - 1].Add(b); acc += add; }
            float yy = y + 18;
            foreach (var ln in linhas)
            {
                float total = ln.Sum(b => LarguraBotao((string)b[0], (string)b[1])) + 12 * (ln.Count - 1), xx = x + (w - total) / 2, h = 0;
                foreach (var b in ln)
                {
                    float bw = LarguraBotao((string)b[0], (string)b[1]), bh = AlturaBotao((string)b[1]); h = Mathf.Max(h, bh);
                    if (BotaoCss(new Rect(xx, yy, bw, bh), (string)b[0], (string)b[1], (bool)b[2])) ((System.Action)b[3])();
                    xx += bw + 12;
                }
                yy += h + 12;
            }
            return yy - 12 - y;
        }

        // ---------- introScreen ----------
        void TelaIntroCss()
        {
            string papel = MeuPapel(); bool Hc = papel == "H"; string meu; if (!g.cls.TryGetValue(EU, out meu)) meu = Regras.CLASS_BY_TEAM[papel][0];
            string fe; if (!g.feit.TryGetValue(EU, out fe)) fe = Regras.FEIT_BY_TEAM[papel][0]; bool pronto; g.ready.TryGetValue(EU, out pronto);
            var LB = g.lab ?? Lab.De(null); var mud = LabMudancas(LB); string crep = Relogio(LB.crepusculo); var CLn = Mundo.CLIMAS[Mundo.ClimaDaRodada(g)];
            string brief = Hc
                ? "No <b>Crepúsculo</b> (" + crep + "), leia os sinais nos santuários da floresta, que descartam altares que o culto não escolheu, e acenda sentinelas: elas avisam quando alguém consagra um altar por perto." + (LB.economia ? " Negocie com o Ermitão, na cabana perto do acampamento." : "") + " Fique nas trilhas: a mata fechada é escura e abala a sanidade. Na <b>Vigília</b>, os altares despertam: purgue, investigue e sele. Quando um ritual começa, o coro logo ecoa o setor dele: dois altares possíveis, e o que você descartou decide para onde correr. Resista até o amanhecer."
                : "No <b>Crepúsculo</b>, colha ervas na floresta, profane túmulos no cemitério, ao leste da catedral, e consagre seus altares em silêncio." + (LB.economia ? " Negocie com a Carpideira, no oratório perto do seu acampamento." : "") + " Os reagentes ficam na mata, ao sul do desfiladeiro (e nas cabeceiras das pontes). Agachado (C) você anda devagar e quase sem barulho. Na <b>Vigília</b>, os rituais podem começar. O último só na <b>Hora Morta</b>, e a catedral inteira vai ouvir. Quem ataca não canaliza; você sente quando a caça começa a selar o seu ritual.";
            var classes = Regras.CLASS_BY_TEAM[papel]; var feits = Regras.FEIT_BY_TEAM[papel];
            PainelTela("intro" + papel, 800, (x, y, w) =>
            {
                float y0 = y; var stH = Css("serif", 32, Hc ? GOLD : CrimsonHi); if (!medindo) Linha(x, y, w, AlturaNormal(stH), "Esta noite você é " + (Hc ? "Caçador" : "Cultista"), stH); y += AlturaNormal(stH) + 8;
                var stLd = Css("serifIt", 20, MUDO); y += Ps(x, y, Mathf.Min(w, Larg("0", stLd) * 64), CLn.nome + ": " + CLn.sub.ToLowerInvariant() + ".", stLd, 20 * 1.45f) + 8;
                // as margens se fundem como no navegador: a do .lede (8) engole a do p (6); a do h3 (14) engole a do p
                y += P(x, y, w, brief);
                if (mud.Count > 0) y += 6 + P(x, y + 6, w, "Laboratório de playtest: " + string.Join(" · ", mud.ToArray()) + ".", 14, GOLD_HI);
                y += H3(x, y, w, "Arquétipo") - 6;
                y += Cartoes(x, y, w, classes.Length > 2 ? 3 : 2, classes.Select(c => new[] { Regras.CLASSES[c].name, Regras.CLASSES[c].text, Regras.CLASSES[c].hp + " de vida" }).ToList(), i => classes[i] == meu, i => Acao(new Acao { type = "cls", cls = classes[i] }), !pronto);
                y += H3(x, y, w, "Feitiço") - 6;
                y += Cartoes(x, y, w, 3, feits.Select(f => new[] { Regras.FEITICOS[f].nome, Regras.FEITICOS[f].text, "recarga de " + Regras.FEITICOS[f].cd + " s" }).ToList(), i => feits[i] == fe, i => Acao(new Acao { type = "feit", f = feits[i] }), !pronto);
                y += FileiraBotoes(x, y, w, pronto ? "Aguardando os outros" : "Estou pronto", Hc ? "gold" : "go", !pronto, (System.Action)(() => Acao(new Acao { type = "ready" })));
                string esp = (online ? ListaProntos() : "") + TextoTempo();
                if (esp.Trim().Length > 0) y += 10 + P(x, y + 10, w, esp.Trim(), 17, MUDO, 1.45f, TextAnchor.UpperCenter);
                return y - y0;
            });
        }

        // ---------- settingsScreen ----------
        // .set: rótulo de 200 px | controle | valor de 64 px, 14 px entre eles, 12 px entre as linhas, 17 px
        float LinhaAjuste(float x, float y, float w, string rotulo, string valor, float hControle, System.Action<Rect> controle)
        {
            float h = Mathf.Max(LN(17f), hControle); var stL = Css("400", 17, BONE); var stV = Css("600", 17, GOLD_HI, TextAnchor.UpperRight);
            float xc = x + 200 + 14, wc = w - 200 - 64 - 28;
            if (!medindo) { Linha(x, y + (h - LN(17)) / 2, 200, LN(17), rotulo, stL); if (valor != null) Linha(xc + wc + 14, y + (h - LN(17)) / 2, 64, LN(17), valor, stV); }
            controle(new Rect(xc, y + (h - hControle) / 2, wc, hControle)); return h;
        }
        // input[type=range] com accent dourado: a trilha preenchida até o botão
        GUIStyle sTrilha, sBotaoTrilha;
        float SliderCss(Rect r, float v, float a, float b, float passo)
        {
            if (medindo) return v;
            if (sTrilha == null) { sTrilha = new GUIStyle { fixedHeight = 22 }; sBotaoTrilha = new GUIStyle { fixedWidth = 16, fixedHeight = 16 }; }
            float k = Mathf.Clamp01((v - a) / (b - a)), tx = r.x + 8, tw = r.width - 16, cy = r.y + r.height / 2;
            Caixa(new Rect(tx, cy - 2, tw, 4), new Color(.2f, .21f, .23f)); Caixa(new Rect(tx, cy - 2, tw * k, 4), GOLD); Tracos.Disco(tx + tw * k, cy, 8, GOLD);
            float nv = GUI.HorizontalSlider(r, v, a, b, sTrilha, sBotaoTrilha); if (passo > 0) nv = Mathf.Round(nv / passo) * passo; return nv;
        }
        // .set .opts: botões que dividem a coluna (flex 1), 8 px entre eles
        void OpcoesIguais(Rect r, string[][] ops, string atual, System.Action<string> muda, Color[] cores = null)
        {
            float bw = (r.width - 8 * (ops.Length - 1)) / ops.Length;
            for (int i = 0; i < ops.Length; i++)
            {
                bool sel = atual == ops[i][0]; var rb = new Rect(r.x + i * (bw + 8), r.y, bw, r.height); string t = cores != null ? Cor(ops[i][1], cores[i]) : ops[i][1];
                if (BotaoCss(rb, t, sel ? "setSel" : "set") && !sel) { muda(ops[i][0]); if (som != null) som.Tocar("click"); }
            }
        }
        float AjustesCss(float x, float y, float w, System.Action voltar)
        {
            float y0 = y, ho = AlturaBotao("set"), hs = 26;   // o input[type=range]: 22 px e 2 de margem em cima e embaixo
            y += H2(x, y, w, "Ajustes") + 4;   // a margem de 12 da primeira linha engole os 8 do h2
            y += LinhaAjuste(x, y, w, "Sensibilidade do mouse", sensibilidade.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), hs, r => { float n = SliderCss(r, sensibilidade, .2f, 3f, .05f); if (n != sensibilidade) { sensibilidade = n; SalvarAjustes(); } }) + 12;
            float vol = som != null ? som.volume : PlayerPrefs.GetFloat("rr-vol", .8f);
            y += LinhaAjuste(x, y, w, "Volume", Mathf.RoundToInt(vol * 100).ToString(), hs, r => { float n = SliderCss(r, vol * 100, 0, 100, 2) / 100; if (n != vol && som != null) { som.volume = n; SalvarAjustes(); } }) + 12;
            float fov = fp != null ? fp.fovBase : PlayerPrefs.GetFloat("rr-fov", 72);
            y += LinhaAjuste(x, y, w, "Campo de visão", Mathf.RoundToInt(fov).ToString(), hs, r => { float n = SliderCss(r, fov, 60, 105, 1); if (n != fov && fp != null) { fp.fovBase = n; SalvarAjustes(); } }) + 12;
            int q = PlayerPrefs.GetInt("rr-qualidade", Qualidade.nivel);
            y += LinhaAjuste(x, y, w, "Qualidade", null, ho, r => OpcoesIguais(r, new[] { new[] { "3", "Alto" }, new[] { "2", "Médio" }, new[] { "1", "Baixo" }, new[] { "0", "Mínimo" } }, q.ToString(), k => PlayerPrefs.SetInt("rr-qualidade", int.Parse(k)))) + 12;
            float br = PlayerPrefs.GetFloat("rr-brilho", 1);
            y += LinhaAjuste(x, y, w, "Brilho", Mathf.RoundToInt(br * 100) + "%", hs, r => { float n = SliderCss(r, br * 100, 60, 180, 5) / 100; if (n != br) { PlayerPrefs.SetFloat("rr-brilho", n); if (pos != null) pos.brilho = n; } }) + 12;
            y += LinhaAjuste(x, y, w, "Mira", null, ho, r => OpcoesIguais(r, new[] { new[] { "cruz", "Cruz" }, new[] { "ponto", "Ponto" }, new[] { "circulo", "Círculo" } }, PlayerPrefs.GetString("rr-mira", "cruz"), k => PlayerPrefs.SetString("rr-mira", k))) + 12;
            var cores = new[] { "#e8e0d0", "#7dff9a", "#ffe066", "#8ee8ff" };
            y += LinhaAjuste(x, y, w, "Cor da mira", null, ho, r => OpcoesIguais(r, new[] { new[] { cores[0], "Osso" }, new[] { cores[1], "Verde" }, new[] { cores[2], "Amarelo" }, new[] { cores[3], "Ciano" } }, PlayerPrefs.GetString("rr-mira-cor", "#e8e0d0"), k => PlayerPrefs.SetString("rr-mira-cor", k), cores.Select(c => Hx(c)).ToArray())) + 12;
            float mt = PlayerPrefs.GetFloat("rr-mira-tam", 1);
            y += LinhaAjuste(x, y, w, "Tamanho da mira", Mathf.RoundToInt(mt * 100) + "%", hs, r => { float n = SliderCss(r, mt * 100, 60, 180, 10) / 100; if (n != mt) PlayerPrefs.SetFloat("rr-mira-tam", n); }) + 12;
            System.Func<string, string, string[][], float> pref = (rot, chave, ops) => LinhaAjuste(x, y, w, rot, null, ho, r => OpcoesIguais(r, ops, PlayerPrefs.GetInt(chave, int.Parse(ops[0][0])).ToString(), k => { if (k == "r") RevisarDicas(); else PlayerPrefs.SetInt(chave, int.Parse(k)); })) + 12;
            y += pref("Dicas da primeira noite", "rr-dicas-on", new[] { new[] { "1", "Ligadas" }, new[] { "0", "Desligadas" }, new[] { "r", "Rever todas" } });
            y += pref("Música da tela inicial", "rr-musica", new[] { new[] { "1", "Ligada" }, new[] { "0", "Desligada" } });
            y += pref("Legendas dos sons", "rr-legendas", new[] { new[] { "1", "Ligadas" }, new[] { "0", "Desligadas" } });
            y -= 12; y += P(x, y, w, "Médio desliga o brilho difuso e metade das luzes; Baixo desliga também as sombras e usa texturas menores; Mínimo tira o pós-processamento e encurta a distância de visão. A qualidade vale no próximo Play.", 15, MUDO, 1.45f, TextAnchor.UpperCenter, 64, true);
            y += H3(x, y, w, "Acessibilidade") - 6 + 12;
            float cf = fp != null ? fp.camFx : PlayerPrefs.GetFloat("rr-camfx", 1);
            y += LinhaAjuste(x, y, w, "Impacto na câmera", null, ho, r => OpcoesIguais(r, new[] { new[] { "1", "Normal" }, new[] { ".5", "Reduzido" }, new[] { "0", "Desligado" } }, cf >= .99f ? "1" : cf > .01f ? ".5" : "0", k => { float v = float.Parse(k, System.Globalization.CultureInfo.InvariantCulture); if (fp != null) fp.camFx = v; PlayerPrefs.SetFloat("rr-camfx", v); })) + 12;
            y += pref("Clarões e tremor", "rr-suave", new[] { new[] { "0", "Normais" }, new[] { "1", "Reduzidos" } });
            y += pref("Chat dos adversários", "rr-mudo-adv", new[] { new[] { "0", "Mostrar" }, new[] { "1", "Silenciar" } });
            y += pref("Cores das equipes", "rr-dalt", new[] { new[] { "0", "Ouro e carmim" }, new[] { "1", "Ouro e azul" } });
            if (mundo != null) mundo.claroesSuaves = PlayerPrefs.GetInt("rr-suave", 0) == 1;
            y -= 12; y += P(x, y, w, "Reduzidos: relâmpagos, o clarão do Grande Ritual e a cegueira do Flash ficam mais fracos, e a câmera não treme com a sanidade baixa. Ouro e azul troca o carmim do culto por azul no placar, nos altares do HUD, na planta e no replay (ajuda com daltonismo vermelho-verde).", 15, MUDO, 1.45f, TextAnchor.UpperCenter, 64, true);
            y += H3(x, y, w, "Controles") - 6 + 8;
            // .binds: duas colunas de 6 × 14; cada linha com o nome e a tecla emoldurada
            float bw = (w - 14) / 2, bh = 1 + 6 + LN(16f) + 6 + 1; var stN = Css("400", 16, BONE);
            for (int i = 0; i < EntradaUnity.ACOES.Length; i++)
            {
                var a = EntradaUnity.ACOES[i]; var r = new Rect(x + (i % 2) * (bw + 14), y + (i / 2) * (bh + 6), bw, bh); bool cap = capturando == a[0];
                if (BotaoCss(r, a[1], (cap ? "choiceSel" : "choice") + "|mudo")) { capturando = a[0]; capturaQuadro = Time.frameCount; }
                if (!medindo)
                {
                    Linha(r.x + 11, r.y + 7, bw - 22, LN(16), a[1], stN); string kk = cap ? "aperte…" : EntradaUnity.Nome(a[0]); var stK = Css("700", 16, cap ? CrimsonHi : GOLD_HI, TextAnchor.UpperCenter);
                    float kw = Mathf.Max(34, Larg(kk, stK) + 14 + 2); var rk = new Rect(r.xMax - 11 - kw, r.y + 7, kw, LN(16)); Moldura(rk, Hx("#4a525c")); Linha(rk.x, rk.y, kw, LN(16), kk, stK);
                }
            }
            y += ((EntradaUnity.ACOES.Length + 1) / 2) * (bh + 6) - 6;
            y += P(x, y, w, "Clique numa linha e aperte a tecla ou o botão do mouse. Esc cancela. Se a tecla já estiver em uso, as duas ações trocam de lugar.", 15, MUDO, 1.45f, TextAnchor.UpperCenter, 64, true);
            y += H3(x, y, w, "Com controle") - 6;
            y += P(x, y, w, "Analógico esquerdo anda, direito olha (a sensibilidade é a mesma do mouse). RT ataca, RB feitiço, LT ou clique no analógico esquerdo corre. A interage, B transfere, X recarrega, Y lanterna, LB sensor, clique no analógico direito marca (ping), direcional para cima abre as frases (cima/baixo escolhe, A envia), para baixo sinalizador. Select mostra a planta, Start pausa. Nos menus: cima/baixo escolhe, A confirma, B volta.", 15, MUDO, 1.45f, TextAnchor.UpperLeft, 64, true);
            y += FileiraBotoes(x, y, w, "Restaurar padrão", "ghost", true, (System.Action)(() => { EntradaUnity.Padrao(); capturando = null; }), "Voltar", "go", true, voltar);
            return y - y0;
        }

        // ---------- a pausa (local) e o "Clique para jogar" (online) ----------
        void TelaPausaCss()
        {
            if (pausaAjustes) { PainelTela("ajustes", 800, (x, y, w) => AjustesCss(x, y, w, () => { pausaAjustes = false; som.Tocar("click"); })); return; }
            PainelTela("pausa" + online, 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, online ? "Clique para jogar" : "Pausado", TextAnchor.UpperCenter);
                var stP = Css("400", 18, Hx("#cfc7b8"), TextAnchor.UpperCenter); float wb = Mathf.Min(w, Larg("0", stP) * 64);
                y += 6 + Ps(x + (w - wb) / 2, y + 6, wb, online ? "No modo online a partida não pausa: enquanto o mouse estiver solto, seu personagem fica parado." : "Clique para voltar ao jogo.", stP, 18 * 1.45f);
                if (!online) y += ColaTeclas(x, y, w);
                y += FileiraBotoes(x, y, w, online ? "Jogar" : "Continuar", "go", true, (System.Action)(() => { pausaAjustes = false; Travar(true); }),
                    "Ajustes", "ghost", true, (System.Action)(() => { pausaAjustes = true; som.Tocar("click"); }),
                    online ? "Sair da sala" : "Abandonar a noite", "ghost", true, (System.Action)(() => { pausaAjustes = false; if (online) SairOnline(""); else { g = null; LimparVisual(); } }));
                return y - y0;
            });
        }
        // .cola: as teclas do seu papel, em linhas centradas de até 620 px (8 × 16 entre elas)
        float ColaTeclas(float x, float y, float w)
        {
            string papel = MeuPapel(); System.Func<string, string> kb = EntradaUnity.Nome;
            string andar = kb("fwd") == "W" && kb("left") == "A" && kb("back") == "S" && kb("right") == "D" ? "WASD" : kb("fwd") + " " + kb("left") + " " + kb("back") + " " + kb("right");
            var L = new List<string[]> { new[] { andar, "Andar" }, new[] { kb("sprint"), "Correr" }, new[] { kb("agachar"), "Agachar" }, new[] { kb("fire"), "Atacar" }, new[] { kb("abil"), "Feitiço" }, new[] { kb("use"), "Interagir" } };
            if (papel == "H") { L.Add(new[] { kb("sensor"), "Sensor" }); L.Add(new[] { kb("lantern"), "Lanterna" }); L.Add(new[] { kb("flare"), "Flare" }); L.Add(new[] { kb("reload"), "Recarregar" }); } else L.Add(new[] { kb("use2"), "Transferir" });
            L.Add(new[] { kb("map"), "Planta" }); L.Add(new[] { kb("caderno"), "Caderno" }); L.Add(new[] { kb("ping"), "Ping" }); L.Add(new[] { kb("falas"), "Frases" }); if (online) L.Add(new[] { "Enter", "Chat" });
            var stK = Css("700", 14, GOLD_HI); var stN = Css("400", 14, MUDO); float lh = LN(14f), maxW = Mathf.Min(620, w);
            var larg = L.Select(i => Larg(i[0], stK) + 10 + 2 + 6 + Larg(i[1], stN)).ToList();
            var linhas = new List<List<int>> { new List<int>() }; float acc = 0;
            for (int i = 0; i < L.Count; i++) { float add = (linhas[linhas.Count - 1].Count > 0 ? 16 : 0) + larg[i]; if (acc + add > maxW && linhas[linhas.Count - 1].Count > 0) { linhas.Add(new List<int>()); acc = 0; add = larg[i]; } linhas[linhas.Count - 1].Add(i); acc += add; }
            float yy = y + 14;
            foreach (var ln in linhas)
            {
                float tw = ln.Sum(i => larg[i]) + 16 * (ln.Count - 1), xx = x + (w - tw) / 2;
                foreach (var i in ln)
                {
                    if (!medindo) { float kw = Larg(L[i][0], stK) + 10 + 2; Moldura(new Rect(xx, yy, kw, lh), Hx("#4a525c")); Linha(xx + 6, yy, kw, lh, L[i][0], stK); Linha(xx + kw + 6, yy, 200, lh, L[i][1], stN); }
                    xx += larg[i] + 16;
                }
                yy += lh + 8;
            }
            return 14 + linhas.Count * (lh + 8) - 8 + 4;
        }
    }
}
