#!/usr/bin/env python3
# Monta os sons do jogo a partir das gravações em assets/sons/fontes (CC0 e CC BY 3.0; origem em assets/sons/fontes.json).
# Cada som do jogo é uma receita: trechos das gravações, filtros, camadas, normalização. Sai em MP3 mono
# (assets/sons/jogo/*.mp3) e em client/sons.js, que o cliente carrega (e tools/build.js embute no arquivo único).
# Precisa do ffmpeg (ou: pip install imageio-ffmpeg numpy). Uso: python3 tools/montar-sons.py
import os, sys, json, base64, subprocess, shutil, tempfile
import numpy as np

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONTES = os.path.join(RAIZ, 'assets', 'sons', 'fontes')
SAIDA = os.path.join(RAIZ, 'assets', 'sons', 'jogo')
UNITY = os.path.join(RAIZ, 'unity', 'Assets', 'RitualReversal', 'Resources', 'Sons')   # o Som.cs do Unity carrega daqui
SR = 44100

def achar_ffmpeg():
    f = shutil.which('ffmpeg')
    if f: return f
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except Exception:
        sys.exit('ffmpeg não encontrado: instale o ffmpeg ou rode "pip install imageio-ffmpeg"')
FF = achar_ffmpeg()

def ff_ler(args):
    p = subprocess.run([FF, '-v', 'error'] + args + ['-ac', '1', '-ar', str(SR), '-f', 'f32le', '-'], capture_output=True)
    if p.returncode: sys.exit(p.stderr.decode())
    return np.frombuffer(p.stdout, dtype=np.float32).copy()

USADAS = set()   # gravações usadas pela receita em montagem (vai para a tabela de créditos)

def ler(nome, ini=0.0, dur=None, taxa=1.0, af=None):
    """Trecho de uma gravação. taxa muda altura e velocidade juntas (como um disco); af: filtros do ffmpeg."""
    USADAS.add(nome)
    filtros = []
    if taxa != 1.0: filtros.append(f'asetrate={int(SR * taxa)},aresample={SR}')
    if af: filtros.append(af)
    args = ['-i', os.path.join(FONTES, nome)]
    if filtros: args += ['-af', ','.join(filtros)]
    x = ff_ler(args)
    a = int(ini * SR); b = len(x) if dur is None else a + int(dur * SR)
    return x[a:b]

def ff(x, af):
    """Passa um sinal por filtros do ffmpeg (eco, equalização, esticar no tempo...)."""
    with tempfile.NamedTemporaryFile(suffix='.f32', delete=False) as t:
        t.write(x.astype(np.float32).tobytes()); nome = t.name
    try: return ff_ler(['-f', 'f32le', '-ar', str(SR), '-ac', '1', '-i', nome, '-af', af])
    finally: os.unlink(nome)

def fade(x, ent=0.003, sai=0.05, curva=2.0):
    x = x.copy(); n = len(x)
    a = min(n, int(ent * SR)); b = min(n, int(sai * SR))
    if a: x[:a] *= np.linspace(0, 1, a)
    if b: x[n - b:] *= np.linspace(1, 0, b) ** curva
    return x

def db(g): return 10 ** (g / 20)

def mix(*camadas):
    """camadas: (sinal, atraso em s, ganho em dB)"""
    n = max(int(at * SR) + len(s) for s, at, g in camadas)
    y = np.zeros(n, dtype=np.float32)
    for s, at, g in camadas:
        i = int(at * SR); y[i:i + len(s)] += s * db(g)
    return y

def aparar(x, limiar=-50):
    """Tira o silêncio do começo: o som tem de soar no instante do evento."""
    lim = db(limiar) * np.max(np.abs(x)); i = int(np.argmax(np.abs(x) > lim))
    return x[max(0, i - 32):]

def pico(x, alvo=-1.0):
    m = np.max(np.abs(x)) or 1.0
    return x * (db(alvo) / m)

def janela_pico(nome, antes, depois, **k):
    """Trecho em volta do ponto mais forte da gravação (para whooshes cujo auge fica no meio)."""
    x = ler(nome, **k); env = np.convolve(np.abs(x), np.ones(441) / 441, 'same'); i = int(np.argmax(env))
    return x[max(0, i - int(antes * SR)):i + int(depois * SR)]

def golpe(x, tau):
    """Envelope exponencial: o começo domina e o resto vira cauda (a explosão gravada é saturada e achatada)."""
    t = np.arange(len(x)) / SR
    return (x * (np.exp(-t / tau) * .85 + .15 * np.exp(-t / (tau * 4)))).astype(np.float32)

def laco(x, cruz=0.6):
    """Laço sem emenda: o fim entra no começo com crossfade de potência constante."""
    c = int(cruz * SR); a = x[:c]; b = x[-c:]; t = np.linspace(0, np.pi / 2, c)
    meio = x[c:-c]
    return np.concatenate([b * np.cos(t) + a * np.sin(t), meio]).astype(np.float32)

# ---------------- receitas ----------------
# nome do som no jogo -> lista de variações (o jogo sorteia uma a cada vez)
R = {}
def receita(nome):
    def reg(f): R[nome] = f; return f
    return reg

EXPL = 'tnt_explode.ogg'

@receita('tiro_carabina')   # carabina de alavanca: estalo seco, corpo médio, cauda curta de mata
def _():
    out = []
    for k, (ini, tx) in enumerate([(0.0, 1.0), (0.02, 1.06), (0.04, .95)]):
        corpo = golpe(fade(aparar(ler(EXPL, ini, .34, tx, 'highpass=f=90,lowpass=f=6500')), sai=.30, curva=3), .055)
        estalo = fade(ler('perc_impact2.flac', 0, .16, 1.2 * tx, 'highpass=f=350'), sai=.12)
        snap = fade(ler('perc_snap.flac', 0, .08, 1.0, 'highpass=f=1500'), sai=.05)
        y = mix((corpo, 0, 0), (estalo, 0, -4), (snap, 0, -6))
        y = ff(y, 'aecho=0.7:0.45:55|130:0.22|0.12')
        out.append(pico(fade(aparar(y), sai=.15), -1))
    return out

