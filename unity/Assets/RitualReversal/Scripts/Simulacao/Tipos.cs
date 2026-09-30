// Estado do jogo e tabelas de regras (CFG, classes, feitiços, itens, tarefas). Porte de shared/sim.js.
// Os nomes dos campos seguem o JS de propósito (a.hp, A.prog, g.rt...): fica fácil comparar as duas versões linha a linha.
using System;
using System.Collections.Generic;

namespace RitualReversal.Simulacao
{
    public static class CFG
    {
        public static double roundTime = 1080; public static int noites = 1; public static int obolosInicio = 3;
        public static double[] respawnMomento = { 8, 14, 22 };
        public static string formatoNoite = "placar"; // "placar": a noite sempre chega à Hora Morta; "melhorDeTres": 2 a 0 encerra
        public static readonly string[] momentoId = { "crepusculo", "vigilia", "horamorta" };
        public static readonly string[] momentoNome = { "Crepúsculo", "Vigília", "Hora Morta" };
        public static readonly double[] momentoAte = { 180, 600, 1080 };
        // rituais por número de canalizadores (1 ou 2): duração, raio em que se ouve, progresso em que o coro revela
        public static readonly double[] ritDur = { 0, 45, 34 }, ritRad = { 0, 14, 24 }, ritLoc = { 0, 0.55, 0.5 };
        public static double circleR = 3, seal = 8, sealExo = 6.5, coSeal = 1.35, fire1 = 0.7, fire2 = 0.45, sealMin = 5;
        public static readonly double[] cps = { 0.25, 0.75 };
        public static int carry = 2;
        public static double collect = 3.5, consecrate = 3, start = 1.2, purge = 6, burn = 6, decoy = 2, transfer = 12, revive = 5, execute = 4.5, downTime = 20;
        public static double sigilDano = 17, sigilVel = 48, sigilCusto = 14, fervorRegen = 8, fervorEspera = 1.8, quedaDano = 14, quedaPerto = 26, quedaLonge = 55, quedaMin = .75;
        public static double compensacaoMax = .5; // quanto se volta no tempo para julgar um tiro
        public static double regrow = 40, sensorRaio = 58, pegadaVida = 20, visaoRede = 75, nevoaAlcance = 26, speed = 4.6, sprint = 6, botSpeed = 3.9, spawnSafe = 7;
        public static bool iniciarDeGraca = false;
        public static double timerPick = 35, timerIntro = 25, timerSummary = 15;
    }

    public class ClasseDef { public string id, team, name, arma, text; public double hp, speed; }
    public class FeiticoDef { public string id, team, nome, text; public double cd; }
    public class ItemDef { public string id, nome, time, desc; public int preco, momento; }
    public class TarefaDef { public string id, nome; public int meta, rec; }

