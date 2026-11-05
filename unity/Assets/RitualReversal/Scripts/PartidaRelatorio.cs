// Relatório do playtest (relatorioScreen, dadosRelatorio e textoRelatorio do index.html): os números da noite que
// respondem "a investigação funciona?" (Noite.Relatorio, o mesmo Sim.relatorio do sim.js) e as perguntas para quem jogou.
// Baixa tudo num JSON (com o registro inteiro) ou copia como texto para mandar no grupo. As respostas ficam neste aparelho
// (PlayerPrefs "rr-resp-<partida>").
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        bool relAberto; string relMsg = ""; Dictionary<string, object> relCache; string relChave;
        static readonly string[] PERGUNTAS_PLAYTEST = { "Em que momento você sentiu que estava investigando de verdade?", "Em que momento sentiu que estava só correndo de um lugar a outro?",
            "Qual informação mudou a sua decisão?", "Quando você sentiu que não tinha como responder ao adversário?", "Qual mecânica você tiraria do jogo?", "Você jogaria mais uma partida? Por quê?" };
        static readonly Dictionary<string, string> CAUSA_SELO = new Dictionary<string, string> { { "caído", "caiu" }, { "empurrão", "Empurrão" }, { "atordoado", "atordoado" }, { "saiu", "saiu do círculo" }, { "soltou", "soltou o E" }, { "lutar", "parou para lutar" } };
        static readonly Dictionary<string, string> CACA_TXT = new Dictionary<string, string> { { "selou", "Selou" }, { "perdeu_disputa", "Chegou a tempo e perdeu o círculo" }, { "chegou_tarde", "Chegou tarde demais" }, { "perto_sem_entrar", "Chegou perto e não entrou" }, { "altar_errado", "Foi a outro altar e não chegou" }, { "nunca_chegou", "Nunca chegou perto" }, { "aberto", "A noite acabou antes" } };
        static readonly Dictionary<string, string> COMO_DESCOBRIU = new Dictionary<string, string> { { "evp", "EVP" }, { "sentinela", "sentinela" }, { "lanterna", "Lanterna" }, { "rastro_lido", "marcas de arrasto" }, { "localized", "o coro revelou o ritual" } };

        List<string> respostas; string respId;
        List<string> Respostas()
        {
            string id = g != null ? g.matchId : ""; if (respostas != null && respId == id) return respostas; respId = id; respostas = new List<string>();
            var l = Json.Ler(PlayerPrefs.GetString("rr-resp-" + id, "[]")) as List<object>; if (l != null) foreach (var o in l) respostas.Add(o as string ?? "");
            while (respostas.Count < PERGUNTAS_PLAYTEST.Length) respostas.Add(""); return respostas;
        }
        void GuardarRespostas() { PlayerPrefs.SetString("rr-resp-" + respId, Noite.ParaJson(respostas.Cast<object>().ToList())); }

        static Dictionary<string, object> Dd(object o) { return o as Dictionary<string, object> ?? new Dictionary<string, object>(); }
        static double Nm(object o) { return o == null || o is string || o is bool ? 0 : System.Convert.ToDouble(o); }
        static string Tm(object v) { return v != null ? Noite.Relogio(Nm(v)) : "—"; }
        static string Sg(object v) { return v != null ? Mathf.RoundToInt((float)Nm(v)) + " s" : "—"; }
        static string TextoInterrupcoes(Dictionary<string, object> I) { var l = I.Select(kv => { string n; return (CAUSA_SELO.TryGetValue(kv.Key, out n) ? n : kv.Key) + " " + kv.Value; }).ToArray(); return l.Length > 0 ? string.Join(", ", l) : "nenhuma"; }
        // v5.7: o que cada lado fez no círculo (a menos de 12 m, depois que a caça chegou)
        static string TextoDisputa(Dictionary<string, object> R)
        {
            var D = R["disputa"] as Dictionary<string, object>; if (D == null) return ""; var Hh = Dd(D["caca"]); var C = Dd(D["culto"]); var I = Dd(R["interrupcoes"]);
            System.Func<Dictionary<string, object>, string, string> n = (o, k) => { object v; return Mathf.RoundToInt((float)(o.TryGetValue(k, out v) ? Nm(v) : 0)).ToString(); };
            System.Func<Dictionary<string, object>, string> q = o => { object v; int x = o.TryGetValue("quedas", out v) ? (int)Nm(v) : 0; return x + (x == 1 ? " queda" : " quedas"); };
            return "Caça: " + n(Hh, "selando") + " s selando (" + n(Hh, "sobFogo") + " s sob fogo), " + n(Hh, "atacando") + " s atirando, " + n(Hh, "dano") + " de dano, " + q(Hh) + ". " +
                "Culto: " + n(C, "canal") + " s canalizando, " + n(C, "atacando") + " s atacando, " + n(C, "dano") + " de dano, " + q(C) + ". " +
                "Interrupções: " + n(I, "selo") + " de selo, " + n(I, "canal") + " de canalização.";
        }
        Dictionary<string, object> DadosRelatorio()
        {
            var log = LogDaNoite() ?? new List<Registro>(); var rodadas = UmaNoite ? new[] { 1 } : new[] { 1, 2 }; var w = Sim.Winner(g); var L = g.lab ?? Lab.De(null); var resp = Respostas();
            return new Dictionary<string, object> {
                { "jogo", "Ritual Reversal" }, { "versao", Noite.VERSAO }, { "registroVersao", Noite.REGISTRO }, { "partida", g.matchId }, { "quando", System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }, { "papel", MeuPapel() },
                { "lab", new Dictionary<string, object> { { "crepusculo", L.crepusculo }, { "fogo", L.fogo }, { "execucao", L.execucao }, { "economia", L.economia }, { "formato", L.formato } } }, { "mudancas", LabMudancas(L).Cast<object>().ToList() },
                { "vencedor", w != null ? new Dictionary<string, object> { { "win", w.win }, { "why", w.why } } : null }, { "jogadores", g.slots.Select(s => (object)new Dictionary<string, object> { { "nome", s.name }, { "equipe", s.team }, { "humano", s.cid != null } }).ToList() },
                { "metricas", rodadas.Select(r => (object)Noite.Relatorio(log, r)).ToList() }, { "respostas", PERGUNTAS_PLAYTEST.Select((p, i) => (object)new Dictionary<string, object> { { "pergunta", p }, { "resposta", resp[i] } }).ToList() },
                { "registro", log.Select(e => { var o = new Dictionary<string, object> { { "t", e.t }, { "r", e.r }, { "ev", e.ev } }; foreach (var kv in e.d) o[kv.Key] = kv.Value; return (object)o; }).ToList() } };
        }
        string TextoRelatorio(Dictionary<string, object> D)
        {
            var mud = ((List<object>)D["mudancas"]).Cast<string>().ToList(); var w = D["vencedor"] as Dictionary<string, object>;
            var L = new List<string> { "Ritual Reversal v" + Noite.VERSAO + " · relatório do playtest · partida " + D["partida"], "Regras: " + (mud.Count > 0 ? string.Join(" · ", mud.ToArray()) : "normais"), "Resultado: " + (w != null ? w["why"] : "—") };
            foreach (Dictionary<string, object> M in (List<object>)D["metricas"])
            {
                var I = Dd(M["investigacao"]); var E = Dd(M["engano"]); var C = Dd(M["combate"]); var S = Dd(M["selamento"]); string como; object cd = I["comoDescobriu"];
                L.Add(""); L.Add("Rodada " + M["rodada"] + ": " + M["fendas"] + " Fendas, " + M["farois"] + " Faróis, " + Noite.Relogio(Nm(M["duracao"])) + " de noite");
                L.Add("Investigação: primeira verdade aos " + Tm(I["primeiraVerdade"]) + " (" + (cd != null && COMO_DESCOBRIU.TryGetValue((string)cd, out como) ? como : "—") + "), " + I["pistasNovas"] + " pistas novas, maior trecho sem pista " + Noite.Relogio(Nm(I["maiorIntervaloSemPista"])));
                L.Add("Engano: " + E["chamarizes"] + " Chamarizes (" + E["chamarizesDesmascarados"] + " desmascarados, " + E["chamarizesQueimados"] + " queimados), " + E["transferencias"] + " Transferências, " + E["purgasDeAltarVerdadeiro"] + " purgas de altar verdadeiro");
                foreach (Dictionary<string, object> R in (List<object>)M["rituais"]) { string td = TextoDisputa(R); L.Add("Ritual em " + R["altar"] + " (" + Noite.Relogio(Nm(R["inicio"])) + "): localizado após " + Sg(R["localizado"]) + ", caça chegou após " + Sg(R["chegada"]) + ", " + R["tentativasSelo"] + " tentativas de selo, " + R["resultado"] + (td != "" ? ". " + td : "")); }
                foreach (Dictionary<string, object> x in (List<object>)M["lances"]) { string tit, sub; if (Noite.LanceFrase(x["tipo"] as string, Noite.Nomes(x["quem"]), x["altar"] as string, Nm(x["v"]), out tit, out sub)) L.Add("Lance (" + Noite.Relogio(Nm(x["t"])) + "): " + tit + ": " + sub); }
                L.Add("Selamento: " + S["tentativas"] + " tentativas, " + S["selados"] + " selados, interrupções: " + TextoInterrupcoes(Dd(S["interrupcoes"])));
                L.Add("Combate: " + C["quedas"] + " quedas, " + C["execucoes"] + " execuções, " + C["sangraram"] + " sangraram, " + C["reanimacoes"] + " reanimações, primeira queda " + Tm(C["primeiraQueda"]));
            }
            var Rr = ((List<object>)D["respostas"]).Cast<Dictionary<string, object>>().Where(x => ((string)x["resposta"]).Trim().Length > 0).ToList();
            if (Rr.Count > 0) { L.Add(""); L.Add("Respostas:"); foreach (var x in Rr) L.Add("- " + x["pergunta"] + " " + ((string)x["resposta"]).Trim()); }
            return string.Join("\n", L.ToArray());
        }

    }
}