@receita('tiro_revolver')   # revólver de prata: mais grave e mais longo, golpe no peito
def _():
    out = []
    for ini, tx in [(0.0, .9), (0.01, .86)]:
        corpo = golpe(fade(aparar(ler(EXPL, ini, .5, tx, 'highpass=f=60,lowpass=f=4800')), sai=.42, curva=3), .09)
        baque = fade(ler('perc_impact1.flac', 0, .45, .95, 'lowpass=f=1800'), sai=.35)
        snap = fade(ler('perc_snap.flac', 0, .08, .9, 'highpass=f=1200'), sai=.05)
        y = mix((corpo, 0, 0), (baque, 0, -3), (snap, 0, -7))
        y = ff(y, 'aecho=0.75:0.5:70|190:0.28|0.16')
        out.append(pico(fade(aparar(y), sai=.25), -1))
    return out

@receita('alavanca')        # alavanca da carabina: abre e fecha (tocada logo depois do tiro)
def _():
    a = fade(ler('default_dug_metal.1.ogg', 0, .12, 1.15, 'highpass=f=500'), sai=.06)
    b = fade(ler('default_place_node_metal.2.ogg', 0, .14, 1.25, 'highpass=f=400'), sai=.08)
    return [pico(mix((a, 0, 0), (b, .15, -2)), -4)]

@receita('cao')             # cão do revólver armado e tambor girando
def _():
    a = fade(ler('default_dig_metal.ogg', 0, .1, 1.35, 'highpass=f=800'), sai=.05)
    b = fade(ler('perc_snap.flac', 0, .06, 1.6, 'highpass=f=2500'), sai=.04)
    return [pico(mix((a, 0, 0), (b, .07, -8)), -5)]

@receita('recarga')         # três partes: abrir, cartucho, fechar (o jogo sincroniza com a animação)
def _():
    abre = fade(ler('mcl_armor_equip_iron.ogg', 0, .3, 1.1, 'highpass=f=300'), sai=.1)
    cart = fade(ler('mcl_armor_equip_chainmail.ogg', 0, .14, 1.4, 'highpass=f=600'), sai=.08)
    fecha = fade(ler('default_dug_metal.2.ogg', 0, .2, 1.1, 'highpass=f=300'), sai=.1)
    return [pico(abre, -4), pico(cart, -6), pico(fecha, -3)]

@receita('sigilo')          # cetro de vértebras: ar sendo puxado e solto, sem "laser"
def _():
    out = []
    for tx in [1.25, 1.4]:
        w = fade(janela_pico('ambi_dark_woosh.flac', .18, .42, taxa=tx, af='highpass=f=140'), ent=.01, sai=.25)
        flecha = fade(ler('mcl_throwing_throw.ogg', 0, .22, .85, 'highpass=f=200'), sai=.1)
        out.append(pico(mix((w, 0, 0), (flecha, .02, -3)), -2))
    return out

@receita('estouro')         # sigilo estourando (em parede ou em alguém)
def _():
    p = fade(ler('mcl_mobs_mob_poof.ogg', 0, .5, 1.0), sai=.3)
    b = fade(ler('perc_impact2.flac', 0, .3, .8, 'lowpass=f=2500'), sai=.2)
    return [pico(mix((p, 0, 0), (b, 0, -7)), -2)]

for sup, arq, n in [('pedra', 'default_dig_cracky', 3), ('madeira', 'default_dig_choppy', 3), ('terra', 'default_gravel_dig', 2)]:
    def faz(arq=arq, n=n, sup=sup):
        out = []
        for i in range(1, n + 1):
            x = fade(ler(f'{arq}.{i}.ogg', 0, .26, 1.0, 'highpass=f=180' + (',lowpass=f=5500' if sup == 'terra' else '')), sai=.12)
            if sup == 'pedra': x = mix((x, 0, 0), (fade(ler('perc_snap.flac', 0, .05, 1.8, 'highpass=f=3000'), sai=.03), 0, -16))
            out.append(pico(aparar(x), -2))
        return out
    R['impacto_' + sup] = faz

@receita('impacto_corpo')   # tiro que acerta alguém: golpe surdo, um pouco úmido; o mesmo para os dois lados
def _():
    out = []
    for tx in [1.0, .9, 1.1]:
        g = fade(ler('player_damage.ogg', 0, .24, tx), sai=.15)
        umido = fade(ler('mcl_throwing_egg_impact.ogg', 0, .07, tx, 'lowpass=f=4000'), sai=.04)
        baque = fade(ler('drum_heavy_kick.flac', 0, .15, 1.0, 'lowpass=f=320'), sai=.1)
        out.append(pico(aparar(mix((g, 0, 0), (umido, 0, -8), (baque, 0, -9))), -2))
    return out

@receita('derrubou')        # confirmação de queda (só para quem derrubou)
def _():
    t = fade(ler('drum_tom_lo_hard.flac', 0, .6, .8, 'lowpass=f=900'), sai=.4)
    i = fade(ler('perc_impact1.flac', 0, .5, .9, 'lowpass=f=2200'), sai=.4)
    return [pico(mix((t, 0, 0), (i, .01, -5)), -2)]

@receita('dor')             # você levou um golpe
def _():
    g = fade(ler('player_damage.ogg', 0, .25, .82, 'lowpass=f=2600'), sai=.15)
    k = fade(ler('drum_heavy_kick.flac', 0, .2, .9, 'lowpass=f=260'), sai=.12)
    return [pico(mix((g, 0, 0), (k, 0, -5)), -3)]

