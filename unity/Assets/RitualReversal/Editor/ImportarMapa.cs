// Menu "Ritual Reversal > Importar mapa (blockout)": monta a planta do protótipo em caixas cinzas,
// com a MESMA colisão de shared/sim.js, põe um jogador em primeira pessoa no acampamento dos Caçadores
// e um objeto "Partida" que joga a partida contra bots (PartidaLocal).
// A arte entra depois, por cima das caixas; a colisão deve continuar igual.
// Rodar de novo apaga o "Mapa Ritual Reversal" anterior e monta outro (Ctrl+Z desfaz).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RitualReversal.Ferramentas
{
    public static class ImportarMapa
    {
        const string DADOS = "Assets/RitualReversal/Resources/mapa.json";
        const string MATERIAIS = "Assets/RitualReversal/Materiais";
        const string TEXTURAS = "Assets/RitualReversal/Texturas";
        const string RAIZ = "Mapa Ritual Reversal";

        static readonly Dictionary<string, Color> CORES = new Dictionary<string, Color>
        {
            {"limite", new Color(.22f,.26f,.2f)}, {"pedra", new Color(.52f,.5f,.47f)}, {"torre", new Color(.45f,.43f,.4f)},
            {"cripta", new Color(.32f,.31f,.33f)}, {"biombo", new Color(.42f,.3f,.2f)}, {"tumba", new Color(.7f,.7f,.68f)},
            {"coluna", new Color(.55f,.53f,.5f)}, {"coluna_caida", new Color(.5f,.48f,.45f)}, {"escombro", new Color(.4f,.39f,.37f)},
            {"poco", new Color(.45f,.45f,.47f)}, {"sebe", new Color(.18f,.3f,.17f)}, {"ossos", new Color(.62f,.57f,.47f)},
            {"arvore", new Color(.2f,.15f,.11f)}, {"ruina", new Color(.36f,.36f,.33f)}, {"pilar_ruina", new Color(.4f,.4f,.36f)},
            {"estatua", new Color(.55f,.55f,.5f)}, {"tronco", new Color(.3f,.22f,.15f)}, {"raiz", new Color(.3f,.22f,.15f)},
            {"rocha", new Color(.35f,.36f,.32f)}, {"espinheiro", new Color(.1f,.16f,.08f)}, {"tenda", new Color(.4f,.33f,.22f)},
            {"caixote", new Color(.45f,.32f,.2f)}, {"menir", new Color(.47f,.46f,.42f)}, {"carvalho", new Color(.28f,.2f,.14f)},
            {"madeira", new Color(.4f,.3f,.22f)}, {"lapide", new Color(.6f,.6f,.58f)}, {"mausoleu", new Color(.35f,.33f,.3f)},
            {"marco", new Color(.4f,.3f,.2f)}, {"santuario", new Color(.42f,.42f,.4f)},
        };
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        // cena só com a câmera e a Partida: o mundo inteiro do protótipo é montado por código no Play
        [MenuItem("Ritual Reversal/Criar cena do jogo")]
        public static void CriarCena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var c = new GameObject("Câmera"); c.tag = "MainCamera"; var camera = c.AddComponent<Camera>(); camera.nearClipPlane = .05f; camera.farClipPlane = 220; c.AddComponent<AudioListener>();
            c.transform.position = new Vector3(6, 3.4f, 9);
            var raiz = new GameObject(RAIZ); var partida = new GameObject("Partida"); partida.transform.SetParent(raiz.transform); partida.AddComponent<PartidaLocal>();
            EditorSceneManager.MarkSceneDirty(cena); Selection.activeGameObject = partida;
            EditorUtility.DisplayDialog("Ritual Reversal", "Cena criada. Salve (Ctrl+S) e aperte Play.", "OK");
        }

        [MenuItem("Ritual Reversal/Importar mapa (blockout)")]
        public static void Importar()
        {
            string caminho = File.Exists(DADOS) ? DADOS : EditorUtility.OpenFilePanel("Escolha o mapa.json do Ritual Reversal", "", "json");
            if (string.IsNullOrEmpty(caminho)) return;
            var M = JsonUtility.FromJson<MapaDados>(File.ReadAllText(caminho));
            if (M == null || M.pecas == null) { EditorUtility.DisplayDialog("Ritual Reversal", "Não consegui ler o mapa.json.", "OK"); return; }
            cache.Clear(); malhas.Clear();

            var antigo = GameObject.Find(RAIZ); if (antigo != null) Undo.DestroyObjectImmediate(antigo);
            var raiz = new GameObject(RAIZ); Undo.RegisterCreatedObjectUndo(raiz, "Importar mapa Ritual Reversal");
            var estatico = Grupo(raiz, "Estático"); // tudo aqui é marcado como estático (junta desenhos)

            try
            {
                EditorUtility.DisplayProgressBar("Ritual Reversal", "Chão e trilhas", 0f);
                Chao(M, estatico);
                EditorUtility.DisplayProgressBar("Ritual Reversal", "Peças de colisão", .2f);
                Pecas(M, Grupo(estatico, "Peças"));
                EditorUtility.DisplayProgressBar("Ritual Reversal", "Árvores", .6f);
                Arvores(M, Grupo(estatico, "Árvores"));
                Catedral(M, Grupo(estatico, "Pilares e bancos"));
                EditorUtility.DisplayProgressBar("Ritual Reversal", "Altares, pontos e luzes", .85f);
                Objetivos(M, Grupo(raiz, "Objetivos"));
                Luzes(M, Grupo(raiz, "Luzes"));
                Jogador(M, raiz);
                // partida contra bots: roda a simulação portada (Scripts/Simulacao). Desative este objeto para só passear pelo mapa.
                var partida = new GameObject("Partida"); partida.transform.SetParent(raiz.transform); partida.AddComponent<PartidaLocal>();
                foreach (Transform t in estatico.GetComponentsInChildren<Transform>())
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            }
            finally { EditorUtility.ClearProgressBar(); }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = raiz;
            Debug.Log($"Ritual Reversal: {M.pecas.Length} peças, {M.arvores.Length} árvores, {M.altares.Length} altares. Aperte Play para jogar contra os bots (ou desative o objeto Partida para só andar pelo mapa).");
        }

        // ---------- partes do mapa ----------
        static void Chao(MapaDados M, GameObject pai)
        {
            var h = M.limites.meiaLargura; var ct = M.limites.catedral;
            Caixa(pai, "Chão da floresta", 0, 0, -.1f, h.x * 2 + 80, .2f, h.z * 2 + 80, Mat("chao", new Color(.12f, .16f, .1f)), true); // o chão precisa colidir, senão o jogador cai
            Caixa(pai, "Piso da catedral", (ct.x1 + ct.x2) / 2, (ct.z1 + ct.z2) / 2, -.09f, ct.x2 - ct.x1, .2f, ct.z2 - ct.z1, Mat("piso", new Color(.3f, .29f, .28f)), true);
            var tr = Grupo(pai, "Trilhas");
            foreach (var T in M.trilhas)
                for (int i = 1; i < T.pontos.Length; i++)
                {
                    Vector3 a = MapaDados.Pos(T.pontos[i - 1].x, T.pontos[i - 1].z), b = MapaDados.Pos(T.pontos[i].x, T.pontos[i].z);
                    float L = Vector3.Distance(a, b); if (L < .01f) continue;
                    var g = Prim(PrimitiveType.Cube, tr, "Trilha", Mat(T.estreita ? "trilha_estreita" : "trilha", T.estreita ? new Color(.2f, .17f, .12f) : new Color(.33f, .27f, .18f)), false);
                    g.transform.position = (a + b) / 2 + Vector3.up * (T.estreita ? .012f : .016f);
                    g.transform.rotation = Quaternion.LookRotation(b - a);
                    g.transform.localScale = new Vector3(T.largura, .02f, L + T.largura * .5f); Texturizar(g);
                }
            var cl = Grupo(pai, "Clareiras");
            foreach (var c in M.clareiras)
            {
                var g = Prim(PrimitiveType.Cylinder, cl, "Clareira", Mat("clareira", new Color(.2f, .25f, .16f), new Vector2(12, 12)), false);
                g.transform.position = MapaDados.Pos(c.x, c.z, .008f); g.transform.localScale = new Vector3(c.raio * 2, .004f, c.raio * 2);
            }
        }

        static void Pecas(MapaDados M, GameObject pai)
        {
            var grupos = new Dictionary<string, GameObject>();
            foreach (var p in M.pecas)
            {
                if (p.tipo == "arvore_f") continue; // as árvores da mata vêm de M.arvores, com o mesmo tamanho de colisão
                if (!grupos.TryGetValue(p.tipo, out var g)) grupos[p.tipo] = g = Grupo(pai, p.tipo);
                var cor = CORES.TryGetValue(p.tipo, out var c) ? c : new Color(.5f, .5f, .5f);
                Caixa(g, p.tipo, (p.x1 + p.x2) / 2, (p.z1 + p.z2) / 2, p.h / 2, p.x2 - p.x1, p.h, p.z2 - p.z1, Mat(p.tipo, cor), true);
            }
        }

        static void Arvores(MapaDados M, GameObject pai)
        {
            var casca = Mat("casca", new Color(.24f, .19f, .14f), new Vector2(3, 6)); var copa = Mat("copa", new Color(.08f, .12f, .07f), new Vector2(5, 2));
            foreach (var q in M.arvores)
            {
                float H = (q.tipo == "alta" ? 18f : 12f) * q.escala;
                var t = Prim(PrimitiveType.Cylinder, pai, "Árvore " + q.tipo, casca, false);
                t.transform.position = MapaDados.Pos(q.x, q.z, H / 2);
                t.transform.localScale = new Vector3(q.raio * 2, H / 2, q.raio * 2);
                // colisão igual à do protótipo: caixa alinhada aos eixos, meia largura de 0,85 do raio, 12 m de altura
                var bc = t.AddComponent<BoxCollider>(); bc.size = new Vector3(.85f, 12f / (H / 2), .85f); bc.center = new Vector3(0, (6f - H / 2) / (H / 2), 0);
                if (q.tipo == "morta") continue;
                var c = Prim(PrimitiveType.Sphere, t, "Copa", copa, false);
                c.transform.localScale = new Vector3(3.5f, .45f, 3.5f); c.transform.localPosition = new Vector3(0, .95f, 0);
            }
        }

        static void Catedral(MapaDados M, GameObject pai)
        {
            foreach (var p in M.pilares)
            {
                var g = Prim(PrimitiveType.Cylinder, pai, "Pilar", Mat("pilar", CORES["coluna"], new Vector2(3, 5)), true);
                g.transform.position = MapaDados.Pos(p.x, p.z, p.h / 2); g.transform.localScale = new Vector3(p.raio * 2, p.h / 2, p.raio * 2);
            }
            foreach (var b in M.bancos)
                Caixa(pai, "Bancos", (b.x1 + b.x2) / 2, (b.z1 + b.z2) / 2, b.h / 2, Mathf.Abs(b.x2 - b.x1), b.h, Mathf.Abs(b.z2 - b.z1), Mat("banco", new Color(.35f, .25f, .17f)), true);
        }

        static void Objetivos(MapaDados M, GameObject pai)
        {
            foreach (var A in M.altares)
            {
                var g = Grupo(pai, "Altar " + A.nome); g.transform.position = MapaDados.Pos(A.x, A.z);
                Caixa(g, "Pedra do altar", A.x, A.z, .55f, 2.6f, 1.1f, 1.3f, Mat("altar", new Color(.8f, .78f, .74f)), true);
                var circulo = Prim(PrimitiveType.Cylinder, g, "Círculo (3 m)", Mat("circulo", new Color(.35f, .15f, .5f)), false);
                circulo.transform.position = MapaDados.Pos(A.x, A.z, .02f); circulo.transform.localScale = new Vector3(6f, .01f, 6f);
                Rotulo(g, A.nome, MapaDados.Pos(A.x, A.z, 3.5f), new Color(.9f, .7f, 1f), .35f);
            }
            var rg = Grupo(pai, "Reagentes (Claustro)");
            foreach (var r in M.reagentes) { var s = Prim(PrimitiveType.Sphere, rg, "Reagente", Mat("reagente", new Color(.8f, .1f, .2f)), false); s.transform.position = MapaDados.Pos(r.x, r.z, 1f); s.transform.localScale = Vector3.one * .4f; }
            var mg = Grupo(pai, "Mercadores");
            foreach (var n in M.mercadores)
            {
                var c = Prim(PrimitiveType.Capsule, mg, n.nome, Mat("mercador", new Color(.85f, .7f, .35f)), false);
                c.transform.position = MapaDados.Pos(n.x, n.z, 1f); Rotulo(mg, n.nome, MapaDados.Pos(n.x, n.z, 2.6f), new Color(.95f, .85f, .5f), .25f);
            }
            var pt = Grupo(pai, "Pontos de tarefa");
            System.Action<PontoXZ[], string, Color> Pontos = (lista, nome, cor) => { if (lista == null) return; foreach (var p in lista) { var s = Prim(PrimitiveType.Cube, pt, nome, Mat("ponto_" + nome, cor), false); s.transform.position = MapaDados.Pos(p.x, p.z, .5f); s.transform.localScale = new Vector3(.4f, 1f, .4f); } };
            Pontos(M.pontosDeTarefa.pista, "pista", new Color(.95f, .8f, .4f)); Pontos(M.pontosDeTarefa.sentinela, "sentinela", new Color(1f, .6f, .2f));
            Pontos(M.pontosDeTarefa.erva, "erva", new Color(.3f, .9f, .75f)); Pontos(M.pontosDeTarefa.tumulo, "tumulo", new Color(.5f, .5f, .5f));
            var lg = Grupo(pai, "Nomes dos lugares");
            foreach (var L in M.lugares) Rotulo(lg, L.nome, MapaDados.Pos(L.x, L.z, 4f), new Color(.75f, .72f, .65f), .4f);
            var sp = Grupo(pai, "Spawns");
            var h = new GameObject("Spawn Caçadores"); h.transform.SetParent(sp.transform); h.transform.SetPositionAndRotation(MapaDados.Pos(M.spawns.H.x, M.spawns.H.z), MapaDados.Olhar(M.spawns.H.yaw));
            var c2 = new GameObject("Spawn Cultistas"); c2.transform.SetParent(sp.transform); c2.transform.SetPositionAndRotation(MapaDados.Pos(M.spawns.C.x, M.spawns.C.z), MapaDados.Olhar(M.spawns.C.yaw));
        }

        static void Luzes(MapaDados M, GameObject pai)
        {
            // noite: lua fraca e azulada, névoa densa como no protótipo (FogExp2 0,026)
            foreach (var l in Todos<Light>()) if (l.type == LightType.Directional && !l.transform.IsChildOf(pai.transform.root)) { Undo.RecordObject(l.gameObject, "Lua"); l.gameObject.SetActive(false); }
            var lua = new GameObject("Lua"); lua.transform.SetParent(pai.transform); lua.transform.rotation = Quaternion.LookRotation(new Vector3(-18f, -40f, 26f)); // no protótipo a lua vem de (18, 40, 26), com z invertido
            var ll = lua.AddComponent<Light>(); ll.type = LightType.Directional; ll.color = new Color(.54f, .64f, .84f); ll.intensity = .35f; ll.shadows = LightShadows.Soft;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .026f; RenderSettings.fogColor = new Color(.043f, .05f, .078f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.12f, .14f, .2f);
            foreach (var f in M.luzes)
            {
                var g = new GameObject("Luz " + f.tipo); g.transform.SetParent(pai.transform); g.transform.position = MapaDados.Pos(f.x, f.z, f.tipo == "fogueira" ? 1.4f : 2.1f);
                var L = g.AddComponent<Light>(); L.type = LightType.Point; L.range = f.tipo == "fogueira" ? 20f : 14f; L.intensity = f.tipo == "fogueira" ? 3f : 2f;
                L.color = f.tipo == "braseiro" ? new Color(1f, .3f, .23f) : new Color(1f, .55f, .21f);
            }
        }

        static void Jogador(MapaDados M, GameObject raiz)
        {
            foreach (var cam in Todos<Camera>()) if (!cam.transform.IsChildOf(raiz.transform)) { Undo.RecordObject(cam.gameObject, "Câmera"); cam.gameObject.SetActive(false); }
            var j = new GameObject("Jogador"); j.transform.SetParent(raiz.transform);
            j.transform.SetPositionAndRotation(MapaDados.Pos(M.spawns.H.x, M.spawns.H.z, .1f), MapaDados.Olhar(M.spawns.H.yaw));
            var cc = j.AddComponent<CharacterController>(); cc.radius = .4f; cc.height = 1.8f; cc.center = new Vector3(0, .9f, 0); cc.stepOffset = .3f;
            j.AddComponent<JogadorFPS>();
            var cab = new GameObject("Cabeça"); cab.transform.SetParent(j.transform, false); cab.transform.localPosition = new Vector3(0, 1.65f, 0); cab.tag = "MainCamera";
            var c = cab.AddComponent<Camera>(); c.nearClipPlane = .05f; c.farClipPlane = 220f; c.fieldOfView = 72f; c.backgroundColor = new Color(.02f, .027f, .047f); c.clearFlags = CameraClearFlags.SolidColor;
            cab.AddComponent<AudioListener>();
        }

        // ---------- ajudantes ----------
        static T[] Todos<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }

        static GameObject Grupo(GameObject pai, string nome) { var g = new GameObject(nome); g.transform.SetParent(pai.transform, false); return g; }

        static GameObject Prim(PrimitiveType tipo, GameObject pai, string nome, Material mat, bool colide)
        {
            var g = GameObject.CreatePrimitive(tipo); g.name = nome; g.transform.SetParent(pai.transform, true);
            if (!colide) Object.DestroyImmediate(g.GetComponent<Collider>());
            g.GetComponent<Renderer>().sharedMaterial = mat; return g;
        }

        // caixa com o centro em (x, z) do protótipo, base no chão quando y = altura / 2
        static GameObject Caixa(GameObject pai, string nome, float x, float z, float y, float lx, float ly, float lz, Material mat, bool colide)
        {
            var g = Prim(PrimitiveType.Cube, pai, nome, mat, colide);
            g.transform.position = MapaDados.Pos(x, z, y); g.transform.localScale = new Vector3(lx, ly, lz); Texturizar(g); return g;
        }

        // Cubo com coordenadas de textura em metros (uma repetição a cada 2 m, em qualquer tamanho de caixa), para a
        // textura não esticar numa parede comprida. Paredes: u na horizontal, v na vertical; tampo e base: x e z.
        static readonly Dictionary<string, Mesh> malhas = new Dictionary<string, Mesh>();
        static void Texturizar(GameObject g)
        {
            var s = g.transform.localScale; string k = $"{s.x:0.0}x{s.y:0.0}x{s.z:0.0}";
            if (!malhas.TryGetValue(k, out var m))
            {
                m = new Mesh { name = "Caixa " + k }; var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
                System.Action<Vector3, Vector3, Vector3> Face = (normal, a, b) => // b × a = normal: ordem horária vista de fora (frente no Unity)
                {
                    int i0 = v.Count; Vector3 c = normal * .5f;
                    foreach (var q in new[] { c - a * .5f - b * .5f, c - a * .5f + b * .5f, c + a * .5f + b * .5f, c + a * .5f - b * .5f })
                    {
                        v.Add(q); n.Add(normal);
                        Vector2 t = normal.y != 0 ? new Vector2(q.x * s.x, q.z * s.z) : normal.x != 0 ? new Vector2(q.z * s.z, q.y * s.y) : new Vector2(q.x * s.x, q.y * s.y);
                        uv.Add(t / 2f);
                    }
                    tri.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
                };
                Face(Vector3.up, Vector3.right, Vector3.forward); Face(Vector3.down, Vector3.left, Vector3.forward);
                Face(Vector3.right, Vector3.forward, Vector3.up); Face(Vector3.left, Vector3.back, Vector3.up);
                Face(Vector3.forward, Vector3.up, Vector3.right); Face(Vector3.back, Vector3.down, Vector3.right);
                m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds(); m.RecalculateTangents();
                malhas[k] = m;
            }
            g.GetComponent<MeshFilter>().sharedMesh = m;
        }

        static Texture2D Textura(string padrao)
        {
            string arq = $"{TEXTURAS}/{padrao}.asset";
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(arq); if (t != null) return t;
            if (!AssetDatabase.IsValidFolder(TEXTURAS)) AssetDatabase.CreateFolder("Assets/RitualReversal", "Texturas");
            t = GerarTextura.Criar(padrao, 256); AssetDatabase.CreateAsset(t, arq); return t;
        }

        static void Rotulo(GameObject pai, string texto, Vector3 pos, Color cor, float tamanho)
        {
            var g = new GameObject("Rótulo " + texto); g.transform.SetParent(pai.transform); g.transform.position = pos;
            var tm = g.AddComponent<TextMesh>(); tm.text = texto; tm.color = cor; tm.characterSize = tamanho; tm.fontSize = 48; tm.anchor = TextAnchor.MiddleCenter;
#if UNITY_2022_2_OR_NEWER
            var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            var fonte = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            var mr = g.GetComponent<MeshRenderer>(); if (mr == null) mr = g.AddComponent<MeshRenderer>();
            if (fonte) { tm.font = fonte; mr.sharedMaterial = fonte.material; }
            g.AddComponent<OlharParaCamera>();
        }

        static Material Mat(string nome, Color cor, Vector2? repeticao = null)
        {
            if (cache.TryGetValue(nome, out var m)) return m;
            if (!AssetDatabase.IsValidFolder(MATERIAIS)) AssetDatabase.CreateFolder("Assets/RitualReversal", "Materiais");
            string arq = $"{MATERIAIS}/{nome}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(arq);
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit"); // projeto URP (o padrão do Unity 6)
                if (sh == null) sh = Shader.Find("HDRP/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                m = new Material(sh); m.enableInstancing = true; AssetDatabase.CreateAsset(m, arq);
            }
            // marcadores (altar, reagentes, mercadores, pontos) ficam lisos; o resto ganha textura e um tom mais claro,
            // porque a textura escurece a cor em média
            bool lisa = nome.StartsWith("ponto_") || nome == "circulo" || nome == "reagente" || nome == "mercador";
            Texture2D tex = lisa ? null : Textura(GerarTextura.PadraoDe(nome)); if (tex != null) cor = new Color(Mathf.Min(1, cor.r * 1.35f), Mathf.Min(1, cor.g * 1.35f), Mathf.Min(1, cor.b * 1.35f));
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", cor); else m.color = cor;
            var rep = repeticao ?? Vector2.one;
            foreach (var p in new[] { "_BaseMap", "_MainTex" }) if (m.HasProperty(p)) { m.SetTexture(p, tex); m.SetTextureScale(p, rep); }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .12f); // pedra e madeira foscas
            EditorUtility.SetDirty(m); cache[nome] = m; return m;
        }
    }
}
