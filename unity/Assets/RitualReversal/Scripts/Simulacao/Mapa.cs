// Mapa da simulação: caixas de colisão, grade espacial, linha de visão, tiros contra paredes e navegação (A*).
// Porte da parte "MAPA" e "NAVEGAÇÃO" de shared/sim.js. A planta vem de Resources/simulacao.json, gerado por
// tools/exportar-mapa.js a partir do próprio sim.js: o mapa continua tendo uma fonte só.
using System;
using System.Collections.Generic;

namespace RitualReversal.Simulacao
{
    public class Ponto { public double x, z; public Ponto() { } public Ponto(double x, double z) { this.x = x; this.z = z; } }

    public class Caixa { public double x1, x2, z1, z2, h; public bool tall; }

    public class AltarDef { public string name; public double x, z; }
    public class SpawnDef { public double x, z, yaw; }
    public class PortalDef { public string nome, eixo; public double fixo, a, b; public bool brecha; }
    public class Npc : Ponto { public string id, nome, time; public List<string> vende = new List<string>(); }
    public class Segmento { public double ax, az, bx, bz, w; public bool estreita; }
    public class Clareira { public double x, z, r; }
    public class NoNav { public double x, z; public List<int> viz = new List<int>(); public List<double> custo = new List<double>(); }

    // grade espacial de 6 m: cada checagem olha só as caixas das células por onde passa
    public class Grade
    {
        public double c = 6; public Dictionary<int, List<int>> m = new Dictionary<int, List<int>>();
        public uint[] marca; public uint vez; public List<Caixa> list;
        public Grade(List<Caixa> lista)
        {
            list = lista; marca = new uint[lista.Count];
            for (int i = 0; i < lista.Count; i++)
            {
                var b = lista[i];
                for (int cx = JS.Floor(b.x1 / c); cx <= JS.Floor(b.x2 / c); cx++)
                    for (int cz = JS.Floor(b.z1 / c); cz <= JS.Floor(b.z2 / c); cz++)
                    {
                        int k = cx * 1000 + cz; List<int> a; if (!m.TryGetValue(k, out a)) { a = new List<int>(); m[k] = a; }
                        a.Add(i);
                    }
            }
        }
    }

    public static class Mapa
    {
        public static double HX, HZ; // HALF
        public static double CAT_X1, CAT_X2, CAT_Z1, CAT_Z2, CL_X1, CL_X2, CL_Z1, CL_Z2;
        public static List<Caixa> BOX = new List<Caixa>(), TALL = new List<Caixa>();
        public static Grade GB, GT;
        public static List<AltarDef> ALTARS = new List<AltarDef>();
        public static Dictionary<string, SpawnDef> SPAWN = new Dictionary<string, SpawnDef>();
        public static List<Ponto> REAG = new List<Ponto>(), PORTAS = new List<Ponto>();
        public static List<PortalDef> PORTAIS = new List<PortalDef>();
        public static List<Ponto> PISTA_PTS = new List<Ponto>(), ERVA_PTS = new List<Ponto>(), SENTINELA_PTS = new List<Ponto>();
        public static Dictionary<string, List<Ponto>> PONTOS_DEF = new Dictionary<string, List<Ponto>>();
        public static List<Npc> NPCS = new List<Npc>();
        public static List<Ponto> CANDLES = new List<Ponto>();
        public static List<Clareira> CLAREIRAS = new List<Clareira>();
        public static List<Segmento> SEG_TRILHA = new List<Segmento>();
        public static List<NoNav> WP = new List<NoNav>();
        public static int navNos, navArestas, navPrincipalJS; // conferência exportada pelo JS
        public static bool Carregado;

