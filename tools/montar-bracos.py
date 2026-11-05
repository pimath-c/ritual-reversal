#!/usr/bin/env python3
# v6.10: braços da primeira pessoa da caça tirados de um modelo com esqueleto do Sketchfab ("Arms6", bumstrum, CC BY 4.0):
# manga de pano gasto e luva de couro. assets/armas/fontes.json, chave "bracos", diz de onde vem (o GLB é baixado pela API com
# o token em SKETCHFAB_TOKEN, para assets/armas/fontes/bracos_sf.glb, fora do git).
#
# Do braço direito do modelo saem três peças:
#   - a luva: triângulos cujos vértices pesam mais de metade na mão e nos dedos. Cada vértice é levado dos quadros dos ossos do
#     modelo para os 16 ossos do arma.js (pulso, três falanges por dedo, polegar), como o tools/montar-maos.py faz com o MakeHuman:
#     quadro = origem na cabeça do osso, eixo ao longo dele, lado = do indicador para o mínimo; comprimentos esticados para os do
#     jogo (FAL, PL); a mistura pelos pesos é a da pele do three, então a luva nasce na pose de repouso do jogo, dedos retos em -z.
#   - a manga (braço e antebraço numa malha só, v6.10a): pele em dois ossos com os pesos do modelo, que no jogo são os nós B.sup e
#     B.ant do arma.js (o IK põe cada um entre as juntas, com o eixo da dobra do cotovelo em z e escala raio × comprimento × raio).
# Saída: window.RR_BRACOS no fim de client/maos.js ({luva, manga, mangaC: {pos, nor, uv, idx, si, sw}} em base64; inv: os boneInverse
# dos ossos da manga)
# e as texturas (cor, relevo, metal/aspereza do glTF) em JPG em RR_BRACOS.tex e em unity/.../Resources/Armas/bracos_*.bytes.
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
ROLO_POLEGAR = float(os.environ.get('ROLO_POLEGAR', '0.9'))
UNITY = os.path.join(RAIZ, 'unity', 'Assets', 'RitualReversal', 'Resources', 'Armas')


