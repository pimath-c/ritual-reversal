// =====================================================================================================================
// Primeira pessoa: armas, mãos e animação (v5.9)
// ---------------------------------------------------------------------------------------------------------------------
// Como funciona:
//  • Clipes de animação escritos à mão: cada trilha tem chaves (tempo, valor, curva). A arma, as duas mãos (posição,
//    orientação e dedos), as peças da arma (alavanca, portinhola, tambor, extrator…), os objetos de cena (cartuchos,
//    municiador) e um empurrão de câmera. Eventos marcados no tempo tocam os sons e soltam as cápsulas no mundo.
//  • Quadros de referência: uma chave de mão pode estar no corpo, na arma, na alavanca ou no tambor. A mão que segura a
//    alavanca desce com ela; a mão no fuste acompanha o coice. Entre chaves de quadros diferentes, a mão viaja no corpo.
//  • Camadas: o clipe do estado (pronta, sacar, recarga, inspeção, corrida, selar…) com mistura entre estados; por cima,
//    o coice e o ciclo mecânico de cada arma (PERFIL_ARMA), o balanço do passo, a respiração e o tranco de um golpe.
//  • Braços por IK de dois ossos (lei dos cossenos, cotovelo apontado por um vetor de polo) e mãos com dedos de três
//    falanges e polegar opositor: o punho segue a chave, o braço se resolve sozinho.
// Tudo é cosmético. Tempo de recarga (1,6 s), cadência e munição continuam sendo os do servidor; a animação se ajusta a
// eles (a recarga dura exatamente reloadT e termina quando o servidor diz que terminou).
// =====================================================================================================================
// O jogo (index.html) roda dentro de uma função: ele chama RR_ARMA com o que este módulo usa e recebe a API de volta.
window.RR_ARMA=function(D){
'use strict';
const {nivel,camera,world,matMetal,matCouro,matPano,TEX,RT,lateral,cilZ,V3,armaCarabina,cetroOsso,COMPARTILHADAS,efeito,tocar,camFx,VM,PERFIL_ARMA,coice,cicloDe,suave,CFG,ejetarCapsula}=D;
let vmH,vmC,vmCarabina,vmRevolver,vmCetro,vmOrb,vmRunas=[],vmFlash;
const PI=Math.PI, TAU=PI*2;
const cl=(v,a,b)=>v<a?a:v>b?b:v, mix=(a,b,t)=>a+(b-a)*t;
// curvas de tempo de cada trecho (a chave diz como se chega nela)
const CURVA={
  l:u=>u,                                                     // linear
  s:u=>u*u*(3-2*u),                                           // suave nos dois lados
  w:u=>u<.5?4*u*u*u:1-Math.pow(-2*u+2,3)/2,                   // suave forte (pose a pose)
  o:u=>1-Math.pow(1-u,3),                                     // sai rápido, assenta devagar
  q:u=>1-Math.pow(1-u,5),                                     // estalo: quase tudo no começo
  i:u=>u*u*u,                                                 // entra devagar, termina rápido (antecipação → golpe)
  b:u=>{ const c=1.6,c3=c+1; return 1+c3*Math.pow(u-1,3)+c*Math.pow(u-1,2); },   // passa do ponto e volta
  h:u=>u<1?0:1 };                                             // degrau (troca no instante da chave)
const v3=(a)=>new THREE.Vector3(a[0],a[1],a[2]);
const qE=(r)=>new THREE.Quaternion().setFromEuler(new THREE.Euler(r[0]||0,r[1]||0,r[2]||0,'XYZ'));
// orientação da mão por duas direções: para onde apontam os dedos esticados e para onde olha o dorso
function qMao(dedos,dorso){ const z=v3(dedos).normalize().negate(), y=v3(dorso); y.addScaledVector(z,-y.dot(z)).normalize();
  const x=new THREE.Vector3().crossVectors(y,z); return new THREE.Quaternion().setFromRotationMatrix(new THREE.Matrix4().makeBasis(x,y,z)); }
const _eu=new THREE.Euler(0,0,0,'XYZ'), _m1=new THREE.Matrix4(), _m2=new THREE.Matrix4(), _m3=new THREE.Matrix4(), _q1=new THREE.Quaternion(), _q2=new THREE.Quaternion(),
  _v1=new THREE.Vector3(), _v2=new THREE.Vector3(), _v3=new THREE.Vector3(), _v4=new THREE.Vector3(), _s1=new THREE.Vector3(1,1,1), UM=new THREE.Vector3(1,1,1);

// ---------------------------------------------------------------------------------------------------------------------
// Dedos: [indicador, médio, anelar, mínimo, polegar aberto, polegar oposto, polegar dobrado, abertura]
const DEDOS={
  aberta:   [.10,.12,.16,.2,  .6,.2,.08, .35],
  relaxada: [.32,.38,.44,.5,  .35,.35,.25, .12],
  punho:    [.95,.98,1,1,     .15,.85,.75, 0],
  coronha:  [.5,.8,.86,.9,    .3,.75,.5, .02],   // pulso da coronha: dedos pela alça da alavanca, polegar por cima
  gatilho:  [.38,.8,.86,.9,   .3,.75,.5, .02],
  fuste:    [.58,.64,.68,.72, .2,.25,.18, .04],  // fuste no berço da mão, polegar ao longo do lado
  pinca:    [.42,.86,.92,.95, .5,.8,.35, 0],     // cartucho entre polegar e indicador
  empurra:  [.5,.72,.8,.86,   .1,.25,0, 0],      // polegar esticado empurrando
  palma:    [.03,.03,.05,.07, .95,.05,.04, 1],
  garra:    [.56,.62,.68,.74, .42,.55,.55, .75],
  cetro:    [.82,.88,.92,.95, .3,.8,.6, 0],
  revolver: [.36,.84,.9,.94,  .25,.7,.3, 0],     // indicador no gatilho, polegar perto do cão
  cao:      [.36,.84,.9,.94,  .55,.35,.05, 0],   // polegar alto, no cão
  suporte:  [.66,.72,.78,.84, .35,.55,.3, .02],
  berco:    [.42,.48,.54,.6,  .55,.3,.15, .15],  // mão aberta segurando o revólver pelo tambor
  extrai:   [.4,.6,.66,.72,   .2,.15,0, .1],     // polegar esticado na vareta do extrator
  municia:  [.55,.86,.9,.92,  .5,.85,.45, 0] };
const ND=8, ESC_MAO=1.16;

// ---------------------------------------------------------------------------------------------------------------------
// Geometria das mãos e braços
const GEO={};
function capsula(r,len){ const k=r.toFixed(4)+':'+len.toFixed(4); if(GEO[k]) return GEO[k];
  const pts=[]; for(let i=0;i<=5;i++){ const a=-PI/2+i/5*PI/2; pts.push(new THREE.Vector2(Math.cos(a)*r,Math.sin(a)*r)); }
  for(let i=0;i<=5;i++){ const a=i/5*PI/2; pts.push(new THREE.Vector2(Math.cos(a)*r*.94,len+Math.sin(a)*r*.94)); }
  const g=new THREE.LatheGeometry(pts,10); g.rotateX(-PI/2); COMPARTILHADAS.add(g); return GEO[k]=g; }   // de 0 até -len em z
// palma: uma caixa de cantos redondos (superelipsoide) que afina para o pulso
function geoPalma(w,h,l){ const k='palma'+w+h+l; if(GEO[k]) return GEO[k];
  const g=new THREE.SphereGeometry(1,22,16), p=g.attributes.position, e=.42;
  for(let i=0;i<p.count;i++){ let x=p.getX(i),y=p.getY(i),z=p.getZ(i); const f=s=>Math.sign(s)*Math.pow(Math.abs(s),e);
    x=f(x)*w/2; y=f(y)*h/2*(y>0?1:.9); z=f(z)*l/2; const u=(z/l+.5); x*=mix(1,.82,u); y*=mix(1,.85,u);  // u=1 no pulso
    p.setXYZ(i,x,y,z-l/2); }
  g.computeVertexNormals();
  // nós dos dedos no dorso (fundidos na palma: nenhuma malha a mais)
  const partes=[g]; [-.03,-.01,.01,.029].forEach((x,i)=>{ const n=new THREE.SphereGeometry(.0105,10,8); n.scale(1,.62,1.05); n.translate(x,h/2*.62,-l+.012+(i===3?.006:0)); partes.push(n); });
  const tendao=new THREE.CylinderGeometry(.006,.004,l*.6,6); tendao.rotateX(PI/2); tendao.scale(2.6,.6,1); tendao.translate(0,h/2*.7,-l*.55); partes.push(tendao);
  const fund=THREE.BufferGeometryUtils.mergeBufferGeometries(partes.map(q=>q.index?q.toNonIndexed():q)); fund.computeVertexNormals();
  COMPARTILHADAS.add(fund); return GEO[k]=fund; }
// costuras e tira do punho da luva: linhas escuras no dorso, uma tira com botão de latão
function geoCostura(){ if(GEO.costura) return GEO.costura; const L=[];
  [-.02,0,.02].forEach(x=>{ const b=new THREE.BoxGeometry(.0018,.0012,.05); b.translate(x,.0162,-.058); L.push(b); });
  const tira=new THREE.BoxGeometry(.062,.004,.012); tira.translate(0,.0175,-.006); L.push(tira);
  const g=THREE.BufferGeometryUtils.mergeBufferGeometries(L); COMPARTILHADAS.add(g); return GEO.costura=g; }
function geoTubo(){ if(GEO.tubo) return GEO.tubo; const g=new THREE.CylinderGeometry(.8,1,1,16,1,true); g.translate(0,.5,0); COMPARTILHADAS.add(g); return GEO.tubo=g; }   // afina para a ponta (punho)
// comprimentos (indicador → mínimo) das três falanges e o raio
const FAL=[[.036,.024,.019,.0093],[.04,.027,.02,.0096],[.037,.025,.019,.0091],[.029,.02,.016,.0082]];
// Mão com esqueleto: 16 ossos (pulso, três falanges por dedo e o polegar) e uma malha só, com cada vértice preso a um osso
// (pele rígida por falange). Uma chamada de desenho por mão, em vez de uma por falange.
function criarMao(mat,esq,opc){ opc=opc||{};
  const raiz=new THREE.Group(), geo=new THREE.Group(); raiz.add(geo); geo.scale.set(esq?-ESC_MAO:ESC_MAO,ESC_MAO,ESC_MAO);
  const pulso=new THREE.Bone(), ossos=[pulso], dedos=[], X=[-.03,-.01,.01,.029], ABRE=[-.07,-.02,.03,.1];
  const osso=(pai,x,y,z,ordem)=>{ const b=new THREE.Bone(); b.position.set(x,y,z); if(ordem) b.rotation.order=ordem; pai.add(b); ossos.push(b); return b; };
  for(let k=0;k<4;k++){ const F=FAL[k], segs=[]; let pai=pulso;
    for(let j=0;j<3;j++){ const b=j?osso(pai,0,0,-F[j-1]):osso(pai,X[k],.003,-.088+(k===3?.008:0),'YXZ'); segs.push(b); pai=b; }
    dedos.push({segs,abre:ABRE[k]}); }
  const PL=[.034,.027,.021], pb=osso(pulso,-.036,-.008,-.022,'YXZ'), pol=[pb]; pol.push(osso(pb,0,0,-PL[0])); pol.push(osso(pol[1],0,0,-PL[1]));
  // a malha é montada uma vez (é igual nas duas mãos: a esquerda é espelhada pelo grupo)
  if(!GEO.maoPele){ pulso.updateMatrixWorld(true); const idx=b=>ossos.indexOf(b), pele=[], unhas=[];
    const parte=(g,b,L)=>{ const q=(g.index?g.toNonIndexed():g.clone()); q.applyMatrix4(b.matrixWorld); const n=q.attributes.position.count, si=new Uint16Array(n*4), sw=new Float32Array(n*4);
      for(let v=0;v<n;v++){ si[v*4]=idx(b); sw[v*4]=1; } q.setAttribute('skinIndex',new THREE.Uint16BufferAttribute(si,4)); q.setAttribute('skinWeight',new THREE.Float32BufferAttribute(sw,4)); if(!q.attributes.uv) q.setAttribute('uv',new THREE.Float32BufferAttribute(new Float32Array(n*2),2)); L.push(q); };
    parte(geoPalma(.088,.032,.094),pulso,pele);
    dedos.forEach((D,k)=>{ D.segs.forEach((b,j)=>parte(capsula(FAL[k][3]*(1-j*.07),FAL[k][j]),b,pele));
      const u=new THREE.ConeGeometry(.0065,.022,6); u.rotateX(-PI/2); u.translate(0,.003,-.012-FAL[k][2]); parte(u,D.segs[2],unhas); });
    pol.forEach((b,j)=>parte(capsula(.0115*(1-j*.08),PL[j]),b,pele));
    const junta=L=>{ const g=THREE.BufferGeometryUtils.mergeBufferGeometries(L.map(q=>{ for(const a of Object.keys(q.attributes)) if(!['position','normal','uv','skinIndex','skinWeight'].includes(a)) q.deleteAttribute(a); return q; })); COMPARTILHADAS.add(g); return g; };
    GEO.maoPele=junta(pele); GEO.maoUnhas=junta(unhas); }
  mat.skinning=true; const pele=new THREE.SkinnedMesh(GEO.maoPele,mat); pele.add(pulso); geo.add(pele); raiz.updateMatrixWorld(true); const esq_=new THREE.Skeleton(ossos); pele.bind(esq_);
  if(opc.unhas){ opc.unhas.skinning=true; const u=new THREE.SkinnedMesh(GEO.maoUnhas,opc.unhas); geo.add(u); u.bind(esq_,pele.matrixWorld); }
  if(opc.costura){ geo.add(new THREE.Mesh(geoCostura(),opc.costura)); const bt=new THREE.Mesh(GEO.botao||(GEO.botao=(()=>{ const c=new THREE.CylinderGeometry(.0045,.0045,.003,10); c.translate(.024,.0205,-.006); COMPARTILHADAS.add(c); return c; })()),opc.botao); geo.add(bt); }
  if(opc.punho){ const c=new THREE.Mesh(new THREE.CylinderGeometry(.027,.023,.026,16,1,true),opc.punho); c.material.side=THREE.DoubleSide; c.rotation.x=PI/2; c.position.z=.004; geo.add(c); }   // boca do punho da luva (a manga cobre o resto)
  // ponto de pinça (entre polegar e indicador) e de apoio da palma: onde os objetos ficam presos na mão
  const pinca=new THREE.Object3D(); pinca.position.set((esq?1:-1)*.044,-.02,-.098).multiplyScalar(ESC_MAO); raiz.add(pinca);
  const dd=new Float32Array(ND); return {raiz,dedos,pol,pinca,esq,dd}; }
function posarDedos(M,dd,tremor){ for(let k=0;k<4;k++){ const D=M.dedos[k], c=cl(dd[k]+(tremor?tremor[k]:0),-.15,1.1);
    D.segs[0].rotation.set(-c*1.3,D.abre*(.3+dd[7]*1.6),0); D.segs[1].rotation.x=-c*1.55; D.segs[2].rotation.x=-c*1.05; }
  const a=dd[4], o=dd[5], d=dd[6]; const B=M.pol[0]; B.rotation.set(-.2-o*.75,.55+a*.75,-.5-o*.4); M.pol[1].rotation.x=-d*.75; M.pol[2].rotation.x=-d*.95; }
function criarBraco(matSup,matAnt,r){ const sup=new THREE.Mesh(geoTubo(),matSup), ant=new THREE.Mesh(geoTubo(),matAnt), cot=new THREE.Mesh(new THREE.SphereGeometry(1,12,10),matSup);
  return {sup,ant,cot,r}; }
const UP=new THREE.Vector3(0,1,0);
function tubo(m,P,Q,r1){ _v1.subVectors(Q,P); const L=_v1.length(); m.position.copy(P); m.quaternion.setFromUnitVectors(UP,_v1.divideScalar(L||1)); m.scale.set(r1,L,r1); }
// IK de dois ossos: ombro S, punho W, ossos a e b; o cotovelo fica no plano que contém o polo
function ik(S,W,a,b,polo,E){ _v2.subVectors(W,S); let L=_v2.length(); const dir=_v2.divideScalar(L||1); L=cl(L,Math.abs(a-b)+1e-4,a+b-1e-4);
  const cA=(a*a+L*L-b*b)/(2*a*L), sA=Math.sqrt(Math.max(0,1-cA*cA)); _v3.copy(polo).addScaledVector(dir,-polo.dot(dir)).normalize();
  E.copy(S).addScaledVector(dir,a*cA).addScaledVector(_v3,a*sA); return L; }

// ---------------------------------------------------------------------------------------------------------------------
// Peças: cartucho da carabina (chumbo), cartucho de prata do revólver, cápsulas vazias
const PECA={};
function matsPeca(){ if(PECA.latao) return; PECA.latao=matMetal(0xc89a52,.26,1); PECA.latao.userData.compartilhado=true;
  PECA.chumbo=new THREE.MeshStandardMaterial({color:0x6e6c6a,roughness:.55,metalness:.6}); PECA.chumbo.userData.compartilhado=true;
  PECA.prata=matMetal(0xe2e6ea,.16,1); PECA.prata.userData.compartilhado=true;
  PECA.escuro=matMetal(0x141416,.5,.8); PECA.escuro.userData.compartilhado=true;
  PECA.espoleta=matMetal(0xb0a090,.3,1); PECA.espoleta.userData.compartilhado=true; [PECA.latao,PECA.chumbo,PECA.prata,PECA.escuro,PECA.espoleta].forEach(luzDeLeitura); }
function geoCompart(k,f){ if(GEO[k]) return GEO[k]; const g=f(); COMPARTILHADAS.add(g); return GEO[k]=g; }
// cartucho deitado em z, ponta para -z, centro na origem. tipo: 'carabina' (.44 de chumbo), 'prata' (revólver)
function criarCartucho(tipo,vazio){ matsPeca(); const g=new THREE.Group(), car=tipo==='carabina';
  const R=car?.0074:.0064, L=car?.04:.034, B=car?.017:.013;
  const estojo=new THREE.Mesh(geoCompart('estojo'+tipo,()=>{ const c=new THREE.CylinderGeometry(R*.96,R,L,14); c.rotateX(PI/2); return c; }),PECA.latao); g.add(estojo);
  const aro=new THREE.Mesh(geoCompart('aro'+tipo,()=>{ const c=new THREE.CylinderGeometry(R*1.17,R*1.17,.0028,16); c.rotateX(PI/2); c.translate(0,0,L/2-.0014); return c; }),PECA.latao); g.add(aro);
  const esp=new THREE.Mesh(geoCompart('esp'+tipo,()=>{ const c=new THREE.CylinderGeometry(R*.36,R*.36,.001,10); c.rotateX(PI/2); c.translate(0,0,L/2+.0002); return c; }),PECA.espoleta); g.add(esp);
  if(!vazio){ const bala=new THREE.Mesh(geoCompart('bala'+tipo,()=>{ const pts=[]; for(let i=0;i<=8;i++){ const u=i/8; pts.push(new THREE.Vector2(R*.93*Math.sqrt(Math.max(0,1-u*u*.92))*(1-u*.08),u*B)); }
      const c=new THREE.LatheGeometry(pts,14); c.rotateX(-PI/2); c.translate(0,0,-L/2+.0005); return c; }),car?PECA.chumbo:PECA.prata); g.add(bala); }
  else { const boca=new THREE.Mesh(geoCompart('boca'+tipo,()=>{ const c=new THREE.CircleGeometry(R*.82,12); c.translate(0,0,0); c.rotateY(PI); c.translate(0,0,-L/2-.0004); return c; }),PECA.escuro); g.add(boca); }
  g.userData.L=L; g.userData.R=R; return g; }

// ---------------------------------------------------------------------------------------------------------------------
// Armas da primeira pessoa (as do terceiro-pessoa continuam em index.html): mesmas formas, com as peças que se mexem.
function carabinaFP(){ const A=armaCarabina(), aco=matMetal(0x3a3d43,.36), acoClaro=matMetal(0x6a6e75,.3);
  // portinhola de carga no lado direito, presa pela frente: o cartucho a empurra para dentro
  const port=new THREE.Group(); port.position.set(.0262,-.011,-.078); A.g.add(port);
  const placa=new THREE.Mesh(new THREE.BoxGeometry(.0035,.021,.031),acoClaro); placa.position.set(0,0,.0155); port.add(placa);
  const parafuso=new THREE.Mesh(new THREE.CylinderGeometry(.0026,.0026,.004,8),acoClaro); parafuso.rotation.z=PI/2; parafuso.position.set(.003,0,.003); port.add(parafuso);
  // ferrolho por cima da caixa: recua com a alavanca e mostra o cartucho na câmara
  const ferr=new THREE.Group(); ferr.position.set(0,.0612,.018); A.g.add(ferr);
  const fb=new THREE.Mesh(new THREE.BoxGeometry(.0125,.007,.05),acoClaro); ferr.add(fb);
  const fe=new THREE.Mesh(new THREE.CylinderGeometry(.003,.003,.006,8),acoClaro); fe.position.set(0,.004,.018); ferr.add(fe);
  const camara=criarCartucho('carabina'); camara.position.set(0,.047,-.038); camara.visible=false; A.g.add(camara);
  // a alça da alavanca precisa de um ponto de referência para os dedos (o pivô já existe em A.alav)
  return Object.assign(A,{tipo:'carabina',port,ferr,camara,
    // entrada da portinhola e direção de entrada do cartucho (ponta para dentro, levemente para o centro)
    entrada:V3(.0385,-.011,-.034), dirEntrada:V3(-Math.sin(.3),0,-Math.cos(.3)), rotEntrada:[0,.3,0]}); }
function revolverFP(){ matsPeca();
  const g=new THREE.Group(), prata=matMetal(0xc8ccd2,.22,1), escuro=matMetal(0x1c1d20,.45), mad=new THREE.MeshStandardMaterial({color:0x2e1a12,map:TEX.wood.map,normalMap:TEX.wood.normalMap,roughness:.45,emissive:0});
  prata.emissive=new THREE.Color(0x000000);
  const arm=lateral([[-.05,-.022],[.075,-.022],[.085,.042],[-.02,.05],[-.05,.03]],.03,.005,prata); g.add(arm);
  const cano=cilZ(.013,.014,.2,prata,20); cano.position.set(0,.03,-.17); g.add(cano);
  const nerv=new THREE.Mesh(new THREE.BoxGeometry(.012,.012,.2),prata); nerv.position.set(0,.045,-.17); g.add(nerv);
  const trava=new THREE.Mesh(new THREE.BoxGeometry(.004,.008,.012),escuro); trava.position.set(-.016,.024,.012); g.add(trava);   // botão da trava do tambor
  // guindaste: o tambor sai para a esquerda girando em volta de um eixo paralelo ao cano, abaixo dele
  const PIVO=V3(-.012,-.016,-.035), guin=new THREE.Group(); guin.position.copy(PIVO); g.add(guin);
  const braco=new THREE.Mesh(new THREE.BoxGeometry(.006,.03,.012),prata); braco.position.set(.004,.014,-.03); guin.add(braco);
  const tam=new THREE.Group(); tam.position.set(-PIVO.x,.014-PIVO.y,0); guin.add(tam);
  const cil=cilZ(.036,.036,.066,prata,32); tam.add(cil);
  for(let k=0;k<8;k++){ const a=k/8*TAU; const f=cilZ(.0055,.0055,.05,escuro,8); f.position.set(Math.cos(a)*.0345,Math.sin(a)*.0345,-.004); tam.add(f); }
  const furoGeo=new THREE.CircleGeometry(.0068,12);
  for(let k=0;k<8;k++){ const a=(k+.5)/8*TAU, fu=new THREE.Mesh(furoGeo,escuro); fu.position.set(Math.cos(a)*.022,Math.sin(a)*.022,-.0334); fu.rotation.y=PI; tam.add(fu);
    const tr=new THREE.Mesh(furoGeo,escuro); tr.position.set(Math.cos(a)*.022,Math.sin(a)*.022,.0336); tam.add(tr); }   // câmaras abertas na frente e atrás
  // extrator: vareta na frente, estrela atrás; as cápsulas das câmaras andam junto quando ele é empurrado
  const ext=new THREE.Group(); tam.add(ext);
  const vareta=cilZ(.0042,.0042,.056,prata,10); vareta.position.set(0,0,-.062); ext.add(vareta);
  const ponta=cilZ(.0062,.0062,.008,escuro,10); ponta.position.set(0,0,-.092); ext.add(ponta);
  const estrela=cilZ(.0152,.0152,.003,prata,16); estrela.position.set(0,0,.0335); ext.add(estrela);
  const camaras=[]; for(let k=0;k<8;k++){ const a=(k+.5)/8*TAU, cv=criarCartucho('prata'), cz=criarCartucho('prata',true);
    for(const c of [cv,cz]){ c.position.set(Math.cos(a)*.022,Math.sin(a)*.022,.0335-.017+.0016); ext.add(c); }
    camaras.push({vivo:cv,vazio:cz,a}); }
  const cabo=lateral([[-.05,.012],[.012,-.015],[.0,-.1],[-.012,-.14,-.05,-.135],[-.075,-.07],[-.07,-.005]],.034,.007,mad); g.add(cabo);
  const cruzMat=prata.clone(); cruzMat.emissive=new THREE.Color(0x000000); const cruz=[];
  for(const sx of [-1,1]){ const v=new THREE.Mesh(new THREE.BoxGeometry(.002,.05,.009),cruzMat); v.position.set(sx*.021,-.07,.04); g.add(v); const h=new THREE.Mesh(new THREE.BoxGeometry(.002,.009,.03),cruzMat); h.position.set(sx*.021,-.058,.04); g.add(h); cruz.push(v,h); }
  const guarda=new THREE.Mesh(new THREE.TorusGeometry(.025,.005,8,20,PI*1.2),prata); guarda.rotation.y=PI/2; guarda.rotation.x=PI*.6; guarda.position.set(0,-.036,0); g.add(guarda);
  const gat=new THREE.Mesh(new THREE.BoxGeometry(.005,.022,.006),escuro); gat.position.set(0,-.032,.006); gat.rotation.x=.25; g.add(gat);
  const cao=new THREE.Mesh(new THREE.BoxGeometry(.01,.03,.016),escuro); cao.position.set(0,.058,.035); cao.rotation.x=-.6; g.add(cao);
  const mira=new THREE.Mesh(new THREE.BoxGeometry(.004,.014,.01),prata); mira.position.set(0,.05,-.265); g.add(mira);
  // municiador de oito: disco escuro, botão atrás e os oito cartuchos de prata à frente
  const mun=new THREE.Group(), disco=cilZ(.03,.03,.009,escuro,24); disco.position.z=.0; mun.add(disco);
  const botao=cilZ(.009,.0075,.016,escuro,12); botao.position.z=.012; mun.add(botao);
  const munC=[]; for(let k=0;k<8;k++){ const a=(k+.5)/8*TAU, c=criarCartucho('prata'); c.position.set(Math.cos(a)*.022,Math.sin(a)*.022,-.004-.017); mun.add(c); munC.push(c); }
  mun.visible=false;
  return {tipo:'revolver',g,boca:V3(0,.03,-.28),tambor:tam,guin,PIVO,ext,camaras,cao,cruz,cruzMat,prata,mun,munC,trava}; }
function cetroFP(cor){ const C=cetroOsso(cor);
  // as vértebras ganham material próprio para acender em sequência (sacar, lançar, canalizar)
  const vert=[]; C.g.children.forEach(o=>{ if(o.isMesh&&o.geometry&&o.geometry.type==='TorusGeometry'){ o.material=o.material.clone(); o.material.emissive=new THREE.Color(cor); o.material.emissiveIntensity=0; vert.push(o); } });
  vert.sort((a,b)=>a.position.y-b.position.y); return Object.assign(C,{tipo:'cetro',vert}); }

// Luz de leitura: a arma na mão é a estrela, mas a noite é escura. Em vez de mais uma luz na cena (que pesaria em todo
// material do mundo), os materiais da primeira pessoa ganham uma luz própria vinda de cima à esquerda (no espaço da câmera)
// e um contorno de luz fria. Só eles pagam por isso.
const LEITURA={uL:{value:.2},uRim:{value:.16}};
function luzDeLeitura(mat){ const L=Array.isArray(mat)?mat:[mat];
  for(const m of L){ if(!m||m.userData.leitura||!(m.isMeshStandardMaterial)) continue; m.userData.leitura=true;
    m.onBeforeCompile=sh=>{ sh.uniforms.uLeitura=LEITURA.uL; sh.uniforms.uRim=LEITURA.uRim;
      sh.fragmentShader=sh.fragmentShader.replace('void main() {','uniform float uLeitura; uniform float uRim;\nvoid main() {')
        .replace('#include <output_fragment>','{ vec3 nL=normalize(normal); float k=max(dot(nL,normalize(vec3(-.45,.7,.55))),0.); float r=pow(1.-max(dot(nL,vec3(0.,0.,1.)),0.),3.);\n  outgoingLight+=diffuseColor.rgb*uLeitura*k+vec3(.55,.62,.8)*uRim*r*(1.-roughnessFactor*.6); }\n#include <output_fragment>'); };
    m.customProgramCacheKey=()=>'leitura'; m.needsUpdate=true; } }

// ---------------------------------------------------------------------------------------------------------------------
// Pose: o resultado de um clipe num instante. Mãos guardam o quadro (f) e a transformação local nele.
function novaPose(){ return {a:{p:new THREE.Vector3(),q:new THREE.Quaternion()},
  D:{f:'arma',p:new THREE.Vector3(),q:new THREE.Quaternion(),dd:new Float32Array(ND)},
  E:{f:'arma',p:new THREE.Vector3(),q:new THREE.Quaternion(),dd:new Float32Array(ND)},
  c:{},cam:new THREE.Vector3(),pr:{}}; }
function copiarPose(o,s){ o.a.p.copy(s.a.p); o.a.q.copy(s.a.q); for(const h of ['D','E']){ o[h].f=s[h].f; o[h].p.copy(s[h].p); o[h].q.copy(s[h].q); o[h].dd.set(s[h].dd); }
  for(const k in o.c) delete o.c[k]; Object.assign(o.c,s.c); o.cam.copy(s.cam); for(const k in o.pr) delete o.pr[k]; for(const k in s.pr) o.pr[k]=Object.assign({},s.pr[k]); return o; }

// Rig: a arma e as mãos de um lado (caça ou culto), com os quadros de referência das peças
function quadroArma(R,pose,out){ return out.compose(pose.a.p,pose.a.q,UM).multiply(R.arma.base); }
function quadro(R,pose,nome,out){
  if(nome==='corpo') return out.identity();
  quadroArma(R,pose,out); if(nome==='arma') return out;
  const W=R.arma, c=pose.c;
  if(nome==='alavanca'){ const P=W.alav.position; _q1.setFromAxisAngle(_v4.set(1,0,0),(c.alav||0)*.95*.82); return out.multiply(_m3.makeTranslation(P.x,P.y,P.z)).multiply(_m2.makeRotationFromQuaternion(_q1)).multiply(_m3.makeTranslation(-P.x,-P.y,-P.z)); }
  if(nome==='guindaste'||nome==='tambor'){ const P=W.PIVO; out.multiply(_m3.makeTranslation(P.x,P.y,P.z)).multiply(_m2.makeRotationZ((c.guin||0)*1.42));
    if(nome==='guindaste') return out; const T=W.tambor.position; return out.multiply(_m3.makeTranslation(T.x,T.y,T.z)); }
  return out; }
// mão no corpo: aplica o quadro na transformação local
function maoNoCorpo(R,pose,H,P,Q){ if(H.f==='corpo'){ P.copy(H.p); Q.copy(H.q); return; }
  quadro(R,pose,H.f,_m1); _m2.compose(H.p,H.q,UM); _m1.multiply(_m2); _m1.decompose(P,Q,_s1); }

// ---------------------------------------------------------------------------------------------------------------------
// Clipes. Uma chave de mão: [t, quadro, posição da palma, dedos-dir, dorso-dir, dedos (nome ou lista), curva, {reta}]
// Chave da arma: [t, posição, rotação XYZ, curva]. Peças: c:{alav:[[t,v,curva]...]}. Objetos: pr:{nome:[[t,{m,s},curva]]}.
function prepararClipe(C){
  const H=k=>(C[k]||[]).map(([t,f,p,d,o,dd,e,x])=>({t,f,p:v3(p),q:d instanceof THREE.Quaternion?d.clone():qMao(d,o),dd:Float32Array.from(typeof dd==='string'?DEDOS[dd]:dd),e:e||'s',reta:!!(x&&x.reta)}));
  return {nome:C.nome,dur:C.dur,laco:!!C.laco,
    a:(C.a||[]).map(([t,p,r,e])=>({t,p:v3(p),q:qE(r),e:e||'s'})),
    D:H('D'),E:H('E'),
    c:Object.fromEntries(Object.entries(C.c||{}).map(([k,L])=>[k,L.map(([t,v,e])=>({t,v,e:e||'s'}))])),
    cam:(C.cam||[]).map(([t,p,e])=>({t,p:v3(p),e:e||'s'})),
    pr:Object.fromEntries(Object.entries(C.pr||{}).map(([k,L])=>[k,L.map(([t,v,e])=>({t,v,e:e||'s'}))])),
    ev:(C.ev||[]).slice().sort((a,b)=>a[0]-b[0])}; }
function achar(L,t){ let i=0; while(i<L.length-1&&L[i+1].t<=t) i++; return i; }
function trecho(L,t){ if(t<=L[0].t) return [0,0,0]; if(t>=L[L.length-1].t) return [L.length-1,L.length-1,0]; const i=achar(L,t), j=i+1, k=L[j];
  return [i,j,CURVA[k.e]((t-L[i].t)/((k.t-L[i].t)||1))]; }
// Catmull-Rom entre as chaves (arcos naturais), com o tempo já curvado
function cr(p0,p1,p2,p3,u,out){ const u2=u*u,u3=u2*u;
  return out.set(.5*(2*p1.x+(-p0.x+p2.x)*u+(2*p0.x-5*p1.x+4*p2.x-p3.x)*u2+(-p0.x+3*p1.x-3*p2.x+p3.x)*u3),
    .5*(2*p1.y+(-p0.y+p2.y)*u+(2*p0.y-5*p1.y+4*p2.y-p3.y)*u2+(-p0.y+3*p1.y-3*p2.y+p3.y)*u3),
    .5*(2*p1.z+(-p0.z+p2.z)*u+(2*p0.z-5*p1.z+4*p2.z-p3.z)*u2+(-p0.z+3*p1.z-3*p2.z+p3.z)*u3)); }
const _hA={f:'',p:new THREE.Vector3(),q:new THREE.Quaternion()}, _P0=new THREE.Vector3(), _P3=new THREE.Vector3(), _Q=new THREE.Quaternion(), _PA=new THREE.Vector3(), _PB=new THREE.Vector3(), _QA=new THREE.Quaternion(), _QB=new THREE.Quaternion();
function amostrar(R,C,t,base,out){
  copiarPose(out,base);
  if(C.a.length){ const [i,j,u]=trecho(C.a,t), L=C.a; if(i===j){ out.a.p.copy(L[i].p); out.a.q.copy(L[i].q); }
    else { cr(L[Math.max(0,i-1)].p,L[i].p,L[j].p,L[Math.min(L.length-1,j+1)].p,u,out.a.p); out.a.q.copy(L[i].q).slerp(L[j].q,u); } }
  for(const k in C.c){ const L=C.c[k], [i,j,u]=trecho(L,t); out.c[k]=mix(L[i].v,L[j].v,u); }
  for(const k in C.pr){ const L=C.pr[k], [i,j,u]=trecho(L,t), A=L[i].v, B=L[j].v; out.pr[k]={m:u>=1?B.m:A.m,s:mix(A.s||0,B.s||0,u)}; }
  if(C.cam.length){ const [i,j,u]=trecho(C.cam,t); out.cam.copy(C.cam[i].p).lerp(C.cam[j].p,u); }
  for(const h of ['D','E']){ const L=C[h]; if(!L.length) continue; const [i,j,u]=trecho(L,t), A=L[i], B=L[j], H=out[h];
    for(let n=0;n<ND;n++) H.dd[n]=mix(A.dd[n],B.dd[n],u);
    if(i===j||A.f===B.f){ H.f=A.f; if(i===j){ H.p.copy(A.p); H.q.copy(A.q); continue; }
      if(B.reta) H.p.copy(A.p).lerp(B.p,u);
      else { const p0=i>0&&L[i-1].f===A.f?L[i-1].p:A.p, p3=j<L.length-1&&L[j+1].f===A.f?L[j+1].p:B.p; cr(p0,p1(A),p2(B),p3,u,H.p); }
      H.q.copy(A.q).slerp(B.q,u); continue; }
    // quadros diferentes: as duas chaves no corpo, com as peças deste instante; a mão viaja solta
    maoNoCorpo(R,out,A,_PA,_QA); maoNoCorpo(R,out,B,_PB,_QB); H.f='corpo'; H.p.copy(_PA).lerp(_PB,u); H.q.copy(_QA).slerp(_QB,u); }
  return out; }
const p1=k=>k.p, p2=k=>k.p;
// valor de uma peça quando o clipe não diz nada (vértebras acesas, brasa viva, cão armado)
const PADRAO_C={acende:1,brasa:1,cao:1};
// orientação escrita no corpo ("dedos para a esquerda, dorso para cima") convertida para o quadro de uma peça (o tambor)
// no instante t de um clipe: a mão fica presa à peça sem que o braço passe na frente do que ela faz
function qNoQuadro(R,Cparcial,t,nome,dedos,dorso){ const base=amostrar(R,S.clipes[R.nome+'_pronta'],0,novaPose(),novaPose()), pose=amostrar(R,Cparcial,t,base,novaPose());
  quadro(R,pose,nome,_m1); const qF=new THREE.Quaternion().setFromRotationMatrix(_m1.extractRotation(_m1.clone())); return qF.invert().multiply(qMao(dedos,dorso)); }
// mistura de duas poses (troca de estado). Mãos no mesmo quadro misturam por dentro dele; senão, no corpo.
function misturar(R,A,B,w,out){ const o=out; o.a.p.copy(A.a.p).lerp(B.a.p,w); o.a.q.copy(A.a.q).slerp(B.a.q,w);
  for(const k in o.c) delete o.c[k]; for(const k in A.c) o.c[k]=0; for(const k in B.c) o.c[k]=0;
  for(const k in o.c){ const pa=k in A.c?A.c[k]:PADRAO_C[k]||0, pb=k in B.c?B.c[k]:PADRAO_C[k]||0; o.c[k]=mix(pa,pb,w); }
  o.cam.copy(A.cam).lerp(B.cam,w); for(const k in o.pr) delete o.pr[k]; for(const k in (w<.5?A.pr:B.pr)) o.pr[k]=Object.assign({},(w<.5?A.pr:B.pr)[k]);
  for(const h of ['D','E']){ const a=A[h], b=B[h], H=o[h]; for(let n=0;n<ND;n++) H.dd[n]=mix(a.dd[n],b.dd[n],w);
    if(a.f===b.f){ H.f=a.f; H.p.copy(a.p).lerp(b.p,w); H.q.copy(a.q).slerp(b.q,w); }
    else { maoNoCorpo(R,A,a,_PA,_QA); maoNoCorpo(R,B,b,_PB,_QB); H.f='corpo'; H.p.copy(_PA).lerp(_PB,w); H.q.copy(_QA).slerp(_QB,w); } }
  return o; }

// ---------------------------------------------------------------------------------------------------------------------
// Estado do sistema
const S={ok:false,rigs:{},arma:null,R:null,est:{nome:'',C:null,t:0,dur:0,fim:null},de:null,fade:0,fadeDur:0,saida:null,base:null,
  visivel:false,tUlt:0,parado:0,calmo:0,proxAjuste:8,proxInspecao:22,pedidoInspecao:false,jolt:new THREE.Vector3(),joltV:new THREE.Vector3(),
  cam:new THREE.Vector3(),forca:null,velocidade:1,clipes:{},recarga:null,ammoAnt:8,tiros:0,ultTiro:-9,camaraCheia:true,
  runasVisiveis:0,runaNasce:[],trilha:null,fio:null};
const _pose=novaPose(), _de=novaPose(), _out=novaPose(), _tmp=novaPose();

function construir(){ if(S.ok) return; matsPeca();
  const luva=matCouro(0x2a1c14), manga=matPano(0x3b342d), mangaSup=matPano(0x312b25), punho=matCouro(0x3a2618);   // casaco de oleado gasto, luva de couro
  const peleC=new THREE.MeshStandardMaterial({color:0x5a4650,roughness:.62,metalness:0}), mangaC=matPano(0x581320), unha=new THREE.MeshStandardMaterial({color:0x1a1014,roughness:.3,metalness:.2});
  // ---- caça: corpo (vmH, já na câmera) → raiz da arma → carabina/revólver; mãos e braços no corpo
  vmH=new THREE.Group(); camera.add(vmH); vmH.position.set(.2,-.17,-.42); vmH.scale.setScalar(.72);
  const raizH=new THREE.Group(); vmH.add(raizH);
  vmCarabina=carabinaFP(); raizH.add(vmCarabina.g); vmCarabina.base=new THREE.Matrix4();
  vmRevolver=revolverFP(); raizH.add(vmRevolver.g); vmRevolver.g.position.set(.025,.045,-.13); vmRevolver.g.updateMatrix(); vmRevolver.base=vmRevolver.g.matrix.clone();
  vmFlash=new THREE.Sprite(new THREE.SpriteMaterial({map:RT.flame,color:0xffd08a,blending:THREE.AdditiveBlending,depthWrite:false,transparent:true,opacity:0})); vmFlash.scale.set(.3,.3,1); raizH.add(vmFlash);
  const costura=matCouro(0x120a06), botao=matMetal(0xb58c48,.3,1), oh={punho,costura,botao};
  const maosH={D:criarMao(luva,false,oh),E:criarMao(luva,true,oh)}, bracosH={D:criarBraco(mangaSup,manga,.05),E:criarBraco(mangaSup,manga,.05)};
  for(const k of ['D','E']){ vmH.add(maosH[k].raiz); const B=bracosH[k]; vmH.add(B.sup,B.ant,B.cot); }
  const rigH={corpo:vmH,raiz:raizH,maos:maosH,bracos:bracosH,ombro:{D:V3(.02,-.2,.5),E:V3(-.56,-.2,.5)},polo:{D:V3(.7,-1,.2),E:V3(-.8,-1,.1)},osso:{D:[.44,.42],E:[.5,.49]},
    braco:{D:[.054,.044],E:[.054,.044]}};
  // ---- culto: corpo (vmC) → raiz → cetro; mão direita no cetro, esquerda livre
  vmC=new THREE.Group(); camera.add(vmC); vmC.position.set(.26,-.12,-.5); vmC.scale.setScalar(.6);
  const raizC=new THREE.Group(); vmC.add(raizC);
  vmCetro=cetroFP(0xb050ff); raizC.add(vmCetro.g); vmCetro.g.position.set(0,-.95,0); vmCetro.g.updateMatrix(); vmCetro.base=vmCetro.g.matrix.clone();
  vmOrb=vmCetro.halo; vmRunas=[];
  for(let k=0;k<7;k++){ const r=new THREE.Sprite(new THREE.SpriteMaterial({map:RT.rune,color:0xe0a0ff,blending:THREE.AdditiveBlending,depthWrite:false,transparent:true,opacity:.9})); r.scale.set(.035,.035,1); vmCetro.cab.add(r); vmRunas.push(r); }
  const maosC={D:criarMao(peleC,false,{unhas:unha}),E:criarMao(peleC,true,{unhas:unha})}, bracosC={D:criarBraco(mangaC,mangaC,.07),E:criarBraco(mangaC,mangaC,.07)};
  for(const k of ['D','E']){ vmC.add(maosC[k].raiz); const B=bracosC[k]; vmC.add(B.sup,B.ant,B.cot); }
  const rigC={corpo:vmC,raiz:raizC,maos:maosC,bracos:bracosC,ombro:{D:V3(.32,-.95,.55),E:V3(-.75,-.9,.45)},polo:{D:V3(1,-.8,.2),E:V3(-1,-.8,.2)},osso:{D:[.62,.58],E:[.62,.58]},
    braco:{D:[.085,.066],E:[.085,.066]}};
  S.rigs={carabina:Object.assign({arma:vmCarabina},rigH),revolver:Object.assign({arma:vmRevolver},rigH),cetro:Object.assign({arma:vmCetro},rigC)};
  for(const k in S.rigs) S.rigs[k].nome=k;
  for(const o of [vmH,vmC]) o.traverse(x=>{ if(x.isMesh){ x.castShadow=false; x.receiveShadow=false; x.frustumCulled=false; luzDeLeitura(x.material); } });
  montarClipes(); S.ok=true; }

// ---------------------------------------------------------------------------------------------------------------------
// Poses e clipes. Unidades: as do corpo da arma (1 = 0,72 m na carabina e no revólver; 0,6 m no cetro).
// Carabina: caixa da culatra na origem, cano para -z. O pulso da coronha fica em z≈+.09, o fuste de -.1 a -.4.
const P={};   // poses de referência (mãos) usadas por vários clipes
function montarPoses(){
  const W=vmCarabina, A=W.alav.position;
  // mão direita no pulso da coronha, dedos pela alça da alavanca (quadro da alavanca: desce junto quando ela abre)
  P.cD=['alavanca',[.05,-.012,.1],[-.2,-.72,-.66],[1,.18,-.15],'coronha'];
  P.cE=['arma',[-.016,-.05,-.235],[1,.18,-.28],[-.35,-1,.05],'fuste'];
  // revólver (coordenadas do revólver: armação na origem; a raiz o põe em -.03,.075,-.13)
  P.rD=['arma',[.034,-.072,.044],[-.35,-.25,-.9],[.95,.15,.2],'revolver'];
  P.rE=['arma',[-.038,-.084,.03],[.35,-.35,-.85],[-.95,0,.3],'suporte'];
  // cetro: mão direita fechada na haste (o cetro está em y -.95 dentro da raiz; a haste corre em y)
  P.xD=['arma',[.012,.6,.03],[-1,0,.2],[0,.15,1],'cetro'];
  P.xE=['corpo',[-.62,-.85,.1],[.3,1,-.6],[-.4,.2,1],'relaxada'];
}
const chave=(t,H,ext)=>{ const o=ext||{}; return [t,o.f||H[0],o.p||H[1],o.d||H[2],o.c||H[3],o.dd||H[4],o.e||'s',o.reta?{reta:1}:null]; };
function montarClipes(){ montarPoses(); const C=S.clipes;
  // ---------- prontas (pose parada; a vida vem das camadas) ----------
  C.carabina_pronta=prepararClipe({nome:'pronta',dur:1,laco:true,a:[[0,[0,0,0],[0,0,0]]],D:[chave(0,P.cD)],E:[chave(0,P.cE)],c:{alav:[[0,0]],port:[[0,0]]}});
  C.revolver_pronta=prepararClipe({nome:'pronta',dur:1,laco:true,a:[[0,[0,0,0],[0,0,0]]],D:[chave(0,P.rD)],E:[chave(0,P.rE)],c:{guin:[[0,0]],ext:[[0,0]]}});
  C.cetro_pronta=prepararClipe({nome:'pronta',dur:1,laco:true,a:[[0,[0,0,0],[-.3,0,.18]]],D:[chave(0,P.xD)],E:[chave(0,P.xE)]});
  montarClipesCarabina(C); montarClipesRevolver(C); montarClipesCetro(C); }

// ---------------------------------------------------------------------------------------------------------------------
// CARABINA DE ALAVANCA — a recarga é o marco de qualidade.
// Sequência (técnica real da portinhola lateral): a arma rola para a esquerda e mostra o lado direito; a mão direita
// larga o pulso, busca um cartucho no cinto, encosta a ponta na portinhola e o polegar empurra: o cartucho entra três
// quartos e fica com o aro para fora, e o próximo é quem o empurra para dentro do tubo; o último é assentado pelo
// polegar. Na vazia, a arma volta e a alavanca é acionada para levar um cartucho à câmara (ferrolho recua e avança).
// Na tática (ainda há cartucho na câmara), entram mais cartuchos e não há alavanca. Duração: a do servidor (1,6 s).
function poseCartuchoNaMao(R){ return R.maos.D.pinca; }
// posição da palma da mão direita para que o ponto de pinça fique exatamente onde o cartucho deve estar
function maoPorPinca(pPinca,qMaoQ){ const M=S.rigs.carabina.maos.D, off=M.pinca.position.clone().sub(PALMA_CENTRO).applyQuaternion(qMaoQ); return [pPinca.x-off.x,pPinca.y-off.y,pPinca.z-off.z]; }
function montarClipesCarabina(C){
  const W=vmCarabina, E0=W.entrada, dir=W.dirEntrada;
  // a mão que carrega: dedos para a frente e para dentro, dorso para fora (direita) e um pouco para cima; o polegar fica
  // atrás do aro, pronto para empurrar
  const dPush=[dir.x-.05,.35,dir.z], oPush=[.85,.6,.05];
  const qPush=qMao(dPush,oPush);
  qPinca.copy(qPush).invert().multiply(qE(W.rotEntrada));   // o cartucho na mão já sai na orientação da entrada: nada pula na troca
  const naEntrada=(s)=>{ const p=E0.clone().addScaledVector(dir,s*.065); return maoPorPinca(p,qPush); };
  // pegar cartucho no cinto (quadro do corpo: embaixo à direita, fora da tela)
  const cinto=[.16,-.4,.24], cintoD=[-.2,-.6,-.6], cintoO=[.9,.1,.3];
  const subindo=[.08,-.17,.12], subD=[-.3,.2,-.9], subO=[.9,.4,.2];
  C.carabina_recarga=(vazia,n)=>{
    const T=1.6, k={}; const a=[], D=[], E=[], ev=[], pr={c0:[[0,{m:'oculto',s:0},'h']],c1:[[0,{m:'oculto',s:0},'h']]}, c={alav:[[0,0]],port:[[0,0]]}, cam=[[0,[0,0,0]]];
    // pose de carga: arma rolada (lado direito para cima), mais perto, um pouco para o centro, cano levemente alto
    const POS=[-.14,.0,.12], ROT=[.22,.82,1.0];
    const tA=.2;                                   // fim da antecipação
    const ciclo=.29, fimC=tA+n*ciclo;
    const tVolta=vazia?Math.max(fimC,1.06):Math.max(fimC,1.12);
    a.push([0,[0,0,0],[0,0,0]],[.07,[.012,.008,-.006],[-.04,-.03,-.06],'o'],[tA,POS,ROT,'w'],[tA+.08,[POS[0],POS[1]-.006,POS[2]],[ROT[0]-.02,ROT[1],ROT[2]+.03],'o']);
    ev.push([.0,'som','roupa',.5],[.02,'cam',[-.006,0,.004]]);
    // mão esquerda segura o fuste o tempo todo, desliza um pouco para trás para apoiar perto da caixa
    E.push(chave(0,P.cE),chave(tA,P.cE,{p:[-.018,-.05,-.17],dd:'suporte'}));
    // mão direita: larga o pulso, vai ao cinto
    D.push(chave(0,P.cD),chave(.07,P.cD,{p:[.03,-.002,.094],dd:'relaxada',e:'i'}));
    for(let i=0;i<n;i++){ const t0=tA+i*ciclo-.1, tPega=t0+.045, tAlinha=t0+.17, tEmp=tAlinha+.07, tSai=tEmp+.05, nome='c'+(i%2), ant='c'+((i+1)%2), ultimo=i===n-1;
      // a mão desce até o cinto, pega o cartucho (pinça), sobe e alinha a ponta com a portinhola
      D.push(chave(Math.max(.09,t0),['corpo',cinto,cintoD,cintoO,'aberta'],{e:i?'s':'i'}));
      D.push(chave(tPega,['corpo',[cinto[0]-.01,cinto[1]+.02,cinto[2]-.01],cintoD,cintoO,'pinca'],{e:'o'}));
      D.push(chave(tPega+.06,['corpo',subindo,subD,subO,'pinca'],{e:'s'}));
      D.push(chave(tAlinha-.025,['arma',naEntrada(-.35),dPush,oPush,'pinca'],{e:'o'}));
      D.push(chave(tAlinha,['arma',naEntrada(0),dPush,oPush,'pinca'],{e:'s',reta:1}));
      // o polegar empurra: o cartucho entra 3/4 (o anterior é empurrado de 3/4 até o fim e some no tubo)
      D.push(chave(tEmp,['arma',naEntrada(ultimo?.98:.75),dPush,oPush,ultimo?'empurra':'pinca'],{e:'i',reta:1}));
      D.push(chave(tSai,['arma',naEntrada(ultimo?.7:.42),dPush,oPush,'relaxada'],{e:'o',reta:1}));
      pr[nome].push([tPega,{m:'mao',s:0},'h'],[tAlinha,{m:'arma',s:0},'h'],[tEmp,{m:'arma',s:ultimo?1:.75},'i'],[ultimo?tEmp+.001:tEmp+ciclo+.07,{m:ultimo?'oculto':'arma',s:ultimo?1:.75},'h']);
      if(i>0) pr[ant].push([tAlinha,{m:'arma',s:.75},'h'],[tEmp,{m:'arma',s:1.02},'i'],[tEmp+.001,{m:'oculto',s:0},'h']);
      c.port.push([tAlinha,0],[tAlinha+.035,1,'q'],[tEmp,1],[tEmp+.04,ultimo?0:.62,'o']);
      ev.push([tAlinha+.02,'som',ultimo?'cartucho_fim':'cartucho',.75],[tEmp,'cam',[-.004,0,ultimo?-.004:.002]]);
      if(i===0) E.push(chave(tAlinha,P.cE,{p:[-.018,-.05,-.17],dd:[.72,.76,.8,.84,.4,.6,.4,.02]}));   // a mão de apoio aperta quando o polegar empurra
    }
    // volta: a arma desrola; a mão direita volta para o pulso / alavanca
    const tR=tVolta;
    a.push([tR,POS,ROT],[tR+.14,[.006,-.012,.012],[-.05,.02,-.06],'o']);
    D.push(chave(tR,['corpo',[.18,-.15,.2],[-.3,-.6,-.7],[1,.2,.2],'aberta'],{e:'s'}),chave(tR+.13,P.cD,{e:'o'}));
    ev.push([tR+.12,'som','empunhar',.5]);
    E.push(chave(tR,P.cE,{p:[-.018,-.05,-.17],dd:'suporte'}),chave(tR+.16,P.cE,{e:'s'}));
    if(vazia){ // alavanca: abre rápido (o ferrolho recua), segura um instante, fecha com força (o cartucho vai à câmara)
      const tL=Math.min(tR+.16,1.2);
      c.alav.push([tL,0],[tL+.075,1,'q'],[tL+.105,1],[tL+.19,0,'i']);
      a.push([tL+.07,[.008,-.03,.02],[-.1,.03,-.04],'o'],[tL+.19,[.004,-.012,.008],[.03,.0,.01],'i'],[tL+.24,[0,-.002,.002],[-.04,0,0],'q']);
      // o carregador levanta o cartucho do tubo quando a alavanca abre (ele aparece na abertura) e o fecho o leva à câmara
      ev.push([tL+.02,'som','alavanca',.55],[tL+.05,'camara',1],[tL+.19,'cam',[-.012,.004,-.006]]);
      a.push([T,[0,0,0],[0,0,0],'b']); }
    else { // tática: ainda há cartucho na câmara; a mão volta, firma e a arma assenta (sem alavanca)
      a.push([tR+.24,[.002,-.01,.004],[-.035,0,.02],'s'],[tR+.32,[0,-.004,0],[.01,0,0],'o'],[T,[0,0,0],[0,0,0],'b']);
      ev.push([tR+.25,'cam',[-.004,0,.002]]); }
    D.push(chave(T,P.cD)); E.push(chave(T,P.cE)); c.alav.push([T,0]); c.port.push([T,0]);
    cam.push([T,[0,0,0]]);
    return prepararClipe({nome:'recarga',dur:T,a,D,E,c,pr,cam,ev}); };
  // ---------- sacar: sobe de baixo, a mão bate no fuste e a alavanca é acionada no caminho ----------
  C.carabina_sacar=prepararClipe({nome:'sacar',dur:.72,
    a:[[0,[.06,-.42,.2],[-1.0,.35,.7]],[.26,[.0,-.02,.0],[.1,.06,.05],'o'],[.34,[0,0,0],[0,0,0],'s'],[.44,[.004,-.026,.012],[-.08,.02,-.03],'o'],[.56,[.002,-.006,.004],[.02,0,0],'i'],[.72,[0,0,0],[0,0,0],'b']],
    D:[chave(0,P.cD),chave(.72,P.cD)],
    E:[chave(0,['corpo',[-.5,-.6,.1],[.6,.4,-.6],[-.4,-.9,0],'aberta']),chave(.2,P.cE,{p:[-.016,-.06,-.25],dd:'relaxada',e:'o'}),chave(.27,P.cE,{e:'q'}),chave(.72,P.cE)],
    c:{alav:[[0,0],[.36,0],[.43,1,'q'],[.46,1],[.55,0,'i'],[.72,0]]},
    ev:[[0,'som','roupa',.55],[.26,'som','empunhar',.6],[.37,'som','alavanca',.55],[.55,'cam',[-.01,.003,-.004]],[.55,'camara',1]]});
  // ---------- inspeção: mostra o lado direito, confere a câmara pela alavanca, passa o polegar no latão ----------
  C.carabina_inspecao=prepararClipe({nome:'inspecao',dur:3.2,
    a:[[0,[0,0,0],[0,0,0]],[.55,[-.07,.03,.12],[.12,.55,.9],'w'],[1.0,[-.07,.034,.12],[.15,.58,.95],'s'],
       [1.25,[-.07,.03,.12],[.1,.56,.92]],[1.85,[-.07,.032,.12],[.12,.57,.93]],
       [2.25,[-.04,.0,.1],[-.15,-.25,-.5],'w'],[2.75,[-.04,.004,.1],[-.12,-.28,-.52],'s'],[3.2,[0,0,0],[0,0,0],'w']],
    D:[chave(0,P.cD),chave(3.2,P.cD)],
    E:[chave(0,P.cE),chave(.55,P.cE,{p:[-.018,-.05,-.17]}),chave(2.0,P.cE,{p:[-.018,-.05,-.17]}),chave(2.3,P.cE,{p:[-.02,-.035,-.13],d:[.6,.6,-.5],c:[-.6,-.6,.4],dd:'relaxada'}),
       chave(2.55,P.cE,{p:[-.022,-.03,-.09],d:[.6,.6,-.5],c:[-.6,-.6,.4],dd:'relaxada'}),chave(2.8,P.cE,{p:[-.02,-.035,-.13],d:[.6,.6,-.5],c:[-.6,-.6,.4],dd:'relaxada'}),chave(3.2,P.cE)],
    c:{alav:[[0,0],[1.05,0],[1.25,.42,'o'],[1.75,.42],[1.9,0,'i'],[3.2,0]]},
    ev:[[.05,'som','roupa',.45],[1.06,'som','alavanca',.32],[1.88,'som','empunhar',.35],[2.2,'som','roupa',.35],[3.05,'som','empunhar',.4]]});
  // ---------- ajuste de pegada (ocioso): a mão do fuste afrouxa, escorrega e firma; a direita aperta ----------
  C.carabina_ajuste=prepararClipe({nome:'ajuste',dur:1.1,
    a:[[0,[0,0,0],[0,0,0]],[.35,[.002,-.006,0],[.0,.0,-.035]],[.7,[-.001,.002,0],[.0,.0,.012],'o'],[1.1,[0,0,0],[0,0,0]]],
    D:[chave(0,P.cD),chave(.5,P.cD,{dd:[.3,.6,.7,.75,.4,.6,.4,.05]}),chave(.68,P.cD,{e:'o'}),chave(1.1,P.cD)],
    E:[chave(0,P.cE),chave(.25,P.cE,{dd:'relaxada'}),chave(.42,P.cE,{p:[-.016,-.056,-.27],dd:'relaxada'}),chave(.58,P.cE,{p:[-.016,-.05,-.25],e:'q'}),chave(1.1,P.cE)],
    ev:[[.56,'som','empunhar',.25]]});
  // ---------- corrida: arma atravessada no peito, cano para cima e para a esquerda ----------
  C.carabina_corrida=prepararClipe({nome:'corrida',dur:1,laco:true,a:[[0,[-.1,-.07,.12],[.32,.95,.62]]],D:[chave(0,P.cD,{dd:'punho'})],E:[chave(0,P.cE,{dd:'punho'})]});
  // ---------- selar: arma baixa na direita, mão esquerda estendida sobre o círculo, palma para baixo ----------
  C.carabina_selar=prepararClipe({nome:'selar',dur:1,laco:true,a:[[0,[.1,-.2,.14],[-.55,.2,-.3]]],D:[chave(0,P.cD)],
    E:[chave(0,['corpo',[-.26,-.06,-.52],[.12,-.4,-1],[0,1,-.4],'palma'])]});
  // gesto de habilidade (sal, purificação, sinalizador…): a esquerda larga o fuste, lança a mão para a frente e volta
  C.carabina_gesto=prepararClipe({nome:'gesto',dur:.62,a:[[0,[0,0,0],[0,0,0]],[.12,[.03,-.05,.03],[-.12,-.05,-.12],'o'],[.42,[.03,-.05,.03],[-.12,-.05,-.12]],[.62,[0,0,0],[0,0,0],'s']],
    D:[chave(0,P.cD),chave(.62,P.cD)],E:[chave(0,P.cE),chave(.1,['corpo',[-.3,-.3,-.15],[.3,.3,-1],[-.3,1,0],'garra'],{e:'i'}),chave(.2,['corpo',[-.26,-.1,-.56],[.25,.15,-1],[-.1,1,.1],'palma'],{e:'q'}),
      chave(.4,['corpo',[-.26,-.11,-.54],[.25,.15,-1],[-.1,1,.1],'palma']),chave(.62,P.cE,{e:'s'})],ev:[[0,'som','roupa',.5]]});
  C.carabina_abaixada=prepararClipe({nome:'abaixada',dur:1,laco:true,a:[[0,[.06,-.16,.1],[-.45,.25,-.25]]],D:[chave(0,P.cD)],
    E:[chave(0,['corpo',[-.28,-.24,-.36],[.2,-.6,-1],[-.2,1,-.4],'aberta'])]});
}

// ---------------------------------------------------------------------------------------------------------------------
// REVÓLVER DE PRATA (oito câmaras) — recarga universal: o polegar destrava, os dedos da esquerda abrem o tambor e a
// esquerda fica com a arma; cano para cima e o polegar esquerdo bate na vareta (as cápsulas caem; na tática, os
// cartuchos que sobravam caem junto, inteiros); cano para baixo, a direita traz o municiador do cinto, alinha, empurra e
// gira o botão; solta, a esquerda fecha o tambor com um tapa, a direita volta à coronha e o polegar arma o cão.
function montarClipesRevolver(C){
  const T=1.6;
  // poses no quadro da arma (revólver): a mão esquerda segura a armação por baixo, dedos pela janela do tambor
  const eAbre=['arma',[-.034,-.03,-.005],[.55,.6,-.55],[-.7,-.4,.4],'berco'];
  const eSegura=['arma',[-.03,-.042,.0],[.45,.75,-.45],[-.75,-.35,.35],'berco'];
  C.revolver_recarga=(vazia)=>{
    const tTr=.08, tAb=.2, tCima=.34, tBate=.4, tBaixo=.58, tAlinha=.84, tIns=.93, tGira=1.0, tSolta=1.07, tFecha=1.2, tCao=1.33;
    const a=[[0,[0,0,0],[0,0,0]],[tTr,[-.02,-.01,.03],[.06,.12,-.35],'o'],[tAb,[-.06,-.03,.06],[.15,.25,-.95],'w'],
      [tCima,[-.1,-.05,.12],[.88,.3,-.6],'w'],[tBate,[-.1,-.058,.12],[.95,.3,-.6],'q'],[tBate+.06,[-.1,-.05,.12],[.89,.3,-.6],'o'],
      [tBaixo,[-.11,-.07,.13],[-.72,.25,-.5],'w'],[tAlinha,[-.11,-.066,.13],[-.78,.26,-.5],'s'],[tIns,[-.11,-.07,.13],[-.81,.26,-.5],'s'],[tSolta,[-.11,-.066,.13],[-.77,.26,-.5]],
      [tFecha,[-.04,-.01,.06],[-.15,.12,.25],'q'],[tCao,[-.01,0,.02],[.05,.04,.05],'o'],[T,[0,0,0],[0,0,0],'b']];
    // a direita: polegar na trava, solta a coronha quando a esquerda pega a arma, vai ao cinto e volta com o municiador
    // a mão segura o botão do municiador por trás; a chave sai da posição do municiador no eixo do tambor. A orientação é
    // pensada no corpo (dedos para a esquerda e para baixo, dorso para cima: o antebraço vem pela direita e não cobre o
    // tambor) e convertida para o quadro do tambor no instante do encaixe.
    const parcial=prepararClipe({dur:T,a,c:{guin:[[0,0],[tTr+.02,0],[tAb,1,'q'],[tFecha-.04,1],[tFecha,0,'i'],[T,0]]}});
    const R=S.rigs.revolver, qH=qNoQuadro(R,parcial,tAlinha,'tambor',[-.85,-.4,-.35],[.1,.75,.65]); qMunMao.copy(qH).invert(); MUN_NA_MAO.set(0,0,-.014).applyQuaternion(qMunMao);
    const qG=qH.clone().multiply(new THREE.Quaternion().setFromAxisAngle(V3(0,0,1),.5));     // giro do botão: a mão gira em volta do eixo
    const mun=(s,dd,q)=>['tambor',maoPorPonto(V3(0,0,zMun(s)+.014),q||qH),q||qH,null,dd||'municia'];
    const D=[chave(0,P.rD),chave(tTr,P.rD,{dd:[.36,.84,.9,.94,.7,.2,.1,0]}),chave(tAb,P.rD,{dd:'revolver'}),chave(tCima,P.rD,{p:[.03,-.09,.05],dd:'relaxada'}),
      chave(tCima+.1,['corpo',[.2,-.5,.25],[-.2,-.6,-.6],[.9,.1,.3],'aberta'],{e:'i'}),chave(tBaixo,['corpo',[.18,-.48,.24],[-.2,-.6,-.6],[.9,.1,.3],'municia'],{e:'o'}),
      chave(tAlinha-.08,['corpo',[.0,-.2,.12],[-.4,.3,-.9],[.9,.3,.2],'municia'],{e:'s'}),chave(tAlinha,mun(0),{e:'o'}),chave(tIns,mun(1),{e:'i',reta:1}),
      chave(tGira,mun(1,'municia',qG)),chave(tGira+.025,mun(1,'municia',qG)),chave(tSolta,mun(-.9,'municia',qG),{e:'o',reta:1}),
      chave(tSolta+.05,['corpo',[.14,-.26,.16],[-.3,-.4,-.8],[1,.2,.2],'aberta'],{e:'o'}),chave(tFecha-.04,['corpo',[.12,-.2,.12],[-.3,-.4,-.8],[1,.2,.2],'aberta'],{e:'s'}),
      chave(tFecha+.06,P.rD,{e:'o'}),chave(tCao-.05,P.rD,{dd:'cao'}),chave(tCao+.06,P.rD,{dd:'revolver',e:'q'}),chave(T,P.rD)];
    const E=[chave(0,P.rE),chave(tTr,P.rE),chave(tAb,eAbre,{e:'o'}),chave(tCima,eSegura,{dd:'extrai'}),chave(tBate,eSegura,{dd:'extrai',p:[-.03,-.042,.012],e:'q'}),
      chave(tBate+.08,eSegura),chave(tFecha-.06,eSegura),chave(tFecha,eAbre,{p:[-.04,-.028,-.01],dd:'aberta',e:'q'}),chave(tFecha+.12,P.rE,{e:'o'}),chave(T,P.rE)];
    const c={guin:[[0,0],[tTr+.02,0],[tAb,1,'q'],[tFecha-.04,1],[tFecha,0,'i'],[T,0]],ext:[[0,0],[tBate-.03,0],[tBate,1,'q'],[tBate+.09,0,'o'],[T,0]],
      giro:[[0,0],[tFecha,0],[tFecha+.3,1,'o'],[T,1]],cao:[[0,0],[tCao-.04,0],[tCao+.05,1,'q'],[T,1]]};
    // municiador: na mão (cheio), no eixo do tambor entrando, solta os cartuchos ao girar o botão, sai vazio na mão e cai
    const pr={mun:[[0,{m:'oculto',s:0},'h'],[tBaixo-.02,{m:'mao',s:0},'h'],[tAlinha,{m:'tambor',s:0},'h'],[tIns,{m:'tambor',s:1},'i'],[tGira+.02,{m:'tambor',s:1},'h'],[tGira+.021,{m:'mao_vazio',s:0},'h'],[tSolta+.001,{m:'oculto',s:0},'h']],
      cam:[[0,{m:'antigas'},'h'],[tBate+.02,{m:'vazias'},'h'],[tGira+.02,{m:'novas'},'h']]};
    const ev=[[0,'som','roupa',.45],[tTr,'som','cao',.25],[tAb-.03,'som','tambor_abre',.7],[tBate,'som','extrator',.7],[tBate+.02,'ejetar',vazia],
      [tBate+.22,'som','capsulas',.6],[tBaixo,'som','roupa',.35],[tIns-.02,'som','municiador',.75],[tSolta,'soltar'],[tFecha-.01,'som','tambor_fecha',.8],[tFecha,'cam',[-.008,.004,.006]],
      [tCao,'som','cao',.55],[tBate,'cam',[.006,0,0]]];
    return prepararClipe({nome:'recarga',dur:T,a,D,E,c,pr,ev,cam:[[0,[0,0,0]],[T,[0,0,0]]]}); };
  // sacar: sobe de baixo, a esquerda passa o polegar na cruz do cabo (bênção) e o polegar direito arma o cão
  C.revolver_sacar=prepararClipe({nome:'sacar',dur:.86,
    a:[[0,[.02,-.36,.12],[-1.1,.1,-.35]],[.26,[-.01,-.03,.03],[.12,.08,-.2],'o'],[.56,[-.012,-.022,.03],[.1,.1,-.24],'s'],[.7,[0,0,0],[0,0,0],'o'],[.86,[0,0,0],[0,0,0],'s']],
    D:[chave(0,P.rD),chave(.58,P.rD),chave(.64,P.rD,{dd:'cao'}),chave(.72,P.rD,{e:'q'}),chave(.86,P.rD)],
    E:[chave(0,['corpo',[-.5,-.55,.1],[.6,.4,-.6],[-.4,-.9,0],'aberta']),chave(.24,['arma',[-.05,-.07,.06],[.4,-.8,-.2],[-.9,.2,.3],'relaxada'],{e:'o'}),
       chave(.42,['arma',[-.048,-.06,.05],[.45,-.6,-.2],[-.9,.2,.3],'relaxada']),chave(.6,P.rE,{e:'s'}),chave(.86,P.rE)],
    c:{cao:[[0,0],[.6,0],[.68,1,'q'],[.86,1]],cruz:[[0,0],[.26,0],[.36,1,'o'],[.58,0,'s'],[.86,0]]},
    ev:[[0,'som','roupa',.5],[.3,'som','bencao',.6],[.64,'som','cao',.5]]});
  C.revolver_inspecao=prepararClipe({nome:'inspecao',dur:3.4,
    a:[[0,[0,0,0],[0,0,0]],[.55,[-.05,.03,.1],[.12,.5,-.8],'w'],[1.4,[-.05,.034,.1],[.15,.52,-.84],'s'],[1.7,[-.04,.02,.09],[.2,-.3,.75],'w'],[2.6,[-.04,.024,.09],[.22,-.32,.78],'s'],[3.4,[0,0,0],[0,0,0],'w']],
    D:[chave(0,P.rD),chave(3.4,P.rD)],
    E:[chave(0,P.rE),chave(.5,P.rE),chave(.75,['arma',[-.05,-.07,.06],[.4,-.8,-.2],[-.9,.2,.3],'relaxada'],{e:'o'}),chave(1.1,['arma',[-.048,-.062,.05],[.45,-.6,-.2],[-.9,.2,.3],'relaxada']),
       chave(1.35,['arma',[-.05,-.07,.06],[.4,-.8,-.2],[-.9,.2,.3],'relaxada']),chave(1.65,P.rE),chave(1.95,['arma',[-.036,-.02,-.02],[.5,.7,-.4],[-.7,-.5,.4],'berco'],{e:'s'}),
       chave(2.15,['arma',[-.036,-.01,-.03],[.5,.7,-.4],[-.7,-.5,.4],'berco'],{e:'o'}),chave(2.35,['arma',[-.036,-.02,-.02],[.5,.7,-.4],[-.7,-.5,.4],'berco']),chave(2.55,['arma',[-.036,-.01,-.03],[.5,.7,-.4],[-.7,-.5,.4],'berco'],{e:'o'}),
       chave(2.9,P.rE,{e:'s'}),chave(3.4,P.rE)],
    c:{cruz:[[0,0],[.8,0],[1.0,1,'o'],[1.4,.2,'s'],[1.6,0],[3.4,0]],giro:[[0,0],[2.1,0],[2.3,.5,'o'],[2.5,.5],[2.7,1,'o'],[3.4,1]]},
    ev:[[.05,'som','roupa',.45],[.82,'som','bencao',.55],[2.18,'som','cao',.25],[2.58,'som','cao',.25],[3.2,'som','empunhar',.35]]});
  C.revolver_ajuste=prepararClipe({nome:'ajuste',dur:1,
    a:[[0,[0,0,0],[0,0,0]],[.35,[.002,-.004,0],[.02,0,.03]],[.62,[0,.001,0],[-.01,0,-.01],'o'],[1,[0,0,0],[0,0,0]]],
    D:[chave(0,P.rD),chave(.32,P.rD,{dd:[.2,.55,.65,.7,.4,.5,.2,.05]}),chave(.5,P.rD,{e:'q'}),chave(1,P.rD)],
    E:[chave(0,P.rE),chave(.25,P.rE,{p:[-.034,-.11,.012],dd:'relaxada'}),chave(.5,P.rE,{e:'q'}),chave(1,P.rE)],ev:[[.48,'som','empunhar',.22]]});
  C.revolver_corrida=prepararClipe({nome:'corrida',dur:1,laco:true,a:[[0,[.03,-.12,.1],[-.75,.25,-.25]]],D:[chave(0,P.rD)],E:[chave(0,['corpo',[-.42,-.52,.05],[.3,-.4,-.9],[-.5,.6,0],'relaxada'])]});
  C.revolver_selar=prepararClipe({nome:'selar',dur:1,laco:true,a:[[0,[.08,-.2,.12],[-.7,.2,-.25]]],D:[chave(0,P.rD)],E:[chave(0,['corpo',[-.26,-.06,-.52],[.12,-.4,-1],[0,1,-.4],'palma'])]});
  C.revolver_gesto=prepararClipe({nome:'gesto',dur:.62,a:[[0,[0,0,0],[0,0,0]],[.12,[.04,-.04,.02],[-.1,-.1,0],'o'],[.42,[.04,-.04,.02],[-.1,-.1,0]],[.62,[0,0,0],[0,0,0],'s']],
    D:[chave(0,P.rD),chave(.62,P.rD)],E:[chave(0,P.rE),chave(.1,['corpo',[-.3,-.3,-.15],[.3,.3,-1],[-.3,1,0],'garra'],{e:'i'}),chave(.2,['corpo',[-.26,-.1,-.56],[.25,.15,-1],[-.1,1,.1],'palma'],{e:'q'}),
      chave(.4,['corpo',[-.26,-.11,-.54],[.25,.15,-1],[-.1,1,.1],'palma']),chave(.62,P.rE,{e:'s'})],ev:[[0,'som','roupa',.5]]});
  C.revolver_abaixada=prepararClipe({nome:'abaixada',dur:1,laco:true,a:[[0,[.06,-.16,.08],[-.55,.25,-.2]]],D:[chave(0,P.rD)],E:[chave(0,['corpo',[-.28,-.24,-.36],[.2,-.6,-1],[-.2,1,-.4],'aberta'])]});
}

// ---------------------------------------------------------------------------------------------------------------------
// CETRO DE VÉRTEBRAS — não recarrega: o fervor volta e cada runa nasce da brasa e vai para a órbita. Sacar acende as
// vértebras de baixo para cima; lançar faz a onda subir pela espinha antes da brasa soltar; canalizar ergue o cetro para o
// altar e a mão esquerda abre em garra, com um fio de luz da brasa ao círculo.
function montarClipesCetro(C){
  C.cetro_sacar=prepararClipe({nome:'sacar',dur:.8,
    a:[[0,[.05,-.45,.15],[-.9,.2,.5]],[.3,[0,-.01,0],[-.22,0,.14],'o'],[.8,[0,0,0],[-.3,0,.18],'s']],
    D:[chave(0,P.xD),chave(.8,P.xD)],E:[chave(0,P.xE),chave(.8,P.xE)],c:{acende:[[0,0],[.15,0],[.6,1,'s'],[.8,1]],brasa:[[0,0],[.5,0],[.62,1.6,'q'],[.8,1,'s']]},
    ev:[[0,'som','cetro_sacar',.6]]});
  C.cetro_inspecao=prepararClipe({nome:'inspecao',dur:3.2,
    a:[[0,[0,0,0],[-.3,0,.18]],[.7,[-.12,-.06,.12],[-.28,.35,.22],'w'],[2.4,[-.13,-.05,.13],[-.25,.4,.2],'s'],[3.2,[0,0,0],[-.3,0,.18],'w']],
    D:[chave(0,P.xD),chave(.9,P.xD),chave(1.1,P.xD,{dd:[.6,.88,.92,.95,.3,.8,.6,0]}),chave(1.3,P.xD),chave(1.5,P.xD,{dd:[.82,.6,.92,.95,.3,.8,.6,0]}),chave(1.7,P.xD),chave(1.9,P.xD,{dd:[.82,.88,.62,.95,.3,.8,.6,0]}),chave(2.1,P.xD),chave(3.2,P.xD)],
    E:[chave(0,P.xE),chave(.8,['corpo',[-.42,-.34,-.2],[.25,1,-.3],[-.2,.3,1],'garra'],{e:'w'}),chave(2.3,['corpo',[-.4,-.33,-.22],[.25,1,-.3],[-.2,.3,1],'garra']),chave(3.2,P.xE,{e:'w'})],
    c:{orbita:[[0,0],[.7,1,'s'],[2.4,1],[3.2,0,'s']],acende:[[0,1],[3.2,1]]},ev:[[.2,'som','roupa',.4],[.9,'som','runa_volta',.6],[1.6,'som','runa_volta',.45]]});
  C.cetro_ajuste=prepararClipe({nome:'ajuste',dur:1,a:[[0,[0,0,0],[-.3,0,.18]],[.4,[0,.01,0],[-.27,.04,.14]],[1,[0,0,0],[-.3,0,.18]]],
    D:[chave(0,P.xD),chave(.35,P.xD,{dd:[.5,.6,.7,.75,.3,.6,.4,.05]}),chave(.55,P.xD,{e:'q'}),chave(1,P.xD)],E:[chave(0,P.xE),chave(1,P.xE)]});
  C.cetro_corrida=prepararClipe({nome:'corrida',dur:1,laco:true,a:[[0,[-.05,-.1,.08],[-.5,.15,.45]]],D:[chave(0,P.xD)],E:[chave(0,P.xE)]});
  C.cetro_canalizar=prepararClipe({nome:'canalizar',dur:1,laco:true,a:[[0,[-.08,.06,-.05],[-.75,.1,.1]]],D:[chave(0,P.xD)],
    E:[chave(0,['corpo',[-.36,-.2,-.46],[.12,1,-.4],[-.15,.35,1],'garra'])],c:{acende:[[0,1]]}});
  // gesto do culto: a garra esquerda empurra o ar para a frente enquanto o cetro recua um pouco
  C.cetro_gesto=prepararClipe({nome:'gesto',dur:.6,a:[[0,[0,0,0],[-.3,0,.18]],[.14,[.03,-.03,.05],[-.18,0,.22],'o'],[.4,[.03,-.03,.05],[-.18,0,.22]],[.6,[0,0,0],[-.3,0,.18],'s']],
    D:[chave(0,P.xD),chave(.6,P.xD)],E:[chave(0,P.xE),chave(.1,['corpo',[-.5,-.5,-.1],[.3,.5,-1],[-.3,1,0],'punho'],{e:'i'}),chave(.2,['corpo',[-.36,-.22,-.62],[.12,1,-.35],[-.15,.3,1],'garra'],{e:'q'}),
      chave(.4,['corpo',[-.36,-.23,-.6],[.12,1,-.35],[-.15,.3,1],'garra']),chave(.6,P.xE,{e:'s'})],c:{acende:[[0,1],[.15,1.6,'q'],[.6,1]]},ev:[[0,'som','roupa',.45]]});
  C.cetro_abaixada=prepararClipe({nome:'abaixada',dur:1,laco:true,a:[[0,[.02,-.12,-.02],[-.85,0,.1]]],D:[chave(0,P.xD)],
    E:[chave(0,['corpo',[-.36,-.36,-.3],[.25,-.5,-1],[-.2,1,-.3],'garra'])]});
}

// ---------------------------------------------------------------------------------------------------------------------
// Troca de estado
function clipeDe(nome){ const k=S.arma+'_'+nome; return S.clipes[k]; }
function entrar(nome,C,fade,opc){ opc=opc||{};
  if(S.saida) copiarPose(_de,S.saida); S.de=S.saida?_de:null; S.fade=0; S.fadeDur=S.saida?fade:0;
  S.est={nome,C,t:opc.t||0,dur:C.dur,disparados:-1,fim:opc.fim||null,dados:opc.dados||null}; }
function estadoDesejado(m,ctx){
  if(S.recarga) return 'recarga';
  const H=m.team==='H';
  if(H&&m.sealing>=0) return 'selar';
  if(m.hold&&m.hold.key) return 'abaixada';
  if(!H&&ctx.canalizando) return 'canalizar';
  if(ctx.correndo&&ctx.agora-S.ultTiro>.45) return 'corrida';
  return 'pronta'; }

// eventos do clipe (sons, cápsulas no mundo, municiador caindo, empurrões de câmera)
function evento(E,m){ const [,tipo,a1,a2]=E;
  if(tipo==='som'){ if(!tocar(a1,{vol:a2==null?.6:a2,max:2,varia:.035})&&a1==='alavanca') {} return; }
  if(tipo==='cam'){ const f=camFx(); S.joltV.x+=a1[0]*f*30; S.joltV.y+=a1[1]*f*30; S.joltV.z+=a1[2]*f*30; return; }
  if(tipo==='camara'){ S.camaraCheia=true; return; }
  if(tipo==='ejetar') return ejetarTambor(E[2]);
  if(tipo==='soltar') return soltarMuniciador(); }

// ---------------------------------------------------------------------------------------------------------------------
// Objetos que saem da mão para o mundo: cápsulas e cartuchos do tambor, o municiador vazio
function paraMundo(obj,vel,giro,vida,quica){ const p=new THREE.Vector3(), q=new THREE.Quaternion(), s=new THREE.Vector3(); obj.updateMatrixWorld(); obj.matrixWorld.decompose(p,q,s);
  const o=obj.clone(); o.position.copy(p); o.quaternion.copy(q); o.scale.copy(s); o.visible=true; o.traverse(x=>{ x.visible=true; if(x.isMesh) x.frustumCulled=true; });
  const chao=Math.max(.015,camera.position.y-1.67+.015); let tAnt=0;
  efeito(o,vida,(ob,k)=>{ const dt=Math.min(.05,(k-tAnt)*vida); tAnt=k; if(dt<=0) return; vel.y-=9.8*dt; ob.position.addScaledVector(vel,dt); ob.rotateX(giro.x*dt); ob.rotateZ(giro.z*dt);
    if(ob.position.y<chao){ ob.position.y=chao; vel.multiplyScalar(quica); vel.y=Math.abs(vel.y)*.5; giro.multiplyScalar(.5); } });
  // a cópia usa as geometrias compartilhadas; os materiais também (o efeito não pode descartá-los)
  return o; }
function ejetarTambor(vazia){ const W=vmRevolver; if(!W.g.visible) return; const dirW=new THREE.Vector3(0,0,1).transformDirection(W.tambor.matrixWorld);
  W.camaras.forEach((c,i)=>{ const obj=c.vivo.visible?c.vivo:c.vazio.visible?c.vazio:null; if(!obj) return;
    const v=dirW.clone().multiplyScalar(1.1+Math.random()*.6).add(new THREE.Vector3((Math.random()-.5)*.6,.2,(Math.random()-.5)*.6));
    paraMundo(obj,v,new THREE.Vector3((Math.random()-.5)*30,0,(Math.random()-.5)*30),1.8,.32); c.vivo.visible=c.vazio.visible=false; });
  S.tamborEjetado=true; }
function soltarMuniciador(){ const W=vmRevolver; if(!W.mun.visible||!W.g.visible) return; W.munC.forEach(c=>c.visible=false);
  const v=new THREE.Vector3(.3,-.6,.4).applyQuaternion(camera.quaternion); paraMundo(W.mun,v,new THREE.Vector3(6,0,9),1.4,.25); W.munC.forEach(c=>c.visible=true); W.mun.visible=false; }

// ---------------------------------------------------------------------------------------------------------------------
// Aplicação: pose → malhas
const _PW=new THREE.Vector3(), _QW=new THREE.Quaternion(), _EL=new THREE.Vector3(), _SH=new THREE.Vector3(), _PO=new THREE.Vector3(), _WR=new THREE.Vector3();
const PALMA_CENTRO=new THREE.Vector3(0,0,-.047*ESC_MAO);
function aplicar(R,pose,dt,tremor){
  R.raiz.position.copy(pose.a.p); R.raiz.quaternion.copy(pose.a.q);
  const W=R.arma, c=pose.c;
  if(W.tipo==='carabina'){ W.alav.rotation.x=(c.alav||0)*.95; W.cao.rotation.x=c.caoAbs!=null?c.caoAbs:-.5; W.port.rotation.y=-(c.port||0)*.48;
    W.ferr.position.z=.018+cl((c.alav||0)/.7,0,1)*.036; W.camara.visible=(c.alav||0)>.28&&S.camaraCheia; }
  else if(W.tipo==='revolver'){ W.guin.rotation.z=(c.guin||0)*1.42; W.ext.position.z=(c.ext||0)*.03; W.tambor.rotation.z=(c.tambor||0); W.cao.rotation.x=c.caoAbs!=null?c.caoAbs:-.6;
    W.cruzMat.emissive.setRGB(.9*(c.cruz||0),.75*(c.cruz||0),.35*(c.cruz||0)); W.prata.emissive.setRGB(.5*(c.brilho||0),.42*(c.brilho||0),.2*(c.brilho||0)); }
  R.raiz.updateMatrix();
  // mãos e braços
  for(const h of ['D','E']){ const M=R.maos[h], H=pose[h]; maoNoCorpo(R,pose,H,_PW,_QW);
    // a chave dá o centro da palma; o punho fica atrás dele
    _WR.copy(PALMA_CENTRO).applyQuaternion(_QW); M.raiz.position.copy(_PW).sub(_WR); M.raiz.quaternion.copy(_QW); posarDedos(M,H.dd,h==='E'?tremor:null);
    const B=R.bracos[h], [a,b]=R.osso[h]; _SH.copy(R.ombro[h]);
    // o antebraço termina um pouco atrás do punho (a manga cobre a luva)
    _PO.set(0,0,.004).applyQuaternion(_QW).add(M.raiz.position);
    ik(_SH,_PO,a,b,R.polo[h],_EL); tubo(B.sup,_SH,_EL,R.braco[h][0]); tubo(B.ant,_EL,_PO,R.braco[h][1]); B.cot.position.copy(_EL); B.cot.scale.setScalar(R.braco[h][1]*1.02); }
}
// objetos que mudam de pai conforme a pose (cartuchos da carabina, municiador e câmaras do revólver)
function aplicarObjetos(R,pose){ const W=R.arma;
  if(W.tipo==='carabina'){ for(const k of ['c0','c1']){ const pr=pose.pr[k]; let o=W[k]; if(!o){ o=W[k]=criarCartucho('carabina'); o.visible=false; }
      if(!pr||pr.m==='oculto'){ o.visible=false; continue; }
      o.visible=true; const pai=pr.m==='mao'?R.maos.D.pinca:W.g; if(o.parent!==pai) pai.add(o);
      if(pr.m==='mao'){ o.position.set(0,0,0); o.quaternion.copy(qPinca); }
      else { o.position.copy(W.entrada).addScaledVector(W.dirEntrada,pr.s*.065); o.rotation.set(0,.3,0); o.visible=pr.s<1; } } }
  else if(W.tipo==='revolver'){ const pm=pose.pr.mun, pc=pose.pr.cam;
    if(pm&&pm.m!=='oculto'){ W.mun.visible=true; const naMao=pm.m!=='tambor', pai=naMao?R.maos.D.pinca:W.tambor; if(W.mun.parent!==pai) pai.add(W.mun);
      W.munC.forEach(c=>c.visible=pm.m!=='mao_vazio');
      if(naMao){ W.mun.position.copy(MUN_NA_MAO); W.mun.quaternion.copy(qMunMao); } else { W.mun.position.set(0,0,zMun(pm.s)); W.mun.quaternion.identity(); } }
    else W.mun.visible=false;
    const est=pc?pc.m:'antigas';
    W.camaras.forEach((c,i)=>{ if(est==='novas'){ c.vivo.visible=true; c.vazio.visible=false; } else if(est==='vazias'){ c.vivo.visible=c.vazio.visible=false; }
      else { const vivo=i<S.carga; c.vivo.visible=vivo; c.vazio.visible=!vivo; } }); } }
const qPinca=qE([0,0,0]), qMunMao=qE([0,0,0]), MUN_NA_MAO=new THREE.Vector3(), MUN_DED=[.05,-.25,-1], MUN_DOR=[0,1,-.2];
// municiador no eixo do tambor: s=0 com as pontas encostando atrás das câmaras, s=1 com os aros rentes à face traseira
const zMun=s=>.0375+(1-s)*.0365;
// palma da mão direita para que o ponto de pinça caia em p (no quadro da chave)
function maoPorPonto(p,q){ const M=S.rigs.carabina.maos.D, off=M.pinca.position.clone().sub(PALMA_CENTRO).applyQuaternion(q); return [p.x-off.x,p.y-off.y,p.z-off.z]; }

// ---------------------------------------------------------------------------------------------------------------------
// Camadas por cima do clipe: coice e ciclo mecânico (PERFIL_ARMA), dedos que acompanham, cão e tambor
function camadaTiro(pose,m){ const W=S.R.arma, cetro=W.tipo==='cetro', exo=W.tipo==='revolver', Pf=PERFIL_ARMA[cetro?'cetro':exo?'revolver':'carabina'];
  const kc=coice(VM.tT,Pf), cy=cicloDe(VM.tT,Pf), f=Math.max(.35,camFx());
  _q1.setFromEuler(_eu.set(kc*Pf.sobe*f,0,(kc*Pf.giro-cy*(cetro?0:exo?.06:.2))*f)); pose.a.q.premultiply(_q1);
  pose.a.p.z+=kc*Pf.recuoZ*f; pose.a.p.x+=cy*.008; pose.a.p.y-=cy*.022*(cetro?0:1);
  if(W.tipo==='carabina'){ pose.c.alav=(pose.c.alav||0)+cy; pose.c.caoAbs=VM.tT<.03?-.15:-.5-cy*.35;
    // a direita aperta a alça e a abre: dedos fecham um pouco na descida
    pose.D.dd[1]+=cy*.08; pose.D.dd[2]+=cy*.06; }
  else if(exo){ const a=cl((VM.tT-Pf.ciclo[0])/(Pf.ciclo[1]-Pf.ciclo[0]),0,1);
    pose.c.tambor=(S.tiros-1+suave(a))*PI/4+(pose.c.giro||0)*PI*2/8*3;
    // o polegar sobe no cão e o puxa para trás (o cão cai no tiro e é armado no ciclo)
    pose.c.caoAbs=(VM.tT<.04?-.15:-.6-cy*.45)-(1-(pose.c.cao==null?1:pose.c.cao))*-.45;
    pose.D.dd[4]+=cy*.35; pose.D.dd[6]-=cy*.25; pose.D.dd[5]-=cy*.35; }
  return kc; }

// ---------------------------------------------------------------------------------------------------------------------
// Quadro a quadro
function quadroAnim(dt,m,ctx){ if(!S.ok) return;
  const agora=ctx.agora; S.tUlt=agora;
  const qual=m.team==='C'?'cetro':m.cls==='exorcista'?'revolver':'carabina', F=S.forca;
  const arma=F&&F.arma?F.arma:qual, vivo=F?true:m.st==='alive';
  if(arma!==S.arma){ S.arma=arma; S.R=S.rigs[arma]; S.saida=null; S.base=null; entrar('pronta',clipeDe('pronta'),0); if(vivo) iniciarSacar(); }
  const R=S.R; vmH.visible=vivo&&R.corpo===vmH; vmC.visible=vivo&&R.corpo===vmC; vmCarabina.g.visible=arma==='carabina'; vmRevolver.g.visible=arma==='revolver';
  if(!vivo){ S.visivel=false; S.recarga=null; return; }
  if(!S.visivel){ S.visivel=true; iniciarSacar(); }
  // munição atual (o revólver mostra nas câmaras quantos cartuchos e quantas cápsulas há)
  if(!S.recarga){ S.carga=F&&F.carga!=null?F.carga:m.ammo; S.camaraCheia=S.carga>0; }   // câmara vazia quando a arma está vazia (a inspeção mostra)
  // recarga: começa quando o servidor diz (reloadT > 0) e dura o que o servidor diz
  // (só na subida de reloadT: um snapshot atrasado que ainda diz "recarregando" depois do fim não reinicia o clipe)
  if(!F){ if(m.reloadT>0&&!(S.reloadAnt>0)&&!S.recarga&&m.team==='H') iniciarRecarga(m.ammo,m.reloadT); S.reloadAnt=m.reloadT;
    if(S.recarga&&m.reloadT<=0&&S.est.nome==='recarga'&&S.est.t<S.est.dur-.12) S.est.t=Math.max(S.est.t,S.est.dur-.12); }
  // estado desejado (fora dos clipes que terminam sozinhos)
  const em=S.est.nome, fixos=['sacar','recarga','inspecao','ajuste','gesto'];
  if(!F){ const d=estadoDesejado(m,ctx);
    if(fixos.includes(em)){ if(em!=='recarga'&&em!=='sacar'&&d!=='pronta') entrar(d,clipeDe(d),.16); }
    else if(d!==em&&d!=='recarga'){ entrar(d,clipeDe(d),d==='corrida'?.2:em==='corrida'?.22:.18); }
    // ocioso: ajuste de pegada e inspeção (só parado, fora de combate, sem nada para fazer)
    if(S.est.nome==='pronta'){ S.parado=ctx.andando?0:S.parado+dt; S.calmo+=dt;
      if(S.pedidoInspecao||(S.calmo>S.proxInspecao&&S.parado>2.5)){ S.pedidoInspecao=false; S.calmo=0; S.proxInspecao=28+Math.random()*20; entrar('inspecao',clipeDe('inspecao'),.18); }
      else if(S.calmo>S.proxAjuste&&S.calmo<S.proxInspecao-2){ S.proxAjuste=S.calmo+7+Math.random()*7; entrar('ajuste',clipeDe('ajuste'),.12); } } }
  else if(F.clipe&&em!==F.clipe){ forcarEstado(F); }
  // avança o tempo do clipe e dispara os eventos que ficaram para trás
  const E=S.est, C=E.C, vel=F&&F.vel!=null?F.vel:1, tAnt=E.t;
  if(F&&F.t!=null) E.t=F.t*E.dur; else E.t+=dt*vel;
  if(C.laco) E.t%=C.dur; else { if(!F||F.t==null) for(const ev of C.ev) if(ev[0]>tAnt&&ev[0]<=E.t) evento(ev,m); }
  if(!C.laco&&E.t>=C.dur&&!(F&&F.t!=null)){ if(F&&F.repetir){ E.t=0; } else { if(E.nome==='recarga') S.recarga=null; if(F) F.clipe='pronta'; const d=F?'pronta':estadoDesejado(m,ctx); entrar(d,clipeDe(d),.12); } }
  // pose do clipe, mistura com a anterior
  const base=S.base||(S.base=amostrar(R,clipeDe('pronta'),0,novaPose(),novaPose()));
  amostrar(R,S.est.C,Math.min(S.est.t,S.est.C.dur),base,_pose);
  if(S.de&&S.fade<S.fadeDur){ S.fade+=dt*vel; misturar(R,S.de,_pose,CURVA.s(cl(S.fade/S.fadeDur,0,1)),_out); } else copiarPose(_out,_pose);
  S.saida=S.saida||novaPose(); copiarPose(S.saida,_out);
  // camadas: tiro, vida (respiração e tremor sutil), corrida no passo
  if(VM.pausa>0) VM.pausa=Math.max(0,VM.pausa-dt); else VM.tT+=dt;
  { const Pf=PERFIL_ARMA[arma]; if(!VM.som&&VM.tT>=(Pf.somT||0)){ VM.som=true; if(Pf.som&&m.team==='H') tocar(Pf.som,{vol:.5,max:2,varia:.05}); if(Pf.capsula&&m.team==='H') ejetarCapsula(); } }
  const P2=_out; camadaTiro(P2,m);
  if(S.est.nome==='corrida'){ const f=VM.fase||0; P2.a.p.y+=Math.sin(f*2)*.012; P2.a.p.x+=Math.cos(f)*.012; _q1.setFromEuler(_eu.set(Math.sin(f*2)*.03,0,Math.sin(f)*.06)); P2.a.q.premultiply(_q1); }
  const resp=Math.sin(agora*1.6)*.0035, oscil=Math.sin(agora*.71+1.3)*.0025;
  P2.a.p.y+=resp; P2.a.p.x+=oscil*.6; _q1.setFromEuler(_eu.set(resp*.6,oscil*.4,oscil*.5)); P2.a.q.premultiply(_q1);
  // brilho de acerto confirmado (prata, latão, brasa): a arma reage quando o servidor confirma
  S.brilho=Math.max(0,(S.brilho||0)-dt*5); P2.c.brilho=S.brilho;
  aplicarObjetos(R,P2);
  const tremor=S.est.nome==='selar'?[Math.sin(agora*31)*.03,Math.sin(agora*27+1)*.03,Math.sin(agora*29+2)*.03,Math.sin(agora*33+3)*.03]:null;
  aplicar(R,P2,dt,tremor);
  luzNaMao(R,agora);
  // corpo: balanço do passo, atraso do mouse, atraso ao começar a andar, tranco de golpe (mola)
  S.joltV.addScaledVector(S.jolt,-dt*160).multiplyScalar(Math.exp(-dt*16)); S.jolt.addScaledVector(S.joltV,dt);
  const b=ctx.corpo; const cor=R.corpo, h=cor===vmH;
  cor.position.set((h?.2:.26)+b.x,(h?-.17:-.12)+b.y,(h?-.42:-.5)+b.z);
  cor.rotation.set(b.rx+S.jolt.x*1.5,b.ry,b.rz+S.jolt.z*1.5);
  S.cam.set(S.jolt.x*.5,S.jolt.y*.5,S.jolt.z*.5);
  if(arma==='cetro'){ cetroVida(P2,m,dt,agora,ctx); rastroBrasa(); }
  vmFlash.position.copy(arma==='revolver'?vmRevolver.boca.clone().applyMatrix4(vmRevolver.base):vmCarabina.boca);
}
function luzNaMao(R,agora){ const M=R.maos.E; if(!M.luz){ M.luz=new THREE.Sprite(new THREE.SpriteMaterial({map:RT.glow,color:R.corpo===vmH?0xffd890:0xff3a5a,blending:THREE.AdditiveBlending,depthWrite:false,transparent:true,opacity:0}));
    M.luz.position.copy(PALMA_CENTRO).add(V3(0,-.03,-.01)); M.raiz.add(M.luz); }
  const on=S.est.nome==='selar'||S.est.nome==='canalizar'||S.est.nome==='gesto', alvo=on?(.55+.25*Math.sin(agora*9)+.1*Math.sin(agora*23)):0;
  M.luz.material.opacity+=(alvo-M.luz.material.opacity)*.2; const sc=.09+M.luz.material.opacity*.07; M.luz.scale.set(sc,sc,1); M.luz.visible=M.luz.material.opacity>.01; }
function rastroBrasa(){ if(!vmCetro||VM.cast<.45||!S.R||S.R.corpo!==vmC) return; const p=new THREE.Vector3(); vmCetro.brasa.getWorldPosition(p);
  const m=new THREE.SpriteMaterial({map:RT.glow,color:0xc070ff,blending:THREE.AdditiveBlending,depthWrite:false,transparent:true,opacity:.7}); const sp=new THREE.Sprite(m); sp.position.copy(p); sp.scale.set(.07,.07,1);
  efeito(sp,.2,(o,u)=>{ o.material.opacity=.7*(1-u); const s=.07*(1-u*.6); o.scale.set(s,s,1); }); }
function iniciarSacar(){ const C=clipeDe('sacar'); if(C){ S.saida=null; entrar('sacar',C,0); S.camaraCheia=true; } }
function iniciarRecarga(ammo,reloadT){ if(!S.ok||!S.R||S.arma==='cetro') return; const vazia=ammo<=0, falta=8-ammo;
  const C=S.arma==='carabina'?S.clipes.carabina_recarga(vazia,vazia?3:Math.max(1,Math.min(3,falta))):S.clipes.revolver_recarga(vazia);
  S.recarga={vazia,ammo}; S.carga=ammo; S.tamborEjetado=false; if(vazia) S.camaraCheia=false;
  // a recarga dura o que o servidor manda: se o aviso chegou atrasado, o clipe começa adiantado
  entrar('recarga',C,.1,{t:cl(1.6-reloadT,0,.5)}); }
function forcarEstado(F){ const nome=F.clipe; let C;
  if(nome==='recarga'){ C=S.arma==='carabina'?S.clipes.carabina_recarga(!!F.vazia,F.vazia?3:(F.n||3)):S.clipes.revolver_recarga(!!F.vazia); S.recarga={vazia:!!F.vazia}; S.carga=F.vazia?0:(F.carga!=null?F.carga:5); if(F.vazia) S.camaraCheia=false; }
  else C=clipeDe(nome)||clipeDe('pronta');
  S.saida=null; entrar(nome,C,0); }

// ---------------------------------------------------------------------------------------------------------------------
// Cetro: runas que nascem da brasa quando o fervor volta, vértebras acesas, onda do lançamento, fio da canalização
function cetroVida(P2,m,dt,agora,ctx){ const W=vmCetro, c=P2.c, cast=VM.cast, kC=VM.tT;
  const cargas=Math.floor((S.forca&&S.forca.fervor!=null?S.forca.fervor:m.fervor)/CFG.sigilCusto);
  if(cargas>S.runasVisiveis&&S.est.nome!=='sacar'){ for(let k=S.runasVisiveis;k<cargas&&k<7;k++) S.runaNasce[k]=agora; if(S.tUltRuna==null||agora-S.tUltRuna>.3){ tocar('runa_volta',{vol:.4,max:1}); S.tUltRuna=agora; } }
  S.runasVisiveis=cargas;
  const giro=agora*1.5*(1+cast*6), orb=c.orbita||0;
  const recolhe=kC<.06?kC/.06:Math.max(0,1-(kC-.06)/.22), raioR=.085*(1-.8*recolhe)*(1-orb*.25)+.03*cicloDe(kC,PERFIL_ARMA.cetro);
  vmRunas.forEach((r,k)=>{ const on=k<cargas; r.visible=on; if(!on) return; const nasce=S.runaNasce[k]!=null?cl((agora-S.runaNasce[k])/.4,0,1):1, e=CURVA.o(nasce);
    const a=giro*(1-orb*.6)+k/7*TAU+(1-e)*3; const rr=raioR*e; r.position.set(Math.cos(a)*rr,.1+Math.sin(a*2+k)*.012+(1-e)*.0,Math.sin(a)*rr); r.material.rotation=a*2; r.scale.setScalar(.035*(.4+.6*e)*(1+(1-e)*.8)); });
  // vértebras: acendem de baixo para cima ao sacar; no lançamento uma onda sobe pela espinha em 80 ms
  const ac=c.acende==null?1:c.acende, n=W.vert.length;
  W.vert.forEach((v,i)=>{ const u=i/(n-1), base=cl(ac*1.4-u*.9,0,1)*.25, onda=Math.max(0,1-Math.abs(kC*12.5-u*1)*3)*(kC<.15?1:0), can=S.est.nome==='canalizar'?.35+.25*Math.sin(agora*6-u*5):0;
    v.material.emissiveIntensity=base+onda*1.6+can+(S.brilho||0)*.8; });
  const br=c.brasa==null?1:c.brasa; W.brasa.scale.setScalar(Math.max(.001,br)*(1+cast*.5+recolhe*.6+Math.sin(agora*6)*.05+(S.brilho||0)*.5)); W.runa.material.rotation+=dt*(1.5+cast*12);
  vmOrb.material.opacity=Math.min(1,(.3+.5*(m.fervor/100)+cast*.5)*Math.min(1,br)+(S.brilho||0)*.4);
  // fio de luz da brasa até o altar enquanto canaliza
  if(S.est.nome==='canalizar'&&ctx.altar){ if(!S.fio){ const g=new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(),new THREE.Vector3()]); S.fio=new THREE.Line(g,new THREE.LineBasicMaterial({color:0xff4a7a,transparent:true,opacity:.6,blending:THREE.AdditiveBlending,depthWrite:false,toneMapped:false})); S.fio.frustumCulled=false; world.add(S.fio); }
    const p=new THREE.Vector3(); W.brasa.getWorldPosition(p); const pos=S.fio.geometry.attributes.position; pos.setXYZ(0,p.x,p.y,p.z); pos.setXYZ(1,ctx.altar.x,1.05+Math.sin(agora*3)*.05,ctx.altar.z); pos.needsUpdate=true;
    S.fio.material.opacity=.35+.25*Math.sin(agora*9); S.fio.visible=true; }
  else if(S.fio) S.fio.visible=false; }

