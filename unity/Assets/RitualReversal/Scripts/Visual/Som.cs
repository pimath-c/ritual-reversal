// Som do protótipo, como no Web Audio do navegador: as gravações de Resources/Sons (CC0 e CC BY 3.0, créditos em
// docs/AUDIO.md, montadas por tools/montar-sons.py) tocam com variação de altura e volume, posição estéreo,
// abafamento pelas paredes (occlusion) e passa-baixa pela distância. Os laços contínuos também são gravações: os fundos
// por lugar, a chuva, o canto de cada ritual (sussurro, pulsação grave e coro que entram com o progresso), o coro e o
// bordão do Grande Ritual, a música do menu e a tensão (SomMusica.cs). Tudo é misturado em OnAudioFilterRead.
// v6.3: nada é sintetizado (os osciladores e o ruído de reserva saíram); sem a gravação, o som não toca.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;
using Random = UnityEngine.Random;

namespace RitualReversal.Visual
{
    [RequireComponent(typeof(AudioSource))]
    public partial class Som : MonoBehaviour
    {
        public static Som Atual;
        public float volume = .8f;
        int sr = 48000; double agoraAudio; volatile float agoraPrincipal; // segundos no relógio do áudio
        readonly System.Random rnd = new System.Random();
        // ouvinte nas coordenadas do protótipo
        public Vector3 ouvinte; public float yawOuvinte;

        public static Som Criar(GameObject onde)
        {
#if UNITY_2023_1_OR_NEWER
            if (FindAnyObjectByType<AudioListener>() == null) onde.AddComponent<AudioListener>();
#else
            if (FindObjectOfType<AudioListener>() == null) onde.AddComponent<AudioListener>();
#endif
            if (Atual != null) return Atual;
            // objeto próprio: o filtro de áudio não pode ficar no mesmo objeto do AudioListener (pegaria a mixagem inteira)
            var go = new GameObject("Som (síntese)"); go.transform.SetParent(onde.transform, false); go.AddComponent<AudioSource>();
            Atual = go.AddComponent<Som>(); return Atual;
        }
        void Awake()
        {
            sr = AudioSettings.outputSampleRate; if (sr <= 0) sr = 48000;
            var src = GetComponent<AudioSource>(); src.playOnAwake = true; src.loop = true; src.spatialBlend = 0; src.volume = 1;
            src.clip = AudioClip.Create("síntese", sr, 1, sr, false); // silêncio em laço: o som sai do OnAudioFilterRead
            src.Play();
            for (int i = 0; i < AV.Length; i++) AV[i] = new LacoRitual();
            CarregarGravacoes();
        }

