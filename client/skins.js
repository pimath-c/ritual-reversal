/* Ritual Reversal — skins das armas (v6): a coleção "Relíquias da Vigília".
   Só aparência. Nenhuma skin muda dano, cadência, alcance, som de posição ou a silhueta da arma (as formas são as mesmas
   em todas; muda o acabamento). A brasa do cetro vista pelos outros continua com a cor do culto: leitura de equipe vem
   antes de enfeite.
   Raridade aqui é ofício, não sorteio: Ofício (a arma de serviço), Mestre (trabalho de um armeiro), Relíquia (peça única,
   com um acabamento que reage na inspeção). Todas livres para escolher no Arsenal; nada é cobrado, sorteado ou bloqueado.
   Como funciona:
   • Cada material das armas tem um papel (caixa, aco, latao, madeira, prata, escuro, cruz, osso, ferro, brasa). A skin
     diz, por papel, cor, aspereza, metal e um "detalhe": uma textura feita aqui (gravura, têmpera, quadriculado,
     rachaduras, crosta) projetada em três planos no espaço da peça, sem depender das UVs. O canal R escava sulcos (relevo
     pela derivada da altura, fundo mais escuro e áspero), o G incrusta outro material (prata batida, osso, verdete) e o B
     mancha a cor (têmpera, manchas de uso).
   • Os materiais mudam no lugar: os objetos que a animação usa continuam os mesmos, e trocar de skin não recompila nada.
   • Acessórios (selo de cera, arame de cobre, cracas) são malhas pequenas presas às peças; os pendentes balançam com a
     arma (mola em direção à gravidade).
   • Ganchos de inspeção: o clipe marca eventos ['skin','revela'] etc. e a skin decide o que acontece (o brilho que corre
     pela gravura, a cruz que acende, a água que pinga do cetro). */
