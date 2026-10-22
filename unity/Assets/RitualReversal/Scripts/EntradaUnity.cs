// Teclado e mouse com o Input System novo (padrão do Unity 6) ou com o antigo. Mesmas teclas do protótipo.
// Controle (só com o Input System), o mapeamento do navegador (PAD_BOTAO): analógico esquerdo anda, direito olha; RT ataca,
// RB feitiço, LT ou clique no analógico esquerdo corre; A interage, B agacha, X recarrega, Y lanterna, LB sensor; direcional
// para a esquerda transfere, para baixo sinalizador, para cima as frases; clique no analógico direito marca (ping); Select a
// planta; Start pausa. Os botões entram como as teclas correspondentes em Segurando/Apertou.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RitualReversal
{
    public enum Tecla { W, A, S, D, Shift, E, T, Q, F, G, R, Tab, Esc, Enter, Z, C, I, J, N, V, D1, D2, D3, D4, D5, D6 }

    public static class EntradaUnity
    {
#if ENABLE_INPUT_SYSTEM
        static Key Mapear(Tecla t)
        {
            switch (t)
            {
                case Tecla.W: return Key.W; case Tecla.A: return Key.A; case Tecla.S: return Key.S; case Tecla.D: return Key.D;
                case Tecla.Shift: return Key.LeftShift; case Tecla.E: return Key.E; case Tecla.T: return Key.T; case Tecla.Q: return Key.Q;
                case Tecla.F: return Key.F; case Tecla.G: return Key.G; case Tecla.R: return Key.R; case Tecla.Tab: return Key.Tab;
                case Tecla.Esc: return Key.Escape; case Tecla.Z: return Key.Z; case Tecla.C: return Key.C; case Tecla.I: return Key.I; case Tecla.J: return Key.J; case Tecla.N: return Key.N; case Tecla.V: return Key.V; case Tecla.D1: return Key.Digit1; case Tecla.D2: return Key.Digit2; case Tecla.D3: return Key.Digit3; case Tecla.D4: return Key.Digit4; case Tecla.D5: return Key.Digit5; case Tecla.D6: return Key.Digit6; default: return Key.Enter;
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
        public static bool Segurando(Tecla t)
        {
            var k = Keyboard.current; if (k != null && k[Mapear(t)].isPressed) return true; var p = Gamepad.current; if (p == null) return false;
            var b = Botao(p, t); return (b != null && b.isPressed) || (t == Tecla.Shift && p.leftStickButton.isPressed);
        }
        public static bool Apertou(Tecla t) { var k = Keyboard.current; if (k != null && k[Mapear(t)].wasPressedThisFrame) return true; var p = Gamepad.current; var b = p != null ? Botao(p, t) : null; return b != null && b.wasPressedThisFrame; }
        static float Zona(float v) { return Mathf.Abs(v) < .18f ? 0 : Mathf.Sign(v) * (Mathf.Abs(v) - .18f) / .82f; }
        // os analógicos com a zona morta do navegador (y para cima, como no Input System)
        public static Vector2 PadAndar() { var p = Gamepad.current; if (p == null) return Vector2.zero; var v = p.leftStick.ReadValue(); return new Vector2(Zona(v.x), Zona(v.y)); }
        public static Vector2 PadOlhar() { var p = Gamepad.current; if (p == null) return Vector2.zero; var v = p.rightStick.ReadValue(); return new Vector2(Zona(v.x), Zona(v.y)); }
        public static bool PadCima() { var p = Gamepad.current; return p != null && p.dpad.up.wasPressedThisFrame; }
        public static bool PadBaixo() { var p = Gamepad.current; return p != null && p.dpad.down.wasPressedThisFrame; }
        public static bool PadA() { var p = Gamepad.current; return p != null && p.buttonSouth.wasPressedThisFrame; }
        public static bool PadB() { var p = Gamepad.current; return p != null && p.buttonEast.wasPressedThisFrame; }
        public static bool PadStart() { var p = Gamepad.current; return p != null && p.startButton.wasPressedThisFrame; }
        // pixels neste quadro, y para cima
        public static Vector2 Mouse() { var m = UnityEngine.InputSystem.Mouse.current; return m != null ? m.delta.ReadValue() : Vector2.zero; }
        public static bool Atirando() { var m = UnityEngine.InputSystem.Mouse.current; var p = Gamepad.current; return (m != null && m.leftButton.isPressed) || (p != null && p.rightTrigger.isPressed); }
        public static bool CliqueEsq() { var m = UnityEngine.InputSystem.Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
        public static bool CliqueDir() { var m = UnityEngine.InputSystem.Mouse.current; var p = Gamepad.current; return (m != null && m.rightButton.wasPressedThisFrame) || (p != null && p.rightShoulder.wasPressedThisFrame); }
#else
        static KeyCode Mapear(Tecla t)
        {
            switch (t)
            {
                case Tecla.W: return KeyCode.W; case Tecla.A: return KeyCode.A; case Tecla.S: return KeyCode.S; case Tecla.D: return KeyCode.D;
                case Tecla.Shift: return KeyCode.LeftShift; case Tecla.E: return KeyCode.E; case Tecla.T: return KeyCode.T; case Tecla.Q: return KeyCode.Q;
                case Tecla.F: return KeyCode.F; case Tecla.G: return KeyCode.G; case Tecla.R: return KeyCode.R; case Tecla.Tab: return KeyCode.Tab;
                case Tecla.Esc: return KeyCode.Escape; case Tecla.Z: return KeyCode.Z; case Tecla.C: return KeyCode.C; case Tecla.I: return KeyCode.I; case Tecla.J: return KeyCode.J; case Tecla.N: return KeyCode.N; case Tecla.V: return KeyCode.V; case Tecla.D1: return KeyCode.Alpha1; case Tecla.D2: return KeyCode.Alpha2; case Tecla.D3: return KeyCode.Alpha3; case Tecla.D4: return KeyCode.Alpha4; case Tecla.D5: return KeyCode.Alpha5; case Tecla.D6: return KeyCode.Alpha6; default: return KeyCode.Return;
            }
        }
        public static bool Segurando(Tecla t) { return Input.GetKey(Mapear(t)); }
        public static bool Apertou(Tecla t) { return Input.GetKeyDown(Mapear(t)); }
        public static Vector2 Mouse() { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f; }
        public static bool Atirando() { return Input.GetMouseButton(0); }
        public static bool CliqueEsq() { return Input.GetMouseButtonDown(0); }
        public static bool CliqueDir() { return Input.GetMouseButtonDown(1); }
        // sem o Input System, sem controle
        public static Vector2 PadAndar() { return Vector2.zero; } public static Vector2 PadOlhar() { return Vector2.zero; }
        public static bool PadCima() { return false; } public static bool PadBaixo() { return false; } public static bool PadA() { return false; } public static bool PadB() { return false; } public static bool PadStart() { return false; }
#endif
    }
}