        // ---------- gravações ----------
        // Cada clipe é decodificado uma vez para um vetor e tocado no OnAudioFilterRead. Como no navegador: sorteia uma
        // gravação diferente da última, ±4% de altura e ±8% de volume, no máximo N vozes do mesmo som, repetição em
        // menos de 25 ms ignorada (evento repetido não vira eco), passa-baixa no que vem de longe.
        class Gravacao { public float[] d; public int sr; }
        readonly Dictionary<string, List<Gravacao>> grav = new Dictionary<string, List<Gravacao>>();
        readonly Dictionary<string, int> ultimaVar = new Dictionary<string, int>();
        readonly Dictionary<string, double> ultimoInicio = new Dictionary<string, double>();
        readonly Dictionary<string, List<double>> fimVozes = new Dictionary<string, List<double>>();
        public int Gravacoes { get { return grav.Values.Sum(l => l.Count); } }
        void CarregarGravacoes()
        {
            AudioClip[] clipes; try { clipes = Resources.LoadAll<AudioClip>("Sons"); } catch (Exception) { return; }
            // "tiro_carabina", "tiro_carabina_2"...: o nome sem o número final é o som; a ordem segue o número
            foreach (var c in clipes.OrderBy(c => c.name, StringComparer.Ordinal))
            {
                string nome = c.name; int us = nome.LastIndexOf('_'); int num;
                if (us > 0 && int.TryParse(nome.Substring(us + 1), out num)) nome = nome.Substring(0, us);
                if (c.loadState != AudioDataLoadState.Loaded) c.LoadAudioData();
                var buf = new float[c.samples * c.channels]; if (buf.Length == 0 || !c.GetData(buf, 0)) continue;
                var mono = new float[c.samples]; for (int i = 0; i < c.samples; i++) { float a = 0; for (int k = 0; k < c.channels; k++) a += buf[i * c.channels + k]; mono[i] = a / c.channels; }
                int ini = 0, lim = Math.Min(mono.Length, c.frequency / 12); while (ini < lim && Math.Abs(mono[ini]) < 1e-3f) ini++; // o atraso do MP3 não pode atrasar o tiro
                if (ini > 32) mono = mono.Skip(ini).ToArray();
                List<Gravacao> L; if (!grav.TryGetValue(nome, out L)) grav[nome] = L = new List<Gravacao>(); L.Add(new Gravacao { d = mono, sr = c.frequency });
            }
            lacoZ = Laco("zumbido_laco"); lacoP = Laco("pulso_laco"); lacoC = Laco("coro_laco"); lacoB = Laco("bordao_laco"); lacoMus = Laco("musica_laco"); lacoT = Laco("tensao_laco");
            for (int i = 0; i < AMB_LACOS.Length; i++) { List<Gravacao> L; if (grav.TryGetValue(AMB_LACOS[i], out L)) { lacoAmb[i] = L[0]; pAmb[i] = rnd.NextDouble() * L[0].d.Length; } }
            { List<Gravacao> L; if (grav.TryGetValue("chuva_laco", out L)) { lacoChuva = L[0]; pChuva = rnd.NextDouble() * L[0].d.Length; } }
        }
        Gravacao Laco(string nome) { List<Gravacao> L; return grav.TryGetValue(nome, out L) && L.Count > 0 ? L[0] : null; }
        // um passo num laço gravado: avança a posição p e devolve a amostra (0 sem a gravação)
        static double PassoLaco(Gravacao lg, ref double p, double taxa, int sr)
        {
            if (lg == null) return 0; p += (double)lg.sr / sr * taxa; if (p >= lg.d.Length - 1) p -= lg.d.Length - 1; return lg.d[(int)p];
        }
        // ---------- v6: ambiente por lugar ----------
        // Os fundos se misturam conforme onde o ouvinte está (dentro da catedral a noite lá fora fica abafada); na mata, de vez
        // em quando, uma coruja ou um galho rangendo, longe e de um lado. A mesma informação para os dois lados.
        static readonly string[] AMB_LACOS = { "mata_laco", "noite_laco", "cripta_laco", "charco_laco", "fogo_laco" };
        readonly Gravacao[] lacoAmb = new Gravacao[5]; readonly double[] pAmb = new double[5]; readonly float[] gAmb = new float[5], alvoAmb = new float[5];
        List<Vector2> fornos; float ambProx = 20;
        public void TrocouMapa() { fornos = null; }   // outro mapa: fornos e caldeiras de novo
        public void Ambiente(bool jogo, Vector3 P, float dt)
        {
            if (fornos == null && MapaVisual.Carregado) { fornos = new List<Vector2>(); foreach (var w in MapaVisual.WALLS) if (w.kind == "forno" || w.kind == "caldeira") fornos.Add(new Vector2((w.x1 + w.x2) / 2, (w.z1 + w.z2) / 2)); }
            bool dentro = Mapa.Carregado && Mapa.Coberto(P.x, P.z), mata = Mapa.Carregado && Mapa.ZoneAt(P.x, P.z) == "Floresta";
            float cha = MapaVisual.Asilo ? Mathf.Max(0, P.z + 60) + 6 : Mathf.Sqrt((P.x - 46) * (P.x - 46) + (P.z - 40) * (P.z - 40)), forno = 99;   // no asilo, o charco é a margem do lago if (fornos != null) foreach (var f in fornos) forno = Mathf.Min(forno, Vector2.Distance(f, new Vector2(P.x, P.z)));
            alvoAmb[0] = jogo ? (dentro ? 0 : mata ? .5f : .18f) : 0; alvoAmb[1] = jogo ? (dentro ? .06f : mata ? .22f : .38f) : 0; alvoAmb[2] = jogo && dentro ? .55f : 0;
            alvoAmb[3] = jogo && !dentro ? Mathf.Clamp01(1 - (cha - 10) / 35) * .6f : 0; alvoAmb[4] = jogo && !dentro ? Mathf.Clamp01(1 - (forno - 3) / 20) * .5f : 0;
            if (jogo && mata && !dentro) { ambProx -= dt; if (ambProx <= 0) { ambProx = Random.Range(18f, 42f); float a = Random.Range(0, Mathf.PI * 2), d = Random.Range(18f, 34f); Amostra(Random.value < .55f ? "coruja" : "rangido", P.x + Mathf.Cos(a) * d, P.z + Mathf.Sin(a) * d, 60, .45f); } }
        }
        // clima: a chuva gravada (sem a gravação, nada) num passa-alta de 700 Hz e num passa-baixa que fecha sob o telhado
        Gravacao lacoChuva; double pChuva; float gChuva, alvoChuva, corteChuva = 4200, corteChuvaAlvo = 4200; readonly Biquad chuvaHp = new Biquad(), chuvaLp = new Biquad();
        public void Chuva(float chuva, bool coberto) { alvoChuva = chuva > .05f ? .09f * chuva * (coberto ? .55f : 1) : 0; corteChuvaAlvo = coberto ? 650 : 4200; }
        Gravacao lacoZ, lacoP, lacoC, lacoB; // sussurros, pulsação e coro dos rituais em andamento; bordão do Grande Ritual
        public bool Tem(string nome) { return grav.ContainsKey(nome); }
        // toca uma gravação; devolve false se ela não existe. indice >= 0 escolhe a variação.
        public bool Amostra(string nome, float x = N, float z = N, float raio = 40, float vol = 1, float taxa = 1, float varia = .04f, int max = 4, float atraso = 0, int indice = -1)
        {
            List<Gravacao> L; if (!grav.TryGetValue(nome, out L) || L.Count == 0) return false;
            var s = float.IsNaN(x) ? new Espacial { g = 1, p = 0 } : Spatial(x, z, raio); if (s.g < .01f) return true;
            double agora = agoraPrincipal, ult;
            if (indice < 0 && ultimoInicio.TryGetValue(nome, out ult) && agora - ult < .025) return true; ultimoInicio[nome] = agora;
            List<double> fins; if (!fimVozes.TryGetValue(nome, out fins)) fimVozes[nome] = fins = new List<double>(); fins.RemoveAll(f => f < agora); if (fins.Count >= max) return true;
            int i = indice >= 0 ? Math.Min(indice, L.Count - 1) : Random.Range(0, L.Count); int u;
            if (indice < 0 && L.Count > 1 && ultimaVar.TryGetValue(nome, out u) && u == i) i = (i + 1) % L.Count; ultimaVar[nome] = i;
            var gv = L[i]; double passo = taxa * (1 + Random.Range(-varia, varia)) * gv.sr / sr;
            var v = new Voz { amostra = gv.d, passo = passo, vol = vol * s.g * (1 + Random.Range(-.08f, .08f)), gE = 1, gD = 1, dur = gv.d.Length / passo / sr };
            if (!float.IsNaN(x)) { float d = Mathf.Sqrt((x - ouvinte.x) * (x - ouvinte.x) + (z - ouvinte.z) * (z - ouvinte.z)); if (d > 8) { v.filtro = new Biquad(); v.filtro.Ajustar(Tipo.PassaBaixa, Mathf.Clamp(16000 * Mathf.Pow(Mathf.Clamp01(1 - d / raio), 1.6f) + 700, 700, 16000), 0, sr); } }
            Enfileirar(v, atraso, s.p); fins.Add(agora + atraso + v.dur); return true;
        }
        // passos por chão, como no navegador: pedra na catedral, terra nas trilhas, folhas na mata; com chuva, barro por cima
        public float chuva;
        public static string Superficie(double x, double z)
        {
            // asilo: ladrilho e tábua dentro; fora, cascalho nas trilhas, na margem, no pátio e no píer, capim no resto
            if (MapaVisual.Asilo)
            {
                if (Mapa.Coberto(x, z)) return "pedra";
                foreach (var e in MapaVisual.ESPACOS) if (x >= e.x1 && x <= e.x2 && z >= e.z1 && z <= e.z2) return System.Text.RegularExpressions.Regex.IsMatch(e.nome, "^(Trilha|Margem|Pátio|Píer)") ? "terra" : "folhas";
                return "folhas";
            }
            if (Mapa.NaCatedral(x, z)) return "pedra";
            foreach (var sg in Mapa.SEG_TRILHA) { double dx = sg.bx - sg.ax, dz = sg.bz - sg.az, L = dx * dx + dz * dz, t = L > 0 ? ((x - sg.ax) * dx + (z - sg.az) * dz) / L : 0; t = Math.Max(0, Math.Min(1, t)); if (JS.Hypot(x - (sg.ax + dx * t), z - (sg.az + dz * t)) < sg.w / 2 + .4) return "terra"; }
            return "folhas";
        }
        bool Passo(float x, float z, float raio, float vol)
        {
            bool proprio = float.IsNaN(x); float px = proprio ? ouvinte.x : x, pz = proprio ? ouvinte.z : z; string sup = Superficie(px, pz); float k = sup == "pedra" ? 1.5f : 1.25f;
            if (!Amostra("passo_" + sup, x, z, raio, vol * k, 1, .06f, proprio ? 3 : 6)) return false;
            if (chuva > .2f && !(Mapa.NaCatedral(px, pz) && !Mapa.NoClaustro(px, pz))) Amostra("passo_lama", x, z, raio, vol * chuva, 1, .04f, proprio ? 2 : 4);
            return true;
        }
        // tiro com a gravação da arma de quem atirou
        public void Tiro(bool revolver, float x, float z, bool proprio)
        {
            if (Amostra(revolver ? "tiro_revolver" : "tiro_carabina", proprio ? N : x, proprio ? N : z, 55, proprio ? .85f : .95f, 1, .04f, proprio ? 3 : 6)) return;
            Tocar("shot", x, z);
        }
        // ---------- receitas (SFX do protótipo, as mesmas do navegador) ----------
        public void Tocar(string k, float x = N, float z = N, float v = 0) { TocarGravado(k, x, z, v); }
        // o toque curto de sino das frases rápidas, dos pings marcados e do tutorial
        public void Aviso(float vol, float taxa = 1) { Amostra("aviso", N, N, 1, vol, taxa, .01f, 1); }
        bool TocarGravado(string k, float x, float z, float v)
        {
            switch (k)
            {
                case "shot": return Amostra("tiro_carabina", x, z, 55, .95f, 1, .04f, 6);
                case "sigil": return Amostra("sigilo", x, z, 35, .85f, 1, .04f, 5);
                case "step": return Passo(x, z, 16, .55f);
                case "passoForte": return Passo(x, z, 24, .75f);
                case "mystep": return Passo(N, N, 1, .12f);
                case "hit": return Amostra("impacto_corpo", N, N, 1, .5f, 1, .04f, 2);
                case "cabeca": Amostra("impacto_corpo", N, N, 1, .6f, 1.15f, .04f, 2); return Amostra("aviso", N, N, 1, .35f, 1.3f, .01f, 1);   // na cabeça: o golpe e um toque de sino agudo
                case "derrubou": return Amostra("derrubou", N, N, 1, .85f, 1, .04f, 1);
                case "bloqueio": return Amostra("impacto_pedra", N, N, 1, .6f, 1.3f, .04f, 2);
                case "hurt": return Amostra("dor", N, N, 1, .75f, 1, .04f, 2);
                case "sealed": return Amostra("farol", N, N, 1, .9f, 1, .04f, 1);
                case "doom": return Amostra("fenda", N, N, 1, 1, 1, .04f, 1);
                case "corvo": return Amostra("corvo", x, z, 40, .8f, 1, .06f, 3);
                case "bell": return Amostra("sino", N, N, 1, .5f, 1, .01f, 1);
                case "sinoGrande": return Amostra("sino_grande", N, N, 1, .85f, 1, .01f, 2);
                case "collect": return Amostra("coleta", x, z, 26, .8f);
                case "flare": return Amostra("flare", x, z, 30, .7f);
                case "arrasto": return Amostra("transferencia", x, z, 45, .8f, 1, .04f, 2);
                case "push": return Amostra("empurrao", x, z, 28, .9f);
                case "estouro": return Amostra("estouro", x, z, 24, .85f, 1, .04f, 5);
                // asilo: a porta range ao abrir e bate ao fechar; o quadro de força estala e a luz morre
                case "porta_abre": return Amostra("rangido", x, z, 20, .9f, 1, .08f, 4);
                case "porta_fecha": return Amostra("impacto_madeira", x, z, 24, 1, .85f, .04f, 4);
                case "estalo": Amostra("alavanca", x, z, 30, 1, 1, .04f, 3); return Amostra("estalo_fogo", x, z, 50, .8f, 1, .04f, 2);
                // v6.3: o que era sintetizado, agora gravado (as mesmas receitas do navegador)
                case "presage": return Amostra("presagio", N, N, 1, .9f, 1, .02f, 1);
                case "whisper": return Amostra("sussurro", x, z, 26, .6f, 1, .04f, 2);
                case "ping": return Amostra("ping", N, N, 1, .6f, .75f + v * .9f, .01f, 2);   // mais perto, mais agudo
                case "heart": return Amostra("coracao", N, N, 1, .8f, 1, .02f, 1);
                case "batida": return Amostra("tambor", N, N, 1, .5f + v * .4f, 1, .02f, 2);
                case "click": return Amostra("clique", N, N, 1, .7f, 1, .05f, 2);
                case "rune": return Amostra("runa", x, z, 30, .7f);
                case "eco": return Amostra("eco_coro", x, z, 320, .8f, 1, .01f, 1);
                case "flash": return Amostra("clarao", x, z, 30, .85f);
                case "purify": return Amostra("purificacao", x, z, 30, .8f);
                case "vazio": return Amostra("vazio", N, N, 1, .7f, 1, .04f, 2);
                case "alucinacao": { float a = Random.Range(0, Mathf.PI * 2); return Amostra("alucinacao", ouvinte.x + Mathf.Cos(a) * 5, ouvinte.z + Mathf.Sin(a) * 5, 20, .8f, 1, .04f, 1); }
                case "trovao": return Amostra("trovao", N, N, 1, .9f, 1, .03f, 1);
                case "selando": return Amostra("selando", x, z, 400, .9f, 1, .04f, 1);
                case "selando_perto": return Amostra("selando", x, z, 50, .45f, 1, .04f, 1);
                case "evp": return Amostra("evp", x, z, 30, .8f);
                // v6: os sons novos com o alcance e o volume do navegador
                case "queda_corpo": return Amostra(k, x, z, 32, .75f, 1, .05f, 3);
                case "estalo_fogo": return Amostra(k, x, z, 22, .5f, 1, .06f, 3);
                case "folhas": return Amostra(k, x, z, 18, .5f);
                case "sino": return Amostra(k, x, z, 60, .55f, 1, .01f, 1);
                case "selo_quebra": return Amostra(k, x, z, 40, .5f);
                case "canal_quebra": return Amostra(k, x, z, 40, .6f);
                case "assombro": return Amostra(k, x, z, 40, .45f);
                case "sussurro": return Amostra(k, x, z, 30, .6f);
                case "coruja": case "rangido": return Amostra(k, x, z, 60, .45f);
                case "reload":
                    {
                        List<Gravacao> L; if (!grav.TryGetValue("recarga", out L) || L.Count < 3) return false;   // abrir, cartuchos, fechar: no ritmo da recarga (1,6 s)
                        Amostra("recarga", x, z, 18, .7f, 1, .03f, 6, 0, 0); foreach (float t in new[] { .42f, .62f, .82f }) Amostra("recarga", x, z, 18, .7f, 1, .03f, 6, t, 1); Amostra("recarga", x, z, 18, .7f, 1, .03f, 6, 1.15f, 2);
                        if (v > 0) Amostra("cao", x, z, 18, .7f, 1, .04f, 2, 1.38f); return true;
                    }
                case "lance": return Amostra("descoberta", N, N, 1, .55f, .9f, .01f, 1);
                case "descoberta": return Amostra("descoberta", N, N, 1, v > 0 ? .6f : .35f, v > 0 ? 1 : 1.25f, .01f, 1);
                case "ritual_inicio": return Amostra(k, x, z, 140, 1, 1, .01f, 1);
                case "ritual_marco": return Amostra(k, x, z, 120, .8f, 1, .01f, 1);
                case "gota": return Amostra(k, x, z, 14, .35f, 1, .08f, 2);
                case "alavanca": case "cao": return Amostra(k, N, N, 1, .5f, 1, .05f, 2);
                case "impacto_pedra": case "impacto_madeira": case "impacto_terra": return Amostra(k, x, z, 30, .75f, 1, .07f, 6);
                default: return grav.ContainsKey(k) && Amostra(k, x, z, 40, .8f);
            }
        }

