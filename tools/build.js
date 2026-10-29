#!/usr/bin/env node
// Monta a versão de arquivo único (a publicada no claude.ai): client/index.html com shared/sim.js, client/arma.js e client/sons.js
// (as gravações, ver tools/montar-sons.py) embutidos.
// Uso: node tools/build.js [saida]      (padrão: dist/ritual-reversal.html)
// Também grava dist/web/: a mesma página sem os sons, as texturas e os modelos embutidos, que vão ao lado em pedaços de até
// 12 MB (sons.js, texturas-N.js, modelos-N.js). É a versão publicada no claude.ai, onde a página tem limite de 16 MB e cada
// arquivo ao lado tem o seu.
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
const tagModelos='<script src="modelos.js"></script>', modelos=fs.readFileSync(path.join(ROOT,'client','modelos.js'),'utf8');
const tagBonecos='<script src="bonecos.js"></script>', bonecos=fs.readFileSync(path.join(ROOT,'client','bonecos.js'),'utf8');
const tagArmas='<script src="armas.js"></script>', armas=fs.readFileSync(path.join(ROOT,'client','armas.js'),'utf8');
const tagTex='<script src="texturas.js"></script>', texturas=fs.readFileSync(path.join(ROOT,'client','texturas.js'),'utf8');
const tagAsilo='<script src="asilo.js"></script>', asilo=fs.readFileSync(path.join(ROOT,'client','asilo.js'),'utf8');
if(!cli.includes(tagTex)) throw new Error('client/index.html não tem '+tagTex);
if(!cli.includes(tagAsilo)) throw new Error('client/index.html não tem '+tagAsilo);
if(!cli.includes(tagArmas)) throw new Error('client/index.html não tem '+tagArmas);
if(!cli.includes(tagBonecos)) throw new Error('client/index.html não tem '+tagBonecos);
if(!cli.includes(tagModelos)) throw new Error('client/index.html não tem '+tagModelos);
if(!cli.includes(tagSkins)) throw new Error('client/index.html não tem '+tagSkins);
if(!cli.includes(tagMundo)) throw new Error('client/index.html não tem '+tagMundo);
if(!cli.includes(tagArma)) throw new Error('client/index.html não tem '+tagArma);
if(!cli.includes(tagSons)) throw new Error('client/index.html não tem '+tagSons);
fs.mkdirSync(path.dirname(out),{recursive:true});
fs.writeFileSync(out,cli.replace(tag,()=>'<script>\n'+sim+'\n</script>').replace(tagSons,()=>'<script>\n'+sons+'</script>').replace(tagArma,()=>'<script>\n'+arma+'\n</script>').replace(tagMundo,()=>'<script>\n'+mundo+'\n</script>').replace(tagSkins,()=>'<script>\n'+skins+'\n</script>').replace(tagModelos,()=>'<script>\n'+modelos+'\n</script>').replace(tagBonecos,()=>'<script>\n'+bonecos+'\n</script>').replace(tagArmas,()=>'<script>\n'+armas+'\n</script>').replace(tagTex,()=>'<script>\n'+texturas+'</script>').replace(tagAsilo,()=>'<script>\n'+asilo+'\n</script>'));
console.log('gerado',out,(fs.statSync(out).size/1024).toFixed(0)+' KB');

// ---------- dist/web: a página e a arte em arquivos separados ----------
const WEB=path.join(ROOT,'dist','web'); fs.rmSync(WEB,{recursive:true,force:true}); fs.mkdirSync(WEB,{recursive:true});
const LIMITE=12e6;
function pedacos(arquivo,global,prefixo){   // window.X={...}; vira prefixo-1.js, prefixo-2.js... cada um somando ao window.X
  const txt=fs.readFileSync(path.join(ROOT,'client',arquivo),'utf8'), i=txt.indexOf('window.'+global+'=');
  const O=JSON.parse(txt.slice(i+('window.'+global+'=').length,txt.lastIndexOf(';'))), grupos=[[]]; let tam=0;
  for(const k of Object.keys(O)){ const n=JSON.stringify(O[k]).length; if(tam+n>LIMITE&&grupos[grupos.length-1].length){ grupos.push([]); tam=0; } grupos[grupos.length-1].push(k); tam+=n; }
  return grupos.map((ks,j)=>{ const nome=`${prefixo}-${j+1}.js`, sub={}; ks.forEach(k=>sub[k]=O[k]);
    fs.writeFileSync(path.join(WEB,nome),`window.${global}=Object.assign(window.${global}||{},${JSON.stringify(sub)});\n`); return nome; }); }
const tags=nomes=>nomes.map(n=>`<script src="${n}"></script>`).join('\n');
fs.writeFileSync(path.join(WEB,'sons.js'),sons);
const web=cli.replace(tag,()=>'<script>\n'+sim+'\n</script>').replace(tagSons,()=>'<script src="sons.js"></script>').replace(tagArma,()=>'<script>\n'+arma+'\n</script>').replace(tagMundo,()=>'<script>\n'+mundo+'\n</script>')
  .replace(tagSkins,()=>'<script>\n'+skins+'\n</script>').replace(tagModelos,()=>tags(pedacos('modelos.js','RR_MODELOS','modelos'))).replace(tagBonecos,()=>tags(pedacos('bonecos.js','RR_BONECOS','bonecos'))).replace(tagArmas,()=>'<script>\n'+armas+'\n</script>').replace(tagTex,()=>tags(pedacos('texturas.js','RR_TEXTURAS','texturas')))
  .replace(tagAsilo,()=>'<script>\n'+asilo+'\n</script>');
fs.writeFileSync(path.join(WEB,'ritual-reversal.html'),web);
console.log('dist/web: '+fs.readdirSync(WEB).map(f=>`${f} ${(fs.statSync(path.join(WEB,f)).size/1024).toFixed(0)} KB`).join(', '));
