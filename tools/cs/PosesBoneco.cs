// Poses dos bonecos do Unity fora do Unity: monta o mundo (o Animar usa o Mundo para partículas e sons), cria um Boneco de
// cada classe e anima cada um por um roteiro (andar, correr, agachado, ajoelhado, mirando, recarregando, caído, culto
// canalizando...) com o Animar de verdade (Scripts/Visual/Personagens.cs). Em cada instante escolhido grava a malha do boneco
// já na pose, deslocada numa grade, no mesmo formato do ExportarMundo (mundo.json + mundo.bin + texturas). O tools/unity-poses.js
// desenha a grade no three. Uso: mono poses.exe <pasta Resources> <pasta de saída> [regex dos roteiros]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

public static class PosesBoneco
{
    static readonly CultureInfo C = CultureInfo.InvariantCulture;
    static string F(float v) { return float.IsNaN(v) || float.IsInfinity(v) ? "0" : v.ToString("R", C); }
    static string Cor(Color c) { var l = c.linear; return "[" + F(l.r) + "," + F(l.g) + "," + F(l.b) + "," + F(c.a) + "]"; }
    static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }

    // um roteiro: classe, lado, quantos segundos, o que fazer com o ator a cada quadro (t) e os instantes fotografados
    class Roteiro { public string nome, team, cls; public float dur, vel; public Action<Ator, float> passo; public float[] fotos; }

    public static void Main(string[] a)
    {
        string res = a[0], saida = a[1]; var filtro = new Regex(a.Length > 2 ? a[2] : "."); Directory.CreateDirectory(saida); Resources.pasta = res;
        Mapa.Carregar(File.ReadAllText(Path.Combine(res, "simulacao.json"))); Mapa.Usar("catedral");
        var mundo = Mundo.Construir("catedral");
        var raiz = new GameObject("poses").transform; raiz.SetParent(mundo.raiz, false);
        // as fotos de um ciclo de passo: a fase anda 3,2 + 1,35·v rad/s; a 4 m/s um ciclo leva ~0,74 s
        var ciclo = new[] { 1.5f, 1.62f, 1.74f, 1.86f, 1.98f, 2.1f };
        var R = new List<Roteiro> {
            new Roteiro { nome = "andar-soldado", team = "H", cls = "soldado", dur = 2.2f, vel = 3.4f, fotos = ciclo },
            new Roteiro { nome = "correr-soldado", team = "H", cls = "soldado", dur = 2.2f, vel = 6.2f, fotos = ciclo },
            new Roteiro { nome = "andar-ritualista", team = "C", cls = "ritualista", dur = 2.2f, vel = 3.4f, fotos = ciclo },
            new Roteiro { nome = "correr-guardiao", team = "C", cls = "guardiao", dur = 2.2f, vel = 6.2f, fotos = ciclo },
            new Roteiro { nome = "agachado-rastreador", team = "H", cls = "rastreador", dur = 2.2f, vel = 1.7f, passo = (x, t) => { x.ag = true; }, fotos = ciclo },
            new Roteiro { nome = "parado", team = "H", cls = "exorcista", dur = 2.2f, vel = 0, fotos = new[] { 1f, 2.2f } },
            new Roteiro { nome = "parado-culto", team = "C", cls = "ritualista", dur = 2.2f, vel = 0, fotos = new[] { 1f, 2.2f } },
            new Roteiro { nome = "ajoelhado", team = "H", cls = "soldado", dur = 2f, vel = 0, passo = (x, t) => { x.hold = new Hold("col0", t, 5); }, fotos = new[] { 2f } },
            new Roteiro { nome = "selando", team = "H", cls = "exorcista", dur = 2f, vel = 0, passo = (x, t) => { x.sealing = 0; }, fotos = new[] { 2f } },
            new Roteiro { nome = "executando", team = "H", cls = "soldado", dur = 2f, vel = 0, passo = (x, t) => { x.hold = new Hold("exe9", t, 4.5); }, fotos = new[] { 2f } },
            new Roteiro { nome = "recarga-soldado", team = "H", cls = "soldado", dur = 2.6f, vel = 0, passo = (x, t) => { x.reloadT = t < 1 ? 0 : Math.Max(0, 1.6 - (t - 1)); }, fotos = new[] { 1.2f, 1.5f, 1.8f, 2.1f, 2.4f } },
            new Roteiro { nome = "recarga-exorcista", team = "H", cls = "exorcista", dur = 2.6f, vel = 0, passo = (x, t) => { x.reloadT = t < 1 ? 0 : Math.Max(0, 1.6 - (t - 1)); }, fotos = new[] { 1.2f, 1.5f, 1.8f, 2.1f, 2.4f } },
            new Roteiro { nome = "caido", team = "H", cls = "rastreador", dur = 2.5f, vel = 0, passo = (x, t) => { if (t > .5f) x.st = "down"; }, fotos = new[] { .6f, .8f, 1.1f, 2.5f } },
        };
        R = R.FindAll(r => filtro.IsMatch(r.nome));
        var bin = new BinaryWriter(File.Create(Path.Combine(saida, "mundo.bin"))); long pos = 0;
        var mats = new List<Material>(); var texs = new List<Texture2D>(); var malhas = new StringBuilder(); int nm = 0;
        Func<Texture, int> tex = t => { var t2 = t as Texture2D; if (t2 == null) return -1; int i = texs.IndexOf(t2); if (i < 0) { texs.Add(t2); i = texs.Count - 1; } return i; };
        var grade = new StringBuilder(); int linha = 0, nfoto = 0;
        foreach (var r in R)
        {
            var b = new Boneco(raiz, r.team, r.cls) { nome = r.nome };
            var x = new Ator { id = 7, team = r.team, cls = r.cls, name = r.nome, st = "alive" };
            const float dt = 1 / 60f; float t = 0, px = 0; int fi = 0;
            Time.time = 100;
            for (int q = 0; t < r.dur + 1e-4f; q++, t += dt)
            {
                x.st = "alive"; x.ag = false; x.sealing = -1; x.hold = new Hold(); x.reloadT = 0;
                if (r.passo != null) r.passo(x, t);
                px += r.vel * dt; Time.time += dt;
                b.Animar(mundo, x, px, 0, 0, dt, Time.time, new List<Altar>());   // anda em +x com o rosto em −z (yaw 0): o passo aparece de lado
                if (fi < r.fotos.Length && t >= r.fotos[fi] - 1e-4f)
                {
                    // grava o boneco na pose, com o x do passo tirado (fica parado na grade) e a coluna/linha da foto
                    float ox = fi * 1.6f - px, oz = linha * 2.6f;
                    Action<Transform> visita = null;
                    visita = tr =>
                    {
                        if (!tr.gameObject.activeSelf) return;
                        var mf = tr.gameObject.GetComponent<MeshFilter>(); var mr = tr.gameObject.GetComponent<MeshRenderer>();
                        if (mf != null && mr != null && mf.sharedMesh != null && mr.enabled && mr.sharedMaterial != null && mf.sharedMesh.vertices.Length > 0 && !(mr.sharedMaterial.numeros.ContainsKey("_Billboard") && mr.sharedMaterial.numeros["_Billboard"] == 1))
                        {
                            var me = mf.sharedMesh; int mi = mats.IndexOf(mr.sharedMaterial); if (mi < 0) { mats.Add(mr.sharedMaterial); mi = mats.Count - 1; }
                            Func<Vector3, Vector3> P = p => { for (var u = tr; u != mundo.raiz && u != null; u = u.parent) p = u.localRotation * Vector3.Scale(p, u.localScale) + u.localPosition; return p; };
                            Func<Vector3, Vector3> N = n => { for (var u = tr; u != mundo.raiz && u != null; u = u.parent) { var s = u.localScale; n = u.localRotation * new Vector3(n.x / s.x, n.y / s.y, n.z / s.z); } return n.normalized; };
                            long ini = pos; int v = me.vertices.Length;
                            foreach (var p in me.vertices) { var w = P(p); bin.Write(w.x + ox); bin.Write(w.y); bin.Write(w.z + oz); }
                            foreach (var n in me.normals.Length == v ? me.normals : new Vector3[v]) { var w = N(n); bin.Write(w.x); bin.Write(w.y); bin.Write(w.z); }
                            foreach (var u in me.uv.Length == v ? me.uv : new Vector2[v]) { bin.Write(u.x); bin.Write(u.y); }
                            bool temCor = me.colors.Length == v; if (temCor) foreach (var c in me.colors) { bin.Write(c.r); bin.Write(c.g); bin.Write(c.b); bin.Write(c.a); }
                            foreach (var i in me.triangles) bin.Write(i);
                            pos += v * 32L + (temCor ? v * 16L : 0) + me.triangles.Length * 4L;
                            if (nm++ > 0) malhas.Append(",\n");
                            malhas.Append("{\"nome\":\"" + Esc(tr.gameObject.name) + "\",\"mat\":" + mi + ",\"v\":" + v + ",\"t\":" + me.triangles.Length + ",\"ini\":" + ini + ",\"cor\":" + (temCor ? "true" : "false") + ",\"f\":" + nfoto + "}");
                        }
                        foreach (var f in tr.Filhos.ToArray()) visita(f);
                    };
                    visita(b.g);
                    grade.Append(grade.Length > 0 ? "," : "").Append("{\"rot\":\"" + r.nome + " " + r.fotos[fi].ToString("0.00", C) + "s\",\"x\":" + F(fi * 1.6f) + ",\"z\":" + F(oz) + ",\"linha\":" + linha + ",\"col\":" + fi + "}");
                    fi++; nfoto++;
                }
            }
            b.g.gameObject.SetActive(false); linha++;
        }
        bin.Close();
        var j = new StringBuilder(); j.Append("{\"malhas\":[\n").Append(malhas).Append("\n],\"grade\":[").Append(grade).Append("],\"materiais\":[\n");
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
        Console.WriteLine(R.Count + " roteiros, " + nm + " malhas, " + mats.Count + " materiais, " + texs.Count + " texturas");
    }
}
