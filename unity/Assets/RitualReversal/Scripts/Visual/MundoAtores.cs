// Tudo o que se mexe na partida além do cenário (syncActors / syncNoite / efeitos do protótipo): bonecos animados com
// o Véu, sigilos voando, coletáveis, pegadas e marcas de arrasto, círculos de sal, mercadores, pontos de tarefa,
// partículas (700 pontos numa só malha), clarão de impacto, anéis e ondas das habilidades e rastros de tiro.
using System;
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;
using Random = UnityEngine.Random;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        Transform atoresRaiz;
        public readonly Dictionary<int, Boneco> bonecos = new Dictionary<int, Boneco>();
        class ProjV { public GameObject go; public Material mRuna; }
        readonly Dictionary<int, ProjV> projs = new Dictionary<int, ProjV>();
        readonly Dictionary<int, GameObject> picks = new Dictionary<int, GameObject>(), sais = new Dictionary<int, GameObject>();

        void GarantirAtores() { if (atoresRaiz == null) { atoresRaiz = Grupo(raiz, "Partida"); ConstruirParticulas(); } }

        // limpa bonecos, sigilos e efeitos (nova partida)
        public void Limpar()
        {
            foreach (var b in bonecos.Values) b.Destruir(); bonecos.Clear();
            foreach (var p in projs.Values) Destroy(p.go); projs.Clear();
            foreach (var p in picks.Values) Destroy(p); picks.Clear(); foreach (var p in sais.Values) Destroy(p); sais.Clear();
            foreach (var e in efeitos) Destroy(e.go); efeitos.Clear();
            foreach (var p in pontosV.Values) Destroy(p.g.gameObject); pontosV.Clear();
            for (int i = 0; i < PN; i++) { vida[i] = 0; pp[i] = new Vector3(0, -99, 0); }
        }

        // ---------- partículas ----------
        const int PN = 700; int pi0; Mesh pMalha; readonly Vector3[] pp = new Vector3[PN], pv = new Vector3[PN]; readonly Color[] pbase = new Color[PN];
        readonly float[] vida = new float[PN], vmax = new float[PN], grav = new float[PN]; Vector3[] pVerts; Color[] pCores; Light luzImpacto;
        void ConstruirParticulas()
        {
            var g = new Geo(); for (int i = 0; i < PN; i++) g.Juntar(Geo.Sprite(Vector3.zero, .08f, .08f, Color.black));
            pMalha = g.ToMesh("partículas"); pMalha.MarkDynamic(); pMalha.bounds = new Bounds(Vector3.zero, Vector3.one * 2000);
            pVerts = new Vector3[PN * 4]; pCores = new Color[PN * 4]; for (int i = 0; i < PN; i++) pp[i] = new Vector3(0, -99, 0);
            var go = new GameObject("partículas"); go.transform.SetParent(atoresRaiz, false); go.AddComponent<MeshFilter>().sharedMesh = pMalha;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = Mat.Brilho(Color.white, Tex.Brilho(), Mat.Mistura.Aditiva, true); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            luzImpacto = LuzPonto(atoresRaiz, 0xffb070, 0, 7, 2, "clarão"); luzImpacto.enabled = false;
        }
        public void Particulas(float x, float y, float z, int n, int hex, float vel, float vid, float gr, float sobe = 0)
        {
            if (pMalha == null) return; var c = Mat.Hex(hex);
            for (int k = 0; k < n; k++)
            {
                int i = pi0; pi0 = (pi0 + 1) % PN; pp[i] = new Vector3(x, y, z);
                float th = Random.value * PI * 2, ph = Random.value * PI, sp = vel * (.4f + Random.value * .8f);
                pv[i] = new Vector3(Mathf.Cos(th) * Mathf.Sin(ph) * sp, Mathf.Cos(ph) * sp + sobe, Mathf.Sin(th) * Mathf.Sin(ph) * sp);
                vida[i] = vmax[i] = vid * (.6f + Random.value * .6f); grav[i] = gr; pbase[i] = c;
            }
        }
        public void Clarao(float x, float y, float z, int hex, float forca) { if (luzImpacto == null) return; luzImpacto.transform.localPosition = new Vector3(x, y, z); luzImpacto.color = Mat.Hex(hex); iImpacto = forca; }
        float iImpacto;
        void AtualizarParticulas(float dt)
        {
            if (pMalha == null) return;
            for (int i = 0; i < PN; i++)
            {
                Color c = Color.black;
                if (vida[i] > 0)
                {
                    vida[i] -= dt;
                    if (vida[i] <= 0) pp[i] = new Vector3(0, -99, 0);
                    else
                    {
                        var v = pv[i]; v.y -= grav[i] * dt; var p = pp[i] + v * dt;
                        if (p.y < .02f) { p.y = .02f; v.y *= -.3f; v.x *= .6f; v.z *= .6f; }
                        pp[i] = p; pv[i] = v; float k = vida[i] / vmax[i]; c = new Color(pbase[i].r * k, pbase[i].g * k, pbase[i].b * k, 1);
                    }
                }
                for (int j = 0; j < 4; j++) { pVerts[i * 4 + j] = pp[i]; pCores[i * 4 + j] = c; }
            }
            pMalha.vertices = pVerts; pMalha.colors = pCores;
            iImpacto = Mathf.Max(0, iImpacto - dt * 28); luzImpacto.enabled = iImpacto > .01f && Qualidade.nivel >= 1; luzImpacto.intensity = IntensidadeUnity(iImpacto, 7, 2);
        }

        // ---------- efeitos que duram um instante ----------
        class Efeito { public GameObject go; public float dur, t; public Action<GameObject, float> fn; public List<Material> mats = new List<Material>(); }
        readonly List<Efeito> efeitos = new List<Efeito>();
        Efeito NovoEfeito(GameObject go, float dur, Action<GameObject, float> fn, params Material[] mats) { var e = new Efeito { go = go, dur = dur, fn = fn }; e.mats.AddRange(mats); efeitos.Add(e); return e; }
        void AtualizarEfeitos(float dt)
        {
            for (int i = efeitos.Count - 1; i >= 0; i--)
            {
                var e = efeitos[i]; e.t += dt; float k = Mathf.Min(1, e.t / e.dur);
                if (e.go != null) e.fn(e.go, k);
                if (k >= 1) { if (e.go != null) { foreach (var mf in e.go.GetComponentsInChildren<MeshFilter>()) Destroy(mf.sharedMesh); Destroy(e.go); } foreach (var m in e.mats) Destroy(m); efeitos.RemoveAt(i); }
            }
        }
        void AnelChao(float x, float z, float raio, int cor, float dur, Texture2D mapa = null)
        {
            var m = Mat.Brilho(Mat.Hex(cor), mapa, Mat.Mistura.Aditiva); var go = Malha(Geo.Anel(.82f, 1, 48), m, atoresRaiz, "anel", false);
            go.transform.localPosition = new Vector3(x, .06f, z); go.transform.localRotation = Q(-PI / 2, 0, 0);
            NovoEfeito(go, dur, (o, k) => { float r = .3f + raio * (1 - Mathf.Pow(1 - k, 3)); o.transform.localScale = new Vector3(r, r, 1); Mat.Opacidade(m, 1 - k); }, m);
        }
        // Cada habilidade tem uma forma que mostra o próprio alcance: quem vê o efeito entende o que ele pegou.
        public void EfeitoHabilidade(Evento e, bool meu)
        {
            GarantirAtores();
            float x = (float)e.Num("x"), z = (float)e.Num("z"), fx = (float)e.Num("fx"), fz = (float)e.Num("fz"); string F = e.Str("f");
            if (F == "veu") { Particulas(x, 1, z, 40, 0x2a0a3a, 2.2f, 1.2f, -.5f, .5f); Particulas(x, 1, z, 14, 0x8a3aff, 1.2f, .8f, -1, .4f); }
            else if (F == "sal") { AnelChao(x + fx * 1.2f, z + fz * 1.2f, 2.2f, 0xf0ece0, .8f); Particulas(x + fx * 1.2f, .2f, z + fz * 1.2f, 20, 0xffffff, 1.5f, .6f, 4, .6f); }
            else if (F == "runa")
            {
                var m = Mat.Brilho(Mat.Hex(0xc070ff), Tex.Runa(), Mat.Mistura.Aditiva); var go = Malha(Geo.Plano(1, 1), m, atoresRaiz, "runa", false);
                go.transform.localPosition = new Vector3(x, .07f, z);
                NovoEfeito(go, 1.1f, (o, k) => { float r = 1 + 4.5f * Mathf.Sqrt(k); o.transform.localScale = new Vector3(r, r, 1); o.transform.localRotation = Q(-PI / 2, 0, k * 2); Mat.Opacidade(m, 1 - k * k); }, m);
                for (int i = 0; i < 3; i++) StartCoroutine(Depois(i * .12f, () => Particulas(x, .4f, z, 14, 0xb050ff, 1.4f, 1.1f, -2, 1.2f)));
                Clarao(x, 1.2f, z, 0xb050ff, 3);
            }
            else if (F == "empurrao")
            {
                float ang = Mathf.Atan2(fx, fz); var m = Mat.Brilho(Mat.Hex(0xff6a50), null, Mat.Mistura.Aditiva);
                var g = Grupo(atoresRaiz, "empurrão"); var anel = Malha(Geo.Anel(.7f, 1, 32, -1.2f, 2.4f), m, g, "onda", false); anel.transform.localRotation = Q(-PI / 2, 0, -ang + PI / 2);
                g.localPosition = new Vector3(x, .35f, z);
                NovoEfeito(g.gameObject, .45f, (o, k) => { float r = .5f + 3.8f * (1 - Mathf.Pow(1 - k, 2)); o.transform.localScale = new Vector3(r, 1, r); Mat.Opacidade(m, 1 - k); }, m);
                for (int i = 0; i < 5; i++) { float a = ang - .6f + i * .3f; Particulas(x + Mathf.Sin(a) * 2.5f, .3f, z + Mathf.Cos(a) * 2.5f, 6, 0x9a8a70, 2.5f, .6f, 5, 1); }
            }
            else if (F == "flash")
            {
                float px = x + fx * 3, pz = z + fz * 3; var m = Mat.Brilho(Color.white, Tex.Brilho(), Mat.Mistura.Aditiva, true); var sp = Sprite(atoresRaiz, m, 1, 1, "flash");
                sp.transform.localPosition = new Vector3(px, 1.5f, pz); float tam = meu ? 5 : 12, op = meu ? .55f : 1;
                NovoEfeito(sp, .4f, (o, k) => { float r = 2 + tam * k; o.transform.localScale = new Vector3(r, r, 1); Mat.Opacidade(m, (1 - k) * op); }, m);
                Clarao(px, 1.5f, pz, 0xffffff, meu ? 5 : 14); Particulas(px, 1.5f, pz, 24, 0xfff4d0, 6, .35f, 2, .5f);
            }
            else if (F == "purificacao")
            {
                AnelChao(x, z, 6, 0xf3d58e, .7f, Tex.Runa()); AnelChao(x, z, 6, 0xffe8b0, .45f);
                if (!meu)
                {
                    var m = Mat.Brilho(Mat.Hex(0xf3d58e), Tex.Feixe(), Mat.Mistura.Aditiva); var col = Malha(Geo.Cilindro(.6f, 1.4f, 9, 20, 1, true), m, atoresRaiz, "coluna", false);
                    col.transform.localPosition = new Vector3(x, 4.5f, z); NovoEfeito(col, .8f, (o, k) => { Mat.Opacidade(m, (1 - k) * .8f); o.transform.localScale = new Vector3(1 + k, 1, 1 + k); }, m);
                }
                Particulas(x, .5f, z, meu ? 14 : 30, 0xf3d58e, 3.5f, .9f, -1, 1.5f); Clarao(x, 2, z, 0xf3d58e, meu ? 2 : 6);
            }
            var alvos = e["alvos"] as System.Collections.IEnumerable;
            if (alvos != null) foreach (var o in alvos) { Boneco b; if (o != null && bonecos.TryGetValue(Convert.ToInt32(o), out b)) b.flinch = 1; }
        }
        static System.Collections.IEnumerator Depois(float s, Action f) { yield return new WaitForSeconds(s); f(); }

        // rastro do tiro (p0 e p1 no protótipo)
        public void Tracer(Vector3 p0, Vector3 p1)
        {
            GarantirAtores();
            var go = new GameObject("tiro"); go.transform.SetParent(atoresRaiz, false); var l = go.AddComponent<LineRenderer>(); l.useWorldSpace = false;
            l.positionCount = 2; l.SetPosition(0, p0); l.SetPosition(1, p1); l.startWidth = .014f; l.endWidth = .008f;
            var m = Mat.Brilho(Mat.Hex(0xffd9a0, .8f), null, Mat.Mistura.Aditiva); l.sharedMaterial = m; l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            NovoEfeito(go, .07f, (o, k) => Mat.Opacidade(m, .8f * (1 - k)), m);
        }
        // eventos visuais da simulação (handleEvents): acertos, estouros de sigilo, habilidades
        public void EventoVisual(Evento e, int eu, Vector3 bocaPropria)
        {
            GarantirAtores(); Boneco b;
            switch (e.type)
            {
                case "sfx": if (e.Str("k") == "sigil" && e.Int("a") != eu && bonecos.TryGetValue(e.Int("a"), out b)) b.cast = 1; break;
                case "tracer":
                    {
                        bool own = e.Int("a") == eu; var p1 = new Vector3((float)e.Num("x1"), (float)e.Num("y1"), (float)e.Num("z1"));
                        var p0 = own ? bocaPropria : new Vector3((float)e.Num("x0"), (float)e.Num("y0") + .1f, (float)e.Num("z0"));
                        Tracer(p0, p1);
                        if (e["alvo"] != null)
                        {
                            bool cab = e["cab"] is bool && (bool)e["cab"];
                            Particulas(p1.x, p1.y, p1.z, cab ? 22 : 12, 0xb01830, 2.2f, .45f, 6, .6f); if (cab) Particulas(p1.x, p1.y, p1.z, 8, 0xffd070, 3, .25f, 4, 1);
                            if (bonecos.TryGetValue(e.Int("alvo"), out b)) { b.flinch = 1; b.golpe = new Vector2((float)e.Num("x0"), (float)e.Num("z0")); }
                        }
                        else { Particulas(p1.x, p1.y, p1.z, 9, 0xffc070, 4.5f, .3f, 12, 1.2f); Particulas(p1.x, p1.y, p1.z, 5, 0x5a5248, 1.2f, .8f, 1, .4f); Clarao(p1.x, p1.y, p1.z, 0xffb070, 1.2f); }
                        if (!own && bonecos.TryGetValue(e.Int("a"), out b)) b.kick = 1;
                        break;
                    }
                case "hurt": if (e.Int("a") != eu && bonecos.TryGetValue(e.Int("a"), out b)) { b.flinch = 1; if (e["sx"] != null) b.golpe = new Vector2((float)e.Num("sx"), (float)e.Num("sz")); } break;
                case "burst": { float x = (float)e.Num("x"), y = (float)e.Num("y"), z = (float)e.Num("z"); Particulas(x, y, z, 18, 0xb050ff, 3, .5f, 3, .8f); Particulas(x, y, z, 8, 0xff4a70, 1.5f, .7f, 1, .3f); Clarao(x, y, z, 0xb050ff, 2.2f); break; }
                case "hab": EfeitoHabilidade(e, e.Int("a") == eu); break;
            }
        }

        // ---------- bonecos, sigilos, coletáveis, rastros, sal ----------
        public void SyncActors(Jogo g, string papel, Ator eu, Vector3 cam, float lookYaw, float dt)
        {
            GarantirAtores(); float t = Time.time; var vistos = new HashSet<int>();
            foreach (var a in g.actors)
            {
                if (a == eu) continue; vistos.Add(a.id); Boneco b;
                if (!bonecos.TryGetValue(a.id, out b)) { b = new Boneco(atoresRaiz, a.team, a.cls); bonecos[a.id] = b; }
                float x = (float)a.x, z = (float)a.z;
                b.Animar(this, a, x, z, (float)a.yaw, Mathf.Max(dt, .001f), t, g.altars);
                // Véu: silhueta escura e translúcida. Some quase por completo de longe; de perto ou sob a lanterna dá para ver o vulto.
                float op = 1;
                if (a.veuT > 0 && a.team != papel && !(a.revelT > 0) && a.st == "alive")
                {
                    float d = Mathf.Sqrt((x - cam.x) * (x - cam.x) + (z - cam.z) * (z - cam.z));
                    bool luz = eu != null && eu.lantern && d < 16 && ((x - cam.x) * -Mathf.Sin(lookYaw) + (z - cam.z) * -Mathf.Cos(lookYaw)) / Mathf.Max(d, .1f) > .9f;
                    op = luz ? .6f : d < 8 ? .42f : .06f;
                }
                b.Veu(op);
            }
            var sair = new List<int>(); foreach (var k in bonecos.Keys) if (!vistos.Contains(k)) sair.Add(k);
            foreach (var k in sair) { bonecos[k].Destruir(); bonecos.Remove(k); }
            SyncRastros(g, papel, eu);
            // sigilos
            var sp = new HashSet<int>();
            foreach (var p in g.proj)
            {
                sp.Add(p.id); ProjV v;
                if (!projs.TryGetValue(p.id, out v))
                {
                    v = new ProjV(); v.go = Malha(Geo.Esfera(.13f, 20, 16), Pecas.Basico(0xf0c0ff), atoresRaiz, "sigilo", false); v.go.transform.localScale = Vector3.one * .6f;
                    Sprite(v.go.transform, Pecas.Aditivo(0xb040ff, Tex.Brilho(), true), 1.5f, 1.5f, "halo");
                    v.mRuna = Pecas.Aditivo(0xffb0e0, Tex.Runa(), true); Sprite(v.go.transform, v.mRuna, 1.1f, 1.1f, "runa"); projs[p.id] = v;
                }
                v.go.transform.localPosition = new Vector3((float)p.x, (float)p.y, (float)p.z);
                v.mRuna.SetFloat("_Giro", v.mRuna.GetFloat("_Giro") + dt * 9);
                Particulas((float)p.x, (float)p.y, (float)p.z, 2, Random.value < .5f ? 0xb050ff : 0xff4a80, .35f, .4f, -.4f, 0);
            }
            sair.Clear(); foreach (var k in projs.Keys) if (!sp.Contains(k)) sair.Add(k); foreach (var k in sair) { Destroy(projs[k].go); projs.Remove(k); }
            // coletáveis
            var pk = new HashSet<int>();
            foreach (var p in g.pickups)
            {
                pk.Add(p.id); GameObject go;
                if (!picks.TryGetValue(p.id, out go)) { int col = p.kind == "reag" ? 0xff3355 : p.kind == "ess" ? 0xb35cff : 0xffd98a; go = Malha(Geo.Octaedro(p.kind == "reag" ? .18f : .24f), Pecas.Basico(col), atoresRaiz, "coletável", false); picks[p.id] = go; }
                go.transform.localPosition = new Vector3((float)p.x, .4f + Mathf.Sin(t * 3 + p.id) * .08f, (float)p.z); go.transform.localRotation = Q(0, t * 1.8f, 0);
            }
            sair.Clear(); foreach (var k in picks.Keys) if (!pk.Contains(k)) sair.Add(k); foreach (var k in sair) { Destroy(picks[k]); picks.Remove(k); }
            // círculos de sal
            var sv = new HashSet<int>();
            foreach (var q in g.sal)
            {
                sv.Add(q.id);
                if (!sais.ContainsKey(q.id)) { var go = Malha(Geo.Anel(1.9f, 2.2f, 40), Mat.Brilho(Mat.Hex(0xf2eee4, .75f), null, Mat.Mistura.Alfa), atoresRaiz, "sal", false); go.transform.localPosition = new Vector3((float)q.x, .03f, (float)q.z); go.transform.localRotation = Q(-PI / 2, 0, 0); sais[q.id] = go; }
            }
            sair.Clear(); foreach (var k in sais.Keys) if (!sv.Contains(k)) sair.Add(k); foreach (var k in sair) { Destroy(sais[k]); sais.Remove(k); }
        }

        // Pegadas (só o Rastreador vê) e marcas de arrasto da Transferência (qualquer Caçador vê)
        Mesh rastrosM; readonly List<Vector3> rv = new List<Vector3>(), rn = new List<Vector3>(); readonly List<Vector2> ru = new List<Vector2>(); readonly List<Color> rc = new List<Color>(); readonly List<int> rt = new List<int>();
        void SyncRastros(Jogo g, string papel, Ator eu)
        {
            if (rastrosM == null)
            {
                rastrosM = new Mesh { name = "rastros" }; rastrosM.MarkDynamic();
                var go = new GameObject("rastros"); go.transform.SetParent(atoresRaiz, false); go.AddComponent<MeshFilter>().sharedMesh = rastrosM;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = Mat.Brilho(Color.white, Tex.Pegada(), Mat.Mistura.Aditiva, false, false); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            rv.Clear(); rn.Clear(); ru.Clear(); rc.Clear(); rt.Clear(); int n = 0; bool H = papel == "H";
            Action<float, float, float, float, float, float, Color> quad = (x, y, z, rot, sx, sz, c) =>
            {
                var M = M4.T(x, y, z) * M4.RY(rot) * M4.S(sx, 1, sz); int b = rv.Count;
                float[,] q = { { -.16f, -.25f }, { .16f, -.25f }, { .16f, .25f }, { -.16f, .25f } };
                for (int i = 0; i < 4; i++) { rv.Add(M.Ponto(new Vector3(q[i, 0], 0, -q[i, 1]))); rn.Add(Vector3.up); ru.Add(new Vector2(i == 1 || i == 2 ? 1 : 0, i >= 2 ? 1 : 0)); rc.Add(c); }
                rt.Add(b); rt.Add(b + 1); rt.Add(b + 2); rt.Add(b); rt.Add(b + 2); rt.Add(b + 3);
            };
            if (H && eu != null && eu.cls == "rastreador") foreach (var p in g.pegadas) { if (n >= 200) break; float k = 1 - (float)((g.t - p.t) / CFG.pegadaVida); if (k <= 0) continue; quad((float)p.x, .04f, (float)p.z, (float)p.yaw + PI, 1, 1, new Color(.25f * k, .75f * k, .62f * k, 1)); n++; }
            if (H) foreach (var r in g.rastros) { float k = Mathf.Max(.25f, 1 - (float)((g.rt - r.t) / 180)); for (int j = 0; j < 6 && n < 240; j++) { float an = j * 1.047f + .4f; quad((float)r.x + Mathf.Cos(an) * 2.2f, .05f, (float)r.z + Mathf.Sin(an) * 2.2f, -an + PI / 2, 1.6f, 5, new Color(.55f * k, .16f * k, .12f * k, 1)); n++; } }
            rastrosM.Clear(); rastrosM.SetVertices(rv); rastrosM.SetNormals(rn); rastrosM.SetUVs(0, ru); rastrosM.SetColors(rc); rastrosM.SetTriangles(rt, 0); rastrosM.RecalculateBounds();
        }

        // ---------- mercadores e pontos de tarefa (syncNoite) ----------
        class NpcV { public Transform g, corpo; public float fase, giro; public Npc n; }
        readonly List<NpcV> npcs = new List<NpcV>();
        class PontoV { public Transform g, lapide; public GameObject buraco, luz; public Material mLuz; public Light pl; }
        readonly Dictionary<string, PontoV> pontosV = new Dictionary<string, PontoV>();

        GameObject Halo(Transform pai, int cor, float esc, out Material m) { m = Pecas.Aditivo(cor, Tex.Brilho(), true); return Sprite(pai, m, esc, esc, "brilho"); }
        NpcV FazerNPC(Npc n)
        {
            var V = new NpcV { n = n, fase = Random.value * 6 }; V.g = Grupo(atoresRaiz, "Mercador " + n.id); V.corpo = Grupo(V.g, "corpo"); V.g.localPosition = new Vector3((float)n.x, 0, (float)n.z);
            var k = new Kit(); var c = V.corpo; Material mm;
            if (n.id == "ermitao")
            {
                var manto = Pecas.Pano(0x4a4640); manto.SetFloat("_Cull", 0);
                k.Por(c, manto, Pecas.Esfarrapar(Pecas.Torno(.46f, 0, .43f, .3f, .36f, .8f, .3f, 1.2f, .26f, 1.35f, 0, 1.36f), .05f, 3), M4.I);
                k.Por(c, manto, Pecas.Torno(.22f, -.08f, .26f, .06f, .25f, .2f, .18f, .32f, .08f, .4f, 0, .42f), 0, 1.35f, 0);
                k.Por(c, Pecas.Pano(0xb8b2a4), Geo.Cone(.11f, .34f, 12), 0, 1.28f, -.14f, PI);
                k.Por(c, Pecas.Liso(0x3a2616, .8f), Geo.Cilindro(.03f, .035f, 2.1f, 8), .42f, 1.05f, -.1f, 0, 0, -.06f);
                k.Por(c, Pecas.Metal(0x2a2a2e, .5f), Geo.Cilindro(.08f, .08f, .18f, 10, 1, true), .46f, 2.05f, -.12f);
                Pos(Halo(c, 0xffc070, .9f, out mm), .46f, 2.05f, -.12f);
            }
            else if (n.id == "carpideira")
            {
                var veu = Pecas.Pano(0x0d0b10); veu.SetFloat("_Cull", 0);
                k.Por(c, veu, Pecas.Esfarrapar(Pecas.Torno(.5f, 0, .44f, .4f, .34f, .9f, .26f, 1.4f, .2f, 1.62f, .08f, 1.78f, 0, 1.8f), .06f, 8), M4.I);
                k.Por(c, MAT["wax"], Geo.Cilindro(.03f, .03f, .22f, 8), 0, 1.02f, -.34f);
                Pos(Halo(c, 0xffb060, .55f, out mm), 0, 1.18f, -.34f);
            }
            else
            {
                var capa = Pecas.Pano(0x2a1d24); capa.SetFloat("_Cull", 0);
                k.Por(c, capa, Pecas.Esfarrapar(Pecas.Torno(.48f, 0, .44f, .4f, .36f, .95f, .3f, 1.35f, .24f, 1.5f, 0, 1.52f), .04f, 5), M4.I);
                k.Por(c, Pecas.Liso(0xece6da, .25f), Geo.Esfera(.15f, 20, 16), 0, 1.62f, -.04f, 0, 0, 0, 1, 1.2f, .8f);
                var preto = Pecas.Basico(0x000000); foreach (float sx in new[] { -1f, 1f }) k.Por(c, preto, Geo.Esfera(.028f, 10, 8), sx * .055f, 1.66f, -.15f);
                var couro = Pecas.Couro(0x1a1214); couro.SetFloat("_Cull", 0);
                k.Por(c, couro, Pecas.Torno(0, 0, .42f, -.01f, .43f, .015f, .18f, .04f, 0, .045f), 0, 1.8f, 0); k.Por(c, couro, Pecas.Torno(.17f, 0, .16f, .26f, .1f, .3f, 0, .3f), 0, 1.82f, 0);
                var banca = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x3a2616), mapa = Tex.wood.mapa, rugosidade = .7f });
                k.Por(V.g, banca, Geo.Caixa(1.2f, .08f, .6f), 0, .9f, -.75f);
                int[] cores = { 0xff4a70, 0x70ffb0, 0xb070ff, 0xffd070, 0x70b0ff };
                for (int i = 0; i < 5; i++) k.Por(V.g, Pecas.Basico(cores[i], .85f), Geo.Cilindro(.04f, .05f, .18f, 10), -.45f + i * .22f, 1.03f, -.75f);
                foreach (var q in new[] { new Vector2(-.55f, -.5f), new Vector2(.55f, -.5f), new Vector2(-.55f, -1), new Vector2(.55f, -1) }) k.Por(V.g, banca, Geo.Caixa(.06f, .9f, .06f), q.x, .45f, q.y);
                Pos(Halo(c, 0xb070ff, .6f, out mm), 0, 1.62f, -.2f);
            }
            k.Fechar(); return V;
        }
        PontoV FazerPonto(PontoNoite p)
        {
            var V = new PontoV(); V.g = Grupo(atoresRaiz, "Ponto " + p.tipo); V.g.localPosition = new Vector3((float)p.x, 0, (float)p.z); var k = new Kit();
            if (p.tipo == "pista")
            {
                var est = Grupo(V.g, "estela"); est.localPosition = new Vector3(0, .47f, 0); est.localRotation = Q(0, Random.value * 3, 0);
                k.Por(est, MAT["wallDark"], Geo.Caixa(.55f, .95f, .22f), M4.I);
                var r = Malha(Geo.Plano(.4f, .4f), Pecas.Aditivo(0xf3d58e, Tex.Runa(), false), est, "runa", false); Pos(r, 0, .55f - .47f, .12f);
                V.luz = Halo(V.g, 0xf3d58e, .9f, out V.mLuz); Pos(V.luz, 0, .6f, 0);
            }
            else if (p.tipo == "sentinela")
            {
                var ferro = Pecas.Metal(0x22242a, .5f); ferro.SetFloat("_Cull", 0);
                k.Por(V.g, ferro, Geo.Cilindro(.06f, .09f, 2.3f, 10), 0, 1.15f, 0); k.Por(V.g, ferro, Geo.Cilindro(.16f, .13f, .34f, 10, 1, true), 0, 2.4f, 0);
                V.luz = Halo(V.g, 0xffc070, 1.6f, out V.mLuz); Pos(V.luz, 0, 2.4f, 0);
                V.pl = LuzPonto(V.g, 0xffb060, 0, 11, 2, "chama"); V.pl.transform.localPosition = new Vector3(0, 2.4f, 0); V.pl.enabled = false;
            }
            else if (p.tipo == "erva")
            {
                var folha = Mat.Padrao(new Mat.Opcoes { cor = Mat.Hex(0x1d3a34), emissao = Mat.Hex(0x0e4a40), emissaoK = .6f, rugosidade = .8f });
                for (int i = 0; i < 9; i++) { float a = i / 9f * 6.28f; k.Por(V.g, folha, Geo.Cone(.035f, .45f + Random.value * .25f, 6), Mathf.Cos(a) * .14f, .22f, Mathf.Sin(a) * .14f, Mathf.Sin(a) * .4f, 0, -Mathf.Cos(a) * .4f); }
                V.luz = Halo(V.g, 0x60ffd0, .8f, out V.mLuz); Pos(V.luz, 0, .5f, 0);
            }
            else if (p.tipo == "tumulo")
            {
                V.lapide = Grupo(V.g, "lápide"); V.lapide.localPosition = new Vector3(0, .4f, .35f); k.Por(V.lapide, MAT["marble"], Geo.Caixa(.6f, .8f, .14f), M4.I);
                k.Por(V.g, Pecas.Couro(0x2a2018), Geo.Esfera(.55f, 14, 8, 0, PI * 2, 0, PI / 2), 0, 0, 0, 0, 0, 0, 1, .3f, 1.6f);
                V.buraco = Malha(Geo.Circulo(.42f, 16), Pecas.Basico(0x020101), V.g, "buraco", false); V.buraco.transform.localPosition = new Vector3(0, .19f, 0); V.buraco.transform.localRotation = Q(-PI / 2, 0, 0); V.buraco.SetActive(false);
            }
            k.Fechar(); return V;
        }
        public void SyncNoite(Jogo g, Vector3 cam, float dt)
        {
            GarantirAtores(); float t = Time.time;
            if (npcs.Count == 0) foreach (var n in Mapa.NPCS) npcs.Add(FazerNPC(n));
            foreach (var V in npcs)
            {
                V.fase += dt; V.corpo.localPosition = new Vector3(0, Mathf.Sin(V.fase * 1.3f) * .015f, 0);
                float dx = cam.x - (float)V.n.x, dz = cam.z - (float)V.n.z;
                if (dx * dx + dz * dz < 81) { float alvo = Mathf.Atan2(-dx, -dz) + PI; V.giro = V.giro + (alvo - V.giro) * .05f; V.g.localRotation = Q(0, V.giro, 0); }
            }
            var vistos = new HashSet<string>();
            foreach (var p in g.pontos)
            {
                string key = p.tipo + p.i + "_" + JS.Round(p.x) + "_" + JS.Round(p.z); vistos.Add(key); PontoV V;
                if (!pontosV.TryGetValue(key, out V)) { V = FazerPonto(p); pontosV[key] = V; }
                if (p.tipo == "pista" || p.tipo == "erva") { V.g.gameObject.SetActive(p.ativo); if (V.mLuz != null) Mat.Opacidade(V.mLuz, .45f + .25f * Mathf.Sin(t * 2.5f + p.i)); }
                else if (p.tipo == "sentinela") { V.luz.SetActive(p.acesa); V.pl.enabled = p.acesa && Qualidade.nivel >= 3; V.pl.intensity = IntensidadeUnity(p.acesa ? 1.4f + Random.value * .15f : 0, 11, 2); }
                else if (p.tipo == "tumulo") { V.buraco.SetActive(!p.ativo); V.lapide.localRotation = Q(0, 0, p.ativo ? 0 : .35f); }
            }
            var sair = new List<string>(); foreach (var k in pontosV.Keys) if (!vistos.Contains(k)) sair.Add(k);
            foreach (var k in sair) { Destroy(pontosV[k].g.gameObject); pontosV.Remove(k); }
        }

        // chamado a cada quadro, com ou sem partida
        public void AtualizarEfeitosQuadro(float dt) { GarantirAtores(); AtualizarParticulas(dt); AtualizarEfeitos(dt); }
        public void MostrarPartida(bool on) { if (atoresRaiz != null) atoresRaiz.gameObject.SetActive(on); }
    }
}