@receita('empurrao')        # Empurrão: arremesso de ar e o baque de quem é empurrado
def _():
    s = fade(ler('perc_swash.flac', 0, .3, .9), sai=.15)
    k = fade(ler('drum_heavy_kick.flac', 0, .25, .85, 'lowpass=f=400'), sai=.15)
    w = fade(janela_pico('ambi_swoosh.flac', .12, .3, taxa=1.2), sai=.2)
    return [pico(mix((s, 0, 0), (k, .04, -3), (w, 0, -9)), -2)]

for sup, arq, n, af in [('pedra', 'default_hard_footstep', 3, 'highpass=f=140'), ('terra', 'default_sand_footstep', 3, 'highpass=f=100,lowpass=f=6000'),
                        ('folhas', 'default_snow_footstep', 5, 'highpass=f=260')]:
    def faz(arq=arq, n=n, af=af):
        return [pico(fade(aparar(ler(f'{arq}.{i}.ogg', 0, .3, 1.0, af)), sai=.08), -3) for i in range(1, n + 1)]
    R['passo_' + sup] = faz
R['passo_lama'] = lambda: [pico(fade(aparar(ler('mud_footsteps.ogg', 0, .2, tx)), sai=.05), -3) for tx in (1.0, .9)]

R['corvo'] = lambda: [pico(fade(ler('misc_crow.flac', 0, None, tx), sai=.1), -3) for tx in (1.0, .9)]
R['sino'] = lambda: [pico(fade(ler('mcl_bells_bell_stroke.ogg', 0, 7.5), sai=2.5), -2)]
R['sino_grande'] = lambda: [pico(fade(ler('mcl_bells_bell_stroke.ogg', 0, 9.0, .68, 'lowpass=f=3000'), sai=3), -1)]
R['gota'] = lambda: [pico(fade(ler(f'drippingwater_drip.{i}.ogg'), sai=.05), -6) for i in (1, 2, 3)]
R['flare'] = lambda: [pico(fade(ler('tnt_ignite.ogg', 0, 1.3), sai=.5), -4)]
R['coleta'] = lambda: [pico(fade(ler('mcl_potions_bottle_fill.ogg'), sai=.1), -4)]
R['assombro'] = lambda: [pico(fade(ler('cave1.ogg', .8, 4.0, .9, 'lowpass=f=3000'), ent=.4, sai=1.5), -4)]

# ---------- ritual ----------
@receita('ritual_inicio')   # o altar acorda: estrondo grave e o ar se abrindo
def _():
    b = fade(janela_pico('misc_cineboom.flac', .05, 3.4), ent=.01, sai=1.6)
    p = fade(ler('mcl_portals_open_end_portal.ogg', 0, 3.4, .9), ent=.3, sai=1.5)
    return [pico(mix((b, 0, 0), (p, 0, -7)), -1)]

@receita('ritual_marco')    # 25%, 50%, 75%: gongo abafado
def _():
    return [pico(fade(ler('cave4.ogg', 0, 3.0, tx, 'lowpass=f=4000'), sai=1.6), -2) for tx in (.85, .78)]

@receita('fenda')           # ritual completo: estrondo, terra se abrindo e o coro afundando
def _():
    b = fade(janela_pico('misc_cineboom.flac', .05, 4.5, taxa=.8), ent=.01, sai=2.2)
    e = fade(ler(EXPL, 0, 1.3, .7, 'lowpass=f=1500'), sai=.9)
    coro = fade(ff(ler('ambi_choir.flac', 0, None, .5), 'atempo=0.5'), ent=.2, sai=2.0)
    return [pico(mix((b, 0, 0), (e, 0, -4), (coro, .15, -9)), -1)]

@receita('farol')           # ritual selado: sino claro e um acorde do coro subindo
def _():
    sino = fade(ler('perc_bell.flac', 0, 4.5), sai=2.0)
    c = [fade(ff(ler('ambi_choir.flac', 0, None, tx), 'atempo=0.6'), ent=.15, sai=1.4) for tx in (1.0, 1.26, 1.5)]
    return [pico(mix((sino, 0, 0), (c[0], .05, -7), (c[1], .12, -10), (c[2], .2, -12)), -1)]

@receita('selo_quebra')     # selo interrompido: vidro e um golpe surdo
def _():
    out = []
    for i in (1, 2, 3):
        v = fade(ler(f'default_break_glass.{i}.ogg', 0, .6, .9), sai=.3)
        b = fade(ler('perc_impact2.flac', 0, .3, .85, 'lowpass=f=2200'), sai=.2)
        out.append(pico(aparar(mix((v, 0, 0), (b, 0, -8))), -2))
    return out

@receita('canal_quebra')    # canalização interrompida: o coro engasga
def _():
    c = fade(ff(ler('ambi_choir.flac', .1, .7, .7), 'atempo=1.6'), sai=.25)
    p = fade(ler('mcl_mobs_mob_poof.ogg', 0, .4, .7), sai=.25)
    return [pico(mix((c, 0, -2), (p, 0, 0)), -2)]

R['selando'] = lambda: [pico(fade(ler('ambi_glass_rub.flac', 0, 2.2), ent=.05, sai=.9), -3)]

@receita('coro_laco')       # coro do ritual (laço): o coro gravado esticado e dobrado uma oitava abaixo
def _():
    alto = ff(ler('ambi_choir.flac'), 'atempo=0.5,atempo=0.5')
    baixo = ff(ler('ambi_choir.flac', 0, None, .5), 'atempo=0.5')
    n = min(len(alto), len(baixo))
    return [pico(laco(mix((alto[:n], 0, 0), (baixo[:n], 0, -3)), .9), -3)]

R['zumbido_laco'] = lambda: [pico(laco(ler('ambi_haunted_hum.flac', .6, 8.6), 1.2), -3)]   # sussurros e zumbido do ritual (laço)

