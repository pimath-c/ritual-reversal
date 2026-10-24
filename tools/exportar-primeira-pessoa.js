#!/usr/bin/env node
// Primeira pessoa para o Unity: abre o jogo oficial no Chromium, deixa o client/arma.js montar as mãos, as armas e os
// clipes, e grava tudo do jeito que ficou na memória:
//   Resources/primeira-pessoa.json   nós (grupos, malhas, ossos, sprites) com a transformação local, materiais, papéis
//                                    (alavanca, tambor, dedos...), parâmetros dos braços, clipes já preparados (inclusive
//                                    as recargas por munição), dedos, perfis das armas e o catálogo das skins
//   Resources/primeira-pessoa-geo.bytes  posições, normais, UVs (float32) e índices (uint32) de cada geometria
//   Resources/Detalhes/<nome>.bytes  texturas de detalhe das skins (PNG; o Unity lê com ImageConversion)
// O Unity (Scripts/Visual/ArmaFP.cs) monta a mesma árvore e roda o mesmo motor de animação. A pele das mãos (uma malha
// com esqueleto, cada vértice preso a um osso) sai em pedaços rígidos, um por osso, já no espaço do osso.
// Mudou arma.js ou skins.js? Rode isto de novo (node tools/build.js antes) e depois node tools/unity-fp.js.
// Uso: THREE_DIR=/caminho/node_modules/three NODE_PATH=$(npm root -g) node tools/exportar-primeira-pessoa.js
'use strict';
const fs=require('fs'), path=require('path');
const ROOT=path.join(__dirname,'..'), RES=path.join(ROOT,'unity','Assets','RitualReversal','Resources');