        // ---------- filtros e vozes ----------
        public enum Tipo { PassaBaixa, PassaAlta, PassaFaixa }
        class Biquad
        {
            double b0, b1, b2, a1, a2, x1, x2, y1, y2;
            public void Ajustar(Tipo t, double f, double q, int sr)
            {
                f = Math.Max(10, Math.Min(f, sr * .49)); double w = 2 * Math.PI * f / sr, cs = Math.Cos(w), sn = Math.Sin(w), alpha, a0;
                if (t == Tipo.PassaFaixa) { alpha = sn / (2 * Math.Max(q, .0001)); b0 = alpha; b1 = 0; b2 = -alpha; }
                else
                {
                    alpha = sn / (2 * Math.Pow(10, q / 20)); // no Web Audio o Q de passa-baixa e passa-alta é em dB
                    if (t == Tipo.PassaBaixa) { b0 = (1 - cs) / 2; b1 = 1 - cs; b2 = (1 - cs) / 2; } else { b0 = (1 + cs) / 2; b1 = -(1 + cs); b2 = (1 + cs) / 2; }
                }
                a0 = 1 + alpha; b0 /= a0; b1 /= a0; b2 /= a0; a1 = -2 * cs / a0; a2 = (1 - alpha) / a0;
            }
            public double Passo(double x) { double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2; x2 = x1; x1 = x; y2 = y1; y1 = y; return y; }
        }
        class Voz
        {
            public double ini, dur; public float vol, gE, gD; public Biquad filtro; // passa-baixa do que vem de longe
            public float[] amostra; public double passo; // gravação: amostras por amostra de saída (altura e taxa do clipe)
        }
        readonly List<Voz> fila = new List<Voz>(), vozes = new List<Voz>();
        struct Espacial { public float g, p; }
        Espacial Spatial(float x, float z, float raio)
        {
            float dx = x - ouvinte.x, dz = z - ouvinte.z, d = Mathf.Sqrt(dx * dx + dz * dz), rx = Mathf.Cos(yawOuvinte), rz = -Mathf.Sin(yawOuvinte);
            float p = d > .5f ? Mathf.Clamp((dx * rx + dz * rz) / d, -1, 1) : 0; float g = Mathf.Clamp01(1 - d / raio); g *= g;
            if (g > 0 && Mapa.Carregado) g *= Oclusao(ouvinte.x, ouvinte.z, x, z); return new Espacial { g = g, p = p };
        }
        public static float Oclusao(double ax, double az, double bx, double bz)
        {
            int n = 0; Mapa.Algum(Mapa.GT, Math.Min(ax, bx), Math.Min(az, bz), Math.Max(ax, bx), Math.Max(az, bz), b => { if (Mapa.SegBox(ax, az, bx, bz, b, 0) >= 0) { n++; if (n >= 3) return true; } return false; });
            return Mathf.Pow(.55f, n);
        }
        void Enfileirar(Voz v, float atraso, float pan)
        {
            float a = (pan + 1) * .25f * Mathf.PI; v.gE *= Mathf.Cos(a); v.gD *= Mathf.Sin(a);
            lock (fila) { v.ini = agoraPrincipal + .02 + atraso; fila.Add(v); }
        }
        const float N = float.NaN;

