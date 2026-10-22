// O fim da noite com o CSS do navegador: o resumo (summaryScreen) e o resultado (finalScreen), com os lances da noite
// (.lances), os destaques (.premios), o placar por jogador (table.placar), a linha do tempo (.ltBox: o gráfico, a legenda
// e os marcos em duas colunas), o registro da partida (statsTable) e o bloco do desafio (.desafio).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        // .lances: grade de colunas de 220 px no mínimo, 8 px entre elas; a borda dourada (carmim no lance do culto)
        float LancesCss(float x, float y, float w, List<Registro> log)
        {
            var L = log != null ? Noite.Lances(log) : null; if (L == null || L.Count == 0) return 0; var eu = Eu(); float y0 = y;
            y += H3(x, y, w, "Lances da noite") - 6 + 6;
            int n = Mathf.Max(1, Mathf.FloorToInt((w + 8) / (220 + 8))); float cw = (w - 8 * (n - 1)) / n; var stB = Css("serif", 17, BONE); var stS = Css("400", 13, Hx("#c9bfae")); var stT = Css("400", 12, MUDO, TextAnchor.UpperRight);
            for (int i = 0; i < L.Count; i += n)
            {
                float hl = 0; for (int j = i; j < Mathf.Min(L.Count, i + n); j++) hl = Mathf.Max(hl, 7 + AlturaNormal(stB) + AlturaParagrafo(L[j].sub, stS, cw - 22, 19.2f) + 7);
                for (int j = i; j < Mathf.Min(L.Count, i + n); j++)
                {
                    if (medindo) continue; var l = L[j]; bool meu = eu != null && l.quem.Contains(eu.name); var r = new Rect(x + (j - i) * (cw + 8), y, cw, hl);
                    Caixa(r, meu ? new Color(60 / 255f, 46 / 255f, 20 / 255f, .55f) : new Color(16 / 255f, 14 / 255f, 12 / 255f, .75f)); Caixa(new Rect(r.x, r.y, 2, r.height), l.culto ? CrimsonHi : GOLD_HI);
                    Linha(r.x + 12, r.y + 7, cw - 22, 19.2f, Noite.Relogio(l.t), stT); Linha(r.x + 12, r.y + 7, cw - 22 - Larg(Noite.Relogio(l.t), stT) - 6, AlturaNormal(stB), l.tit, stB);
                    Paragrafo(r.x + 12, r.y + 7 + AlturaNormal(stB), cw - 22, 19.2f, l.sub, stS);
                }
                y += hl + 8;
            }
            return y - 8 + 14 - y0;
        }
        // .premios: três colunas de 8 px; o meu com a borda dourada
        float PremiosCss(float x, float y, float w, List<Registro> log)
        {
            var P = log != null ? Noite.Premios(log) : null; if (P == null || P.Count == 0) return 0; var eu = Eu(); float y0 = y;
            y += H3(x, y, w, "Destaques da noite") - 6 + 6;
            float cw = (w - 16) / 3; var stB = Css("serif", 18, GOLD_HI); var stN = Css("400", 17, BONE); var stM = Css("400", 13, MUDO); float h = 1 + 8 + AlturaNormal(stB) + 20.4f + 15.6f + 8 + 1;
            var lista = P.Take(6).ToList();
            for (int i = 0; i < lista.Count; i++)
            {
                var r = new Rect(x + (i % 3) * (cw + 8), y + (i / 3) * (h + 8), cw, h); if (medindo) continue; var p = lista[i]; bool meu = eu != null && p.quem == eu.name;
                Caixa(r, new Color(14 / 255f, 16 / 255f, 19 / 255f, .8f)); Moldura(r, meu ? GOLD_HI : BORDA_OP); if (meu) Moldura(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), new Color(GOLD_HI.r, GOLD_HI.g, GOLD_HI.b, .4f));
                float yy = r.y + 9; Linha(r.x + 11, yy, cw - 22, AlturaNormal(stB), p.nome, stB); yy += AlturaNormal(stB); Linha(r.x + 11, yy, cw - 22, 20.4f, p.quem, stN); yy += 20.4f;
                Linha(r.x + 11, yy, cw - 22, 15.6f, p.verbo + " " + p.n + " " + (p.n == 1 ? p.um : p.varios), stM);
            }
            y += ((lista.Count + 2) / 3) * (h + 8) - 8;
            return y - y0;
        }
        static readonly string[] COL_PLACAR_CSS = { "Derrubou", "Caiu", "Reanimou", "Altares", "Tarefas", "Compras" };
        float PlacarCss(float x, float y, float w, List<Registro> log)
        {
            if (log == null) return 0; var rods = Noite.Placar(log); var eu = Eu(); float y0 = y; if (rods.Count == 0) return 0;
            foreach (var P in rods)
            {
                y += H3(x, y, w, UmaNoite || rods.Count < 2 ? "Placar da noite" : "Placar da rodada " + P.r) - 6 - 10 + 6;
                float lh = 17 * 1.2f, hl = 6 + lh + 6 + 1, yy = y + 10; float[] cols = { 170, 150 }; float numW = (w - cols[0] - cols[1]) / 6;
                System.Action<string[], bool, Color?, bool> linha = (cel, th, c0, meu) =>
                {
                    if (!medindo)
                    {
                        if (meu) Caixa(new Rect(x, yy, w, hl - 1), new Color(210 / 255f, 173 / 255f, 98 / 255f, .08f));
                        var st = th ? Css("500", 17, MUDO) : Css("400", 17, BONE); var stn = th ? Css("500", 17, MUDO, TextAnchor.UpperRight) : Css("400", 17, BONE, TextAnchor.UpperRight);
                        Linha(x + 8, yy + 6, cols[0] - 16, lh, cel[0], th ? st : Css("600", 17, c0 ?? BONE)); Linha(x + cols[0] + 8, yy + 6, cols[1] - 16, lh, cel[1], st);
                        for (int i = 0; i < 6; i++) Linha(x + cols[0] + cols[1] + i * numW + 8, yy + 6, numW - 16, lh, cel[2 + i], stn);
                        Caixa(new Rect(x, yy + hl - 1, w, 1), Hx("#333a42"));
                    }
                    yy += hl;
                };
                linha(new[] { "Jogador", "Classe" }.Concat(COL_PLACAR_CSS).ToArray(), true, null, false);
                foreach (var L in P.linhas) linha(new[] { L.nome + (L.humano ? "" : " <size=12>" + Cor("bot", MUDO) + "</size>"), L.classe }.Concat(L.v.Select(v => v.ToString())).ToArray(), false, L.team == "H" ? GOLD_HI : CrimsonHi, eu != null && L.nome == eu.name);
                y = yy + 10;
            }
            return y - y0;
        }
        // .ltBox: o gráfico (a mesma textura do PartidaNoite), os nomes dos momentos, os ícones dos marcos, a legenda e os marcos
        float GraficoCss(float x, float y, float w, int r)
        {
            RodadaLT R; if (!LT.rod.TryGetValue(r, out R) || R.am.Count == 0) return 0; float y0 = y;
            var log = LogDaNoite(); var mk = Noite.MarcosFinais(R, log, r); double T = Noite.DuracaoGrafico(R);
            y += H3(x, y, w, UmaNoite ? "A noite" : "A noite, rodada " + r) - 6 + 6;   // .ltBox h3: margem 14 em cima e 2 embaixo; o svg, 6
            float s = w / Noite.GW, hs = 180 * s;
            if (!medindo)
            {
                string chave = LT.id + "|" + r + "|" + R.am.Count; Texture2D tex;
                if (!texGraficos.TryGetValue(chave, out tex)) { foreach (var t in texGraficos.Values) Destroy(t); texGraficos.Clear(); texGraficos[chave] = tex = Noite.Grafico(R, g.lab, g.altars.Count); }
                float yg = y + 14 * s; System.Func<double, float> X = t => x + Noite.GX(t, T) * s;
                GUI.DrawTexture(new Rect(x, yg, w, Noite.GH * s), tex);
                for (int i = 0; i < CFG.momentoAte.Length; i++)
                {
                    double a = i > 0 ? Noite.FimMomento(g.lab, i - 1) : 0; if (a >= T) continue;
                    NaBase(X((a + System.Math.Min(Noite.FimMomento(g.lab, i), T)) / 2) - 100, yg + (Noite.GH + 14) * s, 200, CFG.momentoNome[i], Css("400", Mathf.RoundToInt(11 * s), Hx("#8f877b"), TextAnchor.UpperCenter));
                }
                foreach (var m in mk) { string[] ic; if (!Noite.IC.TryGetValue(m.tipo, out ic)) ic = new[] { "•", "#cccccc" }; NaBase(X(m.rt) - 20, yg - 2 * s, 40, ic[0], Css("400", Mathf.RoundToInt(12 * s), Hx(ic[1]), TextAnchor.UpperCenter)); }
            }
            y += hs + 2;
            // .ltLeg: os altares que tiveram ritual e o selamento, 14 px, quebrando
            var stL = Css("400", 14, MUDO); float xx = x, lh = 16.8f; var itens = new List<KeyValuePair<string, Color?>>();
            for (int i = 0; i < g.altars.Count; i++) if (R.am.Any(q => i < q.a.Length && q.a[i] != null)) itens.Add(new KeyValuePair<string, Color?>(g.altars[i].name, Hx(Noite.COR_ALT[i % 6])));
            itens.Add(new KeyValuePair<string, Color?>("Selamento", null));
            foreach (var it in itens)
            {
                float iw = 14 + 6 + Larg(it.Key, stL); if (xx + iw > x + w && xx > x) { xx = x; y += lh + 4; }
                if (!medindo) { if (it.Value.HasValue) Caixa(new Rect(xx, y + lh / 2 - 1.5f, 14, 3), it.Value.Value); else for (int k = 0; k < 2; k++) Caixa(new Rect(xx + k * 7, y + lh / 2 - 1.5f, 4, 3), GOLD_HI); Linha(xx + 20, y, iw, lh, it.Key, stL); }
                xx += iw + 14;
            }
            y += lh;
            // .ltEv: os marcos em duas colunas (22 px), sem os momentos
            var ev = mk.Where(m => m.tipo != "momento").Take(12).ToList();
            if (ev.Count > 0)
            {
                y += 6; float cw = (w - 22) / 2; var stE = Css("400", 14, Hx("#cfc6b6")); var stB = Css("600", 14, GOLD); int meia = (ev.Count + 1) / 2; float hy = 0;
                for (int c = 0; c < 2; c++)
                {
                    float yy = y;
                    for (int i = c * meia; i < Mathf.Min(ev.Count, (c + 1) * meia); i++)
                    {
                        string t = Noite.Relogio(ev[i].rt); float wt = Larg(t, stB) + 4; float hh = AlturaParagrafo(ev[i].txt, stE, cw - wt, 16.8f);
                        if (!medindo) { Linha(x + c * (cw + 22), yy + 2, wt, 16.8f, t, stB); Paragrafo(x + c * (cw + 22) + wt, yy + 2, cw - wt, 16.8f, ev[i].txt, stE); }
                        yy += 2 + hh + 2;
                    }
                    hy = Mathf.Max(hy, yy - y);
                }
                y += hy;
            }
            return y - y0;
        }
        // statsTable: table.res com o cabeçalho "Registro da partida"
        float RegistroCss(float x, float y, float w)
        {
            Estatisticas r1, r2; if (!g.stats.TryGetValue(1, out r1)) return 0; g.stats.TryGetValue(2, out r2);
            var cab = r2 != null ? new[] { "Registro da partida", "Rodada 1", "Rodada 2" } : new[] { "Registro da partida", "Rodada 1" };
            var linhas = LINHAS.Select(l => r2 != null ? new[] { l[1], Valor(r1, l[0]), Valor(r2, l[0]) } : new[] { l[1], Valor(r1, l[0]) }).ToList();
            return Tabela(x, y, w, cab, linhas, r2 != null ? new[] { 120f, 120f } : new[] { 120f });
        }
        // .desafio: os pontos do desafio da noite
        float DesafioCss(float x, float y, float w)
        {
            if (!desAtivo || desFeito == null) return 0; var r = desFeito; float y0 = y; y += 12;
            var stS = Css("400", 12, GOLD); var stB = Css("serif", 34, GOLD_HI); var stP = Css("400", 15, Hx("#cfc6b6")); var stE = Css("400", 15, CrimsonHi);
            string partes = string.Join(" · ", ((List<string>)r["partes"]).ToArray()), em = (bool)r["recorde"] ? "Novo recorde do dia!" : "Recorde do dia: " + r["melhor"];
            float hp = AlturaParagrafo(partes, stP, w - 30, 18), h = 1 + 10 + 14.4f + AlturaNormal(stB) + hp + 18 + 10 + 1;
            if (!medindo)
            {
                var c = new Rect(x, y, w, h); Caixa(c, new Color(38 / 255f, 32 / 255f, 20 / 255f, .6f)); Moldura(c, GOLD); float yy = y + 11;
                Espacada(x + 15, yy, 14.4f, ("Desafio da noite · " + desData).ToUpperInvariant(), stS, .96f); yy += 14.4f; Linha(x + 15, yy, w - 30, AlturaNormal(stB), r["p"] + " pontos", stB); yy += AlturaNormal(stB);
                yy += Paragrafo(x + 15, yy, w - 30, 18, partes, stP); Linha(x + 15, yy, w - 30, 18, em, stE);
            }
            return 12 + h + 12;
        }

        // ---------- summaryScreen ----------
        void TelaResumoCss()
        {
            bool pronto; g.ready.TryGetValue(EU, out pronto); var slot = MeuSlot();
            var sc = g.round == 1 ? g.scores["B"] : g.scores["A"]; bool cultoMeu = slot != null && (g.round == 1 ? "B" : "A") == slot.team;
            PainelTela("resumo" + g.round, 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, UmaNoite ? "O amanhecer" : "Fim da rodada " + g.round);
                if (sc != null) y += P(x, y, w, (cultoMeu ? "Sua equipe completou" : "Os Cultistas adversários completaram") + " <b>" + sc.done + "</b> de 3 rituais" + (sc.lastT.HasValue ? ", o último aos " + Relogio(sc.lastT.Value) : "") + ". " + sc.@sealed + (sc.@sealed == 1 ? " ritual foi selado." : " rituais foram selados."));
                if (g.round == 1 && !UmaNoite) y += 6 + P(x, y + 6, w, "Agora os lados se invertem: quem caçou passa a celebrar.");
                if (!online) { var log = LogDaNoite().Where(e => e.r == g.round).ToList(); y += LancesCss(x, y, w, log); y += PremiosCss(x, y, w, log); }
                y += GraficoCss(x, y, w, g.round); y += RegistroCss(x, y, w);
                y += FileiraBotoes(x, y, w, pronto ? "Aguardando os outros" : g.round == 1 && !UmaNoite ? "Continuar para a rodada 2" : "Ver resultado", "go", !pronto, (System.Action)(() => Acao(new Acao { type = "ready" })));
                string esp = ((online ? ListaProntos() : "") + TextoTempo()).Trim(); if (esp.Length > 0) y += 10 + P(x, y + 10, w, esp, 17, MUDO, 1.45f, TextAnchor.UpperCenter);
                return y - y0;
            });
        }
        // ---------- finalScreen ----------
        void TelaFinalCss()
        {
            var slot = MeuSlot(); var wv = Sim.Winner(g) ?? new Vencedor { why = "" }; bool minha = slot != null && wv.win == slot.team;
            Placar A = slot != null ? g.scores[slot.team] : null, B = slot != null ? g.scores[slot.team == "A" ? "B" : "A"] : null;
            string titulo = wv.win == null ? "Empate" : minha ? "Vitória da sua equipe" : "Vitória dos adversários";
            PainelTela("final" + g.matchId, 800, (x, y, w) =>
            {
                float y0 = y; var stH = Css("serif", 32, wv.win == null ? BONE : minha ? GOLD : CrimsonHi); if (!medindo) Linha(x, y, w, AlturaNormal(stH), titulo, stH); y += AlturaNormal(stH) + 8;
                if (wv.why.Length > 0) y += P(x, y, w, char.ToUpper(wv.why[0]) + wv.why.Substring(1) + ".");
                System.Func<string, Placar, string[]> linha = (n, s) => new[] { n, s.done.ToString(), s.lastT.HasValue ? Relogio(s.lastT.Value) : "—", Mathf.RoundToInt((float)s.maxP * 100) + "%" };
                var L = new List<string[]>(); if (A != null) L.Add(linha(B == null ? "Sua equipe (culto)" : "Sua equipe", A)); if (B != null) L.Add(linha(A == null ? "Adversários (culto)" : "Adversários", B));
                y += Tabela(x, y, w, new[] { "Como Cultistas", "Rituais", "Último ritual", "Maior progresso" }, L, new[] { 90f, 130f, 160f });
                y += DesafioCss(x, y, w);
                var log = LogDaNoite();
                if (online && log == null) y += 6 + P(x, y + 6, w, "Baixando o registro da noite do servidor…", 17, MUDO, 1.45f, TextAnchor.UpperCenter);
                y += LancesCss(x, y, w, log); y += PremiosCss(x, y, w, log); y += PlacarCss(x, y, w, log); y += GraficoCss(x, y, w, 1); y += GraficoCss(x, y, w, 2); y += RegistroCss(x, y, w);
                bool anfitriao = !online || hostCid == EU; var bs = new List<object>();
                if (LT.TemReplay(1)) bs.AddRange(new object[] { "Rever a noite", "ghost", true, (System.Action)(() => AbrirReplay(1)) });
                if (!UmaNoite && LT.TemReplay(2)) bs.AddRange(new object[] { "Rever a rodada 2", "ghost", true, (System.Action)(() => AbrirReplay(2)) });
                bs.AddRange(new object[] { "Relatório do playtest", "ghost", true, (System.Action)(() => { relAberto = true; relMsg = ""; rolRel = Vector2.zero; som.Tocar("click"); }) });
                bs.AddRange(new object[] { "Baixar registro (.json)", "ghost", true, (System.Action)BaixarRegistro });
                if (anfitriao) bs.AddRange(new object[] { "Jogar de novo", "go", true, (System.Action)(() => { repAberto = false; if (!online) { g.matchId = System.DateTime.Now.ToString("yyyyMMdd-HHmmss"); logIni = g.log.Count; } LimparVisual(); Acao(new Acao { type = "restart" }); }) });
                if (online) { if (anfitriao) bs.AddRange(new object[] { "Voltar ao lobby", "ghost", true, (System.Action)(() => Enviar(new JsonEscrita().Txt("t", "lobby"))) }); bs.AddRange(new object[] { "Sair da sala", "ghost", true, (System.Action)(() => SairOnline("")) }); }
                else bs.AddRange(new object[] { "Voltar ao menu", "ghost", true, (System.Action)(() => { g = null; LimparVisual(); }) });
                y += FileiraBotoes(x, y, w, bs.ToArray());
                if (online && !anfitriao) y += 10 + P(x, y + 10, w, "O anfitrião decide o próximo passo.", 17, MUDO, 1.45f, TextAnchor.UpperCenter);
                if (logMsg != "") y += 10 + P(x, y + 10, w, logMsg, 17, MUDO, 1.45f, TextAnchor.UpperCenter);
                return y - y0;
            });
        }
    }
}
