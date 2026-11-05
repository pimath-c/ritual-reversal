// Controles de toque do navegador (TQ e #toque): o direcional aparece onde o dedo encosta na faixa esquerda (42% da
// tela, abaixo dos 84 px de cima), o resto da tela olha, e botões redondos seguram ou apertam a ação da tecla configurada
// (EntradaUnity.ToqueSegurar). Correr e agachar ligam e desligam; o mapa alterna; empurrar o direcional até o alto corre;
// o botão de atacar também olha. Com menu aberto (mouse solto) os controles somem e soltam tudo. As medidas são as do CSS
// (#tqFire 92 px a 22 do canto, .tq 62, .mini 50, o topo com duas fileiras de 58 × 34 à direita).
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        bool toqueAtivo;   // Ajustes: controles de toque (PlayerPrefs "rr-toque"; ligado de saída no celular)
        static bool travadoToque;   // com toque não há cursor para prender: o "locked" do navegador
        class BotaoToque { public string acao, txt; public float dir = -1, esq = -1, baixo, tam = 62, fonte = 12; public bool so_h, so_c; public Color borda = new Color(232 / 255f, 224 / 255f, 208 / 255f, .35f); }
        static readonly BotaoToque[] BOTOES_TOQUE = {
            new BotaoToque { acao = "fire", txt = "Atacar", dir = 22, baixo = 22, tam = 92, fonte = 15, borda = new Color(1, 90 / 255f, 110 / 255f, .55f) },
            new BotaoToque { acao = "use", txt = "Usar", dir = 124, baixo = 24 },
            new BotaoToque { acao = "abil", txt = "Hab.", dir = 28, baixo = 126 },
            new BotaoToque { acao = "reload", txt = "Recarregar", dir = 116, baixo = 106, tam = 56, fonte = 10, so_h = true },
            new BotaoToque { acao = "use2", txt = "Transferir", dir = 116, baixo = 106, tam = 56, fonte = 10, so_c = true },
            new BotaoToque { acao = "lantern", txt = "Lanterna", dir = 22, baixo = 200, tam = 50, fonte = 10, so_h = true },
            new BotaoToque { acao = "sensor", txt = "Sensor", dir = 78, baixo = 178, tam = 50, fonte = 10, so_h = true },
            new BotaoToque { acao = "flare", txt = "Flare", dir = 134, baixo = 170, tam = 50, fonte = 10, so_h = true },
            new BotaoToque { acao = "sprint", txt = "Correr", esq = 14, baixo = 26, tam = 50, fonte = 10 },
            new BotaoToque { acao = "agachar", txt = "Agachar", esq = 70, baixo = 26, tam = 50, fonte = 10 } };
        // #tqTopo: right 10, top 10, 198 de largura, quebra de linha alinhada à direita, 6 entre eles
        static readonly string[][] TOPO_TOQUE = { new[] { "ping", "Marcar" }, new[] { "falas", "Frases" }, new[] { "map", "Mapa" }, new[] { "caderno", "Caderno" }, new[] { "pausa", "Pausa" } };

        int stickId = -1; Vector2 stickOrig, stickDelta; double toqueMx, toqueMz; bool stickCorre;
        readonly Dictionary<int, Vector2> olharToque = new Dictionary<int, Vector2>();
        readonly Dictionary<int, string> dedoBotao = new Dictionary<int, string>();
        readonly HashSet<string> toqueLigado = new HashSet<string>();   // correr, agachar e mapa alternam
        readonly HashSet<int> dedosVivos = new HashSet<int>();

        // os controles valem só com a noite rolando e nada aberto por cima (o #toque.solto do navegador)
        bool ToqueVisivel() { return toqueAtivo && g != null && g.phase == "play" && Travado() && lojaAberta == null && !arsenal; }
        float EscalaTela() { return Screen.height / H; }

        // retângulos dos botões nas medidas do CSS (W e H são a tela em pixels de CSS)
        Rect RetBotaoToque(BotaoToque b) { float x = b.esq >= 0 ? b.esq : W - b.dir - b.tam; return new Rect(x, H - b.baixo - b.tam, b.tam, b.tam); }
        Rect RetTopoToque(int i)
        {
            const float bw = 58, bh = 34, gap = 6, larg = 198; int porLinha = Mathf.Max(1, Mathf.FloorToInt((larg + gap) / (bw + gap)));
            int linha = i / porLinha, col = i % porLinha, naLinha = Mathf.Min(porLinha, TOPO_TOQUE.Length - linha * porLinha);
            float x = W - 10 - (naLinha - col) * (bw + gap) + gap; return new Rect(x, 10 + linha * (bh + gap), bw, bh);
        }
        bool BotaoValido(BotaoToque b) { string p = MeuPapel(); return !(b.so_h && p != "H") && !(b.so_c && p != "C"); }

        void SoltarToque()
        {
            stickId = -1; toqueMx = toqueMz = 0; stickCorre = false; olharToque.Clear(); dedoBotao.Clear(); toqueLigado.Clear(); dedosVivos.Clear();
            EntradaUnity.ToqueSoltarTudo();
        }

        // a cada quadro, antes de ler a entrada: os dedos viram direcional, olhar e botões
        void AtualizarToque()
        {
            EntradaUnity.ToqueNovoQuadro();
            if (!ToqueVisivel()) { if (dedosVivos.Count > 0 || toqueLigado.Count > 0 || stickId >= 0) SoltarToque(); return; }
            float esc = EscalaTela();
            foreach (var t in EntradaUnity.Toques())
            {
                var p = new Vector2(t.pos.x / esc, (Screen.height - t.pos.y) / esc);   // pixels da tela -> pixels de CSS, y para baixo
                bool vivo = dedosVivos.Contains(t.id);
                if (t.fase == 2) { if (vivo) FimDedo(t.id); continue; }
                if (!vivo) { dedosVivos.Add(t.id); ComecoDedo(t.id, p); } else MoveDedo(t.id, p);
            }
            // os dedos que sumiram sem avisar (o Input antigo não repete o fim)
            if (dedosVivos.Count > 0)
            {
                var agora = new HashSet<int>(); foreach (var t in EntradaUnity.Toques()) if (t.fase != 2) agora.Add(t.id);
                foreach (var id in new List<int>(dedosVivos)) if (!agora.Contains(id)) FimDedo(id);
            }
        }
        void ComecoDedo(int id, Vector2 p)
        {
            // os botões ficam por cima das áreas (stopPropagation)
            for (int i = 0; i < TOPO_TOQUE.Length; i++)
                if (RetTopoToque(i).Contains(p)) { ApertarBotaoToque(id, TOPO_TOQUE[i][0], p); return; }
            foreach (var b in BOTOES_TOQUE)
                if (BotaoValido(b) && DentroCirculo(RetBotaoToque(b), p)) { ApertarBotaoToque(id, b.acao, p); return; }
            if (p.y < 84) return;   // #tqEsq e #tqDir começam 84 px abaixo do topo
            if (p.x < W * .42f) { if (stickId < 0) { stickId = id; stickOrig = p; stickDelta = Vector2.zero; } }
            else olharToque[id] = p;
        }
        static bool DentroCirculo(Rect r, Vector2 p) { return (p - r.center).sqrMagnitude <= r.width * r.width / 4; }
        void ApertarBotaoToque(int id, string a, Vector2 p)
        {
            if (a == "pausa") { Travar(false); return; }
            if (a == "map" || a == "sprint" || a == "agachar")
            {
                bool lig = !toqueLigado.Contains(a); if (lig) toqueLigado.Add(a); else toqueLigado.Remove(a);
                EntradaUnity.ToqueSegurar(a, lig); return;
            }
            dedoBotao[id] = a; EntradaUnity.ToqueSegurar(a, true); if (a == "fire") olharToque[id] = p;
        }
        void MoveDedo(int id, Vector2 p)
        {
            if (id == stickId)
            {
                const float R = 56; var d = p - stickOrig; float m = d.magnitude; if (m > R) d *= R / m; stickDelta = d; float k = d.magnitude / R;
                toqueMx = k < .22f ? 0 : d.x / R; toqueMz = k < .22f ? 0 : d.y / R;
                bool corre = toqueMz < -.92; if (corre != stickCorre) { stickCorre = corre; if (!toqueLigado.Contains("sprint")) EntradaUnity.ToqueSegurar("sprint", corre); }
                return;
            }
            Vector2 o; if (olharToque.TryGetValue(id, out o))
            {
                var d = p - o; olharToque[id] = p;
                lookYaw -= d.x * .0052 * sensibilidade; lookPitch = Simulacao.JS.Clamp(lookPitch - d.y * .0052 * sensibilidade, -1.45, 1.45);
                if (fp != null) fp.Mouse(d.x * .5f, d.y * .5f);
            }
        }
        void FimDedo(int id)
        {
            dedosVivos.Remove(id); olharToque.Remove(id);
            if (id == stickId) { stickId = -1; toqueMx = toqueMz = 0; if (stickCorre) { stickCorre = false; if (!toqueLigado.Contains("sprint")) EntradaUnity.ToqueSegurar("sprint", false); } }
            string a; if (dedoBotao.TryGetValue(id, out a)) { dedoBotao.Remove(id); EntradaUnity.ToqueSegurar(a, false); }
        }

        // ---------- desenho ----------
        readonly Dictionary<string, Texture2D> circulos = new Dictionary<string, Texture2D>();
        // círculo com borda (border-radius: 50%), desenhado do tamanho exato: d px de diâmetro, borda de e px
        Texture2D TexCirculo(int d, Color fundo, Color borda, float e)
        {
            string k = d + "|" + ColorUtility.ToHtmlStringRGBA(fundo) + ColorUtility.ToHtmlStringRGBA(borda) + e; Texture2D t; if (circulos.TryGetValue(k, out t)) return t;
            t = new Texture2D(d, d, TextureFormat.RGBA32, false); var px = new Color[d * d]; float r = d / 2f;
            for (int y = 0; y < d; y++) for (int x = 0; x < d; x++)
                {
                    float dist = Mathf.Sqrt((x + .5f - r) * (x + .5f - r) + (y + .5f - r) * (y + .5f - r));
                    float dentroTudo = Mathf.Clamp01(r - dist + .5f), dentroMiolo = Mathf.Clamp01(r - e - dist + .5f), naBorda = dentroTudo - dentroMiolo;
                    float wa = naBorda * borda.a, wb = dentroMiolo * fundo.a, ws = wa + wb;
                    px[y * d + x] = ws > 0 ? new Color((borda.r * wa + fundo.r * wb) / ws, (borda.g * wa + fundo.g * wb) / ws, (borda.b * wa + fundo.b * wb) / ws, ws) : new Color(0, 0, 0, 0);
                }
            t.SetPixels(px); t.Apply(false, true); t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp; circulos[k] = t; return t;
        }
        void DesenharToque()
        {
            if (!ToqueVisivel()) return;
            Color bone = BONE, fundoTq = new Color(12 / 255f, 12 / 255f, 16 / 255f, .45f), fundoOn = new Color(243 / 255f, 213 / 255f, 142 / 255f, .35f);
            // o direcional: anel de 120 com borda de 2 e o botão de 52 que segue o dedo
            if (stickId >= 0)
            {
                GUI.DrawTexture(new Rect(stickOrig.x - 60, stickOrig.y - 60, 120, 120), TexCirculo(120, new Color(10 / 255f, 10 / 255f, 14 / 255f, .25f), new Color(232 / 255f, 224 / 255f, 208 / 255f, .28f), 2));
                GUI.DrawTexture(new Rect(stickOrig.x + stickDelta.x - 26, stickOrig.y + stickDelta.y - 26, 52, 52), TexCirculo(52, new Color(232 / 255f, 224 / 255f, 208 / 255f, .35f), new Color(0, 0, 0, 0), 0));
            }
            foreach (var b in BOTOES_TOQUE)
            {
                if (!BotaoValido(b)) continue; var r = RetBotaoToque(b); bool on = dedoBotao.ContainsValue(b.acao) || toqueLigado.Contains(b.acao);
                GUI.DrawTexture(r, TexCirculo(Mathf.RoundToInt(b.tam), on ? fundoOn : fundoTq, on ? GOLD_HI : b.borda, 2));
                var st = Css("400", Mathf.RoundToInt(b.fonte), bone, TextAnchor.UpperCenter); Linha(r.x, r.y + (r.height - LN(b.fonte)) / 2, r.width, LN(b.fonte), b.txt, st);
            }
            for (int i = 0; i < TOPO_TOQUE.Length; i++)
            {
                var r = RetTopoToque(i); string a = TOPO_TOQUE[i][0]; bool on = dedoBotao.ContainsValue(a) || toqueLigado.Contains(a);
                GUI.DrawTexture(r, TexArredondada2(on ? fundoOn : fundoTq, on ? GOLD_HI : new Color(232 / 255f, 224 / 255f, 208 / 255f, .35f), r));
                Linha(r.x, r.y + (r.height - LN(10)) / 2, r.width, LN(10), TOPO_TOQUE[i][1], Css("400", 10, bone, TextAnchor.UpperCenter));
            }
        }
        // o botão do topo: cantos de 10 e borda de 2, no tamanho exato
        readonly Dictionary<string, Texture2D> pilulasTopo = new Dictionary<string, Texture2D>();
        Texture2D TexArredondada2(Color fundo, Color borda, Rect r)
        {
            int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height); string k = w + "x" + h + ColorUtility.ToHtmlStringRGBA(fundo) + ColorUtility.ToHtmlStringRGBA(borda); Texture2D t;
            if (pilulasTopo.TryGetValue(k, out t)) return t; t = new Texture2D(w, h, TextureFormat.RGBA32, false); var px = new Color[w * h]; const float R = 10, E = 2;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x + .5f, R, w - R), cy = Mathf.Clamp(y + .5f, R, h - R), dist = Mathf.Sqrt((x + .5f - cx) * (x + .5f - cx) + (y + .5f - cy) * (y + .5f - cy));
                    float borda0 = Mathf.Min(Mathf.Min(x + .5f, w - x - .5f), Mathf.Min(y + .5f, h - y - .5f)); bool canto = (x + .5f < R || x + .5f > w - R) && (y + .5f < R || y + .5f > h - R);
                    float fora = canto ? dist - R : -borda0;   // distância com sinal ao contorno
                    float dentroTudo = Mathf.Clamp01(.5f - fora), dentroMiolo = Mathf.Clamp01(.5f - (fora + E)), naBorda = dentroTudo - dentroMiolo;
                    float wa = naBorda * borda.a, wb = dentroMiolo * fundo.a, ws = wa + wb;
                    px[y * w + x] = ws > 0 ? new Color((borda.r * wa + fundo.r * wb) / ws, (borda.g * wa + fundo.g * wb) / ws, (borda.b * wa + fundo.b * wb) / ws, ws) : new Color(0, 0, 0, 0);
                }
            t.SetPixels(px); t.Apply(false, true); t.filterMode = FilterMode.Bilinear; pilulasTopo[k] = t; return t;
        }
    }
}
