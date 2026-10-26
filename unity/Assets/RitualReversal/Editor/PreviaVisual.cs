// Menu "Ritual Reversal > Montar o visual na cena": o mundo que o Play monta por código (Mundo.Construir: catedral,
// pontes, mata, Asilo, altares, céu, luzes e névoa) montado no editor e gravado na cena, para ver e mexer sem apertar Play.
// As malhas, os materiais e as texturas são criados na memória; aqui viram cópias num arquivo só por mapa
// (Assets/RitualReversal/Previa/<mapa>.asset), senão sumiriam ao fechar a cena. A prévia some no Play e fica fora do
// jogo compilado (PreviaNoEditor, etiqueta EditorOnly). "Tirar o visual da cena" apaga a prévia e devolve as luzes, a névoa
// e a luz ambiente de antes. O contorno a nanquim e o pós-processamento são da câmera do jogo: só aparecem no Play.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RitualReversal.Visual;

namespace RitualReversal.Ferramentas
{
    public static class PreviaVisual
    {
        const string PASTA = "Assets/RitualReversal/Previa";

        [MenuItem("Ritual Reversal/Montar o visual na cena/Catedral")]
        static void Catedral() { Montar("catedral"); }
        [MenuItem("Ritual Reversal/Montar o visual na cena/Asilo Blackwater")]
        static void Asilo() { Montar("asilo"); }
        [MenuItem("Ritual Reversal/Tirar o visual da cena")]
        static void Tirar() { if (TirarPrevias() == 0) EditorUtility.DisplayDialog("Ritual Reversal", "Não há visual montado nesta cena.", "OK"); }

        static void Montar(string mapa)
        {
            if (Application.isPlaying) { EditorUtility.DisplayDialog("Ritual Reversal", "Saia do Play antes de montar o visual na cena.", "OK"); return; }
            TirarPrevias();   // uma prévia por vez (e a cena volta ao que era antes dela)
            var cena = EditorSceneManager.GetActiveScene();
            var raizesAntes = new HashSet<GameObject>(cena.GetRootGameObjects());
            var luzesLigadas = Todos<Light>().Where(l => l.enabled).ToList();
            var P = new GameObject("Visual do mapa (prévia do editor)").AddComponent<PreviaNoEditor>(); P.mapa = mapa;
            P.nevoa = RenderSettings.fog; P.modoNevoa = RenderSettings.fogMode; P.densNevoa = RenderSettings.fogDensity; P.corNevoa = RenderSettings.fogColor;
            P.modoAmbiente = RenderSettings.ambientMode; P.ambCeu = RenderSettings.ambientSkyColor; P.ambMeio = RenderSettings.ambientEquatorColor; P.ambChao = RenderSettings.ambientGroundColor;
            raizesAntes.Add(P.gameObject);
            try
            {
                EditorUtility.DisplayProgressBar("Ritual Reversal", "Montando o mundo (o mesmo do Play)", .1f);
                var mundo = Mundo.Construir(mapa);
                if (Mundo.Atual == mundo) Mundo.Atual = null;
                // o que a montagem criou na raiz da cena (o mundo e a luz da lua) vai para dentro da prévia
                foreach (var go in cena.GetRootGameObjects()) if (!raizesAntes.Contains(go)) go.transform.SetParent(P.transform, true);
                // as luzes direcionais da cena que a montagem apagou (como no Play): voltam quando a prévia sai
                P.luzesDesligadas = luzesLigadas.Where(l => l != null && !l.enabled).ToList();
                // sem scripts dentro da prévia: é só a aparência (no Play quem anima é o mundo da partida)
                foreach (var mb in P.GetComponentsInChildren<MonoBehaviour>(true)) if (!(mb is PreviaNoEditor)) Object.DestroyImmediate(mb);
                EditorUtility.DisplayProgressBar("Ritual Reversal", "Gravando malhas, materiais e texturas", .6f);
                Gravar(P.gameObject, mapa);
            }
            catch (System.Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("Ritual Reversal", "A montagem falhou: " + e.Message, "OK"); }
            finally { EditorUtility.ClearProgressBar(); }
            P.gameObject.tag = "EditorOnly";
            Undo.RegisterCreatedObjectUndo(P.gameObject, "Montar o visual na cena");
            EditorSceneManager.MarkSceneDirty(cena); Selection.activeGameObject = P.gameObject;
            if (SceneView.lastActiveSceneView != null) { SceneView.lastActiveSceneView.sceneViewState.showFog = true; SceneView.lastActiveSceneView.Repaint(); }
            EditorUtility.DisplayDialog("Ritual Reversal", "Visual montado. Salve a cena (Ctrl+S) para guardar.\n\nNo Play a prévia some e a partida monta o mundo dela; o contorno a nanquim e o pós-processamento só aparecem no Play.", "OK");
        }

