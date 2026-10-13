// Regras do jogo: fases, noite, combate, habilidades, altares, rituais, selamento e passo principal.
// Porte fiel de shared/sim.js (as seções têm os mesmos nomes). Os bots ficam em Bots.cs.
// Uso: Sim.Iniciar(json); var g = Sim.CreateGame(...); a cada 1/30 s: Sim.MoveHuman(g, eu, entrada, dt); Sim.Step(g, dt).
using System;
using System.Collections.Generic;
using System.Linq;

namespace RitualReversal.Simulacao
{
    public static partial class Sim
    {
        public static int PID = 1; // ids de projéteis, coletáveis e sal (global, como no JS)
        static Jogo G_ATUAL;

        public static void Iniciar(string jsonSimulacao) { Mapa.Carregar(jsonSimulacao); }

        // ---------- utilidades ----------
        public static double Dist(Ponto a, Ponto b) { return JS.Hypot(a.x - b.x, a.z - b.z); }
        static double R2(double v) { return JS.R2(v); }
        static double Rand(double a, double b) { return JS.Rand(a, b); }
        static double Rnd() { return JS.Random(); }
        static int Idx(int n) { return (int)Math.Floor(Rnd() * n); }
        public static Ponto Pt(double x, double z) { return new Ponto(x, z); }

        public static void Ev(Jogo g, string type, params object[] kv)
        {
            var e = new Evento { type = type }; for (int i = 0; i + 1 < kv.Length; i += 2) e.d[(string)kv[i]] = kv[i + 1]; g.events.Add(e);
        }
        public static void Feed(Jogo g, string msg, string cls = "", string team = null) { Ev(g, "feed", "msg", msg, "cls", cls ?? "", "team", team); }
        public static void LogE(Jogo g, string e, params object[] kv)
        {
            var r = new Registro { t = R2(g.rt), r = g.round, ev = e }; for (int i = 0; i + 1 < kv.Length; i += 2) r.d[(string)kv[i]] = kv[i + 1]; g.log.Add(r);
        }

        public static string RoleOf(Jogo g, string team) { return ((g.round == 1) == (team == "A")) ? "H" : "C"; }
        static List<Slot> HumansInRole(Jogo g, string role) { return g.slots.Where(s => s.cid != null && RoleOf(g, s.team) == role).ToList(); }
        static List<Slot> Humans(Jogo g) { return g.slots.Where(s => s.cid != null).ToList(); }
        public static List<Slot> DefaultSlots() { return new List<Slot> { new Slot("A", "local", "Você"), new Slot("A", null, "Irmã Beatriz"), new Slot("B", null, "Irmão Cinza"), new Slot("B", null, "Madre Vesper") }; }
        static List<int> Embaralhar6() { var l = new List<int> { 0, 1, 2, 3, 4, 5 }; JS.Sort(l, (x, y) => Rnd() - .5); return l; }

        // ============ JOGO ============
        public static Jogo CreateGame(OpcoesJogo opts = null)
        {
            if (opts == null) opts = new OpcoesJogo();
            var g = new Jogo { opts = opts, timers = opts.timers, matchId = opts.matchId ?? DateTime.Now.Ticks.ToString() };
            foreach (var s in (opts.slots ?? DefaultSlots())) g.slots.Add(new Slot(s.team, s.cid, s.name));
            for (int i = 0; i < Mapa.REAG.Count; i++) g.reagents.Add(new Reagente { i = i, x = Mapa.REAG[i].x, z = Mapa.REAG[i].z, has = true, t = 0 });
            for (int i = 0; i < 6; i++) g.flares.Add(new Sinalizador());
            EnterPick(g, 1);
            return g;
        }

        static void ResetRoundState(Jogo g)
        {
            g.altars = new List<Altar>();
            for (int i = 0; i < Mapa.ALTARS.Count; i++) { var d = Mapa.ALTARS[i]; g.altars.Add(new Altar { i = i, name = d.name, x = d.x, z = d.z }); }
            foreach (var R in g.reagents) { R.has = true; R.t = 0; }
            g.know = g.altars.Select(a => "?").ToArray(); g.visit = g.altars.Select(a => -99.0).ToArray(); g.decoys = 2; g.transferUsed = false; g.rastros = new List<Rastro>(); g.pegadas = new List<Pegada>();
            g.proj = new List<Projetil>(); g.pickups = new List<Coletavel>(); foreach (var f in g.flares) f.t = 0; g.actors = new List<Ator>(); g.picks = new List<int>(); g.ready = new Dictionary<string, bool>(); g.buff = new Buff();
            g.endAt = 0; g.lastResolveT = 0; g.rt = 0; g.firstContact = null; g.momento = 0; PrepararNoite(g);
        }
        static void EnterPick(Jogo g, int round)
        {
            g.round = round; ResetRoundState(g); g.phase = "pick"; g.phaseT = CFG.timerPick;
            if (HumansInRole(g, "C").Count == 0) { g.picks = Embaralhar6().Take(3).ToList(); ConfirmPicks(g); }
        }
        static void ConfirmPicks(Jogo g)
        {
            foreach (var A in g.altars) A.chosen = false; foreach (int i in g.picks) g.altars[i].chosen = true;
            LogE(g, "choose", "altars", g.picks.Select(i => (object)g.altars[i].name).ToList());
            g.phase = "intro"; g.phaseT = CFG.timerIntro; g.ready = new Dictionary<string, bool>();
            foreach (var s in Humans(g))
            {
                string role = RoleOf(g, s.team); string c;
                if (!g.cls.TryGetValue(s.cid, out c) || c == null || Regras.CLASSES[c].team != role) g.cls[s.cid] = Regras.CLASS_BY_TEAM[role][0];
            }
        }
        static bool AllReady(Jogo g) { var hs = Humans(g); bool r; return hs.Count > 0 && hs.All(s => g.ready.TryGetValue(s.cid, out r) && r); }

        static void StartPlay(Jogo g)
        {
            g.actors = new List<Ator>();
            var taken = new Dictionary<string, List<string>> { { "H", new List<string>() }, { "C", new List<string>() } };
            for (int si = 0; si < g.slots.Count; si++)
            {
                var s = g.slots[si]; if (s.cid == null) continue; string role = RoleOf(g, s.team); string c = g.cls[s.cid]; taken[role].Add(c);
                string fe; string f = g.feit.TryGetValue(s.cid, out fe) && fe != null && Regras.FEITICOS.ContainsKey(fe) && Regras.FEITICOS[fe].team == role ? fe : Regras.FEIT_BY_TEAM[role][0];
                g.actors.Add(MakeActor(g, s, si, role, c, f));
            }
            for (int si = 0; si < g.slots.Count; si++)
            {
                var s = g.slots[si]; if (s.cid != null) continue; string role = RoleOf(g, s.team);
                var opts = Regras.CLASS_BY_TEAM[role]; var nt = opts.Where(o => !taken[role].Contains(o)).ToList();
                string c = nt.Count > 0 ? nt[Idx(nt.Count)] : opts[si % opts.Length]; taken[role].Add(c);
                var usados = g.actors.Where(x => x.team == role).Select(x => x.feitico).ToList(); var livres = Regras.FEIT_BY_TEAM[role].Where(f => !usados.Contains(f)).ToList();
                int k = Idx(livres.Count); string fe = k < livres.Count ? livres[k] : Regras.FEIT_BY_TEAM[role][0];
                g.actors.Add(MakeActor(g, s, si, role, c, fe));
            }
            g.momento = -1; PrepararNoite(g);
            for (int k = 0; k < g.actors.Count; k++) Spawn(g, g.actors[k], true);
            g.phase = "play"; g.rt = 0; g.lastResolveT = 0;
            LogE(g, "round_start", "teams", g.actors.Select(a => (object)new Dictionary<string, object> { { "name", a.name }, { "team", a.team }, { "cls", a.cls }, { "human", a.human } }).ToList());
            Feed(g, "O Crepúsculo começou. Prepare-se: os altares despertam na Vigília.", "big");
        }
        static Ator MakeActor(Jogo g, Slot slot, int si, string role, string cls, string feit)
        {
            var C = Regras.CLASSES[cls];
            return new Ator { id = g.nid++, slot = si, cid = slot.cid, human = slot.cid != null, name = slot.name, team = role, cls = cls, maxHp = C.hp, hp = C.hp, speed = C.speed, feitico = feit, obolos = CFG.obolosInicio };
        }
        static void Spawn(Jogo g, Ator a, bool first)
        {
            var s = Mapa.SPAWN[a.team]; var sameTeam = g.actors.Where(b => b.team == a.team).ToList(); int idx = first ? sameTeam.IndexOf(a) : Idx(2);
            a.x = s.x + Rand(-.6, .6); a.z = s.z + (idx == 0 ? -2.5 : 2.5); a.yaw = s.yaw; a.pitch = 0; a.inp.yaw = a.yaw; a.inp.pitch = 0;
            a.hp = a.maxHp; a.st = "alive"; a.invuln = 2; a.sealing = -1; a.hold = new Hold(); a.ammo = 8; a.reserve = a.cls == "soldado" ? 64 : 48; a.fervor = 100; a.reloadT = 0;
            a.blindT = 0; a.stunT = 0; a.runeT = 0; a.shield = 0; a.hist = new List<double[]>(); a._pegada = null; a.ai.path = new List<Ponto>(); a.ai.goal = null; a.ai.goalKey = ""; a.ai.alert = null;
            if (first) { a.sanity = 100; a.flares = 2; a.carga = 100; a.reag = 0; a.deaths = 0; a.dc = 0; a.revUsed = false; a.abilCd = 0; }
        }
        static void TimeoutPhase(Jogo g)
        {
            if (g.phase == "pick") { while (g.picks.Count < 3) { int i = Idx(6); if (!g.picks.Contains(i)) g.picks.Add(i); } ConfirmPicks(g); }
            else if (g.phase == "intro") StartPlay(g);
            else if (g.phase == "summary") AfterSummary(g);
        }
        static int Noites(Jogo g) { return g.opts != null && g.opts.noites > 0 ? g.opts.noites : CFG.noites; }
        static void AfterSummary(Jogo g) { if (g.round == 1 && Noites(g) > 1) EnterPick(g, 2); else { g.phase = "final"; g.phaseT = 0; } }

