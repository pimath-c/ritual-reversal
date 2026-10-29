#!/usr/bin/env python3
# Armas de verdade (CC0, OpenGameArt) com a forma do modelo e os materiais do jogo: a carabina de alavanca e o revólver.
# assets/armas/fontes.json diz, para cada arma: de onde vem (página, .zip, .fbx e textura de cor), o que cortar (o municiador
# solto que vem junto do revólver), o giro e a escala, e duas âncoras (um ponto do modelo que vai para um ponto da arma do jogo:
# o pivô da alavanca, o centro do tambor). Assim a mão, a alavanca e o tambor continuam onde a animação espera.
#
# Para cada arma:
#   1. converte o FBX em GLB (tools/converter-fbx.js, no Chromium) se ainda não houver em assets/armas/fontes/;
#   2. tira os triângulos das caixas de "cortar"; cada triângulo vai para uma peça ("partes": a alavanca, o tambor; o resto é
#      "estatico") e para um papel de material (caixa, aco, latao, madeira, prata, escuro): madeira e escuro pela cor da textura
#      no centro do triângulo, os outros pela região ("papeis"), com um padrão por arma;
#   3. leva tudo para o quadro da arma do jogo (cano em -z): gira em y, escala e põe a âncora no lugar; cada peça móvel fica no
#      espaço do seu pivô e pode ser ajustada ao tamanho da peça do jogo ("ajuste": raio e comprimento do tambor).
# Grava um GLB enxuto sem textura (o jogo pinta com os materiais por papel, e as skins continuam valendo) com TEXCOORD_1 =
# (peça, papel) e, em extras, as peças, os papéis, os pivôs e a boca do cano. Saída: client/armas.js (RR_ARMAS, embutido por
# build.js e build-mobile.js e servido pelo servidor) e unity/Assets/RitualReversal/Resources/Armas/<nome>.bytes.
# Uso: THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) python3 tools/montar-armas.py   (Pillow, numpy; a conversão precisa do Chromium)
import base64, colorsys, io, json, os, struct, subprocess, sys, urllib.request, zipfile, importlib.util
import numpy as np
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PASTA = os.path.join(RAIZ, 'assets', 'armas')
ORIG = os.path.join(PASTA, 'fontes')
UNITY = os.path.join(RAIZ, 'unity', 'Assets', 'RitualReversal', 'Resources', 'Armas')
_sp = importlib.util.spec_from_file_location('montar_modelos', os.path.join(RAIZ, 'tools', 'montar-modelos.py'))
MM = importlib.util.module_from_spec(_sp); _sp.loader.exec_module(MM)   # ler_glb e juntar (o GLB vira posições, uvs e a imagem)


def fonte_glb(nome, cfg):
    glb = os.path.join(ORIG, nome + '.glb')
    if os.path.exists(glb): return glb
    os.makedirs(ORIG, exist_ok=True); z = os.path.join(ORIG, nome + '.zip')
    if not os.path.exists(z):
        req = urllib.request.Request(cfg['zip'], headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req, timeout=180) as r, open(z, 'wb') as f: f.write(r.read())
    dest = os.path.join(ORIG, nome); os.makedirs(dest, exist_ok=True); Z = zipfile.ZipFile(z)
    for n in Z.namelist():
        if '..' not in n and not n.endswith('/'): open(os.path.join(dest, os.path.basename(n)), 'wb').write(Z.read(n))
    subprocess.run(['node', os.path.join(RAIZ, 'tools', 'converter-fbx.js'), os.path.join(dest, cfg['fbx']), os.path.join(dest, cfg['textura']), glb], check=True)
    return glb


def dentro(c, caixa):
    return all(caixa[k][0] <= c[k] <= caixa[k][1] for k in range(3))


def no_cilindro(c, cil):
    return cil['x'][0] <= c[0] <= cil['x'][1] and (c[1] - cil['yz'][0]) ** 2 + (c[2] - cil['yz'][1]) ** 2 <= cil['raio'] ** 2


