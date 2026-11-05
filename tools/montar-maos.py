#!/usr/bin/env python3
# v6.6: mãos da primeira pessoa tiradas da malha base do MakeHuman (CC0), no lugar das cápsulas por falange do arma.js.
# Lê a malha base com os alvos do Soldado (assets/bonecos/fontes.json), separa a mão direita (triângulos cujos vértices pesam
# mais de metade nos ossos da mão e dos dedos) com os pesos completos do esqueleto "game_engine", e reassenta cada vértice
# no esqueleto de 16 ossos do arma.js (pulso, três falanges por dedo, polegar): cada osso do MakeHuman tem um quadro (origem
# na cabeça, eixo ao longo do osso, "cima" = dorso da mão) e o vértice vai para o quadro do osso correspondente do jogo,
# com o comprimento de cada falange esticado para o do jogo (FAL e PL no arma.js); a mistura dos quadros pelos pesos é a
# mesma conta da pele do three (linear blend), então a mão nasce na pose de repouso do jogo, dedos retos em -z.
# Saída: client/maos.js (window.RR_MAOS = {pos, nor, uv, idx, si, sw} em base64, Float32/Uint16/Uint32), lido pelo
# criarMao do arma.js; sem o arquivo, as cápsulas de antes.
# Uso: python3 -I tools/montar-maos.py   (precisa de numpy; os dados do MakeHuman vêm do montar-bonecos.py)
import base64, importlib.util, json, os, re, sys
import numpy as np

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(RAIZ, 'tools'))
import makehuman as MH

def carregar(nome, arq):
    sp = importlib.util.spec_from_file_location(nome, os.path.join(RAIZ, 'tools', arq)); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m

# esqueleto do jogo (arma.js): FAL = comprimentos das três falanges do indicador ao mínimo; X = posição dos nós; PL = polegar
FAL = [[.036, .024, .019], [.04, .027, .02], [.037, .025, .019], [.029, .02, .016]]
X = [-.03, -.01, .01, .029]
PL = [.034, .027, .021]
BASE_POL = np.array([-.036, -.008, -.022])
DEDOS = ['index', 'middle', 'ring', 'pinky']
ROLO_POLEGAR = float(os.environ.get('ROLO_POLEGAR', '0.9'))   # giro do quadro do polegar em volta do próprio eixo (a unha para fora)


