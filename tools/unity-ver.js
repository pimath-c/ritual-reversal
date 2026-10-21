#!/usr/bin/env node
// O Unity sem o Unity: compila o visual do Unity (unity/Assets/RitualReversal/Scripts/Visual) contra um UnityEngine de
// mentira (tools/cs/UnityFalso.cs), roda a montagem do mundo, exporta cada malha com o seu material e desenha isso no
// Chromium com three.js, com a mesma câmera, névoa e luzes de cada vista do jogo oficial. Grava, por vista, o oficial à
// esquerda e o Unity à direita (dist/unity-ver/*.png). Serve para conferir o porte sem abrir o Unity; a luz e as sombras
// do URP não são as do three, então compare formas, cores, texturas e posições, não o brilho exato.
// Uso: THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/unity-ver.js [pasta] [regex das vistas]
// Precisa do Mono (mcs e mono). Vistas: as de tools/navegador.js e algumas de perto.
'use strict';
const fs=require('fs'), path=require('path'), {execFileSync}=require('child_process'), os=require('os');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), OUT=process.argv[2]||path.join(ROOT,'dist','unity-ver'), FILTRO=new RegExp(process.argv[3]||'.');
const THREE_DIR=process.env.THREE_DIR, UN=path.join(ROOT,'unity','Assets','RitualReversal'), CS=path.join(__dirname,'cs');
fs.mkdirSync(OUT,{recursive:true});
const VISTAS=[
  {nome:'acampamento',x:-122,z:-22,yaw:-Math.PI/2,pitch:.05},
  {nome:'ponte-grande',x:0,z:-2,yaw:0,pitch:.25},
  {nome:'ponte-caida',x:-106,z:-77,yaw:-Math.PI/2,pitch:.02},
  {nome:'aqueduto',x:106,z:-77,yaw:Math.PI/2,pitch:.02},
  {nome:'nave',x:0,z:-38,yaw:0,pitch:.12},
  {nome:'cruzeiro',x:0,z:-66,yaw:0,pitch:.25},
  {nome:'claustro',x:-36,z:-46,yaw:-.7,pitch:.1},
  {nome:'sala-capitulo',x:-53,z:-47,yaw:Math.PI/4,pitch:-.05},
  {nome:'capela-desabada',x:22,z:-71,yaw:-Math.PI/2,pitch:.05},
  {nome:'menires',x:-82,z:50,yaw:Math.PI,pitch:0},
  {nome:'encapuzados',x:82,z:52,yaw:Math.PI,pitch:.05},
  {nome:'portaria',x:0,z:10,yaw:-Math.PI/2,pitch:0},
  {nome:'pedreira',x:-46,z:28,yaw:Math.PI,pitch:.05},
  {nome:'charco',x:46,z:30,yaw:Math.PI,pitch:-.05},
  {nome:'carvoaria',x:14,z:50,yaw:Math.PI,pitch:.05},
  {nome:'calvario',x:0,z:76,yaw:Math.PI,pitch:.1},
  {nome:'ruinas',x:-120,z:-76,yaw:0,pitch:.05},
  {nome:'fachada',x:0,z:-12,yaw:0,pitch:.32},
  {nome:'altar-abside',x:0,z:-80,yaw:0,pitch:-.25},
  {nome:'mata-catedral',x:-60,z:20,yaw:-.6,pitch:.18}].filter(v=>FILTRO.test(v.nome));

// 1) exporta o mundo do Unity
const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'unity-ver-')), EXE=path.join(tmp,'exportar.exe'), EXP=path.join(OUT,'exportado');
const VIS=path.join(UN,'Scripts','Visual'); // o mundo inteiro (Mundo*.cs) e o que ele usa; Som, primeira pessoa e pós ficam de fora
const scripts=fs.readdirSync(VIS).filter(f=>/^(Mundo.*|Geo|Texturas|Materiais|MapaVisual|Arvores|Qualidade|Personagens)\.cs$/.test(f)).map(f=>path.join(VIS,f));
const sim=fs.readdirSync(path.join(UN,'Scripts','Simulacao')).filter(f=>f.endsWith('.cs')).map(f=>path.join(UN,'Scripts','Simulacao',f));
try{ execFileSync('mcs',['-nowarn:414,618,169,649,219,162,168,1717','-out:'+EXE,path.join(CS,'UnityFalso.cs'),path.join(CS,'SomFalso.cs'),path.join(CS,'ExportarMundo.cs'),...scripts,...sim],{stdio:['ignore','pipe','pipe']}); }
catch(e){ console.error('o visual do Unity não compilou contra o UnityEngine de mentira:\n'+String(e.stdout||'')+String(e.stderr||'')); process.exit(2); }
fs.rmSync(EXP,{recursive:true,force:true});
console.log(execFileSync('mono',[EXE,path.join(UN,'Resources'),EXP],{encoding:'utf8'}).trim());
// e uma partida de bots passando pelo visual do Unity (ritual, coleta, execução): exceções aparecem aqui
try{ console.log(execFileSync('mono',[EXE,path.join(UN,'Resources'),EXP,'partida'],{encoding:'utf8'}).trim()); }
catch(e){ console.error(String(e.stdout||'')+String(e.stderr||'')); process.exit(1); }

