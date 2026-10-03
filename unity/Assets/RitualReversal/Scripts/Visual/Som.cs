// Som do protótipo, sintetizado na hora como no Web Audio: cada efeito é ruído filtrado (biquad passa-baixa,
// passa-alta ou passa-faixa) ou um oscilador (seno, quadrada, serra, triângulo) com rampa de frequência, envelope
// de ataque curto e queda exponencial, posição estéreo e abafamento pelas paredes (occlusion). Os laços contínuos
// também: vento da noite, o canto de cada ritual (sussurro, pulsação grave e coro que entram com o progresso) e o
// coro, o bordão e o tambor do Grande Ritual. Tudo é misturado em OnAudioFilterRead, sem nenhum arquivo de áudio.
using System;
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
            if (FindObjectOfType<AudioListener>() == null) onde.AddComponent<AudioListener>();
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
            public volatile bool ligado; public volatile float alvoOut, alvoPan, alvoW, alvoP, alvoC;
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
                O.gW += (O.alvoW - O.gW) * (1 - Mathf.Exp(-kBloco / .2f)); O.gP += (O.alvoP - O.gP) * (1 - Mathf.Exp(-kBloco / .3f)); O.gC += (O.alvoC - O.gC) * (1 - Mathf.Exp(-kBloco / .3f));
                if (O.alvoOut == 0 && O.out_ < .0005f) O.ligado = false;
            }
            coroBp.Ajustar(Tipo.PassaFaixa, 720, 2.2, sr);
            grande += (grandeAlvo - grande) * (1 - Mathf.Exp(-kBloco / (grandeAlvo > grande ? 1.5f : .8f)));
            for (int s = 0; s < n; s++)
            {
                double t = agoraAudio + s * dtS; double L = 0, R = 0;
                double ruido = rnd.NextDouble() * 2 - 1;
                // vento da noite
                double a = ambLp.Passo(ruido) * amb; L += a; R += a;
                // rituais
                foreach (var O in AV)
                {
                    if (!O.ligado) continue;
                    double w = O.bp.Passo(rnd.NextDouble() * 2 - 1) * O.gW;
                    O.fP += 46 * dtS; O.fW += 1.3 * dtS; double pu = Math.Sin(2 * Math.PI * O.fP) * (.5 + .5 * Math.Sin(2 * Math.PI * O.fW)) * O.gP;
                    O.fS0 += 110 * O.det[0] * dtS; O.fS1 += 130.8 * O.det[1] * dtS; O.fS2 += 164.8 * O.det[2] * dtS; O.fS3 += 220.4 * O.det[3] * dtS;
                    double coro = O.lp.Passo(Osc(Onda.Serra, O.fS0) + Osc(Onda.Serra, O.fS1) + Osc(Onda.Serra, O.fS2) + Osc(Onda.Serra, O.fS3)) * O.gC;
                    double m = (w + pu + coro) * O.out_; float ang = (O.pan + 1) * .25f * Mathf.PI; L += m * Math.Cos(ang); R += m * Math.Sin(ang);
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
