// A planta visual (Resources/mapa.json, gerada por tools/exportar-mapa.js): peças com tipo, altura, giro e pose,
// árvores, vegetação, trilhas, clareiras, arcos, pilares, bancos, luzes, portais e lugares. Coordenadas do protótipo.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public class PecaV { public string kind; public float x1, x2, z1, z2, h; public bool tall; public float rot; public int pose; public bool temRot; }
    public class ArvoreV { public float x, z, r, s, rot, inc; public int tipo; }
    public class ArbustoV { public float x, z, s, rot; public int tipo; }
    public class TrilhaV { public float w; public bool estreita; public List<Vector2> pts = new List<Vector2>(); }
    public class LuzV { public float x, z; public string tipo; }
    public class PortalV { public string nome, eixo; public float fixo, a, b; public bool brecha; }

    public static class MapaVisual
    {
        public static List<PecaV> WALLS = new List<PecaV>();
        public static List<ArvoreV> ARVORES = new List<ArvoreV>();
        public static List<ArbustoV> ARBUSTOS = new List<ArbustoV>();
        public static List<TrilhaV> TRILHAS = new List<TrilhaV>();
        public static List<Vector3> CLAREIRAS = new List<Vector3>(); // x, z, r
        public static List<Vector4> ARCOS = new List<Vector4>();
        public static List<Vector2> PILLARS = new List<Vector2>();
        public static List<Vector4> PEWS = new List<Vector4>(); // x1, x2, z1, z2
        public static List<LuzV> CANDLES = new List<LuzV>();
        public static List<PortalV> PORTAIS = new List<PortalV>();
        public static float HX, HZ, H0X, H0Z;
        public static Rect CATEDRAL, CLAUSTRO; // x = x1, y = z1, width, height
        public static bool Carregado;

        static float F(object v) { return (float)(double)v; }
        public static void Carregar(string json)
        {
            var R = Json.O(Json.Ler(json)); var lim = Json.O(R["limites"]);
            var mw = Json.O(lim["meiaLargura"]); HX = F(mw["x"]); HZ = F(mw["z"]); var m0 = Json.O(lim["planoOriginal4v4"]); H0X = F(m0["x"]); H0Z = F(m0["z"]);
            System.Func<string, Rect> ret = k => { var o = Json.O(lim[k]); float x1 = F(o["x1"]), x2 = F(o["x2"]), z1 = F(o["z1"]), z2 = F(o["z2"]); return new Rect(x1, z1, x2 - x1, z2 - z1); };
            CATEDRAL = ret("catedral"); CLAUSTRO = ret("claustro");
            WALLS.Clear();
            foreach (var o in Json.L(R["pecas"]))
            {
                var p = Json.O(o); object v;
                var w = new PecaV { kind = Json.S(p["tipo"]), x1 = F(p["x1"]), x2 = F(p["x2"]), z1 = F(p["z1"]), z2 = F(p["z2"]), h = F(p["h"]), tall = (bool)p["tall"] };
                if (p.TryGetValue("rot", out v) && v != null) { w.rot = F(v); w.temRot = true; }
                if (p.TryGetValue("pose", out v) && v != null) w.pose = (int)F(v);
                WALLS.Add(w);
            }
            ARVORES.Clear();
            foreach (var o in Json.L(R["arvores"]))
            {
                var p = Json.O(o); string t = Json.S(p["tipo"]); object inc;
                ARVORES.Add(new ArvoreV { x = F(p["x"]), z = F(p["z"]), r = F(p["raio"]), s = F(p["escala"]), rot = F(p["rot"]), inc = p.TryGetValue("inc", out inc) && inc != null ? F(inc) : 0, tipo = t == "anciã" ? 0 : t == "alta" ? 1 : 2 });
            }
            ARBUSTOS.Clear();
            foreach (var o in Json.L(R["vegetacaoRasteira"])) { var p = Json.O(o); ARBUSTOS.Add(new ArbustoV { x = F(p["x"]), z = F(p["z"]), s = F(p["escala"]), rot = F(p["rot"]), tipo = Json.S(p["tipo"]) == "moita" ? 1 : 0 }); }
            TRILHAS.Clear();
            foreach (var o in Json.L(R["trilhas"])) { var p = Json.O(o); var T = new TrilhaV { w = F(p["largura"]), estreita = (bool)p["estreita"] }; foreach (var q in Json.L(p["pontos"])) { var pp = Json.O(q); T.pts.Add(new Vector2(F(pp["x"]), F(pp["z"]))); } TRILHAS.Add(T); }
            CLAREIRAS.Clear(); foreach (var o in Json.L(R["clareiras"])) { var p = Json.O(o); CLAREIRAS.Add(new Vector3(F(p["x"]), F(p["z"]), F(p["raio"]))); }
            ARCOS.Clear(); foreach (var o in Json.L(R["arcos"])) { var p = Json.O(o); ARCOS.Add(new Vector4(F(p["x1"]), F(p["z1"]), F(p["x2"]), F(p["z2"]))); }
            PILLARS.Clear(); foreach (var o in Json.L(R["pilares"])) { var p = Json.O(o); PILLARS.Add(new Vector2(F(p["x"]), F(p["z"]))); }
            PEWS.Clear(); foreach (var o in Json.L(R["bancos"])) { var p = Json.O(o); PEWS.Add(new Vector4(F(p["x1"]), F(p["x2"]), F(p["z1"]), F(p["z2"]))); }
            CANDLES.Clear(); foreach (var o in Json.L(R["luzes"])) { var p = Json.O(o); string t = Json.S(p["tipo"]); CANDLES.Add(new LuzV { x = F(p["x"]), z = F(p["z"]), tipo = t == "vela" ? null : t }); }
            PORTAIS.Clear();
            foreach (var o in Json.L(R["portais"])) { var p = Json.O(o); object b; PORTAIS.Add(new PortalV { nome = Json.S(p["nome"]), eixo = Json.S(p["eixo"]), fixo = F(p["fixo"]), a = F(p["a"]), b = F(p["b"]), brecha = p.TryGetValue("brecha", out b) && b is bool && (bool)b }); }
            Carregado = true;
        }
        public static bool NaCatedral(float x, float z, float m = 0) { return x > CATEDRAL.xMin - m && x < CATEDRAL.xMax + m && z > CATEDRAL.yMin - m && z < CATEDRAL.yMax + m; }
    }
}
