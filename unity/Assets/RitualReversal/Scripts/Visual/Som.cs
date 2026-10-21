// Som do protótipo, sintetizado na hora como no Web Audio: cada efeito é ruído filtrado (biquad passa-baixa,
// passa-alta ou passa-faixa) ou um oscilador (seno, quadrada, serra, triângulo) com rampa de frequência, envelope
// de ataque curto e queda exponencial, posição estéreo e abafamento pelas paredes (occlusion). Os laços contínuos
// também: vento da noite, o canto de cada ritual (sussurro, pulsação grave e coro que entram com o progresso) e o
// coro, o bordão e o tambor do Grande Ritual. Tudo é misturado em OnAudioFilterRead.
// v5.8: os sons gravados do navegador (Resources/Sons, CC0 e CC BY 3.0, créditos em docs/AUDIO.md) tocam no mesmo
// misturador, com a mesma variação; a síntese fica de reserva para o que não tiver gravação.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;
using Random = UnityEngine.Random;

namespace RitualReversal.Visual
{
    [RequireComponent(typeof(AudioSource))]
    public class Som : MonoBehaviour
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
            List<Gravacao> z, co; if (grav.TryGetValue("zumbido_laco", out z) && grav.TryGetValue("coro_laco", out co)) { lacoZ = z[0]; lacoC = co[0]; }
            for (int i = 0; i < AMB_LACOS.Length; i++) { List<Gravacao> L; if (grav.TryGetValue(AMB_LACOS[i], out L)) { lacoAmb[i] = L[0]; pAmb[i] = rnd.NextDouble() * L[0].d.Length; } }
        }
        // ---------- v6: ambiente por lugar ----------
        // Os fundos se misturam conforme onde o ouvinte está (dentro da catedral a noite lá fora fica abafada); na mata, de vez
        // em quando, uma coruja ou um galho rangendo, longe e de um lado. A mesma informação para os dois lados.
        static readonly string[] AMB_LACOS = { "mata_laco", "noite_laco", "cripta_laco", "charco_laco", "fogo_laco" };
        readonly Gravacao[] lacoAmb = new Gravacao[5]; readonly double[] pAmb = new double[5]; readonly float[] gAmb = new float[5], alvoAmb = new float[5];
        List<Vector2> fornos; float ambProx = 20;
        public void Ambiente(bool jogo, Vector3 P, float dt)
        {
            if (fornos == null && MapaVisual.Carregado) { fornos = new List<Vector2>(); foreach (var w in MapaVisual.WALLS) if (w.kind == "forno") fornos.Add(new Vector2((w.x1 + w.x2) / 2, (w.z1 + w.z2) / 2)); }
            bool dentro = Mapa.Carregado && Mapa.Coberto(P.x, P.z), mata = Mapa.Carregado && Mapa.ZoneAt(P.x, P.z) == "Floresta";
            float cha = Mathf.Sqrt((P.x - 46) * (P.x - 46) + (P.z - 40) * (P.z - 40)), forno = 99; if (fornos != null) foreach (var f in fornos) forno = Mathf.Min(forno, Vector2.Distance(f, new Vector2(P.x, P.z)));
            alvoAmb[0] = jogo ? (dentro ? 0 : mata ? .5f : .18f) : 0; alvoAmb[1] = jogo ? (dentro ? .06f : mata ? .22f : .38f) : 0; alvoAmb[2] = jogo && dentro ? .55f : 0;
            alvoAmb[3] = jogo && !dentro ? Mathf.Clamp01(1 - (cha - 10) / 35) * .6f : 0; alvoAmb[4] = jogo && !dentro ? Mathf.Clamp01(1 - (forno - 3) / 20) * .5f : 0;
            if (jogo && mata && !dentro) { ambProx -= dt; if (ambProx <= 0) { ambProx = Random.Range(18f, 42f); float a = Random.Range(0, Mathf.PI * 2), d = Random.Range(18f, 34f); Amostra(Random.value < .55f ? "coruja" : "rangido", P.x + Mathf.Cos(a) * d, P.z + Mathf.Sin(a) * d, 60, .45f); } }
        }
        Gravacao lacoZ, lacoC; // sussurros e coro gravados dos rituais em andamento
        public bool Tem(string nome) { return grav.ContainsKey(nome); }
        // toca uma gravação; devolve false se ela não existe (quem chamou usa a síntese). indice >= 0 escolhe a variação.
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
        bool TocarGravado(string k, float x, float z, float v)
        {
            switch (k)
            {
                case "shot": return Amostra("tiro_carabina", x, z, 55, .95f, 1, .04f, 6);
                case "sigil": return Amostra("sigilo", x, z, 35, .85f, 1, .04f, 5);
                case "step": return Passo(x, z, 16, .55f);
                case "passoForte": return Passo(x, z, 24, .75f);
                case "mystep": return Passo(N, N, 1, .12f);
                case "hit": if (!Amostra("impacto_corpo", N, N, 1, .5f, 1, .04f, 2)) return false; Tom(N, N, 1, 1900, 1500, .045f, Onda.Seno, .07f); return true;
                case "cabeca": Amostra("impacto_corpo", N, N, 1, .6f, 1.15f, .04f, 2); return false;   // e o tique sintetizado por cima
                case "derrubou": if (!Amostra("derrubou", N, N, 1, .85f, 1, .04f, 1)) return false; Tom(N, N, 1, 660, 660, .5f, Onda.Triangulo, .1f, .06f); return true;
                case "bloqueio": if (!Amostra("impacto_pedra", N, N, 1, .6f, 1.3f, .04f, 2)) Tom(N, N, 1, 2600, 2300, .06f, Onda.Quadrada, .08f); Tom(N, N, 1, 3100, 2900, .09f, Onda.Seno, .05f); return true;
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
        public enum Onda { Seno, Quadrada, Serra, Triangulo }
        static double Osc(Onda o, double fase)
        {
            double f = fase - Math.Floor(fase);
            switch (o) { case Onda.Quadrada: return f < .5 ? 1 : -1; case Onda.Serra: return 2 * f - 1; case Onda.Triangulo: return 1 - 4 * Math.Abs(f - .5); default: return Math.Sin(2 * Math.PI * f); }
        }
        class Voz
        {
            public double ini, dur, ataque; public float vol, gE, gD; public bool ruido; public Onda onda; public double f0, f1, fase;
            public Biquad filtro; public Tipo ftipo; public double ff0, ff1, fq; // filtro com rampa exponencial opcional (presságio)
            public double subida = -1, pico, queda; // envelope especial: sobe linear até 'subida', cai linear até 'queda'
            public float hs; // chiado de 4200 Hz das vozes "secas" (alucinação)
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
        // sfxNoise(x, z, raio, dur, tipo, freq, q, vol, seco); x = NaN: sem posição
        public void Ruido(float x, float z, float raio, float dur, Tipo tipo, float freq, float q, float vol, bool seco = false, float atraso = 0)
        {
            Espacial s = seco ? new Espacial { g = 1, p = (Random.value < .5f ? -1 : 1) * Random.Range(.6f, 1f) } : float.IsNaN(x) ? new Espacial { g = 1, p = 0 } : Spatial(x, z, raio);
            if (s.g < .01f) return;
            var v = new Voz { ruido = true, dur = dur, ataque = .005, vol = vol * s.g, gE = 1, gD = 1, ftipo = tipo, ff0 = freq, ff1 = freq, fq = q <= 0 ? 1 : q, hs = seco ? .02f : 0 };
            Enfileirar(v, atraso, s.p);
        }
        // sfxTone(x, z, raio, f0, f1, dur, onda, vol)
        public void Tom(float x, float z, float raio, float f0, float f1, float dur, Onda onda, float vol, float atraso = 0)
        {
            var s = float.IsNaN(x) ? new Espacial { g = 1, p = 0 } : Spatial(x, z, raio); if (s.g < .01f) return;
            Enfileirar(new Voz { onda = onda, f0 = f0, f1 = Math.Max(20, f1), dur = dur, ataque = .01, vol = vol * s.g, gE = 1, gD = 1 }, atraso, s.p);
        }
        const float N = float.NaN;

        // ---------- receitas (SFX do protótipo) ----------
        public void Tocar(string k, float x = N, float z = N, float v = 0)
        {
            if (TocarGravado(k, x, z, v)) return;
            switch (k)
            {
                case "shot": Ruido(x, z, 55, .28f, Tipo.PassaBaixa, 1600, .7f, .9f); Tom(x, z, 55, 160, 50, .18f, Onda.Seno, .6f); break;
                case "sigil": Tom(x, z, 35, 720, 160, .35f, Onda.Triangulo, .35f); Ruido(x, z, 35, .3f, Tipo.PassaFaixa, 2400, 3, .25f); break;
                case "step": Ruido(x, z, 16, .09f, Tipo.PassaBaixa, 520, .8f, .55f); break;
                case "passoForte": Ruido(x, z, 24, .1f, Tipo.PassaBaixa, 620, .8f, .75f); break;
                case "mystep": Ruido(N, N, 1, .07f, Tipo.PassaBaixa, 420, .8f, .12f); break;
                case "hit": Tom(N, N, 1, 1400, 900, .07f, Onda.Quadrada, .12f); break;
                case "hurt": Tom(N, N, 1, 220, 90, .25f, Onda.Serra, .2f); break;
                case "presage":
                    {
                        Tom(N, N, 1, 78, 26, 3.2f, Onda.Seno, .9f);
                        var vz = new Voz { ruido = true, dur = 2.4, ftipo = Tipo.PassaFaixa, ff0 = 3200, ff1 = 400, fq = 5, vol = .35f, gE = 1, gD = 1, subida = 2, pico = .35, queda = 2.3 };
                        Enfileirar(vz, 0, 0); break;
                    }
                case "whisper": for (int i = 0; i < 4; i++) Ruido(x, z, 26, .35f, Tipo.PassaFaixa, Random.Range(900, 2600), 6, .5f, v > 0, i * .17f); break;
                case "ping": Tom(N, N, 1, 400 + v * 900, 300 + v * 700, .35f, Onda.Seno, .25f); break;
                case "sealed": { float[] fs = { 523, 659, 784 }; for (int i = 0; i < 3; i++) Tom(N, N, 1, fs[i], fs[i], 1.4f, Onda.Seno, .22f, i * .09f); break; }
                case "doom": foreach (float f in new[] { 98f, 116, 138 }) Tom(N, N, 1, f, f * .97f, 2.6f, Onda.Serra, .14f); Tom(N, N, 1, 60, 30, 2.5f, Onda.Seno, .7f); break;
                case "heart": Tom(N, N, 1, 70, 40, .14f, Onda.Seno, .8f); Tom(N, N, 1, 62, 38, .14f, Onda.Seno, .6f, .19f); break;
                case "corvo":
                    for (int i = 0; i < 2; i++) { Tom(x, z, 40, 560, 380, .22f, Onda.Serra, .16f, i * .26f); Ruido(x, z, 40, .2f, Tipo.PassaFaixa, 1400, 3, .25f, false, i * .26f); }
                    for (int i = 0; i < 6; i++) Ruido(x, z, 26, .06f, Tipo.PassaBaixa, 500, 1, .3f, false, i * .09f); break;
                case "bell": { float[] fs = { 180, 362, 544 }; for (int i = 0; i < 3; i++) Tom(N, N, 1, fs[i], fs[i], 5, Onda.Seno, .08f / (i + 1)); break; }
                case "sinoGrande":
                    {
                        float[] r = { .5f, 1, 1.19f, 1.56f, 2, 2.51f }, g = { .22f, .18f, .1f, .08f, .06f, .04f };
                        for (int i = 0; i < 6; i++) Tom(N, N, 1, 98 * r[i], 98 * r[i] * .995f, 7 - i * .8f, Onda.Seno, g[i]);
                        Ruido(N, N, 1, .15f, Tipo.PassaFaixa, 900, 2, .15f); break;
                    }
                case "batida": Tom(N, N, 1, 70, 32, .35f, Onda.Seno, .35f + v * .3f); Ruido(N, N, 1, .12f, Tipo.PassaBaixa, 180, 1, .25f); break;
                case "click": Tom(N, N, 1, 900, 700, .05f, Onda.Quadrada, .08f); break;
                case "collect": Ruido(x, z, 26, .5f, Tipo.PassaFaixa, 700, 2, .5f); break;
                case "flare": Ruido(x, z, 20, .6f, Tipo.PassaAlta, 2000, 1, .3f); break;
                case "rune": Tom(x, z, 30, 300, 900, .6f, Onda.Triangulo, .3f); break;
                case "arrasto": Tom(x, z, 45, 70, 40, 1.8f, Onda.Serra, .25f); Tom(x, z, 45, 520, 180, 1.2f, Onda.Triangulo, .12f, .3f); break;
                case "push": Ruido(x, z, 28, .35f, Tipo.PassaBaixa, 300, 1, .9f); Tom(x, z, 28, 90, 40, .3f, Onda.Seno, .6f); break;
                case "flash": Ruido(x, z, 30, .4f, Tipo.PassaAlta, 3000, 1, .7f); Tom(x, z, 30, 2400, 1800, .25f, Onda.Seno, .2f); break;
                case "purify": foreach (float f in new[] { 392f, 523 }) Tom(x, z, 30, f, f * 1.5f, .7f, Onda.Seno, .25f); break;
                case "reload":
                    Ruido(x, z, 18, .05f, Tipo.PassaAlta, 2400, 1, .35f); Ruido(x, z, 18, .06f, Tipo.PassaFaixa, 1400, 4, .45f, false, .42f);
                    Ruido(x, z, 18, .05f, Tipo.PassaAlta, 2800, 1, .4f, false, 1.15f); Tom(x, z, 18, 900, 700, .05f, Onda.Quadrada, .06f, 1.15f); break;
                case "vazio": Tom(N, N, 1, 1800, 1600, .03f, Onda.Quadrada, .07f); break;
                case "cabeca": Tom(N, N, 1, 2400, 2400, .12f, Onda.Seno, .18f); Tom(N, N, 1, 3600, 3600, .08f, Onda.Seno, .08f); break;
                case "derrubou": Tom(N, N, 1, 180, 60, .35f, Onda.Seno, .5f); Tom(N, N, 1, 660, 660, .5f, Onda.Triangulo, .12f, .06f); break;
                case "estouro": Ruido(x, z, 22, .25f, Tipo.PassaFaixa, 900, 2, .5f); Tom(x, z, 22, 420, 90, .25f, Onda.Triangulo, .25f); break;
                case "alucinacao": for (int i = 0; i < 3; i++) Ruido(0, 0, 1, .09f, Tipo.PassaBaixa, 520, .8f, .6f, true, i * .38f); break;
            }
        }
        public void Hab(string cls, float x, float z)
        {
            if (cls == "soldado") Tom(x, z, 40, 5200, 3000, .12f, Onda.Quadrada, .12f);
            else if (cls == "exorcista") { float[] fs = { 784, 1046, 1318 }; for (int i = 0; i < 3; i++) Tom(x, z, 34, fs[i], fs[i], .9f, Onda.Seno, .12f, i * .07f); }
            else if (cls == "guardiao") Tom(x, z, 34, 60, 30, .5f, Onda.Seno, .9f);
            else Tom(x, z, 34, 220, 660, .9f, Onda.Serra, .08f);
        }

        // ---------- laços ----------
        class LacoRitual
        {
            public volatile bool ligado; public volatile float alvoOut, alvoPan, alvoW, alvoP, alvoC, alvoTr; public float tr, selAnt; public double pz, pc, fTrem;
            public float out_, pan, gW, gP, gC; public Biquad bp = new Biquad(), lp = new Biquad(); public double fW, fP, fL, fS0, fS1, fS2, fS3; public float[] det = new float[4];
        }
        readonly LacoRitual[] AV = new LacoRitual[8];
        volatile float ambAlvo = .09f, grandeAlvo; float amb, grande; Biquad ambLp = new Biquad(), coroBp = new Biquad(); double fAmb, fCoro0, fCoro1, fCoro2, fCoro3, fCoro4, fGc, fD0, fD1, fTr;

        // chamado a cada quadro pela partida (sync de áudio dos altares e do Grande Ritual)
        public void Rituais(Jogo g, string papel, float grk, float dt)
        {
            ambAlvo = .09f * (1 - .85f * grk);
            for (int i = 0; i < AV.Length; i++)
            {
                var O = AV[i]; var A = g != null && g.phase == "play" && i < g.altars.Count ? g.altars[i] : null;
                string st = A == null ? "" : A.state; if (A != null && papel == "H" && st == "awake") { string kk = g.know[A.i]; if (!(kk == "desperto" || kk == "confirmado" || kk == "chamariz" || kk == "ativo")) st = "dormant"; }
                if (A != null && A.state == "active" && (papel == "C" || st == "active"))
                {
                    if (!O.ligado) { for (int k = 0; k < 4; k++) O.det[k] = 1 + Random.value * .008f; O.out_ = 0; O.ligado = true; }
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
            ambLp.Ajustar(Tipo.PassaBaixa, 260 + 120 * Math.Sin(2 * Math.PI * .07 * tb), 1, sr);
            amb += (ambAlvo - amb) * (1 - Mathf.Exp(-kBloco / .5f));
            foreach (var O in AV)
            {
                if (!O.ligado) continue;
                O.bp.Ajustar(Tipo.PassaFaixa, 1500 + 800 * Math.Sin(2 * Math.PI * 5.3 * tb), 5, sr); O.lp.Ajustar(Tipo.PassaBaixa, 820, 1, sr);
                O.out_ += (O.alvoOut - O.out_) * (1 - Mathf.Exp(-kBloco / .12f)); O.pan += (O.alvoPan - O.pan) * (1 - Mathf.Exp(-kBloco / .1f));
                O.tr += (O.alvoTr - O.tr) * (1 - Mathf.Exp(-kBloco / (O.alvoTr > O.tr ? .12f : .6f)));
                O.gW += (O.alvoW - O.gW) * (1 - Mathf.Exp(-kBloco / .2f)); O.gP += (O.alvoP - O.gP) * (1 - Mathf.Exp(-kBloco / .3f)); O.gC += (O.alvoC - O.gC) * (1 - Mathf.Exp(-kBloco / .3f));
                if (O.alvoOut == 0 && O.out_ < .0005f) O.ligado = false;
            }
            coroBp.Ajustar(Tipo.PassaFaixa, 720, 2.2, sr);
            for (int i = 0; i < gAmb.Length; i++) gAmb[i] += (alvoAmb[i] - gAmb[i]) * (1 - Mathf.Exp(-kBloco / .8f));
            grande += (grandeAlvo - grande) * (1 - Mathf.Exp(-kBloco / (grandeAlvo > grande ? 1.5f : .8f)));
            for (int s = 0; s < n; s++)
            {
                double t = agoraAudio + s * dtS; double L = 0, R = 0;
                double ruido = rnd.NextDouble() * 2 - 1;
                // vento da noite
                double a = ambLp.Passo(ruido) * amb; L += a; R += a;
                // v6: fundos por lugar
                for (int ia = 0; ia < lacoAmb.Length; ia++)
                {
                    var lg = lacoAmb[ia]; if (lg == null || gAmb[ia] < .0005f) continue;
                    pAmb[ia] += (double)lg.sr / sr; if (pAmb[ia] >= lg.d.Length - 1) pAmb[ia] -= lg.d.Length - 1; double am = lg.d[(int)pAmb[ia]] * gAmb[ia]; L += am; R += am;
                }
                // rituais
                foreach (var O in AV)
                {
                    if (!O.ligado) continue;
                    double w, coro;
                    O.fP += 46 * dtS; O.fW += 1.3 * dtS; double pu = Math.Sin(2 * Math.PI * O.fP) * (.5 + .5 * Math.Sin(2 * Math.PI * O.fW)) * O.gP;
                    if (lacoZ != null && lacoC != null)
                    {   // gravações em laço (a pulsação grave continua sintetizada)
                        O.pz += (double)lacoZ.sr / sr * O.det[0]; if (O.pz >= lacoZ.d.Length - 1) O.pz -= lacoZ.d.Length - 1;
                        O.pc += (double)lacoC.sr / sr * O.det[1]; if (O.pc >= lacoC.d.Length - 1) O.pc -= lacoC.d.Length - 1;
                        w = lacoZ.d[(int)O.pz] * O.gW; coro = lacoC.d[(int)O.pc] * 1.6 * O.gC;
                    }
                    else
                    {
                        w = O.bp.Passo(rnd.NextDouble() * 2 - 1) * O.gW;
                        O.fS0 += 110 * O.det[0] * dtS; O.fS1 += 130.8 * O.det[1] * dtS; O.fS2 += 164.8 * O.det[2] * dtS; O.fS3 += 220.4 * O.det[3] * dtS;
                        coro = O.lp.Passo(Osc(Onda.Serra, O.fS0) + Osc(Onda.Serra, O.fS1) + Osc(Onda.Serra, O.fS2) + Osc(Onda.Serra, O.fS3)) * O.gC;
                    }
                    O.fTrem += 7 * dtS; double trem = 1 + O.tr * .35 * Math.Sin(2 * Math.PI * O.fTrem);
                    double m = (w + pu + coro) * O.out_ * trem; float ang = (O.pan + 1) * .25f * Mathf.PI; L += m * Math.Cos(ang); R += m * Math.Sin(ang);
                }
                // Grande Ritual: coro com formante de "ah", bordão grave com tremor
                if (grande > .0005f)
                {
                    fCoro0 += 110 * .992 * dtS; fCoro1 += 130.81 * .996 * dtS; fCoro2 += 164.81 * dtS; fCoro3 += 196 * 1.004 * dtS; fCoro4 += 220.5 * 1.008 * dtS; fGc += .09 * dtS; fD0 += 55 * dtS; fD1 += 41.2 * dtS; fTr += .25 * dtS;
                    double c = coroBp.Passo(Osc(Onda.Serra, fCoro0) + Osc(Onda.Serra, fCoro1) + Osc(Onda.Serra, fCoro2) + Osc(Onda.Serra, fCoro3) + Osc(Onda.Serra, fCoro4)) * (.05 + .035 * Math.Sin(2 * Math.PI * fGc));
                    double d = (Math.Sin(2 * Math.PI * fD0) + Math.Sin(2 * Math.PI * fD1)) * (.16 + .07 * Math.Sin(2 * Math.PI * fTr));
                    double gm = (c + d) * grande; L += gm; R += gm;
                }
                // vozes curtas
                for (int vi = 0; vi < vozes.Count; vi++)
                {
                    var v = vozes[vi]; double tv = t - v.ini; if (tv < 0) continue;
                    if (v.amostra != null)
                    {
                        double ps = tv * sr * v.passo; int i0 = (int)ps; if (i0 >= v.amostra.Length - 1) continue;
                        double xa = v.amostra[i0] + (v.amostra[i0 + 1] - v.amostra[i0]) * (ps - i0); if (v.filtro != null) xa = v.filtro.Passo(xa);
                        xa *= v.vol; L += xa * v.gE; R += xa * v.gD; continue;
                    }
                    double env;
                    if (v.subida > 0) env = tv < v.subida ? v.pico * tv / v.subida : tv < v.queda ? v.pico * (1 - (tv - v.subida) / (v.queda - v.subida)) : 0;
                    else env = tv < v.ataque ? v.vol * tv / v.ataque : v.vol * Math.Pow(.0001 / Math.Max(v.vol, 1e-5), (tv - v.ataque) / Math.Max(v.dur - v.ataque, 1e-3));
                    if (tv > v.dur && v.subida <= 0) env = 0;
                    double x;
                    if (v.ruido)
                    {
                        if (v.filtro == null) { v.filtro = new Biquad(); v.filtro.Ajustar(v.ftipo, v.ff0, v.fq, sr); }
                        if (v.ff1 != v.ff0 && (s & 31) == 0) v.filtro.Ajustar(v.ftipo, v.ff0 * Math.Pow(v.ff1 / v.ff0, Math.Min(1, tv / 2.2)), v.fq, sr);
                        x = v.filtro.Passo(rnd.NextDouble() * 2 - 1);
                        if (v.hs > 0) x += Math.Sin(2 * Math.PI * 4200 * tv) * v.hs * Math.Pow(.0001 / .02, Math.Min(1, tv / v.dur)) / Math.Max(env, 1e-6) * (env > 0 ? 1 : 0);
                    }
                    else { double f = v.f0 * Math.Pow(v.f1 / v.f0, Math.Min(1, tv / v.dur)); v.fase += f * dtS; x = Osc(v.onda, v.fase); }
                    double y = x * env; L += y * v.gE; R += y * v.gD;
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
