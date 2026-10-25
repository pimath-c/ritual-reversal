// Música da tela inicial e trilha de tensão (MUS e TN do index.html), tocadas no OnAudioFilterRead do Som.
// Música: o laço gravado musica_laco (ré menor, Rém, Si♭, Solm, Lá, no coro gravado, um sino a cada volta e eco de nave,
// montado por tools/montar-sons.py). Só fora da partida; quando some, volta ao começo, como o navegador.
// Tensão: o laço gravado tensao_laco (bordão grave e um chiado alto de vidro) que cresce com o perigo; a partida decide o
// nível (Tensao) e o som só segue. v6.3: nada aqui é sintetizado.
using System;

namespace RitualReversal.Visual
{
    public partial class Som
    {
        // ---------- música ----------
        Gravacao lacoMus, lacoT; volatile bool musicaLigada; float musK; double pMus, pTensao;
        public void Musica(bool ligada) { musicaLigada = ligada; }
        // uma vez por bloco: o volume que sobe e desce
        void BlocoMusica(double tb, float kBloco)
        {
            float alvo = musicaLigada ? 1 : 0; musK += (alvo - musK) * Math.Min(1, kBloco * (alvo > 0 ? .5f : 1.5f));
            if (alvo == 0 && musK < .002f) { musK = 0; pMus = 0; }
        }
        double AmostraMusica()
        {
            if (musK < .002f || lacoMus == null) return 0;
            return PassoLaco(lacoMus, ref pMus, 1, sr) * .9 * musK;
        }

        // ---------- tensão ----------
        volatile float tensaoAlvo; float tensaoG;
        public void Tensao(float k) { tensaoAlvo = k; }
        void BlocoTensao(double tb, float kBloco) { tensaoG += (.2f * tensaoAlvo - tensaoG) * (1 - (float)Math.Exp(-kBloco / .25f)); }
        double AmostraTensao()
        {
            if (tensaoG < .0002f || lacoT == null) return 0;
            return PassoLaco(lacoT, ref pTensao, 1, sr) * tensaoG;
        }
    }
}
