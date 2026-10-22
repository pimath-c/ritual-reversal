// Fim da noite do Unity fora do Unity (tools/unity-noite.js): joga uma noite de bots com um Caçador parado no lugar do
// jogador, anota a linha do tempo como o cliente (LinhaDoTempo), e grava a planta fixa, o gráfico de cada rodada e o que
// as telas do fim mostram (marcos, lances, destaques, placar). Uso: mono noite.exe <Resources do Unity> <saída> [semente]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using RitualReversal;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

public static class TesteNoite
{
    static void Gravar(Texture2D t, string arq)
    {
        using (var w = new BinaryWriter(File.Create(arq)))
        {
            w.Write(t.width); w.Write(t.height);
            // as linhas da Texture2D vão de baixo para cima; a imagem, de cima para baixo
            for (int y = t.height - 1; y >= 0; y--) for (int x = 0; x < t.width; x++) { var c = t.pixels[y * t.width + x]; w.Write(c.r); w.Write(c.g); w.Write(c.b); w.Write(c.a); }
        }
    }
    static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }

    public static int Main(string[] a)
    {
        string res = a[0], saida = a[1]; uint semente = a.Length > 2 ? uint.Parse(a[2]) : 7; Directory.CreateDirectory(saida); Resources.pasta = res;
        Mapa.Carregar(File.ReadAllText(Path.Combine(res, "simulacao.json"))); MapaVisual.Carregar(File.ReadAllText(Path.Combine(res, "mapa.json")));
        var t0 = DateTime.Now; var planta = PlantaFixa.Textura(); Console.Error.WriteLine("planta fixa " + planta.width + "×" + planta.height + " em " + (DateTime.Now - t0).TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " s");
        Gravar(planta, Path.Combine(saida, "planta.rgba"));

        JS.Random = JS.Mulberry32(semente);
        var slots = new List<Slot> { new Slot("A", "eu", "Você"), new Slot("A", null, "Irmã Beatriz"), new Slot("B", null, "Irmão Cinza"), new Slot("B", null, "Madre Vesper") };
        var g = Sim.CreateGame(new OpcoesJogo { timers = true, slots = slots, matchId = "teste-noite" });
        var LT = new LinhaDoTempo(); int passos = 0;
        while (g.phase != "final" && passos < 30 * 60 * 40)
        {
            if (g.phase == "pick" || g.phase == "intro" || g.phase == "summary") Sim.Act(g, "eu", new Acao { type = "ready" });
            Sim.Step(g, 1.0 / 30); passos++;
            foreach (var e in g.events) { if (e.type == "momento") LT.Marco(g, "momento", e.Str("nome")); else if (e.type == "eco") LT.Marco(g, "eco", "Eco do coro"); }
            g.events.Clear(); LT.Amostrar(g, "eu");
        }
        var sb = new StringBuilder(); var C = CultureInfo.InvariantCulture; var w = Sim.Winner(g);
        sb.Append("{\"fase\":\"" + g.phase + "\",\"vencedor\":\"" + Esc(w != null ? w.win + " " + w.why : "") + "\",\"rodadas\":[");
        bool prim = true;
        foreach (var kv in LT.rod.OrderBy(k => k.Key))
        {
            var R = kv.Value; var mk = Noite.MarcosFinais(R, g.log, kv.Key); var tex = Noite.Grafico(R, g.lab, g.altars.Count); Gravar(tex, Path.Combine(saida, "grafico" + kv.Key + ".rgba"));
            double T = Noite.DuracaoGrafico(R);
            if (!prim) sb.Append(','); prim = false;
            sb.Append("{\"r\":" + kv.Key + ",\"amostras\":" + R.am.Count + ",\"posicoes\":" + (R.pos != null ? R.pos.Count : 0) + ",\"quedas\":" + (R.quedas != null ? R.quedas.Count : 0) + ",\"T\":" + T.ToString(C) + ",\"marcos\":[");
            sb.Append(string.Join(",", mk.Select(m => "{\"rt\":" + m.rt.ToString(C) + ",\"tipo\":\"" + m.tipo + "\",\"txt\":\"" + Esc(m.txt) + "\"}").ToArray()));
            sb.Append("],\"altares\":[" + string.Join(",", Enumerable.Range(0, g.altars.Count).Where(i => R.am.Any(q => q.a[i] != null)).Select(i => "\"" + Esc(g.altars[i].name) + "\"").ToArray()) + "]}");
        }
        sb.Append("],\"lances\":[" + string.Join(",", Noite.Lances(g.log).Select(L => "{\"t\":" + L.t.ToString(C) + ",\"tit\":\"" + Esc(L.tit) + "\",\"sub\":\"" + Esc(L.sub) + "\",\"culto\":" + (L.culto ? "true" : "false") + "}").ToArray()) + "]");
        sb.Append(",\"premios\":[" + string.Join(",", Noite.Premios(g.log).Select(p => "{\"nome\":\"" + Esc(p.nome) + "\",\"quem\":\"" + Esc(p.quem) + "\",\"txt\":\"" + Esc(p.verbo + " " + p.n + " " + (p.n == 1 ? p.um : p.varios)) + "\"}").ToArray()) + "]");
        sb.Append(",\"placar\":[" + string.Join(",", Noite.Placar(g.log).Select(P => "{\"r\":" + P.r + ",\"linhas\":[" + string.Join(",", P.linhas.Select(L => "{\"nome\":\"" + Esc(L.nome) + "\",\"team\":\"" + L.team + "\",\"classe\":\"" + Esc(L.classe) + "\",\"humano\":" + (L.humano ? "true" : "false") + ",\"v\":[" + string.Join(",", L.v.Select(x => x.ToString()).ToArray()) + "]}").ToArray()) + "]}").ToArray()) + "]");
        // o mesmo registro escrito como o servidor e lido de volta (o caminho do online) tem de dar o mesmo placar
        var js = new StringBuilder("{\"events\":["); bool p1 = true;
        foreach (var e in g.log) { if (!p1) js.Append(','); p1 = false; js.Append("{\"t\":" + e.t.ToString(C) + ",\"r\":" + e.r + ",\"ev\":\"" + e.ev + "\""); foreach (var kv in e.d) js.Append(",\"" + kv.Key + "\":" + Noite.ParaJson(kv.Value)); js.Append('}'); }
        js.Append("]}"); var lido = Noite.RegistroDoJson(js.ToString());
        Func<List<Registro>, string> resumo = L => string.Join("|", Noite.Placar(L).Select(P => string.Join(";", P.linhas.Select(x => x.nome + ":" + string.Join(",", x.v.Select(v => v.ToString()).ToArray())).ToArray())).ToArray()) + "#" + string.Join("|", Noite.Premios(L).Select(p => p.nome + p.quem + p.n).ToArray()) + "#" + string.Join("|", Noite.Lances(L).Select(l => l.sub).ToArray());
        sb.Append(",\"registroOnlineIgual\":" + (resumo(lido) == resumo(g.log) ? "true" : "false") + ",\"eventos\":" + g.log.Count + "}");
        File.WriteAllText(Path.Combine(saida, "noite.json"), sb.ToString());
        return 0;
    }
}
