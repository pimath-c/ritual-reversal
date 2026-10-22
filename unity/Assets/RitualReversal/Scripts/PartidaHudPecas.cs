// As peças do HUD da partida com as medidas do CSS do navegador ("interface mais quieta"): as faixas escuras de cima e de
// baixo (#hud::before/::after), o placar com o relógio (#top), a faixa dos altares em losangos (#strip, .pip, .gem), as
// tarefas da equipe (#tarefas), o feed (#feed), vida, sanidade e óbolos (#vitals), munição e ferramentas (#kit, .tool), o
// sensor (#sensor), o aviso da munição (#ammoHint), os efeitos ativos (#buffs), a disputa do ritual (#race, .plate), os
// marcadores no mundo (.mk), a marca de acerto (#hitmark), a direção do dano (#dmgDir), o anúncio (#anuncio) e a nota da
// habilidade (#habNota).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static Color Hx(string h, float a = 1) { return Desenho.Hex(h, a); }
        static readonly Color STEEL = new Color(.427f, .494f, .557f), BONE = new Color(.91f, .878f, .816f), GOLD = new Color(.824f, .678f, .384f), GOLD_HI = new Color(.953f, .835f, .557f), CRIMSON_HI = new Color(.878f, .263f, .369f);
        static Color CrimsonHi { get { return PlayerPrefs.GetInt("rr-dalt", 0) == 1 ? new Color(.431f, .659f, 1f) : CRIMSON_HI; } }

        // #hud::before e ::after: o escuro que sobe de baixo e desce de cima, atrás de tudo
        void FaixasEscuras()
        {
            PintarDegrade(new Rect(0, 0, W, 150), new Color(0, 0, 0, .62f), new Color(0, 0, 0, 0), true);
            PintarDegrade(new Rect(0, H - 170, W, 170), new Color(0, 0, 0, 0), new Color(0, 0, 0, .6f), true);
        }

        // #top: sua equipe e o placar | relógio, momento e regra | o placar e os adversários
        void Topo(string papel, string relogio, string fase, bool grande, string regra, int nos, int eles)
        {
            var stLado = Css("400", 14, MUDO); var stNum = Css("600", 30, BONE);
            var stRel = Css("600", 36, grande ? Hx("#ff5a6e") : BONE, TextAnchor.UpperCenter); var stFase = Css("serif", 14, grande ? Hx("#ff8a98") : GOLD, TextAnchor.UpperCenter);
            var stRegra = Css("400", 11, MUDO, TextAnchor.UpperCenter);
            string nA = papel == "H" ? "Sua equipe (caça)" : "Sua equipe (culto)", nB = papel == "H" ? "Adversários (culto)" : "Adversários (caça)";
            float wNA = Larg(nos.ToString(), stNum) + 12, wNB = Larg(eles.ToString(), stNum) + 12;
            float wRel = Mathf.Max(88, Larg(relogio, stRel), Larg(fase, stFase), Larg(regra, stRegra) * 1.05f);
            float ladoA = Mathf.Max(150, Larg(nA, stLado) + wNA), ladoB = Mathf.Max(150, Larg(nB, stLado) + wNB), total = ladoA + 22 + wRel + 22 + ladoB;
            float x0 = W / 2 - total / 2, y0 = 14, hRel = 36 * 1.05f, hFase = AlturaNormal(stFase), hRegra = 11 * 1.2f, hWrap = hRel + hFase + 1 + hRegra, xw = x0 + ladoA + 22;
            if (grande) Tracos.Brilho(W / 2, y0 + hRel / 2, Larg(relogio, stRel) * .8f + 18, 30, new Color(.878f, .263f, .369f, .45f));
            NaBase(xw, y0 + (hRel - AlturaNormal(stRel)) / 2 + 36 + 2, wRel, relogio, Css("600", 36, new Color(0, 0, 0, .45f), TextAnchor.UpperCenter));   // text-shadow 0 2px 10px
            Linha(xw, y0, wRel, hRel, relogio, stRel);
            Linha(xw, y0 + hRel, wRel, hFase, fase, stFase);
            Linha(xw, y0 + hRel + hFase + 1, wRel, hRegra, regra, stRegra);
            float bas = y0 + (hWrap - 36) / 2 + 30;   // a linha de base comum do nome e do número
            float xa = x0 + ladoA - wNA; NaBase(xa + 6, bas, wNA - 12, nos.ToString(), stNum); NaBase(xa - Larg(nA, stLado), bas, Larg(nA, stLado) + 2, nA, stLado);
            float xb = xw + wRel + 22; NaBase(xb + 6, bas, wNB - 12, eles.ToString(), stNum); NaBase(xb + wNB, bas, Larg(nB, stLado) + 2, nB, stLado);
        }
        // texto pela linha de base (como o alinhamento de linha do navegador entre tamanhos diferentes)
        void NaBase(float x, float baseline, float w, string txt, GUIStyle st, float sombra = 0)
        {
            float lh = AlturaNormal(st); Linha(x, baseline - SubidaEm(st.font) * st.fontSize - (lh - AlturaNormal(st)) / 2, w, lh, txt, st, sombra);
        }

        // #strip: um losango por altar com o sinal do que a equipe sabe, o nome e a barra do ritual
        static readonly Dictionary<string, string[]> PIP = new Dictionary<string, string[]> {
            // estado: borda, fundo, cor do sinal, brilho
            { "", new[] { "#555c65", "rgba(10,12,14,.7)", "#7d858f", "" } }, { "awake", new[] { "#9b6fc0", "rgba(107,58,138,.35)", "#d6b8f0", "" } },
            { "confirmed", new[] { "#c69cf0", "rgba(107,58,138,.6)", "#ffffff", "rgba(180,120,240,.6)" } }, { "decoy", new[] { "#6c6c6c", "rgba(60,60,60,.4)", "#aaaaaa", "" } },
            { "clean", new[] { "#555c65", "rgba(10,12,14,.7)", "#4f5760", "" } }, { "seen", new[] { "#555c65", "rgba(10,12,14,.7)", "#8a919a", "" } },
            { "purged", new[] { "#b08a3e", "rgba(10,12,14,.7)", "#d8b46a", "" } }, { "active", new[] { "crimson", "rgba(163,32,58,.55)", "#ffffff", "rgba(224,67,94,.8)" } },
            { "fenda", new[] { "#3a1b4a", "rgba(18,7,26,1)", "crimson", "" } }, { "farol", new[] { "#f3d58e", "rgba(210,173,98,.45)", "#ffffff", "" } } };
        static Color Rgba(string s)
        {
            if (s == "crimson") return CrimsonHi; if (s.StartsWith("#")) return Hx(s);
            var p = s.Substring(5, s.Length - 6).Split(','); var c = new Color(float.Parse(p[0]) / 255f, float.Parse(p[1]) / 255f, float.Parse(p[2]) / 255f, float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture)); return c;
        }
        void Faixa(string papel)
        {
            int n = g.altars.Count; float pw = 62, gap = 6, x0 = W / 2 - (n * pw + (n - 1) * gap) / 2, top = 88;
            var stNome = Css("400", 12, Hx("#bdb4a4"), TextAnchor.UpperCenter); float lhNome = 11.5f * 1.15f; float pulso = .5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2);
            for (int i = 0; i < n; i++)
            {
                var A = g.altars[i]; string st = "", ch = "?";
                if (A.state == "fenda") { st = "fenda"; ch = "✕"; } else if (A.state == "farol") { st = "farol"; ch = "✓"; }
                else if (papel == "H") { string k = g.know[i]; if (A.state == "active" && A.localized) { st = "active"; ch = "!"; } else if (k == "desperto") { st = "awake"; ch = "◆"; } else if (k == "confirmado") { st = "confirmed"; ch = "◆"; } else if (k == "chamariz") { st = "decoy"; ch = "◆"; } else if (k == "descartado") { st = "clean"; ch = "✕"; } else if (k == "purgado") { st = "purged"; ch = "○"; } else if (k == "visto") { st = "seen"; ch = "·"; } }
                else { if (A.state == "active") { st = "active"; ch = "!"; } else if (A.state == "awake") { st = A.decoy ? "decoy" : "awake"; ch = "◆"; } else ch = A.chosen ? "○" : "·"; }
                bool eco = papel == "H" && g.eco != null && Sim.SETORES.Any(S => S.id == g.eco.setor && S.altares.Contains(i)), escolhido = papel == "C" && A.chosen;
                var P = PIP[st]; float px = x0 + i * (pw + gap), cx = px + pw / 2, cy = top + 11;
                // box-shadow e o pulso (throb) do ritual em andamento e do eco
                if (P[3] != "") { var gc = Rgba(P[3]); if (st == "active") gc.a = Mathf.Lerp(gc.a, 1, pulso); Tracos.Brilho(cx, cy, 30 + (st == "active" ? 8 * pulso : 0), 30 + (st == "active" ? 8 * pulso : 0), gc); }
                if (eco) Tracos.Brilho(cx, cy, 26 + 14 * pulso, 26 + 14 * pulso, new Color(.878f, .263f, .369f, .9f * pulso));
                Tracos.Gema(cx, cy, 22, 1.5f, Rgba(P[1]), Rgba(P[0]));
                if (escolhido) { var m = GUI.matrix; Tracos.Gema(cx, cy, 22 + 2 * 3 + 3, 1.5f, new Color(0, 0, 0, 0), CrimsonHi); GUI.matrix = m; }
                if (eco) Tracos.GemaTracejada(cx, cy, 22 + 2 * 4 + 3, 1.5f, GOLD_HI);
                var stG = Css("700", 13, Rgba(P[2]), TextAnchor.UpperCenter); Linha(px, top, pw, 22, ch, stG);
                if (st == "decoy") { float lw = Larg(ch, stG); Caixa(new Rect(cx - lw / 2, cy, lw, 1), Rgba(P[2])); }
                float hn = Paragrafo(px, top + 22 + 3, pw, lhNome, A.name, stNome, .9f);
                var rb = new Rect(px + 6, top + 22 + 3 + hn + 3, pw - 12, 3); Caixa(rb, Hx("#2a2f36"));
                if (A.state == "active" && (papel == "C" || A.localized)) Caixa(new Rect(rb.x, rb.y, rb.width * Mathf.Clamp01((float)A.prog), 3), CrimsonHi);
            }
        }

        // #tarefas: à direita, embaixo do topo
        void Tarefas(string papel)
        {
            List<Tarefa> T; if (!g.tarefas.TryGetValue(papel, out T) || T.Count == 0) return;
            float x = W - 26 - 300, y = 120, w = 300; var tit = Css("serif", 17, GOLD); Linha(x, y, w, 17 * 1.35f, "Tarefas da equipe", tit, .9f); y += 17 * 1.35f + 4;
            float lh = 14.5f * 1.35f;
            foreach (var t in T)
            {
                var st = Css("400", 15, t.feita ? Hx("#8f978a") : Hx("#d8cfbf"));
                string txt = (t.feita ? "✓" : "○") + " " + t.nome + (t.meta > 0 ? " " + Cor(t.prog + "/" + t.meta, MUDO) : "");
                var ls = Quebrar(txt, st, w);
                for (int i = 0; i < ls.Count; i++)
                {
                    Linha(x, y + i * lh, w, lh, ls[i], st, .9f);
                    if (t.feita) { float bas = y + i * lh + (lh - AlturaNormal(st)) / 2 + st.fontSize; Caixa(new Rect(x, bas - st.fontSize * .3f, Larg(ls[i], st), 1), Hx("#8f978a")); }
                }
                y += ls.Count * lh + 3;
            }
        }

        // #feed: o mais novo em cima; some depois de 6 s
        void FeedHud()
        {
            float y = 112, x = 18, w = 330, agora = Time.unscaledTime; int n = 0;
            for (int i = feed.Count - 1; i >= 0 && n < 6; i--)
            {
                var a = feed[i]; float idade = agora - a.t; if (idade > 6.6f) continue; n++; float op = idade < 6 ? 1 : Mathf.Clamp01(1 - (idade - 6) / .6f);
                bool big = a.cls.Contains("big"); var st = big ? Css("serif", 20, new Color(BONE.r, BONE.g, BONE.b, op)) : Css("400", 15, new Color(BONE.r, BONE.g, BONE.b, op));
                float lh = AlturaNormal(st), h = AlturaParagrafo(a.msg, st, w - 22, lh) + 10;
                Caixa(new Rect(x, y, w, h), new Color(0, 0, 0, .5f * op));
                var cb = a.cls.Contains("c") ? CrimsonHi : a.cls.Contains("h") ? GOLD : STEEL; cb.a *= op; Caixa(new Rect(x, y, 2, h), cb);
                Paragrafo(x + 12, y + 5, w - 22, lh, a.msg, st); y += h + 4;
            }
        }

        // #vitals: a classe, vida, sanidade e óbolos, presos embaixo à esquerda
        void Vitais(Ator m, bool economia)
        {
            float x = 26, w = 250, hRole = 19 * 1.268f, hMet = 7 + 17 * 1.2f + 2 + 5, hBolsa = economia ? 8 + 17 * 1.2f : 0, y = H - 22 - (hRole + 2 * hMet + hBolsa);
            var cl = Regras.CLASSES[m.cls]; var stR = Css("serif", 19, m.team == "H" ? GOLD : CrimsonHi); float bas = y + (hRole - AlturaNormal(stR)) / 2 + SubidaEm(stR.font) * 19;
            NaBase(x, bas, 200, cl.name, stR, .9f); NaBase(x + Larg(cl.name, stR) + 6, bas, 150, m.team == "H" ? "Caçador" : "Cultista", Css("400", 14, MUDO), .9f);
            y += hRole;
            string san = m.sanity < 15 ? "Sanidade: pânico, você se denuncia" : m.sanity < 40 ? "Sanidade: abalado" : m.sanity < 70 ? "Sanidade: inquieto" : "Sanidade";
            Medidor(x, y, w, "Vida", Mathf.Max(0, Mathf.Ceil((float)m.hp)).ToString(), (float)(m.hp / m.maxHp), Hx("#5d6b77"), Hx("#c9d3dc"), m.shield > 0 ? (float)(m.shield / m.maxHp) : 0); y += hMet;
            Medidor(x, y, w, san, Mathf.RoundToInt((float)m.sanity).ToString(), (float)(m.sanity / 100), Hx("#43285a"), Hx("#a77ad0"), 0); y += hMet;
            if (economia) { var stO = Css("400", 14, MUDO); var stB = Css("400", 17, GOLD_HI); float b2 = y + 8 + 17; NaBase(x, b2, 80, "Óbolos", stO); NaBase(x + Larg("Óbolos", stO) + 4 + 4, b2, 80, "<b>" + Mathf.FloorToInt((float)m.obolos) + "</b>", stB); }
        }
        void Medidor(float x, float y, float w, string rotulo, string valor, float frac, Color a, Color b, float escudo)
        {
            y += 7; var stL = Css("400", 13, MUDO); var stV = Css("600", 17, BONE, TextAnchor.UpperRight);
            Linha(x, y, w - 60, 13 * 1.2f, rotulo, stL); Linha(x + w - 120, y, 120, 17 * 1.2f, valor, stV); y += 17 * 1.2f + 2;
            var tr = new Rect(x, y, w, 5); Caixa(tr, new Color(1, 1, 1, .1f)); frac = Mathf.Clamp01(frac);
            if (frac > 0) PintarDegrade(new Rect(x, y, w * frac, 5), a, b, false);
            if (escudo > 0) Caixa(new Rect(tr.xMax - w * Mathf.Clamp01(escudo), y, w * Mathf.Clamp01(escudo), 5), new Color(200 / 255f, 120 / 255f, 1, .7f));
        }

        // #kit: munição (ou fervor) e a fileira de ferramentas, presos embaixo à direita; #sensor em cima dele
        void KitHud(Ator m)
        {
            float xd = W - 26, hTool = 37.8f, yTools = H - 20 - hTool, yAmmo = yTools - 6 - 52;
            string grande, peq; if (m.team == "H") { if (m.reloadT > 0) { grande = ""; peq = "recarregando…"; } else { grande = m.ammo.ToString(); peq = "/ " + m.reserve; } } else { grande = Mathf.FloorToInt((float)m.fervor).ToString(); peq = "Fervor"; }
            var stG = Css("600", 52, BONE); var stP = Css("500", 19, MUDO); float wP = Larg(peq, stP), wG = grande.Length > 0 ? Larg(grande, stG) + Larg(" ", stG) : 0, bas = yAmmo + (52 - AlturaNormal(stG)) / 2 + 52;
            Tracos.Brilho(xd - (wP + wG) / 2, bas - 18, (wP + wG) / 2 + 16, 30, new Color(0, 0, 0, .35f));   // text-shadow 0 2px 12px
            if (grande.Length > 0) NaBase(xd - wP - wG, bas, wG, grande, stG, .6f); NaBase(xd - wP, bas, wP + 2, peq, stP, .6f);
            // as ferramentas (buildTools), da direita para a esquerda
            var F = Regras.FEITICOS[m.feitico]; var L = new List<object[]>();
            if (m.team == "H") { L.Add(new object[] { EntradaUnity.Nome("sensor"), "Sensor", null, (float)(m.sensorCd / 15), false }); L.Add(new object[] { EntradaUnity.Nome("lantern"), "Lanterna", null, (float)((100 - m.carga) / 100), m.lantern }); L.Add(new object[] { EntradaUnity.Nome("flare"), "Flare", m.flares.ToString(), 0f, false }); }
            else { L.Add(new object[] { "☉", "Reagentes", m.reag.ToString(), 0f, false }); L.Add(new object[] { EntradaUnity.Nome("use"), "Chamariz", g.decoys.ToString(), 0f, false }); }
            L.Add(new object[] { EntradaUnity.Nome("abil"), F.nome, null, (float)(m.abilCd / F.cd), false });
            var stK = Css("700", 12, GOLD, TextAnchor.UpperCenter); var stN = Css("400", 12, BONE, TextAnchor.UpperCenter); float x = xd;
            for (int i = L.Count - 1; i >= 0; i--)
            {
                var t = L[i]; string nome = (string)t[1] + (t[2] != null ? " <b>" + t[2] + "</b>" : ""); float tw = Mathf.Max(58, Mathf.Max(Larg((string)t[0], stK), Larg(nome, stN)) + 18); x -= tw;
                var r = new Rect(x, yTools, tw, hTool); Caixa(r, new Color(0, 0, 0, .35f)); Moldura(r, (bool)t[4] ? GOLD : new Color(.91f, .878f, .816f, .14f));
                Linha(r.x, r.y + 4, tw, 12 * 1.2f, (string)t[0], stK); Linha(r.x, r.y + 4 + 12 * 1.2f, tw, 12 * 1.2f, nome, stN);
                float cd = Mathf.Clamp01((float)t[3]); if (cd > 0) Caixa(new Rect(r.x + 1, r.yMax - 1 - (hTool - 2) * cd, tw - 2, (hTool - 2) * cd), new Color(0, 0, 0, .65f));
                x -= 5;
            }
            if (m.team == "H")
            {
                if (sensorT > 0) sensorT -= dtG; float op = sensorT > 0 ? 1 : .45f;
                string s = sensorV > .6 ? "Sinal forte" : sensorV > .3 ? "Sinal médio" : sensorV > .02 ? "Sinal fraco" : "Nenhum sinal";
                var stS = Css("serif", 16, new Color(203 / 255f, 191 / 255f, 224 / 255f, op), TextAnchor.UpperRight); float hS = AlturaNormal(stS), ys = H - 128 - (hS + 4 + 4);
                Linha(xd - 220, ys, 220, hS, s, stS, .9f); var tr = new Rect(xd - 220, ys + hS + 4, 220, 4); Caixa(tr, new Color(1, 1, 1, .1f * op));
                if (sensorV > 0) PintarDegrade(new Rect(tr.x, tr.y, tr.width * Mathf.Clamp01((float)sensorV), 4), Hx("#5a3a74", op), Hx("#d9b7ff", op), false);
            }
        }

        // #race: a disputa do ritual mais perto, numa placa com os cantos cortados
        Texture2D texPlaca; GUIStyle sPlaca;
        void Placa(Rect r)
        {
            if (texPlaca == null)
            {
                int N = 64; texPlaca = new Texture2D(N, N, TextureFormat.RGBA32, false); var p = new Color[N * N]; Color cima = new Color(38 / 255f, 43 / 255f, 50 / 255f, .84f), baixo = new Color(18 / 255f, 21 / 255f, 25 / 255f, .88f), borda = new Color(140 / 255f, 98 / 255f, 57 / 255f, .45f);
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                    {
                        int X = Mathf.Min(x, N - 1 - x), Y = Mathf.Min(y, N - 1 - y); float d = X + Y;   // corte de 8 px nos cantos
                        p[y * N + x] = d < 8 ? new Color(0, 0, 0, 0) : d < 9 || X == 0 || Y == 0 ? borda : Color.Lerp(baixo, cima, y / (float)(N - 1));
                    }
                texPlaca.SetPixels(p); texPlaca.Apply(false, true); texPlaca.wrapMode = TextureWrapMode.Clamp;
                sPlaca = new GUIStyle { border = new RectOffset(10, 10, 10, 10) }; sPlaca.normal.background = texPlaca;
            }
            GUI.Box(r, GUIContent.none, sPlaca);
        }
        void Disputa(Ator m, string papel)
        {
            Altar R = null; double bd = 1e9;
            foreach (var A in g.altars) { if (A.state != "active") continue; if (papel == "H" && !A.localized && Sim.Dist(m, A) > CFG.ritRad[System.Math.Max(1, A.lastN)]) continue; double d = Sim.Dist(m, A); if (d < bd && (d < 30 || papel == "C")) { bd = d; R = A; } }
            if (R == null) return;
            System.Func<Ator, bool> atacando = c => CFG.ataqueCanal > 0 && g.t - c.ataqueT < CFG.ataqueCanal;
            var noCirculo = g.actors.Where(c => c.team == "C" && c.st == "alive" && Sim.InCircle(c, R)).ToList();
            int n = noCirculo.Count(c => !atacando(c)), s = g.actors.Count(a => a.team == "H" && a.sealing == R.i); string nota;
            if (papel == "C" && m.team == "C" && Sim.InCircle(m, R) && atacando(m)) nota = "Você atacou: a sua canalização parou por um instante";
            else if (s > 0) nota = s >= 2 ? "Selando em dupla" : "Selando";
            else if (n == 0) nota = noCirculo.Count > 0 ? "Ninguém canaliza: quem ataca não celebra" : R.prog > R.cp ? "Círculo vazio: o ritual recua até o checkpoint" : "Círculo vazio";
            else { double mf = Sim.MareFactor(g); nota = n + " " + (n > 1 ? "canalizando" : "canalizando sozinho") + (mf > 1 ? ", Maré Profana +" + Mathf.RoundToInt((float)(mf - 1) * 100) + "%" : ""); }
            var stT = Css("serif", 17, BONE, TextAnchor.UpperCenter); float hT = AlturaNormal(stT), hR = 14 * 1.2f, h = 1 + 10 + hT + 6 + 2 * (4 + hR) + 5 + 16 + 12 + 1, w = 440;
            var caixa = new Rect(W / 2 - w / 2, H - 150 - h, w, h); Placa(caixa);
            float x = caixa.x + 17, y = caixa.y + 11, wc = w - 34; Linha(x, y, wc, hT, R.name, stT); y += hT + 6;
            Fileira(x, y + 4, wc, hR, "Ritual", (float)R.prog, Hx("#6b1224"), CrimsonHi, true); y += 4 + hR;
            Fileira(x, y + 4, wc, hR, "Selamento", (float)R.seal, Hx("#7b5a24"), GOLD_HI, false); y += 4 + hR;
            Linha(x, y + 5, wc, 16, nota, Css("400", 13, MUDO, TextAnchor.UpperCenter));
        }
        void Fileira(float x, float y, float w, float lh, string rotulo, float v, Color a, Color b, bool marcas)
        {
            var st = Css("400", 14, BONE); Linha(x, y, 78, lh, rotulo, st);
            var tr = new Rect(x + 78 + 8, y + (lh - 10) / 2, w - 78 - 44 - 16, 10); Caixa(tr, Hx("#101316")); Moldura(tr, Hx("#333a42"));
            if (v > 0) PintarDegrade(new Rect(tr.x + 1, tr.y + 1, (tr.width - 2) * Mathf.Clamp01(v), 8), a, b, false);
            if (marcas) foreach (var f in new[] { .25f, .75f }) Caixa(new Rect(tr.x + 1 + (tr.width - 2) * f, tr.y - 3, 2, 16), new Color(.91f, .878f, .816f, .6f));
            Linha(tr.xMax + 8, y, 44, lh, "<b>" + Mathf.FloorToInt(v * 100) + "%</b>", st);
        }

        // .mk: o nome, a linha de baixo e o losango, com o pé no ponto do mundo; atrás da câmera vai para a borda mais perto
        void Marcador(Vector3 p, string txt, string sub, string cls, bool prender)
        {
            var sp = cam.WorldToScreenPoint(new Vector3(p.x, p.y, -p.z)); bool atras = sp.z < 0; float esc = Screen.height / 1080f;
            float sx = sp.x / esc, sy = H - sp.y / esc;
            if (atras) { if (!prender) return; sx = W - sx < W / 2 ? 70 : W - 70; sy = H * .45f; }
            if (prender) { sx = Mathf.Clamp(sx, 70, W - 70); sy = Mathf.Clamp(sy, 120, H - 200); }
            Color ct = cls == "caido" ? CrimsonHi : cls == "npc" ? Hx("#e8d6a8") : cls == "ping" ? Hx("#8ee8ff") : cls == "obj" ? Hx("#ff8fa3") : BONE;
            var st = Css(cls == "caido" || cls == "ping" ? "600" : "400", 14, ct, TextAnchor.UpperCenter); var ss = Css("400", 12, MUDO, TextAnchor.UpperCenter);
            float lh = 14 * 1.15f, ls = 12 * 1.15f, tam = cls == "ping" ? 11 : cls == "obj" ? 12 : 9, h = lh + (sub != null ? ls : 0) + 3 + tam, y = sy - h;
            float pulso = .5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / (cls == "ping" ? 1.2f : 1f));
            Linha(sx - 160, y, 320, lh, txt, st, .9f); y += lh; if (sub != null) { Linha(sx - 160, y, 320, ls, sub, ss, .9f); y += ls; }
            float cy = y + 3 + tam / 2;
            if (cls == "npc") Tracos.Anel(sx, cy, 4.5f - 1, 2, GOLD);
            else if (cls == "ping" || cls == "obj") { if (cls == "ping") Tracos.Brilho(sx, cy, 12 + 8 * pulso, 12 + 8 * pulso, new Color(.56f, .91f, 1f, .5f * pulso)); Tracos.Gema(sx, cy, tam, 2, new Color(0, 0, 0, 0), cls == "ping" ? Hx("#8ee8ff") : CrimsonHi); }
            else { if (cls == "caido") Tracos.Brilho(sx, cy, 10 + 10 * pulso, 10 + 10 * pulso, new Color(.878f, .263f, .369f, .8f * pulso)); Tracos.Gema(sx, cy, tam, 0, cls == "cult" || cls == "caido" ? CrimsonHi : GOLD, new Color(0, 0, 0, 0)); }
        }

        // #hitmark: o losango vazado (head dourado, kill carmim e mais grosso, escudo tracejado azul)
        void MarcaAcerto()
        {
            if (hitT <= 0) return; hitT -= dtG; float op = Mathf.Clamp01(hitT / .15f);
            Color c = marcaAcerto == "kill" ? CrimsonHi : marcaAcerto == "head" ? GOLD_HI : marcaAcerto == "escudo" ? Hx("#a8d4ff") : Color.white; c.a = op;
            // 30 px com borda de 2 (3 no kill), na escala de cada tipo; o escudo fica reto
            float k = marcaAcerto == "kill" ? 1.15f : marcaAcerto == "head" ? .9f : marcaAcerto == "escudo" ? .7f : .6f, e = (marcaAcerto == "kill" ? 3 : 2) * k;
            if (marcaAcerto == "escudo") Tracos.GemaTracejada(W / 2, H / 2, 30 * k, e, c, 4 * k, 3 * k, false);
            else Tracos.Gema(W / 2, H / 2, 30 * k, e, new Color(0, 0, 0, 0), c);
        }
        // #dmgDir: um quarto de anel vermelho no raio de 120 px, apontado para quem atirou
        void DirecaoDano()
        {
            for (int i = danos.Count - 1; i >= 0; i--)
            {
                var d = danos[i]; d.t -= dtG; if (d.t <= 0) { danos.RemoveAt(i); continue; } float op = Mathf.Clamp01((d.t - .1f) / 1.1f);
                Tracos.ArcoEm(W / 2, H / 2, 117.5f, 9, d.ang, Mathf.PI / 4, new Color(230 / 255f, 40 / 255f, 60 / 255f, .25f * op));   // drop-shadow
                Tracos.ArcoEm(W / 2, H / 2, 117.5f, 5, d.ang, Mathf.PI / 4, new Color(230 / 255f, 40 / 255f, 60 / 255f, .95f * op));
            }
        }
        // #anuncio: o título grande e a linha de baixo a 30% da altura; entra em 0,6 s e sai em 1,2 s
        void AnuncioHud()
        {
            if (anuncioT <= 0) return; anuncioT -= dtG; float op = Mathf.Clamp01(Mathf.Min((4.5f - anuncioT) / .6f, anuncioT / 1.2f));
            var stB = Css("serif", 68, new Color(BONE.r, BONE.g, BONE.b, op), TextAnchor.UpperCenter); var stS = Css("400", 20, new Color(201 / 255f, 191 / 255f, 174 / 255f, op), TextAnchor.UpperCenter);
            float hB = AlturaNormal(stB), hS = anuncioSub != "" ? 24 : 0, y = H * .3f - (hB + hS) / 2;
            Tracos.Brilho(W / 2, y + hB / 2 + 4, Larg(anuncio, stB) / 2 + 60, hB / 2 + 30, new Color(0, 0, 0, .55f * op));   // text-shadow 0 2px 24px
            Linha(0, y, W, hB, anuncio, stB); if (anuncioSub != "") Linha(0, y + hB, W, hS, anuncioSub, stS, .9f);
        }
        // #habNota: a nota da habilidade embaixo da mira, com a barra da cor do texto à esquerda
        void NotaHabilidade()
        {
            if (notaT <= 0) return; notaT -= dtG; var c = notaCor; c.a = Mathf.Clamp01(notaT / .5f);
            var st = Css("serif", 21, c); float w = Larg(nota, st) + 28, hT = AlturaNormal(st), h = hT + 8, x = W / 2 - w / 2, y = H / 2 + 62;
            Caixa(new Rect(x, y, w, h), new Color(8 / 255f, 9 / 255f, 11 / 255f, .8f * c.a)); Caixa(new Rect(x, y, 2, h), c); Linha(x + 14, y + 4, w - 28, hT, nota, st, .9f);
        }
    }
}
