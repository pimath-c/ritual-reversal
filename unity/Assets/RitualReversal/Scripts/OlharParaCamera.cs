// Rótulos do blockout (nomes de altares, lugares, mercadores) sempre virados para a câmera.
using UnityEngine;

namespace RitualReversal
{
    public class OlharParaCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
