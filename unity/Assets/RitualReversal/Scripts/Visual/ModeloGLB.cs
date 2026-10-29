// Modelos 3D gerados (assets/modelos/fontes.json): o mesmo GLB enxuto que o navegador lê de client/modelos.js, aqui em
// Resources/Modelos/<nome>.bytes. tools/montar-modelos.py grava os dois; o leitor é o mesmo mínimo do modelo() do
// index.html (uma malha: posição, normal, uv, índices e a textura em JPEG). Sem o arquivo, volta null e o mundo usa as
// peças antigas.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public class ModeloGLB
    {
        public Geo geo; public Material mat; public Texture2D tex; public bool alfa;   // alfa: textura PNG com recorte (as folhas da samambaia)
        static readonly Dictionary<string, ModeloGLB> cache = new Dictionary<string, ModeloGLB>();

        public static ModeloGLB Carregar(string nome)
        {
            ModeloGLB m; if (cache.TryGetValue(nome, out m)) return m;
            m = null;
            var ta = Resources.Load<TextAsset>("Modelos/" + nome);
            if (ta != null && ta.bytes != null)
            {
                try { m = Ler(ta.bytes, nome); }
                catch (Exception e) { Debug.LogWarning("modelo " + nome + ": " + e.Message); m = null; }
            }
            return cache[nome] = m;
        }

        static ModeloGLB Ler(byte[] b, string nome)
        {
            int jl = BitConverter.ToInt32(b, 12), bo = 20 + jl + 8;
            var J = (Dictionary<string, object>)Json.Ler(Encoding.UTF8.GetString(b, 20, jl));
            Func<object, int> I = o => (int)(double)o;
            var vistas = (List<object>)J["bufferViews"]; var acessores = (List<object>)J["accessors"];
            Func<int, int[]> vista = i => { var v = (Dictionary<string, object>)vistas[i]; int o = bo + (v.ContainsKey("byteOffset") ? I(v["byteOffset"]) : 0); return new[] { o, I(v["byteLength"]) }; };
            Func<int, int, float[]> flutuantes = (i, n) => { var a = (Dictionary<string, object>)acessores[i]; var v = vista(I(a["bufferView"])); int c = I(a["count"]) * n; var r = new float[c]; for (int k = 0; k < c; k++) r[k] = BitConverter.ToSingle(b, v[0] + k * 4); return r; };
            var pr = (Dictionary<string, object>)((List<object>)((Dictionary<string, object>)((List<object>)J["meshes"])[0])["primitives"])[0];
            var at = (Dictionary<string, object>)pr["attributes"];
            float[] P = flutuantes(I(at["POSITION"]), 3), N = flutuantes(I(at["NORMAL"]), 3), U = flutuantes(I(at["TEXCOORD_0"]), 2);
            var g = new Geo();
            for (int k = 0; k < P.Length / 3; k++)
            {
                g.p.Add(new Vector3(P[k * 3], P[k * 3 + 1], P[k * 3 + 2])); g.n.Add(new Vector3(N[k * 3], N[k * 3 + 1], N[k * 3 + 2]));
                g.uv.Add(new Vector2(U[k * 2], 1 - U[k * 2 + 1]));   // o glTF conta o v de cima; a textura do Unity, de baixo
            }
            var ai = (Dictionary<string, object>)acessores[I(pr["indices"])]; var vi = vista(I(ai["bufferView"])); int ct = I(ai["componentType"]), ni = I(ai["count"]);
            for (int k = 0; k < ni; k++) g.t.Add(ct == 5125 ? (int)BitConverter.ToUInt32(b, vi[0] + k * 4) : ct == 5121 ? b[vi[0] + k] : BitConverter.ToUInt16(b, vi[0] + k * 2));
            Texture2D tex = null; bool alfa = false;
            var imgs = J.ContainsKey("images") ? (List<object>)J["images"] : null;
            if (imgs != null && imgs.Count > 0)
            {
                var im0 = (Dictionary<string, object>)imgs[0]; alfa = im0.ContainsKey("mimeType") && (string)im0["mimeType"] == "image/png";
                var vim = vista(I(im0["bufferView"])); var jpg = new byte[vim[1]]; Array.Copy(b, vim[0], jpg, 0, vim[1]);
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = "modelo " + nome, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
                if (!ImageConversion.LoadImage(tex, jpg)) tex = null;
            }
            // PNG com alfa: recorte a .45 e as duas faces, como o alphaTest/DoubleSide do navegador
            var mat = Mat.Padrao(new Mat.Opcoes { mapa = tex, rugosidade = .92f, recorte = alfa && tex != null, corte = .45f, duplaFace = alfa && tex != null }); mat.name = "modelo " + nome;
            return new ModeloGLB { geo = g, mat = mat, tex = tex, alfa = alfa && tex != null };
        }
    }
}
