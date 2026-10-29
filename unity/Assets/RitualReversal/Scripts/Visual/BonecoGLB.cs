// Bonecos de verdade (Quaternius, CC0; assets/bonecos/fontes.json): o mesmo GLB enxuto que o navegador lê de client/bonecos.js,
// aqui em Resources/Bonecos/<nome>.bytes (tools/montar-bonecos.py grava os dois). Uma malha com a peça de cada triângulo em
// TEXCOORD_1.x (body, torso, head, legL, legR, armL, armR) e, em extras, os pivôs das juntas e a mão direita. O Boneco põe cada
// peça na sua junta (o bonecoReal do index.html). Sem o arquivo, volta null e o Boneco monta as peças antigas.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public class BonecoGLB
    {
        public Dictionary<string, Geo> geos = new Dictionary<string, Geo>(); public Material mat;
        public Dictionary<string, Vector3> pivos = new Dictionary<string, Vector3>(); public Vector3 mao;
        public Vector3 torsoMin, torsoMax;   // a caixa do torso no espaço da junta (a frente do peito para a runa, a estola e a lanterna)
        static readonly Dictionary<string, BonecoGLB> cache = new Dictionary<string, BonecoGLB>();

        public static BonecoGLB Carregar(string nome)
        {
            BonecoGLB b; if (cache.TryGetValue(nome, out b)) return b;
            b = null; var ta = Resources.Load<TextAsset>("Bonecos/" + nome);
            if (ta != null && ta.bytes != null)
            {
                try { b = Ler(ta.bytes, nome); }
                catch (Exception e) { Debug.LogWarning("boneco " + nome + ": " + e.Message); b = null; }
            }
            return cache[nome] = b;
        }

        static Vector3 V(object o) { var l = (List<object>)o; return new Vector3((float)(double)l[0], (float)(double)l[1], (float)(double)l[2]); }

        static BonecoGLB Ler(byte[] b, string nome)
        {
            int jl = BitConverter.ToInt32(b, 12), bo = 20 + jl + 8;
            var J = (Dictionary<string, object>)Json.Ler(Encoding.UTF8.GetString(b, 20, jl));
            Func<object, int> I = o => (int)(double)o;
            var vistas = (List<object>)J["bufferViews"]; var acessores = (List<object>)J["accessors"];
            Func<int, int[]> vista = i => { var v = (Dictionary<string, object>)vistas[i]; int o = bo + (v.ContainsKey("byteOffset") ? I(v["byteOffset"]) : 0); return new[] { o, I(v["byteLength"]) }; };
            Func<int, int, float[]> flutuantes = (i, n) => { var a = (Dictionary<string, object>)acessores[i]; var v = vista(I(a["bufferView"])); int c = I(a["count"]) * n; var r = new float[c]; for (int k = 0; k < c; k++) r[k] = BitConverter.ToSingle(b, v[0] + k * 4); return r; };
            var malha = (Dictionary<string, object>)((List<object>)J["meshes"])[0];
            var pr = (Dictionary<string, object>)((List<object>)malha["primitives"])[0]; var at = (Dictionary<string, object>)pr["attributes"];
            float[] P = flutuantes(I(at["POSITION"]), 3), N = flutuantes(I(at["NORMAL"]), 3), U = flutuantes(I(at["TEXCOORD_0"]), 2), U2 = flutuantes(I(at["TEXCOORD_1"]), 2);
            var ai = (Dictionary<string, object>)acessores[I(pr["indices"])]; var vi = vista(I(ai["bufferView"])); int ct = I(ai["componentType"]), ni = I(ai["count"]);
            var idx = new int[ni]; for (int k = 0; k < ni; k++) idx[k] = ct == 5125 ? (int)BitConverter.ToUInt32(b, vi[0] + k * 4) : BitConverter.ToUInt16(b, vi[0] + k * 2);
            var ex = (Dictionary<string, object>)malha["extras"]; var partes = (List<object>)ex["partes"];
            var B = new BonecoGLB();
            foreach (var kv in (Dictionary<string, object>)ex["pivos"]) B.pivos[kv.Key] = V(kv.Value);
            B.mao = V(ex["mao"]);
            for (int pi = 0; pi < partes.Count; pi++)
            {
                // os vértices de cada peça (só os que os triângulos dela usam), na ordem em que aparecem
                var g = new Geo(); var novo = new Dictionary<int, int>();
                for (int t = 0; t < ni; t += 3)
                {
                    if (Mathf.RoundToInt(U2[idx[t] * 2]) != pi) continue;
                    for (int c = 0; c < 3; c++)
                    {
                        int v = idx[t + c], nv;
                        if (!novo.TryGetValue(v, out nv))
                        {
                            nv = g.p.Count; novo[v] = nv;
                            g.p.Add(new Vector3(P[v * 3], P[v * 3 + 1], P[v * 3 + 2])); g.n.Add(new Vector3(N[v * 3], N[v * 3 + 1], N[v * 3 + 2]));
                            g.uv.Add(new Vector2(U[v * 2], 1 - U[v * 2 + 1]));   // o glTF conta o v de cima; a textura do Unity, de baixo
                        }
                        g.t.Add(nv);
                    }
                }
                if (g.p.Count > 0) B.geos[(string)partes[pi]] = g;
            }
            Geo T; if (B.geos.TryGetValue("torso", out T))
            {
                B.torsoMin = Vector3.one * 1e9f; B.torsoMax = -Vector3.one * 1e9f;
                foreach (var p in T.p) { B.torsoMin = Vector3.Min(B.torsoMin, p); B.torsoMax = Vector3.Max(B.torsoMax, p); }
            }
            Texture2D tex = null;
            var imgs = J.ContainsKey("images") ? (List<object>)J["images"] : null;
            if (imgs != null && imgs.Count > 0)
            {
                var vim = vista(I(((Dictionary<string, object>)imgs[0])["bufferView"])); var jpg = new byte[vim[1]]; Array.Copy(b, vim[0], jpg, 0, vim[1]);
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = "boneco " + nome, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
                if (!ImageConversion.LoadImage(tex, jpg)) tex = null;
            }
            B.mat = Mat.Padrao(new Mat.Opcoes { mapa = tex, rugosidade = .88f }); B.mat.name = "boneco " + nome;
            return B;
        }
    }
}
