#!/usr/bin/env node
// Monta a versão de arquivo único (a publicada no claude.ai): client/index.html com shared/sim.js, client/arma.js e client/sons.js
// (as gravações, ver tools/montar-sons.py) embutidos.
// Uso: node tools/build.js [saida]      (padrão: dist/ritual-reversal.html)
'use strict';
const fs=require('fs'), path=require('path');
const ROOT=path.join(__dirname,'..');
const out=process.argv[2]||path.join(ROOT,'dist','ritual-reversal.html');
const cli=fs.readFileSync(path.join(ROOT,'client','index.html'),'utf8');
const sim=fs.readFileSync(path.join(ROOT,'shared','sim.js'),'utf8');
const tag='<script src="sim.js"></script>';
if(!cli.includes(tag)) throw new Error('client/index.html não tem '+tag);
const tagSons='<script src="sons.js"></script>', sons=fs.readFileSync(path.join(ROOT,'client','sons.js'),'utf8');
const tagArma='<script src="arma.js"></script>', arma=fs.readFileSync(path.join(ROOT,'client','arma.js'),'utf8');
const tagMundo='<script src="mundo.js"></script>', mundo=fs.readFileSync(path.join(ROOT,'client','mundo.js'),'utf8');
const tagSkins='<script src="skins.js"></script>', skins=fs.readFileSync(path.join(ROOT,'client','skins.js'),'utf8');
if(!cli.includes(tagSkins)) throw new Error('client/index.html não tem '+tagSkins);
if(!cli.includes(tagMundo)) throw new Error('client/index.html não tem '+tagMundo);
if(!cli.includes(tagArma)) throw new Error('client/index.html não tem '+tagArma);
if(!cli.includes(tagSons)) throw new Error('client/index.html não tem '+tagSons);
fs.mkdirSync(path.dirname(out),{recursive:true});
fs.writeFileSync(out,cli.replace(tag,()=>'<script>\n'+sim+'\n</script>').replace(tagSons,()=>'<script>\n'+sons+'</script>').replace(tagArma,()=>'<script>\n'+arma+'\n</script>').replace(tagMundo,()=>'<script>\n'+mundo+'\n</script>').replace(tagSkins,()=>'<script>\n'+skins+'\n</script>'));
console.log('gerado',out,(fs.statSync(out).size/1024).toFixed(0)+' KB');