def regra(c, r):
    return dentro(c, r['caixa']) if 'caixa' in r else no_cilindro(c, r['cilindro'])


def gravar_glb(p, n, uv2, idx, extras):
    pos = p.astype(np.float32).tobytes(); nor = n.astype(np.float32).tobytes(); t2 = uv2.astype(np.float32).tobytes()
    grande = len(p) > 65535; ind = idx.astype(np.uint32 if grande else np.uint16).tobytes()
    def alinhar(b): return b + b'\0' * ((4 - len(b) % 4) % 4)
    partes, vistas, ofs = [], [], 0
    for b, alvo in ((pos, 34962), (nor, 34962), (t2, 34962), (ind, 34963)):
        vistas.append({'buffer': 0, 'byteOffset': ofs, 'byteLength': len(b), 'target': alvo}); b = alinhar(b); partes.append(b); ofs += len(b)
    BIN = b''.join(partes)
    J = {'asset': {'version': '2.0', 'generator': 'Ritual Reversal montar-armas.py'}, 'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'mesh': 0}],
         'meshes': [{'primitives': [{'attributes': {'POSITION': 0, 'NORMAL': 1, 'TEXCOORD_1': 2}, 'indices': 3}], 'extras': extras}],
         'accessors': [{'bufferView': 0, 'componentType': 5126, 'count': len(p), 'type': 'VEC3', 'min': p.min(0).tolist(), 'max': p.max(0).tolist()},
                       {'bufferView': 1, 'componentType': 5126, 'count': len(p), 'type': 'VEC3'},
                       {'bufferView': 2, 'componentType': 5126, 'count': len(p), 'type': 'VEC2'},
                       {'bufferView': 3, 'componentType': 5125 if grande else 5123, 'count': len(idx), 'type': 'SCALAR'}],
         'bufferViews': vistas, 'buffers': [{'byteLength': len(BIN)}]}
    js = json.dumps(J, separators=(',', ':')).encode(); js += b' ' * ((4 - len(js) % 4) % 4)
    return struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(BIN)) + struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(BIN), 0x004E4942) + BIN


