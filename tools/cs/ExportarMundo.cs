// Roda o visual do Unity (Scripts/Visual) fora do Unity, contra o UnityEngine de mentira (UnityFalso.cs), e grava o que
// ele montou: cada malha com o seu material (cor, textura, transparência) já nas coordenadas do protótipo, as texturas e as
// luzes. tools/unity-ver.js desenha isso no navegador com as mesmas câmeras do jogo, para comparar com o oficial.
// Uso: mono exportar.exe <pasta Resources do Unity> <pasta de saída>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

public static class ExportarMundo
{
    static readonly CultureInfo C = CultureInfo.InvariantCulture;
    static string F(float v) { return float.IsNaN(v) || float.IsInfinity(v) ? "0" : v.ToString("R", C); }
    static string Cor(Color c) { var l = c.linear; return "[" + F(l.r) + "," + F(l.g) + "," + F(l.b) + "," + F(c.a) + "]"; }

    public static void Main(string[] a)
    {
        string res = a[0], saida = a[1]; Directory.CreateDirectory(saida); Resources.pasta = res;
        Mapa.Carregar(File.ReadAllText(Path.Combine(res, "simulacao.json")));
        var t0 = DateTime.Now; var mundo = Mundo.Construir(); Console.Error.WriteLine("mundo montado em " + (DateTime.Now - t0).TotalSeconds.ToString("0.0", C) + " s");
        if (a.Length > 2 && a[2] == "partida") { Partida(mundo); return; }
        var raiz = mundo.raiz;
        var bin = new BinaryWriter(File.Create(Path.Combine(saida, "mundo.bin"))); long pos = 0;
        var mats = new List<Material>(); var texs = new List<Texture2D>(); var malhas = new StringBuilder(); int nm = 0; long nv = 0, nt = 0;
        Func<Texture, int> tex = t => { var t2 = t as Texture2D; if (t2 == null) return -1; int i = texs.IndexOf(t2); if (i < 0) { texs.Add(t2); i = texs.Count - 1; } return i; };
        Action<Transform> visita = null;
        visita = tr =>
        {
            if (!tr.gameObject.activeSelf) return;
            var mf = tr.gameObject.GetComponent<MeshFilter>(); var mr = tr.gameObject.GetComponent<MeshRenderer>();
            if (mf != null && mr != null && mf.sharedMesh != null && mr.enabled && mr.sharedMaterial != null && mf.sharedMesh.vertices.Length > 0)
            {
                var me = mf.sharedMesh; int mi = mats.IndexOf(mr.sharedMaterial); if (mi < 0) { mats.Add(mr.sharedMaterial); mi = mats.Count - 1; }
                // da malha até a raiz espelhada (sem ela): coordenadas do protótipo
                Func<Vector3, Vector3> P = p => { for (var t = tr; t != raiz && t != null; t = t.parent) p = t.localRotation * Vector3.Scale(p, t.localScale) + t.localPosition; return p; };
                Func<Vector3, Vector3> N = n => { for (var t = tr; t != raiz && t != null; t = t.parent) { var s = t.localScale; n = t.localRotation * new Vector3(n.x / s.x, n.y / s.y, n.z / s.z); } return n.normalized; };
                long ini = pos; int v = me.vertices.Length;
                // num sprite (billboard) a posição é o centro e o canto vai em uv2: o centro passa pela transformação, o canto não
                foreach (var p in me.vertices) { var q = P(p); bin.Write(q.x); bin.Write(q.y); bin.Write(q.z); }
                foreach (var n in me.normals.Length == v ? me.normals : new Vector3[v]) { var q = N(n); bin.Write(q.x); bin.Write(q.y); bin.Write(q.z); }
                foreach (var u in me.uv.Length == v ? me.uv : new Vector2[v]) { bin.Write(u.x); bin.Write(u.y); }
                bool temUv2 = me.uv2.Length == v, temCor = me.colors.Length == v;
                if (temUv2) { var s = Escala(tr, raiz); foreach (var u in me.uv2) { bin.Write(u.x * s.x); bin.Write(u.y * s.y); } }
                if (temCor) foreach (var c in me.colors) { bin.Write(c.r); bin.Write(c.g); bin.Write(c.b); bin.Write(c.a); }
                foreach (var i in me.triangles) bin.Write(i);
                pos += v * 32L + (temUv2 ? v * 8L : 0) + (temCor ? v * 16L : 0) + me.triangles.Length * 4L;
                if (nm++ > 0) malhas.Append(",\n");
                malhas.Append("{\"nome\":\"" + Esc(tr.gameObject.name) + "\",\"pai\":\"" + Esc(tr.parent != null ? tr.parent.gameObject.name : "") + "\",\"mat\":" + mi + ",\"v\":" + v + ",\"t\":" + me.triangles.Length + ",\"ini\":" + ini + ",\"uv2\":" + (temUv2 ? 1 : 0) + ",\"cor\":" + (temCor ? 1 : 0) + ",\"sombra\":" + (mr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off ? 1 : 0) + "}");
                nv += v; nt += me.triangles.Length / 3;
            }
            foreach (var f in tr.Filhos.ToArray()) visita(f);
        };
        visita(raiz);
        bin.Close();
        var j = new StringBuilder(); j.Append("{\"malhas\":[\n").Append(malhas).Append("\n],\"materiais\":[\n");
        for (int i = 0; i < mats.Count; i++)
        {
            var m = mats[i]; Func<string, string> num = k => m.numeros.ContainsKey(k) ? F(m.numeros[k]) : "null";
            var mapa = m.GetTexture("_BaseMap") ?? m.GetTexture("_MainTex"); var esc = m.escalas.ContainsKey("_BaseMap") ? m.escalas["_BaseMap"] : m.GetTextureScale("_MainTex"); var des = m.GetTextureOffset("_MainTex");
            var det = m.GetTexture("_DetailAlbedoMap"); var dEsc = m.GetTextureScale("_DetailAlbedoMap");
            j.Append(i > 0 ? ",\n" : "").Append("{\"nome\":\"" + Esc(m.name) + "\",\"shader\":\"" + Esc(m.shader != null ? m.shader.name : "") + "\",\"cor\":" + Cor(m.cores.ContainsKey("_BaseColor") ? m.cores["_BaseColor"] : m.GetColor("_Color")));
            j.Append(",\"mapa\":" + (mapa != null ? tex(mapa) : -1) + ",\"esc\":[" + F(esc.x) + "," + F(esc.y) + "],\"desl\":[" + F(des.x) + "," + F(des.y) + "]");
            j.Append(",\"detalhe\":" + (det != null ? tex(det) : -1) + ",\"dEsc\":[" + F(dEsc.x) + "," + F(dEsc.y) + "]");
            j.Append(",\"cull\":" + num("_Cull") + ",\"surface\":" + num("_Surface") + ",\"clip\":" + num("_AlphaClip") + ",\"cutoff\":" + num("_Cutoff") + ",\"dst\":" + num("_DstBlend") + ",\"zwrite\":" + num("_ZWrite"));
            j.Append(",\"billboard\":" + num("_Billboard") + ",\"nevoa\":" + num("_Nevoa") + ",\"corte\":" + num("_Corte") + ",\"metal\":" + num("_Metallic") + ",\"liso\":" + num("_Smoothness") + ",\"fila\":" + m.renderQueue);
            j.Append(",\"emissao\":" + (m.chaves.Contains("_EMISSION") ? Cor(m.GetColor("_EmissionColor")) : "null"));
            j.Append(",\"nevoaK\":" + num("_NevoaK") + ",\"rim\":" + (m.cores.ContainsKey("_Rim") ? Cor(m.cores["_Rim"]) : "null") + "}");
        }
        j.Append("\n],\"texturas\":[\n");
        for (int i = 0; i < texs.Count; i++)
        {
            var t = texs[i]; var arq = "t" + i + ".rgba"; var bytes = new byte[t.width * t.height * 4]; int k = 0;
            foreach (var p in t.pixels) { bytes[k++] = p.r; bytes[k++] = p.g; bytes[k++] = p.b; bytes[k++] = p.a; }
            File.WriteAllBytes(Path.Combine(saida, arq), bytes);
            j.Append(i > 0 ? ",\n" : "").Append("{\"arq\":\"" + arq + "\",\"w\":" + t.width + ",\"h\":" + t.height + ",\"srgb\":" + (t.linear ? 0 : 1) + ",\"repete\":" + (t.wrapMode == TextureWrapMode.Repeat ? 1 : 0) + ",\"nome\":\"" + Esc(t.name) + "\"}");
        }
        j.Append("\n],\"fontes\":[");
        for (int i = 0; i < mundo.FONTES.Count; i++) { var f = mundo.FONTES[i]; j.Append(i > 0 ? "," : "").Append("[" + F(f.x) + "," + F(f.y) + "," + F(f.z) + "," + f.cor + "," + F(f.i) + "," + F(f.alc) + "]"); }
        j.Append("]}");
        File.WriteAllText(Path.Combine(saida, "mundo.json"), j.ToString());
        Console.WriteLine(nm + " malhas, " + mats.Count + " materiais, " + texs.Count + " texturas, " + nv + " vértices, " + nt + " triângulos");
    }
    // Uma partida de bots passando pelo visual do Unity quadro a quadro, como a PartidaVisual faz, com os estados que o
    // mundo da v6 desenha forçados no meio: ritual ativo com selo, alguém coletando e queimando reagentes, uma execução.
    // Qualquer exceção sai com o quadro e a pilha (no Unity seria um erro vermelho por quadro no Console).
    static void Partida(Mundo mundo)
    {
        var slots = new List<Slot> { new Slot("A", "local", "Você"), new Slot("A", null, "Irmã Beatriz"), new Slot("B", null, "Irmão Cinza"), new Slot("B", null, "Madre Vesper") };
        var g = Sim.CreateGame(new OpcoesJogo { timers = false, slots = slots, matchId = "teste-unity-5" });   // este código sorteia tempestade: chuva e relâmpagos
        int trovoes = 0; mundo.AoTrovao = d => trovoes++;
        Sim.Act(g, "local", new Acao { type = "ready" }); const double dt = 1 / 30.0; int q = 0;
        try
        {
            for (; q < 2400; q++)
            {
                Sim.Step(g, dt); Time.time += (float)dt; Time.deltaTime = (float)dt;
                if (g.phase != "play") { foreach (var s in g.slots) if (s.cid != null) Sim.Act(g, s.cid, new Acao { type = "ready" }); continue; }
                Ator eu = null; foreach (var x in g.actors) if (x.cid == "local") eu = x;
                if (q == 300) { var A = g.altars[1]; A.state = "active"; A.prog = .62; A.cp = .5; A.seal = .2; A.localized = true; }
                if (q > 300 && q < 900 && g.reagents.Count > 1) { var b = g.actors[1]; b.hold = new Hold(q < 600 ? "col0" : "burn1", (q % 300) / 60.0, 5); if (q == 599) g.reagents[0].has = false; }
                if (q > 900 && q < 1100) { var v = g.actors[2]; v.st = "down"; g.actors[1].hold = new Hold("exe" + v.id, (q - 900) / 40.0, 4.5); if (q == 1099) v.st = "dead"; }
                if (q == 1200) { var A = g.altars[1]; A.state = "farol"; } if (q == 1500) g.altars[2].state = "fenda";
                var cam = new Vector3((float)(eu != null ? eu.x : 0), 1.67f, (float)(eu != null ? eu.z : 0)); string papel = Sim.RoleOf(g, "A");
                mundo.Atualizar(g, papel, eu, cam, (float)(eu != null ? eu.yaw : 0), (float)dt);
                mundo.AtualizarClima(g, cam, (float)dt);
                mundo.SyncActors(g, papel, eu, cam, 0, (float)dt); mundo.SyncNoite(g, cam, (float)dt); mundo.AtualizarEfeitosQuadro((float)dt);
            }
        }
        catch (Exception e) { Console.WriteLine("ERRO no quadro " + q + " (fase " + g.phase + "): " + e); Environment.Exit(1); }
        Console.WriteLine("partida no visual do Unity: " + q + " quadros sem exceção (fase " + g.phase + ", execuções " + mundo.fxExecucoes + ", clima " + mundo.climaTipo + ", chuva " + mundo.climaChuva.ToString("0.00", C) + ", trovões " + trovoes + ")");
    }
    static Vector3 Escala(Transform tr, Transform raiz) { var s = Vector3.one; for (var t = tr; t != raiz && t != null; t = t.parent) s = Vector3.Scale(s, t.localScale); return s; }
    static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }
}