// roda dentro da página (window.__T do #autotest): devolve {dados, geo (base64), texturas {nome: dataURL PNG}} e deixa em
// window.__FPNOS os objetos do three na ordem dos nós (tools/unity-fp.js compara nó a nó)
function coletar(){
  const T=window.__T, A=T.ARMA, S=A.S, V=A.vm, SK=T.SKINS;
  const nos=[], idx=new Map(), objs=[], mats=[], midx=new Map(), geos=[], gidx=new Map(), blocos=[]; let bytes=0;
  const empilha=a=>{ blocos.push(new Uint8Array(a.buffer,a.byteOffset,a.byteLength)); bytes+=a.byteLength; };
  const arr=v=>v.toArray(), r6=x=>Math.round(x*1e6)/1e6;
  const nomeTex=t=>{ if(!t) return null; for(const k in T.TEX) for(const s of ['map','normalMap','roughnessMap']) if(T.TEX[k]&&T.TEX[k][s]===t) return k+'.'+s;
    for(const k in T.RT) if(T.RT[k]===t) return 'RT.'+k; return '?'; };
  function salvarGeo(pos,nor,uv,ind){ const i=geos.length; geos.push({v:pos.length/3,t:ind.length,ini:bytes}); empilha(pos); empilha(nor); empilha(uv); empilha(ind); return i; }
  function triangulos(g){ const n=g.attributes.position.count; return g.index?Array.from(g.index.array):Array.from({length:n},(_,i)=>i); }
  function geoComum(g){ if(gidx.has(g)) return gidx.get(g); const P=g.attributes.position, N=g.attributes.normal, U=g.attributes.uv, n=P.count;
    const pos=new Float32Array(n*3), nor=new Float32Array(n*3), uv=new Float32Array(n*2);
    for(let i=0;i<n;i++){ pos[i*3]=P.getX(i); pos[i*3+1]=P.getY(i); pos[i*3+2]=P.getZ(i); if(N){ nor[i*3]=N.getX(i); nor[i*3+1]=N.getY(i); nor[i*3+2]=N.getZ(i); } if(U){ uv[i*2]=U.getX(i); uv[i*2+1]=U.getY(i); } }
    const k=salvarGeo(pos,nor,uv,Uint32Array.from(triangulos(g))); gidx.set(g,k); return k; }
  // pedaço da pele presa ao osso b: v no osso = boneInverse[b] · bindMatrix · v (o osso leva o pedaço como um filho)
  const pecasCache=new Map();
  function pecaPele(sm,b){ const g=sm.geometry, M=sm.skeleton.boneInverses[b].clone().multiply(sm.bindMatrix), chave=g.uuid+'#'+b+'#'+M.elements.map(x=>x.toFixed(5)).join(',');
    if(pecasCache.has(chave)) return pecasCache.get(chave);
    const P=g.attributes.position, N=g.attributes.normal, U=g.attributes.uv, SI=g.attributes.skinIndex, tri=triangulos(g), Nm=new THREE.Matrix3().getNormalMatrix(M);
    const mapa=new Map(), pos=[], nor=[], uv=[], ind=[], v=new THREE.Vector3(), nn=new THREE.Vector3();
    for(let t=0;t<tri.length;t+=3){ if(SI.getX(tri[t])!==b) continue;
      for(let j=0;j<3;j++){ const i=tri[t+j]; if(!mapa.has(i)){ mapa.set(i,pos.length/3); v.fromBufferAttribute(P,i).applyMatrix4(M); nn.fromBufferAttribute(N,i).applyMatrix3(Nm).normalize();
          pos.push(v.x,v.y,v.z); nor.push(nn.x,nn.y,nn.z); uv.push(U?U.getX(i):0,U?U.getY(i):0); } ind.push(mapa.get(i)); } }
    const k=ind.length?salvarGeo(new Float32Array(pos),new Float32Array(nor),new Float32Array(uv),Uint32Array.from(ind)):-1; pecasCache.set(chave,k); return k; }
  function mat(m){ if(midx.has(m)) return midx.get(m);
    const M={k:m.isMeshStandardMaterial?'std':m.isMeshBasicMaterial?'basico':m.isSpriteMaterial?'sprite':'?',nome:m.name||'',cor:arr(m.color),opac:m.opacity,transp:!!m.transparent,zwrite:m.depthWrite!==false,
      mistura:m.blending===THREE.AdditiveBlending?'aditiva':'normal',tom:m.toneMapped!==false,lado:m.side,mapa:nomeTex(m.map),rep:m.map?arr(m.map.repeat):null};
    if(M.k==='std') Object.assign(M,{rug:m.roughness,met:m.metalness,emis:arr(m.emissive),emisI:m.emissiveIntensity,normal:nomeTex(m.normalMap),nrep:m.normalMap?arr(m.normalMap.repeat):null,
      nesc:m.normalMap?arr(m.normalScale):null,papel:m.userData.papel||null,claro:m.userData.claro||1,base:m.userData.base||null,leitura:!!m.userData.leitura,skin:!!m.userData.skinU});
    if(M.k==='sprite') M.giro=m.rotation;
    const i=mats.length; mats.push(M); midx.set(m,i); return i; }
  // árvore: os pais vêm antes dos filhos; os pedaços da pele entram no fim (os ossos já têm índice)
  function arvore(raiz,paiRaiz,lista,listaObjs,mapa){ const peles=[];
    const no=(o,pai)=>{ const i=lista.length, N={pai,n:o.name||o.type,p:arr(o.position),q:arr(o.quaternion),s:arr(o.scale),vis:o.visible,ordem:o.rotation.order};
      mapa.set(o,i); lista.push(N); listaObjs.push(o);
      if(o.isSkinnedMesh){ N.k='pele'; peles.push([o,i]); }
      else if(o.isMesh){ N.k='m'; N.geo=geoComum(o.geometry); N.mat=mat(o.material); }
      else if(o.isSprite){ N.k='sp'; N.mat=mat(o.material); }
      else N.k=o.isBone?'osso':'g';
      for(const c of o.children) no(c,i); };
    no(raiz,paiRaiz);
    for(const [sm] of peles){ const ossos=sm.skeleton.bones;
      for(let b=0;b<ossos.length;b++){ const g=pecaPele(sm,b); if(g<0) continue; const pai=mapa.get(ossos[b]);
        lista.push({pai,n:'pele '+b,k:'m',geo:g,mat:mat(sm.material),p:[0,0,0],q:[0,0,0,1],s:[1,1,1],vis:sm.visible,ordem:'XYZ'}); listaObjs.push(null); } }
  }
  arvore(V.vmH,-1,nos,objs,idx); arvore(V.vmC,-1,nos,objs,idx);
  const I=o=>o&&idx.has(o)?idx.get(o):-1, IL=L=>L.map(I);
  const W=V.vmCarabina, R=V.vmRevolver, X=V.vmCetro;
  // peças que só ganham pai no meio de um clipe (o municiador vai para a mão e para o tambor): entram como raízes soltas
  for(const o of [R.mun]) if(!idx.has(o)) arvore(o,-1,nos,objs,idx);
  const papeis={vmH:I(V.vmH),vmC:I(V.vmC),flash:I(V.vmFlash),runas:IL(V.vmRunas),orb:I(V.vmOrb),
    carabina:{g:I(W.g),alav:I(W.alav),cao:I(W.cao),port:I(W.port),ferr:I(W.ferr),camara:I(W.camara),boca:arr(W.boca),janela:arr(W.janela),entrada:arr(W.entrada),dirEntrada:arr(W.dirEntrada),rotEntrada:W.rotEntrada,base:W.base.elements.slice()},
    revolver:{g:I(R.g),tambor:I(R.tambor),guin:I(R.guin),PIVO:arr(R.PIVO),ext:I(R.ext),camaras:R.camaras.map(c=>({vivo:I(c.vivo),vazio:I(c.vazio),a:c.a})),cao:I(R.cao),cruz:IL(R.cruz),
      cruzMat:midx.get(R.cruzMat),prata:midx.get(R.prata),mun:I(R.mun),munC:IL(R.munC),trava:I(R.trava),boca:arr(R.boca),base:R.base.elements.slice()},
    cetro:{g:I(X.g),cab:I(X.cab),brasa:I(X.brasa),halo:I(X.halo),runa:I(X.runa),vert:IL(X.vert),corOriginal:X.corOriginal,base:X.base.elements.slice()}};
  const mao=M=>({raiz:I(M.raiz),dedos:M.dedos.map(D=>({segs:IL(D.segs),abre:D.abre})),pol:IL(M.pol),pinca:I(M.pinca),esq:M.esq});
  const rigs={}; for(const k of ['carabina','revolver','cetro']){ const G=S.rigs[k];
    rigs[k]={corpo:I(G.corpo),raiz:I(G.raiz),maos:{D:mao(G.maos.D),E:mao(G.maos.E)},bracos:{D:{sup:I(G.bracos.D.sup),ant:I(G.bracos.D.ant),cot:I(G.bracos.D.cot)},E:{sup:I(G.bracos.E.sup),ant:I(G.bracos.E.ant),cot:I(G.bracos.E.cot)}},
      ombro:{D:arr(G.ombro.D),E:arr(G.ombro.E)},polo:{D:arr(G.polo.D),E:arr(G.polo.E)},osso:G.osso,braco:G.braco}; }
  // clipes preparados (as recargas são funções da munição: todas as variantes)
  const chaveMao=k=>({t:k.t,f:k.f,p:arr(k.p),q:arr(k.q),dd:Array.from(k.dd,r6),e:k.e,reta:k.reta});
  const trilhas=o=>Object.fromEntries(Object.entries(o).map(([k,L])=>[k,L.map(c=>({t:c.t,v:c.v,e:c.e}))]));
  const clipe=C=>({nome:C.nome,dur:C.dur,laco:C.laco,a:C.a.map(k=>({t:k.t,p:arr(k.p),q:arr(k.q),e:k.e})),D:C.D.map(chaveMao),E:C.E.map(chaveMao),c:trilhas(C.c),
    cam:C.cam.map(k=>({t:k.t,p:arr(k.p),e:k.e})),pr:trilhas(C.pr),ev:C.ev});
  const clipes={};
  for(const k in S.clipes){ const C=S.clipes[k]; if(typeof C!=='function'){ clipes[k]=clipe(C); continue; }
    if(k==='carabina_recarga') for(const vz of [true,false]) for(const n of [1,2,3]) clipes[`carabina_recarga_${vz?1:0}_${n}`]=clipe(C(vz,n));
    if(k==='revolver_recarga') for(const vz of [true,false]) clipes[`revolver_recarga_${vz?1:0}`]=clipe(C(vz)); }
  const cte=A.exportar();   // depois das recargas: qMunMao e MUN_NA_MAO saem da do revólver
  // acessórios das skins: monta cada skin que tem acessório, grava a árvore presa à peça e volta para a de serviço
  const acess={}, texturas={};
  for(const arma of ['carabina','revolver','cetro']) for(const sk of SK.SKINS[arma]){
    for(const p of Object.values(sk.papeis||{})) if(p.det&&!texturas[p.det]) texturas[p.det]=SK.textura(p.det).image.toDataURL('image/png');
    if(!(sk.acessorios||[]).length) continue;
    A.trocarSkin(arma,sk.id); const Aw=arma==='carabina'?W:arma==='revolver'?R:X, lista=[];
    for(const g of Aw.acess||[]){ const raizes=g.parent?[g]:(g.userData.pecas||[]);
      for(const r of raizes){ const L=[], O=[], mp=new Map(); arvore(r,-1,L,O,mp);
        const pend=SK.PEND.filter(P=>mp.has(P.pivo)).map(P=>({no:mp.get(P.pivo),comp:P.comp}));
        lista.push({pai:I(r.parent),nos:L,pend}); } }
    acess[arma+':'+sk.id]=lista; A.trocarSkin(arma,SK.SKINS[arma][0].id); }
  const dados={versao:1,nos,materiais:mats,geometrias:geos,papeis,rigs,clipes,dedos:A.DEDOS,perfil:T.PERFIL_ARMA,cte,
    skins:{catalogo:SK.SKINS,colecao:SK.COLECAO,oficios:SK.OFICIOS,acessorios:acess}};
  // binário em base64 (pedaços: o btoa não aguenta uma string enorme de uma vez)
  const tudo=new Uint8Array(bytes); let o=0; for(const b of blocos){ tudo.set(b,o); o+=b.length; }
  let s=''; for(let i=0;i<tudo.length;i+=0x8000) s+=String.fromCharCode.apply(null,tudo.subarray(i,i+0x8000));
  window.__FPNOS=objs; return {dados,geo:btoa(s),texturas}; }

