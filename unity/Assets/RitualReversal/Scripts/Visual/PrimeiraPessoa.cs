// Primeira pessoa (buildViewmodels + a parte de câmera do tick do protótipo): carabina ou revólver com mãos de luva
// para o Caçador, cetro de vértebras com as runas em órbita (uma por sigilo disponível) para o Cultista; balanço ao
// andar, arrasto do mouse, coice, recarga, corrida; lanterna presa à câmera e clarão da boca da arma; campo de visão
// que abre na corrida, câmera que inclina ao andar de lado e treme com a sanidade baixa.
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

        public void Tiro() { iBoca = 3; Mat.Opacidade(mFlash, 1); k = 1; kickP += .018f; spread = Mathf.Min(1, spread + .35f); }
        public void Sigilo() { cast = 1; spread = Mathf.Min(1, spread + .2f); }
        public void Mouse(float dx, float dy) { mdx += dx; mdy += dy; }

        // posiciona a câmera (Unity) e anima a arma; pos em coordenadas do protótipo
        public void Atualizar(Ator m, float x, float z, bool moving, float mx, bool correndo, float yaw, float pitch, float dt, float agora)
        {
            float s = (float)m.sanity, shake = s < 40 ? (40 - s) / 40 * .012f : 0; bool down = m.st == "down";
            float bob = moving ? Mathf.Sin(agora * 11) * .045f : 0;
            cam.transform.position = new Vector3(x, down ? .45f : 1.65f + bob, -z);
            fov = Mathf.Lerp(fov, fovBase + (correndo ? 7 : 0), .1f); if (Mathf.Abs(cam.fieldOfView - fov) > .05f) cam.fieldOfView = fov;
            roll = Mathf.Lerp(roll, moving && !down ? -mx * .022f : 0, .1f); kickP = Mathf.Lerp(kickP, 0, .14f);
            float p = pitch + kickP + Random.Range(-shake, shake), y = yaw + Random.Range(-shake, shake);
            cam.transform.rotation = Quaternion.Euler(-p * Mathf.Rad2Deg, -y * Mathf.Rad2Deg, roll * Mathf.Rad2Deg);
            lanterna.enabled = m.team == "H" && m.lantern && m.st == "alive";
            iBoca = Mathf.Max(0, iBoca - dt * 30); boca.enabled = iBoca > .01f; boca.intensity = Mundo.IntensidadeUnity(iBoca, 9, 2);
            Mat.Opacidade(mFlash, Mathf.Max(0, mFlash.GetColor("_Color").a - dt * 14));
            k = Mathf.Max(0, k - dt * 7); cast = Mathf.Max(0, cast - dt * 4); spread = Mathf.Max(0, spread - dt * 2.5f);
            sx = Mathf.Lerp(sx, Mathf.Clamp(mdx * .0009f, -.05f, .05f), .2f); sy = Mathf.Lerp(sy, Mathf.Clamp(mdy * .0009f, -.05f, .05f), .2f); mdx *= .7f; mdy *= .7f;
            float bt = agora * 10.5f, bx = moving ? Mathf.Cos(bt * .5f) * .012f : 0, by = moving ? Mathf.Abs(Mathf.Sin(bt * .5f)) * .014f : Mathf.Sin(agora * 2) * .003f;
            float rl = m.reloadT > 0 ? Mathf.Sin((1 - (float)m.reloadT / 1.6f) * PI) : 0; spr = Mathf.Lerp(spr, correndo ? 1 : 0, .12f);
            vmH.localPosition = new Vector3(.2f - sx + bx + spr * .03f, -.17f - sy - by - rl * .12f - spr * .05f, -.42f + k * .06f);
            vmH.localRotation = Mundo.Q(k * .22f + rl * .25f - spr * .25f, spr * .6f + sx * 2, rl * .9f);
            bool exo = m.cls == "exorcista"; var arma = exo ? revolver : carabina; carabina.g.gameObject.SetActive(!exo); revolver.g.gameObject.SetActive(exo);
            flash.transform.localPosition = arma.boca + arma.g.localPosition;
            vmC.localPosition = new Vector3(.26f - sx + bx + spr * .02f, -.12f - sy - by - spr * .08f, -.5f - cast * .14f); vmC.localRotation = Mundo.Q(-.3f - cast * .55f - spr * .3f, sx * 1.5f, .18f + spr * .25f);
            int cargas = Mathf.FloorToInt((float)(m.fervor / CFG.sigilCusto)); float giro = agora * 1.5f * (1 + cast * 6);
            for (int i = 0; i < runas.Count; i++) { bool on = i < cargas; runas[i].SetActive(on); float a = giro + i / 7f * PI * 2; runas[i].transform.localPosition = new Vector3(Mathf.Cos(a) * .085f, .1f + Mathf.Sin(a * 2 + i) * .012f, Mathf.Sin(a) * .085f); mRunas[i].SetFloat("_Giro", a * 2); }
            Mat.Opacidade(cetro.mHalo, Mathf.Min(1, .3f + .5f * (float)(m.fervor / 100) + cast * .5f)); cetro.brasa.transform.localScale = Vector3.one * (1 + cast * .5f + Mathf.Sin(agora * 6) * .05f);
            giroCetro += dt * (1.5f + cast * 12); cetro.mRuna.SetFloat("_Giro", giroCetro);
            vmH.gameObject.SetActive(m.team == "H" && m.st == "alive"); vmC.gameObject.SetActive(m.team == "C" && m.st == "alive");
        }
        public void Esconder() { vmH.gameObject.SetActive(false); vmC.gameObject.SetActive(false); lanterna.enabled = false; boca.enabled = false; }
    }
}
