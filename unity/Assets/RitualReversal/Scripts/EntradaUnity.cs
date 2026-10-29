// Teclado e mouse com o Input System novo (padrão do Unity 6) ou com o antigo. Mesmas teclas do protótipo.
// Controle (só com o Input System), o mapeamento do navegador (PAD_BOTAO): analógico esquerdo anda, direito olha; RT ataca,
// RB feitiço, LT ou clique no analógico esquerdo corre; A interage, B agacha, X recarrega, Y lanterna, LB sensor; direcional
// para a esquerda transfere, para baixo sinalizador, para cima as frases; clique no analógico direito marca (ping); Select a
// planta; Start pausa. Os botões entram como as teclas correspondentes em Segurando/Apertou.
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RitualReversal
{
    public enum Tecla { W, A, S, D, Shift, E, T, Q, F, G, R, Tab, Esc, Enter, Z, C, I, J, N, V, D1, D2, D3, D4, D5, D6, F9, F10 }

    public static class EntradaUnity
    {
        // ---------- teclas configuráveis (SET.binds do navegador: os mesmos nomes de ação e os códigos do KeyboardEvent) ----------
        public static readonly string[][] ACOES = {
            new[]{"fwd","Andar para frente","KeyW"}, new[]{"back","Andar para trás","KeyS"}, new[]{"left","Passo à esquerda","KeyA"}, new[]{"right","Passo à direita","KeyD"},
            new[]{"sprint","Correr","ShiftLeft"}, new[]{"agachar","Agachar (segurar): devagar e quase sem som","KeyC"}, new[]{"fire","Atacar","Mouse0"}, new[]{"abil","Habilidade da classe","Mouse2"},
            new[]{"use","Interagir (segurar)","KeyE"}, new[]{"use2","Transferência (segurar)","KeyT"}, new[]{"sensor","Sensor Áurico","KeyQ"}, new[]{"lantern","Lanterna","KeyF"},
            new[]{"flare","Flare","KeyG"}, new[]{"reload","Recarregar","KeyR"}, new[]{"inspecionar","Inspecionar a arma","KeyI"}, new[]{"map","Planta da catedral","Tab"},
            new[]{"ping","Marcar para a equipe","KeyZ"}, new[]{"falas","Frases rápidas","KeyV"}, new[]{"caderno","Caderno dos altares","KeyJ"} };
        static Dictionary<string, string> binds;
        public static Dictionary<string, string> Binds
        {
            get
            {
                if (binds != null) return binds; binds = new Dictionary<string, string>(); foreach (var a in ACOES) binds[a[0]] = a[2];
                var s = PlayerPrefs.GetString("rr-binds", ""); var d = s.Length > 0 ? RitualReversal.Simulacao.Json.Ler(s) as Dictionary<string, object> : null;
                if (d != null) foreach (var kv in d) if (binds.ContainsKey(kv.Key) && kv.Value is string) binds[kv.Key] = (string)kv.Value;
                return binds;
            }
        }
        // a ação recebe a tecla; se outra ação já usava essa tecla, as duas trocam de lugar (como no navegador)
        public static void Definir(string acao, string codigo)
        {
            var B = Binds; string ant = B[acao]; foreach (var k in new List<string>(B.Keys)) if (k != acao && B[k] == codigo) B[k] = ant; B[acao] = codigo; Salvar();
        }
        public static void Padrao() { PlayerPrefs.DeleteKey("rr-binds"); binds = null; }
        static void Salvar() { var j = new JsonEscrita(); foreach (var kv in Binds) j.Txt(kv.Key, kv.Value); PlayerPrefs.SetString("rr-binds", j.ToString()); PlayerPrefs.Save(); }
        static readonly Dictionary<string, string> NOMES = new Dictionary<string, string> { {"Space","Espaço"},{"ShiftLeft","Shift esq."},{"ShiftRight","Shift dir."},{"ControlLeft","Ctrl esq."},{"ControlRight","Ctrl dir."},{"AltLeft","Alt esq."},{"AltRight","Alt dir."},{"Tab","Tab"},{"Enter","Enter"},{"Backquote","`"},{"ArrowUp","↑"},{"ArrowDown","↓"},{"ArrowLeft","←"},{"ArrowRight","→"},{"CapsLock","CapsLock"},{"Mouse0","Botão esq."},{"Mouse1","Botão meio"},{"Mouse2","Botão dir."},{"Mouse3","Mouse 4"},{"Mouse4","Mouse 5"} };
        public static string NomeCodigo(string c) { string n; if (c == null) return "—"; if (NOMES.TryGetValue(c, out n)) return n; if (c.StartsWith("Key")) return c.Substring(3); if (c.StartsWith("Digit")) return c.Substring(5); if (c.StartsWith("Numpad")) return "Num " + c.Substring(6); return c; }
        // o nome da tecla de uma ação, para os textos do HUD (o kb() do navegador)
        public static string Nome(string acao) { string c; return Binds.TryGetValue(acao, out c) ? NomeCodigo(c) : "—"; }
        // ---------- toque (os botões redondos do navegador): cada botão segura ou aperta a ação da tecla configurada ----------
        static readonly HashSet<string> toqueSeg = new HashSet<string>(), toqueAp = new HashSet<string>();
        public static void ToqueSegurar(string acao, bool sim) { if (sim) { if (toqueSeg.Add(acao)) toqueAp.Add(acao); } else toqueSeg.Remove(acao); }
        public static bool ToqueSegurando(string acao) { return toqueSeg.Contains(acao); }
        public static void ToqueNovoQuadro() { toqueAp.Clear(); }
        public static void ToqueSoltarTudo() { toqueSeg.Clear(); toqueAp.Clear(); }
        public static bool Segurando(Tecla t) { var a = Acao(t); return SegurandoDisp(t) || (a != null && toqueSeg.Contains(a)); }
        public static bool Apertou(Tecla t) { var a = Acao(t); return ApertouDisp(t) || (a != null && toqueAp.Contains(a)); }
        public static bool Atirando() { return AtirandoDisp() || toqueSeg.Contains("fire"); }
        public static bool CliqueDir() { return CliqueDirDisp() || toqueAp.Contains("abil"); }
        // os dedos na tela: posição em pixels da tela (y para cima, como o Unity) e a fase (0 encostou, 1 segue, 2 saiu)
        public struct Toque { public int id; public Vector2 pos; public int fase; }
        public static List<Toque> toquesTeste;   // o teste no Mono põe os dedos aqui
        public static List<Toque> Toques()
        {
            if (toquesTeste != null) return toquesTeste; var L = new List<Toque>();
#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current; if (ts == null) return L;
            foreach (var t in ts.touches)
            {
                var f = t.phase.ReadValue(); if (f == UnityEngine.InputSystem.TouchPhase.None) continue;
                L.Add(new Toque { id = t.touchId.ReadValue(), pos = t.position.ReadValue(), fase = f == UnityEngine.InputSystem.TouchPhase.Began ? 0 : f == UnityEngine.InputSystem.TouchPhase.Ended || f == UnityEngine.InputSystem.TouchPhase.Canceled ? 2 : 1 });
            }
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i); var f = t.phase;
                L.Add(new Toque { id = t.fingerId, pos = t.position, fase = f == UnityEngine.TouchPhase.Began ? 0 : f == UnityEngine.TouchPhase.Ended || f == UnityEngine.TouchPhase.Canceled ? 2 : 1 });
            }
#endif
            return L;
        }

        static string Acao(Tecla t)
        {
            switch (t)
            {
                case Tecla.W: return "fwd"; case Tecla.S: return "back"; case Tecla.A: return "left"; case Tecla.D: return "right"; case Tecla.Shift: return "sprint"; case Tecla.C: return "agachar";
                case Tecla.E: return "use"; case Tecla.T: return "use2"; case Tecla.Q: return "sensor"; case Tecla.F: return "lantern"; case Tecla.G: return "flare"; case Tecla.R: return "reload";
                case Tecla.I: return "inspecionar"; case Tecla.Tab: return "map"; case Tecla.Z: return "ping"; case Tecla.V: return "falas"; case Tecla.J: return "caderno";
            }
            return null;   // Esc, Enter, N, F9, F10 e os números das frases não mudam
        }
        static int BotaoMouse(string c) { return c != null && c.StartsWith("Mouse") && c.Length == 6 ? c[5] - '0' : -1; }   // Mouse0 esq., Mouse1 meio, Mouse2 dir.