// ---------------------------------------------------------------------------------------------------------------------
// Entradas vindas do jogo
// um fiapo pequeno (sprite transparente perto da câmera custa preenchimento: nada nos níveis Baixo e Mínimo)
function fumaca(exo){ if(!vmFlash||!S.R||S.R.corpo!==vmH||(nivel&&nivel()<2)) return; const p=new THREE.Vector3(); vmFlash.getWorldPosition(p);
  for(let k=0;k<1;k++){ const m=new THREE.SpriteMaterial({map:RT.glow,color:0x8a8478,transparent:true,opacity:0,depthWrite:false}); const sp=new THREE.Sprite(m); sp.position.copy(p);
    const v=new THREE.Vector3((Math.random()-.5)*.12,.14+Math.random()*.08,(Math.random()-.5)*.12), atraso=k*.05;
    efeito(sp,.9,(o,u)=>{ const t=Math.max(0,u*.9-atraso); o.position.addScaledVector(v,1/60); const s=.04+t*.16; o.scale.set(s,s,1); o.material.opacity=Math.max(0,.18*(1-t/.85))*(t>0?1:0); }); } }
function tiro(exo){ S.ultTiro=S.tUlt; S.calmo=0; S.tiros++;
  // cada disparo tem um clarão diferente (rotação e tamanho) e deixa um fiapo de fumaça na boca do cano
  if(vmFlash){ vmFlash.material.rotation=Math.random()*TAU; const sc=(exo?.36:.3)*(.8+Math.random()*.4); vmFlash.scale.set(sc*(1+Math.random()*.3),sc,1); }
  fumaca(exo); if(['sacar','inspecao','ajuste','corrida'].includes(S.est.nome)&&S.R) entrar('pronta',clipeDe('pronta'),.07); }
