#!/usr/bin/env python3
# Bonecos de verdade (CC0, Quaternius) no lugar dos bonecos montados por código, sem esqueleto com pele: cada personagem é
# "assado" em peças rígidas presas às mesmas juntas que a animação procedural já mexe (body, torso, head, legL, legR, armL,
# armR). Assim a animação, a arma na mão, as marcas e o porte para o Unity continuam iguais; só muda o que se vê.
#
# assets/bonecos/fontes.json diz de onde vem cada pacote (página do itch.io, arquivo, pasta dos glTF) e como montar cada
# boneco: as peças (glTF + filtro de malhas + "cabeca" para tirar só a cabeça de um corpo inteiro), a recoloração das
# texturas por classe e o tamanho. Os pacotes ficam em assets/bonecos/fontes/ (fora do git); sem eles, o script baixa do
# itch.io (gratuito, CC0).
#
# Para cada boneco:
#   1. lê o glTF com o esqueleto e põe cada vértice na pose de repouso (soma dos ossos pelos pesos, como faz a GPU);
#   2. gira 180° em y (os modelos olham para +z; os bonecos do jogo olham para -z);
#   3. cada vértice vai para a junta do osso de maior peso (coxa/canela/pé -> perna, braço/mão/dedos -> braço, clavícula
#      e coluna -> torso, pescoço/cabeça -> cabeça, bacia -> corpo); o triângulo vai para a junta da maioria;
#   4. os braços saem da pose em T e descem até ficar ao lado do corpo (giro em volta do ombro), como os do jogo;
#   5. cada peça fica no espaço da sua junta (vértice menos o pivô) e é reduzida (pyfqmr);
#   6. as texturas de cor vão para um atlas (recoloridas: o verde do ranger vira o vermelho do culto ou o couro da caça).
# Grava o GLB enxuto de sempre (o mesmo leitor dos modelos) com TEXCOORD_1.x = índice da peça e, em "extras", as peças,
# os pivôs das juntas e a posição da mão direita (onde a arma fica). Saída: client/bonecos.js (RR_BONECOS, embutido por
# build.js e build-mobile.js e servido pelo servidor) e unity/Assets/RitualReversal/Resources/Bonecos/<nome>.bytes.
# Uso: python3 tools/montar-bonecos.py   (Pillow, numpy, pyfqmr)
import base64, colorsys, http.cookiejar, io, json, os, re, struct, sys, urllib.parse, urllib.request, zipfile
import numpy as np
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PASTA = os.path.join(RAIZ, 'assets', 'bonecos')
ORIG = os.path.join(PASTA, 'fontes')
UNITY = os.path.join(RAIZ, 'unity', 'Assets', 'RitualReversal', 'Resources', 'Bonecos')
PARTES = ['body', 'torso', 'head', 'legL', 'legR', 'armL', 'armR']


# ---------- pacotes do itch.io (gratuitos): página -> arquivo .zip ----------
def baixar_itch(pagina, trecho, saida):
    cj = http.cookiejar.CookieJar(); op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj)); op.addheaders = [('User-Agent', 'Mozilla/5.0')]
    h = op.open(pagina).read().decode(); t = re.search(r'csrf_token" value="([^"]*)"', h).group(1)
    u = json.loads(op.open(pagina + '/download_url', urllib.parse.urlencode({'csrf_token': t}).encode()).read())['url']
    h2 = op.open(u).read().decode()
    ups = re.findall(r'data-upload_id="(\d+)".{0,800}?title="([^"]*)"', h2, re.S)
    uid = next(i for i, n in ups if trecho in n)
    r = json.loads(op.open(pagina + f'/file/{uid}?' + urllib.parse.urlencode({'source': 'game_download', 'after_download_lightbox': '1', 'as_props': '1'}),
                           urllib.parse.urlencode({'csrf_token': t}).encode()).read())
    with op.open(r['url']) as resp, open(saida, 'wb') as f:
        while True:
            b = resp.read(1 << 20)
            if not b: break
            f.write(b)