#if ENABLE_INPUT_SYSTEM
        static Key Mapear(Tecla t)
        {
            switch (t)
            {
                case Tecla.W: return Key.W; case Tecla.A: return Key.A; case Tecla.S: return Key.S; case Tecla.D: return Key.D;
                case Tecla.Shift: return Key.LeftShift; case Tecla.E: return Key.E; case Tecla.T: return Key.T; case Tecla.Q: return Key.Q;
                case Tecla.F: return Key.F; case Tecla.G: return Key.G; case Tecla.R: return Key.R; case Tecla.Tab: return Key.Tab;
                case Tecla.Esc: return Key.Escape; case Tecla.Z: return Key.Z; case Tecla.C: return Key.C; case Tecla.I: return Key.I; case Tecla.J: return Key.J; case Tecla.N: return Key.N; case Tecla.V: return Key.V; case Tecla.D1: return Key.Digit1; case Tecla.D2: return Key.Digit2; case Tecla.D3: return Key.Digit3; case Tecla.D4: return Key.Digit4; case Tecla.D5: return Key.Digit5; case Tecla.D6: return Key.Digit6; case Tecla.F9: return Key.F9; case Tecla.F10: return Key.F10; default: return Key.Enter;
            }
        }
        static UnityEngine.InputSystem.Controls.ButtonControl Botao(Gamepad p, Tecla t)
        {
            switch (t)
            {
                case Tecla.E: return p.buttonSouth; case Tecla.C: return p.buttonEast; case Tecla.R: return p.buttonWest; case Tecla.F: return p.buttonNorth; case Tecla.Q: return p.leftShoulder;
                case Tecla.Shift: return p.leftTrigger; case Tecla.T: return p.dpad.left; case Tecla.G: return p.dpad.down; case Tecla.Z: return p.rightStickButton; case Tecla.Tab: return p.selectButton; case Tecla.Esc: return p.startButton;
            }
            return null;
        }
        // código do navegador -> tecla do Input System ("KeyW" -> W, "Digit1" -> Digit1, "ShiftLeft" -> LeftShift...)
        static readonly Dictionary<string, Key> ESPECIAIS = new Dictionary<string, Key> { {"ShiftLeft",Key.LeftShift},{"ShiftRight",Key.RightShift},{"ControlLeft",Key.LeftCtrl},{"ControlRight",Key.RightCtrl},{"AltLeft",Key.LeftAlt},{"AltRight",Key.RightAlt},{"ArrowUp",Key.UpArrow},{"ArrowDown",Key.DownArrow},{"ArrowLeft",Key.LeftArrow},{"ArrowRight",Key.RightArrow},{"Backquote",Key.Backquote},{"CapsLock",Key.CapsLock},{"Space",Key.Space},{"Tab",Key.Tab},{"Enter",Key.Enter} };
        static Key ParaKey(string c)
        {
            Key k; if (c == null) return Key.None; if (ESPECIAIS.TryGetValue(c, out k)) return k;
            string n = c.StartsWith("Key") ? c.Substring(3) : c.StartsWith("Numpad") ? "Numpad" + c.Substring(6) : c; return System.Enum.TryParse(n, out k) ? k : Key.None;
        }
        static string DeKey(Key k) { foreach (var kv in ESPECIAIS) if (kv.Value == k) return kv.Key; string n = k.ToString(); if (n.Length == 1) return "Key" + n; return n; }
        static bool Codigo(string c, bool apertou)
        {
            int b = BotaoMouse(c); var m = UnityEngine.InputSystem.Mouse.current;
            if (b >= 0) { if (m == null) return false; var bc = b == 0 ? m.leftButton : b == 2 ? m.rightButton : m.middleButton; return bc != null && (apertou ? bc.wasPressedThisFrame : bc.isPressed); }
            var kb = Keyboard.current; var key = ParaKey(c); if (kb == null || key == Key.None) return false; var kc = kb[key]; return kc != null && (apertou ? kc.wasPressedThisFrame : kc.isPressed);
        }
        // a tecla ou o botão do mouse apertado neste quadro, como código do navegador (para trocar a tecla nos Ajustes)
        public static string Capturar()
        {
            var m = UnityEngine.InputSystem.Mouse.current; if (m != null) { if (m.leftButton.wasPressedThisFrame) return "Mouse0"; if (m.rightButton.wasPressedThisFrame) return "Mouse2"; if (m.middleButton != null && m.middleButton.wasPressedThisFrame) return "Mouse1"; }
            var kb = Keyboard.current; if (kb == null) return null; foreach (var k in kb.allKeys) if (k != null && k.wasPressedThisFrame) return DeKey(k.keyCode); return null;
        }
        static bool SegurandoDisp(Tecla t)
        {
            var a = Acao(t); var k = Keyboard.current;
            if (a != null ? Codigo(Binds[a], false) : (k != null && k[Mapear(t)].isPressed)) return true; var p = Gamepad.current; if (p == null) return false;
            var b = Botao(p, t); return (b != null && b.isPressed) || (t == Tecla.Shift && p.leftStickButton.isPressed);
        }
        static bool ApertouDisp(Tecla t)
        {
            var a = Acao(t); var k = Keyboard.current;
            if (a != null ? Codigo(Binds[a], true) : (k != null && k[Mapear(t)].wasPressedThisFrame)) return true; var p = Gamepad.current; var b = p != null ? Botao(p, t) : null; return b != null && b.wasPressedThisFrame;
        }
        static float Zona(float v) { return Mathf.Abs(v) < .18f ? 0 : Mathf.Sign(v) * (Mathf.Abs(v) - .18f) / .82f; }
        // os analógicos com a zona morta do navegador (y para cima, como no Input System)
        public static Vector2 PadAndar() { var p = Gamepad.current; if (p == null) return Vector2.zero; var v = p.leftStick.ReadValue(); return new Vector2(Zona(v.x), Zona(v.y)); }
        public static Vector2 PadOlhar() { var p = Gamepad.current; if (p == null) return Vector2.zero; var v = p.rightStick.ReadValue(); return new Vector2(Zona(v.x), Zona(v.y)); }
        public static bool PadCima() { var p = Gamepad.current; return p != null && p.dpad.up.wasPressedThisFrame; }
        public static bool PadBaixo() { var p = Gamepad.current; return p != null && p.dpad.down.wasPressedThisFrame; }
        public static bool PadA() { var p = Gamepad.current; return p != null && p.buttonSouth.wasPressedThisFrame; }
        public static bool PadB() { var p = Gamepad.current; return p != null && p.buttonEast.wasPressedThisFrame; }
        public static bool PadStart() { var p = Gamepad.current; return p != null && p.startButton.wasPressedThisFrame; }
        // nos menus: alguma coisa no controle (para mostrar o foco) e o passo de cima/baixo pelo direcional ou pelo analógico
        public static bool PadAlgum() { var p = Gamepad.current; if (p == null) return false; return p.buttonSouth.wasPressedThisFrame || p.buttonEast.wasPressedThisFrame || p.dpad.up.wasPressedThisFrame || p.dpad.down.wasPressedThisFrame || p.startButton.wasPressedThisFrame || p.leftStick.ReadValue().magnitude > .5f; }
        static float eixoAnt;
        public static int PadMenuPasso()
        {
            var p = Gamepad.current; if (p == null) return 0; float e = p.leftStick.ReadValue().y; int r = 0;
            if (p.dpad.up.wasPressedThisFrame || (e > .6f && eixoAnt <= .6f)) r = -1; if (p.dpad.down.wasPressedThisFrame || (e < -.6f && eixoAnt >= -.6f)) r = 1; eixoAnt = e; return r;
        }
        // pixels neste quadro, y para cima
        public static Vector2 Mouse() { var m = UnityEngine.InputSystem.Mouse.current; return m != null ? m.delta.ReadValue() : Vector2.zero; }
        static bool AtirandoDisp() { var p = Gamepad.current; return Codigo(Binds["fire"], false) || (p != null && p.rightTrigger.isPressed); }
        public static bool CliqueEsq() { return Codigo(Binds["fire"], true); }
        static bool CliqueDirDisp() { var p = Gamepad.current; return Codigo(Binds["abil"], true) || (p != null && p.rightShoulder.wasPressedThisFrame); }
