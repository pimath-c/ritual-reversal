/* Ritual Reversal — o mundo da v6: catedral em cruz no promontório, desfiladeiro com três travessias e as subáreas
   da mata. Só visual: toda a colisão, a visão e as regras continuam nas caixas de shared/sim.js. Onde a peça visual
   fica mais alta que a caixa (torres, telhados), é porque a altura da caixa só importa até onde alguém consegue atirar.
   Fábrica igual à de client/arma.js: o jogo chama RR_MUNDO({...}) em buildWorld e recebe {PECAS, construir, vida, quadro}. */
window.RR_MUNDO=function(D){
'use strict';
const {THREE,world,Sim,MAT,TEX,RT,meshBox,boxGeo,canvasTex,vr,FONTES,flames,CEU,PERF,VENTO,desgastar}=D;
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v)), lerp=(a,b,t)=>a+(b-a)*t;
const W=Sim.WALLS, PI=Math.PI;
// alturas visuais (as da prancha docs/conceito/catedral.svg)
const ALT={lateral:15,clerestorio:27,cumeeira:38,transepto:27,empena:34,torreO:54,agulhaO:98,lanterna:48,agulhaC:88,torreL:30,abside:27};
const merge=gs=>THREE.BufferGeometryUtils.mergeBufferGeometries(gs.map(g=>g.index?g.toNonIndexed():g),false);
const add=(m,sombra)=>{ m.castShadow=sombra!==false; m.receiveShadow=true; world.add(m); return m; };
// ---------------- materiais próprios ----------------
const M={};
{ const dup=(base,cor,gasto)=>{ const m=base.clone(); m.color=new THREE.Color(cor); return gasto&&desgastar?desgastar(m,gasto):m; };
  M.pedra=dup(MAT.wall,0x8a8a90,{mancha:.45,base:.45,musgo:.7}); M.pedraEsc=dup(MAT.wall,0x5e5e68,{mancha:.4,base:.35,musgo:.5}); M.pedraClara=dup(MAT.wall,0xa8a49a,{mancha:.35,base:.3,musgo:.3});
  M.telha=MAT.wallDark.clone(); M.telha.color=new THREE.Color(0x2c2a30); M.telha.side=THREE.DoubleSide;
  M.teto=MAT.wallDark.clone(); M.teto.color=new THREE.Color(0x24242c); M.teto.side=THREE.DoubleSide;
  M.chumbo=new THREE.MeshStandardMaterial({color:0x3a3e48,roughness:.55,metalness:.5,flatShading:true});
  M.rocha=new THREE.MeshStandardMaterial({map:TEX.rocha.map,normalMap:TEX.rocha.normalMap,roughness:1,color:0x6a6862,side:THREE.DoubleSide}); M.rocha.normalScale.set(1.4,1.4);
  for(const k of ['map','normalMap']){ M.rocha[k]=M.rocha[k].clone(); M.rocha[k].repeat.set(.25,.25); M.rocha[k].needsUpdate=true; }
  M.fundo=new THREE.MeshBasicMaterial({color:0x020204});
  M.vidro=new THREE.MeshBasicMaterial({map:RT.win,transparent:true,alphaTest:.1,side:THREE.DoubleSide,color:new THREE.Color(1.05,1,1.2),fog:false,toneMapped:false});
  M.vidroFora=M.vidro.clone(); M.vidroFora.color=new THREE.Color(.55,.5,.72);
  M.rosa=new THREE.MeshBasicMaterial({map:RT.rose,transparent:true,alphaTest:.1,side:THREE.DoubleSide,color:new THREE.Color(1.1,1.02,1.2),fog:false,toneMapped:false});
  M.feixe=new THREE.MeshBasicMaterial({map:RT.shaft,transparent:true,opacity:.075,color:0x8fa6d8,blending:THREE.AdditiveBlending,depthWrite:false,side:THREE.DoubleSide,fog:false});
  if(CEU.vitrais) for(const m of [M.vidro,M.vidroFora,M.rosa]) CEU.vitrais.push({m,c:m.color.clone()}); }
// Névoa de marco: as torres, agulhas e telhados da catedral sentem só um quarto da névoa. De qualquer ponto da mata,
// por cima das copas, a silhueta continua legível (diretriz: marcos visíveis de vários lugares para navegar e combinar).
function marco(mat){ const m=mat.clone(), antes=mat.onBeforeCompile, chave=mat.customProgramCacheKey?mat.customProgramCacheKey():'';
  m.onBeforeCompile=(sh,r)=>{ if(antes) antes.call(mat,sh,r); sh.fragmentShader=sh.fragmentShader.replace('#include <fog_fragment>',
  `#ifdef USE_FOG
    float fogFactor = 1.0 - exp( - fogDensity * fogDensity * fogDepth * fogDepth * .07 );
    gl_FragColor.rgb = mix( gl_FragColor.rgb, fogColor, fogFactor );
  #endif`); }; m.customProgramCacheKey=()=>'marco'+chave; return m; }
const MK={pedra:marco(M.pedraEsc),telha:marco(M.telha),chumbo:marco(M.chumbo)};
MK.pedra.color=new THREE.Color(0x3c3c46); MK.telha.color=new THREE.Color(0x1e1e24);
// ---------------- ajudantes de geometria ----------------
const B=(x1,x2,y1,y2,z1,z2,mat,S)=>meshBox(Math.min(x1,x2),Math.max(x1,x2),y1,y2,Math.min(z1,z2),Math.max(z1,z2),mat||M.pedra,S||4);
// janela gótica (lanceta) plana: vidro dos dois lados de uma parede; n = normal da face (+1/-1) no eixo dado
const vidros=[], vidrosFora=[], rosas=[], feixes=[], fundos=[];
function lanceta(eixo,fixo,u,y,larg,alt,n,feixe){ const mk=(dx,lista)=>{ const g=new THREE.PlaneGeometry(larg,alt); g.translate(0,y+alt/2,0);
    if(eixo==='x'){ if(n*dx<0) g.rotateY(PI); g.translate(u,0,fixo+dx); } else { g.rotateY(n*dx>0?PI/2:-PI/2); g.translate(fixo+dx,0,u); } lista.push(g); };
  mk(-n*.56,vidros); mk(n*.56,vidrosFora);
  if(feixe){ const L=alt*1.9, g=new THREE.PlaneGeometry(larg*1.1,L), g2=g.clone(); g2.rotateY(PI/2); const gg=merge([g,g2]); gg.translate(0,-L/2,0); gg.rotateX(n*(eixo==='x'?-.62:0)); gg.rotateZ(n*(eixo==='z'?.62:0));
    if(eixo==='x') gg.translate(u,y+alt*.8,fixo-n*.2); else gg.translate(fixo-n*.2,y+alt*.8,u); feixes.push(gg); } }
function rosacea(eixo,fixo,u,y,r,n){ for(const [dx,cor] of [[-n*.56,0],[n*.56,1]]){ const g=new THREE.CircleGeometry(r,40); if(eixo==='x'){ if(n*dx<0) g.rotateY(PI); g.translate(u,y,fixo+dx); } else { g.rotateY(n*dx>0?PI/2:-PI/2); g.translate(fixo+dx,y,u); } (cor?vidrosFora:rosas).push(g); }
  const anel=new THREE.Mesh(new THREE.TorusGeometry(r+.25,.32,8,40),M.pedraEsc); anel.position.set(eixo==='x'?u:fixo,y,eixo==='x'?fixo:u); if(eixo!=='x') anel.rotation.y=PI/2; add(anel,false); }
// telhado de duas águas ao longo de x ou z (cumeeira no meio); com empena nas pontas pedidas
function telhado(x1,x2,z1,z2,yBase,yTopo,aoLongoX,mat,empenas){ const g=new THREE.Group(); world.add(g);
  const larg=(aoLongoX?z2-z1:x2-x1)/2, comp=aoLongoX?x2-x1:z2-z1, alt=yTopo-yBase, lado=Math.hypot(alt,larg), inc=Math.atan2(alt,larg);
  g.position.set((x1+x2)/2,yBase,(z1+z2)/2); if(!aoLongoX) g.rotation.y=PI/2;
  for(const s of [-1,1]){ const t=new THREE.Mesh(new THREE.PlaneGeometry(comp+.6,lado+.5),mat||MK.telha); t.position.set(0,alt/2,s*larg/2); t.rotation.x=-PI/2+s*inc; t.castShadow=true; g.add(t); }
  for(const e of (empenas||[])){ const sh=new THREE.Shape(); sh.moveTo(-larg,0); sh.lineTo(larg,0); sh.lineTo(0,alt); sh.closePath(); const f=new THREE.Mesh(new THREE.ExtrudeGeometry(sh,{depth:.8,bevelEnabled:false}),MK.pedra); f.rotation.y=PI/2; f.position.set(e*comp/2-(e>0?.8:0),0,0); f.castShadow=true; g.add(f); }
  const c=new THREE.Mesh(new THREE.BoxGeometry(comp+.6,.35,.35),MK.chumbo); c.position.y=alt+.05; g.add(c); return g; }
