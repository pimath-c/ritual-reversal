// O resto das telas da noite com o CSS do navegador: a escolha dos altares (pickScreen), o Rever a noite (replayScreen:
// a planta, os controles .repCtl, a legenda e os últimos marcos) e o relatório do playtest (relatorioScreen: as tabelas
// table.rel com a coluna do que cada número revela e as perguntas em caixas de texto).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        // uma coluna de tabela: número à direita (th.n, td.n) ou texto .fine (13 px apagado)
        class Col { public bool n, fine; public Col(bool n = false, bool fine = false) { this.n = n; this.fine = fine; } }
        // table.res com width:100% e table-layout automático, como o navegador: cada coluna tem a largura mínima (a palavra
        // mais longa) e a máxima (o texto numa linha só), mais o padding de 8 px de cada lado; a célula com colspan reparte o
        // que passa das colunas que ela cobre na proporção das máximas; a largura da tabela vai para as colunas entre a
        // mínima e a máxima na proporção da folga (ou além da máxima, na proporção dela). As células ficam no meio da linha
        // (vertical-align: middle). "\u0001" no começo da linha a faz ocupar da segunda coluna em diante (texto .fine);
        // "\u0002", a linha inteira.
        float TabelaCols(float x, float y, float w, Col[] cols, string[] cab, List<string[]> linhas)
        {
            int nc = cols.Length; float yy = y + 10, lh = LN(17f), lhF = LN(13f);
            System.Func<int, bool, GUIStyle> est = (i, th) => th ? Css("500", 17, MUDO, cols[i].n ? TextAnchor.UpperRight : TextAnchor.UpperLeft) : cols[i].fine ? Css("400", 13, MUDO) : Css("400", 17, BONE, cols[i].n ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
            var stSpan = Css("400", 13, MUDO);
            System.Func<string, GUIStyle, float> minimo = (t, st) => { float m = 0; foreach (var p in t.Split(' ', '\n')) m = Mathf.Max(m, Larg(p, st)); return m + 16; };
            System.Func<string, GUIStyle, float> maximo = (t, st) => { float m = 0; foreach (var p in t.Split('\n')) m = Mathf.Max(m, Larg(p, st)); return m + 16; };
            var mn = new float[nc]; var mx = new float[nc]; for (int i = 0; i < nc; i++) { mn[i] = mx[i] = 16; }
            var todas = new List<KeyValuePair<string[], bool>>(); if (cab != null) todas.Add(new KeyValuePair<string[], bool>(cab, true)); foreach (var l in linhas) todas.Add(new KeyValuePair<string[], bool>(l, false));
            foreach (var kv in todas)
            {
                var cel = kv.Key; if (Abrange(cel) >= 0) continue;
                for (int i = 0; i < nc && i < cel.Length; i++) { var st = est(i, kv.Value); mn[i] = Mathf.Max(mn[i], minimo(cel[i], st)); mx[i] = Mathf.Max(mx[i], maximo(cel[i], st)); }
            }
            foreach (var kv in todas)
            {
                var cel = kv.Key; int a = Abrange(cel); if (a < 0) continue; string t = cel[0].Substring(1); var stA = a == 1 ? stSpan : est(0, false);
                float smn = 0, smx = 0; for (int i = a; i < nc; i++) { smn += mn[i]; smx += mx[i]; }
                float cmn = minimo(t, stA), cmx = maximo(t, stA);
                if (cmn > smn) for (int i = a; i < nc; i++) mn[i] += (cmn - smn) * (smx > 0 ? mx[i] / smx : 1f / (nc - a));
                if (cmx > smx) for (int i = a; i < nc; i++) mx[i] += (cmx - smx) * (smx > 0 ? mx[i] / smx : 1f / (nc - a));
                for (int i = a; i < nc; i++) mx[i] = Mathf.Max(mx[i], mn[i]);
            }
            float tmn = mn.Sum(), tmx = mx.Sum(); var larg = new float[nc];
            for (int i = 0; i < nc; i++)
                larg[i] = w >= tmx ? mx[i] + (w - tmx) * (tmx > 0 ? mx[i] / tmx : 1f / nc) : w > tmn ? mn[i] + (w - tmn) * (tmx > tmn ? (mx[i] - mn[i]) / (tmx - tmn) : 0) : mn[i];
            foreach (var kv in todas)
            {
                var cel = kv.Key; bool th = kv.Value; int a = Abrange(cel); float h;
                if (a >= 0)
                {
                    float ws = larg.Skip(a).Sum() - 16, xs = x + larg.Take(a).Sum(); string t = cel[0].Substring(1); var stA = a == 1 ? stSpan : est(0, false); float la = a == 1 ? lhF : lh, hs = AlturaParagrafo(t, stA, ws, la);
                    // a célula vazia da primeira coluna ainda tem a linha do corpo (17 px)
                    h = Mathf.Max(a == 1 ? lh : 0, hs); if (!medindo) Paragrafo(xs + 8, yy + 6 + (h - hs) / 2, ws, la, t, stA);
                }
                else
                {
                    var hc = new float[nc]; h = 0;
                    for (int i = 0; i < nc; i++) { string t = i < cel.Length ? cel[i] : ""; hc[i] = t == "" ? (cols[i].fine && !th ? lhF : lh) : AlturaParagrafo(t, est(i, th), larg[i] - 16, cols[i].fine && !th ? lhF : lh); h = Mathf.Max(h, hc[i]); }
                    if (!medindo) { float xx = x; for (int i = 0; i < nc && i < cel.Length; i++) { Paragrafo(xx + 8, yy + 6 + (h - hc[i]) / 2, larg[i] - 16, cols[i].fine && !th ? lhF : lh, cel[i], est(i, th)); xx += larg[i]; } }
                }
                if (!medindo) Caixa(new Rect(x, yy + 6 + h + 6, w, 1), Hx("#333a42"));
                yy += 6 + h + 6 + 1;
            }
            return yy - y + 10;
        }
        static int Abrange(string[] cel) { return cel.Length > 0 && cel[0].Length > 0 ? cel[0][0] == '\u0001' ? 1 : cel[0][0] == '\u0002' ? 0 : -1 : -1; }

        // ---------- pickScreen ----------
        void TelaEscolhaCss()
        {
            if (MeuPapel() != "C")
            {
                PainelTela("pickwait" + g.round, 800, (x, y, w) =>
                {
                    float y0 = y; y += H2(x, y, w, "Os Cultistas estão escolhendo os altares"); y += P(x, y, w, "Você vai caçar nesta rodada. Assim que eles confirmarem, você escolhe sua classe.");
                    y += 10 + P(x, y + 10, w, "Tempo restante" + TextoTempo(), 17, MUDO, 1.45f, TextAnchor.UpperCenter); return y - y0;
                });
                return;
            }
            int parceiros = g.slots.Count(s => s.cid != null && Sim.RoleOf(g, s.team) == "C");
            PainelTela("pick" + g.round, 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, "Escolham 3 altares");
                y += P(x, y, w, "Os Caçadores não sabem quais são. Altares perto da sua entrada (leste) são mais rápidos de alcançar, e mais óbvios." + (parceiros > 1 ? " Seu parceiro vê e altera a mesma seleção." : ""));
                // o svg da planta: largura toda, 540 × 380
                float hm = w * VB_A / VB_L; if (!medindo) PlantaEscolha(new Rect(x, y + 6, w, hm)); y += 6 + hm;
                y += FileiraBotoes(x, y, w, "Sortear", "ghost", true, (System.Action)(() => Acao(new Acao { type = "pickRandom" })), "Confirmar " + g.picks.Count + " de 3", "go", g.picks.Count == 3, (System.Action)(() => Acao(new Acao { type = "pickConfirm" })));
                if (g.timers) y += 10 + P(x, y + 10, w, "Tempo restante" + TextoTempo(), 17, MUDO, 1.45f, TextAnchor.UpperCenter);
                return y - y0;
            });
        }

        // ---------- replayScreen ----------
        void TelaReplayCss()
        {
            RodadaLT R; if (!LT.TemReplay(repR) || !LT.rod.TryGetValue(repR, out R)) { repAberto = false; return; }
            var Pp = R.pos; double fim = Pp[Pp.Count - 1].rt;
            bool dalt = PlayerPrefs.GetInt("rr-dalt", 0) == 1; var nTime = new Dictionary<string, int> { { "H", 0 }, { "C", 0 } };
            var cores = R.atores.Select(o => { var L = o.team == "C" ? (dalt ? COR_REP_D : COR_REP[1]) : o.team == "H" ? COR_REP[0] : null; if (L == null) return Desenho.Hex("#cccccc"); return Desenho.Hex(L[(nTime[o.team]++) % 2]); }).ToList();
            Rect rmTela = new Rect();
            PainelTela("replay" + g.matchId + repR, 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, "Rever a noite" + (UmaNoite ? "" : " · rodada " + repR), TextAnchor.UpperCenter);
                y += Lede(x, y, w, (online ? "Os inimigos só aparecem onde você os viu. " : "") + "Os rastros mostram os últimos 20 segundos de cada um.");
                // #repSvg: margem de 6, que some no 14 de baixo do .lede
                float hm = w * PlantaFixa.A / PlantaFixa.L; var rm = new Rect(x, y, w, hm);
                if (!medindo) { Caixa(rm, new Color(6 / 255f, 7 / 255f, 9 / 255f, .6f)); PlantaBase(rm); rmTela = rm; }
                y += hm + 6;
                // .repCtl: o botão, o controle deslizante (flex 1), a hora e as velocidades
                string bt = repPlay ? "Pausar" : repT >= fim - .5 ? "Repetir" : "Continuar"; float hb = AlturaBotao("ghost"), wb = LarguraBotao(bt, "ghost");
                if (BotaoCss(new Rect(x, y, wb, hb), bt, "ghost")) { if (repT >= fim - .5) repT = 0; repPlay = !repPlay; }
                float wv = 0; foreach (var v in new[] { 10, 20, 40, 80 }) wv += LarguraBotao(v + "×", "rep") + 4; wv -= 4;
                var stH = Css("400", 18, BONE); float wh = Mathf.Max(52, Larg(Relogio(repT), stH)), ws = w - wb - wh - wv - 36;
                float nv = SliderCss(new Rect(x + wb + 12, y + (hb - 26) / 2, ws, 26), (float)repT, 0, (float)System.Math.Ceiling(fim), 1);
                if (!medindo) { if (Mathf.Abs(nv - (float)repT) > .01f) repT = nv; repArrastando = GUIUtility.hotControl != 0; Linha(x + wb + 12 + ws + 12, y + (hb - LN(18)) / 2, wh, LN(18), "<b>" + Relogio(repT) + "</b>", stH); }
                float xv = x + w - wv; foreach (var v in new[] { 10, 20, 40, 80 }) { string t = v + "×"; float bw = LarguraBotao(t, repVel == v ? "repOn" : "rep"); if (BotaoCss(new Rect(xv, y + (hb - AlturaBotao("rep")) / 2, bw, AlturaBotao("rep")), t, repVel == v ? "repOn" : "rep")) repVel = v; xv += bw + 4; }
                y += hb;
                // .ltLeg: as cores de cada um e a queda
                var stL = Css("400", 14, MUDO); float xx = x, lh = LN(14);
                for (int j = 0; j <= R.atores.Count; j++)
                {
                    bool q = j == R.atores.Count; string nome = q ? "Queda" : R.atores[j].nome; var st = !q && R.atores[j].eu ? Css("600", 14, BONE) : stL; float iw = 20 + Larg(nome, st);
                    if (xx + iw > x + w && xx > x) { xx = x; y += lh + 4; }
                    if (!medindo) { if (q) NaBase(xx, y + Mathf.Max(Ar(14), Ar(14 * 1.069f)), 14, "✚", Css("400", 14, Color.white)); else Caixa(new Rect(xx, y + MeioX(14) - 1.5f, 14, 3), cores[j]); Linha(xx + 20, y, iw, lh, nome, st); }
                    xx += iw + 14;
                }
                // a fileira da Queda: o ✚ vem da fonte de símbolos do sistema (subida 1,069, descida ,293 em em), que deixa a
                // linha com 19 px; os itens da fileira se esticam até ela (align-items: stretch)
                y += Mathf.Max(Ar(14), Ar(14 * 1.069f)) + Mathf.Max(Ar(14 * .2f), Ar(14 * .293f));
                // #repEv: os quatro últimos marcos até a hora, o mais novo destacado, em duas colunas
                var mk = R.mkFinal ?? R.mk; var ult = mk.Where(m => m.tipo != "momento" && m.rt <= repT).Reverse().Take(4).ToList();
                // #repEv (ul.ltEv): o ul vazio ainda tem a margem de cima, que não some no 18 da fileira (columns faz dele um
                // contexto próprio)
                var stTb = Css("600", 14, GOLD); y += 6 + ListaMarcos(x, y + 6, w, ult.Select(m => new[] { Relogio(m.rt), m.txt }).ToList(), i => Css("400", 14, i == 0 ? BONE : Hx("#cfc6b6")), stTb);
                y += FileiraBotoes(x, y, w, "Voltar ao resultado", "go", true, (System.Action)(() => repAberto = false));
                return y - y0;
            });
            if (repAberto && rmTela.width > 0) DesenharReplay(rmTela, R, cores);   // os rastros giram: fora de qualquer grupo
        }

        // ---------- relatorioScreen ----------
        GUIStyle sPergunta;
        void TelaRelatorioCss()
        {
            var log = LogDaNoite();
            if (log == null)
            {
                PainelTela("rel-carregando", 800, (x, y, w) => { float y0 = y; y += H2(x, y, w, "Relatório do playtest"); y += P(x, y, w, "Buscando o registro da noite no servidor…"); y += FileiraBotoes(x, y, w, "Voltar", "ghost", true, (System.Action)(() => relAberto = false)); return y - y0; });
                return;
            }
            string chave = g.matchId + "|" + log.Count; if (relCache == null || relChave != chave) { relCache = DadosRelatorio(); relChave = chave; }
            var D = relCache; var resp = Respostas(); var vv = D["vencedor"] as Dictionary<string, object>; var mud = ((List<object>)D["mudancas"]).Cast<string>().ToArray();
            string why = vv != null ? (string)vv["why"] : ""; if (why.Length > 0) why = char.ToUpper(why[0]) + why.Substring(1);
            var tres = new[] { new Col(), new Col(true), new Col(false, true) };
            PainelTela("rel" + chave, 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, "Relatório do playtest");
                y += Lede(x, y, w, why + ". Regras: " + (mud.Length > 0 ? string.Join(" · ", mud) : "normais") + ".");
                // mb: a margem de baixo já somada, que colapsa com a de cima da peça seguinte (as tabelas têm 10 em cima e embaixo)
                float mb = 14;
                var mets = (List<object>)D["metricas"];
                foreach (Dictionary<string, object> M in mets)
                {
                    var I = Dd(M["investigacao"]); var E = Dd(M["engano"]); var C = Dd(M["combate"]); var S = Dd(M["selamento"]); var F = Dd(M["ferramentas"]); var Se = Dd(M["sessao"]);
                    if (mets.Count > 1) { y -= Mathf.Min(mb, 14); y += H3(x, y, w, "Rodada " + M["rodada"]); mb = 6; }
                    string como; object cd = I["comoDescobriu"];
                    y -= Mathf.Min(mb, 10); mb = 10; y += TabelaCols(x, y, w, tres, new[] { "Investigação", "", "O que revela" }, new List<string[]> {
                        new[] { "Primeira verdade descoberta pela caça", Tm(I["primeiraVerdade"]), cd != null ? "por " + (COMO_DESCOBRIU.TryGetValue((string)cd, out como) ? como : (string)cd) + ". Se as pistas são úteis e a investigação tem ritmo." : "A caça nunca descobriu um altar verdadeiro." },
                        new[] { "Pistas novas para a caça", I["pistasNovas"].ToString(), "sinais, EVP, sentinelas, Lanterna, rastros, Sal, Sensor com sinal" },
                        new[] { "Maior trecho sem pista nova", Tm(I["maiorIntervaloSemPista"]), "Muito longo: a caça passou tempo andando sem aprender nada." },
                        new[] { "Trecho médio sem pista nova", Tm(I["intervaloMedioSemPista"]), "" } });
                    var rits = (List<object>)M["rituais"]; var L = new List<string[]>();
                    foreach (Dictionary<string, object> R in rits)
                    {
                        string res = (string)R["resultado"], cc; var err = ((List<object>)R["altaresErrados"]).Select(z => z as string).Distinct().ToArray(); var saiu = ((List<object>)R["cultoSaiu"]).Select(z => z as string).ToArray();
                        L.Add(new[] { (string)R["altar"], Tm(R["inicio"]), Sg(R["localizado"]), Sg(R["chegada"]), R["tentativasSelo"].ToString(), res == "farol" ? "Farol" : res == "fenda" ? "Fenda" : "—", (CACA_TXT.TryGetValue((string)R["caca"], out cc) ? cc : "") + (err.Length > 0 ? " (passou por " + string.Join(", ", err) + ")" : "") + (saiu.Length > 0 ? "; culto saiu do círculo: " + string.Join(", ", saiu) : "") });
                        string td = TextoDisputa(R); if (td != "") L.Add(new[] { "\u0001" + td });
                    }
                    if (rits.Count == 0) L.Add(new[] { "\u0002Nenhum ritual começou." });
                    y -= Mathf.Min(mb, 10); mb = 10; y += TabelaCols(x, y, w, new[] { new Col(), new Col(true), new Col(true), new Col(true), new Col(true), new Col(), new Col(false, true) }, new[] { "Ritual", "Início", "Localizado", "Caça chegou", "Selos", "Resultado", "A caça" }, L);
                    y += P(x, y, w, "Tempos contados desde o início de cada ritual. A última coluna separa \"nunca chegou\", \"foi a outro altar\", \"chegou perto e não entrou\", \"chegou tarde\" e \"chegou a tempo e perdeu o círculo\".", 15, MUDO, 1.45f, TextAnchor.UpperLeft, 64, true) - Mathf.Min(mb, 12); mb = 0;
                    y -= Mathf.Min(mb, 10); mb = 10; y += TabelaCols(x, y, w, tres, new[] { "Engano e selamento", "", "O que revela" }, new List<string[]> {
                        new[] { "Chamarizes plantados", E["chamarizes"].ToString(), E["chamarizesDesmascarados"] + " desmascarados pela Lanterna, " + E["chamarizesQueimados"] + " queimados (prenderam a caça por " + Mathf.RoundToInt((float)Nm(E["segundosPresosEmChamariz"])) + " s), " + E["evpEmChamariz"] + " EVP enganados" },
                        new[] { "Purgas de altar verdadeiro", E["purgasDeAltarVerdadeiro"].ToString(), "a caça acertou um altar antes do ritual" },
                        new[] { "Transferências", E["transferencias"].ToString(), F["rastrosVistos"] + " marcas de arrasto vistas, " + F["rastrosLidos"] + " lidas" },
                        new[] { "Tentativas de selo", S["tentativas"].ToString(), S["selados"] + " viraram Farol. Interrupções: " + TextoInterrupcoes(Dd(S["interrupcoes"])) } });
                    y -= Mathf.Min(mb, 10); mb = 10; y += TabelaCols(x, y, w, tres, new[] { "Combate e ferramentas", "", "O que revela" }, new List<string[]> {
                        new[] { "Quedas", C["quedas"].ToString(), "primeira aos " + Tm(C["primeiraQueda"]) + "; " + C["execucoes"] + " execuções, " + C["sangraram"] + " sangraram, " + C["reanimacoes"] + " reanimações. Se cair e reanimar é uma escolha tática." },
                        new[] { "Ferramentas da caça", (Nm(F["sinaisLidos"]) + Nm(F["sentinelasAcesas"]) + Nm(F["sensor"]) + Nm(F["evp"]) + Nm(F["lanterna"]) + Nm(F["sal"])).ToString(), F["sinaisLidos"] + " sinais, " + F["sentinelasAcesas"] + " sentinelas, " + F["sensor"] + " Sensor (" + F["sensorComSinal"] + " com sinal), " + F["evp"] + " EVP, " + F["lanterna"] + " Lanterna, " + F["sal"] + " Sal" },
                        new[] { "Comunicação", (Nm(F["pings"]) + Nm(F["frases"])).ToString(), F["pings"] + " pings, " + F["frases"] + " frases rápidas" },
                        new[] { "Saídas e reconexões", (Nm(Se["saidas"]) + Nm(Se["reconexoes"])).ToString(), Se["saidas"] + " saíram, " + Se["reconexoes"] + " voltaram" } });
                }
                y -= Mathf.Min(mb, 14); y += H3(x, y, w, "Perguntas para quem jogou") - 6;
                // .perguntas: o rótulo de 15 px e a caixa de duas linhas (IM Fell 15 px, fundo escuro, borda fina)
                if (sPergunta == null) { sPergunta = new GUIStyle(Css("serif", 15, BONE)) { wordWrap = true, padding = new RectOffset(9, 9, 7, 7), border = new RectOffset(1, 1, 1, 1), clipping = TextClipping.Clip }; var t = EstiloBotao("choice").normal.background; sPergunta.normal.background = sPergunta.focused.background = sPergunta.hover.background = t; sPergunta.normal.textColor = sPergunta.focused.textColor = BONE; }
                var stQ = Css("400", 15, BONE); float ha = 2 * AlturaNormal(Css("serif", 15, BONE)) + 14;
                for (int i = 0; i < PERGUNTAS_PLAYTEST.Length; i++)
                {
                    y += 8; float hl = Ps(x, y, w, PERGUNTAS_PLAYTEST[i], stQ, 18); y += hl + 4;
                    if (!medindo) { string novo = GUI.TextArea(new Rect(x, y, w, ha), resp[i] ?? "", 600, sPergunta); if (novo != resp[i]) { resp[i] = novo; GuardarRespostas(); } }
                    y += ha;
                }
                y += 8 + P(x, y, w, "Cada pessoa responde no próprio aparelho. Mande o relatório para quem organiza o playtest: o arquivo traz o registro inteiro da noite.", 15, MUDO, 1.45f, TextAnchor.UpperCenter, 64, true);
                y += FileiraBotoes(x, y, w, "Copiar como texto", "ghost", true, (System.Action)(() => { GUIUtility.systemCopyBuffer = TextoRelatorio(DadosRelatorio()); relMsg = "Texto copiado."; }),
                    "Baixar relatório (.json)", "go", true, (System.Action)(() =>
                    {
                        try { string arq = System.IO.Path.Combine(Application.persistentDataPath, "ritual-reversal-relatorio-" + g.matchId + ".json"); System.IO.File.WriteAllText(arq, Noite.ParaJson(DadosRelatorio())); relMsg = "Arquivo salvo em " + arq; }
                        catch (System.Exception e) { relMsg = "Não consegui salvar: " + e.Message; }
                    }),
                    "Voltar", "ghost", true, (System.Action)(() => relAberto = false));
                if (relMsg != "") y += P(x, y, w, relMsg, 15, MUDO, 1.45f, TextAnchor.UpperCenter, 64, true);
                return y - y0;
            });
        }
    }
}
