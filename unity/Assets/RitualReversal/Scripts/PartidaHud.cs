// HUD e telas da partida, em IMGUI (sem nenhum asset): título, escolha de altares, classe e feitiço, pausa, loja,
// resumo e resultado; durante o jogo, relógio, altares, tarefas, vida, sanidade, munição/fervor, interação, disputa
// do ritual, planta (Tab) e mensagens. Os textos são os do protótipo.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        GUIStyle sTitulo, sTexto, sPequeno, sGrande, sBotao, sCentro, sRelogio;
        Texture2D branco;
        float W, H; // tela virtual: 1080 de altura

        void Estilos()
        {
            if (sTexto != null) return;
            branco = Texture2D.whiteTexture;
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, richText = true }; sTexto.normal.textColor = OSSO;
            sPequeno = new GUIStyle(sTexto) { fontSize = 16 };
            sTitulo = new GUIStyle(sTexto) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sGrande = new GUIStyle(sTexto) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
            sCentro = new GUIStyle(sTexto) { alignment = TextAnchor.MiddleCenter };
            sRelogio = new GUIStyle(sTexto) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sBotao = new GUIStyle(GUI.skin.button) { fontSize = 20, wordWrap = true, richText = true, padding = new RectOffset(12, 12, 10, 10) };
        }
        void Caixa(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, branco); GUI.color = o; }
        void Barra(Rect r, float v, Color c) { Caixa(r, new Color(0, 0, 0, .55f)); Caixa(new Rect(r.x, r.y, r.width * Mathf.Clamp01(v), r.height), c); }
        static string Cor(string txt, Color c) { return "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + txt + "</color>"; }
        static string Relogio(double s) { s = System.Math.Max(0, s); int m = (int)(s / 60), ss = (int)(s % 60); return m + ":" + ss.ToString("00"); }
        // OnGUI roda mais de uma vez por quadro: os cronômetros do HUD só andam no desenho
        static float dtG { get { return Event.current != null && Event.current.type == EventType.Repaint ? Time.unscaledDeltaTime : 0; } }
        Rect Janela(float w, float h) { var r = new Rect((W - w) / 2, (H - h) / 2, w, h); Caixa(r, new Color(.03f, .03f, .05f, .92f)); return new Rect(r.x + 30, r.y + 24, w - 60, h - 48); }

        void OnGUI()
        {
            Estilos();
            float esc = Screen.height / 1080f; GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(esc, esc, 1)); W = Screen.width / esc; H = 1080f;
            if (g == null) { TelaTitulo(); return; }
            switch (g.phase)
            {
                case "pick": TelaEscolha(); break;
                case "intro": TelaIntro(); break;
                case "play": Hud(); if (lojaAberta != null) TelaLoja(); else if (!Travado()) TelaPausa(); break;
                case "summary": TelaResumo(); break;
                case "final": TelaFinal(); break;
            }
        }

        // ---------- telas ----------
        void TelaTitulo()
        {
            var r = Janela(900, 620); GUILayout.BeginArea(r);
            GUILayout.Label(Cor("Ritual Reversal", CARMIM_VIVO), sTitulo);
            GUILayout.Label("Dois Cultistas celebram rituais em segredo. Dois Caçadores investigam e selam. Uma noite de 18 minutos: Crepúsculo, Vigília e Hora Morta. Quem vencer dois dos três rituais vence a noite.", sCentro);
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Cor("Caçar", OURO) + "\nvocê e um bot contra dois Cultistas", sBotao, GUILayout.Height(90))) NovaPartida(true);
            if (GUILayout.Button(Cor("Celebrar", CARMIM_VIVO) + "\nvocê e um bot contra dois Caçadores", sBotao, GUILayout.Height(90))) NovaPartida(false);
            GUILayout.EndHorizontal(); GUILayout.Space(16);
            GUILayout.Label("<b>Controles</b>: WASD anda, Shift corre, mouse mira, clique esquerdo ataca, clique direito usa o feitiço. E (segurar) interage, T (segurar) transfere o ritual. Q sensor, F lanterna, G sinalizador, R recarrega (Caçador). Tab mostra a planta. Esc pausa.", sPequeno);
            GUILayout.Label("Porte para o Unity do protótipo: a mesma regra, os mesmos bots, o mesmo mundo, a mesma luz e o mesmo som.", sPequeno);
            GUILayout.EndArea();
        }

        void TelaEscolha()
        {
            if (MeuPapel() != "C") { var r0 = Janela(700, 200); GUI.Label(r0, "Os Cultistas estão escolhendo os altares…", sGrande); return; }
            var r = Janela(820, 640); GUILayout.BeginArea(r);
            GUILayout.Label("Escolham 3 altares", sTitulo);
            GUILayout.Label("Os Caçadores não sabem quais são. Altares perto da sua entrada (leste) são mais rápidos de alcançar, e mais óbvios.", sCentro);
            GUILayout.Space(12);
            for (int i = 0; i < g.altars.Count; i++)
            {
                bool sel = g.picks.Contains(i);
                if (GUILayout.Button((sel ? Cor("● ", CARMIM_VIVO) : "○ ") + g.altars[i].name + "   " + Cor(Lado(g.altars[i]), CINZA * 2f), sBotao)) Acao(new Acao { type = "pick", i = i });
            }
            GUILayout.Space(10); GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sortear", sBotao)) Acao(new Acao { type = "pickRandom" });
            GUI.enabled = g.picks.Count == 3; if (GUILayout.Button("Confirmar " + g.picks.Count + " de 3", sBotao)) Acao(new Acao { type = "pickConfirm" }); GUI.enabled = true;
            GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
        static string Lado(Altar A)
        {
            string ns = A.z < -60 ? "floresta, norte" : A.z > 60 ? "floresta, sul" : A.z < 0 ? "catedral, lado norte" : "catedral, lado sul";
            return ns + (A.x > 20 ? ", leste" : A.x < -20 ? ", oeste" : "");
        }

        void TelaIntro()
        {
            string papel = MeuPapel(); bool Hc = papel == "H"; string meu; if (!g.cls.TryGetValue(EU, out meu)) meu = Regras.CLASS_BY_TEAM[papel][0];
            string fe; if (!g.feit.TryGetValue(EU, out fe)) fe = Regras.FEIT_BY_TEAM[papel][0]; bool pronto; g.ready.TryGetValue(EU, out pronto);
            var r = Janela(1100, 820); GUILayout.BeginArea(r);
            GUILayout.Label(Cor("Esta noite você é " + (Hc ? "Caçador" : "Cultista"), Hc ? OURO : CARMIM_VIVO), sTitulo);
            GUILayout.Label(Hc
                ? "No <b>Crepúsculo</b> (3 min), leia os sinais nos santuários da floresta, que descartam altares que o culto não escolheu, e acenda sentinelas: elas avisam quando alguém consagra um altar por perto. Negocie com o Ermitão, na cabana perto do acampamento. Fique nas trilhas: a mata fechada é escura e abala a sanidade. Na <b>Vigília</b>, os altares despertam: purgue, investigue e sele. Resista até o amanhecer."
                : "No <b>Crepúsculo</b>, colha ervas na floresta, profane túmulos no cemitério e consagre seus altares em silêncio. Negocie com a Carpideira, no cemitério. Os reagentes livres ficam no Claustro. Na <b>Vigília</b>, os rituais podem começar. O último só na <b>Hora Morta</b>, e a catedral inteira vai ouvir.", sTexto);
            GUILayout.Space(8); GUILayout.Label("<b>Arquétipo</b>", sTexto); GUILayout.BeginHorizontal();
            foreach (var c in Regras.CLASS_BY_TEAM[papel]) { var C = Regras.CLASSES[c]; if (GUILayout.Button((c == meu ? Cor("● ", OURO) : "") + "<b>" + C.name + "</b>\n" + C.text + "\n<size=15>" + C.hp + " de vida</size>", sBotao, GUILayout.Height(170)) && !pronto) Acao(new Acao { type = "cls", cls = c }); }
            GUILayout.EndHorizontal(); GUILayout.Space(8); GUILayout.Label("<b>Feitiço</b> (clique direito)", sTexto); GUILayout.BeginHorizontal();
            foreach (var f in Regras.FEIT_BY_TEAM[papel]) { var F = Regras.FEITICOS[f]; if (GUILayout.Button((f == fe ? Cor("● ", OURO) : "") + "<b>" + F.nome + "</b>\n" + F.text + "\n<size=15>recarga de " + F.cd + " s</size>", sBotao, GUILayout.Height(150)) && !pronto) Acao(new Acao { type = "feit", f = f }); }
            GUILayout.EndHorizontal(); GUILayout.Space(14);
            GUI.enabled = !pronto; if (GUILayout.Button(pronto ? "Aguardando os outros" : "Estou pronto", sBotao, GUILayout.Height(56))) Acao(new Acao { type = "ready" }); GUI.enabled = true;
            GUILayout.EndArea();
        }

        void TelaPausa()
        {
            var r = Janela(560, 260); GUILayout.BeginArea(r);
            GUILayout.Label("Pausado", sTitulo); GUILayout.Space(10);
            if (GUILayout.Button("Continuar", sBotao, GUILayout.Height(52))) Travar(true);
            if (GUILayout.Button("Abandonar a partida", sBotao)) { g = null; LimparVisual(); }
            GUILayout.EndArea();
        }

        void TelaLoja()
        {
            var n = Mapa.NPCS.Find(x => x.id == lojaAberta); var m = Eu(); if (n == null || m == null) return;
            var r = Janela(760, 120 + 92 * n.vende.Count); GUILayout.BeginArea(r);
            GUILayout.Label(char.ToUpper(n.nome[0]) + n.nome.Substring(1) + "   " + Cor(m.obolos + " óbolos", OURO), sGrande);
            foreach (var id in n.vende)
            {
                var it = Regras.ITENS[id]; bool serve = (it.time == null || it.time == m.team) && (n.time == null || n.time == m.team);
                GUI.enabled = serve && m.obolos >= it.preco;
                if (GUILayout.Button("<b>" + it.nome + "</b>  " + Cor(it.preco + " óbolos", OURO) + "\n<size=16>" + it.desc + "</size>", sBotao, GUILayout.Height(80))) Acao(new Acao { type = "comprar", npc = n.id, item = id });
                GUI.enabled = true;
            }
            GUILayout.Label("E ou Esc fecha a loja.", sPequeno); GUILayout.EndArea();
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
        void Estatisticas()
        {
            Estatisticas e; if (!g.stats.TryGetValue(1, out e)) return;
            foreach (var l in LINHAS) { GUILayout.BeginHorizontal(); GUILayout.Label(l[1], sPequeno); GUILayout.FlexibleSpace(); GUILayout.Label(Valor(e, l[0]), sPequeno); GUILayout.EndHorizontal(); }
        }
        void TelaResumo()
        {
            var sc = g.scores[g.round == 1 ? "B" : "A"]; bool pronto; g.ready.TryGetValue(EU, out pronto);
            var r = Janela(760, 760); GUILayout.BeginArea(r);
            GUILayout.Label("O amanhecer", sTitulo);
            if (sc != null) GUILayout.Label("Os Cultistas completaram <b>" + sc.done + "</b> de 3 rituais" + (sc.lastT.HasValue ? ", o último aos " + Relogio(sc.lastT.Value) : "") + ". " + sc.@sealed + (sc.@sealed == 1 ? " ritual foi selado." : " rituais foram selados."), sCentro);
            GUILayout.Space(10); Estatisticas(); GUILayout.Space(10);
            GUI.enabled = !pronto; if (GUILayout.Button("Ver resultado", sBotao, GUILayout.Height(52))) Acao(new Acao { type = "ready" }); GUI.enabled = true;
            GUILayout.EndArea();
        }
        void TelaFinal()
        {
            var slot = MeuSlot(); var w = Sim.Winner(g) ?? new Vencedor { why = "" }; bool minha = slot != null && w.win == slot.team;
            string titulo = w.win == null ? "Empate" : minha ? "Vitória da sua equipe" : "Vitória dos adversários";
            var r = Janela(760, 780); GUILayout.BeginArea(r);
            GUILayout.Label(Cor(titulo, w.win == null ? OSSO : minha ? OURO : CARMIM_VIVO), sTitulo);
            if (w.why.Length > 0) GUILayout.Label(char.ToUpper(w.why[0]) + w.why.Substring(1) + ".", sCentro);
            GUILayout.Space(10); Estatisticas(); GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Jogar de novo", sBotao, GUILayout.Height(52))) { LimparVisual(); Acao(new Acao { type = "restart" }); }
            if (GUILayout.Button("Trocar de lado", sBotao, GUILayout.Height(52))) { g = null; LimparVisual(); }
            GUILayout.EndHorizontal(); GUILayout.EndArea();
        }

        // ---------- HUD ----------
        void Hud()
        {
            var m = Eu(); if (m == null) return; string papel = MeuPapel(); float agora = Time.unscaledTime;
            // relógio e momento
            int mi = System.Math.Max(0, g.momento); string prox = mi + 1 < CFG.momentoNome.Length ? CFG.momentoNome[mi + 1] : null;
            var GA = g.altars.FirstOrDefault(A => A.state == "active" && A.grande);
            string nome = GA != null ? "Grande Ritual em " + GA.name + (papel == "C" || GA.localized ? ": " + Mathf.FloorToInt((float)GA.prog * 100) + "%" : "") : prox != null ? CFG.momentoNome[mi] + ", " + prox + " em seguida" : CFG.momentoNome[mi] + ", depois o amanhecer";
            GUI.Label(new Rect(W / 2 - 200, 10, 400, 40), Relogio(CFG.momentoAte[mi] - g.rt), sRelogio);
            GUI.Label(new Rect(W / 2 - 300, 48, 600, 26), Cor(nome, GA != null ? CARMIM_VIVO : OURO), sCentro);
            int fendas = Sim.Fendas(g), farois = Sim.Farois(g);
            GUI.Label(new Rect(W / 2 - 300, 74, 600, 24), Cor("Fendas " + fendas, CARMIM_VIVO) + "   ·   " + Cor("Faróis " + farois, OURO), sCentro);

            // altares
            float x0 = W / 2 - 3 * 150;
            for (int i = 0; i < g.altars.Count; i++)
            {
                var A = g.altars[i]; string ch = "?"; Color c = CINZA * 1.6f; bool barra = false;
                if (A.state == "fenda") { ch = "✕"; c = CARMIM_VIVO; }
                else if (A.state == "farol") { ch = "✓"; c = OURO; }
                else if (papel == "H") { string k = g.know[i]; if (A.state == "active" && A.localized) { ch = "!"; c = CARMIM_VIVO; barra = true; } else if (k == "desperto") { ch = "◆"; c = ROXO_VIVO; } else if (k == "confirmado") { ch = "◆"; c = ROXO_VIVO; } else if (k == "chamariz") { ch = "◆"; c = CINZA * 2f; } else if (k == "limpo") { ch = "·"; c = CINZA; } }
                else { if (A.state == "active") { ch = "!"; c = CARMIM_VIVO; barra = true; } else if (A.state == "awake") { ch = "◆"; c = A.decoy ? CINZA * 2f : ROXO_VIVO; } else { ch = A.chosen ? "○" : "·"; c = A.chosen ? CARMIM_VIVO : CINZA; } }
                var rr = new Rect(x0 + i * 150, 102, 144, 44); Caixa(rr, new Color(0, 0, 0, .45f));
                GUI.Label(new Rect(rr.x, rr.y, rr.width, 26), Cor(ch + " " + A.name, c), sCentro);
                if (barra) Barra(new Rect(rr.x + 8, rr.y + 32, rr.width - 16, 6), (float)A.prog, CARMIM_VIVO);
            }

            // tarefas
            List<Tarefa> tarefas; if (g.tarefas.TryGetValue(papel, out tarefas) && tarefas.Count > 0)
            {
                float y = 170; GUI.Label(new Rect(W - 440, y, 420, 26), Cor(CFG.momentoNome[mi], OURO), sTexto); y += 28;
                foreach (var t in tarefas) { GUI.Label(new Rect(W - 440, y, 420, 50), (t.feita ? Cor("✓ ", OURO) : "") + t.nome + (t.meta > 0 ? " (" + t.prog + "/" + t.meta + ")" : ""), sPequeno); y += 46; }
            }

            // mensagens
            float fy = 170; foreach (var a in feed)
            {
                float idade = agora - a.t; if (idade > 9f) continue; var c = a.cls.Contains("h") ? OURO : a.cls.Contains("c") ? CARMIM_VIVO : OSSO; c.a = Mathf.Clamp01((9f - idade) / 2f);
                var st = new GUIStyle(sPequeno); st.normal.textColor = c; GUI.Label(new Rect(20, fy, 520, 44), a.msg, st); fy += 40;
            }
            if (anuncioT > 0) { anuncioT -= dtG; var st = new GUIStyle(sGrande); var c = OSSO; c.a = Mathf.Clamp01(anuncioT); st.normal.textColor = c; GUI.Label(new Rect(W / 2 - 500, 250, 1000, 50), anuncio, st); }

            // vida, sanidade e classe
            var cl = Regras.CLASSES[m.cls];
            GUI.Label(new Rect(24, H - 150, 400, 30), Cor(cl.name, m.team == "H" ? OURO : CARMIM_VIVO) + "  <size=16>" + (m.team == "H" ? "Caçador" : "Cultista") + "</size>", sTexto);
            Barra(new Rect(24, H - 112, 320, 18), (float)(m.hp / m.maxHp), new Color(.75f, .15f, .2f));
            if (m.shield > 0) Barra(new Rect(24, H - 112, 320 * (float)(m.shield / m.maxHp), 6), 1f, new Color(.7f, .8f, 1f));
            GUI.Label(new Rect(352, H - 118, 100, 30), Mathf.CeilToInt((float)System.Math.Max(0, m.hp)).ToString(), sTexto);
            string san = m.sanity < 15 ? "Sanidade: pânico, você se denuncia" : m.sanity < 40 ? "Sanidade: abalado" : m.sanity < 70 ? "Sanidade: inquieto" : "Sanidade";
            Barra(new Rect(24, H - 82, 320, 10), (float)(m.sanity / 100), new Color(.45f, .55f, .8f)); GUI.Label(new Rect(24, H - 70, 420, 26), san, sPequeno);
            GUI.Label(new Rect(24, H - 44, 600, 26), Cor(Mathf.FloorToInt(m.obolos) + " óbolos", OURO) + (m.team == "C" ? "   Reagentes " + m.reag + "/" + CFG.carry + "   Chamarizes " + g.decoys : "   Sinalizadores " + m.flares + "   Lanterna " + Mathf.RoundToInt((float)m.carga) + "%" + (m.sensorCd > 0 ? "   Sensor " + Mathf.CeilToInt((float)m.sensorCd) + " s" : "   Sensor pronto")), sPequeno);

            // munição, fervor e feitiço
            string arma = m.team == "H" ? (m.reloadT > 0 ? "recarregando…" : m.ammo + " <size=18>/ " + m.reserve + "</size>") : Mathf.FloorToInt((float)m.fervor) + " <size=18>Fervor</size>";
            GUI.Label(new Rect(W - 324, H - 120, 300, 50), "<size=36>" + arma + "</size>", new GUIStyle(sTexto) { alignment = TextAnchor.MiddleRight });
            var F = Regras.FEITICOS[m.feitico];
            GUI.Label(new Rect(W - 424, H - 64, 400, 30), F.nome + (m.abilCd > 0 ? ": " + Mathf.CeilToInt((float)m.abilCd) + " s" : ": pronto (clique direito)"), new GUIStyle(sPequeno) { alignment = TextAnchor.MiddleRight });
            string dica = "";
            if (m.st == "alive") { if (m.team == "H") { if (m.reloadT <= 0 && m.ammo == 0) dica = m.reserve > 0 ? "Sem munição: R para recarregar" : "Sem munição: volte ao spawn para repor"; else if (m.reloadT <= 0 && m.ammo <= 2) dica = "Munição baixa (" + m.ammo + ")"; } else if (m.fervor < CFG.sigilCusto) dica = "Fervor esgotado: pare de conjurar um instante para recuperar"; }
            if (dica != "") GUI.Label(new Rect(W - 624, H - 150, 600, 30), dica, new GUIStyle(sPequeno) { alignment = TextAnchor.MiddleRight });
            var bf = new List<string>(); if (papel == "H" && g.buff.ess > g.t) bf.Add("Essência Profana: selamento a 125% (" + Mathf.CeilToInt((float)(g.buff.ess - g.t)) + " s)"); if (papel == "C" && g.buff.selo > g.t) bf.Add("Selo de Luz: próximo ritual +10% (" + Mathf.CeilToInt((float)(g.buff.selo - g.t)) + " s)"); if (m.runeT > 0) bf.Add("Runa ativa (" + Mathf.CeilToInt((float)m.runeT) + " s)");
            if (bf.Count > 0) GUI.Label(new Rect(W / 2 - 400, H - 40, 800, 30), string.Join("   ", bf.ToArray()), new GUIStyle(sPequeno) { alignment = TextAnchor.MiddleCenter });

            // sensor
            if (sensorT > 0) { sensorT -= dtG; string s = sensorV > .6 ? "Sinal forte" : sensorV > .3 ? "Sinal médio" : sensorV > .02 ? "Sinal fraco" : "Nenhum sinal"; GUI.Label(new Rect(W / 2 - 150, H - 190, 300, 26), "Sensor Áurico: " + s, sCentro); Barra(new Rect(W / 2 - 120, H - 162, 240, 8), (float)sensorV, ROXO_VIVO); }

            // mira e interação
            Caixa(new Rect(W / 2 - 2, H / 2 - 2, 4, 4), new Color(1, 1, 1, .8f));
            if (hitT > 0)
            {
                hitT -= dtG; float hs = marcaAcerto == "kill" ? 1.15f : marcaAcerto == "head" ? .9f : .6f;
                var hc = marcaAcerto == "kill" ? CARMIM_VIVO : marcaAcerto == "head" ? OURO : OSSO; hc.a = Mathf.Clamp01(hitT / .15f);
                GUI.Label(new Rect(W / 2 - 40, H / 2 - 40, 80, 80), "<size=" + Mathf.RoundToInt(44 * hs) + ">" + Cor("✕", hc) + "</size>", sCentro);
            }
            Marcadores(m, papel);
            // direção do dano: arcos vermelhos ao redor da mira
            for (int i = danos.Count - 1; i >= 0; i--)
            {
                var d = danos[i]; d.t -= dtG; if (d.t <= 0) { danos.RemoveAt(i); continue; }
                var mt = GUI.matrix; var pv = new Vector3(W / 2, H / 2, 0); GUI.matrix = mt * Matrix4x4.Translate(pv) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, d.ang * Mathf.Rad2Deg)) * Matrix4x4.Translate(-pv);
                var dc = new Color(.9f, .1f, .15f, Mathf.Clamp01(d.t / 1.2f) * .85f); Caixa(new Rect(W / 2 - 60, H / 2 - 170, 120, 10), dc); GUI.matrix = mt;
            }
            if (notaT > 0) { notaT -= dtG; var nc = notaCor; nc.a = Mathf.Clamp01(notaT / .4f); GUI.Label(new Rect(W / 2 - 400, H / 2 + 130, 800, 30), Cor(nota, nc), sCentro); }
            var ctx = Sim.Contexts(g, m);
            float iy = H / 2 + 60;
            if (ctx.e != null) { GUI.Label(new Rect(W / 2 - 400, iy, 800, 30), "[E] " + ctx.e.label.Replace("Segure E para ", "Segure para ").Replace("Aperte E para ", ""), sCentro); iy += 30; }
            else if (ctx.info != null && ctx.info != "") { GUI.Label(new Rect(W / 2 - 400, iy, 800, 30), ctx.info, sCentro); iy += 30; }
            if (ctx.t != null) { GUI.Label(new Rect(W / 2 - 400, iy, 800, 30), "[T] " + ctx.t.label.Replace("Segure T para ", "Segure: "), sCentro); iy += 30; }
            float anel = m.sealing >= 0 ? (float)g.altars[m.sealing].seal : m.hold != null && m.hold.max > 0 ? (float)(m.hold.t / m.hold.max) : 0f;
            if (anel > 0) Barra(new Rect(W / 2 - 100, iy + 4, 200, 8), anel, m.sealing >= 0 ? OURO : OSSO);

            // disputa do ritual mais próximo
            Altar R = null; double bd = 1e9;
            foreach (var A in g.altars) { if (A.state != "active") continue; if (papel == "H" && !A.localized && Sim.Dist(m, A) > CFG.ritRad[System.Math.Max(1, A.lastN)]) continue; double d = Sim.Dist(m, A); if (d < bd && (d < 30 || papel == "C")) { bd = d; R = A; } }
            if (R != null)
            {
                var rr = new Rect(W / 2 - 220, 160, 440, 92); Caixa(rr, new Color(0, 0, 0, .5f));
                GUI.Label(new Rect(rr.x, rr.y + 4, rr.width, 24), R.name, sCentro);
                Barra(new Rect(rr.x + 20, rr.y + 32, 400, 10), (float)R.prog, CARMIM_VIVO); Barra(new Rect(rr.x + 20, rr.y + 48, 400, 10), (float)R.seal, OURO);
                int n = g.actors.Count(c => c.team == "C" && c.st == "alive" && Sim.InCircle(c, R)), s = g.actors.Count(h => h.team == "H" && h.sealing == R.i); string nota;
                if (s > 0) nota = s >= 2 ? "Selando em dupla" : "Selando";
                else if (n == 0) nota = R.prog > R.cp ? "Círculo vazio: o ritual recua até o checkpoint" : "Círculo vazio";
                else { double mf = Sim.MareFactor(g); nota = n + " " + (n > 1 ? "canalizando" : "canalizando sozinho") + (mf > 1 ? ", Maré Profana +" + Mathf.RoundToInt((float)(mf - 1) * 100) + "%" : ""); }
                GUI.Label(new Rect(rr.x, rr.y + 62, rr.width, 26), "<size=16>Ritual " + Mathf.FloorToInt((float)R.prog * 100) + "% · Selo " + Mathf.FloorToInt((float)R.seal * 100) + "% · " + nota + "</size>", sCentro);
            }

            // tela: dano, cegueira, caído ou morto
            if (hurtT > 0) { hurtT -= dtG; Caixa(new Rect(0, 0, W, H), new Color(.6f, 0, 0, hurtT / .35f * .35f)); }
            if (blindT > 0) { blindT -= dtG; Caixa(new Rect(0, 0, W, H), new Color(1, 1, 1, Mathf.Clamp01(blindT / 1.2f))); }
            if (atordT > 0) { atordT -= dtG; Caixa(new Rect(0, 0, W, H), new Color(.95f, .84f, .55f, atordT * .35f)); }
            if (claraoGrande > 0) { claraoGrande -= dtG; Caixa(new Rect(0, 0, W, H), new Color(.8f, .05f, .1f, Mathf.Clamp01(claraoGrande / 1.2f) * .85f)); }
            if (m.st == "dead") { Caixa(new Rect(0, 0, W, H), new Color(0, 0, 0, .6f)); GUI.Label(new Rect(0, H / 2 - 60, W, 50), "Você morreu", sTitulo); GUI.Label(new Rect(0, H / 2, W, 30), "Retorno em " + Mathf.CeilToInt((float)m.respawnT) + " s", sCentro); }
            else if (m.st == "down") { Caixa(new Rect(0, 0, W, H), new Color(.2f, 0, 0, .45f)); GUI.Label(new Rect(0, H / 2 - 60, W, 50), "Você está caído", sTitulo); GUI.Label(new Rect(0, H / 2, W, 30), "Um aliado pode te reanimar. Sangrando: " + Mathf.CeilToInt((float)m.downT) + " s" + (m.revUsed ? " (sem reanimação neste ciclo)" : ""), sCentro); }

            if (EntradaUnity.Segurando(Tecla.Tab)) Planta(m, papel);
        }

        // Marcadores na tela: aliados (inclusive caídos), mercadores perto, Cultistas denunciados pelo sal e o ritual revelado
        void Marcador(Vector3 p, string txt, string sub, Color c, bool prender)
        {
            var sp = cam.WorldToScreenPoint(new Vector3(p.x, p.y, -p.z)); bool atras = sp.z < 0; float esc = Screen.height / 1080f;
            float sx = sp.x / esc, sy = H - sp.y / esc;
            if (atras) { if (!prender) return; sx = W - sx; sy = H - 8; }
            if (prender) { sx = Mathf.Clamp(sx, 50, W - 50); sy = Mathf.Clamp(sy, 110, H - 60); }
            GUI.Label(new Rect(sx - 150, sy - 44, 300, 26), Cor(txt, c), sCentro);
            if (sub != null) GUI.Label(new Rect(sx - 150, sy - 22, 300, 22), "<size=14>" + sub + "</size>", sCentro);
            Caixa(new Rect(sx - 3, sy - 3, 6, 6), c);
        }
        void Marcadores(Ator m, string papel)
        {
            var c = CamP(); System.Func<double, double, int> dm = (x, z) => Mathf.RoundToInt(Mathf.Sqrt((float)((x - c.x) * (x - c.x) + (z - c.z) * (z - c.z))));
            foreach (var a in g.actors)
            {
                if (a == m || a.team != m.team || a.st == "dead") continue;
                if (a.st == "down") Marcador(new Vector3((float)a.x, 1.1f, (float)a.z), a.name, "caído, " + dm(a.x, a.z) + " m", CARMIM_VIVO, true);
                else Marcador(new Vector3((float)a.x, 2.35f, (float)a.z), a.name, null, m.team == "C" ? CARMIM_VIVO : OURO, false);
            }
            foreach (var n in Mapa.NPCS) if (dm(n.x, n.z) < 14 && (n.time == null || n.time == m.team)) Marcador(new Vector3((float)n.x, 2.3f, (float)n.z), char.ToUpper(n.nome[0]) + n.nome.Substring(1), "mercador", ROXO_VIVO, false);
            if (papel == "H") foreach (var a in g.actors) if (a.team == "C" && a.revelT > 0 && a.st == "alive") Marcador(new Vector3((float)a.x, 2.4f, (float)a.z), a.name, "denunciado pelo sal", CARMIM_VIVO, true);
            foreach (var A in g.altars) { if (A.state != "active" || (papel == "H" && !A.localized)) continue; Marcador(new Vector3((float)A.x, 3.2f, (float)A.z), (A.grande ? "Grande Ritual" : "Ritual") + " em " + A.name, dm(A.x, A.z) + " m", CARMIM_VIVO, true); }
        }

        // planta (Tab): catedral, Claustro, altares como o seu lado os conhece, você e seus aliados
        void Planta(Ator m, string papel)
        {
            float S = Mathf.Min(1100f / (float)(Mapa.HX * 2), 760f / (float)(Mapa.HZ * 2)), ox = W / 2, oz = H / 2;
            System.Func<double, float> X = x => ox + (float)x * S; System.Func<double, float> Z = z => oz + (float)z * S;
            Caixa(new Rect(X(-Mapa.HX), Z(-Mapa.HZ), (float)Mapa.HX * 2 * S, (float)Mapa.HZ * 2 * S), new Color(.05f, .08f, .06f, .92f));
            Caixa(new Rect(X(Mapa.CAT_X1), Z(Mapa.CAT_Z1), (float)(Mapa.CAT_X2 - Mapa.CAT_X1) * S, (float)(Mapa.CAT_Z2 - Mapa.CAT_Z1) * S), new Color(.09f, .1f, .12f, .95f));
            Caixa(new Rect(X(Mapa.CL_X1), Z(Mapa.CL_Z1), (float)(Mapa.CL_X2 - Mapa.CL_X1) * S, (float)(Mapa.CL_Z2 - Mapa.CL_Z1) * S), new Color(.12f, .13f, .15f, .95f));
            foreach (var n in Mapa.NPCS) { Caixa(new Rect(X(n.x) - 4, Z(n.z) - 4, 8, 8), OURO); GUI.Label(new Rect(X(n.x) - 80, Z(n.z) - 30, 160, 24), n.nome, new GUIStyle(sPequeno) { alignment = TextAnchor.MiddleCenter }); }
            foreach (var A in g.altars)
            {
                Color c = CINZA * 1.5f; string k = g.know[A.i];
                if (A.state == "fenda") c = CARMIM; else if (A.state == "farol") c = OURO;
                else if (papel == "C") c = A.state == "active" ? CARMIM_VIVO : A.chosen ? (A.state == "awake" ? ROXO_VIVO : CARMIM) : A.decoy ? CINZA * 2f : c;
                else c = A.state == "active" && A.localized ? CARMIM_VIVO : k == "confirmado" ? ROXO_VIVO : k == "desperto" ? ROXO : k == "chamariz" ? CINZA * 2f : k == "limpo" ? CINZA * .6f : c;
                Caixa(new Rect(X(A.x) - 9, Z(A.z) - 9, 18, 18), c);
                GUI.Label(new Rect(X(A.x) - 90, Z(A.z) + (A.z < 0 ? 12 : -36), 180, 24), A.name, new GUIStyle(sPequeno) { alignment = TextAnchor.MiddleCenter });
            }
            foreach (var a in g.actors) if (a.team == m.team && a.st != "dead") Caixa(new Rect(X(a.x) - 5, Z(a.z) - 5, 10, 10), a == m ? Color.white : (a.team == "H" ? OURO : CARMIM_VIVO));
        }
    }
}