function pinaculo(x,y,z,h,r,mat){ const b=new THREE.Mesh(new THREE.BoxGeometry(r*2,h*.35,r*2),mat||MK.pedra); b.position.set(x,y+h*.175,z); add(b);
  const c=new THREE.Mesh(new THREE.ConeGeometry(r*1.05,h*.65,4),mat||MK.pedra); c.position.set(x,y+h*.35+h*.325,z); c.rotation.y=PI/4; add(c); }
function agulha(x,z,yBase,yTopo,r,lados){ const c=new THREE.Mesh(new THREE.ConeGeometry(r,yTopo-yBase,lados||8,6),MK.pedra); c.position.set(x,(yBase+yTopo)/2,z); c.rotation.y=PI/8; add(c);
  // cordões de pedra subindo a agulha e uma cruz de ferro no alto
  for(let k=1;k<6;k++){ const t=k/6, a=new THREE.Mesh(new THREE.TorusGeometry(r*(1-t)+.05,.12,4,lados||8),MK.pedra); a.rotation.x=PI/2; a.rotation.z=PI/8; a.position.set(x,yBase+(yTopo-yBase)*t,z); add(a,false); }
  const cz=new THREE.Mesh(new THREE.BoxGeometry(.25,3,.25),MK.chumbo); cz.position.set(x,yTopo+1.4,z); add(cz,false); const cb=new THREE.Mesh(new THREE.BoxGeometry(1.4,.22,.22),MK.chumbo); cb.position.set(x,yTopo+1.9,z); add(cb,false); }
// arcobotante: arco do contraforte até a parede alta
function arcobotante(x0,y0,x1,y1,z,mat){ const curva=new THREE.QuadraticBezierCurve3(new THREE.Vector3(x0,y0,z),new THREE.Vector3(x0+(x1-x0)*.25,y1+1.2,z),new THREE.Vector3(x1,y1,z));
  const g=new THREE.TubeGeometry(curva,10,.42,4,false); add(new THREE.Mesh(g,mat||MK.pedra)); }
// quadrilátero p1-p2-p3-p4 (no sentido em que ficam), dos dois lados
function quad(p1,p2,p3,p4,mat){ const g=new THREE.BufferGeometry(), v=new Float32Array([...p1,...p2,...p3,...p1,...p3,...p4]); g.setAttribute('position',new THREE.BufferAttribute(v,3));
  const L=Math.hypot(p2[0]-p1[0],p2[1]-p1[1],p2[2]-p1[2]), H=Math.hypot(p4[0]-p1[0],p4[1]-p1[1],p4[2]-p1[2]); g.setAttribute('uv',new THREE.BufferAttribute(new Float32Array([0,0,L/4,0,L/4,H/4,0,0,L/4,H/4,0,H/4]),2)); g.computeVertexNormals();
  return add(new THREE.Mesh(g,mat||MK.telha)); }
function nervura(w,alto,tubo){ const R=(w*w/4+alto*alto)/(2*alto), f=Math.asin(Math.min(1,(w/2)/R)); const g=new THREE.TorusGeometry(R,tubo||.24,5,24,2*f); g.rotateZ(PI/2-f); g.translate(0,-R,0); return g; }

// ================= chão: promontório, catedral, desfiladeiro =================
// (o chão da mata vem de buildFloresta, que afunda o desfiladeiro e as travessias; aqui ficam os pisos de pedra)
function pisos(){ const piso=(x1,x2,z1,z2,mat,y,rep)=>{ const m=new THREE.Mesh(new THREE.PlaneGeometry(x2-x1,z2-z1),mat); const mt=m.material; if(rep){ m.material=mt.clone(); for(const k of ['map','normalMap','roughnessMap']) if(m.material[k]){ m.material[k]=m.material[k].clone(); m.material[k].repeat.set((x2-x1)/rep,(z2-z1)/rep); m.material[k].needsUpdate=true; } }
    m.rotation.x=-PI/2; m.position.set((x1+x2)/2,y||.012,(z1+z2)/2); m.receiveShadow=true; world.add(m); return m; };
  const lajes=MAT.floor;
  for(const Z of Sim.ZONAS) if(Z.name==='Catedral') piso(Z.x1,Z.x2,Z.z1,Z.z2,lajes,.014,4);
  piso(-11.5,11.5,-97,-78,lajes,.016,4); // coro e abside (a meia-volta da abside cai sobre o desfiladeiro? não: termina em z -96)
  piso(-46,46,-32,-22,lajes,.012,3.5); // adro
  piso(-44.5,-15.5,-64.5,-43.5,lajes,.013,4); // galerias do claustro
  const jardim=new THREE.MeshStandardMaterial({color:0x34402a,map:TEX.chao?TEX.chao.map:null,roughness:1}); piso(Sim.CLAUSTRO.x1,Sim.CLAUSTRO.x2,Sim.CLAUSTRO.z1,Sim.CLAUSTRO.z2,jardim,.02);
  // caminho de lajes do adro até a Sala do Capítulo e até o cemitério
  piso(-70,-44,-46,-40,lajes,.011,3.5); piso(28,46,-36,-30,lajes,.011,3.5); }
