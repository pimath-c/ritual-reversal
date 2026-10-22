// Partida local contra bots: roda a simulação (Scripts/Simulacao, porte fiel do shared/sim.js) a cada quadro,
// como o modo "Jogar sozinho" do protótipo, lê teclado e mouse, move a câmera e repassa os eventos para os efeitos e o HUD.
// Fica num objeto "Partida" que o importador do mapa cria. Visual em PartidaVisual.cs, telas e HUD em PartidaHud.cs.
using System.Collections.Generic;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal : MonoBehaviour
    {
        [Tooltip("Sensibilidade do mouse (1 = a do protótipo)")]
        public float sensibilidade = 1f;

        string EU = "local"; // cid do jogador: "local" contra bots, o que o servidor mandar no online
        Jogo g;
        Camera cam;
        double lookYaw, lookPitch;
        string faseAnterior = "";
        string lojaAberta; // id do mercador com a loja aberta
        readonly Controles botoes = new Controles(); // contadores de Q, F, G, R e habilidade, como no protótipo

        void Awake()
        {
            RitualReversal.Visual.Qualidade.Escolher(PlayerPrefs.GetInt("rr-qualidade", 3));
            if (!Mapa.Carregado)
            {
                var dados = Resources.Load<TextAsset>("simulacao");
                if (dados == null) { Debug.LogError("Ritual Reversal: falta Resources/simulacao.json (rode node tools/exportar-mapa.js)."); enabled = false; return; }
                Sim.Iniciar(dados.text);
            }
            // no Awake, antes de qualquer Start: o jogador livre do blockout não chega a travar o mouse nem a se mover.
            // Na partida quem manda na posição é a simulação.
            cam = Camera.main;
            if (cam == null) { var c = new GameObject("Câmera da partida"); c.tag = "MainCamera"; cam = c.AddComponent<Camera>(); c.AddComponent<AudioListener>(); }
            cam.nearClipPlane = .05f; cam.fieldOfView = 72f;
            var livre = cam.GetComponentInParent<JogadorFPS>(); if (livre != null) livre.enabled = false;
            var cc = cam.GetComponentInParent<CharacterController>(); if (cc != null) cc.enabled = false;
        }

        void Start() { CarregarPreferencias(); CriarVisual(); Travar(false); }

        public void NovaPartida(bool cacador)
        {
            var slots = cacador
                ? new List<Slot> { new Slot("A", EU, "Você"), new Slot("A", null, "Irmã Beatriz"), new Slot("B", null, "Irmão Cinza"), new Slot("B", null, "Madre Vesper") }
                : new List<Slot> { new Slot("A", null, "Irmã Beatriz"), new Slot("A", null, "Irmão Tomé"), new Slot("B", EU, "Você"), new Slot("B", null, "Madre Vesper") };
            if (online) SairOnline(""); EU = "local";
            g = Sim.CreateGame(new OpcoesJogo { timers = false, slots = slots, matchId = System.DateTime.Now.ToString("yyyyMMdd-HHmmss"), dificuldade = dificuldade });
            faseAnterior = ""; lojaAberta = null; feed.Clear(); LimparVisual();
        }

        Ator Eu() { if (g == null) return null; foreach (var a in g.actors) if (a.cid == EU) return a; return null; }
        Slot MeuSlot() { if (g == null) return null; foreach (var s in g.slots) if (s.cid == EU) return s; return null; }
        string MeuPapel() { var s = MeuSlot(); return s != null ? Sim.RoleOf(g, s.team) : "H"; }
        static bool Travado() { return Cursor.lockState == CursorLockMode.Locked; }
        static void Travar(bool sim) { Cursor.lockState = sim ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !sim; }
        public void Acao(Acao a) { if (online) EnviarAcao(a); else if (g != null) Sim.Act(g, EU, a); }

        void Update()
        {
            double raw = Mathf.Max(.001f, Time.unscaledDeltaTime), dt = System.Math.Min(.05, raw);
            if (online) ProcessarRede();
            if (g == null) { AtualizarVisual((float)System.Math.Min(.25, raw)); return; }
            if (arsenal) { PassoArsenal(dt); ultimaEntrada = null; g.events.Clear(); AtualizarVisual((float)System.Math.Min(.25, raw)); return; }   // Arsenal: a noite fica parada
            var m = Eu();
            bool jogando = g.phase == "play";
            bool ativo = jogando && Travado() && lojaAberta == null;
            if (jogando) LerAtalhos(m, ativo);
            var inp = LerEntrada(ativo);
            bool pausado = jogando && !Travado() && lojaAberta == null;
            ultimaEntrada = inp;
            if (online) { inp.viewT = ServerNow() - .1; PassoOnline(inp, dt); m = Eu(); if (m != null && jogando) PreverTiro(m, (float)dt, inp.fire); }
            else if (m != null && jogando && !pausado) { Sim.MoveHuman(g, m, inp, dt); PreverTiro(m, (float)dt, inp.fire); Sim.Step(g, dt); }
            else if (!jogando) Sim.Step(g, dt);
            var evs = new List<Evento>(g.events); g.events.Clear(); TratarEventos(evs, Eu());
            PassoNoite((float)raw);

            if (g.phase != faseAnterior)
            {
                if (g.phase == "play")
                {
                    var eu = Eu(); if (eu != null) { lookYaw = eu.yaw; lookPitch = 0; } Travar(true);
                    // o clima da rodada, se não for noite limpa
                    var CL = Visual.Mundo.CLIMAS[Visual.Mundo.ClimaDaRodada(g)]; if (CL.nome != "Noite limpa") StartCoroutine(Depois(.9f, () => { if (g != null && g.phase == "play") Anunciar(CL.nome, CL.sub); }));
                }
                else { Travar(false); lojaAberta = null; }
                faseAnterior = g.phase;
            }
            if (lojaAberta != null && (m == null || m.st != "alive" || Distancia(m, Mapa.NPCS.Find(n => n.id == lojaAberta)) > 3.4)) lojaAberta = null;
            AtualizarVisual((float)System.Math.Min(.25, raw));
        }

        static double Distancia(Ponto a, Ponto b) { return b == null ? 1e9 : JS.Hypot(a.x - b.x, a.z - b.z); }

        // teclas de toque único: Q sensor, F lanterna, G sinalizador, R recarregar, botão direito habilidade, E na loja
        void LerAtalhos(Ator m, bool ativo)
        {
            if (lojaAberta != null && (EntradaUnity.Apertou(Tecla.Esc) || EntradaUnity.Apertou(Tecla.E))) { lojaAberta = null; Travar(true); return; }
            if (!ativo || m == null) return;
            if (EntradaUnity.Apertou(Tecla.Esc)) { Travar(false); return; } // no editor o próprio Unity já solta o mouse; no jogo compilado, é aqui
            if (EntradaUnity.Apertou(Tecla.Q)) botoes.q++;
            if (EntradaUnity.Apertou(Tecla.F)) botoes.f++;
            if (EntradaUnity.Apertou(Tecla.G)) botoes.g++;
            if (EntradaUnity.Apertou(Tecla.R)) botoes.r++;
            if (EntradaUnity.Apertou(Tecla.Z)) Pingar(m);
            if (EntradaUnity.Apertou(Tecla.I) && fp != null) fp.PedirInspecao(); // inspeção da arma (v5.9)
            if (EntradaUnity.Apertou(Tecla.J)) cadernoAberto = !cadernoAberto;   // caderno dos altares (v5.3)
            if (EntradaUnity.CliqueDir()) { botoes.ab++; if (m.st == "alive" && m.abilCd > 0) { FeiticoDef F; Regras.FEITICOS.TryGetValue(m.feitico ?? "", out F); NotaHab((F != null ? F.nome : "Feitiço") + " recarregando: " + Mathf.CeilToInt((float)m.abilCd) + " s", MUDO); som.Tocar("vazio"); } }
            if (EntradaUnity.Apertou(Tecla.E) && m.st == "alive")
            {
                var ctx = Sim.Contexts(g, m);
                if (ctx.e != null && ctx.e.type == "loja") { lojaAberta = ctx.e.npc; Travar(false); }
            }
        }

        Entrada LerEntrada(bool ativo)
        {
            double mx = 0, mz = 0;
            if (ativo)
            {
                var d = EntradaUnity.Mouse(); if (fp != null) fp.Mouse(d.x, -d.y);
                lookYaw -= d.x * .0022 * sensibilidade; lookPitch = JS.Clamp(lookPitch + d.y * .0022 * sensibilidade, -1.45, 1.45);
                if (EntradaUnity.Segurando(Tecla.W)) mz -= 1; if (EntradaUnity.Segurando(Tecla.S)) mz += 1;
                if (EntradaUnity.Segurando(Tecla.A)) mx -= 1; if (EntradaUnity.Segurando(Tecla.D)) mx += 1;
            }
            return new Entrada
            {
                mx = mx, mz = mz, sp = ativo && EntradaUnity.Segurando(Tecla.Shift), ag = ativo && EntradaUnity.Segurando(Tecla.C), yaw = lookYaw, pitch = lookPitch,
                fire = ativo && EntradaUnity.Atirando(), use = ativo && EntradaUnity.Segurando(Tecla.E), use2 = ativo && EntradaUnity.Segurando(Tecla.T),
                c = botoes.Copia(), viewT = g.t
            };
        }

        // ---------- eventos da simulação ----------
        class Aviso { public string msg, cls; public float t; }
        readonly List<Aviso> feed = new List<Aviso>();
        string anuncio, anuncioSub = ""; float anuncioT, hurtT, hitT, blindT, sensorT; double sensorV;

        void Feed(string msg, string cls) { feed.Add(new Aviso { msg = msg, cls = cls ?? "", t = Time.unscaledTime }); if (feed.Count > 7) feed.RemoveAt(0); }
        // o #anuncio do navegador: título grande, a linha de baixo menor, 4,5 s
        void Anunciar(string titulo, string sub = "") { anuncio = titulo; anuncioSub = sub ?? ""; anuncioT = 4.5f; }

        void TratarEventos(List<Evento> evs, Ator m)
        {
            string papel = MeuPapel(); int eu = m != null ? m.id : -1;
            foreach (var e in evs)
            {
                switch (e.type)
                {
                    case "feed":
                        {
                            string team = e.Str("team"); int to = e.Int("to");
                            if (team != null && team != papel) break; if (to >= 0 && to != eu) break;
                            string cls = e.Str("cls") ?? "", msg = e.Str("msg") ?? "";
                            // o reagente coletado ganha o nome do que foi colhido (o mais perto de quem coletou)
                            if (msg.StartsWith("Reagente coletado") && m != null && Mapa.REAG.Count > 0) { int ri = 0; for (int j = 1; j < Mapa.REAG.Count; j++) if (Sim.Dist(Mapa.REAG[j], m) < Sim.Dist(Mapa.REAG[ri], m)) ri = j; msg = "Coletado: " + NomeReagente(ri) + msg.Substring("Reagente coletado".Length); }
                            Feed(msg, cls); AnotarCaderno(msg);
                            break;
                        }
                    case "hurt": if (e.Int("a") == eu) hurtT = .35f; break;
                    case "blind": if (e.Int("a") == eu) blindT = 1.5f; break;
                    case "sensor": if (e.Int("a") == eu) { sensorV = e.Num("v"); sensorT = 4f; } break;
                    case "momento": Anunciar(e.Str("nome"), e.Int("i") == 1 ? "Os altares despertam" : "Os rituais aceleram e a névoa se fecha"); break;
                }
                EventoAudiovisual(e, papel, eu);
            }
        }
    }
}