def main():
    F = json.load(open(os.path.join(RAIZ, 'assets', 'armas', 'fontes.json'), encoding='utf-8')); cfg = F['bracos']
    glb = MA.baixar_sketchfab(cfg['sketchfab']['uid'], os.path.join(MA.ORIG, 'bracos_sf.glb'))
    J, B = MM.ler_glb(open(glb, 'rb').read())
    sk = J['skins'][0]; nomes = [re.sub(r'_\d+$', '', J['nodes'][j].get('name', '')) for j in sk['joints']]
    IBM = MM.acessor(J, B, sk['inverseBindMatrices']).reshape(-1, 4, 4).transpose(0, 2, 1)
    BIND = np.linalg.inv(IBM)   # quadro de cada osso no espaço da malha, na pose de repouso
    ix = {n: i for i, n in enumerate(nomes)}; S = cfg.get('lado', 'R')
    cab = lambda n: BIND[ix[f'{n}.{S}'], :3, 3]
    eixo = lambda n: BIND[ix[f'{n}.{S}'], :3, 1] / np.linalg.norm(BIND[ix[f'{n}.{S}'], :3, 1])
    pr = J['meshes'][0]['primitives'][0]; at = pr['attributes']
    P = MM.acessor(J, B, at['POSITION']); N = MM.acessor(J, B, at['NORMAL']); UV = MM.acessor(J, B, at['TEXCOORD_0'])
    JT = MM.acessor(J, B, at['JOINTS_0']).astype(int); WT = MM.acessor(J, B, at['WEIGHTS_0'])
    I = MM.acessor(J, B, pr['indices']).reshape(-1, 3).astype(np.int64)
    nv = len(P)

    # ossos do modelo -> grupos: mão (pulso e dedos do jogo), antebraço, braço
    DED = [('point', 1), ('middle', 4), ('ring', 7), ('pink', 10)]
    alvo = {}   # índice do osso do modelo -> índice do osso do jogo (0..15)
    for n in ('hand', 'palm'): alvo[ix[f'{n}.{S}']] = 0
    for dn, b in DED:
        for j in range(3): alvo[ix[f'{dn}{j + 1}.{S}']] = b + j
    for j in range(3): alvo[ix[f'thumb{j + 1}.{S}']] = 13 + j
    ANT = {ix[f'elbow.{S}'], ix[f'forearm.{S}']}; SUP = {ix[f'arm.{S}']}
    Wm = np.zeros((nv, 16)); wAnt = np.zeros(nv); wSup = np.zeros(nv)
    for v in range(nv):
        for k in range(4):
            j, w = JT[v, k], WT[v, k]
            if w <= 0: continue
            if j in alvo: Wm[v, alvo[j]] += w
            elif j in ANT: wAnt[v] += w
            elif j in SUP: wSup[v] += w
    wMao = Wm.sum(1)
    lado_v = np.array([wMao[v] + wAnt[v] + wSup[v] for v in range(nv)]) > .5   # só o braço deste lado
    I = np.array([t for t in I if all(lado_v[t])])
    # ilhas da malha (vértices na mesma posição contam como um só: as costuras do uv não separam): a luva é a ilha que pesa na mão;
    # cortar por triângulo deixava a borda do punho em dente de serra
    chave = {}; rep = np.array([chave.setdefault(tuple(np.round(P[v], 5)), v) for v in range(nv)])
    pai = list(range(nv))
    def raiz(x):
        while pai[x] != x: pai[x] = pai[pai[x]]; x = pai[x]
        return x
    for t in I:
        a0 = raiz(rep[t[0]])
        for v in t[1:]: b0 = raiz(rep[v]); pai[b0] = a0
    ilha = np.array([raiz(rep[t[0]]) for t in I]); tg = []; ilhas = {}
    for t, il in zip(I, ilha): ilhas.setdefault(il, []).append(t)
    for il, ts in ilhas.items():
        vs = np.unique(np.array(ts).reshape(-1)); mao = wMao[vs].mean() > .5
        print(f'ilha {il}: {len(ts)} triângulos, peso na mão {wMao[vs].mean():.2f} -> {"luva" if mao else "manga"}')
        for t in ts: tg.append((t, 0 if mao else 1))

    # ---- luva: quadros do modelo e do jogo (como no montar-maos.py)
    lado_mao = cab('pink1') - cab('point1'); lado_mao /= np.linalg.norm(lado_mao)
    def quadro(o, d, rolo=0.0):
        d = d / np.linalg.norm(d); s = lado_mao - d * np.dot(lado_mao, d); s /= np.linalg.norm(s); u = np.cross(-d, s)
        if rolo: c, sn = np.cos(rolo), np.sin(rolo); s, u = s * c + u * sn, -s * sn + u * c
        return o, d, s, u
    larg = np.linalg.norm(cab('pink1') - cab('point1')); k = (X[3] - X[0]) / larg
    mao_d = cab('middle1') - cab('hand'); comp_palma = np.linalg.norm(mao_d)
    Q, Qj = [quadro(cab('hand'), mao_d)], [(np.zeros(3), .088 / comp_palma)]
    def comp(dn, j):   # comprimento da falange j no modelo (a última: 0,85 da do meio; a ponta não tem osso)
        if j < 2: return np.linalg.norm(cab(f'{dn}{j + 2}') - cab(f'{dn}{j + 1}'))
        return .85 * comp(dn, 1)
    for kd, (dn, _) in enumerate(DED):
        o = np.array([X[kd], .003, -.088 + (.008 if kd == 3 else 0)])
        for j in range(3):
            L = comp(dn, j); d = (cab(f'{dn}{j + 2}') - cab(f'{dn}{j + 1}')) if j < 2 else eixo(f'{dn}{j + 1}')
            Q.append(quadro(cab(f'{dn}{j + 1}'), d)); Qj.append((o.copy(), FAL[kd][j] / L)); o = o + np.array([0, 0, -FAL[kd][j]])
    o = BASE_POL.copy()
    for j in range(3):
        L = comp('thumb', j); d = (cab(f'thumb{j + 2}') - cab(f'thumb{j + 1}')) if j < 2 else eixo(f'thumb{j + 1}')
        Q.append(quadro(cab(f'thumb{j + 1}'), d, ROLO_POLEGAR)); Qj.append((o.copy(), PL[j] / L)); o = o + np.array([0, 0, -PL[j]])
    def leva(p, i):
        o, d, s, u = Q[i]; oj, esc = Qj[i]; q = p - o
        return oj + np.array([np.dot(q, s) * k, np.dot(q, u) * k, -np.dot(q, d) * esc])
    def gira(n, i):
        o, d, s, u = Q[i]; oj, esc = Qj[i]; return np.array([np.dot(n, s) / k, np.dot(n, u) / k, -np.dot(n, d) / esc])

    def montar(tris, f):
        mapa, out = {}, {'pos': [], 'nor': [], 'uv': [], 'idx': [], 'si': [], 'sw': []}
        for t in tris:
            for v in t:
                if v not in mapa:
                    mapa[v] = len(out['pos']); p, n, si, sw = f(v)
                    out['pos'].append(p); out['nor'].append(n / max(1e-9, np.linalg.norm(n))); out['uv'].append(UV[v]); out['si'].append(si); out['sw'].append(sw)
                out['idx'].append(mapa[v])
        return out
    def vert_luva(v):
        w = Wm[v].copy(); w[0] += wAnt[v] + wSup[v]   # a borda do punho que pesa no antebraço fica presa ao pulso
        w /= max(1e-9, w.sum()); top = np.argsort(-w)[:4]; ww = w[top]; ww = ww / ww.sum()
        p = sum(ww[j] * leva(P[v], top[j]) for j in range(4) if ww[j] > 0); n = sum(ww[j] * gira(N[v], top[j]) for j in range(4) if ww[j] > 0)
        return p, n, top, ww
    luva = montar([t for t, g in tg if g == 0], vert_luva)
    # o cano da luva é rígido no pulso e, longo, abria como um sino quando o antebraço entra em outro ângulo: corta num plano reto
    # pouco acima do punho (z do jogo), e a manga cobre o resto
    def cortar_pele(o, zmax):
        pos = [np.array(p) for p in o['pos']]; L = {k: list(o[k]) for k in ('nor', 'uv', 'si', 'sw')}; idx = []
        def novo(a, b, f):
            pos.append(pos[a] + (pos[b] - pos[a]) * f)
            for k in ('nor', 'uv'): L[k].append(np.array(L[k][a]) + (np.array(L[k][b]) - np.array(L[k][a])) * f)
            for k in ('si', 'sw'): L[k].append(L[k][a])   # o vértice novo fica com os pesos do que está dentro (o pulso, no cano)
            return len(pos) - 1
        T = np.array(o['idx']).reshape(-1, 3)
        for t in T:
            d = [zmax - pos[v][2] for v in t]
            if min(d) >= 0: idx += list(t); continue
            if max(d) < 0: continue
            poli = []
            for i in range(3):
                va, vb, da, db = t[i], t[(i + 1) % 3], d[i], d[(i + 1) % 3]
                if da >= 0: poli.append(va)
                if (da >= 0) != (db >= 0): poli.append(novo(va, vb, da / (da - db)) if da >= 0 else novo(vb, va, db / (db - da)))
            for i in range(1, len(poli) - 1): idx += [poli[0], poli[i], poli[i + 1]]
        usados = sorted(set(idx)); m = {v: i for i, v in enumerate(usados)}
        r = {'pos': [pos[v] for v in usados], 'idx': [m[v] for v in idx]}
        for k in L: r[k] = [L[k][v] for v in usados]
        return r
    luva = cortar_pele(luva, cfg.get('luva_corte', .025))

    # ---- mangas (v6.10a): a manga inteira (braço e antebraço) é uma malha só, com pele em dois ossos, que dobra no cotovelo como
    # um braço de verdade (antes, dois tubos rígidos que se cruzavam: "dois pedaços de pau"). Cada osso tem um quadro no espaço do
    # modelo: origem na junta de cima, y ao longo do osso, z = eixo da dobra do cotovelo (o mesmo nos dois ossos: a dobra é uma
    # dobradiça), x = y × z, e a escala (r0, L, r0); no jogo os ossos são os nós B.sup e B.ant do arma.js, postos pelo IK com o
    # mesmo quadro (tuboJunta) e escala (raio, comprimento, raio). O inverso de cada quadro vai em RR_BRACOS.inv (boneInverse).
    mangas = [t for t, gg in tg if gg == 1]
    cot = cab('elbow'); dS = cot - cab('arm'); dA = cab('hand') - cot
    nDobra = np.cross(dS, dA); nDobra /= np.linalg.norm(nDobra)
    vsM = np.unique(np.array(mangas).reshape(-1))
    wS = wSup.copy(); wA = wAnt + wMao   # o que a manga pesa na mão vai para o antebraço
    tot = np.maximum(wS + wA, 1e-9); wS, wA = wS / tot, wA / tot
    def quadroOsso(o, d, vs):
        L = np.linalg.norm(d); y = d / L; z = nDobra - y * np.dot(nDobra, y); z /= np.linalg.norm(z); x = np.cross(y, z)
        a = (P[vs] - o) @ y / L; rad = np.sqrt(((P[vs] - o) @ x) ** 2 + ((P[vs] - o) @ z) ** 2)
        r0 = float(np.median(rad[(a > a.min()) & (a < a.min() + .25 * (a.max() - a.min()))]))
        R = np.eye(4); R[:3, 0] = x * r0; R[:3, 1] = y * L; R[:3, 2] = z * r0; R[:3, 3] = o
        return R, np.linalg.inv(R), r0, L
    Rs, Is, r0s, Ls = quadroOsso(cab('arm'), dS, vsM[wS[vsM] > .5]); Ra, Ia, r0a, La = quadroOsso(cot, dA, vsM[wA[vsM] > .5])
    loc = lambda Mi, p: (Mi @ np.append(p, 1))[:3]
    ysS = np.array([loc(Is, P[v])[1] for v in vsM if wS[v] > .5]); ysA = np.array([loc(Ia, P[v])[1] for v in vsM if wA[v] > .5])
    s_ini, a_fim = ysS.min(), ysA.max()
    print(f'manga: braço {Ls:.3f} m (raio {r0s:.3f}, começa em {s_ini:.2f}), antebraço {La:.3f} m (raio {r0a:.3f}, vai até {a_fim:.2f}), dobra {np.round(nDobra, 2)}')
    def manga(ate):
        # a manga do braço começa abaixo do ombro do modelo: estica até o ombro; a do antebraço estica até `ate` (o cano da luva cobre
        # menos no jogo, que tem a mão menor em relação ao antebraço). Perto do cotovelo, onde os pesos se misturam, nada estica.
        def f(v):
            uS = loc(Is, P[v]); uA = loc(Ia, P[v])
            if uS[1] < 1: uS[1] = 1 - (1 - uS[1]) / (1 - s_ini)
            if uA[1] > 0: uA[1] = uA[1] * ate / a_fim
            # a boca da manga do modelo é larga e, no punho do jogo, abria como uma aba: aperta até `boca` do raio no punho
            t = min(1, max(0, (uA[1] - .55) / max(1e-6, ate - .55)))
            if t > 0: k = 1 - (1 - cfg.get('boca', .72)) * t * t * (3 - 2 * t); uA[0] *= k; uA[2] *= k
            p = wS[v] * (Rs @ np.append(uS, 1))[:3] + wA[v] * (Ra @ np.append(uA, 1))[:3]
            return p, N[v].copy(), np.array([0, 1, 0, 0]), np.array([wS[v], wA[v], 0, 0])
        return montar(mangas, f)
    mangaH = manga(cfg.get('manga_ate', .93)); mangaC = manga(1.0)   # o culto não tem luva: a manga vai até o punho
    col = lambda M: [round(float(x), 7) for x in M.T.reshape(-1)]   # coluna por coluna, como o Matrix4.fromArray do three

    b64 = lambda a, t: base64.b64encode(np.ascontiguousarray(np.array(a, dtype=t)).tobytes()).decode()
    def pacote(o, pele):
        r = {'pos': b64(o['pos'], np.float32), 'nor': b64(o['nor'], np.float32), 'uv': b64(o['uv'], np.float32), 'idx': b64(o['idx'], np.uint32), 'n': len(o['pos'])}
        if pele: r['si'] = b64(o['si'], np.uint16); r['sw'] = b64(o['sw'], np.float32)
        return r
    # texturas: a do material do modelo, em JPG (a cor em sRGB; relevo e metal/aspereza lineares)
    m = J['materials'][pr.get('material', 0)]; pb = m.get('pbrMetallicRoughness', {}); tam = cfg['sketchfab'].get('celula', 2048)
    def imagem(tex):
        im = J['images'][J['textures'][tex['index']]['source']]; v = J['bufferViews'][im['bufferView']]; o = v.get('byteOffset', 0)
        return Image.open(io.BytesIO(B[o:o + v['byteLength']])).convert('RGB').resize((tam, tam), Image.LANCZOS)
    tex = {}
    os.makedirs(UNITY, exist_ok=True)
    for kk, t in (('cor', pb.get('baseColorTexture')), ('nor', m.get('normalTexture')), ('mr', pb.get('metallicRoughnessTexture'))):
        im = imagem(t) if t else Image.new('RGB', (8, 8), {'cor': (128, 128, 128), 'nor': (128, 128, 255), 'mr': (0, 200, 0)}[kk])
        if kk == 'mr': im = MA.fatores_mr(im, pb)
        bb = io.BytesIO(); im.save(bb, 'JPEG', quality=90, subsampling=2 if kk == 'cor' else 0); tex[kk] = base64.b64encode(bb.getvalue()).decode()
        open(os.path.join(UNITY, f'bracos_{kk}.bytes'), 'wb').write(bb.getvalue())
    saida = {'luva': pacote(luva, True), 'manga': pacote(mangaH, True), 'mangaC': pacote(mangaC, True), 'inv': {'sup': col(Is), 'ant': col(Ia)}, 'tex': tex}
    arq = os.path.join(RAIZ, 'client', 'maos.js'); js = open(arq, encoding='utf-8').read()
    js = re.sub(r'\n// v6\.10: braços.*\Z', '', js, flags=re.S).rstrip('\n') + '\n'
    js += ('// v6.10: braços da caça (luva e mangas) do "Arms6" de bumstrum (Sketchfab, CC BY 4.0), montados por tools/montar-bracos.py.\n'
           'window.RR_BRACOS=' + json.dumps(saida, separators=(',', ':')) + ';\n')
    open(arq, 'w', encoding='utf-8').write(js)
    pl = np.array(luva['pos']); print(f'luva: {len(pl)} vértices, {len(luva["idx"]) // 3} triângulos, caixa {pl.min(0).round(3)} .. {pl.max(0).round(3)}; maos.js {len(js) // 1024} KB')


if __name__ == '__main__':
    main()