# ---------- investigação ----------
R['evp'] = lambda: [pico(fade(ler('ambi_glass_hum.flac', 1.0, 2.6, 1.1), ent=.2, sai=1.2), -4)]
R['descoberta'] = lambda: [pico(fade(ler('perc_bell2.flac', 0, 2.2, tx), sai=1.2), -4) for tx in (1.15, 1.0)]
@receita('chamariz_desfeito')
def _():
    p = fade(ler('mcl_mobs_mob_poof.ogg', 0, .7, .7), sai=.4)
    g = fade(ler('ambi_glass_hum.flac', 1.0, 1.2, .8), ent=.05, sai=.8)
    return [pico(mix((p, 0, 0), (g, 0, -8)), -3)]
@receita('transferencia')
def _():
    t = fade(ler('mcl_portals_teleport.ogg', 0, 2.4, .8), sai=1.0)
    d = fade(ler('perc_door.flac', 0, 1.6, .8), sai=.8)
    return [pico(mix((t, 0, 0), (d, .1, -6)), -2)]

# ---------- mecânica das armas na mão (v5.9: recargas, sacar, inspeção) ----------
@receita('cartucho')        # cartucho entrando pela portinhola da carabina: mola da portinhola e latão raspando
def _():
    out = []
    for tx in [1.55, 1.7, 1.45]:
        a = fade(ler('default_dig_metal.ogg', 0, .07, tx, 'highpass=f=1100'), sai=.04)
        b = fade(ler('perc_snap.flac', 0, .05, 1.9, 'highpass=f=2600'), sai=.03)
        c = fade(ler('mcl_armor_equip_chainmail.ogg', .02, .07, 1.9, 'highpass=f=1800'), sai=.05)
        out.append(pico(mix((a, 0, 0), (b, .004, -7), (c, .01, -12)), -6))
    return out

@receita('cartucho_fim')    # o polegar assenta o último cartucho no tubo: mais grave e seco
def _():
    a = fade(ler('default_dug_metal.1.ogg', 0, .1, 1.3, 'highpass=f=600'), sai=.06)
    b = fade(ler('perc_snap.flac', 0, .05, 1.5, 'highpass=f=1800'), sai=.03)
    return [pico(mix((a, 0, 0), (b, 0, -6)), -5)]

@receita('tambor_abre')     # o tambor do revólver sai para o lado
def _():
    a = fade(ler('default_place_node_metal.2.ogg', 0, .12, 1.55, 'highpass=f=700'), sai=.07)
    b = fade(ler('perc_snap.flac', 0, .05, 1.3, 'highpass=f=1500'), sai=.03)
    return [pico(mix((b, 0, -4), (a, .012, 0)), -5)]

@receita('tambor_fecha')    # o tambor volta com um tapa, a trava estala e ele ainda gira um pouco
def _():
    a = fade(ler('default_dug_metal.2.ogg', 0, .14, 1.35, 'highpass=f=500'), sai=.08)
    b = fade(ler('perc_snap.flac', 0, .06, 1.15, 'highpass=f=1200'), sai=.04)
    giro = fade(ler('mcl_armor_equip_chainmail.ogg', 0, .22, 2.2, 'highpass=f=2500'), sai=.16)
    return [pico(mix((b, 0, -3), (a, 0, 0), (giro, .03, -14)), -3)]

@receita('extrator')        # a vareta do extrator empurra as cápsulas para fora do tambor
def _():
    a = fade(ler('mcl_armor_equip_chainmail.ogg', 0, .16, 1.65, 'highpass=f=1200'), sai=.1)
    b = fade(ler('default_dig_metal.ogg', 0, .06, 1.8, 'highpass=f=1500'), sai=.04)
    return [pico(mix((b, 0, -3), (a, .01, 0)), -6)]

@receita('capsulas')        # cápsulas de latão caindo e quicando no chão
def _():
    out = []
    for semente in [3, 11]:
        r = np.random.RandomState(semente); camadas = []
        for k in range(7):
            tx = 2.6 + r.rand() * 1.4; at = k * .045 + r.rand() * .05 + (.12 if k > 3 else 0)
            p = fade(ler('perc_bell2.flac', 0, .09, tx, 'highpass=f=2200'), sai=.07, curva=3)
            camadas.append((p, at, -2 - r.rand() * 8))
        camadas.append((fade(ler('default_dig_metal.ogg', 0, .05, 2.1, 'highpass=f=2000'), sai=.03), .02, -6))
        out.append(pico(mix(*camadas), -8))
    return out

@receita('municiador')      # os oito cartuchos do municiador entram nas câmaras; o botão gira e solta
def _():
    a = fade(ler('mcl_armor_equip_chainmail.ogg', 0, .12, 1.25, 'highpass=f=900'), sai=.08)
    b = fade(ler('default_dig_metal.ogg', 0, .06, 1.5, 'highpass=f=1200'), sai=.04)
    c = fade(ler('perc_snap.flac', 0, .05, 1.7, 'highpass=f=2200'), sai=.03)
    return [pico(mix((a, 0, 0), (b, .02, -2), (c, .11, -4)), -5)]

@receita('roupa')           # tecido e couro: o braço se mexe para a recarga ou para sacar
def _():
    out = []
    for tx in [1.0, 1.15]:
        w = fade(janela_pico('perc_swash.flac', .06, .16, taxa=tx, af='lowpass=f=2200,highpass=f=180'), ent=.02, sai=.1)
        out.append(pico(w, -14))
    return out

@receita('empunhar')        # mão de luva batendo na arma ao firmar a pegada
def _():
    a = fade(ler('perc_impact1.flac', 0, .09, 1.6, 'lowpass=f=2600,highpass=f=220'), sai=.06)
    b = fade(ler('mcl_armor_equip_chainmail.ogg', .03, .06, 1.2, 'highpass=f=1600'), sai=.04)
    return [pico(mix((a, 0, 0), (b, .005, -12)), -9)]

@receita('cetro_sacar')     # o cetro acorda: ar puxado e as vértebras zumbindo
def _():
    w = fade(janela_pico('ambi_dark_woosh.flac', .3, .35, taxa=1.15, af='highpass=f=120'), ent=.02, sai=.25)
    h = fade(ler('ambi_glass_hum.flac', 0, .7, 1.4, 'highpass=f=300'), ent=.2, sai=.4)
    return [pico(mix((w, 0, 0), (h, .15, -8)), -5)]