        public static void Act(Jogo g, string cid, Acao a)
        {
            var slot = g.slots.FirstOrDefault(s => s.cid == cid); if (slot == null || a == null) return;
            string role = RoleOf(g, slot.team);
            if (a.type == "pick" && g.phase == "pick" && role == "C") { int i = a.i; if (i < 0 || i > 5) return; if (g.picks.Contains(i)) g.picks.Remove(i); else if (g.picks.Count < 3) g.picks.Add(i); }
            if (a.type == "pickRandom" && g.phase == "pick" && role == "C") { g.picks = Embaralhar6().Take(3).ToList(); }
            if (a.type == "pickConfirm" && g.phase == "pick" && role == "C" && g.picks.Count == 3) ConfirmPicks(g);
            if (a.type == "cls" && g.phase == "intro" && a.cls != null && Regras.CLASSES.ContainsKey(a.cls) && Regras.CLASSES[a.cls].team == role) g.cls[cid] = a.cls;
            if (a.type == "feit" && g.phase == "intro" && a.f != null && Regras.FEITICOS.ContainsKey(a.f) && Regras.FEITICOS[a.f].team == role) g.feit[cid] = a.f;
            if (a.type == "comprar" && g.phase == "play") Comprar(g, cid, a.npc, a.item);
            if (a.type == "ready" && (g.phase == "intro" || g.phase == "summary")) { g.ready[cid] = true; if (AllReady(g)) { if (g.phase == "intro") StartPlay(g); else AfterSummary(g); } }
            if (a.type == "restart" && g.phase == "final") { g.scores = new Dictionary<string, Placar> { { "A", null }, { "B", null } }; g.stats = new Dictionary<int, Estatisticas>(); EnterPick(g, 1); }
        }
        public static void DropHuman(Jogo g, string cid)
        {
            var s = g.slots.FirstOrDefault(x => x.cid == cid); if (s == null) return; s.cid = null;
            var a = g.actors.FirstOrDefault(x => x.cid == cid); if (a != null) { a.cid = null; a.human = false; a.inp.fire = false; a.inp.use = false; }
            g.ready.Remove(cid);
            if ((g.phase == "intro" || g.phase == "summary") && AllReady(g)) { if (g.phase == "intro") StartPlay(g); else AfterSummary(g); }
        }

        // ============ A NOITE ============
        public static int MomentoDe(double rt) { for (int i = 0; i < CFG.momentoAte.Length; i++) if (rt < CFG.momentoAte[i]) return i; return CFG.momentoAte.Length - 1; }
        public static int Fendas(Jogo g) { return g.altars.Count(A => A.state == "fenda"); }
        public static int Farois(Jogo g) { return g.altars.Count(A => A.state == "farol"); }
        // três rituais por noite, no máximo, e um de cada vez; devolve o motivo do bloqueio ou null
        public static string BloqueioRitual(Jogo g)
        {
            if (g.altars.Any(A => A.state == "active")) return "Já há um ritual em andamento";
            int r = Fendas(g) + Farois(g);
            if (r >= 3) return "Os três rituais desta noite já aconteceram";
            if (g.momento < 1) return "Este altar só desperta na Vigília";
            if (r >= 2 && g.momento < 2) return "O último ritual só pode começar na Hora Morta";
            return null;
        }
        static void PrepararNoite(Jogo g)
        {
            var pistas = new List<Ponto>(Mapa.PONTOS_DEF["pista"]); JS.Sort(pistas, (x, y) => Rnd() - .5);
            var P = new List<PontoNoite>();
            foreach (var p in pistas.Take(3)) P.Add(new PontoNoite { tipo = "pista", x = p.x, z = p.z });
            foreach (var p in Mapa.PONTOS_DEF["sentinela"]) P.Add(new PontoNoite { tipo = "sentinela", x = p.x, z = p.z });
            foreach (var p in Mapa.PONTOS_DEF["erva"]) P.Add(new PontoNoite { tipo = "erva", x = p.x, z = p.z });
            foreach (var p in Mapa.PONTOS_DEF["tumulo"]) P.Add(new PontoNoite { tipo = "tumulo", x = p.x, z = p.z });
            for (int i = 0; i < P.Count; i++) P[i].i = i;
            g.pontos = P; g.sal = new List<Sal>(); g.realocar = new List<double>();
            g.tarefas = new Dictionary<string, List<Tarefa>> { { "H", new List<Tarefa>() }, { "C", new List<Tarefa>() } }; g.rastros = new List<Rastro>(); g.pegadas = new List<Pegada>();
        }
        static void NovoMomento(Jogo g, int mi)
        {
            g.momento = mi; var T = Regras.TAREFAS[mi];
            Func<TarefaDef, Tarefa> nova = t => new Tarefa { id = t.id, nome = t.nome, meta = t.meta, rec = t.rec };
            g.tarefas = new Dictionary<string, List<Tarefa>> { { "H", T["H"].Select(nova).ToList() }, { "C", T["C"].Select(nova).ToList() } };
            if (mi > 0) { Ev(g, "momento", "i", mi, "nome", CFG.momentoNome[mi]); Feed(g, mi == 1 ? "A Vigília começou: os altares despertam." : "A Hora Morta chegou: rituais mais rápidos, a névoa se fecha.", "big"); }
            LogE(g, "momento", "i", mi, "nome", CFG.momentoNome[mi]);
        }
        static void TarefaProg(Jogo g, string role, string id, int n = 1)
        {
            List<Tarefa> L; if (g.tarefas == null || !g.tarefas.TryGetValue(role, out L)) return; var t = L.FirstOrDefault(x => x.id == id); if (t == null || t.feita || t.meta == 0) return;
            t.prog = Math.Min(t.meta, t.prog + n);
            if (t.prog >= t.meta)
            {
                t.feita = true; foreach (var a in g.actors.Where(a => a.team == role)) a.obolos += t.rec;
                Feed(g, "Tarefa cumprida: " + t.nome + ". +" + t.rec + " óbolos para cada um.", role == "H" ? "h" : "c", role); Ev(g, "tarefa", "team", role, "id", id); LogE(g, "tarefa", "team", role, "id", id);
            }
        }
        static void Comprar(Jogo g, string cid, string npcId, string itemId)
        {
            var a = g.actors.FirstOrDefault(x => x.cid == cid); var n = Mapa.NPCS.FirstOrDefault(x => x.id == npcId); ItemDef it = null; if (itemId != null) Regras.ITENS.TryGetValue(itemId, out it);
            Action<string> nega = msg => { if (a != null) Ev(g, "feed", "msg", msg, "cls", "", "to", a.id); };
            if (a == null || n == null || it == null || a.st != "alive") return; if (Dist(a, n) > 3.2) { nega("Chegue mais perto para negociar."); return; }
            if (!n.vende.Contains(itemId) || (n.time != null && n.time != a.team) || (it.time != null && it.time != a.team)) { nega("Este item não é para você."); return; }
            if (it.momento > 0 && g.momento < it.momento) { nega(it.nome + " só aparece a partir da Vigília."); return; }
            if (a.obolos < it.preco) { nega("Faltam " + (it.preco - a.obolos) + " óbolos."); return; }
            if (itemId == "reagente" && a.reag >= CFG.carry) { nega("Você já carrega 2 reagentes."); return; }
            if (itemId == "amuleto" && a.amuleto) { nega("Só um amuleto por noite."); return; }
            if (itemId == "cinza" && g.decoys >= 4) { nega("A equipe já tem Chamarizes demais."); return; }
            a.obolos -= it.preco;
            if (itemId == "reagente") a.reag++; else if (itemId == "cinza") g.decoys++; else if (itemId == "municao") a.reserve = Math.Min(80, a.reserve + 16);
            else if (itemId == "flare") a.flares = Math.Min(4, a.flares + 1); else if (itemId == "oleo") a.carga = 100; else if (itemId == "pocao") a.curaT = 4;
            else if (itemId == "amuleto") { a.amuleto = true; a.maxHp += 25; a.hp += 25; }
            Ev(g, "feed", "msg", "Comprou " + it.nome.ToLowerInvariant() + " com " + n.nome + ".", "cls", a.team == "H" ? "h" : "c", "to", a.id); Ev(g, "sfx", "k", "collect", "x", R2(a.x), "z", R2(a.z), "a", a.id);
            LogE(g, "compra", "who", a.name, "item", itemId, "npc", npcId);
        }
        static void PassoNoite(Jogo g, double dt)
        {
            int mi = MomentoDe(g.rt); if (mi != g.momento) NovoMomento(g, mi);
            foreach (var p in g.pontos) if (p.tipo == "erva" && !p.ativo) { p.t -= dt; if (p.t <= 0) p.ativo = true; }
            for (int i = g.realocar.Count - 1; i >= 0; i--) if (g.rt >= g.realocar[i])
                {
                    g.realocar.RemoveAt(i);
                    var livres = g.altars.Where(A => !A.chosen && A.state == "dormant").ToList();
                    if (livres.Count > 0) { var N = livres[Idx(livres.Count)]; N.chosen = true; Feed(g, "Um novo altar foi escolhido: " + N.name + ".", "c", "C"); LogE(g, "realocado", "altar", N.name); }
                }
            for (int i = g.sal.Count - 1; i >= 0; i--)
            {
                var q = g.sal[i]; q.t -= dt; bool gasto = q.t <= 0;
                foreach (var e in g.actors)
                {
                    if (gasto || e.team != "C" || e.st != "alive" || JS.Hypot(e.x - q.x, e.z - q.z) > 2.2) continue;
                    e.slowT = 3; e.revelT = 6; e.veuT = 0; gasto = true; Feed(g, "O sal denunciou " + e.name + ".", "h", "H"); Ev(g, "feed", "msg", "Você pisou no sal: está lento e visível.", "cls", "c", "to", e.id); LogE(g, "sal", "who", e.name);
                }
                if (gasto) g.sal.RemoveAt(i);
            }
            foreach (var a in g.actors)
            {
                if (a.veuT > 0) a.veuT = Math.Max(0, a.veuT - dt); if (a.slowT > 0) a.slowT = Math.Max(0, a.slowT - dt); if (a.revelT > 0) a.revelT = Math.Max(0, a.revelT - dt);
                if (a.curaT > 0 && a.st == "alive") { a.curaT = Math.Max(0, a.curaT - dt); a.hp = Math.Min(a.maxHp, a.hp + 15 * dt); }
            }
        }
        // pegadas dos Cultistas (só o Rastreador vê) e marcas de arrasto da Transferência
        static void Rastrear(Jogo g)
        {
            bool temRastreador = g.actors.Any(h => h.team == "H" && h.cls == "rastreador");
            if (temRastreador) foreach (var c in g.actors)
                {
                    if (c.team != "C" || c.st != "alive") continue;
                    var u = c._pegada; double d = u != null ? JS.Hypot(c.x - u.x, c.z - u.z) : 0;
                    if (u == null || d > 1.3)
                    {
                        int lado = u != null ? -u.l : 1; c._pegada = new Pegada { x = c.x, z = c.z, l = lado };
                        if (u != null && d < 4) { double ux = (c.x - u.x) / d, uz = (c.z - u.z) / d; g.pegadas.Add(new Pegada { x = R2(c.x + uz * .18 * lado), z = R2(c.z - ux * .18 * lado), yaw = R2(JS.Atan2(ux, uz)), t = R2(g.t) }); }
                    }
                }
            while (g.pegadas.Count > 0 && (g.t - g.pegadas[0].t > CFG.pegadaVida || g.pegadas.Count > 160)) g.pegadas.RemoveAt(0);
            foreach (var R in g.rastros)
            {
                if (g.rt - R.t > 180) continue;
                foreach (var h in g.actors)
                {
                    if (h.team != "H" || h.st != "alive" || JS.Hypot(h.x - R.x, h.z - R.z) > 9) continue; var P = g.altars[R.para]; var O = g.altars[R.de];
                    if (!R.visto) { R.visto = true; Feed(g, "Marcas de arrasto em " + O.name + ": o culto levou o ritual daqui para outro altar.", "big h", "H"); LogE(g, "rastro_visto", "altar", O.name, "by", h.name); }
                    if (!R.lido && h.cls == "rastreador")
                    {
                        R.lido = true; if (g.know[P.i] == "?" || g.know[P.i] == "limpo") g.know[P.i] = "desperto";
                        Feed(g, h.name + " segue o arrasto: ele termina em " + P.name + ".", "big h", "H"); LogE(g, "rastro_lido", "para", P.name, "by", h.name);
                    }
                }
            }
        }
        static void SentinelaAvisa(Jogo g, Altar A, string oque)
        {
            var s = g.pontos.FirstOrDefault(p => p.tipo == "sentinela" && p.acesa && JS.Hypot(p.x - A.x, p.z - A.z) < 24); if (s == null) return;
            if (g.know[A.i] == "?" || g.know[A.i] == "limpo") g.know[A.i] = "desperto"; Feed(g, "Uma sentinela tocou: " + oque + " em " + A.name + ".", "big h", "H"); LogE(g, "sentinela", "altar", A.name);
        }

