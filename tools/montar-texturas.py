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
            baixar(f['id'])
        except Exception as e:
            print(f'{nome}: não baixou ({e}); fica o que já está gravado', file=sys.stderr)
        js[nome] = {}
        for p in MAPAS:
            orig = os.path.join(ORIG, f"{f['id']}_{p}.jpg"); saida = os.path.join(UNITY, f'{nome}_{p}.bytes')
            if os.path.exists(orig):
                dado = reduzir(orig, p, f.get('cinza') and p == 'cor', f.get('media'))
                open(saida, 'wb').write(dado)
            elif os.path.exists(saida):
                dado = open(saida, 'rb').read()
            else:
                continue
            js[nome][p] = base64.b64encode(dado).decode('ascii'); total += len(dado)
        print(f"{nome:9s} {f['id']:20s} " + ' '.join(f'{p} {len(js[nome].get(p, ""))*3//4//1024} KB' for p in MAPAS))
    cab = ('/* Texturas fotográficas CC0 (Poly Haven), geradas por tools/montar-texturas.py a partir de assets/texturas/fontes.json.\n'
           '   Não edite à mão. Autores: ' + '; '.join(f"{n}: {f['id']} ({f['autor']})" for n, f in F.items()) + '. */\n')
    with open(os.path.join(RAIZ, 'client', 'texturas.js'), 'w', encoding='utf-8') as o:
        o.write(cab + 'window.RR_TEXTURAS=' + json.dumps(js, separators=(',', ':')) + ';\n')
    print(f'total {total/1024/1024:.2f} MB em {len(js)} texturas')


if __name__ == '__main__':
    main()