        // ---------- carga ----------
        public static void Carregar(string json)
        {
            var R = Json.O(Json.Ler(json));
            var half = Json.O(R["half"]); HX = Json.D(half["x"]); HZ = Json.D(half["z"]);
            var cat = Json.O(R["catedral"]); CAT_X1 = Json.D(cat["x1"]); CAT_X2 = Json.D(cat["x2"]); CAT_Z1 = Json.D(cat["z1"]); CAT_Z2 = Json.D(cat["z2"]);
            var cl = Json.O(R["claustro"]); CL_X1 = Json.D(cl["x1"]); CL_X2 = Json.D(cl["x2"]); CL_Z1 = Json.D(cl["z1"]); CL_Z2 = Json.D(cl["z2"]);
            BOX.Clear(); TALL.Clear();
            foreach (var o in Json.L(R["caixas"]))
            {
                var v = Json.L(o);
                var b = new Caixa { x1 = Json.D(v[0]), x2 = Json.D(v[1]), z1 = Json.D(v[2]), z2 = Json.D(v[3]), h = Json.D(v[4]), tall = Json.D(v[5]) != 0 };
                BOX.Add(b); if (b.tall) TALL.Add(b);
            }
            GB = new Grade(BOX); GT = new Grade(TALL);
            ALTARS.Clear(); foreach (var o in Json.L(R["altares"])) { var a = Json.O(o); ALTARS.Add(new AltarDef { name = Json.S(a["nome"]), x = Json.D(a["x"]), z = Json.D(a["z"]) }); }
            SPAWN.Clear(); foreach (var kv in Json.O(R["spawns"])) { var s = Json.O(kv.Value); SPAWN[kv.Key] = new SpawnDef { x = Json.D(s["x"]), z = Json.D(s["z"]), yaw = Json.D(s["yaw"]) }; }
            Pares(R["reagentes"], REAG); Pares(R["portas"], PORTAS);
            Pares(R["pista"], PISTA_PTS); Pares(R["erva"], ERVA_PTS); Pares(R["sentinela"], SENTINELA_PTS);
            PORTAIS.Clear();
            foreach (var o in Json.L(R["portais"]))
            {
                var p = Json.O(o); object br;
                PORTAIS.Add(new PortalDef { nome = Json.S(p["nome"]), eixo = Json.S(p["eixo"]), fixo = Json.D(p["fixo"]), a = Json.D(p["a"]), b = Json.D(p["b"]), brecha = p.TryGetValue("brecha", out br) && br is bool && (bool)br });
            }
            PONTOS_DEF.Clear();
            foreach (var kv in Json.O(R["pontos"])) { var l = new List<Ponto>(); foreach (var o in Json.L(kv.Value)) { var p = Json.O(o); l.Add(new Ponto(Json.D(p["x"]), Json.D(p["z"]))); } PONTOS_DEF[kv.Key] = l; }
            NPCS.Clear();
            foreach (var o in Json.L(R["mercadores"]))
            {
                var n = Json.O(o); var npc = new Npc { id = Json.S(n["id"]), nome = Json.S(n["nome"]), x = Json.D(n["x"]), z = Json.D(n["z"]), time = n["time"] as string };
                foreach (var v in Json.L(n["vende"])) npc.vende.Add(Json.S(v)); NPCS.Add(npc);
            }
            Pares(R["luzes"], CANDLES);
            CLAREIRAS.Clear(); foreach (var o in Json.L(R["clareiras"])) { var v = Json.L(o); CLAREIRAS.Add(new Clareira { x = Json.D(v[0]), z = Json.D(v[1]), r = Json.D(v[2]) }); }
            SEG_TRILHA.Clear(); foreach (var o in Json.L(R["trilhas"])) { var v = Json.L(o); SEG_TRILHA.Add(new Segmento { ax = Json.D(v[0]), az = Json.D(v[1]), bx = Json.D(v[2]), bz = Json.D(v[3]), w = Json.D(v[4]), estreita = Json.D(v[5]) != 0 }); }
            var conf = Json.O(R["conferencia"]); navNos = (int)Json.D(conf["nos"]); navArestas = (int)Json.D(conf["arestas"]); navPrincipalJS = (int)Json.D(conf["principal"]);
            MontarNavegacao();
            Carregado = true;
        }

        static void Pares(object v, List<Ponto> saida) { saida.Clear(); foreach (var o in Json.L(v)) { var p = Json.L(o); saida.Add(new Ponto(Json.D(p[0]), Json.D(p[1]))); } }

