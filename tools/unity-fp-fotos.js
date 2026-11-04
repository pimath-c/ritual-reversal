#!/usr/bin/env node
// Primeira pessoa do Unity em fotos, sem o Unity: compila o ArmaFP (Scripts/Visual) contra o UnityEngine de mentira, roda o
// tools/cs/FotoFP.cs (cada arma com as mangas do boneco da classe, nos clipes pronta, recarga, inspeção, corrida, canalizar) e
// desenha o que está sob a câmera no three, uma imagem por caso (dist/unity-fp-fotos/<caso>.png e folha.png).
// Uso: THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/unity-fp-fotos.js [pasta] [regex dos casos]
'use strict';
const fs=require('fs'), path=require('path'), {execFileSync}=require('child_process'), os=require('os');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), OUT=process.argv[2]||path.join(ROOT,'dist','unity-fp-fotos'), FILTRO=process.argv[3]||'.';
const THREE_DIR=process.env.THREE_DIR, UN=path.join(ROOT,'unity','Assets','RitualReversal'), CS=path.join(__dirname,'cs');
fs.mkdirSync(OUT,{recursive:true});
const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'unity-fp-fotos-')), EXE=path.join(tmp,'foto.exe'), EXP=path.join(tmp,'exp');
const VIS=path.join(UN,'Scripts','Visual');
const scripts=fs.readdirSync(VIS).filter(f=>/^(Mundo.*|Geo|Texturas|Materiais|MapaVisual|Arvores|Qualidade|Personagens|ModeloGLB|BonecoGLB|ArmaGLB|ArmaFP|SkinsFP)\.cs$/.test(f)).map(f=>path.join(VIS,f));
const sim=fs.readdirSync(path.join(UN,'Scripts','Simulacao')).filter(f=>f.endsWith('.cs')).map(f=>path.join(UN,'Scripts','Simulacao',f));
try{ execFileSync('mcs',['-nowarn:414,618,169,649,219,162,168,1717','-out:'+EXE,path.join(CS,'UnityFalso.cs'),path.join(CS,'SomFalso.cs'),path.join(CS,'FotoFP.cs'),...scripts,...sim],{stdio:['ignore','pipe','pipe']}); }
catch(e){ console.error('não compilou:\n'+String(e.stdout||'')+String(e.stderr||'')); process.exit(2); }
console.log(execFileSync('mono',[EXE,path.join(UN,'Resources'),EXP,FILTRO],{encoding:'utf8',maxBuffer:1<<26}).trim());

