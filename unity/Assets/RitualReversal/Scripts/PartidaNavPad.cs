// Menus pelo controle (o lerControle do navegador fora da partida): cima e baixo (direcional ou analógico) passam pelos
// botões da tela, na ordem em que aparecem, A confirma e B volta. Todos os botões das telas passam por Bt, que conta a
// ordem, desenha a moldura dourada no botão em foco e o aperta quando o A chega. Só aparece depois que o controle foi usado.
using UnityEngine;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        bool padNav; int padFoco, padBotoes, padContagem; bool padConfirmar; int padConfirmarQuadro = -1;   // o A vale só no Repaint do quadro em que chegou
        bool cliqueNaPassada;   // um botão respondeu nesta passada do OnGUI (a tela pode ter mudado no meio dela)
        bool Bt(string t, params GUILayoutOption[] o) { return Bt(t, sBotao, o); }
        bool Bt(string t, GUIStyle s, params GUILayoutOption[] o)
        {
            int i = padContagem++; bool r = GUILayout.Button(t, s, o); if (r) cliqueNaPassada = true; if (!padNav || i != padFoco) return r;
            var ev = Event.current; if (ev == null || ev.type != EventType.Repaint) return r;
            var rr = GUILayoutUtility.GetLastRect(); var c = OURO_VIVO; Caixa(new Rect(rr.x - 2, rr.y - 2, rr.width + 4, 2), c); Caixa(new Rect(rr.x - 2, rr.yMax, rr.width + 4, 2), c); Caixa(new Rect(rr.x - 2, rr.y, 2, rr.height), c); Caixa(new Rect(rr.xMax, rr.y, 2, rr.height), c);
            if (padConfirmar && padConfirmarQuadro == Time.frameCount && GUI.enabled) { padConfirmar = false; cliqueNaPassada = true; return true; }
            return r;
        }
        // no começo de cada passada do OnGUI: guarda quantos botões a tela tinha e recomeça a contagem
        void ComecoNavPad() { var ev = Event.current; if (ev != null && ev.type == EventType.Repaint) padBotoes = padContagem; padContagem = 0; cliqueNaPassada = false; }
        // a cada quadro (Update), fora da partida ou com o mouse solto
        void NavPad(bool jogando)
        {
            if (EntradaUnity.PadAlgum()) padNav = true; if (!padNav || jogando) return;
            int passo = EntradaUnity.PadMenuPasso(); if (passo != 0 && padBotoes > 0) { padFoco = ((padFoco + passo) % padBotoes + padBotoes) % padBotoes; if (som != null) som.Tocar("click"); }
            if (padFoco >= padBotoes) padFoco = 0;
            if (EntradaUnity.PadA()) { padConfirmar = true; padConfirmarQuadro = Time.frameCount; }
            if (EntradaUnity.PadB()) PadVoltar();
        }
        // B volta: fecha o que estiver aberto por cima (ajustes, telas do menu, loja, replay, relatório, pausa)
        void PadVoltar()
        {
            if (capturando != null) { capturando = null; return; }
            if (g == null) { if (telaMenu != null) telaMenu = null; return; }
            if (lojaAberta != null) { lojaAberta = null; Travar(true); return; }
            if (repAberto) { repAberto = false; return; } if (relAberto) { relAberto = false; return; }
            if (pausaAjustes) { pausaAjustes = false; return; }
        }
    }
}
