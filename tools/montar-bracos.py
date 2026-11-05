#!/usr/bin/env python3
# v6.11: braços da primeira pessoa tirados de modelos com esqueleto do Sketchfab (CC BY 4.0, DJMaesen/bumstrum): a caça usa os braços
# do "revolver animated" (manga de casaco e luva de couro preta) e o culto os do "heavy pistol animated" (a mesma manga com a mão nua).
# assets/armas/fontes.json, chave "bracos": {caca: {...}, culto: {...}}; o GLB vem pela API com o token em SKETCHFAB_TOKEN para
# assets/armas/fontes/bracos_<lado>_sf.glb (fora do git). Só o braço direito do modelo é usado; o esquerdo do jogo é ele espelhado.
#
# Cada braço é UMA malha com pele, da ponta dos dedos ao ombro, com 18 ossos no jogo:
#   0–15  os 16 ossos da mão do arma.js (pulso, três falanges por dedo, polegar), como no tools/montar-maos.py: cada osso do modelo
#         tem um quadro (origem na cabeça, eixo ao longo dele, lado = do indicador para o mínimo) e o do jogo tem a origem e o
#         comprimento das falanges dele (FAL, PL); o boneInverse leva o vértice do espaço do modelo para o osso do jogo.
#   16, 17 o braço e o antebraço: os nós B.sup e B.ant do arma.js, que o IK põe entre as juntas com tuboJunta (y ao longo do osso,
#         z = eixo da dobra do cotovelo, escala raio × comprimento × raio); o boneInverse é o inverso do quadro do osso no modelo.
# Os vértices ficam no espaço do modelo e os pesos são os do modelo: a mão dobra os dedos, o punho mistura a mão e o antebraço e o
# cotovelo mistura os dois ossos do braço, como num braço de verdade.
# Saída: window.RR_BRACOS no fim de client/maos.js ({caca, culto: {pos, nor, uv, idx, si, sw, inv, raio}, tex: {caca, culto}}) e as
# texturas (cor, relevo, metal/aspereza do glTF com os fatores) em JPG em unity/.../Resources/Armas/bracos_<lado>_{cor,nor,mr}.bytes.
# Uso: SKETCHFAB_TOKEN=... python3 tools/montar-bracos.py   (numpy, Pillow)
import base64, importlib.util, io, json, os, re, sys
import numpy as np
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
def carregar(nome, arq):
    sp = importlib.util.spec_from_file_location(nome, os.path.join(RAIZ, 'tools', arq)); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m
MA = carregar('montar_armas', 'montar-armas.py'); MM = MA.MM

# esqueleto da mão do jogo (arma.js; os mesmos números do montar-maos.py)
FAL = [[.036, .024, .019], [.04, .027, .02], [.037, .025, .019], [.029, .02, .016]]
X = [-.03, -.01, .01, .029]
PL = [.034, .027, .021]
BASE_POL = np.array([-.036, -.008, -.022])
ESC_MAO = 1.16   # a escala da mão no arma.js (o raio das mangas sai na mesma escala, para a mão e o braço terem a proporção do modelo)
UNITY = os.path.join(RAIZ, 'unity', 'Assets', 'RitualReversal', 'Resources', 'Armas')