        // ---------- zonas ----------
        public static bool NaCatedral(double x, double z, double m = 0) { return x > CAT_X1 - m && x < CAT_X2 + m && z > CAT_Z1 - m && z < CAT_Z2 + m; }
        public static bool NoClaustro(double x, double z) { return x > CL_X1 && x < CL_X2 && z > CL_Z1 && z < CL_Z2; }
        public static string ZoneAt(double x, double z) { return NoClaustro(x, z) ? "Claustro" : NaCatedral(x, z) ? "Catedral" : "Floresta"; }
        public static double DistSeg(double x, double z, Segmento s)
        {
            double dx = s.bx - s.ax, dz = s.bz - s.az, L = dx * dx + dz * dz; double t = L != 0 ? ((x - s.ax) * dx + (z - s.az) * dz) / L : 0; t = JS.Clamp(t, 0, 1);
            return JS.Hypot(x - (s.ax + dx * t), z - (s.az + dz * t));
        }

        // ---------- colisão e visão ----------
        public static double SegBox(double ax, double az, double bx, double bz, Caixa b, double inf)
        {
            double x1 = b.x1 - inf, x2 = b.x2 + inf, z1 = b.z1 - inf, z2 = b.z2 + inf, dx = bx - ax, dz = bz - az, t0 = 0, t1 = 1;
            if (!Corta(-dx, ax - x1, ref t0, ref t1)) return -1;
            if (!Corta(dx, x2 - ax, ref t0, ref t1)) return -1;
            if (!Corta(-dz, az - z1, ref t0, ref t1)) return -1;
            if (!Corta(dz, z2 - az, ref t0, ref t1)) return -1;
            return t0;
        }
        static bool Corta(double p, double q, ref double t0, ref double t1)
        {
            if (Math.Abs(p) < 1e-9) { if (q < 0) return false; }
            else { double r = q / p; if (p < 0) { if (r > t1) return false; if (r > t0) t0 = r; } else { if (r < t0) return false; if (r < t1) t1 = r; } }
            return true;
        }

        public static bool Algum(Grade G, double x1, double z1, double x2, double z2, Func<Caixa, bool> fn)
        {
            G.vez++; double c = G.c;
            for (int cx = JS.Floor(x1 / c); cx <= JS.Floor(x2 / c); cx++)
                for (int cz = JS.Floor(z1 / c); cz <= JS.Floor(z2 / c); cz++)
                {
                    List<int> a; if (!G.m.TryGetValue(cx * 1000 + cz, out a)) continue;
                    foreach (int i in a) { if (G.marca[i] == G.vez) continue; G.marca[i] = G.vez; if (fn(G.list[i])) return true; }
                }
            return false;
        }

        // segmentos longos percorrem só as células que a linha toca
        public static bool SegClear(double ax, double az, double bx, double bz, double inf, Grade G)
        {
            double L = JS.Hypot(bx - ax, bz - az);
            if (L < G.c * 2) return !Algum(G, Math.Min(ax, bx) - inf, Math.Min(az, bz) - inf, Math.Max(ax, bx) + inf, Math.Max(az, bz) + inf, b => SegBox(ax, az, bx, bz, b, inf) >= 0);
            int n = (int)Math.Ceiling(L / (G.c * .5)); G.vez++;
            for (int k = 0; k <= n; k++)
            {
                double px = ax + (bx - ax) * k / n, pz = az + (bz - az) * k / n, r = G.c * .5 + inf;
                int cx1 = JS.Floor((px - r) / G.c), cx2 = JS.Floor((px + r) / G.c), cz1 = JS.Floor((pz - r) / G.c), cz2 = JS.Floor((pz + r) / G.c);
                for (int cx = cx1; cx <= cx2; cx++) for (int cz = cz1; cz <= cz2; cz++)
                    {
                        List<int> a; if (!G.m.TryGetValue(cx * 1000 + cz, out a)) continue;
                        foreach (int i in a) { if (G.marca[i] == G.vez) continue; G.marca[i] = G.vez; if (SegBox(ax, az, bx, bz, G.list[i], inf) >= 0) return false; }
                    }
            }
            return true;
        }
        public static bool LosClear(Ponto a, Ponto b) { return SegClear(a.x, a.z, b.x, b.z, 0, GT); }

