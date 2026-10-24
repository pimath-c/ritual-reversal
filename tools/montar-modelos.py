#!/usr/bin/env python3
# Modelos 3D gerados (assets/modelos/fontes.json, Higgsfield/Tripo): baixa o GLB original em assets/modelos/fontes/<nome>.glb
# (se ainda não estiver lá), junta as malhas com as transformações dos nós, põe no tamanho do jogo em metros (encaixe
# "caixa": cada eixo no tamanho pedido, o eixo comprido em x; "altura": escala uniforme pela altura, centrado em x/z) com o
# pé em y = 0, reduz a textura de cor a um JPEG quadrado e grava um GLB enxuto (uma malha: posição, normal, uv, índices
# e a textura). Saídas: client/modelos.js (window.RR_MODELOS = {nome: base64}, embutido por build.js e build-mobile.js e
# servido pelo servidor) e unity/Assets/RitualReversal/Resources/Modelos/<nome>.bytes (o mesmo GLB, lido pelo
# Visual/ModeloGLB.cs). Os dois clientes leem o mesmo arquivo com o mesmo leitor mínimo, então ficam iguais.
# Uso: python3 tools/montar-modelos.py            (precisa de numpy e Pillow; baixar precisa do domínio liberado na rede)
#      FONTES_GLB=<pasta> SAIDA_MODELOS=<pasta> ...  (testes: lê os originais de outra pasta e grava tudo em outra pasta)
import base64, io, json, math, os, struct, sys, urllib.request
import numpy as np
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONTES = os.path.join(RAIZ, 'assets', 'modelos')


def ler_glb(b):
    if b[:4] != b'glTF':
        raise ValueError('não é GLB')
    n, pos, J, BIN = len(b), 12, None, b''
    while pos < n:
        cl, ct = struct.unpack('<II', b[pos:pos + 8]); dado = b[pos + 8:pos + 8 + cl]; pos += 8 + cl
        if ct == 0x4E4F534A: J = json.loads(dado)
        elif ct == 0x004E4942: BIN = dado
    req = set(J.get('extensionsRequired', []))
    if req:
        raise ValueError('extensões obrigatórias não suportadas: ' + ', '.join(sorted(req)))
    return J, BIN


def acessor(J, BIN, i):
    a = J['accessors'][i]; v = J['bufferViews'][a['bufferView']]
    tipo = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8, 5122: np.int16, 5120: np.int8}[a['componentType']]
    k = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4}[a['type']]; it = np.dtype(tipo).itemsize
    passo = v.get('byteStride') or k * it; ini = v.get('byteOffset', 0) + a.get('byteOffset', 0)
    bruto = np.frombuffer(BIN, np.uint8, count=passo * (a['count'] - 1) + k * it, offset=ini)
    linhas = np.lib.stride_tricks.as_strided(bruto, shape=(a['count'], k * it), strides=(passo, 1))
    out = np.ascontiguousarray(linhas).view(tipo).reshape(a['count'], k).astype(np.float64 if tipo == np.float32 else np.int64)
    if a.get('normalized') and tipo != np.float32:
        out = out / float(np.iinfo(tipo).max)
    return out