@receita('runa_volta')      # uma runa volta a orbitar o cetro (o fervor recarregou um sigilo)
def _():
    a = fade(ler('ambi_glass_rub.flac', .2, .35, 1.8, 'highpass=f=900'), ent=.03, sai=.25)
    b = fade(ler('perc_bell2.flac', 0, .3, 2.2, 'highpass=f=1500'), sai=.25, curva=3)
    return [pico(mix((a, 0, 0), (b, .02, -10)), -14)]

@receita('bencao')          # o Exorcista toca a cruz do cabo: prata cantando baixinho
def _():
    h = fade(ler('ambi_glass_hum.flac', .3, .8, 1.2, 'highpass=f=400'), ent=.08, sai=.5)
    b = fade(ler('perc_bell2.flac', 0, .6, 1.6, 'highpass=f=900'), sai=.5, curva=3)
    return [pico(mix((h, 0, 0), (b, .04, -9)), -12)]

# ---------------- v6: gravações CC0 do conquest (cortes prontos; aqui só se apara, normaliza e se fecha o laço) ----------------
R['queda_corpo'] = lambda: [pico(fade(aparar(ler(f'cq_death-thud-mid-{k}.mp3', 0, 1.4)), sai=.25), -2) for k in (1, 2, 3)]   # corpo que cai no chão
R['sussurro'] = lambda: [pico(fade(aparar(ler(f'cq_ghost-call-{k}.mp3', 0, 3.0)), ent=.05, sai=.6), -5) for k in (1, 2, 3)]  # vozes sussurrando
R['estalo_fogo'] = lambda: [pico(fade(aparar(ler(f'cq_crackle-{k}.mp3', 0, .8)), sai=.15), -4) for k in (1, 2, 3, 4)]       # brasa estalando
R['folhas'] = lambda: [pico(fade(aparar(ler(f'cq_leaves-{k}.mp3', 0, 1.6)), ent=.08, sai=.5), -6) for k in (1, 2)]          # folhagem mexida
R['coruja'] = lambda: [pico(fade(aparar(ler(f'cq_owl-{k}.mp3', 0, 3.0)), sai=.4), -6) for k in (1, 2, 3)]                   # coruja na mata
R['rangido'] = lambda: [pico(fade(aparar(ler(f'cq_creak-tree-{k}.mp3', 0, 2.5)), sai=.4), -6) for k in (1, 2, 3)]           # árvore rangendo
def _laco_cq(arq, dur, alvo):
    return [pico(laco(ler(arq, 1.0, dur), 1.4), alvo)]
R['mata_laco'] = lambda: _laco_cq('cq_wind-forest-1.mp3', 15, -10)     # vento nas copas (mata)
R['noite_laco'] = lambda: _laco_cq('cq_night-loop-1.mp3', 14, -12)     # grilos da noite (céu aberto)
R['cripta_laco'] = lambda: _laco_cq('cq_crypt-loop-1.mp3', 14, -14)    # ar parado de pedra (dentro da catedral)
R['charco_laco'] = lambda: _laco_cq('cq_frogs-loop-1.mp3', 14, -10)    # sapos do Charco
R['fogo_laco'] = lambda: _laco_cq('cq_brazier-loop-1.mp3', 9, -10)     # fornos da Carvoaria
R['chuva_laco'] = lambda: _laco_cq('cq_rain-loop-1.mp3', 14, -8)       # chuva (clima)

# ---------------- v6.3: o que era sintetizado (bipes, osciladores e ruído) agora sai de gravações ----------------
@receita('clique')          # botões da interface: um estalo seco e baixo
def _():
    a = fade(ler('perc_snap.flac', 0, .05, 2.2, 'highpass=f=2000'), sai=.03)
    b = fade(ler('default_place_node_metal.2.ogg', 0, .05, 2.0, 'highpass=f=1500'), sai=.03)
    return [pico(a, -14), pico(b, -15)]

@receita('aviso')           # frase rápida, ping marcado, passo do tutorial: um toque curto de sino
def _():
    return [pico(fade(ler('perc_bell2.flac', 0, .5, tx, 'highpass=f=700'), sai=.4, curva=3), -12) for tx in (1.6, 1.8)]

@receita('ping')            # sensor do Caçador: o jogo muda a altura com a leitura (mais perto, mais agudo)
def _():
    return [pico(fade(ler('perc_bell2.flac', 0, .45, 1.0, 'highpass=f=400'), sai=.35, curva=2.5), -8)]

@receita('vazio')           # gatilho sem munição, sigilo sem fervor, feitiço recarregando
def _():
    a = fade(ler('default_dig_metal.ogg', 0, .05, 1.9, 'highpass=f=1500'), sai=.03)
    return [pico(a, -12)]

@receita('coracao')         # batimento (sanidade baixa, sendo executado): dois golpes surdos
def _():
    k = lambda tx: fade(ler('drum_heavy_kick.flac', 0, .18, tx, 'lowpass=f=150'), sai=.12)
    return [pico(mix((k(.8), 0, 0), (k(.74), .19, -3)), -3)]

@receita('tambor')          # o tambor do Grande Ritual (acelera com o progresso)
def _():
    t = fade(ler('drum_tom_lo_hard.flac', 0, .7, .66, 'lowpass=f=500'), sai=.4)
    k = fade(ler('drum_heavy_kick.flac', 0, .3, .8, 'lowpass=f=200'), sai=.2)
    return [pico(ff(mix((t, 0, 0), (k, 0, -4)), 'aecho=0.7:0.4:180:0.25'), -3)]

