/* Ritual Reversal — o visual do Asilo Blackwater (o segundo mapa).
   A colisão, as portas e a luz vêm da simulação (shared/sim.js, mapaAsilo); aqui só a aparência: pisos de ladrilho e de
   tábua, paredes de reboco descascado com lambri, tetos e telhados, portas que giram, lâmpadas penduradas por ala (que
   apagam com o quadro de força), quadros de força, móveis, o terreno (horta, gramado com o coreto, cemitério), a margem
   e o lago. Montado por RR_ASILO(dependências).construir(); atualizar(dt, V, agora) a cada quadro. */
window.RR_ASILO=function(D){
'use strict';
const {THREE,world,Sim,MAT,TEX,RT,meshBox,boxGeo,canvasTex,FONTES,flames,CEU,PERF,desgastar,pbr,makeNoise,smooth,scene}=D;
const PI=Math.PI, E=Sim.ESPACOS, W=Sim.WALLS;
let _s=4711; const R=()=>(_s=(_s*16807)%2147483647)/2147483647;   // sorteio próprio: não mexe no vr() da catedral
const add=o=>{ world.add(o); return o; };
const caixa=(x1,x2,y1,y2,z1,z2,mat,S,sombra)=>{ const m=meshBox(x1,x2,y1,y2,z1,z2,mat,S||2); if(sombra===false) m.castShadow=false; return m; };

// ---------- materiais ----------
const n=makeNoise(9091);
TEX.reboco=pbr(256,(u,v,o)=>{ const f=n.fbm(u*5,v*5,5,5), desc=smooth(.62,.7,n.fbm(u*3+7,v*3,3,4)), mancha=smooth(.5,.9,n.fbm(u*2,v*9+3,2,3))*(1-v)*.5;
  const k=.85+.18*(f-.5)-mancha*.4; o.h=f*.3-desc*.5;
  if(desc>.5){ o.r=120*k; o.g=84*k; o.b=64*k; o.ro=.95; } else { o.r=150*k; o.g=160*k; o.b=140*k; o.ro=.9; } },1.6);   // verde-hospital, descascando no tijolo
TEX.lambri=pbr(256,(u,v,o)=>{ const T=8,tx=Math.floor(u*T),ty=Math.floor(v*T),lu=u*T-tx,lv=v*T-ty,e=Math.min(lu,1-lu,lv,1-lv), f=n.fbm(u*9,v*9,9,3), rachado=smooth(.7,.75,n.fbm(u*6+1,v*6,6,3));
  const junta=e<.05?.45:1, k=(.78+.2*f)*junta*(1-rachado*.3); o.h=junta<1?-.4:f*.1; o.r=72*k; o.g=92*k; o.b=84*k; o.ro=junta<1?.95:.35; },1.2);    // azulejo verde-escuro
TEX.ladrilho=pbr(512,(u,v,o)=>{ const T=10,tx=Math.floor(u*T),ty=Math.floor(v*T),lu=u*T-tx,lv=v*T-ty,e=Math.min(lu,1-lu,lv,1-lv), f=n.fbm(u*8,v*8,8,4), suj=smooth(.45,.85,n.fbm(u*2+4,v*2,2,4));
  const claro=(tx+ty)%2===0, junta=e<.04?.5:1, k=(claro?1:.42)*(.82+.2*f)*junta*(1-suj*.35); o.h=junta<1?-.3:f*.08; o.r=196*k; o.g=190*k; o.b=172*k; o.ro=junta<1?.95:.4+suj*.4; },1);   // xadrez gasto
TEX.tabua=pbr(256,(u,v,o)=>{ const tb=Math.floor(u*6), lu=u*6-tb, f=n.fbm(u*3,v*24+tb*7,3,4), junta=lu<.04?.5:1, k=(.65+.3*f)*junta; o.h=junta<1?-.3:f*.2; o.r=82*k; o.g=58*k; o.b=40*k; o.ro=.75; },1.3);
TEX.teto=pbr(256,(u,v,o)=>{ const f=n.fbm(u*4,v*4,4,5), mofo=smooth(.55,.8,n.fbm(u*2+9,v*2,2,4)), k=.6+.2*f-mofo*.35; o.h=f*.2; o.r=120*k; o.g=120*k; o.b=112*k; o.ro=.95; },1);
TEX.grama=pbr(256,(u,v,o)=>{ const f=n.fbm(u*10,v*10,10,5), t=smooth(.5,.8,n.fbm(u*3+2,v*3,3,4)), k=.55+.3*f; o.h=f*.3; o.r=(46+40*t)*k; o.g=(62+20*t)*k; o.b=(34+12*t)*k; o.ro=1; },1.4);
TEX.terra=pbr(256,(u,v,o)=>{ const f=n.fbm(u*8,v*8,8,5), k=.6+.3*f; o.h=f*.4; o.r=64*k; o.g=48*k; o.b=34*k; o.ro=1; },1.6);
TEX.cascalho=pbr(256,(u,v,o)=>{ const f=n.fbm(u*20,v*20,20,4), k=.55+.4*f; o.h=f*.6; o.r=96*k; o.g=92*k; o.b=86*k; o.ro=1; },2.2);
const rep=(t,x,y)=>{ const c={}; for(const k of ['map','normalMap','roughnessMap']) if(t[k]){ c[k]=t[k].clone(); c[k].wrapS=c[k].wrapT=THREE.RepeatWrapping; c[k].repeat.set(x,y); c[k].needsUpdate=true; } return c; };
const M={
  reboco:desgastar(new THREE.MeshStandardMaterial({...TEX.reboco,roughness:1}),{mancha:.5,base:.45,musgo:.2}),
  lambri:new THREE.MeshStandardMaterial({...TEX.lambri,roughness:1}),
  ladrilho:desgastar(new THREE.MeshStandardMaterial({...TEX.ladrilho,roughness:1}),{mancha:.5,base:0,poca:1}),
  tabua:new THREE.MeshStandardMaterial({...TEX.tabua,roughness:1}),
  teto:new THREE.MeshStandardMaterial({...TEX.teto,roughness:1,side:THREE.DoubleSide}),
  telhado:new THREE.MeshStandardMaterial({color:0x2a2622,map:TEX.tabua.map,roughness:1}),
  grama:new THREE.MeshStandardMaterial({...TEX.grama,roughness:1}),
  terra:new THREE.MeshStandardMaterial({...TEX.terra,roughness:1}),
  cascalho:new THREE.MeshStandardMaterial({...TEX.cascalho,roughness:1}),
  muro:MAT.wall, ferro:new THREE.MeshStandardMaterial({color:0x2c2e30,metalness:.8,roughness:.55,normalMap:TEX.metal.normalMap}),
  ferrugem:new THREE.MeshStandardMaterial({color:0x5a3424,metalness:.5,roughness:.9}),
  colchao:new THREE.MeshStandardMaterial({color:0x8e8a78,map:TEX.cloth.map,normalMap:TEX.cloth.normalMap,roughness:1}),
  porcelana:new THREE.MeshStandardMaterial({color:0xd6d2c4,roughness:.35}),
  concreto:new THREE.MeshStandardMaterial({color:0x6a6862,map:TEX.reboco.map,roughness:1}),
  madeira:MAT.wood, carvao:new THREE.MeshStandardMaterial({color:0x111012,roughness:.8,flatShading:true}),
  agua:new THREE.MeshStandardMaterial({color:0x0b1416,metalness:.6,roughness:.08,transparent:true,opacity:.92}),
  lago:new THREE.MeshStandardMaterial({color:0x05090b,metalness:.75,roughness:.12}),
  porta:new THREE.MeshStandardMaterial({color:0x7a5c40,map:TEX.wood.map,normalMap:TEX.wood.normalMap,roughness:.9}),
  lampada:new THREE.MeshStandardMaterial({color:0xfff2d0,emissive:0xffd890,emissiveIntensity:2.2,roughness:.3}),
  cupula:new THREE.MeshStandardMaterial({color:0x2e3532,metalness:.6,roughness:.5}),
  cupulaDentro:new THREE.MeshBasicMaterial({color:0x7a6648,side:THREE.BackSide}),   // o esmalte de dentro, sem luz: a lâmpada colada nele estourava
};
for(const k of ['reboco','ladrilho','grama','terra']) M[k].normalScale&&M[k].normalScale.set(.6,.6);
const ALT=4.4;   // pé-direito: o teto fica a 4,4 m; as paredes da simulação têm 4,5

// ---------- pisos, tetos e telhados ----------
const piso=(e,mat,S,y)=>{ const g=new THREE.PlaneGeometry(e.x2-e.x1,e.z2-e.z1); g.rotateX(-PI/2);
  const uv=g.attributes.uv; for(let i=0;i<uv.count;i++) uv.setXY(i,uv.getX(i)*(e.x2-e.x1)/S,uv.getY(i)*(e.z2-e.z1)/S);
  const m=new THREE.Mesh(g,mat); m.position.set((e.x1+e.x2)/2,y||0,(e.z1+e.z2)/2); m.receiveShadow=true; return add(m); };
const pisoDe=nome=>/Capela|Saguão|Arquivo|Sala de Visitas|Sala de Estar/.test(nome)?[M.tabua,3]:/Caldeiras|Lavanderia|Depósito|Rouparia|Oficina|Escada/.test(nome)?[M.concreto,4]:[M.ladrilho,4];
for(const e of E){
  if(e.int){ const [mat,S]=pisoDe(e.nome); piso(e,mat,S,.01);
    const t=piso(e,M.teto,5,ALT); t.rotation.x=PI; t.receiveShadow=false;
    caixa(e.x1-.6,e.x2+.6,ALT+.02,ALT+.45,e.z1-.6,e.z2+.6,M.telhado,4); }   // telhado por cima, cobre o topo das paredes
  else { const mat=/Horta/.test(e.nome)?M.terra:/Margem|Trilha|Píer/.test(e.nome)?M.cascalho:M.grama; piso(e,/Píer/.test(e.nome)?M.tabua:mat,/Píer/.test(e.nome)?2:6,.005); }
}
// vãos: chão do vão e, nas passagens internas, verga por cima da abertura (o vão da simulação vai até o céu)
for(const v of Sim.VAOS){ const dentro=E.some(e=>e.int&&v.x1<e.x2+.7&&v.x2>e.x1-.7&&v.z1<e.z2+.7&&v.z2>e.z1-.7);
  piso({x1:v.x1,x2:v.x2,z1:v.z1,z2:v.z2},dentro?M.ladrilho:M.cascalho,4,.012);
  if(dentro){ const largo=Math.max(v.x2-v.x1,v.z2-v.z1); caixa(v.x1,v.x2,largo>3.5?3.2:2.75,4.5,v.z1,v.z2,M.reboco,2); } }

// ---------- paredes ----------
for(const w of W){
  if(w.kind==='parede'){ caixa(w.x1,w.x2,0,w.h,w.z1,w.z2,M.reboco,3);
    caixa(w.x1-.03,w.x2+.03,0,1.25,w.z1-.03,w.z2+.03,M.lambri,1.2,false);          // lambri de azulejo até 1,25 m
    caixa(w.x1-.05,w.x2+.05,1.25,1.33,w.z1-.05,w.z2+.05,M.ferrugem,2,false); }     // friso
  else if(w.kind==='muro'){ caixa(w.x1,w.x2,0,w.h,w.z1,w.z2,M.muro,4); caixa(w.x1-.12,w.x2+.12,w.h,w.h+.25,w.z1-.12,w.z2+.12,MAT.wallDark,4,false); }
  else if(w.kind==='lago'){ /* o lago é desenhado inteiro abaixo */ }
}
// o lago Blackwater: espelho escuro até o horizonte, com névoa por cima
{ const g=new THREE.PlaneGeometry(600,260); g.rotateX(-PI/2); const m=new THREE.Mesh(g,M.lago); m.position.set(0,-.35,-66-130); add(m);
  const b=new THREE.Mesh(new THREE.BoxGeometry(220,.4,.6),MAT.wallDark); b.position.set(0,-.1,-66.2); add(b); }
// o píer: tábuas sobre estacas
for(let z=-71.5;z<=-66.5;z+=1){ for(const x of [-2.2,2.2]){ const p=new THREE.Mesh(new THREE.CylinderGeometry(.12,.14,1.4,6),M.madeira); p.position.set(x,-.5,z); add(p); } }

// ---------- portas ----------
const PORTAS=[];   // {piv:[grupos], aberta, alvo}
for(const P of Sim.PORTAS_DIN){ const ax=P.eixo==='x', larg=ax?P.x2-P.x1:P.z2-P.z1, folhas=larg>3.2?2:1, lf=larg/folhas, esp=.12, alt=Math.min(2.7,ALT-.3);
  const portal=[]; for(let k=0;k<folhas;k++){ const piv=new THREE.Group(); piv.userData.dinamico=true;   // giram: ficam fora da fusão de estáticos
    // portas de correr: o canto da folha fica na ponta do vão e, aberta, a folha entra na parede (nada sobra no caminho)
    piv.position.set(ax?(k===0?P.x1:P.x2):P.x,0,ax?P.z:(k===0?P.z1:P.z2));
    const f=new THREE.Mesh(boxGeo(ax?lf:esp,alt,ax?esp:lf,1.5),M.porta); f.castShadow=f.receiveShadow=true;
    const s=k===0?1:-1; if(ax) f.position.set(s*lf/2,alt/2,0); else f.position.set(0,alt/2,s*lf/2);
    const mac=new THREE.Mesh(new THREE.SphereGeometry(.05,8,6),M.ferro); if(ax) mac.position.set(s*(lf-.12),1.05,.08); else mac.position.set(.08,1.05,s*(lf-.12)); piv.add(f,mac);
    const vis=new THREE.Mesh(boxGeo(ax?lf*.5:esp+.02,.35,ax?esp+.02:lf*.5,1),new THREE.MeshStandardMaterial({color:0x1a2224,metalness:.4,roughness:.2})); vis.position.set(ax?s*lf/2:0,1.75,ax?0:s*lf/2); piv.add(vis);   // visor
    add(piv); portal.push({piv,s,x0:piv.position.x,z0:piv.position.z,lf}); }
  PORTAS.push({P,folhas:portal,ang:1}); }   // começam abertas

// ---------- lâmpadas e quadros de força ----------
const LAMP={oeste:[],centro:[],leste:[]};
const flameMat=new THREE.SpriteMaterial({map:RT.glow,color:0xffe0a0,blending:THREE.AdditiveBlending,depthWrite:false,transparent:true,opacity:.4});
for(const c of Sim.CANDLES){ const [x,z,tipo,setor]=c; if(tipo!=='lampada') continue;
  const fio=new THREE.Mesh(new THREE.CylinderGeometry(.012,.012,.7,4),M.ferro); fio.position.set(x,ALT-.35,z); add(fio);
  const cg=new THREE.ConeGeometry(.42,.28,12,1,true), cup=new THREE.Mesh(cg,M.cupula), cin=new THREE.Mesh(cg,M.cupulaDentro); cup.position.set(x,ALT-.78,z); cin.position.copy(cup.position); add(cup); add(cin);
  const lamp=new THREE.Mesh(new THREE.SphereGeometry(.1,10,8),M.lampada.clone()); lamp.position.set(x,ALT-.86,z); add(lamp);
  const brilho=new THREE.Sprite(flameMat.clone()); brilho.scale.set(.55,.55,1); brilho.position.set(x,ALT-.9,z); add(brilho);
  const fonte={x,z,y:ALT-1.7,cor:0xffdca0,int:1.3,alc:13,setor}; FONTES.push(fonte);   // abaixo da cúpula, longe do teto
  LAMP[setor].push({lamp,brilho,fonte,fase:R()*10}); }
const QUADRO=[];
for(const q of Sim.QUADROS){ // caixa de metal na parede mais próxima, com alavanca e uma lâmpada vermelha/verde
  const g=new THREE.Group(); g.position.set(q.x,0,q.z); g.userData.dinamico=true; add(g);
  const cx=new THREE.Mesh(boxGeo(.7,.9,.22,1),M.ferro); cx.position.set(0,1.45,0); g.add(cx);
  const alav=new THREE.Mesh(new THREE.BoxGeometry(.06,.36,.06),M.ferrugem); alav.position.set(.2,1.5,.14); g.add(alav);
  const luz=new THREE.Mesh(new THREE.SphereGeometry(.05,8,6),new THREE.MeshStandardMaterial({color:0x30ff60,emissive:0x30ff60,emissiveIntensity:2})); luz.position.set(-.2,1.75,.12); g.add(luz);
  const cabo=new THREE.Mesh(new THREE.CylinderGeometry(.03,.03,ALT-1.9,5),M.ferro); cabo.position.set(0,1.9+(ALT-1.9)/2,0); g.add(cabo);
  QUADRO.push({q,alav,luz}); }

// ---------- móveis (a caixa da simulação é a colisão; aqui o que se vê) ----------
const leito=w=>{ const ax=w.x2-w.x1>w.z2-w.z1, cx=(w.x1+w.x2)/2, cz=(w.z1+w.z2)/2, L=ax?w.x2-w.x1:w.z2-w.z1, P=ax?w.z2-w.z1:w.x2-w.x1;
  const B=(dx,y1,y2,dz,lx,lz,mat)=>{ const x=ax?cx+dx:cx+dz, z=ax?cz+dz:cz+dx, sx=ax?lx:lz, sz=ax?lz:lx; caixa(x-sx/2,x+sx/2,y1,y2,z-sz/2,z+sz/2,mat,1); };
  B(0,.42,.6,0,L-.1,P-.1,M.colchao); B(0,.36,.42,0,L,P,M.ferro);
  for(const s of [-1,1]){ B(s*(L/2-.03),0,s<0?1.05:.8,0,.05,P,M.ferro); for(const t of [-1,1]) B(s*(L/2-.03),0,.4,t*(P/2-.03),.05,.05,M.ferro); }
  B(-L/2+.4,.6,.72,0,.5,P*.7,M.colchao); };
const PEC={
  leito, banco:w=>{ caixa(w.x1,w.x2,.42,.5,w.z1,w.z2,M.madeira,2); caixa(w.x1,w.x2,.5,w.h,w.z2-.1,w.z2,M.madeira,2); caixa(w.x1+.1,w.x1+.2,0,.42,w.z1,w.z2,M.madeira,2); caixa(w.x2-.2,w.x2-.1,0,.42,w.z1,w.z2,M.madeira,2); },
  mesa:w=>{ caixa(w.x1,w.x2,w.h-.07,w.h,w.z1,w.z2,M.madeira,2); for(const [x,z] of [[w.x1+.1,w.z1+.1],[w.x2-.1,w.z1+.1],[w.x1+.1,w.z2-.1],[w.x2-.1,w.z2-.1]]) caixa(x-.05,x+.05,0,w.h-.07,z-.05,z+.05,M.madeira,1); },
  tanque:w=>{ caixa(w.x1,w.x2,0,w.h,w.z1,w.z2,M.concreto,2); caixa(w.x1+.12,w.x2-.12,w.h-.05,w.h+.01,w.z1+.12,w.z2-.12,M.agua,1,false); },
  bancada:w=>{ caixa(w.x1,w.x2,w.h-.08,w.h,w.z1,w.z2,M.madeira,2); caixa(w.x1,w.x2,0,w.h-.08,w.z1+.1,w.z2-.1,M.madeira,2); },
  maca:w=>{ caixa(w.x1,w.x2,.8,.88,w.z1,w.z2,M.colchao,1); caixa(w.x1+.1,w.x2-.1,.72,.8,w.z1+.1,w.z2-.1,M.ferro,1); for(const [x,z] of [[w.x1+.15,w.z1+.15],[w.x2-.15,w.z1+.15],[w.x1+.15,w.z2-.15],[w.x2-.15,w.z2-.15]]) caixa(x-.03,x+.03,0,.72,z-.03,z+.03,M.ferro,1); },
  cadeira_eletrica:w=>{ const cx=(w.x1+w.x2)/2, cz=(w.z1+w.z2)/2; caixa(cx-.45,cx+.45,.45,.55,cz-.45,cz+.45,M.madeira,1); caixa(cx-.45,cx+.45,.55,1.7,cz+.35,cz+.45,M.madeira,1);
    caixa(cx-.5,cx-.4,.55,.85,cz-.45,cz+.45,M.madeira,1); caixa(cx+.4,cx+.5,.55,.85,cz-.45,cz+.45,M.madeira,1); caixa(cx-.2,cx+.2,1.7,1.85,cz+.2,cz+.45,M.ferro,1); caixa(cx-.4,cx+.4,0,.45,cz-.4,cz+.4,M.madeira,1); },
  banheira:w=>{ const ax=w.x2-w.x1>w.z2-w.z1; caixa(w.x1,w.x2,.12,w.h,w.z1,w.z2,M.porcelana,1); caixa(w.x1+.1,w.x2-.1,w.h-.18,w.h+.005,w.z1+.1,w.z2-.1,M.agua,1,false);
    for(const s of [-1,1]) for(const t of [-1,1]){ const x=ax?(s<0?w.x1+.15:w.x2-.15):(w.x1+w.x2)/2+t*.3, z=ax?(w.z1+w.z2)/2+t*.3:(s<0?w.z1+.15:w.z2-.15); caixa(x-.05,x+.05,0,.12,z-.05,z+.05,M.ferro,1); } },
  caldeira:w=>{ const cx=(w.x1+w.x2)/2, cz=(w.z1+w.z2)/2, r=Math.min(w.x2-w.x1,w.z2-w.z1)/2; const c=new THREE.Mesh(new THREE.CylinderGeometry(r,r,w.h-.4,16),M.ferrugem); c.position.set(cx,.4+(w.h-.4)/2,cz); c.castShadow=true; add(c);
    caixa(w.x1,w.x2,0,.4,w.z1,w.z2,M.concreto,2); const t=new THREE.Mesh(new THREE.CylinderGeometry(.22,.22,ALT-w.h,8),M.ferro); t.position.set(cx,w.h+(ALT-w.h)/2,cz); add(t);
    for(const y of [.9,1.9,2.9]){ const anel=new THREE.Mesh(new THREE.TorusGeometry(r+.02,.05,6,20),M.ferro); anel.rotation.x=Math.PI/2; anel.position.set(cx,y,cz); add(anel); } // cintas rebitadas
    const boca=new THREE.Mesh(new THREE.CircleGeometry(.35,12),new THREE.MeshStandardMaterial({color:0x200800,emissive:0xff5010,emissiveIntensity:.8})); boca.position.set(cx,1.1,cz+r+.01); add(boca);
    for(let k=-1;k<=1;k++){ const grade=new THREE.Mesh(boxGeo(.04,.62,.04,1),M.ferro); grade.position.set(cx+k*.15,1.1,cz+r+.04); add(grade); } // grade da fornalha
    FONTES.push({x:cx,z:cz+r+.6,y:1.1,cor:0xff6a20,int:1.8,alc:9}); },
  carvao:w=>{ for(let k=0;k<7;k++){ const p=new THREE.Mesh(new THREE.DodecahedronGeometry(.25+R()*.25,0),M.carvao); p.position.set(w.x1+R()*(w.x2-w.x1),.15+R()*w.h*.6,w.z1+R()*(w.z2-w.z1)); add(p); } },
  balcao:w=>{ caixa(w.x1,w.x2,0,w.h,w.z1,w.z2,M.madeira,2); caixa(w.x1-.05,w.x2+.05,w.h,w.h+.06,w.z1-.08,w.z2+.08,M.madeira,2); },
  coluna_coreto:w=>{ const cx=(w.x1+w.x2)/2, cz=(w.z1+w.z2)/2; const c=new THREE.Mesh(new THREE.CylinderGeometry(.13,.16,w.h,8),M.ferro); c.position.set(cx,w.h/2,cz); c.castShadow=true; add(c); },
  canteiro:w=>{ caixa(w.x1,w.x2,0,w.h*.6,w.z1,w.z2,M.terra,2); for(let x=w.x1+.3;x<w.x2-.2;x+=.55){ const p=new THREE.Mesh(new THREE.ConeGeometry(.16,.4+R()*.3,5),new THREE.MeshStandardMaterial({color:0x2c3a22,roughness:1,flatShading:true})); p.position.set(x,w.h*.6+.2,(w.z1+w.z2)/2+(R()-.5)*.4); add(p); } },
  lapide:w=>{ const cx=(w.x1+w.x2)/2, cz=(w.z1+w.z2)/2; caixa(w.x1,w.x2,0,w.h-.2,w.z1,w.z2,MAT.lapide,1); const t=new THREE.Mesh(new THREE.CylinderGeometry((w.x2-w.x1)/2,(w.x2-w.x1)/2,w.z2-w.z1,10,1,false,0,PI),MAT.lapide); t.rotation.set(PI/2,0,PI/2); t.position.set(cx,w.h-.2,cz); add(t);
    if(R()<.4){ const cz2=new THREE.Mesh(new THREE.BoxGeometry(.06,.5,.06),M.ferro); cz2.position.set(cx,w.h+.25,cz); add(cz2); } },
};
for(const w of W) if(PEC[w.kind]) PEC[w.kind](w);
// o teto do coreto: chapéu de ferro sobre as seis colunas
{ const t=new THREE.Mesh(new THREE.ConeGeometry(6.6,2.2,6,1),M.ferro); t.position.set(0,3.4+1.1,46); t.castShadow=true; add(t); caixa(-5.8,5.8,0,.3,40.2,51.8,M.concreto,3,false); }
// tanques da hidroterapia: água rasa com borda
for(const q of Sim.AGUA){ caixa(q.x1,q.x2,.02,.22,q.z1,q.z2,M.agua,2,false); for(const [a,b,c,d] of [[q.x1-.2,q.x2+.2,q.z1-.2,q.z1],[q.x1-.2,q.x2+.2,q.z2,q.z2+.2],[q.x1-.2,q.x1,q.z1,q.z2],[q.x2,q.x2+.2,q.z1,q.z2]]) caixa(a,b,0,.3,c,d,M.porcelana,1,false); }
// grade de ferro na margem e no muro do terreno: lanças a cada 30 cm em cima das sebes
for(const w of W) if(w.kind==='muro'&&w.h<3.5){ const ax=w.x2-w.x1>w.z2-w.z1, L=ax?w.x2-w.x1:w.z2-w.z1; if(L>40) continue;
  for(let t=.15;t<L;t+=.3){ const x=ax?w.x1+t:(w.x1+w.x2)/2, z=ax?(w.z1+w.z2)/2:w.z1+t; const l=new THREE.Mesh(new THREE.CylinderGeometry(.015,.015,.8,4),M.ferro); l.position.set(x,w.h+.65,z); add(l); } }

// ---------- céu, lua e luz ambiente (como na catedral, sem o claustro) ----------
{ const ceuTex=canvasTex(8,256,(c,W,H)=>{ const gr=c.createLinearGradient(0,0,0,H); gr.addColorStop(0,'#010205'); gr.addColorStop(.55,'#05090f'); gr.addColorStop(1,'#0c1418'); c.fillStyle=gr; c.fillRect(0,0,W,H); });
  const cupula=new THREE.Group(); cupula.userData.dinamico=true; add(cupula); CEU.cupula=cupula;
  const ceu=new THREE.Mesh(new THREE.SphereGeometry(190,24,14),new THREE.MeshBasicMaterial({map:ceuTex,fog:false,toneMapped:false,side:THREE.BackSide,depthWrite:false})); ceu.renderOrder=-2; cupula.add(ceu); CEU.ceu=ceu;
  const N=1400,pos=new Float32Array(N*3); for(let i=0;i<N;i++){ const a=R()*6.283, el=Math.asin(.12+R()*.88), r=180; pos[i*3]=Math.cos(a)*Math.cos(el)*r; pos[i*3+1]=Math.sin(el)*r; pos[i*3+2]=Math.sin(a)*Math.cos(el)*r; }
  const g=new THREE.BufferGeometry(); g.setAttribute('position',new THREE.BufferAttribute(pos,3));
  const est=new THREE.Points(g,new THREE.PointsMaterial({size:.9,color:0xdfe6ff,map:RT.glow,transparent:true,opacity:.7,depthWrite:false,fog:false,toneMapped:false})); est.renderOrder=-1; cupula.add(est); CEU.estrelas=est;
  const lua=new THREE.Sprite(new THREE.SpriteMaterial({map:RT.glow,color:0xdfe8ff,fog:false,toneMapped:false,depthWrite:false})); lua.scale.set(22,22,1); lua.userData.dinamico=true; add(lua);
  const lua2=new THREE.Sprite(new THREE.SpriteMaterial({map:RT.glow,color:0x6f88c8,fog:false,transparent:true,opacity:.35,depthWrite:false,blending:THREE.AdditiveBlending})); lua2.scale.set(64,64,1); add(lua2); CEU.lua=lua; CEU.lua2=lua2; }

// ---------- a cada quadro ----------
let _flick=0;
function atualizar(dt,V,agora){
  // portas: a folha corre para dentro da parede quando a porta está aberta
  const st=V&&V.portas;
  for(const D2 of PORTAS){ const alvo=st&&st[D2.P.i]===0?0:1; D2.ang+=(alvo-D2.ang)*Math.min(1,dt*7);
    for(const f of D2.folhas){ const d=-f.s*f.lf*.97*D2.ang; if(D2.P.eixo==='x') f.piv.position.x=f.x0+d; else f.piv.position.z=f.z0+d; } }
  // energia por ala: lâmpadas apagadas na ala sem luz (com um tremeluzir no fim do apagão)
  const en=(V&&V.energia)||{};
  for(const s in LAMP){ const rest=en[s]||0, apagada=rest>0, tremendo=apagada&&rest<3;
    for(const L of LAMP[s]){ let k=apagada?(tremendo&&Math.sin(agora*.03+L.fase*7)>.3?.6:0):1;
      if(!apagada&&Math.sin(agora*.0023+L.fase)>.995) k=.35;   // uma ou outra lâmpada falha de vez em quando
      L.lamp.material.emissiveIntensity=2.2*k; L.brilho.material.opacity=.4*k; L.fonte.int=1.3*k; } }
  for(const Q of QUADRO){ const ap=(en[Q.q.setor]||0)>0; Q.alav.rotation.z=ap?.9:0; Q.luz.material.color.set(ap?0xff3020:0x30ff60); Q.luz.material.emissive.set(ap?0xff3020:0x30ff60); }
}
return {atualizar,PORTAS,LAMP};
};
