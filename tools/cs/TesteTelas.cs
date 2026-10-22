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
        void TQuadro(float dt = 1 / 30f, int passosExtras = 0)
        {
            nQuadros++; Time.time += dt; Time.unscaledTime += dt; Time.deltaTime = dt; Time.unscaledDeltaTime = dt; Time.realtimeSinceStartup += dt; Time.frameCount++;
            try { Update(); } catch (Exception e) { Falha("Update (" + Tela() + ")", e); }
            if (g != null && passosExtras > 0 && g.phase == "play" && !arsenal) try { for (int i = 0; i < passosExtras; i++) Sim.Step(g, .05); } catch (Exception e) { Falha("Sim.Step", e); }
            Input.apertadas.Clear();
            string t = Tela(); int n; telas.TryGetValue(t, out n); telas[t] = n + 1;
            foreach (var tipo in new[] { EventType.Layout, EventType.Repaint })
            {
                Event.current = new Event { type = tipo, mousePosition = new Vector2(-100, -100) }; GUILayout.pilha.Clear(); GUI.matrix = new Matrix4x4();
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
            p.Roteiro(); return Relatar(p);
        }
        void Roteiro()
        {
            // título e os menus
            TQuadros(20);
            foreach (var m in new[] { "aprender", "como", "perfil", "novidades", "ajustes" }) { telaMenu = m; TQuadros(6); }
            telaMenu = null; TQuadros(3); labAberto = true; TQuadros(3); labAberto = false;
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