    public static class Regras
    {
        public static readonly Dictionary<string, ClasseDef> CLASSES = new Dictionary<string, ClasseDef>
        {
            {"ritualista", new ClasseDef{id="ritualista",team="C",name="Ritualista",hp=150,speed=4.6,arma="Cetro de vértebras",text="Cetro de vértebras. Passiva: quem canaliza com você ganha escudo ao atingir 25% e 75% do ritual."}},
            {"guardiao", new ClasseDef{id="guardiao",team="C",name="Guardião Profano",hp=200,speed=4.1,arma="Cetro de vértebras",text="Cetro de vértebras. Passiva: 200 de vida e armadura de osso, mas mais lento."}},
            {"soldado", new ClasseDef{id="soldado",team="H",name="Soldado",hp=170,speed=4.6,arma="Carabina de alavanca",text="Carabina de alavanca. Passiva: carrega mais munição de reserva."}},
            {"exorcista", new ClasseDef{id="exorcista",team="H",name="Exorcista",hp=160,speed=4.6,arma="Revólver de prata",text="Revólver de prata. Passiva: sela rituais mais depressa."}},
            {"rastreador", new ClasseDef{id="rastreador",team="H",name="Rastreador",hp=155,speed=4.8,arma="Carabina de alavanca",text="Carabina de alavanca. Passiva: enxerga pegadas frescas dos Cultistas e lê marcas de arrasto: sabe para onde a Transferência levou o ritual."}},
        };
        public static readonly Dictionary<string, FeiticoDef> FEITICOS = new Dictionary<string, FeiticoDef>
        {
            {"runa", new FeiticoDef{id="runa",team="C",nome="Runa",cd=20,text="Canalização 20% mais rápida por 8 s, mas o ritual fica audível de mais longe."}},
            {"empurrao", new FeiticoDef{id="empurrao",team="C",nome="Empurrão",cd=12,text="Arremessa quem está à sua frente e interrompe o selamento."}},
            {"veu", new FeiticoDef{id="veu",team="C",nome="Véu das Sombras",cd=24,text="Por 5 s você vira uma silhueta tênue: some a mais de 8 m e reaparece sob a lanterna. Atacar ou levar dano desfaz o véu."}},
            {"flash", new FeiticoDef{id="flash",team="H",nome="Flash",cd=18,text="Cega os inimigos no cone por 1,5 s. Cultista cego canaliza pela metade."}},
            {"purificacao", new FeiticoDef{id="purificacao",team="H",nome="Purificação",cd=25,text="Atordoa inimigos a 6 m por 1 s e desfaz Runa e Véu."}},
            {"sal", new FeiticoDef{id="sal",team="H",nome="Círculo de Sal",cd=16,text="Deixa sal no chão por 60 s. O primeiro Cultista que pisar fica lento e aparece para a sua equipe."}},
        };
        public static readonly Dictionary<string, string[]> FEIT_BY_TEAM = new Dictionary<string, string[]> { { "H", new[] { "flash", "purificacao", "sal" } }, { "C", new[] { "runa", "empurrao", "veu" } } };
        public static readonly Dictionary<string, string[]> CLASS_BY_TEAM = new Dictionary<string, string[]> { { "H", new[] { "soldado", "exorcista", "rastreador" } }, { "C", new[] { "ritualista", "guardiao" } } };
        public static readonly Dictionary<string, ItemDef> ITENS = new Dictionary<string, ItemDef>
        {
            {"reagente", new ItemDef{id="reagente",nome="Reagente",preco=3,time="C",desc="Um frasco para consagrar ou iniciar um ritual."}},
            {"cinza", new ItemDef{id="cinza",nome="Cinza de Chamariz",preco=3,time="C",desc="Mais um Chamariz para a equipe."}},
            {"municao", new ItemDef{id="municao",nome="Munição benta",preco=1,time="H",desc="16 balas na reserva."}},
            {"flare", new ItemDef{id="flare",nome="Sinalizador",preco=2,time="H",desc="Mais um sinalizador de luz."}},
            {"oleo", new ItemDef{id="oleo",nome="Óleo de lamparina",preco=1,time="H",desc="Enche a carga da Lanterna e do EVP."}},
            {"pocao", new ItemDef{id="pocao",nome="Tônico amargo",preco=2,time=null,desc="Recupera 60 de vida em 4 s."}},
            {"amuleto", new ItemDef{id="amuleto",nome="Amuleto",preco=6,time=null,momento=1,desc="+25 de vida máxima até o amanhecer. Um por noite."}},
        };
        // tarefas de cada momento da noite, por papel
        public static readonly List<Dictionary<string, TarefaDef[]>> TAREFAS = new List<Dictionary<string, TarefaDef[]>>
        {
            new Dictionary<string, TarefaDef[]> {
                {"H", new[]{ new TarefaDef{id="pistas",nome="Recolha pistas nos santuários da floresta",meta=3,rec=3}, new TarefaDef{id="sentinelas",nome="Acenda sentinelas nos caminhos dos altares: elas denunciam consagrações por perto",meta=2,rec=3} }},
                {"C", new[]{ new TarefaDef{id="ervas",nome="Colha ervas-noturnas na floresta",meta=4,rec=3}, new TarefaDef{id="tumulos",nome="Profane túmulos no cemitério: cada um rende um reagente",meta=2,rec=3} }} },
            new Dictionary<string, TarefaDef[]> {
                {"H", new[]{ new TarefaDef{id="purgar",nome="Purgue um altar consagrado",meta=1,rec=4}, new TarefaDef{id="selar",nome="Entre num círculo e comece a selar",meta=1,rec=5} }},
                {"C", new[]{ new TarefaDef{id="consagrar",nome="Consagre altares",meta=2,rec=4}, new TarefaDef{id="ritual",nome="Complete um ritual",meta=1,rec=5} }} },
            new Dictionary<string, TarefaDef[]> {
                {"H", new[]{ new TarefaDef{id="final",nome="Sele dois dos três rituais ou resista até o amanhecer",meta=0,rec=0} }},
                {"C", new[]{ new TarefaDef{id="final",nome="Vença dois dos três rituais antes do amanhecer",meta=0,rec=0} }} },
        };
        public static readonly Dictionary<string, string[]> BOT_NAMES = new Dictionary<string, string[]> { { "A", new[] { "Irmã Beatriz", "Irmão Tomé" } }, { "B", new[] { "Irmão Cinza", "Madre Vesper" } } };
    }

