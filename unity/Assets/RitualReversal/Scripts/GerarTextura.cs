// Texturas procedurais do blockout: geradas por código (sem baixar nada), sem emenda (repetem nas bordas) e
// quase brancas, para o material tingir com a cor de cada peça. Cada unidade de textura cobre 2 m no mundo.
// Usado pelo importador do mapa (Editor/ImportarMapa.cs), que salva o resultado em Assets/RitualReversal/Texturas.
using UnityEngine;

namespace RitualReversal
{
    public static class GerarTextura
    {
        public static readonly string[] Padroes = { "pedra", "lajota", "madeira", "casca", "terra", "grama", "folhas", "osso" };

        // qual padrão cada material do importador usa
        public static string PadraoDe(string material)
        {
            switch (material)
            {
                case "chao": case "clareira": return "grama";
                case "trilha": case "trilha_estreita": return "terra";
                case "piso": case "altar": return "lajota";
                case "madeira": case "biombo": case "caixote": case "banco": case "tenda": case "marco": return "madeira";
                case "casca": case "tronco": case "raiz": case "carvalho": case "arvore": return "casca";
                case "sebe": case "espinheiro": case "copa": return "folhas";
                case "ossos": return "osso";
                case "rocha": return "terra";
                default: return "pedra";
            }
        }

        // ruído de valor periódico (sem emenda) em várias oitavas
        static float Hash(int x, int y, int semente) { unchecked { int h = x * 374761393 + y * 668265263 + semente * 982451653; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float Valor(float x, float y, int periodo, int semente)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y); float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int ax = ((x0 % periodo) + periodo) % periodo, bx = (ax + 1) % periodo, ay = ((y0 % periodo) + periodo) % periodo, by = (ay + 1) % periodo;
            float a = Hash(ax, ay, semente), b = Hash(bx, ay, semente), c = Hash(ax, by, semente), d = Hash(bx, by, semente);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
        static float Fbm(float u, float v, int base_, int oitavas, int semente)
        {
            float s = 0, amp = .5f, tot = 0; int p = base_;
            for (int o = 0; o < oitavas; o++) { s += Valor(u * p, v * p, p, semente + o * 17) * amp; tot += amp; amp *= .5f; p *= 2; }
            return s / tot;
        }

        public static Texture2D Criar(string padrao, int tam = 256)
        {
            var t = new Texture2D(tam, tam, TextureFormat.RGBA32, true) { name = padrao, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            var px = new Color[tam * tam];
            for (int j = 0; j < tam; j++)
                for (int i = 0; i < tam; i++)
                {
                    float u = (float)i / tam, v = (float)j / tam; Color c;
                    switch (padrao)
                    {
                        case "pedra": c = Pedra(u, v, 6, 8, .09f, true); break;     // blocos de 33 × 50 cm, fiadas desencontradas
                        case "lajota": c = Pedra(u, v, 3, 6, .05f, false); break;   // lajes quadradas de ~67 cm
                        case "madeira":
                            { float g = Fbm(u, v, 4, 4, 5); float veio = .5f + .5f * Mathf.Sin((v * 22f + g * 6f) * Mathf.PI); float tabua = Mathf.Repeat(u * 5f, 1f) < .04f ? .55f : 1f;
                                float k = (.72f + .18f * veio + .1f * Fbm(u, v, 16, 3, 9)) * tabua; c = new Color(k, k * .96f, k * .9f); break; }
                        case "casca":
                            { float sulco = Fbm(u * 1f, v, 12, 3, 3); float fend = Mathf.Abs(Mathf.Sin((u * 14f + Fbm(u, v, 3, 3, 4) * 3f) * Mathf.PI));
                                float k = .55f + .3f * Mathf.Pow(fend, .35f) * (.6f + .4f * sulco); c = new Color(k, k * .95f, k * .9f); break; }
                        case "grama":
                            { float g = Fbm(u, v, 6, 5, 11), f = Fbm(u, v, 64, 2, 12); float k = .42f + .55f * g + .3f * (f - .5f);
                                c = new Color(k * .92f, k, k * .88f); break; }
                        case "folhas":
                            { float g = Fbm(u, v, 10, 4, 21), f = Fbm(u, v, 40, 2, 22); float k = .45f + .45f * Mathf.SmoothStep(.3f, .75f, g) + .12f * (f - .5f);
                                c = new Color(k * .9f, k, k * .85f); break; }
                        case "osso":
                            { float g = Fbm(u, v, 8, 4, 31); float k = .55f + .45f * g + .15f * (Fbm(u, v, 32, 2, 33) - .5f); c = new Color(k, k * .97f, k * .9f); break; }
                        default: // terra
                            { float g = Fbm(u, v, 5, 5, 41), f = Fbm(u, v, 48, 2, 42); float seixo = f > .72f ? .15f : 0f; float k = .62f + .3f * g + seixo;
                                c = new Color(k, k * .95f, k * .88f); break; }
                    }
                    c.a = 1f; px[j * tam + i] = c;
                }
            t.SetPixels(px); t.Apply(true); return t;
        }

        // blocos em fiadas: 'linhas' fiadas e 'colunas' blocos por unidade de textura, rejunte escuro, cada bloco com tom próprio
        static Color Pedra(float u, float v, int linhas, int colunas, float rejunte, bool desencontrar)
        {
            float lv = v * linhas; int fila = Mathf.FloorToInt(lv); float desl = desencontrar ? (fila % 2) * .5f : 0f;
            float lu = u * colunas / 2f + desl; int bloco = Mathf.FloorToInt(lu);
            float fu = lu - bloco, fv = lv - fila;
            float borda = Mathf.Min(Mathf.Min(fu, 1 - fu) * 2f / colunas * linhas, Mathf.Min(fv, 1 - fv));
            int porUnidade = colunas / 2; float tom = .6f + .4f * Hash(((bloco % porUnidade) + porUnidade) % porUnidade, fila % linhas, 7);
            float grao = Fbm(u, v, 16, 3, 8), manchas = Fbm(u, v, 4, 3, 2);
            float k = tom * (.68f + .32f * grao) * (.75f + .25f * manchas);
            if (borda < rejunte) k *= .5f + .3f * borda / rejunte;
            return new Color(k, k * .98f, k * .95f);
        }
    }
}
