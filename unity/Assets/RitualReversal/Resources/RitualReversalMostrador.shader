// Mostrador no chão do altar (v5.6, buildAltars do index.html): a borda no raio exato do círculo, o progresso do ritual
// (carmim, com os dois pontos de controle) e o selo (dourado, por dentro). Uma onda de luz corre para fora quando o ritual
// fecha, abre Fenda ou é interrompido (_Fl, _CorF). Só aparece com o ritual ativo e conhecido (_Op, MundoAltares.cs).
Shader "RitualReversal/Mostrador"
{
    Properties
    {
        _Prog ("Progresso", Float) = 0
        _Selo ("Selo", Float) = 0
        _Op ("Opacidade", Float) = 0
        _Fl ("Onda", Float) = 0
        _CorF ("Cor da onda", Vector) = (1,1,1,1)
        _R ("Raio do círculo", Float) = 3
        _Lado ("Lado do quadro", Float) = 7.2
        _CorR ("Cor do ritual", Vector) = (1,.227,.353,1)
        _CorS ("Cor do selo", Vector) = (.953,.835,.557,1)
        _CorB ("Cor da borda", Vector) = (.878,.263,.369,1)
        _Cp ("Pontos de controle", Vector) = (.25,.5,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+3" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Prog, _Selo, _Op, _Fl, _R, _Lado; float4 _CorF, _CorR, _CorS, _CorB, _Cp;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert (Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; return o; }
            float Banda(float r, float a, float b) { return smoothstep(a - .03, a + .01, r) * (1.0 - smoothstep(b - .01, b + .03, r)); }
            half4 frag (Varyings i) : SV_Target
            {
                float2 p = (i.uv - .5) * _Lado; float r = length(p); float ang = frac(atan2(p.x, -p.y) / 6.2831853 + 1.0);
                float4 c = 0;
                float borda = Banda(r, _R - .07, _R + .03); c = lerp(c, float4(_CorB.rgb, .85), borda);
                float fora = Banda(r, _R + .14, _R + .36), cheio = step(ang, _Prog);
                float tick = (1.0 - smoothstep(0, .006, abs(ang - _Cp.x))) + (1.0 - smoothstep(0, .006, abs(ang - _Cp.y)));
                c = lerp(c, float4(.16, .17, .2, .6), fora * (1.0 - cheio)); c = lerp(c, float4(_CorR.rgb, .95), fora * cheio); c = lerp(c, float4(1.0, .93, .85, .95), fora * saturate(tick));
                float dentro = Banda(r, _R - .42, _R - .2), sv = step(ang, _Selo) * step(.001, _Selo);
                c = lerp(c, float4(.16, .17, .2, .5), dentro * step(.001, _Selo) * (1.0 - sv)); c = lerp(c, float4(_CorS.rgb, .95), dentro * sv);
                float onda = _Fl * Banda(r, _R - .2 + (1.0 - _Fl) * .5, _R + .12 + (1.0 - _Fl) * .5); c = lerp(c, float4(_CorF.rgb, .9), saturate(onda) * .6);
                float a = c.a * _Op; clip(a - .01);
                return half4(c.rgb, a);
            }
            ENDHLSL
        }
    }
}