#else
        static KeyCode Mapear(Tecla t)
        {
            switch (t)
            {
                case Tecla.W: return KeyCode.W; case Tecla.A: return KeyCode.A; case Tecla.S: return KeyCode.S; case Tecla.D: return KeyCode.D;
                case Tecla.Shift: return KeyCode.LeftShift; case Tecla.E: return KeyCode.E; case Tecla.T: return KeyCode.T; case Tecla.Q: return KeyCode.Q;
                case Tecla.F: return KeyCode.F; case Tecla.G: return KeyCode.G; case Tecla.R: return KeyCode.R; case Tecla.Tab: return KeyCode.Tab;
                case Tecla.Esc: return KeyCode.Escape; case Tecla.Z: return KeyCode.Z; case Tecla.C: return KeyCode.C; case Tecla.I: return KeyCode.I; case Tecla.J: return KeyCode.J; case Tecla.N: return KeyCode.N; case Tecla.V: return KeyCode.V; case Tecla.D1: return KeyCode.Alpha1; case Tecla.D2: return KeyCode.Alpha2; case Tecla.D3: return KeyCode.Alpha3; case Tecla.D4: return KeyCode.Alpha4; case Tecla.D5: return KeyCode.Alpha5; case Tecla.D6: return KeyCode.Alpha6; case Tecla.F9: return KeyCode.F9; case Tecla.F10: return KeyCode.F10; default: return KeyCode.Return;
            }
        }
        static readonly Dictionary<string, KeyCode> ESPECIAIS = new Dictionary<string, KeyCode> { {"ShiftLeft",KeyCode.LeftShift},{"ShiftRight",KeyCode.RightShift},{"ControlLeft",KeyCode.LeftControl},{"ControlRight",KeyCode.RightControl},{"AltLeft",KeyCode.LeftAlt},{"AltRight",KeyCode.RightAlt},{"ArrowUp",KeyCode.UpArrow},{"ArrowDown",KeyCode.DownArrow},{"ArrowLeft",KeyCode.LeftArrow},{"ArrowRight",KeyCode.RightArrow},{"Backquote",KeyCode.BackQuote},{"CapsLock",KeyCode.CapsLock},{"Space",KeyCode.Space},{"Tab",KeyCode.Tab},{"Enter",KeyCode.Return} };
        static KeyCode ParaKey(string c)
        {
            KeyCode k; if (c == null) return KeyCode.None; if (ESPECIAIS.TryGetValue(c, out k)) return k; int b = BotaoMouse(c); if (b >= 0) return KeyCode.Mouse0 + (b == 2 ? 1 : b == 1 ? 2 : b);
            string n = c.StartsWith("Key") ? c.Substring(3) : c.StartsWith("Digit") ? "Alpha" + c.Substring(5) : c.StartsWith("Numpad") ? "Keypad" + c.Substring(6) : c; return System.Enum.TryParse(n, out k) ? k : KeyCode.None;
        }
        public static string Capturar()
        {
            if (Input.GetMouseButtonDown(0)) return "Mouse0"; if (Input.GetMouseButtonDown(1)) return "Mouse2"; if (Input.GetMouseButtonDown(2)) return "Mouse1";
            foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (k >= KeyCode.Mouse0 || !Input.GetKeyDown(k)) continue; foreach (var kv in ESPECIAIS) if (kv.Value == k) return kv.Key; string n = k.ToString();
                if (n.StartsWith("Alpha")) return "Digit" + n.Substring(5); if (n.StartsWith("Keypad")) return "Numpad" + n.Substring(6); return n.Length == 1 ? "Key" + n : n;
            }
            return null;
        }
        static bool SegurandoDisp(Tecla t) { var a = Acao(t); return Input.GetKey(a != null ? ParaKey(Binds[a]) : Mapear(t)); }
        static bool ApertouDisp(Tecla t) { var a = Acao(t); return Input.GetKeyDown(a != null ? ParaKey(Binds[a]) : Mapear(t)); }
        public static Vector2 Mouse() { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f; }
        static bool AtirandoDisp() { return Input.GetKey(ParaKey(Binds["fire"])); }
        public static bool CliqueEsq() { return Input.GetKeyDown(ParaKey(Binds["fire"])); }
        static bool CliqueDirDisp() { return Input.GetKeyDown(ParaKey(Binds["abil"])); }
        // sem o Input System, sem controle
        public static Vector2 PadAndar() { return Vector2.zero; } public static Vector2 PadOlhar() { return Vector2.zero; }
        public static bool PadCima() { return false; } public static bool PadBaixo() { return false; } public static bool PadA() { return false; } public static bool PadB() { return false; } public static bool PadStart() { return false; }
        public static bool PadAlgum() { return false; } public static int PadMenuPasso() { return 0; }
#endif
    }
}