// paredões do desfiladeiro: uma cortina de rocha em cada lado das fatias que dá para a terra firme
function desfiladeiro(){ const A=Sim.ABISMO, T=Sim.TRAVESSIAS, FUNDO=-34, D0=Sim.DESFILADEIRO, HZ=Sim.HALF.z;
  const noAbismo=(x,z)=>A.some(b=>x>b.x1-.05&&x<b.x2+.05&&z>b.z1-.05&&z<b.z2+.05)||T.some(t=>t.eixo==='z'?(x>t.a-1.05&&x<t.b+1.05&&z>t.de&&z<t.ate):(z>t.a-1.05&&z<t.b+1.05&&x>t.de&&x<t.ate));
  const geos=[]; let sd=90; const R=()=>((sd=(sd*16807)%2147483647)/2147483647);
  // relevo da rocha: ruído suave que depende só da posição no mundo, então as fatias de 1 m se encaixam sem fenda
  const hs=(i,j)=>{ const s=Math.sin(i*127.1+j*311.7)*43758.5453; return s-Math.floor(s); };
  const vn=(x,y)=>{ const i=Math.floor(x), j=Math.floor(y), fx=x-i, fy=y-j, ux=fx*fx*(3-2*fx), uy=fy*fy*(3-2*fy);
    return lerp(lerp(hs(i,j),hs(i+1,j),ux),lerp(hs(i,j+1),hs(i+1,j+1),ux),uy); };
  const relevo=(s,y)=>vn(s*.18,y*.12)*.6+vn(s*.5+7,y*.4)*.28+vn(s*1.6,y*1.3)*.12;
  const face=(ax,az,bx,bz,nx,nz)=>{ const L=Math.hypot(bx-ax,bz-az), n=Math.max(1,Math.round(L)); for(let k=0;k<n;k++){ const t0=k/n, t1=(k+1)/n, mx=lerp(ax,bx,(t0+t1)/2), mz=lerp(az,bz,(t0+t1)/2);
      if(noAbismo(mx+nx*.6,mz+nz*.6)) continue; // do outro lado também é abismo: não é borda
      const g=new THREE.PlaneGeometry(L/n,-FUNDO,1,12); g.translate(0,FUNDO/2,0); g.rotateY(Math.atan2(-nx,-nz)); g.translate(mx-nx*.02,0,mz-nz*.02);
      const p=g.attributes.position, uv=g.attributes.uv;
      for(let i=0;i<p.count;i++){ const x=p.getX(i), y=p.getY(i), z=p.getZ(i), s=nx?z:x, borda=clamp(-y/2.5,0,1);
        const d=relevo(s,y)*2.4*borda; p.setXYZ(i,x-nx*d,y,z-nz*d); uv.setXY(i,s/3,y/3); }
      g.computeVertexNormals(); geos.push(g); } };
  for(const b of A){ face(b.x1,b.z1,b.x1,b.z2,-1,0); face(b.x2,b.z1,b.x2,b.z2,1,0); if(b.z1>-HZ+.1) face(b.x1,b.z1,b.x2,b.z1,0,-1); face(b.x1,b.z2,b.x2,b.z2,0,1); }
  // atrás do promontório (ao norte) o mapa termina num penhasco: a mesma rocha, voltada para o vazio
  { const n=Math.round(D0.x2-D0.x1); for(let k=0;k<n;k++){ const x=D0.x1+k+.5, g=new THREE.PlaneGeometry(1,60,1,14); g.translate(0,-30,0); g.rotateY(PI); g.translate(x,0,-HZ-.6);
      const p=g.attributes.position, uv=g.attributes.uv; for(let i=0;i<p.count;i++){ const px=p.getX(i), y=p.getY(i); p.setZ(i,-HZ-.6-relevo(px+40,y)*3*clamp(-y/2.5,0,1)); uv.setXY(i,px/3,y/3); }
      g.computeVertexNormals(); geos.push(g); } }
  // pedras soltas na beira, só do lado do abismo (não invadem o chão onde se anda)
  for(const b of A) for(const [ax,az,bx,bz,nx,nz] of [[b.x1,b.z1,b.x1,b.z2,-1,0],[b.x2,b.z1,b.x2,b.z2,1,0],[b.x1,b.z2,b.x2,b.z2,0,1]]){ const L=Math.hypot(bx-ax,bz-az);
    for(let t=1.5;t<L;t+=3+R()*3){ const x=lerp(ax,bx,t/L)-nx*.7, z=lerp(az,bz,t/L)-nz*.7; if(noAbismo(x+nx*1.3,z+nz*1.3)) continue; const r=new THREE.Mesh(new THREE.DodecahedronGeometry(.45+R()*.35,0),M.rocha); r.position.set(x,-.15+R()*.2,z); r.rotation.set(R()*3,R()*3,R()*3); r.scale.y=.6; add(r); } }
  if(geos.length){ const m=new THREE.Mesh(merge(geos),M.rocha); m.receiveShadow=true; m.castShadow=false; world.add(m); }
  // fundo do abismo e camadas de névoa que se mexem devagar (a névoa some quem cai no olhar; ninguém cai de verdade)
  const fundo=new THREE.Mesh(new THREE.PlaneGeometry(D0.x2-D0.x1+10,D0.z2-D0.z1+70),new THREE.MeshBasicMaterial({color:0x010102}));
  fundo.rotation.x=-PI/2; fundo.position.set(0,FUNDO+.5,(D0.z1+D0.z2)/2-30); world.add(fundo);
  const tex=canvasTex(256,256,(c,Wd,H)=>{ c.clearRect(0,0,Wd,H); let s2=5; const r=()=>((s2=(s2*16807)%2147483647)/2147483647);
    for(let k=0;k<70;k++){ const x=r()*Wd, y=r()*H, rr=20+r()*60, g=c.createRadialGradient(x,y,0,x,y,rr); g.addColorStop(0,'rgba(180,196,226,.32)'); g.addColorStop(1,'rgba(180,196,226,0)'); c.fillStyle=g; c.fillRect(0,0,Wd,H); } });
  tex.wrapS=tex.wrapT=THREE.RepeatWrapping;
  const nev=[]; for(const [y,op,rep] of [[-3.5,.32,7],[-9,.42,5],[-17,.55,4]]){ const mt=new THREE.MeshBasicMaterial({map:tex.clone(),transparent:true,opacity:op,depthWrite:false,color:0x9aaacc,fog:true});
    mt.map.wrapS=mt.map.wrapT=THREE.RepeatWrapping; mt.map.repeat.set(rep*2.2,rep); mt.map.needsUpdate=true;
    const p=new THREE.Mesh(new THREE.PlaneGeometry(D0.x2-D0.x1+6,D0.z2-D0.z1+66),mt); p.rotation.x=-PI/2; p.position.set(0,y,(D0.z1+D0.z2)/2-30); p.renderOrder=3; p.userData.dinamico=true; world.add(p); nev.push(p); }
  return nev; }
// ================= travessias =================
function pontes(){ const [PG,PC,AQ]=Sim.TRAVESSIAS;
  // Ponte Grande: tabuleiro de pedra, um arco grande sobre o vazio e os encontros descendo até sumir na névoa
  { const sh=new THREE.Shape(); sh.moveTo(PG.de,0); sh.lineTo(PG.ate,0); sh.lineTo(PG.ate,-16); sh.absarc((PG.de+PG.ate)/2,-16,(PG.ate-PG.de)/2-1.2,0,PI,false); sh.lineTo(PG.de,-16); sh.lineTo(PG.de,0);
    const g=new THREE.ExtrudeGeometry(sh,{depth:PG.b-PG.a+2,bevelEnabled:false}); g.rotateY(-PI/2); g.translate(PG.b+1,0,0);
    // o Shape foi desenhado em (z, y): gira para que o comprimento fique ao longo de z
    const m=new THREE.Mesh(g,M.pedraEsc); m.rotation.set(0,0,0); add(m);
    for(const z of [PG.de+.6,PG.ate-.6]) B(PG.a-1.4,PG.b+1.4,-34,-15,z-1,z+1,M.pedraEsc);
    B(PG.a-1,PG.b+1,-.6,.02,PG.de,PG.ate,MAT.floor,3); }
  // Ponte Caída: o tronco de uma árvore anciã que caiu sobre o abismo; o topo do tronco é o chão de quem passa
  { const z=(PC.a+PC.b)/2, r=2.35, L=PC.ate-PC.de+6, c=new THREE.Mesh(new THREE.CylinderGeometry(r*.92,r*1.12,L,18,6),MAT.casca); c.rotation.z=PI/2; c.position.set((PC.de+PC.ate)/2-1,-r+.04,z); add(c);
    const p=c.geometry.attributes.position; for(let i=0;i<p.count;i++){ const y=p.getY(i); p.setX(i,p.getX(i)*(1+.06*Math.sin(y*1.7))); } c.geometry.computeVertexNormals();
    // a raiz arrancada na cabeceira da mata e os galhos quebrados no lado do promontório
    const raiz=new THREE.Mesh(new THREE.SphereGeometry(4.2,12,8),MAT.casca); raiz.scale.set(.45,1,1.15); raiz.position.set(PC.de-3.6,-1.5,z); add(raiz);
    for(let k=0;k<10;k++){ const a=k/10*PI*2, g=new THREE.Mesh(new THREE.ConeGeometry(.35,3.4,5),MAT.casca); g.position.set(PC.de-4.2,-1.5+Math.sin(a)*2.8,z+Math.cos(a)*3.2); g.rotation.set(Math.cos(a)*1.2,0,PI/2+Math.sin(a)*1.1); add(g); }
    for(const [dx,ang,dz] of [[2,1.1,-2.6],[4.5,-1,2.7],[7,.9,-2.9]]){ const g=new THREE.Mesh(new THREE.CylinderGeometry(.18,.5,5,6),MAT.casca); g.position.set(PC.ate-dx,-.5,z+dz); g.rotation.set(ang,0,.5); add(g); }
    const musgo=new THREE.Mesh(new THREE.CylinderGeometry(r*.94,r*1.13,L*.7,18,1,true,PI*.65,PI*.7),new THREE.MeshStandardMaterial({color:0x334a2a,roughness:1,side:THREE.DoubleSide})); musgo.rotation.z=PI/2; musgo.position.copy(c.position); musgo.position.y+=.03; add(musgo,false); }
  // Aqueduto: passarela de pedra sobre arcos altos e estreitos, que descem até o fundo
  { const z1=AQ.a-1, z2=AQ.b+1, x1=AQ.de, x2=AQ.ate; B(x1,x2,-1.4,.02,z1,z2,M.pedra,3);
    for(const x of [x1+.2,(x1+x2)/2,x2-.2]) B(x-1,x+1,-34,-1.4,z1,z2,M.pedraEsc);
    for(const [a,b] of [[x1+.2,(x1+x2)/2],[(x1+x2)/2,x2-.2]]){ const sh=new THREE.Shape(); sh.moveTo(a,-1.4); sh.lineTo(b,-1.4); sh.lineTo(b,-9); sh.absarc((a+b)/2,-9,(b-a)/2-1,0,PI,false); sh.lineTo(a,-9); sh.lineTo(a,-1.4);
      const g=new THREE.ExtrudeGeometry(sh,{depth:z2-z1,bevelEnabled:false}); g.translate(0,0,z1); add(new THREE.Mesh(g,M.pedraEsc)); }
    // arco de entrada sobre as duas cabeceiras (os pilares são peças da simulação)
    for(const x of [81,99]){ const a=new THREE.Mesh(new THREE.TorusGeometry((-73.1+80.9)/2,.5,6,16,PI),MAT.pedraVelha); a.position.set(x,7.6,-77); a.rotation.y=PI/2; add(a); } } }

