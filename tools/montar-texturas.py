#!/usr/bin/env python3
# Texturas fotográficas (escaneadas, CC0, Poly Haven) no lugar das texturas que o jogo gerava por código.
# assets/texturas/fontes.json diz qual textura do Poly Haven vai em cada nome do jogo (TEX.floor, TEX.wall, TEX.reboco...),
# com a página, a licença e o autor. Baixa os originais de 1K (cor, relevo no padrão OpenGL e rugosidade) para
# assets/texturas/fontes/ (fora do git: o .gitignore deixa só este script e o fontes.json), reduz ao tamanho do jogo
# e grava a mesma imagem para os dois clientes:
#   - client/texturas.js: window.RR_TEXTURAS = {nome: {cor, nor, rug}} em JPEG base64 (embutido por build.js e
#     build-mobile.js e servido pelo servidor); o index.html troca a textura gerada pela foto (pbrReal);
#   - unity/Assets/RitualReversal/Resources/Texturas/<nome>_cor.bytes, _nor.bytes e _rug.bytes (o Texturas.cs lê).
# Tamanhos: cor 1024 px (512 com PEQUENO=1), relevo 512, rugosidade 256 (cinza). "cinza": true tira a cor da foto
# (a cor vem do material do jogo); "media" é a cor média (sRGB, 0–255) da textura que o jogo gerava, e a foto é
# ajustada para ela, canal a canal (os materiais foram calibrados para esse brilho). Sem os originais e sem rede, mantém o que já está gravado.
# Uso: python3 tools/montar-texturas.py        (precisa de Pillow; baixar precisa de api.polyhaven.com liberado)
import base64, io, json, os, sys, urllib.request
from PIL import Image

# o Poly Haven recusa o User-Agent padrão do urllib (403)
def pedido(url): return urllib.request.Request(url, headers={'User-Agent': 'RitualReversal/1.0 (ferramenta de montagem)'})

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PASTA = os.path.join(RAIZ, 'assets', 'texturas')
ORIG = os.path.join(PASTA, 'fontes')
UNITY = os.path.join(RAIZ, 'unity', 'Assets', 'RitualReversal', 'Resources', 'Texturas')
TAM = {'cor': 512 if os.environ.get('PEQUENO') else 1024, 'nor': 512, 'rug': 256}
QUAL = {'cor': 80, 'nor': 88, 'rug': 82}
RELEVO = 0.5
MAPAS = {'cor': ['Diffuse', 'diff'], 'nor': ['nor_gl'], 'rug': ['Rough', 'rough']}


def baixar_itch(f):
    """v6.5d: textura de um pacote do itch.io (CC0): "itch" = {"pagina", "arquivo" (trecho do nome do zip), "cor", "nor", "rug"
    (arquivos dentro do zip)}. O zip fica em fontes/ e os três mapas viram fontes/<id>_<mapa>.jpg, como os do Poly Haven."""
    import importlib.util, zipfile
    i, it = f['id'], f['itch']
    if all(os.path.exists(os.path.join(ORIG, f'{i}_{p}.jpg')) for p in MAPAS): return
    z = os.path.join(ORIG, it['arquivo'] + '.zip')
    if not os.path.exists(z):
        sp = importlib.util.spec_from_file_location('montar_bonecos', os.path.join(RAIZ, 'tools', 'montar-bonecos.py'))
        mb = importlib.util.module_from_spec(sp); sp.loader.exec_module(mb); mb.baixar_itch(it['pagina'], it['arquivo'], z)
    Z = zipfile.ZipFile(z)
    for p in MAPAS:
        nome = next(n for n in Z.namelist() if n.endswith(it[p]))
        Image.open(io.BytesIO(Z.read(nome))).convert('RGB').save(os.path.join(ORIG, f'{i}_{p}.jpg'), 'JPEG', quality=95)


