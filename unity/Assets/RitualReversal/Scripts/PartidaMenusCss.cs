// Os menus do título com o CSS do navegador: Aprender a jogar (aprenderScreen), Como jogar (comoJogarScreen, com os
// passos numerados em duas colunas), Novidades (novidadesScreen) e o perfil (perfilScreen: tabelas table.res, últimas
// partidas e as conquistas em quatro colunas). Mais o .lede genérico e a tabela.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        // .lede: IM Fell itálico 20 px apagado, no meio, caixa de 64ch, 14 px embaixo
        float Lede(float x, float y, float w, string txt)
        {
            var st = Css("serifIt", 20, MUDO, TextAnchor.UpperCenter); float wb = Mathf.Min(w, Larg("0", st) * 64);
            return Ps(x + (w - wb) / 2, y, wb, txt, st, 20 * 1.45f) + 14;
        }
        // table.res: 17 px, células com padding 6 × 8 e a linha de baixo; a primeira coluna leva o resto da largura
        float Tabela(float x, float y, float w, string[] cab, List<string[]> linhas, float[] largNum, Color?[] corPrimeira = null)
        {
            float lh = LN(17f), hl = 6 + lh + 6 + 1, yy = y + 10; var stTh = Css("500", 17, MUDO); var stTd = Css("400", 17, BONE);
            System.Action<string[], bool, Color?> linha = (cel, th, c0) =>
            {
                if (!medindo)
                {
                    float xd = x + w;
                    for (int i = cel.Length - 1; i >= 1; i--) { float cw = largNum[i - 1]; xd -= cw; Linha(xd + 8, yy + 6, cw - 16, lh, cel[i], th ? Css("500", 17, MUDO, TextAnchor.UpperRight) : Css("400", 17, BONE, TextAnchor.UpperRight)); }
                    Linha(x + 8, yy + 6, xd - x - 16, lh, cel[0], th ? stTh : c0.HasValue ? Css("400", 17, c0.Value) : stTd);
                    Caixa(new Rect(x, yy + hl - 1, w, 1), Hx("#333a42"));
                }
                yy += hl;
            };
            if (cab != null) linha(cab, true, null);
            for (int i = 0; i < linhas.Count; i++) linha(linhas[i], false, corPrimeira != null && i < corPrimeira.Length ? corPrimeira[i] : null);
            return yy - y + 10;
        }

        // ---------- aprenderScreen ----------
        void TelaAprenderCss()
        {
            PainelTela("aprender", 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, "Aprender a jogar", TextAnchor.UpperCenter);
                y += Lede(x, y, w, "Uma noite guiada, passo a passo, contra bots mansos. Escolha o lado.") - 14;
                var itens = new List<string[]> { new[] { Cor("Como Caçador", GOLD), "Ler santuários, acender sentinelas, usar o sensor e a lanterna, purgar altares e selar um ritual.", "" }, new[] { Cor("Como Cultista", CrimsonHi), "Pegar reagentes, consagrar altares, cumprir tarefas, iniciar um ritual e canalizar no círculo.", "" } };
                y += 4 + Cartoes(x, y, w, 2, itens, i => false, i => IniciarSolo(i == 0 ? "H" : "C", "treino", true, false), true);
                y += FileiraBotoes(x, y, w, "Voltar", "ghost", true, (System.Action)(() => { telaMenu = null; som.Tocar("click"); }));
                return y - y0;
            });
        }

        // ---------- comoJogarScreen ----------
        // .passo: o número numa caixa de 28 px com borda dourada, o título e o texto ao lado
        float Passo(float x, float y, float w, int n, string t, string d)
        {
            var stB = Css("600", 16, GOLD, TextAnchor.UpperCenter); var stT = Css("600", 18, BONE); var stD = Css("400", 16, Hx("#c9c1b2"));
            float xt = x + 28 + 12, wt = w - 40; if (!medindo) { var r = new Rect(x, y, 28, 28); Moldura(r, GOLD); Linha(r.x, r.y + 4, 28, LN(16), n.ToString(), stB); Linha(xt, y, wt, LN(18f), t, stT); }
            float h = LN(18f) + Ps(xt, y + LN(18f), wt, d, stD, 16 * 1.4f); return Mathf.Max(28, h);
        }
        void TelaComoJogarCss()
        {
            System.Func<string, string> kb = EntradaUnity.Nome;
            PainelTela("como", 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, "Como jogar", TextAnchor.UpperCenter);
                y += Lede(x, y, w, UmaNoite ? "Uma equipe celebra, a outra caça. Os Cultistas vencem completando dois dos três rituais; os Caçadores, selando dois ou resistindo até o amanhecer." : "Duas rodadas. Na primeira sua equipe caça; na segunda, celebra. Vence quem completar mais rituais como Cultista.");
                y += Lede(x, y, w, "Cada partida é uma noite de até 18 minutos, em três momentos: o Crepúsculo, a Vigília e a Hora Morta. Quando uma equipe leva dois rituais, a noite acaba ali.");
                y += Lede(x, y, w, "A catedral fica no meio de uma floresta. As trilhas e clareiras pegam luar; a mata fechada é breu. Dois altares ficam lá fora: os Menires e o Carvalho Oco.");
                var cult = new[] { new[] { "Crepúsculo: prepare-se", "Colha ervas na floresta e profane túmulos no cemitério, ao leste da catedral. Tarefas rendem óbolos; negocie reagentes com a Carpideira, no oratório perto do seu acampamento. Os reagentes ficam na mata, ao sul do desfiladeiro: cada lugar tem o seu (sal negro na Pedreira, raiz de mandrágora no Charco, velas de cera na Encruzilhada…)." },
                    new[] { "Consagre em silêncio", "Você escolheu 3 de 6 altares. Consagrar gasta 1 reagente. Cuidado com as sentinelas acesas: elas denunciam você." },
                    new[] { "Vigília: os rituais começam", "Inicie com mais um reagente e fique no círculo. Quem ataca não canaliza: cada sigilo pausa a sua canalização por um instante, então em dupla um celebra e o outro defende. A Transferência deixa marcas de arrasto no altar de origem. Chamarizes (" + kb("use") + " num altar não escolhido) confundem a investigação." },
                    new[] { "Hora Morta: o Grande Ritual", "Se estiver 1 a 1, o ritual decisivo só começa agora, e a catedral inteira ouve. Vença dois dos três antes do amanhecer." } };
                var caca = new[] { new[] { "Crepúsculo: prepare-se", "Cada sinal lido num santuário descarta um altar que o culto não escolheu. Acenda sentinelas e negocie munição, óleo e sinalizadores com o Ermitão, na cabana perto do acampamento." },
                    new[] { "Investigue", kb("sensor") + " lê sinais por perto, o EVP sussurra junto de altares consagrados, e a Lanterna (" + kb("lantern") + ") mostra resíduo só nos verdadeiros." },
                    new[] { "Vigília: purgue e sele", "Segure " + kb("use") + " num altar consagrado para purgar. Depois do Presságio, o coro ecoa o setor do ritual: dois altares possíveis, e o que você já descartou decide para onde correr. Sele em dupla. Marcas de arrasto num altar denunciam uma Transferência." },
                    new[] { "Resista até o amanhecer", "Cada ritual selado força os Cultistas a recomeçar em outro altar. Selem dois dos três rituais, ou resistam até o amanhecer." } };
                // .cols: duas colunas com 18 px entre elas
                float cw = (w - 18) / 2, hMax = 0;
                for (int c = 0; c < 2; c++)
                {
                    float cx = x + c * (cw + 18), cy = y; cy += H3(cx, cy, cw, c == 0 ? "Como Cultista" : "Como Caçador", c == 0 ? CrimsonHi : GOLD) - 6;
                    var L = c == 0 ? cult : caca; for (int i = 0; i < L.Length; i++) cy += 10 + Passo(cx, cy + 10, cw, i + 1, L[i][0], L[i][1]) + (i == L.Length - 1 ? 10 : 0);
                    hMax = Mathf.Max(hMax, cy - y);
                }
                y += hMax;
                y += P(x, y, w, "Caído, um aliado pode te reanimar. O inimigo pode te executar, mas levar dano recomeça a execução: atire em quem executa o seu aliado. O som diz muito: use fones (as legendas mostram de que lado vem cada som). " + kb("ping") + " marca um ponto, um altar ou um inimigo para o aliado, e " + kb("falas") + " abre as frases rápidas; o bot aliado também obedece.", 15, MUDO, 1.45f, TextAnchor.UpperCenter, 64, true);
                y += FileiraBotoes(x, y, w, "Entendi", "go", true, (System.Action)(() => { telaMenu = null; som.Tocar("click"); }));
                return y - y0;
            });
        }

        // ---------- novidadesScreen ----------
        void TelaNovidadesCss()
        {
            PainelTela("novidades", 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, "Novidades da v5.8", TextAnchor.UpperCenter); y += Lede(x, y, w, "O que mudou desde a última versão.") - 14 + 12;
                // .novidades: duas colunas (10 × 22), a barra dourada e o título serifado
                float cw = (w - 22) / 2; var stB = Css("serif", 19, GOLD_HI); var stS = Css("400", 15, Hx("#cfc6b6"));
                for (int i = 0; i < NOVIDADES.Length; i += 2)
                {
                    float hl = 0;
                    for (int j = i; j < Mathf.Min(NOVIDADES.Length, i + 2); j++)
                    {
                        float cx = x + (j - i) * (cw + 22), h = 2 + AlturaNormal(stB) + AlturaParagrafo(NOVIDADES[j][1], stS, cw - 14, 18) + 2; hl = Mathf.Max(hl, h);
                        if (!medindo) { Caixa(new Rect(cx, y, 2, h), GOLD); Linha(cx + 14, y + 2, cw - 14, AlturaNormal(stB), NOVIDADES[j][0], stB); Paragrafo(cx + 14, y + 2 + AlturaNormal(stB), cw - 14, 18, NOVIDADES[j][1], stS); }
                    }
                    y += hl + 10;
                }
                y += 2;
                y += FileiraBotoes(x, y, w, "Voltar", "go", true, (System.Action)(() => { telaMenu = null; som.Tocar("click"); }));
                return y - y0;
            });
        }

        // ---------- perfilScreen ----------
        void TelaPerfilCss()
        {
            var p = Perfil(); System.Func<double, double, string> pct = (a, b) => b > 0 ? Mathf.RoundToInt((float)(a / b * 100)) + "%" : "—";
            int partidas = (int)Nn(p, "partidas"), vit = (int)Nn(p, "vitorias"), der = (int)Nn(p, "derrotas"), emp = (int)Nn(p, "empates"), nc = O(p, "conq").Count;
            PainelTela("perfil", 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, "Seu perfil", TextAnchor.UpperCenter);
                y += Lede(x, y, w, partidas + (partidas == 1 ? " partida" : " partidas") + " · " + vit + (vit == 1 ? " vitória" : " vitórias") + ", " + der + (der == 1 ? " derrota" : " derrotas") + (emp > 0 ? ", " + emp + (emp == 1 ? " empate" : " empates") : "") + " · " + nc + " de " + CONQ.Length + " conquistas");
                // as tabelas são table.res com o layout automático do CSS (TabelaCols): as colunas de números se alargam
                float cw = (w - 18) / 2, ya = y, yb = y; var c3 = new[] { new Col(), new Col(true), new Col(true) };
                ya += H3(x, ya, cw, "Por papel") - 6; var pa = O(p, "papel");
                ya += TabelaCols(x, ya, cw, c3, new[] { "", "Rodadas", "Vencidas" }, new List<string[]> { new[] { Cor("Caçador", GOLD), Nn(O(pa, "H"), "rodadas").ToString(), pct(Nn(O(pa, "H"), "vitorias"), Nn(O(pa, "H"), "rodadas")) }, new[] { Cor("Cultista", CrimsonHi), Nn(O(pa, "C"), "rodadas").ToString(), pct(Nn(O(pa, "C"), "vitorias"), Nn(O(pa, "C"), "rodadas")) } });
                ya += H3(x, ya, cw, "Classes") - 6 - 10; var cl = O(p, "classes");
                // o small: font-size smaller, 17 / 1,2
                ya += TabelaCols(x, ya, cw, c3, new[] { "", "Rodadas", "Vencidas" }, Regras.CLASSES.Select(kv => { object o; var c = cl.TryGetValue(kv.Key, out o) ? o as Dictionary<string, object> : null; return new[] { kv.Value.name + " <size=14>" + Cor(kv.Value.team == "H" ? "Caçador" : "Cultista", MUDO) + "</size>", (c != null ? Nn(c, "rodadas") : 0).ToString(), c != null ? pct(Nn(c, "vitorias"), Nn(c, "rodadas")) : "—" }; }).ToList());
                float xb = x + cw + 18; yb += H3(xb, yb, cw, "Recordes numa partida") - 6; var rec = O(p, "recordes");
                yb += TabelaCols(xb, yb, cw, new[] { new Col(), new Col(true) }, null, new[] { new[] { "quedas", "Inimigos derrubados" }, new[] { "reanimacoes", "Aliados reanimados" }, new[] { "purgas", "Altares purgados" }, new[] { "rituais", "Rituais iniciados" }, new[] { "tarefas", "Tarefas cumpridas" } }.Select(k => new[] { k[1], Nn(rec, k[0]).ToString() }).ToList());
                var hist = Lst(p, "hist");
                if (hist.Count > 0)
                {
                    yb += H3(xb, yb, cw, "Últimas partidas") - 6 - 10; var L = new List<string[]>(); var cores = new List<Color?>();
                    foreach (var o in hist.Take(8))
                    {
                        var h = o as Dictionary<string, object>; if (h == null) continue; var data = new System.DateTime(1970, 1, 1).AddMilliseconds(Nn(h, "data")).ToLocalTime(); object rv; var rods = h.TryGetValue("rodadas", out rv) ? rv as List<object> : null;
                        string cls = rods == null ? "" : string.Join(" / ", rods.Select(z => { var d = z as Dictionary<string, object>; object c; string k = d != null && d.TryGetValue("cls", out c) ? c as string : ""; ClasseDef C; return k != null && Regras.CLASSES.TryGetValue(k, out C) ? C.name : k; }).ToArray());
                        string res = h.TryGetValue("resultado", out rv) ? rv as string : ""; object md;
                        L.Add(new[] { data.ToString("dd/MM/yyyy") + (h.TryGetValue("modo", out md) && (md as string) == "online" ? " · online" : ""), cls, Cor(res, res == "vitória" ? GOLD_HI : res == "derrota" ? CrimsonHi : BONE) });
                    }
                    yb += TabelaCols(xb, yb, cw, new[] { new Col(), new Col(), new Col(true) }, null, L);
                }
                y = Mathf.Max(ya, yb);
                y += H3(x, y, w, "Conquistas") - 6; var cq = O(p, "conq");
                // .conqs: quatro colunas, 6 px entre elas; as que faltam ficam a meia opacidade
                float qw = (w - 18) / 4; var stQb = Css("serif", 16, BONE); var stQs = Css("400", 13, MUDO); y += 6;
                for (int i = 0; i < CONQ.Length; i += 4)
                {
                    float hl = 0; for (int j = i; j < Mathf.Min(CONQ.Length, i + 4); j++) hl = Mathf.Max(hl, 1 + 6 + AlturaNormal(stQb) + AlturaParagrafo(CONQ[j][2], stQs, qw - 18, 12.5f * 1.25f) + 6 + 1);
                    for (int j = i; j < Mathf.Min(CONQ.Length, i + 4); j++)
                    {
                        if (medindo) continue; bool ok = cq.ContainsKey(CONQ[j][0]); var r = new Rect(x + (j - i) * (qw + 6), y, qw, hl); var c0 = GUI.color; if (!ok) GUI.color = new Color(1, 1, 1, .5f);
                        Caixa(r, new Color(14 / 255f, 16 / 255f, 19 / 255f, .6f)); Moldura(r, ok ? GOLD : Hx("#2c3239"));
                        Linha(r.x + 9, r.y + 7, qw - 18, AlturaNormal(stQb), CONQ[j][1], ok ? Css("serif", 16, GOLD_HI) : stQb); Paragrafo(r.x + 9, r.y + 7 + AlturaNormal(stQb), qw - 18, 12.5f * 1.25f, CONQ[j][2], stQs); GUI.color = c0;
                    }
                    y += hl + 6;
                }
                y -= 6;
                if (!confirmarApagar) y += FileiraBotoes(x, y, w, "Apagar perfil", "ghost", true, (System.Action)(() => confirmarApagar = true), "Voltar", "go", true, (System.Action)(() => { telaMenu = null; confirmarApagar = false; }));
                else
                {
                    y += 10 + P(x, y + 10, w, "Apagar todo o histórico e as conquistas?", 17, BONE, 1.45f, TextAnchor.UpperCenter, 0);
                    y += FileiraBotoes(x, y, w, "Apagar", "go", true, (System.Action)(() => { PlayerPrefs.DeleteKey("rr-perfil"); confirmarApagar = false; }), "Não", "ghost", true, (System.Action)(() => confirmarApagar = false));
                }
                return y - y0;
            });
        }
    }
}
