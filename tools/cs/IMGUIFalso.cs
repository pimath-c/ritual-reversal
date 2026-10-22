// IMGUI, entrada, áudio e rede de mentira, só para o teste das telas (tools/unity-telas.js), junto com UnityFalso.cs.
// O GUILayout é o do Unity em miniatura: na passada de Layout cada chamada vira uma entrada num grupo, o fim da passada
// calcula larguras e alturas como o GUILayoutGroup do Unity (CalcWidth, SetHorizontal, CalcHeight, SetVertical, margens
// que se fundem, FlexibleSpace com peso 10000, rolagem) e as outras passadas pegam os retângulos pela ordem. Daí saem os
// mesmos erros do Unity: "Getting control N's position in a group with only N controls" quando o Repaint chama mais
// controles que o Layout, "Mismatched LayoutGroup" e End sem Begin. O texto é medido com as fontes de verdade (cmap e hmtx
// dos TTF: as do jogo em Resources/Fontes e, no lugar do Arial do Unity, a Liberation Sans). Com GUILayout.foto definido, o
// Repaint grava cada coisa desenhada (retângulos, texturas, corridas de texto já quebradas em linhas, com a matriz e o
// recorte) num JSON que tools/foto-imgui.py transforma em PNG. Os botões podem ser apertados pelo texto (GUILayout.Apertar).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using FalsoIMGUI;

