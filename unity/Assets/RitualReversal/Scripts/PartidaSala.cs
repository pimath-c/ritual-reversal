// O resto do online do navegador (titleScreen, lobbyScreen, renderSalas, renderChat e o chat da partida do index.html):
// sala pública e a lista de salas abertas (/api/salas, a cada 5 s, só na tela inicial), a dificuldade dos bots e o
// laboratório no lobby (só o anfitrião muda: {t:'opcoes'}), o chat da sala e, na partida, Enter abre o chat: vai só para
// a sua equipe, Tab troca para a sala toda, Esc cancela. "Chat dos adversários: Silenciar" nos Ajustes esconde o deles.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        string lobbyDif = "normal"; Lab lobbyLab; bool lobbyPublica;
        class LinhaChat { public string de, cid, msg, para, time; }
        readonly List<LinhaChat> chat = new List<LinhaChat>(); string rascunhoLobby = ""; Vector2 rolChat;
        bool SalaPublica { get { return PlayerPrefs.GetInt("rr-sala-publica", 1) == 1; } }
        static bool MudoAdv { get { return PlayerPrefs.GetInt("rr-mudo-adv", 0) == 1; } }

        // o que o hello leva além do nome: se a sala nova aparece na lista e as skins escolhidas (o "vis" do navegador)
        JsonEscrita HelloExtra(JsonEscrita j, bool criar)
        {
            j.Bool("publica", criar && SalaPublica);
            var vis = new JsonEscrita(); var s = Json.Ler(PlayerPrefs.GetString("rr-skins", "")) as Dictionary<string, object>; bool tem = false;
            if (s != null) foreach (var k in new[] { "carabina", "revolver", "cetro" }) { object v; if (s.TryGetValue(k, out v) && v is string) { vis.Txt(k, (string)v); tem = true; } }
            if (tem) j.Obj("vis", vis); return j;
        }
        void LerLobbyExtra(Dictionary<string, object> m)
        {
            object v; lobbyDif = m.TryGetValue("dif", out v) && v is string ? (string)v : "normal"; lobbyPublica = m.TryGetValue("publica", out v) && v is bool && (bool)v;
            var d = m.TryGetValue("lab", out v) ? v as Dictionary<string, object> : null; lobbyLab = d != null ? LabDoJson(Noite.ParaJson(d)) : Lab.De(null);
        }
        void ReceberChat(Dictionary<string, object> m)
        {
            object v; System.Func<string, string> S = k => m.TryGetValue(k, out v) ? v as string : null;
            var c = new LinhaChat { de = S("de") ?? "", cid = S("cid"), msg = S("msg") ?? "", para = S("para") ?? "todos", time = S("time") };
            if (MudoAdv && c.time != null && g != null && c.time != MeuPapel()) return;   // adversários silenciados nos Ajustes
            chat.Add(c); while (chat.Count > 40) chat.RemoveAt(0); rolChat.y = 1e6f;
            if (g != null && g.phase == "play") Feed(c.de + (c.para == "equipe" ? " (equipe)" : c.time != null ? " (para todos)" : "") + ": " + c.msg, "");
            else if (c.cid != EU && som != null) som.Tocar("click");
        }

        // ---------- salas abertas (tela inicial) ----------
        class SalaAberta { public string code, anfitriao; public int jogadores; }
        List<SalaAberta> salas = new List<SalaAberta>(); float salasT = -99; UnityEngine.Networking.UnityWebRequest pedidoSalas;
        void BuscarSalas()
        {
            if (online || g != null) return;
            if (pedidoSalas != null && pedidoSalas.isDone)
            {
                var nova = new List<SalaAberta>();
                try { if (pedidoSalas.result == UnityEngine.Networking.UnityWebRequest.Result.Success) foreach (var o in (Json.Ler(pedidoSalas.downloadHandler.text) as List<object>) ?? new List<object>()) { var d = o as Dictionary<string, object>; if (d == null) continue; object v; nova.Add(new SalaAberta { code = d.TryGetValue("code", out v) ? v as string : "", anfitriao = d.TryGetValue("anfitriao", out v) ? v as string : "", jogadores = d.TryGetValue("jogadores", out v) && v != null ? System.Convert.ToInt32(v) : 0 }); } }
                catch { }
                salas = nova; pedidoSalas.Dispose(); pedidoSalas = null;
            }
            if (pedidoSalas != null || Time.realtimeSinceStartup - salasT < 5) return; salasT = Time.realtimeSinceStartup;
            try { var u = Conexao.Endereco(servidor); pedidoSalas = UnityEngine.Networking.UnityWebRequest.Get((u.Scheme == "wss" ? "https://" : "http://") + u.Authority + "/api/salas"); pedidoSalas.timeout = 4; pedidoSalas.SendWebRequest(); } catch { pedidoSalas = null; }
        }
        // dentro do TelaOnline: a caixa da sala pública e a lista
        void SalasAbertas()
        {
            bool pub = GUILayout.Toggle(SalaPublica, " Sala pública: aparece na lista para qualquer um entrar", new GUIStyle(GUI.skin.toggle) { fontSize = 16 });
            if (pub != SalaPublica) PlayerPrefs.SetInt("rr-sala-publica", pub ? 1 : 0);
            if (salas.Count == 0) return; GUILayout.Label("<b>" + Cor("Salas abertas", OURO) + "</b>", sPequeno);
            foreach (var s in salas.ToList())
            {
                GUILayout.BeginHorizontal(); GUILayout.Label("<b>" + s.code + "</b>", sPequeno, GUILayout.Width(80)); GUILayout.Label(Cor(s.anfitriao + " · " + s.jogadores + " de 4", MUDO), sPequeno, GUILayout.Width(300));
                if (GUILayout.Button("Entrar", sBotao, GUILayout.Width(140))) { codigoDigitado = s.code; Conectar(false, s.code); } GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            }
        }

        // ---------- lobby: dificuldade, laboratório e chat ----------
        void LobbyExtra(bool anfitriao)
        {
            if (lobbySlots.Any(s => s.cid == null))
            {
                GUILayout.BeginHorizontal(); GUILayout.Label("Bots", sPequeno, GUILayout.Width(80)); var DN = new Dictionary<string, string> { { "facil", "Fáceis" }, { "normal", "Normais" }, { "dificil", "Difíceis" } };
                foreach (var k in DN.Keys)
                {
                    if (anfitriao) { if (GUILayout.Button(lobbyDif == k ? Cor(DN[k], OURO_VIVO) : DN[k], sBotao, GUILayout.Width(130))) Enviar(new JsonEscrita().Txt("t", "opcoes").Txt("dif", k)); }
                    else if (lobbyDif == k) GUILayout.Label("<b>" + DN[k] + "</b>", sPequeno, GUILayout.Width(130));
                }
                GUILayout.Label("<size=13>" + Cor("valem para os bots do lado sem jogadores", MUDO) + "</size>", sPequeno); GUILayout.EndHorizontal();
            }
            PainelLab(lobbyLab, anfitriao ? (System.Action<Lab>)(L => Enviar(new JsonEscrita().Txt("t", "opcoes").Obj("lab", LabEscrita(L)))) : null);
            // chat da sala
            GUILayout.BeginVertical(Fundo(new Color(0, 0, 0, .3f)), GUILayout.Height(150)); rolChat = GUILayout.BeginScrollView(rolChat, GUILayout.Height(120));
            if (chat.Count == 0) GUILayout.Label(Cor("Ninguém falou ainda.", MUDO), sPequeno);
            foreach (var c in chat) GUILayout.Label("<b>" + Cor(c.de, c.cid == EU ? OURO_VIVO : OSSO) + "</b>" + (c.para == "equipe" ? " <size=12>(equipe)</size>" : "") + " " + c.msg, sPequeno);
            GUILayout.EndScrollView(); GUILayout.EndVertical();
            GUILayout.BeginHorizontal(); var ev = Event.current; bool enter = ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter) && GUI.GetNameOfFocusedControl() == "chatSala";
            GUI.SetNextControlName("chatSala"); rascunhoLobby = GUILayout.TextField(rascunhoLobby ?? "", 140, GUILayout.ExpandWidth(true), GUILayout.Height(36));
            if ((GUILayout.Button("Enviar", sBotao, GUILayout.Width(130)) || enter) && rascunhoLobby.Trim().Length > 0) { Enviar(new JsonEscrita().Txt("t", "chat").Txt("msg", rascunhoLobby.Trim())); rascunhoLobby = ""; if (enter) ev.Use(); }
            GUILayout.EndHorizontal();
        }
        static JsonEscrita LabEscrita(Lab L) { return new JsonEscrita().Num("crepusculo", L.crepusculo).Txt("fogo", L.fogo).Num("execucao", L.execucao).Bool("economia", L.economia).Txt("formato", L.formato); }

        // ---------- chat na partida online ----------
        bool chatAberto, chatTodos, chatFocar; string chatTexto = ""; int chatAbertoNoQuadro = -1;
        void AbrirChatJogo() { chatAberto = true; chatTodos = false; chatTexto = ""; chatFocar = true; chatAbertoNoQuadro = Time.frameCount; }
        void ChatJogo()
        {
            if (!chatAberto) return; if (!online || g == null || g.phase != "play") { chatAberto = false; return; }
            var ev = Event.current;
            if (ev.type == EventType.KeyDown && Time.frameCount != chatAbertoNoQuadro)
            {
                if (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter) { var t = chatTexto.Trim(); if (t.Length > 0) Enviar(new JsonEscrita().Txt("t", "chat").Txt("msg", t).Txt("para", chatTodos ? "todos" : "equipe")); chatAberto = false; ev.Use(); return; }
                if (ev.keyCode == KeyCode.Tab) { chatTodos = !chatTodos; ev.Use(); }
                else if (ev.keyCode == KeyCode.Escape) { chatAberto = false; ev.Use(); return; }
            }
            var r = new Rect(18, 76, 350, 34); Caixa(r, new Color(.031f, .035f, .043f, .92f)); var cb = chatTodos ? CARMIM_HI : OURO;
            Caixa(new Rect(r.x, r.y, r.width, 1), cb); Caixa(new Rect(r.x, r.yMax - 1, r.width, 1), cb); Caixa(new Rect(r.x, r.y, 1, r.height), cb); Caixa(new Rect(r.xMax - 1, r.y, 1, r.height), cb);
            var st = new GUIStyle(GUI.skin.textField) { fontSize = 15 }; st.normal.background = st.focused.background = null; st.normal.textColor = st.focused.textColor = OSSO;
            GUI.SetNextControlName("chatJogo"); chatTexto = GUI.TextField(new Rect(r.x + 8, r.y + 5, r.width - 16, 24), chatTexto ?? "", 140, st);
            if (chatFocar) { GUI.FocusControl("chatJogo"); chatFocar = false; }
            if (chatTexto.Length == 0) GUI.Label(new Rect(r.x + 10, r.y + 7, r.width - 16, 22), "<size=13>" + Cor(chatTodos ? "Para todos, inclusive os adversários · Tab: só a equipe" : "Para a sua equipe · Tab: para todos · Enter envia, Esc cancela", MUDO) + "</size>", sPequeno);
        }
    }
}