@receita('presagio')        # presságio: um ritual vai começar (o ar desce e escurece)
def _():
    b = fade(janela_pico('misc_cineboom.flac', .05, 3.0, taxa=.55, af='lowpass=f=600'), ent=.05, sai=1.6)
    w = fade(ler('ambi_dark_woosh.flac', 0, 3.0, .7, 'lowpass=f=2500'), ent=.6, sai=1.2)
    return [pico(mix((b, 0, 0), (w, 0, -6)), -2)]

@receita('eco_coro')        # eco do coro: três vozes graves vindas do setor do ritual
def _():
    v = [fade(ff(ler('ambi_choir.flac', 0, None, .5 * tx), 'atempo=0.75'), ent=.25, sai=1.2) for tx in (1.0, 1.26, 1.5)]
    return [pico(ff(mix((v[0], 0, 0), (v[1], .14, -2), (v[2], .28, -4)), 'aecho=0.8:0.5:300:0.3'), -3)]

@receita('runa')            # Runa e Véu: vidro esfregado subindo e um toque de sino
def _():
    a = fade(ler('ambi_glass_rub.flac', .1, .7, 1.25, 'highpass=f=500'), ent=.05, sai=.4)
    b = fade(ler('perc_bell2.flac', 0, .6, 1.35, 'highpass=f=800'), sai=.45, curva=3)
    return [pico(mix((a, 0, 0), (b, .12, -8)), -4)]

@receita('clarao')          # Flash: a pólvora acende de uma vez
def _():
    f = fade(ler('tnt_ignite.ogg', 0, .45, 1.4, 'highpass=f=1200'), sai=.3)
    s = fade(ler('perc_snap.flac', 0, .06, 1.3, 'highpass=f=1500'), sai=.04)
    return [pico(mix((f, 0, 0), (s, 0, -3)), -3)]

@receita('purificacao')     # Purificação: sino claro com o coro por cima
def _():
    b = fade(ler('perc_bell.flac', 0, 1.4, 1.5), sai=.9)
    c = fade(ff(ler('ambi_choir.flac', 0, None, 1.5), 'atempo=0.8'), ent=.05, sai=.8)
    return [pico(mix((b, 0, 0), (c, .03, -9)), -3)]

@receita('sal')             # Círculo de Sal: sal escorrendo da mão no chão
def _():
    a = fade(ler('default_sand_footstep.2.ogg', 0, .3, 1.6, 'highpass=f=1800'), ent=.03, sai=.2)
    b = fade(ler('default_gravel_dig.1.ogg', 0, .25, 1.8, 'highpass=f=2200'), sai=.2)
    return [pico(mix((a, 0, 0), (b, .12, -4), (a, .26, -6)), -6)]

@receita('trovao')          # trovão depois do relâmpago: estrondo longo e baixo
def _():
    out = []
    for tx in (.55, .5):
        b = fade(janela_pico('misc_cineboom.flac', .05, 4.0, taxa=tx, af='lowpass=f=450'), ent=.08, sai=2.4)
        e = fade(ler(EXPL, 0, 1.4, tx * .7, 'lowpass=f=240'), ent=.05, sai=1.2)
        out.append(pico(mix((b, 0, 0), (e, .1, -3)), -2))
    return out

@receita('alucinacao')      # sanidade baixa: passos que não são de ninguém
def _():
    return [pico(mix(*[(fade(ler(f'default_sand_footstep.{i}.ogg', 0, .25, .85, 'lowpass=f=900'), sai=.1), k * .38, -k)
                       for k, i in enumerate((1, 2, 3))]), -6)]

def _acorde_coro(semis, dur):
    """Um acorde do coro gravado: uma cópia por nota (semitons a partir do ré do coro, uma oitava abaixo), esticada até dur."""
    vozes = []
    for s in semis:
        x = ff(ler('ambi_choir.flac', 0, None, .5 * 2 ** (s / 12)), 'atempo=0.5,atempo=0.5')
        while len(x) < int(dur * SR): x = np.concatenate([x, x])   # o coro gravado é curto: emenda até a duração do acorde
        vozes.append((fade(x[:int(dur * SR)], ent=1.6, sai=1.8, curva=1), 0, -2 * len(vozes)))
    return mix(*vozes)

@receita('musica_laco')     # música da tela inicial (laço): ré menor (Rém, Si♭, Solm, Lá) no coro gravado, sino a cada volta, eco de nave
def _():
    acordes = [[0, 3, 7], [-4, 0, 3], [-7, -4, 0], [-5, -1, 2]]; passo = 6.4
    camadas = [(_acorde_coro(a, passo + 1.8), i * passo, 0) for i, a in enumerate(acordes)]
    camadas.append((fade(ler('mcl_bells_bell_stroke.ogg', 0, 6.0, .5, 'lowpass=f=2500'), sai=3), .9, -6))
    camadas.append((fade(ler('ambi_haunted_hum.flac', 0, None, .5, 'lowpass=f=400'), ent=2, sai=3), 2.0, -16))
    y = ff(mix(*camadas), 'aecho=0.8:0.55:430:0.45')
    y = y[:int((4 * passo + 1.8) * SR)]
    return [pico(laco(y, 1.8), -6)]

R['pulso_laco'] = lambda: [pico(laco(ler('ambi_haunted_hum.flac', 0, None, .4, 'lowpass=f=170,lowpass=f=170,lowpass=f=200,tremolo=f=1.3:d=0.8'), 1.5), -4)]   # pulsação grave do ritual (laço)
R['bordao_laco'] = lambda: [pico(laco(ler('ambi_haunted_hum.flac', 0, None, .5, 'lowpass=f=320,lowpass=f=320,lowpass=f=400,tremolo=f=0.25:d=0.4'), 1.5), -4)] # bordão do Grande Ritual (laço)
@receita('tensao_laco')     # trilha de tensão (laço): bordão grave e um chiado alto de vidro
def _():
    d = ler('ambi_haunted_hum.flac', 0, None, .5, 'lowpass=f=180,lowpass=f=180,lowpass=f=220,tremolo=f=0.35:d=0.5')
    c = ler('ambi_glass_hum.flac', 0, None, 1.2, 'bandpass=f=3200:width_type=q:w=2')
    n = min(len(d), len(c))
    return [pico(laco(mix((d[:n], 0, 0), (c[:n], 0, -10)), 1.5), -4)]

