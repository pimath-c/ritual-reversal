// A noite contada no fim (linha do tempo, prêmios e placar do index.html). O cliente anota a cada segundo o progresso e o
// selo de cada ritual, os momentos, o Grande Ritual e as próprias quedas, e a cada 2 s onde cada um estava (Rever a noite);
// no fim junta isso ao registro da partida (local: o do jogo; online: o arquivo que o servidor libera no fim): a verdade da
// noite, inclusive o que os Caçadores não viram na hora. Sem IMGUI: as telas ficam em PartidaNoite.cs, e o teste
// (tools/unity-noite.js) roda tudo isto no Mono com uma noite de bots.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public class AmostraLT { public double rt; public float[][] a; }          // por altar: {progresso, selo} ou null
    public class MarcoLT { public double rt; public string tipo, txt; public int i = -1; }
    public class PosLT { public double rt; public float[][] p; }              // por ator: {x, z, caído} ou null
    public class QuedaLT { public double rt; public float x, z; public string nome, team; }
    public class AtorLT { public int id; public string nome, team; public bool eu; }
    public class RodadaLT
    {
        public List<AmostraLT> am = new List<AmostraLT>(); public List<MarcoLT> mk = new List<MarcoLT>(), mkFinal; public string[] est; public bool grande; public double ultimo = -9;
        public string cls, papel, nome;
        public List<PosLT> pos; public double posUlt = -9; public List<QuedaLT> quedas; public Dictionary<int, string> stAnt; public List<AtorLT> atores;
    }

    public class LinhaDoTempo
    {
        public string id; public readonly Dictionary<int, RodadaLT> rod = new Dictionary<int, RodadaLT>();
        public RodadaLT Rodada(Jogo g)
        {
            string mid = g.matchId ?? ""; if (id != mid) { id = mid; rod.Clear(); }
            int r = g.round > 0 ? g.round : 1; RodadaLT R;
            if (!rod.TryGetValue(r, out R)) rod[r] = R = new RodadaLT { est = g.altars.Select(A => A.state).ToArray() };
            return R;
        }
        public void Marco(Jogo g, string tipo, string txt) { if (g == null || g.phase != "play") return; Rodada(g).mk.Add(new MarcoLT { rt = g.rt, tipo = tipo, txt = txt }); }
        public void Amostrar(Jogo g, string meuCid)
        {
            if (g == null || g.phase != "play" || g.altars == null) return; var R = Rodada(g);
            var eu = g.actors.Find(a => a.cid == meuCid); if (eu != null) { R.cls = eu.cls; R.papel = eu.team; R.nome = eu.name; }
            for (int i = 0; i < g.altars.Count; i++)
            {
                var A = g.altars[i];
                if (i < R.est.Length && A.state != R.est[i])
                {
                    R.est[i] = A.state;
                    if (A.state == "active") R.mk.Add(new MarcoLT { rt = g.rt, tipo = "inicio", txt = "Ritual começou em " + A.name, i = i });
                    else if (A.state == "fenda") R.mk.Add(new MarcoLT { rt = g.rt, tipo = "fenda", txt = "Fenda aberta em " + A.name, i = i });
                    else if (A.state == "farol") R.mk.Add(new MarcoLT { rt = g.rt, tipo = "farol", txt = "Ritual selado em " + A.name + ": Farol", i = i });
                }
                if (A.grande && A.state == "active" && !R.grande) { R.grande = true; R.mk.Add(new MarcoLT { rt = g.rt, tipo = "grande", txt = "Grande Ritual em " + A.name, i = i }); }
            }
            AmostrarPosicoes(R, g, meuCid);
            if (g.rt - R.ultimo < 1 && R.am.Count > 0) return; R.ultimo = g.rt;
            R.am.Add(new AmostraLT { rt = g.rt, a = g.altars.Select(A => A.state == "active" ? new[] { R3(A.prog), R3(A.seal) } : null).ToArray() });
        }
        static float R3(double v) { return (float)(System.Math.Round(v * 1000) / 1000); }
        static float R1(double v) { return (float)(System.Math.Round(v * 10) / 10); }
        // a planta da noite inteira: posições a cada 2 s (local: todos; online: só quem você via), quedas e altares
        static void AmostrarPosicoes(RodadaLT R, Jogo g, string meuCid)
        {
            if (R.pos == null) { R.pos = new List<PosLT>(); R.posUlt = -9; R.quedas = new List<QuedaLT>(); R.stAnt = new Dictionary<int, string>(); R.atores = g.actors.Select(a => new AtorLT { id = a.id, nome = a.name, team = a.team, eu = a.cid == meuCid }).ToList(); }
            foreach (var a in g.actors)
            {
                string s; bool tinha = R.stAnt.TryGetValue(a.id, out s);
                if (a.st != s) { R.stAnt[a.id] = a.st; if (a.st == "down" && tinha && s != null && !a.hidden) R.quedas.Add(new QuedaLT { rt = g.rt, x = R1(a.x), z = R1(a.z), nome = a.name, team = a.team }); }
            }
            if (g.rt - R.posUlt < 2) return; R.posUlt = g.rt;
            R.pos.Add(new PosLT { rt = g.rt, p = R.atores.Select(o => { var a = g.actors.Find(b => b.id == o.id); return a != null && !a.hidden && a.st != "dead" ? new[] { R1(a.x), R1(a.z), a.st == "down" ? 1f : 0f } : null; }).ToArray() });
        }
        public bool TemReplay(int r) { RodadaLT R; return rod.TryGetValue(r, out R) && R.pos != null && R.pos.Count > 3; }
    }

    public static class Noite
    {
        public const string VERSAO = "6.1"; public const int REGISTRO = 3;   // os do sim.js (Sim.VERSAO, Sim.REGISTRO), para o arquivo do registro
        public static string Relogio(double s) { s = System.Math.Max(0, s); int m = (int)(s / 60), ss = (int)(s % 60); return m + ":" + ss.ToString("00"); }
        public static string ListaNomes(List<string> L) { return L.Count < 2 ? (L.Count == 1 ? L[0] : "") : string.Join(", ", L.GetRange(0, L.Count - 1).ToArray()) + " e " + L[L.Count - 1]; }
        public static List<string> Nomes(object v) { var L = new List<string>(); var en = v as System.Collections.IEnumerable; if (en != null && !(v is string)) foreach (var x in en) if (x != null) L.Add(x.ToString()); return L; }
        static string S(Registro e, string k) { return e[k] as string; }
        static double N(Registro e, string k) { var v = e[k]; return v == null || v is string ? 0 : System.Convert.ToDouble(v); }

        // ---------- lances (v5.7): os textos do LANCE_TXT ----------
        static readonly HashSet<string> CULTO = new HashSet<string> { "defesa", "transferencia", "chamariz" };
        public static bool LanceDoCulto(string tipo, string team) { return CULTO.Contains(tipo) || (tipo == "resgate" && team == "C"); }
        public static bool LanceFrase(string tipo, List<string> q, string alt, double v, out string tit, out string sub)
        {
            string quem = ListaNomes(q), pct = Mathf.RoundToInt((float)v * 100) + "%"; alt = alt ?? "";
            switch (tipo)
            {
                case "selo_limite": tit = "Selo no último instante"; sub = quem + (q.Count > 1 ? " selaram" : " selou") + " com o ritual em " + pct; return true;
                case "achou_cedo": tit = "Achou antes do coro"; sub = quem + " encontrou o ritual" + (alt != "" ? " em " + alt : "") + " com " + pct; return true;
                case "defesa": tit = "Defesa até o fim"; sub = quem + (q.Count > 1 ? " seguraram" : " segurou") + " o ritual" + (alt != "" ? " em " + alt : "") + " com o selo em " + pct; return true;
                case "resgate": tit = "Resgate"; sub = q.Count > 1 ? q[0] + " impediu a execução e " + q[1] + " reanimou" : (q.Count == 1 ? q[0] : "") + " impediu a execução e reanimou"; return true;
                case "transferencia": tit = "A Transferência deu certo"; sub = quem + " levou o ritual" + (alt != "" ? " para " + alt : "") + ", e ele virou Fenda"; return true;
                case "chamariz": tit = "O Chamariz funcionou"; sub = "O Chamariz de " + quem + (alt != "" ? " em " + alt : "") + " prendeu a caça por " + Mathf.RoundToInt((float)v) + " s"; return true;
            }
            tit = sub = null; return false;
        }
        public class LanceR { public double t; public string tit, sub; public bool culto; public List<string> quem; }
        public static List<LanceR> Lances(List<Registro> log)
        {
            var L = new List<LanceR>(); if (log == null) return L;
            foreach (var e in log)
            {
                if (e.ev != "lance") continue; string tit, sub; var q = Nomes(e["quem"]);
                if (LanceFrase(S(e, "tipo"), q, S(e, "altar"), N(e, "v"), out tit, out sub)) L.Add(new LanceR { t = e.t, tit = tit, sub = sub, culto = LanceDoCulto(S(e, "tipo"), S(e, "team")), quem = q });
            }
            return L;
        }

        // ---------- marcos da linha do tempo tirados do registro ----------
        public static List<MarcoLT> MarcosDoLog(List<Registro> log, int r)
        {
            var o = new List<MarcoLT>();
            foreach (var e in log)
            {
                if (e.r != r) continue; string by = S(e, "by"), who = S(e, "who"), alt = S(e, "altar");
                switch (e.ev)
                {
                    case "ritual_start": o.Add(new MarcoLT { rt = e.t, tipo = "inicio", txt = by + " iniciou o ritual em " + alt }); break;
                    case "ritual_complete": o.Add(new MarcoLT { rt = e.t, tipo = "fenda", txt = "Fenda aberta em " + alt }); break;
                    case "sealed": o.Add(new MarcoLT { rt = e.t, tipo = "farol", txt = "Ritual selado em " + alt + ": Farol" }); break;
                    case "down": o.Add(new MarcoLT { rt = e.t, tipo = "queda", txt = by + " derrubou " + who }); break;
                    case "revive": o.Add(new MarcoLT { rt = e.t, tipo = "reanima", txt = by + " reanimou " + who }); break;
                    case "transfer": o.Add(new MarcoLT { rt = e.t, tipo = "inicio", txt = by + " transferiu o ritual de " + S(e, "from") + " para " + S(e, "to") }); break;
                    case "eco": { string st = S(e, "setor"); var Sd = Sim.SETORES.FirstOrDefault(x => x.id == st); o.Add(new MarcoLT { rt = e.t, tipo = "eco", txt = "O coro ecoou " + (Sd != null ? Sd.nome : st) + " (era " + alt + ")" }); break; }
                    case "localized": o.Add(new MarcoLT { rt = e.t, tipo = "eco", txt = "A caça localizou o ritual em " + alt }); break;
                    case "execucao_interrompida": o.Add(new MarcoLT { rt = e.t, tipo = "reanima", txt = by + " interrompeu a execução de " + who }); break;
                    case "interrupcao": o.Add(new MarcoLT { rt = e.t, tipo = "queda", txt = by + " interrompeu " + (S(e, "tipo") == "selo" ? "o selo" : "a canalização") + " de " + who + " em " + alt }); break;
                    case "lance": { string tit, sub; if (LanceFrase(S(e, "tipo"), Nomes(e["quem"]), alt, N(e, "v"), out tit, out sub)) o.Add(new MarcoLT { rt = e.t, tipo = "lance", txt = tit + ": " + sub }); break; }
                }
            }
            return o;
        }
        // os marcos que o gráfico mostra: com o registro, os momentos e o Grande Ritual anotados mais os fatos do registro
        public static List<MarcoLT> MarcosFinais(RodadaLT R, List<Registro> log, int r)
        {
            if (log == null || log.Count == 0) return R.mkFinal = R.mk;
            return R.mkFinal = R.mk.Where(m => m.tipo == "momento" || m.tipo == "grande").Concat(MarcosDoLog(log, r)).OrderBy(m => m.rt).ToList();
        }
        public static double FimMomento(Lab lab, int i) { return i == 0 ? (lab != null ? lab.crepusculo : 180) : CFG.momentoAte[i]; }
        public static readonly string[] COR_ALT = { "#e0435e", "#b98cff", "#ff9a5a", "#5ad1c8", "#7fb2ff", "#ff6fb0" };
        public const float GW = 640, GH = 150;
        public static double DuracaoGrafico(RodadaLT R) { return System.Math.Max(CFG.roundTime > 0 ? CFG.roundTime : 1080, R.am.Count > 0 ? R.am[R.am.Count - 1].rt : 0); }
        public static float GX(double t, double T) { return (float)(t / T * GW); }
        public static float GY(double v) { return (float)(GH - 8 - v * (GH - 30)); }
        // o fundo do gráfico (graficoNoite): caixa, divisas dos momentos, progresso de cada ritual e o selamento tracejado
        public static Texture2D Grafico(RodadaLT R, Lab lab, int nAltares)
        {
            double T = DuracaoGrafico(R); var D = new Desenho(GW, GH, 2, new Color(0, 0, 0, 0));
            D.Ret(0, 0, GW, GH, new Color(10 / 255f, 12 / 255f, 15 / 255f, .6f)); D.RetBorda(.5f, .5f, GW - 1, GH - 1, 1, Desenho.Hex("#333a42"));
            for (int i = 0; i < CFG.momentoAte.Length; i++) { double ate = FimMomento(lab, i); if (ate < T) { float x = GX(ate, T); D.Polilinha(new[] { new Vector2(x, 0), new Vector2(x, GH) }, 1, Desenho.Hex("#4a525c"), 3, 4); } }
            for (int i = 0; i < nAltares; i++)
            {
                var linhas = new List<List<Vector2>>(); var selos = new List<List<Vector2>>(); List<Vector2> seg = new List<Vector2>(), sel = new List<Vector2>();
                foreach (var q in R.am)
                {
                    var v = i < q.a.Length ? q.a[i] : null;
                    if (v != null) { seg.Add(new Vector2(GX(q.rt, T), GY(v[0]))); sel.Add(new Vector2(GX(q.rt, T), GY(v[1]))); }
                    else { if (seg.Count > 0) { linhas.Add(seg); selos.Add(sel); } seg = new List<Vector2>(); sel = new List<Vector2>(); }
                }
                if (seg.Count > 0) { linhas.Add(seg); selos.Add(sel); }
                foreach (var L in linhas) { if (L.Count == 1) D.Circulo(L[0].x, L[0].y, 1.1f, Desenho.Hex(COR_ALT[i % 6])); else D.Polilinha(L, 2.2f, Desenho.Hex(COR_ALT[i % 6])); }
                foreach (var L in selos) if (L.Count > 1 && L.Any(p => p.y < GH - 9)) D.Polilinha(L, 1.6f, Desenho.Hex("#f3d58e"), 4, 3);
            }
            return D.Textura();
        }
        // ícones dos marcos (IC do graficoNoite)
        public static readonly Dictionary<string, string[]> IC = new Dictionary<string, string[]> {
            {"inicio",new[]{"▲","#e0435e"}},{"fenda",new[]{"✕","#e0435e"}},{"farol",new[]{"✓","#f3d58e"}},{"grande",new[]{"✶","#ff5a6e"}},{"momento",new[]{"◆","#8f877b"}},
            {"eco",new[]{"◎","#d6b8f0"}},{"queda",new[]{"✚","#c9d3dc"}},{"morte",new[]{"☠","#c9d3dc"}},{"reanima",new[]{"♥","#8fd0a0"}},{"lance",new[]{"★","#f3d58e"}} };

        // ---------- destaques da noite (PREMIOS) ----------
        public class Premio { public string nome, quem, verbo, um, varios; public int n; }
        static readonly string[][] PREMIOS = {
            new[]{"down","by","Mão Pesada","derrubou","inimigo","inimigos"}, new[]{"revive","by","Anjo da Guarda","reanimou","aliado","aliados"}, new[]{"purge","by","Purificador","purgou","altar","altares"},
            new[]{"ritual_start","by","Sumo Sacerdote","iniciou","ritual","rituais"}, new[]{"consecrate","by","Consagrador","consagrou","altar","altares"}, new[]{"ponto","by","Mãos Ocupadas","cumpriu","tarefa","tarefas"},
            new[]{"decoy","by","Ilusionista","plantou","chamariz","chamarizes"}, new[]{"transfer","by","Andarilho","transferiu","ritual","rituais"}, new[]{"push_seal","by","Muralha","empurrou","selador","seladores"},
            new[]{"rastro_lido","by","Farejador","leu","rastro","rastros"}, new[]{"compra","who","Freguês","fez","compra","compras"} };
        public static List<Premio> Premios(List<Registro> log)
        {
            var o = new List<Premio>(); if (log == null || log.Count == 0) return o;
            foreach (var P in PREMIOS)
            {
                var nomes = new List<string>(); var n = new Dictionary<string, int>();
                foreach (var e in log) { string q = e.ev == P[0] ? S(e, P[1]) : null; if (string.IsNullOrEmpty(q)) continue; if (!n.ContainsKey(q)) { n[q] = 0; nomes.Add(q); } n[q]++; }
                string top = null; foreach (var q in nomes) if (top == null || n[q] > n[top]) top = q;
                if (top != null) o.Add(new Premio { nome = P[2], quem = top, n = n[top], verbo = P[3], um = P[4], varios = P[5] });
            }
            return o.OrderByDescending(p => p.n).Take(6).ToList();
        }

        // ---------- placar de cada jogador, uma tabela por rodada (placarJogadores) ----------
        public class LinhaPlacar { public string nome, team, cls, classe; public bool humano; public int[] v; }
        public class PlacarRodada { public int r; public List<LinhaPlacar> linhas = new List<LinhaPlacar>(); }
        public static List<PlacarRodada> Placar(List<Registro> log)
        {
            var o = new List<PlacarRodada>(); if (log == null || log.Count == 0) return o;
            var rods = new List<int>(); foreach (var e in log) if (e.ev == "round_start" && !rods.Contains(e.r)) rods.Add(e.r);
            foreach (int r in rods)
            {
                var ini = log.First(e => e.ev == "round_start" && e.r == r); var teams = ini["teams"] as System.Collections.IEnumerable; if (teams == null) continue;
                var L = log.Where(e => e.r == r).ToList(); var P = new PlacarRodada { r = r };
                System.Func<System.Func<Registro, bool>, int> n = f => L.Count(f);
                foreach (var t in teams)
                {
                    var d = t as Dictionary<string, object>; if (d == null) continue; object ov;
                    string nome = d.TryGetValue("name", out ov) ? ov as string : "", team = d.TryGetValue("team", out ov) ? ov as string : "", cls = d.TryGetValue("cls", out ov) ? ov as string : "";
                    bool humano = d.TryGetValue("human", out ov) && ov is bool && (bool)ov; ClasseDef C; Regras.CLASSES.TryGetValue(cls ?? "", out C);
                    int alt = team == "H" ? n(e => e.ev == "purge" && S(e, "by") == nome) : n(e => (e.ev == "consecrate" || e.ev == "ritual_start" || e.ev == "decoy" || e.ev == "transfer") && S(e, "by") == nome);
                    P.linhas.Add(new LinhaPlacar { nome = nome, team = team, cls = cls, classe = C != null ? C.name : cls, humano = humano,
                        v = new[] { n(e => e.ev == "down" && S(e, "by") == nome), n(e => e.ev == "down" && S(e, "who") == nome), n(e => e.ev == "revive" && S(e, "by") == nome), alt, n(e => e.ev == "ponto" && S(e, "by") == nome), n(e => e.ev == "compra" && S(e, "who") == nome) } });
                }
                P.linhas = P.linhas.OrderBy(x => x.team == "H" ? 0 : 1).ToList(); o.Add(P);
            }
            return o;
        }

        // ---------- relatório do playtest (Sim.relatorio do sim.js, a mesma estrutura de saída) ----------
        static readonly string[] INFO_CACA = { "eco", "pista_info", "evp", "sentinela", "lanterna", "decoy_spotted", "rastro_visto", "rastro_lido", "localized", "sal" };
        static double R2(double v) { return JS.R2(v); }
        static bool Verd(object v) { if (v == null) return false; if (v is bool) return (bool)v; if (v is string) return ((string)v).Length > 0; try { double d = System.Convert.ToDouble(v); return d != 0 && !double.IsNaN(d); } catch { return true; } }
        static double Num(object v) { if (v == null || v is string || v is bool) return 0; try { return System.Convert.ToDouble(v); } catch { return 0; } }
        static bool Maior(object v, double lim) { return v != null && !(v is string) && !(v is bool) && Num(v) > lim; }
        static Dictionary<string, object> ResumoDisputa(List<Registro> L)
        {
            if (L.Count == 0) return null;
            System.Func<string, Dictionary<string, object>> soma = team =>
            {
                var o = new Dictionary<string, object> { { "jogadores", 0 } };
                foreach (var e in L) if (S(e, "team") == team)
                    {
                        o["jogadores"] = (int)o["jogadores"] + 1;
                        foreach (var k in new[] { "perto", "circulo", "selando", "sobFogo", "canal", "atacando", "dano", "sofrido", "quedas", "derrubou" }) { object a; o[k] = R2((o.TryGetValue(k, out a) ? Num(a) : 0) + Num(e[k])); }
                    }
                return o;
            };
            return new Dictionary<string, object> { { "caca", soma("H") }, { "culto", soma("C") } };
        }
        static Dictionary<string, object> Contagem(IEnumerable<string> chaves) { var o = new Dictionary<string, object>(); foreach (var k in chaves) { var kk = k ?? "undefined"; object v; o[kk] = (o.TryGetValue(kk, out v) ? (int)v : 0) + 1; } return o; }
        public static Dictionary<string, object> Relatorio(List<Registro> log, int r)
        {
            if (r == 0) r = 1; var L = (log ?? new List<Registro>()).Where(e => (e.r == 0 ? 1 : e.r) == r).ToList();
            System.Func<string, List<Registro>> c = k => L.Where(e => e.ev == k).ToList(); var fim = c("round_end").FirstOrDefault(); double T = fim != null ? fim.t : (L.Count > 0 ? L[L.Count - 1].t : 0);
            var verdade = L.Where(e => (e.ev == "evp" && Verd(e["real"])) || e.ev == "sentinela" || e.ev == "lanterna" || e.ev == "rastro_lido" || e.ev == "localized").ToList();
            var info = L.Where(e => INFO_CACA.Contains(e.ev) || (e.ev == "sensor" && Maior(e["v"], .02))).Select(e => e.t).OrderBy(t => t).ToList();
            double maior = 0, soma = 0, ant = 0; int n = 0; foreach (var t in info.Concat(new[] { T })) { double d = t - ant; if (d > maior) maior = d; soma += d; n++; ant = t; }
            var rituais = new List<object>();
            foreach (var s in c("ritual_start"))
            {
                string alt = S(s, "altar");
                var fimR = L.FirstOrDefault(e => (e.ev == "sealed" || e.ev == "ritual_complete") && S(e, "altar") == alt && e.t >= s.t);
                var loc = L.FirstOrDefault(e => e.ev == "localized" && S(e, "altar") == alt && e.t >= s.t); var cheg = L.FirstOrDefault(e => e.ev == "hunter_arrive" && S(e, "altar") == alt && e.t >= s.t);
                int sel = L.Count(e => e.ev == "seal_start" && S(e, "altar") == alt && e.t >= s.t && (fimR == null || e.t <= fimR.t));
                var no = L.Where(e => e.t >= s.t && (fimR == null || e.t <= fimR.t)).ToList(); string resultado = fimR != null ? (fimR.ev == "sealed" ? "farol" : "fenda") : "aberto";
                bool perto = no.Any(e => e.ev == "caca_perto" && S(e, "altar") == alt); var errados = no.Where(e => e.ev == "altar_errado" && S(e, "ritual") == alt).ToList();
                string caca = resultado == "farol" ? "selou" : resultado == "aberto" ? "aberto" : cheg != null ? (Num(cheg["remaining"]) < CFG.sealMin ? "chegou_tarde" : "perdeu_disputa") : perto ? "perto_sem_entrar" : errados.Count > 0 ? "altar_errado" : "nunca_chegou";
                rituais.Add(new Dictionary<string, object> {
                    { "altar", alt }, { "inicio", s.t }, { "localizado", loc != null ? (object)R2(loc.t - s.t) : null }, { "comoLocalizou", loc != null ? loc["how"] : null }, { "chegada", cheg != null ? (object)R2(cheg.t - s.t) : null },
                    { "restanteNaChegada", cheg != null ? cheg["remaining"] : null }, { "tentativasSelo", sel }, { "resultado", resultado }, { "caca", caca }, { "altaresErrados", errados.Select(e => e["altar"]).ToList() },
                    { "interrupcoesSelo", no.Where(e => e.ev == "seal_stop" && S(e, "altar") == alt).Select(e => e["cause"]).ToList() }, { "cultoSaiu", no.Where(e => e.ev == "canal_saiu" && S(e, "altar") == alt).Select(e => e["motivo"]).ToList() },
                    { "duracao", fimR != null ? (object)R2(fimR.t - s.t) : null }, { "localizarAoFim", loc != null && fimR != null ? (object)R2(fimR.t - loc.t) : null },
                    { "disputa", ResumoDisputa(no.Where(e => e.ev == "disputa" && S(e, "altar") == alt && fimR != null && e.t == fimR.t).ToList()) },
                    { "interrupcoes", new Dictionary<string, object> { { "selo", no.Count(e => e.ev == "interrupcao" && S(e, "altar") == alt && S(e, "tipo") == "selo") }, { "canal", no.Count(e => e.ev == "interrupcao" && S(e, "altar") == alt && S(e, "tipo") == "canal") } } } });
            }
            var mortes = c("death"); var purgas = c("purge");
            return new Dictionary<string, object> {
                { "rodada", r }, { "duracao", T }, { "fendas", c("ritual_complete").Count }, { "farois", c("sealed").Count },
                { "investigacao", new Dictionary<string, object> { { "primeiraVerdade", verdade.Count > 0 ? (object)verdade[0].t : null }, { "comoDescobriu", verdade.Count > 0 ? verdade[0].ev : null }, { "pistasNovas", info.Count }, { "maiorIntervaloSemPista", R2(maior) }, { "intervaloMedioSemPista", n > 0 ? (object)R2(soma / n) : null } } },
                { "ferramentas", new Dictionary<string, object> { { "sinaisLidos", c("pista_info").Count }, { "sentinelasAcesas", L.Count(e => e.ev == "ponto" && S(e, "tipo") == "sentinela") }, { "avisosDeSentinela", c("sentinela").Count },
                    { "sensor", c("sensor").Count }, { "sensorComSinal", c("sensor").Count(e => Maior(e["v"], .02)) }, { "evp", c("evp").Count }, { "lanterna", c("lanterna").Count + c("decoy_spotted").Count },
                    { "sal", c("sal").Count }, { "rastrosVistos", c("rastro_visto").Count }, { "rastrosLidos", c("rastro_lido").Count }, { "pings", c("ping").Count }, { "frases", c("fala").Count }, { "feiticos", Contagem(c("ability").Select(e => S(e, "feitico"))) } } },
                { "engano", new Dictionary<string, object> { { "chamarizes", c("decoy").Count }, { "chamarizesDesmascarados", c("decoy_spotted").Count }, { "chamarizesQueimados", purgas.Count(e => Verd(e["decoy"])) },
                    { "purgasDeAltarVerdadeiro", purgas.Count(e => !Verd(e["decoy"])) }, { "evpEmChamariz", c("evp").Count(e => !Verd(e["real"])) }, { "transferencias", c("transfer").Count },
                    { "segundosPresosEmChamariz", R2(purgas.Sum(e => Verd(e["decoy"]) && Verd(e["isca"]) ? Num(e["isca"]) : 0)) } } },
                { "rituais", rituais }, { "conversao", Contagem(rituais.Select(x => (string)((Dictionary<string, object>)x)["caca"])) },
                { "lances", c("lance").Select(e => (object)new Dictionary<string, object> { { "t", e.t }, { "tipo", e["tipo"] }, { "quem", e["quem"] ?? new List<object>() }, { "altar", e["altar"] }, { "team", e["team"] }, { "v", e["v"] } }).ToList() },
                { "selamento", new Dictionary<string, object> { { "tentativas", c("seal_start").Count }, { "interrupcoes", Contagem(c("seal_stop").Select(e => S(e, "cause"))) }, { "selados", c("sealed").Count } } },
                { "combate", new Dictionary<string, object> { { "primeiraQueda", c("down").Count > 0 ? (object)c("down")[0].t : null }, { "quedas", c("down").Count }, { "execucoes", mortes.Count(e => S(e, "how") == "exec") }, { "sangraram", mortes.Count(e => S(e, "how") == "bleed") }, { "reanimacoes", c("revive").Count } } },
                { "sessao", new Dictionary<string, object> { { "saidas", c("saiu").Count }, { "reconexoes", c("reconnect").Count }, { "botsReiniciados", c("bot_reiniciado").Count } } } };
        }
        // JSON de valores simples, listas e dicionários (o registro baixado)
        public static string ParaJson(object v)
        {
            var C = System.Globalization.CultureInfo.InvariantCulture;
            if (v == null) return "null"; var st = v as string; if (st != null) { var sb = new System.Text.StringBuilder("\""); foreach (char c in st) { if (c == '"' || c == '\\') sb.Append('\\'); sb.Append(c < ' ' ? ' ' : c); } return sb.Append('"').ToString(); }
            if (v is bool) return (bool)v ? "true" : "false";
            if (v is double) { double d = (double)v; return double.IsNaN(d) || double.IsInfinity(d) ? "0" : d.ToString("R", C); }
            if (v is float) return ((double)(float)v).ToString("R", C); if (v is int || v is long) return System.Convert.ToString(v, C);
            var dic = v as System.Collections.IDictionary; if (dic != null) { var L = new List<string>(); foreach (System.Collections.DictionaryEntry kv in dic) L.Add(ParaJson(kv.Key.ToString()) + ":" + ParaJson(kv.Value)); return "{" + string.Join(",", L.ToArray()) + "}"; }
            var en = v as System.Collections.IEnumerable; if (en != null) { var L = new List<string>(); foreach (var x in en) L.Add(ParaJson(x)); return "[" + string.Join(",", L.ToArray()) + "]"; }
            return System.Convert.ToString(v, C);
        }
        // o registro baixado do servidor ({events:[{t, r, ev, ...}]}) no mesmo formato do registro local
        public static List<Registro> RegistroDoJson(string json)
        {
            var L = new List<Registro>(); var R = Json.Ler(json) as Dictionary<string, object>; object evs; if (R == null || !R.TryGetValue("events", out evs)) return L;
            foreach (var o in (evs as List<object>) ?? new List<object>())
            {
                var d = o as Dictionary<string, object>; if (d == null) continue; var e = new Registro(); object v;
                foreach (var kv in d) { if (kv.Key == "t") e.t = System.Convert.ToDouble(kv.Value); else if (kv.Key == "r") e.r = System.Convert.ToInt32(kv.Value); else if (kv.Key == "ev") e.ev = kv.Value as string; else e.d[kv.Key] = kv.Value; }
                if (e.ev != null && d.TryGetValue("ev", out v)) L.Add(e);
            }
            return L;
        }
    }
}
