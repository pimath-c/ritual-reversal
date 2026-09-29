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

        public const string EU = "local";
        Jogo g;
        Camera cam;
        double lookYaw, lookPitch;
        string faseAnterior = "";
        string lojaAberta; // id do mercador com a loja aberta
        readonly Controles botoes = new Controles(); // contadores de Q, F, G, R e habilidade, como no protótipo

        void Awake()
        {
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

        void Start() { CriarVisual(); Travar(false); }

        public void NovaPartida(bool cacador)
        {
            var slots = cacador
                ? new List<Slot> { new Slot("A", EU, "Você"), new Slot("A", null, "Irmã Beatriz"), new Slot("B", null, "Irmão Cinza"), new Slot("B", null, "Madre Vesper") }
                : new List<Slot> { new Slot("A", null, "Irmã Beatriz"), new Slot("A", null, "Irmão Tomé"), new Slot("B", EU, "Você"), new Slot("B", null, "Madre Vesper") };
            g = Sim.CreateGame(new OpcoesJogo { timers = false, slots = slots, matchId = System.DateTime.Now.ToString("yyyyMMdd-HHmmss") });
            faseAnterior = ""; lojaAberta = null; feed.Clear(); LimparVisual();
        }

        Ator Eu() { if (g == null) return null; foreach (var a in g.actors) if (a.cid == EU) return a; return null; }
        Slot MeuSlot() { if (g == null) return null; foreach (var s in g.slots) if (s.cid == EU) return s; return null; }
        string MeuPapel() { var s = MeuSlot(); return s != null ? Sim.RoleOf(g, s.team) : "H"; }
        static bool Travado() { return Cursor.lockState == CursorLockMode.Locked; }
        static void Travar(bool sim) { Cursor.lockState = sim ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !sim; }
        public void Acao(Acao a) { if (g != null) Sim.Act(g, EU, a); }

        void Update()
        {
            if (g == null) return;
            double raw = Mathf.Max(.001f, Time.unscaledDeltaTime), dt = System.Math.Min(.05, raw);
            var m = Eu();
            bool jogando = g.phase == "play";
            bool ativo = jogando && Travado() && lojaAberta == null;
            if (jogando) LerAtalhos(m, ativo);
            var inp = LerEntrada(ativo);
            bool pausado = jogando && !Travado() && lojaAberta == null;
            if (m != null && jogando && !pausado) { Sim.MoveHuman(g, m, inp, dt); Sim.Step(g, dt); }
            else if (!jogando) Sim.Step(g, dt);
            var evs = new List<Evento>(g.events); g.events.Clear(); TratarEventos(evs, Eu());

            if (g.phase != faseAnterior)
            {
                if (g.phase == "play") { var eu = Eu(); if (eu != null) { lookYaw = eu.yaw; lookPitch = 0; } Travar(true); }
                else { Travar(false); lojaAberta = null; }
                faseAnterior = g.phase;
            }
            if (lojaAberta != null && (m == null || m.st != "alive" || Distancia(m, Mapa.NPCS.Find(n => n.id == lojaAberta)) > 3.4)) lojaAberta = null;
            AtualizarVisual((float)raw);
            AtualizarCamera(Eu());
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
            if (EntradaUnity.CliqueDir()) { botoes.ab++; if (m.st == "alive" && m.abilCd > 0) Anunciar("Habilidade recarregando: " + Mathf.CeilToInt((float)m.abilCd) + " s", .9f); }
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
                var d = EntradaUnity.Mouse();
                lookYaw -= d.x * .0022 * sensibilidade; lookPitch = JS.Clamp(lookPitch + d.y * .0022 * sensibilidade, -1.45, 1.45);
                if (EntradaUnity.Segurando(Tecla.W)) mz -= 1; if (EntradaUnity.Segurando(Tecla.S)) mz += 1;
                if (EntradaUnity.Segurando(Tecla.A)) mx -= 1; if (EntradaUnity.Segurando(Tecla.D)) mx += 1;
            }
            return new Entrada
            {
                mx = mx, mz = mz, sp = ativo && EntradaUnity.Segurando(Tecla.Shift), yaw = lookYaw, pitch = lookPitch,
                fire = ativo && EntradaUnity.Atirando(), use = ativo && EntradaUnity.Segurando(Tecla.E), use2 = ativo && EntradaUnity.Segurando(Tecla.T),
                c = botoes.Copia(), viewT = g.t
            };
        }

        void AtualizarCamera(Ator m)
        {
            if (cam == null || m == null) return;
            float olho = m.st == "down" ? .55f : 1.62f;
            cam.transform.position = new Vector3((float)m.x, olho, (float)-m.z);
            cam.transform.rotation = Quaternion.Euler((float)(-lookPitch * Mathf.Rad2Deg), (float)(-lookYaw * Mathf.Rad2Deg), 0f);
        }

        // ---------- eventos da simulação ----------
        class Aviso { public string msg, cls; public float t; }
        readonly List<Aviso> feed = new List<Aviso>();
        string anuncio; float anuncioT, hurtT, hitT, blindT, sensorT; double sensorV;

        void Feed(string msg, string cls) { feed.Add(new Aviso { msg = msg, cls = cls ?? "", t = Time.unscaledTime }); if (feed.Count > 7) feed.RemoveAt(0); }
        void Anunciar(string msg, float s = 3f) { anuncio = msg; anuncioT = s; }

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
                            string cls = e.Str("cls") ?? ""; Feed(e.Str("msg"), cls); if (cls.Contains("big")) Anunciar(e.Str("msg"));
                            break;
                        }
                    case "tracer": Tracer(e); break;
                    case "hit": if (e.Int("a") == eu) hitT = .15f; break;
                    case "hurt": if (e.Int("a") == eu) hurtT = .35f; break;
                    case "blind": if (e.Int("a") == eu) blindT = 1.2f; break;
                    case "stun": if (e.Int("a") == eu) Anunciar("Atordoado!", 1f); break;
                    case "sensor": if (e.Int("a") == eu) { sensorV = e.Num("v"); sensorT = 4f; } break;
                    case "burst": Estouro(e); break;
                    case "momento": Anunciar(e.Str("nome"), 4f); break;
                }
            }
        }
    }
}
