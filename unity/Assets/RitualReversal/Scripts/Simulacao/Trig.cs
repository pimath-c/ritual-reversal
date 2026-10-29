// Seno, cosseno e arco-tangente do fdlibm (a mesma biblioteca que o motor V8 usa em Math.sin/cos/atan2).
// O Math.Sin/Atan2 do .NET arredonda diferente em ~3% e ~18% dos casos; com estas funções o C# e o protótipo
// chegam ao mesmo bit, e a simulação dá o mesmo resultado em qualquer plataforma (bom para a rede no futuro).
// Tradução direta de fdlibm 5.3 (Sun Microsystems, 1993; livre para uso), como está em v8/src/base/ieee754.cc.
using System;

namespace RitualReversal.Simulacao
{
    public static partial class JS
    {
        static int Hi(double x) { return (int)(BitConverter.DoubleToInt64Bits(x) >> 32); }
        static uint Lo(double x) { return (uint)BitConverter.DoubleToInt64Bits(x); }
        static double Palavras(int hi, uint lo) { return BitConverter.Int64BitsToDouble(((long)hi << 32) | lo); }

        // ---------- redução do argumento a [-pi/4, pi/4] ----------
        const double invpio2 = 6.36619772367581382433e-01, pio2_1 = 1.57079632673412561417e+00, pio2_1t = 6.07710050650619224932e-11,
            pio2_2 = 6.07710050630396597660e-11, pio2_2t = 2.02226624879595063154e-21, pio2_3 = 2.02226624871116645580e-21, pio2_3t = 8.47842766036889956997e-32;
        static readonly int[] npio2_hw = {
            0x3FF921FB, 0x400921FB, 0x4012D97C, 0x401921FB, 0x401F6A7A, 0x4022D97C, 0x4025FDBB, 0x402921FB, 0x402C463A, 0x402F6A7A, 0x4031475C,
            0x4032D97C, 0x40346B9C, 0x4035FDBB, 0x40378FDB, 0x403921FB, 0x403AB41B, 0x403C463A, 0x403DD85A, 0x403F6A7A, 0x40407E4C, 0x4041475C,
            0x4042106C, 0x4042D97C, 0x4043A28C, 0x40446B9C, 0x404534AC, 0x4045FDBB, 0x4046C6CB, 0x40478FDB, 0x404858EB, 0x404921FB };

        // devolve n e y0 + y1 = x - n*pi/2; false se |x| for grande demais (nunca acontece no jogo)
        static bool RemPio2(double x, out int n, out double y0, out double y1)
        {
            int hx = Hi(x), ix = hx & 0x7fffffff; double z;
            n = 0; y0 = x; y1 = 0;
            if (ix <= 0x3fe921fb) return true;
            if (ix < 0x4002d97c)
            {
                if (hx > 0)
                {
                    z = x - pio2_1;
                    if (ix != 0x3ff921fb) { y0 = z - pio2_1t; y1 = (z - y0) - pio2_1t; }
                    else { z -= pio2_2; y0 = z - pio2_2t; y1 = (z - y0) - pio2_2t; }
                    n = 1; return true;
                }
                z = x + pio2_1;
                if (ix != 0x3ff921fb) { y0 = z + pio2_1t; y1 = (z - y0) + pio2_1t; }
                else { z += pio2_2; y0 = z + pio2_2t; y1 = (z - y0) + pio2_2t; }
                n = -1; return true;
            }
            if (ix <= 0x413921fb)
            {
                double t = Math.Abs(x); n = (int)(t * invpio2 + 0.5); double fn = n; double r = t - fn * pio2_1, w = fn * pio2_1t;
                if (n < 32 && ix != npio2_hw[n - 1]) y0 = r - w;
                else
                {
                    int j = ix >> 20; y0 = r - w; int i = j - ((Hi(y0) >> 20) & 0x7ff);
                    if (i > 16)
                    {
                        t = r; w = fn * pio2_2; r = t - w; w = fn * pio2_2t - ((t - r) - w); y0 = r - w; i = j - ((Hi(y0) >> 20) & 0x7ff);
                        if (i > 49) { t = r; w = fn * pio2_3; r = t - w; w = fn * pio2_3t - ((t - r) - w); y0 = r - w; }
                    }
                }
                y1 = (r - y0) - w;
                if (hx < 0) { y0 = -y0; y1 = -y1; n = -n; }
                return true;
            }
            return false;
        }

        const double S1 = -1.66666666666666324348e-01, S2 = 8.33333333332248946124e-03, S3 = -1.98412698298579493134e-04,
            S4 = 2.75573137070700676789e-06, S5 = -2.50507602534068634195e-08, S6 = 1.58969099521155010221e-10;
        static double KSin(double x, double y, int iy)
        {
            int ix = Hi(x) & 0x7fffffff;
            if (ix < 0x3e400000) { if ((int)x == 0) return x; }
            double z = x * x, v = z * x, r = S2 + z * (S3 + z * (S4 + z * (S5 + z * S6)));
            if (iy == 0) return x + v * (S1 + z * r);
            return x - ((z * (0.5 * y - v * r) - y) - v * S1);
        }
        const double C1 = 4.16666666666666019037e-02, C2 = -1.38888888888741095749e-03, C3 = 2.48015872894767294178e-05,
            C4 = -2.75573143513906633035e-07, C5 = 2.08757232129817482790e-09, C6 = -1.13596475577881948265e-11;
        static double KCos(double x, double y)
        {
            int ix = Hi(x) & 0x7fffffff;
            if (ix < 0x3e400000) { if ((int)x == 0) return 1.0; }
            double z = x * x, r = z * (C1 + z * (C2 + z * (C3 + z * (C4 + z * (C5 + z * C6)))));
            if (ix < 0x3FD33333) return 1.0 - (0.5 * z - (z * r - x * y));
            double qx = ix > 0x3fe90000 ? 0.28125 : Palavras(ix - 0x00200000, 0);
            double iz = 0.5 * z - qx, a = 1.0 - qx;
            return a - (iz - (z * r - x * y));
        }

