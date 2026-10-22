// O canto do kit e a mira do navegador (buildTools, #kit, #cross, aplicarMira e #net do index.html): a fileira de
// ferramentas embaixo da munição (Caçador: Sensor, Lanterna e sinalizadores; Cultista: reagentes e Chamarizes; e o
// feitiço), com a recarga escurecendo de baixo para cima; a mira em cruz, ponto ou círculo, na cor e no tamanho dos
// Ajustes, que abre um pouco com o tiro e com o movimento; a linha de quadros por segundo (e, online, sala, ping e
// pacotes) no canto de cima. Brilho nos Ajustes: a exposição do pós (1,25 × brilho, como o toneMappingExposure).
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        float abertura, fpsMedia = 60;
        void AbrirMira(float v) { abertura = Mathf.Min(1, abertura + v); }

        // a mira: 22 px, traços de 2 px com o miolo vazio; abre com o tiro (abertura) e com o movimento
        void Mira()
        {
            string estilo = PlayerPrefs.GetString("rr-mira", "cruz"); Color c; if (!ColorUtility.TryParseHtmlString(PlayerPrefs.GetString("rr-mira-cor", "#e8e0d0"), out c)) c = OSSO; c.a = .85f;
            abertura = Mathf.Max(0, abertura - dtG * 2.2f); bool anda = ultimaEntrada != null && (ultimaEntrada.mx != 0 || ultimaEntrada.mz != 0);
            float k = (1 + abertura * .8f + (anda ? .35f : 0)) * PlayerPrefs.GetFloat("rr-mira-tam", 1), cx = W / 2, cy = H / 2;
            if (estilo == "ponto") { Tracos.Disco(cx, cy, 3 * k, c); return; }
            if (estilo == "circulo") { Tracos.Anel(cx, cy, 7 * k, 2 * k, c); return; }
            float L = 8 * k, e = 2 * k, vazio = 3 * k;
            Caixa(new Rect(cx - e / 2, cy - L, e, L - vazio), c); Caixa(new Rect(cx - e / 2, cy + vazio, e, L - vazio), c);
            Caixa(new Rect(cx - L, cy - e / 2, L - vazio, e), c); Caixa(new Rect(cx + vazio, cy - e / 2, L - vazio, e), c);
        }
        // a linha do canto de cima (#net): quadros por segundo; online, também a sala e o ping
        void LinhaRede()
        {
            float dt = Time.unscaledDeltaTime; if (dtG > 0 && dt > 0) fpsMedia += (1 / dt - fpsMedia) * .05f;
            string linha = Mathf.RoundToInt(fpsMedia) + " fps"; bool ruim = false;
            if (online) { int ping = Mathf.RoundToInt(this.ping); linha = "Sala " + salaCodigo + " · " + ping + " ms · " + linha; ruim = ping > 250; }
            // #net: 13 px apagado (carmim com ping alto), 12 px do topo e 16 da direita
            Linha(W - 16 - 400, 12, 400, LN(13), linha, Css("400", 13, ruim ? CrimsonHi : MUDO, TextAnchor.UpperRight));
        }
    }
}
