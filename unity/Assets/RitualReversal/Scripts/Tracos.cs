// Traços do IMGUI para as camadas das telas que mudam a cada quadro (marcadores da planta, rastros do replay, linha do
// tempo): discos, anéis, arcos, segmentos, losangos e a seta do jogador, tingidos com GUI.color. Os que giram (segmento,
// losango, seta) só valem fora de GUILayout.BeginArea e de rolagens.
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal
{
    public static class Tracos
    {
        static Texture2D disco, seta; static readonly Dictionary<int, Texture2D> aneis = new Dictionary<int, Texture2D>();
        static Texture2D Gerar(int N, System.Func<float, float, float> cob)
        {
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false); var p = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) p[y * N + x] = new Color(1, 1, 1, Mathf.Clamp01(cob((x + .5f) / N * 2 - 1, (y + .5f) / N * 2 - 1)));
            t.SetPixels(p); t.Apply(false, true); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; return t;
        }
        const int N = 96;
        static Texture2D TexDisco { get { if (disco == null) disco = Gerar(N, (x, y) => (1 - Mathf.Sqrt(x * x + y * y)) * N / 2 + .5f); return disco; } }
        // anel com a espessura como fração do raio (quantizada)
        static Texture2D Anel(float frac)
        {
            int q = Mathf.Clamp(Mathf.RoundToInt(frac * 40), 1, 40); Texture2D t;
            if (!aneis.TryGetValue(q, out t)) { float f = q / 40f; aneis[q] = t = Gerar(N, (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return (f / 2 - Mathf.Abs(d - (1 - f / 2))) * N / 2 + .5f; }); }
            return t;
        }
        // seta do jogador (M0 -9 L6 6 L0 2 L-6 6Z), ponta para cima
        static Texture2D Seta
        {
            get
            {
                if (seta != null) return seta;
                // a seta é côncava: dentro ou fora pela regra par-ímpar, e a distância até o contorno mais próximo
                var P = new[] { new Vector2(0, -9), new Vector2(6, 6), new Vector2(0, 2), new Vector2(-6, 6) };
                return seta = Gerar(N, (x, y) =>
                {
                    var q = new Vector2(x * 9, -y * 9); bool dentro = false; float d = 1e9f;
                    for (int i = 0, j = P.Length - 1; i < P.Length; j = i++)
                    {
                        Vector2 a = P[j], b = P[i], ab = b - a;
                        if ((b.y > q.y) != (a.y > q.y) && q.x < (a.x - b.x) * (q.y - b.y) / (a.y - b.y) + b.x) dentro = !dentro;
                        float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude); d = Mathf.Min(d, Vector2.Distance(q, a + ab * t));
                    }
                    return (dentro ? d : -d) * N / 18 + .5f;
                });
            }
        }
        static void Tinge(Rect r, Texture2D t, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, t); GUI.color = o; }
        // gira o que vier depois em volta de p (graus no sentido horário da tela). O GUIUtility.RotateAroundPivot põe o giro
        // por fora do GUI.matrix sem passar o pivô pela escala: fora de 1080 linhas de tela o giro sai em volta do ponto errado.
        // Aqui o giro entra por dentro da matriz; vale fora de áreas e grupos (lá dentro o deslocamento do grupo ficaria de fora)
        public static void Girar(float graus, Vector2 p) { GUI.matrix = GUI.matrix * Matrix4x4.TRS(p, Quaternion.Euler(0, 0, graus), Vector3.one) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one); }
        public static void Disco(float x, float y, float r, Color c) { Tinge(new Rect(x - r, y - r, 2 * r, 2 * r), TexDisco, c); }
        // stroke do SVG: a espessura fica metade para dentro e metade para fora do raio
        public static void Anel(float x, float y, float r, float e, Color c) { float R = r + e / 2; Tinge(new Rect(x - R, y - R, 2 * R, 2 * R), Anel(e / R), c); }
        public static void Seg(Vector2 a, Vector2 b, float e, Color c)
        {
            var d = b - a; float len = d.magnitude; if (len < .01f) return; var m = GUI.matrix;
            Girar(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a); Tinge(new Rect(a.x, a.y - e / 2, len, e), Texture2D.whiteTexture, c); GUI.matrix = m;
        }
        public static void Polilinha(IList<Vector2> p, float e, Color c) { for (int i = 0; i + 1 < p.Count; i++) Seg(p[i], p[i + 1], e, c); }
        // arco do progresso (stroke-dasharray com rotate(-90)): de cima, no sentido horário
        public static void Arco(float x, float y, float r, float e, float frac, Color c)
        {
            frac = Mathf.Clamp01(frac); if (frac <= 0) return; int n = Mathf.Max(2, Mathf.CeilToInt(2 * Mathf.PI * r * frac / Mathf.Max(.8f, e * .45f)));
            for (int i = 0; i <= n; i++) { float a = -Mathf.PI / 2 + 2 * Mathf.PI * frac * i / n; Disco(x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r, e / 2, c); }
        }
        public static void Losango(float x, float y, float meio, Color c, Color borda)
        {
            var m = GUI.matrix; Girar(45, new Vector2(x, y));
            if (borda.a > 0) Tinge(new Rect(x - meio - .7f, y - meio - .7f, 2 * meio + 1.4f, 2 * meio + 1.4f), Texture2D.whiteTexture, borda);
            Tinge(new Rect(x - meio, y - meio, 2 * meio, 2 * meio), Texture2D.whiteTexture, c); GUI.matrix = m;
        }
        public static void SetaJogador(float x, float y, float escala, float grausHorario, Color c)
        {
            var m = GUI.matrix; Girar(grausHorario, new Vector2(x, y)); float R = 9 * escala; Tinge(new Rect(x - R, y - R, 2 * R, 2 * R), Seta, c); GUI.matrix = m;
        }
    }
}
