// O título do navegador (titleScreen e #scr.inicial): uma coluna de 470 px a 7% da esquerda, no meio da altura, sobre o
// degradê lateral; o sigilo, o nome em IM Fell, a frase em itálico, o botão carmim, o papel e os bots em botões de
// opção, o laboratório dobrável, o desafio da noite, os links sublinhados e o jogo online (servidor, nome e Criar sala,
// sala pública, salas abertas, código e Entrar na sala). Medido antes de desenhar; rola se não couber.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        Vector2 rolTitulo; float altTitulo;
        void TituloCss()
        {
            FundoTitulo();
            float w = Mathf.Min(470, W - 32), x = Mathf.Max(16, W * .07f);
            // mede e desenha: no meio da altura quando cabe; senão, rolando
            medindo = true; altTitulo = ColunaTitulo(x, 0, w); medindo = false;
            if (altTitulo + 32 <= H) ColunaTitulo(x, (H - altTitulo) / 2, w);
            else
            {
                rolTitulo = GUI.BeginScrollView(new Rect(0, 0, W, H), rolTitulo, new Rect(0, 0, W - 20, altTitulo + 32), false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                ColunaTitulo(x, 16, w); GUI.EndScrollView();
            }
        }
        float ColunaTitulo(float x, float y0, float w)
        {
            float y = y0;
            if (!medindo) GUI.DrawTexture(new Rect(x, y, 74, 74), Sigilo()); y += 74 + 6;
            // o .panel h1 vence o .tituloJogo no navegador: clamp(42px, 6vw, 68px), linha de 1, margens 6 e 4
            int t1 = Mathf.RoundToInt(Mathf.Clamp(W * .06f, 42, 68)); var stT = Css("serif", t1, BONE); float lhT = t1; y += 6;
            if (!medindo) { Tracos.Brilho(x + Larg("Ritual Reversal", stT) / 2, y + lhT / 2 + 2, Larg("Ritual Reversal", stT) / 2 + 30, lhT / 2 + 18, new Color(0, 0, 0, .55f)); Linha(x, y, w, lhT, "Ritual Reversal", stT); }
            y += lhT + 4;
            var stL = Css("serifIt", 19, Hx("#bfb6a6")); float wl = Mathf.Min(w, Larg("0", stL) * 40);
            y += Ps(x, y, wl, UmaNoite ? "Dois cultistas celebram rituais em segredo. Dois caçadores investigam e selam. Uma noite, três rituais: quem levar dois vence." : "Dois cultistas celebram um ritual em segredo. Dois caçadores tentam descobrir onde. Na segunda rodada, os papéis se invertem.", stL, 19 * 1.45f) + 22;
            // .acoes: coluna com 12 px entre as peças
            string bs = "Jogar sozinho, com bots"; float hb = AlturaBotao("go");
            if (BotaoCss(new Rect(x, y, LarguraBotao(bs, "go"), hb), bs, "go")) IniciarSolo(PapelSolo, null, false, false); y += hb + 12;
            y += LinhaOpcoes(x, y, "Você", new[] { new[] { "H", "Caça" }, new[] { "C", "Celebra" }, new[] { "?", "Sorteia" } }, PapelSolo, k => { papelSolo = k; PlayerPrefs.SetString("rr-papel", k); }) + 12;
            y += LinhaOpcoes(x, y, "Bots inimigos", new[] { new[] { "facil", "Fácil" }, new[] { "normal", "Normal" }, new[] { "dificil", "Difícil" } }, dificuldade, k => { dificuldade = k; PlayerPrefs.SetString("rr-dif", k); }) + 12;
            y += PainelLabCss(x, y, Mathf.Min(640, w), LabSolo, MudarLabSolo) + 12;
            {
                var D = DesafioDe(HojeDesafio()); object rec; var des = O(Perfil(), "desafios"); bool tem = des.TryGetValue(HojeDesafio(), out rec) && rec != null;
                string sub = "Hoje você " + (D[1] == "H" ? "caça" : "celebra") + ", " + Mundo.CLIMAS[D[2]].nome.ToLowerInvariant() + ", contra bots difíceis" + (tem ? " · recorde " + System.Convert.ToInt32(rec) : "");
                float wd = Mathf.Max(Larg("Desafio da noite", Css("600", 20, BONE)), Larg(sub, Css("400", 13, MUDO))) + 58, hd = 1 + 10 + 24 + 2 + 13 * 1.2f + 10 + 1;
                if (BotaoDuasLinhas(new Rect(x, y, wd, hd), "Desafio da noite", sub, "ghost")) IniciarDesafio(); y += hd + 12;
            }
            float xl = x; float hl = AlturaBotao("lk");
            foreach (var l in new[] { new[] { "aprender", "Aprender a jogar" }, new[] { "como", "Como jogar" }, new[] { "perfil", "Perfil" }, new[] { "novidades", "Novidades" }, new[] { "arsenal", "Arsenal" }, new[] { "ajustes", "Ajustes" } })
            {
                float wk = LarguraBotao(l[1], "lk");
                if (BotaoCss(new Rect(xl, y, wk, hl), l[1], "lk")) { som.Tocar("click"); if (l[0] == "arsenal") IniciarArsenal(); else { telaMenu = l[0]; rolPerfil = Vector2.zero; } }
                xl += wk + 18;
            }
            y += hl;
            y += OnlineCss(x, y, w);
            // p.err e p.fine
            var stE = Css("400", 16, CrimsonHi); if (!medindo && erroRede.Length > 0) Paragrafo(x, y + 6, w, 16 * 1.45f, erroRede, stE); y += Mathf.Max(20, erroRede.Length > 0 ? AlturaParagrafo(erroRede, stE, w, 16 * 1.45f) : 0) + 12;
            y += 12 + P(x, y + 12, w, "Para jogar online, alguém roda o servidor do projeto; digite aqui o endereço que ele mostra.", 15, MUDO);
            return y - y0;
        }
        // .dif: o rótulo apagado e os botões de opção, 6 px entre eles
        float LinhaOpcoes(float x, float y, string rotulo, string[][] ops, string atual, System.Action<string> muda)
        {
            var stR = Css("400", 15, MUDO); float h = AlturaBotao("op"), xx = x;
            if (!medindo) Linha(xx, y + (h - 18) / 2, 200, 18, rotulo, stR); xx += Larg(rotulo, stR) + 4 + 6;
            foreach (var o in ops)
            {
                bool sel = atual == o[0]; float wb = LarguraBotao(o[1], sel ? "opSel" : "op");
                if (BotaoCss(new Rect(xx, y, wb, h), o[1], sel ? "opSel" : "op") && !sel) { muda(o[0]); som.Tocar("click"); }
                xx += wb + 6;
            }
            return h;
        }
        // details.lab: a caixa com a linha de resumo; aberta, o aviso e uma linha por regra (os botões quebram linha).
        // Margem de 4 em volta, borda de 1, padding 6 × 12; o .fine e as linhas com 6 px entre eles
        float PainelLabCss(float x, float y, float w, Lab lab, System.Action<Lab> muda)
        {
            var L = Lab.De(lab); var mud = LabMudancas(L); float wc = w - 26, xc = x + 13;
            var stS = Css("400", 15, BONE); var stSb = Css("500", 15, GOLD_HI); float lhS = 15 * 1.2f;
            string res = "Laboratório de playtest", extra = mud.Count > 0 ? " · " + mud.Count + (mud.Count > 1 ? " regras alteradas" : " regra alterada") : "";
            string aviso = "Para testar com pessoas: mude uma regra por vez e compare as noites no relatório do fim da partida. A primeira opção de cada linha é a regra normal." + (muda == null ? " Só o anfitrião muda." : "");
            bool links = muda != null && mud.Count > 0;
            float h = 1 + 6 + lhS;
            if (labAberto)
            {
                bool m0 = medindo; medindo = true; h += 6 + P(0, 0, wc, aviso, 15, MUDO) + 6;
                for (int i = 0; i < LAB_UI.Length; i++) h += (i > 0 ? 6 : 0) + RegraLab(0, 0, wc, LAB_UI[i], L, muda);
                h += 6 + (links ? AlturaBotao("lk") : 0); medindo = m0;
            }
            h += 6 + 1;
            float yy = y + 4;
            if (!medindo) { var caixa = new Rect(x, yy, w, h); Caixa(caixa, new Color(8 / 255f, 9 / 255f, 11 / 255f, .55f)); Moldura(caixa, BORDA_OP); }
            yy += 1 + 6;
            // o marcador do details (▼ aberto, ▶ fechado) e o resumo; clicar troca
            if (!medindo)
            {
                Linha(xc, yy, 20, lhS, labAberto ? "▼" : "▶", Css("400", 11, BONE)); float xs = xc + 15, bas = yy + (lhS - 18) / 2 + 15;
                NaBase(xs, bas, 400, res, stS); if (extra != "") NaBase(xs + Larg(res, stS), bas, 300, extra, stSb);
            }
            if (BotaoCss(new Rect(x + 1, yy - 2, w - 2, lhS + 4), res, "nada")) { labAberto = !labAberto; PlayerPrefs.SetInt("rr-lab-aberto", labAberto ? 1 : 0); }
            yy += lhS;
            if (labAberto)
            {
                yy += 6; yy += P(xc, yy, wc, aviso, 15, MUDO) + 6;
                for (int i = 0; i < LAB_UI.Length; i++) { if (i > 0) yy += 6; yy += RegraLab(xc, yy, wc, LAB_UI[i], L, muda); }
                yy += 6;
                if (links) { string t = "Voltar tudo ao normal"; if (BotaoCss(new Rect(xc, yy, LarguraBotao(t, "lk"), AlturaBotao("lk")), t, "lk")) { muda(Lab.De(null)); som.Tocar("click"); } }
            }
            return 4 + h + 4;
        }
        // .labRow: o nome com 170 px no mínimo e os botões; o que não cabe desce
        float RegraLab(float x, float y, float w, LinhaLab R, Lab L, System.Action<Lab> muda)
        {
            var stN = Css("400", 14, MUDO); float h = AlturaBotao("lab"), xx = x, yy = y; var v = ValorLab(L, R.k);
            float wn = Mathf.Max(170, Larg(R.nome, stN)) + 4 + 6;
            if (!medindo) Linha(xx, yy + (h - 16.8f) / 2, wn, 16.8f, R.nome, stN); xx += wn;
            foreach (var op in R.ops)
            {
                bool sel = Igual(op.v, v);
                if (muda == null) { if (sel) { if (!medindo) Linha(xx, yy + (h - 16.8f) / 2, 200, 16.8f, op.txt, Css("500", 14, GOLD_HI)); xx += Larg(op.txt, Css("500", 14, GOLD_HI)) + 6; } continue; }
                float wb = LarguraBotao(op.txt, sel ? "labSel" : "lab"); if (xx + wb > x + w + .5f) { xx = x; yy += h + 6; }
                if (BotaoCss(new Rect(xx, yy, wb, h), op.txt, sel ? "labSel" : "lab") && !sel) { muda(ComValor(L, R.k, op.v)); som.Tocar("click"); }
                xx += wb + 6;
            }
            return yy - y + h;
        }
        // .online: a linha de cima, o servidor, o nome e Criar sala, a sala pública, as salas abertas, o código e Entrar
        float OnlineCss(float x, float y, float w)
        {
            float y0 = y; y += 22; if (!medindo) Caixa(new Rect(x, y, w, 1), new Color(.91f, .878f, .816f, .14f)); y += 1 + 14;
            float hc = 42, hb = AlturaBotao("go");
            y += Titulo3(x, y, w, "Servidor") + 8;
            servidor = CampoCss(new Rect(x, y, 260, hc), servidor, 120, "iServ", "ex.: abcd.trycloudflare.com"); y += hc + 14;
            y += Titulo3(x, y, w, "Jogar online") + 8;
            nomeJogador = CampoCss(new Rect(x, y, 170, hc), nomeJogador, 16, "iName", "Seu nome");
            if (BotaoCss(new Rect(x + 170 + 10, y + (hc - hb) / 2, LarguraBotao("Criar sala", "gold"), hb), "Criar sala", "gold")) Conectar(true, ""); y += hc;
            bool pub = CaixinhaCss(x, y + 6, "Sala pública: aparece na lista para qualquer um entrar", SalaPublica, Css("400", 15, MUDO)); if (pub != SalaPublica) PlayerPrefs.SetInt("rr-sala-publica", pub ? 1 : 0); y += 6 + 18;
            if (salas.Count > 0)
            {
                y += Titulo3(x, y + 14, w, "Salas abertas") + 14 + 4;
                foreach (var s in salas.ToList())
                {
                    float hs = AlturaBotao("ghostP"); var stC = Css("400", 20, GOLD_HI); string b = "Entrar"; float wb = LarguraBotao(b, "ghostP");
                    if (!medindo) { Linha(x, y + 4, 80, hs, s.code, stC); Linha(x + 76, y + 4, w - 76 - wb - 12, hs, s.anfitriao + " · " + s.jogadores + " de 4", Css("400", 15, Hx("#cfc6b6"))); }
                    if (BotaoCss(new Rect(x + w - wb, y + 4, wb, hs), b, "ghostP")) { codigoDigitado = s.code; Conectar(false, s.code); }
                    y += hs + 8;
                }
            }
            y += 8; codigoDigitado = CampoCss(new Rect(x, y, 170, hc), codigoDigitado, 4, "iCode", "Código");
            bool ok = (codigoDigitado ?? "").Trim().Length == 4;
            if (BotaoCss(new Rect(x + 170 + 10, y + (hc - hb) / 2, LarguraBotao("Entrar na sala", "ghost"), hb), "Entrar na sala", "ghost", ok)) Conectar(false, codigoDigitado.Trim().ToUpperInvariant()); y += hc;
            if (online && rede != null && !rede.aberta && !rede.fechada) { if (!medindo) Linha(x, y + 6, w, 20, "Conectando…", Css("400", 16, MUDO)); y += 26; }
            return y - y0;
        }
        // o h3 do título: margem 0 em cima e 8 embaixo
        float Titulo3(float x, float y, float w, string txt) { var st = Css("serif", 21, BONE); float h = AlturaNormal(st); if (!medindo) Linha(x, y, w, h, txt, st); return h; }
    }
}
