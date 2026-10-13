// Pós-processamento do protótipo num Volume global do URP: bloom (UnrealBloomPass 0,85 / 0,55 / 0,72), ACES com
// exposição 1,25, sombras puxadas para o azul-frio e luzes para o âmbar das velas, saturação e contraste da etapa
// final, vinheta oval, granulado de filme e aberração cromática que cresce quando a sanidade cai.
// A Grande Ritual tinge a imagem de carmim; a sanidade baixa tira a cor e aperta a vinheta.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RitualReversal.Visual
{
    public class Pos
    {
        Volume vol; Bloom bloom; ColorAdjustments cor; Vignette vinheta; ChromaticAberration aberr; FilmGrain grao; SplitToning tons;

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
        }

        // sanidade 0..100, grande 0..1 (céu de sangue), ferido 0..1 (tomou dano agora)
        public void Atualizar(float sanidade, float grande, float ferido, float cego)
        {
            float k = Mathf.Clamp01((70 - sanidade) / 70);
            aberr.intensity.Override(Mathf.Clamp01(.08f + Mathf.Max(0, (45 - sanidade) / 45) * .55f));
            cor.saturation.Override(22 - k * 55 - grande * 10); cor.contrast.Override(10 + k * 25);
            cor.colorFilter.Override(Color.Lerp(Color.white, new Color(1f, .62f, .6f), grande * .6f + ferido * .5f));
            cor.postExposure.Override(Mathf.Log(1.25f, 2) + cego * 3);
            vinheta.intensity.Override(Mathf.Clamp01(.42f + k * .25f + grande * .15f + ferido * .2f));
            vinheta.color.Override(Color.Lerp(Color.black, new Color(.35f, 0, .04f), Mathf.Max(grande * .8f, ferido)));
        }
    }
}
