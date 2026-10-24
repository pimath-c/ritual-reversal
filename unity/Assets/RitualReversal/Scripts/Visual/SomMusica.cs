// Música da tela inicial e trilha de tensão (MUS e TN do index.html), sintetizadas no OnAudioFilterRead do Som.
// Música: órgão em ré menor (Rém, Si♭, Solm, Lá), um acorde a cada 6,4 s com três parciais por nota, coro em "ah" (dente de
// serra por três formantes, com vibrato) a cada dois acordes e um sino a cada volta, tudo com eco de nave (0,43 s de atraso,
// realimentação de 0,52 por um passa-baixa de 1600 Hz). Só fora da partida.
// Tensão: um bordão grave (55, 55,4 e 82,6 Hz com tremor lento) e um chiado alto (ruído num passa-faixa que passeia em
// torno de 3200 Hz) que crescem com o perigo; a partida decide o nível (Tensao) e o som só segue.
using System;
using System.Collections.Generic;

namespace RitualReversal.Visual
{
    public partial class Som
    {
        // ---------- música ----------
        class NotaMus { public double t0, dur, f, g, fase, vibF; public int tipo; public Onda onda; public Biquad[] formantes; public double[] gf; } // tipo 0 órgão, 1 coro, 2 sino
        static readonly int[][] MUS_ACORDES = { new[] { 50, 53, 57, 62 }, new[] { 46, 50, 53, 58 }, new[] { 43, 50, 55, 58 }, new[] { 45, 52, 57, 61 } };
        static readonly int[] MUS_CORO = { 69, 70, 67, 69, 65, 67, 64, 62 };
        static double MidiHz(int n) { return 440 * Math.Pow(2, (n - 69) / 12.0); }
        readonly List<NotaMus> notasMus = new List<NotaMus>(); volatile bool musicaLigada; float musK; double musProx; int musI;
        float[] ecoMus; int ecoPos; readonly Biquad ecoLp = new Biquad();
        public void Musica(bool ligada) { musicaLigada = ligada; }
        void AgendarAcorde(double t0)
        {
            var A = MUS_ACORDES[musI % 4]; const double dur = 6.4;
            foreach (int n in A)
                foreach (var p in new[] { new object[] { 1.0, Onda.Seno, .05 }, new object[] { 2.0, Onda.Triangulo, .018 }, new object[] { 4.0, Onda.Seno, .006 } })
                    notasMus.Add(new NotaMus { t0 = t0, dur = dur, f = MidiHz(n) * (double)p[0] * Math.Pow(2, (rnd.NextDouble() - .5) * 6 / 1200), onda = (Onda)p[1], g = (double)p[2], tipo = 0 });
            if (musI % 2 == 1)
            {
                var c = new NotaMus { t0 = t0, dur = dur, f = MidiHz(MUS_CORO[(musI >> 1) % MUS_CORO.Length]), onda = Onda.Serra, g = .022, tipo = 1, formantes = new[] { new Biquad(), new Biquad(), new Biquad() }, gf = new[] { 1, .6, .18 } };
                c.formantes[0].Ajustar(Tipo.PassaFaixa, 780, 7, sr); c.formantes[1].Ajustar(Tipo.PassaFaixa, 1150, 9, sr); c.formantes[2].Ajustar(Tipo.PassaFaixa, 2600, 12, sr); notasMus.Add(c);
            }
            if (musI % 4 == 0) foreach (var b in new[] { new[] { 1, .05, 4.5 }, new[] { 2.76, .022, 2.6 }, new[] { 5.4, .01, 1.4 }, new[] { .5, .03, 5 } }) notasMus.Add(new NotaMus { t0 = t0 + .05, dur = b[2] - .05, f = MidiHz(62) * b[0], g = b[1], tipo = 2, onda = Onda.Seno });
            musI++;
        }
        // uma vez por bloco: o volume que sobe e desce e o próximo acorde
        void BlocoMusica(double tb, float kBloco)
        {
            float alvo = musicaLigada ? 1 : 0; musK += (alvo - musK) * Math.Min(1, kBloco * (alvo > 0 ? .5f : 1.5f));
            if (alvo > 0 && tb > musProx - .4) { if (musProx < tb) musProx = tb + .1; AgendarAcorde(musProx); musProx += 6.4; }
            if (ecoMus == null) { ecoMus = new float[(int)(.43 * sr)]; ecoLp.Ajustar(Tipo.PassaBaixa, 1600, 1, sr); }
            notasMus.RemoveAll(o => tb > o.t0 + o.dur + 2);
        }
        double AmostraMusica(double t, double dtS)
        {
            if (musK < .002f && notasMus.Count == 0) return 0; double s = 0;
            foreach (var o in notasMus)
            {
                double tv = t - o.t0; if (tv < 0) continue; double env;
                if (o.tipo == 0) env = tv < 1.6 ? o.g * tv / 1.6 : tv < o.dur - 1.2 ? o.g : tv < o.dur + 1.8 ? o.g * (1 - (tv - (o.dur - 1.2)) / 3.0) : 0;
                else if (o.tipo == 1) env = tv < 1.4 ? o.g * tv / 1.4 : tv < o.dur ? o.g * (1 - (tv - 1.4) / (o.dur - 1.4)) : 0;
                else env = tv < o.dur ? o.g * Math.Pow(.0001 / o.g, tv / o.dur) : 0;
                if (env <= 0) continue;
                double f = o.f; if (o.tipo == 1) { o.vibF += 5 * dtS; f *= Math.Pow(2, 3.5 * Math.Sin(2 * Math.PI * o.vibF) / 1200); }
                o.fase += f * dtS; double x = Osc(o.onda, o.fase);
                if (o.formantes != null) { double y = 0; for (int i = 0; i < 3; i++) y += o.formantes[i].Passo(x) * o.gf[i]; x = y; }
                s += x * env;
            }
            s *= .7; double eco = ecoLp.Passo(ecoMus[ecoPos]); ecoMus[ecoPos] = (float)(s + eco * .52); ecoPos = (ecoPos + 1) % ecoMus.Length;
            return (s + eco) * .9 * musK;
        }

        // ---------- tensão ----------
        volatile float tensaoAlvo; float tensaoG; double fT0, fT1, fT2, fTr2, fLf; readonly Biquad tensaoBp = new Biquad();
        public void Tensao(float k) { tensaoAlvo = k; }
        void BlocoTensao(double tb, float kBloco)
        {
            tensaoG += (.09f * tensaoAlvo - tensaoG) * (1 - (float)Math.Exp(-kBloco / .25f));
            if (tensaoG > .0002f) tensaoBp.Ajustar(Tipo.PassaFaixa, 3200 + 900 * Math.Sin(2 * Math.PI * fLf), 6, sr);
        }
        double AmostraTensao(double dtS)
        {
            if (tensaoG < .0002f) return 0;
            fT0 += 55 * dtS; fT1 += 55.4 * dtS; fT2 += 82.6 * dtS; fTr2 += .35 * dtS; fLf += .12 * dtS;
            double drone = (Math.Sin(2 * Math.PI * fT0) + Math.Sin(2 * Math.PI * fT1) + Math.Sin(2 * Math.PI * fT2)) * (.5 + .25 * Math.Sin(2 * Math.PI * fTr2));
            double chiado = tensaoBp.Passo(rnd.NextDouble() * 2 - 1) * .18;
            return (drone + chiado) * tensaoG;
        }
    }
}