def montar_lado(nome, cfg):
    glb = MA.baixar_sketchfab(cfg['sketchfab']['uid'], os.path.join(MA.ORIG, f'bracos_{nome}_sf.glb'))
    J, B = MM.ler_glb(open(glb, 'rb').read())
    sk = J['skins'][0]; juntas = sk['joints']; nomes = [re.sub(r'_\d+$', '', J['nodes'][j].get('name', '')) for j in juntas]
    IBM = MM.acessor(J, B, sk['inverseBindMatrices']).reshape(-1, 4, 4).transpose(0, 2, 1)
    BIND = np.linalg.inv(IBM)   # quadro de cada osso no espaço da malha, na pose de repouso
    ix = {n: i for i, n in enumerate(nomes)}; S = cfg.get('lado', 'R')
    filhos = {}
    for i, j in enumerate(juntas):
        for c in J['nodes'][j].get('children', []):
            if c in juntas: filhos.setdefault(i, []).append(juntas.index(c))
    cab = lambda i: BIND[i, :3, 3]
    osso = lambda n: ix[f'{S}_{n}']
    def proximo(i):   # a cabeça do filho (a próxima falange ou a ponta)
        f = filhos.get(i, []); return cab(f[0]) if f else None
    no = next(n for n in J['nodes'] if 'mesh' in n and 'skin' in n)
    pr = J['meshes'][no['mesh']]['primitives'][0]; at = pr['attributes']
    P = MM.acessor(J, B, at['POSITION']); N = MM.acessor(J, B, at['NORMAL']); UV = MM.acessor(J, B, at['TEXCOORD_0'])
    JT = MM.acessor(J, B, at['JOINTS_0']).astype(int); WT = MM.acessor(J, B, at['WEIGHTS_0'])
    I = MM.acessor(J, B, pr['indices']).reshape(-1, 3).astype(np.int64)

    # ossos do modelo -> ossos do jogo
    DED = [('point', 1), ('middle', 4), ('ring', 7), ('pink', 10)]
    alvo = {osso('wrist'): 0, osso('palm'): 0, osso('arm'): 16, osso('elbow'): 17}
    for dn, b in DED:
        for j in range(3): alvo[osso(f'{dn}{j + 1}')] = b + j
    for j in range(3): alvo[osso(f'thumb{j + 1}')] = 13 + j
    # juntas de fora do mapa (as pontas dos dedos, Joint_3_*) vão para o osso do pai mapeado mais perto: sem isso, a ponta do dedo
    # ficava com o pouco peso que sobra noutro osso e espetava para fora
    pai_de = {c: i for i, cs in filhos.items() for c in cs}
    for i, n in enumerate(nomes):
        if i in alvo: continue
        p_ = pai_de.get(i)
        while p_ is not None and p_ not in alvo: p_ = pai_de.get(p_)
        if p_ is not None and alvo[p_] < 16: alvo[i] = alvo[p_]
    W = np.zeros((len(P), 18))
    for kk in range(4):
        for v in range(len(P)):
            j = JT[v, kk]
            if j in alvo: W[v, alvo[j]] += WT[v, kk]
    deste = W.sum(1) > .5   # só o braço deste lado (o outro pesa nos ossos L_)
    tris = I[deste[I].all(1)]

    # quadros da mão: modelo e jogo (como no montar-maos.py)
    lado_mao = cab(osso('pink1')) - cab(osso('point1')); lado_mao /= np.linalg.norm(lado_mao)
    def quadro(o, d, rolo=0.0):
        d = d / np.linalg.norm(d); s = lado_mao - d * np.dot(lado_mao, d); s /= np.linalg.norm(s); u = np.cross(-d, s)
        if rolo: c, sn = np.cos(rolo), np.sin(rolo); s, u = s * c + u * sn, -s * sn + u * c
        return o, d, s, u
    k = (X[3] - X[0]) / np.linalg.norm(cab(osso('pink1')) - cab(osso('point1')))
    mao_d = cab(osso('middle1')) - cab(osso('wrist'))
    Q = [None] * 16; Qj = [None] * 16
    Q[0] = quadro(cab(osso('wrist')), mao_d); Qj[0] = (np.zeros(3), .088 / np.linalg.norm(mao_d))
    def falange(n):   # (cabeça, direção, comprimento) de uma falange do modelo
        i = osso(n); t = proximo(i); h = cab(i); return h, t - h, np.linalg.norm(t - h)
    for kd, (dn, b) in enumerate(DED):
        o = np.array([X[kd], .003, -.088 + (.008 if kd == 3 else 0)])
        for j in range(3):
            h, d, L = falange(f'{dn}{j + 1}'); Q[b + j] = quadro(h, d); Qj[b + j] = (o.copy(), FAL[kd][j] / L); o = o + np.array([0, 0, -FAL[kd][j]])
    o = BASE_POL.copy(); rolo = float(cfg.get('rolo_polegar', .9))
    for j in range(3):
        h, d, L = falange(f'thumb{j + 1}'); Q[13 + j] = quadro(h, d, rolo); Qj[13 + j] = (o.copy(), PL[j] / L); o = o + np.array([0, 0, -PL[j]])
    inv = []
    for i in range(16):   # boneInverse: v -> M (v - o): o vértice no quadro do osso do jogo (o osso em repouso está em oj, sem giro)
        o, d, s, u = Q[i]; oj, esc = Qj[i]; M = np.eye(4); M[0, :3] = s * k; M[1, :3] = u * k; M[2, :3] = -d * esc; M[:3, 3] = -M[:3, :3] @ o
        inv.append(M)

    # braço e antebraço: quadro com o eixo da dobra do cotovelo em z (o mesmo nos dois), escala (r0, L, r0)
    om, oc, op = cab(osso('arm')), cab(osso('elbow')), cab(osso('wrist'))
    dS, dA = oc - om, op - oc; nD = np.cross(dS, dA)
    if np.linalg.norm(nD) < 1e-4 * np.linalg.norm(dS) * np.linalg.norm(dA): nD = np.cross(dS, lado_mao)
    nD /= np.linalg.norm(nD)
    raios = []
    for o, d, b in ((om, dS, 16), (oc, dA, 17)):
        L = np.linalg.norm(d); y = d / L; z = nD - y * np.dot(nD, y); z /= np.linalg.norm(z); x = np.cross(y, z)
        vs = np.unique(tris.reshape(-1)); vs = vs[W[vs, b] > .5]
        a = (P[vs] - o) @ y / L; rad = np.sqrt(((P[vs] - o) @ x) ** 2 + ((P[vs] - o) @ z) ** 2)
        r0 = float(np.median(rad[a < a.min() + .3 * (a.max() - a.min())]))
        R = np.eye(4); R[:3, 0] = x * r0; R[:3, 1] = y * L; R[:3, 2] = z * r0; R[:3, 3] = o
        inv.append(np.linalg.inv(R)); raios.append(r0 * k * ESC_MAO)   # o raio no jogo, na escala da mão
        print(f'  {nome} {"braço" if b == 16 else "antebraço"}: {L:.3f} m, raio {r0:.3f} m -> {r0 * k * ESC_MAO:.4f} no jogo; y de {a.min():.2f} a {a.max():.2f}')

    # vértices usados (os do braço deste lado), os 4 maiores pesos
    usados = np.unique(tris.reshape(-1)); mapa = np.full(len(P), -1); mapa[usados] = np.arange(len(usados))
    Wu = W[usados]; si = np.argsort(-Wu, 1)[:, :4]; sw = np.take_along_axis(Wu, si, 1); sw /= np.maximum(sw.sum(1, keepdims=True), 1e-9)
    idx = mapa[tris].reshape(-1)
    b64 = lambda a, t: base64.b64encode(np.ascontiguousarray(np.array(a, dtype=t)).tobytes()).decode()
    col = lambda M: [round(float(x), 7) for x in M.T.reshape(-1)]   # coluna por coluna, como o Matrix4.fromArray do three
    pac = {'pos': b64(P[usados], np.float32), 'nor': b64(N[usados], np.float32), 'uv': b64(UV[usados], np.float32), 'idx': b64(idx, np.uint32),
           'si': b64(si, np.uint16), 'sw': b64(sw, np.float32), 'n': int(len(usados)), 'inv': [col(M) for M in inv], 'raio': [round(r, 5) for r in raios]}
    print(f'  {nome}: {len(usados)} vértices, {len(tris)} triângulos, mão {k:.3f}× (largura do modelo para o jogo)')

    # texturas do material do braço
    m = J['materials'][pr.get('material', 0)]; pb = m.get('pbrMetallicRoughness', {}); tam = cfg['sketchfab'].get('celula', 2048)
    def imagem(t):
        im = J['images'][J['textures'][t['index']]['source']]; v = J['bufferViews'][im['bufferView']]; o = v.get('byteOffset', 0)
        return Image.open(io.BytesIO(B[o:o + v['byteLength']])).convert('RGB').resize((tam, tam), Image.LANCZOS)
    tex = {}
    for kk, t in (('cor', pb.get('baseColorTexture')), ('nor', m.get('normalTexture')), ('mr', pb.get('metallicRoughnessTexture'))):
        im = imagem(t) if t else Image.new('RGB', (8, 8), {'cor': (128, 128, 128), 'nor': (128, 128, 255), 'mr': (0, 200, 0)}[kk])
        if kk == 'mr': im = MA.fatores_mr(im, pb)
        if kk == 'cor' and cfg.get('tingir_mao'):   # a luva (o que pesa na mão, pelo uv) vira couro: a luminância × a cor pedida
            from PIL import ImageDraw
            mascara = Image.new('L', im.size, 0); dm = ImageDraw.Draw(mascara); Wt = W[:, :16].sum(1)
            for t in tris:
                if Wt[t].min() > .5: dm.polygon([(float(UV[v, 0] % 1) * im.size[0], float(UV[v, 1] % 1) * im.size[1]) for v in t], fill=255)
            a_ = np.asarray(im).astype(np.float64) / 255; lum = (a_ @ [.3, .59, .11])[..., None]; c = np.array(cfg['tingir_mao'])
            novo = np.clip(lum ** cfg.get('contraste_mao', 1.0) * c, 0, 1); mk = (np.asarray(mascara) > 0)[..., None]
            im = Image.fromarray((np.where(mk, novo, a_) * 255).round().astype(np.uint8))
        bb = io.BytesIO(); im.save(bb, 'JPEG', quality=90, subsampling=2 if kk == 'cor' else 0); tex[kk] = base64.b64encode(bb.getvalue()).decode()
        open(os.path.join(UNITY, f'bracos_{nome}_{kk}.bytes'), 'wb').write(bb.getvalue())
    return pac, tex


