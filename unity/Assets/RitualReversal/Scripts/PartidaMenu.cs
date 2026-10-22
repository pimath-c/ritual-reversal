// Menu e telas fora da noite (titleScreen, aprenderScreen, comoJogarScreen, novidadesScreen e settingsScreen do
// index.html), a partida solo (startLocal: papel, dificuldade, laboratório, tutorial, desafio), o tutorial guiado
// (PASSOS_TUT: cada passo diz o que fazer, marca o alvo na tela e na bússola e confere no registro; N pula) e as dicas da
// primeira noite (DICAS: cada uma aparece uma vez, quando a situação acontece, no máximo uma a cada 12 s).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        string telaMenu;   // null (título), "ajustes", "como", "perfil", "novidades", "aprender"
        string papelSolo;
        string PapelSolo { get { return papelSolo ?? (papelSolo = PlayerPrefs.GetString("rr-papel", "H")); } }

        // ---------- partida solo (startLocal) ----------
        public void NovaPartida(bool cacador) { IniciarSolo(cacador ? "H" : "C", null, false, false); }
        void IniciarSolo(string papel, string dif, bool tutorial, bool desafio)
        {
            if (papel == "?") papel = Random.value < .5f ? "H" : "C";
            var slots = papel == "C"
                ? new List<Slot> { new Slot("A", null, "Irmã Beatriz"), new Slot("A", null, "Irmão Tomé"), new Slot("B", "local", "Você"), new Slot("B", null, "Madre Vesper") }
                : new List<Slot> { new Slot("A", "local", "Você"), new Slot("A", null, "Irmã Beatriz"), new Slot("B", null, "Irmão Cinza"), new Slot("B", null, "Madre Vesper") };
            if (online) SairOnline(""); EU = "local";
            g = Sim.CreateGame(new OpcoesJogo { timers = false, slots = slots, matchId = System.DateTime.Now.ToString("yyyyMMdd-HHmmss"), dificuldade = dif ?? dificuldade, lab = tutorial || desafio ? null : LabSolo });
            faseAnterior = ""; lojaAberta = null; feed.Clear(); LimparVisual(); logIni = 0; telaMenu = null;
            tutAtivo = tutorial; tutPapel = papel; tutI = 0; tutFim = false; tutAvancou = false; desAtivo = false; desFeito = null;
        }

        // ---------- sigilo do título (o SVG do navegador, desenhado uma vez) ----------
        static Texture2D texSigilo;
        static Texture2D Sigilo()
        {
            if (texSigilo != null) return texSigilo; var D = new Desenho(120, 120, 3, new Color(0, 0, 0, 0));
            System.Func<float, float, List<Vector2>> arco = (a0, a1) => { var p = new List<Vector2>(); for (int i = 0; i <= 48; i++) { float a = Mathf.Lerp(a0, a1, i / 48f); p.Add(new Vector2(60 + Mathf.Cos(a) * 52, 60 + Mathf.Sin(a) * 52)); } return p; };
            D.Polilinha(arco(-Mathf.PI / 2, -Mathf.PI * 1.5f), 3, Desenho.Hex("#d2ad62")); D.Polilinha(arco(-Mathf.PI / 2, Mathf.PI / 2), 3, Desenho.Hex("#a3203a"));
            D.Polilinha(new[] { new Vector2(60, 4), new Vector2(60, 116) }, 1.2f, Desenho.Hex("#e8e0d0", .5f));
            var olho = new List<Vector2>(); System.Func<Vector2, Vector2, Vector2, float, Vector2> q = (a, c, b, t) => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b;
            for (int i = 0; i <= 32; i++) olho.Add(q(new Vector2(22, 60), new Vector2(60, 102), new Vector2(98, 60), i / 32f)); for (int i = 1; i <= 32; i++) olho.Add(q(new Vector2(98, 60), new Vector2(60, 18), new Vector2(22, 60), i / 32f));
            D.Polilinha(olho, 2.4f, Desenho.Hex("#e8e0d0")); D.Circulo(60, 70, 12, Desenho.Hex("#a3203a")); D.Circulo(60, 70, 5, Desenho.Hex("#050506"));
            return texSigilo = D.Textura();
        }

        // ---------- título ----------
        void TelaTitulo()
        {
            EstilosNoite();
            switch (telaMenu) { case "ajustes": TelaAjustes(); return; case "como": TelaComoJogar(); return; case "perfil": TelaPerfil(); return; case "novidades": TelaNovidades(); return; case "aprender": TelaAprender(); return; }
            var r = Janela(Mathf.Min(1040, W - 40), H - 50); GUILayout.BeginArea(r); rolFim = GUILayout.BeginScrollView(rolFim);
            var rs = GUILayoutUtility.GetRect(r.width - 30, 110); GUI.DrawTexture(new Rect(rs.center.x - 55, rs.y, 110, 110), Sigilo());
            GUILayout.Label(Cor("Ritual Reversal", OSSO), new GUIStyle(sTitulo) { fontSize = 54, fontStyle = FontStyle.Normal });
            GUILayout.Label(Cor(UmaNoite ? "Dois cultistas celebram rituais em segredo. Dois caçadores investigam e selam. Uma noite, três rituais: quem levar dois vence." : "Dois cultistas celebram um ritual em segredo. Dois caçadores tentam descobrir onde. Na segunda rodada, os papéis se invertem.", new Color(.79f, .75f, .68f)), sCentro);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (GUILayout.Button("<size=24>Jogar sozinho, com bots</size>", sBotao, GUILayout.Width(440), GUILayout.Height(62))) IniciarSolo(PapelSolo, null, false, false);
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            var sb = new GUIStyle(sBotao) { fontSize = 16, padding = new RectOffset(10, 10, 6, 6) };
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label("Você", sPequeno, GUILayout.Width(130));
            foreach (var k in new[] { new[] { "H", "Caça" }, new[] { "C", "Celebra" }, new[] { "?", "Sorteia" } })
                if (GUILayout.Button(PapelSolo == k[0] ? Cor(k[1], OURO_VIVO) : k[1], sb, GUILayout.Width(120))) { papelSolo = k[0]; PlayerPrefs.SetString("rr-papel", k[0]); som.Tocar("click"); }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label("Bots inimigos", sPequeno, GUILayout.Width(130));
            foreach (var d in new[] { new[] { "facil", "Fácil" }, new[] { "normal", "Normal" }, new[] { "dificil", "Difícil" } })
                if (GUILayout.Button(dificuldade == d[0] ? Cor(d[1], OURO_VIVO) : d[1], sb, GUILayout.Width(120))) { dificuldade = d[0]; PlayerPrefs.SetString("rr-dif", dificuldade); som.Tocar("click"); }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.BeginVertical(GUILayout.Width(760)); PainelLab(LabSolo, MudarLabSolo); GUILayout.EndVertical(); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            {
                var D = DesafioDe(HojeDesafio()); object rec; var des = O(Perfil(), "desafios"); bool tem = des.TryGetValue(HojeDesafio(), out rec) && rec != null;
                string sub = "Hoje você " + (D[1] == "H" ? "caça" : "celebra") + ", " + Mundo.CLIMAS[D[2]].nome.ToLowerInvariant() + ", contra bots difíceis" + (tem ? " · recorde " + System.Convert.ToInt32(rec) : "");
                GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                if (GUILayout.Button("Desafio da noite\n<size=14>" + Cor(sub, MUDO) + "</size>", sBotao, GUILayout.Width(560))) IniciarDesafio();
                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            }
            var lk = new GUIStyle(sPequeno) { richText = true, alignment = TextAnchor.MiddleCenter };
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            foreach (var l in new[] { new[] { "aprender", "Aprender a jogar" }, new[] { "como", "Como jogar" }, new[] { "perfil", "Perfil" }, new[] { "novidades", "Novidades" }, new[] { "arsenal", "Arsenal" }, new[] { "ajustes", "Ajustes" } })
                if (GUILayout.Button("<u>" + Cor(l[1], OURO) + "</u>", lk, GUILayout.ExpandWidth(false))) { som.Tocar("click"); if (l[0] == "arsenal") IniciarArsenal(); else { telaMenu = l[0]; rolPerfil = Vector2.zero; } }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.Space(16); TelaOnline(r);
            GUILayout.Label("<size=14>" + Cor("Para jogar online com amigos, rode o servidor do projeto (npm start na pasta server) e digite o endereço dele. Use fones: o som diz onde e em que fase está cada ritual.", MUDO) + "</size>", sCentro);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        void Voltar(string txt = "Voltar") { GUILayout.Space(12); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); if (GUILayout.Button(txt, sBotao, GUILayout.Width(240), GUILayout.Height(50))) { telaMenu = null; som.Tocar("click"); } GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); }

        void TelaAprender()
        {
            var r = Janela(Mathf.Min(900, W - 40), 470); GUILayout.BeginArea(r);
            GUILayout.Label("Aprender a jogar", sTitulo); GUILayout.Label("Uma noite guiada, passo a passo, contra bots mansos. Escolha o lado.", sCentro); GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<b>" + Cor("Como Caçador", OURO) + "</b>\n<size=16>Ler santuários, acender sentinelas, usar o sensor e a lanterna, purgar altares e selar um ritual.</size>", sBotao, GUILayout.Height(150))) IniciarSolo("H", "treino", true, false);
            if (GUILayout.Button("<b>" + Cor("Como Cultista", CARMIM_HI) + "</b>\n<size=16>Pegar reagentes, consagrar altares, cumprir tarefas, iniciar um ritual e canalizar no círculo.</size>", sBotao, GUILayout.Height(150))) IniciarSolo("C", "treino", true, false);
            GUILayout.EndHorizontal(); Voltar(); GUILayout.EndArea();
        }

        static readonly string[][] NOVIDADES = {
            new[]{"Catedral e desfiladeiro (v6)","O mapa foi refeito em duas regiões: a catedral em cruz no alto do promontório e a mata ao sul, separadas por um desfiladeiro com três travessias (a Ponte Grande, a árvore caída e o aqueduto). Cada parte da mata tem o seu reagente."},
            new[]{"Cobertura e agachar (v6)","Túmulos, muretas e pedras baixas agora param tiros e sigilos abaixo da altura deles. Agachado (C, ou o botão B do controle) você anda na metade da velocidade, com passos que só se ouvem a 5 m, e fica mais baixo atrás da cobertura."},
            new[]{"Execução, reagentes e velas (v6)","Executar alguém tem pose, tremor e um fecho que todos por perto veem; quem está sendo executado vê o mundo fechar. Os reagentes têm forma e respondem à coleta. Oito velas em cada altar contam o ritual: vermelho-escuro é progresso que não volta."},
            new[]{"Arsenal (v6)","No menu, o Arsenal mostra as armas de perto e a coleção Relíquias da Vigília: três acabamentos por arma (Ofício, Mestre e Relíquia), todos livres, só aparência. Cada arma tem uma inspeção nova (tecla I)."},
            new[]{"Mãos de verdade (v5.9)","As armas agora são seguradas por mãos com dedos e braços que se dobram. A carabina recarrega cartucho por cartucho pela portinhola (e aciona a alavanca quando estava vazia); o revólver abre o tambor, ejeta as cápsulas e usa um municiador de oito."},
            new[]{"Sacar, inspecionar, gesticular","Cada arma tem seu jeito de sacar (a alavanca, a bênção na cruz do revólver, as vértebras do cetro acendendo). Tecla I inspeciona a arma; as habilidades têm um gesto da mão livre."},
            new[]{"Do primeiro playtest (v5.8b)","Salvar um ritual que chegou a 85% ou mais vira o lance \"Selo no último instante\", mesmo que o progresso recue enquanto você sela. A loja não cobra munição com a reserva cheia. O registro baixado diz a dificuldade dos bots."},
            new[]{"Sons gravados","Tiros, impactos, passos por chão, sino, corvos, coro e o ritual inteiro agora usam gravações reais de bibliotecas abertas, com variação a cada vez. Créditos em Ajustes."},
            new[]{"Cada arma tem seu peso","A carabina coiceia curto e abre a alavanca a cada tiro; o revólver sobe mais e gira o tambor; o cetro recolhe as runas e as solta."},
            new[]{"Acerto que se sente","O acerto confirmado pelo servidor tem marca, som, uma pausa curta no coice e um empurrão leve na câmera. Golpe no escudo de osso aparece como bloqueio."},
            new[]{"Quem leva o tiro reage","O alvo cede na direção do golpe, a cabeça estala no tiro na cabeça, e a queda e a reanimação têm movimento."},
            new[]{"O ritual como evento","As runas acendem uma a uma no início, pulsam a cada etapa, tremem e douram quando a caça sela, se fecham no Farol e explodem na Fenda."},
            new[]{"Investigação","EVP, resíduo na Lanterna e Chamariz desmascarado têm som e efeito próprios no altar."},
            new[]{"Ajuste","Impacto na câmera: Normal, Reduzido ou Desligado."},
            new[]{"Na v5.7","Interrupções, Fenda e Farol com nomes, lances da noite e o registro da disputa no círculo."} };
        void TelaNovidades()
        {
            var r = Janela(Mathf.Min(980, W - 40), H - 60); GUILayout.BeginArea(r); rolPerfil = GUILayout.BeginScrollView(rolPerfil);
            GUILayout.Label("Novidades da v5.8", sTitulo); GUILayout.Label("O que mudou desde a última versão.", sCentro); GUILayout.Space(8);
            foreach (var n in NOVIDADES) { GUILayout.BeginVertical(Fundo(new Color(.06f, .06f, .07f, .7f))); GUILayout.Label("<b>" + Cor(n[0], OURO) + "</b>", sPequeno); GUILayout.Label("<size=15>" + n[1] + "</size>", sPequeno); GUILayout.EndVertical(); }
            Voltar(); GUILayout.EndScrollView(); GUILayout.EndArea();
        }

        void Passo(int n, string t, string d)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label("<size=26>" + Cor(n.ToString(), OURO) + "</size>", sTexto, GUILayout.Width(34));
            GUILayout.BeginVertical(); GUILayout.Label("<b>" + t + "</b>", sPequeno); GUILayout.Label("<size=14>" + Cor(d, new Color(.79f, .75f, .68f)) + "</size>", sPequeno); GUILayout.EndVertical(); GUILayout.EndHorizontal(); GUILayout.Space(4);
        }
        void TelaComoJogar()
        {
            var r = Janela(Mathf.Min(1100, W - 40), H - 50); GUILayout.BeginArea(r); rolPerfil = GUILayout.BeginScrollView(rolPerfil);
            GUILayout.Label("Como jogar", sTitulo);
            GUILayout.Label(Cor(UmaNoite ? "Uma equipe celebra, a outra caça. Os Cultistas vencem completando dois dos três rituais; os Caçadores, selando dois ou resistindo até o amanhecer." : "Duas rodadas. Na primeira sua equipe caça; na segunda, celebra. Vence quem completar mais rituais como Cultista.", new Color(.79f, .75f, .68f)), sCentro);
            GUILayout.Label(Cor("Cada partida é uma noite de até 18 minutos, em três momentos: o Crepúsculo, a Vigília e a Hora Morta. Quando uma equipe leva dois rituais, a noite acaba ali.", new Color(.79f, .75f, .68f)), sCentro);
            GUILayout.Label(Cor("A catedral fica no meio de uma floresta. As trilhas e clareiras pegam luar; a mata fechada é breu. Dois altares ficam lá fora: os Menires e o Carvalho Oco.", new Color(.79f, .75f, .68f)), sCentro);
            GUILayout.BeginHorizontal(); GUILayout.BeginVertical(GUILayout.Width(r.width / 2 - 26));
            GUILayout.Label("<b>" + Cor("Como Cultista", CARMIM_HI) + "</b>", sTexto);
            Passo(1, "Crepúsculo: prepare-se", "Colha ervas na floresta e profane túmulos no cemitério, ao leste da catedral. Tarefas rendem óbolos; negocie reagentes com a Carpideira, no oratório perto do seu acampamento. Os reagentes ficam na mata, ao sul do desfiladeiro: cada lugar tem o seu (sal negro na Pedreira, raiz de mandrágora no Charco, velas de cera na Encruzilhada…).");
            Passo(2, "Consagre em silêncio", "Você escolheu 3 de 6 altares. Consagrar gasta 1 reagente. Cuidado com as sentinelas acesas: elas denunciam você.");
            Passo(3, "Vigília: os rituais começam", "Inicie com mais um reagente e fique no círculo. Quem ataca não canaliza: cada sigilo pausa a sua canalização por um instante, então em dupla um celebra e o outro defende. A Transferência deixa marcas de arrasto no altar de origem. Chamarizes (E num altar não escolhido) confundem a investigação.");
            Passo(4, "Hora Morta: o Grande Ritual", "Se estiver 1 a 1, o ritual decisivo só começa agora, e a catedral inteira ouve. Vença dois dos três antes do amanhecer.");
            GUILayout.EndVertical(); GUILayout.BeginVertical();
            GUILayout.Label("<b>" + Cor("Como Caçador", OURO) + "</b>", sTexto);
            Passo(1, "Crepúsculo: prepare-se", "Cada sinal lido num santuário descarta um altar que o culto não escolheu. Acenda sentinelas e negocie munição, óleo e sinalizadores com o Ermitão, na cabana perto do acampamento.");
            Passo(2, "Investigue", "Q lê sinais por perto, o EVP sussurra junto de altares consagrados, e a Lanterna (F) mostra resíduo só nos verdadeiros.");
            Passo(3, "Vigília: purgue e sele", "Segure E num altar consagrado para purgar. Depois do Presságio, o coro ecoa o setor do ritual: dois altares possíveis, e o que você já descartou decide para onde correr. Sele em dupla. Marcas de arrasto num altar denunciam uma Transferência.");
            Passo(4, "Resista até o amanhecer", "Cada ritual selado força os Cultistas a recomeçar em outro altar. Selem dois dos três rituais, ou resistam até o amanhecer.");
            GUILayout.EndVertical(); GUILayout.EndHorizontal();
            GUILayout.Label("<size=14>" + Cor("Caído, um aliado pode te reanimar. O inimigo pode te executar, mas levar dano recomeça a execução: atire em quem executa o seu aliado. O som diz muito: use fones (as legendas mostram de que lado vem cada som). Z marca um ponto, um altar ou um inimigo para o aliado; o bot aliado também obedece.", MUDO) + "</size>", sCentro);
            Voltar("Entendi"); GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        void TelaAjustes()
        {
            var r = Janela(Mathf.Min(980, W - 40), H - 60); GUILayout.BeginArea(r); rolPerfil = GUILayout.BeginScrollView(rolPerfil);
            GUILayout.Label("Ajustes", sTitulo); Ajustes(); Voltar(); GUILayout.EndScrollView(); GUILayout.EndArea();
        }

        // ================= APRENDER A JOGAR (TUTORIAL) =================
        bool tutAtivo, tutFim, tutAvancou; string tutPapel = "H"; int tutI; Ponto tutAlvo;
        class PassoTut { public string t; public bool avanca; public System.Func<Ator, Ponto> alvo; public System.Func<Ator, bool> feito; }
        bool MeuLog(string ev, System.Func<Registro, bool> f = null) { return g != null && g.log.Skip(logIni).Any(e => e.ev == ev && (e["by"] as string) == "Você" && (f == null || f(e))); }
        static Ponto MaisPerto(IEnumerable<Ponto> lista, Ator m) { Ponto b = null; double bd = 1e9; foreach (var o in lista) { double d = JS.Hypot(o.x - m.x, o.z - m.z); if (d < bd) { bd = d; b = o; } } return b; }
        List<PassoTut> PassosTut(string papel)
        {
            if (papel == "H") return new List<PassoTut> {
                new PassoTut { t = "Ande com WASD e olhe com o mouse. Vá até o santuário marcado e segure E para ler o sinal: ele descarta um altar que o culto não escolheu.", alvo = m => MaisPerto(g.pontos.Where(p => p.tipo == "pista" && p.ativo).Cast<Ponto>(), m), feito = m => MeuLog("ponto", e => (e["tipo"] as string) == "pista") },
                new PassoTut { t = "Acenda a sentinela marcada (segure E). Acesa, ela avisa quando alguém consagra um altar por perto.", alvo = m => MaisPerto(g.pontos.Where(p => p.tipo == "sentinela" && !p.acesa).Cast<Ponto>(), m), feito = m => MeuLog("ponto", e => (e["tipo"] as string) == "sentinela") },
                new PassoTut { t = "Aperte Q para usar o Sensor Áurico. Ele mede a força do sinal dos altares consagrados por perto.", feito = m => MeuLog("sensor") },
                new PassoTut { t = "Adiantamos o relógio: a Vigília chegou e os altares consagrados despertam. Ligue a lanterna (F): ela mostra resíduo só nos altares verdadeiros.", avanca = true, feito = m => g.momento >= 1 && m.lantern },
                new PassoTut { t = "Vá a um altar desperto (diamante roxo no alto da tela) e segure E para purgá-lo antes que o ritual comece.", alvo = m => MaisPerto(g.altars.Where(A => A.i < g.know.Length && (g.know[A.i] == "desperto" || g.know[A.i] == "confirmado")).Cast<Ponto>(), m), feito = m => MeuLog("purge") },
                new PassoTut { t = "Quando um ritual começar, o coro logo ecoa o setor: dois altares possíveis, marcados na faixa do topo. Vá até o certo, siga o som até o círculo e segure E dentro dele para selar. Com o aliado junto o selo anda muito mais rápido.", alvo = m => MaisPerto(g.altars.Where(A => A.state == "active").Cast<Ponto>(), m), feito = m => m.sealing >= 0 } };
            return new List<PassoTut> {
                new PassoTut { t = "Ande com WASD e olhe com o mouse. Pegue um reagente na mata: siga o marcador até o brilho vermelho (sal negro, raiz, velas…) e segure E.", alvo = m => MaisPerto(g.reagents.Where(R => R.has).Cast<Ponto>(), m), feito = m => m.reag > 0 },
                new PassoTut { t = "Vá a um dos seus altares (marcado) e segure E para consagrá-lo. Isso gasta o reagente e prepara o altar para a Vigília.", alvo = m => MaisPerto(g.altars.Where(A => A.chosen && A.state == "dormant").Cast<Ponto>(), m), feito = m => MeuLog("consecrate") },
                new PassoTut { t = "Colha uma erva na floresta ou profane um túmulo no cemitério (segure E no ponto marcado). Tarefas rendem óbolos para a loja.", alvo = m => MaisPerto(g.pontos.Where(p => (p.tipo == "erva" || p.tipo == "tumulo") && p.ativo).Cast<Ponto>(), m), feito = m => MeuLog("ponto", e => (e["tipo"] as string) == "erva" || (e["tipo"] as string) == "tumulo") },
                new PassoTut { t = "Adiantamos o relógio até a Vigília: os rituais já podem começar. Pegue outro reagente.", avanca = true, alvo = m => MaisPerto(g.reagents.Where(R => R.has).Cast<Ponto>(), m), feito = m => g.momento >= 1 && m.reag > 0 },
                new PassoTut { t = "No altar consagrado, segure E para iniciar o ritual.", alvo = m => MaisPerto(g.altars.Where(A => A.chosen && A.state == "awake").Cast<Ponto>(), m), feito = m => MeuLog("ritual_start") },
                new PassoTut { t = "Fique dentro do círculo sem atacar: o ritual só avança com quem canaliza, e cada sigilo pausa a sua canalização por um instante. Aos 25%, o coro ecoa o setor para os Caçadores. Leve o ritual a 25%.", alvo = m => MaisPerto(g.altars.Where(A => A.state == "active").Cast<Ponto>(), m), feito = m => g.altars.Any(A => A.state == "active" && A.prog >= .25) } };
        }
        List<PassoTut> passosTut; string passosPapel;
        // a cada quadro de jogo (Update): confere o passo e adianta o relógio quando ele pede
        void AtualizarTutorial(Ator m)
        {
            tutAlvo = null; if (!tutAtivo || g == null || g.phase != "play" || m == null || online) return;
            if (passosTut == null || passosPapel != tutPapel) { passosTut = PassosTut(tutPapel); passosPapel = tutPapel; }
            if (tutI >= passosTut.Count)
            {
                if (!tutFim) { tutFim = true; som.Tocar("sealed"); var p = Perfil(); var conq = O(p, "conq"); if (!conq.ContainsKey("aprendiz")) { conq["aprendiz"] = Agora(); SalvarPerfil(p); filaConq.Add(CONQ.First(q => q[0] == "aprendiz")); } }
                return;
            }
            var P = passosTut[tutI];
            if (P.avanca && !tutAvancou) { tutAvancou = true; double ate = Noite.FimMomento(g.lab, 0); if (g.rt < ate - 3) g.rt = ate - 3; }
            if (P.feito(m)) { tutI++; tutAvancou = false; som.Tom(float.NaN, float.NaN, 1, 660, 880, .18f, Som.Onda.Seno, .2f); return; }
            tutAlvo = P.alvo != null ? P.alvo(m) : null;
        }
        void PularPassoTutorial() { if (!tutAtivo || tutFim) return; tutI++; tutAvancou = false; som.Tocar("click"); }
        // o quadro do tutorial, à esquerda (#tutorial)
        void PainelTutorial()
        {
            if (!tutAtivo || g == null || g.phase != "play" || passosTut == null) return; float x = 26, y = 300, w = 330;
            string txt; if (tutFim) txt = "<size=12>" + Cor("TUTORIAL COMPLETO", OURO) + "</size>\n<size=15>Você já sabe o básico de " + (tutPapel == "H" ? "caçar" : "celebrar") + ". Termine a noite contra os bots ou saia pela pausa (Esc).</size>";
            else txt = "<size=12>" + Cor("APRENDER · PASSO " + (tutI + 1) + " DE " + passosTut.Count, CARMIM_HI) + "</size>\n<size=15>" + passosTut[tutI].t + "</size>\n<size=12><i>" + Cor("N pula este passo", MUDO) + "</i></size>";
            var st = new GUIStyle(sPequeno) { wordWrap = true, richText = true }; float h = st.CalcHeight(new GUIContent(txt), w - 28) + 26;
            Caixa(new Rect(x, y, w, h), new Color(.031f, .035f, .043f, .85f)); Caixa(new Rect(x, y, 3, h), tutFim ? OURO : CARMIM_HI);
            GUI.Label(new Rect(x + 14, y + 8, w - 28, h - 16), txt, st);
            if (!tutFim) { Caixa(new Rect(x + 14, y + h - 10, w - 28, 3), new Color(1, 1, 1, .12f)); Caixa(new Rect(x + 14, y + h - 10, (w - 28) * tutI / passosTut.Count, 3), CARMIM_HI); }
        }

        // ================= DICAS DA PRIMEIRA NOITE =================
        static readonly Dictionary<string, string> DICAS = new Dictionary<string, string> {
            {"h_inicio","Leia os santuários da floresta (segure E): cada sinal descarta um altar que o culto não escolheu."},
            {"h_sentinela","Sentinela apagada aqui perto: acenda (segure E) e ela avisa se alguém consagrar um altar ao redor."},
            {"h_desperto","Um altar despertou. Vá até ele e segure E para purgar antes que o ritual comece."},
            {"h_ouvido","Esse som é um ritual. Siga as legendas e o Sensor Áurico (Q) até o círculo."},
            {"h_circulo","Você está no círculo: segure E para selar. Em dupla o selo anda muito mais rápido."},
            {"h_escuro","A mata fechada é breu e abala a sanidade. A lanterna (F) ajuda, mas também denuncia você."},
            {"c_inicio","Colha ervas e profane túmulos para ganhar óbolos. Os reagentes ficam na mata: Pedreira, Charco, Carvoaria, Encruzilhada e nas cabeceiras das pontes."},
            {"c_consagrar","Este é um dos seus altares: segure E para consagrar (gasta 1 reagente). Faça isso no Crepúsculo."},
            {"c_vigilia","Os rituais já podem começar: segure E num altar consagrado, com um reagente, e fique no círculo."},
            {"c_circulo","O ritual só avança com quem está no círculo sem atacar: um sigilo pausa a sua canalização por um instante. Em dupla, um canaliza e o outro defende."},
            {"h_eco","O coro ecoou: o ritual está num dos dois altares marcados na faixa do topo. O que você já descartou decide para onde correr."},
            {"c_sentinela","Sentinela acesa: consagrar perto dela avisa os Caçadores. Prefira altares longe da luz."},
            {"queda","Você caiu. Um aliado pode te reanimar segurando E perto de você; o inimigo pode te executar."},
            {"aliado","Seu aliado caiu. Vá até ele e segure E para reanimar antes que ele sangre até o fim."},
            {"sanidade","Sanidade baixa: saia do escuro, fique perto de velas ou do aliado. Em pânico, você se denuncia."},
            {"loja","Mercador aqui: aperte E para negociar com seus óbolos."},
            {"ping","Viu algo? Z marca um ponto, um altar ou um inimigo para o seu aliado."} };
        HashSet<string> dicasVistas; readonly List<string> filaDicas = new List<string>(); float dicaT, dicaAberta; string dicaAtual;
        static bool DicasLigadas { get { return PlayerPrefs.GetInt("rr-dicas-on", 1) == 1; } }
        HashSet<string> DicasVistas()
        {
            if (dicasVistas == null) { dicasVistas = new HashSet<string>(); var d = Json.Ler(PlayerPrefs.GetString("rr-dicas", "{}")) as Dictionary<string, object>; if (d != null) foreach (var k in d.Keys) dicasVistas.Add(k); }
            return dicasVistas;
        }
        void Dica(string id) { if (!DicasLigadas || DicasVistas().Contains(id) || filaDicas.Contains(id)) return; filaDicas.Add(id); }
        void RevisarDicas() { dicasVistas = new HashSet<string>(); PlayerPrefs.DeleteKey("rr-dicas"); PlayerPrefs.SetInt("rr-dicas-on", 1); }
        // a cada quadro de jogo (Update)
        void AtualizarDicas(float dt, Ator m)
        {
            dicaT -= dt; dicaAberta -= dt;
            if (m == null || !DicasLigadas || g == null || g.phase != "play" || tutAtivo || arsenal) return;
            string role = MeuPapel(); System.Func<Ponto, double, bool> perto = (o, r) => JS.Hypot(o.x - m.x, o.z - m.z) < r; double rt = g.rt;
            if (rt > 4) Dica(role == "H" ? "h_inicio" : "c_inicio"); if (rt > 70) Dica("ping"); if (m.st == "down") Dica("queda");
            if (m.st == "alive")
            {
                if (g.actors.Any(a => a != m && a.team == m.team && a.st == "down" && perto(a, 30))) Dica("aliado");
                if (m.sanity < 40) Dica("sanidade");
                if (g.lab.economia && Mapa.NPCS.Any(n => (n.time == null || n.time == m.team) && perto(n, 4))) Dica("loja");
                if (role == "H")
                {
                    if (g.pontos.Any(p => p.tipo == "sentinela" && !p.acesa && perto(p, 7))) Dica("h_sentinela");
                    if (g.know != null && g.know.Any(k => k == "desperto" || k == "confirmado")) Dica("h_desperto");
                    if (LEG.Keys.Any(k => k.StartsWith("rit"))) Dica("h_ouvido");
                    if (g.altars.Any(A => A.state == "active" && Sim.InCircle(m, A))) Dica("h_circulo");
                    if (!m.lantern && Mapa.ZoneAt(m.x, m.z) == "Floresta" && Sim.LightAt(g, m.x, m.z) < .12 && rt > 20) Dica("h_escuro");
                }
                else
                {
                    if (m.reag > 0 && g.altars.Any(A => A.chosen && A.state == "dormant" && perto(A, 6))) Dica("c_consagrar");
                    if (g.momento >= 1 && g.altars.Any(A => A.chosen && A.state == "awake")) Dica("c_vigilia");
                    if (g.altars.Any(A => A.state == "active" && Sim.InCircle(m, A))) Dica("c_circulo");
                    if (g.pontos.Any(p => p.tipo == "sentinela" && p.acesa && perto(p, 14))) Dica("c_sentinela");
                }
            }
            if (dicaT <= 0 && filaDicas.Count > 0)
            {
                var id = filaDicas[0]; filaDicas.RemoveAt(0); DicasVistas().Add(id);
                PlayerPrefs.SetString("rr-dicas", Noite.ParaJson(DicasVistas().ToDictionary(k => k, k => (object)1))); dicaAtual = DICAS[id]; dicaAberta = 8; dicaT = 12;
            }
        }
        // o quadro da dica, à direita (#dica): entra deslizando e some depois de 8 s
        void PainelDica()
        {
            if (dicaAtual == null || g == null || g.phase != "play") return; float op = Mathf.Clamp01(Mathf.Min((8 - dicaAberta) / .5f, dicaAberta / .5f)); if (op <= 0) return;
            float w = 300, x = W - 26 - w + 16 * (1 - op), y = 340; var st = new GUIStyle(sPequeno) { fontSize = 15, wordWrap = true, richText = true };
            string txt = "<size=12>" + Cor("DICA", OURO) + "</size>\n" + Cor(dicaAtual, new Color(.867f, .827f, .761f)); float h = st.CalcHeight(new GUIContent(txt), w - 28) + 20;
            var ou = OURO; ou.a = op; Caixa(new Rect(x, y, w, h), new Color(.031f, .035f, .043f, .82f * op)); Caixa(new Rect(x, y, 2, h), ou); var o = GUI.color; GUI.color = new Color(1, 1, 1, op); GUI.Label(new Rect(x + 14, y + 9, w - 28, h - 14), txt, st); GUI.color = o;
        }
    }
}