def folhas_do_atlas(caminho_zip):
    """As folhas soltas de um atlas do ambientCG (cor + opacidade): componentes ligadas da máscara, cada uma recortada em RGBA."""
    import zipfile
    import numpy as np
    Z = zipfile.ZipFile(caminho_zip); nomes = Z.namelist()
    cor = Image.open(io.BytesIO(Z.read(next(n for n in nomes if n.endswith('_Color.png'))))).convert('RGB')
    alfa = Image.open(io.BytesIO(Z.read(next(n for n in nomes if n.endswith('_Opacity.png'))))).convert('L').resize(cor.size)
    W, H = cor.size; k = 8; m = np.array(alfa.resize((W // k, H // k))) > 60; lab = np.zeros(m.shape, dtype=np.int32); n = 0; caixas = []
    for y0 in range(m.shape[0]):
        for x0 in range(m.shape[1]):
            if not m[y0, x0] or lab[y0, x0]: continue
            n += 1; pilha = [(y0, x0)]; lab[y0, x0] = n; x1 = x2 = x0; y1 = y2 = y0
            while pilha:
                y, x = pilha.pop(); x1, x2, y1, y2 = min(x1, x), max(x2, x), min(y1, y), max(y2, y)
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    a, b = y + dy, x + dx
                    if 0 <= a < m.shape[0] and 0 <= b < m.shape[1] and m[a, b] and not lab[a, b]: lab[a, b] = n; pilha.append((a, b))
            if (x2 - x1) * (y2 - y1) >= 20: caixas.append(((x1 - 1) * k, (y1 - 1) * k, (x2 + 2) * k, (y2 + 2) * k))
    rgba = cor.copy(); rgba.putalpha(alfa)
    return [rgba.crop((max(0, a), max(0, b), min(W, c), min(H, d))) for a, b, c, d in caixas]


def compor_folhagem(f, destino):
    """v6.5f: cartão de ramagem para as copas e moitas (RGBA, 512 px): galhinhos escuros saindo de baixo e folhas fotografadas
    (atlas CC0 do ambientCG, "folhagem": {"ambientcg": [ids], "pesos": [...], "folhas": N, "semente": s, "tom": [r, g, b]})
    presas ao longo deles, mais densas no meio. Tudo determinístico pela semente."""
    import math, random
    F = f['folhagem']; rnd = random.Random(F.get('semente', 1)); grupos = []
    for a in F['ambientcg']:
        z = os.path.join(ORIG, a + '_1K-PNG.zip')
        if not os.path.exists(z):
            with urllib.request.urlopen(pedido(f'https://ambientcg.com/get?file={a}_1K-PNG.zip'), timeout=180) as r, open(z, 'wb') as o: o.write(r.read())
        grupos.append(folhas_do_atlas(z))
    T = 1024; P = 300; img = Image.new('RGBA', (T + 2 * P, T + 2 * P), (0, 0, 0, 0)); from PIL import ImageDraw; d = ImageDraw.Draw(img)   # margem: folha que passa da borda é cortada no fim
    # galhos: um tronco fino que sobe de baixo e ramos que abrem para os lados; as folhas ficam presas nos pontos deles
    pontos = []
    def ramo(x, y, ang, comp, larg, nivel):
        passos = 9; px, py = x, y
        for i in range(passos):
            ang += rnd.uniform(-.12, .12); nx, ny = px + math.cos(ang) * comp / passos, py - math.sin(ang) * comp / passos
            d.line([(px, py), (nx, ny)], fill=(46, 36, 28, 255), width=max(2, int(larg * (1 - i / passos * .6)))); px, py = nx, ny
            if i >= 2 and math.hypot(px - P - T / 2, py - P - T * .46) < T * .4: pontos.append((px, py, ang, nivel))   # só no maço redondo de cima
            if nivel < 2 and i in (2, 3, 4, 5, 6, 7): ramo(px, py, ang + (1 if (i + nivel) % 2 else -1) * rnd.uniform(.45, 1.0), comp * rnd.uniform(.32, .5), larg * .6, nivel + 1)
    ramo(P + T / 2 + rnd.uniform(-20, 20), P + T - 8, math.pi / 2 + rnd.uniform(-.1, .1), T * .8, 12, 0)
    pesos = F.get('pesos') or [1] * len(grupos); tom = F.get('tom', [.55, .62, .5])
    for k in range(F.get('folhas', 90)):
        x, y, ang, nivel = pontos[rnd.randrange(len(pontos))]
        g = rnd.choices(grupos, weights=pesos)[0]; folha = g[rnd.randrange(len(g))]
        alt = T * rnd.uniform(.085, .13); esc = alt / max(folha.size); folha = folha.resize((max(2, int(folha.size[0] * esc)), max(2, int(folha.size[1] * esc))), Image.LANCZOS)
        # a folha do atlas aponta para cima (pecíolo embaixo): gira para sair do galho, para um lado ou para o outro
        giro = math.degrees(ang - math.pi / 2) + rnd.choice([-1, 1]) * rnd.uniform(25, 70)
        r, gg, b, a = folha.split(); v = rnd.uniform(.8, 1.1)
        folha = Image.merge('RGBA', [c.point(lambda q, m=tom[i] * v: min(255, int(q * m))) for i, c in enumerate((r, gg, b))] + [a]).rotate(giro, Image.BICUBIC, expand=True)
        ox, oy = x - folha.size[0] / 2 + math.cos(math.radians(giro + 90)) * alt * .45, y - folha.size[1] / 2 - math.sin(math.radians(giro + 90)) * alt * .45
        img.alpha_composite(folha, (max(0, int(ox)), max(0, int(oy))))
    img.crop((P, P, P + T, P + T)).save(destino)


def baixar(i):
    os.makedirs(ORIG, exist_ok=True)
    falta = [p for p in MAPAS if not os.path.exists(os.path.join(ORIG, f'{i}_{p}.jpg'))]
    if not falta:
        return
    with urllib.request.urlopen(pedido(f'https://api.polyhaven.com/files/{i}'), timeout=60) as r:
        d = json.load(r)
    for p in falta:
        url = next((d[n]['1k']['jpg']['url'] for n in MAPAS[p] if n in d and '1k' in d[n] and 'jpg' in d[n]['1k']), None)
        if not url:
            raise ValueError(f'{i}: sem o mapa {p} em 1K')
        with urllib.request.urlopen(pedido(url), timeout=120) as r, open(os.path.join(ORIG, f'{i}_{p}.jpg'), 'wb') as f:
            f.write(r.read())


def reduzir(caminho, p, cinza, media=None):
    im = Image.open(caminho)
    im = im.convert('L') if p == 'rug' or cinza else im.convert('RGB')
    if p == 'nor':
        # relevo mais suave que o da foto (a luz em faixas do estilo ilustrado exagera o relevo): x e y pela metade
        im = im.convert('RGB'); px = im.load(); w, h = im.size
        for y in range(h):
            for x in range(w):
                r, g, b = px[x, y]; nx, ny = (r / 127.5 - 1) * RELEVO, (g / 127.5 - 1) * RELEVO; nz = max(0.0, 1 - nx * nx - ny * ny) ** .5
                px[x, y] = (round((nx + 1) * 127.5), round((ny + 1) * 127.5), round((nz + 1) * 127.5))
    im = im.resize((TAM[p], TAM[p]), Image.LANCZOS)
    if p == 'cor' and media:
        # a cor média da textura que o jogo gerava (o brilho com que os materiais foram calibrados); a foto mantém o
        # detalhe e passa a ter a mesma média em cada canal
        im = im.convert('RGB'); atual = [max(1.0, sum(i * c for i, c in enumerate(h)) / max(1, sum(h))) for h in (im.histogram()[k * 256:(k + 1) * 256] for k in range(3))]
        im = Image.merge('RGB', [ch.point(lambda v, f=media[k] / atual[k]: min(255, round(v * f))) for k, ch in enumerate(im.split())])
    b = io.BytesIO(); im.save(b, 'JPEG', quality=QUAL[p], optimize=True, progressive=False)
    return b.getvalue()


def main():
    F = json.load(open(os.path.join(PASTA, 'fontes.json'), encoding='utf-8'))
    os.makedirs(UNITY, exist_ok=True)
    js, total = {}, 0
    for nome, f in F.items():
        try:
            os.makedirs(ORIG, exist_ok=True)
            if f.get('folhagem'):
                if not os.path.exists(os.path.join(ORIG, f"{f['id']}_cor.png")): compor_folhagem(f, os.path.join(ORIG, f"{f['id']}_cor.png"))
            elif f.get('itch'): baixar_itch(f)
            else: baixar(f['id'])
        except Exception as e:
            print(f'{nome}: não baixou ({e}); fica o que já está gravado', file=sys.stderr)
        js[nome] = {}
        for p in MAPAS:
            orig = os.path.join(ORIG, f"{f['id']}_{p}.jpg"); saida = os.path.join(UNITY, f'{nome}_{p}.bytes')
            if f.get('folhagem'):   # v6.5f: ramagem com alfa: só a cor, em PNG
                orig = os.path.join(ORIG, f"{f['id']}_cor.png")
                if p != 'cor': continue
            if f.get('folhagem') and os.path.exists(orig):
                b = io.BytesIO(); Image.open(orig).convert('RGBA').resize((f.get('tam', 512),) * 2, Image.LANCZOS).save(b, 'PNG', optimize=True); dado = b.getvalue(); open(saida, 'wb').write(dado)
            elif os.path.exists(orig):
                dado = reduzir(orig, p, f.get('cinza') and p == 'cor', f.get('media'))
                open(saida, 'wb').write(dado)
            elif os.path.exists(saida):
                dado = open(saida, 'rb').read()
            else:
                continue
            js[nome][p] = base64.b64encode(dado).decode('ascii'); total += len(dado)
        print(f"{nome:9s} {f['id']:20s} " + ' '.join(f'{p} {len(js[nome].get(p, ""))*3//4//1024} KB' for p in MAPAS))
    cab = ('/* Texturas fotográficas CC0 (Poly Haven e itch.io), geradas por tools/montar-texturas.py a partir de assets/texturas/fontes.json.\n'
           '   Não edite à mão. Autores: ' + '; '.join(f"{n}: {f['id']} ({f['autor']})" for n, f in F.items()) + '. */\n')
    with open(os.path.join(RAIZ, 'client', 'texturas.js'), 'w', encoding='utf-8') as o:
        o.write(cab + 'window.RR_TEXTURAS=' + json.dumps(js, separators=(',', ':')) + ';\n')
    print(f'total {total/1024/1024:.2f} MB em {len(js)} texturas')


if __name__ == '__main__':
    main()
