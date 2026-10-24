// O texto e as caixas do HUD como o CSS do navegador os põe: cada linha numa caixa de altura line-height com a linha de
// base onde o navegador a põe (meia entrelinha em cima, depois a subida da fonte), quebra de linha feita aqui (o GUIStyle
// não tem line-height), sombra do texto (text-shadow 0 1px 2px #000) com uma cópia escura um pixel abaixo, molduras de 1 px,
// degradês (os linear-gradient das barras e das faixas do HUD) e o brilho suave dos box-shadow. As fontes do navegador:
// Barlow Condensed em quatro pesos (400 do corpo, 500, 600 dos números, 700 das teclas) e IM Fell English.
using System.Collections.Generic;
using UnityEngine;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        static Font fonteCond400, fonteCond600, fonteCond700;
        // a fonte dos símbolos que a Barlow e a IM Fell não têm (◆ ✓ ✕ ✚ ✶ ☉...): no navegador eles vêm da fonte reserva do
        // sistema, a DejaVu Sans no Linux; aqui ela vem junto, para não depender da reserva do sistema (no Android e no
        // Windows faltam glifos)
        static Font fonteSimbolo, fonteSimboloN;
        // subida e descida de cada fonte (hhea, em em): a altura "normal" da linha no navegador é a soma
        static float SubidaEm(Font f) { return f == null ? 1f : f == fonteSistema ? subSis : f == fonteSans ? subSans : f == fonteSerif || f == fonteSerifIt ? .905f : f == fonteSimbolo || f == fonteSimboloN ? .928f : 1f; }
        static float DescidaEm(Font f) { return f == null ? .2f : f == fonteSistema ? descSis : f == fonteSans ? descSans : f == fonteSerif || f == fonteSerifIt ? .363f : f == fonteSimbolo || f == fonteSimboloN ? .236f : .2f; }
        // o system-ui do navegador (o painel do Arsenal e o campo do chat da partida): a fonte da interface do sistema
        // operacional, com as métricas dela (hhea); a primeira da lista que estiver instalada
        static Font fonteSistema; static float subSis = .928f, descSis = .236f;
        static readonly string[][] FONTES_SISTEMA = { new[] { "Segoe UI", "1.0791", ".2510" }, new[] { "Roboto", ".9277", ".2441" }, new[] { "Helvetica Neue", ".9520", ".2130" },
            new[] { "Ubuntu", ".9320", ".1890" }, new[] { "DejaVu Sans", ".9282", ".2358" }, new[] { "Arial", ".9053", ".2119" }, new[] { "Liberation Sans", ".9053", ".2119" } };
        // o sans-serif genérico (o campo do chat da partida): Arial no Windows, Helvetica no Mac, Roboto no Android
        static Font fonteSans; static float subSans = .928f, descSans = .236f;
        static readonly string[][] FONTES_SANS = { new[] { "Arial", ".9053", ".2119" }, new[] { "Helvetica", ".9160", ".2120" }, new[] { "Roboto", ".9277", ".2441" },
            new[] { "DejaVu Sans", ".9282", ".2358" }, new[] { "Liberation Sans", ".9053", ".2119" } };
        static void CarregarFonteSistema()
        {
            if (fonteSistema == null) fonteSistema = FonteDoSistema(FONTES_SISTEMA, ref subSis, ref descSis);
            if (fonteSans == null) fonteSans = FonteDoSistema(FONTES_SANS, ref subSans, ref descSans);
        }
        static Font FonteDoSistema(string[][] lista, ref float sub, ref float desc)
        {
            string[] inst; try { inst = Font.GetOSInstalledFontNames(); } catch { inst = new string[0]; }
            foreach (var f in lista)
                if (System.Array.IndexOf(inst, f[0]) >= 0)
                {
                    var fo = Font.CreateDynamicFontFromOSFont(f[0], 16); if (fo == null) continue;
                    sub = float.Parse(f[1], System.Globalization.CultureInfo.InvariantCulture); desc = float.Parse(f[2], System.Globalization.CultureInfo.InvariantCulture); return fo;
                }
            sub = 1; desc = .2f; return null;   // nenhuma achada: fica a Barlow (o Css troca "sys" e "sans" por ela)
        }
        static float LNsis(float px) { return Ar(px * subSis) + Ar(px * descSis); }
        static float LNsans(float px) { return Ar(px * subSans) + Ar(px * descSans); }
        // o line-height: normal do navegador: a subida e a descida arredondadas, cada uma, para o pixel (as duas fontes não
        // têm entrelinha extra); LN e LNs são a mesma conta para a Barlow e para a IM Fell num tamanho qualquer
        static float Ar(float v) { return Mathf.Floor(v + .5f); }
        static float SubidaPx(GUIStyle st) { return Ar(SubidaEm(st.font) * st.fontSize); }
        static float AlturaNormal(GUIStyle st) { return SubidaPx(st) + Ar(DescidaEm(st.font) * st.fontSize); }
        static float LN(float px) { return Ar(px) + Ar(px * .2f); }
        // line-height normal quando a linha usa um glifo que a fonte não tem: o Chromium junta as medidas da fonte reserva
        // (a DejaVu Sans: subida .928, descida .236) às da fonte, e a linha cresce (o ☉ de 12 px dá 15 no lugar de 14)
        static float AlturaNormalCom(string txt, GUIStyle st)
        {
            bool falta = false; if (st.font != null) foreach (var ch in txt) if (ch > ' ' && !st.font.HasCharacter(ch)) { falta = true; break; }
            if (!falta) return AlturaNormal(st);
            return Mathf.Max(SubidaPx(st), Ar(.928f * st.fontSize)) + Mathf.Max(AlturaNormal(st) - SubidaPx(st), Ar(.236f * st.fontSize));
        }
        // o meio de um vertical-align: middle numa linha normal da Barlow (topo em 0): a linha de base menos meia altura-x (.506)
        static float MeioX(float px) { return Ar(px) - .253f * px; }
        static float LNs(float px) { return Ar(px * .905f) + Ar(px * .363f); }

        readonly Dictionary<string, GUIStyle> estilosCss = new Dictionary<string, GUIStyle>();
        // peso: 400, 500, 600, 700 (Barlow) ou "serif"/"serifIt"
        GUIStyle Css(string peso, int tam, Color cor, TextAnchor al = TextAnchor.UpperLeft, FontStyle fs = FontStyle.Normal)
        {
            string k = peso + "|" + tam + "|" + ColorUtility.ToHtmlStringRGBA(cor) + "|" + (int)al + "|" + (int)fs; GUIStyle s;
            if (estilosCss.TryGetValue(k, out s)) return s;
            var f = peso == "sys" ? fonteSistema ?? fonteCond400 : peso == "sans" ? fonteSans ?? fonteCond400 : peso == "serif" ? fonteSerif : peso == "serifIt" ? fonteSerifIt : peso == "700" ? fonteCond700 : peso == "600" ? fonteCond600 : peso == "500" ? fonteCond : fonteCond400;
            s = new GUIStyle(GUI.skin.label) { fontSize = tam, wordWrap = false, richText = true, alignment = al, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0), clipping = TextClipping.Overflow, fontStyle = fs };
            s.font = f ?? fonteCond; s.normal.textColor = cor; estilosCss[k] = s; return s;
        }
        static float Larg(string txt, GUIStyle st)
        {
            var tr = Trechos(txt, st); if (tr == null) return st.CalcSize(new GUIContent(txt)).x;
            float w = 0; foreach (var t in tr) w += LargTrecho(t.s, t.st); return w;
        }

        // ---------- trechos com a fonte reserva ----------
        // O Chromium desenha cada caractere que a fonte não tem com a fonte reserva, na mesma linha de base; aqui o texto se
        // parte em trechos (fora das etiquetas do texto rico) e os que a fonte não tem vão na fonte dos símbolos, com o peso
        // (600 e 700 viram negrito, como a DejaVu Sans Bold) e o itálico do estilo
        struct Trecho { public string s; public GUIStyle st; }
        static readonly Dictionary<Font, Dictionary<char, bool>> glifos = new Dictionary<Font, Dictionary<char, bool>>();
        static bool Falta(Font f, char c)
        {
            if (c < 0x2000 || f == null || f == fonteSimbolo || f == fonteSimboloN || f == fonteSistema || f == fonteSans) return false;
            Dictionary<char, bool> d; if (!glifos.TryGetValue(f, out d)) glifos[f] = d = new Dictionary<char, bool>();
            bool falta; if (!d.TryGetValue(c, out falta)) d[c] = falta = !f.HasCharacter(c); return falta;
        }
        static readonly Dictionary<GUIStyle, GUIStyle> estilosSimbolo = new Dictionary<GUIStyle, GUIStyle>(), estilosEsq = new Dictionary<GUIStyle, GUIStyle>();
        static GUIStyle EstiloSimbolo(GUIStyle st)
        {
            GUIStyle s; if (!estilosSimbolo.TryGetValue(st, out s)) estilosSimbolo[st] = s = NovoEstiloSimbolo(st); return s;
        }
        // para os estilos feitos na hora (TextoBase, TextoSvg): o texto que é só símbolo vai inteiro na fonte dos símbolos
        static GUIStyle SoSimbolos(GUIStyle st, string txt)
        {
            if (fonteSimbolo == null || st.font == null || string.IsNullOrEmpty(txt)) return st;
            foreach (var c in txt) if (c != ' ' && !Falta(st.font, c)) return st;
            return NovoEstiloSimbolo(st);
        }
        static GUIStyle NovoEstiloSimbolo(GUIStyle st)
        {
            GUIStyle s; bool negrito = st.font == fonteCond600 || st.font == fonteCond700 || st.fontStyle == FontStyle.Bold || st.fontStyle == FontStyle.BoldAndItalic, it = st.fontStyle == FontStyle.Italic || st.fontStyle == FontStyle.BoldAndItalic;
            if (negrito && fonteSimboloN != null) s = new GUIStyle(st) { font = fonteSimboloN, fontStyle = it ? FontStyle.Italic : FontStyle.Normal };
            else s = new GUIStyle(st) { font = fonteSimbolo, fontStyle = negrito ? (it ? FontStyle.BoldAndItalic : FontStyle.Bold) : it ? FontStyle.Italic : FontStyle.Normal };
            return s;
        }
        static GUIStyle Esq(GUIStyle st)
        {
            if (st.alignment == TextAnchor.UpperLeft) return st; GUIStyle s; if (estilosEsq.TryGetValue(st, out s)) return s;
            s = new GUIStyle(st) { alignment = TextAnchor.UpperLeft }; estilosEsq[st] = s; return s;
        }
        // null quando não há o que trocar (o caminho de sempre)
        static List<Trecho> Trechos(string txt, GUIStyle st)
        {
            if (fonteSimbolo == null || string.IsNullOrEmpty(txt) || st == null || st.font == null) return null;
            bool algum = false; int prof = 0;
            for (int i = 0; i < txt.Length; i++) { char c = txt[i]; if (c == '<' && st.richText) { int f = txt.IndexOf('>', i); if (f > i) { prof += txt[i + 1] == '/' ? -1 : 1; i = f; continue; } } if (prof == 0 && Falta(st.font, c)) { algum = true; break; } }
            if (!algum) return null;
            var o = new List<Trecho>(); var sb = new System.Text.StringBuilder(); bool simb = false; prof = 0; var ss = EstiloSimbolo(st);
            for (int i = 0; i < txt.Length; i++)
            {
                char c = txt[i];
                if (c == '<' && st.richText) { int f = txt.IndexOf('>', i); if (f > i) { if (simb) { o.Add(new Trecho { s = sb.ToString(), st = ss }); sb.Length = 0; simb = false; } prof += txt[i + 1] == '/' ? -1 : 1; sb.Append(txt, i, f - i + 1); i = f; continue; } }
                bool e = prof == 0 && Falta(st.font, c);
                if (e != simb && sb.Length > 0) { o.Add(new Trecho { s = sb.ToString(), st = simb ? ss : st }); sb.Length = 0; }
                simb = e; sb.Append(c);
            }
            if (sb.Length > 0) o.Add(new Trecho { s = sb.ToString(), st = simb ? ss : st }); return o;
        }
        // a largura de um trecho com os espaços do fim (que o CalcSize não conta)
        static float LargTrecho(string s, GUIStyle st)
        {
            int n = 0; while (n < s.Length && s[s.Length - 1 - n] == ' ') n++;
            if (n == s.Length) return n * LargEspaco(st);
            return st.CalcSize(new GUIContent(s)).x + n * LargEspaco(st);
        }
        // y arredondado no pixel da tela, passando pela matriz do GUI (escala da interface e transformações sem giro):
        // primeiro em 1/64 de pixel (o LayoutUnit do Chromium, que tira o erro do float), depois meio pixel para cima
        static float PixelY(float y)
        {
            var m = GUI.matrix; bool reta = m.m10 == 0 && m.m11 != 0; float c = OrigemRecorte.y, py = reta ? (y + c) * m.m11 + m.m13 : y;
            py = Mathf.Floor(Mathf.Round(py * 64) / 64 + .5f); return reta ? (py - m.m13) / m.m11 - c : py;
        }
        // a largura de um espaço: o CalcSize não conta o espaço no fim do texto, então mede entre duas letras
        static float LargEspaco(GUIStyle st) { return Larg("i i", st) - Larg("ii", st); }
        static string SemCor(string t) { return System.Text.RegularExpressions.Regex.Replace(t, "</?color[^>]*>", ""); }
        // uma linha de texto numa caixa de linha do CSS (topo y, altura lh), alinhada em x..x+w pelo alinhamento do estilo;
        // a meia entrelinha entra inteira, para baixo, como no Chromium (FontHeight::AddLeading: ascent += half_leading.Floor()),
        // e a linha de base cai no pixel mais próximo da tela, como o Skia desenha o texto
        void Linha(float x, float y, float w, float lh, string txt, GUIStyle st, float sombra = 0)
        {
            float base_ = PixelY(y + Mathf.Floor((lh - AlturaNormal(st)) / 2) + SubidaPx(st));
            var tr = Trechos(txt, st);
            if (tr == null) { RotuloNaBase(x, base_, w, txt, st, sombra); return; }
            // os trechos um depois do outro na mesma linha de base, alinhados pelo total
            var lw = new float[tr.Count]; float total = 0; for (int i = 0; i < tr.Count; i++) total += lw[i] = LargTrecho(tr[i].s, tr[i].st);
            var al = st.alignment; float xx = al == TextAnchor.UpperCenter || al == TextAnchor.MiddleCenter || al == TextAnchor.LowerCenter ? x + (w - total) / 2 : al == TextAnchor.UpperRight || al == TextAnchor.MiddleRight || al == TextAnchor.LowerRight ? x + w - total : x;
            for (int i = 0; i < tr.Count; i++) { RotuloNaBase(xx, base_, lw[i] + 4, tr[i].s, Esq(tr[i].st), sombra); xx += lw[i]; }
        }
        // o rótulo com a linha de base já no pixel (base_)
        void RotuloNaBase(float x, float base_, float w, string txt, GUIStyle st, float sombra)
        {
            // o retângulo do rótulo com a fonte no tamanho em que ela vai ser rasterizada (a de pixels da tela, ver TextoNitido)
            float s = EscalaTexto(), fp = s == 1 ? st.fontSize : Mathf.RoundToInt(st.fontSize * s);
            var r = new Rect(x, base_ - Mathf.Ceil(SubidaEm(st.font) * fp) / s, w, (Ar(SubidaEm(st.font) * fp) + Ar(DescidaEm(st.font) * fp) + 2) / s);
            if (sombra > 0) { var ss = new GUIStyle(st); var c = Color.black; c.a = sombra * st.normal.textColor.a; ss.normal.textColor = c; TextoNitido(new Rect(r.x, r.y + 1, r.width, r.height), SemCor(txt), ss, false); }
            TextoNitido(r, txt, st);
        }

        // ---------- texto nítido com a interface escalada ----------
        // O IMGUI rasteriza a fonte no fontSize do estilo e só depois o GUI.matrix estica os quadriláteros: com a interface
        // escalada (1440p, 4K, o celular) o texto sairia borrado. Aqui a fonte vai no tamanho dos pixels da tela e uma matriz
        // de 1/s em volta do rótulo devolve o tamanho: o texto fica no mesmo lugar, rasterizado 1 para 1, como no navegador.
        // O IMGUI soma o deslocamento do recorte (rolagem ou grupo) antes de aplicar o GUI.matrix, então o pivô precisa da
        // origem do recorte: Rolagem e Grupo a guardam numa pilha (o BeginScrollView arredonda a rolagem, como aqui).
        static readonly List<Vector2> recortes = new List<Vector2>();
        static Vector2 OrigemRecorte { get { return recortes.Count > 0 ? recortes[recortes.Count - 1] : Vector2.zero; } }
        static Vector2 Rolagem(Rect r, Vector2 rol, Rect vista)
        {
            var v = GUI.BeginScrollView(r, rol, vista, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            recortes.Add(OrigemRecorte + r.position + new Vector2(Mathf.Round(-v.x - vista.x), Mathf.Round(-v.y - vista.y))); return v;
        }
        static void FimRolagem() { if (recortes.Count > 0) recortes.RemoveAt(recortes.Count - 1); GUI.EndScrollView(); }
        static void Grupo(Rect r) { GUI.BeginGroup(r); recortes.Add(OrigemRecorte + r.position); }
        static void FimGrupo() { if (recortes.Count > 0) recortes.RemoveAt(recortes.Count - 1); GUI.EndGroup(); }
        // a escala do GUI.matrix quando ela é uniforme e sem giro (com giro, o texto fica como está)
        static float EscalaTexto()
        {
            var m = GUI.matrix; if (m.m01 != 0 || m.m10 != 0 || Mathf.Abs(m.m00 - m.m11) > 1e-4f || m.m00 <= 0) return 1;
            return Mathf.Abs(m.m00 - 1) < .01f ? 1 : m.m00;
        }
        static readonly Dictionary<GUIStyle, Dictionary<int, GUIStyle>> estilosPx = new Dictionary<GUIStyle, Dictionary<int, GUIStyle>>();
        static RectOffset Vezes(RectOffset o, float s) { return new RectOffset(Mathf.RoundToInt(o.left * s), Mathf.RoundToInt(o.right * s), Mathf.RoundToInt(o.top * s), Mathf.RoundToInt(o.bottom * s)); }
        // o estilo com a fonte, o padding e os tamanhos fixos em pixels da tela, sem fundo (só o texto)
        static GUIStyle EstiloPx(GUIStyle st, float s, bool guardar)
        {
            int fp = Mathf.Max(1, Mathf.RoundToInt((st.fontSize > 0 ? st.fontSize : 13) * s)); Dictionary<int, GUIStyle> d = null; GUIStyle p;
            if (guardar) { if (!estilosPx.TryGetValue(st, out d)) estilosPx[st] = d = new Dictionary<int, GUIStyle>(); if (d.TryGetValue(fp, out p)) return p; }
            p = new GUIStyle(st) { fontSize = fp, fixedWidth = st.fixedWidth * s, fixedHeight = st.fixedHeight * s, padding = Vezes(st.padding, s), border = new RectOffset(0, 0, 0, 0) };
            foreach (var e in new[] { p.normal, p.hover, p.active, p.focused, p.onNormal }) e.background = null;
            if (guardar) d[fp] = p; return p;
        }
        static readonly System.Text.RegularExpressions.Regex reTam = new System.Text.RegularExpressions.Regex("<size=([0-9.]+)>");
        static void TextoNitido(Rect r, string txt, GUIStyle st, bool guardar = true)
        {
            float s = EscalaTexto(); if (s == 1) { GUI.Label(r, txt, st); return; }
            var sp = EstiloPx(st, s, guardar);
            if (st.richText && txt != null && txt.IndexOf("<size=", System.StringComparison.Ordinal) >= 0)
                txt = reTam.Replace(txt, m => "<size=" + Mathf.RoundToInt(float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) * s) + ">");
            var m0 = GUI.matrix; var o = OrigemRecorte + r.position;
            GUI.matrix = m0 * Matrix4x4.TRS(o, Quaternion.identity, Vector3.one) * Matrix4x4.Scale(new Vector3(1 / s, 1 / s, 1)) * Matrix4x4.TRS(-o, Quaternion.identity, Vector3.one);
            try { GUI.Label(new Rect(r.x, r.y, r.width * s, r.height * s), txt, sp); } finally { GUI.matrix = m0; }
        }
        // o botão: o fundo e o clique no GUI.matrix de sempre, o texto nítido por cima (na cor do hover quando o mouse está nele)
        static bool BotaoNitido(Rect r, string txt, GUIStyle st)
        {
            if (EscalaTexto() == 1) return GUI.Button(r, txt, st);
            bool b = GUI.Button(r, new GUIContent("", txt), st);   // o texto vai só na dica (que a interface não mostra)
            if (Event.current.type == EventType.Repaint && !string.IsNullOrEmpty(txt))
            {
                bool sobre = GUI.enabled && r.Contains(Event.current.mousePosition);
                var lab = sobre && st.hover.textColor != st.normal.textColor ? EstiloHover(st) : st; TextoNitido(r, txt, lab);
            }
            return b;
        }
        // o campo de texto: o fundo (com a borda de 1 px, dourada no foco) no GUI.matrix de sempre e o campo em si, sem fundo,
        // sob a matriz de 1/s com a fonte em pixels da tela; o TextEditor mede o cursor no mesmo espaço do campo
        static readonly Dictionary<GUIStyle, GUIStyle[]> fundosCampo = new Dictionary<GUIStyle, GUIStyle[]>();
        static string CampoNitido(Rect r, string valor, int max, GUIStyle st, string nome, bool area = false)
        {
            float s = EscalaTexto();
            if (s == 1) { if (nome != null) GUI.SetNextControlName(nome); return area ? GUI.TextArea(r, valor ?? "", max, st) : GUI.TextField(r, valor ?? "", max, st); }
            GUIStyle[] f; if (!fundosCampo.TryGetValue(st, out f))
            {
                f = new GUIStyle[2]; for (int i = 0; i < 2; i++) { f[i] = new GUIStyle { border = st.border }; f[i].normal.background = i == 1 && st.focused.background != null ? st.focused.background : st.normal.background; }
                fundosCampo[st] = f;
            }
            if (Event.current.type == EventType.Repaint) GUI.Box(r, GUIContent.none, f[nome != null && GUI.GetNameOfFocusedControl() == nome ? 1 : 0]);
            var sp = EstiloPx(st, s, true); var m0 = GUI.matrix; var o = OrigemRecorte + r.position;
            GUI.matrix = m0 * Matrix4x4.TRS(o, Quaternion.identity, Vector3.one) * Matrix4x4.Scale(new Vector3(1 / s, 1 / s, 1)) * Matrix4x4.TRS(-o, Quaternion.identity, Vector3.one);
            try
            {
                if (nome != null) GUI.SetNextControlName(nome); var rr = new Rect(r.x, r.y, r.width * s, r.height * s);
                return area ? GUI.TextArea(rr, valor ?? "", max, sp) : GUI.TextField(rr, valor ?? "", max, sp);
            }
            finally { GUI.matrix = m0; }
        }
        static readonly Dictionary<GUIStyle, GUIStyle> estilosHover = new Dictionary<GUIStyle, GUIStyle>();
        static GUIStyle EstiloHover(GUIStyle st) { GUIStyle h; if (!estilosHover.TryGetValue(st, out h)) { h = new GUIStyle(st); h.normal.textColor = st.hover.textColor; estilosHover[st] = h; } return h; }
        // quebra como o navegador (nas palavras, dentro da largura) e devolve as linhas; recuo tira largura só da primeira
        static List<string> Quebrar(string txt, GUIStyle st, float w, float recuo = 0)
        {
            var o = new List<string>(); foreach (var par in txt.Split('\n'))
            {
                string linha = ""; foreach (var p in par.Split(' '))
                {
                    string tenta = linha.Length == 0 ? p : linha + " " + p;
                    if (linha.Length > 0 && Larg(tenta, st) > w - (o.Count == 0 ? recuo : 0)) { o.Add(linha); linha = p; } else linha = tenta;
                }
                o.Add(linha);
            }
            return o;
        }
        // um parágrafo (quebrado na largura w) com a altura de linha lh; devolve a altura usada
        float Paragrafo(float x, float y, float w, float lh, string txt, GUIStyle st, float sombra = 0)
        {
            var ls = Quebrar(txt, st, w); for (int i = 0; i < ls.Count; i++) Linha(x, y + i * lh, w, lh, ls[i], st, sombra); return ls.Count * lh;
        }
        static float AlturaParagrafo(string txt, GUIStyle st, float w, float lh) { return Quebrar(txt, st, w).Count * lh; }
        void Moldura(Rect r, Color c, float e = 1)
        {
            Caixa(new Rect(r.x, r.y, r.width, e), c); Caixa(new Rect(r.x, r.yMax - e, r.width, e), c); Caixa(new Rect(r.x, r.y + e, e, r.height - 2 * e), c); Caixa(new Rect(r.xMax - e, r.y + e, e, r.height - 2 * e), c);
        }
        // degradê de duas cores (vertical: a de cima primeiro), esticado no retângulo como o linear-gradient
        readonly Dictionary<string, Texture2D> degrades = new Dictionary<string, Texture2D>();
        Texture2D Degrade(Color a, Color b, bool vertical)
        {
            string k = ColorUtility.ToHtmlStringRGBA(a) + ColorUtility.ToHtmlStringRGBA(b) + vertical; Texture2D t;
            if (degrades.TryGetValue(k, out t)) return t;
            int n = 64; t = vertical ? new Texture2D(1, n, TextureFormat.RGBA32, false) : new Texture2D(n, 1, TextureFormat.RGBA32, false); var p = new Color[n];
            // linha 0 é a de baixo: no vertical a cor de cima vai no fim
            for (int i = 0; i < n; i++) { float f = (i + .5f) / n; p[i] = vertical ? Color.Lerp(b, a, f) : Color.Lerp(a, b, f); }
            t.SetPixels(p); t.Apply(false, true); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; degrades[k] = t; return t;
        }
        void PintarDegrade(Rect r, Color a, Color b, bool vertical) { GUI.DrawTexture(r, Degrade(a, b, vertical)); }
    }
}
