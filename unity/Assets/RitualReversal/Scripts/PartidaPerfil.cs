// Perfil, histórico e conquistas (perfil, perfilFim, CONQ e perfilScreen do index.html) e o desafio da noite (desafioDe,
// pontosDesafio, desafioFim, blocoDesafio). Tudo neste aparelho: PlayerPrefs "rr-perfil", o mesmo JSON do localStorage do
// navegador (partidas, vitórias por papel e classe, recordes, conquistas, as últimas 20 partidas e o recorde de cada desafio).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static readonly string[][] CONQ = {
            new[]{"primeira","Primeira noite","Termine uma partida."}, new[]{"vitoria","Amanhecer","Vença uma partida."}, new[]{"fenda","Fenda aberta","Complete um ritual como Cultista."},
            new[]{"farol","Farol","Sele um ritual como Caçador."}, new[]{"doisazero","Dois a zero","Como Cultistas, abra duas Fendas sem deixar selar nenhum ritual."}, new[]{"muralha","Muralha de fé","Como Caçadores, sele dois rituais na mesma rodada."},
            new[]{"anjo","Anjo da guarda","Reanime 3 aliados numa partida."}, new[]{"mao","Mão pesada","Derrube 5 inimigos numa partida."}, new[]{"purificador","Purificador","Purgue 3 altares numa partida."},
            new[]{"ilusionista","Ilusionista","Plante um Chamariz."}, new[]{"andarilho","Andarilho","Transfira um ritual."}, new[]{"intocado","Intocado","Passe uma rodada inteira sem cair."},
            new[]{"tempestade","Sob a tempestade","Jogue uma rodada sob tempestade."}, new[]{"classes","Todas as máscaras","Jogue com todas as cinco classes."}, new[]{"veterano","Veterano","Termine 10 partidas."},
            new[]{"aprendiz","Aprendiz","Termine o tutorial."}, new[]{"desafio","Noite marcada","Termine um desafio da noite."}, new[]{"online","Em boa companhia","Termine uma partida online."} };

        // o perfil é o próprio JSON (dicionários e listas), como no navegador
        static Dictionary<string, object> O(Dictionary<string, object> d, string k) { object v; if (!d.TryGetValue(k, out v) || !(v is Dictionary<string, object>)) d[k] = v = new Dictionary<string, object>(); return (Dictionary<string, object>)v; }
        static List<object> Lst(Dictionary<string, object> d, string k) { object v; if (!d.TryGetValue(k, out v) || !(v is List<object>)) d[k] = v = new List<object>(); return (List<object>)v; }
        static double Nn(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) && v != null && !(v is string) && !(v is bool) && !(v is Dictionary<string, object>) && !(v is List<object>) ? System.Convert.ToDouble(v) : 0; }
        static void Mais(Dictionary<string, object> d, string k, double n = 1) { d[k] = Nn(d, k) + n; }
        Dictionary<string, object> Perfil()
        {
            Dictionary<string, object> p = null; try { p = Json.Ler(PlayerPrefs.GetString("rr-perfil", "")) as Dictionary<string, object>; } catch { }
            p = p ?? new Dictionary<string, object>();
            foreach (var k in new[] { "partidas", "vitorias", "derrotas", "empates" }) if (!p.ContainsKey(k)) p[k] = 0.0;
            var pa = O(p, "papel"); foreach (var k in new[] { "H", "C" }) { var x = O(pa, k); if (!x.ContainsKey("rodadas")) { x["rodadas"] = 0.0; x["vitorias"] = 0.0; } }
            O(p, "classes"); O(p, "recordes"); O(p, "conq"); Lst(p, "hist"); Lst(p, "ids"); return p;
        }
        static void SalvarPerfil(Dictionary<string, object> p) { try { PlayerPrefs.SetString("rr-perfil", Noite.ParaJson(p)); PlayerPrefs.Save(); } catch { } }
        static double Agora() { return (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalMilliseconds; }

        // ---------- fim da partida: conta a noite no perfil (uma vez por partida) ----------
        string perfilId; float perfilDesde; readonly List<string[]> filaConq = new List<string[]>(); string[] conqAtual; float conqT;
        void PerfilFim()
        {
            if (g == null || g.phase != "final" || arsenal) return; string id = (g.matchId ?? "") + "|" + EU; if (perfilId == id) return;
            if (online && logOnline == null) { if (perfilDesde == 0) perfilDesde = Time.realtimeSinceStartup; if (Time.realtimeSinceStartup - perfilDesde < 4) return; }
            perfilId = id; perfilDesde = 0; var p = Perfil(); var ids = Lst(p, "ids"); if (ids.Contains(id)) return; ids.Add(id); if (ids.Count > 60) ids.RemoveAt(0);
            var slot = MeuSlot(); if (slot == null) return; var w = Sim.Winner(g) ?? new Vencedor(); bool venceu = w.win == slot.team, empate = w.win == null; var eu = Eu();
            Mais(p, "partidas"); if (empate) Mais(p, "empates"); else if (venceu) Mais(p, "vitorias"); else Mais(p, "derrotas");
            RodadaLT R1; string meu = eu != null ? eu.name : LT.rod.TryGetValue(1, out R1) ? R1.nome : ""; var log = LogDaNoite() ?? new List<Registro>();
            System.Func<string, string, int?, int> conta = (ev, campo, r) => log.Count(e => e.ev == ev && (e[campo] as string) == meu && (r == null || e.r == r));
            var novas = new List<string>(); var conq = O(p, "conq"); System.Action<string> ganha = k => { if (!conq.ContainsKey(k)) { conq[k] = Agora(); novas.Add(k); } };
            var linha = new Dictionary<string, object> { { "data", Agora() }, { "modo", online ? "online" : "local" }, { "resultado", empate ? "empate" : venceu ? "vitória" : "derrota" } }; var rodadas = new List<object>(); linha["rodadas"] = rodadas;
            foreach (int r in new[] { 1, 2 })
            {
                RodadaLT R; if (!LT.rod.TryGetValue(r, out R) || R.papel == null) continue; string papel = R.papel;
                var sc = papel == "C" ? g.scores[slot.team] : g.scores[slot.team == "A" ? "B" : "A"]; bool ganhou = papel == "C" ? sc != null && sc.done >= 2 : sc != null && sc.done < 2;
                var P = O(O(p, "papel"), papel); Mais(P, "rodadas"); if (ganhou) Mais(P, "vitorias");
                var C = O(O(p, "classes"), R.cls ?? ""); Mais(C, "rodadas"); if (ganhou) Mais(C, "vitorias");
                string clima = Mundo.ClimaDe(Mundo.ChaveClima(g) + ":" + r);
                rodadas.Add(new Dictionary<string, object> { { "papel", papel }, { "cls", R.cls }, { "fendas", sc != null ? sc.done : 0 }, { "farois", sc != null ? sc.@sealed : 0 }, { "clima", clima } });
                if (papel == "C" && sc != null && sc.done >= 1) ganha("fenda"); if (papel == "C" && sc != null && sc.done >= 2 && sc.@sealed == 0) ganha("doisazero");
                if (papel == "H" && sc != null && sc.@sealed >= 1) ganha("farol"); if (papel == "H" && sc != null && sc.@sealed >= 2) ganha("muralha");
                if (log.Count > 0 && !log.Any(e => e.ev == "down" && (e["who"] as string) == meu && e.r == r)) ganha("intocado");
                if (clima == "tempestade") ganha("tempestade");
            }
            var n = new Dictionary<string, int> { { "quedas", conta("down", "by", null) }, { "reanimacoes", conta("revive", "by", null) }, { "purgas", conta("purge", "by", null) }, { "rituais", conta("ritual_start", "by", null) }, { "tarefas", conta("ponto", "by", null) } };
            var rec = O(p, "recordes"); foreach (var kv in n) { if (kv.Value > Nn(rec, kv.Key)) rec[kv.Key] = (double)kv.Value; linha[kv.Key] = kv.Value; }
            ganha("primeira"); if (venceu) ganha("vitoria"); if (n["reanimacoes"] >= 3) ganha("anjo"); if (n["quedas"] >= 5) ganha("mao"); if (n["purgas"] >= 3) ganha("purificador");
            if (conta("decoy", "by", null) > 0) ganha("ilusionista"); if (conta("transfer", "by", null) > 0) ganha("andarilho"); if (online) ganha("online");
            if (Nn(p, "partidas") >= 10) ganha("veterano"); var cls = O(p, "classes"); if (Regras.CLASSES.Keys.All(c => cls.ContainsKey(c))) ganha("classes");
            var hist = Lst(p, "hist"); hist.Insert(0, linha); while (hist.Count > 20) hist.RemoveAt(hist.Count - 1); SalvarPerfil(p);
            foreach (var k in novas) { var c = CONQ.FirstOrDefault(q => q[0] == k); if (c != null) filaConq.Add(c); }
        }
        // a faixa da conquista, que entra pela direita (#conquista)
        void ToastConquista()
        {
            float dt = dtG; conqT -= dt;
            if (conqT <= 0) { if (conqAtual != null && conqT < -.5f) conqAtual = null; if (conqAtual == null && filaConq.Count > 0 && conqT < -.6f) { conqAtual = filaConq[0]; filaConq.RemoveAt(0); conqT = 4.5f; if (som != null) som.Tocar("sealed"); } }
            if (conqAtual == null) return;
            // #conquista: entra e sai pela direita com o transform do CSS (.5 s, cubic-bezier(.2,.9,.3,1.2), que passa um pouco do ponto)
            float dx = conqT > 0 ? 360 * (1 - CurvaCss(.2f, .9f, .3f, 1.2f, (4.5f - conqT) / .5f)) : 360 * CurvaCss(.2f, .9f, .3f, 1.2f, -conqT / .5f);
            float w = 300, x = W - 22 - w + dx; var stDesc = Css("400", 14, Hx("#cfc6b6"));
            var linhas = Quebrar(conqAtual[2], stDesc, w - 30); float h = 2 + 20 + LN(12) + LNs(22f) + linhas.Count * LN(16), y = H - 150 - h;
            var r = new Rect(x, y, w, h); PintarDegrade(r, new Color(38 / 255f, 32 / 255f, 20 / 255f, .95f), new Color(18 / 255f, 15 / 255f, 10 / 255f, .95f), true); Moldura(r, GOLD);
            float yy = y + 11; Espacada(x + 15, yy, LN(12), "CONQUISTA", Css("400", 12, GOLD), .96f); yy += LN(12);
            Linha(x + 15, yy, w - 30, LNs(22f), conqAtual[1], Css("serif", 22, GOLD_HI)); yy += LNs(22f);
            // o span fica numa linha do corpo (16 px, line-height normal 19,2 px): linha de base 16 px abaixo do topo, o que a
            // caixa de 20,8 px do Linha dá para um texto de 14 px
            foreach (var l in linhas) { Linha(x + 15, yy, w - 30, 21f, l, stDesc); yy += LN(16); }
        }
        // cubic-bezier do CSS: acha o t em que a curva passa por x e devolve o y
        static float CurvaCss(float x1, float y1, float x2, float y2, float x)
        {
            x = Mathf.Clamp01(x); float a = 0, b = 1, t = x;
            for (int i = 0; i < 30; i++) { t = (a + b) / 2; float u = 1 - t, bx = 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t; if (bx < x) a = t; else b = t; }
            float v = 1 - t; return 3 * v * v * t * y1 + 3 * v * t * t * y2 + t * t * t;
        }

        // ---------- tela do perfil ----------
        Vector2 rolPerfil; bool confirmarApagar;
        void TelaPerfil() { TelaPerfilCss(); }


        // ---------- desafio da noite ----------
        // todo dia um desafio: o papel e o clima saem da data, os bots são difíceis e o clima é o mesmo em todas as tentativas
        bool desAtivo; string desData; Dictionary<string, object> desFeito;
        static string HojeDesafio() { var d = System.DateTime.Now; return d.Year + "-" + d.Month.ToString("00") + "-" + d.Day.ToString("00"); }
        static string[] DesafioDe(string data)
        {
            uint h = 2166136261; for (int i = 0; i < data.Length; i++) { char c = data[i]; h = unchecked((h ^ c) * 16777619u); if (char.IsHighSurrogate(c) && i + 1 < data.Length) i++; }
            string id = "desafio-" + data; return new[] { id, h % 2 == 1 ? "C" : "H", Mundo.ClimaDe(id + ":1") };
        }
        void IniciarDesafio()
        {
            string data = HojeDesafio(); var D = DesafioDe(data);
            IniciarSolo(D[1], "dificil", false, true); g.matchId = D[0] + "#" + (long)Agora(); desAtivo = true; desData = data; desFeito = null;
        }
        Dictionary<string, object> PontosDesafio()
        {
            var slot = MeuSlot(); if (g == null || slot == null) return null; var w = Sim.Winner(g) ?? new Vencedor(); string papel = Sim.RoleOf(g, slot.team); var sc = g.scores["B"] ?? new Placar();
            var log = g.log; System.Func<string, int> conta = ev => log.Count(e => e.ev == ev && (e["by"] as string) == "Você"); int quedas = log.Count(e => e.ev == "down" && (e["who"] as string) == "Você");
            int p = w.win == slot.team ? 1000 : 0; var partes = new List<string>();
            if (papel == "C") { p += sc.done * 300 - sc.@sealed * 150; if (w.win == slot.team && sc.lastT.HasValue) p += System.Math.Max(0, (int)JS.Round((CFG.momentoAte[2] - sc.lastT.Value) / 2)); partes.Add(sc.done + " Fendas"); partes.Add(sc.@sealed + " Faróis"); }
            else { p += sc.@sealed * 300 - sc.done * 150; partes.Add(sc.@sealed + " Faróis"); partes.Add(sc.done + " Fendas"); }
            int rq = conta("down"), rv = conta("revive"), pg = conta("purge"), rs = conta("ritual_start"); p += rq * 40 + rv * 60 + pg * 50 + rs * 40 - quedas * 40; p = System.Math.Max(0, p);
            partes.Add(rq + " inimigos derrubados"); partes.Add(quedas + " quedas suas"); return new Dictionary<string, object> { { "p", p }, { "papel", papel }, { "partes", partes } };
        }
        void DesafioFim()
        {
            if (!desAtivo || g == null || g.phase != "final" || desFeito != null) return; var r = PontosDesafio(); if (r == null) return; desFeito = r;
            var p = Perfil(); var des = O(p, "desafios"); object ant; bool tem = des.TryGetValue(desData, out ant) && ant != null; int pts = (int)r["p"], a = tem ? System.Convert.ToInt32(ant) : 0;
            r["recorde"] = !tem || pts > a; if ((bool)r["recorde"]) des[desData] = (double)pts; r["melhor"] = System.Math.Max(pts, a);
            var conq = O(p, "conq"); if (!conq.ContainsKey("desafio")) { conq["desafio"] = Agora(); filaConq.Add(CONQ.First(q => q[0] == "desafio")); } SalvarPerfil(p);
        }
        void BlocoDesafio()
        {
            if (!desAtivo || desFeito == null) return; var r = desFeito;
            GUILayout.BeginVertical(Fundo(new Color(.14f, .11f, .05f, .9f)));
            GUILayout.Label("<size=12>" + Cor("DESAFIO DA NOITE · " + desData, OURO) + "</size>", sCentro); GUILayout.Label("<size=34><b>" + r["p"] + " pontos</b></size>", sCentro);
            GUILayout.Label("<size=15>" + string.Join(" · ", ((List<string>)r["partes"]).ToArray()) + "</size>", sCentro);
            GUILayout.Label("<i>" + Cor((bool)r["recorde"] ? "Novo recorde do dia!" : "Recorde do dia: " + r["melhor"], OURO_VIVO) + "</i>", sCentro); GUILayout.EndVertical();
        }
    }
}
