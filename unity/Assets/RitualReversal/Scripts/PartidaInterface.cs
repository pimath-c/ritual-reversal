// Interface da v5.3 em diante (client/index.html), em IMGUI: legendas dos sons com direção e distância e a bússola no
// topo (aliados, rituais revelados, sinalizadores, mercadores, pings). As mesmas regras do navegador: o que se ouve
// depende do alcance e da oclusão do som (Som.Oclusao), e a bússola tem o norte em -z, com 70° para cada lado.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static readonly Color OURO_VIVO = new Color(1f, 1f, 1f), CARMIM_HI = new Color(.89f, .149f, .18f), MUDO = new Color(.635f, .616f, .576f), TEXTO_LEG = new Color(.788f, .765f, .722f);

        // ---------------------------------------------------------------------------------------------------------------
        // Legendas: o que se ouve, de onde vem e a que distância (LEG_TXT: texto, alcance, cor)
        class Leg { public string txt, cor; public float x = float.NaN, z = float.NaN, t; public int ordem; }
        int legOrdem;
        readonly Dictionary<string, Leg> LEG = new Dictionary<string, Leg>();
        static readonly Dictionary<string, object[]> LEG_TXT = new Dictionary<string, object[]>
        {
            { "shot", new object[] { "Tiro", 55f, "" } }, { "sigil", new object[] { "Sigilo", 35f, "cult" } }, { "step", new object[] { "Passos", 16f, "" } },
            { "passoForte", new object[] { "Passos pesados", 24f, "" } }, { "passoLeve", new object[] { "Passos leves", 5f, "" } }, { "collect", new object[] { "Algo foi coletado", 26f, "" } },
            { "flare", new object[] { "Sinalizador aceso", 20f, "" } }, { "rune", new object[] { "Runa conjurada", 30f, "cult" } }, { "arrasto", new object[] { "Arrasto de ritual", 45f, "rit" } },
            { "push", new object[] { "Empurrão", 28f, "cult" } }, { "flash", new object[] { "Clarão", 30f, "" } }, { "purify", new object[] { "Purificação", 30f, "" } },
            { "reload", new object[] { "Recarga", 18f, "" } }, { "estouro", new object[] { "Sigilo estourou", 22f, "cult" } },
            { "porta_abre", new object[] { "Porta rangendo", 20f, "" } }, { "porta_fecha", new object[] { "Porta batendo", 24f, "" } }, { "estalo", new object[] { "Quadro de força", 50f, "" } }, { "corvo", new object[] { "Corvos levantam voo", 40f, "" } },
            { "whisper", new object[] { "Sussurros", 26f, "cult" } }, { "evp", new object[] { "Sussurros num altar", 26f, "cult" } }, { "hab", new object[] { "Feitiço", 34f, "" } }
        };
        static readonly string[] LADOS = { "à frente", "à frente, à direita", "à direita", "atrás, à direita", "atrás", "atrás, à esquerda", "à esquerda", "à frente, à esquerda" };
        static bool LegendasLigadas { get { return PlayerPrefs.GetInt("rr-legendas", 1) == 1; } }
        void Legenda(string k, float x, float z, string txt = null, string cor = null, float raio = 0)
        {
            if (!LegendasLigadas || g == null || g.phase != "play") return;
            object[] L; if (!LEG_TXT.TryGetValue(k, out L)) L = new object[] { k, 40f, "" };
            if (!float.IsNaN(x))
            {
                var c = CamP(); float d = Mathf.Sqrt((x - c.x) * (x - c.x) + (z - c.z) * (z - c.z)), gg = Mathf.Clamp01(1 - d / (raio > 0 ? raio : (float)L[1])); gg *= gg;
                if (gg > 0) gg *= Som.Oclusao(c.x, c.z, x, z); if (gg < .012f) return;
            }
            string chave = k + (float.IsNaN(x) ? "" : ":" + Math.Floor(x / 10 + .5) + "," + Math.Floor(z / 10 + .5));
            Leg it; if (!LEG.TryGetValue(chave, out it)) { it = new Leg { ordem = ++legOrdem }; LEG[chave] = it; }
            it.txt = txt ?? (string)L[0]; it.cor = cor ?? (string)L[2]; it.x = x; it.z = z; it.t = k.StartsWith("rit") ? .6f : 2.8f;
            if (LEG.Count > 6) { string pior = null; foreach (var kv in LEG) if (pior == null || kv.Value.t < LEG[pior].t) pior = kv.Key; LEG.Remove(pior); }
        }
        // #legendas: cada .lg é uma fileira flex (padding 2 12 2 8, vão de 7 px, fundo .58) com a seta de 12 × 14 girada para a
        // origem (ou o ponto de 8 px quando é aqui, ou o anel quando não tem lugar), o texto de 15 px e o small de 13 px à parte.
        // A coluna sobe a partir de 26 px da esquerda e 172 do fundo; no toque desce a partir do canto (W - 200, 64), alinhada à
        // direita e com scale(.8)
        void Legendas()
        {
            if (LEG.Count == 0) return; var P = CamP(); float yaw = (float)lookYaw, dt = dtG;
            var mortos = new List<string>(); var vivos = new List<Leg>();
            foreach (var kv in LEG) { kv.Value.t -= dt; if (kv.Value.t <= 0) mortos.Add(kv.Key); else vivos.Add(kv.Value); }
            foreach (var k in mortos) LEG.Remove(k);
            vivos.Sort((a, b) => a.ordem.CompareTo(b.ordem));   // a ordem de chegada (a do Map do navegador)
            if (vivos.Count == 0) return;
            if (hudToque) ComTransformacao(W - 200, 64, W - 200, 64, .8f, () => FileirasLegendas(vivos, P, yaw, true)); else FileirasLegendas(vivos, P, yaw, false);
        }
        void FileirasLegendas(List<Leg> vivos, Vector3 P, float yaw, bool toque)
        {
            float lh = LN(15f), lhS = LN(13f), h = lh + 4, y = toque ? 64 : H - 172 - h;
            foreach (var it in vivos)
            {
                float op = Mathf.Min(1, it.t / .5f); string marca = "sem", resto = ""; float ang = 0; bool perto = false;
                if (!float.IsNaN(it.x))
                {
                    float dx = it.x - P.x, dz = it.z - P.z, d = Mathf.Sqrt(dx * dx + dz * dz), fr = dx * -Mathf.Sin(yaw) + dz * -Mathf.Cos(yaw), di = dx * Mathf.Cos(yaw) + dz * -Mathf.Sin(yaw);
                    ang = Mathf.Atan2(di, fr); perto = d < 10; marca = d < 3 ? "aqui" : "seta";
                    resto = d < 3 ? "aqui" : LADOS[((Mathf.RoundToInt(ang / (Mathf.PI / 4)) % 8) + 8) % 8] + ", " + Mathf.RoundToInt(d) + " m";
                }
                // .perto vem depois de .rit no CSS: perto, até o ritual fica branco
                Color ct = perto ? Color.white : it.cor == "rit" ? new Color(1f, .604f, .671f) : TEXTO_LEG; ct.a = op;
                Color cm = it.cor == "rit" ? CARMIM_HI : marca == "seta" && it.cor == "cult" ? new Color(.773f, .549f, 1f) : OURO_VIVO; cm.a = op;
                var st = Css("mono", 13, ct); var sm = Css("mono", 11, new Color(MUDO.r, MUDO.g, MUDO.b, op));   // Nanquim: mono 12,5 e 11,5
                float wt = Larg(it.txt, st), ws = resto != "" ? Larg(resto, sm) : 0, w = 8 + 12 + 7 + wt + (resto != "" ? 7 + ws : 0) + 12;
                float x = toque ? W - 200 - w : 26; Caixa(new Rect(x, y, w, h), new Color(10 / 255f, 10 / 255f, 10 / 255f, .5f * op));
                float cx = x + 8 + 6, cy = y + 2 + lh / 2;
                if (marca == "seta") Tracos.SetaSom(cx, cy, ang, cm);
                else if (marca == "aqui" || it.cor == "rit") Tracos.Disco(cx, cy, 4, cm);   // .rit i.sem também é cheio
                else Tracos.Anel(cx, cy, 3.5f, 1, cm);
                Linha(x + 27, y + 2, wt + 4, lh, it.txt, st, .8f);
                if (resto != "") Linha(x + 27 + wt + 7, y + 2 + (lh - lhS) / 2, ws + 4, lhS, resto, sm, .8f);
                y += toque ? h + 3 : -(h + 3);
            }
        }

        // legendas que nascem dos eventos (handleEvents do navegador): o que o jogador não fez e não é passo de aliado
        void LegendaDoEvento(Evento e, string papel, int eu)
        {
            float x = (float)e.Num("x"), z = (float)e.Num("z");
            switch (e.type)
            {
                case "sfx":
                    {
                        string k = e.Str("k"); int a = e.Int("a"); if (a == eu) break;
                        var em = g.actors.Find(b => b.id == a); bool aliado = em != null && em.team == papel;
                        if (!(aliado && (k == "step" || k == "passoForte" || k == "reload" || k == "collect"))) Legenda(k, x, z); break;
                    }
                case "burst": Legenda("estouro", x, z); break;
                case "reload": if (e.Int("a") != eu) { var em = g.actors.Find(b => b.id == e.Int("a")); if (!(em != null && em.team == papel)) Legenda("reload", x, z); } break;
                case "presage": Legenda("presage", float.NaN, float.NaN, "Presságio: um ritual vai começar", "rit"); break;
                case "evp": Legenda("evp", x, z); break;
                case "momento": Legenda("bell", float.NaN, float.NaN, "O sino da catedral"); break;
                case "hab":
                    if (e.Int("a") != eu) { string f = e.Str("f"); FeiticoDef F; string nome = f != null && Regras.FEITICOS.TryGetValue(f, out F) ? F.nome : "Feitiço"; Legenda("hab", x, z, nome, Array.IndexOf(Regras.FEIT_BY_TEAM["C"], f) >= 0 ? "cult" : ""); }
                    break;
            }
        }
        // rituais ouvidos (a cada quadro): sussurros, pulsação e coro, conforme o progresso
        void LegendasDosRituais(string papel)
        {
            var c = CamP();
            for (int i = 0; i < g.altars.Count; i++)
            {
                var A = g.altars[i]; if (A.state != "active" || (papel == "H" && !A.localized)) continue;
                float raio = (float)CFG.ritRad[Math.Max(1, A.lastN)] * 1.6f, d = Mathf.Sqrt((float)((A.x - c.x) * (A.x - c.x) + (A.z - c.z) * (A.z - c.z))), gg = Mathf.Clamp01(1 - d / raio); gg *= gg;
                if (gg > 0) gg *= Som.Oclusao(c.x, c.z, A.x, A.z); if (A.localized) gg = Mathf.Max(gg, .2f);
                if (gg <= .03f) continue; double p = A.prog;
                string t = A.grande ? "Grande Ritual: " + (p >= .75 ? "coro" : p >= .25 ? "pulsação" : "sussurros") : p >= .75 ? "Coro do ritual" : p >= .25 ? "Pulsação do ritual" : "Sussurros do ritual";
                Legenda("rit" + i, (float)A.x, (float)A.z, t, "rit", 1e9f);
            }
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Bússola: o rumo de um ponto é o ângulo a partir do norte (-z), no sentido horário
        const float BUS_W = 440, BUS_MEIO = 70 * Mathf.Deg2Rad;
        void Bussola(Ator m, string papel)
        {
            // #bussola: 440 × 26 em top 148, com as pontas sumindo (mask 18% / 82%) e o triângulo do rumo embaixo
            var P = CamP(); float rumo = -(float)lookYaw, k = BUS_W / 2 / BUS_MEIO, x0 = W / 2 - BUS_W / 2, y0 = 148;
            Func<float, float> px = b => { float r = b - rumo; r = Mathf.Atan2(Mathf.Sin(r), Mathf.Cos(r)); return Mathf.Abs(r) > BUS_MEIO ? float.NaN : BUS_W / 2 + r * k; };
            Func<float, float> mascara = x => { float f = x / BUS_W; return Mathf.Clamp01(Mathf.Min(f / .18f, (1 - f) / .18f)); };
            Func<Color, float, Color> A = (c, a) => { c.a *= a; return c; };
            for (int gr = 0; gr < 360; gr += 15)
            {
                float x = px(gr * Mathf.Deg2Rad); if (float.IsNaN(x)) continue; float op = mascara(x); string nome = null;
                switch (gr) { case 0: nome = "N"; break; case 45: nome = "NE"; break; case 90: nome = "L"; break; case 135: nome = "SE"; break; case 180: nome = "S"; break; case 225: nome = "SO"; break; case 270: nome = "O"; break; case 315: nome = "NO"; break; }
                // os pontos e os marcadores são <i>: itálico (sintético, a Barlow e a DejaVu daqui não têm o itálico)
                if (nome != null) Linha(x0 + x - 30, y0 + 6, 60, LN(13f), nome, Css("mono500", 11, A(gr == 0 ? CrimsonHi : Hx("#c9c3b8"), op), TextAnchor.UpperCenter), .9f);
                else Caixa(new Rect(x0 + x, y0, 1, 6), new Color(.91f, .878f, .816f, .35f * op));
            }
            Action<double, double, Color, string, bool> mk = (x, z, c, ch, grande) =>
            {
                float b = Mathf.Atan2((float)(x - P.x), -(float)(z - P.z)), q = px(b); if (float.IsNaN(q)) return;
                int tam = grande ? 15 : 12; Linha(x0 + q - 20, y0 + (grande ? 1 : 3), 40, tam, ch, Css("400", tam, A(c, mascara(q)), TextAnchor.UpperCenter, FontStyle.Italic), .9f);
            };
            foreach (var a in g.actors)
            {
                if (a == m || a.team != m.team || a.st == "dead" || EscondidoNoCliente(a)) continue;
                mk(a.x, a.z, a.st == "down" ? CrimsonHi : m.team == "C" ? Hx("#ff8b8f") : GOLD_HI, a.st == "down" ? "✚" : "◆", false);
            }
            foreach (var Al in g.altars) { if (Al.state != "active" || (papel == "H" && !Al.localized)) continue; mk(Al.x, Al.z, CrimsonHi, "✶", true); }
            if (g.lab == null || g.lab.economia) foreach (var n in Mapa.NPCS) { if ((n.time == null || n.time == m.team) && Mathf.Sqrt((float)((n.x - P.x) * (n.x - P.x) + (n.z - P.z) * (n.z - P.z))) < 60) mk(n.x, n.z, Hx("#e8d6a8"), "○", false); }
            if (g.flares != null) foreach (var f in g.flares) if (f.t > 0) mk(f.x, f.z, Hx("#ffb070"), "✦", false);
            foreach (var p in pings) mk(p.x, p.z, Hx("#8ee8ff"), "⬥", true);
            if (tutAtivo && tutAlvo != null && !tutFim) mk(tutAlvo.x, tutAlvo.z, CrimsonHi, "◉", true);   // o objetivo do tutorial
            for (int i = 0; i < 4; i++) Caixa(new Rect(W / 2 - 1 - i, y0 + 26 - 4 + i, 2 + 2 * i, 1), GOLD_HI);   // o ::after: triângulo de 8 × 4
        }
        // ---------------------------------------------------------------------------------------------------------------
        // Lance da noite (v5.7): uma faixa curta, uma de cada vez, logo abaixo do anúncio
        class LanceF { public string tit, sub; public bool c, meu; }
        readonly List<LanceF> filaLance = new List<LanceF>(); LanceF lanceAtual; float lanceT = -1;
        bool LanceTexto(Evento e, out string tit, out string sub)
        {
            int ai = e.Int("altar"); return Noite.LanceFrase(e.Str("tipo"), Nomes(e["quem"]), ai >= 0 && ai < g.altars.Count ? g.altars[ai].name : "", e["v"] != null ? e.Num("v") : 0, out tit, out sub);
        }
        void MostrarLance(Evento e)
        {
            string tit, sub; if (!LanceTexto(e, out tit, out sub)) return; var m = Eu(); var q = Nomes(e["quem"]);
            filaLance.Add(new LanceF { tit = tit, sub = sub, c = Noite.LanceDoCulto(e.Str("tipo"), e.Str("team")), meu = m != null && q.Contains(m.name) });
        }
        Texture2D faixaLance;
        Texture2D TexFaixaLance
        {
            get
            {
                if (faixaLance != null) return faixaLance; int n = 100; faixaLance = new Texture2D(n, 1, TextureFormat.RGBA32, false); var p = new Color[n];
                for (int i = 0; i < n; i++) { float f = (i + .5f) / n, a = f < .18f ? f / .18f : f > .82f ? (1 - f) / .18f : 1; p[i] = new Color(12 / 255f, 10 / 255f, 8 / 255f, .82f * a); }
                faixaLance.SetPixels(p); faixaLance.Apply(false, true); faixaLance.wrapMode = TextureWrapMode.Clamp; return faixaLance;
            }
        }
        void FaixaLance()
        {
            float dt = dtG; lanceT -= dt;
            if (lanceT <= 0) { if (lanceAtual != null) { lanceAtual = null; lanceT = 0; } if (filaLance.Count > 0 && lanceT < -.9f) { lanceAtual = filaLance[0]; filaLance.RemoveAt(0); lanceT = 4.2f; som.Tocar("lance"); } }
            if (lanceAtual == null) return;
            // #lance: faixa que esmaece nas pontas (18% / 82%), bordas douradas (carmim no lance do culto), padding 4 × 44 × 6
            float op = Mathf.Clamp01(Mathf.Min((4.2f - lanceT) / .25f, lanceT / .9f)), y = H * .3f + 64;
            Color cs = lanceAtual.c ? CrimsonHi : GOLD_HI, ct = lanceAtual.meu ? GOLD_HI : BONE, cb = Hx("#c9c3b8"); cs.a = ct.a = cb.a = op;
            var stS = Css("mono", 11, cs); var stB = Css("serif", 24, ct, TextAnchor.UpperCenter); var stP = Css("mono", 13, cb, TextAnchor.UpperCenter);   // Nanquim
            string peq = "LANCE DA NOITE"; float esp = 11 * .24f, wS = 0; foreach (var ch in peq) wS += (ch == ' ' ? Larg("a a", stS) - Larg("aa", stS) : Larg(ch.ToString(), stS)) + esp;
            float hS = LN(11f), hB = AlturaNormal(stB), hP = LN(16f), w = Mathf.Max(wS, Mathf.Max(Larg(lanceAtual.tit, stB), Larg(lanceAtual.sub, stP))) + 88, h = 4 + hS + hB + hP + 6, x = W / 2 - w / 2;
            var o = GUI.color; GUI.color = new Color(1, 1, 1, op); GUI.DrawTexture(new Rect(x, y, w, h), TexFaixaLance); GUI.color = o;
            var borda = lanceAtual.c ? new Color(.878f, .263f, .369f, .6f * op) : new Color(.953f, .835f, .557f, .55f * op); Caixa(new Rect(x, y, w, 1), borda); Caixa(new Rect(x, y + h - 1, w, 1), borda);
            Espacada(W / 2 - wS / 2 + esp / 2, y + 4, hS, peq, stS, esp);
            Linha(x, y + 4 + hS, w, hB, Maiusc(lanceAtual.tit), stB); Linha(x, y + 4 + hS + hB, w, hP, lanceAtual.sub, stP);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Queda e espectador (v5.3): quem te derrubou, de onde e com o quê; morto, a câmera acompanha um aliado de pé
        // (clique troca) ou gira devagar sobre onde você caiu
        class GolpeR { public int s; public float x, z, t; }
        Texture2D texQueda;
        Texture2D TexQueda
        {
            get
            {
                // radial-gradient(ellipse at center, rgba(40,0,8,.3), rgba(0,0,0,.75)): até o canto mais longe
                if (texQueda != null) return texQueda; int N = 96; texQueda = new Texture2D(N, N, TextureFormat.RGBA32, false); var p = new Color[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float dx = (x + .5f) / N * 2 - 1, dy = (y + .5f) / N * 2 - 1, d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f); p[y * N + x] = Color.Lerp(new Color(40 / 255f, 0, 8 / 255f, .3f), new Color(0, 0, 0, .75f), d); }
                texQueda.SetPixels(p); texQueda.Apply(false, true); texQueda.wrapMode = TextureWrapMode.Clamp; return texQueda;
            }
        }
        GolpeR golpe; string espQuem = "", espPrev = "alive"; int espIdx, espN; Vector3? espPos; float espAng; Ator espAlvo;
        void RegistrarGolpe(Evento e) { if (e["sx"] == null) return; golpe = new GolpeR { s = e.Int("s"), x = (float)e.Num("sx"), z = (float)e.Num("sz"), t = Time.time }; }
        void EstadoProprio(Ator m)
        {
            if (m.st == espPrev) return;
            if (m.st == "down" && espPrev == "alive") StartCoroutine(Depois(.25f, () => som.Tocar("queda_corpo")));
            if (m.st == "dead" && espPrev == "down") { var E = mundo.ExecRecente(m.id); if (E != null && E.k > .6f) mundo.FxExecucao((float)m.x, (float)m.z, m.team, E); }
            if ((m.st == "down" || m.st == "dead") && espPrev == "alive") RegistrarQueda(m);
            if (m.st == "alive") espQuem = ""; espPrev = m.st;
        }
        void RegistrarQueda(Ator m)
        {
            espQuem = "";
            LT.Marco(g, m.st == "down" ? "queda" : "morte", m.st == "down" ? "Você caiu" : "Você morreu");
            if (golpe != null && Time.time - golpe.t < 4)
            {
                var a = g.actors.Find(b => b.id == golpe.s);
                if (a != null)
                {
                    ClasseDef C; Regras.CLASSES.TryGetValue(a.cls, out C); string arma = C != null && !string.IsNullOrEmpty(C.arma) ? ", " + C.arma.ToLowerInvariant() : "";
                    int d = Mathf.RoundToInt(Mathf.Sqrt((float)((golpe.x - m.x) * (golpe.x - m.x) + (golpe.z - m.z) * (golpe.z - m.z))));
                    espQuem = "Derrubado por " + Cor("<b>" + a.name + "</b>", a.team == "H" ? OURO_VIVO : CARMIM_HI) + " (" + (C != null ? C.name : "") + arma + ") a " + d + " m";
                    if (m.st == "down") { float dx = golpe.x - (float)m.x, dz = golpe.z - (float)m.z; virarYaw = Mathf.Atan2(-dx, -dz); virarT = .7f; }
                }
            }
            if (espQuem == "" && m.st == "dead") espQuem = "Você sangrou até o fim";
        }
        void Espectar(Ator m, float dt)
        {
            var al = g.actors.Where(a => a.team == m.team && a != m && a.st != "dead" && !a.hidden).ToList(); espN = al.Count;
            if (EntradaUnity.CliqueEsq() && al.Count > 0) espIdx = (espIdx + 1) % al.Count;
            var alvo = al.Count > 0 ? al[espIdx % al.Count] : null; espAlvo = alvo;
            float ax, az, ay, cx, cy, cz;
            if (alvo != null)
            {
                ax = (float)alvo.x; az = (float)alvo.z; ay = alvo.st == "down" ? .5f : 1.45f; float yaw = (float)alvo.yaw;
                float d = 3.4f, bx = Mathf.Sin(yaw), bz = Mathf.Cos(yaw); while (d > .9f && !Mapa.SegClear(ax, az, ax + bx * d, az + bz * d, .3, Mapa.GB)) d -= .35f;
                cx = ax + bx * d; cz = az + bz * d; cy = ay + .9f;
            }
            else { espAng += dt * .25f; ax = (float)m.x; az = (float)m.z; ay = .3f; cx = ax + Mathf.Cos(espAng) * 5; cz = az + Mathf.Sin(espAng) * 5; cy = 4.2f; }
            var alvoP = new Vector3(cx, cy, cz); if (espPos == null || Vector3.Distance(espPos.Value, alvoP) > 40) espPos = alvoP;
            espPos = Vector3.Lerp(espPos.Value, alvoP, 1 - Mathf.Exp(-dt * 6)); var P = espPos.Value;
            cam.transform.position = new Vector3(P.x, P.y, -P.z); cam.transform.LookAt(new Vector3(ax, ay, -az));
        }
        // tela de caído, sendo executado ou morto (com o espectador, o fundo clareia para dar para ver)
        void TelaQueda(Ator m)
        {
            string tit = null, sub = null; var E = m.st == "down" ? mundo.ExecRecente(m.id) : null; bool esp = m.st == "dead" && espAlvo != null;
            if (m.st == "dead") { tit = "Você morreu"; sub = "Retorno em " + Mathf.CeilToInt((float)m.respawnT) + " s"; }
            else if (E != null) { var por = g.actors.Find(a => a.id == E.por); tit = "Você está sendo executado"; sub = (por != null ? por.name : "Alguém") + " está sobre você. Se um aliado acertar " + (por != null ? por.name : "quem executa") + ", a execução recomeça."; }
            else if (m.st == "down") { tit = "Você está caído"; sub = "Um aliado pode te reanimar. Sangrando: " + Mathf.CeilToInt((float)m.downT) + " s" + (m.revUsed ? " (sem reanimação neste ciclo)" : ""); }
            if (tit == null) return;
            // #death: o vermelho escuro radial no meio (espectador: só as faixas escuras de cima e de baixo), título serifado,
            // a linha de baixo, quem derrubou e, morto, quem você acompanha
            if (esp) { PintarDegrade(new Rect(0, 0, W, H * .28f), new Color(0, 0, 0, .55f), new Color(0, 0, 0, 0), true); PintarDegrade(new Rect(0, H * .75f, W, H * .25f), new Color(0, 0, 0, 0), new Color(0, 0, 0, .5f), true); }
            else GUI.DrawTexture(new Rect(0, 0, W, H), TexQueda);
            var stH = Css("display", esp ? 34 : 72, BONE, TextAnchor.UpperCenter); var stP = Css("mono", 15, MUDO, TextAnchor.UpperCenter); var stQ = Css("mono", 15, BONE, TextAnchor.UpperCenter); var stE = Css("mono", 13, Hx("#c9c3b8"), TextAnchor.UpperCenter);   // Nanquim
            string acomp = m.st == "dead" ? (espAlvo != null ? "Acompanhando " + Cor("<b>" + espAlvo.name + "</b>", GOLD_HI) + (espN > 1 ? " · " + EntradaUnity.Nome("fire") + " troca" : "") : "Nenhum aliado de pé: vendo onde você caiu") : "";
            float hH = AlturaNormal(stH), hP = 24, h = hH + 6 + hP + 6 + (espQuem != "" ? 6 + hP + 6 : 6) + (acomp != "" ? 8 + LN(16) : 0), y = esp ? 190 : H / 2 - h / 2;
            Linha(0, y, W, hH, Maiusc(tit), stH); y += hH + 6; Linha(0, y, W, hP, sub, stP); y += hP + 6;
            if (espQuem != "") { Linha(0, y + 6, W, hP, espQuem, stQ); y += 6 + hP + 6; } else y += 6;
            if (acomp != "") Linha(0, y + 8, W, LN(16), acomp, stE);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Caderno dos altares (J): o que a equipe sabe de cada altar e a última pista sobre ele, com a hora
        bool cadernoAberto; readonly Dictionary<int, KeyValuePair<double, string>> notasCad = new Dictionary<int, KeyValuePair<double, string>>(); string cadId;
        static readonly Dictionary<string, string> REAG_NOMES = new Dictionary<string, string> { { "sal", "sal negro" }, { "mandragora", "raiz de mandrágora" }, { "cinza", "cinza de osso" }, { "cera", "cera de defunto" }, { "flor", "flor-de-sepultura" }, { "corvo", "penas de corvo" } };
        static string NomeReagente(int i) { string t = i < Mundo.REAG_TIPOS.Length ? Mundo.REAG_TIPOS[i] : null, n; return t != null && REAG_NOMES.TryGetValue(t, out n) ? n : "reagente"; }
        void AnotarCaderno(string msg)
        {
            if (g == null || g.altars == null) return; if (cadId != g.matchId) { cadId = g.matchId; notasCad.Clear(); }
            for (int i = 0; i < g.altars.Count; i++) if (msg.Contains(g.altars[i].name)) notasCad[i] = new KeyValuePair<double, string>(g.rt, msg);
        }
        string[] EstadoCaderno(Altar A, int i, string papel)
        {
            if (A.state == "fenda") return new[] { "Fenda", "Ritual completo: conta para o culto", "fenda" }; if (A.state == "farol") return new[] { "Farol", "Ritual selado: conta para os Caçadores", "farol" };
            if (papel == "H")
            {
                if (A.state == "active" && A.localized) return new[] { "Ritual!", "Ritual em andamento, " + Mathf.FloorToInt((float)A.prog * 100) + "%: vá selar", "ativo" };
                switch (g.know != null && i < g.know.Length ? g.know[i] : "?")
                {
                    case "?": return new[] { "Sem pistas", "Nada se sabe ainda: santuários, sentinelas e o sensor ajudam", "nada" };
                    case "visto": return new[] { "Visitado", "Nada desperto quando a caça passou. Pode ainda ser do culto e não ter sido consagrado", "nada" };
                    case "descartado": return new[] { "Descartado", "Um sinal do santuário (ou um Chamariz queimado) disse que o culto não escolheu este altar. Cuidado com a Transferência", "limpo" };
                    case "purgado": return new[] { "Purgado", "Era um altar do culto e foi purgado. Pode ser consagrado de novo", "purgado" };
                    case "desperto": return new[] { "Desperto", "Consagrado ou Chamariz: purgue, ou confirme com a lanterna", "desperto" };
                    case "confirmado": return new[] { "Confirmado", "Resíduo de reagente: é um altar do culto. Purgue antes da Vigília", "confirmado" };
                    case "chamariz": return new[] { "Chamariz", "Sem resíduo: é isca. Não perca tempo aqui", "chamariz" };
                    case "ativo": return new[] { "Ritual!", "Ritual em andamento: vá selar", "ativo" };
                }
                return new[] { "?", "", "nada" };
            }
            if (A.state == "active") return new[] { "Ritual", "Seu ritual está em " + Mathf.FloorToInt((float)A.prog * 100) + "%" + (A.localized ? ", e os Caçadores sabem onde" : ""), "ativo" };
            if (A.decoy) return new[] { "Chamariz", "Isca plantada: atrai os Caçadores", "chamariz" };
            if (A.chosen) return A.state == "awake" ? new[] { "Consagrado", "Pronto: inicie o ritual com um reagente na Vigília", "confirmado" } : new[] { "Seu altar", "Ainda não consagrado: traga um reagente", "desperto" };
            return new[] { "Fora do plano", "Não é um dos altares escolhidos", "limpo" };
        }
        static Color CorCaderno(string c)
        {
            switch (c) { case "limpo": return new Color(.49f, .52f, .56f); case "purgado": return new Color(.85f, .71f, .42f); case "desperto": return new Color(.78f, .61f, .94f); case "confirmado": return new Color(.88f, .78f, 1f);
                case "chamariz": return new Color(.67f, .67f, .67f); case "ativo": case "fenda": return CARMIM_HI; case "farol": return OURO_VIVO; default: return new Color(.36f, .39f, .42f); }
        }
        void Caderno(string papel)
        {
            // #caderno: 360 px à direita, top 120, padding 10 × 14, borda clara, um bloco .cd por altar (borda de cima escura)
            if (!cadernoAberto || g == null || g.phase != "play") return;
            if (cadId != g.matchId) { cadId = g.matchId; notasCad.Clear(); }
            float x = W - 26 - 360, y = 120, w = 360, wc = w - 28; var stN = Css("serif", 16, BONE); var stD = Css("400", 14, Hx("#c9c3b8")); var stE = Css("mono", 12, MUDO);
            // a linha do nome: o b (IM Fell 17) divide a linha com o strut do corpo (Barlow 16): a subida da Barlow e a descida da IM Fell
            float subN = Mathf.Max(Ar(16), SubidaPx(stN)), hN = subN + Mathf.Max(Ar(16 * .2f), AlturaNormal(stN) - SubidaPx(stN)), lhD = LN(13.5f), lhE = LN(12.5f); var blocos = new List<float>(); float h = 1 + 10 + LN(12) + 4;
            var dados = new List<string[]>();
            for (int i = 0; i < g.altars.Count; i++)
            {
                var A = g.altars[i]; var e = EstadoCaderno(A, i, papel); KeyValuePair<double, string> n; bool tem = notasCad.TryGetValue(i, out n);
                string nota = tem ? Relogio(n.Key) + " · " + n.Value : null; dados.Add(new[] { A.name, e[0], e[1], e[2], nota });
                float hb = 1 + 5 + hN + AlturaParagrafo(e[1], stD, wc, lhD) + (nota != null ? 1 + AlturaParagrafo(nota, stE, wc, lhE) : 0) + 6; blocos.Add(hb); h += hb;
            }
            h = Mathf.Min(h + 10 + 1, H - 300); var caixa = new Rect(x, y, w, h);
            Caixa(caixa, new Color(10 / 255f, 10 / 255f, 10 / 255f, .9f));
            Grupo(new Rect(x + 1, y + 1, w - 2, h - 2));   // o overflow:hidden
            float yy = 10; Espacada(14 - 1, yy, LN(12), "CADERNO DOS ALTARES · " + EntradaUnity.Nome("caderno").ToUpper() + " FECHA", Css("mono", 12, MUDO), .72f); yy += LN(12) + 4;
            for (int i = 0; i < dados.Count; i++)
            {
                var d = dados[i]; Caixa(new Rect(13, yy, wc, 1), Hx("#222222")); float yl = yy + 1 + 5, bas = yl + subN;
                NaBase(13, bas, wc, Maiusc(d[0]), stN); var cc = CorCaderno(d[3]); var stI = Css("600", 13, cc); float xi = 13 + Larg(Maiusc(d[0]), stN) + 8, wi = Larg(d[1], stI) + 12;
                float topoI = bas - 13 - (AlturaNormal(stI) - LN(13f)) / 2; Moldura(new Rect(xi, topoI, wi, LN(13f)), cc); NaBase(xi + 6, bas, wi, d[1], stI);
                float yd = yl + hN; yd += Paragrafo(13, yd, wc, lhD, d[2], stD); if (d[4] != null) Paragrafo(13, yd + 1, wc, lhE, d[4], stE);
                yy += blocos[i];
            }
            FimGrupo();
        }
        // letter-spacing: cada letra com o espaço a mais depois dela
        // o mesmo na linha de base; devolve a largura (cada letra soma o espaçamento, a última também, como no CSS)
        float EspacadaNaBase(float x, float bas, string txt, GUIStyle st, float esp, bool desenha = true)
        {
            float espaco = Larg("a a", st) - Larg("aa", st), x0 = x;
            foreach (var ch in txt) { string c = ch.ToString(); if (ch != ' ' && desenha) NaBase(x, bas, 40, c, st); x += (ch == ' ' ? espaco : Larg(c, st)) + esp; }
            return x - x0;
        }
        void Espacada(float x, float y, float lh, string txt, GUIStyle st, float esp)
        {
            float espaco = Larg("a a", st) - Larg("aa", st);   // o espaço sozinho pode medir zero
            foreach (var ch in txt) { string c = ch.ToString(); if (ch != ' ') Linha(x, y, 40, lh, c, st); x += (ch == ' ' ? espaco : Larg(c, st)) + esp; }
        }

        // inimigos fora de vista não chegam no snapshot online (Ator.hidden); no jogo local, todos vêm
        static bool EscondidoNoCliente(Ator a) { return a.hidden; }
    }
}
