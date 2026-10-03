// Parte visual e sonora da partida: monta o mundo do protótipo (Scripts/Visual: catedral, floresta, céu, luzes,
// altares), a primeira pessoa, o pós-processamento e o som sintetizado, e a cada quadro repassa o estado da simulação
// para eles: bonecos animados, sigilos, pegadas, sal, mercadores, pontos de tarefa, partículas e a câmera.
// O blockout do importador sai de cena quando a partida começa (a colisão é a da simulação, igual à do protótipo).
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static readonly Color OURO = new Color(.82f, .68f, .38f), CARMIM = new Color(.64f, .13f, .23f), CARMIM_VIVO = new Color(.88f, .26f, .37f),
            ROXO = new Color(.42f, .25f, .56f), ROXO_VIVO = new Color(.75f, .55f, .95f), CINZA = new Color(.35f, .36f, .38f), OSSO = new Color(.91f, .88f, .82f);

        Mundo mundo; PrimeiraPessoa fp; Pos pos; Som som;
        Entrada ultimaEntrada; float localCd, passoT, alucT = 8, coracaoT, sinoT = 45; bool ultimoTiroCab;

        void CriarVisual()
        {
            // a câmera do passeio livre fica, mas solta do jogador do blockout; o blockout some
            cam.transform.SetParent(null, true);
            var raizBlockout = transform.parent;
            if (raizBlockout != null) foreach (Transform c in raizBlockout) if (c != transform) c.gameObject.SetActive(false);
            mundo = Mundo.Construir(); fp = new PrimeiraPessoa(cam);
            try { pos = new Pos(cam); } catch (System.Exception e) { Debug.LogWarning("Ritual Reversal: sem pós-processamento (" + e.Message + ")"); }
            som = Som.Criar(cam.gameObject); mundo.AoGritarCorvo = (x, z) => som.Tocar("corvo", x, z);
        }

        void LimparVisual() { if (mundo != null) mundo.Limpar(); if (som != null) som.Silenciar(); }

        // posição da câmera nas coordenadas do protótipo
        Vector3 CamP() { var p = cam.transform.position; return new Vector3(p.x, p.y, -p.z); }

        void AtualizarVisual(float dt)
        {
            if (mundo == null) return;
            float agora = Time.time; var m = Eu(); string papel = MeuPapel(); bool jogando = g != null && g.phase == "play" && m != null;
            if (jogando)
            {
                bool correndo = ultimaEntrada != null && ultimaEntrada.sp && ultimaEntrada.mz < 0 && m.moving && m.st == "alive";
                fp.Atualizar(m, (float)m.x, (float)m.z, m.moving, ultimaEntrada != null ? (float)ultimaEntrada.mx : 0, correndo, (float)lookYaw, (float)lookPitch, dt, agora);
            }
            else
            {
                fp.Esconder(); float a = agora * .04f;
                cam.transform.position = new Vector3(6 + Mathf.Sin(a) * 17, 3.4f, -(-4 + Mathf.Cos(a) * 13)); cam.transform.LookAt(new Vector3(6, 4.5f, 4)); cam.transform.Rotate(0, .28f * Mathf.Rad2Deg, 0, Space.Self);
                cam.fieldOfView = 72;
            }
            var c = CamP(); float yawCam = jogando ? (float)lookYaw : -cam.transform.eulerAngles.y * Mathf.Deg2Rad;
            mundo.Atualizar(g, papel, m, c, yawCam, dt);
            mundo.MostrarPartida(jogando);
            if (jogando) { mundo.SyncActors(g, papel, m, c, (float)lookYaw, dt); mundo.SyncNoite(g, c, dt); }
            mundo.AtualizarEfeitosQuadro(dt);
            // som: ouvinte, rituais, passos, alucinações, coração, sino
            som.ouvinte = c; som.yawOuvinte = yawCam; som.Rituais(jogando ? g : null, papel, mundo.GRk, dt);
            if (jogando)
            {
                float s = (float)m.sanity;
                if (m.moving && m.st == "alive") { passoT -= dt; if (passoT <= 0) { passoT = .45f; som.Tocar("mystep"); } }
                if (s < 40) { alucT -= dt; if (alucT <= 0) { alucT = Random.Range(6f, 12f); float an = Random.Range(0, Mathf.PI * 2); if (Random.value < .5f) som.Tocar("whisper", (float)m.x + Mathf.Cos(an) * 6, (float)m.z + Mathf.Sin(an) * 6, 1); else som.Tocar("alucinacao"); } }
                if (s < 15) { coracaoT -= dt; if (coracaoT <= 0) { coracaoT = .85f; som.Tocar("heart"); } }
                sinoT -= dt; if (sinoT <= 0) { sinoT = Random.Range(45f, 80f); som.Tocar("bell"); }
                if (pos != null) pos.Atualizar(s, mundo.GRk, Mathf.Clamp01(hurtT / .35f), Mathf.Clamp01(blindT / 1.5f));
            }
            else if (pos != null) pos.Atualizar(100, 0, 0, 0);
        }

        // retorno imediato da arma (predictFire): som, clarão e coice antes da simulação confirmar
        void PreverTiro(Ator m, float dt, bool atirando)
        {
            localCd = Mathf.Max(0, localCd - dt);
            if (m == null || m.st != "alive" || !atirando || g.phase != "play" || m.blindT > 0 || m.stunT > 0 || localCd > 0) return;
            var c = CamP();
            if (m.team == "H")
            {
                if (m.reloadT > 0 || m.sealing >= 0) return;
                if (m.ammo <= 0) { localCd = .35f; som.Tocar("vazio"); if (m.reserve > 0) botoes.r++; return; }
                localCd = .3f; som.Tocar("shot", c.x, c.z); fp.Tiro();
            }
            else { if (m.fervor < CFG.sigilCusto) { localCd = .35f; som.Tocar("vazio"); return; } localCd = .42f; som.Tocar("sigil", c.x, c.z); fp.Sigilo(); }
        }

        // eventos da simulação que viram efeito e som (handleEvents do protótipo)
        void EventoAudiovisual(Evento e, string papel, int eu)
        {
            if (e.type == "presage" && papel != "H") return;
            if ((e.type == "sensor" || e.type == "evp" || e.type == "blind" || e.type == "stun") && e.Int("a") != eu) return;
            if (e["to"] != null && e.Int("to") != eu) return;
            if (mundo != null) mundo.EventoVisual(e, eu, fp.BocaMundo(mundo.raiz));
            float x = (float)e.Num("x"), z = (float)e.Num("z");
            switch (e.type)
            {
                case "sfx":
                    {
                        string k = e.Str("k"); if (e.Int("a") == eu && (k == "shot" || k == "sigil" || k == "step" || k == "passoForte")) break;
                        som.Tocar(k, x, z); break;
                    }
                case "tracer": if (e.Int("a") == eu) ultimoTiroCab = e["cab"] is bool && (bool)e["cab"]; break;
                case "hit":
                    if (e.Int("a") == eu)
                    {
                        bool derrubou = e["derrubou"] is bool && (bool)e["derrubou"]; marcaAcerto = derrubou ? "kill" : ultimoTiroCab ? "head" : "";
                        hitT = derrubou ? .45f : ultimoTiroCab ? .3f : .15f; som.Tocar(derrubou ? "derrubou" : ultimoTiroCab ? "cabeca" : "hit"); ultimoTiroCab = false;
                    }
                    break;
                case "hurt": if (e.Int("a") == eu) { som.Tocar("hurt"); if (e["sx"] != null) DirecaoDano((float)e.Num("sx"), (float)e.Num("sz")); fp.kickP += Random.Range(-.02f, .02f); } break;
                case "burst": som.Tocar("estouro", (float)e.Num("x"), (float)e.Num("z")); break;
                case "reload": if (e.Int("a") == eu) som.Tocar("reload"); else som.Tocar("reload", x, z); break;
                case "presage": som.Tocar("presage"); break;
                case "sensor": som.Tocar("ping", float.NaN, float.NaN, (float)e.Num("v")); break;
                case "evp": som.Tocar("whisper", x, z); break;
                case "blind": NotaHab("Você foi cegado", new Color(1, .96f, .82f)); break;
                case "stun": atordT = 1; NotaHab("Você foi atordoado", OURO); fp.kickP += .03f; break;
                case "momento": som.Tocar("bell"); break;
                case "grande":
                    som.Tocar("doom"); som.Tocar("sinoGrande"); StartCoroutine(Depois(2.3f, () => som.Tocar("sinoGrande"))); StartCoroutine(Depois(4.6f, () => som.Tocar("sinoGrande")));
                    mundo.EspantarCorvos(); claraoGrande = 1.2f; fp.kickP += .05f;
                    { int ai = e.Int("altar"); Anunciar("O Grande Ritual" + (ai >= 0 && ai < g.altars.Count ? ": " + g.altars[ai].name + ", a catedral inteira ouve" : ""), 4.5f); }
                    break;
                case "hab":
                    {
                        Ator quem = g.actors.Find(a => a.id == e.Int("a")); som.Hab(quem != null ? quem.cls : "", x, z);
                        if (e.Int("a") == eu) { var al = e["alvos"] as System.Collections.ICollection; int n = al != null ? al.Count : 0; string f = e.Str("f"); NotaHab(TextoHab(f, n), n > 0 || f == "runa" || f == "veu" || f == "sal" ? OURO : CINZA * 2); if (f == "empurrao") fp.kickP += .02f; }
                        break;
                    }
                case "doom": som.Tocar("doom"); break;
                case "sealed": som.Tocar("sealed"); break;
            }
        }
        static System.Collections.IEnumerator Depois(float s, System.Action f) { yield return new WaitForSeconds(s); f(); }
        static string TextoHab(string f, int n)
        {
            switch (f)
            {
                case "runa": return "Runa: canalização +20% por 8 s";
                case "veu": return "Véu: você é só uma silhueta por 5 s";
                case "sal": return "Círculo de Sal deixado no chão por 60 s";
                case "empurrao": return n > 0 ? "Empurrão: " + n + (n > 1 ? " atingidos" : " atingido") : "Empurrão: ninguém no alcance (4 m à frente)";
                case "flash": return n > 0 ? "Flash: " + n + (n > 1 ? " cegados" : " cegado") + " por 1,5 s" : "Flash: ninguém no cone (14 m à frente)";
                default: return n > 0 ? "Purificação: " + n + (n > 1 ? " atordoados" : " atordoado") + " por 1 s" : "Purificação: ninguém a 6 m";
            }
        }

        // ---------- avisos visuais do HUD ----------
        string marcaAcerto = ""; float atordT, claraoGrande, notaT; string nota; Color notaCor;
        class Dano { public float ang, t; }
        readonly List<Dano> danos = new List<Dano>();
        void NotaHab(string txt, Color c) { nota = txt; notaCor = c; notaT = 1.8f; }
        void DirecaoDano(float sx, float sz)
        {
            var c = CamP(); float dx = sx - c.x, dz = sz - c.z, yaw = (float)lookYaw;
            float frente = dx * -Mathf.Sin(yaw) + dz * -Mathf.Cos(yaw), direita = dx * Mathf.Cos(yaw) + dz * -Mathf.Sin(yaw);
            danos.Add(new Dano { ang = Mathf.Atan2(direita, frente), t = 1.2f });
        }
    }
}
