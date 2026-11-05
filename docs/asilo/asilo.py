# Planta do Asilo Blackwater: espaços abertos (salas, corredores, pátios) e vãos que os ligam; o resto é parede.
# Proposta do segundo mapa (25/10). Uso: python3 -I docs/asilo/asilo.py asilo.json && python3 -I docs/asilo/planta.py asilo.json planta.svg
# Mede os percursos pela grade (A*, 8 direções, passo 0,5 m) e desenha a planta em SVG.
import json, heapq, math, sys
P = 0.5
X0, X1, Z0, Z1 = -108, 108, -72, 66

E = []   # espaços: (nome, x1, x2, z1, z2, tipo)  tipo: 'int' (coberto) / 'ext' (luar)
def esp(n, x1, x2, z1, z2, t='int'): E.append((n, x1, x2, z1, z2, t))
V = []   # vãos: (x1, x2, z1, z2, porta?)  porta=True: porta que abre e fecha
def vao(x1, x2, z1, z2, porta=False): V.append((x1, x2, z1, z2, porta))

def lado(s):   # espelha um espaço do oeste para o leste
    return s
# ---------------- exterior ----------------
esp('Gramado', -60, 60, 26, 62, 'ext')
esp('Horta', -102, -62, 10, 62, 'ext')
esp('Cemitério', 62, 102, 10, 62, 'ext')
esp('Margem', -106, 106, -66, -60, 'ext')
esp('Pátio Norte', -46, 46, -60, -43, 'ext')
esp('Trilha Oeste', -106, -102, -60, 62, 'ext')
esp('Trilha Leste', 102, 106, -60, 62, 'ext')
esp('Píer', -2.5, 2.5, -72, -66, 'ext')
# portões na sebe entre a horta/cemitério e o gramado
for s in (-1, 1):
    vao(*sorted((s*62, s*60)), 38, 42); vao(*sorted((s*62, s*60)), 54, 58)
    vao(*sorted((s*102, s*106)), 14, 18); vao(*sorted((s*102, s*106)), 44, 48)   # horta/cemitério -> trilha
# ---------------- bloco central ----------------
esp('Saguão', -12, 12, 2, 24)
vao(-3, 3, 24, 26, True)            # porta principal
esp('Corredor da Capela', -3, 3, -12, 2)
esp('Arquivo', -12, -4, -12, 0); vao(-4, -3, -8, -5, True)
esp('Farmácia', 4, 12, -12, 0); vao(3, 4, -8, -5, True)
esp('Capela', -12, 12, -40, -12)
vao(-2, 2, -43, -40, True)          # porta norte da capela
# ---------------- alas (oeste escrita, leste espelhada) ----------------
def ala(s, nomes):
    X = lambda a, b: tuple(sorted((s*a, s*b)))
    esp('Corredor %s 1' % nomes['ala'], *X(12, 48), 8, 12); vao(*X(12, 12.5), 8, 12)
    for i, (a, b) in enumerate(((14, 24), (25, 35), (36, 46))):
        esp(nomes['n1'][i], *X(a, b), -2, 7); vao(*X(a+3, a+5), 7, 8, i == 1)
        esp(nomes['s1'][i], *X(a, b), 13, 22); vao(*X(a+3, a+5), 12, 13, i == 1)
    esp('Escada %s' % nomes['ala'], *X(48, 52), -20, 24); vao(*X(48, 52), 24, 26, True)   # saída lateral para o gramado
    esp('Corredor %s 2' % nomes['ala'], *X(52, 84), -20, -16)
    esp(nomes['refeitorio'], *X(56, 84), -32, -21); vao(*X(70, 74), -21, -20)
    esp(nomes['rouparia'], *X(52, 55), -32, -21); vao(*X(52, 55), -21, -20, True)
    esp(nomes['estar'], *X(56, 69), -15, -4); vao(*X(60, 63), -16, -15)
    esp(nomes['consultorio'], *X(70, 84), -15, -4); vao(*X(74, 77), -16, -15, True)
    esp(nomes['ward'], *X(85, 100), -34, 6); vao(*X(84, 85), -20, -16, True); vao(*X(90, 94), 6, 10)   # pátio de banho de sol
    esp(nomes['anexo'], *X(48, 76), -56, -38); vao(*X(60, 64), -38, -32)                           # anexo ao norte
    vao(*X(62, 66), -60, -56); vao(*X(46, 48), -51, -47, True)                                        # anexo -> margem / pátio
for s, n in ((-1, dict(ala='Oeste', n1=['Quadro Oeste', 'Quartos 2', 'Quartos 1'], s1=['Quartos 3', 'Sala de Visitas', 'Quartos 4'],
                       refeitorio='Refeitório', rouparia='Rouparia', estar='Sala de Estar', consultorio='Consultório',
                       ward='Enfermaria', anexo='Hidroterapia')),
             (1, dict(ala='Leste', n1=['Quadro Leste', 'Celas 2', 'Celas 1'], s1=['Celas 3', 'Sala de Contenção', 'Celas 4'],
                      refeitorio='Lavanderia', rouparia='Depósito', estar='Oficina', consultorio='Eletrochoque',
                      ward='Isolamento', anexo='Caldeiras'))):
    ala(s, n)

ALT = [('Enfermaria', -92, -14), ('Capela', 0, -30), ('Isolamento', 92, -14),
       ('Hidroterapia', -62, -47), ('Coreto', 0, 46), ('Caldeiras', 62, -47)]
