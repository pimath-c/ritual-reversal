#!/usr/bin/env python3
# Leitor mínimo dos formatos do MakeHuman (CC0), sem o MakeHuman nem o Blender: monta um personagem a partir dos dados do
# MPFB 2 (malha base, alvos de forma, esqueleto "game_engine" e pesos) e dos pacotes de recursos (roupas .mhclo, peles e
# materiais .mhmat). Devolve o que o tools/montar-bonecos.py espera de um glTF: primitivas na pose de repouso (posição,
# normal, uv, índices, imagem e o osso de maior peso de cada vértice) e as posições dos ossos, com os nomes do esqueleto
# "game_engine" (thigh_l, spine_01, upperarm_l, hand_l, neck_01...). Unidades em metros, y para cima, o boneco olhando para +z
# com a esquerda em +x (como os glTF da Quaternius).
#
# Formatos (texto):
#   base.obj      v / vt / f com grupos: "body" é a pele; "helper-*" são apoios das roupas; "joint-*" são cubinhos cujo centro
#                 é a posição de um osso. Unidade: decímetro.
#   *.target.gz   "índice dx dy dz" por linha: deslocamento de um vértice da malha base.
#   rig.*.json    osso → cabeça (cubo "cube_name" ou média de "vertex_indices") e pai.
#   weights.*.json  {"weights": {osso: [[vértice, peso], ...]}}.
#   *.mhclo       roupa: obj_file, material, x/y/z_scale (dois vértices da base e a distância de referência), delete_verts
#                 (vértices da pele escondidos pela roupa) e, depois de "verts 0", uma linha por vértice da roupa:
#                 "v1 v2 v3 w1 w2 w3 dx dy dz" (soma ponderada de três vértices da base mais um deslocamento na escala) ou "v".
#   *.mhmat       material: diffuseTexture <arquivo> (e outras linhas que não usamos).
import gzip, json, os, re
import numpy as np

DM = .1   # decímetro → metro


def ler_obj(caminho):
    """Vértices, uvs e faces (cada face: lista de (v, vt) e o grupo) de um .obj; faces com 3 ou 4 lados."""
    V, T, F, g = [], [], [], ''
    for linha in open(caminho, encoding='utf-8', errors='ignore'):
        if linha.startswith('v '): V.append([float(x) for x in linha.split()[1:4]])
        elif linha.startswith('vt '): T.append([float(x) for x in linha.split()[1:3]])
        elif linha.startswith('g '): g = linha.split(None, 1)[1].strip()
        elif linha.startswith('f '):
            ids = []
            for t in linha.split()[1:]:
                a = t.split('/'); ids.append((int(a[0]) - 1, int(a[1]) - 1 if len(a) > 1 and a[1] else -1))
            F.append((ids, g))
    return np.array(V, dtype=np.float64), np.array(T or [[0, 0]], dtype=np.float64), F


def ler_alvo(caminho):
    idx, d = [], []
    abrir = gzip.open if caminho.endswith('.gz') else open
    with abrir(caminho, 'rt', encoding='utf-8', errors='ignore') as f:
        for linha in f:
            s = linha.split()
            if len(s) == 4 and not linha.startswith('#'): idx.append(int(s[0])); d.append([float(x) for x in s[1:]])
    return np.array(idx, dtype=np.int64), np.array(d, dtype=np.float64).reshape(-1, 3)   # vazio: o alvo é a própria base


def ler_mhmat(caminho):
    """Textura de cor do material (caminho absoluto) ou None."""
    for linha in open(caminho, encoding='utf-8', errors='ignore'):
        s = linha.split(None, 1)
        if len(s) == 2 and s[0] == 'diffuseTexture':
            t = s[1].strip(); f = os.path.join(os.path.dirname(caminho), t)
            return f if os.path.exists(f) else os.path.join(os.path.dirname(caminho), os.path.basename(t))
    return None


def ler_mhclo(caminho):
    C = {'refs': [], 'del': [], 'escala': {}}; modo = None
    for linha in open(caminho, encoding='utf-8', errors='ignore'):
        s = linha.split()
        if not s or s[0].startswith('#'): continue
        if s[0] == 'verts': modo = 'verts'; continue
        if s[0] == 'delete_verts': modo = 'del'; continue
        if modo == 'verts' and re.match(r'^-?\d', s[0]):
            if len(s) >= 9: C['refs'].append(([int(x) for x in s[0:3]], [float(x) for x in s[3:6]], [float(x) for x in s[6:9]]))
            else: C['refs'].append(([int(s[0])] * 3, [1., 0, 0], [0., 0, 0]))
            continue
        if modo == 'del' and re.match(r'^\d', s[0]):
            # números soltos ou intervalos "a - b"
            i = 0
            while i < len(s):
                if i + 2 < len(s) and s[i + 1] == '-': C['del'].extend(range(int(s[i]), int(s[i + 2]) + 1)); i += 3
                else: C['del'].append(int(s[i])); i += 1
            continue
        # palavra-chave (material, x_scale...) não muda o modo: no cabelo, "material" vem depois de "verts 0"
        if s[0] in ('obj_file', 'material', 'name'): C[s[0]] = s[1]
        elif s[0] in ('x_scale', 'y_scale', 'z_scale'): C['escala'][s[0][0]] = (int(s[1]), int(s[2]), float(s[3]))
    return C


