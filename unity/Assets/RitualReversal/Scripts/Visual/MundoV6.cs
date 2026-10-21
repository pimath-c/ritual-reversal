// O mundo da v6 (client/mundo.js): catedral em cruz no promontório, desfiladeiro com três travessias e as peças novas
// da mata. Só visual: toda a colisão, a visão e as regras continuam nas caixas da simulação. Onde a peça visual fica
// mais alta que a caixa (torres, telhados), é porque a altura da caixa só importa até onde alguém consegue atirar.
// Mesmas medidas, mesma ordem de sorteio (vr) e mesmos materiais do navegador; tudo nas coordenadas do protótipo.
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        // alturas visuais (as da prancha docs/conceito/catedral.svg)
        public static class ALT { public const float lateral = 15, clerestorio = 27, cumeeira = 38, transepto = 27, empena = 34, torreO = 54, agulhaO = 98, lanterna = 48, agulhaC = 88, torreL = 30, abside = 27; }

        Material mPedra, mPedraEsc, mPedraClara, mTelha, mTeto, mChumbo, mRocha, mFundo, mVidro, mVidroFora, mRosa, mFeixe, mkPedra, mkTelha, mkChumbo;
        Geo gVidros, gVidrosFora, gRosas, gFeixes;
        readonly List<Material> nevoasAbismo = new List<Material>();

        // ---------------- materiais próprios ----------------
        void CriarMateriaisV6()
        {
            Func<int, Material> pedra = cor => Mat.Pbr(Tex.wall, cor, 1, null, .55f);
            mPedra = pedra(0x8a8a90); mPedraEsc = pedra(0x5e5e68); mPedraClara = pedra(0xa8a49a);
            Func<int, Material> telha = cor => Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(cor), mapa = Tex.wall.mapa, relevo = Tex.wall.relevo, relevoK = .55f, duplaFace = true });
            mTelha = telha(0x2c2a30); mTeto = telha(0x24242c);
            mChumbo = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x3a3e48), rugosidade = .55f, metal = .5f }); facetados.Add(mChumbo);
            mRocha = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x6a6862), mapa = Tex.rocha.mapa, relevo = Tex.rocha.relevo, relevoK = 1.4f, repetir = new Vector2(.25f, .25f), duplaFace = true });
            mFundo = Mat.Brilho(Mat.Hex(0x020204), null, Mat.Mistura.Alfa, false, true, 0, true, 2000);
            // vitrais: o Grande Ritual tinge estes três de sangue (MundoVida)
            winCor = Mat.Lin(new Color(1.05f, 1, 1.2f)); winForaCor = Mat.Lin(new Color(.55f, .5f, .72f)); roseCor = Mat.Lin(new Color(1.1f, 1.02f, 1.2f));
            mVidro = winMat = Mat.Brilho(winCor, Tex.Vitral(false), Mat.Mistura.Alfa, false, false, .1f, true, 2460);
            mVidroFora = winFora = Mat.Brilho(winForaCor, Tex.Vitral(false), Mat.Mistura.Alfa, false, false, .1f, true, 2460);
            mRosa = roseMat = Mat.Brilho(roseCor, Tex.Vitral(true), Mat.Mistura.Alfa, false, false, .1f, true, 2460);
            mFeixe = Mat.Brilho(Mat.Hex(0x8fa6d8, .075f), Tex.RaioLuz(), Mat.Mistura.Aditiva, false, false, 0, false, 3002);
            // marcos: torres, agulhas e telhados. No navegador eles sentem só um quarto da névoa (a silhueta se lê de toda a
            // mata); aqui usam o shader RitualReversal/Marco, que faz o mesmo, quando ele existe
            mkPedra = Marco(pedra(0x3c3c46)); mkTelha = Marco(telha(0x1e1e24)); mkChumbo = Marco(Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x3a3e48), rugosidade = .55f, metal = .5f })); facetados.Add(mkChumbo);
            var nomes = new Dictionary<string, Material> { { "pedra", mPedra }, { "pedraEsc", mPedraEsc }, { "pedraClara", mPedraClara }, { "telha", mTelha }, { "teto", mTeto }, { "chumbo", mChumbo }, { "rochaAbismo", mRocha }, { "fundo", mFundo },
                { "vitral", mVidro }, { "vitralFora", mVidroFora }, { "rosacea", mRosa }, { "raioVitral", mFeixe }, { "marcoPedra", mkPedra }, { "marcoTelha", mkTelha }, { "marcoChumbo", mkChumbo } };
            foreach (var kv in nomes) kv.Value.name = kv.Key;
            gVidros = new Geo(); gVidrosFora = new Geo(); gRosas = new Geo(); gFeixes = new Geo();
        }
        // material de marco: mesma cor e textura do Lit, com a névoa reduzida (shader próprio; sem ele, fica o Lit)
        static Material Marco(Material m)
        {
            var sh = Shader.Find("RitualReversal/Marco"); if (sh == null || !sh.isSupported) return m;
            var r = new Material(sh) { enableInstancing = true };
            r.SetColor("_BaseColor", m.GetColor("_BaseColor"));
            if (m.GetTexture("_BaseMap") != null) { r.SetTexture("_BaseMap", m.GetTexture("_BaseMap")); r.SetTextureScale("_BaseMap", m.GetTextureScale("_BaseMap")); }
            r.SetFloat("_Cull", m.HasProperty("_Cull") ? m.GetFloat("_Cull") : 2); r.SetFloat("_NevoaK", .07f);
            return r;
        }

        // ---------------- ajudantes de geometria ----------------
        void B(float x1, float x2, float y1, float y2, float z1, float z2, Material mat = null, float S = 4)
        { Caixa(Math.Min(x1, x2), Math.Max(x1, x2), y1, y2, Math.Min(z1, z2), Math.Max(z1, z2), mat ?? mPedra, S); }
        // janela gótica (lanceta) plana: vidro dos dois lados de uma parede; n = normal da face (+1/-1) no eixo dado
        void Lanceta(char eixo, float fixo, float u, float y, float larg, float alt, int n, bool feixe)
        {
            Action<float, Geo> mk = (dx, lista) =>
            {
                var g = Geo.Plano(larg, alt).Mover(0, y + alt / 2, 0);
                if (eixo == 'x') { if (n * dx < 0) g.GirarY(PI); g.Mover(u, 0, fixo + dx); } else { g.GirarY(n * dx > 0 ? PI / 2 : -PI / 2); g.Mover(fixo + dx, 0, u); }
                lista.Juntar(g);
            };
            mk(-n * .56f, gVidros); mk(n * .56f, gVidrosFora);
            if (feixe)
            {
                float L = alt * 1.9f; var gg = Geo.Unir(new[] { Geo.Plano(larg * 1.1f, L), Geo.Plano(larg * 1.1f, L).GirarY(PI / 2) });
                gg.Mover(0, -L / 2, 0); gg.GirarX(n * (eixo == 'x' ? -.62f : 0)); gg.GirarZ(n * (eixo == 'z' ? .62f : 0));
                if (eixo == 'x') gg.Mover(u, y + alt * .8f, fixo - n * .2f); else gg.Mover(fixo - n * .2f, y + alt * .8f, u);
                gFeixes.Juntar(gg);
            }
        }
        void Rosacea(char eixo, float fixo, float u, float y, float r, int n)
        {
            for (int c = 0; c < 2; c++)
            {
                float dx = c == 0 ? -n * .56f : n * .56f; var g = Geo.Circulo(r, 40);
                if (eixo == 'x') { if (n * dx < 0) g.GirarY(PI); g.Mover(u, y, fixo + dx); } else { g.GirarY(n * dx > 0 ? PI / 2 : -PI / 2); g.Mover(fixo + dx, y, u); }
                (c == 1 ? gVidrosFora : gRosas).Juntar(g);
            }
            Est(Geo.Toro(r + .25f, .32f, 8, 40), mPedraEsc, TRS(eixo == 'x' ? u : fixo, y, eixo == 'x' ? fixo : u, 0, eixo != 'x' ? PI / 2 : 0, 0), false);
        }
        // telhado de duas águas ao longo de x ou z (cumeeira no meio)
        void Telhado(float x1, float x2, float z1, float z2, float yBase, float yTopo, bool aoLongoX, Material mat)
        {
            float larg = (aoLongoX ? z2 - z1 : x2 - x1) / 2, comp = aoLongoX ? x2 - x1 : z2 - z1, alt = yTopo - yBase, lado = Mathf.Sqrt(alt * alt + larg * larg), inc = Mathf.Atan2(alt, larg);
            var G = TRS((x1 + x2) / 2, yBase, (z1 + z2) / 2, 0, aoLongoX ? 0 : PI / 2, 0);
            foreach (int s in new[] { -1, 1 }) Est(Geo.Plano(comp + .6f, lado + .5f), mat ?? mkTelha, G * TRS(0, alt / 2, s * larg / 2, -PI / 2 + s * inc, 0, 0));
            Est(Geo.Caixa(comp + .6f, .35f, .35f), mkChumbo, G * M4.T(0, alt + .05f, 0), false);
        }
        void Pinaculo(float x, float y, float z, float h, float r, Material mat = null)
        {
            Est(Geo.Caixa(r * 2, h * .35f, r * 2), mat ?? mkPedra, x, y + h * .175f, z);
            Est(Geo.Cone(r * 1.05f, h * .65f, 4), mat ?? mkPedra, TRS(x, y + h * .35f + h * .325f, z, 0, PI / 4, 0));
        }
        void Agulha(float x, float z, float yBase, float yTopo, float r, int lados = 8)
        {
            Est(Geo.Cone(r, yTopo - yBase, lados, 6), mkPedra, TRS(x, (yBase + yTopo) / 2, z, 0, PI / 8, 0));
            // cordões de pedra subindo a agulha e uma cruz de ferro no alto
            for (int k = 1; k < 6; k++) { float t = k / 6f; Est(Geo.Toro(r * (1 - t) + .05f, .12f, 4, lados), mkPedra, TRS(x, yBase + (yTopo - yBase) * t, z, PI / 2, 0, PI / 8), false); }
            Est(Geo.Caixa(.25f, 3, .25f), mkChumbo, x, yTopo + 1.4f, z, false); Est(Geo.Caixa(1.4f, .22f, .22f), mkChumbo, x, yTopo + 1.9f, z, false);
        }
        // arcobotante: arco do contraforte até a parede alta (curva quadrática, como a QuadraticBezierCurve3 do three)
        void Arcobotante(float x0, float y0, float x1, float y1, float z, Material mat = null)
        {
            var a = new Vector3(x0, y0, z); var c = new Vector3(x0 + (x1 - x0) * .25f, y1 + 1.2f, z); var b = new Vector3(x1, y1, z); var pts = new List<Vector3>();
            for (int i = 0; i <= 10; i++) { float t = i / 10f; pts.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b); }
            Est(Geo.Tubo(new Geo.Curva(pts), 10, .42f, 4), mat ?? mkPedra, M4.I);
        }
        // quadrilátero p1-p2-p3-p4 (material de dupla face); a normal aponta para cima, como um telhado
        void Quad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Material mat = null)
        {
            var g = new Geo(); var nor = Vector3.Cross(p2 - p1, p3 - p1).normalized; if (nor.y < 0) nor = -nor;
            float L = (p2 - p1).magnitude, H = (p4 - p1).magnitude;
            int a = g.V(p1, nor, new Vector2(0, 0)), b = g.V(p2, nor, new Vector2(L / 4, 0)), c = g.V(p3, nor, new Vector2(L / 4, H / 4)), d = g.V(p4, nor, new Vector2(0, H / 4));
            g.Tri(a, b, c); g.Tri(a, c, d); Est(g.Orientar(), mat ?? mkTelha, M4.I);
        }
        static Geo Nervura(float w, float alto, float tubo = .24f)
        {
            float R = (w * w / 4 + alto * alto) / (2 * alto), f = Mathf.Asin(Mathf.Min(1, (w / 2) / R));
            return Geo.Toro(R, tubo, 5, 24, 2 * f).GirarZ(PI / 2 - f).Mover(0, -R, 0);
        }
        // contorno com um arco (Shape.absarc do three: do ângulo 0 a PI, anti-horário, 24 divisões)
        static void Arco(List<Vector2> l, float cx, float cy, float r) { for (int i = 0; i <= 24; i++) { float a = PI * i / 24; l.Add(new Vector2(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a))); } }
        static Geo Triangulo(float a, float b, float h) { return Geo.Forma(new List<Vector2> { new Vector2(a, 0), new Vector2(b, 0), new Vector2(0, h) }); }
        static Geo ExtrusaoTriangulo(float a, float b, float h, float prof) { return Geo.Extrusao(new List<Vector2> { new Vector2(a, 0), new Vector2(b, 0), new Vector2(0, h) }, prof); }

        // ================= chão: promontório, catedral =================
        // (o chão da mata vem de BuildFloresta, que deixa o desfiladeiro vazio; aqui ficam os pisos de pedra)
        void Pisos()
        {
            Action<float, float, float, float, Material, float, float> piso = (x1, x2, z1, z2, mat, y, rep) =>
            {
                var g = Geo.Plano(x2 - x1, z2 - z1); if (rep > 0) g.EscalarUV((x2 - x1) / rep, (z2 - z1) / rep);
                Est(g, mat, TRS((x1 + x2) / 2, y, (z1 + z2) / 2, -PI / 2, 0, 0), false);
            };
            var lajes = M("lajes");
            foreach (var Z in Mapa.ZONAS) if (Z.name == "Catedral") piso((float)Z.x1, (float)Z.x2, (float)Z.z1, (float)Z.z2, lajes, .014f, 4);
            piso(-11.5f, 11.5f, -97, -78, lajes, .016f, 4); // coro e abside
            piso(-46, 46, -32, -22, lajes, .012f, 3.5f); // adro
            piso(-44.5f, -15.5f, -64.5f, -43.5f, lajes, .013f, 4); // galerias do claustro
            var jardim = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x34402a), mapa = Tex.chao.mapa }); jardim.name = "jardimClaustro";
            var CL = MapaVisual.CLAUSTRO; piso(CL.xMin, CL.xMax, CL.yMin, CL.yMax, jardim, .02f, 0);
            // caminho de lajes do adro até a Sala do Capítulo e até o cemitério
            piso(-70, -44, -46, -40, lajes, .011f, 3.5f); piso(28, 46, -36, -30, lajes, .011f, 3.5f);
        }

        // ================= paredões do desfiladeiro: uma cortina de rocha em cada lado que dá para a terra firme =================
        void Desfiladeiro()
        {
            var A = MapaVisual.ABISMO; var T = MapaVisual.TRAVESSIAS; const float FUNDO = -34; var D0 = MapaVisual.DESFILADEIRO; float HZ = MapaVisual.HZ;
            Func<float, float, bool> noAbismo = (x, z) =>
            {
                foreach (var b in A) if (x > b.x1 - .05f && x < b.x2 + .05f && z > b.z1 - .05f && z < b.z2 + .05f) return true;
                foreach (var t in T) if (t.eixo == "z" ? (x > t.a - 1.05f && x < t.b + 1.05f && z > t.de && z < t.ate) : (z > t.a - 1.05f && z < t.b + 1.05f && x > t.de && x < t.ate)) return true;
                return false;
            };
            var geos = new Geo(); double sd = 90; Func<float> R = () => { sd = (sd * 16807) % 2147483647; return (float)(sd / 2147483647); };
            // relevo da rocha: ruído suave que depende só da posição no mundo, então as fatias de 1 m se encaixam sem fenda
            Func<int, int, float> hs = (i, j) => { double s = Math.Sin(i * 127.1 + j * 311.7) * 43758.5453; return (float)(s - Math.Floor(s)); };
            Func<float, float, float> vn = (x, y) =>
            {
                int i = Mathf.FloorToInt(x), j = Mathf.FloorToInt(y); float fx = x - i, fy = y - j, ux = fx * fx * (3 - 2 * fx), uy = fy * fy * (3 - 2 * fy);
                return Mathf.Lerp(Mathf.Lerp(hs(i, j), hs(i + 1, j), ux), Mathf.Lerp(hs(i, j + 1), hs(i + 1, j + 1), ux), uy);
            };
            Func<float, float, float> relevo = (s, y) => vn(s * .18f, y * .12f) * .6f + vn(s * .5f + 7, y * .4f) * .28f + vn(s * 1.6f, y * 1.3f) * .12f;
            Action<float, float, float, float, float, float> face = (ax, az, bx, bz, nx, nz) =>
            {
                float L = Mathf.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az)); int n = Math.Max(1, (int)JS.Round(L));
                for (int k = 0; k < n; k++)
                {
                    float t0 = (float)k / n, t1 = (float)(k + 1) / n, mx = Mathf.Lerp(ax, bx, (t0 + t1) / 2), mz = Mathf.Lerp(az, bz, (t0 + t1) / 2);
                    if (noAbismo(mx + nx * .6f, mz + nz * .6f)) continue; // do outro lado também é abismo: não é borda
                    var g = Geo.Plano(L / n, -FUNDO, 1, 12).Mover(0, FUNDO / 2, 0).GirarY(Mathf.Atan2(-nx, -nz)).Mover(mx - nx * .02f, 0, mz - nz * .02f);
                    for (int i = 0; i < g.p.Count; i++)
                    {
                        var q = g.p[i]; float s = nx != 0 ? q.z : q.x, borda = Mathf.Clamp01(-q.y / 2.5f), d = relevo(s, q.y) * 2.4f * borda;
                        g.p[i] = new Vector3(q.x - nx * d, q.y, q.z - nz * d); g.uv[i] = new Vector2(s / 3, q.y / 3);
                    }
                    geos.Juntar(g.NormaisDasFaces());
                }
            };
            foreach (var b in A) { face(b.x1, b.z1, b.x1, b.z2, -1, 0); face(b.x2, b.z1, b.x2, b.z2, 1, 0); if (b.z1 > -HZ + .1f) face(b.x1, b.z1, b.x2, b.z1, 0, -1); face(b.x1, b.z2, b.x2, b.z2, 0, 1); }
            // atrás do promontório (ao norte) o mapa termina num penhasco: a mesma rocha, voltada para o vazio
            {
                int n = (int)JS.Round(D0.x2 - D0.x1);
                for (int k = 0; k < n; k++)
                {
                    float x = D0.x1 + k + .5f; var g = Geo.Plano(1, 60, 1, 14).Mover(0, -30, 0).GirarY(PI).Mover(x, 0, -HZ - .6f);
                    for (int i = 0; i < g.p.Count; i++) { var q = g.p[i]; g.p[i] = new Vector3(q.x, q.y, -HZ - .6f - relevo(q.x + 40, q.y) * 3 * Mathf.Clamp01(-q.y / 2.5f)); g.uv[i] = new Vector2(q.x / 3, q.y / 3); }
                    geos.Juntar(g.NormaisDasFaces());
                }
            }
            // pedras soltas na beira, só do lado do abismo (não invadem o chão onde se anda)
            foreach (var b in A)
                foreach (var e in new[] { new[] { b.x1, b.z1, b.x1, b.z2, -1, 0 }, new[] { b.x2, b.z1, b.x2, b.z2, 1, 0 }, new[] { b.x1, b.z2, b.x2, b.z2, 0, 1 } })
                {
                    float ax = e[0], az = e[1], bx = e[2], bz = e[3], nx = e[4], nz = e[5], L = Mathf.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
                    for (float t = 1.5f; t < L; t += 3 + R() * 3)
                    {
                        float x = Mathf.Lerp(ax, bx, t / L) - nx * .7f, z = Mathf.Lerp(az, bz, t / L) - nz * .7f; if (noAbismo(x + nx * 1.3f, z + nz * 1.3f)) continue;
                        float r = .45f + R() * .35f, y = -.15f + R() * .2f, rx = R() * 3, ry = R() * 3, rz = R() * 3;
                        Est(Geo.Dodecaedro(r), mRocha, TRS(x, y, z, rx, ry, rz, 1, .6f, 1));
                    }
                }
            if (geos.p.Count > 0) Est(geos, mRocha, M4.I, false);
            // fundo do abismo e camadas de névoa que se mexem devagar (a névoa some quem cai no olhar; ninguém cai de verdade)
            var mFundoAbismo = Mat.Brilho(Mat.Hex(0x010102), null, Mat.Mistura.Alfa, false, true, 0, true, 2000); mFundoAbismo.name = "fundoAbismo";
            Est(Geo.Plano(D0.x2 - D0.x1 + 10, D0.z2 - D0.z1 + 70), mFundoAbismo, TRS(0, FUNDO + .5f, (D0.z1 + D0.z2) / 2 - 30, -PI / 2, 0, 0), false);
            Texture2D tex;
            {
                var c = new Tela(256, 256); double s2 = 5; Func<float> r2 = () => { s2 = (s2 * 16807) % 2147483647; return (float)(s2 / 2147483647); };
                for (int k = 0; k < 70; k++)
                {
                    float x = r2() * 256, y = r2() * 256, rr = 20 + r2() * 60;
                    c.gradiente = Tela.Radial(x, y, 0, rr, 0f, "rgba(180,196,226,.32)", 1f, "rgba(180,196,226,0)"); c.Retangulo(x - rr, y - rr, rr * 2, rr * 2); // fora do raio o degradê não pinta
                }
                tex = c.Textura(true, "nevoaAbismo");
            }
            float[,] camadas = { { -3.5f, .32f, 7 }, { -9, .42f, 5 }, { -17, .55f, 4 } };
            nevoasAbismo.Clear();
            for (int i = 0; i < 3; i++)
            {
                float y = camadas[i, 0], op = camadas[i, 1], rep = camadas[i, 2];
                var mt = Mat.Brilho(Mat.Hex(0x9aaacc, op), tex, Mat.Mistura.Alfa, false, true, 0, false, 3003 + i); mt.name = "nevoaAbismo" + i;
                mt.SetTextureScale("_MainTex", new Vector2(rep * 2.2f, rep)); nevoasAbismo.Add(mt);
                var p = Malha(Geo.Plano(D0.x2 - D0.x1 + 6, D0.z2 - D0.z1 + 66), mt, raiz, "névoa do abismo", false); Pos(p, 0, y, (D0.z1 + D0.z2) / 2 - 30); Rot(p, -PI / 2, 0, 0);
            }
        }
        // névoa do desfiladeiro andando devagar
        void QuadroV6(float dt)
        {
            for (int i = 0; i < nevoasAbismo.Count; i++)
            {
                var m = nevoasAbismo[i]; var o = m.GetTextureOffset("_MainTex"); o.x += dt * (.004f + i * .002f); o.y += dt * .0015f * (i % 2 == 1 ? 1 : -1);
                m.SetTextureOffset("_MainTex", new Vector2(o.x % 1, o.y % 1));
            }
        }

        // ================= travessias =================
        void Pontes()
        {
            var T = MapaVisual.TRAVESSIAS; TravessiaV PG = T[0], PC = T[1], AQ = T[2];
            // Ponte Grande: tabuleiro de pedra, um arco grande sobre o vazio e os encontros descendo até sumir na névoa
            {
                var sh = new List<Vector2> { new Vector2(PG.de, 0), new Vector2(PG.ate, 0), new Vector2(PG.ate, -16) }; Arco(sh, (PG.de + PG.ate) / 2, -16, (PG.ate - PG.de) / 2 - 1.2f); sh.Add(new Vector2(PG.de, -16));
                // o contorno foi desenhado em (z, y): gira para que o comprimento fique ao longo de z
                Est(Geo.Extrusao(sh, PG.b - PG.a + 2).GirarY(-PI / 2).Mover(PG.b + 1, 0, 0), mPedraEsc, M4.I);
                foreach (float z in new[] { PG.de + .6f, PG.ate - .6f }) B(PG.a - 1.4f, PG.b + 1.4f, -34, -15, z - 1, z + 1, mPedraEsc);
                B(PG.a - 1, PG.b + 1, -.6f, .02f, PG.de, PG.ate, M("floor"), 3);
            }
            // Ponte Caída: o tronco de uma árvore anciã que caiu sobre o abismo; o topo do tronco é o chão de quem passa
            {
                float z = (PC.a + PC.b) / 2, r = 2.35f, L = PC.ate - PC.de + 6; var c = Geo.Cilindro(r * .92f, r * 1.12f, L, 18, 6);
                for (int i = 0; i < c.p.Count; i++) { var q = c.p[i]; c.p[i] = new Vector3(q.x * (1 + .06f * Mathf.Sin(q.y * 1.7f)), q.y, q.z); }
                var cp = new Vector3((PC.de + PC.ate) / 2 - 1, -r + .04f, z);
                Est(c.NormaisDasFaces(), M("casca"), TRS(cp.x, cp.y, cp.z, 0, 0, PI / 2));
                // a raiz arrancada na cabeceira da mata e os galhos quebrados no lado do promontório
                Est(Geo.Esfera(4.2f, 12, 8), M("casca"), TRS(PC.de - 3.6f, -1.5f, z, 0, 0, 0, .45f, 1, 1.15f));
                for (int k = 0; k < 10; k++) { float a = k / 10f * PI * 2; Est(Geo.Cone(.35f, 3.4f, 5), M("casca"), TRS(PC.de - 4.2f, -1.5f + Mathf.Sin(a) * 2.8f, z + Mathf.Cos(a) * 3.2f, Mathf.Cos(a) * 1.2f, 0, PI / 2 + Mathf.Sin(a) * 1.1f)); }
                foreach (var q in new[] { new Vector3(2, 1.1f, -2.6f), new Vector3(4.5f, -1, 2.7f), new Vector3(7, .9f, -2.9f) }) Est(Geo.Cilindro(.18f, .5f, 5, 6), M("casca"), TRS(PC.ate - q.x, -.5f, z + q.z, q.y, 0, .5f));
                var musgo = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x334a2a), duplaFace = true }); musgo.name = "musgoPonte";
                Est(Geo.Cilindro(r * .94f, r * 1.13f, L * .7f, 18, 1, true, PI * .65f, PI * .7f), musgo, TRS(cp.x, cp.y + .03f, cp.z, 0, 0, PI / 2), false);
            }
            // Aqueduto: passarela de pedra sobre arcos altos e estreitos, que descem até o fundo
            {
                float z1 = AQ.a - 1, z2 = AQ.b + 1, x1 = AQ.de, x2 = AQ.ate; B(x1, x2, -1.4f, .02f, z1, z2, mPedra, 3);
                foreach (float x in new[] { x1 + .2f, (x1 + x2) / 2, x2 - .2f }) B(x - 1, x + 1, -34, -1.4f, z1, z2, mPedraEsc);
                foreach (var ab in new[] { new Vector2(x1 + .2f, (x1 + x2) / 2), new Vector2((x1 + x2) / 2, x2 - .2f) })
                {
                    float a = ab.x, b = ab.y; var sh = new List<Vector2> { new Vector2(a, -1.4f), new Vector2(b, -1.4f), new Vector2(b, -9) }; Arco(sh, (a + b) / 2, -9, (b - a) / 2 - 1); sh.Add(new Vector2(a, -9));
                    Est(Geo.Extrusao(sh, z2 - z1).Mover(0, 0, z1), mPedraEsc, M4.I);
                }
                // arco de entrada sobre as duas cabeceiras (os pilares são peças da simulação)
                foreach (float x in new float[] { 81, 99 }) Est(Geo.Toro((-73.1f + 80.9f) / 2, .5f, 6, 16, PI), M("pedraVelha"), TRS(x, 7.6f, -77, 0, PI / 2, 0));
            }
        }

        // ================= a catedral por fora =================
        void CatedralV6()
        {
            // ---- nave: paredes laterais baixas (naves laterais) e o clerestório alto sobre os pilares ----
            const float zN1 = -64, zN2 = -32;
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * 16;
                // parede lateral (com a porta do claustro a oeste e a brecha da torre a leste)
                var vaos = s < 0 ? new[] { new Vector2(-56, -52) } : new[] { new Vector2(-53, -49) }; var pts = new List<float> { zN1 }; foreach (var v in vaos) { pts.Add(v.x); pts.Add(v.y); } pts.Add(zN2);
                for (int i = 0; i < pts.Count; i += 2) B(x - .5f, x + .5f, 0, ALT.lateral, pts[i], pts[i + 1], mPedra, 4);
                foreach (var v in vaos) B(x - .5f, x + .5f, s < 0 ? 7 : 5.5f + vr() * 2, ALT.lateral, v.x, v.y, mPedra, 4);
                B(x - .75f * s, x + .25f * s, ALT.lateral, ALT.lateral + .5f, zN1, zN2, mPedraEsc); // cornija
                foreach (float y in new[] { 1.2f, 11.6f }) B(x + s * .5f, x + s * .75f, y, y + .35f, zN1, zN2, mPedraEsc); // cordões de pedra na face de fora
                // contrafortes com pináculos e arcobotantes até o clerestório; janelas entre eles
                foreach (float z in new float[] { -40, -46, -52, -58 })
                {
                    B(x + s * .5f, x + s * 2.3f, 0, ALT.lateral - 1, z - .9f, z + .9f, mkPedra); B(x + s * .5f, x + s * 1.6f, ALT.lateral - 1, ALT.lateral + 3, z - .7f, z + .7f, mkPedra);
                    Pinaculo(x + s * 1.05f, ALT.lateral + 3, z, 5, .55f); Arcobotante(x + s * 1.05f, ALT.lateral + 1.5f, s * 8.5f, ALT.clerestorio - 4.5f, z);
                }
                foreach (float z in new float[] { -37, -43, -49, -55, -61 }) { if (s > 0 && z == -49) continue; if (s < 0 && z == -55) continue; Lanceta('z', x, z, 3, 2.2f, 7.5f, s, true); }
                // clerestório: parede alta a 8,5 m do eixo, sobre os arcos dos pilares; janelas altas entre os pilares
                B(s * 8 - .5f, s * 8 + .5f, ALT.lateral - 1, ALT.clerestorio, zN1, -40, mkPedra, 4);
                foreach (float z in new float[] { -43, -49, -55, -61 }) Lanceta('z', s * 8, z, ALT.lateral + 2.5f, 2, 7.2f, s, true);
                // telhado de uma água das naves laterais, do pé do clerestório até a cornija
                Quad(new Vector3(s * 8.5f, ALT.lateral + 3.6f, zN1), new Vector3(s * 8.5f, ALT.lateral + 3.6f, zN2), new Vector3(s * 16.9f, ALT.lateral + .4f, zN2), new Vector3(s * 16.9f, ALT.lateral + .4f, zN1));
            }
            Telhado(-8.5f, 8.5f, -64, -32, ALT.clerestorio, ALT.cumeeira, false, mkTelha);
            // ---- fachada: empena, galeria de reis, rosácea, portal com arquivoltas, nichos ----
            {
                const float z = -32; B(-16.5f, -4, 0, ALT.clerestorio, z - .5f, z + .5f, mPedra); B(4, 16.5f, 0, ALT.clerestorio, z - .5f, z + .5f, mPedra); B(-4, 4, 9.6f, ALT.clerestorio, z - .5f, z + .5f, mPedra);
                Est(ExtrusaoTriangulo(-16.5f, 16.5f, 9, 1.2f), mkPedra, M4.T(0, ALT.clerestorio, z - .6f));
                for (int k = 0; k < 9; k++)
                {
                    float x = -12 + k * 3; B(x - .8f, x + .8f, 22, 25.4f, z + .5f, z + 1.1f, mPedraEsc, 2);
                    Est(Geo.Cilindro(.32f, .4f, 2.4f, 8), mPedraClara, x, 23.4f, z + .9f); Est(Geo.Esfera(.28f, 8, 6), mPedraClara, x, 24.85f, z + .9f);
                }
                Rosacea('x', z, 0, 16, 4.6f, 1);
                for (int k = 0; k < 5; k++) { float r = 4.6f + k * .75f; Est(Geo.Toro(r, .32f, 6, 28, PI), k % 2 == 1 ? mPedraEsc : mPedra, 0, 5.4f, z + .6f + k * .12f); }
                for (int k = 0; k < 5; k++) foreach (int sx in new[] { -1, 1 }) B(sx * (4.2f + k * .75f) - .3f, sx * (4.2f + k * .75f) + .3f, 0, 5.4f, z + .4f, z + 1.2f, k % 2 == 1 ? mPedraEsc : mPedra, 2);
                Est(Geo.Circulo(4, 24, 0, PI), mPedraEsc, 0, 5.6f, z + .52f, false);
                foreach (float x in new[] { -9.5f, 9.5f, -12.5f, 12.5f }) { B(x - 1, x + 1, 6.5f, 11, z + .5f, z + 1.1f, mPedraEsc, 2); Est(Geo.Cilindro(.35f, .45f, 3.2f, 8), mPedraClara, x, 8.2f, z + .85f); }
                foreach (float x in new[] { -16.5f, 16.5f }) Pinaculo(x, ALT.clerestorio, z, 9, .7f); Pinaculo(0, ALT.clerestorio + 9, z, 4, .4f);
            }
            // ---- nártex: teto mais baixo, três arcos para a nave ----
            B(-15.5f, 15.5f, ALT.clerestorio - 6, ALT.clerestorio - 5.6f, -39.5f, -32.5f, mTeto);
            foreach (var ab in new[] { new Vector2(-12, -8), new Vector2(-4, 4), new Vector2(8, 12) }) { float c = (ab.x + ab.y) / 2, w = ab.y - ab.x; Est(Geo.Toro(w / 2, .3f, 6, 18, PI), mPedraEsc, c, w > 5 ? 9 : 6.5f, -40); }
            foreach (var ab in new[] { new Vector2(-15.5f, -12), new Vector2(-8, -4), new Vector2(4, 8), new Vector2(12, 15.5f) }) B(ab.x, ab.y, 0, ALT.clerestorio - 5, -40.5f, -39.5f, mPedra);
            foreach (var q in new[] { new Vector3(-12, -8, 6.5f), new Vector3(-4, 4, 9), new Vector3(8, 12, 6.5f) }) B(q.x, q.y, q.z, ALT.clerestorio - 5, -40.5f, -39.5f, mPedra);
            // ---- torre oeste inteira: sineira acesa e agulha; torre leste partida ----
            {
                float x1 = -28, x2 = -16.5f, z1 = -44, z2 = -31.5f, cx = (x1 + x2) / 2, cz = (z1 + z2) / 2;
                B(x1, x2, 0, ALT.torreO, z1, z2, mkPedra, 4); foreach (float y in new float[] { 14, 28, 40 }) B(x1 - .5f, x2 + .5f, y, y + .7f, z1 - .5f, z2 + .5f, mkPedra);
                foreach (var c in new[] { new Vector2(x1, z1), new Vector2(x2, z1), new Vector2(x1, z2), new Vector2(x2, z2) }) { B(c.x - 1.1f, c.x + 1.1f, 0, ALT.torreO - 4, c.y - 1.1f, c.y + 1.1f, mkPedra); Pinaculo(c.x, ALT.torreO, c.y, 9, 1.05f); }
                var lados = new[] { new Vector3(0, 1, 0), new Vector3(0, -1, PI), new Vector3(-1, 0, -PI / 2), new Vector3(1, 0, PI / 2) };
                foreach (var L in lados) foreach (float o in new[] { -2.6f, 2.6f }) Est(Geo.Plano(2.1f, 7.5f), mFundo, TRS(cx + L.x * 5.8f + (L.y != 0 ? o : 0), ALT.torreO - 8, cz + L.y * 6.3f + (L.x != 0 ? o : 0), 0, L.z, 0), false);
                // arcadas cegas em dois andares em cada face: a torre deixa de ser um bloco liso
                foreach (var L in lados) foreach (var yh in new[] { new Vector2(16, 9), new Vector2(30, 7) }) foreach (float o in new[] { -3.3f, -1.1f, 1.1f, 3.3f })
                        {
                            float vx = cx + L.x * 5.78f + (L.y != 0 ? o : 0), vz = cz + L.y * 6.28f + (L.x != 0 ? o : 0);
                            Est(Geo.Plano(1.4f, yh.y), mFundo, TRS(vx, yh.x + yh.y / 2, vz, 0, L.z, 0), false);
                            Est(Geo.Toro(.7f, .14f, 4, 10, PI), mkPedra, TRS(vx + L.x * .04f, yh.x + yh.y, vz + L.y * .04f, 0, L.z, 0), false);
                        }
                Agulha(cx, cz, ALT.torreO, ALT.agulhaO, 5.2f, 8);
                Est(Geo.Cilindro(.9f, 1.5f, 2.2f, 14, 1, true), M("metalDupla"), cx, ALT.torreO - 7, cz, false);
                // a luz da sineira é o farol que orienta quem está na mata
                FONTES.Add(new Fonte { x = cx, z = cz, y = ALT.torreO - 8, cor = 0xffa050, i = 2.2f, alc = 26 });
                var farol = Mat.Brilho(Mat.Hex(0xffb060, .9f), Tex.Brilho(), Mat.Mistura.Aditiva, true, false); farol.name = "farolTorre";
                SpriteEst(cx, ALT.torreO - 8, cz + 6.6f, 9, 9, farol);
            }
            {
                float x1 = 16.5f, x2 = 28, z1 = -44, z2 = -31.5f;
                float[,] perfil = { { x1, ALT.torreL - 6 }, { 18, ALT.torreL - 2 }, { 19.4f, ALT.torreL - 4 }, { 21.2f, ALT.torreL + 2 }, { 22.8f, ALT.torreL - 1.5f }, { 24.6f, ALT.torreL + .5f }, { 26.2f, ALT.torreL - 7 }, { x2, ALT.torreL - 5 } };
                for (int i = 0; i < perfil.GetLength(0) - 1; i++)
                {
                    float a = perfil[i, 0], ha = perfil[i, 1], b = perfil[i + 1, 0], hb = perfil[i + 1, 1];
                    B(a, b, 0, Math.Min(ha, hb), z1, z2, mPedra, 4); B(a, b, Math.Min(ha, hb), Math.Max(ha, hb) - .4f, z1 + .6f + vr() * 1.5f, z2 - .6f - vr() * 1.5f, mPedra, 4);
                }
                B(x1 - .5f, x2 + .5f, 14, 14.7f, z1 - .5f, z2 + .5f, mPedraEsc);
                for (int k = 0; k < 6; k++) { float len = 6 + vr() * 4, py = ALT.torreL - 3 + vr() * 3, rx = vr() - .5f, ry = vr() * .4f, rz = .3f + vr() * .4f; Est(Geo.Caixa(.35f, .35f, len), M("wood"), TRS(x1 + 1.5f + k * 1.8f, py, (z1 + z2) / 2, rx, ry, rz)); }
            }
            // ---- transepto: paredes altas, empenas nas pontas com rosáceas; o braço leste perdeu o teto ----
            Func<float, float, float, List<PortalV>> vaosT = (fixo, a, b) => { var l = new List<PortalV>(); foreach (var p in MapaVisual.PORTAIS) if (p.eixo == "x" && Math.Abs(p.fixo - fixo) < .01f && p.a >= a - .1f && p.b <= b + .1f) l.Add(p); return l; };
            foreach (var ff in new[] { new Vector2(-64, 1), new Vector2(-78, -1) })
            {
                float fixo = ff.x; int face = (int)ff.y;
                foreach (var ab in fixo == -64 ? new[] { new Vector2(-44.5f, -15.5f), new Vector2(15.5f, 44.5f) } : new[] { new Vector2(-44.5f, -11.5f), new Vector2(11.5f, 44.5f) })
                {
                    float a = ab.x, b = ab.y; bool leste = a > 0; var vaos = vaosT(fixo, a, b); var pts = new List<float> { a }; foreach (var v in vaos) { pts.Add(v.a); pts.Add(v.b); } pts.Add(b);
                    for (int i = 0; i < pts.Count; i += 2) B(pts[i], pts[i + 1], 0, leste && fixo == -64 ? ALT.transepto - 9 : ALT.transepto, fixo - .5f, fixo + .5f, mPedra, 4);
                    foreach (var p in vaos) { if (!p.brecha) B(p.a, p.b, 6, ALT.transepto, fixo - .5f, fixo + .5f, mPedra, 4); else B(p.a, p.b, 13 + vr() * 4, ALT.transepto - 9, fixo - .5f, fixo + .5f, mPedra, 4); }
                    for (float x = a + 5; x < b - 3; x += 7)
                    {
                        bool noVao = false; foreach (var v in vaos) if (x > v.a - 3 && x < v.b + 3) noVao = true; if (noVao) continue;
                        B(x - .9f, x + .9f, 0, ALT.transepto - 2, fixo + face * .5f, fixo + face * 2.2f, mkPedra); Pinaculo(x, ALT.transepto - 2, fixo + face * 1.35f, 5, .6f);
                        if (!(leste && fixo == -64)) Lanceta('x', fixo, x + 3.5f, 4, 2.2f, 9, face, true);
                    }
                }
            }
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * 44; float[] pts = { -78, -73, -69, -64 };
                for (int i = 0; i < pts.Length; i += 2) B(x - .5f, x + .5f, 0, ALT.transepto, pts[i], pts[i + 1], mPedra, 4); B(x - .5f, x + .5f, 6, ALT.transepto, -73, -69, mPedra, 4);
                Est(ExtrusaoTriangulo(-7, 7, ALT.empena - ALT.transepto, 1), mkPedra, TRS(x - .5f, ALT.transepto, -71, 0, PI / 2, 0, 1, s > 0 ? .55f : 1, 1));
                Rosacea('z', x, -71, 17, 3.6f, s); foreach (float z in new float[] { -78, -64 }) Pinaculo(x, ALT.transepto, z, 8, .8f);
            }
            Telhado(-44.5f, -9, -78.5f, -63.5f, ALT.transepto, ALT.empena, true, mkTelha);
            // braço leste: só o madeiramento quebrado ficou; o luar entra pelo buraco
            for (int k = 0; k < 7; k++) { float x = 18 + k * 3.8f, len = 9 + vr() * 5, py = ALT.transepto - 1 - vr() * 6, pz = -71 + (vr() - .5f) * 3, rx = (vr() - .5f) * .9f, rz = (vr() - .5f) * .6f; Est(Geo.Caixa(.4f, .4f, len), M("wood"), TRS(x, py, pz, rx, 0, rz)); }
            // ---- torre do cruzeiro (lanterna) e agulha ----
            {
                B(-9.3f, 9.3f, ALT.clerestorio, ALT.lanterna, -63.7f, -62.7f, mkPedra, 4); B(-9.3f, 9.3f, ALT.clerestorio, ALT.lanterna, -79.3f, -78.3f, mkPedra, 4);
                B(-9.3f, -8.3f, ALT.clerestorio, ALT.lanterna, -78.3f, -63.7f, mkPedra, 4); B(8.3f, 9.3f, ALT.clerestorio, ALT.lanterna, -78.3f, -63.7f, mkPedra, 4);
                B(-9.8f, 9.8f, ALT.lanterna, ALT.lanterna + .8f, -79.8f, -62.2f, mkPedra);
                foreach (var c in new[] { new Vector2(-9.3f, -79.3f), new Vector2(9.3f, -79.3f), new Vector2(-9.3f, -62.7f), new Vector2(9.3f, -62.7f) }) Pinaculo(c.x, ALT.lanterna, c.y, 10, 1);
                foreach (float o in new[] { -5.5f, -1.8f, 1.8f, 5.5f })
                {
                    Lanceta('x', -63.2f, o, ALT.lanterna - 14, 1.8f, 9, 1, false); Lanceta('x', -78.8f, o, ALT.lanterna - 14, 1.8f, 9, -1, false);
                    Lanceta('z', 8.8f, -71 + o, ALT.lanterna - 14, 1.8f, 9, 1, false); Lanceta('z', -8.8f, -71 + o, ALT.lanterna - 14, 1.8f, 9, -1, false);
                }
                Agulha(0, -71, ALT.lanterna, ALT.agulhaC, 6.4f, 8);
            }
            // ---- coro e abside: paredes, janelas altas e telhado em leque ----
            foreach (int s in new[] { -1, 1 }) { float x = s * 12; B(x - .5f, x + .5f, 0, ALT.abside, -85, -83, mPedra, 4); B(x - .5f, x + .5f, 0, ALT.abside, -80, -78, mPedra, 4); B(x - .5f, x + .5f, 5.8f, ALT.abside, -83, -80, mPedra, 4); }
            {
                const int n = 15;
                for (int k = 0; k < n; k++)
                {
                    float a = PI * (1 + (k + .5f) / n), a0 = PI * (1 + (float)k / n), a1 = PI * (1 + (k + 1f) / n), r = 11, x0 = Mathf.Cos(a0) * r, z0 = -85 + Mathf.Sin(a0) * r, x1 = Mathf.Cos(a1) * r, z1 = -85 + Mathf.Sin(a1) * r;
                    float L = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0)) + .6f;
                    Est(Geo.Caixa(L, ALT.abside, 1.4f, 4), mPedra, TRS((x0 + x1) / 2, ALT.abside / 2, (z0 + z1) / 2, 0, -Mathf.Atan2(z1 - z0, x1 - x0), 0));
                    if (k % 2 == 1)
                    {
                        float cx = Mathf.Cos(a) * (r + .75f), cz = -85 + Mathf.Sin(a) * (r + .75f); gVidrosFora.Juntar(Geo.Plano(1.7f, 10).Mover(0, 13, 0).GirarY(-a - PI / 2).Mover(cx, 0, cz));
                        gVidros.Juntar(Geo.Plano(1.7f, 10).Mover(0, 13, 0).GirarY(-a + PI / 2).Mover(Mathf.Cos(a) * (r - .75f), 0, -85 + Mathf.Sin(a) * (r - .75f)));
                    }
                    if (k % 2 == 0) { float cx = Mathf.Cos(a) * (r + 1.6f), cz = -85 + Mathf.Sin(a) * (r + 1.6f); B(cx - .8f, cx + .8f, 0, ALT.abside - 3, cz - .8f, cz + .8f, mkPedra); Pinaculo(cx, ALT.abside - 3, cz, 6, .6f); }
                }
                Est(Geo.Cilindro(0, 11.8f, 9, 16, 1, true, PI / 2, PI), mkTelha, 0, ALT.abside + 4.5f, -85);
            }
            Telhado(-12.5f, 12.5f, -85, -78, ALT.abside, ALT.abside + 8.5f, false, mkTelha);
            // ---- Sala do Capítulo, Sacristia: telhados de quatro águas; Claustro: galeria em meia-água ----
            Action<float, float, float, float, float, float> quatroAguas = (x1, x2, z1, z2, y, h) =>
            {
                float d = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1));
                Est(Geo.Cone(d / 2 * 1.02f, h, 4, 1, true), mkTelha, TRS((x1 + x2) / 2, y + h / 2, (z1 + z2) / 2, 0, PI / 4, 0, (x2 - x1) / d * 1.41f, 1, (z2 - z1) / d * 1.41f));
            };
            quatroAguas(-71, -51, -65, -45, 14, 6); quatroAguas(-31, -11.5f, -91, -77.5f, 14, 5);
            {
                var CL = MapaVisual.CLAUSTRO; float yA = 8.4f, yB = 6, c1 = CL.yMin, c2 = CL.yMax, cx1 = CL.xMin, cx2 = CL.xMax;
                Quad(new Vector3(-44, yA, -64), new Vector3(-16, yA, -64), new Vector3(-16, yB, c1 + .5f), new Vector3(-44, yB, c1 + .5f), mTelha);
                Quad(new Vector3(-44, yA, -44), new Vector3(-16, yA, -44), new Vector3(-16, yB, c2 - .5f), new Vector3(-44, yB, c2 - .5f), mTelha);
                Quad(new Vector3(-44, yA, -64), new Vector3(-44, yA, -44), new Vector3(cx1 + .5f, yB, c2), new Vector3(cx1 + .5f, yB, c1), mTelha);
                Quad(new Vector3(-16, yA, -64), new Vector3(-16, yA, -44), new Vector3(cx2 - .5f, yB, c2), new Vector3(cx2 - .5f, yB, c1), mTelha);
                // arcos entre as colunas da galeria
                var cols = new List<Vector2>(); foreach (var w in MapaVisual.WALLS) if (w.kind == "coluna_claustro") cols.Add(new Vector2((w.x1 + w.x2) / 2, (w.z1 + w.z2) / 2));
                foreach (var c in cols) { Est(Geo.Caixa(1.1f, .45f, 1.1f), mPedraEsc, c.x, 5.4f, c.y, false); Est(Geo.Cilindro(.3f, .36f, 5.2f, 10), mPedraClara, c.x, 2.6f, c.y); }
                for (int i = 0; i < cols.Count; i++) for (int j = i + 1; j < cols.Count; j++)
                    {
                        Vector2 a = cols[i], b = cols[j]; float d = Vector2.Distance(a, b); if (d > 4.7f || d < 3) continue;
                        Est(Geo.Toro(d / 2, .18f, 5, 14, PI), mPedraEsc, TRS((a.x + b.x) / 2, 5.6f, (a.y + b.y) / 2, 0, -Mathf.Atan2(b.y - a.y, b.x - a.x), 0), false);
                    }
            }
            // vitrais e feixes: uma malha só de cada tipo
            Est(gVidros, mVidro, M4.I, false); Est(gVidrosFora, mVidroFora, M4.I, false); Est(gRosas, mRosa, M4.I, false); if (Qualidade.nivel >= 1) Est(gFeixes, mFeixe, M4.I, false);
        }

        // ================= a catedral por dentro: abóbadas, arcadas, pilares, jubé, colunata da abside =================
        void InteriorV6()
        {
            // tetos: a nave sob a abóbada alta, as naves laterais mais baixas, o transepto, o coro; o cruzeiro abre para a lanterna
            Action<float, float, float, float, float> teto = (x1, x2, z1, z2, y) => Est(Geo.Plano(x2 - x1, z2 - z1), mTeto, TRS((x1 + x2) / 2, y, (z1 + z2) / 2, PI / 2, 0, 0), false);
            teto(-8, 8, -62.7f, -40, ALT.clerestorio - 1.2f); teto(-16, -8, -64, -40, ALT.lateral - 1); teto(8, 16, -64, -40, ALT.lateral - 1);
            teto(-44, -9.3f, -78, -64, ALT.transepto - 1.2f); teto(-12, 12, -85, -79.3f, ALT.abside - 1.2f); teto(-9.3f, 9.3f, -79.3f, -62.7f, ALT.lanterna - 1);
            teto(-30, -12.5f, -90, -78.5f, 12); teto(-70, -52, -64, -46, 12);
            // nervuras das abóbadas (cada tramo da nave, das naves laterais e do transepto)
            Action<float, float, float, float, bool, float> nerv = (w, x, y, z, girar, alto) => Est(Nervura(w, alto), mPedraEsc, TRS(x, y, z, 0, girar ? PI / 2 : 0, 0), false);
            foreach (float z in new float[] { -40, -46, -52, -58, -64 }) nerv(16, 0, ALT.clerestorio - 1.1f, z, false, 5);
            foreach (float z in new float[] { -43, -49, -55, -61 }) { var g = Nervura(Mathf.Sqrt(16 * 16 + 6 * 6), 5); Est(g, mPedraEsc, TRS(0, ALT.clerestorio - 1.1f, z, 0, Mathf.Atan2(6, 16), 0), false); Est(g, mPedraEsc, TRS(0, ALT.clerestorio - 1.1f, z, 0, -Mathf.Atan2(6, 16), 0), false); }
            foreach (int s in new[] { -1, 1 }) foreach (float z in new float[] { -40, -46, -52, -58 }) nerv(8, s * 12, ALT.lateral - 1, z, false, 3);
            for (float x = -41; x < -10; x += 7) nerv(14, x, ALT.transepto - 1.1f, -71, true, 4.6f);
            // arcada da nave: arcos apontados entre os pilares, trifório e o pé do clerestório
            foreach (int s in new[] { -1, 1 })
            {
                float[] zs = { -40, -46, -52, -58, -64 };
                for (int i = 0; i < zs.Length - 1; i++)
                {
                    float c = (zs[i] + zs[i + 1]) / 2; Est(Nervura(6, 3.6f, .45f), mPedra, TRS(s * 8, ALT.lateral - 3.2f, c, 0, PI / 2, 0), false);
                    foreach (float o in new[] { -1.6f, 0, 1.6f }) Est(Nervura(1.4f, .9f, .12f), mPedraEsc, TRS(s * 7.4f, ALT.lateral + 1.4f, c + o, 0, PI / 2, 0), false);
                }
            }
            // pilares da nave: feixes de colunas até o arranque da abóbada
            var fuste = Geo.Cilindro(.62f, .72f, ALT.lateral - 3, 14); var fusteFino = Geo.Cilindro(.16f, .18f, ALT.clerestorio - 2, 6); var basG = Geo.Cilindro(1.05f, 1.15f, .7f, 14); var capitel = Geo.Cilindro(1.05f, .72f, .8f, 14);
            foreach (var p in MapaVisual.PILLARS)
            {
                Est(fuste, mPedra, p.x, (ALT.lateral - 3) / 2, p.y);
                foreach (var o in new[] { new Vector2(.6f, 0), new Vector2(-.6f, 0), new Vector2(0, .6f), new Vector2(0, -.6f) }) Est(fusteFino, mPedraClara, p.x + o.x, (ALT.clerestorio - 2) / 2, p.y + o.y, false);
                Est(basG, M("marble"), p.x, .35f, p.y, false); Est(capitel, mPedraEsc, p.x, ALT.lateral - 2.6f, p.y, false);
            }
            // pilares do cruzeiro: as peças da simulação ganham colunas em feixe até a lanterna
            var ffc = Geo.Cilindro(.22f, .24f, ALT.clerestorio, 8);
            foreach (var w in MapaVisual.WALLS)
            {
                if (w.kind != "pilar_cruzeiro") continue; float x = (w.x1 + w.x2) / 2, z = (w.z1 + w.z2) / 2; B(w.x1, w.x2, 0, ALT.clerestorio, w.z1, w.z2, mPedra, 4);
                foreach (var o in new[] { new Vector2(1.3f, 0), new Vector2(-1.3f, 0), new Vector2(0, 1.3f), new Vector2(0, -1.3f), new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) }) Est(ffc, mPedraClara, x + o.x, ALT.clerestorio / 2, z + o.y, false);
            }
            foreach (var q in new[] { new Vector4(-8, -64, 8, -64), new Vector4(-8, -78, 8, -78), new Vector4(-8, -64, -8, -78), new Vector4(8, -64, 8, -78) })
            {
                float L = Mathf.Sqrt((q.z - q.x) * (q.z - q.x) + (q.w - q.y) * (q.w - q.y));
                Est(Nervura(L, 6, .55f), mPedra, TRS((q.x + q.z) / 2, ALT.clerestorio - .5f, (q.y + q.w) / 2, 0, -Mathf.Atan2(q.w - q.y, q.z - q.x), 0), false);
            }
            // jubé: anteparo de pedra com arcos cegos, passagem dos lados e o grande crucifixo no alto
            foreach (var w in MapaVisual.WALLS)
            {
                if (w.kind != "jube") continue; B(w.x1, w.x2, 0, w.h, w.z1, w.z2, mPedraEsc, 3); B(w.x1 - .2f, w.x2 + .2f, w.h, w.h + .6f, w.z1 - .3f, w.z2 + .3f, mPedra);
                for (float x = w.x1 + .6f; x < w.x2 - .4f; x += 1.2f) Est(Nervura(1, .7f, .08f), mPedra, x + .5f, w.h - 1.4f, w.z2 + .06f, false);
            }
            {
                var G = M4.T(0, 9.6f, -78); var est = M("estatua");
                Est(Geo.Caixa(.5f, 7, .5f), M("wood"), G * M4.T(0, 3.5f, 0), false); Est(Geo.Caixa(4.2f, .45f, .45f), M("wood"), G * M4.T(0, 5.2f, 0), false);
                Est(Geo.Cilindro(.22f, .16f, 2.2f, 8), est, G * M4.T(0, 4.1f, .32f), false);
                foreach (int s in new[] { -1, 1 }) Est(Geo.Cilindro(.07f, .07f, 1.8f, 6), est, G * TRS(s * .95f, 5.05f, .32f, 0, 0, s * 1.35f), false);
                Est(Geo.Esfera(.2f, 8, 6), est, G * M4.T(0, 5.35f, .36f), false);
                foreach (int s in new[] { -1, 1 }) Est(Geo.Cilindro(.02f, .02f, 9, 4), M("metal"), G * M4.T(s * 1.8f, 10.5f, 0), false);
            }
            // colunata em meia-lua atrás do altar-mor
            foreach (var w in MapaVisual.WALLS)
            {
                if (w.kind != "coluna_abside") continue; float x = (w.x1 + w.x2) / 2, z = (w.z1 + w.z2) / 2;
                Est(Geo.Cilindro(.5f, .58f, w.h, 14), mPedraClara, x, w.h / 2, z); Est(Geo.Cilindro(.85f, .55f, .7f, 14), mPedraEsc, x, w.h + .35f, z, false); Est(Geo.Cilindro(.8f, .9f, .5f, 14), M("marble"), x, .25f, z, false);
            }
            // ombreiras e lintéis das portas (as brechas ficam com a borda quebrada)
            foreach (var P2 in MapaVisual.PORTAIS)
            {
                if (P2.nome == "Grande Porta" || P2.nome.StartsWith("Arco") || P2.nome.StartsWith("Jubé")) continue; bool ax = P2.eixo == "x"; float f = P2.fixo;
                Action<float, float, float, float, float, float, Material> box = (a, b, y1, y2, e1, e2, m) => { if (ax) B(a, b, y1, y2, f + e1, f + e2, m, 2); else B(f + e1, f + e2, y1, y2, a, b, m, 2); };
                if (P2.brecha) { for (int k = 0; k < 4; k++) { float a = P2.a + (P2.b - P2.a) * k / 4, b = a + (P2.b - P2.a) / 4, y1 = 4.5f + vr() * 3, y2 = 6.5f + vr() * 2; box(a, b, y1, y2, -.5f, .5f, mPedra); } continue; }
                box(P2.a - .5f, P2.b + .5f, 5.3f, 5.75f, -.8f, .8f, mPedraEsc); foreach (float e in new[] { P2.a - .45f, P2.b - .05f }) box(e, e + .5f, 0, 5.6f, -.75f, .75f, mPedraEsc);
                float c = (P2.a + P2.b) / 2; Est(Geo.Toro((P2.b - P2.a) / 2, .26f, 6, 16, PI), mPedraEsc, ax ? TRS(c, 5.6f, f) : TRS(f, 5.6f, c, 0, PI / 2, 0), false);
            }
        }

        // ================= promontório: adro, mosteiro, cemitério =================
        void PromontorioV6()
        {
            // beira do penhasco ao norte (atrás da abside): rochas soltas sobre o limite
            for (float x = -80; x <= 80; x += 6) { if (Math.Abs(x) < 10) continue; float r = 1.4f + vr() * 1.2f, px = x + vr() * 3, pz = -100.2f + vr() * .6f; Est(Geo.Dodecaedro(r), mRocha, TRS(px, .3f, pz, 0, 0, 0, 1, .6f, 1)); }
            // tanques secos do adro
            var agua = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x080a0e), rugosidade = .15f, metal = .4f }); agua.name = "aguaTanque";
            foreach (var w in MapaVisual.WALLS) { if (w.kind != "tanque") continue; B(w.x1, w.x2, 0, w.h, w.z1, w.z2, mPedraClara, 2); Est(Geo.Plano(w.x2 - w.x1 - .6f, w.z2 - w.z1 - .6f), agua, TRS((w.x1 + w.x2) / 2, w.h - .2f, (w.z1 + w.z2) / 2, -PI / 2, 0, 0), false); }
        }

        void ConstruirV6() { Pisos(); Desfiladeiro(); Pontes(); CatedralV6(); InteriorV6(); PromontorioV6(); }

        // ================= peças novas (Peca chama PecaV6 antes das antigas) =================
        bool PecaV6(PecaV w)
        {
            float cx = (w.x1 + w.x2) / 2, cz = (w.z1 + w.z2) / 2; bool aoX = (w.x2 - w.x1) >= (w.z2 - w.z1); float L = Math.Max(w.x2 - w.x1, w.z2 - w.z1), T = Math.Min(w.x2 - w.x1, w.z2 - w.z1);
            switch (w.kind)
            {
                case "catedral": case "fachada": case "torre_oeste": case "torre_leste": case "abside": case "jube": case "pilar_cruzeiro": case "coluna_claustro": case "coluna_abside": case "abismo": case "tanque": return true;
                case "claustro": B(w.x1, w.x2, 0, 9.5f, w.z1, w.z2, mPedra, 4); B(w.x1 - .15f, w.x2 + .15f, 9.5f, 10, w.z1 - .15f, w.z2 + .15f, mPedraEsc, 2); return true;
                case "ruina": Ruina(w, aoX, L, T, cx, cz); return true;
                case "limite":
                    {
                        bool norte = w.z2 <= -MapaVisual.HZ + .01f; var D0 = MapaVisual.DESFILADEIRO;
                        Action<float, float> muro = (x1, x2) => { B(x1, x2, 0, w.h, w.z1, w.z2, M("wall")); B(x1 - .15f, x2 + .15f, w.h, w.h + .3f, w.z1 - .15f, w.z2 + .15f, M("wallDark")); };
                        if (norte) { muro(w.x1, D0.x1); muro(D0.x2, w.x2); } else muro(w.x1, w.x2); return true;
                    }
                case "pedra": B(w.x1, w.x2, 0, w.h, w.z1, w.z2, mPedra, 4); if (w.h > 2.2f) B(w.x1 - .1f, w.x2 + .1f, w.h, w.h + .35f, w.z1 - .1f, w.z2 + .1f, mPedraEsc, 2); return true;
                case "parapeito":
                    {
                        B(w.x1, w.x2, 0, .25f, w.z1, w.z2, mPedraClara, 2); B(w.x1 - .08f, w.x2 + .08f, w.h - .18f, w.h, w.z1 - .08f, w.z2 + .08f, mPedraClara, 2);
                        var bal = Geo.Cilindro(.11f, .16f, w.h - .43f, 8); for (float t = .35f; t < L - .2f; t += .55f) Est(bal, mPedraClara, aoX ? w.x1 + t : cx, .25f + (w.h - .43f) / 2, aoX ? cz : w.z1 + t, false); return true;
                    }
                case "mureta": B(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("pedraVelha"), 2); B(w.x1 - .06f, w.x2 + .06f, w.h, w.h + .12f, w.z1 - .06f, w.z2 + .06f, mPedraEsc, 2); return true;
                case "estatua_ponte": B(w.x1, w.x2, 0, 1.35f, w.z1, w.z2, mPedraClara, 2); B(w.x1 - .1f, w.x2 + .1f, 1.3f, 1.45f, w.z1 - .1f, w.z2 + .1f, mPedraEsc, 2); Encapuzado(cx, 1.45f, cz, w.rot, 1.05f); return true;
                case "penhasco":
                    {
                        int n = Math.Max(1, (int)JS.Round(L / 2.2f));
                        for (int k = 0; k < n; k++)
                        {
                            float t = (k + .5f) / n * L, h = w.h * (.8f + vr() * .45f), px = aoX ? w.x1 + t : cx + (vr() - .5f) * .4f, pz = aoX ? cz + (vr() - .5f) * .4f : w.z1 + t, rx = vr() * .4f, ry = vr() * 3, rz = vr() * .4f;
                            Est(Geo.Dodecaedro(1, 1), mRocha, TRS(px, h * .45f, pz, rx, ry, rz, aoX ? 1.6f : 1.3f, h / 1.6f, aoX ? 1.3f : 1.6f));
                        }
                        return true;
                    }
                case "bloco": B(w.x1, w.x2, 0, w.h, w.z1, w.z2, mPedraClara, 1.5f); return true;
                case "guindaste":
                    {
                        var mad = M("wood"); Est(Geo.Caixa(.4f, w.h, .4f), mad, cx, w.h / 2, cz); Est(Geo.Caixa(.3f, .3f, 7.5f), mad, TRS(cx, w.h - 1.4f, cz + 2.6f, -.35f, 0, 0));
                        foreach (int s in new[] { -1, 1 }) Est(Geo.Caixa(.25f, w.h * .8f, .25f), mad, TRS(cx + s * 1.3f, w.h * .38f, cz - .8f, 0, 0, s * .25f));
                        if (!MAT.ContainsKey("corda")) { MAT["corda"] = Mat.Padrao(0x5a4a32); MAT["corda"].name = "corda"; }
                        Est(Geo.Cilindro(.03f, .03f, 5, 4), MAT["corda"], cx, w.h - 3.5f, cz + 5.7f, false); Est(Geo.Caixa(1.1f, .8f, 1.1f), mPedraClara, cx, w.h - 6.2f, cz + 5.7f); return true;
                    }
                case "passarela":
                    {
                        for (float t = .2f; t < L; t += .45f) { float py = .3f + (vr() - .5f) * .06f, ry = (vr() - .5f) * .08f; Est(Geo.Caixa(aoX ? .34f : 1.6f, .08f, aoX ? 1.6f : .34f), M("tabuas"), TRS(aoX ? w.x1 + t : cx, py, aoX ? cz : w.z1 + t, 0, ry, 0), false); }
                        var est = Geo.Cilindro(.08f, .08f, 1, 5); for (float t = 1; t < L; t += 2.5f) foreach (int s in new[] { -1, 1 }) Est(est, M("wood"), aoX ? w.x1 + t : cx + s * .8f, .2f, aoX ? cz + s * .8f : w.z1 + t, false);
                        return true;
                    }
                // forno de carvão em colmeia: parede reta até 1,6 m e cúpula por cima, para o ombro do forno ficar acima do olho
                case "forno":
                    {
                        float r = (w.x2 - w.x1) / 2; if (!MAT.ContainsKey("forno")) { MAT["forno"] = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x2a241e), mapa = Tex.wall.mapa }); MAT["forno"].name = "forno"; MAT["bocaForno"] = Mat.Brilho(Mat.Hex(0xff6a2a), null, Mat.Mistura.Alfa, false, true, 0, true, 2000); MAT["bocaForno"].name = "bocaForno"; }
                        Est(Geo.Cilindro(r * 1.06f, r * 1.12f, 1.6f, 16), MAT["forno"], cx, .8f, cz);
                        Est(Geo.Esfera(r * 1.06f, 16, 8, 0, PI * 2, 0, PI / 2), MAT["forno"], TRS(cx, 1.6f, cz, 0, 0, 0, 1, (w.h + .5f - 1.6f) / (r * 1.06f), 1));
                        Est(Geo.Circulo(.45f, 10), MAT["bocaForno"], cx, .4f, cz + r * 1.02f, false); FONTES.Add(new Fonte { x = cx, z = cz + r + .5f, y = .6f, cor = 0xff5a20, i = 1.1f, alc = 7 }); return true;
                    }
                case "cruz":
                    {
                        bool madeira = w.h > 5.5f || Math.Abs(cz + 27) > 1; var mat = madeira ? M("wood") : mPedraClara;
                        Est(Geo.Caixa(.34f, w.h, .34f), mat, cx, w.h / 2, cz); Est(Geo.Caixa(w.h * .42f, .3f, .3f), mat, cx, w.h * .74f, cz);
                        Est(Geo.Dodecaedro(.9f), mRocha, TRS(cx, .15f, cz, 0, 0, 0, 1, .45f, 1)); return true;
                    }
                case "banco": B(w.x1, w.x2, 0, w.h, w.z1, w.z2, mPedraClara, 2); return true;
                case "arca": B(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("wood"), 1); B(w.x1 - .05f, w.x2 + .05f, w.h - .12f, w.h, w.z1 - .05f, w.z2 + .05f, M("metal"), 1); return true;
                case "torre_portaria":
                    {
                        B(w.x1, w.x2, 0, w.h, w.z1, w.z2, M("pedraVelha"), 3);
                        for (int k = 0; k < 4; k++) { float a = w.x1 + .4f + k * (w.x2 - w.x1 - .8f) / 3; B(a - .35f, a + .35f, w.h, w.h + .9f, w.z1, w.z1 + .7f, M("pedraVelha"), 2); B(a - .35f, a + .35f, w.h, w.h + .9f, w.z2 - .7f, w.z2, M("pedraVelha"), 2); }
                        foreach (float y in new float[] { 4, 8 }) Est(Geo.Plano(.25f, 1.3f), mFundo, cx, y, w.z2 + .02f, false); return true;
                    }
            }
            return false;
        }
        // muro arruinado da mata: fiadas de blocos desencontrados sobre um miolo de pedra. O topo quebrado fica sempre ACIMA
        // da altura da caixa (a colisão vai até w.h e uma parede alta para tiro e visão): o que se vê nunca é mais baixo do
        // que o que bloqueia. Blocos só faltam acima de w.h; abaixo, o miolo fecha qualquer fresta.
        void Ruina(PecaV w, bool aoX, float L, float T, float cx, float cz)
        {
            var blocos = new Geo(); var miolo = new Geo();
            // a caixa segue o eixo do muro (u ao longo, t na espessura): num muro ao longo de z, u vai em z
            Action<float, float, float, float, float, float, Geo> caixa = (u1, u2, y1, y2, t1, t2, lista) =>
            {
                var g = Geo.Caixa(aoX ? u2 - u1 : t2 - t1, y2 - y1, aoX ? t2 - t1 : u2 - u1);
                for (int i = 0; i < g.uv.Count; i++) { float a = vr(), b = vr(); g.uv[i] = new Vector2(g.uv[i].x * (u2 - u1) / 2.5f + a, g.uv[i].y * (y2 - y1) / 2.5f + b); } // cada bloco pega outro pedaço da textura
                float um = (u1 + u2) / 2 - L / 2, tm = (t1 + t2) / 2; lista.Juntar(g.Mover(aoX ? um : tm, (y1 + y2) / 2, aoX ? tm : um));
            };
            // topo: uma linha quebrada entre h e 1,35h, mais baixa nas pontas (as pontas caíram primeiro)
            Func<float, float> topo = u => { float borda = Math.Min(u, L - u) / Math.Max(1, L * .25f); return w.h * (1 + .35f * Math.Min(1, borda) * (.35f + .65f * Math.Abs(Mathf.Sin(u * 1.7f + cx * .3f) * Mathf.Cos(u * .9f + cz * .2f)))); };
            caixa(.04f, L - .04f, 0, w.h, -T / 2 + .09f, T / 2 - .09f, miolo);
            int fil = 0;
            for (float y = 0; y < w.h * 1.36f; fil++)
            {
                float hr = .42f + vr() * .16f, u = fil % 2 == 1 ? -.4f * vr() : 0;
                while (u < L)
                {
                    float lb = .8f + vr() * .7f, u1 = Math.Max(0, u), u2 = Math.Min(L, u + lb), um = (u1 + u2) / 2; u += lb; if (u2 - u1 < .25f) continue;
                    float tp = topo(um); if (y >= tp) continue; float y2 = Math.Min(y + hr, tp + .2f); if (y > w.h && vr() < .25f) continue;
                    float ins = (vr() - .5f) * .08f, ins2 = (vr() - .5f) * .08f; caixa(u1 + .015f, u2 - .015f, y + .012f, y2 - .012f, -T / 2 + ins, T / 2 + ins2, blocos);
                }
                y += hr;
            }
            Est(miolo, mPedraEsc, M4.T(cx, 0, cz)); Est(blocos, mPedra, M4.T(cx, 0, cz));
            // entulho na base, dos dois lados (pedras soltas não entram na colisão: ficam rentes ao muro e baixas)
            for (int k = 0; k < Math.Ceiling(L / 2.5f); k++)
            {
                float r = .25f + vr() * .3f, t = vr() * L - L / 2, lado = vr() < .5f ? -1 : 1, d = T / 2 + .25f + vr() * .35f, rx = vr() * 3, ry = vr() * 3;
                Est(Geo.Dodecaedro(r), mRocha, TRS(aoX ? cx + t : cx + lado * d, .12f, aoX ? cz + lado * d : cz + t, rx, ry, 0, 1, .6f, 1));
            }
        }
        // fiel encapuzado: manto até o chão, capuz fundo sem rosto, mãos juntas
        public void Encapuzado(float x, float y, float z, float rot, float esc = 1)
        {
            var G = TRS(x, y, z, 0, rot, 0, esc, esc, esc); var MM = M("estatua");
            Est(Geo.Torno(new float[,] { { .5f, 0 }, { .46f, .25f }, { .38f, .8f }, { .3f, 1.25f }, { .26f, 1.48f }, { .2f, 1.62f }, { 0, 1.66f } }, 9), MM, G);
            Est(Geo.Esfera(.26f, 10, 8, 0, PI * 2, 0, PI * .62f), MM, G * TRS(0, 1.72f, 0, -.35f, 0, 0), false);
            Est(Geo.Circulo(.15f, 10), mFundo, G * TRS(0, 1.66f, -.2f, 0, PI, 0), false);
            Est(Geo.Esfera(.12f, 8, 6), MM, G * M4.T(0, 1.05f, -.33f), false);
            foreach (int s in new[] { -1, 1 }) Est(Geo.Cilindro(.08f, .11f, .6f, 6), MM, G * TRS(s * .2f, 1.2f, -.18f, -.9f, 0, s * -.5f), false);
        }
    }
}
