// As peças do HUD da partida com as medidas do CSS do navegador ("interface mais quieta"): as faixas escuras de cima e de
// baixo (#hud::before/::after), o placar com o relógio (#top), a faixa dos altares em losangos (#strip, .pip, .gem), as
// tarefas da equipe (#tarefas), o feed (#feed), vida, sanidade e óbolos (#vitals), munição e ferramentas (#kit, .tool), o
// sensor (#sensor), o aviso da munição (#ammoHint), os efeitos ativos (#buffs), a disputa do ritual (#race, .plate), os
// marcadores no mundo (.mk), a marca de acerto (#hitmark), a direção do dano (#dmgDir), o anúncio (#anuncio) e a nota da
// habilidade (#habNota). v6.3, direção Nanquim: números em Big Shoulders, rótulos em IBM Plex Mono, nada de placas, e o
// movimento que o navegador faz com as classes (tranco da munição, tremor da vida, risco no altar descartado, eco piscando,
// feed letra a letra, anúncio carimbado).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static Color Hx(string h, float a = 1) { return Desenho.Hex(h, a); }
        static readonly Color STEEL = new Color(.431f, .416f, .384f), BONE = new Color(.925f, .902f, .855f), GOLD = new Color(.812f, .784f, .733f), GOLD_HI = new Color(1f, 1f, 1f), CRIMSON_HI = new Color(.89f, .149f, .18f);
        static Color CrimsonHi { get { return PlayerPrefs.GetInt("rr-dalt", 0) == 1 ? new Color(.431f, .659f, 1f) : CRIMSON_HI; } }

        // #hud::before e ::after: o escuro que sobe de baixo e desce de cima, atrás de tudo
        void FaixasEscuras()
        {
            PintarDegrade(new Rect(0, 0, W, 150), new Color(0, 0, 0, .62f), new Color(0, 0, 0, 0), true);
            PintarDegrade(new Rect(0, H - 170, W, 170), new Color(0, 0, 0, 0), new Color(0, 0, 0, .6f), true);
        }

        // #top: sua equipe e o placar | relógio, momento e regra | o placar e os adversários
        bool hudToque;   // body.toque: o HUD compacto dos controles de toque
        // transform: scale(s) com o transform-origin no ponto (ox, oy) do lugar do computador, levado para (nx, ny)
        void ComTransformacao(float ox, float oy, float nx, float ny, float s, System.Action desenho)
        {
            // o pivô no espaço depois do recorte (o IMGUI soma o deslocamento da rolagem antes do GUI.matrix)
            var c = OrigemRecorte; var m0 = GUI.matrix; GUI.matrix = m0 * Matrix4x4.TRS(new Vector3(nx + c.x, ny + c.y, 0), Quaternion.identity, new Vector3(s, s, 1)) * Matrix4x4.TRS(new Vector3(-ox - c.x, -oy - c.y, 0), Quaternion.identity, Vector3.one);
            try { desenho(); } finally { GUI.matrix = m0; }
        }
        void Topo(string papel, string relogio, string fase, bool grande, string regra, int nos, int eles)
        {
            // Nanquim: os lados em mono 12, os números em Big Shoulders 30, o relógio em Big Shoulders 40, o momento em mono 11 minúsculo
            var stLado = Css("mono", 12, MUDO); var stNum = Css("serif", 30, BONE);
            var stRel = Css("serif", 40, grande ? Hx("#e3262e") : BONE, TextAnchor.UpperCenter); var stFase = Css("mono", 11, grande ? Hx("#ff8b8f") : MUDO, TextAnchor.UpperCenter);
            var stRegra = Css("mono", 11, MUDO); fase = (fase ?? "").ToLowerInvariant();
            string nA = papel == "H" ? "Sua equipe (caça)" : "Sua equipe (culto)", nB = papel == "H" ? "Adversários (culto)" : "Adversários (caça)";
            if (hudToque) { nA = nB = ""; regra = ""; }   // body.toque: #top .side span e #regra somem
            // letter-spacing: .01em do lado (14 px, herdado também pelo número) e .05em da regra (11 px)
            const float eL = .14f, eR = .55f;
            float wNA = Larg(nos.ToString(), stNum) + nos.ToString().Length * eL + 12, wNB = Larg(eles.ToString(), stNum) + eles.ToString().Length * eL + 12;
            float lA = EspacadaNaBase(0, 0, nA, stLado, eL, false), lB = EspacadaNaBase(0, 0, nB, stLado, eL, false), wReg = EspacadaNaBase(0, 0, regra, stRegra, eR, false);
            float wRel = Mathf.Max(88, Larg(relogio, stRel), Larg(fase, stFase), wReg);
            float minLado = hudToque ? 40 : 150, ladoA = Mathf.Max(minLado, lA + wNA), ladoB = Mathf.Max(minLado, lB + wNB), total = ladoA + 22 + wRel + 22 + ladoB;
            float x0 = W / 2 - total / 2, y0 = 14, hRel = 40 * 1.05f, hFase = AlturaNormal(stFase), hRegra = regra == "" ? -1 : LN(11f), hWrap = hRel + hFase + 1 + hRegra, xw = x0 + ladoA + 22;
            if (grande) Tracos.Brilho(W / 2, y0 + hRel / 2, Larg(relogio, stRel) * .8f + 18, 30, new Color(.878f, .263f, .369f, .45f));
            NaBase(xw, y0 + (hRel - AlturaNormal(stRel)) / 2 + SubidaPx(stRel) + 2, wRel, relogio, Css("serif", 40, new Color(0, 0, 0, .45f), TextAnchor.UpperCenter));   // text-shadow 0 2px 10px
            Linha(xw, y0, wRel, hRel, relogio, stRel);
            Linha(xw, y0 + hRel, wRel, hFase, fase, stFase);
            if (regra != "") EspacadaNaBase(xw + (wRel - wReg) / 2, y0 + hRel + hFase + 1 + (hRegra - AlturaNormal(stRegra)) / 2 + SubidaPx(stRegra), regra, stRegra, eR);
            float bas = y0 + (hWrap - 36) / 2 + 30;   // a linha de base comum do nome e do número
            float xa = x0 + ladoA - wNA; NaBase(xa + 6, bas, wNA - 12, nos.ToString(), stNum); EspacadaNaBase(xa - lA, bas, nA, stLado, eL);
            float xb = xw + wRel + 22; NaBase(xb + 6, bas, wNB - 12, eles.ToString(), stNum); EspacadaNaBase(xb + wNB, bas, nB, stLado, eL);
        }
        // texto pela linha de base (como o alinhamento de linha do navegador entre tamanhos diferentes)
        void NaBase(float x, float baseline, float w, string txt, GUIStyle st, float sombra = 0)
        {
            float lh = AlturaNormal(st); Linha(x, baseline - SubidaPx(st), w, lh, txt, st, sombra);
        }

        // #strip: um losango por altar com o sinal do que a equipe sabe, o nome e a barra do ritual
        static readonly Dictionary<string, string[]> PIP = new Dictionary<string, string[]> {
            // estado: borda, fundo, cor do sinal, brilho
            // Nanquim: borda fina, osso no que a equipe sabe, vermelho no ritual; sem brilho
            { "", new[] { "#6e6a62", "rgba(10,10,10,.6)", "#a29d93", "" } }, { "awake", new[] { "#ece6da", "rgba(0,0,0,0)", "#ece6da", "" } },
            { "confirmed", new[] { "#ece6da", "rgba(236,230,218,1)", "#0a0a0a", "" } }, { "decoy", new[] { "#6e6a62", "rgba(10,10,10,.6)", "#a29d93", "" } },
            { "clean", new[] { "#6e6a62", "rgba(10,10,10,.6)", "#a29d93", "" } }, { "seen", new[] { "#6e6a62", "rgba(10,10,10,.6)", "#a29d93", "" } },
            { "purged", new[] { "#a29d93", "rgba(10,10,10,.6)", "#a29d93", "" } }, { "active", new[] { "#c4161c", "rgba(196,22,28,1)", "#ece6da", "" } },
            { "fenda", new[] { "#c4161c", "rgba(10,10,10,1)", "crimson", "" } }, { "farol", new[] { "#ece6da", "rgba(236,230,218,1)", "#0a0a0a", "" } } };
        static Color Rgba(string s)
        {
            if (s == "crimson") return CrimsonHi; if (s.StartsWith("#")) return Hx(s);
            var p = s.Substring(5, s.Length - 6).Split(','); var c = new Color(float.Parse(p[0]) / 255f, float.Parse(p[1]) / 255f, float.Parse(p[2]) / 255f, float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture)); return c;
        }
        void Faixa(string papel)
        {
            int n = g.altars.Count; float pw = 62, gap = 6, x0 = W / 2 - (n * pw + (n - 1) * gap) / 2, top = 88;
            float lhNome = 11.5f * 1.15f;
            // @keyframes ecoPisca (1,6 s): some duas vezes no começo de cada volta
            float fe = (Time.unscaledTime % 1.6f) / 1.6f, ecoOp = SuaveUI() ? 1 : (fe > .03f && fe < .09f) || (fe > .15f && fe < .21f) ? .25f : 1;
            for (int i = 0; i < n; i++)
            {
                var A = g.altars[i]; string st = "", ch = "?";
                if (A.state == "fenda") { st = "fenda"; ch = "✕"; } else if (A.state == "farol") { st = "farol"; ch = "✓"; }
                else if (papel == "H") { string k = g.know[i]; if (A.state == "active" && A.localized) { st = "active"; ch = "!"; } else if (k == "desperto") { st = "awake"; ch = "◆"; } else if (k == "confirmado") { st = "confirmed"; ch = "◆"; } else if (k == "chamariz") { st = "decoy"; ch = "◆"; } else if (k == "descartado") { st = "clean"; ch = "✕"; } else if (k == "purgado") { st = "purged"; ch = "○"; } else if (k == "visto") { st = "seen"; ch = "·"; } }
                else { if (A.state == "active") { st = "active"; ch = "!"; } else if (A.state == "awake") { st = A.decoy ? "decoy" : "awake"; ch = "◆"; } else ch = A.chosen ? "○" : "·"; }
                bool eco = papel == "H" && g.eco != null && Sim.SETORES.Any(S => S.id == g.eco.setor && S.altares.Contains(i)), escolhido = papel == "C" && A.chosen;
                var P = PIP[st]; float px = x0 + i * (pw + gap), cx = px + pw / 2, cy = top + 7;
                // o risco do altar descartado atravessa o nome quando o estado muda (@keyframes riscar, ,35 s)
                bool riscado = st == "clean" || st == "decoy" || st == "purged"; string ant; estadoPip.TryGetValue(i, out ant);
                if (ant != st) { estadoPip[i] = st; if (riscado && ant != null) Marcar("pip" + i); }
                var c0 = GUI.color; if (eco) GUI.color = new Color(c0.r, c0.g, c0.b, c0.a * ecoOp);
                Tracos.Gema(cx, cy, 14, 1, Rgba(P[1]), Rgba(P[0]));
                if (escolhido) { var m = GUI.matrix; Tracos.Gema(cx, cy, 14 + 2 * 3 + 2, 1, new Color(0, 0, 0, 0), CrimsonHi); GUI.matrix = m; }
                if (eco) Tracos.GemaTracejada(cx, cy, 14 + 2 * 3 + 2, 1, CrimsonHi);
                var stG = Css("mono500", 9, Rgba(P[2]), TextAnchor.UpperCenter); Linha(px, top, pw, 14, ch, stG);
                // o nome: mono 10,5 em minúsculas; apagado no descartado, vermelho no ritual e no eco
                var corN = riscado ? Hx("#6e6a62") : st == "active" || st == "fenda" || eco ? CrimsonHi : BONE; var stNome = Css("mono", 11, corN, TextAnchor.UpperCenter);
                string nome = A.name.ToLowerInvariant(); var ls = Quebrar(nome, stNome, pw); float hn = ls.Count * lhNome;
                for (int l = 0; l < ls.Count; l++)
                {
                    float yl = top + 14 + 3 + l * lhNome; Linha(px, yl, pw, lhNome, ls[l], stNome, .9f);
                    if (riscado) { float lw = Larg(ls[l], stNome) + 2, k = Sai(Anim("pip" + i, .35f)); Caixa(new Rect(cx - lw / 2, yl + lhNome * .52f, lw * k, 1), corN); }
                }
                GUI.color = c0;
                var rb = new Rect(px + 6, top + 14 + 3 + hn + 3, pw - 12, 2); Caixa(rb, Hx("#262626"));
                if (A.state == "active" && (papel == "C" || A.localized)) Caixa(new Rect(rb.x, rb.y, rb.width * Mathf.Clamp01((float)A.prog), 2), SANGUE);
            }
        }

        readonly Dictionary<int, string> estadoPip = new Dictionary<int, string>();
        // #tarefas: à direita, embaixo do topo
        void Tarefas(string papel)
        {
            List<Tarefa> T; if (!g.tarefas.TryGetValue(papel, out T) || T.Count == 0) return;
            // body.toque: à esquerda, 128 px do topo, 220 de largura, letra de 11 (o título 13) e linha de 1,25
            bool tq = hudToque; float x = tq ? 12 : W - 26 - 300, y = tq ? 128 : 120, w = tq ? 220 : 300, ft = tq ? 13 : 17, fi = tq ? 11 : 14.5f, lk = tq ? 1.25f : 1.35f;
            var tit = Css("mono", tq ? 11 : 11, MUDO); Linha(x, y, w, ft * lk, "tarefas da equipe", tit, .9f); y += ft * lk + Mathf.Max(tq ? 2 : 4, 3);   // a margem de baixo do b colapsa com a de cima (3) da primeira tarefa
            float lh = fi * lk;
            foreach (var t in T)
            {
                var cor = t.feita ? Hx("#6e6a62") : Hx("#c9c3b8"); var st = Css("mono", tq ? 11 : 12, cor);
                // o ○ não existe na Barlow: o navegador pega o da fonte reserva (DejaVu Sans: avanço .873em, anel de .762em
                // com traço de .056em, centro a .26em da linha de base); aqui ele é desenhado, igual em qualquer sistema
                float rc = t.feita ? 0 : fi * .873f + LargEspaco(st);
                string txt = (t.feita ? "✓ " : "") + t.nome + (t.meta > 0 ? " " + Cor(t.prog + "/" + t.meta, MUDO) : "");
                var ls = Quebrar(txt, st, w, rc);
                for (int i = 0; i < ls.Count; i++)
                {
                    float xi = i == 0 ? x + rc : x; Linha(xi, y + i * lh, w, lh, ls[i], st, .9f);
                    float bas = PixelY(y + i * lh + Mathf.Floor((lh - AlturaNormal(st)) / 2) + SubidaPx(st));
                    if (i == 0 && !t.feita) { float r = fi * (.381f - .028f), cx = x + fi * .436f, cy = bas - fi * .2605f; Tracos.Anel(cx, cy + 1, r, fi * .056f + .6f, new Color(0, 0, 0, .9f)); Tracos.Anel(cx, cy, r, fi * .056f, cor); }
                    if (t.feita) Caixa(new Rect(xi, bas - st.fontSize * .3f, Larg(ls[i], st), 1), Hx("#6e6a62"));
                }
                y += ls.Count * lh + 3;
            }
        }

        // #feed: o mais novo em cima; some depois de 6 s
        void FeedHud()
        {
            // body.toque: 214 px do topo, 12 da esquerda, 240 de largura, letra de 11,5 (13 no grande), padding 2 × 7, só 3
            bool tq = hudToque; float y = tq ? 214 : 112, x = tq ? 12 : 18, w = tq ? 240 : 330, padV = tq ? 2 : 5, padH = tq ? 7 : 10, agora = Time.unscaledTime; int n = 0;
            for (int i = feed.Count - 1; i >= 0 && n < (tq ? 3 : 6); i--)
            {
                var a = feed[i]; float idade = agora - a.t; if (idade > 6.6f) continue; n++; float op = idade < 6 ? 1 : Mathf.Clamp01(1 - (idade - 6) / .6f);
                // Nanquim: sem caixa nem barra; mono 12,5 (vermelho claro do culto), o grande em Big Shoulders maiúsculo
                bool big = a.cls.Contains("big"); var cb = a.cls.Contains("c") ? Hx("#ff8b8f") : BONE; var cor = new Color(cb.r, cb.g, cb.b, op);
                var st = big ? Css("serif", tq ? 13 : 20, cor) : Css("mono", tq ? 12 : 13, cor); string msg = big ? Maiusc(a.msg) : a.msg;
                float lh = AlturaNormal(st), h = AlturaParagrafo(msg, st, w, lh) + 2;
                // a linha nova entra letra a letra (@keyframes digitar, ,45 s em 18 passos)
                float kd = SuaveUI() || animacaoNaFoto ? 1 : Mathf.Floor(Mathf.Clamp01(idade / .45f) * 18) / 18; float yy = y;
                Recorte(new Rect(x - 2, y - 2, (w + 4) * kd, h + 4), () => Paragrafo(x, yy + 1, w, lh, msg, st, .9f), kd); y += h + 4;
            }
        }

        // #vitals: a classe, vida, sanidade e óbolos, presos embaixo à esquerda
        void Vitais(Ator m, bool economia)
        {
            // body.toque: preso em cima (12, 8) com 170 de largura (a escala de ,78 vem de fora)
            // Nanquim: a classe em mono 12 minúsculo; a vida em Big Shoulders 40 vermelha, a sanidade em 30; barras de 2 px
            float hVida = 7 + AlturaNormal(Css("serif", 40, BONE)) + 2 + 2, hSan = 7 + AlturaNormal(Css("serif", 30, BONE)) + 2 + 2;
            float x = hudToque ? 12 : 26, w = hudToque ? 170 : 250, hRole = LN(12f), hBolsa = economia ? 8 + LN(18f) : 0, y = hudToque ? 8 : H - 22 - (hRole + hVida + hSan + hBolsa);
            var cl = Regras.CLASSES[m.cls]; var stR = Css("mono", 12, m.team == "H" ? GOLD : CrimsonHi); float bas = y + (hRole - AlturaNormal(stR)) / 2 + SubidaPx(stR);
            string nomeC = cl.name.ToLowerInvariant(); NaBase(x, bas, 200, nomeC, stR, .9f); NaBase(x + Larg(nomeC, stR) + 6, bas, 150, m.team == "H" ? "caçador" : "cultista", Css("mono", 11, MUDO), .9f);
            y += hRole;
            // o tremor quando a vida cai (@keyframes ferido, ,28 s)
            int hp = Mathf.Max(0, Mathf.CeilToInt((float)m.hp)); if (ultimaVida >= 0 && hp < ultimaVida) Marcar("vida"); ultimaVida = hp;
            string san = m.sanity < 15 ? "Sanidade: pânico, você se denuncia" : m.sanity < 40 ? "Sanidade: abalado" : m.sanity < 70 ? "Sanidade: inquieto" : "Sanidade";
            Medidor(x, y, w, "Vida", hp.ToString(), (float)(m.hp / m.maxHp), BONE, 40, CrimsonHi, m.shield > 0 ? (float)(m.shield / m.maxHp) : 0, true); y += hVida;
            Medidor(x, y, w, san, Mathf.RoundToInt((float)m.sanity).ToString(), (float)(m.sanity / 100), MUDO, 30, BONE, 0, false); y += hSan;
            if (economia) { var stO = Css("mono", 11, MUDO); var stB = Css("serif", 18, BONE); float b2 = y + 8 + 18; NaBase(x, b2, 80, "Óbolos", stO); NaBase(x + Larg("Óbolos", stO) + 4 + 4, b2, 80, Mathf.FloorToInt((float)m.obolos).ToString(), stB); }
        }
        int ultimaVida = -1;
        void Medidor(float x, float y, float w, string rotulo, string valor, float frac, Color barra, int tam, Color corV, float escudo, bool vida)
        {
            y += 7; var stL = Css("mono", 11, MUDO); float hv = AlturaNormal(Css("serif", tam, BONE));
            // .meter .lab: align-items baseline: o rótulo na linha de base do número
            float bas = y + SubidaPx(Css("serif", tam, BONE)); NaBase(x, bas, w - 60, rotulo, stL);
            float dx = 0; Color cv = corV;
            if (vida) { float t = Anim("vida", .28f); if (t < 1) { dx = t < .3f ? Mathf.Lerp(-3, 3, t / .3f) : t < .6f ? Mathf.Lerp(3, -2, (t - .3f) / .3f) : Mathf.Lerp(-2, 0, (t - .6f) / .4f); cv = Color.Lerp(BONE, corV, t); } }
            NaBase(x + w - 120 + dx, bas, 120, valor, Css("serif", tam, cv, TextAnchor.UpperRight)); y += hv + 2;
            var tr = new Rect(x, y, w, 2); Caixa(tr, new Color(.925f, .902f, .855f, .14f)); frac = Mathf.Clamp01(frac);
            if (frac > 0) Caixa(new Rect(x, y, w * frac, 2), barra);
            if (escudo > 0) Caixa(new Rect(tr.xMax - w * Mathf.Clamp01(escudo), y, w * Mathf.Clamp01(escudo), 2), new Color(.925f, .902f, .855f, .5f));
        }

        // #kit: munição (ou fervor) e a fileira de ferramentas, presos embaixo à direita; #sensor em cima dele
        void KitHud(Ator m)
        {
            // body.toque: sem a fileira de ferramentas; a munição com o canto de baixo à direita em (W - 190, H - 96)
            // as ferramentas (buildTools), da direita para a esquerda; a fileira tem a altura da mais alta (flex estica todas)
            var F = Regras.FEITICOS[m.feitico]; var L = new List<object[]>();
            if (m.team == "H") { L.Add(new object[] { EntradaUnity.Nome("sensor"), "Sensor", null, (float)(m.sensorCd / 15), false }); L.Add(new object[] { EntradaUnity.Nome("lantern"), "Lanterna", null, (float)((100 - m.carga) / 100), m.lantern }); L.Add(new object[] { EntradaUnity.Nome("flare"), "Flare", m.flares.ToString(), 0f, false }); }
            else { L.Add(new object[] { "☉", "Reagentes", m.reag.ToString(), 0f, false }); L.Add(new object[] { EntradaUnity.Nome("use"), "Chamariz", g.decoys.ToString(), 0f, false }); }
            string pront = m.abilCd > 0 ? "" : ProntidaoHab(m); L.Add(new object[] { EntradaUnity.Nome("abil"), F.nome, null, (float)(m.abilCd / F.cd), false, pront });
            // Nanquim: a ferramenta é texto numa linha ("Q sensor": a tecla em osso, o nome em mono apagado), sem caixa;
            // a munição em Big Shoulders 900 de 88 px (linha de ,85) com o resto em 26 px
            var stK = Css("mono500", 12, BONE); float lhK = 0; foreach (var t in L) lhK = Mathf.Max(lhK, AlturaNormalCom((string)t[0], stK));
            float xd = hudToque ? W - 190 : W - 26, hTool = lhK + 6, yTools = H - 20 - hTool, hAm = Mathf.Round(88 * .85f), yAmmo = hudToque ? H - 96 - hAm : yTools - 6 - hAm;
            string grande, peq; if (m.team == "H") { if (m.reloadT > 0) { grande = ""; peq = "recarregando…"; } else { grande = m.ammo.ToString(); peq = "/ " + m.reserve; } } else { grande = Mathf.FloorToInt((float)m.fervor).ToString(); peq = "Fervor"; }
            int nAm = m.team == "H" ? m.ammo : Mathf.FloorToInt((float)m.fervor); if (ultimaMunicao >= 0 && nAm < ultimaMunicao) Marcar("municao"); ultimaMunicao = nAm;
            var stG = Css("display", 88, BONE); var stP = Css("display400", 26, MUDO); float wP = Larg(peq, stP), wG = grande.Length > 0 ? Larg(grande, stG) + LargEspaco(stG) : 0, bas = yAmmo + hAm - 6;
            // o tranco (@keyframes tranco, ,18 s): escala 1,08 e 2 px à direita, com a origem no canto de baixo à direita
            float tk = Anim("municao", .18f), sk = Mathf.Lerp(1.08f, 1, tk), dk = Mathf.Lerp(2, 0, tk);
            ComTransformacao(xd, bas, xd + dk, bas, sk, () => { if (grande.Length > 0) NaBase(xd - wP - wG, bas, wG, grande, stG, .6f); NaBase(xd - wP, bas, wP + 2, peq, stP, .6f); });
            float x = xd;
            for (int i = hudToque ? -1 : L.Count - 1; i >= 0; i--)
            {
                var t = L[i]; bool on = (bool)t[4]; string tecla = (string)t[0], nome = (string)t[1] + (t[2] != null ? " " + t[2] : ""), pr = t.Length > 5 ? (string)t[5] : "";
                // .tool.vazio: o feitiço não faria nada agora (apagado, com o motivo em vermelho)
                if (pr != "") nome += " " + Cor(pr, CrimsonHi);
                var stTk = on ? Css("mono500", 12, CrimsonHi) : pr != "" ? Css("mono500", 12, Hx("#6e6a62")) : stK; var stN = Css("mono", 12, on ? BONE : pr != "" ? Hx("#6e6a62") : MUDO);
                float tw = Larg(tecla, stTk) + 5 + Larg(nome, stN) + 8; x -= tw;
                var r = new Rect(x, yTools, tw, hTool); float lk = AlturaNormalCom(tecla, stTk);
                Linha(r.x + 4, r.y + 2, tw, lk, tecla, stTk, .9f); Linha(r.x + 4 + Larg(tecla, stTk) + 5, r.y + 2, tw, lk, nome, stN, .9f);
                float cd = Mathf.Clamp01((float)t[3]); if (cd > 0) Caixa(new Rect(r.x, r.yMax - hTool * cd, tw, hTool * cd), new Color(10 / 255f, 10 / 255f, 10 / 255f, .7f));
                x -= 5;
            }
        }
        int ultimaMunicao = -1;
        // o que o feitiço do culto faria agora (os números do UseAbility): a Runa só acelera quem canaliza um ritual em
        // andamento; o Empurrão pega quem está a 3,8 m, no cone à frente
        string ProntidaoHab(Ator m)
        {
            if (m.feitico == "runa") return g.altars.Any(A => A.state == "active" && Sim.InCircle(m, A)) ? "" : "fora do ritual";
            if (m.feitico == "empurrao")
            {
                double fx = -System.Math.Sin(m.yaw), fz = -System.Math.Cos(m.yaw);
                foreach (var e in g.actors)
                {
                    if (e.team == m.team || e.st != "alive" || e.hidden) continue; double dx = e.x - m.x, dz = e.z - m.z, d = System.Math.Sqrt(dx * dx + dz * dz);
                    if (d <= 3.8 && d > .01 && (dx * fx + dz * fz) / d >= .34) return "";
                }
                return "ninguém à frente";
            }
            return "";
        }
        // #sensor: o sinal do Sensor Áurico em cima do kit (no toque: 190 da direita, 150 de baixo, 170 de largura, sem escala)
        void SensorHud(Ator m)
        {
            if (m.team != "H") return;
            float xd = hudToque ? W - 190 : W - 26, ws = hudToque ? 170 : 220, baixo = hudToque ? 150 : 128;
            if (sensorT > 0) sensorT -= dtG; float op = sensorT > 0 ? 1 : .45f;
            string s = sensorV > .6 ? "Sinal forte" : sensorV > .3 ? "Sinal médio" : sensorV > .02 ? "Sinal fraco" : "Nenhum sinal";
            var stS = Css("mono", 13, new Color(BONE.r, BONE.g, BONE.b, op), TextAnchor.UpperRight); float hS = AlturaNormal(stS), ys = H - baixo - (hS + 4 + 2);
            Linha(xd - ws, ys, ws, hS, s, stS, .9f); var tr = new Rect(xd - ws, ys + hS + 4, ws, 2); Caixa(tr, new Color(1, 1, 1, .1f * op));
            if (sensorV > 0) Caixa(new Rect(tr.x, tr.y, tr.width * Mathf.Clamp01((float)sensorV), 2), new Color(BONE.r, BONE.g, BONE.b, op));
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
            // #race: 440 de largura, padding 10 16 12, 150 px do fundo; body.toque: min(360px, 40vw), padding 5 10, 6 px do fundo,
            // título de 14 e fileiras de 12 com margin-top 2
            bool tq = hudToque; int fT = tq ? 14 : 17, fR = tq ? 12 : 14; float pv = tq ? 5 : 10, pb = tq ? 5 : 12, ph = tq ? 10 : 16, mt = tq ? 2 : 4;
            // Nanquim: fundo preto chapado de ,72, o nome em Big Shoulders maiúsculo (20 px), as fileiras em mono 12
            fT = tq ? 14 : 20; fR = 12;
            var stT = Css("serif", fT, BONE, TextAnchor.UpperCenter); float hT = AlturaNormal(stT), hR = LN(fR), h = pv + hT + 6 + 2 * (mt + hR) + 5 + 16 + pb, w = tq ? Mathf.Min(360, W * .4f) : 440;
            var caixa = new Rect(W / 2 - w / 2, H - (tq ? 6 : 150) - h, w, h); Caixa(caixa, new Color(10 / 255f, 10 / 255f, 10 / 255f, .72f));
            float x = caixa.x + ph, y = caixa.y + pv, wc = w - 2 * ph; Linha(x, y, wc, hT, Maiusc(R.name), stT); y += hT + 6;
            Fileira(x, y + mt, wc, hR, fR, "Ritual", (float)R.prog, SANGUE, true); y += mt + hR;
            Fileira(x, y + mt, wc, hR, fR, "Selamento", (float)R.seal, BONE, false); y += mt + hR;
            Linha(x, y + 5, wc, 16, nota, Css("mono", 12, MUDO, TextAnchor.UpperCenter));
        }
        void Fileira(float x, float y, float w, float lh, int tam, string rotulo, float v, Color cor, bool marcas)
        {
            var st = Css("mono", tam, BONE); Linha(x, y, 78, lh, rotulo, st);
            var tr = new Rect(x + 78 + 8, y + (lh - 3) / 2, w - 78 - 44 - 16, 3); Caixa(tr, Hx("#222222"));
            if (v > 0) Caixa(new Rect(tr.x, tr.y, tr.width * Mathf.Clamp01(v), 3), cor);
            if (marcas) foreach (var f in new[] { .25f, .75f }) Caixa(new Rect(tr.x + tr.width * f, tr.y - 3, 2, 9), new Color(.925f, .902f, .855f, .6f));
            Linha(tr.xMax + 8, y, 44, lh, Mathf.FloorToInt(v * 100) + "%", Css("mono500", tam, BONE));
        }

        // .mk: o nome, a linha de baixo e o losango, com o pé no ponto do mundo; atrás da câmera vai para a borda mais perto
        void Marcador(Vector3 p, string txt, string sub, string cls, bool prender)
        {
            var sp = cam.WorldToScreenPoint(new Vector3(p.x, p.y, -p.z)); bool atras = sp.z < 0; float esc = Screen.height / 1080f;
            float sx = sp.x / esc, sy = H - sp.y / esc;
            if (atras) { if (!prender) return; sx = W - sx < W / 2 ? 70 : W - 70; sy = H * .45f; }
            if (prender) { sx = Mathf.Clamp(sx, 70, W - 70); sy = Mathf.Clamp(sy, 120, H - 200); }
            Color ct = cls == "caido" ? CrimsonHi : cls == "npc" ? Hx("#e8d6a8") : cls == "ping" ? Hx("#8ee8ff") : cls == "obj" ? Hx("#ff8b8f") : BONE;
            var st = Css(cls == "caido" || cls == "ping" ? "mono500" : "mono", 12, ct, TextAnchor.UpperCenter); var ss = Css("mono", 11, MUDO, TextAnchor.UpperCenter);
            float lh = 12 * 1.15f, ls = 11 * 1.15f, tam = cls == "ping" ? 11 : cls == "obj" ? 12 : 9, h = lh + (sub != null ? ls : 0) + 3 + tam, y = sy - h;
            float pulso = .5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / (cls == "ping" ? 1.2f : 1f));
            Linha(sx - 160, y, 320, lh, txt, st, .9f); y += lh; if (sub != null) { Linha(sx - 160, y, 320, ls, sub, ss, .9f); y += ls; }
            float cy = y + 3 + tam / 2;
            if (cls == "npc") Tracos.Anel(sx, cy, 4.5f - 1, 2, GOLD);
            else if (cls == "ping" || cls == "obj") { if (cls == "ping") Tracos.Brilho(sx, cy, 12 + 8 * pulso, 12 + 8 * pulso, new Color(.56f, .91f, 1f, .5f * pulso)); Tracos.Gema(sx, cy, tam, 2, new Color(0, 0, 0, 0), cls == "ping" ? Hx("#8ee8ff") : CrimsonHi); }
            else { if (cls == "caido") Tracos.Brilho(sx, cy, 10 + 10 * pulso, 10 + 10 * pulso, new Color(.878f, .263f, .369f, .8f * pulso)); Tracos.Gema(sx, cy, tam, 0, cls == "cult" || cls == "caido" ? SANGUE : BONE, new Color(0, 0, 0, 0)); }
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
            // b: clamp(40px, 6vw, 68px) (34 px no toque); o span em linha (20 px, 14 no toque) divide a linha com o strut do
            // corpo (Barlow 16: 16 de subida, 3 de descida)
            // Nanquim: Big Shoulders 900 em maiúsculas, carimbado ao entrar (,32 s); a linha de baixo em mono 15
            int tB = hudToque ? 34 : Mathf.RoundToInt(Mathf.Clamp(W * .06f, 40, 68)), tS = hudToque ? 14 : 15;
            var stB = Css("display", tB, new Color(BONE.r, BONE.g, BONE.b, op), TextAnchor.UpperCenter); var stS = Css("mono", tS, new Color(201 / 255f, 195 / 255f, 184 / 255f, op), TextAnchor.UpperCenter);
            string tit = Maiusc(anuncio); if (anuncio != anuncioMarcado) { anuncioMarcado = anuncio; Marcar("anuncio"); }
            float sS = Mathf.Max(16, SubidaPx(stS)), hB = AlturaNormal(stB), hS = anuncioSub != "" ? sS + Mathf.Max(3, AlturaNormal(stS) - SubidaPx(stS)) : 0, y = H * .3f - (hB + hS) / 2;
            Tracos.Brilho(W / 2, y + hB / 2 + 4, Larg(tit, stB) / 2 + 60, hB / 2 + 30, new Color(0, 0, 0, .55f * op));   // text-shadow 0 2px 24px
            float yb = y; ComCarimbo("anuncio", .32f, 0, W / 2, y + hB / 2, () => Linha(0, yb, W, hB, tit, stB)); if (anuncioSub != "") NaBase(0, y + hB + sS, W, anuncioSub, stS, .9f);
        }
        string anuncioMarcado;
        // #habNota: a nota da habilidade embaixo da mira, com a barra da cor do texto à esquerda
        void NotaHabilidade()
        {
            if (notaT <= 0) return; notaT -= dtG; var c = notaCor; c.a = Mathf.Clamp01(notaT / .5f);
            // Nanquim: mono 14 sem caixa, com sombra
            var st = Css("mono", 14, c); float w = Larg(nota, st) + 28, hT = AlturaNormal(st), x = W / 2 - w / 2, y = H / 2 + 62;
            Linha(x + 14, y + 4, w - 28, hT, nota, st, .9f);
        }
    }
}