// ================= a catedral por fora =================
function catedral(){
  // ---- nave: paredes laterais baixas (naves laterais) e o clerestório alto sobre os pilares ----
  const zN1=-64, zN2=-32;
  for(const s of [-1,1]){ const x=s*16;
    // parede lateral (com a porta do claustro a oeste e a brecha da torre a leste)
    const vaos=s<0?[[-56,-52]]:[[-53,-49]], pts=[zN1,...vaos.flat(),zN2];
    for(let i=0;i<pts.length;i+=2){ B(x-.5,x+.5,0,ALT.lateral,pts[i],pts[i+1],M.pedra,4); }
    for(const [a,b] of vaos) B(x-.5,x+.5,s<0?7:5.5+vr()*2,ALT.lateral,a,b,M.pedra,4);
    B(x-.75*s,x+.25*s,ALT.lateral,ALT.lateral+.5,zN1,zN2,M.pedraEsc); // cornija
    for(const y of [1.2,11.6]) B(x+s*.5,x+s*.75,y,y+.35,zN1,zN2,M.pedraEsc); // cordões de pedra na face de fora
    // contrafortes com pináculos e arcobotantes até o clerestório; janelas entre eles
    for(const z of [-40,-46,-52,-58]){ B(x+s*.5,x+s*2.3,0,ALT.lateral-1,z-.9,z+.9,MK.pedra); B(x+s*.5,x+s*1.6,ALT.lateral-1,ALT.lateral+3,z-.7,z+.7,MK.pedra);
      pinaculo(x+s*1.05,ALT.lateral+3,z,5,.55); arcobotante(x+s*1.05,ALT.lateral+1.5,s*8.5,ALT.clerestorio-4.5,z); }
    for(const z of [-37,-43,-49,-55,-61]){ if(s>0&&z===-49) continue; if(s<0&&z===-55) continue; lanceta('z',x,z,3,2.2,7.5,s,true); }
    // clerestório: parede alta a 8,5 m do eixo, sobre os arcos dos pilares; janelas altas entre os pilares
    B(s*8-.5,s*8+.5,ALT.lateral-1,ALT.clerestorio,zN1,-40,MK.pedra,4);
    for(const z of [-43,-49,-55,-61]) lanceta('z',s*8,z,ALT.lateral+2.5,2,7.2,s,true);
    // telhado de uma água das naves laterais, do pé do clerestório até a cornija
    quad([s*8.5,ALT.lateral+3.6,zN1],[s*8.5,ALT.lateral+3.6,zN2],[s*16.9,ALT.lateral+.4,zN2],[s*16.9,ALT.lateral+.4,zN1]); }
  telhado(-8.5,8.5,-64,-32,ALT.clerestorio,ALT.cumeeira,false,MK.telha,[]);
  // ---- fachada: empena, galeria de reis, rosácea, portal com arquivoltas, nichos ----
  { const z=-32; B(-16.5,-4,0,ALT.clerestorio,z-.5,z+.5,M.pedra); B(4,16.5,0,ALT.clerestorio,z-.5,z+.5,M.pedra); B(-4,4,9.6,ALT.clerestorio,z-.5,z+.5,M.pedra);
    const sh=new THREE.Shape(); sh.moveTo(-16.5,0); sh.lineTo(16.5,0); sh.lineTo(0,9); sh.closePath(); const emp=new THREE.Mesh(new THREE.ExtrudeGeometry(sh,{depth:1.2,bevelEnabled:false}),MK.pedra); emp.position.set(0,ALT.clerestorio,z-.6); add(emp);
    for(let k=0;k<9;k++){ const x=-12+k*3; B(x-.8,x+.8,22,25.4,z+.5,z+1.1,M.pedraEsc,2); const est=new THREE.Mesh(new THREE.CylinderGeometry(.32,.4,2.4,8),M.pedraClara); est.position.set(x,23.4,z+.9); add(est); const cab=new THREE.Mesh(new THREE.SphereGeometry(.28,8,6),M.pedraClara); cab.position.set(x,24.85,z+.9); add(cab); }
    rosacea('x',z,0,16,4.6,1);
    for(let k=0;k<5;k++){ const r=4.6+k*.75, a=new THREE.Mesh(new THREE.TorusGeometry(r,.32,6,28,PI),k%2?M.pedraEsc:M.pedra); a.position.set(0,5.4,z+.6+k*.12); add(a); }
    for(let k=0;k<5;k++) for(const sx of [-1,1]) B(sx*(4.2+k*.75)-.3,sx*(4.2+k*.75)+.3,0,5.4,z+.4,z+1.2,k%2?M.pedraEsc:M.pedra,2);
    const tim=new THREE.Mesh(new THREE.CircleGeometry(4,24,0,PI),M.pedraEsc); tim.position.set(0,5.6,z+.52); add(tim,false);
    for(const x of [-9.5,9.5,-12.5,12.5]){ B(x-1,x+1,6.5,11,z+.5,z+1.1,M.pedraEsc,2); const s=new THREE.Mesh(new THREE.CylinderGeometry(.35,.45,3.2,8),M.pedraClara); s.position.set(x,8.2,z+.85); add(s); }
    for(const x of [-16.5,16.5]){ pinaculo(x,ALT.clerestorio,z,9,.7); } pinaculo(0,ALT.clerestorio+9,z,4,.4); }
  // ---- nártex: teto mais baixo, três arcos para a nave ----
  B(-15.5,15.5,ALT.clerestorio-6,ALT.clerestorio-5.6,-39.5,-32.5,M.teto);
  for(const [a,b] of [[-12,-8],[-4,4],[8,12]]){ const c=(a+b)/2, w=b-a, ar=new THREE.Mesh(new THREE.TorusGeometry(w/2,.3,6,18,PI),M.pedraEsc); ar.position.set(c,w>5?9:6.5,-40); add(ar); }
  for(const [a,b] of [[-15.5,-12],[-8,-4],[4,8],[12,15.5]]) B(a,b,0,ALT.clerestorio-5,-40.5,-39.5,M.pedra);
  for(const [a,b,y] of [[-12,-8,6.5],[-4,4,9],[8,12,6.5]]) B(a,b,y,ALT.clerestorio-5,-40.5,-39.5,M.pedra);
  // ---- torre oeste inteira: sineira acesa e agulha; torre leste partida ----
  { const x1=-28,x2=-16.5,z1=-44,z2=-31.5, cx=(x1+x2)/2, cz=(z1+z2)/2;
    B(x1,x2,0,ALT.torreO,z1,z2,MK.pedra,4); for(const y of [14,28,40]) B(x1-.5,x2+.5,y,y+.7,z1-.5,z2+.5,MK.pedra);
    for(const [x,z] of [[x1,z1],[x2,z1],[x1,z2],[x2,z2]]){ B(x-1.1,x+1.1,0,ALT.torreO-4,z-1.1,z+1.1,MK.pedra); pinaculo(x,ALT.torreO,z,9,1.05); }
    for(const [sx,sz,ry] of [[0,1,0],[0,-1,PI],[-1,0,-PI/2],[1,0,PI/2]]) for(const o of [-2.6,2.6]){ const v=new THREE.Mesh(new THREE.PlaneGeometry(2.1,7.5),M.fundo); v.position.set(cx+sx*(5.8)+(sz?o:0),ALT.torreO-8,cz+sz*(6.3)+(sx?o:0)); v.rotation.y=ry; world.add(v); }
    // arcadas cegas em dois andares em cada face: a torre deixa de ser um bloco liso
    for(const [sx,sz,ry] of [[0,1,0],[0,-1,PI],[-1,0,-PI/2],[1,0,PI/2]]) for(const [y,h] of [[16,9],[30,7]]) for(const o of [-3.3,-1.1,1.1,3.3]){
      const g=new THREE.PlaneGeometry(1.4,h); const vv=new THREE.Mesh(g,M.fundo); vv.position.set(cx+sx*(5.78)+(sz?o:0),y+h/2,cz+sz*(6.28)+(sx?o:0)); vv.rotation.y=ry; world.add(vv);
      const ar=new THREE.Mesh(new THREE.TorusGeometry(.7,.14,4,10,PI),MK.pedra); ar.position.set(vv.position.x+sx*.04,y+h,vv.position.z+sz*.04); ar.rotation.y=ry; world.add(ar); }
    agulha(cx,cz,ALT.torreO,ALT.agulhaO,5.2,8);
    const sino=new THREE.Mesh(new THREE.CylinderGeometry(.9,1.5,2.2,14,1,true),MAT.metal); sino.material.side=THREE.DoubleSide; sino.position.set(cx,ALT.torreO-7,cz); world.add(sino);
    // a luz da sineira é o farol que orienta quem está na mata
    FONTES.push({x:cx,z:cz,y:ALT.torreO-8,cor:0xffa050,int:2.2,alc:26}); const brilho=new THREE.Sprite(new THREE.SpriteMaterial({map:RT.glow,color:0xffb060,blending:THREE.AdditiveBlending,depthWrite:false,transparent:true,opacity:.9,fog:false}));
    brilho.scale.set(9,9,1); brilho.position.set(cx,ALT.torreO-8,cz+6.6); world.add(brilho); CEU.farolTorre=brilho; }
  { const x1=16.5,x2=28,z1=-44,z2=-31.5; const perfil=[[x1,ALT.torreL-6],[18,ALT.torreL-2],[19.4,ALT.torreL-4],[21.2,ALT.torreL+2],[22.8,ALT.torreL-1.5],[24.6,ALT.torreL+.5],[26.2,ALT.torreL-7],[x2,ALT.torreL-5]];
    for(let i=0;i<perfil.length-1;i++){ const [a,ha]=perfil[i],[b,hb]=perfil[i+1]; B(a,b,0,Math.min(ha,hb),z1,z2,M.pedra,4); B(a,b,Math.min(ha,hb),Math.max(ha,hb)-.4,z1+.6+vr()*1.5,z2-.6-vr()*1.5,M.pedra,4); }
    B(x1-.5,x2+.5,14,14.7,z1-.5,z2+.5,M.pedraEsc);
    for(let k=0;k<6;k++){ const v=new THREE.Mesh(new THREE.BoxGeometry(.35,.35,6+vr()*4),MAT.wood); v.position.set(x1+1.5+k*1.8,ALT.torreL-3+vr()*3,(z1+z2)/2); v.rotation.set(vr()-.5,vr()*.4,.3+vr()*.4); add(v); } }
  // ---- transepto: paredes altas, empenas nas pontas com rosáceas; o braço leste perdeu o teto ----
  const vaosT=(fixo,a,b)=>Sim.PORTAIS.filter(p=>p.eixo==='x'&&p.fixo===fixo&&p.a>=a-.1&&p.b<=b+.1).map(p=>[p.a,p.b]);
  for(const [fixo,face] of [[-64,1],[-78,-1]]) for(const [a,b] of fixo===-64?[[-44.5,-15.5],[15.5,44.5]]:[[-44.5,-11.5],[11.5,44.5]]){ const leste=a>0, vaos=vaosT(fixo,a,b), pts=[a,...vaos.flat(),b];
    for(let i=0;i<pts.length;i+=2) B(pts[i],pts[i+1],0,leste&&fixo===-64?ALT.transepto-9:ALT.transepto,fixo-.5,fixo+.5,M.pedra,4);
    for(const [va,vb] of vaos){ const p=Sim.PORTAIS.find(q=>q.eixo==='x'&&q.fixo===fixo&&q.a===va); if(!p||!p.brecha) B(va,vb,6,ALT.transepto,fixo-.5,fixo+.5,M.pedra,4); else B(va,vb,13+vr()*4,ALT.transepto-9,fixo-.5,fixo+.5,M.pedra,4); }
    for(let x=a+5;x<b-3;x+=7){ if(vaos.some(([va,vb])=>x>va-3&&x<vb+3)) continue; B(x-.9,x+.9,0,ALT.transepto-2,fixo+face*.5,fixo+face*2.2,MK.pedra); pinaculo(x,ALT.transepto-2,fixo+face*1.35,5,.6); if(!(leste&&fixo===-64)) lanceta('x',fixo,x+3.5,4,2.2,9,face,true); } }
  for(const s of [-1,1]){ const x=s*44, vaos=[[-73,-69]], pts=[-78,-73,-69,-64];
    for(let i=0;i<pts.length;i+=2) B(x-.5,x+.5,0,ALT.transepto,pts[i],pts[i+1],M.pedra,4); B(x-.5,x+.5,6,ALT.transepto,-73,-69,M.pedra,4);
    if(s<0||true){ const sh=new THREE.Shape(); sh.moveTo(-7,0); sh.lineTo(7,0); sh.lineTo(0,ALT.empena-ALT.transepto); sh.closePath(); const e=new THREE.Mesh(new THREE.ExtrudeGeometry(sh,{depth:1,bevelEnabled:false}),MK.pedra);
      if(s>0){ e.scale.set(1,.55,1); } e.rotation.y=PI/2; e.position.set(x-.5,ALT.transepto,-71); add(e); }
    rosacea('z',x,-71,17,3.6,s); for(const z of [-78,-64]) pinaculo(x,ALT.transepto,z,8,.8); }
  telhado(-44.5,-9,-78.5,-63.5,ALT.transepto,ALT.empena,true,MK.telha,[]);
  // braço leste: só o madeiramento quebrado ficou; o luar entra pelo buraco
  for(let k=0;k<7;k++){ const x=18+k*3.8, v=new THREE.Mesh(new THREE.BoxGeometry(.4,.4,9+vr()*5),MAT.wood); v.position.set(x,ALT.transepto-1-vr()*6,-71+(vr()-.5)*3); v.rotation.set((vr()-.5)*.9,0,(vr()-.5)*.6); add(v); }
  // ---- torre do cruzeiro (lanterna) e agulha ----
  { B(-9.3,9.3,ALT.clerestorio,ALT.lanterna,-63.7,-62.7,MK.pedra,4); B(-9.3,9.3,ALT.clerestorio,ALT.lanterna,-79.3,-78.3,MK.pedra,4); B(-9.3,-8.3,ALT.clerestorio,ALT.lanterna,-78.3,-63.7,MK.pedra,4); B(8.3,9.3,ALT.clerestorio,ALT.lanterna,-78.3,-63.7,MK.pedra,4);
    B(-9.8,9.8,ALT.lanterna,ALT.lanterna+.8,-79.8,-62.2,MK.pedra);
    for(const [x,z] of [[-9.3,-79.3],[9.3,-79.3],[-9.3,-62.7],[9.3,-62.7]]) pinaculo(x,ALT.lanterna,z,10,1);
    for(const o of [-5.5,-1.8,1.8,5.5]){ lanceta('x',-63.2,o,ALT.lanterna-14,1.8,9,1,false); lanceta('x',-78.8,o,ALT.lanterna-14,1.8,9,-1,false); lanceta('z',8.8,-71+o,ALT.lanterna-14,1.8,9,1,false); lanceta('z',-8.8,-71+o,ALT.lanterna-14,1.8,9,-1,false); }
    agulha(0,-71,ALT.lanterna,ALT.agulhaC,6.4,8); }
  // ---- coro e abside: paredes, janelas altas e telhado em leque ----
  for(const s of [-1,1]){ const x=s*12; B(x-.5,x+.5,0,ALT.abside,-85,-83,M.pedra,4); B(x-.5,x+.5,0,ALT.abside,-80,-78,M.pedra,4); B(x-.5,x+.5,5.8,ALT.abside,-83,-80,M.pedra,4); }
  { const n=15; for(let k=0;k<n;k++){ const a=PI*(1+(k+.5)/n), a0=PI*(1+k/n), a1=PI*(1+(k+1)/n), r=11, x0=Math.cos(a0)*r, z0=-85+Math.sin(a0)*r, x1=Math.cos(a1)*r, z1=-85+Math.sin(a1)*r;
      const L=Math.hypot(x1-x0,z1-z0)+.6, seg=new THREE.Mesh(boxGeo(L,ALT.abside,1.4,4),M.pedra); seg.position.set((x0+x1)/2,ALT.abside/2,(z0+z1)/2); seg.rotation.y=-Math.atan2(z1-z0,x1-x0); add(seg);
      if(k%2===1){ const cx=Math.cos(a)*(r+.75), cz=-85+Math.sin(a)*(r+.75); const g=new THREE.PlaneGeometry(1.7,10); g.translate(0,13,0); g.rotateY(-a-PI/2); g.translate(cx,0,cz); vidrosFora.push(g); const gi=new THREE.PlaneGeometry(1.7,10); gi.translate(0,13,0); gi.rotateY(-a+PI/2); gi.translate(Math.cos(a)*(r-.75),0,-85+Math.sin(a)*(r-.75)); vidros.push(gi); }
      if(k%2===0){ const cx=Math.cos(a)*(r+1.6), cz=-85+Math.sin(a)*(r+1.6); B(cx-.8,cx+.8,0,ALT.abside-3,cz-.8,cz+.8,MK.pedra); pinaculo(cx,ALT.abside-3,cz,6,.6); } }
    const tel=new THREE.Mesh(new THREE.ConeGeometry(11.8,9,16,1,true,PI/2,PI),MK.telha); tel.position.set(0,ALT.abside+4.5,-85); add(tel); }
  telhado(-12.5,12.5,-85,-78,ALT.abside,ALT.abside+8.5,false,MK.telha,[]);
  // ---- Sala do Capítulo, Sacristia: telhados de quatro águas; Claustro: galeria em meia-água ----
  const quatroAguas=(x1,x2,z1,z2,y,h)=>{ const g=new THREE.Mesh(new THREE.ConeGeometry(Math.hypot(x2-x1,z2-z1)/2*1.02,h,4,1,true),MK.telha); g.rotation.y=PI/4; g.scale.set((x2-x1)/Math.hypot(x2-x1,z2-z1)*1.41,1,(z2-z1)/Math.hypot(x2-x1,z2-z1)*1.41); g.position.set((x1+x2)/2,y+h/2,(z1+z2)/2); add(g); };
  quatroAguas(-71,-51,-65,-45,14,6); quatroAguas(-31,-11.5,-91,-77.5,14,5);
  { const CL=Sim.CLAUSTRO, yA=8.4, yB=6;
    quad([-44,yA,-64],[-16,yA,-64],[-16,yB,CL.z1+.5],[-44,yB,CL.z1+.5],M.telha); quad([-44,yA,-44],[-16,yA,-44],[-16,yB,CL.z2-.5],[-44,yB,CL.z2-.5],M.telha);
    quad([-44,yA,-64],[-44,yA,-44],[CL.x1+.5,yB,CL.z2],[CL.x1+.5,yB,CL.z1],M.telha); quad([-16,yA,-64],[-16,yA,-44],[CL.x2-.5,yB,CL.z2],[CL.x2-.5,yB,CL.z1],M.telha);
    // arcos entre as colunas da galeria
    const cols=W.filter(w=>w.kind==='coluna_claustro').map(w=>({x:(w.x1+w.x2)/2,z:(w.z1+w.z2)/2}));
    for(const c of cols){ const cap=new THREE.Mesh(new THREE.BoxGeometry(1.1,.45,1.1),M.pedraEsc); cap.position.set(c.x,5.4,c.z); add(cap,false);
      const col=new THREE.Mesh(new THREE.CylinderGeometry(.3,.36,5.2,10),M.pedraClara); col.position.set(c.x,2.6,c.z); add(col); }
    for(let i=0;i<cols.length;i++) for(let j=i+1;j<cols.length;j++){ const a=cols[i], b=cols[j], d=Math.hypot(a.x-b.x,a.z-b.z); if(d>4.7||d<3) continue;
      const ar=new THREE.Mesh(new THREE.TorusGeometry(d/2,.18,5,14,PI),M.pedraEsc); ar.position.set((a.x+b.x)/2,5.6,(a.z+b.z)/2); ar.rotation.y=-Math.atan2(b.z-a.z,b.x-a.x); add(ar,false); } }
  // vitrais e feixes: uma malha só de cada tipo
  const unir=(lista,mat,ordem)=>{ if(!lista.length) return; const m=new THREE.Mesh(merge(lista),mat); m.userData.dinamico=true; if(ordem) m.renderOrder=ordem; world.add(m); return m; };
  unir(vidros,M.vidro); unir(vidrosFora,M.vidroFora); unir(rosas,M.rosa); if(PERF.nivel>=1) unir(feixes,M.feixe,2);
}
// ================= a catedral por dentro: abóbadas, arcadas, pilares, jubé, colunata da abside =================
function interior(){
  // tetos: a nave sob a abóbada alta, as naves laterais mais baixas, o transepto, o coro; o cruzeiro abre para a lanterna
  const teto=(x1,x2,z1,z2,y)=>{ const m=new THREE.Mesh(new THREE.PlaneGeometry(x2-x1,z2-z1),M.teto); m.rotation.x=PI/2; m.position.set((x1+x2)/2,y,(z1+z2)/2); world.add(m); };
  teto(-8,8,-62.7,-40,ALT.clerestorio-1.2); teto(-16,-8,-64,-40,ALT.lateral-1); teto(8,16,-64,-40,ALT.lateral-1);
  teto(-44,-9.3,-78,-64,ALT.transepto-1.2); teto(-12,12,-85,-79.3,ALT.abside-1.2); teto(-9.3,9.3,-79.3,-62.7,ALT.lanterna-1);
  teto(-30,-12.5,-90,-78.5,12); teto(-70,-52,-64,-46,12);
  // nervuras das abóbadas (cada tramo da nave, das naves laterais e do transepto)
  const nerv=(w,x,y,z,girar,alto)=>{ const m=new THREE.Mesh(nervura(w,alto||4.4),M.pedraEsc); m.position.set(x,y,z); if(girar) m.rotation.y=PI/2; world.add(m); };
  for(const z of [-40,-46,-52,-58,-64]) nerv(16,0,ALT.clerestorio-1.1,z,false,5);
  for(const z of [-43,-49,-55,-61]){ for(const [a,b] of [[-8,8]]){ const mx=new THREE.Mesh(nervura(Math.hypot(16,6),5),M.pedraEsc); mx.position.set(0,ALT.clerestorio-1.1,z); mx.rotation.y=Math.atan2(6,16); world.add(mx); const my=mx.clone(); my.rotation.y=-Math.atan2(6,16); world.add(my); } }
  for(const s of [-1,1]) for(const z of [-40,-46,-52,-58]) nerv(8,s*12,ALT.lateral-1,z,false,3);
  for(let x=-41;x<-10;x+=7) nerv(14,x,ALT.transepto-1.1,-71,true,4.6);
  // arcada da nave: arcos apontados entre os pilares, trifório e o pé do clerestório
  const P=Sim.PILLARS.map(([x,z])=>({x,z})).concat([{x:-8,z:-40},{x:8,z:-40},{x:-8,z:-64},{x:8,z:-64}]);
  for(const s of [-1,1]){ const zs=[-40,-46,-52,-58,-64]; for(let i=0;i<zs.length-1;i++){ const za=zs[i], zb=zs[i+1], c=(za+zb)/2;
      const ar=new THREE.Mesh(nervura(6,3.6,.45),M.pedra); ar.position.set(s*8,ALT.lateral-3.2,c); ar.rotation.y=PI/2; world.add(ar);
      for(const o of [-1.6,0,1.6]){ const t=new THREE.Mesh(nervura(1.4,.9,.12),M.pedraEsc); t.position.set(s*7.4,ALT.lateral+1.4,c+o); t.rotation.y=PI/2; world.add(t); } } }
  // pilares da nave: feixes de colunas até o arranque da abóbada
  const fuste=new THREE.CylinderGeometry(.62,.72,ALT.lateral-3,14), fusteFino=new THREE.CylinderGeometry(.16,.18,ALT.clerestorio-2,6), base=new THREE.CylinderGeometry(1.05,1.15,.7,14), capitel=new THREE.CylinderGeometry(1.05,.72,.8,14);
  for(const {x,z} of Sim.PILLARS.map(([x,z])=>({x,z}))){ const f=new THREE.Mesh(fuste,M.pedra); f.position.set(x,(ALT.lateral-3)/2,z); add(f);
    for(const [ox,oz] of [[.6,0],[-.6,0],[0,.6],[0,-.6]]){ const ff=new THREE.Mesh(fusteFino,M.pedraClara); ff.position.set(x+ox,(ALT.clerestorio-2)/2,z+oz); world.add(ff); }
    const b=new THREE.Mesh(base,MAT.marble); b.position.set(x,.35,z); world.add(b); const c=new THREE.Mesh(capitel,M.pedraEsc); c.position.set(x,ALT.lateral-2.6,z); world.add(c); }
  // pilares do cruzeiro: as peças da simulação ganham colunas em feixe até a lanterna
  for(const w of W.filter(w=>w.kind==='pilar_cruzeiro')){ const x=(w.x1+w.x2)/2, z=(w.z1+w.z2)/2; B(w.x1,w.x2,0,ALT.clerestorio,w.z1,w.z2,M.pedra,4);
    for(const [ox,oz] of [[1.3,0],[-1.3,0],[0,1.3],[0,-1.3],[1,1],[-1,1],[1,-1],[-1,-1]]){ const ff=new THREE.Mesh(new THREE.CylinderGeometry(.22,.24,ALT.clerestorio,8),M.pedraClara); ff.position.set(x+ox,ALT.clerestorio/2,z+oz); world.add(ff); } }
  for(const [x1,z1,x2,z2] of [[-8,-64,8,-64],[-8,-78,8,-78],[-8,-64,-8,-78],[8,-64,8,-78]]){ const L=Math.hypot(x2-x1,z2-z1), a=new THREE.Mesh(nervura(L,6,.55),M.pedra); a.position.set((x1+x2)/2,ALT.clerestorio-.5,(z1+z2)/2); a.rotation.y=-Math.atan2(z2-z1,x2-x1); world.add(a); }
  // jubé: anteparo de pedra com arcos cegos, passagem dos lados e o grande crucifixo no alto
  for(const w of W.filter(w=>w.kind==='jube')){ B(w.x1,w.x2,0,w.h,w.z1,w.z2,M.pedraEsc,3); B(w.x1-.2,w.x2+.2,w.h,w.h+.6,w.z1-.3,w.z2+.3,M.pedra);
    for(let x=w.x1+.6;x<w.x2-.4;x+=1.2){ const a=new THREE.Mesh(nervura(1,.7,.08),M.pedra); a.position.set(x+.5,w.h-1.4,w.z2+.06); world.add(a); } }
  { const cruz=new THREE.Group(); cruz.position.set(0,9.6,-78); world.add(cruz); const madeira=MAT.wood;
    const h=new THREE.Mesh(new THREE.BoxGeometry(.5,7,.5),madeira), v=new THREE.Mesh(new THREE.BoxGeometry(4.2,.45,.45),madeira); h.position.y=3.5; v.position.y=5.2; cruz.add(h,v);
    const corpo=new THREE.Mesh(new THREE.CylinderGeometry(.22,.16,2.2,8),MAT.estatua); corpo.position.set(0,4.1,.32); cruz.add(corpo);
    for(const s of [-1,1]){ const b=new THREE.Mesh(new THREE.CylinderGeometry(.07,.07,1.8,6),MAT.estatua); b.position.set(s*.95,5.05,.32); b.rotation.z=s*1.35; cruz.add(b); }
    const cab=new THREE.Mesh(new THREE.SphereGeometry(.2,8,6),MAT.estatua); cab.position.set(0,5.35,.36); cruz.add(cab);
    for(const s of [-1,1]){ const c=new THREE.Mesh(new THREE.CylinderGeometry(.02,.02,9,4),MAT.metal); c.position.set(s*1.8,10.5,0); cruz.add(c); } }
  // colunata em meia-lua atrás do altar-mor
  for(const w of W.filter(w=>w.kind==='coluna_abside')){ const x=(w.x1+w.x2)/2, z=(w.z1+w.z2)/2, c=new THREE.Mesh(new THREE.CylinderGeometry(.5,.58,w.h,14),M.pedraClara); c.position.set(x,w.h/2,z); add(c);
    const k=new THREE.Mesh(new THREE.CylinderGeometry(.85,.55,.7,14),M.pedraEsc); k.position.set(x,w.h+.35,z); world.add(k); const b=new THREE.Mesh(new THREE.CylinderGeometry(.8,.9,.5,14),MAT.marble); b.position.set(x,.25,z); world.add(b); }
  // ombreiras e lintéis das portas (as brechas ficam com a borda quebrada)
  for(const P2 of Sim.PORTAIS){ if(P2.nome==='Grande Porta'||P2.nome.startsWith('Arco')||P2.nome.startsWith('Jubé')) continue; const ax=P2.eixo==='x', f=P2.fixo, box=(a,b,y1,y2,e1,e2,m)=>ax?B(a,b,y1,y2,f+e1,f+e2,m,2):B(f+e1,f+e2,y1,y2,a,b,m,2);
    if(P2.brecha){ for(let k=0;k<4;k++){ const a=P2.a+(P2.b-P2.a)*k/4, b=a+(P2.b-P2.a)/4; box(a,b,4.5+vr()*3,6.5+vr()*2,-.5,.5,M.pedra); } continue; }
    box(P2.a-.5,P2.b+.5,5.3,5.75,-.8,.8,M.pedraEsc); for(const e of [P2.a-.45,P2.b-.05]) box(e,e+.5,0,5.6,-.75,.75,M.pedraEsc);
    const arco=new THREE.Mesh(new THREE.TorusGeometry((P2.b-P2.a)/2,.26,6,16,PI),M.pedraEsc); const c=(P2.a+P2.b)/2; if(ax) arco.position.set(c,5.6,f); else { arco.position.set(f,5.6,c); arco.rotation.y=PI/2; } world.add(arco); }
}
// ================= promontório: adro, mosteiro, cemitério =================
function promontorio(){
  // beira do penhasco ao norte (atrás da abside): rochas soltas sobre o limite
  for(let x=-80;x<=80;x+=6){ if(Math.abs(x)<10) continue; const r=new THREE.Mesh(new THREE.DodecahedronGeometry(1.4+vr()*1.2,0),M.rocha); r.position.set(x+vr()*3,.3,-100.2+vr()*.6); r.scale.y=.6; add(r); }
  // tanques secos do adro e os fiéis de pedra que vigiam a ponte
  for(const w of W.filter(w=>w.kind==='tanque')){ B(w.x1,w.x2,0,w.h,w.z1,w.z2,M.pedraClara,2); const agua=new THREE.Mesh(new THREE.PlaneGeometry(w.x2-w.x1-.6,w.z2-w.z1-.6),new THREE.MeshStandardMaterial({color:0x080a0e,roughness:.15,metalness:.4})); agua.rotation.x=-PI/2; agua.position.set((w.x1+w.x2)/2,w.h-.2,(w.z1+w.z2)/2); world.add(agua); }
}

