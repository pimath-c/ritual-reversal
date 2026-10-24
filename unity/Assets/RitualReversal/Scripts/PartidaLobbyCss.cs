// A sala online e a reconexão com o CSS do navegador (lobbyScreen e reconexaoScreen): o código grande da sala, as duas
// colunas de vagas (.slots), a dificuldade dos bots (.difOnline), o laboratório dobrável, o chat (#chatLista e a .linha
// com o campo e o Enviar) e a fileira de botões; a reconexão com o texto no meio e a contagem em .wait.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        int chatVisto = -1;
        void TelaLobbyCss()
        {
            bool anfitriao = hostCid == EU; string chave = "lobby" + string.Join(",", lobbySlots.Select(s => s.name + "|" + s.cid)) + hostCid + lobbyDif;
            PainelTela(chave, 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, "Sala", TextAnchor.UpperCenter);
                // p.bigcode: Barlow 700 de 54 px, letter-spacing .12em, no meio, na linha de 1,45 do .panel p
                var stC = Css("700", 54, GOLD_HI); float esp = 54 * .12f, lhC = 54 * 1.45f;
                if (!medindo) { float wc = 0; foreach (var ch in salaCodigo ?? "") wc += Larg(ch.ToString(), stC) + esp; Espacada(x + (w - wc) / 2, y, lhC, salaCodigo ?? "", stC, esp); }
                y += lhC;
                y += Lede(x, y, w, "Mande este código para seus amigos. Vagas sem jogador ficam com bots.");
                // .slots: duas colunas com 14 px entre elas; o h3 da equipe (margem 0 0 6) e as vagas (.slot, 8 px embaixo)
                float cw = (w - 14) / 2, hs = AlturaBotao("slot"), hh = AlturaNormal(Css("serif", 21, BONE));
                for (int col = 0; col < 2; col++)
                {
                    float cx = x + col * (cw + 14), cy = y;
                    if (!medindo) Linha(cx, cy, cw, hh, col == 0 ? "Equipe A" : "Equipe B", Css("serif", 21, col == 0 ? GOLD : CrimsonHi)); cy += hh + 6;
                    for (int i = col * 2; i < col * 2 + 2 && i < lobbySlots.Count; i++)
                    {
                        var s = lobbySlots[i]; string txt = s.name + (s.cid != null ? (s.cid == hostCid ? " (anfitrião)" : "") : " (bot)") + (s.cid == EU ? " (você)" : "");
                        string tipo = s.cid == EU ? "slotMe" : s.cid == null ? "slotBot" : "slot"; var r = new Rect(cx, cy, cw, hs);
                        // a vaga com jogador é um botão desativado, mas o CSS da .slot vence o do navegador: fica com a mesma cara
                        if (s.cid == null) { if (BotaoCss(r, txt, tipo)) Enviar(new JsonEscrita().Txt("t", "slot").Num("i", i)); }
                        else if (!medindo) GUI.Box(r, txt, EstiloBotao(tipo));
                        cy += hs + 8;
                    }
                }
                y += hh + 6 + 2 * (hs + 8);
                y += P(x, y, w, (UmaNoite ? "A Equipe A caça e a Equipe B celebra." : "A Equipe A caça na rodada 1 e celebra na rodada 2.") + " Clique numa vaga de bot para trocar de lugar." + (lobbyPublica ? " Esta sala aparece na lista de salas abertas." : ""), 15, MUDO, 1.45f, TextAnchor.UpperCenter, 64, true);
                // .dif.difOnline: "Bots", as três dificuldades (ou a escolhida, para quem não é anfitrião) e o aviso embaixo
                if (lobbySlots.Any(s => s.cid == null))
                {
                    y += 6; var DN = new[] { new[] { "facil", "Fáceis" }, new[] { "normal", "Normais" }, new[] { "dificil", "Difíceis" } };
                    var stR = Css("400", 15, MUDO); var stB = Css("600", 15, BONE); string atual = lobbyDif ?? "normal";
                    float hl = anfitriao ? AlturaBotao("op") : 18, wl = Larg("Bots", stR) + 4;
                    foreach (var d in DN) { if (anfitriao) wl += 6 + LarguraBotao(d[1], d[0] == atual ? "opSel" : "op"); else if (d[0] == atual) wl += 6 + Larg(d[1], stB); }
                    float xx = x + (w - wl) / 2;
                    if (!medindo) Linha(xx, y + (hl - 18) / 2, 100, 18, "Bots", stR); xx += Larg("Bots", stR) + 4 + 6;
                    foreach (var d in DN)
                    {
                        if (anfitriao) { string t = d[0] == atual ? "opSel" : "op"; float bw = LarguraBotao(d[1], t); if (BotaoCss(new Rect(xx, y, bw, hl), d[1], t) && d[0] != atual) Enviar(new JsonEscrita().Txt("t", "opcoes").Txt("dif", d[0])); xx += bw + 6; }
                        else if (d[0] == atual) { if (!medindo) Linha(xx, y, 200, 18, d[1], stB); xx += Larg(d[1], stB) + 6; }
                    }
                    y += hl + 6;
                    if (!medindo) Linha(x, y, w, LN(12.5f), "valem para os bots do lado sem jogadores", Css("400", 12, MUDO, TextAnchor.UpperCenter));
                    y += LN(12.5f);
                }
                // details.lab (margem 4 auto: no meio, até 640 px); para quem não é anfitrião, só a regra escolhida de cada linha
                float wlab = Mathf.Min(640, w);
                y += PainelLabCss(x + (w - wlab) / 2, y, wlab, lobbyLab, anfitriao ? (System.Action<Lab>)(L => Enviar(new JsonEscrita().Txt("t", "opcoes").Obj("lab", LabEscrita(L)))) : null) - 4;
                // .chat (12 px acima): a lista de 120 px que rola até a última mensagem e a .linha com o campo e o Enviar
                y += 12; ChatLista(new Rect(x, y, w, 120)); y += 120 + 8;
                {
                    string bt = "Enviar"; float wb = LarguraBotao(bt, "ghost"), hb = AlturaBotao("ghost"); var ev = Event.current;
                    bool enter = !medindo && ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter) && GUI.GetNameOfFocusedControl() == "chatSala";
                    rascunhoLobby = CampoCss(new Rect(x, y, w - 10 - wb, hb), rascunhoLobby ?? "", 140, "chatSala", "Mensagem para a sala");
                    if ((BotaoCss(new Rect(x + w - wb, y, wb, hb), bt, "ghost") || enter) && rascunhoLobby.Trim().Length > 0) { Enviar(new JsonEscrita().Txt("t", "chat").Txt("msg", rascunhoLobby.Trim())); rascunhoLobby = ""; if (enter) ev.Use(); }
                    y += hb;
                }
                // .btnrow: Começar (anfitrião) ou o .wait, e Sair da sala
                if (anfitriao) y += FileiraBotoes(x, y, w, "Começar a partida", "go", true, (System.Action)(() => Enviar(new JsonEscrita().Txt("t", "start"))), "Sair da sala", "ghost", true, (System.Action)(() => SairOnline("")));
                else
                {
                    string esp2 = "Aguardando o anfitrião começar…", bs = "Sair da sala"; var stW = Css("400", 17, MUDO); float ws = Larg(esp2, stW), wb = LarguraBotao(bs, "ghost"), hb = AlturaBotao("ghost");
                    float xx = x + (w - ws - 12 - wb) / 2, yy = y + 18;
                    // o span.wait tem margin-top 10: a caixa dele (10 + 20,4) fica no meio da linha
                    if (!medindo) Linha(xx, yy + (hb - (10 + LN(17))) / 2 + 10, ws, LN(17), esp2, stW);
                    if (BotaoCss(new Rect(xx + ws + 12, yy, wb, hb), bs, "ghost")) SairOnline("");
                    y += 18 + hb;
                }
                return y - y0;
            });
        }

        // #chatLista: caixa de 120 px (borda #333a42, padding 6 × 10), 15 px com linha de 1,4; o nome em negrito (ouro para
        // você), "(equipe)" em letra menor e a mensagem; rola para a última quando chega uma nova
        Vector2 rolChatCss;
        void ChatLista(Rect r)
        {
            if (medindo) return;
            Caixa(r, new Color(8 / 255f, 9 / 255f, 11 / 255f, .6f)); Moldura(r, Hx("#333a42"));
            var stM = Css("400", 15, BONE); var stN = Css("700", 15, BONE); var stNe = Css("700", 15, GOLD_HI); var stP = Css("400", 12, BONE);
            float wi = r.width - 22, lh = 15 * 1.4f;
            var msgs = new List<List<KeyValuePair<string, GUIStyle>>>();
            if (chat.Count == 0) msgs.Add(new List<KeyValuePair<string, GUIStyle>> { new KeyValuePair<string, GUIStyle>("Ninguém falou ainda.", Css("400", 15, MUDO)) });
            foreach (var c in chat)
            {
                var m = new List<KeyValuePair<string, GUIStyle>> { new KeyValuePair<string, GUIStyle>(c.de, c.cid == EU ? stNe : stN) };
                if (c.para == "equipe") m.Add(new KeyValuePair<string, GUIStyle>("(equipe)", stP));
                m.Add(new KeyValuePair<string, GUIStyle>(c.msg, stM)); msgs.Add(m);
            }
            float hTot = 0; foreach (var m in msgs) hTot += Fluxo(0, 0, wi, lh, 16.5f, m, true);
            var dentro = new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2); bool rola = hTot + 12 > dentro.height;
            if (chatVisto != chat.Count) { chatVisto = chat.Count; rolChatCss.y = 1e6f; }
            if (rola)
            {
                rolChatCss = Rolagem(dentro, rolChatCss, new Rect(0, 0, dentro.width - 16, hTot + 12));
                float yy = 6; foreach (var m in msgs) yy += Fluxo(9, yy, wi - 16, lh, 16.5f, m, false); FimRolagem();
            }
            else { float yy = r.y + 1 + 6; foreach (var m in msgs) yy += Fluxo(r.x + 1 + 10, yy, wi, lh, 16.5f, m, false); }
        }
        // texto em linha com estilos diferentes (o b, o small e o resto): quebra nas palavras na largura w, todas as
        // palavras na mesma linha de base (a lb px do topo da linha); devolve a altura
        float Fluxo(float x, float y, float w, float lh, float lb, List<KeyValuePair<string, GUIStyle>> partes, bool soMede, float margemPrimeira = 0)
        {
            float xx = 0, yy = 0; bool primeira = true; int ip = 0;
            foreach (var p in partes)
            {
                if (ip++ == 1) xx += margemPrimeira;   // o margin-right do primeiro pedaço (o b do tempo)
                float esp = Larg("a a", p.Value) - Larg("aa", p.Value);
                foreach (var pal in p.Key.Split(' '))
                {
                    if (pal.Length == 0) continue; float pw = Larg(pal, p.Value), antes = primeira ? 0 : esp;
                    if (!primeira && xx + antes + pw > w) { xx = 0; yy += lh; antes = 0; }
                    xx += antes; if (!soMede) NaBase(x + xx, y + yy + lb, pw + 4, pal, p.Value); xx += pw; primeira = false;
                }
            }
            return yy + lh;
        }

        // ---------- reconexaoScreen ----------
        void TelaReconexaoCss()
        {
            PainelTela("recon", 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, "Conexão perdida", TextAnchor.UpperCenter);
                // o p com text-align center e margin 0 auto: a caixa de 64ch no meio
                var st = Css("400", 18, Hx("#cfc7b8"), TextAnchor.UpperCenter); float wb = Mathf.Min(w, Larg("0", st) * 64);
                y += Ps(x + (w - wb) / 2, y, wb, "Um bot está jogando no seu lugar. Tentando voltar à sala " + salaCodigo + "…", st, 18 * 1.45f);
                // p.wait: o .panel p vence o .wait (18 px, cor do parágrafo, margem 6, caixa de 64ch à esquerda); só o center fica
                y += 6 + P(x, y + 6, w, "Tentativa " + (reconTent + 1) + ", sua vaga fica guardada por mais " + Mathf.CeilToInt(Mathf.Max(0, reconAte - Time.realtimeSinceStartup)) + " s", 18, null, 1.45f, TextAnchor.UpperCenter);
                y += FileiraBotoes(x, y, w, "Voltar ao menu", "ghost", true, (System.Action)(() => SairOnline("")));
                return y - y0;
            });
        }
    }
}