        // ============ LUZ E SANIDADE ============
        public static double LightAt(Jogo g, double x, double z)
        {
            double L = 0;
            foreach (var c in Mapa.CANDLES) { double d = JS.Hypot(x - c.x, z - c.z); if (d < 8.5) L += 1 - d / 8.5; }
            if (Mapa.NoClaustro(x, z)) L += 0.55; // luar: o Claustro não tem teto
            else if (!Mapa.NaCatedral(x, z))
            {
                double lua = 0; foreach (var c in Mapa.CLAREIRAS) { double d = JS.Hypot(x - c.x, z - c.z); if (d < c.r + 2) lua = Math.Max(lua, .55 * JS.Clamp((c.r + 2 - d) / 4, 0, 1)); }
                if (lua < .42) foreach (var sg in Mapa.SEG_TRILHA) { if (sg.estreita) continue; double d = Mapa.DistSeg(x, z, sg); if (d < sg.w / 2 + 1) { lua = Math.Max(lua, .42 * JS.Clamp((sg.w / 2 + 1 - d) / 1.5, 0, 1)); if (lua >= .42) break; } }
                L += lua;
            }
            foreach (var f in g.flares) if (f.t > 0) { double d = JS.Hypot(x - f.x, z - f.z); if (d < 12) L += 1.4 * (1 - d / 12); }
            foreach (var A in g.altars)
            {
                if (A.state == "farol") { double d = JS.Hypot(x - A.x, z - A.z); if (d < 12) L += 1.3 * (1 - d / 12); }
                if (A.state == "fenda") { double d = JS.Hypot(x - A.x, z - A.z); if (d < 9) L -= 0.8 * (1 - d / 9); }
            }
            return L;
        }
        static void UpdateSanity(Jogo g, Ator a, double dt)
        {
            double L = LightAt(g, a.x, a.z) + (a.lantern ? .3 : 0); double d = 0;
            bool nearRit = g.altars.Any(A => (A.state == "active" || (A.state == "awake" && !A.decoy)) && Dist(a, A) < 15);
            bool ally = g.actors.Any(b => b != a && b.team == a.team && b.st == "alive" && Dist(a, b) < 9);
            bool alone = !g.actors.Any(b => b != a && b.team == a.team && b.st == "alive" && Dist(a, b) < 16);
            if (a.team == "H") { d += L >= .35 ? 1.4 : -.7; if (nearRit) d -= 1.5; if (ally) d += 1; if (alone) d -= .25; }
            else { d += L < .35 ? 1 : -.35; if (nearRit) d += 2; if (alone) d -= .3; foreach (var f in g.flares) if (f.t > 0 && JS.Hypot(a.x - f.x, a.z - f.z) < 10) d -= 3; }
            foreach (var A in g.altars) { double dd = Dist(a, A); if (A.state == "fenda" && dd < 9) d += a.team == "H" ? -2 : 3; if (A.state == "farol" && dd < 12) d += a.team == "H" ? 3 : -2; }
            a.sanity = JS.Clamp(a.sanity + d * dt, 0, 100);
            if (a.sanity < 15) a.panicT = g.t;
        }

        // ============ MOVIMENTO (também serve de predição no cliente) ============
        public static void MoveHuman(Jogo g, Ator a, Entrada inp, double dt)
        {
            if (inp != null)
            {
                a.inp.mx = JS.Clamp(double.IsNaN(inp.mx) ? 0 : inp.mx, -1, 1); a.inp.mz = JS.Clamp(double.IsNaN(inp.mz) ? 0 : inp.mz, -1, 1); a.inp.sp = inp.sp;
                if (inp.temYaw && !double.IsNaN(inp.yaw) && !double.IsInfinity(inp.yaw)) a.inp.yaw = inp.yaw;
                if (inp.temPitch && !double.IsNaN(inp.pitch) && !double.IsInfinity(inp.pitch)) a.inp.pitch = JS.Clamp(inp.pitch, -1.5, 1.5);
                a.inp.fire = inp.fire; a.inp.use = inp.use; a.inp.use2 = inp.use2; if (inp.temViewT && !double.IsNaN(inp.viewT) && !double.IsInfinity(inp.viewT)) a.inp.viewT = inp.viewT;
                if (inp.c != null) a.inp.c = inp.c.Copia();
            }
            a.yaw = a.inp.yaw; a.pitch = a.inp.pitch;
            double mx = a.inp.mx, mz = a.inp.mz, len = JS.Hypot(mx, mz);
            a.moving = false;
            if (a.st == "dead" || a.stunT > 0 || len < 0.01) return;
            mx /= len; mz /= len;
            double sp = a.speed * (a.slowT > 0 ? .6 : 1);
            if (a.st == "down") sp = .8;
            else { if (a.inp.sp && mz < 0 && a.sealing < 0) sp = CFG.sprint * (a.speed / CFG.speed); if (a.sealing >= 0) sp *= .4; if (a.hp < a.maxHp * .5) sp *= .9; }
            double s = JS.Sin(a.yaw), c = JS.Cos(a.yaw);
            MoverSeguro(a, a.x + (mx * c + mz * s) * sp * dt, a.z + (-mx * s + mz * c) * sp * dt, .4); a.moving = true;
        }