function sigilo(){ S.ultTiro=S.tUlt; S.calmo=0; if(['sacar','inspecao','ajuste','corrida'].includes(S.est.nome)&&S.R) entrar('pronta',clipeDe('pronta'),.07); }
function acerto(e){ S.brilho=e.derrubou?1:e.escudo?.25:.6; S.calmo=0; }
function golpe(lado){ S.calmo=0; const f=camFx(); S.joltV.x+=(Math.random()-.5)*.5*f; S.joltV.z+=lado*.6*f; S.joltV.y-=.3*f;
  if(['inspecao','ajuste'].includes(S.est.nome)&&S.R) entrar('pronta',clipeDe('pronta'),.1); }
// vista da vitrine: uma câmera presa à do jogador, olhando a arma de outro ângulo (para conferir contato e atravessamento)
const VISTAS={ladoPerto:[[-.4,-.06,-.36],[.1,-.21,-.44]],cimaPerto:[[.1,.2,-.38],[.1,-.2,-.42]],lado:[[-.62,-.12,-.36],[.12,-.2,-.42]],cima:[[.12,.45,-.4],[.12,-.2,-.42]],frente:[[.18,-.12,-1.15],[.12,-.2,-.42]],direita:[[.75,-.1,-.4],[.12,-.2,-.42]],perto:[[-.05,-.06,-.12],[.15,-.22,-.45]]};
let camV=null;
// luz de estúdio (só na vitrine): três luzes presas à câmera para conferir forma e contato, sem o escuro da noite
let estudio=null;
function luzEstudio(on){ if(on&&!estudio){ estudio=new THREE.Group(); const a=new THREE.DirectionalLight(0xffffff,1.6); a.position.set(-1,1.2,.6); const b=new THREE.DirectionalLight(0x9fb4ff,.7); b.position.set(1,.2,.4);
    const c=new THREE.DirectionalLight(0xffe0c0,.9); c.position.set(.3,.6,-1.2); const t=new THREE.Object3D(); t.position.set(.1,-.2,-.45); estudio.add(a,b,c,t); [a,b,c].forEach(l=>l.target=t); camera.add(estudio); LEITURA.uL.value=.05; }
  else if(!on&&estudio){ camera.remove(estudio); estudio=null; LEITURA.uL.value=.2; } }
