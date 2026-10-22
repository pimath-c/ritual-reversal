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
        bool relAberto; Vector2 rolRel; string relMsg = ""; Dictionary<string, object> relCache; string relChave;
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

        // uma linha das tabelas: o nome, o número e o que ele revela
        void LinhaRel(string a, string b, string c)
        {
            GUILayout.BeginHorizontal(Fundo(new Color(0, 0, 0, .22f))); GUILayout.Label("<size=15>" + a + "</size>", sPequeno, GUILayout.Width(330)); GUILayout.Label("<size=15><b>" + b + "</b></size>", sCelN, GUILayout.Width(80));
            GUILayout.Label("<size=13>" + Cor(c ?? "", MUDO) + "</size>", sPequeno); GUILayout.EndHorizontal();
        }
        void TelaRelatorio()
        {
            var log = LogDaNoite(); float lw = Mathf.Min(1100, W - 40); var r = Janela(lw, H - 50); GUILayout.BeginArea(r);
            if (log == null) { GUILayout.Label("Relatório do playtest", sTitulo); GUILayout.Label("Buscando o registro da noite no servidor…", sCentro); if (GUILayout.Button("Voltar", sBotao)) relAberto = false; GUILayout.EndArea(); return; }
            string chave = g.matchId + "|" + log.Count; if (relCache == null || relChave != chave) { relCache = DadosRelatorio(); relChave = chave; }   // o IMGUI desenha várias vezes por quadro
            var D = relCache; var resp = Respostas(); var w = D["vencedor"] as Dictionary<string, object>; var mud = ((List<object>)D["mudancas"]).Cast<string>().ToArray();
            rolRel = GUILayout.BeginScrollView(rolRel);
            GUILayout.Label("Relatório do playtest", sTitulo);
            string why = w != null ? (string)w["why"] : ""; if (why.Length > 0) why = char.ToUpper(why[0]) + why.Substring(1);
            GUILayout.Label(Cor(why + ". Regras: " + (mud.Length > 0 ? string.Join(" · ", mud) : "normais") + ".", new Color(.79f, .75f, .68f)), sCentro);
            var mets = (List<object>)D["metricas"];
            foreach (Dictionary<string, object> M in mets)
            {
                var I = Dd(M["investigacao"]); var E = Dd(M["engano"]); var C = Dd(M["combate"]); var S = Dd(M["selamento"]); var F = Dd(M["ferramentas"]); var Se = Dd(M["sessao"]);
                if (mets.Count > 1) H3("Rodada " + M["rodada"]);
                H3("Investigação"); string como; object cd = I["comoDescobriu"];
                LinhaRel("Primeira verdade descoberta pela caça", Tm(I["primeiraVerdade"]), cd != null ? "por " + (COMO_DESCOBRIU.TryGetValue((string)cd, out como) ? como : (string)cd) + ". Se as pistas são úteis e a investigação tem ritmo." : "A caça nunca descobriu um altar verdadeiro.");
                LinhaRel("Pistas novas para a caça", I["pistasNovas"].ToString(), "sinais, EVP, sentinelas, Lanterna, rastros, Sal, Sensor com sinal");
                LinhaRel("Maior trecho sem pista nova", Tm(I["maiorIntervaloSemPista"]), "Muito longo: a caça passou tempo andando sem aprender nada.");
                LinhaRel("Trecho médio sem pista nova", Tm(I["intervaloMedioSemPista"]), "");
                H3("Rituais");
                GUILayout.BeginHorizontal(); foreach (var h in new[] { new KeyValuePair<string, float>("Ritual", 170), new KeyValuePair<string, float>("Início", 80), new KeyValuePair<string, float>("Localizado", 100), new KeyValuePair<string, float>("Caça chegou", 110), new KeyValuePair<string, float>("Selos", 70), new KeyValuePair<string, float>("Resultado", 100) }) GUILayout.Label(Cor(h.Key, MUDO), sCel, GUILayout.Width(h.Value)); GUILayout.Label(Cor("A caça", MUDO), sCel); GUILayout.EndHorizontal();
                var rits = (List<object>)M["rituais"]; if (rits.Count == 0) GUILayout.Label("Nenhum ritual começou.", sPequeno);
                foreach (Dictionary<string, object> R in rits)
                {
                    GUILayout.BeginHorizontal(Fundo(new Color(0, 0, 0, .22f))); string res = (string)R["resultado"], cc; var err = ((List<object>)R["altaresErrados"]).Select(x => x as string).Distinct().ToArray(); var saiu = ((List<object>)R["cultoSaiu"]).Select(x => x as string).ToArray();
                    GUILayout.Label((string)R["altar"], sCel, GUILayout.Width(158)); GUILayout.Label(Tm(R["inicio"]), sCel, GUILayout.Width(80)); GUILayout.Label(Sg(R["localizado"]), sCel, GUILayout.Width(100)); GUILayout.Label(Sg(R["chegada"]), sCel, GUILayout.Width(110));
                    GUILayout.Label(R["tentativasSelo"].ToString(), sCel, GUILayout.Width(70)); GUILayout.Label(res == "farol" ? "Farol" : res == "fenda" ? "Fenda" : "—", sCel, GUILayout.Width(100));
                    GUILayout.Label("<size=13>" + Cor((CACA_TXT.TryGetValue((string)R["caca"], out cc) ? cc : "") + (err.Length > 0 ? " (passou por " + string.Join(", ", err) + ")" : "") + (saiu.Length > 0 ? "; culto saiu do círculo: " + string.Join(", ", saiu) : ""), MUDO) + "</size>", sPequeno);
                    GUILayout.EndHorizontal(); string td = TextoDisputa(R); if (td != "") GUILayout.Label("<size=13>" + Cor(td, MUDO) + "</size>", sPequeno);
                }
                GUILayout.Label("<size=13>" + Cor("Tempos contados desde o início de cada ritual. A última coluna separa \"nunca chegou\", \"foi a outro altar\", \"chegou perto e não entrou\", \"chegou tarde\" e \"chegou a tempo e perdeu o círculo\".", MUDO) + "</size>", sPequeno);
                H3("Engano e selamento");
                LinhaRel("Chamarizes plantados", E["chamarizes"].ToString(), E["chamarizesDesmascarados"] + " desmascarados pela Lanterna, " + E["chamarizesQueimados"] + " queimados (prenderam a caça por " + Mathf.RoundToInt((float)Nm(E["segundosPresosEmChamariz"])) + " s), " + E["evpEmChamariz"] + " EVP enganados");
                LinhaRel("Purgas de altar verdadeiro", E["purgasDeAltarVerdadeiro"].ToString(), "a caça acertou um altar antes do ritual");
                LinhaRel("Transferências", E["transferencias"].ToString(), F["rastrosVistos"] + " marcas de arrasto vistas, " + F["rastrosLidos"] + " lidas");
                LinhaRel("Tentativas de selo", S["tentativas"].ToString(), S["selados"] + " viraram Farol. Interrupções: " + TextoInterrupcoes(Dd(S["interrupcoes"])));
                H3("Combate e ferramentas");
                LinhaRel("Quedas", C["quedas"].ToString(), "primeira aos " + Tm(C["primeiraQueda"]) + "; " + C["execucoes"] + " execuções, " + C["sangraram"] + " sangraram, " + C["reanimacoes"] + " reanimações. Se cair e reanimar é uma escolha tática.");
                LinhaRel("Ferramentas da caça", (Nm(F["sinaisLidos"]) + Nm(F["sentinelasAcesas"]) + Nm(F["sensor"]) + Nm(F["evp"]) + Nm(F["lanterna"]) + Nm(F["sal"])).ToString(), F["sinaisLidos"] + " sinais, " + F["sentinelasAcesas"] + " sentinelas, " + F["sensor"] + " Sensor (" + F["sensorComSinal"] + " com sinal), " + F["evp"] + " EVP, " + F["lanterna"] + " Lanterna, " + F["sal"] + " Sal");
                LinhaRel("Comunicação", (Nm(F["pings"]) + Nm(F["frases"])).ToString(), F["pings"] + " pings, " + F["frases"] + " frases rápidas");
                LinhaRel("Saídas e reconexões", (Nm(Se["saidas"]) + Nm(Se["reconexoes"])).ToString(), Se["saidas"] + " saíram, " + Se["reconexoes"] + " voltaram");
            }
            H3("Perguntas para quem jogou");
            for (int i = 0; i < PERGUNTAS_PLAYTEST.Length; i++)
            {
                GUILayout.Label(PERGUNTAS_PLAYTEST[i], sPequeno); string novo = GUILayout.TextArea(resp[i] ?? "", 600, new GUIStyle(GUI.skin.textArea) { fontSize = 15, wordWrap = true }, GUILayout.MinHeight(48));
                if (novo != resp[i]) { resp[i] = novo; GuardarRespostas(); }
            }
            GUILayout.Label("<size=13>" + Cor("Cada pessoa responde no próprio aparelho. Mande o relatório para quem organiza o playtest: o arquivo traz o registro inteiro da noite.", MUDO) + "</size>", sCentro);
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copiar como texto", sBotao, GUILayout.Width(240))) { GUIUtility.systemCopyBuffer = TextoRelatorio(DadosRelatorio()); relMsg = "Texto copiado."; }
            if (GUILayout.Button("Baixar relatório (.json)", sBotao, GUILayout.Width(280)))
            {
                try { string arq = System.IO.Path.Combine(Application.persistentDataPath, "ritual-reversal-relatorio-" + g.matchId + ".json"); System.IO.File.WriteAllText(arq, Noite.ParaJson(DadosRelatorio())); relMsg = "Arquivo salvo em " + arq; }
                catch (System.Exception e) { relMsg = "Não consegui salvar: " + e.Message; }
            }
            if (GUILayout.Button("Voltar", sBotao, GUILayout.Width(160))) relAberto = false;
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); if (relMsg != "") GUILayout.Label(Cor(relMsg, MUDO), sCentro);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