def normais(p, idx):
    n = np.zeros_like(p); t = idx.reshape(-1, 3); fn = np.cross(p[t[:, 1]] - p[t[:, 0]], p[t[:, 2]] - p[t[:, 0]])
    for k in range(3): np.add.at(n, t[:, k], fn)
    return n / np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)


def triangular(faces, filtro):
    """(v, vt) de cada canto dos triângulos das faces que passam no filtro."""
    cantos = []
    for ids, g in faces:
        if not filtro(g, ids): continue
        for k in range(1, len(ids) - 1): cantos.extend([ids[0], ids[k], ids[k + 1]])
    return cantos


def malha_de(cantos, P, T, pesos_osso):
    """Vértices únicos por (v, vt): posição, uv, índices e o osso de cada vértice."""
    mapa, pos, uv, osso, idx = {}, [], [], [], []
    for c in cantos:
        k = mapa.get(c)
        if k is None:
            k = mapa[c] = len(pos); pos.append(P[c[0]]); uv.append(T[c[1]] if c[1] >= 0 else (0, 0)); osso.append(pesos_osso(c[0]))
        idx.append(k)
    p = np.array(pos); i = np.array(idx, dtype=np.int64)
    return {'p': p, 'n': normais(p, i), 'uv': np.array(uv, dtype=np.float64), 'idx': i, 'osso': osso}


def montar_humano(dados, recursos, cfg):
    """cfg: {"alvos": [[caminho relativo a targets/, peso], ...], "pele": "skins/.../x.mhmat", "roupas": ["pasta/x.mhclo" ou
    ["pasta/x.mhclo", "pasta/y.mhmat"], ...],
    "esconder": ["grupo"...]}. dados = pasta data do MPFB; recursos = pasta com os pacotes de recursos desempacotados."""
    P0, T0, F0 = ler_obj(os.path.join(dados, '3dobjs', 'base.obj'))
    P = P0.copy()
    for rel, w in cfg.get('alvos', []):
        ii, d = ler_alvo(os.path.join(dados, 'targets', rel)); P[ii] += d * w
    rig = json.load(open(os.path.join(dados, 'rigs', 'standard', 'rig.game_engine.json')))
    W = json.load(open(os.path.join(dados, 'rigs', 'standard', 'weights.game_engine.json')))['weights']
    # cada vértice da base: o osso de maior peso
    melhor = np.full(len(P), -1.0); dono = [''] * len(P)
    for osso, lista in W.items():
        for v, w in lista:
            if w > melhor[v]: melhor[v] = w; dono[v] = osso
    # posição dos ossos: centro do cubinho do grupo ou média dos vértices
    grupos = {}
    for ids, g in F0:
        if g.startswith('joint-'): grupos.setdefault(g, set()).update(v for v, _ in ids)
    def cabeca(b):
        h = rig[b]['head']
        if h.get('strategy') == 'CUBE' and h.get('cube_name') in grupos: return P[sorted(grupos[h['cube_name']])].mean(0)
        if h.get('vertex_indices'): return P[h['vertex_indices']].mean(0)
        return np.zeros(3)
    ossos = {b: cabeca(b) for b in rig}
    chao = P[sorted(grupos['joint-ground'])].mean(0)[1] if 'joint-ground' in grupos else P[:, 1].min()
    desloc = np.array([0, -chao, 0])
    escondidos = set()
    roupas = []
    for rel in cfg.get('roupas', []):
        mat = None
        if isinstance(rel, list): rel, mat = rel   # [roupa, material]: os olhos vêm sem material próprio
        caminho = os.path.join(recursos, rel); C = ler_mhclo(caminho); escondidos.update(C['del'])
        if mat: C['material'] = os.path.relpath(os.path.join(recursos, mat), os.path.dirname(caminho))
        roupas.append((caminho, C))
    corpo_img = ler_mhmat(os.path.join(recursos, cfg['pele'])) if cfg.get('pele') else None
    so = set(cfg.get('grupos', ['body']))
    prims = []
    cantos = triangular(F0, lambda g, ids: g in so and not any(v in escondidos for v, _ in ids))
    m = malha_de(cantos, P, T0, lambda v: dono[v]); m.update(malha='corpo', img=corpo_img); prims.append(m)
    for caminho, C in roupas:
        pasta = os.path.dirname(caminho)
        Pr, Tr, Fr = ler_obj(os.path.join(pasta, C['obj_file']))
        # escala dos deslocamentos: distância atual entre os dois vértices de referência / a de quando a roupa foi feita
        esc = np.ones(3)
        for k, eixo in enumerate('xyz'):
            if eixo in C['escala']:
                a, b, ref = C['escala'][eixo]; esc[k] = abs(P[a][k] - P[b][k]) / ref if ref else 1
        Q = np.zeros((len(C['refs']), 3)); donos = []
        for i, (vs, ws, d) in enumerate(C['refs']):
            Q[i] = sum(P[v] * w for v, w in zip(vs, ws)) + np.array(d) * esc
            donos.append(dono[vs[int(np.argmax(ws))]])
        cantos = triangular(Fr, lambda g, ids: True)
        m = malha_de(cantos, Q, Tr, lambda v: donos[v])
        m.update(malha=C.get('name', os.path.basename(caminho)), img=ler_mhmat(os.path.join(pasta, C['material'])) if C.get('material') else None)
        prims.append(m)
    for m in prims: m['p'] = (m['p'] + desloc) * DM
    return prims, {b: (q + desloc) * DM for b, q in ossos.items()}