def pacote(F, chave):
    """Pasta com os glTF do pacote (extrai do .zip, baixando se faltar)."""
    p = F['fontes'][chave]; zipf = os.path.join(ORIG, chave + '.zip'); dest = os.path.join(ORIG, chave)
    if not os.path.isdir(dest):
        os.makedirs(ORIG, exist_ok=True)
        if not os.path.exists(zipf):
            print('baixando', p['itch']); baixar_itch(p['itch'], p['arquivo'], zipf)
        Z = zipfile.ZipFile(zipf)
        for n in Z.namelist():
            if n.startswith(p['pasta']) and not n.endswith('/') and '..' not in n and n.lower().endswith(('.gltf', '.bin', '.png', '.jpg', '.txt')):
                alvo = os.path.join(dest, n[len(p['pasta']):]); os.makedirs(os.path.dirname(alvo), exist_ok=True)
                open(alvo, 'wb').write(Z.read(n))
    return dest


# ---------- glTF com esqueleto ----------
def matriz_no(nd):
    if 'matrix' in nd: return np.array(nd['matrix'], dtype=np.float64).reshape(4, 4).T
    t = np.array(nd.get('translation', [0, 0, 0])); r = nd.get('rotation', [0, 0, 0, 1]); s = np.array(nd.get('scale', [1, 1, 1]))
    x, y, z, w = r
    R = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)], [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)], [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
    M = np.eye(4); M[:3, :3] = R * s; M[:3, 3] = t; return M


def ler_gltf(caminho):
    """Malhas do glTF na pose de repouso, já no espaço do mundo: lista de primitivas com p, n, uv, idx, imagem e o nome do
    osso de maior peso de cada vértice; e a posição de cada osso."""
    J = json.load(open(caminho, encoding='utf-8')); pasta = os.path.dirname(caminho)
    bufs = [open(os.path.join(pasta, urllib.parse.unquote(b['uri'])), 'rb').read() for b in J['buffers']]
    TIPO = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8, 5122: np.int16, 5120: np.int8}
    def acc(i):
        a = J['accessors'][i]; v = J['bufferViews'][a['bufferView']]; b = bufs[v.get('buffer', 0)]; o = v.get('byteOffset', 0) + a.get('byteOffset', 0)
        n = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}[a['type']]; dt = TIPO[a['componentType']]; sz = np.dtype(dt).itemsize
        st = v.get('byteStride')
        if st and st != n * sz: arr = np.ndarray((a['count'], n), dtype=dt, buffer=b, offset=o, strides=(st, sz)).copy()
        else: arr = np.frombuffer(b, dtype=dt, count=a['count'] * n, offset=o).reshape(a['count'], n).copy()
        if a.get('normalized') and dt != np.float32: arr = arr.astype(np.float64) / np.iinfo(dt).max
        return arr
    mundo = {}
    def visitar(i, pai):
        M = pai @ matriz_no(J['nodes'][i]); mundo[i] = M
        for c in J['nodes'][i].get('children', []): visitar(c, M)
    for r in J['scenes'][J.get('scene', 0)]['nodes']: visitar(r, np.eye(4))
    ossos = {J['nodes'][i].get('name', str(i)): mundo[i][:3, 3].copy() for i in mundo}
    imagens = {}
    def imagem(mat):
        if mat is None: return None
        bct = J['materials'][mat].get('pbrMetallicRoughness', {}).get('baseColorTexture')
        if bct is None: return None
        uri = urllib.parse.unquote(J['images'][J['textures'][bct['index']]['source']]['uri'])
        if uri not in imagens:
            f = os.path.join(pasta, uri)
            if not os.path.exists(f):   # alguns glTF do pacote citam "<nome>_png.png" que não veio no .zip
                f = os.path.join(pasta, uri.replace('_png.png', '.png'))
            imagens[uri] = f
        return imagens[uri]
    prims = []
    for i, nd in enumerate(J['nodes']):
        if 'mesh' not in nd: continue
        malha = J['meshes'][nd['mesh']]; sk = J['skins'][nd['skin']] if 'skin' in nd else None
        if sk:
            ibm = acc(sk['inverseBindMatrices']).reshape(-1, 4, 4).transpose(0, 2, 1)
            JM = np.array([mundo[j] @ ibm[k] for k, j in enumerate(sk['joints'])]); nomes = [J['nodes'][j].get('name', '') for j in sk['joints']]
        for pr in malha['primitives']:
            at = pr['attributes']; p = acc(at['POSITION']).astype(np.float64); n = acc(at['NORMAL']).astype(np.float64)
            uv = acc(at['TEXCOORD_0']).astype(np.float64) if 'TEXCOORD_0' in at else np.zeros((len(p), 2))
            idx = acc(pr['indices']).reshape(-1).astype(np.int64)
            if sk:
                jt = acc(at['JOINTS_0']).astype(np.int64); w = acc(at['WEIGHTS_0']).astype(np.float64); w /= np.maximum(w.sum(1, keepdims=True), 1e-9)
                M = np.einsum('vk,vkij->vij', w, JM[jt])
                p = np.einsum('vij,vj->vi', M[:, :3, :3], p) + M[:, :3, 3]; n = np.einsum('vij,vj->vi', M[:, :3, :3], n)
                osso = [nomes[jt[v, np.argmax(w[v])]] for v in range(len(p))]
            else:
                M = mundo[i]; p = p @ M[:3, :3].T + M[:3, 3]; n = n @ M[:3, :3].T; osso = [J['nodes'][i].get('name', '')] * len(p)
            n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)
            prims.append({'malha': malha.get('name', ''), 'p': p, 'n': n, 'uv': uv, 'idx': idx, 'img': imagem(pr.get('material')), 'osso': osso})
    return prims, ossos