def matriz_no(nd):
    if 'matrix' in nd:
        return np.array(nd['matrix'], dtype=np.float64).reshape(4, 4).T
    t = nd.get('translation', [0, 0, 0]); r = nd.get('rotation', [0, 0, 0, 1]); s = nd.get('scale', [1, 1, 1])
    x, y, z, w = r
    R = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                  [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                  [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
    M = np.eye(4); M[:3, :3] = R * np.array(s); M[:3, 3] = t
    return M


def juntar(J, BIN):
    """Todas as primitivas de triângulos da cena, já no espaço do mundo; devolve posições, normais, uvs, índices e a imagem."""
    P, N, U, I, img = [], [], [], [], None
    def visitar(i, pai):
        nonlocal img
        nd = J['nodes'][i]; M = pai @ matriz_no(nd)
        if 'mesh' in nd:
            for pr in J['meshes'][nd['mesh']]['primitives']:
                if pr.get('mode', 4) != 4: continue
                at = pr['attributes']; p = acessor(J, BIN, at['POSITION'])
                idx = acessor(J, BIN, pr['indices']).reshape(-1) if 'indices' in pr else np.arange(len(p))
                uv = acessor(J, BIN, at['TEXCOORD_0']) if 'TEXCOORD_0' in at else np.zeros((len(p), 2))
                n = acessor(J, BIN, at['NORMAL']) if 'NORMAL' in at else None
                pw = (np.c_[p, np.ones(len(p))] @ M.T)[:, :3]
                if n is not None:
                    nw = n @ np.linalg.inv(M[:3, :3]); nw /= np.maximum(np.linalg.norm(nw, axis=1, keepdims=True), 1e-12)
                else:
                    nw = None
                base = sum(len(q) for q in P); P.append(pw); N.append(nw); U.append(uv); I.append(idx + base)
                if img is None and 'material' in pr:
                    m = J['materials'][pr['material']]; bct = m.get('pbrMetallicRoughness', {}).get('baseColorTexture')
                    if bct is not None:
                        im = J['images'][J['textures'][bct['index']]['source']]
                        if 'bufferView' in im:
                            v = J['bufferViews'][im['bufferView']]; o = v.get('byteOffset', 0)
                            img = Image.open(io.BytesIO(BIN[o:o + v['byteLength']])).convert('RGB')
        for c in nd.get('children', []): visitar(c, M)
    cena = J['scenes'][J.get('scene', 0)]
    for r in cena['nodes']: visitar(r, np.eye(4))
    p = np.concatenate(P); uv = np.concatenate(U); idx = np.concatenate(I).astype(np.int64)
    if any(n is None for n in N):
        n = normais(p, idx)
    else:
        n = np.concatenate(N)
    return p, n, uv, idx, img


def normais(p, idx):
    t = idx.reshape(-1, 3); fn = np.cross(p[t[:, 1]] - p[t[:, 0]], p[t[:, 2]] - p[t[:, 0]]); n = np.zeros_like(p)
    for k in range(3): np.add.at(n, t[:, k], fn)
    return n / np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)


def encaixar(p, n, cfg):
    """No tamanho do jogo: o pé em y = 0 e o meio em x/z = 0; normais pela inversa transposta da escala."""
    mn, mx = p.min(0), p.max(0); ext = mx - mn
    if cfg['encaixe'] == 'caixa' and ext[2] > ext[0]:
        # o eixo comprido vai para x (girando 90° em volta de y; a frente, que era +x, fica em +z)
        p = np.c_[-p[:, 2], p[:, 1], p[:, 0]]; n = np.c_[-n[:, 2], n[:, 1], n[:, 0]]; mn, mx = p.min(0), p.max(0); ext = mx - mn
    c = (mn + mx) / 2; p = p - np.array([c[0], mn[1], c[2]])
    if cfg['encaixe'] == 'caixa':
        e = np.array(cfg['tamanho'], dtype=np.float64) / np.maximum(ext, 1e-9)
    else:
        e = np.full(3, cfg['tamanho'][1] / max(ext[1], 1e-9))
    p = p * e; n = n / e; n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)
    return p, n