window.RR_SKINS=function(D){
'use strict';
const {THREE,RT,efeito,tocar}=D;
const PI=Math.PI, TAU=PI*2, V3=(x,y,z)=>new THREE.Vector3(x,y,z);

// ---------------------------------------------------------------------------------------------------------------------
// Catálogo
const COLECAO={nome:'Relíquias da Vigília',texto:'Armas da Ordem e do culto, cada uma com o trabalho de quem a fez. Três ofícios por arma: o de serviço, o de um mestre e a relíquia.'};
const OFICIOS={oficio:{nome:'Ofício',cor:'#b8ab96'},mestre:{nome:'Mestre',cor:'#d9b26a'},reliquia:{nome:'Relíquia',cor:'#e66a5a'}};
const SKINS={
  carabina:[
    {id:'servico',nome:'Serviço',oficio:'oficio',texto:'Aço oxidado a quente e coronha de nogueira com óleo de linhaça. A carabina como sai da oficina da Ordem, com as marcas de quem a usou antes.',papeis:{}},
    {id:'tempera',nome:'Têmpera de Forja',oficio:'mestre',
      texto:'A caixa foi aquecida em carvão de osso e couro e mergulhada em água: as manchas azuis, roxas e cor de palha são do fogo, não de tinta. Quadriculado cortado à mão no pulso e no fuste; latão polido.',
      papeis:{caixa:{cor:0x6a6e78,rug:.24,met:1,det:'tempera',esc:9,mancha:1,manchaCor:0x2a3a78,inc:.85,incCor:0xc89a52,incMet:1,incRug:.28},
        aco:{cor:0x22252c,rug:.26,met:1},latao:{cor:0xe6b866,rug:.14},madeira:{cor:0x7a4020,rug:.42,det:'xadrez',esc:11,sulco:.55,bump:.35}}},
    {id:'relicario',nome:'Relicário do Mosteiro',oficio:'reliquia',
      texto:'Folhagem gravada a buril em toda a caixa, com fio de prata batido nos sulcos; coronha de carvalho do pântano, cruzes de osso embutidas e o selo de cera do mosteiro amarrado no cano com a fita da bênção. Na inspeção, a luz corre pela prata.',
      papeis:{caixa:{cor:0x2c2e34,rug:.3,met:1,det:'gravura',esc:7,sulco:.9,bump:.8,inc:.85,incCor:0xb4b8c0,incMet:1,incRug:.2,mancha:.5,manchaCor:0x18191d},
        aco:{cor:0x1e2025,rug:.32,met:1},latao:{cor:0xd4d8de,rug:.16,met:1},madeira:{cor:0x3a2416,rug:.5,det:'cruzes',esc:4,sulco:.6,bump:.5,inc:1,incCor:0xe6dcc2,incMet:0,incRug:.38}},
      acessorios:['selo'],insp:{revela:{cor:0xfff0c8,forca:1.5},som:'bencao'}}],
  revolver:[
    {id:'prata',nome:'Prata Benta',oficio:'oficio',texto:'Prata de lei sobre aço e cabo de nogueira escura com a cruz em prata: o revólver que todo Exorcista recebe no voto.',papeis:{}},
    {id:'lavrada',nome:'Prata Lavrada',oficio:'mestre',
      texto:'Ramos e espinhos gravados na armação, no tambor e no cano, com o fundo pontilhado e escurecido a enxofre para a figura saltar. Cabo de marfim amarelado pelo uso; cruz banhada a ouro.',
      papeis:{prata:{cor:0xdde1e6,rug:.16,met:1,det:'gravura',esc:10,sulco:.9,bump:.8,mancha:.7,manchaCor:0x46464c},madeira:{cor:0xd6c9a8,rug:.3,met:0,det:'veio',esc:4,mancha:.12,manchaCor:0xb8a47c},
        cruz:{cor:0xe0b04a,rug:.18,met:1},escuro:{cor:0x202126}},
      insp:{revela:{cor:0xffffff,forca:1.1},cruz:[1,.78,.3]}},
    {id:'martir',nome:'Mártir',oficio:'reliquia',
      texto:'Prata oxidada até o negro, só os relevos da gravura polidos à mão; esmalte cor de sangue velho na cruz e uma granada no pé do cabo. Quando o polegar traça a cruz, o esmalte acende.',
      papeis:{prata:{cor:0x2e2f35,rug:.34,met:1,det:'gravura',esc:10,sulco:.4,bump:.7,inc:.9,incCor:0xd6d9de,incMet:1,incRug:.12},madeira:{cor:0x170c0a,rug:.38},
        cruz:{cor:0x5a0a10,rug:.22,met:.15},escuro:{cor:0x111114}},
      acessorios:['granada'],insp:{revela:{cor:0xff6070,forca:1.2},cruz:[1.3,.12,.1],som:'bencao'}}],
  cetro:[
    {id:'vertebras',nome:'Vértebras',oficio:'oficio',texto:'A espinha de um penitente sem nome, presa em ferro forjado. A brasa é a dele.',papeis:{}},
    {id:'ossario',nome:'Ossário',oficio:'mestre',
      texto:'Ossos amarelados do ossuário da Capela Desabada, rachados de séculos e costurados com arame de cobre; amuletos de falange pendurados por correntinhas. A brasa queima cor de âmbar.',
      papeis:{osso:{cor:0xc9b07a,rug:.72,det:'rachado',esc:8,sulco:1,bump:1.1,mancha:.75,manchaCor:0x6e4c22},ferro:{cor:0xa8643a,rug:.34,met:1},brasa:{cor:0xff9a2a}},
      acessorios:['arame','amuletos'],insp:{revela:{cor:0xffb050,forca:.7}}},
    {id:'afogado',nome:'Coro Afogado',oficio:'reliquia',
      texto:'Osso negro tirado do fundo do Charco, ainda molhado, com cracas e verdete crescendo nas garras; a brasa é verde como água parada. Na inspeção, a cabeça respira e pinga.',
      papeis:{osso:{cor:0x23282a,rug:.16,met:.05,det:'crosta',esc:9,sulco:.8,bump:1,inc:1,incCor:0x4c7c62,incMet:0,incRug:.92},ferro:{cor:0x3c6a58,rug:.62,met:.55},brasa:{cor:0x3dffb4}},
      acessorios:['cracas'],insp:{revela:{cor:0x5dffc8,forca:.9},gotas:true,som:'runa_volta'}}]};
const porId=(arma,id)=>SKINS[arma].find(s=>s.id===id)||SKINS[arma][0];

// escolha do jogador (no navegador; vai para a sala online no anúncio do jogador)
const ESC={carabina:'servico',revolver:'prata',cetro:'vertebras'};
try{ const s=JSON.parse(localStorage.getItem('rr-skins')||'{}'); for(const k in ESC) if(s[k]&&SKINS[k].some(x=>x.id===s[k])) ESC[k]=s[k]; }catch(e){}
function salvar(){ try{ localStorage.setItem('rr-skins',JSON.stringify(ESC)); }catch(e){} }
// skins que vêm da rede: só ids do catálogo passam (o resto vira a de serviço)
function limpar(v){ const o={}; for(const k in ESC){ const id=v&&typeof v[k]==='string'?v[k]:''; o[k]=SKINS[k].some(x=>x.id===id)?id:SKINS[k][0].id; } return o; }

// ---------------------------------------------------------------------------------------------------------------------
// Texturas de detalhe (512 px, repetem sem emenda). R: sulco, G: incrustação, B: mancha.
const TEXS={}, N=512;
function ruido(seed,P){ let s=seed; const r=()=>(s=(s*16807)%2147483647)/2147483647; const g=new Float32Array(P*P); for(let i=0;i<P*P;i++) g[i]=r();
  const h=(a,b)=>g[(((a%P)+P)%P)*P+(((b%P)+P)%P)];
  return (x,y)=>{ const xi=Math.floor(x),yi=Math.floor(y),xf=x-xi,yf=y-yi,u=xf*xf*(3-2*xf),w=yf*yf*(3-2*yf);
    return (h(xi,yi)*(1-u)+h(xi+1,yi)*u)*(1-w)+(h(xi,yi+1)*(1-u)+h(xi+1,yi+1)*u)*w; }; }
// campo por pixel (função de u,v em 0..1 que devolve [r,g,b] de 0 a 1)
function campo(fn){ const c=document.createElement('canvas'); c.width=c.height=N; const x=c.getContext('2d'), im=x.createImageData(N,N), d=im.data;
  for(let j=0;j<N;j++) for(let i=0;i<N;i++){ const v=fn(i/N,j/N), k=(j*N+i)*4; d[k]=v[0]*255; d[k+1]=v[1]*255; d[k+2]=v[2]*255; d[k+3]=255; }
  x.putImageData(im,0,0); return c; }
// traços que dão a volta na borda (a textura repete)
function tela(desenho){ const c=document.createElement('canvas'); c.width=c.height=N; const x=c.getContext('2d'); x.fillStyle='#000'; x.fillRect(0,0,N,N); x.globalCompositeOperation='lighter'; x.lineCap=x.lineJoin='round';
  const embrulho=f=>{ for(const dx of [-N,0,N]) for(const dy of [-N,0,N]){ x.save(); x.translate(dx,dy); f(x); x.restore(); } }; desenho(x,embrulho); return c; }
function textura(nome){ if(TEXS[nome]) return TEXS[nome]; const c=GERA[nome](); const t=new THREE.CanvasTexture(c); t.wrapS=t.wrapT=THREE.RepeatWrapping; t.anisotropy=4; return TEXS[nome]=t; }
const GERA={
  // gravura de folhagem: espirais com folhas (acanto), fundo pontilhado. R=G=linhas, B=pontilhado do fundo
  gravura:()=>tela((x,emb)=>{ let s=11; const r=()=>(s=(s*16807)%2147483647)/2147483647;
    const espiral=(cx,cy,R0,sent,cor,larg)=>{ x.strokeStyle=cor; x.lineWidth=larg; x.beginPath(); let px=0,py=0; for(let k=0;k<=60;k++){ const t=k/60, a=sent*t*PI*2.6, rr=R0*(1-t*.82); px=cx+Math.cos(a)*rr; py=cy+Math.sin(a)*rr; k?x.lineTo(px,py):x.moveTo(px,py); } x.stroke();
      // folhas ao longo da espiral: gotas que saem para fora
      for(let k=6;k<54;k+=7){ const t=k/60, a=sent*t*PI*2.6, rr=R0*(1-t*.82), qx=cx+Math.cos(a)*rr, qy=cy+Math.sin(a)*rr, n=a+sent*PI/2, L=R0*.32*(1-t*.6);
        x.beginPath(); x.moveTo(qx,qy); x.quadraticCurveTo(qx+Math.cos(n-.5)*L,qy+Math.sin(n-.5)*L,qx+Math.cos(n)*L*1.25,qy+Math.sin(n)*L*1.25); x.quadraticCurveTo(qx+Math.cos(n+.5)*L,qy+Math.sin(n+.5)*L,qx,qy); x.stroke();
        x.beginPath(); x.moveTo(qx,qy); x.lineTo(qx+Math.cos(n)*L*.9,qy+Math.sin(n)*L*.9); x.stroke(); } };
    // fundo pontilhado (mate de buril)
    x.fillStyle='rgb(0,0,255)'; for(let k=0;k<2600;k++){ const px=r()*N, py=r()*N; x.fillRect(px,py,2.4,2.4); }
    const cs=[]; for(let k=0;k<5;k++) cs.push([r()*N,r()*N,70+r()*50,r()<.5?1:-1]);
    emb(()=>{ for(const [cx,cy,R0,sn] of cs) espiral(cx,cy,R0,sn,'rgb(255,255,0)',5.5);
      // ramos que ligam as espirais
      x.strokeStyle='rgb(255,255,0)'; x.lineWidth=4.5; for(let k=0;k<cs.length;k++){ const a=cs[k], b=cs[(k+1)%cs.length]; x.beginPath(); x.moveTo(a[0],a[1]); x.bezierCurveTo(a[0]+60,a[1]-50,b[0]-60,b[1]+50,b[0],b[1]); x.stroke(); } });
    }),
  // têmpera de cor: duas manchas de fogo (B azul/roxo, G palha) que se misturam como água
  tempera:()=>{ const a=ruido(5,32), b=ruido(9,32); return campo((u,v)=>{ const f=(n,x,y)=>(n(x,y)*.55+n(x*2,y*2)*.28+n(x*4,y*4)*.17);
    const w1=f(a,u*8+f(b,u*8,v*8)*3,v*8), w2=f(b,u*8+7,v*8+f(a,u*8+3,v*8)*3); return [0,Math.max(0,Math.min(1,(w2-.48)*3.2)),Math.max(0,Math.min(1,(w1-.42)*2.4))]; }); },
  // quadriculado de coronha: losangos finos cortados a 30°
  xadrez:()=>tela((x,emb)=>{ x.strokeStyle='rgb(255,0,0)'; x.lineWidth=3.2; const p=N/16; emb(()=>{ for(let k=-48;k<=48;k++){ x.beginPath(); x.moveTo(k*p,0); x.lineTo(k*p+N*.58,N); x.stroke(); x.beginPath(); x.moveTo(k*p,N); x.lineTo(k*p+N*.58,0); x.stroke(); } }); }),
  // cruzes de osso embutidas na madeira, espaçadas, com o contorno do encaixe
  cruzes:()=>tela((x,emb)=>{ const cruz=(cx,cy,s)=>{ x.fillRect(cx-s*.12,cy-s*.5,s*.24,s); x.fillRect(cx-s*.36,cy-s*.22,s*.72,s*.22); };
    emb(()=>{ for(const [cx,cy] of [[128,128],[384,384]]){ x.fillStyle='rgb(0,255,0)'; cruz(cx,cy,120); x.strokeStyle='rgb(255,0,0)'; x.lineWidth=3; x.strokeRect(cx-17,cy-62,34,124); } }); }),
  // marfim: linhas de Schreger, cruzadas e suaves (só mancha)
  veio:()=>campo((u,v)=>{ const a=Math.sin((u*6+v*3)*TAU), b=Math.sin((u*6-v*3)*TAU); return [0,0,Math.max(0,(a*b)*.5+.25)]; }),
  // osso rachado: rachaduras que andam e se ramificam (R) e manchas de séculos (B)
  rachado:()=>{ const m=ruido(13,32); const c=tela((x,emb)=>{ let s=21; const r=()=>(s=(s*16807)%2147483647)/2147483647; const fissuras=[];
      for(let k=0;k<16;k++){ let px=r()*N, py=r()*N, a=r()*TAU; const pts=[[px,py]]; for(let i=0;i<30;i++){ a+=(r()-.5)*.9; px+=Math.cos(a)*7; py+=Math.sin(a)*7; pts.push([px,py]); if(r()<.06){ fissuras.push([px,py,a+(r()<.5?1:-1)]); } } fissuras.push(pts); }
      emb(()=>{ x.strokeStyle='rgb(255,0,0)'; for(const f of fissuras){ if(f.length===3){ let [px,py,a]=f; x.lineWidth=1.4; x.beginPath(); x.moveTo(px,py); for(let i=0;i<8;i++){ a+=(Math.sin(i*7.1)*.4); px+=Math.cos(a)*6; py+=Math.sin(a)*6; x.lineTo(px,py); } x.stroke(); continue; }
        x.lineWidth=2.4; x.beginPath(); f.forEach(([px,py],i)=>i?x.lineTo(px,py):x.moveTo(px,py)); x.stroke(); } }); });
    const x=c.getContext('2d'), im=x.getImageData(0,0,N,N), d=im.data; for(let j=0;j<N;j++) for(let i=0;i<N;i++){ const k=(j*N+i)*4, u=i/N*8, v=j/N*8, f=m(u,v)*.6+m(u*2,v*2)*.4; d[k+2]=Math.max(0,Math.min(255,(f-.45)*2.6*255)); }
    x.putImageData(im,0,0); return c; },
  // crosta do fundo do charco: placas de verdete (G) e furos de craca (R)
  crosta:()=>{ const a=ruido(31,32), b=ruido(37,32); return campo((u,v)=>{ const f=a(u*8,v*8)*.6+a(u*16,v*16)*.4, g=b(u*24,v*24);
    const placa=Math.max(0,Math.min(1,(f-.5)*4)); const furo=Math.max(0,Math.min(1,(g-.72)*6)); return [furo*.9,placa,0]; }); }
};
const VAZIA=new THREE.DataTexture(new Uint8Array([0,0,0,255]),1,1); VAZIA.needsUpdate=true;

// ---------------------------------------------------------------------------------------------------------------------
// Materiais: o detalhe entra no shader do material (encadeado com o que já houver, como a luz de leitura da 1ª pessoa)
function preparar(raiz){ raiz.traverse(o=>{ if(!o.isMesh) return; for(const m of (Array.isArray(o.material)?o.material:[o.material])) if(m&&m.isMeshStandardMaterial&&m.userData.papel) prepararMat(m); }); }
function prepararMat(m){ if(m.userData.skinU) return;
  const U={uDet:{value:VAZIA},uEsc:{value:10},uSulco:{value:0},uInc:{value:0},uMancha:{value:0},uBump:{value:0},uManchaCor:{value:new THREE.Color(0)},
    uIncCor:{value:new THREE.Color(0)},uIncMet:{value:1},uIncRug:{value:.2},uVarre:{value:new THREE.Vector3(0,.06,0)},uVarreCor:{value:new THREE.Color(1,1,1)}};
  m.userData.skinU=U; const ant=m.onBeforeCompile, antK=m.customProgramCacheKey;
  m.onBeforeCompile=(sh,r)=>{ if(ant) ant.call(m,sh,r); Object.assign(sh.uniforms,U);
    sh.vertexShader=sh.vertexShader.replace('void main() {','varying vec3 vPosO; varying vec3 vNorO;\nvoid main() {').replace('#include <begin_vertex>','#include <begin_vertex>\n vPosO=position; vNorO=objectNormal;');
    sh.fragmentShader=sh.fragmentShader.replace('void main() {',`varying vec3 vPosO; varying vec3 vNorO; uniform sampler2D uDet; uniform float uEsc,uSulco,uInc,uMancha,uBump,uIncMet,uIncRug; uniform vec3 uManchaCor,uIncCor,uVarre,uVarreCor;
void main() {`)
      .replace('#include <metalnessmap_fragment>',`#include <metalnessmap_fragment>
  vec3 rrB=pow(abs(normalize(vNorO)),vec3(4.)); rrB/=(rrB.x+rrB.y+rrB.z+1e-5);
  vec4 rrD=texture2D(uDet,vPosO.zy*uEsc)*rrB.x+texture2D(uDet,vPosO.xz*uEsc)*rrB.y+texture2D(uDet,vPosO.xy*uEsc)*rrB.z;
  float rrS=rrD.r*uSulco, rrI=rrD.g*uInc, rrM=rrD.b*uMancha*(1.-rrD.g*uInc);
  diffuseColor.rgb=mix(diffuseColor.rgb,uManchaCor,rrM); diffuseColor.rgb*=1.-rrS*.62; roughnessFactor=min(1.,roughnessFactor+rrS*.38);
  diffuseColor.rgb=mix(diffuseColor.rgb,uIncCor,rrI); roughnessFactor=mix(roughnessFactor,uIncRug,rrI); metalnessFactor=mix(metalnessFactor,uIncMet,rrI);`)
      .replace('#include <normal_fragment_maps>',`#include <normal_fragment_maps>
  if(uBump>0.){ float rrH=-rrS+rrI*.25; vec2 dH=vec2(dFdx(rrH),dFdy(rrH))*uBump*.004; vec3 sX=dFdx(-vViewPosition), sY=dFdy(-vViewPosition);
    vec3 R1=cross(sY,normal), R2=cross(normal,sX); float fD=dot(sX,R1); normal=normalize(abs(fD)*normal-sign(fD)*(dH.x*R1+dH.y*R2)); }`)
      .replace('#include <emissivemap_fragment>',`#include <emissivemap_fragment>
  if(uVarre.z>0.){ vec2 rrP=-vViewPosition.xy; float rrV=exp(-pow((dot(rrP,vec2(.93,.37))-uVarre.x)/uVarre.y,2.))*uVarre.z; totalEmissiveRadiance+=uVarreCor*rrV*(.12+rrS*1.4+rrI*1.8); }`); };
  m.customProgramCacheKey=()=>(antK?antK.call(m):'')+'|skin'; m.extensions=Object.assign({},m.extensions,{derivatives:true}); m.needsUpdate=true; }
// ajusta um material ao papel da skin (guarda o original na primeira vez, para a de serviço voltar exata)
function ajustar(m,p,fp){ const b=m.userData.base||(m.userData.base={cor:m.color.getHex(),rug:m.roughness,met:m.metalness});
  const claro=m.userData.claro||1; m.color.setHex(p.cor!=null?p.cor:b.cor); if(claro!==1&&p.cor!=null) m.color.multiplyScalar(claro);
  if(m.roughness!=null) m.roughness=p.rug!=null?p.rug:b.rug; if(m.metalness!=null) m.metalness=p.met!=null?p.met:b.met;
  const U=m.userData.skinU; if(!U) return; const T=fp&&p.det?textura(p.det):null;
  U.uDet.value=T||VAZIA; U.uEsc.value=p.esc||10; U.uSulco.value=T?(p.sulco!=null?p.sulco:0):0; U.uInc.value=T?(p.inc||0):0; U.uMancha.value=T?(p.mancha||0):0; U.uBump.value=T?(p.bump||0):0;
  U.uManchaCor.value.setHex(p.manchaCor||0); U.uIncCor.value.setHex(p.incCor||0); U.uIncMet.value=p.incMet!=null?p.incMet:1; U.uIncRug.value=p.incRug!=null?p.incRug:.2; }

// ---------------------------------------------------------------------------------------------------------------------
// Acessórios (malhas pequenas presas à arma). Pendentes: {g, pivo} com mola para a gravidade.
const PEND=[];
const matA={};
function mA(k,f){ return matA[k]||(matA[k]=f()); }
function pendente(pai,pos,comp){ const pivo=new THREE.Group(); pivo.position.copy(pos); pai.add(pivo); const g=new THREE.Group(); pivo.add(g); const P={pivo,g,ax:0,az:0,vx:0,vz:0,comp}; PEND.push(P); return P; }
const ACESS={
  // selo de cera do mosteiro com a fita de pergaminho, pendurado na braçadeira do cano (carabina)
  selo(A,fp){ const g=new THREE.Group(), cera=mA('cera',()=>new THREE.MeshStandardMaterial({color:0x7a1010,roughness:.42,metalness:0})), perg=mA('perg',()=>new THREE.MeshStandardMaterial({color:0xd8c9a2,roughness:.9,side:THREE.DoubleSide})),
      fio=mA('fio',()=>new THREE.MeshStandardMaterial({color:0x5a4228,roughness:1}));
    A.g.add(g); const P=pendente(g,V3(.021,.006,-.3),.05);
    const corda=new THREE.Mesh(new THREE.CylinderGeometry(.0012,.0012,.03,5),fio); corda.position.y=-.015; P.g.add(corda);
    const disco=new THREE.Mesh(new THREE.CylinderGeometry(.011,.012,.0045,14),cera); disco.rotation.z=PI/2; disco.position.y=-.034; P.g.add(disco);
    // cruz em relevo na cera e a borda irregular (três gotas)
    const c1=new THREE.Mesh(new THREE.BoxGeometry(.0016,.012,.0026),cera); c1.position.set(.0025,-.034,0); P.g.add(c1); const c2=new THREE.Mesh(new THREE.BoxGeometry(.0016,.0026,.008),cera); c2.position.set(.0025,-.032,0); P.g.add(c2);
    for(let k=0;k<3;k++){ const a=k*2.1+.4, gt=new THREE.Mesh(new THREE.SphereGeometry(.0032,8,6),cera); gt.position.set(0,-.034+Math.sin(a)*.011,Math.cos(a)*.011); P.g.add(gt); }
    for(const [dz,rot,L] of [[-.004,.12,.05],[.004,-.1,.042]]){ const fg=new THREE.PlaneGeometry(.007,L,1,4); fg.translate(0,-L/2,0); fg.rotateY(PI/2);
      const f=new THREE.Mesh(fg,perg); f.position.set(.001,-.038,dz); f.rotation.x=rot; P.g.add(f); }
    return g; },
  // granada no pé do cabo, dos dois lados (revólver)
  granada(A){ const g=new THREE.Group(), m=mA('granada',()=>new THREE.MeshStandardMaterial({color:0x6a0612,roughness:.08,metalness:.1,emissive:0x220004}));
    for(const sx of [-1,1]){ const s=new THREE.Mesh(new THREE.OctahedronGeometry(.0058,1),m); s.scale.set(.6,1,1); s.position.set(sx*.0245,-.118,.034); g.add(s);
      const aro=new THREE.Mesh(new THREE.TorusGeometry(.0066,.0011,6,16),mA('aroG',()=>new THREE.MeshStandardMaterial({color:0xd8a840,roughness:.25,metalness:1}))); aro.rotation.y=PI/2; aro.position.copy(s.position); g.add(aro); }
    A.g.add(g); return g; },
  // arame de cobre costurando as vértebras (cetro)
  arame(A){ const g=new THREE.Group(), m=mA('cobre',()=>new THREE.MeshStandardMaterial({color:0xb86a3c,roughness:.3,metalness:1})); const V=A.vert||[];
    for(let i=0;i+1<V.length;i++){ const p=V[i].position.clone().lerp(V[i+1].position,.5); for(const dy of [-.004,.004]){ const t=new THREE.Mesh(new THREE.TorusGeometry(.0172,.0017,6,20),m); t.rotation.x=PI/2; t.position.copy(p); t.position.y+=dy; t.rotation.y=i*.7; g.add(t); } }
    A.g.add(g); return g; },
  // amuletos de falange em correntinhas, presos no anel da cabeça (cetro)
  amuletos(A){ const g=new THREE.Group(), osso=mA('falange',()=>new THREE.MeshStandardMaterial({color:0xcbb487,roughness:.7})), elo=mA('elo',()=>new THREE.MeshStandardMaterial({color:0x8a6a48,roughness:.4,metalness:1}));
    A.cab.add(g); for(const a of [.9,2.6,4.6]){ const P=pendente(g,V3(Math.cos(a)*.052,.03,Math.sin(a)*.052),.06);
      for(let k=0;k<4;k++){ const e=new THREE.Mesh(new THREE.TorusGeometry(.0024,.0007,4,8),elo); e.position.y=-.004-k*.0045; e.rotation.y=k*PI/2; P.g.add(e); }
      const f=new THREE.Mesh(new THREE.CylinderGeometry(.0035,.0028,.02,6),osso); f.position.y=-.032; P.g.add(f);   // falange (o three r128 não tem cápsula)
      const nodo=new THREE.Mesh(new THREE.SphereGeometry(.0045,8,6),osso); nodo.position.y=-.022; P.g.add(nodo); }
    return g; },
  // cracas nas garras e na ponta da espinha (cetro)
  cracas(A){ const g=new THREE.Group(), m=mA('craca',()=>new THREE.MeshStandardMaterial({color:0x7c8a7a,roughness:.95})), boca=mA('cracaB',()=>new THREE.MeshStandardMaterial({color:0x101412,roughness:1}));
    let s=17; const r=()=>(s=(s*16807)%2147483647)/2147483647;
    for(let k=0;k<26;k++){ const a=(k%4)/4*TAU+.4+(r()-.5)*.5, y=.03+r()*.13, rr=.05+.022*Math.sin(y/.18*PI)-.01;
      const c=new THREE.Mesh(new THREE.CylinderGeometry(.0028,.0058,.0055,7),m); c.position.set(Math.cos(a)*rr,y,Math.sin(a)*rr); c.lookAt(c.position.clone().multiplyScalar(3).setY(y)); c.rotateX(PI/2); A.cab.add(c); g.userData[k]=c;
      const b=new THREE.Mesh(new THREE.CircleGeometry(.0019,6),boca); b.position.y=.0029; b.rotation.x=-PI/2; c.add(b); }
    g.userData.pecas=Object.values(g.userData); return g; }
};
function limparAcessorios(A){ A.acess=A.acess||[]; for(const g of A.acess){ const pecas=g.userData.pecas; if(pecas) pecas.forEach(p=>{ if(p.parent) p.parent.remove(p); p.traverse(o=>o.geometry&&o.geometry.dispose()); }); if(g.parent) g.parent.remove(g);
    g.traverse(o=>{ if(o.geometry) o.geometry.dispose(); const i=PEND.findIndex(P=>P.pivo===o); if(i>=0) PEND.splice(i,1); }); } A.acess=[]; }

// ---------------------------------------------------------------------------------------------------------------------
// Aplicar uma skin a uma arma montada ({g, cab, brasa, halo, vert, cruzCor...}). fp: primeira pessoa (com detalhe e
// acessórios que balançam); fora dela, só cor e acabamento (a arma é pequena na tela e não vale um shader a mais).
function aplicar(A,arma,id,fp){ const sk=porId(arma,id); A.skin=sk;
  A.g.traverse(o=>{ if(!o.isMesh) return; for(const m of (Array.isArray(o.material)?o.material:[o.material])){ const pp=m&&m.userData.papel; if(!pp||pp==='brasa') continue; ajustar(m,sk.papeis[pp]||{},fp); } });
  // a brasa e as vértebras acesas tomam a cor da skin só na primeira pessoa (os outros veem a cor do culto)
  if(arma==='cetro'&&fp){ const cor=new THREE.Color((sk.papeis.brasa&&sk.papeis.brasa.cor)||A.corOriginal||0xb050ff); A.corBrasa=cor;
    if(A.brasa) A.brasa.material.color.copy(cor); if(A.halo) A.halo.material.color.copy(cor); if(A.vert) A.vert.forEach(v=>v.material.emissive.copy(cor)); }
  if(arma==='revolver') A.cruzCor=(sk.insp&&sk.insp.cruz)||[.9,.75,.35];
  // acessórios só na primeira pessoa: no terceiro-pessoa são poucos pixels, e os pendentes custariam um quadro a mais por ator
  limparAcessorios(A); if(fp) for(const a of (sk.acessorios||[])) if(ACESS[a]) A.acess.push(ACESS[a](A,fp));
  return sk; }

// ---------------------------------------------------------------------------------------------------------------------
// Ganchos de inspeção e quadro a quadro (varredura de luz, gotas, pendentes)
const VARRE=[];
function gancho(A,nome,camera){ const sk=A.skin; if(!sk) return; const I=sk.insp||{};
  if(nome==='revela'){ const R=I.revela||{cor:0xffffff,forca:.55}; const mats=[]; A.g.traverse(o=>{ if(o.isMesh&&o.material&&o.material.userData&&o.material.userData.skinU) mats.push(o.material); });
    VARRE.push({mats,t:0,dur:1.1,cor:new THREE.Color(R.cor),forca:R.forca}); if(I.som) tocar(I.som,{vol:.32,max:1}); }
  if(nome==='gotas'&&I.gotas&&A.brasa) A.gotasT=2.6;
  if(nome==='respira'&&I.gotas) A.respiraT=2.2; }
const _g=new THREE.Vector3(), _q=new THREE.Quaternion(), _p=new THREE.Vector3();
function quadro(dt,armas){
  for(let i=VARRE.length-1;i>=0;i--){ const V=VARRE[i]; V.t+=dt; const u=V.t/V.dur, x=-.45+u*1.0, k=Math.sin(Math.min(1,u)*PI)*V.forca;
    for(const m of V.mats){ const U=m.userData.skinU; U.uVarre.value.set(x,.07,u<1?k:0); U.uVarreCor.value.copy(V.cor); } if(u>=1) VARRE.splice(i,1); }
  // pendentes: a ponta procura a gravidade no quadro do pai, com mola e amortecimento (balança quando a arma vira)
  for(const P of PEND){ if(!P.pivo.parent) continue; P.pivo.parent.updateMatrixWorld(); P.pivo.parent.getWorldQuaternion(_q); _g.set(0,-1,0).applyQuaternion(_q.invert());
    const tx=Math.atan2(-_g.z,-_g.y), tz=Math.atan2(_g.x,-_g.y), k=90, c=7;
    P.vx+=((tx-P.ax)*k-P.vx*c)*dt; P.vz+=((tz-P.az)*k-P.vz*c)*dt; P.ax+=P.vx*dt; P.az+=P.vz*dt; P.ax=Math.max(-1.4,Math.min(1.4,P.ax)); P.az=Math.max(-1.4,Math.min(1.4,P.az));
    P.pivo.rotation.set(P.ax,0,P.az); }
  for(const A of armas){ if(!A||!A.g.visible) continue;
    if(A.gotasT>0){ A.gotasT-=dt; A.gotaAcc=(A.gotaAcc||0)+dt; if(A.gotaAcc>.16){ A.gotaAcc=0; gota(A); } }
    if(A.respiraT>0){ A.respiraT-=dt; const u=1-A.respiraT/2.2, s=1+Math.sin(u*PI*3)*.06*Math.sin(u*PI); A.cab.scale.set(s,1+(s-1)*.4,s); } else if(A.cab&&A.cab.scale.x!==1) A.cab.scale.set(1,1,1); } }
function gota(A){ A.brasa.getWorldPosition(_p); const m=new THREE.SpriteMaterial({map:RT.glow,color:0x9fe8d0,transparent:true,opacity:.85,depthWrite:false}); const sp=new THREE.Sprite(m);
  const ang=Math.random()*TAU; sp.position.copy(_p).add(V3(Math.cos(ang)*.035,-.05,Math.sin(ang)*.035)); sp.scale.set(.012,.018,1); const v=V3(0,0,0);
  efeito(sp,.7,(o,u)=>{ v.y-=9.8*.016; o.position.addScaledVector(v,.016); o.material.opacity=.85*(1-u); }); }

return {COLECAO,OFICIOS,SKINS,ESC,porId,limpar,salvar,preparar,aplicar,gancho,quadro,textura,
  escolher(arma,id){ if(!SKINS[arma]||!SKINS[arma].some(s=>s.id===id)) return false; ESC[arma]=id; salvar(); return true; }};
};
