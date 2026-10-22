// Luz do navegador (three.js r128, MeshStandardMaterial com luzes "não físicas") e o estilo ilustrado do index.html, para
// os shaders do Ritual Reversal (Ilustrado, PrimeiraPessoa). Incluir depois do Lighting.hlsl do URP.
#ifndef RR_LUZ_INCLUDED
#define RR_LUZ_INCLUDED

// BRDF do three (bsdfs.glsl): Schlick, Smith correlacionado, GGX
float3 RR_FSchlick(float3 f0, float dotLH) { float fr = exp2((-5.55473 * dotLH - 6.98316) * dotLH); return (1.0 - f0) * fr + f0; }
float RR_GSmith(float a, float nl, float nv) { float a2 = a * a; float gv = nl * sqrt(a2 + (1.0 - a2) * nv * nv), gl = nv * sqrt(a2 + (1.0 - a2) * nl * nl); return 0.5 / max(gv + gl, 1e-6); }
float RR_DGGX(float a, float nh) { float a2 = a * a; float d = nh * nh * (a2 - 1.0) + 1.0; return INV_PI * a2 / (d * d); }
float3 RR_GGX(float3 L, float3 V, float3 N, float3 f0, float rug)
{
    float a = rug * rug; float3 H = normalize(L + V); float nl = saturate(dot(N, L)), nv = saturate(dot(N, V)), nh = saturate(dot(N, H)), lh = saturate(dot(L, H));
    return RR_FSchlick(f0, lh) * (RR_GSmith(a, nl, nv) * RR_DGGX(a, nh));
}

// relevo sem tangentes (perturbNormal2Arb do three). sinal: o de dx × dy contra a normal (a tela do OpenGL e a do Direct3D
// têm o y trocado) vezes −1 na face de trás de uma peça dupla
float3 RR_Perturbar(float3 n, float3 q0, float3 q1, float2 st0, float2 st1, float3 mapN, float sinal)
{
    float3 q1p = cross(q1, n), q0p = cross(n, q0), T = q1p * st0.x + q0p * st1.x, B = q1p * st0.y + q0p * st1.y;
    float det = max(dot(T, T), dot(B, B)), sc = det == 0 ? 0 : sinal * rsqrt(det);
    return normalize(T * (mapN.x * sc) + B * (mapN.y * sc) + n * mapN.z);
}

// a aspereza do three: no mínimo 0,0525, mais a rugosidade da própria geometria (normal que varia muito num pixel)
float RR_Aspereza(float rug, float3 nGeo) { float3 dxy = max(abs(ddx(nGeo)), abs(ddy(nGeo))); return min(max(rug, .0525) + max(max(dxy.x, dxy.y), dxy.z), 1); }

// difusa (irradiância × Lambert, sem o 1/π) e especular (× GGX × π) de todas as luzes, mais a luz ambiente pela SH
void RR_LuzThree(float3 positionWS, float4 positionCS, float3 n, float3 vWS, float3 difCor, float3 f0, float rug, out float3 dif, out float3 esp)
{
    float4 sc = TransformWorldToShadowCoord(positionWS); half4 mask = half4(1, 1, 1, 1);
    dif = 0; esp = 0;
    Light L0 = GetMainLight(sc, positionWS, mask);
    { float3 rad = L0.color * (L0.distanceAttenuation * L0.shadowAttenuation * saturate(dot(n, L0.direction))); dif += rad * difCor; esp += rad * PI * RR_GGX(L0.direction, vWS, n, f0, rug); }
    #if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS) || defined(_CLUSTER_LIGHT_LOOP)
    {
        InputData inputData = (InputData)0; inputData.positionWS = positionWS; inputData.normalWS = n; inputData.viewDirectionWS = vWS;
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS); inputData.shadowMask = mask;
        uint nLuzes = GetAdditionalLightsCount();
        #ifdef LIGHT_LOOP_BEGIN
            LIGHT_LOOP_BEGIN(nLuzes)
                Light Lk = GetAdditionalLight(lightIndex, positionWS, mask);
                float3 rad = Lk.color * (Lk.distanceAttenuation * Lk.shadowAttenuation * saturate(dot(n, Lk.direction))); dif += rad * difCor; esp += rad * PI * RR_GGX(Lk.direction, vWS, n, f0, rug);
            LIGHT_LOOP_END
        #else
            for (uint lightIndex = 0u; lightIndex < nLuzes; ++lightIndex)
            {
                Light Lk = GetAdditionalLight(lightIndex, positionWS, mask);
                float3 rad = Lk.color * (Lk.distanceAttenuation * Lk.shadowAttenuation * saturate(dot(n, Lk.direction))); dif += rad * difCor; esp += rad * PI * RR_GGX(Lk.direction, vWS, n, f0, rug);
            }
        #endif
    }
    #endif
    dif += SampleSH(n) * difCor;
}

// estilo ilustrado (index.html, estiloIlustrado): a luz difusa muda em faixas; o brilho especular vira um reflexo recortado
float3 RR_Ilustrar(float3 cor, float3 dif, float3 esp)
{
    float3 albedoT = max(cor, .04); float3 luzT = dif / albedoT; float liT = dot(luzT, float3(.299, .587, .114));
    float bT = .035; bT = lerp(bT, .11, smoothstep(.03, .05, liT)); bT = lerp(bT, .26, smoothstep(.11, .15, liT)); bT = lerp(bT, .52, smoothstep(.30, .38, liT)); bT = lerp(bT, .95, smoothstep(.70, .85, liT));
    float3 difT = luzT * (bT / max(liT, .0001)) * albedoT;
    esp *= smoothstep(.06, .09, dot(esp, float3(.333, .333, .333))) * 1.15;
    return difT + esp;
}
#endif