def main():
    F = json.load(open(os.path.join(RAIZ, 'assets', 'armas', 'fontes.json'), encoding='utf-8')); C = F['bracos']
    os.makedirs(UNITY, exist_ok=True)
    for velho in os.listdir(UNITY):   # as texturas do Arms6 (v6.10) saem
        if re.fullmatch(r'bracos_(cor|nor|mr)\.bytes', velho): os.remove(os.path.join(UNITY, velho))
    saida = {'tex': {}}
    for nome in ('caca', 'culto'):
        saida[nome], saida['tex'][nome] = montar_lado(nome, C[nome])
    arq = os.path.join(RAIZ, 'client', 'maos.js'); js = open(arq, encoding='utf-8').read()
    js = re.sub(r'\n// v6\.1\d?: braços.*\Z', '', js, flags=re.S).rstrip('\n') + '\n'
    js += ('// v6.11: braços da primeira pessoa (manga, luva e mão) dos modelos de DJMaesen/bumstrum (Sketchfab, CC BY 4.0), montados por\n'
           '// tools/montar-bracos.py. Não edite à mão.\nwindow.RR_BRACOS=' + json.dumps(saida, separators=(',', ':')) + ';\n')
    open(arq, 'w', encoding='utf-8').write(js)
    print(f'maos.js {len(js) // 1024} KB')


if __name__ == '__main__':
    main()
