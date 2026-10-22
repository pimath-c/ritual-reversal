// O texto e as caixas do HUD como o CSS do navegador os põe: cada linha numa caixa de altura line-height com a linha de
// base onde o navegador a põe (meia entrelinha em cima, depois a subida da fonte), quebra de linha feita aqui (o GUIStyle
// não tem line-height), sombra do texto (text-shadow 0 1px 2px #000) com uma cópia escura um pixel abaixo, molduras de 1 px,
// degradês (os linear-gradient das barras e das faixas do HUD) e o brilho suave dos box-shadow. As fontes do navegador:
// Barlow Condensed em quatro pesos (400 do corpo, 500, 600 dos números, 700 das teclas) e IM Fell English.
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static Font fonteCond400, fonteCond600, fonteCond700;
        // subida e descida de cada fonte (hhea, em em): a altura "normal" da linha no navegador é a soma
        static float SubidaEm(Font f) { return f != null && f == fonteSerif || f != null && f == fonteSerifIt ? .905f : 1f; }
        static float DescidaEm(Font f) { return f != null && f == fonteSerif || f != null && f == fonteSerifIt ? .363f : .2f; }
        static float AlturaNormal(GUIStyle st) { return (SubidaEm(st.font) + DescidaEm(st.font)) * st.fontSize; }

        readonly Dictionary<string, GUIStyle> estilosCss = new Dictionary<string, GUIStyle>();
        // peso: 400, 500, 600, 700 (Barlow) ou "serif"/"serifIt"
        GUIStyle Css(string peso, int tam, Color cor, TextAnchor al = TextAnchor.UpperLeft)
        {
            string k = peso + "|" + tam + "|" + ColorUtility.ToHtmlStringRGBA(cor) + "|" + (int)al; GUIStyle s;
            if (estilosCss.TryGetValue(k, out s)) return s;
            var f = peso == "serif" ? fonteSerif : peso == "serifIt" ? fonteSerifIt : peso == "700" ? fonteCond700 : peso == "600" ? fonteCond600 : peso == "500" ? fonteCond : fonteCond400;
            s = new GUIStyle(GUI.skin.label) { fontSize = tam, wordWrap = false, richText = true, alignment = al, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0), clipping = TextClipping.Overflow, fontStyle = FontStyle.Normal };
            s.font = f ?? fonteCond; s.normal.textColor = cor; estilosCss[k] = s; return s;
        }
        static float Larg(string txt, GUIStyle st) { return st.CalcSize(new GUIContent(txt)).x; }
        static string SemCor(string t) { return System.Text.RegularExpressions.Regex.Replace(t, "</?color[^>]*>", ""); }
        // uma linha de texto numa caixa de linha do CSS (topo y, altura lh), alinhada em x..x+w pelo alinhamento do estilo
        void Linha(float x, float y, float w, float lh, string txt, GUIStyle st, float sombra = 0)
        {
            float base_ = y + (lh - AlturaNormal(st)) / 2 + SubidaEm(st.font) * st.fontSize;
            var r = new Rect(x, base_ - Mathf.Ceil(SubidaEm(st.font) * st.fontSize), w, Mathf.Ceil(AlturaNormal(st)) + 2);
            if (sombra > 0) { var ss = new GUIStyle(st); var c = Color.black; c.a = sombra * st.normal.textColor.a; ss.normal.textColor = c; GUI.Label(new Rect(r.x, r.y + 1, r.width, r.height), SemCor(txt), ss); }
            GUI.Label(r, txt, st);
        }
        // quebra como o navegador (nas palavras, dentro da largura) e devolve as linhas
        static List<string> Quebrar(string txt, GUIStyle st, float w)
        {
            var o = new List<string>(); foreach (var par in txt.Split('\n'))
            {
                string linha = ""; foreach (var p in par.Split(' '))
                {
                    string tenta = linha.Length == 0 ? p : linha + " " + p;
                    if (linha.Length > 0 && Larg(tenta, st) > w) { o.Add(linha); linha = p; } else linha = tenta;
                }
                o.Add(linha);
            }
            return o;
        }
        // um parágrafo (quebrado na largura w) com a altura de linha lh; devolve a altura usada
        float Paragrafo(float x, float y, float w, float lh, string txt, GUIStyle st, float sombra = 0)
        {
            var ls = Quebrar(txt, st, w); for (int i = 0; i < ls.Count; i++) Linha(x, y + i * lh, w, lh, ls[i], st, sombra); return ls.Count * lh;
        }
        static float AlturaParagrafo(string txt, GUIStyle st, float w, float lh) { return Quebrar(txt, st, w).Count * lh; }
        void Moldura(Rect r, Color c, float e = 1)
        {
            Caixa(new Rect(r.x, r.y, r.width, e), c); Caixa(new Rect(r.x, r.yMax - e, r.width, e), c); Caixa(new Rect(r.x, r.y + e, e, r.height - 2 * e), c); Caixa(new Rect(r.xMax - e, r.y + e, e, r.height - 2 * e), c);
        }
        // degradê de duas cores (vertical: a de cima primeiro), esticado no retângulo como o linear-gradient
        readonly Dictionary<string, Texture2D> degrades = new Dictionary<string, Texture2D>();
        Texture2D Degrade(Color a, Color b, bool vertical)
        {
            string k = ColorUtility.ToHtmlStringRGBA(a) + ColorUtility.ToHtmlStringRGBA(b) + vertical; Texture2D t;
            if (degrades.TryGetValue(k, out t)) return t;
            int n = 64; t = vertical ? new Texture2D(1, n, TextureFormat.RGBA32, false) : new Texture2D(n, 1, TextureFormat.RGBA32, false); var p = new Color[n];
            // linha 0 é a de baixo: no vertical a cor de cima vai no fim
            for (int i = 0; i < n; i++) { float f = (i + .5f) / n; p[i] = vertical ? Color.Lerp(b, a, f) : Color.Lerp(a, b, f); }
            t.SetPixels(p); t.Apply(false, true); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; degrades[k] = t; return t;
        }
        void PintarDegrade(Rect r, Color a, Color b, bool vertical) { GUI.DrawTexture(r, Degrade(a, b, vertical)); }
    }
}
