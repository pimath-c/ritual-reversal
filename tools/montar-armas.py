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
import base64, colorsys, io, json, os, re, struct, subprocess, sys, urllib.request, zipfile, importlib.util
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


def baixar_sketchfab(uid, destino):
    """O GLB de um modelo do Sketchfab (CC BY), pela API de download. Precisa do token da conta em SKETCHFAB_TOKEN (variável de
    ambiente; nunca no repositório). Só baixa se o arquivo ainda não estiver em assets/armas/fontes/."""
    if os.path.exists(destino): return destino
    tok = os.environ.get('SKETCHFAB_TOKEN')
    if not tok: sys.exit(f'falta {os.path.basename(destino)}: ponha o token do Sketchfab em SKETCHFAB_TOKEN para baixar o modelo {uid}')
    req = urllib.request.Request(f'https://api.sketchfab.com/v3/models/{uid}/download', headers={'Authorization': 'Token ' + tok})
    d = json.load(urllib.request.urlopen(req, timeout=60)); os.makedirs(os.path.dirname(destino), exist_ok=True)
    with urllib.request.urlopen(d['glb']['url'], timeout=600) as r, open(destino + '.parcial', 'wb') as f: f.write(r.read())
    os.replace(destino + '.parcial', destino); return destino


def fatores_mr(im, pb):
    """Os fatores do glTF entram na textura de metal/aspereza (G aspereza × roughnessFactor, B metal × metallicFactor), que o jogo
    lê com aspereza e metal 1."""
    r, g, b = im.split(); fr, fm = pb.get('roughnessFactor', 1.0), pb.get('metallicFactor', 1.0)
    if fr != 1: g = g.point(lambda x: round(x * fr))
    if fm != 1: b = b.point(lambda x: round(x * fm))
    return Image.merge('RGB', (r, g, b))


