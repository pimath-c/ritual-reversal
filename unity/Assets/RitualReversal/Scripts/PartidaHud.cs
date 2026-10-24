// HUD e telas da partida, em IMGUI (sem nenhum asset): título, escolha de altares, classe e feitiço, pausa, loja,
// resumo e resultado; durante o jogo, relógio, altares, tarefas, vida, sanidade, munição/fervor, interação, disputa
// do ritual, planta (Tab) e mensagens. Os textos são os do protótipo.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        GUIStyle sTexto, sPequeno;
        Texture2D branco;
        float W, H; // tela virtual: 1080 de altura

        // as fontes do navegador (Resources/Fontes, OFL): Barlow Condensed no corpo, IM Fell English nos títulos; sem elas, a do Unity
        static Font fonteCond, fonteSerif, fonteSerifIt;
        void Estilos()
        {
            if (sTexto != null) return;
            branco = Texture2D.whiteTexture;
            CarregarFonteSistema(); fonteCond = Resources.Load<Font>("Fontes/BarlowCondensed-Medium"); fonteCond400 = Resources.Load<Font>("Fontes/BarlowCondensed-Regular") ?? fonteCond; fonteCond600 = Resources.Load<Font>("Fontes/BarlowCondensed-SemiBold") ?? fonteCond; fonteCond700 = Resources.Load<Font>("Fontes/BarlowCondensed-Bold") ?? fonteCond; fonteSerif = Resources.Load<Font>("Fontes/IMFellEnglish-Regular"); fonteSerifIt = Resources.Load<Font>("Fontes/IMFellEnglish-Italic"); fonteSimbolo = Resources.Load<Font>("Fontes/DejaVuSans"); fonteSimboloN = Resources.Load<Font>("Fontes/DejaVuSans-Bold");
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, richText = true }; sTexto.normal.textColor = OSSO; if (fonteCond != null) sTexto.font = fonteCond;
            sPequeno = new GUIStyle(sTexto) { fontSize = 16 };
        }
        void Caixa(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, branco); GUI.color = o; }
        void Barra(Rect r, float v, Color c) { Caixa(r, new Color(0, 0, 0, .55f)); Caixa(new Rect(r.x, r.y, r.width * Mathf.Clamp01(v), r.height), c); }
        static string Cor(string txt, Color c) { return "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + txt + "</color>"; }
        static string Relogio(double s) { s = System.Math.Max(0, System.Math.Ceiling(s - 1e-9)); int m = (int)(s / 60), ss = (int)(s % 60); return m + ":" + ss.ToString("00"); }   // o fmt do navegador: para cima
        // OnGUI roda mais de uma vez por quadro: os cronômetros do HUD só andam no desenho
        static float dtG { get { return Event.current != null && Event.current.type == EventType.Repaint ? Time.unscaledDeltaTime : 0; } }

        // a escala da interface: no computador, a tela virtual de 1080 de altura; no celular, o pixel de CSS do navegador
        // (a densidade da tela: 160 ppp no Android, 163 no iOS), para os botões terem o tamanho que têm no navegador
        static float EscalaUI()
        {
            if (!Application.isMobilePlatform || Screen.dpi <= 0) return Screen.height / 1080f;
            return Mathf.Max(1, Screen.dpi / (Application.platform == RuntimePlatform.IPhonePlayer ? 163f : 160f));
        }
        void OnGUI()
        {
            if (Referenciando) return;   // fotos de referência (F10): a cena sem HUD, como o navegador.js
            Estilos(); ComecoNavPad(); recortes.Clear();   // uma passada interrompida (ExitGUI) não deixa recorte para trás
            float esc = EscalaUI(); GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(esc, esc, 1)); W = Screen.width / esc; H = Screen.height / esc;
            // um clique que muda a tela no meio da passada deixa o resto dela sem o layout que o Layout calculou: o Unity
            // acusaria "Getting control N's position..." no console; ExitGUI encerra a passada sem erro (o próximo quadro já
            // desenha a tela nova)
            try { Telas(); }
            catch (System.ArgumentException) { if (!cliqueNaPassada) throw; GUIUtility.ExitGUI(); }
        }
        void Telas()
        {
            if (reconectando) { TelaReconexao(); return; }
            if (g == null) { if (online && noLobby) TelaLobby(); else TelaTitulo(); ToastConquista(); return; }
            switch (g.phase)
            {
                case "pick": TelaEscolha(); break;
                case "intro": TelaIntro(); break;
                case "play": if (arsenal) { PainelArsenal(); break; } Hud(); DesenharToque(); if (lojaAberta != null) TelaLoja(); else if (vitrine) PainelVitrine(); else if (!Travado()) TelaPausa(); break;
                case "summary": TelaResumo(); break;
                case "final": TelaFinal(); break;
            }
            ToastConquista();
        }

        // ---------- telas ----------
        void TelaEscolha() { TelaEscolhaCss(); }
        static string Lado(Altar A)
        {
            string ns = A.z < -60 ? "floresta, norte" : A.z > 60 ? "floresta, sul" : A.z < 0 ? "catedral, lado norte" : "catedral, lado sul";
            return ns + (A.x > 20 ? ", leste" : A.x < -20 ? ", oeste" : "");
        }

        void TelaIntro() { TelaIntroCss(); }

        // ajustes (settingsScreen do protótipo): ficam salvos entre as sessões
        void SalvarAjustes() { PlayerPrefs.SetFloat("rr-sens", sensibilidade); if (som != null) PlayerPrefs.SetFloat("rr-vol", som.volume); if (fp != null) PlayerPrefs.SetFloat("rr-fov", fp.fovBase); }

        // pausa (local) ou mouse solto (online, a partida não para): a cola das teclas do seu papel, Ajustes e sair
        bool pausaAjustes;
        void TelaPausa() { TelaPausaCss(); }

        // lojaScreen: o nome do mercador, os óbolos no .lede, um .item por mercadoria (nome e descrição à esquerda, o
        // botão dourado com o preço ou, desativado, o motivo) e o Voltar à noite
        void TelaLoja()
        {
            var n = Mapa.NPCS.Find(x => x.id == lojaAberta); var m = Eu(); if (n == null || m == null) return;
            var itens = n.vende.Where(k => Regras.ITENS[k].time == null || Regras.ITENS[k].time == m.team).ToList();
            PainelTela("loja" + n.id + m.obolos + "|" + m.reag + (m.amuleto ? 1 : 0) + g.momento, 800, (x, y, w) =>
            {
                float y0 = y; y += H2(x, y, w, char.ToUpper(n.nome[0]) + n.nome.Substring(1));
                // o style="text-align:left" vence o .lede, mas a margem 0 auto 14 dele tem !important: a caixa de 64ch fica no meio
                var stL = Css("serifIt", 20, MUDO); float wl = Mathf.Min(w, Larg("0", stL) * 64);
                if (!medindo) Linha(x + (w - wl) / 2, y, wl, 20 * 1.45f, "Você tem <b>" + m.obolos + "</b> óbolos.", stL);
                y += 20 * 1.45f + 14;
                var stB = Css("serif", 20, BONE); var stS = Css("400", 15, Hx("#c9c1b2")); float hb = AlturaNormal(stB);
                for (int i = 0; i < itens.Count; i++)
                {
                    var k = itens[i]; var it = Regras.ITENS[k];
                    string bloq = it.momento > 0 && g.momento < it.momento ? "Só a partir da Vigília" : k == "amuleto" && m.amuleto ? "Já tem um" : k == "reagente" && m.reag >= 2 ? "Mãos cheias" : m.obolos < it.preco ? "Faltam " + (it.preco - m.obolos) : "";
                    string bt = bloq != "" ? bloq : it.preco + " óbolos", tipo = bloq != "" ? "lojaGhost" : "lojaGold";
                    float wb = Mathf.Max(120, LarguraBotao(bt, tipo)), hbt = AlturaBotao(tipo), wt = w - 26 - 14 - wb;
                    // o span de 15 px quebra dentro da linha do corpo (16 px, normal: 19 px)
                    float ht = hb + AlturaParagrafo(it.desc, stS, wt, LN(16)), hi = Mathf.Max(ht, hbt), r0 = y + (i > 0 ? 10 : 0);
                    var caixa = new Rect(x, r0, w, 1 + 10 + hi + 10 + 1);
                    if (!medindo)
                    {
                        Caixa(caixa, new Color(14 / 255f, 16 / 255f, 19 / 255f, .8f)); Moldura(caixa, BORDA_OP);
                        float ty = r0 + 11 + (hi - ht) / 2; Linha(x + 13, ty, wt, hb, it.nome, stB);
                        var ls = Quebrar(it.desc, stS, wt); for (int l = 0; l < ls.Count; l++) NaBase(x + 13, ty + hb + l * LN(16) + 16, wt, ls[l], stS);
                    }
                    if (BotaoCss(new Rect(x + w - 13 - wb, r0 + 11 + (hi - hbt) / 2, wb, hbt), bt, tipo, bloq == "")) Acao(new Acao { type = "comprar", npc = n.id, item = k });
                    y = r0 + caixa.height;
                }
                y += FileiraBotoes(x, y, w, "Voltar à noite", "ghost", true, (System.Action)(() => { lojaAberta = null; Travar(true); }));
                return y - y0;
            });
        }

        static readonly string[][] LINHAS = {
            new[]{"rituais","Rituais iniciados"}, new[]{"completos","Rituais completos"}, new[]{"selados","Rituais selados"}, new[]{"chegadas","Caçador chegou ao círculo"},
            new[]{"tentativasSelo","Tentativas de selamento"}, new[]{"cancelados","Selamentos interrompidos"}, new[]{"fracaoSobFogo","Tempo selando sob fogo"}, new[]{"primeiroContato","Primeiro contato"},
            new[]{"quedas","Quedas"}, new[]{"reanimacoes","Reanimações"}, new[]{"purgas","Purgas"}, new[]{"chamarizes","Chamarizes"}, new[]{"transferencias","Transferências"} };
        static string Valor(Estatisticas e, string k)
        {
            switch (k)
            {
                case "rituais": return e.rituais.ToString(); case "completos": return e.completos.ToString(); case "selados": return e.selados.ToString(); case "chegadas": return e.chegadas.ToString();
                case "tentativasSelo": return e.tentativasSelo.ToString(); case "cancelados": return e.cancelados.ToString();
                case "fracaoSobFogo": return e.fracaoSobFogo.HasValue ? Mathf.RoundToInt((float)e.fracaoSobFogo.Value * 100) + "%" : "—";
                case "primeiroContato": return e.primeiroContato.HasValue ? Relogio(e.primeiroContato.Value) : "—";
                case "quedas": return e.quedas.ToString(); case "reanimacoes": return e.reanimacoes.ToString(); case "purgas": return e.purgas.ToString(); case "chamarizes": return e.chamarizes.ToString();
                default: return e.transferencias.ToString();
            }
        }
        // o #interact do navegador: o anel de progresso (46 px, logo abaixo da mira) e os rótulos com a tecla numa caixinha dourada
        void Rotulo(float y, string tecla, string txt)
        {
            // #interact .lbl: 18 px, fundo escuro, padding 3 × 10; a tecla em kbd dourado com moldura e 6 px depois dela
            var st = Css("400", 18, BONE); var sk = Css("700", 18, GOLD); float lh = LN(18f);
            float wk = tecla != null ? Larg(tecla, sk) + 10 + 2 : 0, wt = Larg(txt.Replace("<", "<\u200B"), st), w = wk + (tecla != null ? 6 : 0) + wt + 20, x = W / 2 - w / 2;
            Caixa(new Rect(x, y, w, lh + 6), new Color(8 / 255f, 9 / 255f, 11 / 255f, .72f));
            if (tecla != null) { Moldura(new Rect(x + 10, y + 3, wk, lh), GOLD); Linha(x + 10 + 6, y + 3, wk, lh, tecla, sk); }
            Linha(x + 10 + wk + (tecla != null ? 6 : 0), y + 3, wt + 4, lh, txt, st);
        }
        void InteracaoHud(Ator m)
        {
            var ctx = Sim.Contexts(g, m); if (ctx.e == null && ctx.t == null && string.IsNullOrEmpty(ctx.info)) return;
            float anel = m.sealing >= 0 ? (float)g.altars[m.sealing].seal : m.hold != null && m.hold.max > 0 ? (float)(m.hold.t / m.hold.max) : 0f;
            // o svg de 46 px, margin-bottom 4, e cada rótulo com margin-top 3; no toque o #interact fica centrado no ponto
            // (translate(-50%, -50%)) e encolhe para .85 em volta do meio
            int n = (ctx.e != null || !string.IsNullOrEmpty(ctx.info) ? 1 : 0) + (ctx.t != null ? 1 : 0); float hTudo = 46 + 4 + n * (3 + LN(18f) + 6), py = H / 2 + 46;
            System.Action desenho = () =>
            {
                float topo = hudToque ? py - hTudo / 2 : py, cx = W / 2, cy = topo + 23; Tracos.Anel(cx, cy, 19, 4, new Color(1, 1, 1, .18f)); if (anel > 0) Tracos.Arco(cx, cy, 19, 4, Mathf.Clamp01(anel), Desenho.Hex("#f3d58e"));
                float y = topo + 46 + 4 + 3;
                if (ctx.e != null) { Rotulo(y, EntradaUnity.Nome("use"), ctx.e.label.Replace("Segure E para ", "Segure para ")); y += LN(18f) + 6 + 3; }
                else if (!string.IsNullOrEmpty(ctx.info)) { Rotulo(y, null, ctx.info); y += LN(18f) + 6 + 3; }
                if (ctx.t != null) Rotulo(y, EntradaUnity.Nome("use2"), ctx.t.label.Replace("Segure T para ", "Segure: "));
            };
            if (hudToque) ComTransformacao(W / 2, py, W / 2, py, .85f, desenho); else desenho();
        }

        // ---------- HUD ----------
        // o véu da execução: o centro limpo, a borda fechando em sangue escuro (o #exeVeu do navegador)
        Texture2D texVeuExe;
        void VeuExecucao()
        {
            if (veuExe < .01f) return;
            if (texVeuExe == null)
            {
                int N = 128; texVeuExe = new Texture2D(N, N, TextureFormat.RGBA32, false); var px = new Color32[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                    {
                        float dx = (x + .5f) / N * 2 - 1, dy = (y + .5f) / N * 2 - 1, d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f; Color c;
                        if (d < .18f) c = new Color(0, 0, 0, 0); else if (d < .62f) c = Color.Lerp(new Color(30 / 255f, 0, 6 / 255f, 0), new Color(30 / 255f, 0, 6 / 255f, .75f), (d - .18f) / .44f); else c = Color.Lerp(new Color(30 / 255f, 0, 6 / 255f, .75f), new Color(0, 0, 0, .98f), (d - .62f) / .38f);
                        px[y * N + x] = c;
                    }
                texVeuExe.SetPixels32(px); texVeuExe.Apply(); texVeuExe.wrapMode = TextureWrapMode.Clamp;
            }
            var o = GUI.color; GUI.color = new Color(1, 1, 1, veuExe); GUI.DrawTexture(new Rect(0, 0, W, H), texVeuExe, ScaleMode.StretchToFill); GUI.color = o;
        }
        void Hud()
        {
            var m = Eu(); if (m == null) return; string papel = MeuPapel(); float agora = Time.unscaledTime;
            VeuExecucao();
            FaixasEscuras();
            // relógio e momento
            int mi = System.Math.Max(0, g.momento); string prox = mi + 1 < CFG.momentoNome.Length ? CFG.momentoNome[mi + 1] : null;
            var GA = g.altars.FirstOrDefault(A => A.state == "active" && A.grande);
            string nome = GA != null ? "Grande Ritual em " + GA.name + (papel == "C" || GA.localized ? ": " + Mathf.FloorToInt((float)GA.prog * 100) + "%" : "") : prox != null ? CFG.momentoNome[mi] + ", " + prox + " em seguida" : CFG.momentoNome[mi] + ", depois o amanhecer";
            int fendas = Sim.Fendas(g), farois = Sim.Farois(g), nos = papel == "C" ? fendas : farois, eles = papel == "C" ? farois : fendas;
            string regra = g.lab != null && g.lab.formato == "placar" ? "2 rituais decidem · a noite vai até o amanhecer" : "Quem levar 2 dos 3 rituais vence";
            // body.toque: o HUD encolhe e muda de lugar para os botões caberem (transform: scale com o transform-origin do CSS)
            hudToque = toqueAtivo; bool eco = g.lab == null || g.lab.economia;
            if (hudToque)
            {
                ComTransformacao(W / 2, 10, W / 2, 4, .72f, () => Topo(papel, Relogio(Sim.FimMomento(g, mi) - g.rt), nome, GA != null, regra, nos, eles));
                ComTransformacao(W / 2, 88, W / 2, 50, .78f, () => Faixa(papel)); Tarefas(papel); FeedHud(); AnuncioHud();
                ComTransformacao(12, 8, 12, 8, .78f, () => Vitais(m, eco)); ComTransformacao(W - 190, H - 96, W - 190, H - 96, .72f, () => KitHud(m)); SensorHud(m); LinhaRede();
            }
            else
            {
                Topo(papel, Relogio(Sim.FimMomento(g, mi) - g.rt), nome, GA != null, regra, nos, eles);
                Faixa(papel); Tarefas(papel); FeedHud(); AnuncioHud();
                Vitais(m, eco); KitHud(m); SensorHud(m); LinhaRede();
            }
            // #ammoHint embaixo da mira e #buffs embaixo da faixa
            string dica = "";
            if (m.st == "alive") { if (m.team == "H") { if (m.reloadT <= 0 && m.ammo == 0) dica = m.reserve > 0 ? "Sem munição: " + EntradaUnity.Nome("reload") + " para recarregar" : "Sem munição: volte ao spawn para repor"; else if (m.reloadT <= 0 && m.ammo <= 2) dica = "Munição baixa (" + m.ammo + ")"; } else if (m.fervor < CFG.sigilCusto) dica = "Fervor esgotado: pare de conjurar um instante para recuperar"; }
            if (dica != "") Linha(0, H / 2 + 30, W, 18, dica, Css("400", 15, GOLD_HI, TextAnchor.UpperCenter), .9f);
            var bf = new List<string>(); if (papel == "H" && g.buff.ess > g.t) bf.Add("Essência Profana: selamento a 125% (" + Mathf.CeilToInt((float)(g.buff.ess - g.t)) + " s)"); if (papel == "C" && g.buff.selo > g.t) bf.Add("Selo de Luz: próximo ritual +10% (" + Mathf.CeilToInt((float)(g.buff.selo - g.t)) + " s)"); if (m.runeT > 0) bf.Add("Runa ativa (" + Mathf.CeilToInt((float)m.runeT) + " s)");
            if (bf.Count > 0) { int fb = hudToque ? 12 : 15; Linha(0, hudToque ? 108 : 182, W, LN(fb), string.Join("   ", bf.ToArray()), Css("400", fb, GOLD_HI, TextAnchor.UpperCenter), .9f); }

            // mira, acerto, marcadores, dano, nota da habilidade, interação e a disputa do ritual
            Mira(); MarcaAcerto(); Marcadores(m, papel); DirecaoDano(); NotaHabilidade(); InteracaoHud(m); Disputa(m, papel);

            // tela: dano, cegueira, caído ou morto
            if (hurtT > 0) { hurtT -= dtG; Caixa(new Rect(0, 0, W, H), new Color(.6f, 0, 0, hurtT / .35f * .35f)); }
            if (blindT > 0) { blindT -= dtG; Caixa(new Rect(0, 0, W, H), new Color(1, 1, 1, Mathf.Clamp01(blindT / 1.2f))); }
            if (atordT > 0) { atordT -= dtG; Caixa(new Rect(0, 0, W, H), new Color(.95f, .84f, .55f, atordT * .35f)); }
            if (claraoGrande > 0) { claraoGrande -= dtG; Caixa(new Rect(0, 0, W, H), new Color(.8f, .05f, .1f, Mathf.Clamp01(claraoGrande / 1.2f) * .85f)); }
            TelaQueda(m);

            if (hudToque) ComTransformacao(W / 2, 148, W / 2, 84, .72f, () => Bussola(m, papel)); else Bussola(m, papel);
            Legendas(); FaixaLance(); Caderno(papel); PainelTutorial(); PainelDica(); ChatJogo(); PainelFalas();
            if (EntradaUnity.Segurando(Tecla.Tab)) Planta(m, papel);
        }

        // Marcadores na tela: aliados (inclusive caídos), mercadores perto, Cultistas denunciados pelo sal e o ritual revelado
        void Marcadores(Ator m, string papel)
        {
            var c = CamP(); System.Func<double, double, int> dm = (x, z) => Mathf.RoundToInt(Mathf.Sqrt((float)((x - c.x) * (x - c.x) + (z - c.z) * (z - c.z))));
            foreach (var a in g.actors)
            {
                if (a == m || a.hidden || a.team != m.team || a.st == "dead") continue;
                if (a.st == "down") Marcador(new Vector3((float)a.x, 1.1f, (float)a.z), a.name, "caído, " + dm(a.x, a.z) + " m", "caido", true);
                else Marcador(new Vector3((float)a.x, 2.35f, (float)a.z), a.name, null, m.team == "C" ? "cult" : "", false);
            }
            if (g.lab == null || g.lab.economia) foreach (var n in Mapa.NPCS) if (dm(n.x, n.z) < 14 && (n.time == null || n.time == m.team)) Marcador(new Vector3((float)n.x, 2.3f, (float)n.z), char.ToUpper(n.nome[0]) + n.nome.Substring(1), "mercador", "npc", false);
            if (papel == "H") foreach (var a in g.actors) if (a.team == "C" && !a.hidden && a.revelT > 0 && a.st == "alive") Marcador(new Vector3((float)a.x, 2.4f, (float)a.z), a.name, "denunciado pelo sal", "caido", true);
            // o objetivo do tutorial (altares no alto, pontos do chão mais baixos)
            if (tutAtivo && tutAlvo != null && !tutFim) Marcador(new Vector3((float)tutAlvo.x, tutAlvo is Altar ? 3.4f : 1.4f, (float)tutAlvo.z), "Objetivo", dm(tutAlvo.x, tutAlvo.z) + " m", "obj", true);
            for (int i = pings.Count - 1; i >= 0; i--) { var p = pings[i]; p.t -= dtG; if (p.t <= 0) { pings.RemoveAt(i); continue; } Marcador(new Vector3(p.x, p.txt.StartsWith("Altar") ? 3.6f : .8f, p.z), p.txt, p.nome + " · " + dm(p.x, p.z) + " m", "ping", true); }
            foreach (var A in g.altars) { if (A.state != "active" || (papel == "H" && !A.localized)) continue; Marcador(new Vector3((float)A.x, 3.2f, (float)A.z), (A.grande ? "Grande Ritual" : "Ritual") + " em " + A.name, dm(A.x, A.z) + " m", "obj", true); }
        }

    }
}
