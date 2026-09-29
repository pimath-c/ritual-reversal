// Parte visual da partida: bonecos, altares (como o seu lado os enxerga), reagentes, pontos de tarefa, projéteis,
// coletáveis, sinalizadores, sal, pegadas, rastros de tiro e a lanterna. Tudo em formas simples (blockout).
// Protótipo (x, z) -> Unity (x, y, -z); giro -yaw.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static readonly Color OURO = new Color(.82f, .68f, .38f), CARMIM = new Color(.64f, .13f, .23f), CARMIM_VIVO = new Color(.88f, .26f, .37f),
            ROXO = new Color(.42f, .25f, .56f), ROXO_VIVO = new Color(.75f, .55f, .95f), CINZA = new Color(.35f, .36f, .38f), OSSO = new Color(.91f, .88f, .82f);

        Transform raizFx;
        Light lanterna;
        readonly Dictionary<int, GameObject> bonecos = new Dictionary<int, GameObject>();
        readonly List<AltarFx> altaresFx = new List<AltarFx>();
        readonly List<GameObject> reagentesFx = new List<GameObject>();
        readonly List<Light> flaresFx = new List<Light>();
        readonly Dictionary<int, GameObject> projFx = new Dictionary<int, GameObject>(), pickFx = new Dictionary<int, GameObject>(), salFx = new Dictionary<int, GameObject>();
        readonly List<GameObject> pontosFx = new List<GameObject>(); List<PontoNoite> pontosDe;
        readonly List<GameObject> pegadasFx = new List<GameObject>();
        class Temporario { public GameObject go; public float t, dur; public LineRenderer linha; }
        readonly List<Temporario> temporarios = new List<Temporario>();
        readonly Dictionary<string, Material> materiais = new Dictionary<string, Material>();

        class AltarFx { public GameObject anel, feixe; public Light luz; public Renderer rAnel, rFeixe; }

        static Vector3 P(double x, double z, float y = 0f) { return new Vector3((float)x, y, (float)-z); }

        // ---------- materiais ----------
        Material Mat(string nome, Color cor, bool brilha)
        {
            Material m; if (materiais.TryGetValue(nome, out m)) return m;
            Shader sh = brilha ? (Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color")) : Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh); Cor(m, cor); materiais[nome] = m; return m;
        }
        static void Cor(Material m, Color c) { if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); if (m.HasProperty("_Color")) m.SetColor("_Color", c); }

        GameObject Forma(PrimitiveType tipo, string nome, Material mat, Transform pai)
        {
            var go = GameObject.CreatePrimitive(tipo); go.name = nome; var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = mat; go.transform.SetParent(pai, false); return go;
        }
        Light Luz(Transform pai, string nome, Color cor, float alcance, float intensidade)
        {
            var go = new GameObject(nome); go.transform.SetParent(pai, false); var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = cor; l.range = alcance; l.intensity = intensidade; l.shadows = LightShadows.None; return l;
        }

        void CriarVisual()
        {
            raizFx = new GameObject("Partida (efeitos)").transform;
            // os marcadores fixos do importador viram dinâmicos aqui (aparecem e somem conforme a partida)
            foreach (var nome in new[] { "Reagentes (Claustro)", "Pontos de tarefa" }) { var o = GameObject.Find(nome); if (o != null) o.SetActive(false); }
            for (int i = 0; i < Mapa.ALTARS.Count; i++)
            {
                var A = Mapa.ALTARS[i]; var fx = new AltarFx();
                fx.anel = Forma(PrimitiveType.Cylinder, "Anel " + A.name, Mat("anel_" + i, CINZA, true), raizFx); fx.anel.transform.position = P(A.x, A.z, .04f); fx.anel.transform.localScale = new Vector3(6f, .01f, 6f);
                fx.rAnel = fx.anel.GetComponent<Renderer>(); fx.rAnel.sharedMaterial = new Material(fx.rAnel.sharedMaterial);
                fx.feixe = Forma(PrimitiveType.Cylinder, "Feixe " + A.name, Mat("feixe_" + i, CARMIM_VIVO, true), raizFx); fx.feixe.transform.position = P(A.x, A.z, 30f); fx.feixe.transform.localScale = new Vector3(.5f, 30f, .5f);
                fx.rFeixe = fx.feixe.GetComponent<Renderer>(); fx.rFeixe.sharedMaterial = new Material(fx.rFeixe.sharedMaterial);
                fx.luz = Luz(raizFx, "Luz " + A.name, CARMIM_VIVO, 14f, 0f); fx.luz.transform.position = P(A.x, A.z, 2.5f);
                altaresFx.Add(fx);
            }
            for (int i = 0; i < Mapa.REAG.Count; i++) { var r = Forma(PrimitiveType.Sphere, "Reagente " + i, Mat("reagente", new Color(.85f, .12f, .22f), true), raizFx); r.transform.position = P(Mapa.REAG[i].x, Mapa.REAG[i].z, .9f); r.transform.localScale = Vector3.one * .35f; reagentesFx.Add(r); }
            for (int i = 0; i < 6; i++) { var l = Luz(raizFx, "Sinalizador " + i, new Color(1f, .35f, .2f), 12f, 0f); flaresFx.Add(l); }
            lanterna = new GameObject("Lanterna").AddComponent<Light>(); lanterna.type = LightType.Spot; lanterna.range = 22f; lanterna.spotAngle = 42f; lanterna.intensity = 3.5f;
            lanterna.color = new Color(1f, .93f, .78f); lanterna.shadows = LightShadows.Soft; lanterna.enabled = false;
        }

        void LimparVisual()
        {
            foreach (var d in new[] { bonecos, projFx, pickFx, salFx }) { foreach (var go in d.Values) Destroy(go); d.Clear(); }
            foreach (var go in pontosFx) Destroy(go); pontosFx.Clear(); pontosDe = null;
            foreach (var go in pegadasFx) Destroy(go); pegadasFx.Clear();
            foreach (var t in temporarios) Destroy(t.go); temporarios.Clear();
        }

        // ---------- bonecos ----------
        GameObject Boneco(Ator a)
        {
            GameObject b; if (bonecos.TryGetValue(a.id, out b)) return b;
            b = new GameObject(a.name + " (" + a.cls + ")"); b.transform.SetParent(raizFx, false);
            var cor = a.team == "H" ? OURO : CARMIM;
            var corpo = Forma(PrimitiveType.Capsule, "Corpo", Mat("corpo_" + a.team, cor, false), b.transform); corpo.transform.localPosition = new Vector3(0, .9f, 0); corpo.transform.localScale = new Vector3(.8f, .9f, .8f);
            var rosto = Forma(PrimitiveType.Cube, "Rosto", Mat("rosto", OSSO, false), corpo.transform); rosto.transform.localPosition = new Vector3(0, .6f, .42f); rosto.transform.localScale = new Vector3(.55f, .18f, .2f);
            var arma = Forma(PrimitiveType.Cube, "Arma", Mat("arma", new Color(.15f, .12f, .1f), false), corpo.transform); arma.transform.localPosition = new Vector3(.35f, .15f, .5f); arma.transform.localScale = new Vector3(.12f, .12f, .7f);
            var rot = new GameObject("Nome"); rot.transform.SetParent(b.transform, false); rot.transform.localPosition = new Vector3(0, 2.3f, 0);
            var tm = rot.AddComponent<TextMesh>(); tm.text = a.name; tm.characterSize = .12f; tm.fontSize = 40; tm.anchor = TextAnchor.MiddleCenter; tm.color = cor; rot.AddComponent<OlharParaCamera>();
            var fonte = Fonte(); if (fonte != null) { tm.font = fonte; var mr = rot.GetComponent<MeshRenderer>(); if (mr == null) mr = rot.AddComponent<MeshRenderer>(); mr.sharedMaterial = fonte.material; }
            bonecos[a.id] = b; return b;
        }

        static Font fonteNomes;
        static Font Fonte()
        {
            if (fonteNomes != null) return fonteNomes;
#if UNITY_2022_2_OR_NEWER
            fonteNomes = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            fonteNomes = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            return fonteNomes;
        }
        static void Acender(Light l, float intensidade) { l.intensity = intensidade; l.enabled = intensidade > 0f; }

        void AtualizarVisual(float dt)
        {
            for (int i = temporarios.Count - 1; i >= 0; i--)
            {
                var t = temporarios[i]; t.t += dt;
                if (t.linha != null) { var c = t.linha.startColor; c.a = Mathf.Clamp01(1f - t.t / t.dur); t.linha.startColor = c; t.linha.endColor = c; }
                if (t.t >= t.dur) { Destroy(t.go); temporarios.RemoveAt(i); }
            }
            if (g == null) return;
            var m = Eu(); string papel = MeuPapel(); bool jogando = g.phase == "play";
            raizFx.gameObject.SetActive(jogando);
            if (!jogando) { lanterna.enabled = false; return; }

            foreach (var a in g.actors)
            {
                var b = Boneco(a);
                bool visivel = a.st != "dead" && a != m;
                if (visivel && a.team != papel && a.veuT > 0 && m != null && Sim.Dist(m, a) > 8) visivel = false; // Véu: some de longe
                b.SetActive(visivel); if (!visivel) continue;
                b.transform.position = P(a.x, a.z);
                b.transform.rotation = MapaDados.Olhar((float)a.yaw) * (a.st == "down" ? Quaternion.Euler(80f, 0f, 0f) : Quaternion.identity);
                b.transform.Find("Nome").gameObject.SetActive(a.team == papel);
            }

            for (int i = 0; i < g.altars.Count && i < altaresFx.Count; i++) AtualizarAltar(g.altars[i], altaresFx[i], papel, m);
            for (int i = 0; i < g.reagents.Count && i < reagentesFx.Count; i++) reagentesFx[i].SetActive(g.reagents[i].has);
            for (int i = 0; i < g.flares.Count && i < flaresFx.Count; i++) { var f = g.flares[i]; Acender(flaresFx[i], f.t > 0 ? 3f * Mathf.Min(1f, (float)f.t / 2f) : 0f); flaresFx[i].transform.position = P(f.x, f.z, .4f); }

            Sincronizar(projFx, g.proj, q => q.id, q => { var s = Forma(PrimitiveType.Sphere, "Sigilo", Mat("sigilo", q.team == "C" ? CARMIM_VIVO : OURO, true), raizFx); s.transform.localScale = Vector3.one * .28f; return s; },
                (q, go) => go.transform.position = new Vector3((float)q.x, (float)q.y, (float)-q.z));
            Sincronizar(pickFx, g.pickups, q => q.id, q => { var s = Forma(PrimitiveType.Sphere, "Coletável " + q.kind, Mat("pick_" + q.kind, q.kind == "reag" ? new Color(.85f, .12f, .22f) : q.kind == "ess" ? ROXO_VIVO : OURO, true), raizFx); s.transform.localScale = Vector3.one * .3f; return s; },
                (q, go) => go.transform.position = P(q.x, q.z, .35f + .08f * Mathf.Sin(Time.time * 3f)));
            Sincronizar(salFx, papel == "H" ? g.sal : new List<Sal>(), q => q.id, q => { var s = Forma(PrimitiveType.Cylinder, "Sal", Mat("sal", OSSO, true), raizFx); s.transform.localScale = new Vector3(2.4f, .005f, 2.4f); return s; },
                (q, go) => go.transform.position = P(q.x, q.z, .02f));

            if (pontosDe != g.pontos)
            {
                foreach (var go in pontosFx) Destroy(go); pontosFx.Clear(); pontosDe = g.pontos;
                foreach (var p in g.pontos)
                {
                    var cor = p.tipo == "pista" ? OURO : p.tipo == "sentinela" ? new Color(1f, .6f, .2f) : p.tipo == "erva" ? new Color(.3f, .9f, .75f) : CINZA;
                    var go = Forma(PrimitiveType.Cube, "Ponto " + p.tipo, Mat("ponto_" + p.tipo, cor, true), raizFx); go.transform.position = P(p.x, p.z, .5f); go.transform.localScale = new Vector3(.3f, 1f, .3f);
                    if (p.tipo == "sentinela") { var l = Luz(go.transform, "Chama", new Color(1f, .55f, .2f), 10f, 0f); l.transform.localPosition = new Vector3(0, 1.2f, 0); }
                    pontosFx.Add(go);
                }
            }
            for (int i = 0; i < g.pontos.Count && i < pontosFx.Count; i++)
            {
                var p = g.pontos[i]; bool meu = papel == "H" ? (p.tipo == "pista" || p.tipo == "sentinela") : (p.tipo == "erva" || p.tipo == "tumulo");
                pontosFx[i].SetActive(p.tipo == "sentinela" ? (meu || p.acesa) : (meu && p.ativo));
                var l = pontosFx[i].GetComponentInChildren<Light>(true); if (l != null) Acender(l, p.acesa ? 2.2f : 0f);
            }

            // pegadas: só o Rastreador vê
            int np = m != null && m.cls == "rastreador" ? g.pegadas.Count : 0;
            while (pegadasFx.Count < np) { var s = Forma(PrimitiveType.Cube, "Pegada", Mat("pegada", new Color(.55f, .15f, .2f), true), raizFx); s.transform.localScale = new Vector3(.12f, .01f, .26f); pegadasFx.Add(s); }
            for (int i = 0; i < pegadasFx.Count; i++)
            {
                bool on = i < np; pegadasFx[i].SetActive(on); if (!on) continue; var q = g.pegadas[i];
                pegadasFx[i].transform.position = P(q.x, q.z, .015f); pegadasFx[i].transform.rotation = Quaternion.Euler(0f, (float)(-q.yaw * Mathf.Rad2Deg), 0f); // a pegada é simétrica: -yaw basta
            }

            lanterna.enabled = m != null && m.lantern && m.st == "alive";
            if (lanterna.enabled && cam != null) { lanterna.transform.position = cam.transform.position + cam.transform.right * .2f - cam.transform.up * .1f; lanterna.transform.rotation = cam.transform.rotation; }
        }

        // o que o seu lado sabe de cada altar (mesma regra da planta do protótipo)
        void AtualizarAltar(Altar A, AltarFx fx, string papel, Ator m)
        {
            Color anel = CINZA; bool feixe = false; float luz = 0f; Color corLuz = CARMIM_VIVO; float t = Time.time;
            if (A.state == "fenda") { anel = new Color(.07f, .03f, .1f); luz = 2.5f; corLuz = new Color(.6f, .05f, .15f); }
            else if (A.state == "farol") { anel = new Color(.95f, .84f, .56f); luz = 4f; corLuz = new Color(1f, .9f, .6f); feixe = true; }
            else if (papel == "C")
            {
                if (A.state == "active") { anel = CARMIM_VIVO; feixe = true; luz = 3f + Mathf.Sin(t * 6f); }
                else if (A.chosen) anel = A.state == "awake" ? ROXO_VIVO : CARMIM;
                else if (A.decoy) anel = CINZA * 1.4f;
                else anel = CINZA * .6f;
            }
            else
            {
                string k = g.know[A.i];
                if (A.state == "active" && A.localized) { anel = CARMIM_VIVO; feixe = true; luz = 3f + Mathf.Sin(t * 6f); }
                else if (k == "confirmado") anel = ROXO_VIVO; else if (k == "desperto") anel = ROXO; else if (k == "chamariz") anel = CINZA * 1.4f; else if (k == "limpo") anel = CINZA * .4f; else anel = CINZA * .7f;
            }
            Cor(fx.rAnel.sharedMaterial, anel);
            fx.feixe.SetActive(feixe); if (feixe) Cor(fx.rFeixe.sharedMaterial, A.state == "farol" ? new Color(1f, .9f, .6f) : CARMIM_VIVO);
            Acender(fx.luz, luz); fx.luz.color = corLuz;
        }

        void Sincronizar<T>(Dictionary<int, GameObject> vivos, List<T> lista, System.Func<T, int> id, System.Func<T, GameObject> criar, System.Action<T, GameObject> mover)
        {
            var presentes = new HashSet<int>();
            foreach (var q in lista) { int k = id(q); presentes.Add(k); GameObject go; if (!vivos.TryGetValue(k, out go)) { go = criar(q); vivos[k] = go; } mover(q, go); }
            var sair = new List<int>(); foreach (var k in vivos.Keys) if (!presentes.Contains(k)) sair.Add(k);
            foreach (var k in sair) { Destroy(vivos[k]); vivos.Remove(k); }
        }

        void Tracer(Evento e)
        {
            var go = new GameObject("Tiro"); go.transform.SetParent(raizFx, false); var l = go.AddComponent<LineRenderer>();
            l.positionCount = 2; l.SetPosition(0, new Vector3((float)e.Num("x0"), (float)e.Num("y0"), (float)-e.Num("z0"))); l.SetPosition(1, new Vector3((float)e.Num("x1"), (float)e.Num("y1"), (float)-e.Num("z1")));
            l.startWidth = .03f; l.endWidth = .015f; l.sharedMaterial = Mat("tiro", new Color(1f, .85f, .5f), true); l.startColor = l.endColor = new Color(1f, .85f, .5f, 1f);
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            temporarios.Add(new Temporario { go = go, dur = .09f, linha = l });
        }
        void Estouro(Evento e)
        {
            var s = Forma(PrimitiveType.Sphere, "Estouro", Mat("estouro_" + e.Str("team"), e.Str("team") == "C" ? CARMIM_VIVO : OURO, true), raizFx);
            s.transform.position = new Vector3((float)e.Num("x"), (float)e.Num("y"), (float)-e.Num("z")); s.transform.localScale = Vector3.one * .6f;
            temporarios.Add(new Temporario { go = s, dur = .15f });
        }
    }
}
