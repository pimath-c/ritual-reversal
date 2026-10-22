// Arsenal (v6, startArsenal do index.html): as skins de cada arma, na mão, sob a luz de estúdio. Uma partida local
// parada (como Caçador; o Arsenal força a arma), o jogador no corredor da nave olhando as velas, os outros longe; o
// painel à direita escolhe a arma, a skin, uma ação (inspecionar, recarregar ou lançar sigilo, sacar), a vista e a luz.
// Nada muda no jogo além da aparência: a escolha vale nas partidas (PlayerPrefs "rr-skins").
using System.Collections;
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
        void PainelArsenal()
        {
            if (fp == null || fp.arma == null || fp.arma.skins == null) return;
            var S = fp.arma.skins; float w = Mathf.Min(400, W); var r = new Rect(W - w, 0, w, H); Caixa(r, new Color(.031f, .027f, .035f, .94f));
            GUILayout.BeginArea(new Rect(r.x + 20, 22, w - 40, H - 40)); arsRolagem = GUILayout.BeginScrollView(arsRolagem);
            GUILayout.Label("<size=30>Arsenal</size>", sTexto); GUILayout.Label("<size=13>" + Cor(S.colecao, OURO) + "</size>", sPequeno);
            GUILayout.Label("<size=13>" + Cor(S.colecaoTexto + " Nada muda no jogo além da aparência; tudo é livre para escolher.", MUDO) + "</size>", sPequeno);
            var sb = new GUIStyle(sBotao) { fontSize = 14, padding = new RectOffset(6, 6, 8, 8) };
            GUILayout.BeginHorizontal();
            foreach (var k in new[] { new[] { "carabina", "Carabina" }, new[] { "revolver", "Revólver" }, new[] { "cetro", "Cetro" } })
                if (Bt(arsArma == k[0] ? Cor(k[1], OURO_VIVO) : k[1], sb)) { arsArma = k[0]; ArsenalForca("sacar"); som.Tocar("click"); }
            GUILayout.EndHorizontal(); GUILayout.Space(8);
            string usada = S.Escolhida(arsArma);
            foreach (var sk in S.catalogo[arsArma])
            {
                string[] O; S.oficios.TryGetValue(sk.oficio, out O); Color co; if (O == null || !ColorUtility.TryParseHtmlString(O[1], out co)) co = OSSO;
                string t = "<size=12>" + Cor(O != null ? O[0] : sk.oficio, co) + "</size>\n<b>" + sk.nome + "</b>" + (sk.id == usada ? "  <size=12>" + Cor("· em uso", OURO) + "</size>" : "") + "\n<size=13>" + Cor(sk.texto, new Color(.81f, .776f, .714f)) + "</size>";
                if (Bt(t, new GUIStyle(sb) { alignment = TextAnchor.UpperLeft, wordWrap = true, richText = true })) { fp.arma.TrocarSkin(arsArma, sk.id); ArsenalForca("inspecao"); som.Tocar("click"); }
            }
            GUILayout.Space(6); GUILayout.BeginHorizontal();
            if (Bt("Inspecionar", sb)) ArsenalForca("inspecao");
            if (arsArma == "cetro") { if (Bt("Lançar sigilo", sb)) { ArsenalForca("pronta"); StartCoroutine(Depois(.06f, () => fp.arma.DisparoForcado(null))); } }
            else if (Bt("Recarregar", sb)) ArsenalForca("recarga", true, 3, 0);
            if (Bt("Sacar", sb)) ArsenalForca("sacar");
            GUILayout.EndHorizontal(); GUILayout.Space(8);
            GUILayout.Label("<size=13>" + Cor("Vista", MUDO) + "</size>", sPequeno); GUILayout.BeginHorizontal();
            foreach (var v in new[] { new[] { "", "Jogador" }, new[] { "ladoPerto", "De lado" }, new[] { "perto", "De perto" } })
                if (Bt((arsVista ?? "") == v[0] ? Cor(v[1], OURO_VIVO) : v[1], sb)) { arsVista = v[0] == "" ? null : v[0]; if (fp.arma.forca != null) fp.arma.forca.vista = arsVista; }
            GUILayout.EndHorizontal();
            GUILayout.Label("<size=13>" + Cor("Luz", MUDO) + "</size>", sPequeno); GUILayout.BeginHorizontal();
            if (Bt(!arsEstudio ? Cor("Da noite", OURO_VIVO) : "Da noite", sb)) arsEstudio = false;
            if (Bt(arsEstudio ? Cor("Estúdio", OURO_VIVO) : "Estúdio", sb)) arsEstudio = true;
            GUILayout.EndHorizontal(); GUILayout.Space(14);
            if (Bt("Voltar ao menu", sb)) SairArsenal();
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