    // ---------- estado ----------
    public class Slot { public string team, cid, name; public Slot() { } public Slot(string team, string cid, string name) { this.team = team; this.cid = cid; this.name = name; } }
    public class Hold { public string key = ""; public double t, max; public Hold() { } public Hold(string key, double t, double max) { this.key = key; this.t = t; this.max = max; } }
    // contadores dos botões de toque único (Q, F, G, R, habilidade): a ação dispara quando o número muda
    public class Controles { public int q, f, g, r, ab; public Controles Copia() { return (Controles)MemberwiseClone(); } }
    public class Entrada
    {
        public double mx, mz; public bool sp; public double yaw, pitch; public bool fire, use, use2; public double viewT; public Controles c = new Controles();
        public bool temYaw = true, temPitch = true, temViewT = true; // o JS só troca o valor quando ele veio na mensagem
    }
    public class Alerta : Ponto { public double t; }
    public class RastroAlvo : Ponto { public double ate; public string k; }
    public class VigiaPt : Ponto { public int i; }
    public class Vigia : Ponto { public double t; }
    public class Pegada : Ponto { public double yaw, t; public int l; }

    public class IA
    {
        public List<Ponto> path = new List<Ponto>(); public Ponto goal; public string goalKey = ""; public double repath, stuckT, lx, lz;
        public Alerta alert; public Ator target; public double wait; public bool decoyUsed; public double strafe = 1, strafeT; public int? patrol; public double patrolT;
        public Dictionary<string, double> ignora; public string progKey; public double melhor, progT, progX, progZ;
        public RastroAlvo rastro; public VigiaPt vigiaPt; public Vigia vigia; public double esperando; public int? esperaAltar;
        public int? decoyAlvo; public double decoyT, decoyPrazo; public int chamarizes; public double chamarizT, emperrado;
        public bool Ignorado(string k, double t) { double v; return ignora != null && ignora.TryGetValue(k, out v) && v > t; }
        public void Ignorar(string k, double ate) { if (ignora == null) ignora = new Dictionary<string, double>(); ignora[k] = ate; }
    }

    public class Ator : Ponto
    {
        public int id, slot; public string cid; public bool human; public string name, team, cls; public double maxHp, hp, speed;
        public double yaw, pitch; public string st = "alive"; public double downT, respawnT; public int deaths, dc; public bool revUsed; public int reag;
        public double hitT = -9; public Dictionary<int, double> att = new Dictionary<int, double>(); public double fireCd; public int ammo = 8, reserve = 48; public double reloadT;
        public double fervor = 100, sanity = 100, invuln; public int sealing = -1; public Hold hold = new Hold(); public bool lantern; public double carga = 100; public int flares = 2;
        public double sensorCd, abilCd, lastShotT = -9; public bool moving; public double panicT = -9, blindT, stunT, runeT, shield, shieldT, pushedT = -9;
        public List<double[]> hist = new List<double[]>(); public string feitico; public int obolos; public double veuT, slowT, revelT, curaT; public bool amuleto;
        public Entrada inp = new Entrada(); public Controles lc = new Controles(); public bool lcIni;
        public IA ai = new IA();
        public double _vx, _vz; public double? _px, _pz; public Pegada _pegada; public double stepT;
    }

