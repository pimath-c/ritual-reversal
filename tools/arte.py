#!/usr/bin/env python3
# Refaz as tabelas de docs/ARTE.md (créditos de cada textura e modelo) a partir de assets/texturas/fontes.json e
# assets/modelos/fontes.json. O texto antes da primeira tabela e depois da última fica como está.
# Uso: python3 tools/arte.py
import json, os, re

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOC = os.path.join(RAIZ, 'docs', 'ARTE.md')


def ler(*p): return json.load(open(os.path.join(RAIZ, *p), encoding='utf-8'))


def main():
    T, M = ler('assets', 'texturas', 'fontes.json'), ler('assets', 'modelos', 'fontes.json')
    tex = ['| No jogo | Onde aparece | Textura | Autor |', '| --- | --- | --- | --- |']
    for n, f in T.items():
        tex.append(f"| `{n}` | {f['uso']} | [{f['id']}]({f['pagina']}) | {f['autor']} |")
    mod = ['| No jogo | Onde aparece | Origem | Autor |', '| --- | --- | --- | --- |']
    gerados = {'altar': 'altar dos rituais', 'estatua': 'estátuas do monge'}
    for n, f in M.items():
        if not isinstance(f, dict):
            continue
        if f.get('itch'):
            mod.append(f"| `{n}` | {f['uso']} | [{f['itch']['pagina'].split('/')[2].split('.')[0]} (itch.io)]({f['itch']['pagina']}), {f['licenca']} | {f['autor']} |")
        elif f.get('polyhaven'):
            mod.append(f"| `{n}` | {f['uso']} | [{f['polyhaven']}](https://polyhaven.com/a/{f['polyhaven']}), CC0 | {f['autor']} |")
        else:
            mod.append(f"| `{n}` | {f.get('uso', gerados.get(n, ''))} | Higgsfield/Tripo (gerado) | — |")
    bon = ['| No jogo | Classe | Origem | Autor |', '| --- | --- | --- | --- |']
    try:
        B = ler('assets', 'bonecos', 'fontes.json')
        for n, f in B['bonecos'].items():
            if f.get('creditos'):   # v6.5c: MakeHuman, peça por peça ("peça: autor, licença")
                bon.append(f"| `{n}` | {f['uso']} | [MakeHuman](https://static.makehumancommunity.org/assets/assetpacks/index.html) | " + '; '.join(f['creditos']) + ' |'); continue
            pac = sorted({p[0] for p in f['pecas']})
            bon.append(f"| `{n}` | {f['uso']} | " + ', '.join(f"[{B['fontes'][k]['itch'].rsplit('/', 1)[1]}]({B['fontes'][k]['itch']}), {B['fontes'][k]['licenca']}" for k in pac) + f" | {B['fontes'][pac[0]]['autor']} |")
    except FileNotFoundError:
        bon = []
    arm = ['| No jogo | Onde aparece | Origem | Autor |', '| --- | --- | --- | --- |']
    try:
        A = ler('assets', 'armas', 'fontes.json')
        for n, f in A['armas'].items(): arm.append(f"| `{n}` | {f['uso']} | [{f['pagina'].rsplit('/', 1)[1]}]({f['pagina']}), {f['licenca']} | {f['autor']} |")
    except FileNotFoundError:
        arm = []
    s = open(DOC, encoding='utf-8').read()
    s = re.sub(r'(## Texturas[^\n]*\n\n)(\|.*\n)+', lambda m: m.group(1) + '\n'.join(tex) + '\n', s, count=1)
    s = re.sub(r'(## Modelos[^\n]*\n\n)(\|.*\n)+', lambda m: m.group(1) + '\n'.join(mod) + '\n', s, count=1)
    if bon:
        if '## Bonecos' not in s:
            s = s.replace('\nNos dois clientes, a foto', '\n## Bonecos\n\nPersonagens em peças rígidas por junta (`tools/montar-bonecos.py` e `tools/makehuman.py`, `assets/bonecos/fontes.json`).\n\n' + '\n'.join(bon) + '\n\nNos dois clientes, a foto', 1)
        else:
            s = re.sub(r'(## Bonecos[^\n]*\n\n[^\n|]*\n\n)(\|.*\n)+', lambda m: m.group(1) + '\n'.join(bon) + '\n', s, count=1)
    if arm:
        if '## Armas' not in s:
            s = s.replace('\nNos dois clientes, a foto', '\n## Armas\n\nA forma do modelo no quadro da arma do jogo, pintada com os materiais por papel (`tools/montar-armas.py`, `assets/armas/fontes.json`).\n\n' + '\n'.join(arm) + '\n\nNos dois clientes, a foto', 1)
        else:
            s = re.sub(r'(## Armas[^\n]*\n\n[^\n|]*\n\n)(\|.*\n)+', lambda m: m.group(1) + '\n'.join(arm) + '\n', s, count=1)
    open(DOC, 'w', encoding='utf-8').write(s)
    print(f'docs/ARTE.md: {len(T)} texturas, {len(mod) - 2} modelos, {max(0, len(bon) - 2)} bonecos')


if __name__ == '__main__':
    main()