        public static double Sin(double x)
        {
            int ix = Hi(x) & 0x7fffffff;
            if (ix <= 0x3fe921fb) return KSin(x, 0, 0);
            if (ix >= 0x7ff00000) return x - x;
            int n; double y0, y1; if (!RemPio2(x, out n, out y0, out y1)) return Math.Sin(x);
            switch (n & 3) { case 0: return KSin(y0, y1, 1); case 1: return KCos(y0, y1); case 2: return -KSin(y0, y1, 1); default: return -KCos(y0, y1); }
        }
        public static double Cos(double x)
        {
            int ix = Hi(x) & 0x7fffffff;
            if (ix <= 0x3fe921fb) return KCos(x, 0);
            if (ix >= 0x7ff00000) return x - x;
            int n; double y0, y1; if (!RemPio2(x, out n, out y0, out y1)) return Math.Cos(x);
            switch (n & 3) { case 0: return KCos(y0, y1); case 1: return -KSin(y0, y1, 1); case 2: return -KCos(y0, y1); default: return KSin(y0, y1, 1); }
        }

        // ---------- arco-tangente ----------
        static readonly double[] atanhi = { 4.63647609000806093515e-01, 7.85398163397448278999e-01, 9.82793723247329054082e-01, 1.57079632679489655800e+00 };
        static readonly double[] atanlo = { 2.26987774529616870924e-17, 3.06161699786838301793e-17, 1.39033110312309984516e-17, 6.12323399573676603587e-17 };
        static readonly double[] aT = { 3.33333333333329318027e-01, -1.99999999998764832476e-01, 1.42857142725034663711e-01, -1.11111104054623557880e-01,
            9.09088713343650656196e-02, -7.69187620504482999495e-02, 6.66107313738753120669e-02, -5.83357013379057348645e-02, 4.97687799461593236017e-02,
            -3.65315727442169155270e-02, 1.62858201153657823623e-02 };
        public static double Atan(double x)
        {
            int hx = Hi(x), ix = hx & 0x7fffffff, id;
            if (ix >= 0x44100000)
            {
                if (ix > 0x7ff00000 || (ix == 0x7ff00000 && Lo(x) != 0)) return x + x;
                return hx > 0 ? atanhi[3] + atanlo[3] : -atanhi[3] - atanlo[3];
            }
            if (ix < 0x3fdc0000) { if (ix < 0x3e400000) return x; id = -1; }
            else
            {
                x = Math.Abs(x);
                if (ix < 0x3ff30000) { if (ix < 0x3fe60000) { id = 0; x = (2.0 * x - 1.0) / (2.0 + x); } else { id = 1; x = (x - 1.0) / (x + 1.0); } }
                else { if (ix < 0x40038000) { id = 2; x = (x - 1.5) / (1.0 + 1.5 * x); } else { id = 3; x = -1.0 / x; } }
            }
            double z = x * x, w = z * z;
            double s1 = z * (aT[0] + w * (aT[2] + w * (aT[4] + w * (aT[6] + w * (aT[8] + w * aT[10])))));
            double s2 = w * (aT[1] + w * (aT[3] + w * (aT[5] + w * (aT[7] + w * aT[9]))));
            if (id < 0) return x - x * (s1 + s2);
            z = atanhi[id] - ((x * (s1 + s2) - atanlo[id]) - x);
            return hx < 0 ? -z : z;
        }
        const double pi_o_4 = 7.8539816339744827900E-01, pi_o_2 = 1.5707963267948965580E+00, pi = 3.1415926535897931160E+00, pi_lo = 1.2246467991473531772E-16;
        public static double Atan2(double y, double x)
        {
            if (double.IsNaN(x) || double.IsNaN(y)) return x + y;
            int hx = Hi(x), ix = hx & 0x7fffffff, hy = Hi(y), iy = hy & 0x7fffffff; uint lx = Lo(x), ly = Lo(y);
            if (((hx - 0x3ff00000) | (int)lx) == 0) return Atan(y);
            int m = ((hy >> 31) & 1) | ((hx >> 30) & 2);
            if ((iy | (int)ly) == 0) { switch (m) { case 0: case 1: return y; case 2: return pi; default: return -pi; } }
            if ((ix | (int)lx) == 0) return hy < 0 ? -pi_o_2 : pi_o_2;
            if (ix == 0x7ff00000)
            {
                if (iy == 0x7ff00000) { switch (m) { case 0: return pi_o_4; case 1: return -pi_o_4; case 2: return 3.0 * pi_o_4; default: return -3.0 * pi_o_4; } }
                switch (m) { case 0: return 0.0; case 1: return -0.0; case 2: return pi; default: return -pi; }
            }
            if (iy == 0x7ff00000) return hy < 0 ? -pi_o_2 : pi_o_2;
            int k = (iy - ix) >> 20; double z;
            if (k > 60) { z = pi_o_2 + 0.5 * pi_lo; m &= 1; }
            else if (hx < 0 && k < -60) z = 0.0;
            else z = Atan(Math.Abs(y / x));
            switch (m) { case 0: return z; case 1: return -z; case 2: return pi - (z - pi_lo); default: return (z - pi_lo) - pi; }
        }
    }
}
