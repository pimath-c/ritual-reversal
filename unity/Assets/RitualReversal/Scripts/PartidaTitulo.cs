// O título do navegador (titleScreen e #scr.inicial): uma coluna de 470 px a 7% da esquerda, no meio da altura, sobre o
// degradê lateral. v6.3, direção Nanquim: o nome enorme em Big Shoulders 900 (a segunda palavra só no contorno) saindo pela
// borda, a pincelada vermelha, a frase em mono, o botão vermelho com a seta, as opções em mono entre colchetes, o
// laboratório dobrável, o desafio da noite, os links em mono e o jogo online (servidor, nome e Criar sala,
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
            // @media (max-height:430px): a coluna também é um .screen .panel e encolhe para 0,86 a partir do meio de cima
            float k = H <= 430 ? .86f : 1;
            if (altTitulo + 32 <= H) { float y0 = (H - altTitulo) / 2; ComTransformacao(x + w / 2, y0, x + w / 2, y0, k, () => ColunaTitulo(x, y0, w)); }
            else
            {
                rolTitulo = Rolagem(new Rect(0, 0, W, H), rolTitulo, new Rect(0, 0, W - 20, altTitulo + 32));
                ComTransformacao(x + w / 2, 16, x + w / 2, 16, k, () => ColunaTitulo(x, 16, w)); FimRolagem();
            }
        }
        static readonly Dictionary<string, string> NOME_MAPA = new Dictionary<string, string> { { "catedral", "Catedral da Dor" }, { "asilo", "Asilo Blackwater" } };
        static string NomeMapa(string id) { string n; return id != null && NOME_MAPA.TryGetValue(id, out n) ? n : NOME_MAPA["catedral"]; }
        float ColunaTitulo(float x, float y0, float w)
        {
            float y = y0;
            // a tinta entra uma vez por sessão: as palavras reveladas da esquerda, a pincelada pintada, o resto subindo
            if (!medindo && !tituloEntrou) { tituloEntrou = true; Marcar("titulo"); }
            // #scr .tituloJogo: Big Shoulders 900, clamp(64px, 7vw, 128px), linha de ,8, margem esquerda de -,06em, sem quebra
            int t1 = Mathf.RoundToInt(Mathf.Clamp(W * .07f, 64, 128)); var stT = Css("display", t1, BONE); float lhT = Mathf.Round(t1 * .8f), xt = x - .06f * t1; y += 6;
            {
                float k1 = Sai(Anim("titulo", .5f)), k2 = Sai(Anim("titulo", .5f, .22f)), yy = y;
                float l1 = Larg("RITUAL", stT) + 8, l2 = Larg("REVERSAL", stT) + 8;
                Recorte(new Rect(xt - 4, yy - 4, l1 * k1, lhT + 8), () => Linha(xt, yy, W, lhT, "RITUAL", stT), k1);
                Recorte(new Rect(xt - 4, yy + lhT - 4, l2 * k2 + 4, lhT + 8), () => TextoContorno(xt, yy + lhT, W, lhT, "REVERSAL", stT, BONE, new Color(.04f, .04f, .04f), 1), k2);
            }
            y += 2 * lhT + 6;
            // .pincel: min(560px, 92%) × 26, 2 px em cima e 18 embaixo, pintada da esquerda (aos ,55 s)
            DesenharPincel(new Rect(x, y + 2, Mathf.Min(560, w * .92f), 26), SANGUE, Sai(Anim("titulo", .35f, .55f))); y += 2 + 26 + 18;
            // #scr.inicial .lede: mono 14, linha de 1,55, no máximo 46ch, apagada
            var stL = Css("mono", 14, MUDO); float wl = Mathf.Min(w, Larg("0", stL) * 46);
            float yl = y; ComEntrada("titulo", .35f, .75f, d => y = yl + Ps(x, yl + d, wl, UmaNoite ? "Dois cultistas celebram rituais em segredo. Dois caçadores investigam e selam. Uma noite, três rituais: quem levar dois vence." : "Dois cultistas celebram um ritual em segredo. Dois caçadores tentam descobrir onde. Na segunda rodada, os papéis se invertem.", stL, Mathf.Round(14 * 1.55f)) + 22);
            // v6.2: o mapa (no navegador a página recarrega; aqui o mundo é desmontado e montado de novo)
            y += LinhaOpcoes(x, y, "Mapa", new[] { new[] { "catedral", NOME_MAPA["catedral"] }, new[] { "asilo", NOME_MAPA["asilo"] } }, MapaMundo, k => { PlayerPrefs.SetString("rr-mapa", k); TrocarMundo(k); }) + 12;
            // .acoes: coluna com 14 px entre as peças; o #bSolo maior (clamp(30px, 2,6vw, 40px)) com a seta que cutuca
            string bs = "Jogar sozinho, com bots"; var stJ = Css("serif", Mathf.RoundToInt(Mathf.Clamp(W * .026f, 30, 40)), SANGUE); float hb = Mathf.Round(AlturaNormal(stJ)) + 13, wj = Larg("JOGAR SOZINHO, COM BOTS →", stJ);
            if (BotaoCss(new Rect(x, y, wj, hb), bs, "nada")) IniciarSolo(PapelSolo, null, false, false);
            if (!medindo)
            {
                // @keyframes cutucar (2,4 s): parada até 70%, 6 px à direita aos 80%, de volta aos 100%
                float f = (Time.unscaledTime % 2.4f) / 2.4f, cut = SuaveUI() ? 0 : f < .7f ? 0 : f < .8f ? 6 * (f - .7f) / .1f : 6 * (1 - (f - .8f) / .2f);
                string sem = "JOGAR SOZINHO, COM BOTS "; Linha(x, y + 6, wj, AlturaNormal(stJ), sem, stJ); Linha(x + LargTrecho(sem, stJ) + cut, y + 6, 60, AlturaNormal(stJ), "→", stJ);
                Traco(new Rect(x, y, wj, hb), "go", (padNav && padContagem - 1 == padFoco) || new Rect(x, y, wj, hb).Contains(Event.current.mousePosition));
            }
            y += hb + 14;
            y += LinhaOpcoes(x, y, "Você", new[] { new[] { "H", "Caça" }, new[] { "C", "Celebra" }, new[] { "?", "Sorteia" } }, PapelSolo, k => { papelSolo = k; PlayerPrefs.SetString("rr-papel", k); }) + 12;
            y += LinhaOpcoes(x, y, "Bots inimigos", new[] { new[] { "facil", "Fácil" }, new[] { "normal", "Normal" }, new[] { "dificil", "Difícil" } }, dificuldade, k => { dificuldade = k; PlayerPrefs.SetString("rr-dif", k); }) + 12;
            y += PainelLabCss(x, y, Mathf.Min(640, w), LabSolo, MudarLabSolo) + 12;
            {
                var D = DesafioDe(HojeDesafio()); object rec; var des = O(Perfil(), "desafios"); bool tem = des.TryGetValue(HojeDesafio(), out rec) && rec != null;
                string sub = "Hoje você " + (D[1] == "H" ? "caça" : "celebra") + ", " + Mundo.CLIMAS[D[2]].nome.ToLowerInvariant() + ", contra bots difíceis" + (tem ? " · recorde " + System.Convert.ToInt32(rec) : "");
                float wd = Mathf.Max(Larg("desafio da noite", Css("mono", 14, MUDO)), Larg(sub.ToLowerInvariant(), Css("mono", 13, MUDO))), hd = 4 + LN(14f) + 2 + LN(13f) + 5;
                if (BotaoDuasLinhas(new Rect(x, y, wd, hd), "Desafio da noite", sub, "ghost")) IniciarDesafio(); y += hd + 12;
            }
            // .links: quebram linha (8 px entre as linhas, 20 entre os links)
            float xl = x; float hl = AlturaBotao("lk");
            foreach (var l in new[] { new[] { "aprender", "Aprender a jogar" }, new[] { "como", "Como jogar" }, new[] { "perfil", "Perfil" }, new[] { "novidades", "Novidades" }, new[] { "arsenal", "Arsenal" }, new[] { "ajustes", "Ajustes" } })
            {
                float wk = LarguraBotao(l[1], "lk"); if (xl > x && xl + wk > x + w) { xl = x; y += hl + 8; }
                if (BotaoCss(new Rect(xl, y, wk, hl), l[1], "lk")) { som.Tocar("click"); if (l[0] == "arsenal") IniciarArsenal(); else { telaMenu = l[0]; } }
                xl += wk + 20;
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
            // .dif (Nanquim): o rótulo em mono 13 apagado com 82 px no mínimo, 14 px entre as peças
            var stR = Css("mono", 13, Hx("#6e6a62")); float h = AlturaBotao("op"), xx = x;
            if (!medindo) Linha(xx, y + (h - LN(13)) / 2, 200, LN(13), rotulo, stR); xx += Mathf.Max(82, Larg(rotulo, stR)) + 14;
            foreach (var o in ops)
            {
                bool sel = atual == o[0]; float wb = LarguraBotao(o[1], sel ? "opSel" : "op");
                if (BotaoCss(new Rect(xx, y, wb, h), o[1], sel ? "opSel" : "op") && !sel) { muda(o[0]); som.Tocar("click"); }
                xx += wb + 14;
            }
            return h;
        }
        // details.lab: a caixa com a linha de resumo; aberta, o aviso e uma linha por regra (os botões quebram linha).
        // Margem de 4 em volta, borda de 1, padding 6 × 12; o .fine e as linhas com 6 px entre eles
        float PainelLabCss(float x, float y, float w, Lab lab, System.Action<Lab> muda)
        {
            var L = Lab.De(lab); var mud = LabMudancas(L); float wc = w - 26, xc = x + 13;
            var stS = Css("mono", 13, BONE); var stSb = Css("mono500", 13, BONE); float lhS = LN(15f);
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
            if (!medindo) { var caixa = new Rect(x, yy, w, h); Caixa(caixa, FUNDO_OP); Moldura(caixa, BORDA_OP); }
            yy += 1 + 6;
            // o marcador do details (▼ aberto, ▶ fechado) e o resumo; clicar troca
            if (!medindo)
            {
                // o marcador: a caixa de 2/3 da subida (10 px na Barlow de 15) com o pé na linha de base, e o texto 16 px depois
                float xs = xc + 16, bas = yy + (lhS - 18) / 2 + 15; Tracos.Disclosure(xc, PixelY(bas) - 10, 10, labAberto, BONE);
                // summary com letter-spacing .04em (o b dentro também)
                float wr = EspacadaNaBase(xs, bas, res, stS, .6f); if (extra != "") EspacadaNaBase(xs + wr, bas, extra, stSb, .6f);
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
            if (!medindo) Linha(xx, yy + (h - LN(14)) / 2, wn, LN(14), R.nome, stN); xx += wn;
            foreach (var op in R.ops)
            {
                bool sel = Igual(op.v, v);
                if (muda == null) { if (sel) { if (!medindo) Linha(xx, yy + (h - LN(14)) / 2, 200, LN(14), op.txt, Css("500", 14, GOLD_HI)); xx += Larg(op.txt, Css("500", 14, GOLD_HI)) + 6; } continue; }
                float wb = LarguraBotao(op.txt, sel ? "labSel" : "lab"); if (xx + wb > x + w + .5f) { xx = x; yy += h + 6; }
                if (BotaoCss(new Rect(xx, yy, wb, h), op.txt, sel ? "labSel" : "lab") && !sel) { muda(ComValor(L, R.k, op.v)); som.Tocar("click"); }
                xx += wb + 6;
            }
            return yy - y + h;
        }
        // .online: a linha de cima, o servidor, o nome e Criar sala, a sala pública, as salas abertas, o código e Entrar
        float OnlineCss(float x, float y, float w)
        {
            float y0 = y; y += 22; if (!medindo) Caixa(new Rect(x, y, w, 1), Hx("#262626")); y += 1 + 14;
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
                    if (!medindo) { Linha(x, y + 4, 80, hs, s.code, stC); Linha(x + 76, y + 4, w - 76 - wb - 12, hs, s.anfitriao + " · " + s.jogadores + " de 4" + (s.mapa != null && s.mapa != "catedral" ? " · " + NomeMapa(s.mapa) : ""), Css("400", 15, Hx("#c9c3b8"))); }
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
        float Titulo3(float x, float y, float w, string txt) { var st = Css("serif", 22, BONE); float h = Mathf.Round(22 * .95f); if (!medindo) Linha(x, y, w, h, Maiusc(txt), st); return h; }
        bool tituloEntrou;
    }
}