def gravar_glb(p, n, uv, idx, jpeg):
    pos = p.astype(np.float32).tobytes(); nor = n.astype(np.float32).tobytes(); tex = uv.astype(np.float32).tobytes()
    grande = len(p) > 65535; ind = idx.astype(np.uint32 if grande else np.uint16).tobytes()
    def alinhar(b): return b + b'\0' * ((4 - len(b) % 4) % 4)
    partes, vistas, ofs = [], [], 0
    for b, alvo in ((pos, 34962), (nor, 34962), (tex, 34962), (ind, 34963), (jpeg, None)):
        v = {'buffer': 0, 'byteOffset': ofs, 'byteLength': len(b)}
        if alvo: v['target'] = alvo
        vistas.append(v); b = alinhar(b); partes.append(b); ofs += len(b)
    BIN = b''.join(partes)
    J = {'asset': {'version': '2.0', 'generator': 'Ritual Reversal tools/montar-modelos.py'},
         'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'mesh': 0}],
         'meshes': [{'primitives': [{'attributes': {'POSITION': 0, 'NORMAL': 1, 'TEXCOORD_0': 2}, 'indices': 3, 'material': 0}]}],
         'materials': [{'pbrMetallicRoughness': {'baseColorTexture': {'index': 0}, 'metallicFactor': 0, 'roughnessFactor': 1}}],
         'textures': [{'source': 0, 'sampler': 0}], 'samplers': [{'magFilter': 9729, 'minFilter': 9987}],
         'images': [{'bufferView': 4, 'mimeType': 'image/jpeg'}],
         'accessors': [{'bufferView': 0, 'componentType': 5126, 'count': len(p), 'type': 'VEC3', 'min': p.min(0).tolist(), 'max': p.max(0).tolist()},
                       {'bufferView': 1, 'componentType': 5126, 'count': len(p), 'type': 'VEC3'},
                       {'bufferView': 2, 'componentType': 5126, 'count': len(p), 'type': 'VEC2'},
                       {'bufferView': 3, 'componentType': 5125 if grande else 5123, 'count': len(idx), 'type': 'SCALAR'}],
         'bufferViews': vistas, 'buffers': [{'byteLength': len(BIN)}]}
    js = json.dumps(J, separators=(',', ':')).encode(); js += b' ' * ((4 - len(js) % 4) % 4)
    total = 12 + 8 + len(js) + 8 + len(BIN)
    return struct.pack('<III', 0x46546C67, 2, total) + struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(BIN), 0x004E4942) + BIN


def main():
    cfgs = json.load(open(os.path.join(FONTES, 'fontes.json'), encoding='utf-8'))
    origens = os.environ.get('FONTES_GLB') or os.path.join(FONTES, 'fontes'); os.makedirs(origens, exist_ok=True)
    alt = os.environ.get('SAIDA_MODELOS')
    unity = alt or os.path.join(RAIZ, 'unity', 'Assets', 'RitualReversal', 'Resources', 'Modelos'); os.makedirs(unity, exist_ok=True)
    js_saida = os.path.join(alt, 'modelos.js') if alt else os.path.join(RAIZ, 'client', 'modelos.js')
    saida = {}
    for nome, cfg in cfgs.items():
        if nome.startswith('_'): continue
        orig = os.path.join(origens, nome + '.glb')
        if not os.path.exists(orig):
            print('baixando', nome); urllib.request.urlretrieve(cfg['url'], orig)
        J, BIN = ler_glb(open(orig, 'rb').read())
        p, n, uv, idx, img = juntar(J, BIN)
        p, n = encaixar(p, n, cfg)
        if img is None:
            img = Image.new('RGB', (4, 4), (150, 148, 140))
        t = cfg.get('textura', 512); img = img.resize((t, t), Image.LANCZOS)
        if cfg.get('brilho'): img = img.point(lambda v, b=cfg['brilho']: min(255, round(v * b)))   # o bronze escuro do gerador vira a pedra clara das estátuas
        buf = io.BytesIO(); img.save(buf, 'JPEG', quality=86, optimize=True)
        glb = gravar_glb(p, n, uv, idx, buf.getvalue())
        open(os.path.join(unity, nome + '.bytes'), 'wb').write(glb)
        saida[nome] = base64.b64encode(glb).decode()
        print('%-8s %6d vértices %6d triângulos  %4.2f × %4.2f × %4.2f m  textura %d  GLB %d KB' % (nome, len(p), len(idx) // 3, *(p.max(0) - p.min(0)), t, len(glb) // 1024))
    js = ('// Modelos 3D gerados (Higgsfield/Tripo), montados por tools/montar-modelos.py a partir de assets/modelos/fontes.json.\n'
          '// GLB enxutos em base64; lidos por lerGLB no index.html (o Unity lê os mesmos em Resources/Modelos).\n'
          'window.RR_MODELOS=' + json.dumps(saida, separators=(',', ':')) + ';\n')
    open(js_saida, 'w').write(js)
    print(os.path.relpath(js_saida, RAIZ), len(js) // 1024, 'KB')


if __name__ == '__main__':
    main()
