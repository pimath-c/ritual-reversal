// A planta do navegador (plantaFixa e mapSvg do index.html), num viewBox de 540 × 380: a camada fixa (mata, trilhas,
// desfiladeiro, promontório, catedral, travessias, bancos, peças e pilares) é desenhada uma vez numa textura; por cima,
// no IMGUI, os nomes, os altares no estado que o seu lado conhece, os mercadores, os sinalizadores, os pings, os aliados e
// você. Serve à planta do Tab, à escolha dos altares (clique no círculo) e ao Rever a noite.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        const float VB_L = PlantaFixa.L, VB_A = PlantaFixa.A;
        static float PS { get { return PlantaFixa.S; } }
        static float PX(double x) { return PlantaFixa.X(x); }
        static float PZ(double z) { return PlantaFixa.Z(z); }

        // texto do SVG: âncora no meio, y na linha de base; s = pixels por unidade do viewBox
        void TextoSvg(Rect r, float x, float y, string txt, float tam, Color c, bool italico = false, bool negrito = false, bool serif = false)
        {
            float s = r.width / VB_L; int fs = Mathf.Max(8, Mathf.RoundToInt(tam * s));
            var st = new GUIStyle(sPequeno) { fontSize = fs, alignment = TextAnchor.LowerCenter, wordWrap = false, fontStyle = italico ? FontStyle.Italic : negrito ? FontStyle.Bold : FontStyle.Normal, richText = false };
            if (italico && fonteSerifIt != null) { st.font = fonteSerifIt; st.fontStyle = FontStyle.Normal; } else if (serif && fonteSerif != null) st.font = fonteSerif;   // os nomes dos lugares e dos altares, como no SVG
            st.normal.textColor = c; TextoNitido(new Rect(r.x + x * s - 160, r.y + y * s - fs * 1.15f, 320, fs * 1.4f), txt, SoSimbolos(st, txt), false);
        }
        Vector2 PontoSvg(Rect r, double x, double z) { float s = r.width / VB_L; return new Vector2(r.x + PX(x) * s, r.y + PZ(z) * s); }

        // a camada fixa com os nomes (mercadores só com a economia ligada)
        void PlantaBase(Rect r)
        {
            GUI.DrawTexture(r, PlantaFixa.Textura()); float s = r.width / VB_L;
            if (g == null || g.lab.economia) foreach (var n in Mapa.NPCS) { var p = PontoSvg(r, n.x, n.z); Tracos.Disco(p.x, p.y, 4 * s, Desenho.Hex("#d2ad62")); TextoSvg(r, PX(n.x), PZ(n.z) - 7, System.Text.RegularExpressions.Regex.Replace(n.nome, "^(o|a) ", ""), 10, Desenho.Hex("#d2ad62")); }
            if (MapaVisual.Asilo) { PlantaAsiloBase(r); return; }
            foreach (var L in MapaVisual.LUGARES) TextoSvg(r, PX(L.x), PZ(L.z), L.nome, 10, Desenho.Hex("#8a8272"), true);
            SpawnDef sh, sc;
            if (Mapa.SPAWN.TryGetValue("H", out sh)) TextoSvg(r, PX(sh.x), PZ(sh.z) + 34, "Caçadores", 12, Desenho.Hex("#d2ad62"));
            if (Mapa.SPAWN.TryGetValue("C", out sc)) TextoSvg(r, PX(sc.x), PZ(sc.z) + 34, "Cultistas", 12, CorCulto());
        }
        // asilo (plantaAsilo/plantaAsiloDin do navegador): nomes das salas longe dos altares e mercadores, os nascimentos, e por
        // cima o que muda: as alas sem luz (sombra), os quadros cortados (vermelhos) e as portas (cheias fechadas, tracejadas abertas)
        static readonly Dictionary<string, float> NOME_SALA_Z = new Dictionary<string, float> { { "Gramado", 32 }, { "Saguão", 19 }, { "Pátio Norte", -55 }, { "Horta", 28 }, { "Cemitério", 28 }, { "Arquivo", -1.5f }, { "Farmácia", -1.5f } };
        void PlantaAsiloBase(Rect r)
        {
            float s = r.width / VB_L; var comAltar = new HashSet<string>(); foreach (var A in Mapa.ALTARS) comAltar.Add(A.name);
            // a sombra das alas sem luz fica embaixo dos nomes
            if (g != null && g.energia != null)
                foreach (var E in MapaVisual.ESPACOS)
                {
                    if (!E.interno) continue; float cx = (E.x1 + E.x2) / 2; string st = cx < -12.5f ? "oeste" : cx > 12.5f ? "leste" : "centro"; double fim;   // a divisão das lâmpadas do mapaAsilo
                    if (g.energia.TryGetValue(st, out fim) && fim > g.t) RetSvg(r, E.x1, E.x2, E.z1, E.z2, new Color(0, 0, 0, .45f));
                }
            foreach (var E in MapaVisual.ESPACOS)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(E.nome, "^(Corredor|Trilha|Margem|Píer|Escada)") || comAltar.Contains(E.nome) || (E.x2 - E.x1) * PS < E.nome.Length * 4.6f + 4) continue;
                float cz; if (!NOME_SALA_Z.TryGetValue(E.nome, out cz)) cz = (E.z1 + E.z2) / 2;
                TextoSvg(r, PX((E.x1 + E.x2) / 2), PZ(cz) + 3, E.nome, 9, Desenho.Hex(E.interno ? "#8a8272" : "#6e7a62"), true);
            }
            SpawnDef sh, sc;
            if (Mapa.SPAWN.TryGetValue("H", out sh)) TextoSvg(r, PX(sh.x), PZ(sh.z) + 22, "Caçadores", 12, Desenho.Hex("#d2ad62"));
            if (Mapa.SPAWN.TryGetValue("C", out sc)) TextoSvg(r, PX(sc.x), PZ(sc.z) + 22, "Culto", 12, CorCulto());
            if (g == null) return;
            if (g.energia != null) foreach (var q in MapaVisual.QUADROS) { double fim; if (g.energia.TryGetValue(q.setor, out fim) && fim > g.t) { RetPx(r, PX(q.x) - 4, PZ(q.z) - 4, 8, 8, Desenho.Hex("#ff5040")); RetPx(r, PX(q.x) - 2.8f, PZ(q.z) - 2.8f, 5.6f, 5.6f, Desenho.Hex("#5a1010")); } }
            if (g.portas != null)
                foreach (var P in MapaVisual.PORTAS_DIN)
                {
                    bool ab = P.i >= g.portas.Count || g.portas[P.i], ax = P.eixo == "x";
                    Vector2 a = PontoSvg(r, ax ? P.x1 : P.x, ax ? P.z : P.z1), b = PontoSvg(r, ax ? P.x2 : P.x, ax ? P.z : P.z2);
                    if (!ab) Tracos.Seg(a, b, 3.5f * s, Desenho.Hex("#e0b86a"));
                    else { float L = Vector2.Distance(a, b), passo = 4 * s; for (float t = 0; t < L; t += passo) Tracos.Seg(Vector2.Lerp(a, b, t / L), Vector2.Lerp(a, b, Mathf.Min(L, t + 2 * s) / L), 1.5f * s, Desenho.Hex("#8a7650")); }
                }
        }
        void RetSvg(Rect r, float x1, float x2, float z1, float z2, Color c) { RetPx(r, PX(x1), PZ(z1), (x2 - x1) * PS, (z2 - z1) * PS, c); }
        void RetPx(Rect r, float x, float y, float w, float h, Color c) { float s = r.width / VB_L; var c0 = GUI.color; GUI.color = c; GUI.DrawTexture(new Rect(r.x + x * s, r.y + y * s, w * s, h * s), Texture2D.whiteTexture); GUI.color = c0; }
        static Color CorCulto() { return Desenho.Hex(PlayerPrefs.GetInt("rr-dalt", 0) == 1 ? "#6ea8ff" : "#e0435e"); }

        // estilo do altar na planta (altarMapStyle): preenchimento e contorno, ou null para o neutro
        bool EstiloAltar(Altar A, string papel, out Color fill, out Color stroke)
        {
            fill = Desenho.Hex("#1b1f24"); stroke = Desenho.Hex("#6d7e8e"); string f = null, s = null;
            if (A.state == "fenda") { f = "#12071a"; s = "#e0435e"; }
            else if (A.state == "farol") { f = "#6d4f1c"; s = "#f3d58e"; }
            else if (papel == "C") { if (A.state == "active") { f = "#a3203a"; s = "#fff"; } else if (A.chosen) { f = A.state == "awake" ? "#43285a" : "#2a0f18"; s = "#e0435e"; } else if (A.decoy) { f = "#333"; s = "#888"; } }
            else
            {
                string k = g.know != null && A.i < g.know.Length ? g.know[A.i] : null;
                if (A.state == "active" && A.localized) { f = "#a3203a"; s = "#fff"; } else if (k == "confirmado") { f = "#6b3a8a"; s = "#e0c0ff"; } else if (k == "desperto") { f = "#43285a"; s = "#9b6fc0"; }
                else if (k == "chamariz") { f = "#333"; s = "#888"; } else if (k == "descartado") { f = "#101214"; s = "#3f4650"; } else if (k == "purgado") { f = "#2a2312"; s = "#b08a3e"; }
            }
            if (f == null) return false; fill = Desenho.Hex(f); stroke = Desenho.Hex(s); return true;
        }
        float RaioAltarSvg { get { return Mathf.Max(7, (float)CFG.circleR * PS + 3); } }
        void AltarSvg(Rect r, Altar A, Color fill, Color stroke)
        {
            float s = r.width / VB_L, R = RaioAltarSvg * s; var p = PontoSvg(r, A.x, A.z);
            Tracos.Disco(p.x, p.y, R, fill); Tracos.Anel(p.x, p.y, R, 2.5f * s, stroke);
            TextoSvg(r, PX(A.x), PZ(A.z) + (A.z < 0 ? 22 : -14), A.name, 13, Desenho.Hex("#e8e0d0"), false, false, true);
        }

        // planta do Tab (mapSvg com o jogador): aberta enquanto segura Tab
        void Planta(Ator m, string papel)
        {
            // #tabMap .panel: 700 px, padding 18 × 22, a placa do HUD; o SVG com margem de 10 em cima e embaixo
            float w = Mathf.Min(700, W * .9f), hS = (w - 44) * VB_A / VB_L, h = hS + 56; var caixa = new Rect((W - w) / 2, (H - h) / 2, w, h);
            Placa(caixa);
            var r = new Rect(caixa.x + 22, caixa.y + 28, w - 44, hS); float s = r.width / VB_L;
            PlantaBase(r);
            foreach (var A in g.altars) { Color f, st; EstiloAltar(A, papel, out f, out st); AltarSvg(r, A, f, st); }
            if (g.lab.economia) foreach (var n in Mapa.NPCS) if (n.time == null || n.time == m.team) { var p = PontoSvg(r, n.x, n.z); Tracos.Anel(p.x, p.y, 4 * s, 1.5f * s, Desenho.Hex("#d2ad62")); }
            foreach (var f in g.flares) if (f.t > 0) { var p = PontoSvg(r, f.x, f.z); Tracos.Disco(p.x, p.y, 5 * s, Desenho.Hex("#ffb070", .8f)); }
            var cp = Desenho.Hex("#8ee8ff");
            foreach (var pg in pings) { var p = PontoSvg(r, pg.x, pg.z); Tracos.Anel(p.x, p.y, 7 * s, 2 * s, cp); Tracos.Disco(p.x, p.y, 2 * s, cp); TextoSvg(r, PX(pg.x), PZ(pg.z) - 10, pg.txt, 11, cp); }
            foreach (var a in g.actors)
            {
                if (a == m || a.hidden || a.team != m.team || a.st == "dead") continue; var p = PontoSvg(r, a.x, a.z);
                Tracos.Losango(p.x, p.y, 4 * s, a.st == "down" ? Color.white : m.team == "H" ? Desenho.Hex("#d2ad62") : CorCulto(), new Color(0, 0, 0, 0)); TextoSvg(r, PX(a.x), PZ(a.z) - 9, a.name, 11, Desenho.Hex("#e8e0d0"));
            }
            var eu = PontoSvg(r, m.x, m.z); Tracos.SetaJogador(eu.x, eu.y, s, -(float)lookYaw * Mathf.Rad2Deg, Color.white);
        }

        // escolha dos altares (pickScreen): a planta com os círculos clicáveis
        void PlantaEscolha(Rect r)
        {
            PlantaBase(r); float s = r.width / VB_L; var ev = Event.current;
            foreach (var A in g.altars)
            {
                bool sel = g.picks.Contains(A.i); var p = PontoSvg(r, A.x, A.z); float R = RaioAltarSvg * s;
                bool sobre = Vector2.Distance(ev.mousePosition, p) < R + 4 * s;
                AltarSvg(r, A, sel ? Desenho.Hex("#5a0e1e") : sobre ? Desenho.Hex("#2a2f36") : Desenho.Hex("#1b1f24"), sel ? Desenho.Hex("#e0435e") : sobre ? Desenho.Hex("#c9d3dc") : Desenho.Hex("#6d7e8e"));
                if (sobre && ev.type == EventType.MouseDown && ev.button == 0) { Acao(new Acao { type = "pick", i = A.i }); som.Tocar("click"); ev.Use(); }
            }
        }
    }
}