SPAWN = {'H': (-90, 50), 'C': (90, 50)}
QUADROS = [('Quadro Oeste', -19, 2.5), ('Arquivo', -8, -6), ('Quadro Leste', 19, 2.5)]

# ---------------- grade ----------------
nx, nz = int((X1-X0)/P), int((Z1-Z0)/P)
livre = bytearray(nx*nz)
def marca(x1, x2, z1, z2, r=.4):   # o centro do jogador cabe a r da parede
    for i in range(max(0, int((x1+r-X0)/P)), min(nx, int(math.ceil((x2-r-X0)/P)))):
        for j in range(max(0, int((z1+r-Z0)/P)), min(nz, int(math.ceil((z2-r-Z0)/P)))):
            livre[j*nx+i] = 1
for n, x1, x2, z1, z2, t in E: marca(x1, x2, z1, z2)
for x1, x2, z1, z2, p in V:
    # o vão liga os dois lados: estica 0,6 m para dentro de cada espaço
    if x2-x1 <= z2-z1: marca(x1-.6, x2+.6, z1, z2, .4)
    else: marca(x1, x2, z1-.6, z2+.6, .4)
def cel(x, z): return int((x-X0)/P), int((z-Z0)/P)
def dist_de(x, z):
    D = [1e18]*(nx*nz); i, j = cel(x, z); k0 = j*nx+i
    if not livre[k0]: sys.exit('origem bloqueada %s %s' % (x, z))
    D[k0] = 0; h = [(0, k0)]
    viz = [(1, 0, 1), (-1, 0, 1), (0, 1, 1), (0, -1, 1), (1, 1, 1.4142), (1, -1, 1.4142), (-1, 1, 1.4142), (-1, -1, 1.4142)]
    while h:
        d, k = heapq.heappop(h)
        if d > D[k]: continue
        ci, cj = k % nx, k // nx
        for di, dj, c in viz:
            a, b = ci+di, cj+dj
            if 0 <= a < nx and 0 <= b < nz and livre[b*nx+a] and (di == 0 or dj == 0 or (livre[cj*nx+a] and livre[b*nx+ci])):
                nd = d+c*P; kk = b*nx+a
                if nd < D[kk]: D[kk] = nd; heapq.heappush(h, (nd, kk))
    return D
def em(D, x, z): i, j = cel(x, z); return D[j*nx+i]

DS = {t: dist_de(*SPAWN[t]) for t in 'HC'}
DA = [dist_de(x, z) for _, x, z in ALT]
v = 4.6
print('== Dos nascimentos até cada altar ==')
for k, (n, x, z) in enumerate(ALT):
    h, c = em(DS['H'], x, z), em(DS['C'], x, z)
    print('%d %-13s caça %4.0f m (%2.0f s)  culto %4.0f m (%2.0f s)  diferença %+4.0f m' % (k, n, h, h/v, c, c/v, h-c))
mh = sum(em(DS['H'], x, z) for _, x, z in ALT)/6; mc = sum(em(DS['C'], x, z) for _, x, z in ALT)/6
print('média: caça %.0f m, culto %.0f m' % (mh, mc))
print('== Entre altares ==')
M = [[em(DA[a], ALT[b][1], ALT[b][2]) for b in range(6)] for a in range(6)]
for a in range(6): print(' '.join('%5.0f' % M[a][b] for b in range(6)))
print('mais próximo de cada:', [round(min(M[a][b] for b in range(6) if b != a)) for a in range(6)])
area_int = sum((x2-x1)*(z2-z1) for n, x1, x2, z1, z2, t in E if t == 'int'); area_ext = sum((x2-x1)*(z2-z1) for n, x1, x2, z1, z2, t in E if t == 'ext')
print('área interna %.0f m², externa %.0f m²; portas %d, vãos %d' % (area_int, area_ext, sum(1 for q in V if q[4]), sum(1 for q in V if not q[4])))
PONTOS = {
 'erva': [(-70, -26), (70, -26), (-62, -10), (62, -10), (8, -6), (-8, -6), (-30, 18), (30, 18)],
 'tumulo': [(76, 24), (92, 34), (-60, -63), (60, -63), (-8, -37), (8, -37)],
 'pista': [(-104, -30), (104, -30), (-38, -52), (38, -52), (-30, 10), (30, 10), (-40, 50), (40, 50)],
 'sentinela': [(-80, -18), (0, -8), (80, -18), (-50, -49), (0, 30), (50, -49)],
 'oratorio': [(-104, 0), (104, 0), (-20, -56), (20, -56), (-56, 40), (56, 40), (-50, 0), (50, 0)],
 'reagente': [(-96, 26), (96, 26), (-30, -63), (30, -63), (-70, -52), (70, -52), (-96, -30), (96, -30), (-78, -24), (78, -24)],
 'loja': [(-80, 56), (80, 56), (0, 12)]}
for k, pts in PONTOS.items():
    for x, z in pts:
        i, j = cel(x, z)
        if not livre[j*nx+i]: print('PONTO BLOQUEADO', k, x, z)
json.dump({'pontos': PONTOS, 'espacos': E, 'vaos': V, 'altares': ALT, 'spawn': SPAWN, 'quadros': QUADROS, 'dist': M,
           'spawnDist': {t: [em(DS[t], x, z) for _, x, z in ALT] for t in 'HC'}}, open(sys.argv[1] if len(sys.argv) > 1 else 'asilo.json', 'w'), ensure_ascii=False, indent=1)
