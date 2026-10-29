// Estilo ilustrado do Ritual Reversal (URP): o MeshStandardMaterial do navegador com a luz em faixas (index.html,
// estiloIlustrado). Mundo, personagens e peças usam este shader no lugar do URP/Lit (Mat.Padrao); as propriedades são as
// mesmas do Lit (_BaseMap, _BaseColor, _BumpMap, _Smoothness, _Metallic, _EmissionColor, _Cull, _Cutoff, detalhe x2,
// transparência), para nada mais precisar mudar.
//  • luz como no three (r128, luzes "não físicas"): difusa de Lambert e especular GGX (Schlick, Smith correlacionado), luz
//    ambiente pela SH do URP; sem reflexo de ambiente (o navegador não tem envMap);
//  • a difusa muda em faixas (0,035 / 0,11 / 0,26 / 0,52 / 0,95 da luz), o especular vira um reflexo recortado;
//  • _Rim (rgb): luz de recorte nas silhuetas (personagens: âmbar na caça, carmim no culto; violeta nos mercadores);
//  • relevo pelas derivadas de tela, como o perturbNormal2Arb do three (as malhas não têm tangentes).
Shader "RitualReversal/Ilustrado"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Cor", Color) = (1,1,1,1)
        _BumpMap ("Relevo", 2D) = "bump" {}
        _BumpScale ("Força do relevo", Float) = 1
        _Smoothness ("Lisura", Range(0,1)) = 0
        _Metallic ("Metal", Range(0,1)) = 0
        [HDR] _EmissionColor ("Emissão", Color) = (0,0,0,1)
        _Cutoff ("Recorte", Range(0,1)) = .5
        _DetailAlbedoMap ("Detalhe", 2D) = "grey" {}
        _DetailAlbedoMapScale ("Força do detalhe", Float) = 1
        _DetailNormalMap ("Relevo do detalhe", 2D) = "bump" {}
        _DetailNormalMapScale ("Força do relevo do detalhe", Float) = 1
        _Rim ("Luz de recorte", Color) = (0,0,0,0)
        _NevoaK ("Fração da névoa", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Faces", Float) = 2
        [HideInInspector] _Surface ("Superfície", Float) = 0
        [HideInInspector] _SrcBlend ("Origem", Float) = 1
        [HideInInspector] _DstBlend ("Destino", Float) = 0
        [HideInInspector] _ZWrite ("Profundidade", Float) = 1
        [HideInInspector] _AlphaClip ("Recorte ligado", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _DETAIL_MULX2
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RitualReversalLuz.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_DetailAlbedoMap); SAMPLER(sampler_DetailAlbedoMap);
            TEXTURE2D(_DetailNormalMap); SAMPLER(sampler_DetailNormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _DetailAlbedoMap_ST; half4 _BaseColor; half4 _EmissionColor; half4 _Rim;
                half _Cutoff, _Smoothness, _Metallic, _BumpScale, _DetailAlbedoMapScale, _DetailNormalMapScale, _Cull, _Surface, _NevoaK;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 uvD : TEXCOORD1; float3 positionWS : TEXCOORD2; float3 normalWS : TEXCOORD3; float fog : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap); o.uvD = TRANSFORM_TEX(v.uv, _DetailAlbedoMap); o.fog = ComputeFogFactor(p.positionCS.z) * sqrt(_NevoaK);   // marcos (torres, telhados): 0,07 da névoa
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv); half3 cor = tex.rgb * _BaseColor.rgb; half alfa = tex.a * _BaseColor.a;
                #if defined(_ALPHATEST_ON)
                    clip(alfa - _Cutoff);
                #endif
                #if defined(_DETAIL_MULX2)
                    cor *= lerp(1, SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, i.uvD).rgb * 2, _DetailAlbedoMapScale);
                #endif
                float3 vWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 n = normalize(i.normalWS); float costas = (_Cull < .5 && dot(n, vWS) < 0) ? -1 : 1; n *= costas;
                float3 nGeo = n;
                #if defined(_NORMALMAP) || defined(_DETAIL_MULX2)
                {
                    float3 q0 = ddx(i.positionWS), q1 = ddy(i.positionWS); float sinal = (dot(cross(q0, q1), n) >= 0 ? 1 : -1) * costas;
                    #if defined(_NORMALMAP)
                        float3 mapN = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv).xyz * 2 - 1; mapN.xy *= _BumpScale;
                        n = RR_Perturbar(n, q0, q1, ddx(i.uv), ddy(i.uv), mapN, sinal);
                    #endif
                    #if defined(_DETAIL_MULX2)
                        float3 detN = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, i.uvD).xyz * 2 - 1;
                        if (abs(detN.x) + abs(detN.y) < 1.9) { detN.xy *= _DetailNormalMapScale; n = RR_Perturbar(n, q0, q1, ddx(i.uvD), ddy(i.uvD), detN, sinal); }
                    #endif
                }
                #endif
                // material do three: aspereza (com a rugosidade da geometria), difusa sem o metal, reflexo de 4% nos não metais
                float rug = RR_Aspereza(1 - saturate(_Smoothness / .9), nGeo), met = _Metallic;
                float3 dif, esp; RR_LuzThree(i.positionWS, i.positionCS, n, vWS, cor * (1 - met), lerp(float3(.04, .04, .04), cor, met), rug, dif, esp);
                float3 c = RR_Ilustrar(cor, dif, esp);
                #if defined(_EMISSION)
                    c += _EmissionColor.rgb;
                #endif
                float rimT = pow(1 - saturate(dot(n, vWS)), 2.5); c += _Rim.rgb * smoothstep(.32, .5, rimT) * .85;
                c = MixFog(c, i.fog);
                #if defined(_SURFACE_TYPE_TRANSPARENT)
                    return half4(c, alfa);
                #else
                    return half4(c, 1);
                #endif
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
