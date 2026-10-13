// Roda uma partida só de bots na simulação em C# e escreve o estado a cada passo, no mesmo formato que
// tools/comparar-cs.js escreve para o shared/sim.js. Uso (chamado pelo comparar-cs.js):
//   mono CompararSim.exe <simulacao.json> <semente> <passos> <saida> [partidas] [bots|H|C]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RitualReversal.Simulacao;

static class CompararSim
{
    static string H(double v) { if (v == 0) v = 0; return ((ulong)BitConverter.DoubleToInt64Bits(v)).ToString("x"); }
    static string V(object v)
    {
        if (v == null) return "null";
        if (v is bool) return (bool)v ? "true" : "false";
        if (v is string) return "\"" + (string)v + "\"";
        if (v is int || v is double || v is long) return H(Convert.ToDouble(v, CultureInfo.InvariantCulture));
        if (v is Dictionary<string, object>) return O((Dictionary<string, object>)v);
        if (v is System.Collections.IEnumerable) { var sb = new StringBuilder("["); bool pri = true; foreach (var x in (System.Collections.IEnumerable)v) { if (!pri) sb.Append(','); sb.Append(V(x)); pri = false; } return sb.Append(']').ToString(); }
        return "?" + v;
    }
    static string O(Dictionary<string, object> d)
    {
        var ks = d.Keys.ToList(); ks.Sort(string.CompareOrdinal);
        return "{" + string.Join(",", ks.Select(k => k + ":" + V(d[k]))) + "}";
    }
    static string Linha(Jogo g, int nLog)
    {
        var p = new List<string>();
        p.Add("t:" + H(g.t)); p.Add("ph:" + g.phase); p.Add("rt:" + H(g.rt)); p.Add("m:" + g.momento);
        foreach (var a in g.actors)
            p.Add(string.Join(",", new[] { a.id.ToString(), a.st, H(a.x), H(a.z), H(a.yaw), H(a.hp), a.reag.ToString(), a.sealing.ToString(), a.hold.key, H(a.hold.t), a.ai.goalKey, H(a.fervor), a.ammo.ToString(), a.reserve.ToString(), a.obolos.ToString(), H(a.sanity), a.ai.path.Count.ToString(), a.cls, a.feitico }));
        foreach (var A in g.altars) p.Add(string.Join(",", new[] { A.state, A.chosen ? "1" : "0", A.decoy ? "1" : "0", H(A.prog), H(A.seal), H(A.cp), A.localized ? "1" : "0" }));
        p.Add(string.Join(",", g.know)); p.Add(string.Join("", g.reagents.Select(R => R.has ? "1" : "0")));
        p.Add(string.Join(";", g.proj.Select(q => q.id + "@" + H(q.x) + "," + H(q.y) + "," + H(q.z))));
        p.Add(string.Join(";", g.pickups.Select(q => q.id + q.kind + "@" + H(q.x) + "," + H(q.z))));
        p.Add("d" + g.decoys + (g.transferUsed ? "T" : "F"));
        foreach (var e in g.events) { var d = new Dictionary<string, object>(e.d); p.Add("E" + e.type + O(d)); }
        for (int i = nLog; i < g.log.Count; i++) { var r = g.log[i]; var d = new Dictionary<string, object>(r.d); d["t"] = r.t; d["r"] = r.r; d["ev"] = r.ev; p.Add("L" + O(d)); }
        return string.Join("|", p);
    }
    // jogador roteirizado: espelho exato da função roteiro() de tools/comparar-cs.js
    class Dir { public Ponto alvo; public double trocaT, compraT; public List<Ponto> path = new List<Ponto>(); public Controles c = new Controles(); }
    static void Roteiro(Jogo g, Dir D, Func<double> r)
    {
        double dt = 1.0 / 30;
        if (g.phase == "pick") { if (r() < .02) Sim.Act(g, "eu", new Acao { type = "pick", i = (int)Math.Floor(r() * 6) }); if (r() < .01) Sim.Act(g, "eu", new Acao { type = "pickRandom" }); if (r() < .005) Sim.Act(g, "eu", new Acao { type = "pickConfirm" }); Sim.Step(g, dt); return; }
        if (g.phase == "intro")
        {
            string role = Sim.RoleOf(g, g.slots.First(s => s.cid == "eu").team);
            if (r() < .02) { var L = Regras.CLASS_BY_TEAM[role]; Sim.Act(g, "eu", new Acao { type = "cls", cls = L[(int)Math.Floor(r() * L.Length)] }); }
            if (r() < .02) { var L = Regras.FEIT_BY_TEAM[role]; Sim.Act(g, "eu", new Acao { type = "feit", f = L[(int)Math.Floor(r() * L.Length)] }); }
            if (r() < .004) Sim.Act(g, "eu", new Acao { type = "ready" }); Sim.Step(g, dt); return;
        }
        if (g.phase != "play") { Sim.Step(g, dt); return; }
        var m = g.actors.First(a => a.cid == "eu");
        if (D.alvo == null || g.t > D.trocaT)
        {
            var Ls = new List<Ponto>(); Ls.AddRange(g.altars); Ls.AddRange(g.reagents); Ls.AddRange(Mapa.NPCS); Ls.AddRange(g.pontos);
            int k = (int)Math.Floor(r() * Ls.Count); D.alvo = new Ponto(Ls[k].x, Ls[k].z); D.trocaT = g.t + 15 + r() * 15; D.path = Mapa.FindPath(m, D.alvo);
        }
        if (D.path.Count > 0 && JS.Hypot(D.path[0].x - m.x, D.path[0].z - m.z) < .8) D.path.RemoveAt(0);
        if (D.path.Count == 0 && JS.Hypot(D.alvo.x - m.x, D.alvo.z - m.z) > 2 && r() < .1) D.path = Mapa.FindPath(m, D.alvo);
        Ponto q = D.path.Count > 0 ? D.path[0] : null; double yaw = q != null ? JS.Atan2(-(q.x - m.x), -(q.z - m.z)) : m.yaw; bool fire = false;
        Ator inimigo = null; double bd = 18; foreach (var e in g.actors) { if (e.team == m.team || e.st != "alive") continue; double d = JS.Hypot(e.x - m.x, e.z - m.z); if (d < bd && Mapa.LosClear(m, e)) { bd = d; inimigo = e; } }
        if (inimigo != null) { yaw = JS.Atan2(-(inimigo.x - m.x), -(inimigo.z - m.z)); fire = r() < .7; }
        double mz = q != null ? -1 : 0; double mx = r() < .1 ? (r() < .5 ? -1 : 1) : 0; bool sp = r() < .3, use = r() >= .1, use2 = r() < .5; double pitch = (r() - .5) * .2;
        if (r() < 1.0 / 90) D.c.q++; if (r() < 1.0 / 90) D.c.f++; if (r() < 1.0 / 90) D.c.g++; if (r() < 1.0 / 90) D.c.r++; if (r() < 1.0 / 90) D.c.ab++;
        if (g.t > D.compraT)
        {
            D.compraT = g.t + 10; Npc n = null; double nd = 1e9; foreach (var x in Mapa.NPCS) { double d = JS.Hypot(x.x - m.x, x.z - m.z); if (d < nd) { nd = d; n = x; } }
            Sim.Act(g, "eu", new Acao { type = "comprar", npc = n.id, item = n.vende[(int)Math.Floor(r() * n.vende.Count)] });
        }
        Sim.MoveHuman(g, m, new Entrada { mx = mx, mz = mz, sp = sp, yaw = yaw, pitch = pitch, fire = fire, use = use, use2 = use2, c = D.c.Copia(), viewT = g.t - .05 }, dt); Sim.Step(g, dt);
    }
    static int Main(string[] args)
    {
        Sim.Iniciar(File.ReadAllText(args[0]));
        uint semente = uint.Parse(args[1]); int passos = int.Parse(args[2]); string saida = args[3]; int partidas = args.Length > 4 ? int.Parse(args[4]) : 1; string modo = args.Length > 5 ? args[5] : "bots";
        Console.Error.WriteLine("nav C#: " + Mapa.WP.Count + " nós, " + Mapa.Arestas() + " arestas, principal " + Mapa.principal + " (JS: " + Mapa.navNos + ", " + Mapa.navArestas + ", " + Mapa.navPrincipalJS + ")");
        JS.Random = JS.Mulberry32(semente);
        using (var w = new StreamWriter(saida))
        {
            for (int m = 0; m < partidas; m++)
            {
                var slots = new List<Slot>(); for (int j = 0; j < 4; j++) slots.Add(new Slot(j < 2 ? "A" : "B", (modo == "H" && j == 0) || (modo == "C" && j == 2) ? "eu" : null, "b" + j));
                var g = Sim.CreateGame(new OpcoesJogo { timers = true, slots = slots, matchId = "cmp" + m });
                int nLog = 0; var D = new Dir(); var r = JS.Mulberry32((semente ^ 0x5bd1e995u) + (uint)m);
                for (int n = 0; n < passos && g.phase != "final"; n++)
                {
                    if (modo == "bots") Sim.Step(g, 1.0 / 30); else Roteiro(g, D, r);
                    w.Write(Linha(g, nLog)); w.Write('\n'); nLog = g.log.Count; g.events.Clear();
                }
                var v = Sim.Winner(g); w.Write("FIM " + (v != null ? v.win + " " + v.why : "-") + "\n");
            }
        }
        return 0;
    }
}
