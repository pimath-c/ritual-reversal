// Brilhos do Ritual Reversal (URP): chamas, halos, runas, feixes, vitrais, céu, estrelas e partículas.
// Faz o papel dos SpriteMaterial/PointsMaterial/MeshBasicMaterial aditivos do protótipo.
// _Billboard = 1: cada vértice é um canto de um quadrado virado para a câmera (o centro fica na posição e o canto em
// TEXCOORD1, em metros, multiplicado pela escala do objeto). _Nevoa = 0: ignora a névoa (céu, lua, vitrais).
Shader "RitualReversal/Brilho"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Cor", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Origem", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destino", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Faces", Float) = 0
        _ZWrite ("Escreve profundidade", Float) = 0
        _Billboard ("Virado para a câmera", Float) = 0
        _Nevoa ("Névoa", Float) = 1
        _Corte ("Corte de alfa", Float) = 0
        _Giro ("Giro (rad)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Brilho"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _Billboard;
                float _Nevoa;
                float _Corte;
                float _Giro;
                float _SrcBlend;
                float _DstBlend;
                float _Cull;
                float _ZWrite;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 canto : TEXCOORD1;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float fogCoord : TEXCOORD1;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 posVS = TransformWorldToView(posWS);
                float s = sin(_Giro);
                float c = cos(_Giro);
                float2 canto = float2(v.canto.x * c - v.canto.y * s, v.canto.x * s + v.canto.y * c);
                float sx = length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20));
                float sy = length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21));
                posVS.xy += canto * float2(sx, sy) * _Billboard;
                o.positionCS = TransformWViewToHClip(posVS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color * i.color;
                clip(c.a - _Corte);
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    half k = lerp(1.0h, (half)ComputeFogIntensity(i.fogCoord), (half)_Nevoa);
                    c.rgb *= k;
                    c.a *= k;
                #endif
                return c;
            }
            ENDHLSL
        }
    }
}
