// As três formas de árvore do protótipo (buildFloresta): anciã, alta e morta. Cada uma vira três malhas:
// casca (tronco torneado e torcido, raízes, galhos), copa (tufos facetados) e musgo pendurado. Conta pura.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal.Visual
{
    public static class Arvores
    {
        const float PI = (float)Math.PI;
        public static Geo Tufo(float x, float y, float z, float r, float achata = .72f) { return Geo.Icosaedro(r).Escalar(1, achata, 1).Mover(x, y, z); }

        public class Modelo { public Geo casca, copa, musgo; public Vector3 cor; }
        // as três formas de árvore do protótipo
        public static Modelo[] Modelos()
        {
            Func<float[,], int, float, float, Geo> fuste = (perfil, seg, torce, sem) =>
            {
                var g = Geo.Torno(perfil, seg);
                for (int i = 0; i < g.p.Count; i++)
                {
                    var q = g.p[i]; float a = Mathf.Atan2(q.z, q.x), y = q.y, nd = 1 + .16f * Mathf.Sin(a * 3 + y * .8f + sem) + .09f * Mathf.Sin(a * 5 - y * 1.9f + sem * 2);
                    g.p[i] = new Vector3(q.x * nd + Mathf.Sin(y * .32f + sem) * torce * y / 10, y, q.z * nd + Mathf.Cos(y * .27f + sem * 1.3f) * torce * y / 10);
                }
                return g.NormaisDasFaces().Orientar();
            };
            Func<int, float, float, float, int, List<Geo>> raizes = (n, r0, len, esp, sem) =>
            {
                var o = new List<Geo>(); for (int k = 0; k < n; k++) o.Add(Geo.Cone(esp, len, 5).Mover(0, len / 2, 0).GirarZ(-(PI / 2 + .22f + ((k * 7 + sem) % 3) * .05f)).Mover(r0 * .35f, .9f, 0).GirarY(k / (float)n * PI * 2 + sem * .3f)); return o;
            };
            Func<float, float, float, float, float, float, KeyValuePair<Geo, Vector3>> galho = (y, incl, dir, len, r1, r2) =>
            {
                var g = Geo.Cilindro(r2, r1, len, 6, 1, true).Mover(0, len / 2, 0).GirarZ(-incl).Mover(0, y, 0).GirarY(dir);
                float px = Mathf.Sin(incl) * len, py = y + Mathf.Cos(incl) * len; return new KeyValuePair<Geo, Vector3>(g, new Vector3(px * Mathf.Cos(dir), py, -px * Mathf.Sin(dir)));
            };
            Func<Vector3, float, float, List<Geo>> barba = (p, alt, giro) => { var o = new List<Geo>(); foreach (float d in new[] { 0, PI / 2 }) o.Add(Geo.Plano(1.3f, alt).Mover(0, -alt / 2, 0).GirarY(giro + d).Mover(p.x, p.y, p.z)); return o; };
            Func<Vector3, float, float, Geo> tufo = (p, r, ach) => Tufo(p.x, p.y, p.z, r, ach);
            var M = new Modelo[3];
            { // anciã
                var gal = new List<KeyValuePair<Geo, Vector3>>(); for (int k = 0; k < 4; k++) gal.Add(galho(6.2f + k * 1.1f, .8f + (k % 2) * .25f, k * 1.7f + .4f, 5.5f - k * .4f, .62f - k * .06f, .22f));
                var topo = galho(9.5f, .25f, 2.2f, 3.5f, .5f, .2f);
                var casca = new List<Geo> { fuste(new float[,] { { 2.5f, 0 }, { 1.7f, .6f }, { 1.3f, 1.8f }, { 1.15f, 4 }, { 1, 7 }, { .8f, 9.5f }, { .5f, 11.5f } }, 9, 1.2f, 1.3f) }; casca.AddRange(raizes(6, 1.2f, 3.6f, .55f, 1)); foreach (var q in gal) casca.Add(q.Key); casca.Add(topo.Key);
                var copa = new List<Geo>(); for (int k = 0; k < gal.Count; k++) copa.Add(tufo(gal[k].Value, 3.3f + (k % 2) * .7f, .72f)); copa.Add(tufo(topo.Value, 3.6f, .72f)); copa.Add(tufo(new Vector3(0, 13.5f, 0), 3.4f, .6f)); copa.Add(tufo(new Vector3(1.8f, 12.2f, 1.5f), 2.6f, .72f));
                var musgo = new List<Geo>(); for (int k = 0; k < gal.Count; k++) { var p = gal[k].Value; musgo.AddRange(barba(new Vector3(p.x * .7f, p.y - 1.2f, p.z * .7f), 2.8f + k * .4f, k)); }
                M[0] = new Modelo { casca = Geo.Unir(casca), copa = Geo.Unir(copa), musgo = Geo.Unir(musgo), cor = new Vector3(.1f, .13f, .085f) };
            }
            { // alta
                var gal = new List<KeyValuePair<Geo, Vector3>>(); for (int k = 0; k < 3; k++) gal.Add(galho(12.5f + k * 1.3f, .7f, k * 2.1f + .9f, 3, .32f, .12f));
                var toco = new List<KeyValuePair<Geo, Vector3>> { galho(5.5f, 1.1f, .7f, 1.6f, .22f, .14f), galho(8, 1, 3.4f, 1.8f, .2f, .12f) };
                var casca = new List<Geo> { fuste(new float[,] { { 1.3f, 0 }, { .9f, .5f }, { .72f, 1.5f }, { .64f, 6 }, { .52f, 11 }, { .36f, 15 }, { .18f, 18 } }, 8, .6f, 2.7f) }; casca.AddRange(raizes(5, .7f, 2.2f, .32f, 2)); foreach (var q in gal) casca.Add(q.Key); foreach (var q in toco) casca.Add(q.Key);
                var copa = new List<Geo>(); foreach (var q in gal) copa.Add(tufo(q.Value, 2.9f, .72f)); copa.Add(tufo(new Vector3(0, 17.8f, 0), 3.2f, .8f)); copa.Add(tufo(new Vector3(.8f, 15.8f, -1), 2.8f, .72f)); copa.Add(tufo(new Vector3(-1.1f, 16.4f, .9f), 2.7f, .72f));
                var musgo = new List<Geo>(); for (int k = 0; k < toco.Count; k++) musgo.AddRange(barba(toco[k].Value, 2.2f + k * .6f, k * 2));
                M[1] = new Modelo { casca = Geo.Unir(casca), copa = Geo.Unir(copa), musgo = Geo.Unir(musgo), cor = new Vector3(.085f, .11f, .08f) };
            }
            { // morta
                var gal = new List<KeyValuePair<Geo, Vector3>>(); for (int k = 0; k < 5; k++) gal.Add(galho(4.5f + k * 1.3f, .5f + (k % 3) * .2f, k * 1.3f + .2f, 3.4f - k * .35f, .26f - k * .03f, .06f));
                var casca = new List<Geo> { fuste(new float[,] { { 1.05f, 0 }, { .72f, .5f }, { .58f, 1.5f }, { .5f, 5 }, { .38f, 8.5f }, { .2f, 11.5f }, { .06f, 12.5f } }, 7, 2.2f, 4.1f) }; casca.AddRange(raizes(5, .58f, 2, .28f, 3)); foreach (var q in gal) casca.Add(q.Key);
                var musgo = new List<Geo>(); for (int k = 0; k < gal.Count; k++) { var p = gal[k].Value; musgo.AddRange(barba(new Vector3(p.x * .85f, p.y - .3f, p.z * .85f), 2.4f + (k % 3) * .9f, k * 1.1f)); }
                M[2] = new Modelo { casca = Geo.Unir(casca), copa = null, musgo = Geo.Unir(musgo) };
            }
            return M;
        }
    }
}