// 2) a página que desenha a exportação (three r128, os mesmos ajustes do renderizador do jogo)
const PAGINA=`<!doctype html><html><head><meta charset="utf-8"><style>html,body{margin:0;background:#000;overflow:hidden}</style></head><body>
<script src="http://ver.local/three.min.js"></script><script>
(async()=>{ console.log('baixando'); const J=await (await fetch('http://ver.local/exp/mundo.json')).json(); console.log('json'); const BIN=await (await fetch('http://ver.local/exp/mundo.bin')).arrayBuffer(); console.log('bin '+BIN.byteLength);
  const renderer=new THREE.WebGLRenderer({antialias:true}); renderer.setSize(innerWidth,innerHeight); document.body.appendChild(renderer.domElement);
  renderer.toneMapping=THREE.ACESFilmicToneMapping; renderer.toneMappingExposure=1.25; renderer.outputEncoding=THREE.sRGBEncoding; renderer.shadowMap.enabled=true; renderer.shadowMap.type=THREE.PCFSoftShadowMap;
  const scene=new THREE.Scene(); scene.background=new THREE.Color(0x05070c); scene.fog=new THREE.FogExp2(0x0b0d14,.026);
  const camera=new THREE.PerspectiveCamera(72,innerWidth/innerHeight,.05,220); camera.rotation.order='YXZ'; scene.add(camera);
  const tex=await Promise.all(J.texturas.map(async T=>{ const d=new Uint8Array(await (await fetch('http://ver.local/exp/'+T.arq)).arrayBuffer());
    const t=new THREE.DataTexture(d,T.w,T.h,THREE.RGBAFormat); t.encoding=T.srgb?THREE.sRGBEncoding:THREE.LinearEncoding; t.flipY=false; t.generateMipmaps=true; t.minFilter=THREE.LinearMipmapLinearFilter; t.magFilter=THREE.LinearFilter;
    t.wrapS=t.wrapT=T.repete?THREE.RepeatWrapping:THREE.ClampToEdgeWrapping; t.anisotropy=4; t.needsUpdate=true; return t; })); console.log('texturas');
  const texCom=(i,esc,desl)=>{ if(i<0) return null; const t=tex[i].clone(); t.needsUpdate=true; t.repeat.set(esc[0],esc[1]); t.offset.set(desl[0],desl[1]); return t; };
  const mats=J.materiais.map(M=>{ const cor=new THREE.Color(M.cor[0],M.cor[1],M.cor[2]), brilho=/Brilho/.test(M.shader), dupla=M.cull===0;
    if(brilho){ const m=new THREE.MeshBasicMaterial({color:cor,map:texCom(M.mapa,M.esc,M.desl),transparent:true,opacity:M.cor[3],depthWrite:M.zwrite===1,side:THREE.DoubleSide,fog:M.nevoa!==0,
        blending:M.dst===1?THREE.AdditiveBlending:THREE.NormalBlending,alphaTest:M.corte||0,vertexColors:true});
      if(M.billboard===1){ m.onBeforeCompile=sh=>{ sh.vertexShader='attribute vec2 canto;\\n'+sh.vertexShader.replace('#include <project_vertex>','vec4 mvPosition=modelViewMatrix*vec4(transformed,1.0); mvPosition.xy+=canto; gl_Position=projectionMatrix*mvPosition;'); }; m.customProgramCacheKey=()=>'bb'; }
      m.userData={fila:M.fila,bb:M.billboard===1}; return m; }
    const transp=M.surface===1, m=new THREE.MeshStandardMaterial({color:cor,map:texCom(M.mapa,M.esc,M.desl),roughness:M.liso==null?1:Math.min(1,1-M.liso/.9),metalness:M.metal||0,side:dupla?THREE.DoubleSide:THREE.FrontSide,
      transparent:transp,opacity:transp?M.cor[3]:1,alphaTest:M.clip===1?(M.cutoff||.5):0,emissive:M.emissao?new THREE.Color(M.emissao[0],M.emissao[1],M.emissao[2]):new THREE.Color(0)});
    if(M.detalhe>=0){ const d=texCom(M.detalhe,[1,1],[0,0]); m.onBeforeCompile=sh=>{ sh.uniforms.detalhe={value:d}; sh.uniforms.dEsc={value:new THREE.Vector2(M.dEsc[0],M.dEsc[1])};
        sh.fragmentShader='uniform sampler2D detalhe; uniform vec2 dEsc;\\n'+sh.fragmentShader.replace('#include <map_fragment>','#include <map_fragment>\\n diffuseColor.rgb*=sRGBToLinear(texture2D(detalhe,vUv*dEsc)).rgb*2.0;'); }; m.customProgramCacheKey=()=>'det'+M.detalhe; } // como o _DETAIL_MULX2 do URP
    if(/Marco/.test(M.shader)){ m.onBeforeCompile=sh=>{ sh.fragmentShader=sh.fragmentShader.replace('#include <fog_fragment>','#ifdef USE_FOG\\n float fogFactor=1.0-exp(-fogDensity*fogDensity*fogDepth*fogDepth*.07);\\n gl_FragColor.rgb=mix(gl_FragColor.rgb,fogColor,fogFactor);\\n #endif'); }; m.customProgramCacheKey=()=>'marco'; } // RitualReversal/Marco: _NevoaK .07
    m.userData={fila:M.fila}; return m; });
  console.log('materiais prontos; '+J.malhas.length+' malhas');
  for(const mm of J.malhas){ const M=mats[mm.mat], v=mm.v; let o=mm.ini; const g=new THREE.BufferGeometry();
    const f=(n)=>{ const a=new Float32Array(BIN,o,n); o+=n*4; return a; };
    g.setAttribute('position',new THREE.BufferAttribute(f(v*3),3)); g.setAttribute('normal',new THREE.BufferAttribute(f(v*3),3)); g.setAttribute('uv',new THREE.BufferAttribute(f(v*2),2));
    if(mm.uv2) g.setAttribute('canto',new THREE.BufferAttribute(f(v*2),2));
    if(mm.cor) g.setAttribute('color',new THREE.BufferAttribute(f(v*4),4)); else if(M.isMeshBasicMaterial){ const c=new Float32Array(v*4).fill(1); g.setAttribute('color',new THREE.BufferAttribute(c,4)); }
    const idx=new Uint32Array(BIN,o,mm.t); g.setIndex(new THREE.BufferAttribute(idx,1));
    const me=new THREE.Mesh(g,M); me.castShadow=!!mm.sombra; me.receiveShadow=true; me.frustumCulled=!M.userData.bb; if(M.transparent) me.renderOrder=M.userData.fila; me.name=mm.nome; me.userData.pai=mm.pai; scene.add(me); }
  const ceu=scene.children.filter(o=>o.userData.pai==='Céu'), luas=scene.children.filter(o=>o.name==='lua'||o.name==='halo da lua');
  scene.add(new THREE.HemisphereLight(0x33476e,0x100a08,.5));
  const moon=new THREE.DirectionalLight(0x8aa2d6,.42); moon.castShadow=true; moon.shadow.mapSize.set(2048,2048); Object.assign(moon.shadow.camera,{left:-60,right:60,top:60,bottom:-60,near:1,far:170}); moon.shadow.bias=-.0006; moon.shadow.normalBias=.03; scene.add(moon,moon.target);
  const luar=new THREE.PointLight(0x8fa8e0,.9,70,1.5); luar.position.set(-30,26,-54); scene.add(luar);
  const velas=[]; for(let i=0;i<12;i++){ const L=new THREE.PointLight(0xff8a36,0,14,2); scene.add(L); velas.push(L); }
  window.__vista=s=>{ camera.position.fromArray(s.pos); camera.quaternion.fromArray(s.quat); camera.fov=s.fov; camera.aspect=innerWidth/innerHeight; camera.updateProjectionMatrix();
    scene.fog.color.setRGB(s.fog[0],s.fog[1],s.fog[2]); scene.fog.density=s.fog[3]; renderer.toneMappingExposure=s.exp;
    const d=new THREE.Vector3(18,40,26).normalize(); moon.position.copy(camera.position).addScaledVector(d,80); moon.target.position.copy(camera.position); moon.target.updateMatrixWorld();
    velas.forEach((L,i)=>{ const q=s.velas[i]; if(!q){ L.intensity=0; return; } L.position.fromArray(q.p); L.color.setRGB(q.c[0],q.c[1],q.c[2]); L.intensity=q.i; L.distance=q.d; });
    for(const o of ceu) o.position.copy(camera.position); for(const o of luas) o.position.copy(camera.position).addScaledVector(d,150);
    renderer.render(scene,camera); return true; };
  window.__pronto=true; })().catch(e=>{ window.__erro=String(e&&e.stack||e); });
</script></body></html>`;