const PAGINA=`<!doctype html><html><head><meta charset="utf-8"><style>html,body{margin:0;background:#000;overflow:hidden}</style></head><body>
<script src="/three.min.js"></script><script>
(async()=>{ const J=await (await fetch('/exp/mundo.json')).json(), BIN=await (await fetch('/exp/mundo.bin')).arrayBuffer();
  const renderer=new THREE.WebGLRenderer({antialias:true,preserveDrawingBuffer:true}); renderer.setSize(innerWidth,innerHeight); document.body.appendChild(renderer.domElement);
  renderer.toneMapping=THREE.ACESFilmicToneMapping; renderer.toneMappingExposure=1.1; renderer.outputEncoding=THREE.sRGBEncoding;
  const scene=new THREE.Scene(); scene.background=new THREE.Color(0x30343c);
  const camera=new THREE.PerspectiveCamera(32,innerWidth/innerHeight,.05,100);
  const tex=await Promise.all(J.texturas.map(async T=>{ const d=new Uint8Array(await (await fetch('/exp/'+T.arq)).arrayBuffer());
    const t=new THREE.DataTexture(d,T.w,T.h,THREE.RGBAFormat); t.encoding=T.srgb?THREE.sRGBEncoding:THREE.LinearEncoding; t.flipY=false; t.generateMipmaps=true; t.minFilter=THREE.LinearMipmapLinearFilter;
    t.wrapS=t.wrapT=T.repete?THREE.RepeatWrapping:THREE.ClampToEdgeWrapping; t.needsUpdate=true; return t; }));
  const mats=J.materiais.map(M=>{ const cor=new THREE.Color(M.cor[0],M.cor[1],M.cor[2]); let map=null; if(M.mapa>=0){ map=tex[M.mapa].clone(); map.needsUpdate=true; map.repeat.set(M.esc[0],M.esc[1]); }
    if(/Brilho/.test(M.shader)) return new THREE.MeshBasicMaterial({color:cor,map,transparent:true,opacity:M.cor[3],depthWrite:false,side:THREE.DoubleSide,blending:THREE.AdditiveBlending});
    const tr=M.surface===1; return new THREE.MeshStandardMaterial({color:cor,map,roughness:M.liso==null?1:Math.min(1,1-M.liso/.9),metalness:M.metal||0,side:M.cull===0?THREE.DoubleSide:THREE.FrontSide,transparent:tr,opacity:tr?M.cor[3]:1,alphaTest:M.clip===1?.5:0,
      emissive:M.emissao?new THREE.Color(M.emissao[0],M.emissao[1],M.emissao[2]):new THREE.Color(0)}); });
  const porFoto={};
  for(const mm of J.malhas){ let o=mm.ini; const v=mm.v, g=new THREE.BufferGeometry(), f=n=>{ const a=new Float32Array(BIN,o,n); o+=n*4; return a; };
    g.setAttribute('position',new THREE.BufferAttribute(f(v*3),3)); g.setAttribute('normal',new THREE.BufferAttribute(f(v*3),3)); g.setAttribute('uv',new THREE.BufferAttribute(f(v*2),2));
    g.setIndex(new THREE.BufferAttribute(new Uint32Array(BIN,o,mm.t),1));
    const me=new THREE.Mesh(g,mats[mm.mat]); me.frustumCulled=false; scene.add(me); (porFoto[mm.f]=porFoto[mm.f]||[]).push(me); }
  scene.background=new THREE.Color(0x1a1c22); scene.add(new THREE.HemisphereLight(0x8a96b8,0x2a2018,.8)); const sol=new THREE.DirectionalLight(0xffe2c0,1.4); sol.position.set(-1,2,1); scene.add(sol);
  camera.fov=72; camera.near=.02; camera.updateProjectionMatrix();
  const VISTA=${JSON.stringify(process.env.VISTA||'')};
  if(VISTA==='perto'){ camera.position.set(.02,.02,-.12); camera.lookAt(.17,-.2,-.52); camera.fov=30; camera.updateProjectionMatrix(); scene.children.filter(o=>o.isLight).forEach(l=>l.intensity*=1.6); }   // as mãos de perto, com mais luz
  if(VISTA==='lado'){ camera.position.set(.75,.05,-.15); camera.lookAt(.12,-.22,-.55); camera.fov=50; camera.updateProjectionMatrix(); }   // de lado, para ver as peças em volume
  window.__foto=i=>{ for(const k in porFoto) for(const m of porFoto[k]) m.visible=+k===i; renderer.render(scene,camera); return renderer.domElement.toDataURL('image/jpeg',.9); };
  window.__grade=J.fotos; window.__pronto=true; })().catch(e=>{ window.__erro=String(e&&e.stack||e); });
</script></body></html>`;

(async()=>{
  const srv=require('http').createServer((q,res)=>{ const rel=decodeURIComponent(q.url.split('?')[0].slice(1));
    if(rel==='pagina'){ res.writeHead(200,{'Content-Type':'text/html'}); return res.end(PAGINA); }
    const f=rel==='three.min.js'?path.join(THREE_DIR,'build','three.min.js'):rel.startsWith('exp/')?path.join(EXP,rel.slice(4)):null;
    if(!f||!fs.existsSync(f)){ res.writeHead(404); return res.end(); }
    res.writeHead(200,{'Content-Length':fs.statSync(f).size}); fs.createReadStream(f).pipe(res); });
  await new Promise(ok=>srv.listen(0,'127.0.0.1',ok));
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const p=await browser.newPage({viewport:{width:+process.env.LARG||640,height:Math.round((+process.env.LARG||640)*9/16)}}); p.setDefaultTimeout(600000);
  await p.goto(`http://127.0.0.1:${srv.address().port}/pagina`); await p.waitForFunction(()=>window.__pronto||window.__erro);
  const er=await p.evaluate(()=>window.__erro); if(er){ console.error(er); process.exit(2); }
  const G=await p.evaluate(()=>window.__grade), fotos=[];
  for(let i=0;i<G.length;i++){ const u=await p.evaluate(i=>__foto(i),i); fotos.push({nome:G[i],u}); fs.writeFileSync(path.join(OUT,G[i].replace(/ /g,'_')+'.jpg'),Buffer.from(u.split(',')[1],'base64')); }
  const comp=await browser.newPage({viewport:{width:1300,height:400}});
  await comp.setContent(`<body style="margin:0;background:#111;color:#ddd;font:13px sans-serif;display:flex;flex-wrap:wrap;width:1300px">${fotos.map(f=>`<div><div style="height:18px;padding-left:4px">${f.nome}</div><img width="320" src="${f.u}"></div>`).join('')}</body>`);
  await comp.screenshot({path:path.join(OUT,'folha.png'),fullPage:true}); console.log(fotos.length+' fotos em '+OUT);
  await browser.close(); srv.close(); fs.rmSync(tmp,{recursive:true,force:true});
})().catch(e=>{ console.error(e); process.exit(2); });
