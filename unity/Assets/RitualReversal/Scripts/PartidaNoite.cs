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
            RodadaLT R; if (g.phase != "final" || LT.id != g.matchId) { repAberto = false; relAberto = false; }
            if (repAberto && repPlay && !repArrastando && LT.rod.TryGetValue(repR, out R) && R.pos != null && R.pos.Count > 0) { double fim = R.pos[R.pos.Count - 1].rt; repT = System.Math.Min(fim, repT + dt * repVel); if (repT >= fim) repPlay = false; }
        }

        // ---------- pedaços das telas ----------
        // texto com âncora no meio e y na linha de base (como o <text> do SVG)
        void TextoBase(float x, float yBase, string txt, int fs, Color c, FontStyle estilo = FontStyle.Normal)
        {
            var st = new GUIStyle(sPequeno) { fontSize = Mathf.Max(8, fs), alignment = TextAnchor.LowerCenter, wordWrap = false, richText = false, fontStyle = estilo }; st.normal.textColor = c;
            TextoNitido(new Rect(x - 200, yBase - st.fontSize * 1.2f, 400, st.fontSize * 1.45f), txt, SoSimbolos(st, txt), false);
        }

        static readonly string[] COL_PLACAR = { "Derrubou", "Caiu", "Reanimou", "Altares", "Tarefas", "Compras" };


        string TextoTempo() { return g.timers ? " (" + Mathf.CeilToInt((float)System.Math.Max(0, g.phaseT)) + " s)" : ""; }
        string ListaProntos() { return string.Join("   ", g.slots.Where(s => s.cid != null).Select(s => { bool p; g.ready.TryGetValue(s.cid, out p); return s.name + " " + (p ? "✓" : "…"); }).ToArray()); }

        void TelaResumo() { TelaResumoCss(); }
        void TelaFinal() { if (repAberto) { TelaReplayCss(); return; } if (relAberto) { TelaRelatorioCss(); return; } TelaFinalCss(); }
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
                    Tracos.Disco(c.x, c.y, rr, new Color(224 / 255f, 67 / 255f, 94 / 255f, .35f)); Tracos.Anel(c.x, c.y, rr, 2 * s, Desenho.Hex("#e3262e"));
                    Tracos.Arco(c.x, c.y, rr + 4 * s, 3 * s, at[0], Desenho.Hex("#ff8a9a")); if (at[1] > .01f) Tracos.Arco(c.x, c.y, rr + 8 * s, 2 * s, at[1], Desenho.Hex("#ffffff"));
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
                TextoBase(c.x, c.y - 10 * s, o.nome, Mathf.RoundToInt(10.5f * s), Desenho.Hex("#ece6da"));
            }
        }
    }
}
