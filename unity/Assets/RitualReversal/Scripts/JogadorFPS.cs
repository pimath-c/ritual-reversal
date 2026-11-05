// Jogador em primeira pessoa para andar pelo blockout. Mesmas medidas do protótipo:
// raio 0,4 m, andar 4,6 m/s, correr 6 m/s, olhos a 1,65 m (CFG.speed / CFG.sprint em shared/sim.js).
// Funciona com o Input System novo e com o antigo. Clique para prender o mouse, Esc para soltar.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RitualReversal
{
    [RequireComponent(typeof(CharacterController))]
    public class JogadorFPS : MonoBehaviour
    {
        public float velocidade = 4.6f;
        public float velocidadeCorrendo = 6f;
        [Tooltip("Graus por pixel de movimento do mouse")]
        public float sensibilidade = 0.12f;
        public float gravidade = -20f;
        public Transform cabeca;
        [Tooltip("Mostra a posição em coordenadas do protótipo, para comparar com o jogo no navegador")]
        public bool mostrarPosicao = true;

        CharacterController cc;
        float pitch, vy;

        void Start()
        {
            cc = GetComponent<CharacterController>();
            if (cabeca == null) { var cam = GetComponentInChildren<Camera>(); if (cam) cabeca = cam.transform; }
            Prender(true);
        }

        void Prender(bool sim)
        {
            Cursor.lockState = sim ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !sim;
        }

        void Update()
        {
            Vector2 mov = Vector2.zero, olhar = Vector2.zero;
            bool corre = false, soltar = false, clique = false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var m = Mouse.current;
            if (k != null)
            {
                if (k.wKey.isPressed) mov.y += 1; if (k.sKey.isPressed) mov.y -= 1;
                if (k.dKey.isPressed) mov.x += 1; if (k.aKey.isPressed) mov.x -= 1;
                corre = k.leftShiftKey.isPressed; soltar = k.escapeKey.wasPressedThisFrame;
            }
            if (m != null) { olhar = m.delta.ReadValue() * sensibilidade; clique = m.leftButton.wasPressedThisFrame; }
#else
            mov = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            corre = Input.GetKey(KeyCode.LeftShift); soltar = Input.GetKeyDown(KeyCode.Escape); clique = Input.GetMouseButtonDown(0);
            olhar = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * sensibilidade * 10f;
#endif
            if (soltar) Prender(false);
            else if (clique && Cursor.lockState != CursorLockMode.Locked) Prender(true);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(0f, olhar.x, 0f);
                pitch = Mathf.Clamp(pitch - olhar.y, -85f, 85f);
                if (cabeca) cabeca.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector3 dir = transform.right * mov.x + transform.forward * mov.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            float v = corre && mov.y > 0f ? velocidadeCorrendo : velocidade; // no protótipo só se corre para a frente
            vy = cc.isGrounded ? -2f : vy + gravidade * Time.deltaTime;
            cc.Move((dir * v + Vector3.up * vy) * Time.deltaTime);
        }

        void OnGUI()
        {
            if (!mostrarPosicao) return;
            var p = transform.position;
            GUI.Label(new Rect(12, 10, 520, 22), $"protótipo: x {p.x:0.0}  z {-p.z:0.0}    (Esc solta o mouse, clique prende)");
        }
    }
}
