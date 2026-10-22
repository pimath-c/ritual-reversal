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
        readonly List<LinhaChat> chat = new List<LinhaChat>(); string rascunhoLobby = "";
        bool SalaPublica { get { return PlayerPrefs.GetInt("rr-sala-publica", 1) == 1; } }
        static bool MudoAdv { get { return PlayerPrefs.GetInt("rr-mudo-adv", 0) == 1; } }

        // o que o hello leva além do nome: se a sala nova aparece na lista e as skins escolhidas (o "vis" do navegador)
        JsonEscrita HelloExtra(JsonEscrita j, bool criar)
        {
            j.Bool("publica", criar && SalaPublica);
            var vis = new JsonEscrita(); string sk = PlayerPrefs.GetString("rr-skins", ""); var s = sk.Length > 0 ? Json.Ler(sk) as Dictionary<string, object> : null; bool tem = false;   // sem skin escolhida, a chave nem existe
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
            chat.Add(c); while (chat.Count > 40) chat.RemoveAt(0);
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
            // #chatJogo input: 350 px a 18, 76; 15 px no sans-serif do sistema, padding 7 × 10, fundo escuro e borda de 1 px em
            // ouro (carmim quando vai para todos); o placeholder no cinza do navegador
            var r = new Rect(18, 76, 350, LNsans(15) + 14 + 2); var cb = chatTodos ? CrimsonHi : GOLD; string k = chatTodos ? "chatTodos" : "chatEquipe";
            GUIStyle st; if (!estilosBotao.TryGetValue(k, out st))
            {
                st = new GUIStyle(Css("sans", 15, BONE)) { padding = new RectOffset(11, 11, 8, 8), border = new RectOffset(1, 1, 1, 1), clipping = TextClipping.Clip, alignment = TextAnchor.UpperLeft };
                var t = TexBotao(new Color(8 / 255f, 9 / 255f, 11 / 255f, .92f), new Color(8 / 255f, 9 / 255f, 11 / 255f, .92f), cb, false); st.normal.background = st.focused.background = st.hover.background = t;
                st.normal.textColor = st.focused.textColor = st.hover.textColor = BONE; estilosBotao[k] = st;
            }
            GUI.SetNextControlName("chatJogo"); chatTexto = GUI.TextField(r, chatTexto ?? "", 140, st);
            if (chatFocar) { GUI.FocusControl("chatJogo"); chatFocar = false; }
            if (chatTexto.Length == 0) Linha(r.x + 11, r.y + 8, r.width - 22, LNsans(15), chatTodos ? "Para todos, inclusive os adversários · Tab: só a equipe" : "Para a sua equipe · Tab: para todos · Enter envia, Esc cancela", Css("sans", 15, Hx("#757575")));
        }
    }
}
