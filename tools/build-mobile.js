#!/usr/bin/env node
// Monta mobile/www para o app Android (Capacitor): o mesmo cliente do navegador, com shared/sim.js embutido e
// o three.js copiado para dentro do app (joga contra bots sem internet). As fontes do Google continuam por link:
// sem internet o jogo usa as fontes do sistema.
// Uso: node tools/build-mobile.js   (depois de "npm install" em mobile/)
'use strict';
const fs=require('fs'), path=require('path');
const ROOT=path.join(__dirname,'..'), WWW=path.join(ROOT,'mobile','www'), THREE=path.join(ROOT,'mobile','node_modules','three');
const CDN='https://cdn.jsdelivr.net/npm/three@0.128.0/';
if(!fs.existsSync(THREE)) throw new Error('rode "npm install" dentro de mobile/ primeiro');
let html=fs.readFileSync(path.join(ROOT,'client','index.html'),'utf8');
const sim=fs.readFileSync(path.join(ROOT,'shared','sim.js'),'utf8');
const tag='<script src="sim.js"></script>';
if(!html.includes(tag)) throw new Error('client/index.html não tem '+tag);
fs.rmSync(WWW,{recursive:true,force:true}); fs.mkdirSync(WWW,{recursive:true});
const usados=[...new Set([...html.matchAll(/https:\/\/cdn\.jsdelivr\.net\/npm\/three@0\.128\.0\/([\w\/.-]+\.js)/g)].map(m=>m[1]))];
for(const rel of usados){ const de=path.join(THREE,rel), para=path.join(WWW,'vendor','three',rel);
  if(!fs.existsSync(de)) throw new Error('three.js não tem '+rel); fs.mkdirSync(path.dirname(para),{recursive:true}); fs.copyFileSync(de,para); }
html=html.split(CDN).join('vendor/three/')
  .replace(tag,()=>'<script>\n'+sim+'\n</script>')
  .replace('<head>','<head>\n<script>window.RR_APP=true;</script>');
if(!html.includes('window.RR_APP=true')) throw new Error('não achei <head> para marcar o app');
fs.writeFileSync(path.join(WWW,'index.html'),html);
console.log(`mobile/www: index.html ${(fs.statSync(path.join(WWW,'index.html')).size/1024).toFixed(0)} KB + ${usados.length} arquivos do three.js`);