def ler_texturado(glb, celula, usar=None):
    """Todas as primitivas do GLB no espaço do mundo, com o uv original, o nome do nó de cada triângulo e as três texturas de cada
    material (cor, relevo, metal/aspereza do glTF: G aspereza, B metal) juntas em atlas paralelos (um quadrado de celula px por
    material, em grade); o uv de cada primitiva vai para o quadrado do seu material."""
    J, BIN = MM.ler_glb(open(glb, 'rb').read())
    P, N, U, I, NO, MAT = [], [], [], [], [], []
    def visitar(i, pai):
        nd = J['nodes'][i]; M = pai @ MM.matriz_no(nd)
        if 'mesh' in nd:
            for pr in J['meshes'][nd['mesh']]['primitives']:
                if pr.get('mode', 4) != 4: continue
                at = pr['attributes']; p = MM.acessor(J, BIN, at['POSITION'])
                idx = MM.acessor(J, BIN, pr['indices']).reshape(-1) if 'indices' in pr else np.arange(len(p))
                uv = MM.acessor(J, BIN, at['TEXCOORD_0']) if 'TEXCOORD_0' in at else np.zeros((len(p), 2))
                pw = (np.c_[p, np.ones(len(p))] @ M.T)[:, :3]
                n = MM.acessor(J, BIN, at['NORMAL']) @ np.linalg.inv(M[:3, :3]); n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)
                base = sum(len(q) for q in P); P.append(pw); N.append(n); U.append(uv); I.append(idx + base)
                nt = len(idx) // 3; NO.extend([nd.get('name', '') + '#' + str(nd['mesh'])] * nt); MAT.append(np.full(len(p), pr.get('material', 0)))
        for c in nd.get('children', []): visitar(c, M)
    for r in J['scenes'][J.get('scene', 0)]['nodes']: visitar(r, np.eye(4))
    def imagem(tex, padrao):
        if tex is None: return Image.new('RGB', (8, 8), padrao)
        im = J['images'][J['textures'][tex['index']]['source']]; v = J['bufferViews'][im['bufferView']]; o = v.get('byteOffset', 0)
        return Image.open(io.BytesIO(BIN[o:o + v['byteLength']])).convert('RGB')
    usar = list(range(len(J['materials']))) if usar is None else usar
    mats = [J['materials'][k] for k in usar]; col = int(np.ceil(np.sqrt(len(mats)))); lin = int(np.ceil(len(mats) / col))
    atlas = {k: Image.new('RGB', (celula * col, celula * lin), c) for k, c in (('cor', (128, 128, 128)), ('nor', (128, 128, 255)), ('mr', (0, 200, 0)))}
    for k, m in enumerate(mats):
        pb = m.get('pbrMetallicRoughness', {}); xy = ((k % col) * celula, (k // col) * celula)
        for nome, tex, pad in (('cor', pb.get('baseColorTexture'), (128, 128, 128)), ('nor', m.get('normalTexture'), (128, 128, 255)), ('mr', pb.get('metallicRoughnessTexture'), (0, 255, 0))):
            im = imagem(tex, pad).resize((celula, celula), Image.LANCZOS)
            if nome == 'mr': im = fatores_mr(im, pb)
            atlas[nome].paste(im, xy)
    mt = np.array([usar.index(k) if k in usar else 0 for k in np.concatenate(MAT)]); uv = np.concatenate(U)
    uv = np.c_[((mt % col) + uv[:, 0] % 1) / col, ((mt // col) + uv[:, 1] % 1) / lin]   # os uvs dos modelos ficam entre 0 e 1 (repetição não é usada)
    return np.concatenate(P), np.concatenate(N), uv, np.concatenate(I).astype(np.int64), NO, atlas


def dentro(c, caixa):
    return all(caixa[k][0] <= c[k] <= caixa[k][1] for k in range(3))


def no_cilindro(c, cil):
    return cil['x'][0] <= c[0] <= cil['x'][1] and (c[1] - cil['yz'][0]) ** 2 + (c[2] - cil['yz'][1]) ** 2 <= cil['raio'] ** 2


def regra(c, r, no=''):
    """Uma regra vale se todas as condições dela valem: o nome do nó ("nos", expressão regular), a caixa e o cilindro."""
    if 'nos' in r and not re.search(r['nos'], no): return False
    if 'caixa' in r and not dentro(c, r['caixa']): return False
    if 'cilindro' in r and not no_cilindro(c, r['cilindro']): return False
    return True


def gravar_glb(p, n, uv2, idx, extras, uv0=None):
    pos = p.astype(np.float32).tobytes(); nor = n.astype(np.float32).tobytes(); t2 = uv2.astype(np.float32).tobytes()
    grande = len(p) > 65535; ind = idx.astype(np.uint32 if grande else np.uint16).tobytes()
    def alinhar(b): return b + b'\0' * ((4 - len(b) % 4) % 4)
    partes, vistas, ofs = [], [], 0
    blocos = [(pos, 34962), (nor, 34962), (t2, 34962), (ind, 34963)] + ([(uv0.astype(np.float32).tobytes(), 34962)] if uv0 is not None else [])
    for b, alvo in blocos:
        vistas.append({'buffer': 0, 'byteOffset': ofs, 'byteLength': len(b), 'target': alvo}); b = alinhar(b); partes.append(b); ofs += len(b)
    BIN = b''.join(partes)
    J = {'asset': {'version': '2.0', 'generator': 'Ritual Reversal montar-armas.py'}, 'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'mesh': 0}],
         'meshes': [{'primitives': [{'attributes': {'POSITION': 0, 'NORMAL': 1, 'TEXCOORD_1': 2}, 'indices': 3}], 'extras': extras}],
         'accessors': [{'bufferView': 0, 'componentType': 5126, 'count': len(p), 'type': 'VEC3', 'min': p.min(0).tolist(), 'max': p.max(0).tolist()},
                       {'bufferView': 1, 'componentType': 5126, 'count': len(p), 'type': 'VEC3'},
                       {'bufferView': 2, 'componentType': 5126, 'count': len(p), 'type': 'VEC2'},
                       {'bufferView': 3, 'componentType': 5125 if grande else 5123, 'count': len(idx), 'type': 'SCALAR'}],
         'bufferViews': vistas, 'buffers': [{'byteLength': len(BIN)}]}
    if uv0 is not None:   # o uv original do modelo (no atlas das texturas de verdade)
        J['accessors'].append({'bufferView': 4, 'componentType': 5126, 'count': len(p), 'type': 'VEC2'}); J['meshes'][0]['primitives'][0]['attributes']['TEXCOORD_0'] = 4
    js = json.dumps(J, separators=(',', ':')).encode(); js += b' ' * ((4 - len(js) % 4) % 4)
    return struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(BIN)) + struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(BIN), 0x004E4942) + BIN


def montar(nome, cfg):
    atlas = None
    if 'sketchfab' in cfg:   # modelo do Sketchfab: o uv original e as três texturas (cor, relevo, metal/aspereza) vão junto
        sf = cfg['sketchfab']; p, n, uv, idx, NO, atlas = ler_texturado(baixar_sketchfab(sf['uid'], os.path.join(ORIG, nome + '_sf.glb')), sf.get('celula', 1024), sf.get('materiais')); img = atlas['cor']
    else:
        J, BIN = MM.ler_glb(open(fonte_glb(nome, cfg), 'rb').read()); p, n, uv, idx, img = MM.juntar(J, BIN); NO = [''] * (len(idx) // 3)
    px = img.convert('RGB').load(); iw, ih = img.size
    tri = idx.reshape(-1, 3); cent = p[tri].mean(1); uvc = uv[tri].mean(1); NO = np.array(NO)
    manter = np.array([not any(regra(c, k, no) for k in cfg.get('cortar', [])) for c, no in zip(cent, NO)]); tri, cent, uvc, NO = tri[manter], cent[manter], uvc[manter], NO[manter]
    # ilhas pequenas (argolas, fivelas e pontas de tira da bandoleira presas à cartucheira): saem as ilhas de um nó com menos de
    # `min` triângulos (vértices na mesma posição contam como um só)
    for ci in cfg.get('cortar_ilhas', []):
        sel = [k for k in range(len(tri)) if re.search(ci['nos'], NO[k])]; pai = {}
        def raiz(x):
            while pai.setdefault(x, x) != x: pai[x] = pai[pai[x]]; x = pai[x]
            return x
        chave = lambda v: tuple(np.round(p[v], 5))
        for k in sel:
            a0 = raiz(chave(tri[k][0]))
            for v in tri[k][1:]: pai[raiz(chave(v))] = a0
        grupos = {}
        for k in sel: grupos.setdefault(raiz(chave(tri[k][0])), []).append(k)
        fora = {k for g in grupos.values() if len(g) < ci['min'] for k in g}
        manter = np.array([k not in fora for k in range(len(tri))]); tri, cent, uvc, NO = tri[manter], cent[manter], uvc[manter], NO[manter]
    PARTES = ['estatico'] + [r['parte'] for r in cfg.get('partes', [])]; PAPEIS = cfg['papeis_lista']
    def papel(c, u, no):
        for rg in cfg.get('papeis', []):   # as regras por nome de nó valem antes da cor
            if 'nos' in rg and regra(c, rg, no): return rg['papel']
        r, g, b = (v / 255 for v in px[int(u[0] % 1 * (iw - 1)), int(u[1] % 1 * (ih - 1))]); h, s, v = colorsys.rgb_to_hsv(r, g, b)
        if 'madeira' in PAPEIS and (h < 45 / 360 or h > 340 / 360) and s > .28 and v > .08: return 'madeira'
        if 'escuro' in PAPEIS and cfg.get('escuro_val') and v < cfg['escuro_val']: return 'escuro'
        for rg in cfg.get('papeis', []):
            if 'nos' not in rg and regra(c, rg, no): return rg['papel']
        return cfg['papel_padrao']
    parte = [next((r['parte'] for r in cfg.get('partes', []) if regra(c, r, no)), 'estatico') for c, no in zip(cent, NO)]
    pap = [papel(c, u, no) for c, u, no in zip(cent, uvc, NO)]
    # do modelo para o quadro da arma do jogo: gira em y, escala e leva a âncora ao lugar
    a = np.radians(cfg['giro']); R = np.array([[np.cos(a), 0, np.sin(a)], [0, 1, 0], [-np.sin(a), 0, np.cos(a)]])
    s = cfg['escala']; A0 = np.array(cfg['ancora_modelo'], dtype=np.float64); A1 = np.array(cfg['ancora_jogo'], dtype=np.float64)
    q = (p - A0) @ R.T * s + A1; m = n @ R.T
    pivos = {r['parte']: r['pivo'] for r in cfg.get('partes', [])}
    ps, ns, u2, ids, u0 = [], [], [], [], []
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
            u0.append(uv[usados]); ps.append(v); ns.append(nn); u2.append(np.c_[np.full(len(v), pi, dtype=np.float64), np.full(len(v), PAPEIS.index(pa), dtype=np.float64)]); ids.append(iv + base)
    P, N, U2, I = np.concatenate(ps), np.concatenate(ns), np.concatenate(u2), np.concatenate(ids)
    N /= np.maximum(np.linalg.norm(N, axis=1, keepdims=True), 1e-12)
    boca = ((np.array(cfg['boca_modelo']) - A0) @ R.T * s + A1).round(4).tolist()
    extras = {'partes': PARTES, 'papeis': PAPEIS, 'pivos': pivos, 'boca': boca}
    if atlas: extras['foto'] = True   # tem TEXCOORD_0 e as texturas <nome>_cor/_nor/_mr
    cont = {pa: pap.count(pa) for pa in PAPEIS}; cpt = {pt: parte.count(pt) for pt in PARTES}
    glb = gravar_glb(P, N, U2, I, extras, np.concatenate(u0) if atlas else None)
    return glb, extras, len(I) // 3, cont, cpt, (P.min(0).round(3).tolist(), P.max(0).round(3).tolist()), atlas


def main():
    F = json.load(open(os.path.join(PASTA, 'fontes.json'), encoding='utf-8'))
    os.makedirs(UNITY, exist_ok=True); saida = {}; texs = {}
    for nome, cfg in F['armas'].items():
        glb, ex, nt, cont, cpt, caixa, atlas = montar(nome, cfg)
        open(os.path.join(UNITY, nome + '.bytes'), 'wb').write(glb); saida[nome] = base64.b64encode(glb).decode()
        for k in ('cor', 'nor', 'mr'):   # JPG: a cor em sRGB; o relevo e o metal/aspereza lineares (o jogo diz qual é qual)
            arq = os.path.join(UNITY, f'{nome}_{k}.bytes')
            if atlas:
                b = io.BytesIO(); atlas[k].save(b, 'JPEG', quality=90 if k == 'cor' else 92, subsampling=0 if k != 'cor' else 2); open(arq, 'wb').write(b.getvalue())
                texs.setdefault(nome, {})[k] = base64.b64encode(b.getvalue()).decode()
            elif os.path.exists(arq): os.remove(arq)
        print(f"{nome:9s} {nt:5d} triângulos  {len(glb) // 1024} KB  boca {ex['boca']}  papéis {cont}  peças {cpt}  caixa {caixa}")
    js = ('// Armas (Sketchfab, CC BY; créditos em docs/ARTE.md) no quadro das armas do jogo, montadas por tools/montar-armas.py a partir de\n'
          '// assets/armas/fontes.json. GLB com TEXCOORD_1 = (peça, papel) e o uv do modelo; RR_ARMAS_TEX = as texturas (JPG: cor, relevo,\n'
          '// metal/aspereza do glTF). A skin de serviço usa as fotos; as outras pintam por papel. O Unity lê Resources/Armas.\n'
          'window.RR_ARMAS=' + json.dumps(saida, separators=(',', ':')) + ';\n'
          'window.RR_ARMAS_TEX=' + json.dumps(texs, separators=(',', ':')) + ';\n')
    open(os.path.join(RAIZ, 'client', 'armas.js'), 'w').write(js)
    print('client/armas.js', len(js) // 1024, 'KB')


if __name__ == '__main__':
    main()
