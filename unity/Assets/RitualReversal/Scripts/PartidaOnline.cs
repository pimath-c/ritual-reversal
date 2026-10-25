// Partida online (connect / onSnap / interpActors / lobbyScreen do protótipo): entra numa sala do servidor Node,
// mostra o lobby, manda as entradas a cada quadro e desenha o que o snapshot conta. Cai a conexão no meio da noite?
// Tenta voltar por até 2 minutos com a mesma senha de retorno, que é quanto o servidor guarda a vaga.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        string dificuldade = "normal";
        bool online; Conexao rede; string salaCodigo = "", hostCid, erroRede = "", nomeJogador, servidor, codigoDigitado = "";
        string token; bool noLobby; List<Slot> lobbySlots = new List<Slot>();
        // tempo do servidor, previsão do próprio jogador e interpolação dos outros
        double? offset; int seq; readonly List<Entrada> pendentes = new List<Entrada>(); readonly Dictionary<Entrada, double> dtDe = new Dictionary<Entrada, double>(), seqDe = new Dictionary<Entrada, double>();
        Ator pred; int predId = -1; float erroX, erroZ, ping; readonly Dictionary<int, float> enviadas = new Dictionary<int, float>();
        class Quadro { public double t; public Dictionary<int, double[]> pos = new Dictionary<int, double[]>(); }
        readonly List<Quadro> quadros = new List<Quadro>();
        float reconAte; int reconTent; float reconT; bool reconectando;

        void CarregarPreferencias()
        {
            sensibilidade = PlayerPrefs.GetFloat("rr-sens", sensibilidade); dificuldade = PlayerPrefs.GetString("rr-dif", "normal");
            toqueAtivo = toqueGlobal = PlayerPrefs.GetInt("rr-toque", Application.isMobilePlatform ? 1 : 0) == 1;   // o TOQUE_DISP do navegador
            nomeJogador = PlayerPrefs.GetString("rr-nome", "Jogador"); servidor = PlayerPrefs.GetString("rr-servidor", "localhost:8080");
            token = PlayerPrefs.GetString("rr-token", ""); if (token.Length == 0) { token = System.Guid.NewGuid().ToString("N").Substring(0, 20); PlayerPrefs.SetString("rr-token", token); }
        }

        void Conectar(bool criar, string codigo)
        {
            PlayerPrefs.SetString("rr-nome", nomeJogador); PlayerPrefs.SetString("rr-servidor", servidor); PlayerPrefs.Save();
            if (rede != null) rede.Fechar();
            rede = new Conexao(); rede.Abrir(servidor); online = true; erroRede = "";
            rede.Enviar(HelloExtra(new JsonEscrita().Txt("t", "hello").Bool("create", criar).Txt("code", (codigo ?? "").ToUpperInvariant()).Txt("name", nomeJogador).Txt("token", token), criar).ToString()); chat.Clear();
        }
        void SairOnline(string msg)
        {
            if (rede != null) rede.Fechar(); rede = null; online = false; noLobby = false; reconectando = false; g = null; EU = "local"; LimparVisual(); erroRede = msg ?? ""; Travar(false);
        }

        void Enviar(JsonEscrita j) { if (rede != null) rede.Enviar(j.ToString()); }
        void EnviarAcao(Acao a)
        {
            var o = new JsonEscrita().Txt("type", a.type);
            if (a.type == "pick" || a.type == "slot") o.Num("i", a.i);
            if (a.type == "ping") { o.Num("x", a.x).Num("z", a.z).Txt("k", a.k).Num("i", a.i); }
            if (a.cls != null) o.Txt("cls", a.cls); if (a.f != null) o.Txt("f", a.f); if (a.npc != null) o.Txt("npc", a.npc); if (a.item != null) o.Txt("item", a.item);
            Enviar(new JsonEscrita().Txt("t", "act").Obj("a", o));
        }
        double ServerNow() { return offset.HasValue ? Time.realtimeSinceStartup + offset.Value : 0; }

        // mensagens do servidor (chamado no começo de cada quadro)
        void ProcessarRede()
        {
            if (rede == null) return;
            string txt;
            while (rede.Receber(out txt))
            {
                var m = Json.Ler(txt) as Dictionary<string, object>; if (m == null) continue; object tipo; m.TryGetValue("t", out tipo);
                switch (tipo as string)
                {
                    case "error": SairOnline(m["msg"] as string); return;
                    case "welcome":
                        EU = m["cid"] as string; salaCodigo = m["code"] as string; noLobby = true;
                        { object mp; if (m.TryGetValue("mapa", out mp) && mp is string) TrocarMundo((string)mp); }   // entrar numa sala de outro mapa troca o mundo
                        if (reconectando || m.ContainsKey("voltou")) { reconectando = false; quadros.Clear(); pred = null; pendentes.Clear(); offset = null; Feed("Conexão restabelecida. Você voltou ao seu personagem.", "big"); }
                        break;
                    case "lobby":
                        {
                            hostCid = m["host"] as string; lobbySlots.Clear(); LerLobbyExtra(m);
                            foreach (var o in (List<object>)m["slots"]) { var d = (Dictionary<string, object>)o; lobbySlots.Add(new Slot(d["team"] as string, d["cid"] as string, d["name"] as string)); }
                            if (!(m["inGame"] is bool && (bool)m["inGame"])) { g = null; quadros.Clear(); pred = null; LimparVisual(); }
                            break;
                        }
                    case "snap": AoSnapshot(m); break;
                    case "chat": ReceberChat(m); break;
                }
            }
            if (rede != null && rede.fechada)
            {
                // caiu no meio da noite: tenta voltar
                if (g != null && g.phase != "final" && !reconectando) { reconectando = true; reconAte = Time.realtimeSinceStartup + 118; reconTent = 0; reconT = .4f; Travar(false); }
                if (reconectando)
                {
                    if (Time.realtimeSinceStartup > reconAte) { SairOnline("Não foi possível voltar à partida a tempo."); return; }
                    reconT -= Time.unscaledDeltaTime; if (reconT <= 0) { reconT = 2; reconTent++; var cod = salaCodigo; Conectar(false, cod); }
                }
                else SairOnline(string.IsNullOrEmpty(rede.erro) ? "A conexão com o servidor caiu." : "Não consegui falar com o servidor: " + rede.erro);
            }
        }

        void AoSnapshot(Dictionary<string, object> m)
        {
            var s = (Dictionary<string, object>)m["s"]; var ng = Snapshot.Ler(s); noLobby = false;
            object lg; if (m.TryGetValue("log", out lg)) GuardarLogUrl(lg);
            float agora = Time.realtimeSinceStartup; double alvo = ng.t - agora + .02;
            offset = offset.HasValue ? offset.Value + (alvo - offset.Value) * .05 : alvo; if (System.Math.Abs(alvo - offset.Value) > .5) offset = alvo;
            object cidO; if (m.TryGetValue("cid", out cidO) && cidO is string) EU = (string)cidO;
            int ack = m.ContainsKey("ack") && m["ack"] != null ? System.Convert.ToInt32(m["ack"]) : 0;
            float ts; if (enviadas.TryGetValue(ack, out ts)) { float r = (agora - ts) * 1000; ping = ping > 0 ? ping * .8f + r * .2f : r; }
            var velhas = new List<int>(); foreach (var k in enviadas.Keys) if (k <= ack) velhas.Add(k); foreach (var k in velhas) enviadas.Remove(k);
            // quadro para a interpolação
            var q = new Quadro { t = ng.t }; foreach (var a in ng.actors) q.pos[a.id] = new[] { a.x, a.z, a.yaw, a.hidden ? 1 : 0 };
            quadros.Add(q); while (quadros.Count > 12) quadros.RemoveAt(0);
            // previsão do próprio jogador: estado do servidor + entradas ainda não confirmadas
            var meS = ng.actors.Find(a => a.cid == EU);
            if (meS != null)
            {
                bool novo = pred == null || predId != meS.id; float antesX = novo ? (float)meS.x : (float)pred.x, antesZ = novo ? (float)meS.z : (float)pred.z;
                var p = Snapshot.Ator(FindDict(s, meS.id));
                if (novo) { lookYaw = meS.yaw; lookPitch = 0; p.inp = new Entrada { yaw = meS.yaw }; pendentes.Clear(); }
                else { p.inp = pred.inp; p.yaw = pred.yaw; p.pitch = pred.pitch; }
                pred = p; predId = meS.id;
                pendentes.RemoveAll(i => seqDe[i] <= ack); foreach (var k in new List<Entrada>(dtDe.Keys)) if (!pendentes.Contains(k)) { dtDe.Remove(k); seqDe.Remove(k); }
                foreach (var i in pendentes) Sim.MoveHuman(ng, pred, i, dtDe[i]);
                float ex = antesX - (float)pred.x, ez = antesZ - (float)pred.z; if (!novo && Mathf.Sqrt(ex * ex + ez * ez) < 3) { erroX += ex; erroZ += ez; } else { erroX = 0; erroZ = 0; }
            }
            else pred = null;
            g = ng;
            var evs = new List<Evento>(); object evO; if (m.TryGetValue("ev", out evO) && evO is List<object>) foreach (var o in (List<object>)evO) { var d = o as Dictionary<string, object>; if (d != null) evs.Add(Snapshot.Evento(d)); }
            TratarEventos(evs, Eu());
        }
        static Dictionary<string, object> FindDict(Dictionary<string, object> s, int id)
        {
            foreach (var o in (List<object>)s["actors"]) { var d = (Dictionary<string, object>)o; if (System.Convert.ToInt32(d["id"]) == id) return d; }
            return null;
        }

        // a cada quadro: manda a entrada, prevê o próprio movimento e interpola os outros
        void PassoOnline(Entrada inp, double dt)
        {
            if (g == null) return;
            seq++; dtDe[inp] = dt; seqDe[inp] = seq; enviadas[seq] = Time.realtimeSinceStartup; if (enviadas.Count > 400) enviadas.Clear();
            if (pred != null && g.phase == "play") { Sim.MoveHuman(g, pred, inp, dt); pendentes.Add(inp); while (pendentes.Count > 120) { dtDe.Remove(pendentes[0]); seqDe.Remove(pendentes[0]); pendentes.RemoveAt(0); } }
            var c = new JsonEscrita().Num("q", inp.c.q).Num("f", inp.c.f).Num("g", inp.c.g).Num("r", inp.c.r).Num("ab", inp.c.ab);
            Enviar(new JsonEscrita().Txt("t", "in").Num("mx", inp.mx).Num("mz", inp.mz).Bool("sp", inp.sp).Bool("ag", inp.ag).Num("yaw", inp.yaw).Num("pitch", inp.pitch).Bool("fire", inp.fire).Bool("use", inp.use).Bool("use2", inp.use2)
                .Obj("c", c).Num("dt", dt).Num("viewT", inp.viewT).Num("seq", seq));
            // correção da previsão some aos poucos
            float suav = Mathf.Exp(-(float)dt * 14); erroX *= suav; erroZ *= suav;
            var eu = Eu(); if (eu != null && pred != null) { eu.x = pred.x + erroX; eu.z = pred.z + erroZ; eu.yaw = pred.yaw; eu.pitch = pred.pitch; eu.moving = pred.moving; }
            Interpolar();
        }
        void Interpolar()
        {
            double T = ServerNow() - .1; if (quadros.Count < 2) return;
            Quadro a = quadros[0], b = quadros[quadros.Count - 1];
            for (int i = quadros.Count - 1; i > 0; i--) if (quadros[i - 1].t <= T) { a = quadros[i - 1]; b = quadros[i]; break; }
            double k = JS.Clamp((T - a.t) / System.Math.Max(1e-3, b.t - a.t), 0, 1.2);
            foreach (var act in g.actors)
            {
                if (act.cid == EU) continue; double[] pa, pb; if (!a.pos.TryGetValue(act.id, out pa) || !b.pos.TryGetValue(act.id, out pb)) continue;
                if (pb[3] > 0) continue; if (pa[3] > 0) { act.x = pb[0]; act.z = pb[1]; act.yaw = pb[2]; continue; } // reapareceu: surge no lugar, sem deslizar desde a origem
                act.x = JS.Lerp(pa[0], pb[0], k); act.z = JS.Lerp(pa[1], pb[1], k);
                double dy = pb[2] - pa[2]; dy = ((dy + System.Math.PI) % (System.Math.PI * 2) + System.Math.PI * 2) % (System.Math.PI * 2) - System.Math.PI; act.yaw = pa[2] + dy * System.Math.Min(1, k);
            }
        }

        void TelaLobby() { TelaLobbyCss(); }
        void TelaReconexao() { TelaReconexaoCss(); }
    }
}
