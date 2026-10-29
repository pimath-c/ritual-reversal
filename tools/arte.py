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
        if f.get('polyhaven'):
            mod.append(f"| `{n}` | {f['uso']} | [{f['polyhaven']}](https://polyhaven.com/a/{f['polyhaven']}), CC0 | {f['autor']} |")
        else:
            mod.append(f"| `{n}` | {f.get('uso', gerados.get(n, ''))} | Higgsfield/Tripo (gerado) | — |")
    s = open(DOC, encoding='utf-8').read()
    s = re.sub(r'(## Texturas[^\n]*\n\n)(\|.*\n)+', lambda m: m.group(1) + '\n'.join(tex) + '\n', s, count=1)
    s = re.sub(r'(## Modelos[^\n]*\n\n)(\|.*\n)+', lambda m: m.group(1) + '\n'.join(mod) + '\n', s, count=1)
    open(DOC, 'w', encoding='utf-8').write(s)
    print(f'docs/ARTE.md: {len(T)} texturas, {len(mod) - 2} modelos')


if __name__ == '__main__':
    main()
