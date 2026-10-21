// Skins das armas na primeira pessoa (client/skins.js, a coleção "Relíquias da Vigília"). Só aparência: cada material
// das armas tem um papel (caixa, aco, latao, madeira, prata, escuro, cruz, osso, ferro, brasa) e a skin diz, por papel,
// cor, aspereza, metal e um detalhe (gravura, têmpera, quadriculado, rachaduras, crosta) projetado em três planos no
// espaço da peça pelo shader RitualReversal/PrimeiraPessoa (R escava sulcos, G incrusta outro material, B mancha).
// Acessórios (selo de cera, granada, arame, amuletos, cracas) saem prontos da exportação (tools/exportar-primeira-pessoa.js)
// e os pendentes balançam com uma mola em direção à gravidade. Ganchos de inspeção: o clipe marca ['skin','revela'] e
// a skin decide (a luz que corre pela gravura, a cruz que acende, a água que pinga do cetro).
// A escolha fica em PlayerPrefs "rr-skins" (o mesmo JSON do localStorage do navegador).
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public class SkinsFP
    {
        public class Papel { public int? cor; public float? rug, met, sulco, incMet, incRug; public string det; public float esc = 10, inc, mancha, bump; public int manchaCor, incCor; }
        public class Skin
        {
            public string id, nome, oficio, texto; public readonly Dictionary<string, Papel> papeis = new Dictionary<string, Papel>(); public readonly List<string> acessorios = new List<string>();
            public int revelaCor = 0xffffff; public float revelaForca = .55f; public string som; public float[] cruz; public bool gotas, temRevela;
        }
        class Base { public string papel; public Color cor; public float rug, met, claro = 1; }
        class Varre { public List<Material> mats; public float t, dur; public Color cor; public float forca; }
        class Pend { public Transform pivo; public float ax, az, vx, vz; }

        public readonly Dictionary<string, List<Skin>> catalogo = new Dictionary<string, List<Skin>>();
        public readonly Dictionary<string, string> ESC = new Dictionary<string, string> { { "carabina", "servico" }, { "revolver", "prata" }, { "cetro", "vertebras" } };
        public string colecao, colecaoTexto; public readonly Dictionary<string, string[]> oficios = new Dictionary<string, string[]>();   // id -> nome, cor
        readonly Dictionary<Material, Base> bases = new Dictionary<Material, Base>();
        readonly Dictionary<string, List<object>> acessorios = new Dictionary<string, List<object>>();
        readonly Dictionary<string, Texture2D> texs = new Dictionary<string, Texture2D>();
        readonly List<Varre> VARRE = new List<Varre>(); readonly List<Pend> PEND = new List<Pend>();
        static readonly Dictionary<Material, Color> emisBase = new Dictionary<Material, Color>();
        readonly ArmaFP fp;

        static int? Int(Dictionary<string, object> o, string k) { object v; return o.TryGetValue(k, out v) && v != null ? (int?)(int)Json.D(v) : null; }
        static float? Num(Dictionary<string, object> o, string k) { object v; return o.TryGetValue(k, out v) && v != null ? (float?)(float)Json.D(v) : null; }
        public SkinsFP(Dictionary<string, object> D, ArmaFP fp)
        {
            this.fp = fp;
            foreach (var kv in Json.O(D["catalogo"]))
            {
                var L = new List<Skin>();
                foreach (var o in Json.L(kv.Value))
                {
                    var s = Json.O(o); object v; var sk = new Skin { id = Json.S(s["id"]), nome = Json.S(s["nome"]), oficio = Json.S(s["oficio"]), texto = Json.S(s["texto"]) };
                    foreach (var pp in Json.O(s["papeis"]))
                    {
                        var p = Json.O(pp.Value);
                        sk.papeis[pp.Key] = new Papel
                        {
                            cor = Int(p, "cor"), rug = Num(p, "rug"), met = Num(p, "met"), det = p.TryGetValue("det", out v) ? (string)v : null, esc = Num(p, "esc") ?? 10, sulco = Num(p, "sulco"),
                            inc = Num(p, "inc") ?? 0, mancha = Num(p, "mancha") ?? 0, bump = Num(p, "bump") ?? 0, manchaCor = Int(p, "manchaCor") ?? 0, incCor = Int(p, "incCor") ?? 0, incMet = Num(p, "incMet"), incRug = Num(p, "incRug")
                        };
                    }
                    if (s.TryGetValue("acessorios", out v)) foreach (var a in Json.L(v)) sk.acessorios.Add((string)a);
                    if (s.TryGetValue("insp", out v))
                    {
                        var I = Json.O(v); object w;
                        if (I.TryGetValue("revela", out w)) { var r = Json.O(w); sk.temRevela = true; sk.revelaCor = Int(r, "cor") ?? 0xffffff; sk.revelaForca = Num(r, "forca") ?? .55f; }
                        if (I.TryGetValue("som", out w)) sk.som = (string)w;
                        if (I.TryGetValue("cruz", out w)) { var l = Json.L(w); sk.cruz = new[] { (float)Json.D(l[0]), (float)Json.D(l[1]), (float)Json.D(l[2]) }; }
                        if (I.TryGetValue("gotas", out w)) sk.gotas = (bool)w;
                    }
                    L.Add(sk);
                }
                catalogo[kv.Key] = L;
            }
            var col = Json.O(D["colecao"]); colecao = Json.S(col["nome"]); colecaoTexto = Json.S(col["texto"]);
            foreach (var kv in Json.O(D["oficios"])) { var o = Json.O(kv.Value); oficios[kv.Key] = new[] { Json.S(o["nome"]), Json.S(o["cor"]) }; }
            foreach (var kv in Json.O(D["acessorios"])) acessorios[kv.Key] = Json.L(kv.Value);
            try
            {
                var s = PlayerPrefs.GetString("rr-skins", ""); if (s.Length > 0) { var o = Json.O(Json.Ler(s)); foreach (var k in new List<string>(ESC.Keys)) { object v; if (o.TryGetValue(k, out v) && v is string && catalogo[k].Exists(x => x.id == (string)v)) ESC[k] = (string)v; } }
            }
            catch (Exception) { }
        }
        public Skin PorId(string arma, string id) { var L = catalogo[arma]; return L.Find(s => s.id == id) ?? L[0]; }
        public string Escolhida(string arma) { return ESC[arma]; }
        public bool Escolher(string arma, string id)
        {
            if (!catalogo.ContainsKey(arma) || !catalogo[arma].Exists(s => s.id == id)) return false; ESC[arma] = id;
            PlayerPrefs.SetString("rr-skins", "{\"carabina\":\"" + ESC["carabina"] + "\",\"revolver\":\"" + ESC["revolver"] + "\",\"cetro\":\"" + ESC["cetro"] + "\"}"); PlayerPrefs.Save(); return true;
        }

        // materiais com papel: guarda o acabamento original (para a de serviço voltar exata)
        public void Registrar(Material m, Dictionary<string, object> d)
        {
            object v; if (!d.TryGetValue("papel", out v) || v == null) return; var b = new Base { papel = (string)v };
            object bb; if (d.TryGetValue("base", out bb) && bb != null) { var o = Json.O(bb); b.cor = ArmaFP.HexLinear((int)Json.D(o["cor"])); b.rug = (float)Json.D(o["rug"]); b.met = (float)Json.D(o["met"]); }
            else { var l = Json.L(d["cor"]); b.cor = new Color((float)Json.D(l[0]), (float)Json.D(l[1]), (float)Json.D(l[2])); b.rug = (float)Json.D(d["rug"]); b.met = (float)Json.D(d["met"]); }
            b.claro = Num(d, "claro") ?? 1; bases[m] = b;
        }
        public void Preparar(ArmaFP.Arma A)
        {
            A.papeis.Clear();
            foreach (var r in A.g.GetComponentsInChildren<Renderer>(true)) { var m = r.sharedMaterial; Base b; if (m != null && bases.TryGetValue(m, out b) && b.papel != "brasa" && !A.papeis.Contains(m)) A.papeis.Add(m); }
        }
        Texture2D Textura(string nome)
        {
            Texture2D t; if (texs.TryGetValue(nome, out t)) return t;
            var ta = Resources.Load<TextAsset>("Detalhes/" + nome); t = null;
            if (ta != null)
            {
                try
                {
                    var png = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                    if (ImageConversion.LoadImage(png, ta.bytes))
                    {
                        // os canais são dados (sulco, incrustação, mancha), não cor: textura linear, que repete sem emenda
                        t = new Texture2D(png.width, png.height, TextureFormat.RGBA32, true, true) { name = "detalhe " + nome, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
                        t.SetPixels32(png.GetPixels32()); t.Apply(true); UnityEngine.Object.Destroy(png);
                    }
                }
                catch (Exception e) { Debug.LogWarning("detalhe " + nome + ": " + e.Message); }
            }
            return texs[nome] = t;
        }
        // ajusta um material ao papel da skin
        void Ajustar(Material m, Papel p)
        {
            var b = bases[m]; var cor = p.cor != null ? ArmaFP.HexLinear(p.cor.Value) : b.cor; if (b.claro != 1 && p.cor != null) cor = new Color(cor.r * b.claro, cor.g * b.claro, cor.b * b.claro);
            float rug = p.rug ?? b.rug, met = p.met ?? b.met; Mat.Cor(m, Mat.Lin(cor));
            if (m.HasProperty("_Rug")) { m.SetFloat("_Rug", rug); m.SetFloat("_Metal", met); } else { m.SetFloat("_Smoothness", Mathf.Clamp01(1 - rug) * .9f); m.SetFloat("_Metallic", met); }
            if (!m.HasProperty("_Det")) return;
            var T = p.det != null ? Textura(p.det) : null; bool on = T != null;
            m.SetTexture("_Det", T); m.SetFloat("_DetEsc", p.esc); m.SetFloat("_Sulco", on ? p.sulco ?? 0 : 0); m.SetFloat("_Inc", on ? p.inc : 0); m.SetFloat("_Mancha", on ? p.mancha : 0); m.SetFloat("_Relevo", on ? p.bump : 0);
            m.SetColor("_ManchaCor", Mat.Hex(p.manchaCor)); m.SetColor("_IncCor", Mat.Hex(p.incCor)); m.SetFloat("_IncMet", p.incMet ?? 1); m.SetFloat("_IncRug", p.incRug ?? .2f);
        }
        static readonly Papel VAZIO = new Papel();
        // aplicar uma skin a uma arma montada (primeira pessoa: com detalhe e acessórios)
        public Skin Aplicar(ArmaFP.Arma A, string arma, string id)
        {
            var sk = PorId(arma, id); A.skin = sk;
            foreach (var m in A.papeis) { Papel p; Ajustar(m, sk.papeis.TryGetValue(bases[m].papel, out p) ? p : VAZIO); }
            // a brasa e as vértebras acesas tomam a cor da skin só na primeira pessoa (os outros veem a cor do culto)
            if (arma == "cetro")
            {
                Papel pb; var cor = sk.papeis.TryGetValue("brasa", out pb) && pb.cor != null ? ArmaFP.HexLinear(pb.cor.Value) : A.corOriginal; A.corBrasa = cor;
                if (A.mBrasa != null) Mat.Cor(A.mBrasa, Mat.Lin(cor)); if (A.mHalo != null) Mat.Cor(A.mHalo, Mat.Lin(new Color(cor.r, cor.g, cor.b, A.mHalo.HasProperty("_Color") ? A.mHalo.GetColor("_Color").a : .8f)));
                foreach (var v in A.vert) Emissao(v, cor);
            }
            if (arma == "revolver") A.cruzCor = sk.cruz ?? new[] { .9f, .75f, .35f };
            foreach (var g in A.acess) { PEND.RemoveAll(P => P.pivo == null || P.pivo.IsChildOf(g.transform)); UnityEngine.Object.Destroy(g); }
            A.acess.Clear();
            List<object> L;
            if (acessorios.TryGetValue(arma + ":" + sk.id, out L))
                foreach (var o in L)
                {
                    var e = Json.O(o); var nos = Json.L(e["nos"]); var T = fp.Arvore(nos, fp.Nos[(int)Json.D(e["pai"])], fp.Malhas, fp.Mats); A.acess.Add(T[0].gameObject);
                    foreach (var p in Json.L(e["pend"])) PEND.Add(new Pend { pivo = T[(int)Json.D(Json.O(p)["no"])] });
                }
            return sk;
        }
        // emissão no material da primeira pessoa (cor linear do three) e só a intensidade (vértebras do cetro)
        public static void Emissao(Material m, Color c)
        {
            if (m == null) return; if (m.HasProperty("_Emissao")) { m.SetColor("_Emissao", Mat.Lin(c)); return; }
            emisBase[m] = c; m.SetColor("_EmissionColor", Mat.Lin(c)); m.EnableKeyword("_EMISSION");
        }
        public static void EmissaoI(Material m, float k)
        {
            if (m == null) return; if (m.HasProperty("_EmissaoI")) { m.SetFloat("_EmissaoI", k); return; }
            Color c; if (!emisBase.TryGetValue(m, out c)) return; m.SetColor("_EmissionColor", Mat.Lin(new Color(c.r * k, c.g * k, c.b * k))); m.EnableKeyword("_EMISSION");
        }

        // ganchos de inspeção e quadro a quadro (varredura de luz, gotas, pendentes)
        public void Gancho(ArmaFP.Arma A, string nome)
        {
            var sk = A.skin; if (sk == null) return;
            if (nome == "revela")
            {
                var mats = new List<Material>(); foreach (var m in A.papeis) if (m.HasProperty("_Varre")) mats.Add(m);
                VARRE.Add(new Varre { mats = mats, dur = 1.1f, cor = ArmaFP.HexLinear(sk.temRevela ? sk.revelaCor : 0xffffff), forca = sk.temRevela ? sk.revelaForca : .55f });
                if (sk.som != null) fp.Tocar(sk.som, .32f, 1);
            }
            if (nome == "gotas" && sk.gotas && A.brasa != null) A.gotasT = 2.6f;
            if (nome == "respira" && sk.gotas) A.respiraT = 2.2f;
        }
        public void Quadro(float dt, ArmaFP.Arma[] armas)
        {
            for (int i = VARRE.Count - 1; i >= 0; i--)
            {
                var V = VARRE[i]; V.t += dt; float u = V.t / V.dur, x = -.45f + u, k = Mathf.Sin(Mathf.Min(1, u) * Mathf.PI) * V.forca;
                foreach (var m in V.mats) { m.SetVector("_Varre", new Vector4(x, .07f, u < 1 ? k : 0, 0)); m.SetColor("_VarreCor", Mat.Lin(V.cor)); }
                if (u >= 1) VARRE.RemoveAt(i);
            }
            // pendentes: a ponta procura a gravidade no quadro do pai, com mola e amortecimento (balança quando a arma vira)
            foreach (var P in PEND)
            {
                if (P.pivo == null || P.pivo.parent == null || !P.pivo.gameObject.activeInHierarchy) continue;
                var g = P.pivo.parent.InverseTransformVector(Vector3.down).normalized;   // a gravidade nas coordenadas do three do pai
                float tx = Mathf.Atan2(-g.z, -g.y), tz = Mathf.Atan2(g.x, -g.y), K = 90, c = 7;
                P.vx += ((tx - P.ax) * K - P.vx * c) * dt; P.vz += ((tz - P.az) * K - P.vz * c) * dt; P.ax += P.vx * dt; P.az += P.vz * dt;
                P.ax = Mathf.Clamp(P.ax, -1.4f, 1.4f); P.az = Mathf.Clamp(P.az, -1.4f, 1.4f); P.pivo.localRotation = Qd.Euler(P.ax, 0, P.az).F;
            }
            foreach (var A in armas)
            {
                if (A == null || !A.g.gameObject.activeInHierarchy) continue;
                if (A.gotasT > 0) { A.gotasT -= dt; A.gotaAcc += dt; if (A.gotaAcc > .16f) { A.gotaAcc = 0; Gota(A); } }
                if (A.cab == null) continue;
                if (A.respiraT > 0) { A.respiraT -= dt; float u = 1 - A.respiraT / 2.2f, s = 1 + Mathf.Sin(u * Mathf.PI * 3) * .06f * Mathf.Sin(u * Mathf.PI); A.cab.localScale = new Vector3(s, 1 + (s - 1) * .4f, s); }
                else if (A.cab.localScale.x != 1) A.cab.localScale = Vector3.one;
            }
        }
        void Gota(ArmaFP.Arma A)
        {
            if (fp.raizMundo == null) return;
            var p = fp.raizMundo.InverseTransformPoint(A.brasa.position); float ang = UnityEngine.Random.value * Mathf.PI * 2; p += new Vector3(Mathf.Cos(ang) * .035f, -.05f, Mathf.Sin(ang) * .035f);
            var m = Mat.Brilho(Mat.Hex(0x9fe8d0, .85f), Tex.Brilho(), Mat.Mistura.Alfa, true); var sp = Mundo.Malha(Geo.Sprite(Vector3.zero, 1, 1), m, fp.raizMundo, "gota", false).transform;
            sp.localPosition = p; sp.localScale = new Vector3(.012f, .018f, 1); float vy = 0;
            fp.Efeito(sp, m, .7f, (o, u) => { vy -= 9.8f * .016f; o.localPosition += new Vector3(0, vy * .016f, 0); Mat.Opacidade(m, .85f * (1 - u)); });
        }
    }
}