function camVista(cam){ luzEstudio(!!(S.forca&&S.forca.estudio));
  if(S.vista==='peca'){ if(!camV){ camV=new THREE.PerspectiveCamera(50,1,.01,60); cam.add(camV); }
    // perto da peça que trabalha: a portinhola da carabina ou o tambor do revólver (no espaço da arma)
    const W=S.arma==='revolver'?vmRevolver:vmCarabina; W.g.updateMatrixWorld(true);
    const alvo=S.arma==='revolver'?W.tambor.localToWorld(V3(0,0,0)):W.g.localToWorld(W.entrada.clone());
    const de=S.arma==='revolver'?W.g.localToWorld(V3(-.16,.07,.12)):W.g.localToWorld(V3(.17,.09,.06));
    camV.parent.worldToLocal(de); camV.position.copy(de); camV.aspect=cam.aspect; camV.fov=38; camV.up.set(0,1,0).applyQuaternion(cam.quaternion.clone().invert()).normalize(); camV.up.set(0,1,0);
    camV.lookAt(alvo); camV.updateProjectionMatrix(); camV.updateMatrixWorld(); return camV; }
  const v=S.vista&&VISTAS[S.vista]; if(!v) return null;
  if(!camV){ camV=new THREE.PerspectiveCamera(50,1,.01,60); cam.add(camV); }
  camV.aspect=cam.aspect; camV.fov=S.vista==='perto'?62:S.vista.endsWith('Perto')?40:50; camV.position.set(...v[0]); camV.up.set(0,S.vista.startsWith('cima')?0:1,S.vista.startsWith('cima')?-1:0); camV.lookAt(cam.localToWorld(new THREE.Vector3(...v[1])));
  camV.updateProjectionMatrix(); camV.updateMatrixWorld(); return camV; }
