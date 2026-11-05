// Etapa GRADE do Ritual Reversal (URP, passe de tela cheia depois do pós-processamento; Pos.cs o põe no renderizador).
// A parte da etapa final do index.html que o Volume do URP não faz:
//  • contorno a nanquim: onde a profundidade salta (silhuetas) ou dobra (quinas), a cor puxa para a tinta; some longe;
//  • névoa rasteira: reconstrói o ponto no mundo pela profundidade e integra uma névoa que adensa perto do chão e rola
//    devagar; dentro da catedral quase some.
// Como no navegador, as contas são feitas na cor de tela (sRGB), depois do ACES.
Shader "RitualReversal/Grade"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "Grade"
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag

            float4 _Tinta, _NevoaCor, _Cat, _Tan;      // _Cat: retângulo da catedral (x1, z1, x2, z2) nas coordenadas do protótipo
            float _NevoaDens, _T, _Contorno, _Longe;
            float4x4 _CamMundo;                         // câmera → mundo do Unity (espaço da câmera do OpenGL: frente em -z)

            float h(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }
            float vr2(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f); float a = h(i), b = h(i + float2(1, 0)), c2 = h(i + float2(0, 1)), d2 = h(i + float2(1, 1)); return lerp(lerp(a, b, f.x), lerp(c2, d2, f.x), f.y); }
            float Lin(float2 uv) { return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            float4 Frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float3 col = LinearToSRGB(saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb));
                // contorno a nanquim
                float2 px = 1.4 / _ScreenParams.xy; float d0 = Lin(uv);
                float dl = Lin(uv - float2(px.x, 0)), dr = Lin(uv + float2(px.x, 0)), du = Lin(uv + float2(0, px.y)), dd = Lin(uv - float2(0, px.y));
                float salto = max(max(abs(dl - d0), abs(dr - d0)), max(abs(du - d0), abs(dd - d0))) / max(d0, .05);
                float i0 = 1.0 / d0, dobra = abs(1.0 / dl + 1.0 / dr + 1.0 / du + 1.0 / dd - 4.0 * i0) / i0;
                float borda = max(smoothstep(.1, .2, salto), smoothstep(.012, .03, dobra)) * (1.0 - smoothstep(28.0, 60.0, d0));
                if (d0 > _Longe * .95) borda = smoothstep(.1, .2, salto);
                col = lerp(col, _Tinta.rgb, borda * .88 * _Contorno);
                // névoa rasteira (o mundo do Unity é o do protótipo com z trocado de sinal)
                {
                    float dist0 = min(d0, 90.0);
                    float3 pW = mul(_CamMundo, float4(float3((uv.x * 2.0 - 1.0) * _Tan.x, (uv.y * 2.0 - 1.0) * _Tan.y, -1.0) * dist0, 1.0)).xyz; float3 cW = mul(_CamMundo, float4(0, 0, 0, 1)).xyz;
                    pW.z = -pW.z; cW.z = -cW.z;
                    float L = length(pW - cW), H = 1.9, yc = max(cW.y, 0.0), yp = max(pW.y, 0.0), eC = exp(-yc / H), eP = exp(-yp / H);
                    float integ = abs(yp - yc) > .05 ? H * (eC - eP) / (yp - yc) : eC;
                    float dentro = min(min(pW.x - _Cat.x, _Cat.z - pW.x), min(pW.z - _Cat.y, _Cat.w - pW.z)), mata = 1.0 - .88 * smoothstep(-3.0, 4.0, dentro);
                    float ond = vr2(pW.xz * .045 + float2(_T * .035, _T * .021)) * .65 + vr2(pW.xz * .13 - float2(_T * .05, -_T * .03)) * .35;
                    float f = 1.0 - exp(-_NevoaDens * mata * (.45 + 1.1 * ond) * integ * L);
                    col = lerp(col, _NevoaCor.rgb, clamp(f, 0.0, .9));
                }
                return float4(SRGBToLinear(saturate(col)), 1);
            }
            ENDHLSL
        }
    }
}
