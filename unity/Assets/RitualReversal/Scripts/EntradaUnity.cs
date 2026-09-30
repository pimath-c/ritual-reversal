// Teclado e mouse com o Input System novo (padrão do Unity 6) ou com o antigo. Mesmas teclas do protótipo.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RitualReversal
{
    public enum Tecla { W, A, S, D, Shift, E, T, Q, F, G, R, Tab, Esc, Enter }

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
                case Tecla.Esc: return Key.Escape; default: return Key.Enter;
            }
        }
        public static bool Segurando(Tecla t) { var k = Keyboard.current; return k != null && k[Mapear(t)].isPressed; }
        public static bool Apertou(Tecla t) { var k = Keyboard.current; return k != null && k[Mapear(t)].wasPressedThisFrame; }
        // pixels neste quadro, y para cima
        public static Vector2 Mouse() { var m = UnityEngine.InputSystem.Mouse.current; return m != null ? m.delta.ReadValue() : Vector2.zero; }
        public static bool Atirando() { var m = UnityEngine.InputSystem.Mouse.current; return m != null && m.leftButton.isPressed; }
        public static bool CliqueEsq() { var m = UnityEngine.InputSystem.Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
        public static bool CliqueDir() { var m = UnityEngine.InputSystem.Mouse.current; return m != null && m.rightButton.wasPressedThisFrame; }
#else
        static KeyCode Mapear(Tecla t)
        {
            switch (t)
            {
                case Tecla.W: return KeyCode.W; case Tecla.A: return KeyCode.A; case Tecla.S: return KeyCode.S; case Tecla.D: return KeyCode.D;
                case Tecla.Shift: return KeyCode.LeftShift; case Tecla.E: return KeyCode.E; case Tecla.T: return KeyCode.T; case Tecla.Q: return KeyCode.Q;
                case Tecla.F: return KeyCode.F; case Tecla.G: return KeyCode.G; case Tecla.R: return KeyCode.R; case Tecla.Tab: return KeyCode.Tab;
                case Tecla.Esc: return KeyCode.Escape; default: return KeyCode.Return;
            }
        }
        public static bool Segurando(Tecla t) { return Input.GetKey(Mapear(t)); }
        public static bool Apertou(Tecla t) { return Input.GetKeyDown(Mapear(t)); }
        public static Vector2 Mouse() { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f; }
        public static bool Atirando() { return Input.GetMouseButton(0); }
        public static bool CliqueEsq() { return Input.GetMouseButtonDown(0); }
        public static bool CliqueDir() { return Input.GetMouseButtonDown(1); }
#endif
    }
}