        // cópias das malhas, materiais e texturas feitos na memória, num arquivo só; a cena passa a apontar para as cópias
        static void Gravar(GameObject raiz, string mapa)
        {
            if (!AssetDatabase.IsValidFolder(PASTA)) AssetDatabase.CreateFolder("Assets/RitualReversal", "Previa");
            string caminho = PASTA + "/" + mapa + ".asset";
            AssetDatabase.DeleteAsset(caminho);
            var principal = new Mesh { name = "prévia " + mapa }; AssetDatabase.CreateAsset(principal, caminho);
            var texs = new Dictionary<Texture, Texture>(); var mats = new Dictionary<Material, Material>(); var malhas = new Dictionary<Mesh, Mesh>();
            System.Func<Texture, Texture> Tex = t =>
            {
                if (t == null || AssetDatabase.Contains(t)) return t; Texture c; if (texs.TryGetValue(t, out c)) return c;
                var t2 = t as Texture2D;
                if (t2 == null || !t2.isReadable) { Debug.LogWarning("Ritual Reversal: a textura " + t.name + " não pôde ser gravada; ela some ao reabrir a cena."); texs[t] = t; return t; }
                c = Object.Instantiate(t2); c.name = t.name; AssetDatabase.AddObjectToAsset(c, caminho); texs[t] = c; return c;
            };
            System.Func<Material, Material> Mat = m =>
            {
                if (m == null || AssetDatabase.Contains(m)) return m; Material c; if (mats.TryGetValue(m, out c)) return c;
                c = new Material(m) { name = m.name };
                foreach (var n in m.GetTexturePropertyNames()) { var t = m.GetTexture(n); if (t != null) c.SetTexture(n, Tex(t)); }
                AssetDatabase.AddObjectToAsset(c, caminho); mats[m] = c; return c;
            };
            System.Func<Mesh, Mesh> Malha = m =>
            {
                if (m == null || AssetDatabase.Contains(m)) return m; Mesh c; if (malhas.TryGetValue(m, out c)) return c;
                c = Object.Instantiate(m); c.name = m.name; AssetDatabase.AddObjectToAsset(c, caminho); malhas[m] = c; return c;
            };
            foreach (var mf in raiz.GetComponentsInChildren<MeshFilter>(true)) mf.sharedMesh = Malha(mf.sharedMesh);
            foreach (var sm in raiz.GetComponentsInChildren<SkinnedMeshRenderer>(true)) sm.sharedMesh = Malha(sm.sharedMesh);
            foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = r.sharedMaterials.Select(Mat).ToArray();
                var pr = r as ParticleSystemRenderer; if (pr != null) { pr.trailMaterial = Mat(pr.trailMaterial); pr.mesh = Malha(pr.mesh); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Ritual Reversal: prévia de " + mapa + " gravada em " + caminho + " (" + malhas.Count + " malhas, " + mats.Count + " materiais, " + texs.Count + " texturas).");
        }

        static T[] Todos<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>(true);
#endif
        }

        // apaga as prévias da cena e devolve as luzes, a névoa e a luz ambiente de antes; diz quantas tirou
        static int TirarPrevias()
        {
            var lista = Todos<PreviaNoEditor>(); if (lista.Length == 0) return 0;
            foreach (var P in lista)
            {
                foreach (var l in P.luzesDesligadas) if (l != null) { Undo.RecordObject(l, "Tirar o visual da cena"); l.enabled = true; }
                RenderSettings.fog = P.nevoa; RenderSettings.fogMode = P.modoNevoa; RenderSettings.fogDensity = P.densNevoa; RenderSettings.fogColor = P.corNevoa;
                RenderSettings.ambientMode = P.modoAmbiente; RenderSettings.ambientSkyColor = P.ambCeu; RenderSettings.ambientEquatorColor = P.ambMeio; RenderSettings.ambientGroundColor = P.ambChao;
                Undo.DestroyObjectImmediate(P.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return lista.Length;
        }
    }
}
