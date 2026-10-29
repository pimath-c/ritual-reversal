#!/usr/bin/env python3
# Junta as fotos de referência do Unity (F10, PartidaReferencia.cs) com as do navegador (tools/referencia-navegador.js):
# para cada vista, navegador à esquerda e Unity à direita, na mesma altura, e uma folha com todas em miniatura.
# Uso: python3 -I tools/comparar-referencia.py <pasta do Unity> [pasta do navegador] [saída]
# (padrão: dist/referencia/navegador e dist/referencia/lado-a-lado; precisa do Pillow)
import os, sys
from PIL import Image, ImageDraw, ImageFont

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
uni = sys.argv[1]
nav = sys.argv[2] if len(sys.argv) > 2 else os.path.join(RAIZ, 'dist', 'referencia', 'navegador')
saida = sys.argv[3] if len(sys.argv) > 3 else os.path.join(RAIZ, 'dist', 'referencia', 'lado-a-lado')
os.makedirs(saida, exist_ok=True)
fonte_arq = os.path.join(RAIZ, 'unity', 'Assets', 'RitualReversal', 'Resources', 'Fontes', 'DejaVuSans.ttf')
fonte = ImageFont.truetype(fonte_arq, 22) if os.path.exists(fonte_arq) else ImageFont.load_default()

# o Unity grava "01-acampamento.png"; o navegador também. Casa pelo nome da vista (sem o número)
def por_vista(pasta):
    return {f.split('-', 1)[1]: os.path.join(pasta, f) for f in sorted(os.listdir(pasta)) if f.endswith('.png') and '-' in f}

U, N = por_vista(uni), por_vista(nav)
comuns = [v for v in N if v in U]
if not comuns:
    sys.exit('nenhuma vista em comum entre ' + uni + ' e ' + nav)
miniaturas = []
for i, v in enumerate(comuns):
    a = Image.open(N[v]).convert('RGB'); b = Image.open(U[v]).convert('RGB')
    h = min(a.height, b.height, 1080)
    a = a.resize((round(a.width * h / a.height), h), Image.LANCZOS); b = b.resize((round(b.width * h / b.height), h), Image.LANCZOS)
    c = Image.new('RGB', (a.width + b.width + 8, h + 40), (17, 17, 17)); c.paste(a, (0, 40)); c.paste(b, (a.width + 8, 40))
    d = ImageDraw.Draw(c); nome = v[:-4]
    d.text((10, 8), nome + ': navegador', fill=(220, 220, 220), font=fonte); d.text((a.width + 18, 8), nome + ': Unity', fill=(220, 220, 220), font=fonte)
    arq = os.path.join(saida, '%02d-%s' % (i + 1, v)); c.save(arq); miniaturas.append(c)
    print('lado a lado:', os.path.basename(arq))
# folha: duas colunas de miniaturas
lw = 960; mini = [m.resize((lw, round(m.height * lw / m.width)), Image.LANCZOS) for m in miniaturas]
mh = max(m.height for m in mini); linhas = (len(mini) + 1) // 2
folha = Image.new('RGB', (lw * 2 + 8, linhas * (mh + 8)), (17, 17, 17))
for i, m in enumerate(mini): folha.paste(m, ((i % 2) * (lw + 8), (i // 2) * (mh + 8)))
folha.save(os.path.join(saida, 'folha.jpg'), quality=85)
print('%d vistas; folha em %s' % (len(comuns), os.path.join(saida, 'folha.jpg')))
faltam = [v for v in N if v not in U]
if faltam: print('sem foto do Unity:', ', '.join(f[:-4] for f in faltam))
