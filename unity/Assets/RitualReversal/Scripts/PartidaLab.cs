// Laboratório de playtest (LAB_UI, labMudancas e labPainel do index.html): o anfitrião, ou quem joga sozinho, muda uma
// regra de cada vez para comparar noites no relatório. A primeira opção de cada linha é a regra normal. Solo: PlayerPrefs
// "rr-lab" (JSON); online: o anfitrião manda {t:'opcoes', lab} e a sala inteira vê no lobby.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        class OpLab { public object v; public string txt; }
        class LinhaLab { public string k, nome; public OpLab[] ops; }
        static readonly LinhaLab[] LAB_UI = {
            new LinhaLab { k = "crepusculo", nome = "Crepúsculo", ops = new[] { new OpLab { v = 180.0, txt = "3:00" }, new OpLab { v = 120.0, txt = "2:00" }, new OpLab { v = 90.0, txt = "1:30" } } },
            new LinhaLab { k = "fogo", nome = "Selo sob fogo (1 / 2 inimigos)", ops = new[] { new OpLab { v = "atual", txt = "70% / 45%" }, new OpLab { v = "a", txt = "80% / 60%" }, new OpLab { v = "b", txt = "90% / 70%" } } },
            new LinhaLab { k = "execucao", nome = "Executar um caído", ops = new[] { new OpLab { v = 4.5, txt = "4,5 s" }, new OpLab { v = 6.0, txt = "6 s" }, new OpLab { v = 7.5, txt = "7,5 s" } } },
            new LinhaLab { k = "economia", nome = "Óbolos e mercadores", ops = new[] { new OpLab { v = true, txt = "Ligados" }, new OpLab { v = false, txt = "Desligados" } } },
            new LinhaLab { k = "formato", nome = "Fim da noite", ops = new[] { new OpLab { v = "melhorDeTres", txt = "2 rituais encerram" }, new OpLab { v = "placar", txt = "Noite inteira" } } } };
        static object ValorLab(Lab L, string k) { switch (k) { case "crepusculo": return L.crepusculo; case "fogo": return L.fogo; case "execucao": return L.execucao; case "economia": return L.economia; default: return L.formato; } }
        static Lab ComValor(Lab o, string k, object v)
        {
            var L = Lab.De(o);
            switch (k) { case "crepusculo": L.crepusculo = (double)v; break; case "fogo": L.fogo = (string)v; break; case "execucao": L.execucao = (double)v; break; case "economia": L.economia = (bool)v; break; default: L.formato = (string)v; break; }
            return Lab.De(L);
        }
        static bool Igual(object a, object b) { return a is double && b is double ? System.Math.Abs((double)a - (double)b) < 1e-9 : Equals(a, b); }
        // as regras que diferem da normal, já escritas ("Crepúsculo: 2:00")
        static List<string> LabMudancas(Lab lab)
        {
            Lab L = Lab.De(lab), P = Lab.De(null); var o = new List<string>();
            foreach (var R in LAB_UI) { var v = ValorLab(L, R.k); if (!Igual(v, ValorLab(P, R.k))) { var op = R.ops.FirstOrDefault(x => Igual(x.v, v)); o.Add(R.nome + ": " + (op != null ? op.txt : v.ToString())); } }
            return o;
        }
        static string LabJson(Lab L) { return new JsonEscrita().Num("crepusculo", L.crepusculo).Txt("fogo", L.fogo).Num("execucao", L.execucao).Bool("economia", L.economia).Txt("formato", L.formato).ToString(); }
        static Lab LabDoJson(string s)
        {
            var d = string.IsNullOrEmpty(s) ? null : Json.Ler(s) as Dictionary<string, object>; if (d == null) return Lab.De(null); object v; var L = new Lab();
            if (d.TryGetValue("crepusculo", out v) && v != null) L.crepusculo = System.Convert.ToDouble(v); if (d.TryGetValue("fogo", out v)) L.fogo = v as string ?? "atual";
            if (d.TryGetValue("execucao", out v) && v != null) L.execucao = System.Convert.ToDouble(v); L.economia = !(d.TryGetValue("economia", out v) && v is bool && !(bool)v);
            if (d.TryGetValue("formato", out v)) L.formato = v as string; return Lab.De(L);
        }
        Lab labSolo; bool labAberto;
        Lab LabSolo { get { if (labSolo == null) { labSolo = LabDoJson(PlayerPrefs.GetString("rr-lab", "")); labAberto = PlayerPrefs.GetInt("rr-lab-aberto", 0) == 1; } return labSolo; } }

        // o painel (dobrável) com uma linha por regra; muda == null: só mostra (online, quem não é anfitrião)
        void PainelLab(Lab lab, System.Action<Lab> muda)
        {
            var L = Lab.De(lab); var mud = LabMudancas(L);
            string tit = (labAberto ? "▾ " : "▸ ") + "Laboratório de playtest" + (mud.Count > 0 ? "  " + Cor("· " + mud.Count + (mud.Count > 1 ? " regras alteradas" : " regra alterada"), OURO_VIVO) : "");
            if (Bt(tit, new GUIStyle(sPequeno) { richText = true }, GUILayout.ExpandWidth(false))) { labAberto = !labAberto; PlayerPrefs.SetInt("rr-lab-aberto", labAberto ? 1 : 0); }
            if (!labAberto) return;
            GUILayout.Label("<size=14>" + Cor("Para testar com pessoas: mude uma regra por vez e compare as noites no relatório do fim da partida. A primeira opção de cada linha é a regra normal." + (muda == null ? " Só o anfitrião muda." : ""), MUDO) + "</size>", sPequeno);
            var sb = new GUIStyle(sBotao) { fontSize = 15, padding = new RectOffset(8, 8, 5, 5) };
            foreach (var R in LAB_UI)
            {
                GUILayout.BeginHorizontal(); GUILayout.Label("<size=15>" + R.nome + "</size>", sPequeno, GUILayout.Width(280)); var v = ValorLab(L, R.k);
                foreach (var op in R.ops)
                {
                    bool sel = Igual(op.v, v);
                    if (muda == null) { if (sel) GUILayout.Label("<size=15><b>" + op.txt + "</b></size>", sPequeno); continue; }
                    if (Bt(sel ? Cor(op.txt, OURO_VIVO) : op.txt, sb, GUILayout.Width(170)) && !sel) { muda(ComValor(L, R.k, op.v)); if (som != null) som.Tocar("click"); }
                }
                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            }
            if (muda != null && mud.Count > 0 && Bt("Voltar tudo ao normal", new GUIStyle(sPequeno) { richText = true }, GUILayout.ExpandWidth(false))) { muda(Lab.De(null)); if (som != null) som.Tocar("click"); }
        }
        void MudarLabSolo(Lab L) { labSolo = Lab.De(L); PlayerPrefs.SetString("rr-lab", LabJson(labSolo)); PlayerPrefs.Save(); }
    }
}