// ---------------------------------------------------------------------------------------------------------------------
// Vitrine (F9 numa partida local, ou o jogo aberto com #vitrine): escolhe a arma e o clipe, a velocidade, a vista e a luz,
// e arrasta a linha do tempo. Não é um menu do jogo: é uma ferramenta para olhar as animações sem jogar uma noite.
const VIT={arma:'carabina',clipe:'recarga',vazia:true,vel:1,vista:null,estudio:false,repetir:true,t:null};
function aplicarVitrine(reiniciar){ const o={arma:VIT.arma,clipe:VIT.clipe,vazia:VIT.vazia,n:3,carga:VIT.vazia?0:4,vel:VIT.vel,vista:VIT.vista,estudio:VIT.estudio,repetir:VIT.repetir,t:VIT.t,fervor:100};
  if(reiniciar||!S.forca) forcarAPI(o); else { Object.assign(S.forca,o); S.vista=o.vista; } }
function atirarVitrine(){ VM.k=1; VM.tT=0; VM.som=false; VM.tiros++; S.tiros++; if(VIT.arma==='cetro'){ VM.cast=1; tocar('sigilo',{vol:.6}); } else { vmFlash.material.opacity=1; tocar(VIT.arma==='revolver'?'tiro_revolver':'tiro_carabina',{vol:.7}); }
  if(S.est.nome!=='pronta'){ VIT.clipe='pronta'; aplicarVitrine(true); } }