        // ============ COMBATE ============
        static List<Ator> EnemiesOf(Jogo g, Ator a) { return g.actors.Where(b => b.team != a.team).ToList(); }
        static int RecentAttackers(Jogo g, Ator a) { int n = 0; foreach (var kv in a.att) if (g.t - kv.Value < 1) n++; return n; }
        static Ponto PosAt(Ator a, double T)
        {
            var h = a.hist; if (h.Count == 0 || T >= h[h.Count - 1][0]) return Pt(a.x, a.z);
            for (int i = h.Count - 1; i > 0; i--) if (h[i - 1][0] <= T) { double k = (T - h[i - 1][0]) / Math.Max(1e-6, h[i][0] - h[i - 1][0]); return Pt(JS.Lerp(h[i - 1][1], h[i][1], k), JS.Lerp(h[i - 1][2], h[i][2], k)); }
            return Pt(h[0][1], h[0][2]);
        }
        static void TraceShot(Jogo g, Ator a, double ox, double oy, double oz, double dx, double dy, double dz, double T, bool forceMiss)
        {
            a.veuT = 0; a.lastShotT = g.t; a.ammo--; if (a.ammo <= 0) Reload(g, a);
            Ev(g, "sfx", "k", "shot", "x", R2(a.x), "z", R2(a.z), "a", a.id);
            double tWall = Mapa.RaioParede(ox, oz, dx, dz, 90, 0, d => oy + dy * d);
            if (dy < 0) { double tf = -oy / dy; if (tf < tWall) tWall = tf; }
            Ator best = null; double bt = tWall; bool head = false;
            if (!forceMiss) foreach (var e in EnemiesOf(g, a))
                {
                    if (e.st == "dead") continue; var p = PosAt(e, T);
                    double fx = ox - p.x, fz = oz - p.z, A2 = dx * dx + dz * dz, B = 2 * (fx * dx + fz * dz), C = fx * fx + fz * fz - .45 * .45, disc = B * B - 4 * A2 * C; if (disc < 0 || A2 < 1e-9) continue;
                    double t = (-B - Math.Sqrt(disc)) / (2 * A2); if (t < 0 || t > bt) continue; double y = oy + dy * t, top = e.st == "down" ? .6 : 1.95; if (y < 0 || y > top) continue; best = e; bt = t; head = e.st == "alive" && y > 1.5;
                }
            Ev(g, "tracer", "a", a.id, "x0", R2(ox), "y0", R2(oy - .2), "z0", R2(oz), "x1", R2(ox + dx * bt), "y1", R2(oy + dy * bt), "z1", R2(oz + dz * bt), "alvo", best != null ? (object)best.id : null, "cab", head);
            if (best != null)
            {
                double d = bt, k = JS.Clamp(1 - (d - CFG.quedaPerto) / (CFG.quedaLonge - CFG.quedaPerto) * (1 - CFG.quedaMin), CFG.quedaMin, 1);
                Damage(g, best, JS.Round((head ? 26 : CFG.quedaDano) * k), a);
            }
        }
        static void FireHuman(Jogo g, Ator a)
        {
            a.fireCd = .42; double y = a.inp.yaw, p = a.inp.pitch; double dx = -JS.Sin(y) * JS.Cos(p), dy = JS.Sin(p), dz = -JS.Cos(y) * JS.Cos(p);
            double T = JS.Clamp(a.inp.viewT != 0 ? a.inp.viewT : g.t, g.t - CFG.compensacaoMax, g.t);
            TraceShot(g, a, a.x, 1.62, a.z, dx, dy, dz, T, false);
        }
        static void FireBotAt(Jogo g, Ator a, double tx, double ty, double tz, bool miss)
        {
            a.fireCd = .55 + Rand(0, .2); double dx = tx - a.x, dy = ty - 1.62, dz = tz - a.z; double l = JS.Hypot(dx, dy, dz); if (l == 0 || double.IsNaN(l)) l = 1; dx /= l; dy /= l; dz /= l;
            TraceShot(g, a, a.x, 1.62, a.z, dx, dy, dz, g.t, miss);
        }
        static void CastSigil(Jogo g, Ator a, double dx, double dy, double dz)
        {
            a.veuT = 0; a.lastShotT = g.t; a.fireCd = a.human ? .42 : 1 + Rand(0, .3); a.fervor -= CFG.sigilCusto;
            Ev(g, "sfx", "k", "sigil", "x", R2(a.x), "z", R2(a.z), "a", a.id);
            double rx = JS.Cos(a.yaw), rz = -JS.Sin(a.yaw); double ox = a.x + rx * .3, oy = 1.45, oz = a.z + rz * .3;
            double tx = a.x + dx * 40, ty = 1.62 + dy * 40, tz = a.z + dz * 40; double vx = tx - ox, vy = ty - oy, vz = tz - oz; double l = JS.Hypot(vx, vy, vz); if (l == 0 || double.IsNaN(l)) l = 1; double sp = CFG.sigilVel;
            g.proj.Add(new Projetil { id = PID++, x = ox, y = oy, z = oz, vx = vx / l * sp, vy = vy / l * sp, vz = vz / l * sp, owner = a.id, team = a.team, life = 2.5, dmg = CFG.sigilDano });
        }
        static void Reload(Jogo g, Ator a) { if (a.team != "H" || a.reloadT > 0 || a.ammo >= 8 || a.reserve <= 0) return; a.reloadT = 1.6; Ev(g, "reload", "a", a.id, "x", R2(a.x), "z", R2(a.z)); }
        static void Damage(Jogo g, Ator t, double amt, Ator src)
        {
            if (t.st == "dead" || t.invuln > 0) return;
            if (!g.firstContact.HasValue || g.firstContact.Value == 0) { g.firstContact = R2(g.rt); LogE(g, "first_contact"); }
            if (t.st == "down") { t.downT -= amt / 8; t.att[src.id] = g.t; return; }
            if (t.shield > 0) { double s = Math.Min(t.shield, amt); t.shield -= s; amt -= s; }
            t.hp -= amt; t.hitT = g.t; t.att[src.id] = g.t; t.veuT = 0;
            Ev(g, "hurt", "a", t.id, "s", src.id, "sx", R2(src.x), "sz", R2(src.z)); Ev(g, "hit", "a", src.id, "alvo", t.id, "derrubou", t.hp <= 0);
            if (!t.human) t.ai.alert = new Alerta { x = src.x, z = src.z, t = g.t };
            if (t.hp <= 0) GoDown(g, t, src);
        }
        static void GoDown(Jogo g, Ator t, Ator src)
        {
            if (t.revUsed) { Kill(g, t, src); return; }
            if (src != null && src.team != t.team) src.obolos += 1;
            t.st = "down"; t.hp = 0; t.downT = CFG.downTime; t.sealing = -1; t.hold = new Hold(); t.lantern = false;
            Feed(g, src.name + " derrubou " + t.name, t.team == "C" ? "h" : "c"); LogE(g, "down", "who", t.name, "team", t.team, "by", src.name, "x", R2(t.x), "z", R2(t.z));
        }
        static void Kill(Jogo g, Ator t, Ator src, string how = null)
        {
            t.st = "dead"; t.hp = 0; t.deaths++; t.dc++; t.sealing = -1; t.hold = new Hold(); t.lantern = false;
            t.respawnT = CFG.respawnMomento[Math.Max(0, g.momento)] + Math.Min(6, 2 * (t.dc - 1));
            for (int k = 0; k < t.reag; k++) DropPickup(g, "reag", t.x + Rand(-.6, .6), t.z + Rand(-.6, .6)); t.reag = 0;
            DropPickup(g, t.team == "C" ? "ess" : "selo", t.x + Rand(-.4, .4), t.z + Rand(-.4, .4));
            string msg = how == "exec" ? src.name + " executou " + t.name : src != null ? t.name + " morreu" : t.name + " sangrou até morrer";
            Feed(g, msg, t.team == "C" ? "h" : "c"); LogE(g, "death", "who", t.name, "team", t.team, "how", how ?? "bleed", "x", R2(t.x), "z", R2(t.z));
        }
        static void DropPickup(Jogo g, string kind, double x, double z) { g.pickups.Add(new Coletavel { id = PID++, kind = kind, x = R2(x), z = R2(z), life = kind == "reag" ? 30 : 40 }); }

        // ============ HABILIDADES ============
        static bool UseAbility(Jogo g, Ator a)
        {
            if (a.abilCd > 0 || a.st != "alive" || a.stunT > 0) return false;
            double fx = -JS.Sin(a.yaw), fz = -JS.Cos(a.yaw); var alvos = new List<object>();
            string F = a.feitico;
            if (F == "runa") { a.runeT = 8; Ev(g, "sfx", "k", "rune", "x", R2(a.x), "z", R2(a.z), "a", a.id); }
            else if (F == "veu") { a.veuT = 5; Ev(g, "sfx", "k", "rune", "x", R2(a.x), "z", R2(a.z), "a", a.id); }
            else if (F == "sal")
            {
                var meus = g.sal.Where(q => q.dono == a.id).ToList(); if (meus.Count >= 2) g.sal.RemoveAt(g.sal.IndexOf(meus[0]));
                g.sal.Add(new Sal { id = PID++, x = R2(a.x + fx * 1.2), z = R2(a.z + fz * 1.2), dono = a.id, t = 60 }); Ev(g, "sfx", "k", "collect", "x", R2(a.x), "z", R2(a.z), "a", a.id);
            }
            else if (F == "empurrao")
            {
                foreach (var e in EnemiesOf(g, a))
                {
                    if (e.st != "alive") continue; double dx = e.x - a.x, dz = e.z - a.z, d = JS.Hypot(dx, dz); if (d > 3.8 || d < .01) continue; if ((dx * fx + dz * fz) / d < .34) continue;
                    double ux = dx / d, uz = dz / d; for (int s = 0; s < 8; s++) MoverSeguro(e, e.x + ux * .5, e.z + uz * .5, .4);
                    bool was = e.sealing >= 0; e.sealing = -1; e.hold = new Hold(); e.pushedT = g.t; e.stunT = Math.Max(e.stunT, .45); Damage(g, e, 10, a); alvos.Add(e.id);
                    if (was) LogE(g, "push_seal", "who", e.name, "by", a.name);
                }
                Ev(g, "sfx", "k", "push", "x", R2(a.x), "z", R2(a.z), "a", a.id);
            }
            else if (F == "flash")
            {
                foreach (var e in EnemiesOf(g, a))
                {
                    if (e.st != "alive") continue; double dx = e.x - a.x, dz = e.z - a.z, d = JS.Hypot(dx, dz); if (d > 14 || d < .01) continue; if ((dx * fx + dz * fz) / d < .8 || !Mapa.LosClear(a, e)) continue;
                    e.blindT = 1.5; alvos.Add(e.id); Ev(g, "blind", "a", e.id);
                }
                Ev(g, "sfx", "k", "flash", "x", R2(a.x + fx * 3), "z", R2(a.z + fz * 3), "a", a.id);
            }
            else if (F == "purificacao")
            {
                foreach (var e in EnemiesOf(g, a))
                {
                    if (e.st != "alive") continue; if (Dist(a, e) > 6 || !Mapa.LosClear(a, e)) continue; e.stunT = 1; e.runeT = 0; e.veuT = 0; e.shield = 0; alvos.Add(e.id); Ev(g, "stun", "a", e.id);
                }
                Ev(g, "sfx", "k", "purify", "x", R2(a.x), "z", R2(a.z), "a", a.id);
            }
            Ev(g, "hab", "a", a.id, "f", F, "x", R2(a.x), "z", R2(a.z), "fx", R2(fx), "fz", R2(fz), "alvos", alvos);
            a.abilCd = Regras.FEITICOS[F].cd; LogE(g, "ability", "who", a.name, "feitico", F, "alvos", alvos.Count); return true;
        }