def junta_do_osso(nome):
    n = nome.lower(); lado = 'L' if n.endswith('_l') else 'R' if n.endswith('_r') else None
    if lado and any(k in n for k in ('thigh', 'calf', 'foot', 'ball')): return 'leg' + lado
    if 'clavicle' in n or 'spine' in n: return 'torso'
    if lado: return 'arm' + lado
    if 'neck' in n or 'head' in n: return 'head'
    return 'body'


def girar(d, t):
    """Matriz que leva a direção d até t (Rodrigues)."""
    d = d / np.linalg.norm(d); t = t / np.linalg.norm(t); v = np.cross(d, t); c = float(np.dot(d, t)); s = np.linalg.norm(v)
    if s < 1e-9: return np.eye(3)
    K = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]]); return np.eye(3) + K + K @ K * ((1 - c) / s ** 2)


# ---------- texturas ----------
def recolorir(im, regras):
    """regras: {"verde": [matiz, sat×, val×]} troca a matiz dos pixels verdes; {"mult": [r, g, b]} multiplica."""
    a = np.asarray(im.convert('RGB'), dtype=np.float64) / 255
    if 'verde' in regras:
        h, s, v = np.vectorize(colorsys.rgb_to_hsv)(a[..., 0], a[..., 1], a[..., 2])
        m = (h > 60 / 360) & (h < 175 / 360) & (s > .12)
        hm, sm, vm = regras['verde']; h = np.where(m, hm / 360, h); s = np.where(m, np.clip(s * sm, 0, 1), s); v = np.where(m, np.clip(v * vm, 0, 1), v)
        r, g, b = np.vectorize(colorsys.hsv_to_rgb)(h, s, v); a = np.stack([r, g, b], -1)
    if 'mult' in regras: a = np.clip(a * np.array(regras['mult']), 0, 1)
    return Image.fromarray((a * 255).round().astype(np.uint8))


def pot(n): return 1 << max(0, (n - 1).bit_length())


def gravar_glb(p, n, uv, uv2, idx, jpeg, extras):
    pos = p.astype(np.float32).tobytes(); nor = n.astype(np.float32).tobytes(); tex = uv.astype(np.float32).tobytes(); tex2 = uv2.astype(np.float32).tobytes()
    grande = len(p) > 65535; ind = idx.astype(np.uint32 if grande else np.uint16).tobytes()
    def alinhar(b): return b + b'\0' * ((4 - len(b) % 4) % 4)
    partes, vistas, ofs = [], [], 0
    for b, alvo in ((pos, 34962), (nor, 34962), (tex, 34962), (tex2, 34962), (ind, 34963), (jpeg, None)):
        v = {'buffer': 0, 'byteOffset': ofs, 'byteLength': len(b)}
        if alvo: v['target'] = alvo
        vistas.append(v); b = alinhar(b); partes.append(b); ofs += len(b)
    BIN = b''.join(partes); mn = p.min(0).tolist(); mx = p.max(0).tolist()
    J = {'asset': {'version': '2.0', 'generator': 'Ritual Reversal montar-bonecos.py'}, 'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'mesh': 0}],
         'meshes': [{'primitives': [{'attributes': {'POSITION': 0, 'NORMAL': 1, 'TEXCOORD_0': 2, 'TEXCOORD_1': 3}, 'indices': 4, 'material': 0}], 'extras': extras}],
         'materials': [{'pbrMetallicRoughness': {'baseColorTexture': {'index': 0}, 'metallicFactor': 0, 'roughnessFactor': .9}}],
         'textures': [{'source': 0}], 'images': [{'bufferView': 5, 'mimeType': 'image/jpeg'}],
         'accessors': [{'bufferView': 0, 'componentType': 5126, 'count': len(p), 'type': 'VEC3', 'min': mn, 'max': mx},
                       {'bufferView': 1, 'componentType': 5126, 'count': len(p), 'type': 'VEC3'},
                       {'bufferView': 2, 'componentType': 5126, 'count': len(p), 'type': 'VEC2'},
                       {'bufferView': 3, 'componentType': 5126, 'count': len(p), 'type': 'VEC2'},
                       {'bufferView': 4, 'componentType': 5125 if grande else 5123, 'count': len(idx), 'type': 'SCALAR'}],
         'bufferViews': vistas, 'buffers': [{'byteLength': len(BIN)}]}
    js = json.dumps(J, separators=(',', ':')).encode(); js += b' ' * ((4 - len(js) % 4) % 4)
    return struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(BIN)) + struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(BIN), 0x004E4942) + BIN


