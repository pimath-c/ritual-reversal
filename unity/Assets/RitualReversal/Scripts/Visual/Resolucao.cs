// Resolução adaptativa (medirRes e aplicarRes do navegador): durante a partida mede os quadros por segundo a cada 2 s
// de jogo; abaixo de 28 baixa a resolução interna em 0,1 (até a metade), acima de 52 sobe 0,05 de volta. A base vem da
// qualidade, como o RES.base do navegador: min(densidade da tela, .75/1/1,25/1,5) pixels por pixel de CSS, aqui convertida
// em escala de renderização do URP (renderScale). O valor original do asset volta no fim (o asset é do projeto).
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace RitualReversal.Visual
{
    public static class Resolucao
    {
        public static float k = 1;   // o RES.k: o quanto a adaptação baixou
        static float acc; static int n; static float original = -1; static PropertyInfo prop; static object asset;
        static readonly float[] BASE = { .75f, 1, 1.25f, 1.5f };

        // a densidade da tela em pixels de CSS (o devicePixelRatio): 96 ppp é 1
        static float Densidade() { float d = Screen.dpi; return d > 0 ? Mathf.Max(1, d / 96f) : 1; }
        public static float Escala() { float dpr = Densidade(); return Mathf.Max(.35f, Mathf.Min(dpr, BASE[Mathf.Clamp(Qualidade.nivel, 0, 3)]) * k) / dpr; }

        static bool Achar()
        {
            if (prop != null && asset != null) return true;
            asset = GraphicsSettings.currentRenderPipeline; if (asset == null) return false;
            prop = asset.GetType().GetProperty("renderScale", BindingFlags.Instance | BindingFlags.Public);
            if (prop == null || !prop.CanWrite) { prop = null; return false; }
            if (original < 0) original = (float)prop.GetValue(asset, null);
            return true;
        }
        public static void Aplicar() { if (Achar()) prop.SetValue(asset, Mathf.Clamp(Escala(), .1f, 2f), null); }
        // a cada quadro: o tempo do quadro (sem escala), se a noite está rolando e se o ajuste está ligado
        public static void Medir(float raw, bool jogando, bool ligada)
        {
            if (!ligada && k != 1) { k = 1; Aplicar(); }
            if (!ligada || !jogando || raw > .25f) { acc = 0; n = 0; return; }
            acc += raw; n++; if (acc < 2) return; float fps = n / acc; acc = 0; n = 0;
            if (fps < 28 && k > .5f) { k = Mathf.Max(.5f, k - .1f); Aplicar(); }
            else if (fps > 52 && k < 1) { k = Mathf.Min(1, k + .05f); Aplicar(); }
        }
        public static void Restaurar() { if (prop != null && asset != null && original >= 0) prop.SetValue(asset, original, null); k = 1; acc = 0; n = 0; prop = null; asset = null; original = -1; }
    }
}
