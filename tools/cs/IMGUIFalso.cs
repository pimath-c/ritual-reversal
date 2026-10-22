// IMGUI, entrada, áudio e rede de mentira, só para o teste de fumaça das telas (tools/unity-telas.js), junto com
// UnityFalso.cs. O GUILayout guarda uma pilha dos grupos abertos (área, horizontal, vertical, rolagem) e acusa quando um
// End não fecha o Begin certo ou quando sobra grupo aberto no fim da passada: no Unity isso vira erro vermelho por quadro
// ("EndLayoutGroup: BeginLayoutGroup must be called first") e a tela some. Os botões podem ser apertados pelo texto
// (GUILayout.Apertar), uma vez, para o teste percorrer os caminhos de clique.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextClipping { Overflow, Clip }
    public enum EventType { MouseDown, MouseUp, MouseMove, MouseDrag, KeyDown, KeyUp, ScrollWheel, Repaint, Layout, Used }
    public enum KeyCode { None, Return, KeypadEnter, Escape, Tab, Space, LeftShift, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6 }
    public enum CursorLockMode { None, Locked, Confined }
    public enum ScaleMode { StretchToFill, ScaleAndCrop, ScaleToFit }
    public class RectOffset { public int left, right, top, bottom; public RectOffset() { } public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; } }
    public class GUIStyleState { public Color textColor = Color.white; public Texture2D background; }
    public class GUIStyle
    {
        public int fontSize; public bool wordWrap, richText; public TextAnchor alignment; public FontStyle fontStyle; public TextClipping clipping;
        public RectOffset padding = new RectOffset(), margin = new RectOffset(); public GUIStyleState normal = new GUIStyleState(), focused = new GUIStyleState(), hover = new GUIStyleState();
        public GUIStyle() { }
        public GUIStyle(GUIStyle o) { if (o == null) return; fontSize = o.fontSize; wordWrap = o.wordWrap; richText = o.richText; alignment = o.alignment; fontStyle = o.fontStyle; clipping = o.clipping; padding = o.padding; margin = o.margin; normal = new GUIStyleState { textColor = o.normal.textColor, background = o.normal.background }; }
        public float CalcHeight(GUIContent c, float w) { int n = c != null && c.text != null ? c.text.Length : 0; return (fontSize > 0 ? fontSize : 14) * 1.3f * (1 + n * 7 / Math.Max(40f, w)); }
        public Vector2 CalcSize(GUIContent c) { int n = c != null && c.text != null ? c.text.Length : 0; float f = fontSize > 0 ? fontSize : 14; return new Vector2(n * f * .55f, f * 1.3f); }
    }
    public class GUIContent { public string text, tooltip; public GUIContent(string t) { text = t; } public GUIContent(string t, string tip) { text = t; tooltip = tip; } }
    public class GUISkin { public GUIStyle label = new GUIStyle(), button = new GUIStyle(), toggle = new GUIStyle(), textField = new GUIStyle(), textArea = new GUIStyle(), box = new GUIStyle(); }
    public class GUILayoutOption { }
    public class Event
    {
        public EventType type = EventType.Layout; public KeyCode keyCode; public Vector2 mousePosition; public int button;
        public static Event current = new Event();
        public void Use() { type = EventType.Used; }
    }
    public static class GUI
    {
        public static GUISkin skin = new GUISkin(); public static Color color = Color.white; public static Matrix4x4 matrix; public static bool enabled = true, changed;
        public static string tooltip = "";
        public static void Label(Rect r, string t) { GUILayout.Texto(t); } public static void Label(Rect r, string t, GUIStyle s) { GUILayout.Texto(t); } public static void Label(Rect r, GUIContent c, GUIStyle s) { GUILayout.Texto(c.text); }
        public static void DrawTexture(Rect r, Texture t) { if (t == null) throw new Exception("GUI.DrawTexture com textura nula"); }
        public static void DrawTexture(Rect r, Texture t, ScaleMode m) { DrawTexture(r, t); }
        public static string TextField(Rect r, string t, int max, GUIStyle s) { return t; }
        public static void SetNextControlName(string n) { } public static void FocusControl(string n) { } public static string GetNameOfFocusedControl() { return ""; }
        public static bool Button(Rect r, string t) { return GUILayout.Apertado(t); } public static bool Button(Rect r, string t, GUIStyle s) { return GUILayout.Apertado(t); }
    }
    public static class GUIUtility
    {
        public static string systemCopyBuffer = ""; public static int hotControl;
        public static void RotateAroundPivot(float ang, Vector2 p) { }
    }
    public static class GUILayout
    {
        // a pilha dos grupos abertos nesta passada e os erros de pareamento achados
        public static readonly List<string> pilha = new List<string>(); public static readonly List<string> erros = new List<string>();
        public static readonly HashSet<string> textos = new HashSet<string>(); static string apertar; public static int controles;
        public static void Apertar(string texto) { apertar = texto; }
        static string Limpo(string t) { return System.Text.RegularExpressions.Regex.Replace(t ?? "", "<[^>]*>", ""); }
        public static void Texto(string t) { controles++; textos.Add(Limpo(t)); }
        public static bool Apertado(string t) { Texto(t); if (apertar != null && Limpo(t).Contains(apertar) && Event.current.type != EventType.Layout) { apertar = null; return true; } return false; }
        static void Abre(string k) { controles++; pilha.Add(k); }
        static void Fecha(string k) { if (pilha.Count == 0 || pilha[pilha.Count - 1] != k) { erros.Add("End" + k + " sem Begin" + k + " (aberto: " + (pilha.Count > 0 ? pilha[pilha.Count - 1] : "nada") + ")"); return; } pilha.RemoveAt(pilha.Count - 1); }
        public static void BeginArea(Rect r) { Abre("Area"); } public static void EndArea() { Fecha("Area"); }
        public static void BeginHorizontal(params GUILayoutOption[] o) { Abre("Horizontal"); } public static void BeginHorizontal(GUIStyle s, params GUILayoutOption[] o) { Abre("Horizontal"); } public static void EndHorizontal() { Fecha("Horizontal"); }
        public static void BeginVertical(params GUILayoutOption[] o) { Abre("Vertical"); } public static void BeginVertical(GUIStyle s, params GUILayoutOption[] o) { Abre("Vertical"); } public static void EndVertical() { Fecha("Vertical"); }
        public static Vector2 BeginScrollView(Vector2 v, params GUILayoutOption[] o) { Abre("ScrollView"); return v; } public static void EndScrollView() { Fecha("ScrollView"); }
        public static void Label(string t, params GUILayoutOption[] o) { Texto(t); } public static void Label(string t, GUIStyle s, params GUILayoutOption[] o) { Texto(t); }
        public static bool Button(string t, params GUILayoutOption[] o) { return Apertado(t); } public static bool Button(string t, GUIStyle s, params GUILayoutOption[] o) { return Apertado(t); }
        public static bool Toggle(bool v, string t, GUIStyle s, params GUILayoutOption[] o) { Texto(t); return v; }
        public static string TextField(string t, int max, params GUILayoutOption[] o) { controles++; return t; } public static string TextArea(string t, int max, GUIStyle s, params GUILayoutOption[] o) { controles++; return t; }
        public static float HorizontalSlider(float v, float a, float b, params GUILayoutOption[] o) { controles++; return v; }
        public static void Space(float px) { } public static void FlexibleSpace() { }
        public static GUILayoutOption Width(float w) { return null; } public static GUILayoutOption Height(float h) { return null; } public static GUILayoutOption MinHeight(float h) { return null; } public static GUILayoutOption ExpandWidth(bool e) { return null; }
    }
    public static class GUILayoutUtility { public static Rect GetRect(float w, float h, params GUILayoutOption[] o) { return new Rect(0, 0, w, h); } }
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
