// HUD e telas da partida, em IMGUI (sem nenhum asset): título, escolha de altares, classe e feitiço, pausa, loja,
// resumo e resultado; durante o jogo, relógio, altares, tarefas, vida, sanidade, munição/fervor, interação, disputa
// do ritual, planta (Tab) e mensagens. Os textos são os do protótipo.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;
using RitualReversal.Visual;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        GUIStyle sTitulo, sTexto, sPequeno, sGrande, sBotao, sCentro, sRelogio;
        Texture2D branco;
        float W, H; // tela virtual: 1080 de altura

        // as fontes do navegador (Resources/Fontes, OFL): Barlow Condensed no corpo, IM Fell English nos títulos; sem elas, a do Unity
        static Font fonteCond, fonteSerif, fonteSerifIt;
        void Estilos()
        {
            if (sTexto != null) return;
            branco = Texture2D.whiteTexture;
            fonteCond = Resources.Load<Font>("Fontes/BarlowCondensed-Medium"); fonteCond400 = Resources.Load<Font>("Fontes/BarlowCondensed-Regular") ?? fonteCond; fonteCond600 = Resources.Load<Font>("Fontes/BarlowCondensed-SemiBold") ?? fonteCond; fonteCond700 = Resources.Load<Font>("Fontes/BarlowCondensed-Bold") ?? fonteCond; fonteSerif = Resources.Load<Font>("Fontes/IMFellEnglish-Regular"); fonteSerifIt = Resources.Load<Font>("Fontes/IMFellEnglish-Italic");
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, richText = true }; sTexto.normal.textColor = OSSO; if (fonteCond != null) sTexto.font = fonteCond;
            sPequeno = new GUIStyle(sTexto) { fontSize = 16 };
            sTitulo = new GUIStyle(sTexto) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = fonteSerif != null ? FontStyle.Normal : FontStyle.Bold }; if (fonteSerif != null) sTitulo.font = fonteSerif;
            sGrande = new GUIStyle(sTexto) { fontSize = 30, alignment = TextAnchor.MiddleCenter }; if (fonteSerif != null) sGrande.font = fonteSerif;
            sCentro = new GUIStyle(sTexto) { alignment = TextAnchor.MiddleCenter };
            sRelogio = new GUIStyle(sTexto) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sBotao = new GUIStyle(GUI.skin.button) { fontSize = 20, wordWrap = true, richText = true, padding = new RectOffset(12, 12, 10, 10) }; if (fonteCond != null) sBotao.font = fonteCond;
        }
        void Caixa(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, branco); GUI.color = o; }
        void Barra(Rect r, float v, Color c) { Caixa(r, new Color(0, 0, 0, .55f)); Caixa(new Rect(r.x, r.y, r.width * Mathf.Clamp01(v), r.height), c); }
        static string Cor(string txt, Color c) { return "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + txt + "</color>"; }
        static string Relogio(double s) { s = System.Math.Max(0, System.Math.Ceiling(s - 1e-9)); int m = (int)(s / 60), ss = (int)(s % 60); return m + ":" + ss.ToString("00"); }   // o fmt do navegador: para cima
        // OnGUI roda mais de uma vez por quadro: os cronômetros do HUD só andam no desenho
        static float dtG { get { return Event.current != null && Event.current.type == EventType.Repaint ? Time.unscaledDeltaTime : 0; } }
        Rect Janela(float w, float h) { var r = new Rect((W - w) / 2, (H - h) / 2, w, h); Caixa(r, new Color(.03f, .03f, .05f, .92f)); return new Rect(r.x + 30, r.y + 24, w - 60, h - 48); }

        void OnGUI()
        {
            Estilos(); ComecoNavPad();
            float esc = Screen.height / 1080f; GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(esc, esc, 1)); W = Screen.width / esc; H = 1080f;
            // um clique que muda a tela no meio da passada deixa o resto dela sem o layout que o Layout calculou: o Unity
            // acusaria "Getting control N's position..." no console; ExitGUI encerra a passada sem erro (o próximo quadro já
            // desenha a tela nova)
            try { Telas(); }
            catch (System.ArgumentException) { if (!cliqueNaPassada) throw; GUIUtility.ExitGUI(); }
        }
        void Telas()
        {
            if (reconectando) { TelaReconexao(); return; }
            if (g == null) { if (online && noLobby) TelaLobby(); else TelaTitulo(); ToastConquista(); return; }
            switch (g.phase)
            {
                case "pick": TelaEscolha(); break;
                case "intro": TelaIntro(); break;
                case "play": if (arsenal) { PainelArsenal(); break; } Hud(); if (lojaAberta != null) TelaLoja(); else if (!Travado()) TelaPausa(); break;
                case "summary": TelaResumo(); break;
                case "final": TelaFinal(); break;
            }
            ToastConquista();
        }

        // ---------- telas ----------
        void TelaEscolha()
        {
            if (MeuPapel() != "C")
            {
                var r0 = Janela(760, 230); GUILayout.BeginArea(r0); GUILayout.Label("Os Cultistas estão escolhendo os altares", sTitulo);
                GUILayout.Label("Você vai caçar nesta rodada. Assim que eles confirmarem, você escolhe sua classe.", sCentro); GUILayout.EndArea(); return;
            }
            float w = Mathf.Min(900, W - 40), hm = (w - 60) * VB_A / VB_L; var r = Janela(w, hm + 250); GUILayout.BeginArea(r);
            GUILayout.Label("Escolham 3 altares", sTitulo);
            int parceiros = g.slots.Count(s => s.cid != null && Sim.RoleOf(g, s.team) == "C");
            GUILayout.Label("Os Caçadores não sabem quais são. Altares perto da sua entrada (leste) são mais rápidos de alcançar, e mais óbvios." + (parceiros > 1 ? " Seu parceiro vê e altera a mesma seleção." : ""), sCentro);
            var rm = GUILayoutUtility.GetRect(r.width, hm); PlantaEscolha(rm);
            GUILayout.Space(8); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (Bt("Sortear", sBotao, GUILayout.Width(200))) Acao(new Acao { type = "pickRandom" });
            GUI.enabled = g.picks.Count == 3; if (Bt("Confirmar " + g.picks.Count + " de 3", sBotao, GUILayout.Width(260))) Acao(new Acao { type = "pickConfirm" }); GUI.enabled = true;
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
        static string Lado(Altar A)
        {
            string ns = A.z < -60 ? "floresta, norte" : A.z > 60 ? "floresta, sul" : A.z < 0 ? "catedral, lado norte" : "catedral, lado sul";
            return ns + (A.x > 20 ? ", leste" : A.x < -20 ? ", oeste" : "");
        }

        void TelaIntro()
        {
            string papel = MeuPapel(); bool Hc = papel == "H"; string meu; if (!g.cls.TryGetValue(EU, out meu)) meu = Regras.CLASS_BY_TEAM[papel][0];
            string fe; if (!g.feit.TryGetValue(EU, out fe)) fe = Regras.FEIT_BY_TEAM[papel][0]; bool pronto; g.ready.TryGetValue(EU, out pronto);
            var LB = g.lab ?? Lab.De(null); var mud = LabMudancas(LB); string crep = Relogio(LB.crepusculo); var CLn = Mundo.CLIMAS[Mundo.ClimaDaRodada(g)];
            var r = Janela(1100, 880); GUILayout.BeginArea(r);
            GUILayout.Label(Cor("Esta noite você é " + (Hc ? "Caçador" : "Cultista"), Hc ? OURO : CARMIM_VIVO), sTitulo);
            GUILayout.Label("<i>" + CLn.nome + ": " + CLn.sub.ToLowerInvariant() + ".</i>", sTexto);
            GUILayout.Label(Hc
                ? "No <b>Crepúsculo</b> (" + crep + "), leia os sinais nos santuários da floresta, que descartam altares que o culto não escolheu, e acenda sentinelas: elas avisam quando alguém consagra um altar por perto." + (LB.economia ? " Negocie com o Ermitão, na cabana perto do acampamento." : "") + " Fique nas trilhas: a mata fechada é escura e abala a sanidade. Na <b>Vigília</b>, os altares despertam: purgue, investigue e sele. Quando um ritual começa, o coro logo ecoa o setor dele: dois altares possíveis, e o que você descartou decide para onde correr. Resista até o amanhecer."
                : "No <b>Crepúsculo</b>, colha ervas na floresta, profane túmulos no cemitério, ao leste da catedral, e consagre seus altares em silêncio." + (LB.economia ? " Negocie com a Carpideira, no oratório perto do seu acampamento." : "") + " Os reagentes ficam na mata, ao sul do desfiladeiro (e nas cabeceiras das pontes). Agachado (C) você anda devagar e quase sem barulho. Na <b>Vigília</b>, os rituais podem começar. O último só na <b>Hora Morta</b>, e a catedral inteira vai ouvir. Quem ataca não canaliza; você sente quando a caça começa a selar o seu ritual.", sTexto);
            if (mud.Count > 0) GUILayout.Label(Cor("Laboratório de playtest: " + string.Join(" · ", mud.ToArray()) + ".", OURO_VIVO), sPequeno);
            GUILayout.Space(8); GUILayout.Label("<b>Arquétipo</b>", sTexto); GUILayout.BeginHorizontal();
            foreach (var c in Regras.CLASS_BY_TEAM[papel]) { var C = Regras.CLASSES[c]; if (Bt((c == meu ? Cor("● ", OURO) : "") + "<b>" + C.name + "</b>\n" + C.text + "\n<size=15>" + C.hp + " de vida</size>", sBotao, GUILayout.Height(170)) && !pronto) Acao(new Acao { type = "cls", cls = c }); }
            GUILayout.EndHorizontal(); GUILayout.Space(8); GUILayout.Label("<b>Feitiço</b> (clique direito)", sTexto); GUILayout.BeginHorizontal();
            foreach (var f in Regras.FEIT_BY_TEAM[papel]) { var F = Regras.FEITICOS[f]; if (Bt((f == fe ? Cor("● ", OURO) : "") + "<b>" + F.nome + "</b>\n" + F.text + "\n<size=15>recarga de " + F.cd + " s</size>", sBotao, GUILayout.Height(150)) && !pronto) Acao(new Acao { type = "feit", f = f }); }
            GUILayout.EndHorizontal(); GUILayout.Space(14);
            GUI.enabled = !pronto; if (Bt(pronto ? "Aguardando os outros" : "Estou pronto", sBotao, GUILayout.Height(56))) Acao(new Acao { type = "ready" }); GUI.enabled = true;
            GUILayout.Label((online ? ListaProntos() : "") + TextoTempo(), sCentro);
            GUILayout.EndArea();
        }

        // ajustes (settingsScreen do protótipo): ficam salvos entre as sessões
        static readonly string[] NIVEIS = { "Mínimo", "Baixo", "Médio", "Alto" };
        void Ajustes()
        {
            GUILayout.BeginHorizontal(); GUILayout.Label("Sensibilidade " + sensibilidade.ToString("0.00"), sPequeno, GUILayout.Width(230)); sensibilidade = GUILayout.HorizontalSlider(sensibilidade, .2f, 3f, GUILayout.Width(380)); GUILayout.EndHorizontal();
            float vol = som != null ? som.volume : .8f;
            GUILayout.BeginHorizontal(); GUILayout.Label("Volume " + Mathf.RoundToInt(vol * 100) + "%", sPequeno, GUILayout.Width(230)); vol = GUILayout.HorizontalSlider(vol, 0, 1, GUILayout.Width(380)); GUILayout.EndHorizontal();
            if (som != null) som.volume = vol;
            float fov = fp != null ? fp.fovBase : 72;
            GUILayout.BeginHorizontal(); GUILayout.Label("Campo de visão " + Mathf.RoundToInt(fov) + "°", sPequeno, GUILayout.Width(230)); fov = Mathf.Round(GUILayout.HorizontalSlider(fov, 60, 100, GUILayout.Width(380))); GUILayout.EndHorizontal();
            if (fp != null) fp.fovBase = fov;
            // v5.8: impacto na câmera (coice, empurrões e tremores): Normal, Reduzido ou Desligado
            GUILayout.BeginHorizontal(); GUILayout.Label("Impacto na câmera", sPequeno, GUILayout.Width(230));
            float cf = fp != null ? fp.camFx : PlayerPrefs.GetFloat("rr-camfx", 1);
            foreach (var o in new[] { new KeyValuePair<float, string>(1, "Normal"), new KeyValuePair<float, string>(.5f, "Reduzido"), new KeyValuePair<float, string>(0, "Desligado") })
                if (Bt((Mathf.Approximately(cf, o.Key) ? "● " : "") + o.Value, sBotao, GUILayout.Width(125))) { if (fp != null) fp.camFx = o.Key; PlayerPrefs.SetFloat("rr-camfx", o.Key); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Label("Qualidade (vale no próximo Play)", sPequeno, GUILayout.Width(230));
            int q = PlayerPrefs.GetInt("rr-qualidade", Qualidade.nivel);
            for (int i = 3; i >= 0; i--) if (Bt((q == i ? "● " : "") + NIVEIS[i], sBotao, GUILayout.Width(110))) PlayerPrefs.SetInt("rr-qualidade", i);
            GUILayout.EndHorizontal();
            // dicas, legendas e acessibilidade (como no navegador)
            System.Action<string, string, string[][]> linha = (rotulo, chave, ops) =>
            {
                GUILayout.BeginHorizontal(); GUILayout.Label(rotulo, sPequeno, GUILayout.Width(230)); int v = PlayerPrefs.GetInt(chave, int.Parse(ops[0][0]));
                foreach (var o in ops) if (Bt((v == int.Parse(o[0]) ? "● " : "") + o[1], sBotao, GUILayout.Width(o[1].Length > 12 ? 170 : 125))) PlayerPrefs.SetInt(chave, int.Parse(o[0]));
                GUILayout.EndHorizontal();
            };
            linha("Dicas da primeira noite", "rr-dicas-on", new[] { new[] { "1", "Ligadas" }, new[] { "0", "Desligadas" } });
            GUILayout.BeginHorizontal(); GUILayout.Space(234); if (Bt("Rever todas as dicas", sBotao, GUILayout.Width(254))) RevisarDicas(); GUILayout.EndHorizontal();
            linha("Legendas dos sons", "rr-legendas", new[] { new[] { "1", "Ligadas" }, new[] { "0", "Desligadas" } });
            linha("Clarões e tremor", "rr-suave", new[] { new[] { "0", "Normais" }, new[] { "1", "Reduzidos" } });
            linha("Cores das equipes", "rr-dalt", new[] { new[] { "0", "Ouro e carmim" }, new[] { "1", "Ouro e azul" } });
            linha("Chat dos adversários", "rr-mudo-adv", new[] { new[] { "0", "Mostrar" }, new[] { "1", "Silenciar" } });
            linha("Música da tela inicial", "rr-musica", new[] { new[] { "1", "Ligada" }, new[] { "0", "Desligada" } });
            // mira e brilho
            GUILayout.BeginHorizontal(); GUILayout.Label("Mira", sPequeno, GUILayout.Width(230)); string mira = PlayerPrefs.GetString("rr-mira", "cruz");
            foreach (var o in new[] { new[] { "cruz", "Cruz" }, new[] { "ponto", "Ponto" }, new[] { "circulo", "Círculo" } }) if (Bt((mira == o[0] ? "● " : "") + o[1], sBotao, GUILayout.Width(125))) PlayerPrefs.SetString("rr-mira", o[0]);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Label("Cor da mira", sPequeno, GUILayout.Width(230)); string mc = PlayerPrefs.GetString("rr-mira-cor", "#e8e0d0");
            foreach (var o in new[] { new[] { "#e8e0d0", "Osso" }, new[] { "#7dff9a", "Verde" }, new[] { "#ffe066", "Amarelo" }, new[] { "#8ee8ff", "Ciano" } }) { Color cc; ColorUtility.TryParseHtmlString(o[0], out cc); if (Bt((mc == o[0] ? "● " : "") + Cor(o[1], cc), sBotao, GUILayout.Width(110))) PlayerPrefs.SetString("rr-mira-cor", o[0]); }
            GUILayout.EndHorizontal();
            float mt = PlayerPrefs.GetFloat("rr-mira-tam", 1);
            GUILayout.BeginHorizontal(); GUILayout.Label("Tamanho da mira " + Mathf.RoundToInt(mt * 100) + "%", sPequeno, GUILayout.Width(230)); float mt2 = Mathf.Round(GUILayout.HorizontalSlider(mt, .6f, 1.8f, GUILayout.Width(380)) * 10) / 10; GUILayout.EndHorizontal(); if (mt2 != mt) PlayerPrefs.SetFloat("rr-mira-tam", mt2);
            float br = PlayerPrefs.GetFloat("rr-brilho", 1);
            GUILayout.BeginHorizontal(); GUILayout.Label("Brilho " + Mathf.RoundToInt(br * 100) + "%", sPequeno, GUILayout.Width(230)); float br2 = Mathf.Round(GUILayout.HorizontalSlider(br, .6f, 1.8f, GUILayout.Width(380)) * 20) / 20; GUILayout.EndHorizontal(); if (br2 != br) PlayerPrefs.SetFloat("rr-brilho", br2);
            if (pos != null) pos.brilho = PlayerPrefs.GetFloat("rr-brilho", 1);
            // controles: clique numa linha e aperte a tecla ou o botão do mouse (Esc cancela; se a tecla já estiver em uso, as duas trocam)
            GUILayout.Space(10); GUILayout.Label("<b>" + Cor("CONTROLES", OURO) + "</b>", sPequeno);
            foreach (var a in EntradaUnity.ACOES)
            {
                GUILayout.BeginHorizontal(); GUILayout.Label("<size=15>" + a[1] + "</size>", sPequeno, GUILayout.Width(420));
                if (Bt(capturando == a[0] ? Cor("aperte…", OURO_VIVO) : EntradaUnity.Nome(a[0]), sBotao, GUILayout.Width(190))) { capturando = a[0]; capturaQuadro = Time.frameCount; }
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("<size=14>" + Cor("Clique numa linha e aperte a tecla ou o botão do mouse. Esc cancela. Se a tecla já estiver em uso, as duas ações trocam de lugar.", MUDO) + "</size>", sPequeno);
            if (Bt("Restaurar padrão", sBotao, GUILayout.Width(260))) { EntradaUnity.Padrao(); capturando = null; }
            if (mundo != null) mundo.claroesSuaves = PlayerPrefs.GetInt("rr-suave", 0) == 1;
            GUILayout.Label("<size=14>" + Cor("Reduzidos: relâmpagos ficam mais fracos. Ouro e azul troca o carmim do culto por azul na planta e no replay (ajuda com daltonismo vermelho-verde).", MUDO) + "</size>", sPequeno);
            if (GUI.changed) SalvarAjustes();
        }
        void SalvarAjustes() { PlayerPrefs.SetFloat("rr-sens", sensibilidade); if (som != null) PlayerPrefs.SetFloat("rr-vol", som.volume); if (fp != null) PlayerPrefs.SetFloat("rr-fov", fp.fovBase); }

        // pausa (local) ou mouse solto (online, a partida não para): a cola das teclas do seu papel, Ajustes e sair
        bool pausaAjustes;
        void TelaPausa()
        {
            bool comRolagem = pausaAjustes;   // o botão pode mudar pausaAjustes no meio da passada: Begin e End seguem o começo
            var r = Janela(Mathf.Min(900, W - 40), comRolagem ? H - 60 : 520); GUILayout.BeginArea(r); if (comRolagem) rolPerfil = GUILayout.BeginScrollView(rolPerfil);
            GUILayout.Label(online ? "Clique para jogar" : "Pausado", sTitulo);
            GUILayout.Label(online ? "No modo online a partida não pausa: enquanto o mouse estiver solto, seu personagem fica parado." : "Clique em Continuar para voltar ao jogo.", sCentro);
            if (comRolagem) { Ajustes(); }
            else
            {
                string papel = MeuPapel(); System.Func<string, string> kb = EntradaUnity.Nome; string andar = kb("fwd") == "W" && kb("left") == "A" && kb("back") == "S" && kb("right") == "D" ? "WASD" : kb("fwd") + " " + kb("left") + " " + kb("back") + " " + kb("right");
                var L = new List<string[]> { new[] { andar, "Andar" }, new[] { kb("sprint"), "Correr" }, new[] { kb("agachar"), "Agachar" }, new[] { kb("fire"), "Atacar" }, new[] { kb("abil"), "Feitiço" }, new[] { kb("use"), "Interagir" } };
                if (papel == "H") { L.Add(new[] { kb("sensor"), "Sensor" }); L.Add(new[] { kb("lantern"), "Lanterna" }); L.Add(new[] { kb("flare"), "Flare" }); L.Add(new[] { kb("reload"), "Recarregar" }); } else L.Add(new[] { kb("use2"), "Transferir" });
                L.Add(new[] { kb("map"), "Planta" }); L.Add(new[] { kb("caderno"), "Caderno" }); L.Add(new[] { kb("ping"), "Ping" }); L.Add(new[] { kb("falas"), "Frases" }); L.Add(new[] { kb("inspecionar"), "Inspecionar" }); if (online) L.Add(new[] { "Enter", "Chat" });
                GUILayout.Space(8);
                for (int i = 0; i < L.Count; i += 4) { GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); for (int k = i; k < Mathf.Min(L.Count, i + 4); k++) GUILayout.Label("<b>" + Cor(L[k][0], OURO_VIVO) + "</b> " + L[k][1], sPequeno, GUILayout.Width(190)); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); }
            }
            GUILayout.Space(12); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (Bt("Continuar", sBotao, GUILayout.Width(220), GUILayout.Height(52))) { pausaAjustes = false; Travar(true); }
            if (Bt(pausaAjustes ? "Voltar" : "Ajustes", sBotao, GUILayout.Width(200), GUILayout.Height(52))) pausaAjustes = !pausaAjustes;
            if (Bt(online ? "Sair da sala" : "Sair da noite", sBotao, GUILayout.Width(220), GUILayout.Height(52))) { pausaAjustes = false; if (online) SairOnline(""); else { g = null; LimparVisual(); } }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            if (comRolagem) GUILayout.EndScrollView(); GUILayout.EndArea();
        }

        void TelaLoja()
        {
            var n = Mapa.NPCS.Find(x => x.id == lojaAberta); var m = Eu(); if (n == null || m == null) return;
            var r = Janela(760, 120 + 92 * n.vende.Count); GUILayout.BeginArea(r);
            GUILayout.Label(char.ToUpper(n.nome[0]) + n.nome.Substring(1) + "   " + Cor(m.obolos + " óbolos", OURO), sGrande);
            foreach (var id in n.vende)
            {
                var it = Regras.ITENS[id]; bool serve = (it.time == null || it.time == m.team) && (n.time == null || n.time == m.team);
                GUI.enabled = serve && m.obolos >= it.preco;
                if (Bt("<b>" + it.nome + "</b>  " + Cor(it.preco + " óbolos", OURO) + "\n<size=16>" + it.desc + "</size>", sBotao, GUILayout.Height(80))) Acao(new Acao { type = "comprar", npc = n.id, item = id });
                GUI.enabled = true;
            }
            GUILayout.Label("E ou Esc fecha a loja.", sPequeno); GUILayout.EndArea();
        }

        static readonly string[][] LINHAS = {
            new[]{"rituais","Rituais iniciados"}, new[]{"completos","Rituais completos"}, new[]{"selados","Rituais selados"}, new[]{"chegadas","Caçador chegou ao círculo"},
            new[]{"tentativasSelo","Tentativas de selamento"}, new[]{"cancelados","Selamentos interrompidos"}, new[]{"fracaoSobFogo","Tempo selando sob fogo"}, new[]{"primeiroContato","Primeiro contato"},
            new[]{"quedas","Quedas"}, new[]{"reanimacoes","Reanimações"}, new[]{"purgas","Purgas"}, new[]{"chamarizes","Chamarizes"}, new[]{"transferencias","Transferências"} };
        static string Valor(Estatisticas e, string k)
        {
            switch (k)
            {
                case "rituais": return e.rituais.ToString(); case "completos": return e.completos.ToString(); case "selados": return e.selados.ToString(); case "chegadas": return e.chegadas.ToString();
                case "tentativasSelo": return e.tentativasSelo.ToString(); case "cancelados": return e.cancelados.ToString();
                case "fracaoSobFogo": return e.fracaoSobFogo.HasValue ? Mathf.RoundToInt((float)e.fracaoSobFogo.Value * 100) + "%" : "—";
                case "primeiroContato": return e.primeiroContato.HasValue ? Relogio(e.primeiroContato.Value) : "—";
                case "quedas": return e.quedas.ToString(); case "reanimacoes": return e.reanimacoes.ToString(); case "purgas": return e.purgas.ToString(); case "chamarizes": return e.chamarizes.ToString();
                default: return e.transferencias.ToString();
            }
        }
        // o #interact do navegador: o anel de progresso (46 px, logo abaixo da mira) e os rótulos com a tecla numa caixinha dourada
        void Rotulo(float y, string tecla, string txt)
        {
            // #interact .lbl: 18 px, fundo escuro, padding 3 × 10; a tecla em kbd dourado com moldura e 6 px depois dela
            var st = Css("400", 18, BONE); var sk = Css("700", 18, GOLD); float lh = 18 * 1.2f;
            float wk = tecla != null ? Larg(tecla, sk) + 10 + 2 : 0, wt = Larg(txt.Replace("<", "<\u200B"), st), w = wk + (tecla != null ? 6 : 0) + wt + 20, x = W / 2 - w / 2;
            Caixa(new Rect(x, y, w, lh + 6), new Color(8 / 255f, 9 / 255f, 11 / 255f, .72f));
            if (tecla != null) { Moldura(new Rect(x + 10, y + 3, wk, lh), GOLD); Linha(x + 10 + 6, y + 3, wk, lh, tecla, sk); }
            Linha(x + 10 + wk + (tecla != null ? 6 : 0), y + 3, wt + 4, lh, txt, st);
        }
        void InteracaoHud(Ator m)
        {
            var ctx = Sim.Contexts(g, m); if (ctx.e == null && ctx.t == null && string.IsNullOrEmpty(ctx.info)) return;
            float anel = m.sealing >= 0 ? (float)g.altars[m.sealing].seal : m.hold != null && m.hold.max > 0 ? (float)(m.hold.t / m.hold.max) : 0f;
            float cx = W / 2, cy = H / 2 + 46 + 23; Tracos.Anel(cx, cy, 19, 4, new Color(1, 1, 1, .18f)); if (anel > 0) Tracos.Arco(cx, cy, 19, 4, Mathf.Clamp01(anel), Desenho.Hex("#f3d58e"));
            float y = cy + 23 + 4 + 3;   // o svg de 46 px, margin-bottom 4, e o margin-top 3 do rótulo
            if (ctx.e != null) { Rotulo(y, EntradaUnity.Nome("use"), ctx.e.label.Replace("Segure E para ", "Segure para ")); y += 18 * 1.2f + 6 + 3; }
            else if (!string.IsNullOrEmpty(ctx.info)) { Rotulo(y, null, ctx.info); y += 18 * 1.2f + 6 + 3; }
            if (ctx.t != null) Rotulo(y, EntradaUnity.Nome("use2"), ctx.t.label.Replace("Segure T para ", "Segure: "));
        }

        // ---------- HUD ----------
        // o véu da execução: o centro limpo, a borda fechando em sangue escuro (o #exeVeu do navegador)
        Texture2D texVeuExe;
        void VeuExecucao()
        {
            if (veuExe < .01f) return;
            if (texVeuExe == null)
            {
                int N = 128; texVeuExe = new Texture2D(N, N, TextureFormat.RGBA32, false); var px = new Color32[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                    {
                        float dx = (x + .5f) / N * 2 - 1, dy = (y + .5f) / N * 2 - 1, d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f; Color c;
                        if (d < .18f) c = new Color(0, 0, 0, 0); else if (d < .62f) c = Color.Lerp(new Color(30 / 255f, 0, 6 / 255f, 0), new Color(30 / 255f, 0, 6 / 255f, .75f), (d - .18f) / .44f); else c = Color.Lerp(new Color(30 / 255f, 0, 6 / 255f, .75f), new Color(0, 0, 0, .98f), (d - .62f) / .38f);
                        px[y * N + x] = c;
                    }
                texVeuExe.SetPixels32(px); texVeuExe.Apply(); texVeuExe.wrapMode = TextureWrapMode.Clamp;
            }
            var o = GUI.color; GUI.color = new Color(1, 1, 1, veuExe); GUI.DrawTexture(new Rect(0, 0, W, H), texVeuExe, ScaleMode.StretchToFill); GUI.color = o;
        }
        void Hud()
        {
            var m = Eu(); if (m == null) return; string papel = MeuPapel(); float agora = Time.unscaledTime;
            VeuExecucao();
            FaixasEscuras();
            // relógio e momento
            int mi = System.Math.Max(0, g.momento); string prox = mi + 1 < CFG.momentoNome.Length ? CFG.momentoNome[mi + 1] : null;
            var GA = g.altars.FirstOrDefault(A => A.state == "active" && A.grande);
            string nome = GA != null ? "Grande Ritual em " + GA.name + (papel == "C" || GA.localized ? ": " + Mathf.FloorToInt((float)GA.prog * 100) + "%" : "") : prox != null ? CFG.momentoNome[mi] + ", " + prox + " em seguida" : CFG.momentoNome[mi] + ", depois o amanhecer";
            int fendas = Sim.Fendas(g), farois = Sim.Farois(g), nos = papel == "C" ? fendas : farois, eles = papel == "C" ? farois : fendas;
            string regra = g.lab != null && g.lab.formato == "placar" ? "2 rituais decidem · a noite vai até o amanhecer" : "Quem levar 2 dos 3 rituais vence";
            Topo(papel, Relogio(Sim.FimMomento(g, mi) - g.rt), nome, GA != null, regra, nos, eles);
            Faixa(papel); Tarefas(papel); FeedHud(); AnuncioHud();
            Vitais(m, g.lab == null || g.lab.economia); KitHud(m); LinhaRede();
            // #ammoHint embaixo da mira e #buffs embaixo da faixa
            string dica = "";
            if (m.st == "alive") { if (m.team == "H") { if (m.reloadT <= 0 && m.ammo == 0) dica = m.reserve > 0 ? "Sem munição: " + EntradaUnity.Nome("reload") + " para recarregar" : "Sem munição: volte ao spawn para repor"; else if (m.reloadT <= 0 && m.ammo <= 2) dica = "Munição baixa (" + m.ammo + ")"; } else if (m.fervor < CFG.sigilCusto) dica = "Fervor esgotado: pare de conjurar um instante para recuperar"; }
            if (dica != "") Linha(0, H / 2 + 30, W, 18, dica, Css("400", 15, GOLD_HI, TextAnchor.UpperCenter), .9f);
            var bf = new List<string>(); if (papel == "H" && g.buff.ess > g.t) bf.Add("Essência Profana: selamento a 125% (" + Mathf.CeilToInt((float)(g.buff.ess - g.t)) + " s)"); if (papel == "C" && g.buff.selo > g.t) bf.Add("Selo de Luz: próximo ritual +10% (" + Mathf.CeilToInt((float)(g.buff.selo - g.t)) + " s)"); if (m.runeT > 0) bf.Add("Runa ativa (" + Mathf.CeilToInt((float)m.runeT) + " s)");
            if (bf.Count > 0) Linha(0, 182, W, 18, string.Join("   ", bf.ToArray()), Css("400", 15, GOLD_HI, TextAnchor.UpperCenter), .9f);

            // mira, acerto, marcadores, dano, nota da habilidade, interação e a disputa do ritual
            Mira(); MarcaAcerto(); Marcadores(m, papel); DirecaoDano(); NotaHabilidade(); InteracaoHud(m); Disputa(m, papel);

            // tela: dano, cegueira, caído ou morto
            if (hurtT > 0) { hurtT -= dtG; Caixa(new Rect(0, 0, W, H), new Color(.6f, 0, 0, hurtT / .35f * .35f)); }
            if (blindT > 0) { blindT -= dtG; Caixa(new Rect(0, 0, W, H), new Color(1, 1, 1, Mathf.Clamp01(blindT / 1.2f))); }
            if (atordT > 0) { atordT -= dtG; Caixa(new Rect(0, 0, W, H), new Color(.95f, .84f, .55f, atordT * .35f)); }
            if (claraoGrande > 0) { claraoGrande -= dtG; Caixa(new Rect(0, 0, W, H), new Color(.8f, .05f, .1f, Mathf.Clamp01(claraoGrande / 1.2f) * .85f)); }
            TelaQueda(m);

            Bussola(m, papel); Legendas(); FaixaLance(); Caderno(papel); PainelTutorial(); PainelDica(); ChatJogo(); PainelFalas();
            if (EntradaUnity.Segurando(Tecla.Tab)) Planta(m, papel);
        }

        // Marcadores na tela: aliados (inclusive caídos), mercadores perto, Cultistas denunciados pelo sal e o ritual revelado
        void Marcadores(Ator m, string papel)
        {
            var c = CamP(); System.Func<double, double, int> dm = (x, z) => Mathf.RoundToInt(Mathf.Sqrt((float)((x - c.x) * (x - c.x) + (z - c.z) * (z - c.z))));
            foreach (var a in g.actors)
            {
                if (a == m || a.hidden || a.team != m.team || a.st == "dead") continue;
                if (a.st == "down") Marcador(new Vector3((float)a.x, 1.1f, (float)a.z), a.name, "caído, " + dm(a.x, a.z) + " m", "caido", true);
                else Marcador(new Vector3((float)a.x, 2.35f, (float)a.z), a.name, null, m.team == "C" ? "cult" : "", false);
            }
            if (g.lab == null || g.lab.economia) foreach (var n in Mapa.NPCS) if (dm(n.x, n.z) < 14 && (n.time == null || n.time == m.team)) Marcador(new Vector3((float)n.x, 2.3f, (float)n.z), char.ToUpper(n.nome[0]) + n.nome.Substring(1), "mercador", "npc", false);
            if (papel == "H") foreach (var a in g.actors) if (a.team == "C" && !a.hidden && a.revelT > 0 && a.st == "alive") Marcador(new Vector3((float)a.x, 2.4f, (float)a.z), a.name, "denunciado pelo sal", "caido", true);
            // o objetivo do tutorial (altares no alto, pontos do chão mais baixos)
            if (tutAtivo && tutAlvo != null && !tutFim) Marcador(new Vector3((float)tutAlvo.x, tutAlvo is Altar ? 3.4f : 1.4f, (float)tutAlvo.z), "Objetivo", dm(tutAlvo.x, tutAlvo.z) + " m", "obj", true);
            for (int i = pings.Count - 1; i >= 0; i--) { var p = pings[i]; p.t -= dtG; if (p.t <= 0) { pings.RemoveAt(i); continue; } Marcador(new Vector3(p.x, p.txt.StartsWith("Altar") ? 3.6f : .8f, p.z), p.txt, p.nome + " · " + dm(p.x, p.z) + " m", "ping", true); }
            foreach (var A in g.altars) { if (A.state != "active" || (papel == "H" && !A.localized)) continue; Marcador(new Vector3((float)A.x, 3.2f, (float)A.z), (A.grande ? "Grande Ritual" : "Ritual") + " em " + A.name, dm(A.x, A.z) + " m", "obj", true); }
        }

    }
}
