# Desenha a planta do Asilo Blackwater (asilo.json) em SVG, com as cores por variáveis CSS da página.
import json, sys
d = json.load(open(sys.argv[1]))
K = 4.2; X0, X1, Z0, Z1 = -110, 110, -76, 66
W, H = (X1-X0)*K, (Z1-Z0)*K
sx = lambda x: (x-X0)*K; sz = lambda z: (z-Z0)*K
o = ['<svg viewBox="0 0 %.0f %.0f" class="planta" role="img" aria-label="Planta do Asilo Blackwater">' % (W, H)]
o.append('<defs><pattern id="agua" width="10" height="10" patternUnits="userSpaceOnUse" patternTransform="rotate(20)"><rect width="10" height="10" class="agua"/><path d="M0 5 Q2.5 3 5 5 T10 5" class="onda"/></pattern></defs>')
o.append('<rect x="0" y="0" width="%.0f" height="%.0f" class="parede"/>' % (W, H))
o.append('<rect x="0" y="0" width="%.0f" height="%.1f" fill="url(#agua)"/>' % (W, sz(-66)))
o.append('<text x="%.0f" y="%.0f" class="rot-lago">LAGO BLACKWATER</text>' % (sx(-80), sz(-69.5)))
for n, x1, x2, z1, z2, t in d['espacos']:
    o.append('<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" class="%s"/>' % (sx(x1), sz(z1), (x2-x1)*K, (z2-z1)*K, 'ext' if t == 'ext' else 'int'))
for x1, x2, z1, z2, p in d['vaos']:
    if not p: o.append('<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" class="vao"/>' % (sx(x1), sz(z1), (x2-x1)*K, (z2-z1)*K))
for x1, x2, z1, z2, p in d['vaos']:
    if p:
        if x2-x1 >= z2-z1: zc = (z1+z2)/2; o.append('<rect x="%.1f" y="%.1f" width="%.1f" height="5" class="porta"/>' % (sx(x1), sz(zc)-2.5, (x2-x1)*K))
        else: xc = (x1+x2)/2; o.append('<rect x="%.1f" y="%.1f" width="5" height="%.1f" class="porta"/>' % (sx(xc)-2.5, sz(z1), (z2-z1)*K))
# nomes das salas grandes
for n, x1, x2, z1, z2, t in d['espacos']:
    if n.startswith(('Corredor', 'Trilha', 'Margem', 'Píer', 'Escada')) or (x2-x1)*K < len(n)*6.4 + 6: continue
    if any(n == a[0] for a in d['altares']): continue   # o nome do altar já está lá
    cx, cz = (x1+x2)/2, (z1+z2)/2
    if n in ('Enfermaria', 'Isolamento'): cz = -26
    if n in ('Hidroterapia', 'Caldeiras', 'Capela'): cz = z1+4
    if n == 'Gramado': cz = 32
    if n == 'Saguão': cz = 19
    if n in ('Arquivo', 'Farmácia'): cz = -1.5
    if n in ('Horta', 'Cemitério'): cz = 28
    if n == 'Pátio Norte': cz = -55
    o.append('<text x="%.1f" y="%.1f" class="sala%s">%s</text>' % (sx(cx), sz(cz), ' ext-t' if t == 'ext' else '', n))
P = d.get('pontos', {})
SIMB = {'erva': ('◆', 'p-erva'), 'tumulo': ('✝', 'p-tum'), 'pista': ('?', 'p-pista'), 'sentinela': ('▲', 'p-sent'), 'oratorio': ('✚', 'p-ora'),
        'reagente': ('●', 'p-reag'), 'loja': ('$', 'p-loja')}
for k, pts in P.items():
    s, c = SIMB[k]
    for p in pts: o.append('<text x="%.1f" y="%.1f" class="pt %s">%s</text>' % (sx(p[0]), sz(p[1])+4, c, s))
for nome, x, z in d['quadros']:
    o.append('<g class="quadro"><rect x="%.1f" y="%.1f" width="16" height="16" rx="2"/><text x="%.1f" y="%.1f">⚡</text></g>' % (sx(x)-8, sz(z)-8, sx(x), sz(z)+5))
SET = {0: 'oeste', 3: 'oeste', 1: 'centro', 4: 'centro', 2: 'leste', 5: 'leste'}
for i, (n, x, z) in enumerate(d['altares']):
    o.append('<g class="altar s-%s"><circle cx="%.1f" cy="%.1f" r="%.1f"/><text x="%.1f" y="%.1f" class="num">%d</text><text x="%.1f" y="%.1f" class="nome-alt">%s</text></g>'
             % (SET[i], sx(x), sz(z), 3*K, sx(x), sz(z)+4.5, i+1, sx(x), sz(z)-3*K-5, n))
for t, (x, z) in d['spawn'].items():
    o.append('<g class="spawn sp-%s"><rect x="%.1f" y="%.1f" width="58" height="22" rx="11"/><text x="%.1f" y="%.1f">%s</text></g>' % (t, sx(x)-29, sz(z)-11, sx(x), sz(z)+5, 'caça' if t == 'H' else 'culto'))
o.append('</svg>')
open(sys.argv[2], 'w').write('\n'.join(o))
print('svg', len('\n'.join(o)))