(async()=>{
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const VP={width:960,height:540};
  // jogo oficial
  const of=await browser.newPage({viewport:VP}); const erros=[]; of.on('pageerror',e=>erros.push('oficial: '+e.message));
  await of.route('**/*',r=>{ const u=r.request().url(); if(THREE_DIR&&u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')) return r.fulfill({path:path.join(THREE_DIR,u.split('three@0.128.0/')[1]),contentType:'application/javascript'}); if(u.includes('fonts.g')) return r.abort(); return r.continue(); });
  await of.goto('file://'+(process.env.HTML||path.join(ROOT,'dist','ritual-reversal.html'))+'#autotest'); await of.waitForFunction(()=>window.__T,null,{timeout:180000});
  await of.addStyleTag({content:'#scr{display:none!important}'});
  await of.evaluate(()=>{ __T.startLocal(); }); await of.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000});
  await of.evaluate(()=>{ __T.sendAct({type:'ready'}); }); await of.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000}); await of.waitForTimeout(800);
  // primeiro todas as fotos do oficial (as duas páginas juntas disputam a GPU emulada)
  const fotos=[];
  for(const v of VISTAS){
    await of.evaluate(v=>{ const m=__T.game.actors.find(a=>a.cid==='local'); m.x=v.x; m.z=v.z; m.invuln=999; m.hp=m.maxHp; __T.LOOK.yaw=v.yaw; __T.LOOK.pitch=v.pitch; m.inp.yaw=v.yaw; m.inp.pitch=v.pitch;
      for(const a of __T.game.actors) if(a!==m){ a.x=a.team==='H'?-125:125; a.z=a.team==='H'?-8:8; } },v);
    await of.waitForTimeout(1500);
    const st=await of.evaluate(()=>{ const sc=__T.cena(), velas=[]; let c=sc.children.find(o=>o.isPerspectiveCamera)||null;
      if(!c) sc.traverse(o=>{ if(!c&&o.isPerspectiveCamera) c=o; });
      sc.traverse(o=>{ if(o.isPointLight&&o.parent===sc&&o.intensity>0&&o.distance>0&&o.distance<30) velas.push({p:o.position.toArray(),c:[o.color.r,o.color.g,o.color.b],i:o.intensity,d:o.distance}); });
      c.updateMatrixWorld(); const p=new THREE.Vector3(), q=new THREE.Quaternion(); c.getWorldPosition(p); c.getWorldQuaternion(q);
      return {pos:p.toArray(),quat:q.toArray(),fov:c.fov,fog:[sc.fog.color.r,sc.fog.color.g,sc.fog.color.b,sc.fog.density],exp:__T.rend().toneMappingExposure,velas:velas.slice(0,12)}; });
    fotos.push({v,st,a:await of.screenshot()}); console.log('oficial',v.nome);
  }
  await of.close();
  // depois a exportação do Unity
  const un=await browser.newPage({viewport:VP}); un.on('pageerror',e=>{ console.error('a página do Unity falhou: '+e.message); process.exit(2); }); un.on('console',m=>{ if(process.env.DEBUG) console.log('[unity]',m.text()); });
  un.on('crash',()=>{ console.error('a página do Unity caiu (memória?)'); process.exit(2); });
  await un.route('http://ver.local/**',r=>{ const u=r.request().url(), rel=u.split('ver.local/')[1];
    if(rel==='pagina') return r.fulfill({body:PAGINA,contentType:'text/html'});
    if(rel==='three.min.js') return THREE_DIR?r.fulfill({path:path.join(THREE_DIR,'build','three.min.js'),contentType:'application/javascript'}):r.fulfill({status:404,body:''});
    if(rel.startsWith('exp/')) return r.fulfill({path:path.join(EXP,rel.slice(4)),contentType:'application/octet-stream'});
    return r.fulfill({status:404,body:''}); });
  await un.goto('http://ver.local/pagina'); await un.waitForFunction(()=>window.__pronto||window.__erro,null,{timeout:600000});
  const eu=await un.evaluate(()=>window.__erro); if(eu){ console.error('a página do Unity falhou: '+eu); process.exit(2); }
  const comp=await browser.newPage({viewport:{width:VP.width*2+8,height:VP.height+30}});
  for(const {v,st,a} of fotos){
    await un.evaluate(s=>window.__vista(s),st); await un.waitForTimeout(200);
    const b=await un.screenshot();
    await comp.setContent(`<body style="margin:0;background:#111;color:#ccc;font:14px sans-serif;display:flex;gap:8px;flex-wrap:wrap"><div><div style="height:26px">${v.nome}: oficial</div><img src="data:image/png;base64,${a.toString('base64')}"></div><div><div style="height:26px">Unity (exportado, desenhado no three)</div><img src="data:image/png;base64,${b.toString('base64')}"></div></body>`);
    await comp.screenshot({path:path.join(OUT,v.nome+'.png')}); console.log('vista',v.nome);
  }
  if(erros.length) console.log('ERROS:\n'+erros.join('\n'));
  await browser.close(); fs.rmSync(tmp,{recursive:true,force:true}); process.exit(erros.length?1:0);
})().catch(e=>{ console.error(e); process.exit(2); });
