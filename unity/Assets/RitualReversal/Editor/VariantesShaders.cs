// Variantes dos shaders no build. Os materiais do jogo são criados por código no Play (Mat.Padrao), então nenhum material do
// projeto liga _NORMALMAP, _EMISSION, _ALPHATEST_ON, _SURFACE_TYPE_TRANSPARENT ou _DETAIL_MULX2. No editor isso não faz falta
// (ele compila a variante na hora), mas o build só leva as variantes de shader_feature que algum material incluído usa: o resto
// é jogado fora e o jogo compilado desenha o chão sem o detalhe de pedra, a folhagem sem recorte, os vidros opacos.
// Antes de cada build este script grava em Resources/Variantes um material por combinação de palavras-chave, para o
// RitualReversal/Ilustrado e para o URP/Lit (a reserva com PlayerPrefs "rr-ilustrado" = 0); tudo o que está em Resources vai
// no build, e com eles as variantes. Também dá para gerar pelo menu "Ritual Reversal > Gerar variantes dos shaders".
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RitualReversal.Ferramentas
{
    public class VariantesShaders : IPreprocessBuildWithReport
    {
        const string PASTA = "Assets/RitualReversal/Resources/Variantes";
        static readonly string[] SHADERS = { "RitualReversal/Ilustrado", "Universal Render Pipeline/Lit" };
        // as palavras-chave que o Materiais.cs, a mata (chão), as skins e os personagens ligam em tempo de jogo
        static readonly string[] CHAVES = { "_NORMALMAP", "_EMISSION", "_ALPHATEST_ON", "_SURFACE_TYPE_TRANSPARENT", "_DETAIL_MULX2" };

        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report) { Gerar(); }

        [MenuItem("Ritual Reversal/Gerar variantes dos shaders")]
        public static void Gerar()
        {
            if (!AssetDatabase.IsValidFolder(PASTA)) AssetDatabase.CreateFolder("Assets/RitualReversal/Resources", "Variantes");
            int n = 0;
            foreach (var nome in SHADERS)
            {
                var sh = Shader.Find(nome); if (sh == null) { Debug.LogWarning("Variantes: shader não encontrado: " + nome); continue; }
                string prefixo = nome.Replace("/", "-").Replace(" ", "");
                for (int bits = 0; bits < 1 << CHAVES.Length; bits++)
                {
                    string arq = PASTA + "/" + prefixo + "-" + bits.ToString("00") + ".mat";
                    var m = AssetDatabase.LoadAssetAtPath<Material>(arq); bool novo = m == null;
                    if (novo) m = new Material(sh); else m.shader = sh;
                    for (int k = 0; k < CHAVES.Length; k++) { if ((bits & (1 << k)) != 0) m.EnableKeyword(CHAVES[k]); else m.DisableKeyword(CHAVES[k]); }
                    // o transparente do URP/Lit também depende do _Surface; o Ilustrado só olha a palavra-chave
                    if ((bits & 8) != 0 && m.HasProperty("_Surface")) m.SetFloat("_Surface", 1);
                    if ((bits & 4) != 0 && m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1);
                    if (novo) AssetDatabase.CreateAsset(m, arq); else EditorUtility.SetDirty(m);
                    n++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Variantes dos shaders: " + n + " materiais em " + PASTA);
        }
    }
}
