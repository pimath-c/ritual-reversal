// Primeira pessoa (a parte de câmera do tick do protótipo): balanço ao andar, arrasto do mouse, campo de visão que abre
// na corrida, câmera que inclina ao andar de lado e treme com a sanidade baixa, lanterna presa à câmera, clarão da boca
// da arma e as cápsulas da carabina. As mãos, as armas e as animações (client/arma.js) ficam em ArmaFP.cs: aqui só
// se monta o "corpo" (balanço, atraso do mouse e do passo) que o ArmaFP aplica por cima dos clipes.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public class PrimeiraPessoa
    {
        const float PI = Mathf.PI;
        public Camera cam; public Transform vm; public ArmaFP arma;
        public Light lanterna, boca;
        // estado (VM e FX do protótipo)
        public float k, sx, sy, mdx, mdy, fov = 72, roll, kickP, spread;
        public float fovBase = 72;
        public float pausa { get { return arma != null ? (float)arma.pausa : 0; } set { if (arma != null) arma.pausa = value; } }
        public float fovPunch, rollImp, fase, bobK, agK, ix, iz;
        public float camFx = 1;                        // Ajustes, Impacto na câmera: 1 normal, .5 reduzido, 0 desligado
        public System.Action AoPasso;                   // o passo do jogador toca no ponto mais baixo do balanço
        readonly ArmaFP.Ctx ctx = new ArmaFP.Ctx();
        // cápsulas de latão ejetadas pela janela da carabina (poucas, reaproveitadas)
        class Capsula { public Transform t; public Vector3 v; public float w, vida; }
        readonly List<Capsula> capsulas = new List<Capsula>(); Material mLatao; Mesh geoCapsula;

        public PrimeiraPessoa(Camera cam)
        {
            this.cam = cam; cam.nearClipPlane = .05f; cam.farClipPlane = 220; cam.fieldOfView = 72;
            vm = Mundo.Grupo(cam.transform, "Primeira pessoa (espelhada)"); vm.localScale = new Vector3(1, 1, -1);
            arma = new ArmaFP(vm); arma.AoCapsula = EjetarCapsula;
            // lanterna (SpotLight do three: 26 m, meio-ângulo 0,42 rad, penumbra 0,55, decaimento 1,6) e clarão da boca
            var lg = new GameObject("Lanterna"); lg.transform.SetParent(cam.transform, false); lg.transform.localPosition = new Vector3(.25f, -.2f, 0); lg.transform.localRotation = Quaternion.LookRotation(new Vector3(-.25f, .2f, 5)); // fora do espelho: eixos do Unity
            lanterna = lg.AddComponent<Light>(); lanterna.type = LightType.Spot; lanterna.color = Mat.Hex(0xfff0d0); lanterna.range = 26; lanterna.spotAngle = .42f * 2 * Mathf.Rad2Deg; lanterna.innerSpotAngle = lanterna.spotAngle * .45f;
            lanterna.intensity = Mundo.IntensidadeUnity(3.2f, 26, 1.6f); lanterna.shadows = Qualidade.nivel >= 2 ? LightShadows.Soft : LightShadows.None; lanterna.enabled = false;
            boca = Mundo.LuzPonto(cam.transform, 0xffc27a, 0, 9, 2, "clarão da arma"); boca.transform.localPosition = new Vector3(.3f, -.2f, .8f); boca.enabled = false;
        }
        float iBoca;
        // ponto de saída do tiro próprio (camera.localToWorld(.27,-.2,-.9)), nas coordenadas do protótipo
        public Vector3 BocaMundo(Transform raizMundo) { return raizMundo.InverseTransformPoint(vm.TransformPoint(new Vector3(.27f, -.2f, -.9f))); }

        public void Tiro(bool exo = false)
        {
            iBoca = exo ? 3.6f : 3; k = 1; kickP += (exo ? .03f : .018f) * Mathf.Max(.35f, camFx); spread = Mathf.Min(1, spread + (exo ? .45f : .35f));
            arma.tT = 0; arma.somFeito = false; arma.vmTiros++; arma.Tiro(exo);
        }
        public void Sigilo() { arma.cast = 1; arma.tT = 0; arma.Sigilo(); arma.somFeito = false; spread = Mathf.Min(1, spread + .2f); }
        // acerto confirmado pelo servidor: o coice segura no auge por um instante e o campo de visão dá um empurrão
        public void Acerto(bool escudo, bool derrubou, bool cabeca) { pausa = escudo ? .02f : derrubou ? .075f : cabeca ? .055f : .04f; fovPunch -= (derrubou ? 1.5f : escudo ? .2f : .55f) * camFx; arma.Acerto(escudo, derrubou); }
        // golpe sofrido: a câmera inclina para o lado de onde veio (lado = -1 ou 1; 0 sem direção)
        public void Golpe(float lado) { rollImp += lado * .025f * camFx; kickP += Random.Range(-.02f, .02f) * camFx; arma.Golpe(lado); }
        public void Tranco(float kick, float fovMenos) { kickP += kick * camFx; fovPunch -= fovMenos * camFx; }
        // o golpe final da execução, quando o servidor confirma a morte
        public void Executou() { arma.Executou(); fovPunch -= 1.2f * camFx; }
        public void Hab() { arma.Hab(); }
        public void PedirInspecao() { arma.PedirInspecao(); }
        public void Mouse(float dx, float dy) { mdx += dx; mdy += dy; }

        // posiciona a câmera (Unity) e anima a primeira pessoa; pos em coordenadas do protótipo
        public void Atualizar(Ator m, Jogo g, float x, float z, bool moving, float mx, float mz, bool correndo, float yaw, float pitch, float dt, float agora)
        {
            float s = (float)m.sanity, shake = s < 40 ? (40 - s) / 40 * .012f * Mathf.Min(1, camFx * 2) : 0; bool down = m.st == "down";
            bool agachado = m.ag && m.st == "alive"; correndo = correndo && !agachado;
            agK = Mathf.Lerp(agK, agachado ? 1 : 0, Mathf.Min(1, dt * (agachado ? 9 : 7))); // desce em ~0,25 s e sobe um pouco mais devagar
            // passos e balanço no mesmo ciclo: dois passos por ciclo (andando 0,45 s, correndo 0,3 s); o passo toca no ponto
            // mais baixo do balanço e o balanço some suave ao parar
            bool andando = moving && m.st == "alive"; bobK = Mathf.Lerp(bobK, andando ? 1 : 0, Mathf.Min(1, dt * 8));
            if (andando) { float f0 = fase; fase += dt * PI / (correndo ? .3f : agachado ? .6f : .45f); if (Mathf.Floor(f0 / PI + .5f) != Mathf.Floor(fase / PI + .5f) && AoPasso != null) AoPasso(); }
            float bob = -Mathf.Abs(Mathf.Sin(fase)) * (correndo ? .05f : .034f) * bobK * Mathf.Max(.5f, camFx) * (1 - agK * .55f);
            cam.transform.position = new Vector3(x, down ? .45f : Mathf.Lerp(1.67f, 1.0f, agK) + bob, -z);
            fovPunch = Mathf.Lerp(fovPunch, 0, Mathf.Min(1, dt * 10)); rollImp = Mathf.Lerp(rollImp, 0, Mathf.Min(1, dt * 7));
            fov = Mathf.Lerp(fov, fovBase + (correndo ? 7 : 0), .1f); float fovAlvo = fov + fovPunch; if (Mathf.Abs(cam.fieldOfView - fovAlvo) > .05f) cam.fieldOfView = fovAlvo;
            roll = Mathf.Lerp(roll, moving && !down ? -mx * .022f : 0, .1f); kickP = Mathf.Lerp(kickP, 0, .14f);
            var ac = arma.ok ? arma.camJolt : new V3d();
            float p = pitch + kickP + (float)ac.x + Random.Range(-shake, shake), y = yaw + (float)ac.y + Random.Range(-shake, shake);
            cam.transform.rotation = Quaternion.Euler(-p * Mathf.Rad2Deg, -y * Mathf.Rad2Deg, (roll + rollImp + (float)ac.z) * Mathf.Rad2Deg);
            lanterna.enabled = m.team == "H" && m.lantern && m.st == "alive";
            iBoca = Mathf.Max(0, iBoca - dt * 30); boca.enabled = iBoca > .01f; boca.intensity = Mundo.IntensidadeUnity(iBoca, 9, 2);
            arma.Clarao(dt);
            k = Mathf.Max(0, k - dt * 7); arma.cast = System.Math.Max(0, arma.cast - dt * 4); spread = Mathf.Max(0, spread - dt * 2.5f);
            sx = Mathf.Lerp(sx, Mathf.Clamp(mdx * .0009f, -.05f, .05f), .2f); sy = Mathf.Lerp(sy, Mathf.Clamp(mdy * .0009f, -.05f, .05f), .2f); mdx *= .7f; mdy *= .7f;
            ix = Mathf.Lerp(ix, andando ? mx : 0, Mathf.Min(1, dt * 6)); iz = Mathf.Lerp(iz, andando ? mz : 0, Mathf.Min(1, dt * 5)); // a arma atrasa um pouco ao começar a andar ou mudar de lado
            float bx = Mathf.Cos(fase) * .011f * bobK, by = Mathf.Abs(Mathf.Sin(fase)) * .013f * bobK + (1 - bobK) * Mathf.Sin(agora * 2) * .003f;
            // contexto do quadro da arma (o mesmo objeto que o index.html passa ao ARMA.quadro)
            Altar altC = null;
            if (m.team == "C" && g != null) foreach (var A in g.altars) if (A.state == "active" && Mathf.Sqrt((float)((x - A.x) * (x - A.x) + (z - A.z) * (z - A.z))) < CFG.circleR) { altC = A; break; }
            string hk = m.hold != null ? m.hold.key ?? "" : ""; double hmax = m.hold != null ? m.hold.max : 0;
            ctx.agora = agora; ctx.andando = andando; ctx.correndo = correndo; ctx.agK = agK; ctx.canalizando = altC != null && hk.Length == 0; ctx.temAltar = altC != null;
            if (altC != null) { ctx.altarX = (float)altC.x; ctx.altarZ = (float)altC.z; }
            ctx.execK = hk.StartsWith("exe") && hmax > 0 ? m.hold.t / hmax : 0; ctx.colK = hk.StartsWith("col") && hmax > 0 ? m.hold.t / hmax : 0; ctx.queimando = hk.StartsWith("burn");
            ctx.bx = -sx + bx - ix * .012f; ctx.by = -sy - by; ctx.bz = iz * -.008f; ctx.brx = iz * -.03f; ctx.bry = sx * 1.4f; ctx.brz = -ix * .05f;
            arma.fase = fase; arma.camFx = camFx; arma.camY = cam.transform.position.y;
            arma.Quadro(dt, m, ctx);
            arma.AtualizarEfeitos(dt);
            AtualizarCapsulas(dt);
        }
        // cápsula de latão: sai pela janela para a direita e para cima, gira, quica no chão e some (coordenadas do Unity)
        void EjetarCapsula()
        {
            Vector3 jan; var g = arma.JanelaCarabina(out jan); if (!g.gameObject.activeInHierarchy) return;
            if (geoCapsula == null) { geoCapsula = Geo.Cilindro(.006f, .006f, .03f, 8).ToMesh("cápsula"); mLatao = Pecas.Metal(0xc8a050, .25f, 1); }
            Capsula c = null; foreach (var o in capsulas) if (o.vida <= 0) { c = o; break; }
            if (c == null) { if (capsulas.Count >= 6) return; var go = new GameObject("cápsula"); go.AddComponent<MeshFilter>().sharedMesh = geoCapsula; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mLatao; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; c = new Capsula { t = go.transform }; capsulas.Add(c); }
            c.t.gameObject.SetActive(true); c.t.position = g.TransformPoint(jan); c.t.rotation = cam.transform.rotation;
            c.v = cam.transform.TransformDirection(new Vector3(1, .9f, -.25f)) * Random.Range(1.6f, 2.2f); c.w = Random.Range(14f, 22f) * Mathf.Rad2Deg; c.vida = .9f;
        }
        void AtualizarCapsulas(float dt)
        {
            foreach (var c in capsulas)
            {
                if (c.vida <= 0) continue; c.vida -= dt; if (c.vida <= 0) { c.t.gameObject.SetActive(false); continue; }
                c.v.y -= 9.8f * dt; c.t.position += c.v * dt; c.t.Rotate(c.w * dt, 0, c.w * .6f * dt, Space.Self);
                if (c.t.position.y < .02f) { var q = c.t.position; q.y = .02f; c.t.position = q; c.v *= .3f; c.v.y = Mathf.Abs(c.v.y) * .4f; }
            }
        }
        public void Esconder(float dt = 0)
        {
            arma.Esconder(); lanterna.enabled = false; boca.enabled = false; foreach (var c in capsulas) { c.vida = 0; c.t.gameObject.SetActive(false); }
            if (dt > 0) arma.AtualizarEfeitos(dt);
        }
    }
}
