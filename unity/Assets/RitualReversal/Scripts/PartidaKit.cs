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

        void Ferramenta(Rect r, string tecla, string nome, string num, float cd, bool aceso)
        {
            Caixa(r, new Color(.031f, .035f, .043f, .6f)); var b = aceso ? OURO : new Color(.2f, .227f, .259f);
            Caixa(new Rect(r.x, r.y, r.width, 1), b); Caixa(new Rect(r.x, r.yMax - 1, r.width, 1), b); Caixa(new Rect(r.x, r.y, 1, r.height), b); Caixa(new Rect(r.xMax - 1, r.y, 1, r.height), b);
            var st = new GUIStyle(sPequeno) { fontSize = 13, alignment = TextAnchor.UpperCenter, wordWrap = false, richText = true };
            GUI.Label(new Rect(r.x, r.y + 3, r.width, 18), "<b>" + Cor(tecla, OURO) + "</b>", st);
            GUI.Label(new Rect(r.x, r.y + 20, r.width, 18), nome + (num != null ? " <b>" + num + "</b>" : ""), st);
            if (cd > 0) Caixa(new Rect(r.x, r.yMax - r.height * Mathf.Clamp01(cd), r.width, r.height * Mathf.Clamp01(cd)), new Color(0, 0, 0, .65f));
        }
        void Kit(Ator m)
        {
            float w = 74, h = 42, gap = 6, y = H - 26 - h; var F = Regras.FEITICOS[m.feitico];
            var lista = new System.Collections.Generic.List<System.Action<Rect>>();
            if (m.team == "H")
            {
                lista.Add(r => Ferramenta(r, "Q", "Sensor", null, (float)(m.sensorCd / 15), false));
                lista.Add(r => Ferramenta(r, "F", "Lanterna", null, (float)((100 - m.carga) / 100), m.lantern));
                lista.Add(r => Ferramenta(r, "G", "Flare", m.flares.ToString(), 0, false));
            }
            else { lista.Add(r => Ferramenta(r, "☉", "Reagentes", m.reag.ToString(), 0, false)); lista.Add(r => Ferramenta(r, "E", "Chamariz", g.decoys.ToString(), 0, false)); }
            lista.Add(r => Ferramenta(new Rect(r.x - 30, r.y, r.width + 30, r.height), "Botão dir.", F.nome.Length > 12 ? F.nome.Split(' ')[0] : F.nome, null, (float)(m.abilCd / F.cd), false));
            float x = W - 26 - 30 - lista.Count * w - (lista.Count - 1) * gap;
            for (int i = 0; i < lista.Count; i++) lista[i](new Rect(x + 30 + i * (w + gap), y, w, h));
        }
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
            GUI.Label(new Rect(W - 418, 12, 400, 22), "<size=13>" + Cor(linha, ruim ? CARMIM_HI : MUDO) + "</size>", new GUIStyle(sPequeno) { alignment = TextAnchor.UpperRight, wordWrap = false });
        }
    }
}
