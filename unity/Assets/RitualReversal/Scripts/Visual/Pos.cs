// Pós-processamento do protótipo num Volume global do URP: bloom (UnrealBloomPass 0,85 / 0,55 / 0,72), ACES com
// exposição 1,25, sombras puxadas para o azul-frio e luzes para o âmbar das velas, saturação e contraste da etapa
// final, vinheta oval, granulado de filme e aberração cromática que cresce quando a sanidade cai.
// A Grande Ritual tinge a imagem de carmim; a sanidade baixa tira a cor e aperta a vinheta.
// Etapa GRADE (contorno a nanquim e névoa rasteira, Resources/RitualReversalGrade.shader): um passe de tela cheia do URP
// (FullScreenPassRendererFeature) posto no renderizador do projeto quando a partida começa e tirado quando ela acaba.
// Tudo por reflexão: se o URP do projeto não tiver o recurso, a etapa só não aparece (aviso no console).
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RitualReversal.Visual
{
    public class Pos
    {
        Volume vol; Bloom bloom; ColorAdjustments cor; Vignette vinheta; ChromaticAberration aberr; FilmGrain grao; SplitToning tons;
        Material grade; ScriptableObject recurso, dadosR; System.Collections.IList listaR; float densG = .024f; Vector4 corG = COR_NEVOA;
        static readonly Vector4 COR_NEVOA = new Vector4(.075f, .086f, .09f, 1), COR_NEVOA_HM = new Vector4(.085f, .062f, .068f, 1), COR_NEVOA_GR = new Vector4(.13f, .035f, .045f, 1);
        static readonly float[] DENS_GRADE = { .024f, .03f, .04f };

        public Pos(Camera cam)
        {
            var dados = cam.GetUniversalAdditionalCameraData(); if (dados != null) { dados.renderPostProcessing = true; dados.antialiasing = Qualidade.nivel >= 2 ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.FastApproximateAntialiasing; }
            var go = new GameObject("Pós-processamento"); vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10;
            var p = ScriptableObject.CreateInstance<VolumeProfile>(); vol.profile = p;
            bloom = p.Add<Bloom>(true); bloom.intensity.Override(.85f); bloom.threshold.Override(.72f); bloom.scatter.Override(.55f);
            var tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            cor = p.Add<ColorAdjustments>(true); cor.postExposure.Override(Mathf.Log(1.25f, 2)); cor.saturation.Override(22); cor.contrast.Override(10);
            tons = p.Add<SplitToning>(true); tons.shadows.Override(new Color(.42f, .45f, .62f)); tons.highlights.Override(new Color(.66f, .55f, .4f)); tons.balance.Override(-10);
            vinheta = p.Add<Vignette>(true); vinheta.intensity.Override(.42f); vinheta.smoothness.Override(.55f); vinheta.rounded.Override(false); vinheta.color.Override(Color.black);
            aberr = p.Add<ChromaticAberration>(true); aberr.intensity.Override(.08f);
            grao = p.Add<FilmGrain>(true); grao.type.Override(FilmGrainLookup.Medium1); grao.intensity.Override(Qualidade.nivel >= 2 ? .3f : 0); grao.response.Override(.8f);
            bloom.active = Qualidade.nivel >= 1;
            if (Qualidade.nivel >= 1) CriarGrade();   // no mínimo, nada de pós-processamento (como no navegador)
        }

        static void Def(object o, string campo, object v) { var f = o.GetType().GetField(campo, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); if (f != null) f.SetValue(o, v); }
        void CriarGrade()
        {
            try
            {
                var sh = Shader.Find("RitualReversal/Grade"); if (sh == null || !sh.isSupported) return;
                var asset = GraphicsSettings.currentRenderPipeline; if (asset == null) return;
                const BindingFlags fl = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var lista = asset.GetType().GetField("m_RendererDataList", fl) != null ? asset.GetType().GetField("m_RendererDataList", fl).GetValue(asset) as Array : null;
                if (lista == null || lista.Length == 0) { Debug.LogWarning("Ritual Reversal: sem a etapa GRADE (o URP não mostrou a lista de renderizadores)"); return; }
                int idx = 0; var fi = asset.GetType().GetField("m_DefaultRendererIndex", fl); if (fi != null) idx = (int)fi.GetValue(asset);
                var dados = lista.GetValue(Mathf.Clamp(idx, 0, lista.Length - 1)) as ScriptableObject; if (dados == null) return;
                var tipo = Type.GetType("UnityEngine.Rendering.Universal.FullScreenPassRendererFeature, Unity.RenderPipelines.Universal.Runtime");
                var pr = dados.GetType().GetProperty("rendererFeatures", fl); var lr = pr != null ? pr.GetValue(dados, null) as System.Collections.IList : null;
                if (tipo == null || lr == null) { Debug.LogWarning("Ritual Reversal: sem a etapa GRADE (este URP não tem o FullScreenPassRendererFeature)"); return; }
                grade = new Material(sh) { name = "GRADE" };
                var rec = ScriptableObject.CreateInstance(tipo); rec.name = "Ritual Reversal: GRADE";
                Def(rec, "passMaterial", grade); Def(rec, "passIndex", 0); Def(rec, "fetchColorBuffer", true); Def(rec, "bindDepthStencilAttachment", false);
                var ip = tipo.GetField("injectionPoint", fl); if (ip != null) ip.SetValue(rec, Enum.Parse(ip.FieldType, "AfterRenderingPostProcessing"));
                var rq = tipo.GetField("requirements", fl); if (rq != null) rq.SetValue(rec, Enum.Parse(rq.FieldType, "Depth"));
                lr.Add(rec); var sd = dados.GetType().GetMethod("SetDirty", fl, null, Type.EmptyTypes, null); if (sd != null) sd.Invoke(dados, null);
                recurso = rec; dadosR = dados; listaR = lr;
            }
            catch (Exception e) { Debug.LogWarning("Ritual Reversal: sem a etapa GRADE (" + e.Message + ")"); grade = null; }
        }
        // tira o passe do renderizador (o recurso só existe nesta partida; sem isto o asset do URP ficaria com uma referência perdida)
        public void Destruir()
        {
            try
            {
                if (listaR != null && recurso != null) { listaR.Remove(recurso); var sd = dadosR.GetType().GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null); if (sd != null) sd.Invoke(dadosR, null); }
                if (recurso != null) UnityEngine.Object.Destroy(recurso); if (grade != null) UnityEngine.Object.Destroy(grade);
            }
            catch (Exception) { }
            recurso = null; listaR = null; grade = null;
        }
        // a câmera e a noite: névoa que adensa com o momento (e com o clima), cor carmim no Grande Ritual
        public void Grade(Camera cam, int momento, float grande, float nevoaClima, float dt, float agora)
        {
            if (grade == null) return; int mi = Mathf.Clamp(momento, 0, 2); float k = Mathf.Min(1, dt * .6f);
            densG = Mathf.Lerp(densG, DENS_GRADE[mi] * (1 + (nevoaClima - 1) * 1.6f), k); corG = Vector4.Lerp(corG, grande > .05f ? COR_NEVOA_GR : mi >= 2 ? COR_NEVOA_HM : COR_NEVOA, k);
            float tv = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad / 2);
            // cores da etapa final são de tela (sRGB): SetVector, sem a conversão de SetColor
            grade.SetVector("_Tinta", new Vector4(12 / 255f, 6 / 255f, 16 / 255f, 1)); grade.SetVector("_NevoaCor", corG); grade.SetFloat("_NevoaDens", densG);
            grade.SetVector("_Cat", new Vector4(-96, -58, 96, 58)); grade.SetVector("_Tan", new Vector4(tv * cam.aspect, tv, 0, 0)); grade.SetMatrix("_CamMundo", cam.cameraToWorldMatrix);
            grade.SetFloat("_T", agora); grade.SetFloat("_Longe", cam.farClipPlane); grade.SetFloat("_Contorno", 1);
        }

        // sanidade 0..100, grande 0..1 (céu de sangue), ferido 0..1 (tomou dano agora)
        public float brilho = 1;   // Ajustes: a exposição é 1,25 × brilho, como o toneMappingExposure do navegador
        public void Atualizar(float sanidade, float grande, float ferido, float cego)
        {
            float k = Mathf.Clamp01((70 - sanidade) / 70);
            aberr.intensity.Override(Mathf.Clamp01(.08f + Mathf.Max(0, (45 - sanidade) / 45) * .55f));
            cor.saturation.Override(22 - k * 55 - grande * 10); cor.contrast.Override(10 + k * 25);
            cor.colorFilter.Override(Color.Lerp(Color.white, new Color(1f, .62f, .6f), grande * .6f + ferido * .5f));
            cor.postExposure.Override(Mathf.Log(1.25f * Mathf.Max(.1f, brilho), 2) + cego * 3);
            vinheta.intensity.Override(Mathf.Clamp01(.42f + k * .25f + grande * .15f + ferido * .2f));
            vinheta.color.Override(Color.Lerp(Color.black, new Color(.35f, 0, .04f), Mathf.Max(grande * .8f, ferido)));
        }
    }
}
