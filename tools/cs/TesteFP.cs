// Primeira pessoa do Unity (Scripts/Visual/ArmaFP.cs) contra o navegador, sem abrir o Unity: tools/unity-fp.js grava os
// casos (arma, clipe, instante, estado do jogador e camadas) num JSON, este programa roda o ArmaFP com eles sobre o
// UnityEngine de mentira (tools/cs/UnityFalso.cs) e devolve, para cada caso medido, a matriz de cada nó no espaço da
// câmera do three. O navegador faz o mesmo com o client/arma.js e o unity-fp.js compara.
// Uso: mono teste.exe <Resources> <casos.json> <saida.json>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

public static class TesteFP
{
    static double N(Dictionary<string, object> o, string k, double v = 0) { object x; return o.TryGetValue(k, out x) && x != null ? Json.D(x) : v; }
    static bool B(Dictionary<string, object> o, string k) { object x; return o.TryGetValue(k, out x) && x is bool && (bool)x; }
    static string S(Dictionary<string, object> o, string k) { object x; return o.TryGetValue(k, out x) ? x as string : null; }

    public static int Main(string[] a)
    {
        Resources.pasta = a[0]; Tex.Construir();
        var casos = Json.L(Json.Ler(File.ReadAllText(a[1])));
        var cam = new GameObject("câmera").AddComponent<Camera>();
        var vm = new GameObject("primeira pessoa").transform; vm.SetParent(cam.transform, false); vm.localScale = new Vector3(1, 1, -1);
        var mundo = new GameObject("mundo").transform; mundo.localScale = new Vector3(1, 1, -1);
        var fp = new ArmaFP(vm); if (!fp.ok) { Console.Error.WriteLine("o ArmaFP não montou"); return 2; }
        fp.raizMundo = mundo;
        var T = fp.Nos; var sb = new StringBuilder("["); bool primeiro = true; var ci = CultureInfo.InvariantCulture;
        foreach (var o in casos)
        {
            var c = Json.O(o); object v;
            if (c.TryGetValue("forca", out v))
            {
                if (v == null) fp.Forcar(null);
                else
                {
                    var f = Json.O(v); object t, cg, fv;
                    fp.Forcar(new ArmaFP.Forca
                    {
                        arma = S(f, "arma"), clipe = S(f, "clipe"), vazia = B(f, "vazia"), n = (int)N(f, "n", 3), t = f.TryGetValue("t", out t) && t != null ? (double?)Json.D(t) : null,
                        carga = f.TryGetValue("carga", out cg) && cg != null ? (int?)(int)Json.D(cg) : null, fervor = f.TryGetValue("fervor", out fv) && fv != null ? (double?)Json.D(fv) : null, vel = N(f, "vel", 1)
                    });
                }
            }
            if (c.TryGetValue("vm", out v)) { var V = Json.O(v); object x; if (V.TryGetValue("tT", out x)) fp.tT = Json.D(x); if (V.TryGetValue("pausa", out x)) fp.pausa = Json.D(x); if (V.TryGetValue("cast", out x)) fp.cast = Json.D(x); if (V.TryGetValue("fase", out x)) fp.fase = Json.D(x); }
            var M = Json.O(c["m"]);
            var m = new Ator { team = S(M, "team"), cls = S(M, "cls"), st = S(M, "st") ?? "alive", ammo = (int)N(M, "ammo", 8), reloadT = N(M, "reloadT"), sealing = (int)N(M, "sealing", -1), fervor = N(M, "fervor", 100) };
            if (M.TryGetValue("hold", out v) && v != null) { var h = Json.O(v); m.hold = new Hold(S(h, "key"), N(h, "t"), N(h, "max")); }
            var X = Json.O(c["ctx"]); var ctx = new ArmaFP.Ctx { agora = N(X, "agora"), andando = B(X, "andando"), correndo = B(X, "correndo"), canalizando = B(X, "canalizando"), queimando = B(X, "queimando"), agK = N(X, "agK"), execK = N(X, "execK"), colK = N(X, "colK") };
            if (X.TryGetValue("altar", out v) && v != null) { var l = Json.L(v); ctx.temAltar = true; ctx.altarX = (float)Json.D(l[0]); ctx.altarZ = (float)Json.D(l[1]); }
            var cp = Json.L(X["corpo"]); ctx.bx = (float)Json.D(cp[0]); ctx.by = (float)Json.D(cp[1]); ctx.bz = (float)Json.D(cp[2]); ctx.brx = (float)Json.D(cp[3]); ctx.bry = (float)Json.D(cp[4]); ctx.brz = (float)Json.D(cp[5]);
            fp.Quadro(N(c, "dt", 1 / 60.0), m, ctx); fp.AtualizarEfeitos((float)N(c, "dt", 1 / 60.0));
            if (!B(c, "medir")) continue;
            sb.Append(primeiro ? "" : ",").Append("{\"est\":\"").Append(fp.EstadoAtual).Append("\",\"t\":").Append(fp.TempoClipe.ToString("R", ci)).Append(",\"nos\":["); primeiro = false;
            for (int i = 0; i < T.Length; i++)
            {
                if (i > 0) sb.Append(',');
                var t = T[i]; bool vis = t.gameObject.activeInHierarchy; var mm = Md.Id();
                for (var q = t; q != null && q != vm; q = q.parent) mm = Md.Compor(V3d.DeF(q.localPosition), Qd.DeF(q.localRotation), V3d.DeF(q.localScale)).Mul(mm);
                sb.Append('[').Append(vis ? 1 : 0); foreach (int k in new[] { 0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14 }) sb.Append(',').Append(mm.e[k].ToString("R", ci)); sb.Append(']');
            }
            sb.Append("]}");
        }
        sb.Append(']'); File.WriteAllText(a[2], sb.ToString());
        // fumaça: cada skin (acessórios, pendentes, ganchos da inspeção) e as entradas do jogo, procurando exceções
        int n = 0; double ag = 100;
        foreach (var arma in new[] { "carabina", "revolver", "cetro" })
            foreach (var sk in fp.skins.catalogo[arma])
            {
                fp.TrocarSkin(arma, sk.id); fp.Forcar(new ArmaFP.Forca { arma = arma, clipe = "inspecao", repetir = true, vel = 1, fervor = 100 });
                var m = new Ator { team = arma == "cetro" ? "C" : "H", cls = arma == "revolver" ? "exorcista" : "soldado", st = "alive" };
                for (int i = 0; i < 420; i++)
                {
                    if (i == 30) { fp.tT = 0; fp.somFeito = false; fp.Tiro(arma == "revolver"); }
                    if (i == 60) fp.Sigilo(); if (i == 90) fp.Golpe(1); if (i == 120) fp.Hab(); if (i == 150) fp.Executou(); if (i == 180) fp.DisparoForcado(arma);
                    ag += 1 / 60.0; fp.Quadro(1 / 60.0, m, new ArmaFP.Ctx { agora = ag, canalizando = arma == "cetro" && i > 300, temAltar = true, altarX = 2, altarZ = 3 }); fp.AtualizarEfeitos(1 / 60f); n++;
                }
                fp.TrocarSkin(arma, fp.skins.catalogo[arma][0].id);
            }
        Console.Error.WriteLine("fumaça: " + n + " quadros com as 9 skins, tiro, sigilo, golpe, habilidade e execução, sem exceção");
        return 0;
    }
}
