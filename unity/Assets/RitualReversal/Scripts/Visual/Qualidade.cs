// Nível de qualidade do protótipo: 3 alto, 2 médio, 1 baixo, 0 mínimo. Define texturas, luzes, sombras e detalhes.
namespace RitualReversal.Visual
{
    public static class Qualidade
    {
        public static int nivel = 3;
        public static void Escolher(int n) { nivel = n < 0 ? 0 : n > 3 ? 3 : n; }
    }
}
