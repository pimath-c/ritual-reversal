#!/usr/bin/env python3
# Fotos das telas do Unity sem o Unity: lê o que o IMGUI de mentira gravou (tools/cs/IMGUIFalso.cs, com RR_FOTOS=<pasta>
# no tools/unity-telas.js) e desenha cada tela num PNG do tamanho da tela: retângulos e texturas tingidos com o GUI.color,
# fundos em nove partes (o border do GUIStyle), corridas de texto já quebradas em linhas pelo C# com as fontes de verdade
# (negrito e itálico sintéticos quando a fonte tem um peso só, como o Unity faz), a matriz do GUI (escala e giros) e o
# recorte de cada grupo. Não há mundo 3D atrás: o fundo é escuro e liso.
# Uso: python3 -I tools/foto-imgui.py <pasta> [nome ...]   (grava <pasta>/<nome>.png)
import sys, os, json, struct, math
from PIL import Image, ImageDraw, ImageFont
import numpy as np

FUNDO = (12, 14, 18, 255)
Q = 4   # superamostragem do que vai girado


class Pintor:
    def __init__(self, pasta):
        self.pasta = pasta
        self.tex = {}
        self.fontes = {}
        self.cache = {}

    def textura(self, tid):
        if tid not in self.tex:
            if tid == 'w':
                self.tex[tid] = np.full((1, 1, 4), 255, np.float32)
            else:
                with open(os.path.join(self.pasta, 'tex-' + tid + '.rgba'), 'rb') as f:
                    b = f.read()
                w, h = struct.unpack('<ii', b[:8])
                self.tex[tid] = np.frombuffer(b[8:8 + w * h * 4], np.uint8).reshape(h, w, 4).astype(np.float32)
        return self.tex[tid]

    def fonte(self, arq, tam):
        k = (arq, tam)
        if k not in self.fontes:
            try:
                self.fontes[k] = ImageFont.truetype(arq, tam)
            except Exception:
                self.fontes[k] = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', tam)
        return self.fontes[k]

    # a textura no tamanho de destino, em nove partes quando o estilo tem border
    def esticada(self, tid, W, H, borda, cor, fit):
        k = (tid, W, H, tuple(borda) if borda else None, tuple(cor), fit)
        if k in self.cache:
            return self.cache[k]
        a = self.textura(tid)
        h, w = a.shape[:2]
        img = Image.fromarray(a.astype(np.uint8), 'RGBA')
        if borda and (W > 0 and H > 0):
            l, r, t, b = borda
            l, r, t, b = min(l, w), min(r, w - min(l, w)), min(t, h), min(b, h - min(t, h))
            fx = min(1.0, W / max(1, l + r)) if l + r > W else 1.0
            fy = min(1.0, H / max(1, t + b)) if t + b > H else 1.0
            dl, dr, dt, db = int(round(l * fx)), int(round(r * fx)), int(round(t * fy)), int(round(b * fy))
            out = Image.new('RGBA', (W, H))
            xs, xd = [0, l, w - r, w], [0, dl, W - dr, W]
            ys, yd = [0, t, h - b, h], [0, dt, H - db, H]
            for i in range(3):
                for j in range(3):
                    sw, sh = xs[i + 1] - xs[i], ys[j + 1] - ys[j]
                    dw, dh = xd[i + 1] - xd[i], yd[j + 1] - yd[j]
                    if sw <= 0 or sh <= 0 or dw <= 0 or dh <= 0:
                        continue
                    out.paste(img.crop((xs[i], ys[j], xs[i + 1], ys[j + 1])).resize((dw, dh), Image.BILINEAR), (xd[i], yd[j]))
            img = out
        elif fit:
            k2 = min(W / w, H / h)
            iw, ih = max(1, int(round(w * k2))), max(1, int(round(h * k2)))
            out = Image.new('RGBA', (W, H))
            out.paste(img.resize((iw, ih), Image.BILINEAR), ((W - iw) // 2, (H - ih) // 2))
            img = out
        else:
            img = img.resize((W, H), Image.BILINEAR) if (w, h) != (W, H) else img
        arr = np.asarray(img).astype(np.float32) * np.array(cor, np.float32).reshape(1, 1, 4)
        img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), 'RGBA')
        if len(self.cache) > 4000:
            self.cache.clear()
        self.cache[k] = img
        return img

    # põe uma imagem que cobre o retângulo r (no espaço antes da matriz) na tela, girada se a matriz girar
    def colocar(self, tela, img, r, m, cl, s):
        a, b, tx, c, d, ty = m
        x, y, w, h = r
        X0, Y0 = a * x + b * y + tx, c * x + d * y + ty
        if abs(b) < 1e-6 and abs(c) < 1e-6:
            bx, by = int(round(X0)), int(round(Y0))
        else:
            # pixel (u, v) da imagem -> deslocamento na tela a partir de (X0, Y0); a imagem pode ter sido arredondada (ou
            # superamostrada), então a escala vem do tamanho dela e não de s
            W, H = img.size
            A = np.array([[a * w / W, b * h / H], [c * w / W, d * h / H]])
            cant = [A @ np.array(p) for p in ((0, 0), (W, 0), (0, H), (W, H))]
            bx, by = int(math.floor(X0 + min(p[0] for p in cant))) - 1, int(math.floor(Y0 + min(p[1] for p in cant))) - 1
            bw, bh = int(math.ceil(X0 + max(p[0] for p in cant))) - bx + 2, int(math.ceil(Y0 + max(p[1] for p in cant))) - by + 2
            Ai = np.linalg.inv(A)
            # (X, Y) da caixa, em 1/q de pixel -> (u, v) da imagem; a caixa começa no pixel inteiro (bx, by), sem perder a fração
            q = Q
            co = (Ai[0, 0] / q, Ai[0, 1] / q, Ai[0, 0] * (bx - X0) + Ai[0, 1] * (by - Y0), Ai[1, 0] / q, Ai[1, 1] / q, Ai[1, 0] * (bx - X0) + Ai[1, 1] * (by - Y0))
            pm = np.asarray(img, np.float32); pm[..., :3] *= pm[..., 3:4] / 255
            g = Image.fromarray(np.clip(pm, 0, 255).astype(np.uint8), 'RGBA').transform((bw * q, bh * q), Image.AFFINE, co, resample=Image.BILINEAR)
            # média de q × q (a cobertura de cada pixel), com a cor pré-multiplicada para a borda não escurecer
            g = np.asarray(g, np.float32).reshape(bh, q, bw, q, 4).mean(axis=(1, 3))
            al = g[..., 3:4]; g[..., :3] = np.where(al > 0, g[..., :3] * 255 / np.maximum(al, 1e-6), 0)
            img = Image.fromarray(np.clip(np.round(g), 0, 255).astype(np.uint8), 'RGBA')
        W, H = img.size
        cx0, cy0, cx1, cy1 = cl[0], cl[1], cl[0] + cl[2], cl[1] + cl[3]
        x0, y0, x1, y1 = max(bx, int(math.floor(cx0))), max(by, int(math.floor(cy0))), min(bx + W, int(math.ceil(cx1)), tela.size[0]), min(by + H, int(math.ceil(cy1)), tela.size[1])
        x0, y0 = max(x0, 0), max(y0, 0)
        if x1 <= x0 or y1 <= y0:
            return
        tela.alpha_composite(img, (x0, y0), (x0 - bx, y0 - by, x1 - bx, y1 - by))

    def textura_cmd(self, tela, k):
        m = k['m']
        s = math.hypot(m[0], m[3]) or 1
        x, y, w, h = k['r']
        W, H = max(1, int(round(w * s))), max(1, int(round(h * s)))
        if w <= 0 or h <= 0:
            return
        cor = [k['c'][0], k['c'][1], k['c'][2], k['c'][3]]
        if abs(m[1]) > 1e-6 or abs(m[3]) > 1e-6:
            # girada: a textura em Q vezes o tamanho, para as bordas finas (1,5 px) não engrossarem no arredondamento
            W, H = max(1, int(round(w * s * Q))), max(1, int(round(h * s * Q)))
            img = self.esticada(k['tex'], W, H, [int(round(v * s * Q)) for v in k['b']] if 'b' in k else None, cor, 'fit' in k)
        else:
            img = self.esticada(k['tex'], W, H, [int(round(v * s)) for v in k['b']] if 'b' in k else None, cor, 'fit' in k)
        self.colocar(tela, img, (x, y, w, h), m, k['cl'], s)

    def texto_cmd(self, tela, k):
        m = k['m']
        s = math.hypot(m[0], m[3]) or 1
        tam = max(1, int(round(k['z'] * s)))
        f = self.fonte(k['f'], tam)
        asc, desc = f.getmetrics()
        larg = int(math.ceil(max(f.getlength(k['s']), k['w'] * s))) + 4
        alt = asc + desc + 2
        inc = .2 if k.get('si') else 0
        extra = int(math.ceil(alt * inc))
        img = Image.new('RGBA', (larg + extra + 2, alt))
        dr = ImageDraw.Draw(img)
        c = k['c']
        cor = (int(round(c[0] * 255)), int(round(c[1] * 255)), int(round(c[2] * 255)), int(round(c[3] * 255)))
        # negrito sintético como o do FreeType (embolden de ~1/24 do tamanho): a letra de novo, um pouco à direita
        sb = 0
        extra_b = max(.5, tam / 24) if k.get('sb') else 0
        if 'a' in k and len(k['a']) == len(k['s']):
            # cada letra no avanço que o C# calculou (arredondado por letra, como o FreeType com hinting do Unity)
            xx = 1.0
            for ch, av in zip(k['s'], k['a']):
                if ch != ' ':
                    dr.text((xx, asc), ch, font=f, fill=cor, anchor='ls')
                    if extra_b:
                        dr.text((xx + extra_b, asc), ch, font=f, fill=cor, anchor='ls')
                xx += av * s
        else:
            dr.text((1, asc), k['s'], font=f, fill=cor, anchor='ls')
            if extra_b:
                dr.text((1 + extra_b, asc), k['s'], font=f, fill=cor, anchor='ls')
        if inc:
            W, H = img.size
            img = img.transform((W, H), Image.AFFINE, (1, inc, -inc * asc, 0, 1, 0), resample=Image.BILINEAR)
        x, y = k['x'] - 1 / s, k['y'] - asc / s
        self.colocar(tela, img, (x, y, img.size[0] / s, img.size[1] / s), m, k['cl'], s)

    def desenhar(self, arq, saida):
        with open(arq, encoding='utf-8') as f:
            d = json.load(f)
        tela = Image.new('RGBA', (d['w'], d['h']), FUNDO)
        for k in d['cmds']:
            if k['k'] == 't':
                self.textura_cmd(tela, k)
            elif k['k'] == 'x':
                self.texto_cmd(tela, k)
        tela.convert('RGB').save(saida)
        return len(d['cmds'])


def main():
    if len(sys.argv) < 2:
        print('uso: foto-imgui.py <pasta> [nome ...]')
        return 2
    pasta = sys.argv[1]
    nomes = sys.argv[2:] or sorted(n[:-5] for n in os.listdir(pasta) if n.endswith('.json'))
    p = Pintor(pasta)
    for n in nomes:
        q = p.desenhar(os.path.join(pasta, n + '.json'), os.path.join(pasta, n + '.png'))
        print('%s.png (%d comandos)' % (n, q))
    return 0


if __name__ == '__main__':
    sys.exit(main())