        // ============ CONTEXTOS (E / T) ============
        public static bool InCircle(Ponto a, Ponto A) { return JS.Hypot(a.x - A.x, a.z - A.z) <= CFG.circleR; }
        public static Contextos Contexts(Jogo g, Ator a)
        {
            var o = new Contextos();
            if (a == null || a.st != "alive") return o;
            foreach (var b in g.actors)
            {
                if (b == a || b.st != "down" || Dist(a, b) > 1.8) continue;
                o.e = b.team == a.team ? new Ctx { key = "rev" + b.id, type = "revive", b = b.id, label = "Segure E para reanimar " + b.name, time = CFG.revive } : new Ctx { key = "exe" + b.id, type = "execute", b = b.id, label = "Segure E para executar " + b.name, time = CFG.execute };
                return o;
            }
            foreach (var n in Mapa.NPCS)
            {
                if (Dist(a, n) > 2.6) continue;
                if (n.time != null && n.time != a.team) { o.info = char.ToUpper(n.nome[0]) + n.nome.Substring(1) + " não negocia com você"; return o; }
                o.e = new Ctx { key = "loja" + n.id, type = "loja", npc = n.id, label = "Aperte E para negociar com " + n.nome, time = 0 }; return o;
            }
            foreach (var p in g.pontos)
            {
                if (!p.ativo || JS.Hypot(a.x - p.x, a.z - p.z) > 1.9) continue;
                if (a.team == "H" && p.tipo == "pista") { o.e = new Ctx { key = "pt" + p.i, type = "ponto", P = p.i, label = "Segure E para examinar o sinal antigo", time = 2 }; return o; }
                if (a.team == "H" && p.tipo == "sentinela" && !p.acesa) { o.e = new Ctx { key = "pt" + p.i, type = "ponto", P = p.i, label = "Segure E para acender a sentinela", time = 4 }; return o; }
                if (a.team == "C" && p.tipo == "erva") { o.e = new Ctx { key = "pt" + p.i, type = "ponto", P = p.i, label = "Segure E para colher a erva-noturna", time = 2.5 }; return o; }
                if (a.team == "C" && p.tipo == "tumulo") { o.e = new Ctx { key = "pt" + p.i, type = "ponto", P = p.i, label = "Segure E para profanar o túmulo", time = 4 }; return o; }
            }
            if (a.team == "H")
            {
                foreach (var A in g.altars) if (A.state == "active" && InCircle(a, A)) { o.e = new Ctx { key = "seal" + A.i, type = "seal", A = A.i, label = "Segure E para selar", time = 0 }; return o; }
                foreach (var A in g.altars) if (A.state == "awake" && Dist(a, A) < 3.4) { o.e = new Ctx { key = "purge" + A.i, type = "purge", A = A.i, label = "Segure E para purgar o altar", time = CFG.purge }; return o; }
                foreach (var R in g.reagents) if (R.has && Dist(a, R) < 2) { o.e = new Ctx { key = "burn" + R.i, type = "burn", R = R.i, label = "Segure E para queimar o reagente", time = CFG.burn }; return o; }
            }
            else
            {
                foreach (var R in g.reagents) if (R.has && Dist(a, R) < 2)
                    {
                        if (a.reag < CFG.carry) o.e = new Ctx { key = "col" + R.i, type = "collect", R = R.i, label = "Segure E para coletar", time = CFG.collect }; else o.info = "Você já carrega " + CFG.carry + " reagentes";
                        return o;
                    }
                foreach (var A in g.altars)
                {
                    double d = Dist(a, A);
                    if (A.state == "active" && InCircle(a, A)) { o.info = A.chosen ? "Canalizando o ritual" : ""; return o; }
                    if (d >= 3.4) continue;
                    if (A.chosen && A.state == "dormant") { if (a.reag > 0) o.e = new Ctx { key = "cons" + A.i, type = "consecrate", A = A.i, label = "Segure E para consagrar", time = CFG.consecrate }; else o.info = "Você precisa de 1 reagente"; return o; }
                    if (A.chosen && A.state == "awake" && !A.decoy) { string bl = BloqueioRitual(g); if (bl != null) { o.info = bl; return o; } }
                    if (A.chosen && A.state == "awake" && !A.decoy) { if (a.reag > 0 || CFG.iniciarDeGraca) o.e = new Ctx { key = "start" + A.i, type = "start", A = A.i, label = "Segure E para iniciar o ritual", time = CFG.start }; else o.info = "Você precisa de 1 reagente"; return o; }
                    if (!A.chosen && A.state == "dormant")
                    {
                        if (g.decoys > 0) o.e = new Ctx { key = "dec" + A.i, type = "decoy", A = A.i, label = "Segure E para plantar um Chamariz (" + g.decoys + ")", time = CFG.decoy };
                        var from = TransferSource(g, a);
                        if (!g.transferUsed && a.reag > 0 && from != null) o.t = new Ctx { key = "tr" + A.i, type = "transfer", A = A.i, from = from.i, label = "Segure T para transferir " + from.name + " para cá", time = CFG.transfer };
                        if (o.e == null && o.t == null) o.info = "Sem Chamarizes restantes";
                        return o;
                    }
                }
            }
            return o;
        }
        static Altar TransferSource(Jogo g, Ator a) { var c = g.altars.Where(A => A.chosen && A.state == "dormant").ToList(); if (c.Count == 0) return null; JS.Sort(c, (x, y) => Dist(a, y) - Dist(a, x)); return c[0]; }
        static void DoAction(Jogo g, Ator a, Ctx ctx)
        {
            Altar A = ctx.A.HasValue ? g.altars[ctx.A.Value] : null; Reagente R = ctx.R.HasValue ? g.reagents[ctx.R.Value] : null; Ator B = ctx.b.HasValue ? g.actors.FirstOrDefault(x => x.id == ctx.b.Value) : null;
            switch (ctx.type)
            {
                case "ponto":
                    {
                        var p = ctx.P.HasValue && ctx.P.Value < g.pontos.Count ? g.pontos[ctx.P.Value] : null; if (p == null || !p.ativo) break;
                        if (p.tipo == "pista")
                        {
                            p.ativo = false; a.obolos += 1; TarefaProg(g, "H", "pistas");
                            // o sinal responde "onde NÃO está": descarta um altar que o culto não escolheu e que a equipe ainda não conhece
                            var cand = g.altars.Where(X => !X.chosen && X.state == "dormant" && g.know[X.i] == "?").ToList(); int k = Idx(cand.Count); var D = k < cand.Count ? cand[k] : null;
                            if (D != null) { g.know[D.i] = "limpo"; Feed(g, a.name + " leu um sinal antigo: " + D.name + " não foi escolhido pelo culto.", "h", "H"); LogE(g, "pista_info", "altar", D.name); }
                            else Feed(g, a.name + " examinou um sinal antigo; ele não diz nada novo.", "h", "H");
                        }
                        else if (p.tipo == "sentinela") { p.acesa = true; TarefaProg(g, "H", "sentinelas"); Feed(g, a.name + " acendeu uma sentinela.", "h", "H"); Ev(g, "sfx", "k", "flare", "x", p.x, "z", p.z, "a", a.id); }
                        else if (p.tipo == "erva") { p.ativo = false; p.t = 50; a.obolos += 1; TarefaProg(g, "C", "ervas"); }
                        else if (p.tipo == "tumulo") { p.ativo = false; if (a.reag < CFG.carry) a.reag++; else a.obolos += 2; TarefaProg(g, "C", "tumulos"); Feed(g, a.name + " profanou um túmulo.", "c", "C"); }
                        LogE(g, "ponto", "tipo", p.tipo, "by", a.name); break;
                    }
                case "purge":
                    {
                        bool was = A.decoy; TarefaProg(g, "H", "purgar"); A.state = "dormant"; A.decoy = false; g.know[A.i] = "limpo";
                        Feed(g, was ? "Chamariz queimado em " + A.name + "." : A.name + " foi purgado.", "h", "H"); Feed(g, "Um altar foi purgado: " + A.name + ".", "c", "C");
                        LogE(g, "purge", "altar", A.name, "decoy", was, "by", a.name); break;
                    }
                case "burn": R.has = false; R.t = CFG.regrow * 1.2; LogE(g, "burn", "by", a.name); if (a.human) Ev(g, "feed", "msg", "Reagente queimado.", "cls", "h", "to", a.id); break;
                case "collect": R.has = false; R.t = CFG.regrow; a.reag++; Ev(g, "sfx", "k", "collect", "x", R.x, "z", R.z, "a", a.id); LogE(g, "collect", "by", a.name); if (a.human) Ev(g, "feed", "msg", "Reagente coletado (" + a.reag + "/" + CFG.carry + ").", "cls", "c", "to", a.id); break;
                case "consecrate": a.reag--; A.state = "awake"; A.decoy = false; TarefaProg(g, "C", "consagrar"); SentinelaAvisa(g, A, "consagração"); Feed(g, A.name + " consagrado.", "c", "C"); LogE(g, "consecrate", "altar", A.name, "by", a.name); break;
                case "start": if (!CFG.iniciarDeGraca) a.reag--; StartRitual(g, A, a); break;
                case "decoy": g.decoys--; A.state = "awake"; A.decoy = true; Feed(g, "Chamariz plantado em " + A.name + ".", "c", "C"); LogE(g, "decoy", "altar", A.name, "by", a.name); break;
                case "transfer":
                    {
                        var F = ctx.from.HasValue && ctx.from.Value < g.altars.Count ? g.altars[ctx.from.Value] : null; if (F == null || !F.chosen || F.state != "dormant" || g.transferUsed || a.reag < 1) break;
                        F.chosen = false; A.chosen = true; a.reag--; g.transferUsed = true;
                        Feed(g, "Transferência: " + A.name + " substitui " + F.name + ".", "c", "C"); LogE(g, "transfer", "from", F.name, "to", A.name, "by", a.name);
                        // a Transferência deixa rastro: marcas de arrasto no altar de origem por 3 min e um lamento audível perto dos dois altares
                        g.rastros.Add(new Rastro { de = F.i, para = A.i, x = F.x, z = F.z, t = g.rt });
                        Ev(g, "sfx", "k", "arrasto", "x", R2(F.x), "z", R2(F.z), "a", a.id); Ev(g, "sfx", "k", "arrasto", "x", R2(A.x), "z", R2(A.z), "a", a.id); break;
                    }
                case "revive":
                    if (B != null && B.st == "down") { B.st = "alive"; B.hp = JS.Round(B.maxHp * .45); B.revUsed = true; B.invuln = 1; B.downT = 0; Feed(g, a.name + " reanimou " + B.name, B.team == "H" ? "h" : "c", B.team); LogE(g, "revive", "who", B.name, "by", a.name); }
                    break;
                case "execute": if (B != null && B.st == "down") Kill(g, B, a, "exec"); break;
            }
        }
        static void HoldStep(Jogo g, Ator a, Ctx ctx, double dt)
        {
            if (a.hold.key != ctx.key) { a.hold.key = ctx.key; a.hold.t = 0; a.hold.max = ctx.time; }
            a.hold.t += dt; if (a.hold.t >= ctx.time) { DoAction(g, a, ctx); a.hold = new Hold(); }
        }

