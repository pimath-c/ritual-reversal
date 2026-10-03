#!/usr/bin/env node
// Monta a versão de arquivo único (a publicada no claude.ai): client/index.html com shared/sim.js embutido.
// Uso: node tools/build.js [saida]      (padrão: dist/ritual-reversal.html)
'use strict';
const fs=require('fs'), path=require('path');
const ROOT=path.join(__dirname,'..');
const out=process.argv[2]||path.join(ROOT,'dist','ritual-reversal.html');
const cli=fs.readFileSync(path.join(ROOT,'client','index.html'),'utf8');
const sim=fs.readFileSync(path.join(ROOT,'shared','sim.js'),'utf8');
const tag='<script src="sim.js"></script>';
if(!cli.includes(tag)) throw new Error('client/index.html não tem '+tag);
fs.mkdirSync(path.dirname(out),{recursive:true});
fs.writeFileSync(out,cli.replace(tag,()=>'<script>\n'+sim+'\n</script>'));
console.log('gerado',out,(fs.statSync(out).size/1024).toFixed(0)+' KB');
