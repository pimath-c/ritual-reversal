// Materiais da primeira pessoa do Ritual Reversal (URP): mãos, braços, armas e acessórios (ArmaFP.cs, SkinsFP.cs).
// A luz do three em faixas (RitualReversalLuz.hlsl: o estilo ilustrado vale para todo MeshStandardMaterial) e, por cima,
// o que o client/arma.js e o client/skins.js põem no shader do three:
//  • luz de leitura: uma luz própria vinda de cima à esquerda no espaço da câmera, com brilho especular, e um contorno de
//    luz fria (_RRLeitura e _RRRim são globais: 0,2 / 0,16 no jogo; 1,3 / 0,4 na vitrine e no Arsenal);
//  • detalhe da skin: textura em três planos no espaço da peça (sem depender das UVs). R escava sulcos (relevo pela
//    derivada da altura, fundo mais escuro e áspero), G incrusta outro material, B mancha a cor;
//  • varredura de luz da inspeção (_Varre: posição, largura e força de uma faixa que corre pela tela).
// O relevo das texturas (cloth, leather, metal, wood) vem das derivadas de tela, como o perturbNormal2Arb do three: as
// malhas exportadas não têm tangentes. A primeira pessoa não projeta nem recebe sombra (como no navegador).
Shader "RitualReversal/PrimeiraPessoa"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Cor", Color) = (1,1,1,1)
        _BumpMap ("Relevo", 2D) = "bump" {}
        _BumpScale ("Força do relevo", Float) = 1
        _TemRelevo ("Tem relevo", Float) = 0
        _Rug ("Aspereza", Range(0,1)) = 1
        _Metal ("Metal", Range(0,1)) = 0
        _Emissao ("Emissão", Color) = (0,0,0,1)
        _EmissaoI ("Intensidade da emissão", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Faces", Float) = 2
        _Det ("Detalhe da skin", 2D) = "black" {}
        _DetEsc ("Escala do detalhe", Float) = 10
        _Sulco ("Sulco", Float) = 0
        _Inc ("Incrustação", Float) = 0
        _Mancha ("Mancha", Float) = 0
        _Relevo ("Relevo do detalhe", Float) = 0
        _ManchaCor ("Cor da mancha", Color) = (0,0,0,1)
        _IncCor ("Cor da incrustação", Color) = (0,0,0,1)
        _IncMet ("Metal da incrustação", Float) = 1
        _IncRug ("Aspereza da incrustação", Float) = .2
        _Varre ("Varredura (x, largura, força)", Vector) = (0,.06,0,0)
        _VarreCor ("Cor da varredura", Color) = (1,1,1,1)
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RitualReversalLuz.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_Det); SAMPLER(sampler_Det);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; float _BumpScale, _TemRelevo, _Rug, _Metal; half4 _Emissao; float _EmissaoI, _Cull;
                float _DetEsc, _Sulco, _Inc, _Mancha, _Relevo; half4 _ManchaCor, _IncCor; float _IncMet, _IncRug; float4 _Varre; half4 _VarreCor;
            CBUFFER_END
            float _RRLeitura, _RRRim;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2;
                float3 posO : TEXCOORD3; float3 norO : TEXCOORD4; float fog : TEXCOORD5;
            };

            Varyings vert (Attributes v)
            {
                Varyings o; VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap); o.posO = v.positionOS.xyz; o.norO = v.normalOS; o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // espaço da câmera: o mesmo do three (x à direita, y para cima, z para quem olha)
                float3 posVS = TransformWorldToView(i.positionWS);
                float3 vWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 nWS = normalize(i.normalWS);
                float costas = (_Cull < .5 && dot(nWS, vWS) < 0) ? -1 : 1;      // face de trás de peça dupla: a normal vira
                nWS *= costas;
                float3 n = normalize(mul((float3x3)UNITY_MATRIX_V, nWS));
                half3 cor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb; float rug = _Rug, met = _Metal;
                // detalhe da skin (client/skins.js)
                float3 bl = pow(abs(normalize(i.norO)), float3(4, 4, 4)); bl /= (bl.x + bl.y + bl.z + 1e-5);
                half4 d = SAMPLE_TEXTURE2D(_Det, sampler_Det, i.posO.zy * _DetEsc) * bl.x + SAMPLE_TEXTURE2D(_Det, sampler_Det, i.posO.xz * _DetEsc) * bl.y + SAMPLE_TEXTURE2D(_Det, sampler_Det, i.posO.xy * _DetEsc) * bl.z;
                float S = d.r * _Sulco, I = d.g * _Inc, M = d.b * _Mancha * (1 - d.g * _Inc);
                cor = lerp(cor, _ManchaCor.rgb, M); cor *= 1 - S * .62; rug = min(1, rug + S * .38);
                cor = lerp(cor, _IncCor.rgb, I); rug = lerp(rug, _IncRug, I); met = lerp(met, _IncMet, I);
                float3 q0 = ddx(posVS), q1 = ddy(posVS);
                // a orientação da tela muda entre OpenGL e Direct3D: o sinal de (dx × dy) contra a normal diz qual é
                float api = dot(cross(q0, q1), n) >= 0 ? 1 : -1;
                if (_TemRelevo > .5)
                {
                    float2 st0 = ddx(i.uv), st1 = ddy(i.uv);
                    float3 mapN = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv).xyz * 2 - 1; mapN.xy *= _BumpScale;
                    float3 q1p = cross(q1, n), q0p = cross(n, q0), T = q1p * st0.x + q0p * st1.x, B = q1p * st0.y + q0p * st1.y;
                    float det = max(dot(T, T), dot(B, B)), sc = det == 0 ? 0 : api * costas * rsqrt(det);
                    n = normalize(T * (mapN.x * sc) + B * (mapN.y * sc) + n * mapN.z);
                }
                if (_Relevo > 0)
                {
                    float h = -S + I * .25; float2 dH = float2(ddx(h), ddy(h)) * _Relevo * .004;
                    float3 R1 = cross(q1, n), R2 = cross(n, q0); float fD = dot(q0, R1);
                    n = normalize(abs(fD) * n - sign(fD) * (dH.x * R1 + dH.y * R2));
                }
                float3 nW = normalize(mul(n, (float3x3)UNITY_MATRIX_V));   // de volta ao mundo (a matriz de vista é ortogonal)

                half3 em = _Emissao.rgb * _EmissaoI;
                if (_Varre.z > 0) { float vv = exp(-pow((dot(posVS.xy, float2(.93, .37)) - _Varre.x) / _Varre.y, 2)) * _Varre.z; em += _VarreCor.rgb * vv * (.12 + S * 1.4 + I * 1.8); }
                // a luz do three em faixas (o estilo ilustrado vale para todo MeshStandardMaterial, a primeira pessoa inclusive)
                float3 dif, esp; RR_LuzThree(i.positionWS, i.positionCS, nW, vWS, cor * (1 - met), lerp(float3(.04, .04, .04), cor, met), RR_Aspereza(rug, nWS), dif, esp);
                half4 c = half4(RR_Ilustrar(cor, dif, esp) + em, 1);

                // luz de leitura (client/arma.js, luzDeLeitura)
                float3 Ld = normalize(float3(-.45, .7, .55)), Vd = normalize(-posVS);
                float k = max(dot(n, Ld), 0), r = pow(1 - max(dot(n, float3(0, 0, 1)), 0), 3);
                float sp = pow(max(dot(n, normalize(Ld + Vd)), 0), lerp(6, 72, 1 - rug)) * (1 - rug * .75) * (.25 + met * .75);
                c.rgb += cor * _RRLeitura * (k * (1 - met * .6) + .22) + lerp(float3(1, 1, 1), cor, met) * min(sp * _RRLeitura * 1.1, .85) + float3(.55, .62, .8) * _RRRim * r * (1 - rug * .6);
                c.rgb = MixFog(c.rgb, i.fog);
                return half4(c.rgb, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