// ================= peças novas (o jogo chama PECAS[kind](w) para cada caixa da simulação) =================
const nada=()=>{};
const PECAS={
  catedral:nada, fachada:nada, torre_oeste:nada, torre_leste:nada, abside:nada, jube:nada, pilar_cruzeiro:nada, coluna_claustro:nada, coluna_abside:nada, abismo:nada, tanque:nada,
  claustro(w){ B(w.x1,w.x2,0,9.5,w.z1,w.z2,M.pedra,4); B(w.x1-.15,w.x2+.15,9.5,10,w.z1-.15,w.z2+.15,M.pedraEsc,2); },
  limite(w){ const norte=w.z2<=-Sim.HALF.z+.01, D0=Sim.DESFILADEIRO; const muro=(x1,x2)=>{ B(x1,x2,0,w.h,w.z1,w.z2,MAT.wall); B(x1-.15,x2+.15,w.h,w.h+.3,w.z1-.15,w.z2+.15,MAT.wallDark); };
    if(norte){ muro(w.x1,D0.x1); muro(D0.x2,w.x2); } else muro(w.x1,w.x2); },
  pedra(w){ B(w.x1,w.x2,0,w.h,w.z1,w.z2,M.pedra,4); if(w.h>2.2) B(w.x1-.1,w.x2+.1,w.h,w.h+.35,w.z1-.1,w.z2+.1,M.pedraEsc,2); },
  parapeito(w){ const aoX=(w.x2-w.x1)>=(w.z2-w.z1), L=Math.max(w.x2-w.x1,w.z2-w.z1); B(w.x1,w.x2,0,.25,w.z1,w.z2,M.pedraClara,2); B(w.x1-.08,w.x2+.08,w.h-.18,w.h,w.z1-.08,w.z2+.08,M.pedraClara,2);
    const bal=new THREE.CylinderGeometry(.11,.16,w.h-.43,8); for(let t=.35;t<L-.2;t+=.55){ const b=new THREE.Mesh(bal,M.pedraClara); b.position.set(aoX?w.x1+t:(w.x1+w.x2)/2,.25+(w.h-.43)/2,aoX?(w.z1+w.z2)/2:w.z1+t); world.add(b); } },
  mureta(w){ B(w.x1,w.x2,0,w.h,w.z1,w.z2,MAT.pedraVelha,2); B(w.x1-.06,w.x2+.06,w.h,w.h+.12,w.z1-.06,w.z2+.06,M.pedraEsc,2); },
  estatua_ponte(w){ const x=(w.x1+w.x2)/2, z=(w.z1+w.z2)/2; B(w.x1,w.x2,0,1.35,w.z1,w.z2,M.pedraClara,2); B(w.x1-.1,w.x2+.1,1.3,1.45,w.z1-.1,w.z2+.1,M.pedraEsc,2);
    PECAS.encapuzado(x,1.45,z,w.rot||0,1.05); },
  // fiel encapuzado: manto até o chão, capuz fundo sem rosto, mãos juntas
  encapuzado(x,y,z,rot,esc){ const g=new THREE.Group(); g.position.set(x,y,z); g.rotation.y=rot; g.scale.setScalar(esc||1); world.add(g); const MM=MAT.estatua;
    const manto=new THREE.Mesh(new THREE.LatheGeometry([[.5,0],[.46,.25],[.38,.8],[.3,1.25],[.26,1.48],[.2,1.62],[0,1.66]].map(([a,b])=>new THREE.Vector2(a,b)),9),MM); g.add(manto); manto.castShadow=true;
    const capuz=new THREE.Mesh(new THREE.SphereGeometry(.26,10,8,0,PI*2,0,PI*.62),MM); capuz.position.set(0,1.72,0); capuz.rotation.x=-.35; g.add(capuz);
    const vazio=new THREE.Mesh(new THREE.CircleGeometry(.15,10),M.fundo); vazio.position.set(0,1.66,-.2); vazio.rotation.y=PI; g.add(vazio);
    const maos=new THREE.Mesh(new THREE.SphereGeometry(.12,8,6),MM); maos.position.set(0,1.05,-.33); g.add(maos);
    for(const s of [-1,1]){ const b=new THREE.Mesh(new THREE.CylinderGeometry(.08,.11,.6,6),MM); b.position.set(s*.2,1.2,-.18); b.rotation.set(-.9,0,s*-.5); g.add(b); } return g; },
  penhasco(w){ const aoX=(w.x2-w.x1)>=(w.z2-w.z1), L=Math.max(w.x2-w.x1,w.z2-w.z1), n=Math.max(1,Math.round(L/2.2));
    for(let k=0;k<n;k++){ const t=(k+.5)/n*L, h=w.h*(.8+vr()*.45), r=new THREE.Mesh(new THREE.DodecahedronGeometry(1,1),M.rocha); const p=r.geometry.attributes.position;
      r.scale.set(aoX?1.6:1.3,h/1.6,aoX?1.3:1.6); r.position.set(aoX?w.x1+t:(w.x1+w.x2)/2+(vr()-.5)*.4,h*.45,aoX?(w.z1+w.z2)/2+(vr()-.5)*.4:w.z1+t); r.rotation.set(vr()*.4,vr()*3,vr()*.4); add(r); } },
  bloco(w){ B(w.x1,w.x2,0,w.h,w.z1,w.z2,M.pedraClara,1.5); },
  guindaste(w){ const x=(w.x1+w.x2)/2, z=(w.z1+w.z2)/2, mad=MAT.wood; const m=new THREE.Mesh(new THREE.BoxGeometry(.4,w.h,.4),mad); m.position.set(x,w.h/2,z); add(m);
    const lanca=new THREE.Mesh(new THREE.BoxGeometry(.3,.3,7.5),mad); lanca.position.set(x,w.h-1.4,z+2.6); lanca.rotation.x=-.35; add(lanca);
    for(const s of [-1,1]){ const p=new THREE.Mesh(new THREE.BoxGeometry(.25,w.h*.8,.25),mad); p.position.set(x+s*1.3,w.h*.38,z-.8); p.rotation.z=s*.25; add(p); }
    const corda=new THREE.Mesh(new THREE.CylinderGeometry(.03,.03,5,4),new THREE.MeshStandardMaterial({color:0x5a4a32})); corda.position.set(x,w.h-3.5,z+5.7); world.add(corda);
    const pedra=new THREE.Mesh(new THREE.BoxGeometry(1.1,.8,1.1),M.pedraClara); pedra.position.set(x,w.h-6.2,z+5.7); add(pedra); },
  passarela(w){ const aoX=(w.x2-w.x1)>=(w.z2-w.z1), L=Math.max(w.x2-w.x1,w.z2-w.z1); for(let t=.2;t<L;t+=.45){ const tb=new THREE.Mesh(new THREE.BoxGeometry(aoX?.34:1.6,.08,aoX?1.6:.34),MAT.tabuas); tb.position.set(aoX?w.x1+t:(w.x1+w.x2)/2,.3+(vr()-.5)*.06,aoX?(w.z1+w.z2)/2:w.z1+t); tb.rotation.y=(vr()-.5)*.08; world.add(tb); }
    for(let t=1;t<L;t+=2.5) for(const s of [-1,1]){ const p=new THREE.Mesh(new THREE.CylinderGeometry(.08,.08,1,5),MAT.wood); p.position.set(aoX?w.x1+t:(w.x1+w.x2)/2+s*.8,.2,aoX?(w.z1+w.z2)/2+s*.8:w.z1+t); world.add(p); } },
  forno(w){ const x=(w.x1+w.x2)/2, z=(w.z1+w.z2)/2, r=(w.x2-w.x1)/2; const d=new THREE.Mesh(new THREE.SphereGeometry(r*1.05,14,8,0,PI*2,0,PI/2),new THREE.MeshStandardMaterial({color:0x2a241e,roughness:1,map:TEX.wall.map})); d.scale.y=w.h/r; d.position.set(x,0,z); add(d);
    const boca=new THREE.Mesh(new THREE.CircleGeometry(.45,10),new THREE.MeshBasicMaterial({color:0xff6a2a})); boca.position.set(x,.4,z+r*1.02); world.add(boca); FONTES.push({x,z:z+r+.5,y:.6,cor:0xff5a20,int:1.1,alc:7}); },
  cruz(w){ const x=(w.x1+w.x2)/2, z=(w.z1+w.z2)/2, madeira=w.h>5.5||Math.abs(z+27)>1; const mat=madeira?MAT.wood:M.pedraClara;
    const h=new THREE.Mesh(new THREE.BoxGeometry(.34,w.h,.34),mat); h.position.set(x,w.h/2,z); add(h); const v=new THREE.Mesh(new THREE.BoxGeometry(w.h*.42,.3,.3),mat); v.position.set(x,w.h*.74,z); add(v);
    const pe=new THREE.Mesh(new THREE.DodecahedronGeometry(.9,0),M.rocha); pe.scale.y=.45; pe.position.set(x,.15,z); add(pe); },
  banco(w){ B(w.x1,w.x2,0,w.h,w.z1,w.z2,M.pedraClara,2); },
  arca(w){ B(w.x1,w.x2,0,w.h,w.z1,w.z2,MAT.wood,1); B(w.x1-.05,w.x2+.05,w.h-.12,w.h,w.z1-.05,w.z2+.05,MAT.metal,1); },
  torre_portaria(w){ const x=(w.x1+w.x2)/2, z=(w.z1+w.z2)/2; B(w.x1,w.x2,0,w.h,w.z1,w.z2,MAT.pedraVelha,3);
    for(let k=0;k<4;k++){ const a=w.x1+.4+k*(w.x2-w.x1-.8)/3; B(a-.35,a+.35,w.h,w.h+.9,w.z1,w.z1+.7,MAT.pedraVelha,2); B(a-.35,a+.35,w.h,w.h+.9,w.z2-.7,w.z2,MAT.pedraVelha,2); }
    for(const y of [4,8]){ const s=new THREE.Mesh(new THREE.PlaneGeometry(.25,1.3),M.fundo); s.position.set(x,y,w.z2+.02); world.add(s); } },
};

// ================= montagem =================
let NEVOAS=[];
function construir(){ pisos(); NEVOAS=desfiladeiro(); pontes(); catedral(); interior(); promontorio(); }
// névoa do desfiladeiro andando devagar (o vento é o mesmo dos estandartes)
function quadro(dt){ for(let i=0;i<NEVOAS.length;i++){ const t=NEVOAS[i].material.map; t.offset.x+=dt*(.004+i*.002); t.offset.y+=dt*.0015*(i%2?1:-1); } }
return {PECAS,construir,quadro,ALT,encapuzado:PECAS.encapuzado};
};
