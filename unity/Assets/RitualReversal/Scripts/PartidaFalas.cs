// Frases rápidas (FALAS, falar, receberFala e atualizarFalasBots do index.html): V abre a lista, 1 a 6 escolhe (ou clique);
// só a própria equipe ouve. "Preciso de ajuda!" e "Inimigo aqui!" também marcam o lugar por 8 s. O bot da sua equipe fala
// sozinho (só no cliente, não mexe na simulação): pede ajuda ao cair, agradece a reanimação, avisa quando inicia um ritual
// e, no jogo local, quando avista um inimigo longe de você.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static readonly string[] FALAS = { "Preciso de ajuda!", "Estou indo", "Recuar!", "Inimigo aqui!", "Obrigado" };
        static string TextoFala(int f, string team) { return f == 5 ? (team == "C" ? "Vou iniciar o ritual" : "Vou purgar este altar") : f >= 0 && f < FALAS.Length ? FALAS[f] : ""; }
        bool falasAbertas; float falasT;
        int falaSel;
        void AtalhosFalas()
        {
            if (EntradaUnity.Apertou(Tecla.V)) { falasAbertas = !falasAbertas; falasT = 4; }
            // controle: direcional para cima abre; aberto, cima e baixo escolhem, A envia, B fecha
            if (!falasAbertas && EntradaUnity.PadCima()) { falasAbertas = true; falaSel = 0; falasT = 6; return; }
            if (!falasAbertas) return;
            if (EntradaUnity.PadCima()) { falaSel = (falaSel + 5) % 6; falasT = 6; } if (EntradaUnity.PadBaixo()) { falaSel = (falaSel + 1) % 6; falasT = 6; }
            if (EntradaUnity.PadA()) { Falar(falaSel); return; } if (EntradaUnity.PadB()) { falasAbertas = false; return; }
            var T = new[] { Tecla.D1, Tecla.D2, Tecla.D3, Tecla.D4, Tecla.D5, Tecla.D6 }; for (int i = 0; i < T.Length; i++) if (EntradaUnity.Apertou(T[i])) { Falar(i); return; }
        }
        void Falar(int f) { falasAbertas = false; Acao(new Acao { type = "fala", i = f }); }
        void ReceberFala(Evento e) { ReceberFala(e.Int("a"), e.Int("f"), e.Str("team"), (float)e.Num("x"), (float)e.Num("z")); }
        void ReceberFala(int a, int f, string team, float x, float z)
        {
            var quem = g.actors.Find(q => q.id == a); string nome = quem != null ? quem.name : "Aliado", txt = TextoFala(f, team);
            Feed(nome + ": " + txt, MeuPapel() == "H" ? "h" : "c"); som.Tom(float.NaN, float.NaN, 1, 660, 740, .09f, Som.Onda.Triangulo, .14f);
            if (f == 0 || f == 3) { pings.RemoveAll(p => p.a == a); pings.Add(new PingV { a = a, x = x, z = z, t = 8, txt = txt, nome = nome }); }
            if (quem != null && quem.cid != EU) Legenda("fala" + a, x, z, nome + ": " + txt, "", 1e9f);
        }
        // o quadro das frases, no meio da tela, abaixo da mira (#falas)
        void PainelFalas()
        {
            var m = Eu(); if (!falasAbertas || m == null || g == null || g.phase != "play") { falasAbertas = false; return; }
            falasT -= dtG; if (falasT <= 0) { falasAbertas = false; return; }
            float w = 300, h = 34 + 6 * 26, x = W / 2 - w / 2, y = H / 2 + 90; Caixa(new Rect(x, y, w, h), new Color(.031f, .035f, .043f, .86f));
            GUI.Label(new Rect(x + 10, y + 6, w - 20, 20), "<size=12>" + Cor("FRASES PARA A EQUIPE", OURO) + "</size>", sPequeno);
            var st = new GUIStyle(sPequeno) { fontSize = 16, richText = true };
            for (int f = 0; f < 6; f++)
            {
                var r = new Rect(x + 10, y + 30 + f * 26, w - 20, 24); bool sobre = r.Contains(Event.current.mousePosition);
                if (f == falaSel) Caixa(r, new Color(.953f, .835f, .557f, .1f)); GUI.Label(r, Cor("[" + (f + 1) + "]", OURO_VIVO) + "  " + Cor(TextoFala(f, m.team), sobre || f == falaSel ? OURO_VIVO : OSSO), st);
                if (sobre && Event.current.type == EventType.MouseDown) { Falar(f); Event.current.Use(); }
            }
        }

        // ---------- falas automáticas do bot aliado ----------
        string fbId; readonly Dictionary<int, string> fbSt = new Dictionary<int, string>(), fbAlt = new Dictionary<int, string>(); readonly Dictionary<string, double> fbT = new Dictionary<string, double>(); readonly Dictionary<int, int?> fbAlvo = new Dictionary<int, int?>();
        void FalaBot(Ator a, int f, double? x = null, double? z = null, double intervalo = 6)
        {
            string k = a.id + ":" + f; double t; if (fbT.TryGetValue(k, out t) && g.rt - t < intervalo) return; fbT[k] = g.rt;
            ReceberFala(a.id, f, a.team, (float)(x ?? a.x), (float)(z ?? a.z));
        }
        void AtualizarFalasBots()
        {
            if (g == null || g.phase != "play" || g.actors == null || arsenal) return; var eu = Eu(); if (eu == null) return;
            string id = (g.matchId ?? "") + ":" + (g.round > 0 ? g.round : 1); if (fbId != id) { fbId = id; fbSt.Clear(); fbT.Clear(); fbAlvo.Clear(); fbAlt.Clear(); }
            foreach (var a in g.actors)
            {
                if (a.cid != null || a == eu || a.team != eu.team) continue; string s; fbSt.TryGetValue(a.id, out s); fbSt[a.id] = a.st;
                if (s == "alive" && a.st == "down") FalaBot(a, 0); else if (s == "down" && a.st == "alive") FalaBot(a, 4);
                if (a.st == "alive" && a.ai != null)
                {
                    var t = a.ai.target; int? ant; fbAlvo.TryGetValue(a.id, out ant); fbAlvo[a.id] = t != null ? (int?)t.id : null;
                    if (t != null && t.id != ant && t.st == "alive" && JS.Hypot(t.x - eu.x, t.z - eu.z) > 18) FalaBot(a, 3, t.x, t.z, 20);
                }
            }
            if (eu.team == "C")
                for (int i = 0; i < g.altars.Count; i++)
                {
                    var A = g.altars[i]; string ant; bool tinha = fbAlt.TryGetValue(i, out ant); fbAlt[i] = A.state;
                    if (tinha && ant != null && ant != "active" && A.state == "active") { var q = g.actors.FirstOrDefault(b => b.cid == null && b.team == eu.team && b.st == "alive" && JS.Hypot(b.x - A.x, b.z - A.z) < CFG.circleR + 1.5); if (q != null) FalaBot(q, 5); }
                }
        }
    }
}
