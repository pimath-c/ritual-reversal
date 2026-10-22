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
        static readonly Color OURO_VIVO = new Color(.953f, .835f, .557f), CARMIM_HI = new Color(.878f, .263f, .369f), MUDO = new Color(.639f, .608f, .557f), TEXTO_LEG = new Color(.867f, .827f, .761f);

        // ---------------------------------------------------------------------------------------------------------------
        // Legendas: o que se ouve, de onde vem e a que distância (LEG_TXT: texto, alcance, cor)
        class Leg { public string txt, cor; public float x = float.NaN, z = float.NaN, t; }
        readonly Dictionary<string, Leg> LEG = new Dictionary<string, Leg>();
        static readonly Dictionary<string, object[]> LEG_TXT = new Dictionary<string, object[]>
        {
            { "shot", new object[] { "Tiro", 55f, "" } }, { "sigil", new object[] { "Sigilo", 35f, "cult" } }, { "step", new object[] { "Passos", 16f, "" } },
            { "passoForte", new object[] { "Passos pesados", 24f, "" } }, { "passoLeve", new object[] { "Passos leves", 5f, "" } }, { "collect", new object[] { "Algo foi coletado", 26f, "" } },
            { "flare", new object[] { "Sinalizador aceso", 20f, "" } }, { "rune", new object[] { "Runa conjurada", 30f, "cult" } }, { "arrasto", new object[] { "Arrasto de ritual", 45f, "rit" } },
            { "push", new object[] { "Empurrão", 28f, "cult" } }, { "flash", new object[] { "Clarão", 30f, "" } }, { "purify", new object[] { "Purificação", 30f, "" } },
            { "reload", new object[] { "Recarga", 18f, "" } }, { "estouro", new object[] { "Sigilo estourou", 22f, "cult" } }, { "corvo", new object[] { "Corvos levantam voo", 40f, "" } },
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
            Leg it; if (!LEG.TryGetValue(chave, out it)) { it = new Leg(); LEG[chave] = it; }
            it.txt = txt ?? (string)L[0]; it.cor = cor ?? (string)L[2]; it.x = x; it.z = z; it.t = k.StartsWith("rit") ? .6f : 2.8f;
            if (LEG.Count > 6) { string pior = null; foreach (var kv in LEG) if (pior == null || kv.Value.t < LEG[pior].t) pior = kv.Key; LEG.Remove(pior); }
        }
        void Legendas()
        {
            if (LEG.Count == 0) return; var P = CamP(); float yaw = (float)lookYaw, dt = dtG; float y = H - 172;
            var mortos = new List<string>(); var st = new GUIStyle(sPequeno) { fontSize = 15, wordWrap = false, richText = true, alignment = TextAnchor.MiddleLeft };
            foreach (var kv in LEG)
            {
                var it = kv.Value; it.t -= dt; if (it.t <= 0) { mortos.Add(kv.Key); continue; }
                float op = Mathf.Min(1, it.t / .5f); var corSeta = it.cor == "rit" ? CARMIM_HI : it.cor == "cult" ? new Color(.773f, .549f, 1f) : OURO_VIVO; corSeta.a = op;
                var corTxt = it.cor == "rit" ? new Color(1f, .604f, .671f) : TEXTO_LEG; string resto = ""; float ang = 0; bool aqui = false, sem = float.IsNaN(it.x);
                if (!sem)
                {
                    float dx = it.x - P.x, dz = it.z - P.z, d = Mathf.Sqrt(dx * dx + dz * dz), fr = dx * -Mathf.Sin(yaw) + dz * -Mathf.Cos(yaw), di = dx * Mathf.Cos(yaw) + dz * -Mathf.Sin(yaw);
                    ang = Mathf.Atan2(di, fr); aqui = d < 3; if (d < 10 && it.cor != "rit") corTxt = Color.white;
                    resto = aqui ? "aqui" : LADOS[((Mathf.RoundToInt(ang / (Mathf.PI / 4)) % 8) + 8) % 8] + ", " + Mathf.RoundToInt(d) + " m";
                }
                corTxt.a = op; var corM = MUDO; corM.a = op;
                string txt = Cor(it.txt, corTxt) + (resto != "" ? "  <size=13>" + Cor(resto, corM) + "</size>" : "");
                float w = Mathf.Min(560, st.CalcSize(new GUIContent(txt)).x + 40); var r = new Rect(26, y - 22, w, 22); Caixa(r, new Color(0, 0, 0, .58f * op));
                // seta para a origem do som (gira com o rumo); ponto cheio quando é aqui; anel quando não tem lugar
                var c0 = new Vector2(r.x + 14, r.y + 11);
                if (sem) GUI.Label(new Rect(c0.x - 8, c0.y - 11, 16, 22), Cor("○", corSeta), new GUIStyle(st) { alignment = TextAnchor.MiddleCenter });
                else if (aqui) GUI.Label(new Rect(c0.x - 8, c0.y - 11, 16, 22), Cor("●", corSeta), new GUIStyle(st) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
                else { var mt = GUI.matrix; GUIUtility.RotateAroundPivot(ang * Mathf.Rad2Deg, c0); GUI.Label(new Rect(c0.x - 8, c0.y - 11, 16, 22), Cor("▲", corSeta), new GUIStyle(st) { alignment = TextAnchor.MiddleCenter, fontSize = 13 }); GUI.matrix = mt; }
                GUI.Label(new Rect(r.x + 28, r.y, w - 30, 22), txt, st); y -= 25;
            }
            foreach (var k in mortos) LEG.Remove(k);
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
            var P = CamP(); float rumo = -(float)lookYaw, k = BUS_W / 2 / BUS_MEIO, x0 = W / 2 - BUS_W / 2, y0 = 150;
            Func<float, float> px = b => { float r = b - rumo; r = Mathf.Atan2(Mathf.Sin(r), Mathf.Cos(r)); return Mathf.Abs(r) > BUS_MEIO ? float.NaN : BUS_W / 2 + r * k; };
            var st = new GUIStyle(sPequeno) { fontSize = 13, alignment = TextAnchor.UpperCenter, wordWrap = false, richText = true, fontStyle = FontStyle.Bold };
            for (int gr = 0; gr < 360; gr += 15)
            {
                float x = px(gr * Mathf.Deg2Rad); if (float.IsNaN(x)) continue; string nome = null;
                switch (gr) { case 0: nome = "N"; break; case 45: nome = "NE"; break; case 90: nome = "L"; break; case 135: nome = "SE"; break; case 180: nome = "S"; break; case 225: nome = "SO"; break; case 270: nome = "O"; break; case 315: nome = "NO"; break; }
                if (nome != null) GUI.Label(new Rect(x0 + x - 20, y0 + 5, 40, 20), Cor(nome, gr == 0 ? CARMIM_HI : new Color(.81f, .776f, .714f)), st);
                else Caixa(new Rect(x0 + x, y0, 1, 6), new Color(.91f, .878f, .816f, .35f));
            }
            var sm = new GUIStyle(st) { fontSize = 12, fontStyle = FontStyle.Normal };
            Action<double, double, Color, string, int> mk = (x, z, c, ch, tam) => { float b = Mathf.Atan2((float)(x - P.x), -(float)(z - P.z)), q = px(b); if (!float.IsNaN(q)) GUI.Label(new Rect(x0 + q - 10, y0 + (tam > 12 ? 0 : 2), 20, 20), "<size=" + tam + ">" + Cor(ch, c) + "</size>", sm); };
            foreach (var a in g.actors)
            {
                if (a == m || a.team != m.team || a.st == "dead" || EscondidoNoCliente(a)) continue;
                mk(a.x, a.z, a.st == "down" ? CARMIM_HI : m.team == "C" ? new Color(1f, .56f, .64f) : OURO_VIVO, a.st == "down" ? "✚" : "◆", 12);
            }
            foreach (var A in g.altars) { if (A.state != "active" || (papel == "H" && !A.localized)) continue; mk(A.x, A.z, CARMIM_HI, "✶", 15); }
            if (g.lab != null && g.lab.economia) foreach (var n in Mapa.NPCS) { if ((n.time == null || n.time == m.team) && Mathf.Sqrt((float)((n.x - P.x) * (n.x - P.x) + (n.z - P.z) * (n.z - P.z))) < 60) mk(n.x, n.z, new Color(.91f, .84f, .66f), "○", 12); }
            if (g.flares != null) foreach (var f in g.flares) if (f.t > 0) mk(f.x, f.z, new Color(1f, .69f, .44f), "✦", 12);
            foreach (var p in pings) mk(p.x, p.z, new Color(.56f, .91f, 1f), "⬥", 15);
            // o triângulo do rumo atual, embaixo no meio
            GUI.Label(new Rect(W / 2 - 10, y0 + 16, 20, 14), "<size=10>" + Cor("▲", OURO_VIVO) + "</size>", new GUIStyle(sm) { alignment = TextAnchor.MiddleCenter });
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
        void FaixaLance()
        {
            float dt = dtG; lanceT -= dt;
            if (lanceT <= 0) { if (lanceAtual != null) { lanceAtual = null; lanceT = 0; } if (filaLance.Count > 0 && lanceT < -.9f) { lanceAtual = filaLance[0]; filaLance.RemoveAt(0); lanceT = 4.2f; som.Tocar("lance"); } }
            if (lanceAtual == null) return;
            float op = Mathf.Clamp01(Mathf.Min((4.2f - lanceT) / .25f, lanceT / .9f)), y = H * .3f + 64, w = 640;
            var borda = lanceAtual.c ? new Color(.878f, .263f, .369f, .6f * op) : new Color(.953f, .835f, .557f, .55f * op);
            Caixa(new Rect(W / 2 - w / 2, y, w, 62), new Color(.047f, .039f, .031f, .82f * op)); Caixa(new Rect(W / 2 - w / 2, y, w, 1), borda); Caixa(new Rect(W / 2 - w / 2, y + 62, w, 1), borda);
            var sc = new GUIStyle(sPequeno) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            Color cs = lanceAtual.c ? CARMIM_HI : OURO_VIVO, ct = lanceAtual.meu ? OURO_VIVO : OSSO, cb = new Color(.79f, .75f, .68f); cs.a = ct.a = cb.a = op;
            GUI.Label(new Rect(0, y + 2, W, 16), "<size=11>" + Cor("L A N C E   D A   N O I T E", cs) + "</size>", sc);
            GUI.Label(new Rect(0, y + 16, W, 28), "<size=22>" + Cor(lanceAtual.tit, ct) + "</size>", sc);
            GUI.Label(new Rect(0, y + 42, W, 18), "<size=14>" + Cor(lanceAtual.sub, cb) + "</size>", sc);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Queda e espectador (v5.3): quem te derrubou, de onde e com o quê; morto, a câmera acompanha um aliado de pé
        // (clique troca) ou gira devagar sobre onde você caiu
        class GolpeR { public int s; public float x, z, t; }
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
            Caixa(new Rect(0, 0, W, H), esp ? new Color(0, 0, 0, .25f) : m.st == "dead" ? new Color(0, 0, 0, .6f) : new Color(.16f, 0, .03f, .45f));
            float y = esp ? H * .12f : H / 2 - 70;
            GUI.Label(new Rect(0, y, W, 50), tit, sTitulo); GUI.Label(new Rect(W / 2 - 500, y + 54, 1000, 30), sub, sCentro);
            if (espQuem != "") GUI.Label(new Rect(W / 2 - 500, y + 86, 1000, 30), espQuem, sCentro);
            if (m.st == "dead") GUI.Label(new Rect(W / 2 - 500, y + 116, 1000, 30), espAlvo != null ? "Acompanhando <b>" + espAlvo.name + "</b>" + (espN > 1 ? " · clique troca" : "") : "Nenhum aliado de pé: vendo onde você caiu", sCentro);
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
            if (!cadernoAberto || g == null || g.phase != "play") return;
            if (cadId != g.matchId) { cadId = g.matchId; notasCad.Clear(); }
            float x = W - 26 - 360, y = 120, w = 360; var linhas = new List<string>();
            var st = new GUIStyle(sPequeno) { fontSize = 14, wordWrap = true, richText = true };
            float h = 30; var alturas = new List<float>();
            for (int i = 0; i < g.altars.Count; i++)
            {
                var A = g.altars[i]; var e = EstadoCaderno(A, i, papel); KeyValuePair<double, string> n; bool tem = notasCad.TryGetValue(i, out n);
                string t = "<size=17>" + A.name + "</size>  <b>" + Cor("[" + e[0] + "]", CorCaderno(e[2])) + "</b>\n" + Cor(e[1], new Color(.81f, .776f, .714f)) + (tem ? "\n<size=12>" + Cor(Relogio(n.Key) + " · " + n.Value, MUDO) + "</size>" : "");
                float hh = st.CalcHeight(new GUIContent(t), w - 28) + 10; linhas.Add(t); alturas.Add(hh); h += hh;
            }
            h = Mathf.Min(h, H - 300); Caixa(new Rect(x, y, w, h), new Color(.031f, .035f, .043f, .97f));
            GUI.Label(new Rect(x + 14, y + 6, w - 28, 20), "<size=12>" + Cor("CADERNO DOS ALTARES · J FECHA", OURO) + "</size>", st);
            float yy = y + 28;
            for (int i = 0; i < linhas.Count && yy < y + h; i++) { Caixa(new Rect(x + 14, yy, w - 28, 1), new Color(.149f, .169f, .192f)); GUI.Label(new Rect(x + 14, yy + 4, w - 28, alturas[i]), linhas[i], st); yy += alturas[i]; }
        }

        // inimigos fora de vista não chegam no snapshot online (Ator.hidden); no jogo local, todos vêm
        static bool EscondidoNoCliente(Ator a) { return a.hidden; }
    }
}
