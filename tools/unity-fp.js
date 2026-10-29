#!/usr/bin/env node
// A primeira pessoa do Unity é a do navegador? Roda a mesma sequência de casos no client/arma.js (Chromium, #autotest) e no
// ArmaFP.cs (Mono, sobre o UnityEngine de mentira de tools/cs) e compara, nó a nó, a matriz de cada mão, osso, peça e arma
// no espaço da câmera. Casos: cada clipe de cada arma forçado em vários instantes (recargas por munição inclusive), o
// coice e o ciclo mecânico em vários tempos depois do tiro, e sequências livres (sacar, correr, selar ou canalizar,
// executar, coletar, recarregar) com mistura entre estados, eventos e tranco de câmera.
// Antes: node tools/build.js e node tools/exportar-primeira-pessoa.js (o Unity lê Resources/primeira-pessoa.*).
// Uso: THREE_DIR=/caminho/node_modules/three NODE_PATH=$(npm root -g) node tools/unity-fp.js [tolerância em metros]
'use strict';
const fs=require('fs'), path=require('path'), os=require('os'), {execFileSync}=require('child_process');
const {abrir,coletar}=require('./exportar-primeira-pessoa.js');
const ROOT=path.join(__dirname,'..'), UN=path.join(ROOT,'unity','Assets','RitualReversal'), CS=path.join(__dirname,'cs'), TOL=+(process.argv[2]||2e-4);

// ---- casos
const casos=[]; let agora=10; const dt=1/60;
const corpo0=[0,0,0,0,0,0];
const quadro=(o)=>{ agora+=dt; casos.push(Object.assign({dt,ctx:{agora,corpo:corpo0}},o)); };
const H=(cls,x)=>Object.assign({team:'H',cls,st:'alive',ammo:8,reloadT:0,sealing:-1,fervor:100,hold:null},x||{});
const C=x=>Object.assign({team:'C',cls:'ritualista',st:'alive',ammo:8,reloadT:0,sealing:-1,fervor:100,hold:null},x||{});
const T9=[0,.07,.19,.31,.43,.55,.67,.79,.91];
const CLIPES={carabina:['pronta','sacar','inspecao','ajuste','corrida','selar','gesto','executar','abaixada'],revolver:['pronta','sacar','inspecao','ajuste','corrida','selar','gesto','executar','abaixada'],
  cetro:['pronta','sacar','inspecao','ajuste','corrida','canalizar','gesto','executar','coletar','abaixada']};
const quem=a=>a==='cetro'?C():H(a==='revolver'?'exorcista':'soldado');
// 1) poses forçadas
for(const arma of ['carabina','revolver','cetro']){
  const variantes=CLIPES[arma].map(c=>({clipe:c}));
  if(arma==='carabina') variantes.push({clipe:'recarga',vazia:true,n:3},{clipe:'recarga',vazia:false,n:1},{clipe:'recarga',vazia:false,n:2},{clipe:'recarga',vazia:false,n:3});
  if(arma==='revolver') variantes.push({clipe:'recarga',vazia:true,carga:0},{clipe:'recarga',vazia:false,carga:5});
  for(const v of variantes) for(const t of T9) quadro({forca:Object.assign({arma,t,fervor:100},v),vm:{tT:9,pausa:0,cast:0,fase:0},m:quem(arma),medir:true});
  // coice e ciclo (a arma pronta, logo depois do tiro)
  for(const tT of [0,.012,.03,.06,.12,.2,.28,.36]) quadro({forca:{arma,clipe:'pronta',t:0,fervor:arma==='cetro'?60:100},vm:{tT,pausa:0,cast:arma==='cetro'?Math.max(0,1-tT*4):0,fase:0},m:quem(arma),medir:true});
}
// 2) sequências livres: morto um quadro (some), vivo (saca), parado, correndo, de volta, selando/canalizando, executando
function sequencia(arma,extra){
  const m0=quem(arma); quadro({forca:null,m:Object.assign({},m0,{st:'dead'}),vm:{tT:9,pausa:0,cast:0,fase:0}});
  for(let i=0;i<150;i++){ const m=Object.assign({},m0), ctx={agora:agora+dt,corpo:[Math.sin(i*.1)*.01,-Math.abs(Math.sin(i*.2))*.01,0,0,.002*i%1,0]};
    let fase=i*.09;
    if(i>=50&&i<80){ ctx.correndo=true; ctx.andando=true; }
    if(i>=95&&i<120){ if(arma==='cetro'){ ctx.canalizando=true; ctx.altar=[3,-2]; } else m.sealing=0; }
    if(i>=125){ m.hold={key:arma==='cetro'&&i>=140?'col3':'exe5',t:(i-125)*.1,max:4.5}; if(m.hold.key[0]==='e') ctx.execK=m.hold.t/4.5; else ctx.colK=m.hold.t/4.5; }
    agora+=dt; casos.push({dt,ctx:Object.assign({agora},ctx,{corpo:ctx.corpo}),m,vm:{fase},medir:i%3===0}); }
  if(extra) extra();
}
function recarga(arma,ammo){ const m0=quem(arma); let r=1.6;
  for(let i=0;i<130;i++){ const m=Object.assign({},m0,{ammo:i<6?ammo:r>0?ammo:8,reloadT:i<6?0:Math.max(0,r)}); if(i>=6) r-=dt; agora+=dt;
    casos.push({dt,ctx:{agora,corpo:corpo0},m,vm:i===0?{tT:9,pausa:0,cast:0,fase:0}:undefined,medir:i%3===0}); } }
for(const arma of ['carabina','revolver','cetro']) sequencia(arma);
recarga('carabina',0); recarga('carabina',6); recarga('revolver',0); recarga('revolver',3);