function forcarAPI(o){ S.forca=o?Object.assign({},o):null; S.vista=o&&o.vista||null; if(o){ if(o.arma&&o.arma!==S.arma) S.arma=null; if(S.est) S.est.nome='_'; } }
function painel(abrir){ let el=document.getElementById('vitrineArma');
  if(el&&abrir!==true){ el.remove(); forcarAPI(null); return; } if(el) return;
  el=document.createElement('div'); el.id='vitrineArma';
  el.style.cssText='position:fixed;top:12px;right:12px;width:290px;max-height:calc(100vh - 24px);overflow:auto;background:rgba(12,9,14,.9);color:#eadfca;font:13px/1.35 system-ui,sans-serif;padding:12px 12px 10px;border:1px solid #6b4a2a;border-radius:8px;z-index:99999';
  const grupo=(rot,itens,chave,fn)=>`<div style="margin:8px 0 3px;color:#c9a36a">${rot}</div><div>${itens.map(([v,t])=>`<button data-k="${chave}" data-v="${v}" style="margin:2px;padding:4px 7px;background:#2a1d18;color:#eadfca;border:1px solid #6b4a2a;border-radius:5px;cursor:pointer">${t}</button>`).join('')}</div>`;
  el.innerHTML=`<b style="font-size:14px">Vitrine das animações</b> <span style="opacity:.6">(F9 fecha)</span>`+
    grupo('Arma',[['carabina','Carabina'],['revolver','Revólver'],['cetro','Cetro']],'arma')+
    grupo('Clipe',[['pronta','Pronta'],['sacar','Sacar'],['recarga:1','Recarga vazia'],['recarga:0','Recarga tática'],['inspecao','Inspeção'],['ajuste','Ajuste de pegada'],['corrida','Corrida'],['selar','Selar / canalizar'],['abaixada','Interagir'],['tiro','Atirar']],'clipe')+
    grupo('Velocidade',[['1','1×'],['.5','½×'],['.25','¼×'],['.1','1/10×']],'vel')+
    grupo('Vista',[['','Jogador'],['lado','Lado'],['cima','Cima'],['frente','Frente'],['direita','Direita'],['peca','Peça (portinhola, tambor)']],'vista')+
    grupo('Luz',[['0','Do jogo'],['1','Estúdio']],'luz')+
    `<div style="margin:8px 0 3px;color:#c9a36a">Linha do tempo <span id="vitT" style="color:#eadfca"></span></div><input id="vitR" type="range" min="0" max="1000" value="0" style="width:100%"><div><button id="vitPlay" style="margin:2px;padding:4px 9px;background:#2a1d18;color:#eadfca;border:1px solid #6b4a2a;border-radius:5px">▶ tocar</button><label style="margin-left:8px"><input id="vitLoop" type="checkbox" checked> repetir</label></div>`;
  document.body.appendChild(el);
  ['mousedown','click','keydown','pointerdown'].forEach(ev=>el.addEventListener(ev,e=>e.stopPropagation()));
  el.querySelectorAll('button[data-k]').forEach(b=>b.onclick=()=>{ const k=b.dataset.k, v=b.dataset.v;
    if(k==='arma'){ VIT.arma=v; if(v==='cetro'&&VIT.clipe==='recarga') VIT.clipe='pronta'; aplicarVitrine(true); }
    else if(k==='clipe'){ if(v==='tiro') return atirarVitrine(); const [c,vz]=v.split(':'); VIT.clipe=c==='selar'&&VIT.arma==='cetro'?'canalizar':c; if(vz!=null) VIT.vazia=vz==='1'; if(c==='recarga'&&VIT.arma==='cetro') VIT.clipe='inspecao'; VIT.t=null; aplicarVitrine(true); }
    else if(k==='vel'){ VIT.vel=+v; aplicarVitrine(false); } else if(k==='vista'){ VIT.vista=v||null; aplicarVitrine(false); } else if(k==='luz'){ VIT.estudio=v==='1'; aplicarVitrine(false); } });
  const R=el.querySelector('#vitR'); R.oninput=()=>{ VIT.t=R.value/1000; aplicarVitrine(false); };
  el.querySelector('#vitPlay').onclick=()=>{ VIT.t=null; if(S.forca) S.forca.t=null; aplicarVitrine(true); };
  el.querySelector('#vitLoop').onchange=e=>{ VIT.repetir=e.target.checked; aplicarVitrine(false); };
  aplicarVitrine(true);
  const tick=()=>{ if(!document.getElementById('vitrineArma')) return; const T=el.querySelector('#vitT'); if(S.est&&S.est.C){ T.textContent=`${S.est.t.toFixed(2)} / ${S.est.dur.toFixed(2)} s`; if(VIT.t==null) R.value=Math.round(S.est.t/S.est.dur*1000); } requestAnimationFrame(tick); }; tick(); }
function hab(){ S.calmo=0; if(['pronta','ajuste','inspecao','corrida'].includes(S.est.nome)&&S.R&&clipeDe('gesto')) entrar('gesto',clipeDe('gesto'),.08); }
function pedirInspecao(){ if(S.est&&S.est.nome==='pronta') S.pedidoInspecao=true; }
return {construir,quadro:quadroAnim,camVista,hab,tiro,sigilo,acerto,golpe,pedirInspecao,S,DEDOS,
  // vitrine: força uma arma e um clipe; t (0 a 1) fixa o instante, vel muda a velocidade
  forcar:forcarAPI,painel,
  get cam(){ return S.cam; },get vm(){ return {vmH,vmC,vmCarabina,vmRevolver,vmCetro,vmOrb,vmRunas,vmFlash}; }};
};