        // ============ FERRAMENTAS DO CAÇADOR ============
        static void UseSensor(Jogo g, Ator a)
        {
            if (a.sensorCd > 0) return; a.sensorCd = 15; double best = 0; double RS = CFG.sensorRaio;
            foreach (var A in g.altars) if (A.state == "awake" || A.state == "active") { double d = Dist(a, A); if (d < RS) best = Math.Max(best, (1 - d / RS) * (A.state == "active" ? 1.3 : 1)); }
            foreach (var R in g.reagents) if (R.has) { double d = Dist(a, R); if (d < 20) best = Math.Max(best, .28 * (1 - d / 20)); }
            best = Math.Min(1, best);
            Ev(g, "sensor", "a", a.id, "v", R2(best)); LogE(g, "sensor", "by", a.name, "v", R2(best));
        }
        static void ThrowFlare(Jogo g, Ator a)
        {
            if (a.flares <= 0) return; var f = g.flares.FirstOrDefault(q => q.t <= 0); if (f == null) { f = g.flares[0]; foreach (var q in g.flares) if (q.t < f.t) f = q; } // todos acesos: troca o que está mais perto de apagar
            double dx = -JS.Sin(a.yaw), dz = -JS.Cos(a.yaw); double d = Mapa.RaioParede(a.x, a.z, dx, dz, 10, .3);
            f.x = R2(a.x + dx * d); f.z = R2(a.z + dz * d); f.t = 14; a.flares--; Ev(g, "sfx", "k", "flare", "x", f.x, "z", f.z, "a", a.id);
        }

        // ============ AÇÕES DO JOGADOR HUMANO ============
        static void HumanActions(Jogo g, Ator a, double dt)
        {
            var c = a.inp.c ?? new Controles(); var L = a.lc; if (!a.lcIni) { a.lc = L = c.Copia(); a.lcIni = true; }
            bool eq = c.q != L.q, ef = c.f != L.f, eg = c.g != L.g, er = c.r != L.r, eab = c.ab != L.ab; L.q = c.q; L.f = c.f; L.g = c.g; L.r = c.r; L.ab = c.ab;
            if (a.st != "alive" || a.stunT > 0) { a.sealing = -1; a.hold.t = 0; return; }
            if (er) Reload(g, a);
            if (a.team == "H") { if (eq) UseSensor(g, a); if (ef) a.lantern = !a.lantern && a.carga > 0; if (eg) ThrowFlare(g, a); }
            if (eab) UseAbility(g, a);
            var ctx = Contexts(g, a); a.sealing = -1;
            if (ctx.e != null && ctx.e.type == "loja") a.hold = new Hold();
            else if (ctx.e != null && ctx.e.type == "seal" && a.inp.use) { a.sealing = ctx.e.A.Value; a.hold = new Hold(ctx.e.key, 0, 0); }
            else if (ctx.e != null && a.inp.use) HoldStep(g, a, ctx.e, dt);
            else if (ctx.t != null && a.inp.use2) HoldStep(g, a, ctx.t, dt);
            else a.hold = new Hold();
            if (a.team == "H")
            {
                if (a.inp.fire && a.fireCd <= 0 && a.ammo > 0 && a.reloadT <= 0 && a.sealing < 0 && a.blindT <= 0) FireHuman(g, a);
                if (a.lantern) { a.carga -= dt * 3.2; if (a.carga <= 0) { a.carga = 0; a.lantern = false; Ev(g, "feed", "msg", "Carga esgotada. Procure luz para recarregar.", "cls", "h", "to", a.id); } }
                else if (LightAt(g, a.x, a.z) > .5) a.carga = Math.Min(100, a.carga + dt * 6);
            }
            else
            {
                if (a.inp.fire && a.fireCd <= 0 && a.fervor >= CFG.sigilCusto && a.blindT <= 0) { double y = a.inp.yaw, p = a.inp.pitch; CastSigil(g, a, -JS.Sin(y) * JS.Cos(p), JS.Sin(p), -JS.Cos(y) * JS.Cos(p)); }
            }
        }

