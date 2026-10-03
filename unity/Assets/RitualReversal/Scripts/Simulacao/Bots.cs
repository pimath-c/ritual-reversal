// Bots: visão, alvo, movimento por caminho, combate, tarefas da noite, Caçador e Cultista.
// Porte da seção "BOTS" de shared/sim.js, na mesma ordem de decisões (e de sorteios).
using System;
using System.Collections.Generic;
using System.Linq;

namespace RitualReversal.Simulacao
{
    public static partial class Sim
    {
        static bool NoCone(Ator a, Ponto t, double alc, double cos)
        {
            double dx = t.x - a.x, dz = t.z - a.z, d = JS.Hypot(dx, dz); if (d > alc || d < .01) return d <= .01;
            return (dx * -JS.Sin(a.yaw) + dz * -JS.Cos(a.yaw)) / d >= cos;
        }
        static bool CanSee(Jogo g, Ator b, Ator t, double range)
        {
            if (t.st == "dead" || b.blindT > 0) return false; double d = Dist(b, t); if (d > range) return false;
            if (t.veuT > 0 && d > 8 && !(b.lantern && NoCone(b, t, 16, .9))) return false; // Véu: silhueta tênue, só some de longe
            if (d > CFG.nevoaAlcance && (Mapa.ZoneAt(b.x, b.z) == "Floresta" || Mapa.ZoneAt(t.x, t.z) == "Floresta")) return false; // névoa da mata
            if (d > 7) { double fx = -JS.Sin(b.yaw), fz = -JS.Cos(b.yaw); if (((t.x - b.x) * fx + (t.z - b.z) * fz) / d < .35 && g.t - b.hitT > 2) return false; }
            if (!Mapa.LosClear(b, t)) return false;
            double lit = LightAt(g, t.x, t.z) + (t.lantern ? .8 : 0) + (g.t - t.lastShotT < 1.5 ? .8 : 0);
            return !(d > 13 && lit < .35);
        }
        static Ator BotPickTarget(Jogo g, Ator b)
        {
            var ens = EnemiesOf(g, b).Where(e => e.st == "alive" && CanSee(g, b, e, 34)).ToList(); if (ens.Count == 0) return null;
            Func<Ator, double> pri = e => Dist(b, e) - (e.sealing >= 0 ? 30 : 0) - (b.team == "H" && g.altars.Any(A => A.state == "active" && InCircle(e, A)) ? 15 : 0);
            JS.Sort(ens, (x, y) => pri(x) - pri(y)); return ens[0];
        }
        static double LerpAngle(double a, double b, double t) { double d = ((b - a + Math.PI) % (Math.PI * 2) + Math.PI * 2) % (Math.PI * 2) - Math.PI; return a + d * t; }
        static Ponto PontoDesvio(Ator b)
        {
            double a0 = Rnd() * 6.283;
            for (int k = 0; k < 8; k++) { double an = a0 + k * Math.PI / 4; var q = Pt(b.x + JS.Cos(an) * 7, b.z + JS.Sin(an) * 7); if (Mapa.Livre(q, .5) && Mapa.SegClear(b.x, b.z, q.x, q.z, .45, Mapa.GB)) return q; }
            return Pt(b.x, b.z);
        }
        static void SetGoal(Jogo g, Ator b, double x, double z, string key)
        {
            var ai = b.ai;
            if (ai.Ignorado(key, g.t)) { if (ai.goalKey != "desvio") { var p = PontoDesvio(b); ai.goal = p; ai.goalKey = "desvio"; ai.path = new List<Ponto> { p }; ai.repath = g.t + 3; } return; }
            if (ai.goalKey != key || ai.goal == null) { ai.goal = Pt(x, z); ai.goalKey = key; ai.path = Mapa.FindPath(b, Pt(x, z)); ai.repath = g.t + 3; }
        }
        static double Comprimento(Ator b, List<Ponto> pp) { double d = 0; Ponto c = b; foreach (var q in pp) { d += JS.Hypot(q.x - c.x, q.z - c.z); c = q; } return d; }
        static void BotMove(Jogo g, Ator b, double dt, double mul)
        {
            var ai = b.ai; b.moving = false; if (ai.goal == null || b.stunT > 0) return;
            {
                double dg = Dist(b, ai.goal); if (ai.progKey != ai.goalKey) { ai.progKey = ai.goalKey; ai.melhor = dg; ai.progT = g.t; ai.progX = b.x; ai.progZ = b.z; }
                if (dg < ai.melhor - 1 || JS.Hypot(b.x - ai.progX, b.z - ai.progZ) > 2) { ai.melhor = Math.Min(ai.melhor, dg); ai.progT = g.t; ai.progX = b.x; ai.progZ = b.z; }
                if (dg > 1.2 && g.t - ai.progT > 10 && ai.goalKey != "desvio") { ai.Ignorar(ai.goalKey, g.t + 25); ai.goal = null; ai.goalKey = ""; ai.progKey = ""; return; }
            }
            if (g.t > ai.repath) { var nova = Mapa.FindPath(b, ai.goal); if (ai.path.Count == 0 || Comprimento(b, nova) < Comprimento(b, ai.path) * .85) ai.path = nova; ai.repath = g.t + 3; }
            if (ai.path.Count == 0) return; var tgt = ai.path[0]; double dx = tgt.x - b.x, dz = tgt.z - b.z, d = JS.Hypot(dx, dz);
            if (d < .7) { ai.path.RemoveAt(0); if (ai.path.Count == 0) return; tgt = ai.path[0]; dx = tgt.x - b.x; dz = tgt.z - b.z; d = JS.Hypot(dx, dz); }
            double sp = (b.st == "down" ? .8 : CFG.botSpeed * (b.speed / CFG.speed)) * mul * (b.sealing >= 0 ? .4 : 1) * (b.slowT > 0 ? .6 : 1);
            double bx0 = b.x, bz0 = b.z; b.x += dx / d * sp * dt; b.z += dz / d * sp * dt; b.moving = true;
            if (ai.target == null) b.yaw = LerpAngle(b.yaw, JS.Atan2(-dx, -dz), .15);
            foreach (var o in g.actors) { if (o == b || o.st == "dead") continue; double ox = b.x - o.x, oz = b.z - o.z, od = JS.Hypot(ox, oz); if (od < .8 && od > .01) { b.x += ox / od * (.8 - od) * .5; b.z += oz / od * (.8 - od) * .5; } }
            Mapa.Resolve(b, .4); if (!Mapa.Livre(b, .4)) { b.x = bx0; b.z = bz0; }
            ai.stuckT += dt;
            if (ai.stuckT > 1.5) { if (JS.Hypot(b.x - ai.lx, b.z - ai.lz) < .6) { ai.path = Mapa.FindPath(b, ai.goal); b.x += Rand(-.5, .5); b.z += Rand(-.5, .5); Mapa.Resolve(b, .4); } ai.stuckT = 0; ai.lx = b.x; ai.lz = b.z; }
        }
        static Ponto CircleSpot(Ponto A, Ator b)
        {
            double a0 = (b.id * 2.1) % (Math.PI * 2);
            for (int k = 0; k < 8; k++)
            {
                double a = a0 + k * Math.PI / 4; var p = Pt(A.x + JS.Cos(a) * 2.1, A.z + JS.Sin(a) * 2.1);
                bool ocupado = G_ATUAL != null && G_ATUAL.actors.Any(o => o != b && o.st != "dead" && JS.Hypot(o.x - p.x, o.z - p.z) < .9);
                if (!ocupado && Mapa.Livre(p, .42)) return p;
            }
            return Pt(A.x + JS.Cos(a0) * 2.1, A.z + JS.Sin(a0) * 2.1);
        }
        static bool BotCombat(Jogo g, Ator b, double dt)
        {
            var ai = b.ai; ai.target = BotPickTarget(g, b);
            if (ai.target == null && ai.alert != null && g.t - ai.alert.t < 4) b.yaw = LerpAngle(b.yaw, JS.Atan2(-(ai.alert.x - b.x), -(ai.alert.z - b.z)), .1);
            if (ai.target == null) return false;
            var t = ai.target; b.yaw = LerpAngle(b.yaw, JS.Atan2(-(t.x - b.x), -(t.z - b.z)), .25);
            if (b.fireCd <= 0 && b.sealing < 0 && b.hold.t <= 0)
            {
                double d = Dist(b, t);
                if (b.team == "H")
                {
                    if (b.reloadT > 0) return true; if (b.ammo <= 0) { Reload(g, b); return true; }
                    bool lit = LightAt(g, t.x, t.z) > .35; double p = JS.Clamp(.78 - d / 55, .22, .78) * (t.moving ? .8 : 1) * (lit ? 1 : .72); bool hit = Rnd() < p; double ex = hit ? 0 : Rand(-1.2, 1.2);
                    FireBotAt(g, b, t.x + ex, Rand(.9, 1.6), t.z + ex, !hit);
                }
                else
                {
                    if (b.fervor < CFG.sigilCusto) return true; double lead = d / CFG.sigilVel;
                    CastSigilAt(g, b, t.x + Rand(-.7, .7) + t._vx * lead * .6, 1.2, t.z + Rand(-.7, .7) + t._vz * lead * .6);
                }
            }
            return true;
        }
        static void CastSigilAt(Jogo g, Ator b, double tx, double ty, double tz) { double dx = tx - b.x, dy = ty - 1.62, dz = tz - b.z; double l = JS.Hypot(dx, dy, dz); if (l == 0 || double.IsNaN(l)) l = 1; CastSigil(g, b, dx / l, dy / l, dz / l); }
        static void BotHold(Jogo g, Ator b, string key, double time, double dt, Ctx ctx)
        {
            if (b.hold.key != key) b.hold = new Hold(key, 0, time);
            b.hold.t += dt; if (b.hold.t >= time) { DoAction(g, b, ctx); b.hold = new Hold(); }
        }
        static bool InEnemySpawn(Ator b, Ponto p) { var s = Mapa.SPAWN[b.team == "H" ? "C" : "H"]; return JS.Hypot(p.x - s.x, p.z - s.z) < CFG.spawnSafe + 2; }
        static bool BotHelpDowned(Jogo g, Ator b, double dt, bool fighting)
        {
            var ally = g.actors.FirstOrDefault(o => o != b && o.team == b.team && o.st == "down" && Dist(o, b) < 20);
            if (ally != null && !fighting)
            {
                if (Dist(b, ally) < 1.5) { b.moving = false; BotHold(g, b, "rev" + ally.id, CFG.revive, dt, new Ctx { type = "revive", b = ally.id }); return true; }
                SetGoal(g, b, ally.x, ally.z, "rv" + ally.id); BotMove(g, b, dt, 1.05); return true;
            }
            var foe = g.actors.FirstOrDefault(o => o.team != b.team && o.st == "down" && Dist(o, b) < 10);
            if (foe != null && !fighting)
            {
                if (Dist(b, foe) < 1.5) { b.moving = false; BotHold(g, b, "exe" + foe.id, CFG.execute, dt, new Ctx { type = "execute", b = foe.id }); return true; }
                SetGoal(g, b, foe.x, foe.z, "ex" + foe.id); BotMove(g, b, dt, 1.05); return true;
            }
            return false;
        }
        static bool BotNoite(Jogo g, Ator b, double dt)
        {
            var ai = b.ai; string role = b.team;
            // compras: Cultista sem reagente compra; Caçador com pouca munição compra
            string quer = role == "C" ? (b.reag == 0 && b.obolos >= Regras.ITENS["reagente"].preco ? "reagente" : null) : (b.reserve < 16 && b.obolos >= Regras.ITENS["municao"].preco ? "municao" : null);
            if (quer != null)
            {
                var n = JS.Primeiro(Mapa.NPCS.Where(x => (x.time == null || x.time == role) && x.vende.Contains(quer)).ToList(), (x, y) => Dist(b, x) - Dist(b, y));
                if (n != null && Dist(b, n) < 55)
                {
                    if (Dist(b, n) < 2.4) { b.moving = false; if (b.cid == null) ComprarBot(g, b, n, quer); return true; }
                    SetGoal(g, b, n.x, n.z, "npc" + n.id); BotMove(g, b, dt, 1); return true;
                }
            }
            // tarefas do Crepúsculo
            if (g.momento != 0) return false;
            List<Tarefa> lt; var pend = (g.tarefas.TryGetValue(role, out lt) ? lt : new List<Tarefa>()).Where(t => !t.feita).Select(t => t.id).ToList();
            var tipos = role == "H" ? new[] { pend.Contains("pistas") ? "pista" : null, pend.Contains("sentinelas") ? "sentinela" : null } : new[] { pend.Contains("ervas") ? "erva" : null, pend.Contains("tumulos") ? "tumulo" : null };
            var alvo = JS.Primeiro(g.pontos.Where(p => p.ativo && tipos.Contains(p.tipo) && !(p.tipo == "sentinela" && p.acesa) && !ai.Ignorado("pn" + p.i, g.t)).ToList(), (x, y) => Dist(b, x) - Dist(b, y));
            if (alvo == null) return false;
            if (Dist(b, alvo) < 1.7)
            {
                b.moving = false; var ctx = Contexts(g, b).e;
                if (ctx != null && ctx.type == "ponto") BotHold(g, b, ctx.key, ctx.time, dt, ctx);
                else ai.Ignorar("pn" + alvo.i, g.t + 40); // a ação não aparece aqui (outra coisa tem prioridade): tenta outro ponto
                return true;
            }
            SetGoal(g, b, alvo.x, alvo.z, "pn" + alvo.i); BotMove(g, b, dt, 1); return true;
        }
        static void ComprarBot(Jogo g, Ator b, Npc n, string item)
        {
            var it = Regras.ITENS[item]; if (b.obolos < it.preco) return; b.obolos -= it.preco;
            if (item == "reagente" && b.reag < CFG.carry) b.reag++; else if (item == "municao") b.reserve = Math.Min(80, b.reserve + 16);
            LogE(g, "compra", "who", b.name, "item", item, "npc", n.id);
        }
        static readonly string[] CONHECIDO = { "confirmado", "desperto", "chamariz" };
        static void AiHunter(Jogo g, Ator b, double dt)
        {
            bool fighting = BotCombat(g, b, dt); var ai = b.ai; b.sealing = -1;
            if (b.reloadT > 0) { b.reloadT -= dt; if (b.reloadT <= 0) { int n = Math.Min(8 - b.ammo, b.reserve); b.ammo += n; b.reserve -= n; if (b.reserve <= 0) b.reserve = 24; } }
            if (ai.target != null && b.abilCd <= 0)
            {
                double d = Dist(b, ai.target);
                if (b.feitico == "flash" && d < 12) UseAbility(g, b);
                if (b.feitico == "purificacao" && EnemiesOf(g, b).Any(e => e.st == "alive" && Dist(b, e) < 5.5 && Mapa.LosClear(b, e))) UseAbility(g, b);
            }
            if (b.feitico == "sal" && b.abilCd <= 0) { var As = g.altars.FirstOrDefault(A => (A.state == "active" || g.know[A.i] == "confirmado") && Dist(b, A) < 7); if (As != null) UseAbility(g, b); }
            if (BotHelpDowned(g, b, dt, fighting)) return;
            if (!fighting && BotNoite(g, b, dt)) return;
            var act = JS.Primeiro(g.altars.Where(A => A.state == "active" && A.localized).ToList(), (x, y) => Dist(b, x) - Dist(b, y));
            if (act != null)
            {
                if (InCircle(b, act))
                {
                    bool close = ai.target != null && Dist(b, ai.target) < 11 && b.hp > 45;
                    if (!close) { b.sealing = act.i; b.moving = false; return; }
                    ai.strafeT -= dt; if (ai.strafeT <= 0) { ai.strafe *= -1; ai.strafeT = Rand(.6, 1.4); }
                    var s1 = CircleSpot(act, b); SetGoal(g, b, s1.x + ai.strafe * .8, s1.z, "fight" + act.i + JS.Num(ai.strafe)); BotMove(g, b, dt, .7); return;
                }
                var s = CircleSpot(act, b); SetGoal(g, b, s.x, s.z, "go" + act.i); BotMove(g, b, dt, fighting ? .8 : 1.05); return;
            }
            var known = JS.Primeiro(g.altars.Where(A => A.state == "awake" && CONHECIDO.Contains(g.know[A.i])).ToList(), (x, y) => Dist(b, x) - Dist(b, y));
            if (known != null && !(g.know[known.i] == "chamariz" && Dist(b, known) > 12))
            {
                if (Dist(b, known) < 3) { b.moving = false; if (!fighting) BotHold(g, b, "purge" + known.i, CFG.purge, dt, new Ctx { type = "purge", A = known.i }); return; }
                var s = CircleSpot(known, b); SetGoal(g, b, s.x, s.z, "pg" + known.i); BotMove(g, b, dt, 1); return;
            }
            if (ai.alert != null && g.t - ai.alert.t < 6 && !fighting && !InEnemySpawn(b, ai.alert))
            {
                SetGoal(g, b, ai.alert.x, ai.alert.z, "al" + JS.Num(JS.Round(ai.alert.t))); BotMove(g, b, dt, 1); if (Dist(b, ai.alert) < 2) ai.alert = null; return;
            }
            if (b.cls == "rastreador" && !fighting)
            { // segue a pegada mais fresca por perto
                if (ai.rastro == null || g.t > ai.rastro.ate || Dist(b, ai.rastro) < 2)
                {
                    var p = g.pegadas.Where(q => g.t - q.t < 10 && JS.Hypot(q.x - b.x, q.z - b.z) < 30).LastOrDefault();
                    ai.rastro = p != null ? new RastroAlvo { x = p.x, z = p.z, ate = g.t + 2.5, k = "rs" + JS.Num(p.t) } : null;
                }
                if (ai.rastro != null && !InEnemySpawn(b, ai.rastro)) { SetGoal(g, b, ai.rastro.x, ai.rastro.z, ai.rastro.k); BotMove(g, b, dt, 1.05); return; }
            }
            var R = g.reagents.FirstOrDefault(r => r.has && Dist(b, r) < 6);
            if (R != null && !fighting && b.id % 2 == 0)
            {
                if (Dist(b, R) < 1.8) { b.moving = false; BotHold(g, b, "burn" + R.i, CFG.burn, dt, new Ctx { type = "burn", R = R.i }); return; }
                SetGoal(g, b, R.x, R.z, "b" + R.i); BotMove(g, b, dt, 1); return;
            }
            string chavePatrulha = ai.patrol.HasValue ? "pt" + ai.patrol.Value : "ptnull";
            bool chegouVigia = ai.goal != null && ai.goalKey == chavePatrulha && Dist(b, ai.goal) < 1.5, desviado = ai.goalKey == "desvio" && (ai.goal == null || Dist(b, ai.goal) < 1.5);
            if (desviado) ai.patrol = null;
            if (ai.patrol == null || g.t > ai.patrolT || Dist(b, g.altars[ai.patrol.Value]) < 6 || chegouVigia)
            {
                var cands = g.altars.Where(A => A.state != "fenda" && A.state != "farol" && g.know[A.i] != "chamariz").ToList();
                var mate = g.actors.FirstOrDefault(h => h != b && h.team == "H"); int? avoid = mate != null ? mate.ai.patrol : -1;
                bool early = g.rt < 60;
                JS.Sort(cands, (x, y) => (g.visit[x.i] - g.visit[y.i]) + (x.i == avoid ? 40 : 0) - (y.i == avoid ? 40 : 0) + (Dist(b, x) - Dist(b, y)) * (early ? 1.2 : .4));
                if (cands.Count > 0) { ai.patrol = cands[0].i; ai.patrolT = g.t + 25; }
            }
            if (ai.patrol != null)
            {
                var A = g.altars[ai.patrol.Value];
                if (ai.vigiaPt == null || ai.vigiaPt.i != A.i)
                {
                    Ponto pt = null; double a0 = b.id * 1.3;
                    for (int k = 0; k < 12 && pt == null; k++) { double an = a0 + k * Math.PI / 6; var q = Pt(A.x + JS.Cos(an) * 4.5, A.z + JS.Sin(an) * 4.5); if (Mapa.Livre(q, .5) && Mapa.SegClear(q.x, q.z, A.x, A.z, .2, Mapa.GT)) pt = q; }
                    var ref_ = pt ?? A; ai.vigiaPt = new VigiaPt { i = A.i, x = ref_.x, z = ref_.z };
                }
                SetGoal(g, b, ai.vigiaPt.x, ai.vigiaPt.z, "pt" + A.i); BotMove(g, b, dt, fighting ? .8 : 1);
            }
        }
        static void AiCult(Jogo g, Ator b, double dt)
        {
            bool fighting = BotCombat(g, b, dt); var ai = b.ai;
            if (b.feitico == "veu" && b.abilCd <= 0 && ai.target != null && Dist(b, ai.target) < 22 && b.hp < b.maxHp * .7) UseAbility(g, b);
            if (b.feitico == "empurrao" && b.abilCd <= 0)
            {
                var t = EnemiesOf(g, b).FirstOrDefault(e => e.st == "alive" && Dist(b, e) < 3.4 && (e.sealing >= 0 || ai.target == e));
                if (t != null) { b.yaw = JS.Atan2(-(t.x - b.x), -(t.z - b.z)); UseAbility(g, b); }
            }
            if (BotHelpDowned(g, b, dt, fighting)) return;
            if (!fighting && BotNoite(g, b, dt)) return;
            var act = JS.Primeiro(g.altars.Where(A => A.state == "active" && A.chosen).ToList(), (x, y) => Dist(b, x) - Dist(b, y));
            if (act != null)
            {
                if (b.feitico == "runa" && b.abilCd <= 0 && InCircle(b, act)) UseAbility(g, b);
                var s = CircleSpot(act, b); if (InCircle(b, act) && Dist(b, s) < 1.2) { b.moving = false; return; }
                SetGoal(g, b, s.x, s.z, "ch" + act.i); BotMove(g, b, dt, 1.05); return;
            }
            var rem = g.altars.Where(A => A.chosen && (A.state == "dormant" || A.state == "awake")).ToList();
            if (rem.Count == 0)
            { // sem altar para trabalhar: vai se abastecer no Claustro
                var R0 = JS.Primeiro(g.reagents.Where(r => r.has).ToList(), (x, y) => Dist(b, x) - Dist(b, y));
                if (R0 != null && b.reag < CFG.carry)
                {
                    if (Dist(b, R0) < 1.8) { b.moving = false; BotHold(g, b, "col" + R0.i, CFG.collect, dt, new Ctx { type = "collect", R = R0.i }); return; }
                    SetGoal(g, b, R0.x + .9, R0.z, "r" + JS.Num(R0.x) + "," + JS.Num(R0.z)); BotMove(g, b, dt, 1); return;
                }
                b.moving = false; ai.esperando = g.t; return;
            }
            JS.Sort(rem, (x, y) => { double v = ((y.state == "awake" && !y.decoy) ? 1 : 0) - ((x.state == "awake" && !x.decoy) ? 1 : 0); return v != 0 ? v : Dist(b, x) - Dist(b, y); });
            var T = rem[0]; if (ai.esperaAltar != T.i) { ai.esperaAltar = T.i; ai.wait = 0; }
            // Chamariz: só um bot da equipe cuida disso (o outro segue o ritual). O primeiro sai no Crepúsculo;
            // o segundo, se sobrar cinza, depois que um ritual se resolver. O prazo para chegar cresce com a distância.
            var dono = JS.Primeiro(g.actors.Where(c => c.team == "C" && !c.human && c.st != "dead").ToList(), (x, y) => x.id - y.id);
            bool querChamariz = dono == b && !ai.decoyUsed && g.decoys > 0 && g.rt > 25 && (ai.chamarizes == 0 || (g.lastResolveT > ai.chamarizT && g.rt - ai.chamarizT > 90));
            if (querChamariz)
            {
                if (ai.decoyAlvo == null || g.altars[ai.decoyAlvo.Value].chosen || g.altars[ai.decoyAlvo.Value].state != "dormant")
                {
                    var D0 = JS.Primeiro(g.altars.Where(A => !A.chosen && A.state == "dormant").ToList(), (x, y) => Dist(b, x) - Dist(b, y));
                    ai.decoyAlvo = D0 != null ? D0.i : (int?)null; ai.decoyT = 0; ai.decoyPrazo = D0 != null ? 20 + Dist(b, D0) / 2.2 : 0;
                }
                var D = ai.decoyAlvo != null ? g.altars[ai.decoyAlvo.Value] : null;
                if (D != null)
                {
                    if (Dist(b, D) < 3.3)
                    {
                        b.moving = false; BotHold(g, b, "dec" + D.i, CFG.decoy, dt, new Ctx { type = "decoy", A = D.i });
                        if (D.state == "awake") { ai.chamarizes = ai.chamarizes + 1; ai.chamarizT = g.rt; ai.decoyAlvo = null; if (ai.chamarizes >= 2) ai.decoyUsed = true; }
                        return;
                    }
                    var sd = CircleSpot(D, b); SetGoal(g, b, sd.x, sd.z, "dg" + D.i); BotMove(g, b, dt, 1);
                    ai.decoyT = ai.decoyT + dt; if (ai.decoyT > ai.decoyPrazo) { ai.decoyUsed = true; ai.decoyAlvo = null; LogE(g, "chamariz_desistiu", "who", b.name, "altar", D.name); } // rota ruim: desiste sem ficar preso
                    return;
                }
            }
            // A Transferência só é usada quando o altar escolhido já foi descoberto (ou a noite já está avançada).
            if (!g.transferUsed && b.reag > 0 && g.rt > 120)
            {
                var F = g.altars.FirstOrDefault(A => A.chosen && A.state == "dormant");
                var D = JS.Primeiro(g.altars.Where(A => !A.chosen && A.state == "dormant").ToList(), (x, y) => Dist(b, x) - Dist(b, y));
                bool descoberta = F != null && (g.know[F.i] == "desperto" || g.know[F.i] == "confirmado" || g.know[F.i] == "chamariz");
                bool emergencia = F != null && g.rt > 420;
                if (F != null && D != null && (descoberta || emergencia))
                {
                    if (Dist(b, D) < 3.4) { b.moving = false; BotHold(g, b, "tr" + D.i, CFG.transfer, dt, new Ctx { type = "transfer", A = D.i, from = F.i }); return; }
                    var sd = CircleSpot(D, b); SetGoal(g, b, sd.x, sd.z, "trg" + D.i); BotMove(g, b, dt, 1.02); return;
                }
            }
            bool partner = g.actors.Any(c => c != b && c.team == "C" && c.st == "alive" && c.reag > 0 && !c.human);
            bool want = b.reag == 0 || (b.reag < CFG.carry && (!partner || T.state == "dormant"));
            if (want)
            {
                var R = JS.Primeiro(g.reagents.Where(r => r.has && !ai.Ignorado("r" + JS.Num(r.x) + "," + JS.Num(r.z), g.t)).ToList(), (x, y) => Dist(b, x) - Dist(b, y));
                var P = JS.Primeiro(g.pickups.Where(p => p.kind != "ess" && !ai.Ignorado("r" + JS.Num(p.x) + "," + JS.Num(p.z), g.t)).ToList(), (x, y) => Dist(b, x) - Dist(b, y));
                Ponto tgt = (P != null && (R == null || Dist(b, P) < Dist(b, R))) ? (Ponto)P : R;
                bool comprometido = tgt != null && ai.goalKey == "r" + JS.Num(tgt.x) + "," + JS.Num(tgt.z); // já decidiu buscar este frasco
                if (tgt != null && (b.reag == 0 || Dist(b, tgt) < 14 || comprometido))
                {
                    if (tgt == R && Dist(b, R) < 1.8) { b.moving = false; if (!fighting || b.hold.t > 0) BotHold(g, b, "col" + R.i, CFG.collect, dt, new Ctx { type = "collect", R = R.i }); return; }
                    double ox = tgt == P ? 0 : .9; SetGoal(g, b, tgt.x + ox, tgt.z, "r" + JS.Num(tgt.x) + "," + JS.Num(tgt.z)); BotMove(g, b, dt, 1); return;
                }
            }
            if (b.reag > 0)
            {
                var s = CircleSpot(T, b);
                if (Dist(b, T) < 3.3)
                {
                    b.moving = false; ai.emperrado = 0;
                    if (T.state == "dormant") BotHold(g, b, "cons" + T.i, CFG.consecrate, dt, new Ctx { type = "consecrate", A = T.i });
                    else if (T.state == "awake" && !T.decoy && BloqueioRitual(g) != null) { b.moving = false; ai.esperando = g.t; }
                    else if (T.state == "awake" && !T.decoy)
                    {
                        var mate = g.actors.FirstOrDefault(c => c != b && c.team == "C" && c.st == "alive"); bool near = mate != null && Dist(mate, T) < 8;
                        ai.wait += dt; if (near || ai.wait > 6 || mate == null || b.hold.key == "start" + T.i) BotHold(g, b, "start" + T.i, CFG.start, dt, new Ctx { type = "start", A = T.i }); // uma vez decidido, segura até o fim
                    }
                    return;
                }
                SetGoal(g, b, s.x, s.z, "to" + T.i); BotMove(g, b, dt, 1);
                if (Dist(b, T) < 6) { ai.emperrado = ai.emperrado + dt; if (ai.emperrado > 6) { ai.emperrado = 0; ai.path = Mapa.FindPath(b, Pt(T.x, T.z)); } } else ai.emperrado = 0;
                return;
            }
            var s2 = CircleSpot(T, b); SetGoal(g, b, s2.x + 2, s2.z, "gd" + T.i); BotMove(g, b, dt, .8);
        }
    }
}
