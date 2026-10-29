#!/usr/bin/env node
// FBX -> GLB (com a textura de cor dentro), no Chromium com o FBXLoader e o GLTFExporter do próprio three.js r128.
// As armas CC0 do OpenGameArt só vêm em FBX; o tools/montar-modelos.py lê GLB. Usado por ele quando um modelo tem
// "fbx" em assets/modelos/fontes.json (o .zip baixado, o .fbx dentro dele e a textura de cor).
// Uso: THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/converter-fbx.js <arquivo.fbx> <textura.png> <saida.glb>
'use strict';
const fs=require('fs'), path=require('path'), http=require('http'), {chromium}=require('playwright');
const [FBX,TEX,SAIDA]=process.argv.slice(2), THREE_DIR=process.env.THREE_DIR;
if(!FBX||!TEX||!SAIDA||!THREE_DIR){ console.error('uso: THREE_DIR=... node tools/converter-fbx.js <fbx> <png> <glb>'); process.exit(2); }
const ARQ={'/m.fbx':FBX,'/t.png':TEX};
const PAG=`<!doctype html><script src="/three/build/three.min.js"></script><script src="/three/examples/js/libs/fflate.min.js"></script>
<script src="/three/examples/js/curves/NURBSUtils.js"></script><script src="/three/examples/js/curves/NURBSCurve.js"></script>
<script src="/three/examples/js/loaders/FBXLoader.js"></script><script src="/three/examples/js/exporters/GLTFExporter.js"></script><script>
new THREE.FBXLoader().load('/m.fbx',o=>{ new THREE.TextureLoader().load('/t.png',tex=>{ tex.encoding=THREE.sRGBEncoding;
  o.traverse(m=>{ if(m.isMesh) m.material=new THREE.MeshStandardMaterial({map:tex,roughness:.6}); });
  new THREE.GLTFExporter().parse(o,glb=>{ const b=new Uint8Array(glb); let s=''; for(let i=0;i<b.length;i+=32768) s+=String.fromCharCode.apply(null,b.subarray(i,i+32768)); window.GLB=btoa(s); },{binary:true,embedImages:true}); }); },undefined,e=>{ window.ERRO=String(e); });
</script>`;
const srv=http.createServer((q,r)=>{ const u=decodeURIComponent(q.url.split('?')[0]);
  if(u==='/') { r.writeHead(200,{'Content-Type':'text/html'}); return r.end(PAG); }
  const f=ARQ[u]||(u.startsWith('/three/')?path.join(THREE_DIR,u.slice(7)):null);
  fs.readFile(f||'',(e,d)=>{ if(e){ r.writeHead(404); return r.end(); } r.writeHead(200,{'Content-Type':f.endsWith('.js')?'application/javascript':'application/octet-stream'}); r.end(d); }); });
srv.listen(0,async()=>{ const b=await chromium.launch(); const p=await b.newPage(); p.on('pageerror',e=>console.error('pageerror',e.message));
  await p.goto(`http://127.0.0.1:${srv.address().port}/`); await p.waitForFunction(()=>window.GLB||window.ERRO,null,{timeout:120000});
  const r=await p.evaluate(()=>window.GLB||('ERRO '+window.ERRO)); await b.close(); srv.close();
  if(r.startsWith('ERRO')){ console.error(r); process.exit(1); }
  fs.writeFileSync(SAIDA,Buffer.from(r,'base64')); console.log('gravado',SAIDA,(fs.statSync(SAIDA).size/1024).toFixed(0)+' KB'); });