        // os feitiços já soam pelo evento 'sfx' (runa, empurrão, flash, purificação); o Círculo de Sal não tem outro som
        public void Hab(string cls, float x, float z, string f) { if (f == "sal") Amostra("sal", x, z, 30, .8f); }

        // ---------- laços ----------
        class LacoRitual
        {
            public volatile bool ligado; public volatile float alvoOut, alvoPan, alvoW, alvoP, alvoC, alvoTr; public float tr, selAnt; public double pz, pc, fTrem;
            public double pp; public float out_, pan, gW, gP, gC; public float[] det = new float[4];
        }
        readonly LacoRitual[] AV = new LacoRitual[8];
        volatile float silencioAlvo = 1, grandeAlvo; float silencio = 1, grande; double pGc, pGb; // a noite se cala no Grande Ritual

        // chamado a cada quadro pela partida (sync de áudio dos altares e do Grande Ritual)
        public void Rituais(Jogo g, string papel, float grk, float dt)
        {
            silencioAlvo = 1 - .85f * grk;
            for (int i = 0; i < AV.Length; i++)
            {
                var O = AV[i]; var A = g != null && g.phase == "play" && i < g.altars.Count ? g.altars[i] : null;
                string st = A == null ? "" : A.state; if (A != null && papel == "H" && st == "awake") { string kk = g.know[A.i]; if (!(kk == "desperto" || kk == "confirmado" || kk == "chamariz" || kk == "ativo")) st = "dormant"; }
                if (A != null && A.state == "active" && (papel == "C" || st == "active"))
                {
                    if (!O.ligado) { O.det[0] = .97f + Random.value * .06f; O.det[1] = .98f + Random.value * .04f; O.out_ = 0; O.ligado = true; }
                    float p = (float)A.prog; O.alvoW = p < .25f ? .55f : .18f; O.alvoP = p >= .25f ? .9f : 0; O.alvoC = p >= .75f ? .32f : 0;
                    O.alvoTr = A.seal > O.selAnt + 1e-5 ? 1 : 0; O.selAnt = (float)A.seal; // a caça selando: o coro treme
                    var s = Spatial((float)A.x, (float)A.z, (float)CFG.ritRad[Math.Max(1, A.lastN)] * 1.6f); float gg = s.g;
                    if (A.localized) gg = Mathf.Max(gg, .18f * Oclusao(ouvinte.x, ouvinte.z, A.x, A.z) + .06f);
                    O.alvoOut = gg * .7f; O.alvoPan = s.p;
                }
                else O.alvoOut = 0;
            }
            // Grande Ritual: sino a cada 11 s e tambor que acelera com o progresso
            Altar GA = null; if (g != null && g.phase == "play") foreach (var A in g.altars) if (A.state == "active" && A.grande) GA = A;
            grandeLigado = GA != null; grandeAlvo = grandeLigado ? 1 : 0;
            if (GA != null) { sinoT -= dt; if (sinoT <= 0) { sinoT = 11; Tocar("sinoGrande"); } batT -= dt; if (batT <= 0) { batT = 1.5f - (float)GA.prog * .8f; Tocar("batida", N, N, (float)GA.prog); } }
            else { sinoT = 4; batT = 0; }
        }
        bool grandeLigado; float sinoT = 4, batT;
        public void Silenciar() { foreach (var O in AV) O.alvoOut = 0; grandeAlvo = 0; }