        // primeira peça alta atingida por um raio; devolve a distância ao longo dele
        public static double RaioParede(double ox, double oz, double dx, double dz, double alcance, double inf, Func<double, double> alturaEm = null)
        {
            double best = alcance; var G = GT; int n = (int)Math.Ceiling(alcance / (G.c * .5)); G.vez++;
            for (int k = 0; k <= n; k++)
            {
                double px = ox + dx * alcance * k / n, pz = oz + dz * alcance * k / n; if ((double)k / n * alcance > best + G.c) break; double r = G.c * .5 + inf;
                for (int cx = JS.Floor((px - r) / G.c); cx <= JS.Floor((px + r) / G.c); cx++)
                    for (int cz = JS.Floor((pz - r) / G.c); cz <= JS.Floor((pz + r) / G.c); cz++)
                    {
                        List<int> a; if (!G.m.TryGetValue(cx * 1000 + cz, out a)) continue;
                        foreach (int i in a)
                        {
                            if (G.marca[i] == G.vez) continue; G.marca[i] = G.vez; var b = G.list[i];
                            double t = SegBox(ox, oz, ox + dx * alcance, oz + dz * alcance, b, inf); if (t < 0) continue; double d = t * alcance;
                            if (d < best && (alturaEm == null || alturaEm(d) < b.h)) best = d;
                        }
                    }
            }
            return best;
        }

        public static void Resolve(Ponto p, double r)
        {
            for (int it = 0; it < 4; it++)
            {
                Algum(GB, p.x - r, p.z - r, p.x + r, p.z + r, b =>
                {
                    double cx = JS.Clamp(p.x, b.x1, b.x2), cz = JS.Clamp(p.z, b.z1, b.z2), dx = p.x - cx, dz = p.z - cz, d = JS.Hypot(dx, dz);
                    if (d < r)
                    {
                        if (d > 1e-6) { p.x = cx + dx / d * r; p.z = cz + dz / d * r; }
                        else
                        {
                            double l = p.x - b.x1, rr = b.x2 - p.x, t = p.z - b.z1, bb = b.z2 - p.z, m = Math.Min(Math.Min(l, rr), Math.Min(t, bb));
                            if (m == l) p.x = b.x1 - r; else if (m == rr) p.x = b.x2 + r; else if (m == t) p.z = b.z1 - r; else p.z = b.z2 + r;
                        }
                    }
                    return false;
                });
                p.x = JS.Clamp(p.x, -HX + r, HX - r); p.z = JS.Clamp(p.z, -HZ + r, HZ - r);
            }
        }

        // verdadeiro se um círculo de raio r em p não encosta em nenhuma peça
        public static bool Livre(Ponto p, double r)
        {
            return !Algum(GB, p.x - r, p.z - r, p.x + r, p.z + r, b => { double cx = JS.Clamp(p.x, b.x1, b.x2), cz = JS.Clamp(p.z, b.z1, b.z2); return JS.Hypot(p.x - cx, p.z - cz) < r - .02; });
        }
        public static bool Livre(double x, double z, double r) { return Livre(new Ponto(x, z), r); }

        // ---------- navegação: grade de 2 m em 8 direções + âncoras, A* ----------
        public static int NX, NZ; public static double X0, Z0; const double PASSO = 2;
        static int[] cel; static int[] comp; public static int principal; public static int ilhas;
        static Dictionary<int, List<int>> idx = new Dictionary<int, List<int>>();
        static double[] gNav; static int[] deNav; static uint[] marcaNav, fechadoNav; static uint vezNav;

        static int Chave(double x, double z) { return JS.Floor(x / 4) * 10000 + JS.Floor(z / 4); }
        static void Indexa(int k) { var q = WP[k]; int c = Chave(q.x, q.z); List<int> L; if (!idx.TryGetValue(c, out L)) { L = new List<int>(); idx[c] = L; } L.Add(k); }
        public static List<int> Vizinhos(double x, double z, double r)
        {
            var saida = new List<int>(); int c1 = JS.Floor((x - r) / 4), c2 = JS.Floor((x + r) / 4), d1 = JS.Floor((z - r) / 4), d2 = JS.Floor((z + r) / 4);
            for (int cx = c1; cx <= c2; cx++) for (int cz = d1; cz <= d2; cz++) { List<int> L; if (idx.TryGetValue(cx * 10000 + cz, out L)) saida.AddRange(L); }
            return saida;
        }
        static void Liga(int a, int b)
        {
            var A = WP[a]; var B = WP[b]; if (A.viz.Contains(b)) return; double d = JS.Hypot(A.x - B.x, A.z - B.z);
            if (!SegClear(A.x, A.z, B.x, B.z, .35, GB)) return;
            A.viz.Add(b); A.custo.Add(d); B.viz.Add(a); B.custo.Add(d);
        }
        static void Ancora(double x, double z)
        {
            if (Math.Abs(x) > HX - 1 || Math.Abs(z) > HZ - 1 || !Livre(x, z, .55)) return; int k = WP.Count; WP.Add(new NoNav { x = x, z = z });
            foreach (int j in Vizinhos(x, z, 3.2)) if (j != k && JS.Hypot(WP[j].x - x, WP[j].z - z) < 3.2) Liga(k, j);
            Indexa(k);
        }

