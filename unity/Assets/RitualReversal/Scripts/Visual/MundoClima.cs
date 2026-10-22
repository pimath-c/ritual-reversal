// Clima da noite (CLIMAS, climaDe e atualizarClima do index.html). Cada rodada sorteia o clima pelo código da partida:
// todos os jogadores da sala veem a mesma noite. Névoa fecha a vista; chuva apaga as estrelas, cai em riscos em volta da
// câmera (não sob o telhado) e soa; tempestade traz relâmpagos que iluminam a mata: dois clarões rápidos e o trovão
// depois, mais tarde quanto mais longe (AoTrovao, com o atraso: quem toca o som e a legenda é a partida).
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal.Visual
{
    public partial class Mundo
    {
        public class ClimaDef { public string nome, sub; public float nevoa, chuva, raios, estrelas; }
        public static readonly Dictionary<string, ClimaDef> CLIMAS = new Dictionary<string, ClimaDef> {
            { "limpa", new ClimaDef { nome = "Noite limpa", sub = "Céu aberto, estrelas e luar", nevoa = 1, chuva = 0, raios = 0, estrelas = 1 } },
            { "nevoa", new ClimaDef { nome = "Noite de névoa", sub = "A névoa fecha a vista: ouça antes de ver", nevoa = 1.4f, chuva = 0, raios = 0, estrelas = .3f } },
            { "chuva", new ClimaDef { nome = "Chuva fina", sub = "A chuva apaga as estrelas e abafa os passos", nevoa = 1.15f, chuva = .55f, raios = 0, estrelas = .1f } },
            { "tempestade", new ClimaDef { nome = "Tempestade", sub = "Relâmpagos iluminam a mata por um instante", nevoa = 1.2f, chuva = 1, raios = 1, estrelas = 0 } } };
        // o mesmo hash do navegador (FNV-1a de 32 bits): o for...of do JS anda por caractere e o charCodeAt(0) de um par
        // substituto é só a primeira metade, então a segunda metade fica de fora
        public static string ClimaDe(string id)
        {
            id = id ?? ""; uint h = 2166136261;
            for (int i = 0; i < id.Length; i++) { char c = id[i]; h = unchecked((h ^ c) * 16777619u); if (char.IsHighSurrogate(c) && i + 1 < id.Length && char.IsLowSurrogate(id[i + 1])) i++; }
            uint r = h % 100;
            return r < 38 ? "limpa" : r < 62 ? "nevoa" : r < 86 ? "chuva" : "tempestade";
        }
        // o desafio da noite usa o mesmo clima em todas as tentativas do dia: o que vem depois de # não conta
        public static string ChaveClima(Jogo g) { string m = g != null && g.matchId != null ? g.matchId : ""; int i = m.IndexOf('#'); return i >= 0 ? m.Substring(0, i) : m; }
        public static string ClimaDaRodada(Jogo g) { return ClimaDe(ChaveClima(g) + ":" + (g != null && g.round > 0 ? g.round : 1)); }

        public string climaTipo; public float climaNevoa = 1, climaChuva, climaEstrelas = 1, flash;
        public bool claroesSuaves;                       // acessibilidade: clarões reduzidos
        public System.Action<float> AoTrovao;             // o trovão vem depois deste atraso (s)
        float raioT = 25, segundoClarao = -1, trovaoEm = -1, trovaoAtraso;
        GameObject chuvaGO; Mesh chuvaMesh; Material mChuva; Vector3[] chuvaV; float[] gotas;
        static readonly Color BRANCO_RAIO = new Color(2.5f, 2.6f, 3f);

        void ConstruirChuva()
        {
            int N = Qualidade.nivel >= 3 ? 2200 : Qualidade.nivel == 2 ? 1400 : Qualidade.nivel == 1 ? 800 : 0; if (N == 0) return;
            gotas = new float[N * 3]; for (int i = 0; i < N; i++) { gotas[i * 3] = Random.Range(-22f, 22f); gotas[i * 3 + 1] = Random.Range(0f, 18f); gotas[i * 3 + 2] = Random.Range(-22f, 22f); }
            chuvaV = new Vector3[N * 2]; var idx = new int[N * 2]; for (int i = 0; i < idx.Length; i++) idx[i] = i;
            chuvaMesh = new Mesh { name = "chuva" }; chuvaMesh.MarkDynamic(); chuvaMesh.vertices = chuvaV; chuvaMesh.SetIndices(idx, MeshTopology.Lines, 0);
            mChuva = Mat.Brilho(Mat.Hex(0x9fb4cc, 0), null, Mat.Mistura.Alfa, false, true);
            chuvaGO = new GameObject("chuva"); chuvaGO.transform.SetParent(raiz, false); chuvaGO.AddComponent<MeshFilter>().sharedMesh = chuvaMesh;
            var mr = chuvaGO.AddComponent<MeshRenderer>(); mr.sharedMaterial = mChuva; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }

        // depois do Atualizar do quadro (que já pôs céu, estrelas e névoa do momento): o clima por cima
        public void AtualizarClima(Jogo g, Vector3 cam, float dt)
        {
            bool jogo = g != null && g.phase == "play"; string tipo = jogo ? ClimaDaRodada(g) : "limpa"; var C = CLIMAS[tipo];
            if (tipo != climaTipo) { climaTipo = tipo; raioT = Random.Range(12f, 30f); }
            float k = Mathf.Min(1, dt * .5f); climaNevoa = Mathf.Lerp(climaNevoa, C.nevoa, k); climaChuva = Mathf.Lerp(climaChuva, C.chuva, k); climaEstrelas = Mathf.Lerp(climaEstrelas, C.estrelas, k);
            bool coberto = Mapa.Carregado && Mapa.Coberto(cam.x, cam.z);
            if (climaChuva > .02f && chuvaGO == null) ConstruirChuva();
            if (chuvaGO != null)
            {
                int N = gotas.Length / 3, n = Mathf.RoundToInt(N * Mathf.Min(1, climaChuva * 1.2f)); float vy = 17 + climaChuva * 6, vx = 2.5f * climaChuva;
                for (int i = 0; i < N; i++)
                {
                    float y = gotas[i * 3 + 1] - vy * dt; if (y < 0) y += 18; gotas[i * 3 + 1] = y;
                    float x = cam.x + gotas[i * 3], z = cam.z + gotas[i * 3 + 2], yy = y;
                    if (i >= n || (Mapa.Carregado && Mapa.Coberto(x, z))) yy = -60;   // sob o telhado não chove
                    chuvaV[i * 2] = new Vector3(x, yy, z); chuvaV[i * 2 + 1] = new Vector3(x - vx * .04f, yy + .55f, z);
                }
                chuvaMesh.vertices = chuvaV; chuvaMesh.bounds = new Bounds(new Vector3(cam.x, 9, cam.z), new Vector3(50, 90, 50));
                Mat.Opacidade(mChuva, .32f * Mathf.Min(1, climaChuva * 1.5f)); chuvaGO.SetActive(climaChuva > .02f);
            }
            // relâmpagos: dois clarões rápidos e o trovão depois
            float agora = Time.time;
            if (jogo && C.raios > 0) { raioT -= dt; if (raioT <= 0) { raioT = Random.Range(18f, 42f); trovaoAtraso = Random.Range(.3f, 2.6f); flash = 1; segundoClarao = agora + .14f; trovaoEm = agora + trovaoAtraso; } }
            if (segundoClarao > 0 && agora >= segundoClarao) { flash = Mathf.Max(flash, .8f); segundoClarao = -1; }
            if (trovaoEm > 0 && agora >= trovaoEm) { trovaoEm = -1; if (AoTrovao != null) AoTrovao(trovaoAtraso); }
            flash = Mathf.Max(0, flash - dt * 5.5f); if (claroesSuaves) flash = Mathf.Min(flash, .25f);
            // névoa, estrelas, a lua e o céu
            RenderSettings.fogDensity = densNevoa * climaNevoa; sol.intensity = .42f + flash * (coberto ? 1.2f : 4.5f);
            if (g == null) return;   // estrelas e céu: só por cima da cor que o Atualizar acabou de pôr neste quadro
            var ce = mEstrelas.HasProperty("_Color") ? mEstrelas.GetColor("_Color") : mEstrelas.GetColor("_BaseColor"); ce.a *= climaEstrelas; Mat.Cor(mEstrelas, ce);
            if (flash > 0) { var cc = mCeu.HasProperty("_Color") ? mCeu.GetColor("_Color") : mCeu.GetColor("_BaseColor"); Mat.Cor(mCeu, Color.Lerp(cc, Mat.Lin(BRANCO_RAIO), flash * .8f)); }
        }
    }
}
