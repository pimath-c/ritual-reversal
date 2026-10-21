// Primeira pessoa (buildViewmodels + a parte de câmera do tick do protótipo): carabina ou revólver com mãos de luva
// para o Caçador, cetro de vértebras com as runas em órbita (uma por sigilo disponível) para o Cultista; balanço ao
// andar, arrasto do mouse, coice, recarga, corrida; lanterna presa à câmera e clarão da boca da arma; campo de visão
// que abre na corrida, câmera que inclina ao andar de lado e treme com a sanidade baixa.
// v5.8: cada arma tem um perfil (coice, mola de volta, ciclo mecânico: alavanca e cão da carabina, tambor e cão do
// revólver, runas do cetro); pausa de impacto quando o acerto é confirmado; passos no mesmo ciclo do balanço.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public class PrimeiraPessoa
    {
        const float PI = Mathf.PI;
        public Camera cam; Transform vm, vmH, vmC; Pecas.Arma carabina, revolver; Pecas.Cetro cetro;
        GameObject flash; Material mFlash; readonly List<GameObject> runas = new List<GameObject>(); readonly List<Material> mRunas = new List<Material>();
        public Light lanterna, boca;
        // estado (VM e FX do protótipo)
        public float k, cast, sx, sy, mdx, mdy, spr, fov = 72, roll, kickP, spread, giroCetro;
        public float fovBase = 72;
        // v5.8: tempo desde o último tiro (anima o perfil), pausa de impacto, empurrões na câmera e ciclo dos passos
        public float tT = 9, pausa, fovPunch, rollImp, fase, bobK, agK, ix, iz; public int tiros; bool somFeito = true;
        public float camFx = 1;                        // Ajustes, Impacto na câmera: 1 normal, .5 reduzido, 0 desligado
        public System.Action<string> AoSomArma;         // alavanca (carabina) ou cão (revólver), no meio do ciclo
        public System.Action AoPasso;                   // o passo do jogador toca no ponto mais baixo do balanço
        class Perfil { public float ataque, recuoZ, sobe, giro, mola, amort, c0, c1, somT; public string som; public bool capsula; }
        static readonly Perfil CARABINA = new Perfil { ataque = .02f, recuoZ = .075f, sobe = .17f, giro = .04f, mola = 17, amort = .55f, c0 = .11f, c1 = .35f, som = "alavanca", somT = .12f, capsula = true };
        static readonly Perfil REVOLVER = new Perfil { ataque = .018f, recuoZ = .05f, sobe = .44f, giro = .1f, mola = 12, amort = .36f, c0 = .2f, c1 = .38f, som = "cao", somT = .26f };
        static readonly Perfil CETRO = new Perfil { ataque = .05f, recuoZ = -.15f, sobe = -.55f, giro = -.06f, mola = 10, amort = .5f, c0 = .04f, c1 = .3f };
        // mola amortecida: 1 no início, passa um pouco do ponto e assenta
        static float Mola(float t, float w, float z) { return Mathf.Exp(-z * w * t) * Mathf.Cos(w * Mathf.Sqrt(1 - z * z) * t); }
        static float Coice(float t, Perfil P) { return t < 0 ? 0 : t < P.ataque ? t / P.ataque : Mola(t - P.ataque, P.mola, P.amort); }
        static float Ciclo(float t, Perfil P) { return t < P.c0 || t > P.c1 ? 0 : Mathf.Sin(PI * (t - P.c0) / (P.c1 - P.c0)); }
        static float Suave(float a) { return a * a * (3 - 2 * a); }
        // cápsulas de latão ejetadas pela janela da carabina (poucas, reaproveitadas)
        class Capsula { public Transform t; public Vector3 v; public float w, vida; }
        readonly List<Capsula> capsulas = new List<Capsula>(); Material mLatao; Mesh geoCapsula;

        public PrimeiraPessoa(Camera cam)
        {
            this.cam = cam; cam.nearClipPlane = .05f; cam.farClipPlane = 220; cam.fieldOfView = 72;
            vm = Mundo.Grupo(cam.transform, "Primeira pessoa (espelhada)"); vm.localScale = new Vector3(1, 1, -1);
            Material luva = Pecas.Couro(0x2a1c14), manga = Pecas.Pano(0x384858), mangaC = Pecas.Pano(0x581320), peleC = Pecas.Liso(0x2e1c1e, .8f); mangaC.SetFloat("_Cull", 0);
            var k1 = new Kit();
            System.Action<Transform, float, float, float, float> mao = (pai, x, y, z, rx) => k1.Por(pai, luva, Geo.Esfera(.036f, 16, 12), x, y, z, rx, 0, 0, 1, 1.2f, 1.35f);
            System.Action<Transform, float, float, float, float, Material> braco = (pai, x, y, z, rx, mat) => k1.Por(pai, mat, Geo.Cilindro(.042f, .052f, .4f, 16), x, y, z, rx);
            vmH = Mundo.Grupo(vm, "Caçador");
            carabina = Pecas.Carabina(k1, vmH); mao(carabina.g, 0, -.06f, .03f, .4f); braco(carabina.g, .02f, -.13f, .2f, 1.2f, manga); mao(carabina.g, -.01f, -.035f, -.25f, .1f);
            revolver = Pecas.Revolver(k1, vmH); revolver.g.localPosition = new Vector3(-.03f, .075f, -.13f);
            mao(revolver.g, 0, -.07f, .02f, .3f); braco(revolver.g, .02f, -.15f, .2f, 1.15f, manga); mao(revolver.g, -.03f, -.09f, .03f, .2f);
            mFlash = Pecas.Aditivo(0xffd08a, Tex.Chama(), true, 0); flash = Mundo.Sprite(vmH, mFlash, .3f, .3f, "clarão");
            vmH.localPosition = new Vector3(.2f, -.17f, -.42f); vmH.localScale = Vector3.one * .72f;
            vmC = Mundo.Grupo(vm, "Cultista");
            cetro = Pecas.CetroOsso(k1, vmC, 0xb050ff); cetro.g.localPosition = new Vector3(0, -.95f, 0);
            // As runas em órbita são a munição: uma por sigilo disponível.
            for (int i = 0; i < 7; i++) { var m = Pecas.Aditivo(0xe0a0ff, Tex.Runa(), true, .9f); mRunas.Add(m); runas.Add(Mundo.Sprite(cetro.cab, m, .035f, .035f, "runa")); }
            var maoC = Mundo.Grupo(cetro.g, "mão"); maoC.localPosition = new Vector3(0, .62f, 0);
            k1.Por(maoC, peleC, Geo.Esfera(.04f, 16, 12), 0, 0, 0, 0, 0, 0, 1.2f, 1.4f, 1.2f);
            k1.Por(maoC, mangaC, Geo.Cilindro(.06f, .1f, .46f, 18, 1, true), .06f, -.2f, .08f, .5f, 0, -.35f);
            vmC.localPosition = new Vector3(.26f, -.12f, -.5f); vmC.localRotation = Mundo.Q(-.3f, 0, .18f); vmC.localScale = Vector3.one * .6f;
            k1.Fechar(false);
            foreach (var r in vm.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // lanterna (SpotLight do three: 26 m, meio-ângulo 0,42 rad, penumbra 0,55, decaimento 1,6) e clarão da boca
            var lg = new GameObject("Lanterna"); lg.transform.SetParent(cam.transform, false); lg.transform.localPosition = new Vector3(.25f, -.2f, 0); lg.transform.localRotation = Quaternion.LookRotation(new Vector3(-.25f, .2f, 5)); // fora do espelho: eixos do Unity
            lanterna = lg.AddComponent<Light>(); lanterna.type = LightType.Spot; lanterna.color = Mat.Hex(0xfff0d0); lanterna.range = 26; lanterna.spotAngle = .42f * 2 * Mathf.Rad2Deg; lanterna.innerSpotAngle = lanterna.spotAngle * .45f;
            lanterna.intensity = Mundo.IntensidadeUnity(3.2f, 26, 1.6f); lanterna.shadows = Qualidade.nivel >= 2 ? LightShadows.Soft : LightShadows.None; lanterna.enabled = false;
            boca = Mundo.LuzPonto(cam.transform, 0xffc27a, 0, 9, 2, "clarão da arma"); boca.transform.localPosition = new Vector3(.3f, -.2f, .8f); boca.enabled = false;
        }
        float iBoca;
        // ponto de saída do tiro próprio (camera.localToWorld(.27,-.2,-.9)), nas coordenadas do protótipo
        public Vector3 BocaMundo(Transform raizMundo) { return raizMundo.InverseTransformPoint(vm.TransformPoint(new Vector3(.27f, -.2f, -.9f))); }

        public void Tiro(bool exo = false) { iBoca = exo ? 3.6f : 3; Mat.Opacidade(mFlash, 1); k = 1; tT = 0; somFeito = false; tiros++; kickP += (exo ? .03f : .018f) * Mathf.Max(.35f, camFx); spread = Mathf.Min(1, spread + (exo ? .45f : .35f)); }
        public void Sigilo() { cast = 1; tT = 0; somFeito = false; spread = Mathf.Min(1, spread + .2f); }
        // acerto confirmado pelo servidor: o coice segura no auge por um instante e o campo de visão dá um empurrão
        public void Acerto(bool escudo, bool derrubou, bool cabeca) { pausa = escudo ? .02f : derrubou ? .075f : cabeca ? .055f : .04f; fovPunch -= (derrubou ? 1.5f : escudo ? .2f : .55f) * camFx; }
        // golpe sofrido: a câmera inclina para o lado de onde veio (lado = -1 ou 1; 0 sem direção)
        public void Golpe(float lado) { rollImp += lado * .025f * camFx; kickP += Random.Range(-.02f, .02f) * camFx; }
        public void Tranco(float kick, float fovMenos) { kickP += kick * camFx; fovPunch -= fovMenos * camFx; }
        public void Mouse(float dx, float dy) { mdx += dx; mdy += dy; }

        // posiciona a câmera (Unity) e anima a arma; pos em coordenadas do protótipo
        public void Atualizar(Ator m, float x, float z, bool moving, float mx, float mz, bool correndo, float yaw, float pitch, float dt, float agora)
        {
            float s = (float)m.sanity, shake = s < 40 ? (40 - s) / 40 * .012f * Mathf.Min(1, camFx * 2) : 0; bool down = m.st == "down";
            // passos e balanço no mesmo ciclo: dois passos por ciclo (andando 0,45 s, correndo 0,3 s); o passo toca no ponto
            // mais baixo do balanço e o balanço some suave ao parar
            bool andando = moving && m.st == "alive"; bobK = Mathf.Lerp(bobK, andando ? 1 : 0, Mathf.Min(1, dt * 8));
            agK = Mathf.Lerp(agK, m.ag && !down ? 1 : 0, Mathf.Min(1, dt * (m.ag ? 9 : 7))); // v6: agachado (C)
            if (andando) { float f0 = fase; fase += dt * PI / (correndo ? .3f : m.ag ? .6f : .45f); if (Mathf.Floor(f0 / PI + .5f) != Mathf.Floor(fase / PI + .5f) && AoPasso != null) AoPasso(); }
            float bob = -Mathf.Abs(Mathf.Sin(fase)) * (correndo ? .05f : .034f) * bobK * Mathf.Max(.5f, camFx) * (1 - agK * .55f);
            cam.transform.position = new Vector3(x, down ? .45f : Mathf.Lerp(1.65f, 1.0f, agK) + bob, -z);
            fovPunch = Mathf.Lerp(fovPunch, 0, Mathf.Min(1, dt * 10)); rollImp = Mathf.Lerp(rollImp, 0, Mathf.Min(1, dt * 7));
            fov = Mathf.Lerp(fov, fovBase + (correndo ? 7 : 0), .1f); float fovAlvo = fov + fovPunch; if (Mathf.Abs(cam.fieldOfView - fovAlvo) > .05f) cam.fieldOfView = fovAlvo;
            roll = Mathf.Lerp(roll, moving && !down ? -mx * .022f : 0, .1f); kickP = Mathf.Lerp(kickP, 0, .14f);
            float p = pitch + kickP + Random.Range(-shake, shake), y = yaw + Random.Range(-shake, shake);
            cam.transform.rotation = Quaternion.Euler(-p * Mathf.Rad2Deg, -y * Mathf.Rad2Deg, (roll + rollImp) * Mathf.Rad2Deg);
            lanterna.enabled = m.team == "H" && m.lantern && m.st == "alive";
            iBoca = Mathf.Max(0, iBoca - dt * 30); boca.enabled = iBoca > .01f; boca.intensity = Mundo.IntensidadeUnity(iBoca, 9, 2);
            Mat.Opacidade(mFlash, Mathf.Max(0, mFlash.GetColor("_Color").a - dt * 14));
            k = Mathf.Max(0, k - dt * 7); cast = Mathf.Max(0, cast - dt * 4); spread = Mathf.Max(0, spread - dt * 2.5f);
            sx = Mathf.Lerp(sx, Mathf.Clamp(mdx * .0009f, -.05f, .05f), .2f); sy = Mathf.Lerp(sy, Mathf.Clamp(mdy * .0009f, -.05f, .05f), .2f); mdx *= .7f; mdy *= .7f;
            ix = Mathf.Lerp(ix, andando ? mx : 0, Mathf.Min(1, dt * 6)); iz = Mathf.Lerp(iz, andando ? mz : 0, Mathf.Min(1, dt * 5)); // a arma atrasa um pouco ao começar a andar ou mudar de lado
            float bx = Mathf.Cos(fase) * .011f * bobK, by = Mathf.Abs(Mathf.Sin(fase)) * .013f * bobK + (1 - bobK) * Mathf.Sin(agora * 2) * .003f;
            float rl = m.reloadT > 0 ? Mathf.Sin((1 - (float)m.reloadT / 1.6f) * PI) : 0; spr = Mathf.Lerp(spr, correndo ? 1 : 0, .12f);
            bool exo = m.cls == "exorcista", cult = m.team == "C"; var arma = exo ? revolver : carabina; Perfil P = cult ? CETRO : exo ? REVOLVER : CARABINA;
            if (pausa > 0) pausa = Mathf.Max(0, pausa - dt); else tT += dt;    // pausa de impacto: o coice segura no auge
            float kc = Coice(tT, P), cy = Ciclo(tT, P);
            if (!somFeito && tT >= P.somT) { somFeito = true; if (!cult && P.som != null && AoSomArma != null) AoSomArma(P.som); if (!cult && P.capsula) EjetarCapsula(); }
            vmH.localPosition = new Vector3(.2f - sx + bx + spr * .03f - ix * .012f + cy * .008f, -.17f - sy - by - rl * .12f - spr * .05f - cy * .022f, -.42f + kc * P.recuoZ - iz * .008f);
            vmH.localRotation = Mundo.Q(kc * P.sobe + rl * .25f - spr * .25f - iz * .03f, spr * .6f + sx * 2, rl * .9f + kc * P.giro - cy * (exo ? .06f : .2f) - ix * .05f);
            carabina.g.gameObject.SetActive(!exo); revolver.g.gameObject.SetActive(exo);
            // peças que se mexem (o cão já tem a inclinação de repouso na malha: -0,5 na carabina, -0,6 no revólver)
            carabina.alav.localRotation = Mundo.Q(exo ? 0 : cy * .95f, 0, 0);                                     // a alavanca abre e fecha
            carabina.cao.localRotation = Mundo.Q((tT < .03f ? -.15f : -.5f - cy * .35f) + .5f, 0, 0);            // o cão cai no tiro e é armado
            float aT = Mathf.Clamp01((tT - P.c0) / (P.c1 - P.c0)); revolver.tambor.localRotation = Mundo.Q(0, 0, (tiros - 1 + (exo ? Suave(aT) : 1)) * PI / 3);
            revolver.cao.localRotation = Mundo.Q((exo ? (tT < .04f ? -.15f : -.6f - cy * .45f) : -.6f) + .6f, 0, 0);
            flash.transform.localPosition = arma.boca + arma.g.localPosition;
            float kC = cult ? kc : 0;
            vmC.localPosition = new Vector3(.26f - sx + bx + spr * .02f - ix * .012f, -.12f - sy - by - spr * .08f, -.5f + kC * P.recuoZ);
            vmC.localRotation = Mundo.Q(-.3f + kC * P.sobe - spr * .3f, sx * 1.5f, .18f + spr * .25f + kC * P.giro - ix * .05f);
            // as runas em órbita se recolhem na brasa no lançamento e voltam abrindo
            float recolhe = cult ? (tT < .06f ? tT / .06f : Mathf.Max(0, 1 - (tT - .06f) / .22f)) : 0, raioR = .085f * (1 - .8f * recolhe) + .03f * cy;
            int cargas = Mathf.FloorToInt((float)(m.fervor / CFG.sigilCusto)); float giro = agora * 1.5f * (1 + cast * 6);
            for (int i = 0; i < runas.Count; i++) { bool on = i < cargas; runas[i].SetActive(on); float a = giro + i / 7f * PI * 2; runas[i].transform.localPosition = new Vector3(Mathf.Cos(a) * raioR, .1f + Mathf.Sin(a * 2 + i) * .012f, Mathf.Sin(a) * raioR); mRunas[i].SetFloat("_Giro", a * 2); }
            Mat.Opacidade(cetro.mHalo, Mathf.Min(1, .3f + .5f * (float)(m.fervor / 100) + cast * .5f)); cetro.brasa.transform.localScale = Vector3.one * (1 + cast * .5f + recolhe * .6f + Mathf.Sin(agora * 6) * .05f);
            giroCetro += dt * (1.5f + cast * 12); cetro.mRuna.SetFloat("_Giro", giroCetro);
            vmH.gameObject.SetActive(m.team == "H" && m.st == "alive"); vmC.gameObject.SetActive(m.team == "C" && m.st == "alive");
            AtualizarCapsulas(dt);
        }
        // cápsula de latão: sai pela janela para a direita e para cima, gira, quica no chão e some (coordenadas do Unity)
        void EjetarCapsula()
        {
            if (!carabina.g.gameObject.activeInHierarchy) return;
            if (geoCapsula == null) { geoCapsula = Geo.Cilindro(.006f, .006f, .03f, 8).ToMesh("cápsula"); mLatao = Pecas.Metal(0xc8a050, .25f, 1); }
            Capsula c = null; foreach (var o in capsulas) if (o.vida <= 0) { c = o; break; }
            if (c == null) { if (capsulas.Count >= 6) return; var go = new GameObject("cápsula"); go.AddComponent<MeshFilter>().sharedMesh = geoCapsula; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mLatao; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; c = new Capsula { t = go.transform }; capsulas.Add(c); }
            c.t.gameObject.SetActive(true); c.t.position = carabina.g.TransformPoint(carabina.janela); c.t.rotation = cam.transform.rotation;
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
        public void Esconder() { vmH.gameObject.SetActive(false); vmC.gameObject.SetActive(false); lanterna.enabled = false; boca.enabled = false; foreach (var c in capsulas) { c.vida = 0; c.t.gameObject.SetActive(false); } }
    }
}