        // ============ ALTARES ============
        public static double MareFactor(Jogo g) { return 1 + Math.Min(.2, Math.Floor((g.rt - g.lastResolveT) / 60) * .05); }
        static void StartRitual(Jogo g, Altar A, Ator by)
        {
            A.state = "active"; A.startT = g.rt; A.localized = false; A.selo = 1; A.arrived = false; A.maxN = 0; A.tardio = g.momento >= 2; A.grande = Fendas(g) + Farois(g) >= 2;
            if (g.buff.selo > g.t) { A.selo = 1.1; g.buff.selo = 0; Feed(g, "Selo de Luz corrompido: este ritual corre 10% mais rápido.", "c", "C"); }
            int n = g.actors.Count(c => c.team == "C" && c.st == "alive" && InCircle(c, A));
            SentinelaAvisa(g, A, "um ritual começou");
            if (A.grande) { Localize(g, A, "grande"); Feed(g, "O Grande Ritual começou em " + A.name + ". A catedral inteira ouve.", "big"); Ev(g, "grande", "altar", A.i); }
            Ev(g, "presage"); Feed(g, "Presságio: um ritual despertou.", "big", "H"); Feed(g, "Ritual iniciado: " + A.name + ".", "c", "C");
            LogE(g, "ritual_start", "altar", A.name, "channelers", Math.Max(1, n), "by", by.name);
        }
        static void Localize(Jogo g, Altar A, string how)
        {
            if (A.localized) return; A.localized = true; g.know[A.i] = "ativo";
            Feed(g, "O coro revela: " + A.name + ".", "big", "H"); Feed(g, "Os Caçadores descobriram " + A.name + ".", "c", "C");
            LogE(g, "localized", "altar", A.name, "prog", R2(A.prog), "how", how);
        }
        // move e, se o resultado ainda ficar preso entre peças (cantos côncavos de escombros), desfaz o passo
        static void MoverSeguro(Ponto a, double nx, double nz, double r) { double px = a.x, pz = a.z; a.x = nx; a.z = nz; Mapa.Resolve(a, r); if (!Mapa.Livre(a, r)) { a.x = px; a.z = pz; } }
        static void ResolveAltar(Jogo g, Altar A, string kind)
        {
            A.state = kind; A.maxProg = Math.Max(A.maxProg, A.prog); g.lastResolveT = g.rt; A.sealers = new List<int>();
            foreach (var a in g.actors) { a.dc = 0; a.revUsed = false; if (a.sealing == A.i) a.sealing = -1; }
            if (kind == "fenda") { A.doneT = R2(g.rt); Ev(g, "doom"); Feed(g, "Ritual completo em " + A.name + ". Uma Fenda se abriu.", "big c"); LogE(g, "ritual_complete", "altar", A.name, "canalizadores", A.maxN > 0 ? A.maxN : 1); }
            else { Ev(g, "sealed"); g.buff.ess = 0; Feed(g, A.name + " foi selado. Um Farol se acendeu.", "big h"); LogE(g, "sealed", "altar", A.name, "prog", R2(A.prog), "canalizadores", A.maxN > 0 ? A.maxN : 1); }
            if (kind == "fenda") TarefaProg(g, "C", "ritual");
            int F = Fendas(g), P = Farois(g);
            if (F + P >= 3 || (CFG.formatoNoite != "placar" && (F >= 2 || P >= 2))) g.endAt = g.rt + 3;
        }
        static void UpdateAltars(Jogo g, double dt)
        {
            foreach (var A in g.altars)
            {
                if (A.state != "active") continue;
                var ch = g.actors.Where(c => c.team == "C" && c.st == "alive" && InCircle(c, A)).Take(2).ToList(); int n = ch.Count;
                bool rune = false;
                if (n > 0)
                {
                    double f = 0; foreach (var c in ch) { double k = (g.t - c.hitT < 1 || c.blindT > 0 || c.stunT > 0) ? .5 : 1; if (c.runeT > 0) { k *= 1.2; rune = true; } f += k; } f /= n;
                    A.prog += dt * f * MareFactor(g) * A.selo * (A.tardio ? 1.25 : 1) * (A.grande ? .6 : 1) / CFG.ritDur[n]; A.lastN = n; A.maxN = Math.Max(A.maxN, n);
                }
                else if (A.prog > A.cp) A.prog = Math.Max(A.cp, A.prog - .02 * dt);
                foreach (double c in CFG.cps) if (A.prog >= c && A.cp < c)
                    {
                        A.cp = c; LogE(g, "checkpoint", "altar", A.name, "cp", c);
                        if (ch.Any(x => x.cls == "ritualista")) foreach (var x in ch) { x.shield = 15; x.shieldT = 8; }
                    }
                A.maxProg = Math.Max(A.maxProg, A.prog);
                int nn = Math.Max(1, A.lastN); double rad = CFG.ritRad[nn] * (rune ? 1.5 : 1) * (A.tardio ? 1.4 : 1);
                if (!A.localized) { if (A.prog >= CFG.ritLoc[nn]) Localize(g, A, "coro"); else if (n > 0 && g.actors.Any(h => h.team == "H" && h.st == "alive" && Dist(h, A) < rad)) Localize(g, A, "ouvido"); }
                if (!A.arrived && g.actors.Any(h => h.team == "H" && h.st == "alive" && InCircle(h, A))) { A.arrived = true; LogE(g, "hunter_arrive", "altar", A.name, "prog", R2(A.prog), "channelers", nn, "remaining", R2((1 - A.prog) * CFG.ritDur[nn])); }
                var sealers = g.actors.Where(h => h.team == "H" && h.st == "alive" && h.sealing == A.i && InCircle(h, A)).ToList();
                if (sealers.Count > 0)
                {
                    if (A.sealers.Count == 0) TarefaProg(g, "H", "selar");
                    if (A.sealers.Count == 0) LogE(g, "seal_start", "altar", A.name, "prog", R2(A.prog), "sealers", sealers.Count);
                    double rate = 0; foreach (var s in sealers) rate = Math.Max(rate, 1 / (s.cls == "exorcista" ? CFG.sealExo : CFG.seal));
                    if (sealers.Count >= 2) rate *= CFG.coSeal; if (g.buff.ess > g.t) rate *= 1.25; rate = Math.Min(rate, 1 / CFG.sealMin);
                    int at = 0; foreach (var s in sealers) at = Math.Max(at, RecentAttackers(g, s));
                    if (at >= 2) rate *= CFG.fire2; else if (at == 1) rate *= CFG.fire1;
                    A.seal += rate * dt; A.sealIdle = 0; A.fireT = A.fireT + (at != 0 ? dt : 0); A.sealT = A.sealT + dt;
                }
                else
                {
                    if (A.sealers.Count > 0)
                    {
                        var who = A.sealers.Select(id => g.actors.FirstOrDefault(x => x.id == id)).Where(x => x != null).ToList();
                        string cause = who.Any(x => x.st != "alive") ? "caído" : who.Any(x => g.t - x.pushedT < .6) ? "empurrão" : who.Any(x => x.stunT > 0) ? "atordoado" : "saiu";
                        LogE(g, "seal_stop", "altar", A.name, "seal", R2(A.seal), "cause", cause);
                    }
                    A.sealIdle += dt; if (A.sealIdle > 3) A.seal = Math.Max(0, A.seal - dt / CFG.seal);
                }
                A.sealers = sealers.Select(s => s.id).ToList();
                if (A.seal >= 1) ResolveAltar(g, A, "farol"); else if (A.prog >= 1) { A.prog = 1; ResolveAltar(g, A, "fenda"); }
            }
        }
        static void UpdateKnowledge(Jogo g, double dt)
        {
            foreach (var h in g.actors)
            {
                if (h.team != "H" || h.st != "alive") continue;
                for (int i = 0; i < g.altars.Count; i++)
                {
                    var A = g.altars[i]; double d = Dist(h, A); if (d >= 10) continue; g.visit[i] = g.t; string k = g.know[i];
                    if (A.state == "awake")
                    {
                        if (k == "?" || k == "limpo") { g.know[i] = "desperto"; Ev(g, "evp", "a", h.id, "x", A.x, "z", A.z); Feed(g, "EVP: vozes em " + A.name + ".", "h", "H"); }
                        if (!h.human && d < 6 && g.know[i] == "desperto" && Rnd() < dt * .35) g.know[i] = A.decoy ? "chamariz" : "confirmado";
                    }
                    else if (A.state == "dormant" && k != "limpo") g.know[i] = "limpo";
                }
                if (h.human && h.lantern)
                {
                    double fx = -JS.Sin(h.yaw), fz = -JS.Cos(h.yaw);
                    for (int i = 0; i < g.altars.Count; i++)
                    {
                        var A = g.altars[i]; if (A.state != "awake" && A.state != "active") continue; double dx = A.x - h.x, dz = A.z - h.z, d = JS.Hypot(dx, dz); if (d > 16 || d < .5) continue;
                        if ((dx * fx + dz * fz) / d < .9 || !Mapa.LosClear(h, A)) continue;
                        if (A.decoy) { if (g.know[i] != "chamariz") { g.know[i] = "chamariz"; Feed(g, "Lanterna: nenhum resíduo em " + A.name + ". É um Chamariz.", "h", "H"); LogE(g, "decoy_spotted", "altar", A.name, "by", h.name); } }
                        else if (g.know[i] != "confirmado" && g.know[i] != "ativo") { g.know[i] = "confirmado"; Feed(g, "Lanterna: resíduo de reagente em " + A.name + ".", "h", "H"); }
                    }
                }
            }
        }

        // ============ PROJÉTEIS, COLETÁVEIS ============
        static void UpdateProjectiles(Jogo g, double dt)
        {
            for (int i = g.proj.Count - 1; i >= 0; i--)
            {
                var p = g.proj[i]; bool dead = false;
                for (int s = 0; s < 3 && !dead; s++)
                {
                    p.x += p.vx * dt / 3; p.y += p.vy * dt / 3; p.z += p.vz * dt / 3;
                    if (p.y < .05 || p.y > 14 || Math.Abs(p.x) > Mapa.HX || Math.Abs(p.z) > Mapa.HZ) dead = true;
                    if (!dead && Mapa.Algum(Mapa.GT, p.x, p.z, p.x, p.z, b => p.x > b.x1 && p.x < b.x2 && p.z > b.z1 && p.z < b.z2 && p.y < b.h)) dead = true;
                    if (!dead) foreach (var e in g.actors)
                        {
                            if (e.team == p.team || e.st == "dead") continue; double top = e.st == "down" ? .6 : 1.95;
                            if (JS.Hypot(p.x - e.x, p.z - e.z) < .5 && p.y > 0 && p.y < top) { var src = g.actors.FirstOrDefault(x => x.id == p.owner); if (src != null) Damage(g, e, p.dmg, src); dead = true; break; }
                        }
                }
                p.life -= dt; if (p.life <= 0) dead = true;
                if (dead) { Ev(g, "burst", "x", R2(p.x), "y", R2(Math.Max(.1, p.y)), "z", R2(p.z), "team", p.team); g.proj.RemoveAt(i); }
            }
        }
        static void UpdatePickups(Jogo g, double dt)
        {
            for (int i = g.pickups.Count - 1; i >= 0; i--)
            {
                var p = g.pickups[i]; p.life -= dt; bool gone = p.life <= 0;
                foreach (var a in g.actors)
                {
                    if (gone || a.st != "alive" || JS.Hypot(a.x - p.x, a.z - p.z) > 1.1) continue;
                    if (p.kind == "reag") { if (a.team == "C" && a.reag < CFG.carry) { a.reag++; gone = true; } else if (a.team == "H") gone = true; }
                    else if (p.kind == "ess") { if (a.team == "H") { g.buff.ess = g.t + 60; Feed(g, "Essência Profana capturada: próximo selamento a 125%.", "h", "H"); LogE(g, "pickup", "kind", "ess", "by", a.name); } gone = true; }
                    else if (p.kind == "selo") { if (a.team == "C") { g.buff.selo = g.t + 120; Feed(g, "Selo de Luz capturado: o próximo ritual corre 10% mais rápido.", "c", "C"); LogE(g, "pickup", "kind", "selo", "by", a.name); } gone = true; }
                }
                if (gone) g.pickups.RemoveAt(i);
            }
        }