        static void MontarNavegacao()
        {
            WP.Clear(); idx.Clear();
            NX = JS.Floor((HX * 2 - 4) / PASSO) + 1; NZ = JS.Floor((HZ * 2 - 4) / PASSO) + 1; X0 = -HX + 2; Z0 = -HZ + 2;
            cel = new int[NX * NZ]; for (int i = 0; i < cel.Length; i++) cel[i] = -1;
            for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++) { double x = X0 + i * PASSO, z = Z0 + j * PASSO; if (Livre(x, z, .8)) { cel[i * NZ + j] = WP.Count; WP.Add(new NoNav { x = x, z = z }); } }
            int[,] dirs = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };
            for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++)
                {
                    int a = cel[i * NZ + j]; if (a < 0) continue;
                    for (int d = 0; d < 4; d++) { int ii = i + dirs[d, 0], jj = j + dirs[d, 1]; if (ii < 0 || jj < 0 || ii >= NX || jj >= NZ) continue; int b = cel[ii * NZ + jj]; if (b >= 0) Liga(a, b); }
                }
            for (int k = 0; k < WP.Count; k++) Indexa(k);
            foreach (var p in PORTAS) Ancora(p.x, p.z);
            foreach (var p in PORTAIS) { double c = (p.a + p.b) / 2; if (p.eixo == "x") Ancora(c, p.fixo); else Ancora(p.fixo, c); }
            foreach (var a in ALTARS) { Ancora(a.x, a.z + 3); Ancora(a.x, a.z - 3); Ancora(a.x + 3, a.z); Ancora(a.x - 3, a.z); }
            foreach (var r in REAG) Ancora(r.x + 1.5, r.z);
            Ancora(SPAWN["H"].x, SPAWN["H"].z); Ancora(SPAWN["C"].x, SPAWN["C"].z);
            foreach (var p in PISTA_PTS) Ancora(p.x, p.z); foreach (var p in ERVA_PTS) Ancora(p.x, p.z); foreach (var p in SENTINELA_PTS) Ancora(p.x, p.z);
            // componentes conexas: só a maior vale como destino
            comp = new int[WP.Count]; for (int i = 0; i < comp.Length; i++) comp[i] = -1; int nc = 0; var tam = new List<int>();
            for (int i = 0; i < WP.Count; i++)
            {
                if (comp[i] >= 0) continue; var st = new Stack<int>(); st.Push(i); comp[i] = nc; tam.Add(0);
                while (st.Count > 0) { int u = st.Pop(); tam[nc]++; foreach (int v in WP[u].viz) if (comp[v] < 0) { comp[v] = nc; st.Push(v); } }
                nc++;
            }
            int max = 0; for (int i = 0; i < tam.Count; i++) if (tam[i] > max) max = tam[i];
            principal = tam.IndexOf(max); ilhas = tam.Count - 1;
            gNav = new double[WP.Count]; deNav = new int[WP.Count]; marcaNav = new uint[WP.Count]; fechadoNav = new uint[WP.Count]; vezNav = 0;
        }
        public static int Arestas() { int s = 0; foreach (var w in WP) s += w.viz.Count; return s; }
        public static bool NaPrincipal(int i) { return comp[i] == principal; }

        public static int NearestWP(Ponto p)
        {
            int best = -1; double bd = 1e9;
            foreach (double r in new double[] { 3, 6, 12 })
            {
                foreach (int i in Vizinhos(p.x, p.z, r)) { if (comp[i] != principal) continue; double d = JS.Hypot(p.x - WP[i].x, p.z - WP[i].z); if (d < bd && d <= r * 1.5 && SegClear(p.x, p.z, WP[i].x, WP[i].z, 0.4, GB)) { bd = d; best = i; } }
                if (best >= 0) return best;
            }
            foreach (double r in new double[] { 12, 30 })
            {
                foreach (int i in Vizinhos(p.x, p.z, r)) { if (comp[i] != principal) continue; double d = JS.Hypot(p.x - WP[i].x, p.z - WP[i].z); if (d < bd) { bd = d; best = i; } }
                if (best >= 0) return best;
            }
            for (int i = 0; i < WP.Count; i++) { if (comp[i] != principal) continue; double d = JS.Hypot(p.x - WP[i].x, p.z - WP[i].z); if (d < bd) { bd = d; best = i; } }
            return best;
        }

        // A* com fila binária de pares (f, nó), com o mesmo desempate do JS
        static List<int> AEstrela(int s, int gl)
        {
            uint vez = ++vezNav; var alvo = WP[gl]; var hf = new List<double>(); var hi = new List<int>();
            Action<double, int> push = (f, i) =>
            {
                hf.Add(f); hi.Add(i); int c = hf.Count - 1;
                while (c > 0) { int p = (c - 1) >> 1; if (hf[p] <= hf[c]) break; Troca(hf, hi, p, c); c = p; }
            };
            marcaNav[s] = vez; gNav[s] = 0; deNav[s] = -1; push(JS.Hypot(WP[s].x - alvo.x, WP[s].z - alvo.z), s);
            while (hf.Count > 0)
            {
                // pop
                int u = hi[0]; int ult = hf.Count - 1; double lf = hf[ult]; int li = hi[ult]; hf.RemoveAt(ult); hi.RemoveAt(ult);
                if (hf.Count > 0)
                {
                    hf[0] = lf; hi[0] = li; int c = 0;
                    for (; ; ) { int l = c * 2 + 1, r = l + 1, m = c; if (l < hf.Count && hf[l] < hf[m]) m = l; if (r < hf.Count && hf[r] < hf[m]) m = r; if (m == c) break; Troca(hf, hi, m, c); c = m; }
                }
                if (fechadoNav[u] == vez) continue; fechadoNav[u] = vez; if (u == gl) break;
                double gu = gNav[u]; var U = WP[u];
                for (int k = 0; k < U.viz.Count; k++)
                {
                    int v = U.viz[k]; double w = U.custo[k]; if (fechadoNav[v] == vez) continue; double gv = gu + w;
                    if (marcaNav[v] != vez || gv < gNav[v]) { marcaNav[v] = vez; gNav[v] = gv; deNav[v] = u; push(gv + JS.Hypot(WP[v].x - alvo.x, WP[v].z - alvo.z), v); }
                }
            }
            if (fechadoNav[gl] != vez) return null;
            var path = new List<int>(); for (int c = gl, guarda = 0; c >= 0 && guarda < 20000; c = deNav[c], guarda++) path.Add(c); path.Reverse(); return path;
        }
        static void Troca(List<double> hf, List<int> hi, int a, int b) { double f = hf[a]; hf[a] = hf[b]; hf[b] = f; int i = hi[a]; hi[a] = hi[b]; hi[b] = i; }

        public static List<Ponto> FindPath(Ponto from, Ponto to)
        {
            if (JS.Hypot(to.x - from.x, to.z - from.z) < 40 && SegClear(from.x, from.z, to.x, to.z, 0.45, GB)) return new List<Ponto> { new Ponto(to.x, to.z) };
            int s = NearestWP(from), gl = NearestWP(to); if (s < 0 || gl < 0) return new List<Ponto> { new Ponto(to.x, to.z) };
            var ids = AEstrela(s, gl); if (ids == null) return new List<Ponto> { new Ponto(to.x, to.z) };
            var path = new List<Ponto>(); foreach (int i in ids) path.Add(new Ponto(WP[i].x, WP[i].z)); path.Add(new Ponto(to.x, to.z));
            // suavização: de cada ponto, pula para o mais distante que ainda se enxerga (até 24 nós à frente)
            var saida = new List<Ponto>(); Ponto cur = from; int ii = 0;
            while (ii < path.Count)
            {
                int j = ii; while (j + 1 < path.Count && j + 1 - ii < 24 && SegClear(cur.x, cur.z, path[j + 1].x, path[j + 1].z, 0.45, GB)) j++;
                saida.Add(path[j]); cur = path[j]; ii = j + 1;
            }
            return saida;
        }
    }
}
