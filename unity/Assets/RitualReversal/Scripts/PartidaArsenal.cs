// Arsenal (v6, startArsenal do index.html): as skins de cada arma, na mão, sob a luz de estúdio. Uma partida local
// parada (como Caçador; o Arsenal força a arma), o jogador no corredor da nave olhando as velas, os outros longe; o
// painel à direita escolhe a arma, a skin, uma ação (inspecionar, recarregar ou lançar sigilo, sacar), a vista e a luz.
// Nada muda no jogo além da aparência: a escolha vale nas partidas (PlayerPrefs "rr-skins").
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        bool arsenal; string arsArma = "carabina", arsVista; bool arsEstudio = true; Vector2 arsRolagem;
        void IniciarArsenal() { NovaPartida(true); arsenal = true; arsArma = "carabina"; arsVista = null; arsEstudio = true; }
        void SairArsenal()
        {
            arsenal = false; if (fp != null && fp.arma != null) { fp.arma.Forcar(null); fp.arma.LuzEstudio(false); fp.arma.Vista(cam, null, fp.fovBase); }
            cam.ResetProjectionMatrix(); g = null; LimparVisual(); Travar(false);
        }
        void ArsenalForca(string clipe, bool vazia = false, int n = 3, int? carga = null)
        {
            if (fp == null || fp.arma == null) return;
            fp.arma.Forcar(new ArmaFP.Forca { arma = arsArma, clipe = clipe, vista = arsVista, estudio = arsEstudio, repetir = false, fervor = 100, vel = 1, vazia = vazia, n = n, carga = carga });
        }
        // a cada quadro de jogo: a simulação fica parada; o jogador posa no corredor da nave
        void PassoArsenal(double dt)
        {
            if (g.phase == "intro") { Acao(new Acao { type = "ready" }); return; }
            if (g.phase != "play") { Sim.Step(g, dt); return; }
            var m = Eu(); if (m == null) return;
            if (faseAnterior != "play") { faseAnterior = "play"; Travar(false); ArsenalForca("sacar"); }
            m.x = 2; m.z = -46; m.invuln = 999; lookYaw = .35; lookPitch = -.06; m.yaw = lookYaw; m.pitch = lookPitch;
            foreach (var a in g.actors) if (a != m) { a.x = a.team == "H" ? -125 : 125; a.z = a.team == "H" ? -8 : 8; }
        }
        // depois da câmera do quadro: a vista da vitrine, a luz e o enquadramento (setViewOffset do three: a arma mais ao centro)
        void CameraArsenal()
        {
            if (!arsenal || fp == null || fp.arma == null || g == null || g.phase != "play") return;
            fp.arma.LuzEstudio(arsEstudio); cam.fieldOfView = fp.arma.Vista(cam, arsVista, fp.fovBase);
            float W0 = Screen.width, H0 = Screen.height; if (W0 < 700) { cam.ResetProjectionMatrix(); return; }
            float f = 1.6f, n = cam.nearClipPlane, top = n * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad / 2), alt = 2 * top, larg = cam.aspect * alt, esq = -.5f * larg;
            esq += W0 * (f - 1) * larg / (W0 * f); top -= H0 * (f - 1) * .75f * alt / (H0 * f); larg *= 1 / f; alt *= 1 / f;
            cam.projectionMatrix = Matrix4x4.Frustum(esq, esq + larg, top - alt, top, n, cam.farClipPlane);
        }
        // #arsenal: a coluna de 400 px à direita (padding 22 20 18) sobre o degradê, na fonte do sistema (system-ui):
        // o h2, a coleção em ouro, o texto, as abas das armas, um cartão por skin (ofício numa pílula à direita, o nome em
        // IM Fell, "em uso", a descrição), as ações, a vista, a luz e o Voltar ao menu. Rola se não couber.
        void PainelArsenal()
        {
            if (fp == null || fp.arma == null || fp.arma.skins == null) return;
            var S = fp.arma.skins; float w = Mathf.Min(400, W), x0 = W - w;
            Color c0 = new Color(5 / 255f, 5 / 255f, 6 / 255f, 0), c1 = new Color(5 / 255f, 5 / 255f, 6 / 255f, .86f), c2 = new Color(5 / 255f, 5 / 255f, 6 / 255f, .94f);
            PintarDegrade(new Rect(x0, 0, w * .14f, H), c0, c1, false); PintarDegrade(new Rect(x0 + w * .14f, 0, w * .86f, H), c1, c2, false);
            medindo = true; float hc = CorpoArsenal(0, 0, w - 40, S); medindo = false;
            if (22 + hc + 18 <= H) { CorpoArsenal(x0 + 20, 22, w - 40, S); return; }
            // a barra de rolagem come a largura do conteúdo, como no navegador
            float wc = w - 40 - 15; medindo = true; hc = CorpoArsenal(0, 0, wc, S); medindo = false;
            arsRolagem = GUI.BeginScrollView(new Rect(x0, 0, w, H), arsRolagem, new Rect(0, 0, w - 15, 22 + hc + 18), false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            CorpoArsenal(20, 22, wc, S); GUI.EndScrollView();
        }
        float CorpoArsenal(float x, float y, float w, SkinsFP S)
        {
            float y0 = y;
            var stH = Css("serif", 30, BONE); if (!medindo) EspacadaNaBase(x, y + SubidaPx(stH), "Arsenal", stH, .6f); y += LNs(30);
            // .col: margem 2 0 12; p.txt: 13 px, linha de 1,45, margem 0 0 12 (o 12 de cima se funde com o da .col)
            y += 2; if (!medindo) Linha(x, y, w, LNsis(13), S.colecao, Css("sys", 13, GOLD)); y += LNsis(13) + 12;
            y += Ps(x, y, w, S.colecaoTexto + " Nada muda no jogo além da aparência; tudo é livre para escolher.", Css("sys", 13, MUDO), 13 * 1.45f) + 12;
            var armas = new[] { new[] { "carabina", "Carabina" }, new[] { "revolver", "Revólver" }, new[] { "cetro", "Cetro" } };
            y += AbasArsenal(x, y, w, armas.Select(a => a[1]).ToArray(), i => armas[i][0] == arsArma, i => { arsArma = armas[i][0]; ArsenalForca("sacar"); som.Tocar("click"); }) + 12;
            string usada = S.Escolhida(arsArma);
            foreach (var sk in S.catalogo[arsArma]) y += CartaoSkin(x, y, w, S, sk, sk.id == usada) + 8;
            // .acoes: margem 10 0 6 (o 10 engole o 8 do último cartão)
            y += 2; bool cetro = arsArma == "cetro";
            y += AbasArsenal(x, y, w, new[] { "Inspecionar", cetro ? "Lançar sigilo" : "Recarregar", "Sacar" }, i => false, i =>
            {
                if (i == 0) ArsenalForca("inspecao"); else if (i == 2) ArsenalForca("sacar");
                else if (cetro) { ArsenalForca("pronta"); StartCoroutine(Depois(.06f, () => fp.arma.DisparoForcado(null))); } else ArsenalForca("recarga", true, 3, 0);
            }) + 6;
            // .rot: 12 px apagado, margem 10 0 4 (funde com o 6 das ações e com o 12 das abas)
            var stR = Css("sys", 12, MUDO);
            y += 4; if (!medindo) Linha(x, y, w, LNsis(12), "Vista", stR); y += LNsis(12) + 4;
            var vistas = new[] { new[] { "", "Jogador" }, new[] { "ladoPerto", "De lado" }, new[] { "perto", "De perto" } };
            y += AbasArsenal(x, y, w, vistas.Select(v => v[1]).ToArray(), i => (arsVista ?? "") == vistas[i][0], i => { arsVista = vistas[i][0] == "" ? null : vistas[i][0]; if (fp.arma.forca != null) fp.arma.forca.vista = arsVista; }) + 12;
            if (!medindo) Linha(x, y, w, LNsis(12), "Luz", stR); y += LNsis(12) + 4;
            y += AbasArsenal(x, y, w, new[] { "Da noite", "Estúdio" }, i => (i == 1) == arsEstudio, i => arsEstudio = i == 1) + 12;
            // button.go.ghost.sair: inline-block com margem de 14 em cima (não se funde com a das abas)
            y += 14; float hb = AlturaBotao("ghost"); if (BotaoCss(new Rect(x, y, w, hb), "Voltar ao menu", "ghost")) SairArsenal(); y += hb;
            return y - y0;
        }
        // .abas / .acoes: botões de largura igual (flex: 1), 6 px entre eles, padding 8 × 6, cantos de 6 px
        float AbasArsenal(float x, float y, float w, string[] textos, System.Func<int, bool> sel, System.Action<int> clique)
        {
            int n = textos.Length; float bw = (w - 6 * (n - 1)) / n, bh = LNsis(13) + 16 + 2;
            for (int i = 0; i < n; i++) { bool s = sel(i); if (BotaoCss(new Rect(x + i * (bw + 6), y, bw, bh), textos[i], s ? "arsAbaSel" : "arsAba") && !s) clique(i); }
            return bh;
        }
        // .skin: borda clara de 1 px com cantos de 8, padding 10 × 12; a pílula do ofício (float: right, 11 px em
        // maiúsculas, letter-spacing .08em, borda na cor dele); o nome em IM Fell 18 na linha do corpo (16 px do sistema)
        // com o "· em uso" em letra menor; o p de 12,5 px com linha de 1,45, 6 px abaixo
        float CartaoSkin(float x, float y, float w, SkinsFP S, SkinsFP.Skin sk, bool emUso)
        {
            float wi = w - 26; var stP = Css("sys", 12, emUso ? Hx("#cfc6b6") : MUDO); float lhP = 12.5f * 1.45f;
            float hl = Mathf.Max(Ar(16 * subSis), 16) + Mathf.Max(Ar(16 * descSis), 7), bas = Mathf.Max(Ar(16 * subSis), 16);
            float h = 1 + 10 + hl + 6 + AlturaParagrafo(sk.texto, stP, wi, lhP) + 10 + 1; var r = new Rect(x, y, w, h);
            if (BotaoCss(r, "", emUso ? "arsSkinSel|mudo" : "arsSkin|mudo") && !emUso) { fp.arma.TrocarSkin(arsArma, sk.id); ArsenalForca("inspecao"); som.Tocar("click"); }
            if (medindo) return h;
            string[] O; S.oficios.TryGetValue(sk.oficio, out O); Color co; if (O == null || !ColorUtility.TryParseHtmlString(O[1], out co)) co = BONE;
            float xi = x + 13, yi = y + 11;
            // a pílula: altura da linha de 11 px + 2 × 2 de padding + borda
            string of = (O != null ? O[0] : sk.oficio).ToUpperInvariant(); var stO = Css("sys", 11, co); float esp = 11 * .08f, wo = EspacadaNaBase(0, 0, of, stO, esp, false) + 14 + 2, ho = LNsis(11) + 4 + 2;
            var rp = new Rect(xi + wi - wo, yi, wo, ho); GUI.Box(rp, GUIContent.none, EstiloPilula(co)); EspacadaNaBase(rp.x + 8, rp.y + 3 + Ar(11 * subSis), of, stO, esp);
            var stB = Css("serif", 18, BONE); NaBase(xi, yi + bas, wi, sk.nome, stB);
            if (emUso) NaBase(xi + Larg(sk.nome, stB) + Larg(" ", Css("sys", 13, GOLD)), yi + bas, 200, "· em uso", Css("sys", 13, GOLD));
            Paragrafo(xi, yi + hl + 6, wi, lhP, sk.texto, stP);
            return h;
        }
        readonly Dictionary<string, GUIStyle> pilulas = new Dictionary<string, GUIStyle>();
        GUIStyle EstiloPilula(Color c)
        {
            string k = ColorUtility.ToHtmlStringRGBA(c); GUIStyle s; if (pilulas.TryGetValue(k, out s)) return s;
            s = new GUIStyle(GUIStyle.none) { border = new RectOffset(10, 10, 10, 10) }; s.normal.background = TexArredondada(new Color(0, 0, 0, 0), c, 10); pilulas[k] = s; return s;
        }
    }
}
