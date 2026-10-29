// Armas de verdade (OpenGameArt, CC0; assets/armas/fontes.json): o mesmo GLB sem textura que o navegador lê de client/armas.js,
// aqui em Resources/Armas/<nome>.bytes (tools/montar-armas.py grava os dois). Cada triângulo traz a peça (estatico, alav, tambor) e o
// papel do material (caixa, aco, madeira, prata, escuro) no TEXCOORD_1; o uv sai da posição (o veio da madeira ao longo da arma),
// como o arma3d do index.html. As armas da terceira pessoa (Pecas.Carabina/Revolver) pintam com os materiais por papel; a primeira
// pessoa vem da exportação do navegador (tools/exportar-primeira-pessoa.js). Sem o arquivo, volta null e ficam as peças antigas.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public class ArmaGLB
    {
        public Dictionary<string, Dictionary<string, Geo>> geos = new Dictionary<string, Dictionary<string, Geo>>();   // [peça][papel]
        public Dictionary<string, Vector3> pivos = new Dictionary<string, Vector3>(); public Vector3 boca;
        static readonly Dictionary<string, ArmaGLB> cache = new Dictionary<string, ArmaGLB>();

        public static ArmaGLB Carregar(string nome)
        {
            ArmaGLB a; if (cache.TryGetValue(nome, out a)) return a;
            a = null; var ta = Resources.Load<TextAsset>("Armas/" + nome);
            if (ta != null && ta.bytes != null)
            {
                try { a = Ler(ta.bytes); }
                catch (Exception e) { Debug.LogWarning("arma " + nome + ": " + e.Message); a = null; }
            }
            return cache[nome] = a;
        }

        // a caixa das peças fixas (de um papel, ou de todas), no espaço da arma
        public Bounds Caixa(string papel)
        {
            bool tem = false; var b = new Bounds();
            Dictionary<string, Geo> est; if (!geos.TryGetValue("estatico", out est)) return b;
            foreach (var kv in est)
            {
                if (papel != null && kv.Key != papel) continue;
                foreach (var p in kv.Value.p) { if (!tem) { b = new Bounds(p, Vector3.zero); tem = true; } else b.Encapsulate(p); }
            }
            return b;
        }

        static Vector3 V(object o) { var l = (List<object>)o; return new Vector3((float)(double)l[0], (float)(double)l[1], (float)(double)l[2]); }

        static ArmaGLB Ler(byte[] b)
        {
            int jl = BitConverter.ToInt32(b, 12), bo = 20 + jl + 8;
            var J = (Dictionary<string, object>)Json.Ler(Encoding.UTF8.GetString(b, 20, jl));
            Func<object, int> I = o => (int)(double)o;
            var vistas = (List<object>)J["bufferViews"]; var acessores = (List<object>)J["accessors"];
            Func<int, int[]> vista = i => { var v = (Dictionary<string, object>)vistas[i]; int o = bo + (v.ContainsKey("byteOffset") ? I(v["byteOffset"]) : 0); return new[] { o, I(v["byteLength"]) }; };
            Func<int, int, float[]> flutuantes = (i, n) => { var a = (Dictionary<string, object>)acessores[i]; var v = vista(I(a["bufferView"])); int c = I(a["count"]) * n; var r = new float[c]; for (int k = 0; k < c; k++) r[k] = BitConverter.ToSingle(b, v[0] + k * 4); return r; };
            var malha = (Dictionary<string, object>)((List<object>)J["meshes"])[0];
            var pr = (Dictionary<string, object>)((List<object>)malha["primitives"])[0]; var at = (Dictionary<string, object>)pr["attributes"];
            float[] P = flutuantes(I(at["POSITION"]), 3), N = flutuantes(I(at["NORMAL"]), 3), U2 = flutuantes(I(at["TEXCOORD_1"]), 2);
            var ai = (Dictionary<string, object>)acessores[I(pr["indices"])]; var vi = vista(I(ai["bufferView"])); int ct = I(ai["componentType"]), ni = I(ai["count"]);
            var idx = new int[ni]; for (int k = 0; k < ni; k++) idx[k] = ct == 5125 ? (int)BitConverter.ToUInt32(b, vi[0] + k * 4) : BitConverter.ToUInt16(b, vi[0] + k * 2);
            var ex = (Dictionary<string, object>)malha["extras"]; var partes = (List<object>)ex["partes"]; var papeis = (List<object>)ex["papeis"];
            var A = new ArmaGLB(); A.boca = V(ex["boca"]);
            foreach (var kv in (Dictionary<string, object>)ex["pivos"]) A.pivos[kv.Key] = V(kv.Value);
            var novos = new Dictionary<string, Dictionary<int, int>>();
            for (int t = 0; t < ni; t += 3)
            {
                string pt = (string)partes[Mathf.RoundToInt(U2[idx[t] * 2])], pa = (string)papeis[Mathf.RoundToInt(U2[idx[t] * 2 + 1])];
                Dictionary<string, Geo> porPapel; if (!A.geos.TryGetValue(pt, out porPapel)) A.geos[pt] = porPapel = new Dictionary<string, Geo>();
                Geo g; if (!porPapel.TryGetValue(pa, out g)) porPapel[pa] = g = new Geo();
                Dictionary<int, int> mapa; string ch = pt + "|" + pa; if (!novos.TryGetValue(ch, out mapa)) novos[ch] = mapa = new Dictionary<int, int>();
                for (int c = 0; c < 3; c++)
                {
                    int v = idx[t + c], nv;
                    if (!mapa.TryGetValue(v, out nv))
                    {
                        nv = g.p.Count; mapa[v] = nv;
                        g.p.Add(new Vector3(P[v * 3], P[v * 3 + 1], P[v * 3 + 2])); g.n.Add(new Vector3(N[v * 3], N[v * 3 + 1], N[v * 3 + 2]));
                        g.uv.Add(new Vector2(P[v * 3 + 2] * 5, P[v * 3 + 1] * 5));
                    }
                    g.t.Add(nv);
                }
            }
            return A;
        }
    }
}
