// Teste de fumaça das telas do Unity (tools/unity-telas.js): roda o PartidaLocal de verdade (Update e OnGUI) contra o
// UnityEngine e o IMGUI de mentira e percorre o jogo: título e menus, uma noite inteira como Caçador (com a pausa, a planta,
// o caderno, as frases e o chat fechado), o resumo e o resultado, o relatório, o Rever a noite, o perfil depois da noite,
// uma noite como Cultista (com a escolha dos altares), o tutorial, o desafio e o Arsenal. Cada quadro tem as duas passadas
// do IMGUI (Layout e Repaint); conta as exceções e os grupos de layout mal fechados. Uso: mono telas.exe <Resources>
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static readonly List<string> falhas = new List<string>(); static readonly Dictionary<string, int> telas = new Dictionary<string, int>(); static int nQuadros;
        static void Falha(string onde, Exception e) { if (falhas.Count < 40) falhas.Add(onde + ": " + e.GetType().Name + ": " + e.Message + "\n      " + string.Join("\n      ", (e.StackTrace ?? "").Split('\n').Take(4).Select(l => l.Trim()).ToArray())); }
        string Tela() { if (g == null) return online ? "lobby" : "título" + (telaMenu != null ? "/" + telaMenu : ""); return arsenal ? "arsenal" : g.phase + (g.phase == "final" ? (repAberto ? "/replay" : relAberto ? "/relatório" : "") : g.phase == "play" && !Travado() ? "/pausa" : ""); }
        // um quadro: o Update e as duas passadas do OnGUI
        KeyCode teclaGUI;   // uma tecla para a próxima passada do OnGUI (KeyDown), como digitar no chat
        void TQuadro(float dt = 1 / 30f, int passosExtras = 0)
        {
            nQuadros++; Time.time += dt; Time.unscaledTime += dt; Time.deltaTime = dt; Time.unscaledDeltaTime = dt; Time.realtimeSinceStartup += dt; Time.frameCount++;
            try { Update(); } catch (Exception e) { Falha("Update (" + Tela() + ")", e); }
            if (g != null && passosExtras > 0 && g.phase == "play" && !arsenal) try { for (int i = 0; i < passosExtras; i++) Sim.Step(g, .05); } catch (Exception e) { Falha("Sim.Step", e); }
            Input.apertadas.Clear();
            string t = Tela(); int n; telas.TryGetValue(t, out n); telas[t] = n + 1;
            var passadas = teclaGUI != KeyCode.None ? new[] { EventType.Layout, EventType.KeyDown, EventType.Layout, EventType.Repaint } : new[] { EventType.Layout, EventType.Repaint }; var tecla = teclaGUI; teclaGUI = KeyCode.None;
            foreach (var tipo in passadas)
            {
                Event.current = new Event { type = tipo, keyCode = tipo == EventType.KeyDown ? tecla : KeyCode.None, mousePosition = new Vector2(-100, -100) }; GUILayout.pilha.Clear(); GUI.matrix = new Matrix4x4();
                try { OnGUI(); } catch (Exception e) { Falha("OnGUI " + tipo + " (" + t + ")", e); }
                if (GUILayout.pilha.Count > 0 && GUILayout.erros.Count < 40) GUILayout.erros.Add("tela " + t + ": sobrou aberto no fim da passada: " + string.Join(", ", GUILayout.pilha.ToArray()));
            }
        }
        void TQuadros(int n, float dt = 1 / 30f, int extras = 0) { for (int i = 0; i < n; i++) TQuadro(dt, extras); }
        void Apertar(string texto, int quadrosDepois = 3) { GUILayout.Apertar(texto); TQuadros(quadrosDepois); }
        // até a fase pedida (ou o limite), apertando o que cada tela pede para seguir
        void AteFase(string fase, int limite, int extras = 9)
        {
            for (int i = 0; i < limite && g != null && g.phase != fase; i++)
            {
                if (g.phase == "intro" && !(g.ready.ContainsKey(EU) && g.ready[EU])) GUILayout.Apertar("Estou pronto");
                if (g.phase == "summary") GUILayout.Apertar("Ver resultado");
                if (g.phase == "pick" && MeuPapel() == "C") { if (g.picks.Count < 3) GUILayout.Apertar("Sortear"); else GUILayout.Apertar("Confirmar 3 de 3"); }
                if (g.phase == "play" && !Travado()) Travar(true);
                TQuadro(.05f, extras);
            }
        }

        public static int Main(string[] a)
        {
            Resources.pasta = a[0]; Screen.width = 1920; Screen.height = 1080; GameObject.chamarAwake = true;
            var go = new GameObject("Partida"); var p = go.AddComponent<PartidaLocal>();
            try { p.Awake(); p.Start(); } catch (Exception e) { Falha("Awake/Start", e); Relatar(p); return 1; }
            p.Roteiro(); if (a.Length > 1) p.RoteiroOnline(a[1]); return Relatar(p);
        }
        void Roteiro()
        {
            // título e os menus
            TQuadros(20);
            foreach (var m in new[] { "aprender", "como", "perfil", "novidades", "ajustes" }) { telaMenu = m; TQuadros(6); }
            telaMenu = null; TQuadros(3); labAberto = true; TQuadros(3); labAberto = false;
            // menus pelo controle: foco no primeiro botão do título (Jogar sozinho) e A
            padNav = true; padFoco = 0; padConfirmar = true; padConfirmarQuadro = Time.frameCount + 1; TQuadro(); if (g == null) falhas.Add("controle: o A no foco não apertou o botão"); g = null; LimparVisual(); padNav = false; TQuadros(2);
            // trocar a tecla do Sensor nos Ajustes: clique na linha, aperte X; depois F, que é da Lanterna: as duas trocam
            telaMenu = "ajustes"; TQuadros(2); capturando = "sensor"; capturaQuadro = Time.frameCount; TQuadros(2); Input.apertadas.Add(KeyCode.X); TQuadro();
            if (EntradaUnity.Nome("sensor") != "X") falhas.Add("teclas: o Sensor não ficou no X (" + EntradaUnity.Nome("sensor") + ")");
            EntradaUnity.Definir("sensor", "KeyF"); if (EntradaUnity.Nome("sensor") != "F" || EntradaUnity.Nome("lantern") != "X") falhas.Add("teclas: a troca com a Lanterna não aconteceu");
            Apertar("Restaurar padrão", 2); if (EntradaUnity.Nome("sensor") != "Q" || EntradaUnity.Nome("lantern") != "F") falhas.Add("teclas: o padrão não voltou"); telaMenu = null; TQuadros(2);
            // uma noite como Caçador: o botão do título, a entrada, a noite inteira em passos de 0,5 s
            papelSolo = "H"; Apertar("Jogar sozinho, com bots", 2);
            AteFase("play", 400);
            TQuadros(60, .05f); Travar(false); TQuadros(6, .05f); pausaAjustes = true; TQuadros(4, .05f); pausaAjustes = false; Travar(true);   // a pausa
            cadernoAberto = true; TQuadros(10, .05f); cadernoAberto = false; falasAbertas = true; falasT = 4; TQuadros(10, .05f);
            Input.apertadas.Add(KeyCode.V); TQuadro(.05f); Input.apertadas.Add(KeyCode.Alpha1); TQuadro(.05f);   // uma frase pelas teclas
            Input.seguradas.Add(KeyCode.Tab); TQuadros(10, .05f); Input.seguradas.Clear();                           // a planta
            AteFase("summary", 3000); TQuadros(10); if (g != null && g.phase == "summary") Apertar("Ver resultado", 3);
            AteFase("final", 400); TQuadros(20);
            if (g != null && g.phase == "final")
            {
                Apertar("Relatório do playtest", 10); Apertar("Copiar como texto", 2); Apertar("Voltar", 3);
                Apertar("Rever a noite", 5); repVel = 80; TQuadros(120); Apertar("Voltar ao resultado", 3);
                Apertar("Baixar registro", 3);
            }
            Apertar("Voltar ao menu", 5); telaMenu = "perfil"; TQuadros(10); telaMenu = null; TQuadros(3);
            // uma noite como Cultista, com a escolha dos altares
            papelSolo = "C"; Apertar("Jogar sozinho, com bots", 2); AteFase("intro", 200); AteFase("play", 200); TQuadros(200, .05f, 9); g = null; LimparVisual(); TQuadros(3);
            // o tutorial (o relógio adianta sozinho) e um passo pulado pela tecla N
            IniciarSolo("H", "treino", true, false); AteFase("play", 300); TQuadros(100, .05f, 3); Input.apertadas.Add(KeyCode.N); TQuadro(.05f); TQuadros(200, .05f, 9); g = null; LimparVisual(); TQuadros(3);
            // o desafio da noite, até o fim
            Apertar("Desafio da noite", 2); AteFase("play", 300); AteFase("final", 3000); TQuadros(10); g = null; LimparVisual(); TQuadros(3);
            // o Arsenal: troca de arma e de skin, as ações e as vistas
            IniciarArsenal(); TQuadros(30); foreach (var b in new[] { "Revólver", "Inspecionar", "Recarregar", "De lado", "De perto", "Jogador", "Cetro", "Lançar sigilo", "Da noite", "Estúdio", "Carabina", "Sacar" }) Apertar(b, 12);
            Apertar("Voltar ao menu", 5); TQuadros(10);
        }
        // online, contra o servidor de verdade (a porta vem do unity-telas.js): a sala, as opções do anfitrião, o chat, a partida
        void Esperar(System.Func<bool> ok, int limite = 300) { for (int i = 0; i < limite && !ok(); i++) { System.Threading.Thread.Sleep(15); TQuadro(1 / 30f); } }
        void RoteiroOnline(string porta)
        {
            servidor = "localhost:" + porta; nomeJogador = "Teste"; Conectar(true, ""); Esperar(() => noLobby && lobbySlots.Count == 4);
            if (!noLobby) { falhas.Add("online: não entrou no lobby (" + erroRede + ")"); return; }
            Apertar("Difíceis", 2); Esperar(() => lobbyDif == "dificil", 100); if (lobbyDif != "dificil") falhas.Add("online: a dificuldade do lobby não chegou (" + lobbyDif + ")");
            labAberto = true; Apertar("2:00", 2); Esperar(() => lobbyLab != null && lobbyLab.crepusculo == 120, 100); if (lobbyLab == null || lobbyLab.crepusculo != 120) falhas.Add("online: o laboratório do lobby não chegou");
            rascunhoLobby = "boa noite"; Apertar("Enviar", 2); Esperar(() => chat.Count > 0, 100); if (chat.Count == 0) falhas.Add("online: o chat do lobby não voltou");
            Apertar("Começar a partida", 2); Esperar(() => g != null, 200); if (g == null) { falhas.Add("online: a partida não começou"); return; }
            if (g.lab == null || g.lab.crepusculo != 120) falhas.Add("online: a partida não veio com o laboratório do lobby");
            for (int i = 0; i < 600 && g != null && g.phase != "play"; i++) { if (g.phase == "intro") GUILayout.Apertar("Estou pronto"); System.Threading.Thread.Sleep(15); TQuadro(1 / 30f); }
            if (g == null || g.phase != "play") { falhas.Add("online: não chegou ao jogo (" + (g != null ? g.phase : "sem jogo") + ")"); return; }
            Travar(true); Esperar(() => false, 60);
            Input.apertadas.Add(KeyCode.Return); TQuadro(1 / 30f); if (!chatAberto) falhas.Add("online: Enter não abriu o chat da partida");
            chatTexto = "estou indo"; TQuadro(1 / 30f); teclaGUI = KeyCode.Return; TQuadro(1 / 30f); if (chatAberto) falhas.Add("online: Enter não enviou o chat");
            Esperar(() => feed.Any(f => f.msg.Contains("estou indo")), 100); if (!feed.Any(f => f.msg.Contains("estou indo"))) falhas.Add("online: a mensagem da partida não voltou para a equipe");
            Travar(false); Esperar(() => false, 10); Apertar("Sair da sala", 3); if (online) falhas.Add("online: Sair da sala não saiu");
            Console.WriteLine("online: lobby, opções, chat, partida e chat da partida");
        }
        static int Relatar(PartidaLocal p)
        {
            Console.WriteLine(nQuadros + " quadros; telas: " + string.Join(", ", telas.OrderBy(k => k.Key).Select(k => k.Key + " " + k.Value).ToArray()));
            var perfil = p.Perfil(); Console.WriteLine("perfil: " + Nn(perfil, "partidas") + " partidas, " + O(perfil, "conq").Count + " conquistas; desafio: " + (O(perfil, "desafios").Count > 0 ? "com recorde" : "sem recorde"));
            foreach (var t in new[] { "Jogar sozinho, com bots", "Desafio da noite", "Laboratório de playtest", "Esta noite você é Caçador", "Lances da noite", "Destaques da noite", "Relatório do playtest", "Rever a noite", "Seu perfil", "Arsenal" })
                if (!GUILayout.textos.Any(x => x.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)) Console.WriteLine("  (não apareceu: " + t + ")");
            foreach (var e in GUILayout.erros.Distinct().Take(20)) Console.WriteLine("LAYOUT " + e);
            foreach (var f in falhas) Console.WriteLine("ERRO " + f);
            return falhas.Count + GUILayout.erros.Count > 0 ? 1 : 0;
        }
    }
}