async function abrir(chromium,THREE_DIR){ const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const page=await browser.newPage({viewport:{width:960,height:540}}); const erros=[]; page.on('pageerror',e=>erros.push(e.message));
  await page.route('**/*',r=>{ const u=r.request().url(); if(THREE_DIR&&u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')) return r.fulfill({path:path.join(THREE_DIR,u.split('three@0.128.0/')[1]),contentType:'application/javascript'});
    if(u.includes('fonts.g')) return r.abort(); return r.continue(); });
  await page.goto('file://'+(process.env.HTML||path.join(ROOT,'dist','ritual-reversal.html'))+'#autotest'); await page.waitForFunction(()=>window.__T&&window.__T.ARMA,null,{timeout:180000});
  return {browser,page,erros}; }

module.exports={coletar,abrir};

if(require.main===module) (async()=>{ const {chromium}=require('playwright');
  const {browser,page,erros}=await abrir(chromium,process.env.THREE_DIR);
  const R=await page.evaluate(coletar);
  const geo=Buffer.from(R.geo,'base64');
  fs.writeFileSync(path.join(RES,'primeira-pessoa.json'),JSON.stringify(R.dados));
  fs.writeFileSync(path.join(RES,'primeira-pessoa-geo.bytes'),geo);
  const DET=path.join(RES,'Detalhes'); fs.mkdirSync(DET,{recursive:true});
  for(const [k,url] of Object.entries(R.texturas)) fs.writeFileSync(path.join(DET,k+'.bytes'),Buffer.from(url.split(',')[1],'base64'));
  const D=R.dados, nv=D.geometrias.reduce((s,g)=>s+g.v,0);
  console.log(`${D.nos.length} nós, ${D.materiais.length} materiais, ${D.geometrias.length} geometrias (${nv} vértices, ${(geo.length/1024).toFixed(0)} KB), ${Object.keys(D.clipes).length} clipes, `+
    `${Object.keys(D.skins.acessorios).length} skins com acessório, ${Object.keys(R.texturas).length} texturas de detalhe`);
  const sem=D.materiais.filter(m=>m.mapa==='?'||m.normal==='?'); if(sem.length) console.log('texturas sem nome:',sem.map(m=>m.nome||m.k).join(', '));
  if(erros.length) console.log('ERROS:\n'+erros.join('\n'));
  await browser.close(); process.exit(erros.length||sem.length?1:0); })().catch(e=>{ console.error(e); process.exit(2); });