namespace UnityEngine
{
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextClipping { Overflow, Clip }
    public enum EventType { MouseDown, MouseUp, MouseMove, MouseDrag, KeyDown, KeyUp, ScrollWheel, Repaint, Layout, Used }
    public enum KeyCode { None, Return, KeypadEnter, Escape, Tab, Space, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, UpArrow, DownArrow, LeftArrow, RightArrow, BackQuote, CapsLock, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9, Mouse0 = 323, Mouse1, Mouse2, Mouse3, Mouse4, Mouse5, Mouse6 }
    public enum CursorLockMode { None, Locked, Confined }
    public enum ScaleMode { StretchToFill, ScaleAndCrop, ScaleToFit }
    public class RectOffset
    {
        public int left, right, top, bottom; public RectOffset() { } public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; }
        public int horizontal { get { return left + right; } } public int vertical { get { return top + bottom; } }
        public RectOffset Copia() { return new RectOffset(left, right, top, bottom); }
    }
    // a fonte guarda de onde veio (Resources.Load<Font>("Fontes/X") tem name "X", como no Unity)
    public class Font : Object { public string arquivo; }
    public class GUIStyleState { public Color textColor = Color.white; public Texture2D background; public GUIStyleState Copia() { return new GUIStyleState { textColor = textColor, background = background }; } }
    public class GUIStyle
    {
        public int fontSize; public bool wordWrap, richText = true, stretchWidth = true, stretchHeight; public TextAnchor alignment; public FontStyle fontStyle; public TextClipping clipping; public Font font;
        public float fixedWidth, fixedHeight; public string nome = "";
        public RectOffset padding = new RectOffset(), margin = new RectOffset(), border = new RectOffset();
        public GUIStyleState normal = new GUIStyleState(), focused = new GUIStyleState(), hover = new GUIStyleState(), active = new GUIStyleState(), onNormal = new GUIStyleState();
        public static readonly GUIStyle none = new GUIStyle { nome = "none" };
        public GUIStyle() { }
        public GUIStyle(GUIStyle o)
        {
            if (o == null) return; fontSize = o.fontSize; font = o.font; wordWrap = o.wordWrap; richText = o.richText; alignment = o.alignment; fontStyle = o.fontStyle; clipping = o.clipping;
            stretchWidth = o.stretchWidth; stretchHeight = o.stretchHeight; fixedWidth = o.fixedWidth; fixedHeight = o.fixedHeight; nome = o.nome == "none" ? "" : o.nome;
            padding = o.padding.Copia(); margin = o.margin.Copia(); border = o.border.Copia();
            normal = o.normal.Copia(); focused = o.focused.Copia(); hover = o.hover.Copia(); active = o.active.Copia(); onNormal = o.onNormal.Copia();
        }
        public Vector2 CalcSize(GUIContent c)
        {
            var l = TextoFalso.Diagramar(c != null ? c.text : "", this, 0); var s = TextoFalso.Tamanho(l);
            return new Vector2(fixedWidth != 0 ? fixedWidth : s.x + padding.horizontal, fixedHeight != 0 ? fixedHeight : s.y + padding.vertical);
        }
        public float CalcHeight(GUIContent c, float largura)
        {
            if (fixedHeight != 0) return fixedHeight;
            var l = TextoFalso.Diagramar(c != null ? c.text : "", this, wordWrap ? largura - padding.horizontal : 0); return TextoFalso.Tamanho(l).y + padding.vertical;
        }
        // com quebra de linha: a menor largura é a da palavra mais longa; a maior, a do texto numa linha só
        public void CalcMinMaxWidth(GUIContent c, out float min, out float max)
        {
            var s = CalcSize(c); max = s.x; min = s.x;
            if (wordWrap && fixedWidth == 0) { float m = 0; foreach (var p in TextoFalso.SemTags(c != null ? c.text : "", this).Split(' ', '\n')) m = Math.Max(m, CalcSize(new GUIContent(TextoFalso.ComoTexto(p, this))).x); min = Math.Min(max, m); }
        }
    }
    public class GUIContent { public string text, tooltip; public GUIContent(string t) { text = t; } public GUIContent(string t, string tip) { text = t; tooltip = tip; } }
    public class GUISkin
    {
        public GUIStyle label, button, toggle, textField, textArea, box, horizontalSlider, horizontalSliderThumb, verticalScrollbar, horizontalScrollbar, scrollView; public Font font;
        // os números e o jeito do skin padrão do Unity (o de jogo, escuro); as texturas são desenhadas aqui mesmo
        public GUISkin()
        {
            Func<string, int, int, int, int, GUIStyle> E = (n, ml, mr, mt, mb) => new GUIStyle { nome = n, margin = new RectOffset(ml, mr, mt, mb) };
            var claro = new Color(.9f, .9f, .9f, 1);
            label = E("label", 4, 4, 2, 2); label.padding = new RectOffset(2, 2, 1, 2); label.wordWrap = true; label.normal.textColor = claro; label.clipping = TextClipping.Clip;
            button = E("button", 4, 4, 2, 2); button.padding = new RectOffset(6, 6, 3, 3); button.border = new RectOffset(6, 6, 6, 4); button.alignment = TextAnchor.MiddleCenter; button.clipping = TextClipping.Clip;
            button.normal.background = TexturaFalsa.Caixa(new Color(.36f, .36f, .36f), new Color(.27f, .27f, .27f), new Color(.1f, .1f, .1f), 3); button.normal.textColor = claro;
            button.hover.background = TexturaFalsa.Caixa(new Color(.42f, .42f, .42f), new Color(.32f, .32f, .32f), new Color(.1f, .1f, .1f), 3); button.hover.textColor = Color.white;
            button.active.background = TexturaFalsa.Caixa(new Color(.2f, .2f, .2f), new Color(.24f, .24f, .24f), new Color(.08f, .08f, .08f), 3);
            toggle = E("toggle", 4, 4, 2, 2); toggle.padding = new RectOffset(15, 0, 3, 0); toggle.normal.textColor = claro; toggle.onNormal.textColor = claro;
            textField = E("textField", 4, 4, 2, 2); textField.padding = new RectOffset(3, 3, 1, 2); textField.border = new RectOffset(3, 3, 3, 3); textField.clipping = TextClipping.Clip; textField.normal.textColor = claro; textField.focused.textColor = claro;
            textField.normal.background = textField.focused.background = TexturaFalsa.Caixa(new Color(.15f, .15f, .15f), new Color(.17f, .17f, .17f), new Color(.06f, .06f, .06f), 2);
            textArea = new GUIStyle(textField) { nome = "textArea", wordWrap = true };
            box = E("box", 4, 4, 4, 4); box.padding = new RectOffset(4, 4, 4, 4); box.border = new RectOffset(6, 6, 6, 6); box.alignment = TextAnchor.UpperCenter; box.normal.textColor = new Color(.79f, .79f, .79f);
            box.normal.background = TexturaFalsa.Caixa(new Color(.1f, .1f, .1f, .55f), new Color(.1f, .1f, .1f, .55f), new Color(.05f, .05f, .05f, .7f), 3);
            horizontalSlider = E("horizontalSlider", 4, 4, 2, 2); horizontalSlider.fixedHeight = 12; horizontalSlider.padding = new RectOffset(-1, -1, 0, 0);
            horizontalSliderThumb = E("thumb", 0, 0, 0, 0); horizontalSliderThumb.fixedWidth = 10; horizontalSliderThumb.fixedHeight = 12;
            verticalScrollbar = E("verticalScrollbar", 1, 4, 0, 0); verticalScrollbar.fixedWidth = 15; verticalScrollbar.stretchHeight = true; verticalScrollbar.stretchWidth = false;
            horizontalScrollbar = E("horizontalScrollbar", 4, 4, 1, 4); horizontalScrollbar.fixedHeight = 15;
            scrollView = E("scrollView", 0, 0, 0, 0);
        }
    }
    public class GUILayoutOption { public int tipo; public float valor; public GUILayoutOption(int t, float v) { tipo = t; valor = v; } }
    public class Event
    {
        public EventType type = EventType.Layout; public KeyCode keyCode; public Vector2 mousePosition; public int button;
        public static Event current = new Event();
        public void Use() { type = EventType.Used; }
    }

    public static class GUI
    {
        public static GUISkin skin = new GUISkin(); public static Color color = Color.white; public static Matrix4x4 matrix = Matrix4x4.identity; public static bool enabled = true, changed;
        public static string tooltip = ""; static string proximoNome, foco;
        public static void Label(Rect r, string t) { Label(r, t, skin.label); }
        public static void Label(Rect r, string t, GUIStyle s) { GUILayout.Texto(t); Desenho2D.Estilo(r, t, s); }
        public static void Label(Rect r, GUIContent c, GUIStyle s) { Label(r, c.text, s); }
        public static void Box(Rect r, string t, GUIStyle s) { Desenho2D.Estilo(r, t, s); }
        public static void DrawTexture(Rect r, Texture t) { DrawTexture(r, t, ScaleMode.StretchToFill); }
        public static void DrawTexture(Rect r, Texture t, ScaleMode m) { if (t == null) throw new Exception("GUI.DrawTexture com textura nula"); Desenho2D.Textura(r, t, Color.white, null, m); }
        public static string TextField(Rect r, string t, int max, GUIStyle s) { string n = proximoNome; proximoNome = null; Desenho2D.Estilo(r, t, s, false, n != null && n == foco); return t; }
        public static void SetNextControlName(string n) { proximoNome = n; } public static void FocusControl(string n) { foco = n; } public static string GetNameOfFocusedControl() { return foco ?? ""; }
        public static bool Button(Rect r, string t) { return Button(r, t, skin.button); }
        public static bool Button(Rect r, string t, GUIStyle s) { Desenho2D.Estilo(r, t, s); return GUILayout.Apertado(t); }
        public static bool Toggle(Rect r, bool v, string t, GUIStyle s) { Desenho2D.Estilo(r, t, s, v); GUILayout.Texto(t); return v; }
        public static float HorizontalSlider(Rect r, float v, float a, float b)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Desenho2D.Ret(new Rect(r.x, r.y + r.height / 2 - 2, r.width, 4), new Color(.12f, .12f, .12f, 1)); float k = b != a ? Mathf.Clamp01((v - a) / (b - a)) : 0;
                var th = skin.horizontalSliderThumb; Desenho2D.Textura(new Rect(r.x + k * (r.width - th.fixedWidth), r.y, th.fixedWidth, th.fixedHeight), skin.button.normal.background, Color.white, new RectOffset(4, 4, 4, 4));
            }
            return v;
        }
        public static void BeginGroup(Rect r) { Desenho2D.Empilhar(r, Vector2.zero); } public static void EndGroup() { Desenho2D.Desempilhar(); }
        // GUI.BeginScrollView: a barra aparece quando o conteúdo é maior que a vista; a posição é limitada fora do Layout
        public static Vector2 BeginScrollView(Rect pos, Vector2 rol, Rect vista)
        {
            var t = Event.current.type; var rec = pos;
            if (t != EventType.Layout && t != EventType.Used)
            {
                var bv = skin.verticalScrollbar; var bh = skin.horizontalScrollbar; bool ph = vista.width > rec.width, pv = false;
                if (ph) rec.height -= bh.fixedHeight + bh.margin.top;
                if (vista.height > rec.height) { rec.width -= bv.fixedWidth + bv.margin.left; pv = true; if (!ph && vista.width > rec.width) { ph = true; rec.height -= bh.fixedHeight + bh.margin.top; } }
                rol.x = ph ? Mathf.Clamp(rol.x, 0, Math.Max(0, vista.width - rec.width)) : 0; rol.y = pv ? Mathf.Clamp(rol.y, 0, Math.Max(0, vista.height - rec.height)) : 0;
                if (t == EventType.Repaint && pv)
                {
                    var tr = new Rect(rec.xMax + bv.margin.left, rec.y, bv.fixedWidth, rec.height); Desenho2D.Ret(tr, new Color(.13f, .13f, .13f, .9f));
                    float k = rec.height / vista.height, hTh = Math.Max(16, tr.height * k), yTh = tr.y + (tr.height - hTh) * (vista.height > rec.height ? rol.y / (vista.height - rec.height) : 0);
                    Desenho2D.Textura(new Rect(tr.x + 2, yTh, tr.width - 4, hTh), skin.button.normal.background, Color.white, new RectOffset(4, 4, 4, 4));
                }
                if (t == EventType.Repaint && ph) Desenho2D.Ret(new Rect(rec.x, rec.yMax + bh.margin.top, rec.width, bh.fixedHeight), new Color(.13f, .13f, .13f, .9f));
            }
            Desenho2D.Empilhar(rec, rol + new Vector2(vista.x, vista.y)); return rol;
        }
        public static void EndScrollView() { Desenho2D.Desempilhar(); }
    }
    public class ExitGUIException : Exception { }
    public static class GUIUtility
    {
        public static string systemCopyBuffer = ""; public static int hotControl;
        public static void ExitGUI() { throw new ExitGUIException(); }
        // como o do Unity: o pivô vai para fora dos grupos sem passar pela matriz, e a rotação entra por fora dela
        // (com o GUI.matrix escalado o giro sai em volta do ponto errado)
        public static void RotateAroundPivot(float ang, Vector2 p)
        {
            var d = Desenho2D.Deslocamento; var q = new Vector3(p.x + d.x, p.y + d.y, 0);
            GUI.matrix = Matrix4x4.TRS(q, Quaternion.Euler(0, 0, ang), new Vector3(1, 1, 1)) * Matrix4x4.TRS(-q, Quaternion.identity, new Vector3(1, 1, 1)) * GUI.matrix;
        }
        public static Vector2 GUIToScreenPoint(Vector2 p) { var d = Desenho2D.Deslocamento; var v = GUI.matrix.MultiplyPoint3x4(new Vector3(p.x + d.x, p.y + d.y, 0)); return new Vector2(v.x, v.y); }
    }
    public static class GUILayout
    {
        // a pilha dos grupos abertos nesta passada e os erros de pareamento achados
        public static readonly List<string> pilha = new List<string>(); public static readonly List<string> erros = new List<string>();
        public static readonly HashSet<string> textos = new HashSet<string>(); static string apertar; public static int controles;
        public static void Apertar(string texto) { apertar = texto; }
        public static bool Pendente { get { return apertar != null; } }
        static string Limpo(string t) { return System.Text.RegularExpressions.Regex.Replace(t ?? "", "<[^>]*>", ""); }
        public static void Texto(string t) { controles++; textos.Add(Limpo(t)); }
        // como no Unity, o botão responde na passada do clique (MouseUp), não no Layout nem no Repaint
        public static bool Apertado(string t) { Texto(t); if (apertar != null && Limpo(t).Contains(apertar) && Event.current.type == EventType.MouseUp) { apertar = null; return true; } return false; }
        static void Abre(string k) { controles++; pilha.Add(k); }
        static void Fecha(string k) { if (pilha.Count == 0 || pilha[pilha.Count - 1] != k) { erros.Add("End" + k + " sem Begin" + k + " (aberto: " + (pilha.Count > 0 ? pilha[pilha.Count - 1] : "nada") + ")"); return; } pilha.RemoveAt(pilha.Count - 1); }

        // ---- o cache do layout (GUILayoutUtility.current): a raiz, as áreas e a pilha dos grupos ----
        static Grupo raiz = new Grupo(), janelas = new Grupo(); static readonly List<Grupo> grupos = new List<Grupo>(); static Grupo topo;
        public static readonly GUIStyle estiloEspaco = new GUIStyle { nome = "espaço", stretchWidth = false };
        static bool EhLayout { get { return Event.current.type == EventType.Layout; } }
        // GUILayoutUtility.Begin: no Layout tudo novo; nas outras passadas os cursores voltam ao começo
        public static void Comeco()
        {
            pilha.Clear(); Desenho2D.Limpar(); GUI.matrix = Matrix4x4.identity; GUI.color = Color.white; GUI.enabled = true;
            if (EhLayout) { raiz = new Grupo(); janelas = new Grupo(); } else { raiz.cursor = 0; janelas.cursor = 0; }
            grupos.Clear(); grupos.Add(raiz); topo = raiz;
        }
        // GUILayoutUtility.Layout, no fim do Layout: a raiz na tela, cada área no seu retângulo
        public static void Fim()
        {
            if (!EhLayout) return;
            raiz.CalcW(); raiz.SetH(0, Math.Min(Screen.width, raiz.maxW)); raiz.CalcH(); raiz.SetV(0, Math.Min(Screen.height, raiz.maxH));
            foreach (Grupo g in janelas.ents) { float a = g.minW, b = g.maxW; g.CalcW(); g.SetH(g.rect.x, Mathf.Clamp(g.maxW, a, b)); a = g.minH; b = g.maxH; g.CalcH(); g.SetV(g.rect.y, Mathf.Clamp(g.maxH, a, b)); }
        }
        static readonly Rect fantasma = new Rect(0, 0, 1, 1);
        static Rect Pegar(Func<Entrada> criar)
        {
            switch (Event.current.type)
            {
                case EventType.Layout: topo.ents.Add(criar()); return fantasma;
                case EventType.Used: return fantasma;
                default: return topo.Proximo().rect;
            }
        }
        static Rect Pegar(GUIContent c, GUIStyle s, GUILayoutOption[] o)
        {
            return Pegar(() =>
            {
                if (s.fixedHeight == 0 && s.wordWrap) return new EntradaTexto(s, c, o);
                var t = s.CalcSize(c); var e = new Entrada((float)Math.Ceiling(Math.Max(t.x, 1)), (float)Math.Ceiling(Math.Max(t.x, 1)), (float)Math.Ceiling(Math.Max(t.y, 1)), (float)Math.Ceiling(Math.Max(t.y, 1)), s); e.Opcoes(o); return e;
            });
        }
        static Rect Pegar(float w, float h, GUIStyle s, GUILayoutOption[] o) { return Pegar(() => { var e = new Entrada(w, w, h, h, s); e.Opcoes(o); return e; }); }
        static T AbrirGrupo<T>(GUIStyle s, GUILayoutOption[] o, bool vertical) where T : Grupo, new()
        {
            T g;
            if (EhLayout || Event.current.type == EventType.Used) { g = new T(); g.Estilo(s ?? GUIStyle.none); g.vertical = vertical; g.Opcoes(o); topo.ents.Add(g); }
            else { g = topo.Proximo() as T; if (g == null) throw new ArgumentException("GUILayout: Mismatched LayoutGroup." + Event.current.type); g.cursor = 0; }
            grupos.Add(g); topo = g; return g;
        }
        static void FecharGrupo() { if (grupos.Count > 1) { grupos.RemoveAt(grupos.Count - 1); topo = grupos[grupos.Count - 1]; } }

        public static void BeginArea(Rect r)
        {
            Abre("Area"); Grupo g;
            if (EhLayout || Event.current.type == EventType.Used) { g = new Grupo(); janelas.ents.Add(g); g.reset = true; g.minW = g.maxW = r.width; g.minH = g.maxH = r.height; g.rect = new Rect(r.x, r.y, r.width, r.height); }
            else { g = janelas.Proximo() as Grupo; if (g == null) throw new ArgumentException("GUILayout: Mismatched LayoutGroup." + Event.current.type); g.cursor = 0; }
            grupos.Add(g); topo = g; GUI.BeginGroup(g.rect);
        }
        public static void EndArea() { Fecha("Area"); FecharGrupo(); GUI.EndGroup(); }
        public static void BeginHorizontal(params GUILayoutOption[] o) { BeginHorizontal(GUIStyle.none, o); }
        public static void BeginHorizontal(GUIStyle s, params GUILayoutOption[] o) { Abre("Horizontal"); var g = AbrirGrupo<Grupo>(s, o, false); if (s != GUIStyle.none) GUI.Box(g.rect, "", s); }
        public static void EndHorizontal() { Fecha("Horizontal"); FecharGrupo(); }
        public static void BeginVertical(params GUILayoutOption[] o) { BeginVertical(GUIStyle.none, o); }
        public static void BeginVertical(GUIStyle s, params GUILayoutOption[] o) { Abre("Vertical"); var g = AbrirGrupo<Grupo>(s, o, true); if (s != GUIStyle.none) GUI.Box(g.rect, "", s); }
        public static void EndVertical() { Fecha("Vertical"); FecharGrupo(); }
        public static Vector2 BeginScrollView(Vector2 v, params GUILayoutOption[] o)
        {
            Abre("ScrollView"); var g = AbrirGrupo<Rolagem>(GUI.skin.scrollView, null, true);
            if (EhLayout) { g.reset = true; g.estW = 1; g.estH = 1; g.barraV = GUI.skin.verticalScrollbar; g.barraH = GUI.skin.horizontalScrollbar; g.Opcoes(o); }
            return GUI.BeginScrollView(g.rect, v, new Rect(0, 0, g.largCliente, g.altCliente));
        }
        public static void EndScrollView() { Fecha("ScrollView"); FecharGrupo(); GUI.EndScrollView(); }
        public static void Label(string t, params GUILayoutOption[] o) { Label(t, GUI.skin.label, o); }
        public static void Label(string t, GUIStyle s, params GUILayoutOption[] o) { Texto(t); var r = Pegar(new GUIContent(t), s, o); Desenho2D.Estilo(r, t, s); }
        public static bool Button(string t, params GUILayoutOption[] o) { return Button(t, GUI.skin.button, o); }
        public static bool Button(string t, GUIStyle s, params GUILayoutOption[] o) { var r = Pegar(new GUIContent(t), s, o); Desenho2D.Estilo(r, t, s); return Apertado(t); }
        public static bool Toggle(bool v, string t, GUIStyle s, params GUILayoutOption[] o) { var r = Pegar(new GUIContent(t), s, o); return GUI.Toggle(r, v, t, s); }
        public static string TextField(string t, int max, params GUILayoutOption[] o) { controles++; var r = Pegar(new GUIContent(t), GUI.skin.textField, o); return GUI.TextField(r, t, max, GUI.skin.textField); }
        public static string TextArea(string t, int max, GUIStyle s, params GUILayoutOption[] o) { controles++; var r = Pegar(new GUIContent(t), s, o); Desenho2D.Estilo(r, t, s); return t; }
        public static float HorizontalSlider(float v, float a, float b, params GUILayoutOption[] o) { controles++; var r = Pegar(new GUIContent("mmmm"), GUI.skin.horizontalSlider, o); return GUI.HorizontalSlider(r, v, a, b); }
        public static void Space(float px)
        {
            if (topo.vertical) Pegar(0, px, estiloEspaco, new[] { Height(px) }); else Pegar(px, 0, estiloEspaco, new[] { Width(px) });
            if (EhLayout) topo.ents[topo.ents.Count - 1].margem = false;
        }
        public static void FlexibleSpace() { var op = topo.vertical ? new GUILayoutOption(7, 10000) : new GUILayoutOption(6, 10000); Pegar(0, 0, estiloEspaco, new[] { op }); }
        public static GUILayoutOption Width(float w) { return new GUILayoutOption(0, w); } public static GUILayoutOption Height(float h) { return new GUILayoutOption(1, h); }
        public static GUILayoutOption MinHeight(float h) { return new GUILayoutOption(4, h); } public static GUILayoutOption ExpandWidth(bool e) { return new GUILayoutOption(6, e ? 1 : 0); }
        internal static Rect GetRect(float w, float h, GUILayoutOption[] o) { return Pegar(w, h, GUIStyle.none, o); }
        internal static Rect Ultimo()
        {
            if (EhLayout || Event.current.type == EventType.Used) return fantasma;
            if (topo.cursor == 0) { erros.Add("GetLastRect logo depois de abrir um grupo"); return fantasma; } return topo.ents[topo.cursor - 1].rect;
        }
    }
    public static class GUILayoutUtility { public static Rect GetRect(float w, float h, params GUILayoutOption[] o) { return GUILayout.GetRect(w, h, o); } public static Rect GetLastRect() { return GUILayout.Ultimo(); } }
    public static class Screen { public static int width = 1920, height = 1080; }
    public static class Cursor { public static CursorLockMode lockState; public static bool visible = true; }
    public static class Input
    {
        public static readonly HashSet<KeyCode> apertadas = new HashSet<KeyCode>();
        public static readonly HashSet<KeyCode> seguradas = new HashSet<KeyCode>();
        public static bool GetKey(KeyCode k) { return seguradas.Contains(k); } public static bool GetKeyDown(KeyCode k) { return apertadas.Contains(k); }
        public static float GetAxisRaw(string e) { return 0; } public static bool GetMouseButton(int b) { return false; } public static bool GetMouseButtonDown(int b) { return false; }
    }
    public enum AudioDataLoadState { Unloaded, Loading, Loaded, Failed }
    public class AudioClip : Object
    {
        public int samples, channels = 1, frequency = 48000; public AudioDataLoadState loadState = AudioDataLoadState.Loaded;
        public static AudioClip Create(string n, int s, int c, int f, bool st) { return new AudioClip { samples = s, channels = c, frequency = f }; }
        public bool LoadAudioData() { return true; } public bool GetData(float[] d, int o) { return false; }
    }
    public class AudioSource : Behaviour { public AudioClip clip; public bool loop, playOnAwake; public float volume = 1, spatialBlend; public void Play() { } public void Stop() { } }
    public static class AudioSettings { public static int outputSampleRate = 48000; }
    public class CharacterController : Behaviour { }
}
namespace FalsoIMGUI
{
    // texturas desenhadas aqui (o skin padrão e o branco único, como o Texture2D.whiteTexture do Unity)
    public static class TexturaFalsa
    {
        public static Texture2D Caixa(Color cima, Color baixo, Color borda, int raio)
        {
            int N = 16; var t = new Texture2D(N, N); t.name = "skin";
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                // linha 0 é a de baixo no Unity
                float k = y / (float)(N - 1); var c = Color.Lerp(baixo, cima, k);
                int dx = Math.Max(Math.Max(raio - x, x - (N - 1 - raio)), 0), dy = Math.Max(Math.Max(raio - y, y - (N - 1 - raio)), 0); float d = (float)Math.Sqrt(dx * dx + dy * dy);
                bool fora = d > raio + .3f, naBorda = x == 0 || y == 0 || x == N - 1 || y == N - 1 || d > raio - .7f;
                t.pixels[y * N + x] = fora ? new Color32(0, 0, 0, 0) : (Color32)(naBorda ? borda : c);
            }
            return t;
        }
    }
    // ---------- fontes e texto ----------
    public class FonteTTF
    {
        public string nome, arquivo; public int upm = 1000, asc = 900, desc = -250, gap; readonly Dictionary<int, int> cmap = new Dictionary<int, int>(); ushort[] avanco = { 500 };
        static int U16(byte[] b, int o) { return (b[o] << 8) | b[o + 1]; } static int S16(byte[] b, int o) { return (short)U16(b, o); } static long U32(byte[] b, int o) { return ((long)U16(b, o) << 16) | (long)U16(b, o + 2); }
        public static FonteTTF Abrir(string arq, string nome)
        {
            var f = new FonteTTF { nome = nome, arquivo = arq }; if (!File.Exists(arq)) return null; var b = File.ReadAllBytes(arq);
            int n = U16(b, 4); var tab = new Dictionary<string, int>();
            for (int i = 0; i < n; i++) { int r = 12 + 16 * i; tab[Encoding.ASCII.GetString(b, r, 4)] = (int)U32(b, r + 8); }
            f.upm = U16(b, tab["head"] + 18); int hh = tab["hhea"]; f.asc = S16(b, hh + 4); f.desc = S16(b, hh + 6); f.gap = S16(b, hh + 8); int nh = U16(b, hh + 34);
            f.avanco = new ushort[nh]; for (int i = 0; i < nh; i++) f.avanco[i] = (ushort)U16(b, tab["hmtx"] + 4 * i);
            int cm = tab["cmap"], nt = U16(b, cm + 2), melhor = -1, fm = 0;
            for (int i = 0; i < nt; i++)
            {
                int pid = U16(b, cm + 4 + 8 * i), eid = U16(b, cm + 6 + 8 * i), off = cm + (int)U32(b, cm + 8 + 8 * i), fmt = U16(b, off);
                if (pid == 0 || pid == 3 && (eid == 1 || eid == 10)) { if (fmt == 12 && fm != 12 || fmt == 4 && melhor < 0) { melhor = off; fm = fmt; } }
            }
            if (fm == 4)
            {
                int seg2 = U16(b, melhor + 6), fins = melhor + 14, inis = fins + seg2 + 2, dels = inis + seg2, rngs = dels + seg2;
                for (int s = 0; s < seg2 / 2; s++)
                {
                    int e = U16(b, fins + 2 * s), a = U16(b, inis + 2 * s), d = S16(b, dels + 2 * s), ro = U16(b, rngs + 2 * s);
                    for (int c = a; c <= e && c != 0xFFFF; c++) { int g; if (ro == 0) g = (c + d) & 0xFFFF; else { g = U16(b, rngs + 2 * s + ro + 2 * (c - a)); if (g != 0) g = (g + d) & 0xFFFF; } if (g != 0) f.cmap[c] = g; }
                }
            }
            else if (fm == 12) { long ng = U32(b, melhor + 12); for (int i = 0; i < ng; i++) { int r = melhor + 16 + 12 * i; long a = U32(b, r), z = U32(b, r + 4), g0 = U32(b, r + 8); for (long c = a; c <= z && c - a < 70000; c++) f.cmap[(int)c] = (int)(g0 + c - a); } }
            return f;
        }
        public bool Tem(int cp) { return cmap.ContainsKey(cp) || cp == ' '; }
        public int Avanco(int cp) { int g; if (!cmap.TryGetValue(cp, out g)) g = 0; return avanco[Math.Min(g, avanco.Length - 1)]; }
        // métricas de linha no tamanho pedido, arredondadas como o FreeType
        public int Altura(int tam) { return (int)Math.Ceiling((asc - desc + gap) * (double)tam / upm); }
        public int Subida(int tam) { return (int)Math.Ceiling(asc * (double)tam / upm); }
    }
    public class Corrida { public float x, y; public string s = ""; public FonteTTF f; public int tam; public bool b, i, sb, si; public Color cor; public float w; }
    public class LinhaTexto { public readonly List<Corrida> corridas = new List<Corrida>(); public float w, alt, sub; }
    public static class TextoFalso
    {
        public static string sistema = "/usr/share/fonts/truetype";
        static readonly Dictionary<string, FonteTTF> fontes = new Dictionary<string, FonteTTF>();
        static FonteTTF Abrir(string chave, string arq) { FonteTTF f; if (!fontes.TryGetValue(chave, out f)) fontes[chave] = f = FonteTTF.Abrir(arq, chave); return f; }
        static FonteTTF Arial(bool b, bool i) { string s = "LiberationSans-" + (b && i ? "BoldItalic" : b ? "Bold" : i ? "Italic" : "Regular"); return Abrir(s, Path.Combine(sistema, "liberation", s + ".ttf")) ?? Reserva; }
        static FonteTTF Reserva { get { return Abrir("DejaVuSans", Path.Combine(sistema, "dejavu", "DejaVuSans.ttf")) ?? new FonteTTF { nome = "nenhuma" }; } }
        // a fonte de cada caractere: a do estilo (negrito e itálico sintéticos, como o Unity faz com um TTF de um peso só),
        // o Arial do Unity quando não há fonte, e a reserva do sistema para o que a fonte não tem
        static FonteTTF Fonte(Font f, bool b, bool i, int cp, out bool sb, out bool si)
        {
            sb = si = false; FonteTTF o;
            if (f == null || string.IsNullOrEmpty(f.arquivo)) o = Arial(b, i);
            else { o = Abrir(f.name, f.arquivo) ?? Arial(b, i); if (o != null && o.nome == f.name) { sb = b; si = i; } }
            if (o.Tem(cp)) return o; sb = si = false; var a = Arial(b, i); if (a.Tem(cp)) return a; return Reserva;
        }
        struct Car { public int cp; public int tam; public bool b, i; public Color? cor; }
        static int TamanhoBase(GUIStyle st) { return st.fontSize > 0 ? st.fontSize : 13; }
        // os caracteres com o que as etiquetas do texto rico mudam (b, i, size, color); etiqueta desconhecida fica no texto
        static List<Car> Ler(string t, GUIStyle st)
        {
            var o = new List<Car>(); t = t ?? ""; var tams = new Stack<int>(); var cores = new Stack<Color?>(); int nb = 0, ni = 0; int tam = TamanhoBase(st); Color? cor = null;
            bool b0 = st.fontStyle == FontStyle.Bold || st.fontStyle == FontStyle.BoldAndItalic, i0 = st.fontStyle == FontStyle.Italic || st.fontStyle == FontStyle.BoldAndItalic;
            for (int k = 0; k < t.Length; k++)
            {
                if (st.richText && t[k] == '<')
                {
                    int f = t.IndexOf('>', k); string tag = f > k ? t.Substring(k + 1, f - k - 1) : null; bool usou = true;
                    if (tag == null) usou = false;
                    else if (tag == "b") nb++; else if (tag == "/b") nb = Math.Max(0, nb - 1); else if (tag == "i") ni++; else if (tag == "/i") ni = Math.Max(0, ni - 1);
                    else if (tag.StartsWith("size=")) { int v; if (int.TryParse(tag.Substring(5), out v)) { tams.Push(tam); tam = v; } else usou = false; }
                    else if (tag == "/size") tam = tams.Count > 0 ? tams.Pop() : TamanhoBase(st);
                    else if (tag.StartsWith("color=")) { Color c; if (Cor(tag.Substring(6), out c)) { cores.Push(cor); cor = c; } else usou = false; }
                    else if (tag == "/color") cor = cores.Count > 0 ? cores.Pop() : null;
                    else usou = false;
                    if (usou) { k = f; continue; }
                }
                int cp = t[k]; if (char.IsHighSurrogate(t[k]) && k + 1 < t.Length) { cp = char.ConvertToUtf32(t[k], t[k + 1]); k++; }
                o.Add(new Car { cp = cp, tam = tam, b = b0 || nb > 0, i = i0 || ni > 0, cor = cor });
            }
            return o;
        }
        static bool Cor(string s, out Color c)
        {
            c = Color.white; s = s.Trim('"');
            var nomes = new Dictionary<string, string> { { "white", "ffffff" }, { "black", "000000" }, { "red", "ff0000" }, { "green", "008000" }, { "yellow", "ffff00" }, { "grey", "808080" }, { "gray", "808080" }, { "orange", "ffa500" } };
            string h; if (nomes.TryGetValue(s, out h)) s = "#" + h; if (!s.StartsWith("#") || (s.Length != 7 && s.Length != 9)) return false;
            Func<int, float> P = i => int.Parse(s.Substring(i, 2), NumberStyles.HexNumber) / 255f;
            try { c = new Color(P(1), P(3), P(5), s.Length == 9 ? P(7) : 1); return true; } catch { return false; }
        }
        public static string SemTags(string t, GUIStyle st) { var sb = new StringBuilder(); foreach (var c in Ler(t, st)) sb.Append(char.ConvertFromUtf32(c.cp)); return sb.ToString(); }
        public static string ComoTexto(string p, GUIStyle st) { return st.richText ? p.Replace("<", "<​") : p; }
        // quebra em linhas (largura <= 0: só nas quebras do texto) e junta em corridas de mesma fonte, tamanho e cor
        public static List<LinhaTexto> Diagramar(string t, GUIStyle st, float largura, Color? corPadrao = null)
        {
            var cs = Ler(t, st); var avs = new float[cs.Count]; var fs = new FonteTTF[cs.Count]; var sbs = new bool[cs.Count]; var sis = new bool[cs.Count];
            for (int k = 0; k < cs.Count; k++) { bool sb, si; var f = Fonte(st.font, cs[k].b, cs[k].i, cs[k].cp, out sb, out si); fs[k] = f; sbs[k] = sb; sis[k] = si; avs[k] = cs[k].cp == '\n' ? 0 : (float)Math.Round(f.Avanco(cs[k].cp) * (double)cs[k].tam / f.upm); }
            // as quebras: índices de início de cada linha
            var inicios = new List<int> { 0 }; float x = 0; int ultEspaco = -1;
            for (int k = 0; k < cs.Count; k++)
            {
                if (cs[k].cp == '\n') { inicios.Add(k + 1); x = 0; ultEspaco = -1; continue; }
                if (cs[k].cp == ' ') { ultEspaco = k; x += avs[k]; continue; }
                if (largura > 0 && x + avs[k] > largura + .01f && k > inicios[inicios.Count - 1])
                {
                    int ini = ultEspaco >= inicios[inicios.Count - 1] ? ultEspaco + 1 : k; inicios.Add(ini); ultEspaco = -1;
                    x = 0; for (int j = ini; j < k; j++) x += avs[j];
                }
                x += avs[k];
            }
            var linhas = new List<LinhaTexto>(); bool _b, _i; var fBase = Fonte(st.font, false, false, 'A', out _b, out _i);
            for (int l = 0; l < inicios.Count; l++)
            {
                int a = inicios[l], z = l + 1 < inicios.Count ? inicios[l + 1] : cs.Count; var L = new LinhaTexto(); float px = 0; Corrida c = null;
                // espaço e quebra no fim da linha não contam
                int zz = z; while (zz > a && (cs[zz - 1].cp == ' ' || cs[zz - 1].cp == '\n')) zz--;
                int maiorTam = 0;
                for (int k = a; k < zz; k++)
                {
                    var car = cs[k]; var cor = car.cor ?? corPadrao ?? st.normal.textColor; maiorTam = Math.Max(maiorTam, car.tam);
                    if (c == null || c.f != fs[k] || c.tam != car.tam || c.b != car.b || c.i != car.i || !cor.Equals(c.cor)) { c = new Corrida { x = px, f = fs[k], tam = car.tam, b = car.b, i = car.i, sb = sbs[k], si = sis[k], cor = cor }; L.corridas.Add(c); }
                    c.s += char.ConvertFromUtf32(car.cp); c.w += avs[k]; px += avs[k];
                }
                if (maiorTam == 0) maiorTam = a < cs.Count ? cs[Math.Min(a, cs.Count - 1)].tam : TamanhoBase(st);
                L.w = px; L.alt = fBase.Altura(maiorTam); L.sub = fBase.Subida(maiorTam); linhas.Add(L);
            }
            return linhas;
        }
        public static Vector2 Tamanho(List<LinhaTexto> ls) { float w = 0, h = 0; foreach (var l in ls) { w = Math.Max(w, l.w); h += l.alt; } return new Vector2((float)Math.Ceiling(w), h); }
    }

    // ---------- o layout do GUILayout (GUILayoutEntry, GUILayoutGroup, GUIScrollGroup do Unity) ----------
    public class Entrada
    {
        public float minW, maxW, minH, maxH; public Rect rect; public int estW, estH; public GUIStyle st; public bool margem = true;
        public Entrada(float a, float b, float c, float d, GUIStyle s) { minW = a; maxW = b; minH = c; maxH = d; Estilo(s ?? GUIStyle.none); }
        public virtual void Estilo(GUIStyle s) { st = s; estW = s.fixedWidth == 0 && s.stretchWidth ? 1 : 0; estH = s.fixedHeight == 0 && s.stretchHeight ? 1 : 0; }
        public virtual int mL { get { return st.margin.left; } } public virtual int mR { get { return st.margin.right; } } public virtual int mT { get { return st.margin.top; } } public virtual int mB { get { return st.margin.bottom; } }
        public virtual void CalcW() { } public virtual void CalcH() { }
        public virtual void SetH(float x, float w) { rect.x = x; rect.width = w; } public virtual void SetV(float y, float h) { rect.y = y; rect.height = h; }
        public virtual void Opcoes(GUILayoutOption[] o)
        {
            if (o == null) return;
            foreach (var i in o)
            {
                if (i == null) continue;
                switch (i.tipo)
                {
                    case 0: minW = maxW = i.valor; estW = 0; break;
                    case 1: minH = maxH = i.valor; estH = 0; break;
                    case 2: minW = i.valor; if (maxW < minW) maxW = minW; break;
                    case 3: maxW = i.valor; if (minW > maxW) minW = maxW; estW = 0; break;
                    case 4: minH = i.valor; if (maxH < minH) maxH = minH; break;
                    case 5: maxH = i.valor; if (minH > maxH) minH = maxH; estH = 0; break;
                    case 6: estW = (int)i.valor; break;
                    case 7: estH = (int)i.valor; break;
                }
            }
            if (maxW != 0 && maxW < minW) maxW = minW; if (maxH != 0 && maxH < minH) maxH = minH;
        }
    }
    // texto com quebra: a altura depende da largura que o grupo der (GUIWordWrapSizer)
    public class EntradaTexto : Entrada
    {
        readonly GUIContent c; float fMinH, fMaxH;
        public EntradaTexto(GUIStyle s, GUIContent c, GUILayoutOption[] o) : base(0, 0, 0, 0, s) { this.c = c; Opcoes(o); fMinH = minH; fMaxH = maxH; }
        public override void CalcW() { if (minW == 0 || maxW == 0) { float a, b; st.CalcMinMaxWidth(c, out a, out b); a = (float)Math.Ceiling(a); b = (float)Math.Ceiling(b); if (minW == 0) minW = a; if (maxW == 0) maxW = b; } }
        public override void CalcH() { if (fMinH == 0 || fMaxH == 0) { float h = st.CalcHeight(c, rect.width); minH = fMinH == 0 ? h : fMinH; maxH = fMaxH == 0 ? h : fMaxH; } }
    }
    public class Grupo : Entrada
    {
        public readonly List<Entrada> ents = new List<Entrada>(); public bool vertical = true, reset; public int cursor; public float espacamento;
        protected int gL, gR, gT, gB; protected bool userW, userH; protected float cMinW, cMaxW, cMinH, cMaxH; protected int nEstX, nEstY;
        public Grupo() : base(0, 0, 0, 0, GUIStyle.none) { }
        public override void Estilo(GUIStyle s) { base.Estilo(s); gL = s.margin.left; gR = s.margin.right; gT = s.margin.top; gB = s.margin.bottom; }
        public override int mL { get { return gL; } } public override int mR { get { return gR; } } public override int mT { get { return gT; } } public override int mB { get { return gB; } }
        public override void Opcoes(GUILayoutOption[] o)
        {
            if (o == null) return; base.Opcoes(o);
            foreach (var i in o) if (i != null) { if (i.tipo == 0 || i.tipo == 2 || i.tipo == 3) userH = true; if (i.tipo == 1 || i.tipo == 4 || i.tipo == 5) userW = true; }   // trocados, como no Unity
        }
        public Entrada Proximo()
        {
            if (cursor < ents.Count) return ents[cursor++];
            throw new ArgumentException("Getting control " + cursor + "'s position in a group with only " + ents.Count + " controls when doing " + Event.current.type + "\nAborting");
        }
        bool SemEstilo { get { return st == GUIStyle.none; } }
        public override void CalcW()
        {
            if (ents.Count == 0) { maxW = minW = st.padding.horizontal; return; }
            int mlMin = 0, mrMin = 0; cMinW = cMaxW = 0; nEstX = 0; bool primeiro = true;
            if (vertical)
            {
                foreach (var i in ents)
                {
                    i.CalcW();
                    if (i.margem)
                    {
                        if (!primeiro) { mlMin = Math.Min(i.mL, mlMin); mrMin = Math.Min(i.mR, mrMin); } else { mlMin = i.mL; mrMin = i.mR; primeiro = false; }
                        cMinW = Math.Max(i.minW + i.mL + i.mR, cMinW); cMaxW = Math.Max(i.maxW + i.mL + i.mR, cMaxW);
                    }
                    nEstX += i.estW;
                }
                cMinW -= mlMin + mrMin; cMaxW -= mlMin + mrMin;
            }
            else
            {
                int ult = 0;
                foreach (var i in ents)
                {
                    i.CalcW(); int m;
                    if (i.margem) { if (!primeiro) m = ult > i.mL ? ult : i.mL; else { m = 0; primeiro = false; } cMinW += i.minW + espacamento + m; cMaxW += i.maxW + espacamento + m; ult = i.mR; nEstX += i.estW; }
                    else { cMinW += i.minW; cMaxW += i.maxW; nEstX += i.estW; }
                }
                cMinW -= espacamento; cMaxW -= espacamento; mlMin = ents[0].mL; mrMin = ult;
            }
            float pl, pr;
            if (!SemEstilo || userW) { pl = Math.Max(st.padding.left, mlMin); pr = Math.Max(st.padding.right, mrMin); } else { gL = mlMin; gR = mrMin; pl = pr = 0; }
            minW = Math.Max(minW, cMinW + pl + pr);
            if (maxW == 0) { estW += nEstX + (st.stretchWidth ? 1 : 0); maxW = cMaxW + pl + pr; } else estW = 0;
            maxW = Math.Max(maxW, minW);
            if (st.fixedWidth != 0) { maxW = minW = st.fixedWidth; estW = 0; }
        }
        public override void SetH(float x, float w)
        {
            base.SetH(x, w); if (reset) x = 0; var p = st.padding;
            if (vertical)
            {
                if (!SemEstilo)
                    foreach (var i in ents) { float l = Math.Max(i.mL, p.left), tx = x + l, tw = w - Math.Max(i.mR, p.right) - l; i.SetH(tx, i.estW != 0 ? tw : Mathf.Clamp(tw, i.minW, i.maxW)); }
                else { float tx = x - mL, tw = w + mL + mR; foreach (var i in ents) i.SetH(tx + i.mL, i.estW != 0 ? tw - i.mL - i.mR : Mathf.Clamp(tw - i.mL - i.mR, i.minW, i.maxW)); }
            }
            else
            {
                if (!SemEstilo) { float l = p.left, r = p.right; if (ents.Count != 0) { l = Math.Max(l, ents[0].mL); r = Math.Max(r, ents[ents.Count - 1].mR); } x += l; w -= r + l; }
                float dist = w - espacamento * (ents.Count - 1), esc = 0; if (cMinW != cMaxW) esc = Mathf.Clamp((dist - cMinW) / (cMaxW - cMinW), 0, 1);
                float porItem = 0; if (dist > cMaxW && nEstX > 0) porItem = (dist - cMaxW) / nEstX;
                int ult = 0; bool primeiro = true;
                foreach (var i in ents)
                {
                    float tw = Mathf.Lerp(i.minW, i.maxW, esc) + porItem * i.estW;
                    if (i.margem) { int ml = i.mL; if (primeiro) { ml = 0; primeiro = false; } x += ult > ml ? ult : ml; ult = i.mR; }
                    i.SetH((float)Math.Round(x), (float)Math.Round(tw)); x += tw + espacamento;
                }
            }
        }
        public override void CalcH()
        {
            if (ents.Count == 0) { maxH = minH = st.padding.vertical; return; }
            int mtMin = 0, mbMin = 0; cMinH = cMaxH = 0; nEstY = 0; bool primeiro = true;
            if (vertical)
            {
                int ult = 0;
                foreach (var i in ents)
                {
                    i.CalcH(); int m;
                    if (i.margem) { if (!primeiro) m = Math.Max(ult, i.mT); else { m = 0; primeiro = false; } cMinH += i.minH + espacamento + m; cMaxH += i.maxH + espacamento + m; ult = i.mB; nEstY += i.estH; }
                    else { cMinH += i.minH; cMaxH += i.maxH; nEstY += i.estH; }
                }
                cMinH -= espacamento; cMaxH -= espacamento; mtMin = ents[0].mT; mbMin = ult;
            }
            else
            {
                foreach (var i in ents)
                {
                    i.CalcH();
                    if (i.margem) { if (!primeiro) { mtMin = Math.Min(i.mT, mtMin); mbMin = Math.Min(i.mB, mbMin); } else { mtMin = i.mT; mbMin = i.mB; primeiro = false; } cMinH = Math.Max(i.minH, cMinH); cMaxH = Math.Max(i.maxH, cMaxH); }
                    nEstY += i.estH;
                }
            }
            float pt, pb;
            if (!SemEstilo || userH) { pt = Math.Max(st.padding.top, mtMin); pb = Math.Max(st.padding.bottom, mbMin); } else { gT = mtMin; gB = mbMin; pt = pb = 0; }
            minH = Math.Max(minH, cMinH + pt + pb);
            if (maxH == 0) { estH += nEstY + (st.stretchHeight ? 1 : 0); maxH = cMaxH + pt + pb; } else estH = 0;
            maxH = Math.Max(maxH, minH);
            if (st.fixedHeight != 0) { maxH = minH = st.fixedHeight; estH = 0; }
        }
        public override void SetV(float y, float h)
        {
            base.SetV(y, h); if (ents.Count == 0) return; if (reset) y = 0; var p = st.padding;
            if (vertical)
            {
                if (!SemEstilo) { float t = p.top, b = p.bottom; t = Math.Max(t, ents[0].mT); b = Math.Max(b, ents[ents.Count - 1].mB); y += t; h -= b + t; }
                float dist = h - espacamento * (ents.Count - 1), esc = 0; if (cMinH != cMaxH) esc = Mathf.Clamp((dist - cMinH) / (cMaxH - cMinH), 0, 1);
                float porItem = 0; if (dist > cMaxH && nEstY > 0) porItem = (dist - cMaxH) / nEstY;
                int ult = 0; bool primeiro = true;
                foreach (var i in ents)
                {
                    float th = Mathf.Lerp(i.minH, i.maxH, esc) + porItem * i.estH;
                    if (i.margem) { int mt = i.mT; if (primeiro) { mt = 0; primeiro = false; } y += ult > mt ? ult : mt; ult = i.mB; }
                    i.SetV((float)Math.Round(y), (float)Math.Round(th)); y += th + espacamento;
                }
            }
            else
            {
                if (!SemEstilo)
                    foreach (var i in ents) { float t = Math.Max(i.mT, p.top), ty = y + t, th = h - Math.Max(i.mB, p.bottom) - t; i.SetV(ty, i.estH != 0 ? th : Mathf.Clamp(th, i.minH, i.maxH)); }
                else { float ty = y - mT, th = h + mT + mB; foreach (var i in ents) i.SetV(ty + i.mT, i.estH != 0 ? th - i.mT - i.mB : Mathf.Clamp(th - i.mT - i.mB, i.minH, i.maxH)); }
            }
        }
    }
    public class Rolagem : Grupo
    {
        public bool precisaV, precisaH; public float largCliente, altCliente; float calcMinW, calcMaxW, calcMinH, calcMaxH; public GUIStyle barraV, barraH;
        public override void CalcW()
        {
            float a = minW, b = maxW; minW = 0; maxW = 0; base.CalcW(); calcMinW = minW; calcMaxW = maxW;
            if (minW > 32) minW = 32; if (a != 0) minW = a; if (b != 0) { maxW = b; estW = 0; }
        }
        public override void SetH(float x, float w)
        {
            float cw = precisaV ? w - barraV.fixedWidth - barraV.margin.left : w;
            if (cw < calcMinW) { precisaH = true; minW = calcMinW; maxW = calcMaxW; base.SetH(x, calcMinW); rect.width = w; largCliente = calcMinW; }
            else { precisaH = false; minW = calcMinW; maxW = calcMaxW; base.SetH(x, cw); rect.width = w; largCliente = cw; }
        }
        public override void CalcH()
        {
            float a = minH, b = maxH; minH = 0; maxH = 0; base.CalcH(); calcMinH = minH; calcMaxH = maxH;
            if (precisaH) { float s = barraH.fixedHeight + barraH.margin.top; minH += s; maxH += s; }
            if (minH > 32) minH = 32; if (a != 0) minH = a; if (b != 0) { maxH = b; estH = 0; }
        }
        public override void SetV(float y, float h)
        {
            float ch = h; if (precisaH) ch -= barraH.fixedHeight + barraH.margin.top;
            if (ch < calcMinH)
            {
                if (!precisaH && !precisaV) { largCliente = rect.width - barraV.fixedWidth - barraV.margin.left; if (largCliente < calcMinW) largCliente = calcMinW; float fora = rect.width; SetH(rect.x, largCliente); CalcH(); rect.width = fora; }
                float om = minH, oM = maxH; minH = calcMinH; maxH = calcMaxH; base.SetV(y, calcMinH); minH = om; maxH = oM; rect.height = h; altCliente = calcMinH;
            }
            else { minH = calcMinH; maxH = calcMaxH; base.SetV(y, ch); rect.height = h; altCliente = ch; }
        }
    }

    // ---------- desenho: a pilha de recortes (BeginGroup, área, rolagem) e a gravação da foto ----------
    public static class Desenho2D
    {
        struct Recorte { public Vector2 deslocamento; public Rect tela; }
        static readonly List<Recorte> pilha = new List<Recorte>();
        public static StringBuilder json; static readonly HashSet<string> salvas = new HashSet<string>(); public static string pasta; public static int comandos;
        static Vector2 Desloc { get { return pilha.Count > 0 ? pilha[pilha.Count - 1].deslocamento : Vector2.zero; } }
        public static Vector2 Deslocamento { get { return Desloc; } }
        static Rect Tela { get { return pilha.Count > 0 ? pilha[pilha.Count - 1].tela : new Rect(0, 0, Screen.width, Screen.height); } }
        public static void Limpar() { pilha.Clear(); }
        public static int Altura { get { return pilha.Count; } }
        static Rect NaTela(Rect r)
        {
            var m = GUI.matrix; var a = m.MultiplyPoint3x4(new Vector3(r.x, r.y, 0)); var b = m.MultiplyPoint3x4(new Vector3(r.xMax, r.yMax, 0));
            return new Rect(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Abs(b.x - a.x), Math.Abs(b.y - a.y));
        }
        static Rect Cruzar(Rect a, Rect b) { float x0 = Math.Max(a.x, b.x), y0 = Math.Max(a.y, b.y), x1 = Math.Min(a.xMax, b.xMax), y1 = Math.Min(a.yMax, b.yMax); return new Rect(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0)); }
        // r no espaço do grupo atual; o recorte novo vale na tela, com a matriz de agora (como o GUIClip)
        public static void Empilhar(Rect r, Vector2 rolagem)
        {
            var d = Desloc; var abs = new Rect(d.x + r.x, d.y + r.y, r.width, r.height);
            pilha.Add(new Recorte { deslocamento = new Vector2(abs.x - (float)Math.Round(rolagem.x), abs.y - (float)Math.Round(rolagem.y)), tela = Cruzar(Tela, NaTela(abs)) });
        }
        public static void Desempilhar() { if (pilha.Count > 0) pilha.RemoveAt(pilha.Count - 1); }
        static string F(float v) { return Math.Round(v, 2).ToString(CultureInfo.InvariantCulture); }
        static string Cor(Color c) { return "[" + F(c.r) + "," + F(c.g) + "," + F(c.b) + "," + F(c.a) + "]"; }
        static string Cabeca(string k)
        {
            var m = GUI.matrix; var t = Tela; comandos++;
            return (json.Length > 1 ? ",\n" : "") + "{\"k\":\"" + k + "\",\"m\":[" + F(m.m00) + "," + F(m.m01) + "," + F(m.m03) + "," + F(m.m10) + "," + F(m.m11) + "," + F(m.m13) + "],\"cl\":[" + F(t.x) + "," + F(t.y) + "," + F(t.width) + "," + F(t.height) + "]";
        }
        static Color Tom(Color c) { var g = GUI.color; var o = new Color(c.r * g.r, c.g * g.g, c.b * g.b, c.a * g.a); if (!GUI.enabled) o.a *= .5f; return o; }
        public static string Textura(Texture t)
        {
            var t2 = t as Texture2D; if (t2 == null) return "w";
            if (t2 == Texture2D.whiteTexture) return "w";
            string id = t2.id + "-" + t2.versao; if (pasta == null || salvas.Contains(id)) return id; salvas.Add(id);
            using (var f = new BinaryWriter(File.Create(Path.Combine(pasta, "tex-" + id + ".rgba"))))
            {
                f.Write(t2.width); f.Write(t2.height);
                // de cima para baixo (no Unity a linha 0 é a de baixo)
                for (int y = t2.height - 1; y >= 0; y--) for (int x = 0; x < t2.width; x++) { var p = t2.pixels[y * t2.width + x]; f.Write(p.r); f.Write(p.g); f.Write(p.b); f.Write(p.a); }
            }
            return id;
        }
        public static void Textura(Rect r, Texture t, Color cor, RectOffset borda, ScaleMode modo = ScaleMode.StretchToFill)
        {
            if (json == null || Event.current.type != EventType.Repaint || t == null) return; var d = Desloc; var c = Tom(cor); if (c.a <= 0.001f) return;
            json.Append(Cabeca("t")).Append(",\"r\":[" + F(d.x + r.x) + "," + F(d.y + r.y) + "," + F(r.width) + "," + F(r.height) + "],\"tex\":\"" + Textura(t) + "\",\"c\":" + Cor(c));
            if (borda != null && borda.horizontal + borda.vertical > 0) json.Append(",\"b\":[" + borda.left + "," + borda.right + "," + borda.top + "," + borda.bottom + "]");
            if (modo == ScaleMode.ScaleToFit) json.Append(",\"fit\":1"); json.Append("}");
        }
        public static void Ret(Rect r, Color cor) { Textura(r, Texture2D.whiteTexture, cor, null); }
        static string Esc(string s) { var b = new StringBuilder(); foreach (var ch in s) { if (ch == '"' || ch == '\\') b.Append('\\').Append(ch); else if (ch < 32) b.Append("\\u" + ((int)ch).ToString("x4")); else b.Append(ch); } return b.ToString(); }
        // o conteúdo de um estilo dentro do retângulo: o fundo do estado, a caixa de seleção do toggle e o texto alinhado
        public static void Estilo(Rect r, string texto, GUIStyle st, bool ligado = false, bool foco = false)
        {
            if (json == null || Event.current.type != EventType.Repaint) return;
            var est = ligado && st.onNormal.background != null ? st.onNormal : foco && st.focused.background != null ? st.focused : st.normal;
            if (est.background != null) Textura(r, est.background, Color.white, st.border);
            if (st.nome == "toggle")
            {
                var cx = new Rect(r.x, r.y + Math.Max(0, (Math.Min(r.height, 22) - 14) / 2f), 14, 14); Ret(cx, new Color(.06f, .06f, .06f, 1)); Ret(new Rect(cx.x + 1, cx.y + 1, 12, 12), new Color(.25f, .25f, .25f, 1));
                if (ligado) Ret(new Rect(cx.x + 4, cx.y + 4, 6, 6), new Color(.9f, .9f, .9f, 1));
            }
            if (string.IsNullOrEmpty(texto)) return;
            var cont = new Rect(r.x + st.padding.left, r.y + st.padding.top, r.width - st.padding.horizontal, r.height - st.padding.vertical);
            var corTexto = (ligado ? st.onNormal : foco ? st.focused : st.normal).textColor;
            var linhas = TextoFalso.Diagramar(texto, st, st.wordWrap ? cont.width : 0, corTexto); var tam = TextoFalso.Tamanho(linhas);
            int al = (int)st.alignment; float y = al < 3 ? cont.y : al < 6 ? cont.y + (cont.height - tam.y) / 2 : cont.yMax - tam.y; y = (float)Math.Round(y);
            var d = Desloc; bool recorta = st.clipping == TextClipping.Clip;
            if (recorta) Empilhar(new Rect(r.x, r.y, r.width, r.height), Vector2.zero);
            foreach (var L in linhas)
            {
                float x = al % 3 == 0 ? cont.x : al % 3 == 1 ? cont.x + (cont.width - L.w) / 2 : cont.xMax - L.w; x = (float)Math.Round(x);
                foreach (var c in L.corridas)
                {
                    if (c.s.Trim().Length == 0) continue; var cor = Tom(c.cor); if (cor.a <= .001f) continue;
                    json.Append(Cabeca("x")).Append(",\"x\":" + F(d.x + x + c.x) + ",\"y\":" + F(d.y + y + L.sub) + ",\"s\":\"" + Esc(c.s) + "\",\"f\":\"" + Esc(c.f.arquivo ?? "") + "\",\"z\":" + c.tam + ",\"w\":" + F(c.w) + (c.sb ? ",\"sb\":1" : "") + (c.si ? ",\"si\":1" : "") + ",\"c\":" + Cor(cor) + "}");
                }
                y += L.alt;
            }
            if (recorta) Desempilhar();
        }
        public static void Comecar(string arquivoPasta) { pasta = arquivoPasta; json = new StringBuilder("["); comandos = 0; }
        public static void Gravar(string arquivo) { if (json == null) return; json.Append("]"); File.WriteAllText(arquivo, "{\"w\":" + Screen.width + ",\"h\":" + Screen.height + ",\"cmds\":" + json + "}"); json = null; }
    }

}
namespace UnityEngine.Networking
{
    public class DownloadHandler { public string text = ""; }
    public class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }
        public bool isDone = true; public Result result = Result.ConnectionError; public DownloadHandler downloadHandler = new DownloadHandler(); public int timeout;
        public static UnityWebRequest Get(string u) { return new UnityWebRequest(); }
        public object SendWebRequest() { return null; } public void Dispose() { }
    }
}
namespace RitualReversal.Visual
{
    // o pós do URP não existe aqui: o construtor falha e a partida segue sem ele (como num Unity sem URP)
    public class Pos
    {
        public float brilho = 1;
        public Pos(UnityEngine.Camera c) { throw new Exception("sem URP no teste"); }
        public void Atualizar(float s, float g, float f, float c) { } public void Grade(UnityEngine.Camera cam, int m, float g, float n, float dt, float t) { } public void Destruir() { }
    }
}
public class JogadorFPS : UnityEngine.MonoBehaviour { }
