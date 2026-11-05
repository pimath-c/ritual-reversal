// A prévia do mapa montada no editor (menu "Ritual Reversal > Montar o visual na cena"): o mesmo mundo do Play, gravado
// na cena para ver e mexer sem apertar Play. No Play ela some (a partida monta o mundo dela) e não vai para o jogo
// compilado (etiqueta EditorOnly). Guarda o que a montagem mudou na cena (luzes desligadas, névoa e luz ambiente) para
// o menu "Tirar o visual da cena" devolver.
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal.Visual
{
    public class PreviaNoEditor : MonoBehaviour
    {
        public string mapa;
        public List<Light> luzesDesligadas = new List<Light>();
        public bool nevoa; public FogMode modoNevoa; public float densNevoa; public Color corNevoa;
        public UnityEngine.Rendering.AmbientMode modoAmbiente; public Color ambCeu, ambMeio, ambChao;

        void Awake() { if (Application.isPlaying) gameObject.SetActive(false); }
    }
}