def montar(nome, cfg):
    J, BIN = MM.ler_glb(open(fonte_glb(nome, cfg), 'rb').read()); p, n, uv, idx, img = MM.juntar(J, BIN)
    px = img.convert('RGB').load(); iw, ih = img.size
    tri = idx.reshape(-1, 3); cent = p[tri].mean(1); uvc = uv[tri].mean(1)
    manter = np.array([not any(regra(c, k) for k in cfg.get('cortar', [])) for c in cent]); tri, cent, uvc = tri[manter], cent[manter], uvc[manter]
    PARTES = ['estatico'] + [r['parte'] for r in cfg.get('partes', [])]; PAPEIS = cfg['papeis_lista']
    def papel(c, u):
        r, g, b = (v / 255 for v in px[int(u[0] % 1 * (iw - 1)), int(u[1] % 1 * (ih - 1))]); h, s, v = colorsys.rgb_to_hsv(r, g, b)
        if 'madeira' in PAPEIS and (h < 45 / 360 or h > 340 / 360) and s > .28 and v > .08: return 'madeira'
        if 'escuro' in PAPEIS and cfg.get('escuro_val') and v < cfg['escuro_val']: return 'escuro'
        for rg in cfg.get('papeis', []):
            if regra(c, rg): return rg['papel']
        return cfg['papel_padrao']
    parte = [next((r['parte'] for r in cfg.get('partes', []) if regra(c, r)), 'estatico') for c in cent]
    pap = [papel(c, u) for c, u in zip(cent, uvc)]
    # do modelo para o quadro da arma do jogo: gira em y, escala e leva a âncora ao lugar
    a = np.radians(cfg['giro']); R = np.array([[np.cos(a), 0, np.sin(a)], [0, 1, 0], [-np.sin(a), 0, np.cos(a)]])
    s = cfg['escala']; A0 = np.array(cfg['ancora_modelo'], dtype=np.float64); A1 = np.array(cfg['ancora_jogo'], dtype=np.float64)
    q = (p - A0) @ R.T * s + A1; m = n @ R.T
    pivos = {r['parte']: r['pivo'] for r in cfg.get('partes', [])}
    ps, ns, u2, ids = [], [], [], []
    for pi, pt in enumerate(PARTES):
        sel = [k for k in range(len(tri)) if parte[k] == pt]
        if not sel: continue
        us, inv = np.unique(tri[sel].reshape(-1), return_inverse=True)
        # um vértice usado por triângulos de papéis diferentes é duplicado: o papel vai por vértice no TEXCOORD_1
        for pa in sorted(set(pap[k] for k in sel)):
            ts = np.array([tri[k] for k in sel if pap[k] == pa]); usados, iv = np.unique(ts.reshape(-1), return_inverse=True)
            v = q[usados].copy(); nn = m[usados].copy()
            if pt in pivos:
                v -= np.array(pivos[pt])
                aj = next((r.get('ajuste') for r in cfg['partes'] if r['parte'] == pt), None)
                if aj:   # a peça do modelo no tamanho da peça do jogo (o tambor com o raio e o comprimento das câmaras)
                    todos = q[np.unique(np.array([tri[k] for k in sel]).reshape(-1))] - np.array(pivos[pt])
                    rx = max(abs(todos[:, 0]).max(), abs(todos[:, 1]).max()); lz = todos[:, 2].max() - todos[:, 2].min(); cz = (todos[:, 2].max() + todos[:, 2].min()) / 2
                    v[:, 0] *= aj['raio'] / rx; v[:, 1] *= aj['raio'] / rx; v[:, 2] = (v[:, 2] - cz) * aj['comprimento'] / lz
            base = sum(len(x) for x in ps)
            ps.append(v); ns.append(nn); u2.append(np.c_[np.full(len(v), pi, dtype=np.float64), np.full(len(v), PAPEIS.index(pa), dtype=np.float64)]); ids.append(iv + base)
    P, N, U2, I = np.concatenate(ps), np.concatenate(ns), np.concatenate(u2), np.concatenate(ids)
    N /= np.maximum(np.linalg.norm(N, axis=1, keepdims=True), 1e-12)
    boca = ((np.array(cfg['boca_modelo']) - A0) @ R.T * s + A1).round(4).tolist()
    extras = {'partes': PARTES, 'papeis': PAPEIS, 'pivos': pivos, 'boca': boca}
    cont = {pa: pap.count(pa) for pa in PAPEIS}; cpt = {pt: parte.count(pt) for pt in PARTES}
    return gravar_glb(P, N, U2, I, extras), extras, len(I) // 3, cont, cpt, (P.min(0).round(3).tolist(), P.max(0).round(3).tolist())


def main():
    F = json.load(open(os.path.join(PASTA, 'fontes.json'), encoding='utf-8'))
    os.makedirs(UNITY, exist_ok=True); saida = {}
    for nome, cfg in F['armas'].items():
        glb, ex, nt, cont, cpt, caixa = montar(nome, cfg)
        open(os.path.join(UNITY, nome + '.bytes'), 'wb').write(glb); saida[nome] = base64.b64encode(glb).decode()
        print(f"{nome:9s} {nt:5d} triângulos  {len(glb) // 1024} KB  boca {ex['boca']}  papéis {cont}  peças {cpt}  caixa {caixa}")
    js = ('// Armas (OpenGameArt, CC0) no quadro das armas do jogo, montadas por tools/montar-armas.py a partir de assets/armas/fontes.json.\n'
          '// GLB sem textura; TEXCOORD_1 = (peça, papel). O jogo pinta com os materiais por papel (as skins valem). O Unity lê Resources/Armas.\n'
          'window.RR_ARMAS=' + json.dumps(saida, separators=(',', ':')) + ';\n')
    open(os.path.join(RAIZ, 'client', 'armas.js'), 'w').write(js)
    print('client/armas.js', len(js) // 1024, 'KB')


if __name__ == '__main__':
    main()
