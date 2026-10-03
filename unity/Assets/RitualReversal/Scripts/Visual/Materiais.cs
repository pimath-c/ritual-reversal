// Materiais do protótipo em URP. MeshStandardMaterial vira URP/Lit (cor, textura, relevo, rugosidade, metal, brilho
// próprio, dupla face, recorte de alfa); os materiais aditivos e sem luz (sprites, pontos, feixes, céu) usam o shader
// RitualReversal/Brilho. As cores do protótipo vêm em hexadecimal e o three as trata como lineares: aqui também.
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal.Visual
{
    public static class Mat
    {
        static Shader lit, brilho;
        public static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        public static Shader Lit { get { if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"); return lit; } }
        public static Shader ShaderBrilho
        {
            get
            {
                if (brilho == null) brilho = Shader.Find("RitualReversal/Brilho");
                if (brilho == null) brilho = Shader.Find("Universal Render Pipeline/Unlit");
                return brilho;
            }
        }
        // cor do protótipo (0xRRGGBB, linear) -> cor para o material (o Unity converte de sRGB para linear)
        public static Color Hex(int hex, float a = 1) { var c = new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, a); return Lin(c); }
        public static Color Lin(Color c) { return QualitySettings.activeColorSpace == ColorSpace.Linear ? new Color(c.r, c.g, c.b, c.a).gamma : c; }

        public class Opcoes
        {
            public Color cor = Color.white; public Texture2D mapa, relevo; public float relevoK = 1; public Vector2 repetir = Vector2.one;
            public float rugosidade = 1, metal = 0; public Color emissao = Color.black; public float emissaoK = 1;
            public bool duplaFace, recorte; public float corte = .5f; public bool transparente; public float opacidade = 1;
        }
        public static Material Padrao(Opcoes o)
        {
            var m = new Material(Lit) { enableInstancing = true };
            var c = o.cor; if (o.transparente) c.a = o.opacidade;
            m.SetColor("_BaseColor", c); if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (o.mapa != null) { m.SetTexture("_BaseMap", o.mapa); m.SetTextureScale("_BaseMap", o.repetir); if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", o.mapa); m.SetTextureScale("_MainTex", o.repetir); } }
            if (o.relevo != null && Qualidade.nivel >= 2) { m.SetTexture("_BumpMap", o.relevo); m.SetTextureScale("_BumpMap", o.repetir); m.SetFloat("_BumpScale", o.relevoK); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Smoothness", Mathf.Clamp01(1 - o.rugosidade) * .9f); m.SetFloat("_Metallic", o.metal);
            if (o.emissao.maxColorComponent > 0) { m.SetColor("_EmissionColor", o.emissao * o.emissaoK); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            if (o.duplaFace) m.SetFloat("_Cull", 0);
            if (o.recorte) { m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", o.corte); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; }
            if (o.transparente)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent");
            }
            return m;
        }
        public static Material Padrao(int hex, Texture2D mapa = null, Texture2D relevo = null, float rug = 1, float metal = 0)
        { return Padrao(new Opcoes { cor = Hex(hex), mapa = mapa, relevo = relevo, rugosidade = rug, metal = metal }); }
        public static Material Pbr(Pbr p, int hex = 0xffffff, float rug = 1, Vector2? rep = null, float relevoK = 1)
        { return Padrao(new Opcoes { cor = Hex(hex), mapa = p.mapa, relevo = p.relevo, rugosidade = rug, repetir = rep ?? Vector2.one, relevoK = relevoK }); }

        public enum Mistura { Aditiva, Alfa }
        // material sem luz para brilhos; billboard = sprite/pontos virados para a câmera
        public static Material Brilho(Color cor, Texture2D tex = null, Mistura mis = Mistura.Aditiva, bool billboard = false, bool nevoa = true, float corte = 0, bool zwrite = false, int fila = 3000)
        {
            var m = new Material(ShaderBrilho) { renderQueue = fila };
            if (m.HasProperty("_Color")) m.SetColor("_Color", cor); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", cor);
            if (tex != null) { m.SetTexture("_MainTex", tex); if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex); }
            if (m.HasProperty("_SrcBlend")) { m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(mis == Mistura.Aditiva ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)); }
            if (m.HasProperty("_Billboard")) m.SetFloat("_Billboard", billboard ? 1 : 0);
            if (m.HasProperty("_Nevoa")) m.SetFloat("_Nevoa", nevoa ? 1 : 0);
            if (m.HasProperty("_Corte")) m.SetFloat("_Corte", corte);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", zwrite ? 1 : 0);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0);
            return m;
        }
        // URP/Lit opaco <-> transparente em tempo de execução (dissolução dos mortos)
        public static void Transparencia(Material m, bool on, float a)
        {
            var c = m.GetColor("_BaseColor"); c.a = on ? a : 1; m.SetColor("_BaseColor", c);
            if (on == (m.renderQueue >= 3000)) return;
            m.SetFloat("_Surface", on ? 1 : 0);
            m.SetFloat("_SrcBlend", (float)(on ? UnityEngine.Rendering.BlendMode.SrcAlpha : UnityEngine.Rendering.BlendMode.One));
            m.SetFloat("_DstBlend", (float)(on ? UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : UnityEngine.Rendering.BlendMode.Zero));
            m.SetFloat("_ZWrite", on && a <= .5f ? 0 : 1);
            if (on) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); else m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", on ? "Transparent" : "Opaque"); m.renderQueue = on ? 3000 : -1;
        }
        public static void Cor(Material m, Color c) { if (m.HasProperty("_Color")) m.SetColor("_Color", c); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); }
        public static void Opacidade(Material m, float a) { var c = m.HasProperty("_Color") ? m.GetColor("_Color") : m.GetColor("_BaseColor"); c.a = a; Cor(m, c); }
    }
}
