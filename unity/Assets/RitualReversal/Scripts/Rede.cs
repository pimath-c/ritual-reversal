// Cliente online: conversa com o servidor Node do protótipo (server/server.js) pelo mesmo protocolo do navegador.
// hello / welcome / lobby / slot / start / in / act / snap. O snapshot vira um Jogo (o mesmo tipo da partida local),
// então o HUD, o visual e o som não precisam saber se a partida é local ou online. O próprio jogador é previsto
// (Sim.MoveHuman com as entradas ainda não confirmadas, correção que some aos poucos); os outros são interpolados
// 0,1 s no passado entre dois snapshots, e quem some da visão fica escondido em vez de deslizar desde a origem.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public class Conexao
    {
        ClientWebSocket ws; readonly ConcurrentQueue<string> recebidas = new ConcurrentQueue<string>(); readonly ConcurrentQueue<string> envio = new ConcurrentQueue<string>();
        readonly SemaphoreSlim sinal = new SemaphoreSlim(0); CancellationTokenSource cancelar = new CancellationTokenSource();
        public volatile bool aberta, fechada; public volatile string erro;

        // aceita http(s)://, ws(s):// ou só host:porta
        public static Uri Endereco(string s)
        {
            s = (s ?? "").Trim().TrimEnd('/'); if (s.Length == 0) s = "localhost:8080";
            if (s.StartsWith("https://")) s = "wss://" + s.Substring(8); else if (s.StartsWith("http://")) s = "ws://" + s.Substring(7); else if (!s.StartsWith("ws")) s = "ws://" + s;
            return new Uri(s);
        }
        public void Abrir(string endereco)
        {
            ws = new ClientWebSocket(); var uri = Endereco(endereco); var tk = cancelar.Token;
            Task.Run(async () =>
            {
                try
                {
                    await ws.ConnectAsync(uri, tk); aberta = true;
                    var envioT = Task.Run(async () =>
                    {
                        while (!tk.IsCancellationRequested && ws.State == WebSocketState.Open)
                        {
                            await sinal.WaitAsync(tk); string m;
                            while (envio.TryDequeue(out m)) await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(m)), WebSocketMessageType.Text, true, tk);
                        }
                    });
                    var buf = new byte[1 << 16]; var acc = new List<byte>();
                    while (ws.State == WebSocketState.Open && !tk.IsCancellationRequested)
                    {
                        var r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), tk);
                        if (r.MessageType == WebSocketMessageType.Close) break;
                        for (int i = 0; i < r.Count; i++) acc.Add(buf[i]);
                        if (r.EndOfMessage) { recebidas.Enqueue(Encoding.UTF8.GetString(acc.ToArray())); acc.Clear(); }
                    }
                }
                catch (Exception e) { if (!tk.IsCancellationRequested) erro = e.Message; }
                finally { aberta = false; fechada = true; }
            });
        }
        public void Enviar(string json) { if (fechada) return; envio.Enqueue(json); sinal.Release(); }
        public bool Receber(out string m) { return recebidas.TryDequeue(out m); }
        public void Fechar() { cancelar.Cancel(); try { if (ws != null && ws.State == WebSocketState.Open) ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { } fechada = true; }
    }

    // JSON mínimo para o que o cliente manda
    public class JsonEscrita
    {
        readonly StringBuilder sb = new StringBuilder("{"); bool primeiro = true;
        JsonEscrita Chave(string k) { if (!primeiro) sb.Append(','); primeiro = false; sb.Append('"').Append(k).Append("\":"); return this; }
        public JsonEscrita Txt(string k, string v) { Chave(k); if (v == null) sb.Append("null"); else { sb.Append('"'); foreach (char c in v) { if (c == '"' || c == '\\') sb.Append('\\'); if (c < ' ') sb.Append(' '); else sb.Append(c); } sb.Append('"'); } return this; }
        public JsonEscrita Num(string k, double v) { Chave(k); sb.Append(double.IsNaN(v) || double.IsInfinity(v) ? "0" : v.ToString("R", CultureInfo.InvariantCulture)); return this; }
        public JsonEscrita Bool(string k, bool v) { Chave(k); sb.Append(v ? "true" : "false"); return this; }
        public JsonEscrita Obj(string k, JsonEscrita o) { Chave(k); sb.Append(o.ToString()); return this; }
        public override string ToString() { return sb.ToString() + "}"; }
    }

    // snapshot (Sim.snapshot do JS) -> Jogo
    public static class Snapshot
    {
        static double D(object o) { return o == null ? 0 : Convert.ToDouble(o, CultureInfo.InvariantCulture); }
        static int I(object o) { return o == null ? 0 : (int)Math.Round(Convert.ToDouble(o, CultureInfo.InvariantCulture)); }
        static bool B(object o) { return o is bool && (bool)o; }
        static string S(object o) { return o as string; }
        static Dictionary<string, object> O(object o) { return o as Dictionary<string, object>; }
        static List<object> L(object o) { return o as List<object> ?? new List<object>(); }
        static object G(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) ? v : null; }

        public static Ator Ator(Dictionary<string, object> a)
        {
            var r = new Ator
            {
                feitico = S(G(a, "feitico")), amuleto = B(G(a, "amuleto")), id = I(G(a, "id")), cid = S(G(a, "cid")), human = B(G(a, "human")), name = S(G(a, "name")), team = S(G(a, "team")), cls = S(G(a, "cls")),
                maxHp = D(G(a, "maxHp")), st = S(G(a, "st")) ?? "alive", reag = I(G(a, "reag")), sealing = G(a, "sealing") == null ? -1 : I(G(a, "sealing")), ammo = I(G(a, "ammo")), reserve = I(G(a, "reserve")),
                fervor = D(G(a, "fervor")), lantern = B(G(a, "lantern")), flares = I(G(a, "flares")), moving = B(G(a, "moving")), deaths = I(G(a, "deaths")), revUsed = B(G(a, "revUsed")),
                obolos = I(G(a, "obolos")), veuT = D(G(a, "veuT")), slowT = D(G(a, "slowT")), revelT = D(G(a, "revelT")), curaT = D(G(a, "curaT")), x = D(G(a, "x")), z = D(G(a, "z")), yaw = D(G(a, "yaw")), pitch = D(G(a, "pitch")),
                hp = D(G(a, "hp")), downT = D(G(a, "downT")), respawnT = D(G(a, "respawnT")), reloadT = D(G(a, "reloadT")), sanity = D(G(a, "sanity")), carga = D(G(a, "carga")), sensorCd = D(G(a, "sensorCd")),
                abilCd = D(G(a, "abilCd")), lastShotT = D(G(a, "lastShotT")), blindT = D(G(a, "blindT")), stunT = D(G(a, "stunT")), runeT = D(G(a, "runeT")), shield = D(G(a, "shield")), invuln = D(G(a, "invuln")), speed = D(G(a, "speed")),
                hidden = B(G(a, "hidden"))
            };
            var h = O(G(a, "hold")); if (h != null) r.hold = new Hold(S(G(h, "key")) ?? "", D(G(h, "t")), D(G(h, "max")));
            r.inp.yaw = r.yaw; r.inp.pitch = r.pitch;
            return r;
        }

        public static Jogo Ler(Dictionary<string, object> s)
        {
            var g = new Jogo { t = D(G(s, "t")), rt = D(G(s, "rt")), phase = S(G(s, "phase")) ?? "pick", phaseT = D(G(s, "phaseT")), round = I(G(s, "round")), timers = B(G(s, "timers")), matchId = S(G(s, "matchId")) };
            foreach (var o in L(G(s, "slots"))) { var d = O(o); g.slots.Add(new Slot(S(G(d, "team")), S(G(d, "cid")), S(G(d, "name")))); }
            foreach (var o in L(G(s, "altars")))
            {
                var d = O(o); var A = new Altar { i = I(G(d, "i")), name = S(G(d, "name")), x = D(G(d, "x")), z = D(G(d, "z")), state = S(G(d, "state")), chosen = B(G(d, "chosen")), decoy = B(G(d, "decoy")), prog = D(G(d, "prog")), cp = D(G(d, "cp")), seal = D(G(d, "seal")), localized = B(G(d, "localized")), grande = B(G(d, "grande")), lastN = Math.Max(1, I(G(d, "lastN"))), maxProg = D(G(d, "maxProg")) };
                if (G(d, "doneT") != null) A.doneT = D(G(d, "doneT")); g.altars.Add(A);
            }
            foreach (var o in L(G(s, "reagents"))) { var d = O(o); g.reagents.Add(new Reagente { i = I(G(d, "i")), x = D(G(d, "x")), z = D(G(d, "z")), has = B(G(d, "has")) }); }
            foreach (var o in L(G(s, "actors"))) g.actors.Add(Ator(O(o)));
            foreach (var o in L(G(s, "proj"))) { var d = O(o); g.proj.Add(new Projetil { id = I(G(d, "id")), x = D(G(d, "x")), y = D(G(d, "y")), z = D(G(d, "z")), vx = D(G(d, "vx")), vy = D(G(d, "vy")), vz = D(G(d, "vz")) }); }
            foreach (var o in L(G(s, "pickups"))) { var d = O(o); g.pickups.Add(new Coletavel { id = I(G(d, "id")), kind = S(G(d, "kind")), x = D(G(d, "x")), z = D(G(d, "z")) }); }
            foreach (var o in L(G(s, "flares"))) { var d = O(o); g.flares.Add(new Sinalizador { x = D(G(d, "x")), z = D(G(d, "z")), t = D(G(d, "t")) }); }
            var know = L(G(s, "know")); g.know = new string[know.Count]; for (int i = 0; i < know.Count; i++) g.know[i] = S(know[i]) ?? "?";
            g.visit = new double[g.altars.Count];
            g.momento = I(G(s, "momento"));
            var tar = O(G(s, "tarefas"));
            if (tar != null) foreach (var kv in tar)
                {
                    var lista = new List<Tarefa>();
                    foreach (var o in L(kv.Value)) { var d = O(o); lista.Add(new Tarefa { id = S(G(d, "id")), nome = S(G(d, "nome")), meta = I(G(d, "meta")), rec = I(G(d, "rec")), prog = I(G(d, "prog")), feita = B(G(d, "feita")) }); }
                    g.tarefas[kv.Key] = lista;
                }
            foreach (var o in L(G(s, "pontos"))) { var d = O(o); g.pontos.Add(new PontoNoite { i = I(G(d, "i")), tipo = S(G(d, "tipo")), x = D(G(d, "x")), z = D(G(d, "z")), ativo = B(G(d, "ativo")), acesa = B(G(d, "acesa")) }); }
            foreach (var o in L(G(s, "sal"))) { var d = O(o); g.sal.Add(new Sal { id = I(G(d, "id")), x = D(G(d, "x")), z = D(G(d, "z")) }); }
            var feit = O(G(s, "feit")); if (feit != null) foreach (var kv in feit) g.feit[kv.Key] = S(kv.Value);
            g.decoys = I(G(s, "decoys")); g.transferUsed = B(G(s, "transferUsed"));
            foreach (var o in L(G(s, "pegadas"))) { var d = O(o); g.pegadas.Add(new Pegada { x = D(G(d, "x")), z = D(G(d, "z")), yaw = D(G(d, "yaw")), t = D(G(d, "t")), l = I(G(d, "l")) }); }
            foreach (var o in L(G(s, "rastros"))) { var d = O(o); g.rastros.Add(new Rastro { x = D(G(d, "x")), z = D(G(d, "z")), t = D(G(d, "t")) }); }
            foreach (var o in L(G(s, "picks"))) g.picks.Add(I(o));
            var rd = O(G(s, "ready")); if (rd != null) foreach (var kv in rd) g.ready[kv.Key] = B(kv.Value);
            var cl = O(G(s, "cls")); if (cl != null) foreach (var kv in cl) g.cls[kv.Key] = S(kv.Value);
            var sc = O(G(s, "scores"));
            if (sc != null) foreach (var kv in sc)
                {
                    var d = O(kv.Value); if (d == null) { g.scores[kv.Key] = null; continue; }
                    var p = new Placar { done = I(G(d, "done")), @sealed = I(G(d, "sealed")), maxP = D(G(d, "maxP")) }; if (G(d, "lastT") != null) p.lastT = D(G(d, "lastT")); g.scores[kv.Key] = p;
                }
            var st = O(G(s, "stats"));
            if (st != null) foreach (var kv in st)
                {
                    var d = O(kv.Value); if (d == null) continue; int ronda; if (!int.TryParse(kv.Key, out ronda)) continue;
                    var e = new Estatisticas
                    {
                        rituais = I(G(d, "rituais")), completos = I(G(d, "completos")), selados = I(G(d, "selados")), chegadas = I(G(d, "chegadas")), tentativasSelo = I(G(d, "tentativasSelo")), cancelados = I(G(d, "cancelados")),
                        quedas = I(G(d, "quedas")), mortes = I(G(d, "mortes")), reanimacoes = I(G(d, "reanimacoes")), purgas = I(G(d, "purgas")), chamarizes = I(G(d, "chamarizes")), transferencias = I(G(d, "transferencias"))
                    };
                    if (G(d, "fracaoSobFogo") != null) e.fracaoSobFogo = D(G(d, "fracaoSobFogo")); if (G(d, "primeiroContato") != null) e.primeiroContato = D(G(d, "primeiroContato"));
                    g.stats[ronda] = e;
                }
            // o snapshot manda quanto falta; o HUD compara com g.t
            var bf = O(G(s, "buff")); if (bf != null) { double ess = D(G(bf, "ess")), selo = D(G(bf, "selo")); g.buff.ess = ess > 0 ? g.t + ess : 0; g.buff.selo = selo > 0 ? g.t + selo : 0; }
            g.lastResolveT = D(G(s, "lastResolveT"));
            // v6.2: o mapa da sala; no asilo, portas (1 aberta, 0 fechada) e o que falta de cada apagão
            g.mapa = S(G(s, "mapa")) ?? "catedral";
            var pt = G(s, "portas") as List<object>;
            if (pt != null)
            {
                g.portas = pt.Select(x => D(x) != 0).ToList(); Mapa.Usar(g.mapa);
                g.fechadas = Mapa.PORTAS_DIN.Where(p => p.i < g.portas.Count && !g.portas[p.i]).Select(p => new Caixa { x1 = p.x1, x2 = p.x2, z1 = p.z1, z2 = p.z2, h = 4.5, tall = true, porta = p.i }).ToList();
            }
            var en = O(G(s, "energia")); if (en != null) { g.energia = new Dictionary<string, double>(); foreach (var kv in en) { double r = D(kv.Value); g.energia[kv.Key] = r > 0 ? g.t + r : 0; } }
            // regras do laboratório de playtest escolhidas pelo anfitrião (relógio do Crepúsculo, mercadores)
            var lb = O(G(s, "lab")); if (lb != null) g.lab = Lab.De(new Lab { crepusculo = D(G(lb, "crepusculo")), fogo = S(G(lb, "fogo")), execucao = D(G(lb, "execucao")), economia = !(G(lb, "economia") is bool) || B(G(lb, "economia")), formato = S(G(lb, "formato")) });
            return g;
        }

        public static Evento Evento(Dictionary<string, object> d)
        {
            var e = new Evento { type = S(G(d, "type")) };
            foreach (var kv in d) if (kv.Key != "type") e.d[kv.Key] = kv.Value;
            return e;
        }
    }
}