    public class Altar : Ponto
    {
        public int i; public string name, state = "dormant"; public bool chosen, decoy, localized, arrived, tardio, grande;
        public double prog, cp, seal, sealIdle, startT, maxProg, selo = 1, fireT, sealT; public int lastN = 1, maxN; public double? doneT;
        public List<int> sealers = new List<int>();
    }
    public class Reagente : Ponto { public int i; public bool has = true; public double t; }
    public class Projetil { public int id, owner; public double x, y, z, vx, vy, vz, life, dmg; public string team; }
    public class Coletavel : Ponto { public int id; public string kind; public double life; }
    public class Sinalizador : Ponto { public double t; }
    public class PontoNoite : Ponto { public string tipo; public bool ativo = true, acesa; public double t; public int i; }
    public class Sal : Ponto { public int id, dono; public double t; }
    public class Rastro : Ponto { public int de, para; public double t; public bool visto, lido; }
    public class Tarefa { public string id, nome; public int meta, rec, prog; public bool feita; }
    public class Placar { public int done, @sealed; public double? lastT; public double maxP; }
    public class Buff { public double ess, selo; }

    // ações que o jogador pede fora do movimento (menus, compras, pronto)
    public class Acao { public string type, cls, f, npc, item; public int i; }

    // contexto da tecla E/T: o que dá para fazer onde o jogador está
    public class Ctx { public string key = "", type, label, npc; public double time; public int? b, P, A, R, from; }
    public class Contextos { public Ctx e, t; public string info; }

    // eventos para o cliente (sons, efeitos, mensagens) e registro da partida (log) para estatísticas e testes
    public class Evento
    {
        public string type; public Dictionary<string, object> d = new Dictionary<string, object>();
        public object this[string k] { get { object v; return d.TryGetValue(k, out v) ? v : null; } }
        public double Num(string k) { object v = this[k]; return v == null ? 0 : Convert.ToDouble(v); }
        public int Int(string k, int padrao = -1) { object v = this[k]; return v == null ? padrao : Convert.ToInt32(v); }
        public string Str(string k) { return this[k] as string; }
    }
    public class Registro { public double t; public int r; public string ev; public Dictionary<string, object> d = new Dictionary<string, object>(); public object this[string k] { get { object v; return d.TryGetValue(k, out v) ? v : null; } } }

    public class Estatisticas
    {
        public int rituais, completos, selados, chegadas, tentativasSelo, cancelados, quedas, mortes, reanimacoes, purgas, chamarizes, transferencias;
        public double? fracaoSobFogo, primeiroContato;
    }
    public class Vencedor { public string win, why; }

    public class OpcoesJogo { public bool timers; public List<Slot> slots; public string matchId; public int noites; }

    public class Jogo
    {
        public OpcoesJogo opts; public double t, rt, phaseT; public string phase = "pick"; public int round = 1; public bool timers;
        public List<Slot> slots = new List<Slot>(); public List<Altar> altars = new List<Altar>(); public List<Reagente> reagents = new List<Reagente>();
        public List<Ator> actors = new List<Ator>(); public List<Projetil> proj = new List<Projetil>(); public List<Coletavel> pickups = new List<Coletavel>();
        public List<Sinalizador> flares = new List<Sinalizador>(); public string[] know = new string[0]; public double[] visit = new double[0];
        public int decoys = 2; public bool transferUsed; public double lastResolveT, endAt; public List<Evento> events = new List<Evento>(); public List<Registro> log = new List<Registro>();
        public Dictionary<string, Placar> scores = new Dictionary<string, Placar> { { "A", null }, { "B", null } };
        public Dictionary<int, Estatisticas> stats = new Dictionary<int, Estatisticas>();
        public List<int> picks = new List<int>(); public Dictionary<string, bool> ready = new Dictionary<string, bool>(); public Dictionary<string, string> cls = new Dictionary<string, string>();
        public int nid = 1; public Dictionary<string, string> feit = new Dictionary<string, string>(); public Buff buff = new Buff(); public double? firstContact; public string matchId;
        public int momento; public List<PontoNoite> pontos = new List<PontoNoite>(); public List<Sal> sal = new List<Sal>(); public List<double> realocar = new List<double>();
        public Dictionary<string, List<Tarefa>> tarefas = new Dictionary<string, List<Tarefa>> { { "H", new List<Tarefa>() }, { "C", new List<Tarefa>() } };
        public List<Rastro> rastros = new List<Rastro>(); public List<Pegada> pegadas = new List<Pegada>();
    }
}
