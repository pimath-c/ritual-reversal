#!/usr/bin/env node
// Fim da noite do Unity sem o Unity: compila Noite.cs, Desenho.cs e a planta contra o UnityEngine de mentira
// (tools/cs/UnityFalso.cs), joga uma noite de bots no Mono (tools/cs/TesteNoite.cs) e grava em dist/unity-noite/ a planta
// fixa, o gráfico de cada rodada (PNG) e o que as telas do fim mostram (noite.json: marcos, lances, destaques, placar).
// Com THREE_DIR, fotografa também a planta do navegador (a escolha de altares) para comparar com planta-unity.png.
// Uso: [THREE_DIR=<three@0.128.0>] NODE_PATH=$(npm root -g) node tools/unity-noite.js [semente]
'use strict';
const fs=require('fs'), path=require('path'), os=require('os'), zlib=require('zlib'), {execFileSync}=require('child_process');
const ROOT=path.join(__dirname,'..'), UN=path.join(ROOT,'unity','Assets','RitualReversal'), CS=path.join(__dirname,'cs'), OUT=path.join(ROOT,'dist','unity-noite');
fs.mkdirSync(OUT,{recursive:true});
// PNG sem bibliotecas: cabeçalho, linhas com filtro 0 e zlib
function png(rgba,w,h){ const crc=(()=>{ const T=new Int32Array(256); for(let n=0;n<256;n++){ let c=n; for(let k=0;k<8;k++) c=c&1?0xedb88320^(c>>>1):c>>>1; T[n]=c; } return b=>{ let c=-1; for(const x of b) c=T[(c^x)&255]^(c>>>8); return (c^-1)>>>0; }; })();
  const bloco=(tipo,dados)=>{ const t=Buffer.from(tipo), L=Buffer.alloc(4), C=Buffer.alloc(4); L.writeUInt32BE(dados.length); C.writeUInt32BE(crc(Buffer.concat([t,dados]))); return Buffer.concat([L,t,dados,C]); };
  const ih=Buffer.alloc(13); ih.writeUInt32BE(w,0); ih.writeUInt32BE(h,4); ih[8]=8; ih[9]=6; ih[10]=ih[11]=ih[12]=0;
  const cru=Buffer.alloc((w*4+1)*h); for(let y=0;y<h;y++){ cru[y*(w*4+1)]=0; rgba.copy(cru,y*(w*4+1)+1,y*w*4,(y+1)*w*4); }
  return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),bloco('IHDR',ih),bloco('IDAT',zlib.deflateSync(cru)),bloco('IEND',Buffer.alloc(0))]); }
function converter(arq,saida,fundo){ const b=fs.readFileSync(arq), w=b.readInt32LE(0), h=b.readInt32LE(4), px=Buffer.from(b.subarray(8));
  if(fundo) for(let i=0;i<px.length;i+=4){ const a=px[i+3]/255; for(let k=0;k<3;k++) px[i+k]=Math.round(px[i+k]*a+fundo[k]*(1-a)); px[i+3]=255; }
  fs.writeFileSync(saida,png(px,w,h)); return [w,h]; }

const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'unity-noite-')), EXE=path.join(tmp,'noite.exe');
const S=path.join(UN,'Scripts'), sim=fs.readdirSync(path.join(S,'Simulacao')).filter(f=>f.endsWith('.cs')).map(f=>path.join(S,'Simulacao',f));
try{ execFileSync('mcs',['-nowarn:414,618,169,649,219,162,168,1717','-out:'+EXE,path.join(CS,'UnityFalso.cs'),path.join(CS,'TesteNoite.cs'),path.join(S,'Noite.cs'),path.join(S,'Desenho.cs'),path.join(S,'Visual','MapaVisual.cs'),...sim],{stdio:['ignore','pipe','pipe']}); }
catch(e){ console.error('o fim da noite não compilou contra o UnityEngine de mentira:\n'+String(e.stdout||'')+String(e.stderr||'')); process.exit(2); }
try{ execFileSync('mono',[EXE,path.join(UN,'Resources'),tmp,process.argv[2]||'7'],{stdio:['ignore','inherit','inherit']}); }
catch(e){ console.error('o teste da noite falhou'); process.exit(2); }
converter(path.join(tmp,'planta.rgba'),path.join(OUT,'planta-unity.png'),[8,7,9]);
const N=JSON.parse(fs.readFileSync(path.join(tmp,'noite.json'),'utf8')); fs.writeFileSync(path.join(OUT,'noite.json'),JSON.stringify(N,null,1));
for(const R of N.rodadas) converter(path.join(tmp,'grafico'+R.r+'.rgba'),path.join(OUT,'grafico'+R.r+'.png'),[22,20,24]);
const erros=[];
if(N.fase!=='final') erros.push('a noite não terminou ('+N.fase+')');
for(const R of N.rodadas){ if(R.amostras<60) erros.push('rodada '+R.r+': só '+R.amostras+' amostras'); if(!R.marcos.some(m=>m.tipo==='momento')) erros.push('rodada '+R.r+': sem os momentos'); if(!R.marcos.some(m=>m.tipo==='inicio')) erros.push('rodada '+R.r+': nenhum ritual começou'); if(R.posicoes<20) erros.push('rodada '+R.r+': replay com '+R.posicoes+' posições'); }
if(!N.placar.length||N.placar[0].linhas.length!==4) erros.push('placar sem os quatro jogadores');
if(!N.premios.length) erros.push('nenhum destaque');
if(!N.registroOnlineIgual) erros.push('o registro lido do JSON (online) não dá o mesmo placar, destaques e lances');
console.log(N.vencedor+' · '+N.eventos+' eventos no registro');
for(const R of N.rodadas) console.log('rodada '+R.r+': '+R.amostras+' amostras, '+R.marcos.length+' marcos, '+R.posicoes+' posições, '+R.quedas+' quedas; altares '+R.altares.join(', '));
console.log('lances: '+N.lances.map(l=>l.tit).join('; ')); console.log('destaques: '+N.premios.map(p=>p.nome+' ('+p.quem+', '+p.txt+')').join('; '));
for(const P of N.placar) for(const L of P.linhas) console.log('  '+L.team+' '+L.nome.padEnd(14)+L.classe.padEnd(12)+L.v.join(' '));

(async()=>{
  if(process.env.THREE_DIR){
    const {chromium}=require('playwright'), THREE_DIR=process.env.THREE_DIR;
    const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
    const page=await browser.newPage({viewport:{width:1280,height:900}});
    await page.route('**/*',r=>{ const u=r.request().url();
      if(u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')) return r.fulfill({path:path.join(THREE_DIR,u.split('three@0.128.0/')[1]),contentType:'application/javascript'});
      if(u.startsWith('file:')) return r.continue(); return r.abort(); });
    await page.goto('file://'+path.join(ROOT,'dist','ritual-reversal.html')+'#autotest'); await page.waitForFunction(()=>window.__T,null,{timeout:120000});
    await page.evaluate(()=>__T.startLocal({papel:'C'})); await page.waitForSelector('#mapSvg',{timeout:60000}); await page.waitForTimeout(800);
    const bb=await (await page.$('#mapSvg')).boundingBox(); await page.screenshot({path:path.join(OUT,'planta-navegador.png'),clip:bb,timeout:120000}); await browser.close();
    console.log('planta do navegador: dist/unity-noite/planta-navegador.png');
  }
  console.log('gravado em dist/unity-noite/');
  if(erros.length){ console.log('PROBLEMAS:\n  '+erros.join('\n  ')); process.exit(1); }
})();