        void Update() { agoraPrincipal = (float)agoraAudio; AudioListener.volume = volume; }

        void OnAudioFilterRead(float[] data, int canais)
        {
            int n = data.Length / canais; double dtS = 1.0 / sr;
            lock (fila) { vozes.AddRange(fila); fila.Clear(); }
            for (int i = 0; i < data.Length; i++) data[i] = 0;
            // coeficientes que andam devagar: uma vez por bloco
            double tb = agoraAudio; float kBloco = (float)(n * dtS);
            silencio += (silencioAlvo - silencio) * (1 - Mathf.Exp(-kBloco / .8f));
            foreach (var O in AV)
            {
                if (!O.ligado) continue;
                O.out_ += (O.alvoOut - O.out_) * (1 - Mathf.Exp(-kBloco / .12f)); O.pan += (O.alvoPan - O.pan) * (1 - Mathf.Exp(-kBloco / .1f));
                O.tr += (O.alvoTr - O.tr) * (1 - Mathf.Exp(-kBloco / (O.alvoTr > O.tr ? .12f : .6f)));
                O.gW += (O.alvoW - O.gW) * (1 - Mathf.Exp(-kBloco / .2f)); O.gP += (O.alvoP - O.gP) * (1 - Mathf.Exp(-kBloco / .3f)); O.gC += (O.alvoC - O.gC) * (1 - Mathf.Exp(-kBloco / .3f));
                if (O.alvoOut == 0 && O.out_ < .0005f) O.ligado = false;
            }
            for (int i = 0; i < gAmb.Length; i++) gAmb[i] += (alvoAmb[i] * silencio - gAmb[i]) * (1 - Mathf.Exp(-kBloco / .8f));
            grande += (grandeAlvo - grande) * (1 - Mathf.Exp(-kBloco / (grandeAlvo > grande ? 1.5f : .8f)));
            BlocoMusica(tb, kBloco); BlocoTensao(tb, kBloco);
            gChuva += (alvoChuva - gChuva) * (1 - Mathf.Exp(-kBloco / .4f)); corteChuva += (corteChuvaAlvo - corteChuva) * (1 - Mathf.Exp(-kBloco / .3f));
            if (gChuva > .0005f) { chuvaHp.Ajustar(Tipo.PassaAlta, 700, 1, sr); chuvaLp.Ajustar(Tipo.PassaBaixa, corteChuva, 1, sr); }
            for (int s = 0; s < n; s++)
            {
                double t = agoraAudio + s * dtS; double L = 0, R = 0;
                // v6: fundos por lugar
                for (int ia = 0; ia < lacoAmb.Length; ia++)
                {
                    var lg = lacoAmb[ia]; if (lg == null || gAmb[ia] < .0005f) continue;
                    pAmb[ia] += (double)lg.sr / sr; if (pAmb[ia] >= lg.d.Length - 1) pAmb[ia] -= lg.d.Length - 1; double am = lg.d[(int)pAmb[ia]] * gAmb[ia]; L += am; R += am;
                }
                if (gChuva > .0005f && lacoChuva != null)
                {
                    double ch = chuvaLp.Passo(chuvaHp.Passo(PassoLaco(lacoChuva, ref pChuva, 1, sr))) * gChuva; L += ch; R += ch;
                }
                // rituais
                foreach (var O in AV)
                {
                    if (!O.ligado) continue;
                    // gravações em laço: sussurros e zumbido, pulsação grave (x3, como o pg do navegador) e coro (x1,6)
                    double w = PassoLaco(lacoZ, ref O.pz, O.det[0], sr) * O.gW, pu = PassoLaco(lacoP, ref O.pp, 1, sr) * 3 * O.gP, coro = PassoLaco(lacoC, ref O.pc, O.det[1], sr) * 1.6 * O.gC;
                    O.fTrem += 7 * dtS; double trem = 1 + O.tr * .35 * Math.Sin(2 * Math.PI * O.fTrem);
                    double m = (w + pu + coro) * O.out_ * trem; float ang = (O.pan + 1) * .25f * Mathf.PI; L += m * Math.Cos(ang); R += m * Math.Sin(ang);
                }
                // Grande Ritual: o coro gravado mais grave e o bordão gravado com tremor lento
                if (grande > .0005f)
                {
                    double gm = (PassoLaco(lacoC, ref pGc, .94, sr) * .55 + PassoLaco(lacoB, ref pGb, 1, sr) * .6) * grande; L += gm; R += gm;
                }
                { double mt = AmostraMusica() + AmostraTensao(); L += mt; R += mt; }
                // vozes curtas
                for (int vi = 0; vi < vozes.Count; vi++)
                {
                    var v = vozes[vi]; double tv = t - v.ini; if (tv < 0) continue;
                    double ps = tv * sr * v.passo; int i0 = (int)ps; if (i0 >= v.amostra.Length - 1) continue;
                    double xa = v.amostra[i0] + (v.amostra[i0 + 1] - v.amostra[i0]) * (ps - i0); if (v.filtro != null) xa = v.filtro.Passo(xa);
                    xa *= v.vol; L += xa * v.gE; R += xa * v.gD;
                }
                float vol = .8f;
                if (canais >= 2) { data[s * canais] += (float)(Math.Tanh(L * vol)); data[s * canais + 1] += (float)(Math.Tanh(R * vol)); }
                else data[s] += (float)Math.Tanh((L + R) * .5 * vol);
            }
            agoraAudio += n * dtS;
            vozes.RemoveAll(v => agoraAudio - v.ini > v.dur + .1);
        }
    }
}
