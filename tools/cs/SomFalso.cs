// Só o que o visual do Unity pede ao Som (MundoAtores): a superfície do chão. Usado com UnityFalso.cs em tools/unity-ver.js.
namespace RitualReversal.Visual
{
    public class Som
    {
        public static string Superficie(double x, double z) { return RitualReversal.Simulacao.Mapa.NaCatedral(x, z) ? "pedra" : "terra"; }
    }
}