USO = {   # para que serve cada som no jogo (vai para docs/AUDIO.md)
  'clique': 'botões da interface', 'aviso': 'frase rápida, ping marcado e passo do tutorial', 'ping': 'sensor do Caçador (altura pela leitura)',
  'vazio': 'gatilho sem munição, sem fervor ou feitiço recarregando', 'coracao': 'batimento (sanidade baixa, sendo executado)',
  'tambor': 'tambor do Grande Ritual', 'presagio': 'presságio de ritual', 'eco_coro': 'eco do coro (25% do ritual)', 'runa': 'Runa e Véu',
  'clarao': 'Flash', 'purificacao': 'Purificação', 'sal': 'Círculo de Sal', 'trovao': 'trovão na tempestade', 'alucinacao': 'passos alucinados (sanidade baixa)',
  'musica_laco': 'música da tela inicial (laço)', 'pulso_laco': 'pulsação do ritual em andamento (laço)', 'bordao_laco': 'bordão do Grande Ritual (laço)',
  'tensao_laco': 'trilha de tensão (laço)',
  'tiro_carabina': 'tiro da carabina de alavanca (Soldado, Rastreador)', 'tiro_revolver': 'tiro do revólver (Exorcista)',
  'alavanca': 'ciclo da alavanca depois do tiro', 'cao': 'cão do revólver e tambor', 'recarga': 'recarga (abrir, cartuchos, fechar)',
  'cartucho': 'cartucho entrando pela portinhola da carabina', 'cartucho_fim': 'último cartucho assentado no tubo',
  'tambor_abre': 'tambor do revólver abrindo', 'tambor_fecha': 'tambor do revólver fechando', 'extrator': 'extrator do revólver',
  'capsulas': 'cápsulas caindo no chão', 'municiador': 'municiador do revólver entrando e girando', 'roupa': 'braço e roupa se mexendo',
  'empunhar': 'mão firmando a pegada', 'cetro_sacar': 'cetro acordando ao sacar', 'runa_volta': 'runa voltando a orbitar o cetro',
  'bencao': 'Exorcista toca a cruz do revólver',
  'sigilo': 'sigilo lançado pelo cetro', 'estouro': 'sigilo estourando', 'impacto_pedra': 'bala na pedra', 'impacto_madeira': 'bala na madeira e nos troncos',
  'impacto_terra': 'bala no chão de terra', 'impacto_corpo': 'acerto confirmado (golpe em alguém)', 'derrubou': 'queda confirmada (para quem derrubou)',
  'dor': 'você levou dano', 'empurrao': 'Empurrão', 'passo_pedra': 'passos na catedral e no Claustro', 'passo_terra': 'passos nas trilhas',
  'passo_folhas': 'passos na mata fechada', 'passo_lama': 'passos na chuva (camada por cima)', 'corvo': 'corvos', 'sino': 'sino da catedral',
  'sino_grande': 'sino do Grande Ritual', 'gota': 'gotas de água na nave', 'flare': 'flare aceso', 'coleta': 'reagente coletado', 'assombro': 'alucinação com sanidade baixa',
  'ritual_inicio': 'ritual começando (ou aparecendo para a caça)', 'ritual_marco': 'ritual passando de 25%, 50% e 75%', 'fenda': 'Fenda (ritual completo)',
  'farol': 'Farol (ritual selado)', 'selo_quebra': 'selo interrompido', 'canal_quebra': 'canalização interrompida', 'selando': 'a caça começou a selar',
  'queda_corpo': 'alguém derrubado ou executado cai no chão', 'sussurro': 'inspeção do cetro e execução pelo culto', 'estalo_fogo': 'reagente queimado pela caça',
  'folhas': 'coleta de reagente na mata', 'coruja': 'mata à noite (avulso)', 'rangido': 'mata à noite (avulso)', 'mata_laco': 'fundo da mata (laço)', 'noite_laco': 'fundo a céu aberto (laço)',
  'cripta_laco': 'fundo dentro da catedral (laço)', 'charco_laco': 'fundo perto do Charco (laço)', 'fogo_laco': 'fornos da Carvoaria (laço)', 'chuva_laco': 'chuva, no clima de chuva (laço)',
  'coro_laco': 'coro do ritual em andamento (laço)', 'zumbido_laco': 'sussurros do ritual em andamento (laço)', 'evp': 'EVP: vozes num altar desperto',
  'descoberta': 'descoberta da investigação e lances da noite', 'chamariz_desfeito': 'Lanterna desmascara um Chamariz', 'transferencia': 'Transferência'}