        // ============ PASSO PRINCIPAL ============
        public static void Step(Jogo g, double dt)
        {
            g.t += dt;
            if (g.phase == "play") { g.rt += dt; PlayStep(g, dt); }
            else if (g.timers && g.phase != "final") { g.phaseT -= dt; if (g.phaseT <= 0) TimeoutPhase(g); }
        }
        static void PlayStep(Jogo g, double dt)
        {
            G_ATUAL = g;
            foreach (var a in g.actors)
            {
                a._vx = (a.x - (a._px.HasValue ? a._px.Value : a.x)) / dt; a._vz = (a.z - (a._pz.HasValue ? a._pz.Value : a.z)) / dt; a._px = a.x; a._pz = a.z;
                a.hist.Add(new[] { g.t, a.x, a.z }); while (a.hist.Count > 0 && a.hist[0][0] < g.t - 1.2) a.hist.RemoveAt(0);
                if (a.invuln > 0) a.invuln = Math.Max(0, a.invuln - dt); if (a.blindT > 0) a.blindT = Math.Max(0, a.blindT - dt); if (a.stunT > 0) a.stunT = Math.Max(0, a.stunT - dt);
                if (a.runeT > 0) a.runeT = Math.Max(0, a.runeT - dt); if (a.abilCd > 0) a.abilCd = Math.Max(0, a.abilCd - dt); if (a.sensorCd > 0) a.sensorCd = Math.Max(0, a.sensorCd - dt); if (a.fireCd > 0) a.fireCd = Math.Max(0, a.fireCd - dt);
                if (a.shieldT > 0) { a.shieldT -= dt; if (a.shieldT <= 0) a.shield = 0; }
                if (a.human && a.reloadT > 0) { a.reloadT -= dt; if (a.reloadT <= 0) { int n = Math.Min(8 - a.ammo, a.reserve); a.ammo += n; a.reserve -= n; } }
                if (a.st == "alive" && JS.Hypot(a.x - Mapa.SPAWN[a.team].x, a.z - Mapa.SPAWN[a.team].z) < CFG.spawnSafe) { a.invuln = Math.Max(a.invuln, .25); if (!a.human || a.reserve < 48) a.reserve = Math.Max(a.reserve, 48); }
            }
            foreach (var a in g.actors)
            {
                if (a.st == "dead") { a.respawnT -= dt; if (a.respawnT <= 0) { Spawn(g, a, false); Ev(g, "respawn", "a", a.id); } continue; }
                if (a.st == "down") { a.downT -= dt; a.sealing = -1; if (a.downT <= 0) Kill(g, a, null); if (!a.human) a.moving = false; else HumanActions(g, a, dt); continue; }
                if (a.human) HumanActions(g, a, dt);
                else if (a.stunT <= 0)
                {
                    if (a.team == "H") AiHunter(g, a, dt); else AiCult(g, a, dt);
                    // rede de segurança: bot parado 15 s sem estar ocupado tem a decisão reiniciada (não corrige a causa; limita o estrago)
                    var ai = a.ai; bool vigiando = ai.goal != null && ai.goalKey.StartsWith("pt", StringComparison.Ordinal) && Dist(a, ai.goal) < 2, lutando = ai.target != null && g.t - a.lastShotT < 2.5;
                    bool ocupado = a.sealing >= 0 || a.hold.t > 0 || vigiando || lutando || g.altars.Any(A => A.state == "active" && InCircle(a, A)) || (ai.esperando != 0 && g.t - ai.esperando < .5);
                    if (ai.vigia == null || ocupado || JS.Hypot(a.x - ai.vigia.x, a.z - ai.vigia.z) > 3) ai.vigia = new Vigia { x = a.x, z = a.z, t = g.rt };
                    else if (g.rt - ai.vigia.t > 15)
                    {
                        string objetivoAntes = ai.goalKey; string alvoAntes = ai.target != null ? ai.target.name : null;
                        ai.goal = null; ai.goalKey = ""; ai.path = new List<Ponto>(); ai.ignora = new Dictionary<string, double>(); ai.wait = 0; ai.decoyUsed = true; ai.progKey = ""; ai.vigia = new Vigia { x = a.x, z = a.z, t = g.rt };
                        var q = PontoDesvio(a); ai.goal = q; ai.goalKey = "desvio"; ai.path = new List<Ponto> { q }; ai.repath = g.t + 3;
                        LogE(g, "bot_reiniciado", "who", a.name, "team", a.team, "cls", a.cls, "objetivo", objetivoAntes, "combate", alvoAntes, "reag", a.reag, "x", R2(a.x), "z", R2(a.z));
                    }
                }
                if (a.moving && a.st == "alive") { a.stepT = a.stepT - dt; if (a.stepT <= 0) { bool corre = a.human && a.inp.sp && a.inp.mz < 0; a.stepT = corre ? .3 : .42; Ev(g, "sfx", "k", corre ? "passoForte" : "step", "x", R2(a.x), "z", R2(a.z), "a", a.id); } }
                if (g.t - a.hitT > 6 && a.hp < a.maxHp && a.st == "alive") a.hp = Math.Min(a.maxHp, a.hp + dt * 8);
                if (a.team == "C" && a.st == "alive" && g.t - a.lastShotT > CFG.fervorEspera) a.fervor = Math.Min(100, a.fervor + dt * CFG.fervorRegen);
                UpdateSanity(g, a, dt);
                if (!a.human) foreach (var o in g.actors) if (o.team != a.team && o.st == "alive" && g.t - o.panicT < .5 && Dist(a, o) < 18) a.ai.alert = new Alerta { x = o.x, z = o.z, t = g.t };
            }
            Rastrear(g);
            PassoNoite(g, dt); UpdateAltars(g, dt); UpdateKnowledge(g, dt); UpdateProjectiles(g, dt); UpdatePickups(g, dt);
            foreach (var R in g.reagents) if (!R.has) { R.t -= dt; if (R.t <= 0) R.has = true; }
            foreach (var f in g.flares) if (f.t > 0) f.t = Math.Max(0, f.t - dt);
            if (g.rt >= CFG.roundTime || (g.endAt != 0 && g.rt >= g.endAt)) EndRound(g);
        }
        static void EndRound(Jogo g)
        {
            var done = g.altars.Where(A => A.chosen && A.state == "fenda").ToList();
            double? lastT = done.Count > 0 ? done.Max(A => A.doneT ?? 0) : (double?)null;
            double maxP = 0; foreach (var A in g.altars) if (A.chosen) maxP = Math.Max(maxP, A.maxProg);
            int sealed_ = g.altars.Count(A => A.state == "farol");
            string cultTeam = g.round == 1 ? "B" : "A";
            g.scores[cultTeam] = new Placar { done = done.Count, lastT = lastT, maxP = R2(Math.Min(1, maxP)), @sealed = sealed_ };
            LogE(g, "round_end", "cultTeam", cultTeam, "done", done.Count, "sealed", sealed_, "lastT", lastT);
            g.stats[g.round] = RoundStats(g, g.round);
            g.phase = "summary"; g.phaseT = CFG.timerSummary; g.ready = new Dictionary<string, bool>(); g.proj = new List<Projetil>();
            foreach (var a in g.actors) { a.sealing = -1; a.inp.fire = false; }
        }
        static Estatisticas RoundStats(Jogo g, int round)
        {
            var L = g.log.Where(e => e.r == round).ToList(); Func<string, List<Registro>> c = k => L.Where(e => e.ev == k).ToList();
            double fireT = g.altars.Sum(A => A.fireT), sealT = g.altars.Sum(A => A.sealT);
            var fc = L.FirstOrDefault(e => e.ev == "first_contact");
            return new Estatisticas
            {
                rituais = c("ritual_start").Count, completos = c("ritual_complete").Count, selados = c("sealed").Count, chegadas = c("hunter_arrive").Count,
                tentativasSelo = c("seal_start").Count, cancelados = c("seal_stop").Count(e => (string)e["cause"] != "saiu"),
                fracaoSobFogo = sealT > 0 ? R2(fireT / sealT) : (double?)null, primeiroContato = fc != null ? fc.t : (double?)null,
                quedas = c("down").Count, mortes = c("death").Count, reanimacoes = c("revive").Count, purgas = c("purge").Count, chamarizes = c("decoy").Count, transferencias = c("transfer").Count
            };
        }
        public static Vencedor Winner(Jogo g)
        {
            if (!(Noites(g) > 1))
            {
                var S = g.scores["B"]; if (S == null) return null;
                return S.done >= 2 ? new Vencedor { win = "B", why = "os Cultistas venceram dois dos três rituais" } : new Vencedor { win = "A", why = S.@sealed >= 2 ? "os Caçadores selaram dois dos três rituais" : "os Caçadores resistiram até o amanhecer" };
            }
            var A = g.scores["A"]; var B = g.scores["B"]; if (A == null || B == null) return null;
            if (A.done != B.done) return new Vencedor { win = A.done > B.done ? "A" : "B", why = "mais rituais completados" };
            if (A.done > 0 && A.lastT != B.lastT) return new Vencedor { win = A.lastT < B.lastT ? "A" : "B", why = "desempate: completou o último ritual mais cedo" };
            if (Math.Abs(A.maxP - B.maxP) > 0.005) return new Vencedor { win = A.maxP > B.maxP ? "A" : "B", why = "desempate: maior progresso em um único ritual" };
            return new Vencedor { win = null, why = "empate exato; na versão completa, isso levaria ao Ritual Final" };
        }
    }
}
