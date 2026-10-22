// Telas do fim da noite (summaryScreen, finalScreen e replayScreen do index.html): o resumo da rodada, o resultado com os
// lances, os destaques, o placar de cada jogador, a linha do tempo de cada rodada e o registro, e o Rever a noite na
// planta. Os dados vêm de Noite.cs (LinhaDoTempo, anotada a cada quadro) e do registro da partida: local, o do jogo;
// online, o arquivo que o servidor libera no fim (o "log" do snapshot final), baixado uma vez.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        readonly LinhaDoTempo LT = new LinhaDoTempo();
        string logUrl, logId; List<Registro> logOnline; UnityEngine.Networking.UnityWebRequest pedidoLog; int logIni; string logMsg = "";
        Vector2 rolFim; readonly Dictionary<string, Texture2D> texGraficos = new Dictionary<string, Texture2D>();
        bool UmaNoite { get { return !((g != null && g.opts != null && g.opts.noites > 0 ? g.opts.noites : CFG.noites) > 1); } }

        // o registro desta noite (local: só o que veio depois do último "Jogar de novo"; online: o baixado, ou null)
        List<Registro> LogDaNoite()
        {
            if (g == null) return null;
            if (!online) return logIni > 0 && logIni <= g.log.Count ? g.log.GetRange(logIni, g.log.Count - logIni) : g.log;
            return logId == g.matchId ? logOnline : null;
        }
        // online: pede o arquivo uma vez no fim da noite. Fica no Update: o IMGUI desenha cada quadro em duas passadas
        // (Layout e Repaint), e o que muda entre elas desalinha os controles
        void BaixarLogOnline()
        {
            if (!online || g == null) return;
            if (logId != g.matchId) { logId = g.matchId; logOnline = null; if (pedidoLog != null) { pedidoLog.Dispose(); pedidoLog = null; } }
            if (logOnline == null && pedidoLog == null && g.phase == "final" && !string.IsNullOrEmpty(logUrl)) { pedidoLog = UnityEngine.Networking.UnityWebRequest.Get(logUrl); pedidoLog.SendWebRequest(); }
            if (pedidoLog != null && pedidoLog.isDone)
            {
                // se falhar, fica sem: as telas mostram o que a linha do tempo anotou
                try { logOnline = pedidoLog.result == UnityEngine.Networking.UnityWebRequest.Result.Success ? Noite.RegistroDoJson(pedidoLog.downloadHandler.text) : new List<Registro>(); }
                catch { logOnline = new List<Registro>(); }
                pedidoLog.Dispose(); pedidoLog = null;
            }
        }
        // o endereço do registro no servidor (o snapshot final traz o caminho, /logs/<partida>.json)
        void GuardarLogUrl(object caminho)
        {
            var c = caminho as string; if (string.IsNullOrEmpty(c)) return;
            var u = Conexao.Endereco(servidor); string http = (u.Scheme == "wss" ? "https://" : "http://") + u.Authority; logUrl = http + c;
        }
        // a cada quadro: a linha do tempo, o registro do servidor e o relógio do Rever a noite
        void PassoNoite(float dt)
        {
            if (g == null || arsenal) return; LT.Amostrar(g, EU); BaixarLogOnline();
            RodadaLT R; if (g.phase != "final" || LT.id != g.matchId) repAberto = false;
            if (repAberto && repPlay && !repArrastando && LT.rod.TryGetValue(repR, out R) && R.pos != null && R.pos.Count > 0) { double fim = R.pos[R.pos.Count - 1].rt; repT = System.Math.Min(fim, repT + dt * repVel); if (repT >= fim) repPlay = false; }
        }

        // ---------- pedaços das telas ----------
        GUIStyle sH3, sCel, sCelN; readonly Dictionary<Color, GUIStyle> fundos = new Dictionary<Color, GUIStyle>();
        void EstilosNoite()
        {
            if (sH3 != null) return;
            sH3 = new GUIStyle(sTexto) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = false }; sH3.normal.textColor = OURO;
            sCel = new GUIStyle(sPequeno) { fontSize = 15, wordWrap = false, clipping = TextClipping.Clip }; sCelN = new GUIStyle(sCel) { alignment = TextAnchor.MiddleRight };
        }
        GUIStyle Fundo(Color c)
        {
            GUIStyle s; if (fundos.TryGetValue(c, out s)) return s;
            var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); s = new GUIStyle { padding = new RectOffset(12, 12, 8, 8), margin = new RectOffset(4, 4, 4, 4) }; s.normal.background = t; fundos[c] = s; return s;
        }
        void H3(string t) { GUILayout.Space(14); GUILayout.Label(t.ToUpperInvariant(), sH3); }
        // texto com âncora no meio e y na linha de base (como o <text> do SVG)
        void TextoBase(float x, float yBase, string txt, int fs, Color c, FontStyle estilo = FontStyle.Normal)
        {
            var st = new GUIStyle(sPequeno) { fontSize = Mathf.Max(8, fs), alignment = TextAnchor.LowerCenter, wordWrap = false, richText = false, fontStyle = estilo }; st.normal.textColor = c;
            GUI.Label(new Rect(x - 200, yBase - st.fontSize * 1.2f, 400, st.fontSize * 1.45f), txt, st);
        }

        void LancesDaNoite(List<Registro> log)
        {
            var L = Noite.Lances(log); if (L.Count == 0) return; var eu = Eu(); H3("Lances da noite");
            for (int i = 0; i < L.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(L.Count, i + 2); j++)
                {
                    var l = L[j]; bool meu = eu != null && l.quem.Contains(eu.name);
                    GUILayout.BeginVertical(Fundo(l.culto ? new Color(.12f, .035f, .055f, .85f) : new Color(.1f, .085f, .05f, .85f)), GUILayout.Width(440));
                    GUILayout.Label("<size=12>" + Cor(Noite.Relogio(l.t), MUDO) + "</size>", sPequeno);
                    GUILayout.Label("<b>" + Cor(l.tit, meu ? OURO_VIVO : l.culto ? CARMIM_HI : OSSO) + "</b>", sPequeno);
                    GUILayout.Label("<size=14>" + Cor(l.sub, new Color(.79f, .75f, .68f)) + "</size>", sPequeno);
                    GUILayout.EndVertical();
                }
                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            }
        }
        void Destaques(List<Registro> log)
        {
            var P = Noite.Premios(log); if (P.Count == 0) return; var eu = Eu(); H3("Destaques da noite");
            for (int i = 0; i < P.Count; i += 3)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(P.Count, i + 3); j++)
                {
                    var p = P[j]; bool meu = eu != null && p.quem == eu.name;
                    GUILayout.BeginVertical(Fundo(meu ? new Color(.16f, .13f, .06f, .9f) : new Color(.07f, .07f, .08f, .85f)), GUILayout.Width(290));
                    GUILayout.Label("<b>" + Cor(p.nome, meu ? OURO_VIVO : OURO) + "</b>", sPequeno); GUILayout.Label(p.quem, sPequeno);
                    GUILayout.Label("<size=13>" + Cor(p.verbo + " " + p.n + " " + (p.n == 1 ? p.um : p.varios), MUDO) + "</size>", sPequeno);
                    GUILayout.EndVertical();
                }
                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            }
        }
        static readonly string[] COL_PLACAR = { "Derrubou", "Caiu", "Reanimou", "Altares", "Tarefas", "Compras" };
        void PlacarJogadores(List<Registro> log)
        {
            var rods = Noite.Placar(log); var eu = Eu();
            foreach (var P in rods)
            {
                H3(UmaNoite || rods.Count < 2 ? "Placar da noite" : "Placar da rodada " + P.r);
                GUILayout.BeginHorizontal(); GUILayout.Label(Cor("Jogador", MUDO), sCel, GUILayout.Width(230)); GUILayout.Label(Cor("Classe", MUDO), sCel, GUILayout.Width(170));
                foreach (var c in COL_PLACAR) GUILayout.Label(Cor(c, MUDO), sCelN, GUILayout.Width(84)); GUILayout.EndHorizontal();
                foreach (var L in P.linhas)
                {
                    bool meu = eu != null && L.nome == eu.name;
                    GUILayout.BeginHorizontal(Fundo(meu ? new Color(.16f, .13f, .06f, .7f) : new Color(0, 0, 0, .25f)));
                    GUILayout.Label("<b>" + Cor(L.nome, L.team == "H" ? OURO_VIVO : CARMIM_HI) + "</b>" + (L.humano ? "" : " <size=12>" + Cor("bot", MUDO) + "</size>"), sCel, GUILayout.Width(218));
                    GUILayout.Label(L.classe, sCel, GUILayout.Width(170)); foreach (var v in L.v) GUILayout.Label(v.ToString(), sCelN, GUILayout.Width(84));
                    GUILayout.EndHorizontal();
                }
            }
        }
        void TabelaRegistro()
        {
            Estatisticas r1, r2; if (!g.stats.TryGetValue(1, out r1)) return; g.stats.TryGetValue(2, out r2);
            H3("Registro da partida");
            GUILayout.BeginHorizontal(); GUILayout.Label("", sCel, GUILayout.Width(330)); GUILayout.Label(Cor("Rodada 1", MUDO), sCelN, GUILayout.Width(110)); if (r2 != null) GUILayout.Label(Cor("Rodada 2", MUDO), sCelN, GUILayout.Width(110)); GUILayout.EndHorizontal();
            foreach (var l in LINHAS)
            {
                GUILayout.BeginHorizontal(); GUILayout.Label(l[1], sCel, GUILayout.Width(330)); GUILayout.Label(Valor(r1, l[0]), sCelN, GUILayout.Width(110));
                if (r2 != null) GUILayout.Label(Valor(r2, l[0]), sCelN, GUILayout.Width(110)); GUILayout.EndHorizontal();
            }
        }

        // linha do tempo da rodada (graficoNoite): o fundo é uma textura; nomes dos momentos, ícones e legenda por cima
        void GraficoNoite(int r, float w)
        {
            RodadaLT R; if (!LT.rod.TryGetValue(r, out R) || R.am.Count == 0) return;
            var log = LogDaNoite(); var mk = Noite.MarcosFinais(R, log, r); double T = Noite.DuracaoGrafico(R);
            string chave = LT.id + "|" + r + "|" + R.am.Count; Texture2D tex;
            if (!texGraficos.TryGetValue(chave, out tex)) { foreach (var t in texGraficos.Values) Destroy(t); texGraficos.Clear(); texGraficos[chave] = tex = Noite.Grafico(R, g.lab, g.altars.Count); }
            H3(UmaNoite ? "A noite" : "A noite, rodada " + r);
            var rr = GUILayoutUtility.GetRect(w, w * 180 / Noite.GW, GUILayout.Width(w)); float s = rr.width / Noite.GW, y0 = rr.y + 14 * s;
            System.Func<double, float> X = t => rr.x + Noite.GX(t, T) * s;
            GUI.DrawTexture(new Rect(rr.x, y0, rr.width, Noite.GH * s), tex);
            for (int i = 0; i < CFG.momentoAte.Length; i++)
            {
                double a = i > 0 ? Noite.FimMomento(g.lab, i - 1) : 0; if (a >= T) continue;
                TextoBase(X((a + System.Math.Min(Noite.FimMomento(g.lab, i), T)) / 2), y0 + (Noite.GH + 14) * s, CFG.momentoNome[i], Mathf.RoundToInt(11 * s), Desenho.Hex("#8f877b"));
            }
            string dica = null; var mouse = Event.current.mousePosition;
            foreach (var m in mk)
            {
                string[] ic; if (!Noite.IC.TryGetValue(m.tipo, out ic)) ic = new[] { "•", "#cccccc" }; float x = X(m.rt);
                TextoBase(x, y0 - 2 * s, ic[0], Mathf.RoundToInt(12 * s), Desenho.Hex(ic[1]));
                if (Mathf.Abs(mouse.x - x) < 6 * s && mouse.y > y0 - 16 * s && mouse.y < y0 + 2 * s) dica = Noite.Relogio(m.rt) + " · " + m.txt;
            }
            if (dica != null) { var st = new GUIStyle(sPequeno) { fontSize = 14, wordWrap = false }; var tam = st.CalcSize(new GUIContent(dica)); var rd = new Rect(Mathf.Min(mouse.x + 12, rr.xMax - tam.x - 10), mouse.y + 14, tam.x + 12, tam.y + 6); Caixa(rd, new Color(0, 0, 0, .9f)); GUI.Label(new Rect(rd.x + 6, rd.y + 3, tam.x, tam.y), dica, st); }
            // legenda: os altares que tiveram ritual e o selamento
            GUILayout.BeginHorizontal();
            for (int i = 0; i < g.altars.Count; i++)
            {
                if (!R.am.Any(q => i < q.a.Length && q.a[i] != null)) continue;
                var rq = GUILayoutUtility.GetRect(14, 18, GUILayout.Width(14)); Caixa(new Rect(rq.x, rq.y + 5, 10, 10), Desenho.Hex(Noite.COR_ALT[i % 6])); GUILayout.Label("<size=13>" + g.altars[i].name + "</size>", sPequeno, GUILayout.ExpandWidth(false)); GUILayout.Space(10);
            }
            { var rq = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18)); for (int k = 0; k < 3; k++) Caixa(new Rect(rq.x + k * 6, rq.y + 9, 4, 2), Desenho.Hex("#f3d58e")); GUILayout.Label("<size=13>Selamento</size>", sPequeno, GUILayout.ExpandWidth(false)); }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            foreach (var m in mk.Where(m => m.tipo != "momento").Take(12)) GUILayout.Label("<size=14><b>" + Noite.Relogio(m.rt) + "</b>  " + m.txt + "</size>", sPequeno);
        }

        string TextoTempo() { return g.timers ? " (" + Mathf.CeilToInt((float)System.Math.Max(0, g.phaseT)) + " s)" : ""; }
        string ListaProntos() { return string.Join("   ", g.slots.Where(s => s.cid != null).Select(s => { bool p; g.ready.TryGetValue(s.cid, out p); return s.name + " " + (p ? "✓" : "…"); }).ToArray()); }

        void TelaResumo()
        {
            EstilosNoite(); bool pronto; g.ready.TryGetValue(EU, out pronto); var slot = MeuSlot();
            var sc = g.round == 1 ? g.scores["B"] : g.scores["A"]; bool cultoMeu = slot != null && (g.round == 1 ? "B" : "A") == slot.team;
            float w = Mathf.Min(1000, W - 40); var r = Janela(w, H - 60); GUILayout.BeginArea(r); rolFim = GUILayout.BeginScrollView(rolFim);
            GUILayout.Label(UmaNoite ? "O amanhecer" : "Fim da rodada " + g.round, sTitulo);
            if (sc != null) GUILayout.Label((cultoMeu ? "Sua equipe completou" : "Os Cultistas adversários completaram") + " <b>" + sc.done + "</b> de 3 rituais" + (sc.lastT.HasValue ? ", o último aos " + Relogio(sc.lastT.Value) : "") + ". " + sc.@sealed + (sc.@sealed == 1 ? " ritual foi selado." : " rituais foram selados."), sCentro);
            if (g.round == 1 && !UmaNoite) GUILayout.Label("Agora os lados se invertem: quem caçou passa a celebrar.", sCentro);
            if (!online) { var log = LogDaNoite().Where(e => e.r == g.round).ToList(); LancesDaNoite(log); Destaques(log); }
            GraficoNoite(g.round, r.width - 30); TabelaRegistro();
            GUILayout.Space(16); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUI.enabled = !pronto;
            if (GUILayout.Button(pronto ? "Aguardando os outros" : g.round == 1 && !UmaNoite ? "Continuar para a rodada 2" : "Ver resultado", sBotao, GUILayout.Height(52), GUILayout.Width(340))) Acao(new Acao { type = "ready" });
            GUI.enabled = true; GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.Label((online ? ListaProntos() : "") + TextoTempo(), sCentro);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }

        void TelaFinal()
        {
            EstilosNoite(); if (repAberto) { TelaReplay(); return; }
            var slot = MeuSlot(); var w = Sim.Winner(g) ?? new Vencedor { why = "" }; bool minha = slot != null && w.win == slot.team;
            Placar A = slot != null ? g.scores[slot.team] : null, B = slot != null ? g.scores[slot.team == "A" ? "B" : "A"] : null;
            string titulo = w.win == null ? "Empate" : minha ? "Vitória da sua equipe" : "Vitória dos adversários";
            float lw = Mathf.Min(1000, W - 40); var r = Janela(lw, H - 60); GUILayout.BeginArea(r); rolFim = GUILayout.BeginScrollView(rolFim);
            GUILayout.Label(Cor(titulo, w.win == null ? OSSO : minha ? OURO : CARMIM_VIVO), sTitulo);
            if (w.why.Length > 0) GUILayout.Label(char.ToUpper(w.why[0]) + w.why.Substring(1) + ".", sCentro);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal(); GUILayout.Label(Cor("Como Cultistas", MUDO), sCel, GUILayout.Width(330)); GUILayout.Label(Cor("Rituais", MUDO), sCelN, GUILayout.Width(110)); GUILayout.Label(Cor("Último ritual", MUDO), sCelN, GUILayout.Width(150)); GUILayout.Label(Cor("Maior progresso", MUDO), sCelN, GUILayout.Width(170)); GUILayout.EndHorizontal();
            System.Action<string, Placar> linha = (n, s) =>
            {
                if (s == null) return; GUILayout.BeginHorizontal(Fundo(new Color(0, 0, 0, .25f))); GUILayout.Label(n, sCel, GUILayout.Width(318)); GUILayout.Label(s.done.ToString(), sCelN, GUILayout.Width(110));
                GUILayout.Label(s.lastT.HasValue ? Relogio(s.lastT.Value) : "—", sCelN, GUILayout.Width(150)); GUILayout.Label(Mathf.RoundToInt((float)s.maxP * 100) + "%", sCelN, GUILayout.Width(170)); GUILayout.EndHorizontal();
            };
            linha(A != null && B == null ? "Sua equipe (culto)" : "Sua equipe", A); linha(B != null && A == null ? "Adversários (culto)" : "Adversários", B);
            var log = LogDaNoite();
            if (online && log == null) GUILayout.Label(Cor("Baixando o registro da noite do servidor…", MUDO), sCentro);
            LancesDaNoite(log); Destaques(log); PlacarJogadores(log); GraficoNoite(1, r.width - 30); GraficoNoite(2, r.width - 30); TabelaRegistro();
            GUILayout.Space(16); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            var sb = new GUIStyle(sBotao) { fontSize = 17 };
            if (LT.TemReplay(1) && GUILayout.Button("Rever a noite", sb, GUILayout.Height(48))) AbrirReplay(1);
            if (!UmaNoite && LT.TemReplay(2) && GUILayout.Button("Rever a rodada 2", sb, GUILayout.Height(48))) AbrirReplay(2);
            if (GUILayout.Button("Baixar registro (.json)", sb, GUILayout.Height(48))) BaixarRegistro();
            bool anfitriao = !online || hostCid == EU;
            if (anfitriao && GUILayout.Button("Jogar de novo", sb, GUILayout.Height(48))) { repAberto = false; if (!online) { g.matchId = System.DateTime.Now.ToString("yyyyMMdd-HHmmss"); logIni = g.log.Count; } LimparVisual(); Acao(new Acao { type = "restart" }); }
            if (online) { if (anfitriao && GUILayout.Button("Voltar ao lobby", sb, GUILayout.Height(48))) Enviar(new JsonEscrita().Txt("t", "lobby")); if (GUILayout.Button("Sair da sala", sb, GUILayout.Height(48))) SairOnline(""); }
            else if (GUILayout.Button("Voltar ao menu", sb, GUILayout.Height(48))) { g = null; LimparVisual(); }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            if (online && !anfitriao) GUILayout.Label("O anfitrião decide o próximo passo.", sCentro);
            if (logMsg != "") GUILayout.Label(Cor(logMsg, MUDO), sCentro);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        // local: grava o registro (com dificuldade, laboratório e papel, como o navegador) na pasta de dados do jogo;
        // online: o servidor guarda o arquivo, o navegador do sistema baixa
        void BaixarRegistro()
        {
            if (online) { if (!string.IsNullOrEmpty(logUrl)) { Application.OpenURL(logUrl); logMsg = "O servidor também guarda uma cópia na pasta logs."; } return; }
            var d = new Dictionary<string, object> { { "versao", Noite.VERSAO }, { "registro", Noite.REGISTRO }, { "match", g.matchId }, { "dificuldade", g.opts != null && !string.IsNullOrEmpty(g.opts.dificuldade) ? g.opts.dificuldade : "normal" },
                { "lab", new Dictionary<string, object> { { "crepusculo", g.lab.crepusculo }, { "fogo", g.lab.fogo }, { "execucao", g.lab.execucao }, { "economia", g.lab.economia }, { "formato", g.lab.formato } } },
                { "papel", MeuPapel() }, { "slots", g.slots.Select(s => (object)new Dictionary<string, object> { { "team", s.team }, { "cid", s.cid }, { "name", s.name } }).ToList() },
                { "events", LogDaNoite().Select(e => { var o = new Dictionary<string, object> { { "t", e.t }, { "r", e.r }, { "ev", e.ev } }; foreach (var kv in e.d) o[kv.Key] = kv.Value; return (object)o; }).ToList() } };
            try { string arq = System.IO.Path.Combine(Application.persistentDataPath, "ritual-reversal-" + g.matchId + ".json"); System.IO.File.WriteAllText(arq, Noite.ParaJson(d)); logMsg = "Registro salvo em " + arq; }
            catch (System.Exception e) { logMsg = "Não consegui salvar o registro: " + e.Message; }
        }

        // ================= REVER A NOITE =================
        // A planta da noite inteira: posições a cada 2 s (local: todos; online: só quem você via), quedas e altares.
        bool repAberto, repPlay, repArrastando; int repR = 1; double repT; float repVel = 20;
        void AbrirReplay(int r) { repAberto = true; repR = r; repT = 0; repPlay = true; som.Tocar("click"); }
        static readonly string[][] COR_REP = { new[] { "#f3d58e", "#c99a3e" }, new[] { "#ff6a7e", "#b8243e" } }; static readonly string[] COR_REP_D = { "#8ec4ff", "#4a7fd8" };
        void TelaReplay()
        {
            RodadaLT R; if (!LT.TemReplay(repR) || !LT.rod.TryGetValue(repR, out R)) { repAberto = false; return; }
            var P = R.pos; double fim = P[P.Count - 1].rt;
            bool dalt = PlayerPrefs.GetInt("rr-dalt", 0) == 1; var nTime = new Dictionary<string, int> { { "H", 0 }, { "C", 0 } };
            var cores = R.atores.Select(o => { var L = o.team == "C" ? (dalt ? COR_REP_D : COR_REP[1]) : o.team == "H" ? COR_REP[0] : null; if (L == null) return Desenho.Hex("#cccccc"); return Desenho.Hex(L[(nTime[o.team]++) % 2]); }).ToList();
            float lw = Mathf.Min(1000, W - 40); var r = Janela(lw, H - 60); GUILayout.BeginArea(r);
            GUILayout.Label("Rever a noite" + (UmaNoite ? "" : " · rodada " + repR), sTitulo);
            GUILayout.Label("<size=16>" + Cor((online ? "Os inimigos só aparecem onde você os viu. " : "") + "Os rastros mostram os últimos 20 segundos de cada um.", MUDO) + "</size>", sCentro);
            float mw = Mathf.Min(r.width, (r.height - 300) * PlantaFixa.L / PlantaFixa.A); var rm0 = GUILayoutUtility.GetRect(r.width, mw * PlantaFixa.A / PlantaFixa.L); var rm = new Rect(rm0.x + (rm0.width - mw) / 2, rm0.y, mw, rm0.height);
            PlantaBase(rm); DesenharReplay(rm, R, cores);
            GUILayout.BeginHorizontal();
            string bt = repPlay ? "Pausar" : repT >= fim - .5 ? "Repetir" : "Continuar";
            if (GUILayout.Button(bt, sBotao, GUILayout.Width(150))) { if (repT >= fim - .5) repT = 0; repPlay = !repPlay; }
            GUILayout.BeginVertical(); GUILayout.Space(16);
            float novo = GUILayout.HorizontalSlider((float)repT, 0, (float)System.Math.Ceiling(fim), GUILayout.ExpandWidth(true));
            if (Mathf.Abs(novo - (float)repT) > .01f) repT = Mathf.Round(novo); repArrastando = GUIUtility.hotControl != 0;   // segurando o controle, o relógio não anda
            GUILayout.EndVertical();
            GUILayout.Label("<b>" + Relogio(repT) + "</b>", sTexto, GUILayout.Width(70));
            foreach (var v in new[] { 10, 20, 40, 80 }) if (GUILayout.Button(repVel == v ? Cor(v + "×", OURO_VIVO) : v + "×", sBotao, GUILayout.Width(66))) repVel = v;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            for (int j = 0; j < R.atores.Count; j++) { var rq = GUILayoutUtility.GetRect(14, 18, GUILayout.Width(14)); Caixa(new Rect(rq.x, rq.y + 5, 10, 10), cores[j]); GUILayout.Label("<size=14>" + (R.atores[j].eu ? "<b>" + R.atores[j].nome + "</b>" : R.atores[j].nome) + "</size>", sPequeno, GUILayout.ExpandWidth(false)); GUILayout.Space(10); }
            GUILayout.Label("<size=14>✚ Queda</size>", sPequeno, GUILayout.ExpandWidth(false)); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            var mk = R.mkFinal ?? R.mk; var ult = mk.Where(m => m.tipo != "momento" && m.rt <= repT).Reverse().Take(4).ToList();
            for (int n = 0; n < 4; n++) GUILayout.Label(n < ult.Count ? "<size=14><b>" + Relogio(ult[n].rt) + "</b>  " + (n == 0 ? Cor(ult[n].txt, OURO_VIVO) : ult[n].txt) + "</size>" : " ", sPequeno);
            GUILayout.FlexibleSpace(); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (GUILayout.Button("Voltar ao resultado", sBotao, GUILayout.Height(48), GUILayout.Width(300))) repAberto = false;
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
        void DesenharReplay(Rect rm, RodadaLT R, List<Color> cores)
        {
            var P = R.pos; double t = repT; float s = rm.width / PlantaFixa.L;
            int k = 0; while (k < P.Count - 1 && P[k + 1].rt <= t) k++; int k2 = Mathf.Min(P.Count - 1, k + 1);
            float f = P[k2].rt > P[k].rt ? Mathf.Clamp01((float)((t - P[k].rt) / (P[k2].rt - P[k].rt))) : 0;
            AmostraLT am = null; foreach (var q in R.am) { if (q.rt > t) break; am = q; }
            float r0 = Mathf.Max(7, (float)CFG.circleR * PlantaFixa.S + 3);
            for (int i = 0; i < g.altars.Count; i++)
            {
                var A = g.altars[i]; var c = PontoSvg(rm, A.x, A.z); float rr = r0 * s;
                var fe = R.mk.FirstOrDefault(m => m.i == i && (m.tipo == "fenda" || m.tipo == "farol") && m.rt <= t); var at = am != null && i < am.a.Length ? am.a[i] : null;
                if (fe != null)
                {
                    bool fd = fe.tipo == "fenda"; Tracos.Disco(c.x, c.y, rr, Desenho.Hex(fd ? "#5a0e1e" : "#5a4a1e")); Tracos.Anel(c.x, c.y, rr, 2.5f * s, Desenho.Hex(fd ? "#e0435e" : "#f3d58e"));
                    TextoBase(c.x, c.y + 6 * s, fd ? "✕" : "✓", Mathf.RoundToInt(12 * s), Color.white);
                }
                else if (at != null)
                {
                    Tracos.Disco(c.x, c.y, rr, new Color(224 / 255f, 67 / 255f, 94 / 255f, .35f)); Tracos.Anel(c.x, c.y, rr, 2 * s, Desenho.Hex("#e0435e"));
                    Tracos.Arco(c.x, c.y, rr + 4 * s, 3 * s, at[0], Desenho.Hex("#ff8a9a")); if (at[1] > .01f) Tracos.Arco(c.x, c.y, rr + 8 * s, 2 * s, at[1], Desenho.Hex("#f3d58e"));
                }
            }
            foreach (var q in R.quedas)
            {
                if (q.rt > t) continue; bool novo = t - q.rt < 25; var c = PontoSvg(rm, q.x, q.z);
                TextoBase(c.x, c.y + 5 * s, "✚", Mathf.RoundToInt((novo ? 14 : 10) * s), novo ? Color.white : new Color(.49f, .52f, .56f, .6f));
            }
            for (int j = 0; j < R.atores.Count; j++)
            {
                var o = R.atores[j]; var cor = cores[j]; var pts = new List<Vector2>();
                for (int q = Mathf.Max(0, k - 10); q <= k; q++) { var v = P[q].p[j]; if (v != null) pts.Add(PontoSvg(rm, v[0], v[1])); else pts.Clear(); }
                var a = P[k].p[j]; var b = P[k2].p[j]; if (a == null) continue;
                float x = b != null ? a[0] + (b[0] - a[0]) * f : a[0], z = b != null ? a[1] + (b[1] - a[1]) * f : a[1]; var c = PontoSvg(rm, x, z); pts.Add(c);
                if (pts.Count > 1) Tracos.Polilinha(pts, 1.6f * s, new Color(cor.r, cor.g, cor.b, .55f));
                if (a[2] > 0) Tracos.Anel(c.x, c.y, 6 * s, 2 * s, cor);
                else if (o.team == "H") Tracos.Losango(c.x, c.y, 4.5f * s, cor, Color.black);
                else { Tracos.Disco(c.x, c.y, 5.5f * s, Color.black); Tracos.Disco(c.x, c.y, 5 * s, cor); }
                if (o.eu) Tracos.Anel(c.x, c.y, 9 * s, 1.4f * s, Color.white);
                TextoBase(c.x, c.y - 10 * s, o.nome, Mathf.RoundToInt(10.5f * s), Desenho.Hex("#e8e0d0"));
            }
        }
    }
}