def main():
    os.makedirs(SAIDA, exist_ok=True)
    fontes = json.load(open(os.path.join(RAIZ, 'assets', 'sons', 'fontes.json'), encoding='utf-8'))['arquivos']
    js = {}; resumo = []; usos = {}
    for nome, f in R.items():
        js[nome] = []; USADAS.clear(); variacoes = f(); usos[nome] = sorted(USADAS)
        faltando = [u for u in USADAS if u not in fontes]
        if faltando: sys.exit(f'{nome}: gravação sem licença registrada em fontes.json: {faltando}')
        for k, x in enumerate(variacoes):
            x = np.clip(x, -1, 1).astype(np.float32)
            arq = os.path.join(SAIDA, f'{nome}{"" if k == 0 else "_" + str(k + 1)}.mp3')
            with tempfile.NamedTemporaryFile(suffix='.f32', delete=False) as t:
                t.write(x.tobytes()); tmp = t.name
            q = '6' if nome.endswith('_laco') else '4'   # laços em qualidade menor: são longos e ficam ao fundo
            subprocess.run([FF, '-v', 'error', '-y', '-f', 'f32le', '-ar', str(SR), '-ac', '1', '-i', tmp, '-c:a', 'libmp3lame', '-q:a', q, arq], check=True)
            os.unlink(tmp)
            js[nome].append(base64.b64encode(open(arq, 'rb').read()).decode())
            resumo.append((os.path.basename(arq), len(x) / SR, os.path.getsize(arq)))
    usadas = sorted({u for l in usos.values() for u in l})
    autores = {}
    for u in usadas:
        F = fontes[u]; k = (F['autor'], F['licenca'], F['fonte']); autores.setdefault(k, []).append(u)
    creditos = [{'autor': a, 'licenca': l, 'fonte': fo} for (a, l, fo) in sorted(autores)]
    with open(os.path.join(RAIZ, 'client', 'sons.js'), 'w') as o:
        o.write('// Gerado por tools/montar-sons.py a partir de assets/sons/fontes (licenças em docs/AUDIO.md). Não edite à mão.\n')
        o.write('window.SONS_RR_CREDITOS=' + json.dumps(creditos, ensure_ascii=False) + ';\n')
        o.write('window.SONS_RR=' + json.dumps(js, separators=(',', ':')) + ';\n')
    escrever_doc(usos, fontes, autores)
    os.makedirs(UNITY, exist_ok=True)
    for a in os.listdir(UNITY):
        if a.endswith('.mp3') and not os.path.exists(os.path.join(SAIDA, a)): os.unlink(os.path.join(UNITY, a))   # som que saiu do jogo sai do Unity
    for a in sorted(os.listdir(SAIDA)):
        if a.endswith('.mp3'): shutil.copyfile(os.path.join(SAIDA, a), os.path.join(UNITY, a))
    tot = sum(s for _, _, s in resumo)
    for a, d, s in resumo: print(f'  {a:28s} {d:5.2f} s {s / 1024:6.1f} KB')
    print(f'{len(R)} sons, {len(resumo)} arquivos, {tot / 1024:.0f} KB em MP3; client/sons.js {os.path.getsize(os.path.join(RAIZ, "client", "sons.js")) / 1024:.0f} KB')

def escrever_doc(usos, fontes, autores):
    L = ['# Áudio do Ritual Reversal', '',
         'Gerado por `python3 tools/montar-sons.py` (não edite à mão as tabelas). As gravações originais ficam em',
         '`assets/sons/fontes/` e a origem de cada uma em `assets/sons/fontes.json`; o script monta os sons do jogo',
         '(cortes, filtros, camadas, normalização) em `assets/sons/jogo/*.mp3` e em `client/sons.js`.', '',
         '## Regras de licença', '',
         '- Só entram gravações em **CC0 1.0**, **CC BY 3.0** ou **domínio público**. Nada de CC BY-SA, GPL, uso não comercial ou licença não declarada.',
         '- CC BY 3.0 exige atribuição: os créditos abaixo também aparecem no jogo (Ajustes, Créditos de áudio).',
         '- Os sites de áudio (Freesound, OpenGameArt, Pixabay, Kenney, Sonniss) estavam bloqueados pela rede do ambiente de',
         '  desenvolvimento. As gravações vieram de repositórios públicos no GitHub que as redistribuem com a licença de cada arquivo',
         '  documentada (Sonic Pi, Minetest Game, VoxeLibre; na v6, conquest); o link original do Freesound/OpenGameArt de cada uma está na tabela.',
         '- v6: os arquivos `cq_*.mp3` são cortes (trecho e ganho) que o repositório hoffspot/conquest fez de gravações CC0 do Freesound e de',
         '  uma gravação em domínio público do National Park Service (EUA). A gravação de cada um é CC0 ou domínio público; o corte vem com o',
         '  repositório, sob Apache 2.0 (<https://www.apache.org/licenses/LICENSE-2.0>), cuja licença e aviso valem para esses arquivos.',
         '- Desde a v6.3 nada é sintetizado: os bipes, o vento de ruído, a música do menu, a tensão e as pulsações dos rituais, que eram',
         '  osciladores e ruído no Web Audio e no Som.cs, viram receitas sobre as mesmas gravações (`clique`, `ping`, `musica_laco`...).',
         '  Nenhum som do jogo foi gerado por IA.',
         '- O tiro não é a gravação de uma arma: é montado com gravações reais em CC0 (explosão, impacto, estalo e mecanismo de metal).',
         '  Quando os sites de áudio estiverem acessíveis, troque por gravações de armas reais e rode o script de novo.', '',
         '## Como foi conferido', '',
         '- Ninguém ouviu estes sons durante o desenvolvimento: o ambiente não tem saída de áudio. Cada arquivo foi conferido por',
         '  medida (duração, pico, ataque, centroide e bandas do espectro) e por espectrograma, e todos decodificam no Chromium',
         '  (o teste `tools/navegador.js` exige todas as variações carregadas sem falha: 128 na v6.3). **Falta ouvir em fones e ajustar volumes.**', '',
         '## Sons do jogo', '', '| Som | Uso no jogo | Gravações | Licenças |', '| --- | --- | --- | --- |']
    for nome, us in usos.items():
        lic = sorted({fontes[u]['licenca'] for u in us})
        L.append(f'| `{nome}` | {USO.get(nome, "")} | {", ".join(us)} | {", ".join(lic)} |')
    L += ['', '## Créditos (gravações usadas)', '', '| Autor | Licença | Origem | Arquivos |', '| --- | --- | --- | --- |']
    for (a, l, fo), arqs in sorted(autores.items()):
        L.append(f'| {a} | {l} | {fo} | {", ".join(arqs)} |')
    L += ['', 'Licenças: CC0 1.0 <https://creativecommons.org/publicdomain/zero/1.0/> · CC BY 3.0 <https://creativecommons.org/licenses/by/3.0/>', '']
    open(os.path.join(RAIZ, 'docs', 'AUDIO.md'), 'w', encoding='utf-8').write('\n'.join(L))

if __name__ == '__main__': main()
