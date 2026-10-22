// Parte visual e sonora da partida: monta o mundo do protótipo (Scripts/Visual: catedral, floresta, céu, luzes,
// altares), a primeira pessoa, o pós-processamento e o som sintetizado, e a cada quadro repassa o estado da simulação
// para eles: bonecos animados, sigilos, pegadas, sal, mercadores, pontos de tarefa, partículas e a câmera.
// O blockout do importador sai de cena quando a partida começa (a colisão é a da simulação, igual à do protótipo).
using System.Collections.Generic;
using System.Linq;
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
        Entrada ultimaEntrada; float localCd, alucT = 8, coracaoT, sinoT = 45; bool ultimoTiroCab;
        // v6: sendo executado: o mundo fecha (veuExe, desenhado no HUD), o coração acelera e a câmera vira para quem executa
        float veuExe, virarYaw, virarT; int exeVirou = -1;

        void CriarVisual()
        {
            // a câmera do passeio livre fica, mas solta do jogador do blockout; o blockout some
            cam.transform.SetParent(null, true);
            var raizBlockout = transform.parent;
            if (raizBlockout != null) foreach (Transform c in raizBlockout) if (c != transform) c.gameObject.SetActive(false);
            mundo = Mundo.Construir(); fp = new PrimeiraPessoa(cam);
            try { pos = new Pos(cam); } catch (System.Exception e) { Debug.LogWarning("Ritual Reversal: sem pós-processamento (" + e.Message + ")"); }
            som = Som.Criar(cam.gameObject); som.volume = PlayerPrefs.GetFloat("rr-vol", .8f); fp.fovBase = PlayerPrefs.GetFloat("rr-fov", 72); mundo.AoGritarCorvo = (x, z) => { som.Tocar("corvo", x, z); Legenda("corvo", x, z); };
            // v5.8: impactos e momentos do ritual tocam pelo Som; a arma avisa o meio do ciclo (alavanca, cão) e o passo
            mundo.AoSom = (k, x, z) => som.Tocar(k, x, z); mundo.AoTremor = f => fp.Tranco(.01f * f, f);
            fp.AoPasso = () => som.Tocar("mystep"); fp.camFx = PlayerPrefs.GetFloat("rr-camfx", 1);
            // primeira pessoa (ArmaFP): os sons dos clipes são gravações; sem a gravação, a síntese do Som
            fp.arma.AoTocar = (k, vol, max, varia) => { if (!som.Amostra(k, vol: vol, max: max, varia: varia)) som.Tocar(k); return true; };
            fp.arma.raizMundo = mundo.raiz; mundo.AoExecutar = E => fp.Executou();
        }

        void LimparVisual() { if (mundo != null) mundo.Limpar(); if (som != null) som.Silenciar(); }
        void OnDestroy() { if (pos != null) pos.Destruir(); }

        // posição da câmera nas coordenadas do protótipo
        Vector3 CamP() { var p = cam.transform.position; return new Vector3(p.x, p.y, -p.z); }

        void AtualizarVisual(float dt)
        {
            if (mundo == null) return;
            float agora = Time.time; var m = Eu(); string papel = MeuPapel(); bool jogando = g != null && g.phase == "play" && m != null;
            if (jogando)
            {
                bool correndo = ultimaEntrada != null && ultimaEntrada.sp && ultimaEntrada.mz < 0 && m.moving && m.st == "alive";
                fp.Atualizar(m, g, (float)m.x, (float)m.z, m.moving, ultimaEntrada != null ? (float)ultimaEntrada.mx : 0, ultimaEntrada != null ? (float)ultimaEntrada.mz : 0, correndo, (float)lookYaw, (float)lookPitch, dt, agora);
                EstadoProprio(m); if (m.st == "dead") Espectar(m, dt); else { espAlvo = null; espPos = null; }
                CameraArsenal();
            }
            else
            {
                fp.Esconder(dt); float a = agora * .04f;
                cam.transform.position = new Vector3(6 + Mathf.Sin(a) * 17, 3.4f, -(-4 + Mathf.Cos(a) * 13)); cam.transform.LookAt(new Vector3(6, 4.5f, 4)); cam.transform.Rotate(0, .28f * Mathf.Rad2Deg, 0, Space.Self);
                cam.fieldOfView = 72;
            }
            var c = CamP(); float yawCam = jogando ? (float)lookYaw : -cam.transform.eulerAngles.y * Mathf.Deg2Rad;
            mundo.Atualizar(g, papel, m, c, yawCam, dt);
            mundo.MostrarPartida(jogando);
            if (jogando) { mundo.SyncActors(g, papel, m, c, (float)lookYaw, dt); mundo.SyncNoite(g, c, dt); LegendasDosRituais(papel); }
            mundo.AtualizarEfeitosQuadro(dt);
            // som: ouvinte, rituais, passos, alucinações, coração, sino
            som.ouvinte = c; som.yawOuvinte = yawCam; som.Rituais(jogando ? g : null, papel, mundo.GRk, dt);
            som.Ambiente(jogando, c, dt); // v6: fundos por lugar
            if (jogando)
            {
                float s = (float)m.sanity;
                // (o passo do jogador toca no ciclo do balanço da câmera: fp.AoPasso)
                ProgressoInvestigacao(papel);
                if (s < 40) { alucT -= dt; if (alucT <= 0) { alucT = Random.Range(6f, 12f); float an = Random.Range(0, Mathf.PI * 2); if (Random.value < .5f) som.Tocar("whisper", (float)m.x + Mathf.Cos(an) * 6, (float)m.z + Mathf.Sin(an) * 6, 1); else som.Tocar("alucinacao"); } }
                if (s < 15) { coracaoT -= dt; if (coracaoT <= 0) { coracaoT = .85f; som.Tocar("heart"); } }
                {
                    var E = m.st == "down" ? mundo.ExecRecente(m.id) : null; veuExe = Mathf.Lerp(veuExe, E != null ? .3f + E.k * .7f : 0, Mathf.Min(1, dt * 6));
                    if (E != null)
                    {
                        coracaoT -= dt; if (coracaoT <= 0) { coracaoT = .95f - .62f * E.k; som.Tocar("heart"); }
                        if (exeVirou != E.por) { foreach (var p in g.actors) if (p.id == E.por && !p.hidden) { virarYaw = Mathf.Atan2(-(float)(p.x - m.x), -(float)(p.z - m.z)); virarT = .6f; exeVirou = E.por; } }
                    }
                    else if (m.st != "down") exeVirou = -1;
                    // a câmera vira devagar para quem executa (só caído; o jogador pode olhar de novo depois)
                    if (virarT > 0) { if (m.st != "down") virarT = 0; else { virarT -= dt; float d = virarYaw - (float)lookYaw; d = Mathf.Atan2(Mathf.Sin(d), Mathf.Cos(d)); lookYaw += d * Mathf.Min(1, dt * 7); if (Mathf.Abs(d) < .02f) virarT = 0; } }
                }
                sinoT -= dt; if (sinoT <= 0) { sinoT = Random.Range(45f, 80f); som.Tocar("bell"); }
                if (pos != null) pos.Atualizar(s, mundo.GRk, Mathf.Clamp01(hurtT / .35f), Mathf.Clamp01(blindT / 1.5f));
            }
            else { if (pos != null) pos.Atualizar(100, 0, 0, 0); veuExe = 0; }
            if (pos != null) pos.Grade(cam, g != null ? g.momento : 0, mundo.GRk, 1, dt, agora);
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
                // a cadência é a do servidor (antes o Unity previa um tiro a cada 0,3 s, e um em cada quatro não existia)
                bool exo = m.cls == "exorcista"; localCd = (float)Sim.CADENCIA_HUMANA; som.Tiro(exo, c.x, c.z, true); fp.Tiro(exo);
            }
            else { if (m.fervor < CFG.sigilCusto) { localCd = .35f; som.Tocar("vazio"); return; } localCd = (float)Sim.CADENCIA_HUMANA; som.Tocar("sigil", c.x, c.z); fp.Sigilo(); }
        }

        // eventos da simulação que viram efeito e som (handleEvents do protótipo)
        void EventoAudiovisual(Evento e, string papel, int eu)
        {
            if (e.type == "presage" && papel != "H") return;
            if ((e.type == "sensor" || e.type == "evp" || e.type == "blind" || e.type == "stun") && e.Int("a") != eu) return;
            if (e["to"] != null && e.Int("to") != eu) return;
            if (mundo != null) mundo.EventoVisual(e, eu, fp.BocaMundo(mundo.raiz));
            LegendaDoEvento(e, papel, eu);
            float x = (float)e.Num("x"), z = (float)e.Num("z");
            switch (e.type)
            {
                case "sfx":
                    {
                        string k = e.Str("k"); if (e.Int("a") == eu && (k == "shot" || k == "sigil" || k == "step" || k == "passoForte")) break;
                        if (k == "shot") { var at = g.actors.Find(a => a.id == e.Int("a")); som.Tiro(at != null && at.cls == "exorcista", x, z, false); break; }
                        som.Tocar(k, x, z); break;
                    }
                case "tracer": if (e.Int("a") == eu) ultimoTiroCab = e["cab"] is bool && (bool)e["cab"]; break;
                case "hit":
                    if (e.Int("a") == eu)
                    {
                        // acerto confirmado pelo servidor: marca, som e pausa de impacto (o tiro previsto não mostra nada disso)
                        bool derrubou = e["derrubou"] is bool && (bool)e["derrubou"], escudo = e["escudo"] is bool && (bool)e["escudo"];
                        marcaAcerto = escudo ? "escudo" : derrubou ? "kill" : ultimoTiroCab ? "head" : "";
                        hitT = derrubou ? .45f : ultimoTiroCab ? .3f : .15f; som.Tocar(escudo ? "bloqueio" : derrubou ? "derrubou" : ultimoTiroCab ? "cabeca" : "hit");
                        fp.Acerto(escudo, derrubou, ultimoTiroCab); mundo.PausaImpacto(e.Int("alvo"), derrubou ? .09f : .05f); ultimoTiroCab = false;
                    }
                    break;
                case "hurt":
                    if (e.Int("a") == eu)
                    {
                        som.Tocar("hurt"); float lado = 0;
                        RegistrarGolpe(e);
                        if (e["sx"] != null) { float sx = (float)e.Num("sx"), sz = (float)e.Num("sz"); DirecaoDano(sx, sz); var cp = CamP(); lado = Mathf.Sign((sx - cp.x) * Mathf.Cos((float)lookYaw) - (sz - cp.z) * Mathf.Sin((float)lookYaw)); }
                        fp.Golpe(lado);
                    }
                    break;
                case "burst": som.Tocar("estouro", (float)e.Num("x"), (float)e.Num("z")); break;
                case "reload":
                    {
                        // o revólver do Exorcista arma o cão no fim da recarga
                        var at = g.actors.Find(a => a.id == e.Int("a")); float v = at != null && at.cls == "exorcista" ? 1 : 0;
                        if (e.Int("a") != eu) som.Tocar("reload", x, z, v); break;   // a própria recarga soa pelos eventos do clipe (ArmaFP)
                    }
                case "presage": som.Tocar("presage"); break;
                case "selando":
                    if (papel == "C") { int ai = e.Int("altar"); var A = ai >= 0 && ai < g.altars.Count ? g.altars[ai] : null; som.Tocar("selando", x, z); Legenda("selando", x, z, "Selando o ritual em " + (A != null ? A.name : "um altar"), "cult", 400); LT.Marco(g, "eco", "A caça começou a selar " + (A != null ? A.name : "")); }
                    break;
                case "eco":
                    {
                        // v5.5: o coro ecoa em dois altares do setor; se a equipe já descartou um, a dedução aponta o outro
                        var L = new List<Altar>(); var la = e["altares"] as System.Collections.IList; if (la != null) foreach (var o in la) { int i = System.Convert.ToInt32(o); if (i >= 0 && i < g.altars.Count) L.Add(g.altars[i]); }
                        LT.Marco(g, "eco", "Eco do coro");
                        if (L.Count == 0) break; float ex = (float)L.Average(A => A.x), ez = (float)L.Average(A => A.z);
                        som.Tocar("eco", ex, ez); Legenda("eco", ex, ez, "Coro distante: " + string.Join(" ou ", L.Select(A => A.name).ToArray()), "rit", 400);
                        var poss = papel == "H" && g.know != null ? L.Where(A => AltarPossivel(A.i)).ToList() : L;
                        if (poss.Count == 1 && L.Count == 2) { var fora = L.First(A => A != poss[0]); Anunciar("Dedução: " + fora.name + " foi descartado, o ritual deve estar em " + poss[0].name, 4); som.Tocar("sealed"); Feed("Dedução: " + fora.name + " foi descartado, então o eco aponta " + poss[0].name + ".", "h"); }
                        else Anunciar("O coro ecoa: " + string.Join(" ou ", L.Select(A => A.name).ToArray()), 3.5f);
                        break;
                    }
                case "sensor": som.Tocar("ping", float.NaN, float.NaN, (float)e.Num("v")); break;
                case "evp": som.Tocar("whisper", x, z); som.Tocar("evp", x, z); mundo.RevelarEVP(x, z); break;
                case "blind": NotaHab("Você foi cegado", new Color(1, .96f, .82f)); break;
                case "stun": atordT = 1; NotaHab("Você foi atordoado", OURO); fp.kickP += .03f; break;
                case "momento": som.Tocar("bell"); LT.Marco(g, "momento", e.Str("nome")); break;
                case "grande":
                    som.Tocar("doom"); som.Tocar("sinoGrande"); StartCoroutine(Depois(2.3f, () => som.Tocar("sinoGrande"))); StartCoroutine(Depois(4.6f, () => som.Tocar("sinoGrande")));
                    mundo.EspantarCorvos(); claraoGrande = 1.2f; fp.kickP += .05f;
                    { int ai = e.Int("altar"); Anunciar("O Grande Ritual" + (ai >= 0 && ai < g.altars.Count ? ": " + g.altars[ai].name + ", a catedral inteira ouve" : ""), 4.5f); }
                    break;
                case "hab":
                    {
                        Ator quem = g.actors.Find(a => a.id == e.Int("a")); som.Hab(quem != null ? quem.cls : "", x, z);
                        if (e.Int("a") == eu) { var al = e["alvos"] as System.Collections.ICollection; int n = al != null ? al.Count : 0; string f = e.Str("f"); NotaHab(TextoHab(f, n), n > 0 || f == "runa" || f == "veu" || f == "sal" ? OURO : CINZA * 2); if (f == "empurrao") fp.kickP += .02f; fp.Hab(); }
                        break;
                    }
                case "ping": if (e.Str("team") == papel) ReceberPing(e); break;
                case "fala":
                    if (e.Str("team") == papel)
                    {
                        string[] falas = { "Preciso de ajuda!", "Estou indo", "Recuar!", "Inimigo aqui!", "Obrigado", papel == "H" ? "Vou purgar este altar" : "Vou iniciar o ritual" };
                        int f = e.Int("f"); var quem = g.actors.Find(q => q.id == e.Int("a")); if (f >= 0 && f < falas.Length) Feed((quem != null ? quem.name : "Aliado") + ": " + falas[f], papel == "H" ? "h" : "c");
                    }
                    break;
                case "doom":
                case "sealed":
                    {
                        // v5.7: o fim do ritual com nomes
                        som.Tocar(e.type); var por = Nomes(e["por"]); int ai = e.Int("altar"); string alt = ai >= 0 && ai < g.altars.Count ? g.altars[ai].name : "";
                        if (ai >= 0 && ai < g.altars.Count) { if (e.type == "doom") mundo.RitualFenda(ai); else mundo.RitualSelado(ai); }
                        var m = g.actors.Find(a => a.id == eu); bool meu = m != null && por.Contains(m.name), fenda = e.type == "doom";
                        string tit = fenda ? (meu ? "Você abriu uma Fenda" : "Uma Fenda se abriu") : (meu ? "Você selou o ritual" : "Um Farol se acendeu");
                        string sub = por.Count == 0 ? alt : ListaNomes(por) + (fenda ? (por.Count > 1 ? " completaram o ritual" : " completou o ritual") + (alt != "" ? " em " + alt : "") : (por.Count > 1 ? " selaram " : " selou ") + alt);
                        Anunciar(tit + (sub != "" ? ": " + sub : ""), 4.5f); break;
                    }
                case "interrupcao":
                    {
                        // v5.7: interrupção que se sente (quem fez, quem sofreu, a equipe)
                        if (e.Str("team") != null && e.Str("team") != papel) break;
                        bool selo = e.Str("tipo") == "selo"; var aut = g.actors.Find(a => a.id == e.Int("a")); var vit = g.actors.Find(a => a.id == e.Int("alvo"));
                        { int ai = e.Int("altar"); if (ai >= 0 && ai < g.altars.Count) mundo.RitualInterrompido(ai, e.Str("tipo")); }
                        if (e.Int("a") == eu) { NotaHab(selo ? "Selo interrompido!" : "Canalização interrompida!", selo ? CARMIM_VIVO : OURO); som.Tocar("derrubou"); marcaAcerto = "kill"; hitT = .45f; fp.Tranco(.025f, .8f); }
                        else if (e.Int("alvo") == eu) { NotaHab((aut != null ? aut.name : "Alguém") + " interrompeu " + (selo ? "seu selo" : "sua canalização"), Color.white); som.Tocar("hurt"); }
                        else if (aut != null && vit != null) Feed(aut.name + " interrompeu " + (selo ? "o selo" : "a canalização") + " de " + vit.name + ".", aut.team == "H" ? "h" : "c");
                        break;
                    }
                case "lance":
                    {
                        if (e.Str("team") != null && e.Str("team") != papel) break;
                        MostrarLance(e);   // faixa do lance (PartidaInterface.cs)
                        break;
                    }
            }
        }
        bool AltarPossivel(int i) { string k = g.know != null && i < g.know.Length ? g.know[i] : null, st = g.altars[i].state; return k != "descartado" && k != "chamariz" && st != "fenda" && st != "farol"; }
        // investigação com progresso: quando um sinal descarta um altar, a caça vê quantos ainda podem ser do culto;
        // resíduo de reagente toca a descoberta; o Chamariz desmascarado se desfaz em cinza
        string[] knowAnt; string knowPartida;
        void ProgressoInvestigacao(string papel)
        {
            if (papel != "H" || g.know == null) { knowAnt = null; return; }
            string chave = g.matchId + ":" + g.round; if (chave != knowPartida) { knowPartida = chave; knowAnt = null; }
            if (knowAnt != null && knowAnt.Length == g.know.Length)
                for (int i = 0; i < g.know.Length; i++)
                {
                    string k = g.know[i]; if (k == knowAnt[i]) continue; var A = g.altars[i];
                    if (k == "descartado")
                    {
                        int n = 0; for (int j = 0; j < g.altars.Count; j++) { string kj = g.know[j], sj = g.altars[j].state; if (kj != "descartado" && kj != "chamariz" && sj != "fenda" && sj != "farol") n++; }
                        NotaHab(A.name + " descartado · " + n + (n == 1 ? " altar ainda pode" : " altares ainda podem") + " ser do culto", new Color(.81f, .89f, 1)); som.Tocar("descoberta");
                        mundo.MarcaInvestigacao(i, k);
                    }
                    else if (k == "confirmado") { som.Tocar("descoberta", float.NaN, float.NaN, 1); mundo.MarcaInvestigacao(i, k); }
                    else if (k == "chamariz") { som.Tocar("chamariz_desfeito", (float)A.x, (float)A.z); mundo.MarcaInvestigacao(i, k); }
                }
            knowAnt = (string[])g.know.Clone();
        }
        static List<string> Nomes(object v) { var L = new List<string>(); var en = v as System.Collections.IEnumerable; if (en != null && !(v is string)) foreach (var x in en) if (x != null) L.Add(x.ToString()); return L; }
        static string ListaNomes(List<string> L) { return Noite.ListaNomes(L); }
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

        // ---------- ping de equipe ----------
        class PingV { public int a; public float x, z, t; public string txt, nome; }
        readonly List<PingV> pings = new List<PingV>();
        // mira: inimigo à vista, altar ou ponto no chão (até a parede), como no protótipo
        void Pingar(Ator m)
        {
            if (m == null || m.st == "dead" || g == null || g.phase != "play") return;
            var c = CamP(); float yaw = (float)lookYaw, pitch = (float)lookPitch, cy = Mathf.Cos(pitch);
            var D = new Vector3(-Mathf.Sin(yaw) * cy, Mathf.Sin(pitch), -Mathf.Cos(yaw) * cy);
            System.Func<float, float, float, float, float, float> mira = (x, y, z, max, cos) => { var d = new Vector3(x - c.x, y - c.y, z - c.z); float L = d.magnitude; return L < max && L > .5f && Vector3.Dot(d, D) / L > cos ? L : -1; };
            Acao a = null; float bd = 1e9f;
            foreach (var e in g.actors) { if (e.team == m.team || e.hidden || e.st == "dead") continue; float d = mira((float)e.x, e.st == "down" ? .4f : 1.2f, (float)e.z, 75, .994f); if (d > 0 && d < bd && Mapa.SegClear(c.x, c.z, e.x, e.z, 0, Mapa.GT)) { bd = d; a = new Acao { type = "ping", k = "inimigo", x = e.x, z = e.z, i = -1 }; } }
            if (a == null) foreach (var A in g.altars) { float d = mira((float)A.x, 1, (float)A.z, 95, .985f); if (d > 0 && d < bd) { bd = d; a = new Acao { type = "ping", k = "altar", x = A.x, z = A.z, i = A.i }; } }
            if (a == null)
            {
                float h = D.y < -.01f ? Mathf.Min(60, c.y / -D.y * cy) : 60, ux = D.x / Mathf.Max(cy, 1e-3f), uz = D.z / Mathf.Max(cy, 1e-3f);
                if (!Mapa.SegClear(c.x, c.z, c.x + ux * h, c.z + uz * h, 0, Mapa.GT)) { float lo = 0, hi = h; for (int k = 0; k < 12; k++) { float mid = (lo + hi) / 2; if (Mapa.SegClear(c.x, c.z, c.x + ux * mid, c.z + uz * mid, 0, Mapa.GT)) lo = mid; else hi = mid; } h = Mathf.Max(0, lo - .4f); }
                a = new Acao { type = "ping", k = "ponto", x = System.Math.Round(c.x + ux * h, 2), z = System.Math.Round(c.z + uz * h, 2), i = -1 };
            }
            Acao(a);
        }
        void ReceberPing(Evento e)
        {
            int quem = e.Int("a"); var ator = g.actors.Find(x => x.id == quem); string k = e.Str("k"); int i = e.Int("i");
            string txt = k == "altar" && i >= 0 && i < g.altars.Count ? "Altar " + g.altars[i].name : k == "inimigo" ? "Inimigo visto" : "Aqui";
            pings.RemoveAll(p => p.a == quem); pings.Add(new PingV { a = quem, x = (float)e.Num("x"), z = (float)e.Num("z"), t = k == "inimigo" ? 6 : 10, txt = txt, nome = ator != null ? ator.name : "Aliado" });
            Feed((ator != null ? ator.name : "Aliado") + " marcou: " + txt, MeuPapel() == "H" ? "h" : "c"); som.Tom(float.NaN, float.NaN, 1, 880, 990, .1f, Som.Onda.Seno, .18f); som.Tom(float.NaN, float.NaN, 1, 1320, 1320, .12f, Som.Onda.Seno, .12f, .09f);
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