def main():
    MB = carregar('montar_bonecos', 'montar-bonecos.py')
    F = json.load(open(os.path.join(RAIZ, 'assets', 'bonecos', 'fontes.json'), encoding='utf-8'))
    dados, rec = MB.makehuman_pronto(F)
    cfg = F['bonecos']['soldado']['makehuman']
    P0, T0, F0 = MH.ler_obj(os.path.join(dados, '3dobjs', 'base.obj'))
    P = P0.copy()
    for rel, w in cfg.get('alvos', []):
        ii, d = MH.ler_alvo(os.path.join(dados, 'targets', rel)); P[ii] += d * w
    P = P * MH.DM   # metros
    rig = json.load(open(os.path.join(dados, 'rigs', 'standard', 'rig.game_engine.json')))
    W = json.load(open(os.path.join(dados, 'rigs', 'standard', 'weights.game_engine.json')))['weights']
    grupos = {}
    for ids, g in F0:
        if g.startswith('joint-'): grupos.setdefault(g, set()).update(v for v, _ in ids)
    def ponto(h):
        if h.get('strategy') == 'CUBE' and h.get('cube_name') in grupos: return P[sorted(grupos[h['cube_name']])].mean(0)
        return P[h['vertex_indices']].mean(0)
    cab = {b: ponto(rig[b]['head']) for b in rig}
    cau = {b: ponto(rig[b]['tail']) for b in rig if 'tail' in rig[b]}

    # ossos do jogo, na ordem do criarMao: pulso; indicador..mínimo (3 cada); polegar (3)
    nomes = ['hand_r'] + [f'{d}_0{j}_r' for d in DEDOS for j in (1, 2, 3)] + ['thumb_01_r', 'thumb_02_r', 'thumb_03_r']
    idx_de = {n: i for i, n in enumerate(nomes)}
    # pesos de cada vértice nos ossos da mão
    nv = len(P); Wm = np.zeros((nv, 16)); fora = np.zeros(nv)
    for osso, lista in W.items():
        for v, w in lista:
            if osso in idx_de: Wm[v, idx_de[osso]] += w
            else: fora[v] += w
    soma = Wm.sum(1)
    tris = []
    for ids, g in F0:
        if g != 'body': continue
        vs = [v for v, _ in ids]
        if all(soma[v] > .5 * (soma[v] + fora[v]) for v in vs):
            for k in range(1, len(ids) - 1): tris.append((ids[0], ids[k], ids[k + 1]))

    # quadros do MakeHuman: lado = do indicador para o mínimo (nos nós), cima = dorso; eixo = ao longo do osso
    lado_mao = cab['pinky_01_r'] - cab['index_01_r']; lado_mao /= np.linalg.norm(lado_mao)
    def quadro(o, d, rolo=0.0):
        d = d / np.linalg.norm(d); s = lado_mao - d * np.dot(lado_mao, d); s /= np.linalg.norm(s); u = np.cross(-d, s)
        if rolo: c, sn = np.cos(rolo), np.sin(rolo); s, u = s * c + u * sn, -s * sn + u * c
        return o, d, s, u
    Q, Qj = [], []   # quadro MH e (origem, comprimento-alvo / comprimento MH) do jogo
    mao_d = cab['middle_01_r'] - cab['hand_r']
    larg_mh = np.linalg.norm(cab['pinky_01_r'] - cab['index_01_r']); larg_jogo = X[3] - X[0]; k = larg_jogo / larg_mh
    comp_palma = np.dot(mao_d, mao_d / np.linalg.norm(mao_d))
    Q.append(quadro(cab['hand_r'], mao_d)); Qj.append((np.zeros(3), .088 / comp_palma))
    for kd, dn in enumerate(DEDOS):
        o = np.array([X[kd], .003, -.088 + (.008 if kd == 3 else 0)])
        for j in range(3):
            b = f'{dn}_0{j + 1}_r'; h = cab[b]; t = cau[b]; L = np.linalg.norm(t - h)
            Q.append(quadro(h, t - h)); Qj.append((o.copy(), FAL[kd][j] / L)); o = o + np.array([0, 0, -FAL[kd][j]])
    o = BASE_POL.copy()
    for j in range(3):
        b = f'thumb_0{j + 1}_r'; h = cab[b]; t = cau[b]; L = np.linalg.norm(t - h)
        Q.append(quadro(h, t - h, ROLO_POLEGAR)); Qj.append((o.copy(), PL[j] / L)); o = o + np.array([0, 0, -PL[j]])

    usados = sorted({v for t in tris for v, _ in t})
    def leva(p, i):
        o, d, s, u = Q[i]; oj, esc = Qj[i]; q = p - o
        a, ss, uu = np.dot(q, d), np.dot(q, s), np.dot(q, u)
        return oj + np.array([ss * k, uu * k, -a * esc])
    # vértices únicos por (v, vt) para manter as costuras do uv
    mapa, pos, uv, si, sw, idx = {}, [], [], [], [], []
    for t in tris:
        for v, vt in t:
            c = (v, vt)
            if c not in mapa:
                w = Wm[v] / max(1e-9, Wm[v].sum()); top = np.argsort(-w)[:4]; ww = w[top]; ww = ww / ww.sum()
                p = sum(ww[j] * leva(P[v], top[j]) for j in range(4) if ww[j] > 0)
                mapa[c] = len(pos); pos.append(p); uv.append(T0[vt] if vt >= 0 else (0, 0)); si.append(top); sw.append(ww)
            idx.append(mapa[c])
    pos = np.array(pos, dtype=np.float32); idx = np.array(idx, dtype=np.uint32)
    # o MakeHuman tem a frente em +z e o three gira as faces pela ordem: se a normal média do dorso aponta para baixo, inverte
    nor = MH.normais(pos.astype(np.float64), idx.astype(np.int64)).astype(np.float32)
    dorso = pos[:, 1] > np.percentile(pos[:, 1], 80)
    if nor[dorso, 1].mean() < 0:
        idx = idx.reshape(-1, 3)[:, ::-1].reshape(-1).copy(); nor = -nor
    # o uv do MakeHuman é uma ilha pequena do atlas da pele; a luva usa couro repetido: a ilha vira 0..2,5 (~7 cm por ladrilho)
    uv = np.array(uv, dtype=np.float64); uvBruto = uv.copy(); uv = (uv - uv.min(0)) / max(1e-9, (uv.max(0) - uv.min(0)).max()) * 2.5
    # v6.8: a pele de verdade para a mão nua (o culto, no Unity): o pedaço da mão na textura de pele do MakeHuman (CC0) e o uv original
    # da malha base levado a esse recorte. Resources/Maos/pele_cor.bytes (JPG) e pele_uv.bytes (float32, na ordem dos vértices)
    from PIL import Image
    pele = os.path.join(rec, 'skins', 'middleage_caucasian_male', 'middleage_lightskinned_male_diffuse.png')
    if os.path.exists(pele):
        im = Image.open(pele).convert('RGB'); W, H = im.size
        u0, v0 = uvBruto.min(0) - .004; u1, v1 = uvBruto.max(0) + .004
        caixa = (int(u0 * W), int((1 - v1) * H), int(np.ceil(u1 * W)), int(np.ceil((1 - v0) * H)))
        rec_ = im.crop(caixa); esc = 512 / max(rec_.size); rec_ = rec_.resize((max(1, round(rec_.size[0] * esc)), max(1, round(rec_.size[1] * esc))), Image.LANCZOS)
        uu = (uvBruto[:, 0] * W - caixa[0]) / (caixa[2] - caixa[0]); vv = ((uvBruto[:, 1] - 1) * H + caixa[3]) / (caixa[3] - caixa[1])
        d = os.path.join(RAIZ, 'unity', 'Assets', 'RitualReversal', 'Resources', 'Maos'); os.makedirs(d, exist_ok=True)
        import io; b = io.BytesIO(); rec_.save(b, 'JPEG', quality=90); open(os.path.join(d, 'pele_cor.bytes'), 'wb').write(b.getvalue())
        open(os.path.join(d, 'pele_uv.bytes'), 'wb').write(np.c_[uu, vv].astype('<f4').tobytes())
        print(f'pele da mão: recorte {caixa} da {os.path.basename(pele)} -> {rec_.size}, uv em Resources/Maos')
    b64 = lambda a: base64.b64encode(np.ascontiguousarray(a).tobytes()).decode()
    saida = {'pos': b64(pos), 'nor': b64(nor), 'uv': b64(uv.astype(np.float32)), 'idx': b64(idx),
             'si': b64(np.array(si, dtype=np.uint16)), 'sw': b64(np.array(sw, dtype=np.float32)), 'n': len(pos), 't': len(idx) // 3}
    js = ('// Mãos da primeira pessoa (malha base do MakeHuman, CC0), reassentadas no esqueleto de 16 ossos do arma.js por\n'
          '// tools/montar-maos.py. Não edite à mão.\nwindow.RR_MAOS=' + json.dumps(saida, separators=(',', ':')) + ';\n')
    arq = os.path.join(RAIZ, 'client', 'maos.js')   # v6.10: os braços do montar-bracos.py ficam no fim do arquivo
    if os.path.exists(arq):
        antigo = open(arq, encoding='utf-8').read(); m_ = re.search(r'\n// v6\.1\d?: braços', antigo); i = m_.start() if m_ else -1
        if i >= 0: js += antigo[i + 1:]
    open(arq, 'w', encoding='utf-8').write(js)
    mn, mx = pos.min(0), pos.max(0)
    print(f'mão: {len(pos)} vértices, {len(idx) // 3} triângulos, caixa {np.round(mn, 3)} .. {np.round(mx, 3)}, maos.js {len(js) // 1024} KB')


if __name__ == '__main__':
    main()