def reduzir(p, n, uv, idx, alvo):
    # sem redução por padrão ("triangulos": 0): o uv do vértice mais perto erra nas costuras da textura e mancha a roupa
    if not alvo or len(idx) // 3 <= alvo: return p, n, uv, idx
    import pyfqmr
    s = pyfqmr.Simplify(); s.setMesh(p.astype(np.float64), idx.reshape(-1, 3).astype(np.int32))
    s.simplify_mesh(target_count=alvo, aggressiveness=7, preserve_border=True, verbose=False)
    q, f, _ = s.getMesh(); perto = np.empty(len(q), dtype=np.int64)
    for i in range(0, len(q), 512):
        d = ((q[i:i + 512, None, :] - p[None, :, :]) ** 2).sum(-1); perto[i:i + 512] = d.argmin(1)
    return q, n[perto], uv[perto], f.reshape(-1).astype(np.int64)


def montar(F, nome, cfg):
    prims, ossos = [], {}
    for chave, arq, filtro in cfg['pecas']:
        pp, oo = ler_gltf(os.path.join(pacote(F, chave), arq))
        for pr in pp:
            if isinstance(filtro, list) and any(f.startswith('!') and pr['malha'].startswith(f[1:]) for f in filtro): continue
            if filtro == 'cabeca':   # de um corpo inteiro, só a cabeça e o pescoço
                pr['so'] = {'head'}
            prims.append(pr)
        for k, v in oo.items(): ossos.setdefault(k, v)
    G = np.diag([-1., 1, -1])   # 180° em y: o boneco passa a olhar para -z
    def O(k): return ossos[k] @ G.T
    # o modelo olha para +z com a esquerda ("_l") em +x; girado, olha para -z com a esquerda em -x, como os bonecos do jogo
    piv = {'body': np.zeros(3), 'legL': O('thigh_l'), 'legR': O('thigh_r'), 'torso': O('spine_01'), 'head': O('neck_01'), 'armL': O('upperarm_l'), 'armR': O('upperarm_r')}
    troca = {}
    maos = {'armL': O('hand_l'), 'armR': O('hand_r')}
    # braços da pose em T para ao lado do corpo: do ombro até a mão vira (±0,12, -1, 0)
    rot = {}
    for a in ('armL', 'armR'):
        d = maos[a] - piv[a]; rot[a] = girar(d, np.array([.1 * np.sign(d[0]), -1, 0]))
    imgs = []
    P = {k: [] for k in PARTES}
    for pr in prims:
        p = pr['p'] @ G.T; n = pr['n'] @ G.T
        vj = np.array([troca.get(junta_do_osso(o), junta_do_osso(o)) for o in pr['osso']])
        if pr['img'] not in imgs: imgs.append(pr['img'])
        ti = imgs.index(pr['img'])
        tri = pr['idx'].reshape(-1, 3)
        for k in PARTES:
            if 'so' in pr and k not in pr['so']: continue
            vt = vj[tri]; maioria = np.where((vt[:, 1] == vt[:, 2]) & (vt[:, 0] != vt[:, 1]), vt[:, 1], vt[:, 0])
            sel = tri[maioria == k]
            if not len(sel): continue
            usados, inv = np.unique(sel.reshape(-1), return_inverse=True)
            q = p[usados].copy(); m = n[usados].copy()
            if k in rot: q = (q - piv[k]) @ rot[k].T + piv[k]; m = m @ rot[k].T
            P[k].append((q - piv[k], m, pr['uv'][usados], inv.astype(np.int64), ti))
    # atlas das texturas (com a recoloração de cada uma)
    tam = 1024 if len(imgs) <= 2 else 512; col = pot(int(np.ceil(np.sqrt(len(imgs))))); lin = pot(int(np.ceil(len(imgs) / col)))
    atlas = Image.new('RGB', (tam * col, tam * lin), (128, 128, 128))
    for k, f in enumerate(imgs):
        if f is None: continue
        im = Image.open(f).convert('RGB').resize((tam, tam), Image.LANCZOS)
        regra = cfg.get('cores', {}).get(os.path.basename(f))
        if regra: im = recolorir(im, regra)
        atlas.paste(im, ((k % col) * tam, (k // col) * tam))
    altura = max(float((q[:, 1] + piv[k][1]).max()) for k in PARTES for q, *_ in P[k])
    ps, ns, us, u2, ids, base = [], [], [], [], [], 0
    total = sum(len(i) for v in P.values() for *_, i, _t in v) // 3; alvo = cfg.get('triangulos', 0)
    for pi, k in enumerate(PARTES):
        if not P[k]: continue
        # junta as primitivas da peça (cada uma com o uv já no seu quadrado do atlas) e reduz a peça inteira
        qq, mm, uu, ii, b0 = [], [], [], [], 0
        for q, m, uv, inv, ti in P[k]:
            uvc = np.clip(uv, 0, 1); uva = np.c_[((ti % col) + uvc[:, 0]) / col, ((ti // col) + uvc[:, 1]) / lin]
            qq.append(q); mm.append(m); uu.append(uva); ii.append(inv + b0); b0 += len(q)
        q, m, uv, ix = np.concatenate(qq), np.concatenate(mm), np.concatenate(uu), np.concatenate(ii)
        q, m, uv, ix = reduzir(q, m, uv, ix, max(200, int(alvo * (len(ix) // 3) / max(1, total))) if alvo else 0)
        ps.append(q); ns.append(m); us.append(uv); u2.append(np.c_[np.full(len(q), pi, dtype=np.float64), np.zeros(len(q))]); ids.append(ix + base); base += len(q)
    p, n, uv, uv2, idx = np.concatenate(ps), np.concatenate(ns), np.concatenate(us), np.concatenate(u2), np.concatenate(ids)
    esc = cfg.get('escala', 1.0)
    rel = lambda a, b: ((piv[a] - piv[b]) * esc).round(4).tolist()
    extras = {'partes': PARTES, 'escala': esc,
              'pivos': {'legL': rel('legL', 'body'), 'legR': rel('legR', 'body'), 'torso': rel('torso', 'body'), 'head': rel('head', 'torso'), 'armL': rel('armL', 'torso'), 'armR': rel('armR', 'torso')},
              'mao': (((maos['armR'] - piv['armR']) @ rot['armR'].T) * esc).round(4).tolist(),
              'altura': round(altura * esc, 3)}
    b = io.BytesIO(); atlas.save(b, 'JPEG', quality=86, optimize=True)
    return gravar_glb(p * esc, n, uv, uv2, idx, b.getvalue(), extras), extras, len(idx) // 3, atlas.size


def main():
    F = json.load(open(os.path.join(PASTA, 'fontes.json'), encoding='utf-8'))
    os.makedirs(UNITY, exist_ok=True); saida = {}
    for nome, cfg in F['bonecos'].items():
        glb, ex, nt, at = montar(F, nome, cfg)
        open(os.path.join(UNITY, nome + '.bytes'), 'wb').write(glb); saida[nome] = base64.b64encode(glb).decode()
        print(f"{nome:10s} {nt:6d} triângulos  atlas {at[0]}×{at[1]}  GLB {len(glb) // 1024} KB  mão {ex['mao']}  pivôs {ex['pivos']}")
    js = ('// Bonecos (Quaternius, CC0) em peças rígidas por junta, montados por tools/montar-bonecos.py a partir de assets/bonecos/fontes.json.\n'
          '// GLB em base64; TEXCOORD_1.x = peça (body, torso, head, legL, legR, armL, armR), pivôs e mão em extras. O Unity lê Resources/Bonecos.\n'
          'window.RR_BONECOS=' + json.dumps(saida, separators=(',', ':')) + ';\n')
    open(os.path.join(RAIZ, 'client', 'bonecos.js'), 'w').write(js)
    print('client/bonecos.js', len(js) // 1024, 'KB')


if __name__ == '__main__':
    main()
