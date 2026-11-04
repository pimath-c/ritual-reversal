// Primeira pessoa do Unity em fotos, sem o Unity: monta o ArmaFP (Scripts/Visual/ArmaFP.cs) sob uma câmera, como o TesteFP, força
// cada caso (arma, clipe, instante) e grava as malhas sob a câmera no espaço dela (o do three: −z à frente),
// no formato do ExportarMundo. O tools/unity-fp-fotos.js desenha cada caso. Uso: mono foto.exe <Resources> <saída> [regex]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

public static class FotoFP
{
    static readonly CultureInfo C = CultureInfo.InvariantCulture;
    static string F(float v) { return float.IsNaN(v) || float.IsInfinity(v) ? "0" : v.ToString("R", C); }
    static string Cor(Color c) { var l = c.linear; return "[" + F(l.r) + "," + F(l.g) + "," + F(l.b) + "," + F(c.a) + "]"; }
    static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }

    public static void Main(string[] a)
    {
        Resources.pasta = a[0]; string saida = a[1]; var filtro = new Regex(a.Length > 2 ? a[2] : "."); Directory.CreateDirectory(saida); Tex.Construir();
        var cam = new GameObject("câmera").AddComponent<Camera>();
        var vm = new GameObject("primeira pessoa").transform; vm.SetParent(cam.transform, false); vm.localScale = new Vector3(1, 1, -1);
        var mundo = new GameObject("mundo").transform; mundo.localScale = new Vector3(1, 1, -1);
        var fp = new ArmaFP(vm); if (!fp.ok) { Console.Error.WriteLine("o ArmaFP não montou"); Environment.Exit(2); }
        fp.raizMundo = mundo;
        // casos: nome, arma, boneco, clipe, instantes (frações do clipe), time e classe do jogador
        var casos = new List<object[]> {
            new object[] { "carabina-soldado", "carabina", "soldado", "pronta", new[] { 0.0 }, "H", "soldado" },
            new object[] { "carabina-rastreador", "carabina", "rastreador", "pronta", new[] { 0.0 }, "H", "rastreador" },
            new object[] { "revolver-exorcista", "revolver", "exorcista", "pronta", new[] { 0.0 }, "H", "exorcista" },
            new object[] { "cetro-ritualista", "cetro", "ritualista", "pronta", new[] { 0.0 }, "C", "ritualista" },
            new object[] { "cetro-guardiao", "cetro", "guardiao", "pronta", new[] { 0.0 }, "C", "guardiao" },
            new object[] { "recarga-soldado", "carabina", "soldado", "recarga", new[] { .15, .35, .55, .8 }, "H", "soldado" },
            new object[] { "recarga-exorcista", "revolver", "exorcista", "recarga", new[] { .15, .35, .55, .8 }, "H", "exorcista" },
            new object[] { "inspecao-soldado", "carabina", "soldado", "inspecao", new[] { .2, .45, .7 }, "H", "soldado" },
            new object[] { "corrida-soldado", "carabina", "soldado", "corrida", new[] { 0.0 }, "H", "soldado" },
            new object[] { "canalizar-ritualista", "cetro", "ritualista", "canalizar", new[] { 0.0 }, "C", "ritualista" },
        };
        var bin = new BinaryWriter(File.Create(Path.Combine(saida, "mundo.bin"))); long pos = 0;
        var mats = new List<Material>(); var texs = new List<Texture2D>(); var malhas = new StringBuilder(); var fotos = new StringBuilder(); int nm = 0, nf = 0;
        Func<Texture, int> tex = t => { var t2 = t as Texture2D; if (t2 == null) return -1; int i = texs.IndexOf(t2); if (i < 0) { texs.Add(t2); i = texs.Count - 1; } return i; };
        foreach (var c in casos)
        {
            string nome = (string)c[0]; if (!filtro.IsMatch(nome)) continue;
            foreach (double fr in (double[])c[4])
            {
                var m = new Ator { team = (string)c[5], cls = (string)c[6], st = "alive", ammo = 8, fervor = 100 };
                var ctx = new ArmaFP.Ctx { agora = 10 };
                // força o clipe e anda alguns quadros para a mistura assentar; depois fixa o instante
                fp.Forcar(new ArmaFP.Forca { arma = (string)c[1], clipe = (string)c[3], vazia = true, t = null, vel = 1 });
                for (int q = 0; q < 40; q++) { ctx.agora += 1 / 60.0; fp.Quadro(1 / 60.0, m, ctx); }
                fp.Forcar(new ArmaFP.Forca { arma = (string)c[1], clipe = (string)c[3], vazia = true, t = fr * Math.Max(.01, fp.DuracaoClipe), vel = 1 });
                ctx.agora += 1 / 60.0; fp.Quadro(1 / 60.0, m, ctx);
                Action<Transform> visita = null;
                visita = tr =>
                {
                    if (!tr.gameObject.activeSelf) return;
                    var mf = tr.gameObject.GetComponent<MeshFilter>(); var mr = tr.gameObject.GetComponent<MeshRenderer>();
                    if (mf != null && mr != null && mf.sharedMesh != null && mr.enabled && mr.sharedMaterial != null && mf.sharedMesh.vertices.Length > 0)
                    {
                        var me = mf.sharedMesh; int mi = mats.IndexOf(mr.sharedMaterial); if (mi < 0) { mats.Add(mr.sharedMaterial); mi = mats.Count - 1; }
                        Func<Vector3, Vector3> P = p => { for (var u = tr; u != vm && u != null; u = u.parent) p = u.localRotation * Vector3.Scale(p, u.localScale) + u.localPosition; return p; };
                        Func<Vector3, Vector3> N = n => { for (var u = tr; u != vm && u != null; u = u.parent) { var s = u.localScale; n = u.localRotation * new Vector3(n.x / s.x, n.y / s.y, n.z / s.z); } return n.normalized; };
                        long ini = pos; int v = me.vertices.Length;
                        foreach (var p in me.vertices) { var w = P(p); bin.Write(w.x); bin.Write(w.y); bin.Write(w.z); }
                        foreach (var n in me.normals.Length == v ? me.normals : new Vector3[v]) { var w = N(n); bin.Write(w.x); bin.Write(w.y); bin.Write(w.z); }
                        foreach (var u in me.uv.Length == v ? me.uv : new Vector2[v]) { bin.Write(u.x); bin.Write(u.y); }
                        foreach (var i in me.triangles) bin.Write(i);
                        pos += v * 32L + me.triangles.Length * 4L;
                        if (nm++ > 0) malhas.Append(",\n");
                        malhas.Append("{\"nome\":\"" + Esc(tr.gameObject.name) + "\",\"mat\":" + mi + ",\"v\":" + v + ",\"t\":" + me.triangles.Length + ",\"ini\":" + ini + ",\"f\":" + nf + "}");
                    }
                    // pele (SkinnedMeshRenderer): deformada aqui na CPU, Σ peso · (osso→câmera) · bindpose · v, como o Unity faz na placa
                    var sk = tr.gameObject.GetComponent<SkinnedMeshRenderer>();
                    if (sk != null && sk.enabled && sk.sharedMesh != null && sk.sharedMaterial != null)
                    {
                        var me = sk.sharedMesh; int mi = mats.IndexOf(sk.sharedMaterial); if (mi < 0) { mats.Add(sk.sharedMaterial); mi = mats.Count - 1; }
                        Func<Transform, Matrix4x4> AteVm = o => { var Mx = Matrix4x4.identity; for (var u = o; u != vm && u != null; u = u.parent) Mx = Matrix4x4.TRS(u.localPosition, u.localRotation, u.localScale) * Mx; return Mx; };
                        var K = new Matrix4x4[sk.bones.Length]; for (int b = 0; b < K.Length; b++) K[b] = AteVm(sk.bones[b]) * me.bindposes[b];
                        long ini = pos; int v = me.vertices.Length; var bw = me.boneWeights; var nn = new Vector3[v];
                        for (int i = 0; i < v; i++)
                        {
                            var w = bw[i]; var p = me.vertices[i]; var n = me.normals[i];
                            Vector3 q = K[w.boneIndex0].MultiplyPoint3x4(p) * w.weight0 + K[w.boneIndex1].MultiplyPoint3x4(p) * w.weight1 + K[w.boneIndex2].MultiplyPoint3x4(p) * w.weight2 + K[w.boneIndex3].MultiplyPoint3x4(p) * w.weight3;
                            nn[i] = (K[w.boneIndex0].MultiplyVector(n) * w.weight0 + K[w.boneIndex1].MultiplyVector(n) * w.weight1 + K[w.boneIndex2].MultiplyVector(n) * w.weight2 + K[w.boneIndex3].MultiplyVector(n) * w.weight3).normalized;
                            bin.Write(q.x); bin.Write(q.y); bin.Write(q.z);
                        }
                        foreach (var n in nn) { bin.Write(n.x); bin.Write(n.y); bin.Write(n.z); }
                        foreach (var u in me.uv.Length == v ? me.uv : new Vector2[v]) { bin.Write(u.x); bin.Write(u.y); }
                        foreach (var i in me.triangles) bin.Write(i);
                        pos += v * 32L + me.triangles.Length * 4L;
                        if (nm++ > 0) malhas.Append(",\n");
                        malhas.Append("{\"nome\":\"" + Esc(tr.gameObject.name) + "\",\"mat\":" + mi + ",\"v\":" + v + ",\"t\":" + me.triangles.Length + ",\"ini\":" + ini + ",\"f\":" + nf + "}");
                    }
                    foreach (var f in tr.Filhos.ToArray()) visita(f);
                };
                foreach (var f in vm.Filhos.ToArray()) visita(f);
                fotos.Append(nf > 0 ? "," : "").Append("\"" + nome + " " + fr.ToString("0.00", C) + "\""); nf++;
            }
        }
        bin.Close();
        var j = new StringBuilder(); j.Append("{\"malhas\":[\n").Append(malhas).Append("\n],\"fotos\":[").Append(fotos).Append("],\"materiais\":[\n");
        for (int i = 0; i < mats.Count; i++)
        {
            var m = mats[i]; Func<string, string> num = k => m.numeros.ContainsKey(k) ? F(m.numeros[k]) : "null";
            var mapa = m.GetTexture("_BaseMap") ?? m.GetTexture("_MainTex"); var esc = m.escalas.ContainsKey("_BaseMap") ? m.escalas["_BaseMap"] : m.GetTextureScale("_MainTex");
            j.Append(i > 0 ? ",\n" : "").Append("{\"nome\":\"" + Esc(m.name) + "\",\"shader\":\"" + Esc(m.shader != null ? m.shader.name : "") + "\",\"cor\":" + Cor(m.cores.ContainsKey("_BaseColor") ? m.cores["_BaseColor"] : m.GetColor("_Color")));
            j.Append(",\"mapa\":" + (mapa != null ? tex(mapa) : -1) + ",\"esc\":[" + F(esc.x) + "," + F(esc.y) + "],\"cull\":" + num("_Cull") + ",\"surface\":" + num("_Surface") + ",\"clip\":" + num("_AlphaClip") + ",\"metal\":" + num("_Metallic") + ",\"liso\":" + num("_Smoothness"));
            j.Append(",\"emissao\":" + (m.chaves.Contains("_EMISSION") ? Cor(m.GetColor("_EmissionColor")) : "null") + "}");
        }
        j.Append("\n],\"texturas\":[\n");
        for (int i = 0; i < texs.Count; i++)
        {
            var t = texs[i]; var arq = "t" + i + ".rgba"; var bytes = new byte[t.width * t.height * 4]; int k = 0;
            foreach (var p in t.pixels) { bytes[k++] = p.r; bytes[k++] = p.g; bytes[k++] = p.b; bytes[k++] = p.a; }
            File.WriteAllBytes(Path.Combine(saida, arq), bytes);
            j.Append(i > 0 ? ",\n" : "").Append("{\"arq\":\"" + arq + "\",\"w\":" + t.width + ",\"h\":" + t.height + ",\"srgb\":" + (t.linear ? 0 : 1) + ",\"repete\":" + (t.wrapMode == TextureWrapMode.Repeat ? 1 : 0) + "}");
        }
        j.Append("]}");
        File.WriteAllText(Path.Combine(saida, "mundo.json"), j.ToString());
        Console.WriteLine(nf + " fotos, " + nm + " malhas, " + mats.Count + " materiais, " + texs.Count + " texturas");
    }
}
