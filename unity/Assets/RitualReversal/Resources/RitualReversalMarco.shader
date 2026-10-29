// Marcos do Ritual Reversal (URP): torres, agulhas, contrafortes e telhados da catedral (MundoV6.cs, Marco()).
// Iluminado como um Lit simples (luz principal com sombra, luzes adicionais, luz ambiente), mas sente só uma fração da
// névoa: _NevoaK = 0,07 no expoente, o mesmo do navegador (client/mundo.js, marco()). De qualquer ponto da mata, por
// cima das copas, a silhueta da catedral continua legível. Sem este shader, MundoV6 usa o URP/Lit (névoa inteira).
Shader "RitualReversal/Marco"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Cor", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Faces", Float) = 2
        _NevoaK ("Fração da névoa", Float) = 0.07
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Cull;
                float _NevoaK;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float nevoa : TEXCOORD3;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                // névoa exponencial ao quadrado: exp(-(d·z)²) vira exp(-k·(d·z)²) multiplicando o fator por √k
                o.nevoa = ComputeFogFactor(p.positionCS.z) * sqrt(_NevoaK);
                return o;
            }

            half3 Luz (Light l, half3 n, half3 albedo)
            {
                return albedo * l.color * (l.distanceAttenuation * l.shadowAttenuation) * saturate(dot(n, l.direction));
            }

            half4 frag (Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                half3 c = albedo * SampleSH(n);
                c += Luz(GetMainLight(TransformWorldToShadowCoord(i.positionWS)), n, albedo);
                #if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS) || defined(_CLUSTER_LIGHT_LOOP)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.positionWS;
                    inputData.normalWS = n;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                    uint nLuzes = GetAdditionalLightsCount();
                    #ifdef LIGHT_LOOP_BEGIN
                        LIGHT_LOOP_BEGIN(nLuzes)
                            c += Luz(GetAdditionalLight(lightIndex, i.positionWS), n, albedo);
                        LIGHT_LOOP_END
                    #else
                        for (uint lightIndex = 0u; lightIndex < nLuzes; ++lightIndex)
                            c += Luz(GetAdditionalLight(lightIndex, i.positionWS), n, albedo);
                    #endif
                #endif
                c = MixFog(c, i.nevoa);
                return half4(c, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
