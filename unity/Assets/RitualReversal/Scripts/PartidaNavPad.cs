// Menus pelo controle (o lerControle do navegador fora da partida): cima e baixo (direcional ou analógico) passam pelos
// botões da tela, na ordem em que aparecem, A confirma e B volta. Todos os botões das telas passam por BotaoCss
// (PartidaCssTelas), que conta a ordem, desenha o contorno dourado no botão em foco e o aperta quando o A chega. Só aparece depois que o controle foi usado.
using UnityEngine;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        bool padNav; int padFoco, padBotoes, padContagem; bool padConfirmar; int padConfirmarQuadro = -1;   // o A vale só no Repaint do quadro em que chegou
        bool cliqueNaPassada;   // um botão respondeu nesta passada do OnGUI (a tela pode ter mudado no meio dela)
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