(async()=>{ const {chromium}=require('playwright');
  // ---- navegador
  const {browser,page,erros}=await abrir(chromium,process.env.THREE_DIR);
  await page.evaluate(coletar);
  const js=await page.evaluate(casos=>{ const A=__T.ARMA, VM=__T.VM, objs=window.__FPNOS, out=[];
    const m4=new THREE.Matrix4();
    for(const c of casos){
      if('forca' in c) A.forcar(c.forca);
      if(c.vm) Object.assign(VM,c.vm);
      const X=c.ctx, ctx={agora:X.agora,andando:!!X.andando,correndo:!!X.correndo,agK:X.agK||0,canalizando:!!X.canalizando,altar:X.altar?{x:X.altar[0],z:X.altar[1]}:null,
        execK:X.execK||0,colK:X.colK||0,queimando:!!X.queimando,corpo:{x:X.corpo[0],y:X.corpo[1],z:X.corpo[2],rx:X.corpo[3],ry:X.corpo[4],rz:X.corpo[5]}};
      A.quadro(c.dt,c.m,ctx);
      if(!c.medir) continue;
      const nos=objs.map(o=>{ if(!o) return null; let vis=true; m4.identity(); for(let q=o;q&&!q.isCamera;q=q.parent){ q.updateMatrix(); m4.premultiply(q.matrix); if(!q.visible) vis=false; }
        const e=m4.elements; return [vis?1:0,e[0],e[1],e[2],e[4],e[5],e[6],e[8],e[9],e[10],e[12],e[13],e[14]]; });
      out.push({est:A.S.est.nome,t:A.S.est.t,nos}); }
    return out; },casos);
  await browser.close();
  // ---- Unity (Mono)
  const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'unity-fp-')), EXE=path.join(tmp,'teste.exe'), ENT=path.join(tmp,'casos.json'), SAI=path.join(tmp,'saida.json');
  const VIS=path.join(UN,'Scripts','Visual');
  const scripts=fs.readdirSync(VIS).filter(f=>/^(Mundo.*|Geo|Texturas|Materiais|MapaVisual|Arvores|Qualidade|Personagens|ModeloGLB|BonecoGLB|ArmaGLB|ArmaFP|SkinsFP)\.cs$/.test(f)).map(f=>path.join(VIS,f));
  const sim=fs.readdirSync(path.join(UN,'Scripts','Simulacao')).filter(f=>f.endsWith('.cs')).map(f=>path.join(UN,'Scripts','Simulacao',f));
  try{ execFileSync('mcs',['-nowarn:414,618,169,649,219,162,168,1717','-out:'+EXE,path.join(CS,'UnityFalso.cs'),path.join(CS,'SomFalso.cs'),path.join(CS,'TesteFP.cs'),...scripts,...sim],{stdio:['ignore','pipe','pipe']}); }
  catch(e){ console.error('o ArmaFP não compilou contra o UnityEngine de mentira:\n'+String(e.stdout||'')+String(e.stderr||'')); process.exit(2); }
  fs.writeFileSync(ENT,JSON.stringify(casos));
  try{ execFileSync('mono',[EXE,path.join(UN,'Resources'),ENT,SAI],{stdio:['ignore','pipe','inherit'],maxBuffer:1<<28}); }
  catch(e){ console.error('o ArmaFP falhou:\n'+String(e.stdout||'')); process.exit(2); }
  const cs=JSON.parse(fs.readFileSync(SAI,'utf8'));
  // ---- comparação
  const nomes=require(path.join(UN,'Resources','primeira-pessoa.json')).nos.map(n=>n.n);
  let pior=0, onde='', ruins=0, visDif=0, estDif=0; const porCaso=[];
  for(let k=0;k<js.length;k++){ const a=js[k], b=cs[k]; let pc=0, pn='';
    if(a.est!==b.est||Math.abs(a.t-b.t)>1e-6){ estDif++; if(estDif<6) console.log(`caso ${k}: estado ${a.est} ${a.t.toFixed(4)} no navegador, ${b.est} ${b.t.toFixed(4)} no Unity`); }
    for(let i=0;i<a.nos.length;i++){ const x=a.nos[i], y=b.nos[i]; if(!x) continue;
      if(x[0]!==y[0]){ visDif++; if(visDif<8) console.log(`caso ${k} (${a.est} ${a.t.toFixed(3)}): ${nomes[i]} (${i}) ${x[0]?'visível':'escondido'} no navegador, ${y[0]?'visível':'escondido'} no Unity`); continue; }
      if(!x[0]) continue;
      for(let j=1;j<13;j++){ const d=Math.abs(x[j]-y[j]); if(d>pc){ pc=d; pn=nomes[i]+' ('+i+')'; } } }
    porCaso.push(pc); if(pc>TOL){ ruins++; if(ruins<12) console.log(`caso ${k} (${a.est} ${a.t.toFixed(3)}): diferença ${pc.toExponential(2)} em ${pn}`); }
    if(pc>pior){ pior=pc; onde=`caso ${k} (${a.est}), ${pn}`; } }
  console.log(`${js.length} poses comparadas (${casos.length} quadros), ${js[0].nos.filter(Boolean).length} nós cada: maior diferença ${pior.toExponential(2)} em ${onde}`);
  console.log(`${ruins} poses acima de ${TOL} m; ${visDif} visibilidades diferentes; ${estDif} estados diferentes`);
  if(erros.length) console.log('ERROS no navegador:\n'+erros.join('\n'));
  process.exit(ruins||visDif||estDif||erros.length?1:0); })().catch(e=>{ console.error(e); process.exit(2); });
