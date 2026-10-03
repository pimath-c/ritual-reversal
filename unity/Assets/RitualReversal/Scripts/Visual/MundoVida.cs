// O que o mundo faz a cada quadro: céu e lua presos à câmera, luz da lua e luar, as 12 luzes de vela emprestadas às
// fontes mais próximas (distribuirLuzes), névoa e cor da noite por momento e por zona (ambienteMomento), o Grande
// Ritual (céu e vitrais de sangue, névoa carmim), corvos que voam quando alguém chega, reagentes e sinalizadores.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        Transform cupula; Material mCeu, mSangue, mEstrelas, mLua, mLua2; GameObject sangue; Transform lua, lua2; Light sol, luar;
        readonly List<Light> velas = new List<Light>(); readonly List<Fonte> fonteDaLuz = new List<Fonte>(); float luzT;
        public class FlareV { public Light L; public GameObject s; public Material m; }
        public readonly List<FlareV> flares = new List<FlareV>();
        public float GRk; public bool GRativo; float velaK = 1;
        static readonly Vector3 DIR_LUA = new Vector3(18, 40, 26).normalized;
        Color corNevoa = new Color(.043f, .05f, .078f); float densNevoa = .026f;

        void BuildEfeitos()
        {
            // céu: cúpula presa à câmera, degradê noturno; céu de sangue do Grande Ritual por cima; estrelas
            cupula = Grupo(raiz, "Céu");
            mCeu = Mat.Brilho(Color.white, Tex.Gradiente("ceu", 0f, "#010205", .55f, "#060a14", 1f, "#101a2c"), Mat.Mistura.Alfa, false, false, 0, false, 1000);
            Malha(Geo.Esfera(190, 24, 14), mCeu, cupula, "cúpula", false);
            mSangue = Mat.Brilho(new Color(1, 1, 1, 0), Tex.Gradiente("ceuSangue", 0f, "rgba(90,0,10,0)", .35f, "rgba(150,10,24,.55)", .52f, "rgba(230,40,50,1)", .62f, "rgba(120,6,16,.6)", 1f, "rgba(40,0,4,0)"), Mat.Mistura.Aditiva, false, false, 0, false, 1001);
            sangue = Malha(Geo.Esfera(186, 24, 14), mSangue, cupula, "céu de sangue", false); sangue.SetActive(false);
            mEstrelas = Mat.Brilho(Mat.Hex(0xdfe6ff, .8f), Tex.Brilho(), Mat.Mistura.Aditiva, true, false, 0, false, 1002);
            { var g = new Geo(); var rnd = new System.Random(4); for (int i = 0; i < 1800; i++) { float a = (float)rnd.NextDouble() * 6.283f, el = Mathf.Asin(.12f + (float)rnd.NextDouble() * .88f), r = 180; g.Juntar(Geo.Sprite(new Vector3(Mathf.Cos(a) * Mathf.Cos(el) * r, Mathf.Sin(el) * r, Mathf.Sin(a) * Mathf.Cos(el) * r), .9f, .9f)); } Malha(g, mEstrelas, cupula, "estrelas", false); }
            mLua = Mat.Brilho(Mat.Hex(0xdfe8ff), Tex.Brilho(), Mat.Mistura.Alfa, true, false, 0, false, 1003); lua = Sprite(raiz, mLua, 22, 22, "lua").transform;
            mLua2 = Mat.Brilho(Mat.Hex(0x6f88c8, .35f), Tex.Brilho(), Mat.Mistura.Aditiva, true, false, 0, false, 1004); lua2 = Sprite(raiz, mLua2, 64, 64, "halo da lua").transform;
            // luzes
            luar = LuzPonto(raiz, 0x8fa8e0, .9f, 70, 1.5f, "luar"); luar.transform.localPosition = new Vector3(0, 26, 0);
            foreach (var l in FindObjectsOfType<Light>()) if (l.type == LightType.Directional && l.transform.root != raiz.root) l.enabled = false;
            var sg = new GameObject("Lua (sol)"); sol = sg.AddComponent<Light>(); sol.type = LightType.Directional; sol.color = Mat.Hex(0x8aa2d6); sol.intensity = .42f;
            sol.shadows = Qualidade.nivel >= 2 ? LightShadows.Soft : LightShadows.None; sol.shadowBias = .05f; sol.shadowNormalBias = .4f;
            sol.transform.rotation = Quaternion.LookRotation(Unity(-DIR_LUA));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Mat.Lin(new Color(0x33 / 255f, 0x47 / 255f, 0x6e / 255f) * .5f);
            RenderSettings.ambientEquatorColor = Mat.Lin(new Color((0x33 + 0x10) / 510f, (0x47 + 0x0a) / 510f, (0x6e + 0x08) / 510f) * .5f);
            RenderSettings.ambientGroundColor = Mat.Lin(new Color(0x10 / 255f, 0x0a / 255f, 0x08 / 255f) * .5f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = densNevoa; RenderSettings.fogColor = corNevoa;
            int nLuzes = Qualidade.nivel >= 3 ? 12 : Qualidade.nivel == 2 ? 8 : Qualidade.nivel == 1 ? 5 : 3;
            for (int i = 0; i < nLuzes; i++) { var L = LuzPonto(raiz, 0xff8a36, 0, 14, 2, "vela " + i); L.enabled = false; velas.Add(L); fonteDaLuz.Add(null); }
            var mf = Mat.Brilho(Mat.Hex(0xffe0a8), Tex.Brilho(), Mat.Mistura.Aditiva, true);
            for (int i = 0; i < 6; i++) { var L = LuzPonto(raiz, 0xffd9a0, 0, 16, 2, "sinalizador " + i); L.enabled = false; var m = new Material(mf); var s = Sprite(raiz, m, 1.3f, 1.3f, "sinalizador"); s.SetActive(false); flares.Add(new FlareV { L = L, s = s, m = m }); }
        }
        // direção do protótipo -> direção no Unity (z invertido)
        static Vector3 Unity(Vector3 v) { return new Vector3(v.x, v.y, -v.z); }

        static readonly float[] AMB_NEVOA = { .019f, .026f, .036f }, AMB_VELA = { 1.1f, 1, .8f };
        static readonly int[] AMB_COR = { 0x16110f, 0x0b0d14, 0x15070a };

        // cam: posição da câmera nas coordenadas do protótipo
        public void Atualizar(Jogo g, string papel, Ator eu, Vector3 cam, float lookYaw, float dt)
        {
            float agora = Time.time;
            // céu e lua acompanham a câmera
            cupula.localPosition = cam;
            float dl = 150; lua.localPosition = cam + DIR_LUA * dl; lua2.localPosition = lua.localPosition;
            // luzes emprestadas às fontes mais próximas
            luzT -= dt;
            if (luzT <= 0)
            {
                luzT = .25f; var ord = new List<KeyValuePair<float, Fonte>>(); foreach (var f in FONTES) ord.Add(new KeyValuePair<float, Fonte>(Mathf.Sqrt((f.x - cam.x) * (f.x - cam.x) + (f.z - cam.z) * (f.z - cam.z)), f));
                ord.Sort((a, b) => a.Key.CompareTo(b.Key));
                for (int k = 0; k < velas.Count; k++) { var o = k < ord.Count && ord[k].Key < 60 ? ord[k].Value : null; fonteDaLuz[k] = o; if (o != null) { velas[k].transform.localPosition = new Vector3(o.x, o.y, o.z); velas[k].color = Mat.Hex(o.cor); velas[k].range = o.alc; } }
            }
            for (int i = 0; i < velas.Count; i++)
            {
                var f = fonteDaLuz[i]; if (f == null) { velas[i].enabled = false; continue; }
                float I = (f.i + Mathf.Sin(agora * 11 + i * 3) * .12f + Random.value * (.12f + GRk * .5f)) * velaK * (1 + GRk * .35f);
                velas[i].enabled = true; velas[i].intensity = IntensidadeUnity(I, f.alc, 2);
            }
            if (g == null) return;
            // névoa e cor da noite
            int mi = Mathf.Clamp(g.momento, 0, 2); bool mata = Mapa.ZoneAt(cam.x, cam.z) == "Floresta"; float k2 = Mathf.Min(1, dt * .6f);
            densNevoa = Mathf.Lerp(densNevoa, AMB_NEVOA[mi] * (mata ? 1.45f : 1), k2);
            var alvo = Color.Lerp(Color.Lerp(Mat.Hex(AMB_COR[mi]), Mat.Hex(0x10140f), mata ? .6f : 0), Mat.Hex(0x2a0508), GRk * .75f);
            corNevoa = Color.Lerp(corNevoa, alvo, k2); velaK = Mathf.Lerp(velaK, AMB_VELA[mi], k2);
            RenderSettings.fogDensity = densNevoa; RenderSettings.fogColor = corNevoa;
            // Grande Ritual
            Altar GA = null; if (g.phase == "play") foreach (var A in g.altars) if (A.state == "active" && A.grande) GA = A;
            GRativo = GA != null; GRk = Mathf.Clamp01(GRk + (GRativo ? dt / 1.5f : -dt / 4)); float kk = GRk, t = agora;
            Mat.Cor(mCeu, Mat.Lin(new Color(1 + 1.6f * kk, 1 - .45f * kk, 1 - .4f * kk)));
            sangue.SetActive(kk > .01f); Mat.Cor(mSangue, new Color(1, 1, 1, kk * (.8f + .2f * Mathf.Sin(t * .9f))));
            Mat.Cor(mEstrelas, Mat.Hex(0xdfe6ff, .8f * (1 - .75f * kk)));
            Mat.Cor(mLua, Color.Lerp(Mat.Hex(0xdfe8ff), Mat.Hex(0xff3a2a), kk)); Mat.Cor(mLua2, Mat.Lin(new Color(.44f + .5f * kk, .53f - .35f * kk, .78f - .6f * kk, .35f)));
            float pul = .75f + .25f * Mathf.Sin(t * 2.1f); var sangueV = Mat.Lin(new Color(2.6f, .32f, .3f));
            if (winMat != null) { Mat.Cor(winMat, Color.Lerp(winCor, sangueV, kk * pul)); Mat.Cor(roseMat, Color.Lerp(roseCor, sangueV, kk * pul)); Mat.Cor(winFora, Color.Lerp(winForaCor, sangueV, kk * pul)); }
            foreach (var m in pocas) Mat.Opacidade(m, .2f + .4f * kk * pul);
            // reagentes e sinalizadores
            for (int i = 0; i < g.reagents.Count && i < reagentes.Count; i++) { bool tem = g.reagents[i].has; reagentes[i].jar.SetActive(tem); reagentes[i].halo.SetActive(tem); }
            for (int i = 0; i < g.flares.Count && i < flares.Count; i++)
            {
                var f = g.flares[i]; var o = flares[i];
                if (f.t > 0) { o.L.enabled = true; o.L.transform.localPosition = new Vector3((float)f.x, 1.2f, (float)f.z); o.L.intensity = IntensidadeUnity((float)(f.t > 1 ? 2.6 : 2.6 * f.t) * (.9f + Random.value * .2f), 16, 2); o.s.SetActive(true); o.s.transform.localPosition = new Vector3((float)f.x, .3f, (float)f.z); Mat.Opacidade(o.m, Mathf.Min(1, (float)f.t)); }
                else { o.L.enabled = false; o.s.SetActive(false); }
            }
            if (g.phase == "play") SyncAltars(g, papel, eu, cam, lookYaw, dt);
            AtualizarCorvos(g, cam, dt);
        }

        public void EspantarCorvos() { foreach (var C in CORVOS) if (C.estado == "pousado") { float a = Random.value * 6.283f; C.vx = Mathf.Cos(a) * 6; C.vz = Mathf.Sin(a) * 6; C.vy = 4 + Random.value * 2; C.estado = "voando"; C.t = Random.value * .4f; } }
        public System.Action<float, float> AoGritarCorvo;
        void AtualizarCorvos(Jogo g, Vector3 cam, float dt)
        {
            float agora = Time.time * 1000; var ameacas = new List<Vector2>();
            if (g.phase == "play") { ameacas.Add(new Vector2(cam.x, cam.z)); foreach (var a in g.actors) if (a.st == "alive") ameacas.Add(new Vector2((float)a.x, (float)a.z)); }
            Vector2? grito = null;
            for (int i = 0; i < CORVOS.Count; i++)
            {
                var C = CORVOS[i]; C.t += dt;
                if (C.estado == "pousado")
                {
                    Vector2? perigo = null; float pd = 1e9f; foreach (var q in ameacas) { float d = Vector2.Distance(q, new Vector2(C.x, C.z)); if (d < 6.5f && d < pd) { pd = d; perigo = q; } }
                    if (perigo.HasValue) { float dx = C.x - perigo.Value.x, dz = C.z - perigo.Value.y, a = Mathf.Atan2(dz, dx) + (Random.value - .5f) * 1.2f; C.vx = Mathf.Cos(a) * 5.5f; C.vz = Mathf.Sin(a) * 5.5f; C.vy = 3.5f + Random.value * 1.5f; C.estado = "voando"; C.t = 0; if (Vector2.Distance(new Vector2(cam.x, cam.z), new Vector2(C.x, C.z)) < 25) grito = new Vector2(C.x, C.z); }
                    else if (C.t > 1.5f + ((i * 7) % 5)) { C.t = 0; C.yaw += (Random.value - .5f) * 1.6f; C.bico = .35f; }
                }
                else if (C.estado == "voando") { C.x += C.vx * dt; C.y += C.vy * dt; C.z += C.vz * dt; C.vy += .4f * dt; C.yaw = Mathf.Atan2(-C.vx, -C.vz); if (C.t > 4) { C.estado = "sumido"; C.volta = agora + 30000 + Random.value * 30000; } }
                else if (C.estado == "sumido" && agora > C.volta && !GRativo) { bool perto = false; foreach (var q in ameacas) if (Vector2.Distance(q, new Vector2(C.px, C.pz)) < 16) perto = true; if (!perto) { C.estado = "pousado"; C.x = C.px; C.y = C.py; C.z = C.pz; C.t = 0; } }
                C.bico = Mathf.Max(0, C.bico - dt * 1.5f);
                bool vis = C.estado != "sumido", voa = C.estado == "voando"; float bate = voa ? Mathf.Sin(C.t * 26) * .9f : -.05f;
                C.corpo.gameObject.SetActive(vis); if (!vis) continue;
                C.corpo.localPosition = new Vector3(C.x, C.y, C.z); C.corpo.localRotation = Q(voa ? -.25f : (C.bico > 0 ? Mathf.Sin(C.bico * 9) * .3f : 0), C.yaw, 0); C.corpo.localScale = Vector3.one * 1.3f;
                C.asaD.localRotation = Q(0, 0, voa ? bate : -.12f); C.asaD.localScale = new Vector3(1, 1, voa ? 1 : .7f);
                C.asaE.localRotation = Q(0, 0, -(voa ? bate : -.12f)); C.asaE.localScale = new Vector3(-1, 1, voa ? 1 : .7f);
            }
            if (grito.HasValue && AoGritarCorvo != null) AoGritarCorvo(grito.Value.x, grito.Value.y);
        }
    }
}
