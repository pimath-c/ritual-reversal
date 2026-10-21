#!/usr/bin/env node
// Pranchas de conceito do mapa da v6, desenhadas a partir dos números do layout (shared/sim.js), para fixar a direção
// de arte antes de modelar: a elevação sul da catedral vista da mata (do outro lado do desfiladeiro) e uma vista
// superior conceitual das regiões. Não substituem os modelos 3D nem o layout; orientam os dois.
// Uso: node tools/conceito.js [pasta]   (padrão: docs/conceito)
'use strict';
const fs=require('fs'), path=require('path');
const Sim=require('../shared/sim.js');
const out=process.argv[2]||path.join(__dirname,'..','docs','conceito'); fs.mkdirSync(out,{recursive:true});
let sem=7; const R=()=>((sem=(sem*16807)%2147483647)/2147483647);
const f=v=>v.toFixed(1);

// ================= elevação sul =================
// 1 m = K px; x do jogo vira x da prancha (o oeste à esquerda, como quem olha para o norte a partir da mata); y = altura.
// O chão do promontório fica em CHAO; à frente, a Ponte Grande em perspectiva sobre o desfiladeiro.
// As alturas são as visuais da v6 (ALTURA em client/index.html): a simulação só usa a altura das peças para colisão e
// visão, e qualquer parede acima de 2,2 m já bloqueia; então a catedral pode ser tão alta quanto pede a silhueta.
const ALT={parede:26,cumeeira:38,empena:32,torreO:54,agulhaO:98,lanterna:48,agulhaC:88,torreL:30};
{ const K=6.2, W=1600, H=1150, CX=W/2, CHAO=760, X=x=>CX+x*K, Y=y=>CHAO-y*K;
  const s=[];
  s.push(`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" width="${W}" height="${H}">`);
  s.push(`<defs>
  <linearGradient id="ceu" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#03050b"/><stop offset=".5" stop-color="#0b1222"/><stop offset=".85" stop-color="#1a2944"/><stop offset="1" stop-color="#25385a"/></linearGradient>
  <radialGradient id="lua" cx="0" cy="0" r="1"><stop offset="0" stop-color="#eef3ff"/><stop offset=".06" stop-color="#cfdcff" stop-opacity=".9"/><stop offset=".25" stop-color="#6f88c8" stop-opacity=".25"/><stop offset="1" stop-color="#6f88c8" stop-opacity="0"/></radialGradient>
  <linearGradient id="pedra" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#0d0f15"/><stop offset=".7" stop-color="#191c26"/><stop offset=".96" stop-color="#3b445c"/><stop offset="1" stop-color="#5a6684"/></linearGradient>
  <linearGradient id="pedraEsc" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#08090d"/><stop offset=".9" stop-color="#1c2130"/><stop offset="1" stop-color="#3a4258"/></linearGradient>
  <linearGradient id="nevoa" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8fa2c8" stop-opacity="0"/><stop offset=".5" stop-color="#8fa2c8" stop-opacity=".25"/><stop offset="1" stop-color="#8fa2c8" stop-opacity="0"/></linearGradient>
  <linearGradient id="abismo" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#0b0e15"/><stop offset=".4" stop-color="#030406"/><stop offset="1" stop-color="#000"/></linearGradient>
  <linearGradient id="tabuleiro" x1="0" y1="1" x2="0" y2="0"><stop offset="0" stop-color="#2c3140"/><stop offset="1" stop-color="#3c4356"/></linearGradient>
  <radialGradient id="vela" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#ffd08a"/><stop offset=".4" stop-color="#ff8a36" stop-opacity=".55"/><stop offset="1" stop-color="#ff8a36" stop-opacity="0"/></radialGradient>
  <linearGradient id="vitral" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffb070"/><stop offset=".5" stop-color="#c0442e"/><stop offset="1" stop-color="#43183a"/></linearGradient>
  <radialGradient id="rosa" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#ffd8a0"/><stop offset=".5" stop-color="#c04a30"/><stop offset=".8" stop-color="#3a1630"/><stop offset="1" stop-color="#0b0c11"/></radialGradient>
  <filter id="borra"><feGaussianBlur stdDeviation="8"/></filter><filter id="brilho"><feGaussianBlur stdDeviation="3"/></filter></defs>`);
  s.push(`<rect width="${W}" height="${H}" fill="url(#ceu)"/>`);
  for(let i=0;i<300;i++){ const x=R()*W, y=R()*CHAO*.8, r=R()<.9?.7:1.4; s.push(`<circle cx="${f(x)}" cy="${f(y)}" r="${r}" fill="#dfe6ff" opacity="${(.25+R()*.6).toFixed(2)}"/>`); }
  s.push(`<circle cx="${W-230}" cy="210" r="330" fill="url(#lua)"/><circle cx="${W-230}" cy="210" r="38" fill="#eef2ff"/>`);
  // mata ao fundo e dos lados do promontório
  { let d=`M0 ${f(Y(8))}`; for(let x=0;x<=W;x+=16) d+=` L${x} ${f(Y(10+R()*10+(Math.abs(x-CX)>520?8:0)))}`; d+=` L${W} ${CHAO} L0 ${CHAO}Z`; s.push(`<path d="${d}" fill="#06090b"/>`); }
  const pt=([x,y])=>f(X(x))+','+f(Y(y));
  const poli=(pts,fill,extra)=>s.push(`<polygon points="${pts.map(pt).join(' ')}" fill="${fill||'url(#pedra)'}" ${extra||''}/>`);
  const caixa=(x1,x2,y1,y2,fill)=>poli([[x1,y1],[x2,y1],[x2,y2],[x1,y2]],fill);
  const pinaculo=(x,y,h,w,fill)=>{ caixa(x-w,x+w,y-h*.35,y,fill); poli([[x-w,y],[x,y+h],[x+w,y]],fill); };
  const lanceta=(x,y,w,h,fill)=>s.push(`<path d="M${f(X(x-w))} ${f(Y(y))} L${f(X(x-w))} ${f(Y(y+h-w))} Q ${f(X(x-w))} ${f(Y(y+h))} ${f(X(x))} ${f(Y(y+h+w*.35))} Q ${f(X(x+w))} ${f(Y(y+h))} ${f(X(x+w))} ${f(Y(y+h-w))} L${f(X(x+w))} ${f(Y(y))}Z" fill="${fill}"/>`);
  // --- fundo: transepto com empenas, a capela desabada e a abside ---
  poli([[-44,0],[-44,ALT.parede-2],[-30,ALT.empena],[-16,ALT.parede-2],[-16,0]],'url(#pedraEsc)');
  poli([[16,0],[16,ALT.parede-2],[23,ALT.empena-3],[25,ALT.empena-7],[28,ALT.empena-4],[31,ALT.empena-10],[34,ALT.parede-6],[38,ALT.parede-3],[44,ALT.parede-9],[44,0]],'url(#pedra)');
  for(const x of [-44,-30.5,30.5,44]) pinaculo(x,ALT.parede-2,8,1.1,x>0?'#2c3346':'#0d0f15');
  for(const x of [-37,-23]) lanceta(x,6,1.5,12,'url(#vitral)');
  // --- torre do cruzeiro: lanterna com janelas acesas e agulha ---
  caixa(-9,9,ALT.parede,ALT.lanterna,'url(#pedraEsc)');
  for(const x of [-6,-2,2,6]) lanceta(x,ALT.lanterna-14,1,10,'#3a2418');
  for(const x of [-9,9]) pinaculo(x,ALT.lanterna,9,1.1,x>0?'#3a4258':'#0d0f15');
  poli([[-7,ALT.lanterna],[0,ALT.agulhaC],[7,ALT.lanterna]],'url(#pedraEsc)');
  for(let k=1;k<6;k++){ const y=ALT.lanterna+(ALT.agulhaC-ALT.lanterna)*k/6, w=7*(1-k/6); s.push(`<line x1="${f(X(-w))}" y1="${f(Y(y))}" x2="${f(X(w))}" y2="${f(Y(y))}" stroke="#2a3042" stroke-width="2"/>`); }
  poli([[-.8,ALT.agulhaC],[0,ALT.agulhaC+4],[.8,ALT.agulhaC]],'#d9cfb0');
  // --- nave: telhado, paredes, arcobotantes com pináculos e janelas altas acesas ---
  poli([[-16.5,ALT.parede],[0,ALT.cumeeira],[16.5,ALT.parede]],'#161922');
  caixa(-16.5,16.5,0,ALT.parede);
  for(const x of [-16.5,16.5]){ const sx=Math.sign(x), cor=sx>0?'#3b445c':'#12141b';
    for(const k of [0,1,2]){ const px=x+sx*(4.5+k*3.2), py=12+k*2;
      s.push(`<path d="M${f(X(x))} ${f(Y(ALT.parede-3-k*2))} Q ${f(X(x+sx*(2+k*1.6)))} ${f(Y(ALT.parede-2-k*2))} ${f(X(px))} ${f(Y(py+5))}" stroke="${cor}" stroke-width="${f(1*K)}" fill="none"/>`);
      caixa(px-1,px+1,0,py+5,cor); pinaculo(px,py+5,6,.9,cor); } }
  // --- fachada: duas fiadas de nichos, rosácea, galeria de reis, portal ---
  for(const x of [-12.5,12.5]) lanceta(x,6,1.4,10,'url(#vitral)');
  { const cx=0, cy=17, r=5; s.push(`<circle cx="${f(X(cx))}" cy="${f(Y(cy))}" r="${f((r+1)*K)}" fill="#0b0c11"/><circle cx="${f(X(cx))}" cy="${f(Y(cy))}" r="${f(r*K)}" fill="url(#rosa)"/><circle cx="${f(X(cx))}" cy="${f(Y(cy))}" r="${f(r*K)}" fill="#ffb070" opacity=".25" filter="url(#brilho)"/>`);
    for(let k=0;k<16;k++){ const a=k/16*Math.PI*2; s.push(`<line x1="${f(X(cx))}" y1="${f(Y(cy))}" x2="${f(X(cx+Math.cos(a)*r))}" y2="${f(Y(cy+Math.sin(a)*r))}" stroke="#0b0c11" stroke-width="2.4"/>`); }
    s.push(`<circle cx="${f(X(cx))}" cy="${f(Y(cy))}" r="${f(1.2*K)}" fill="#0b0c11"/>`); }
  for(let k=0;k<9;k++){ const x=-12+k*3; lanceta(x,23,.8,3.2,'#0b0c11'); poli([[x-.35,23],[x-.45,25],[x,25.8],[x+.45,25],[x+.35,23]],'#2c3244'); } // galeria dos reis
  poli([[-16.5,ALT.parede],[0,ALT.parede+9],[16.5,ALT.parede]],'url(#pedra)');
  for(let k=0;k<5;k++){ const w=4+k*1.05, h=8.5+k*1.2; s.push(`<path d="M${f(X(-w))} ${f(Y(0))} L${f(X(-w))} ${f(Y(h*.6))} Q ${f(X(-w))} ${f(Y(h))} ${f(X(0))} ${f(Y(h+1))} Q ${f(X(w))} ${f(Y(h))} ${f(X(w))} ${f(Y(h*.6))} L${f(X(w))} ${f(Y(0))}" fill="none" stroke="${k%2?'#2d3344':'#141720'}" stroke-width="${f(.75*K)}"/>`); }
  s.push(`<path d="M${f(X(-4))} ${f(Y(0))} L${f(X(-4))} ${f(Y(5))} Q ${f(X(-4))} ${f(Y(8.4))} ${f(X(0))} ${f(Y(9.4))} Q ${f(X(4))} ${f(Y(8.4))} ${f(X(4))} ${f(Y(5))} L${f(X(4))} ${f(Y(0))}Z" fill="#1c0f0b"/>`);
  s.push(`<ellipse cx="${f(X(0))}" cy="${f(Y(2))}" rx="${f(6*K)}" ry="${f(3.5*K)}" fill="url(#vela)" opacity=".8"/>`);
  for(const x of [-9.5,9.5]){ lanceta(x,7.5,1,3.8,'#0b0c11'); poli([[x-.45,7.5],[x-.6,10.2],[x,11.2],[x+.6,10.2],[x+.45,7.5]],'#2e3446'); }
  // --- torre oeste: inteira, contrafortes de quina, sineira acesa e agulha ---
  caixa(-28,-16.5,0,ALT.torreO,'url(#pedraEsc)');
  for(const x of [-28.8,-16.7]) caixa(x,x+1.1,0,ALT.torreO-6,'#0a0b10');
  for(const y of [14,28,40]) caixa(-28.8,-15.7,y,y+.7,'#0b0c11');
  for(const x of [-25.4,-19.1]) lanceta(x,ALT.torreO-14,1.1,9,'#030406');
  s.push(`<circle cx="${f(X(-22.25))}" cy="${f(Y(ALT.torreO-9))}" r="${f(1.6*K)}" fill="url(#vela)"/>`);
  for(const x of [-28,-16.5]) pinaculo(x,ALT.torreO,10,1.2,'#11131a');
  poli([[-27,ALT.torreO],[-22.25,ALT.agulhaO],[-17.5,ALT.torreO]],'url(#pedraEsc)');
  for(let k=1;k<8;k++){ const y=ALT.torreO+(ALT.agulhaO-ALT.torreO)*k/8, w=4.75*(1-k/8); s.push(`<line x1="${f(X(-22.25-w))}" y1="${f(Y(y))}" x2="${f(X(-22.25+w))}" y2="${f(Y(y))}" stroke="#262c3c" stroke-width="2"/>`); }
  poli([[-23,ALT.agulhaO],[-22.25,ALT.agulhaO+5],[-21.5,ALT.agulhaO]],'#d9cfb0');
  // --- torre leste: perdeu o topo e o canto; vigas à mostra, sem sino ---
  poli([[16.5,0],[16.5,ALT.torreL-6],[18,ALT.torreL-2],[19.4,ALT.torreL-4],[21.2,ALT.torreL+2],[22.8,ALT.torreL-1.5],[24.6,ALT.torreL+.5],[26.2,ALT.torreL-7],[28,ALT.torreL-5],[28,0]],'url(#pedra)');
  for(let k=0;k<5;k++){ const x=18+k*2.2; s.push(`<line x1="${f(X(x))}" y1="${f(Y(ALT.torreL-6+R()*4))}" x2="${f(X(x+R()*2-1))}" y2="${f(Y(ALT.torreL+2+R()*3))}" stroke="#2a2018" stroke-width="3"/>`); }
  for(const y of [14]) caixa(15.7,28.8,y,y+.7,'#262b3a');
  lanceta(22.25,18,1.2,6,'#05060a');
  for(let k=0;k<11;k++){ const x=X(17+R()*20), y=Y(ALT.torreL+4+R()*18); s.push(`<path d="M${f(x-6)} ${f(y)} q6 -5 6 0 q0 -5 6 0" stroke="#05060a" stroke-width="2" fill="none"/>`); }
  // --- adro e borda do penhasco ---
  caixa(-46,46,-1.4,0,'#1c202c'); for(const x of [-38,38]){ caixa(x-.5,x+.5,0,2.4,'#262b3a'); }
  s.push(`<rect x="0" y="${CHAO}" width="${W}" height="${H-CHAO}" fill="url(#abismo)"/>`);
  { let d=`M0 ${CHAO}`; for(let x=0;x<=W;x+=12) d+=` L${x} ${f(CHAO+4+R()*14+(Math.abs(x-CX)<70?0:R()*26))}`; d+=` L${W} ${CHAO}Z`; s.push(`<path d="${d}" fill="#12141c"/>`); }
  // --- a Ponte Grande em perspectiva: da borda da mata (embaixo) até o adro ---
  { const yP=CHAO+2, yB=H+40, wP=4.5*K, wB=270, xC=CX;
    s.push(`<polygon points="${f(xC-wP)},${yP} ${f(xC+wP)},${yP} ${f(xC+wB)},${yB} ${f(xC-wB)},${yB}" fill="url(#tabuleiro)"/>`);
    for(const sx of [-1,1]){ s.push(`<polygon points="${f(xC+sx*wP)},${yP} ${f(xC+sx*(wP+6))},${yP-6} ${f(xC+sx*(wB+40))},${yB-46} ${f(xC+sx*wB)},${yB}" fill="${sx>0?'#4a5470':'#1a1d28'}"/>`);
      for(const t of [.18,.5]){ const y=yP+(yB-yP)*t, x=xC+sx*(wP+(wB-wP)*t), e=.35+t*2.6; // pedestais e estátuas encapuzadas, maiores quanto mais perto
        s.push(`<rect x="${f(x-9*e)}" y="${f(y-46*e)}" width="${f(18*e)}" height="${f(46*e)}" fill="${sx>0?'#3a4258':'#161922'}"/>`);
        s.push(`<path d="M${f(x-8*e)} ${f(y-46*e)} L${f(x-9*e)} ${f(y-82*e)} Q ${f(x)} ${f(y-104*e)} ${f(x+9*e)} ${f(y-82*e)} L${f(x+8*e)} ${f(y-46*e)}Z" fill="${sx>0?'#4a5470':'#1d2130'}"/>`);
        s.push(`<ellipse cx="${f(x)}" cy="${f(y-84*e)}" rx="${f(4*e)}" ry="${f(5*e)}" fill="#05060a"/>`); }
      // arcos da ponte sob o tabuleiro
      s.push(`<path d="M${f(xC+sx*wP)} ${yP+10} Q ${f(xC+sx*(wP+60))} ${yP+150} ${f(xC+sx*(wB+20))} ${H}" stroke="${sx>0?'#2a3040':'#10121a'}" stroke-width="22" fill="none"/>`); }
    // estátua tombada no meio do tabuleiro e lanternas nas cabeças da ponte
    s.push(`<ellipse cx="${f(xC+12)}" cy="${f(yP+(yB-yP)*.33)}" rx="38" ry="8" fill="#3a4152"/>`);
    for(const sx of [-1,1]) s.push(`<circle cx="${f(xC+sx*(wP+4))}" cy="${yP-12}" r="16" fill="url(#vela)"/>`); }
  s.push(`<rect x="0" y="${CHAO-30}" width="${W}" height="160" fill="url(#nevoa)" filter="url(#borra)"/>`);
  s.push(`<rect x="0" y="${CHAO+150}" width="${W}" height="240" fill="url(#nevoa)" opacity=".7" filter="url(#borra)"/>`);
  // legendas (embaixo, sobre o abismo, longe da silhueta)
  const txt=(x,y,t,sz,cor,anc)=>s.push(`<text x="${x}" y="${y}" fill="${cor||'#c9d2e4'}" font-family="Georgia, serif" font-size="${sz||18}" text-anchor="${anc||'start'}">${t}</text>`);
  txt(40,H-120,'Ritual Reversal — a catedral vista da Portaria, do outro lado do desfiladeiro',26,'#e6ddc4');
  txt(40,H-90,'Prancha de conceito da v6, nas proporções do layout: nave de 33 m, transepto de 88 m, fachada voltada para a Ponte Grande.',15);
  txt(40,H-68,'Silhueta em três alturas: agulha da torre oeste (98 m), agulha do cruzeiro (88 m) e a torre leste partida (~30 m). A lua vem do sudeste.',15);
  const nota=(x,y,t,anc)=>{ s.push(`<line x1="${x}" y1="${y}" x2="${x+(anc==='end'?-40:40)}" y2="${y}" stroke="#8a93a8"/>`); txt(x+(anc==='end'?-46:46),y+5,t,14,'#9aa3b8',anc); };
  nota(X(-23),Y(ALT.agulhaO-12),'torre oeste: sineira acesa; marco que se vê de toda a mata','end');
  nota(X(4),Y(ALT.agulhaC-14),'torre do cruzeiro: o centro do mapa visto de longe');
  nota(X(29),Y(ALT.torreL+6),'torre leste: caiu sobre o Cemitério dos Monges');
  nota(X(-17),Y(13),'rosácea e portal com arquivoltas: a entrada principal','end');
  nota(X(31),Y(ALT.parede-8),'capela desabada: o transepto leste sem teto');
  s.push('</svg>'); fs.writeFileSync(path.join(out,'catedral.svg'),s.join('\n')); }

// ================= vista superior conceitual =================
{ const HX=Sim.HALF.x, HZ=Sim.HALF.z, K=4.4, M=40, W=HX*2*K+M*2, H=HZ*2*K+M*2+60, X=x=>M+(x+HX)*K, Z=z=>M+60+(z+HZ)*K;
  const s=[]; s.push(`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${f(W)} ${f(H)}" width="${f(W)}" height="${f(H)}" font-family="Georgia, serif">`);
  s.push(`<defs><radialGradient id="cl" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#3d4e38"/><stop offset="1" stop-color="#3d4e38" stop-opacity="0"/></radialGradient>
    <pattern id="mata" width="14" height="14" patternUnits="userSpaceOnUse"><rect width="14" height="14" fill="#16201a"/><circle cx="4" cy="5" r="3.6" fill="#1d2a20"/><circle cx="11" cy="11" r="3" fill="#1a2620"/></pattern></defs>`);
  s.push(`<rect width="${f(W)}" height="${f(H)}" fill="#0d1110"/><rect x="${M}" y="${M+60}" width="${f(HX*2*K)}" height="${f(HZ*2*K)}" fill="url(#mata)"/>`);
  const P=Sim.PROMONTORIO, D=Sim.DESFILADEIRO;
  s.push(`<rect x="${f(X(D.x1))}" y="${f(Z(D.z1))}" width="${f((D.x2-D.x1)*K)}" height="${f((D.z2-D.z1)*K)}" fill="#030406"/>`);
  s.push(`<rect x="${f(X(P.x1))}" y="${f(Z(P.z1))}" width="${f((P.x2-P.x1)*K)}" height="${f((P.z2-P.z1)*K)}" fill="#2a2a2e" rx="6"/>`);
  // sombras da catedral sobre o promontório
  for(const Zn of Sim.ZONAS) if(Zn.name==='Planalto'&&Zn.luar<.3) s.push(`<rect x="${f(X(Zn.x1))}" y="${f(Z(Zn.z1))}" width="${f((Zn.x2-Zn.x1)*K)}" height="${f((Zn.z2-Zn.z1)*K)}" fill="#18181c"/>`);
  for(const Zn of Sim.ZONAS) if(Zn.name==='Catedral'||Zn.name==='Claustro') s.push(`<rect x="${f(X(Zn.x1))}" y="${f(Z(Zn.z1))}" width="${f((Zn.x2-Zn.x1)*K)}" height="${f((Zn.z2-Zn.z1)*K)}" fill="${Zn.name==='Claustro'?'#3e4a36':Zn.luar>0?'#5a5246':'#4a443b'}"/>`);
  s.push(`<circle cx="${f(X(0))}" cy="${f(Z(-85))}" r="${f(11*K)}" fill="#4a443b"/>`);
  // trilhas principais (luar) e clareiras
  for(const T of Sim.TRILHAS) if(!T.estreita) s.push(`<polyline points="${T.pts.map(([x,z])=>f(X(x))+','+f(Z(z))).join(' ')}" fill="none" stroke="#7a6a48" stroke-width="${f(T.w*K*.8)}" stroke-linecap="round" stroke-linejoin="round" opacity=".85"/>`);
  for(const c of Sim.CLAREIRAS) s.push(`<circle cx="${f(X(c.x))}" cy="${f(Z(c.z))}" r="${f(c.r*K)}" fill="url(#cl)"/>`);
  // travessias
  for(const T of Sim.TRAVESSIAS){ if(T.eixo==='z') s.push(`<rect x="${f(X(T.a))}" y="${f(Z(T.de))}" width="${f((T.b-T.a)*K)}" height="${f((T.ate-T.de)*K)}" fill="#8a8070"/>`);
    else s.push(`<rect x="${f(X(T.de))}" y="${f(Z(T.a))}" width="${f((T.ate-T.de)*K)}" height="${f((T.b-T.a)*K)}" fill="${T.nome==='Ponte Caída'?'#6a5034':'#8a8070'}"/>`); }
  // reagentes, altares, nascimentos
  for(const [x,z] of Sim.REAG) s.push(`<circle cx="${f(X(x))}" cy="${f(Z(z))}" r="6" fill="#ff3355" stroke="#ffd0d8" stroke-width="1.5"/>`);
  Sim.ALTARS.forEach((A,i)=>{ s.push(`<circle cx="${f(X(A.x))}" cy="${f(Z(A.z))}" r="13" fill="none" stroke="#ffb040" stroke-width="3"/><text x="${f(X(A.x))}" y="${f(Z(A.z)+5)}" fill="#ffd080" font-size="13" text-anchor="middle" font-weight="bold">${i+1}</text>`); });
  for(const t of ['H','C']){ const p=Sim.SPAWN[t]; s.push(`<circle cx="${f(X(p.x))}" cy="${f(Z(p.z))}" r="14" fill="${t==='H'?'#e8c860':'#e04050'}" stroke="#fff" stroke-width="3"/>`); }
  // regiões nomeadas (o "porquê" de cada uma)
  const rot=(x,z,t,sub,sz)=>{ s.push(`<text x="${f(X(x))}" y="${f(Z(z))}" fill="#efe6cc" font-size="${sz||17}" text-anchor="middle">${t}</text>`); if(sub) s.push(`<text x="${f(X(x))}" y="${f(Z(z)+16)}" fill="#a9b0bf" font-size="12" text-anchor="middle">${sub}</text>`); };
  rot(0,-50,'CATEDRAL','nave longa e pilares: duelo de média distância',20); rot(-30,-56,'Claustro','',13); rot(54,-48,'Cemitério dos Monges','cobertura baixa');
  rot(-63,-86,'Mosteiro','sombra da catedral: aproximação escura'); rot(63,-90,'Ossuário','');
  rot(0,-14,'Ponte Grande','rápida e exposta',14); rot(-90,-85,'Ponte Caída','estreita, sem cobertura',13); rot(90,-85,'Aqueduto','entre muretas',13);
  rot(-120,-4,'Acampamento','Caçadores',14); rot(120,-4,'Círculo de Pedras','Cultistas',14);
  rot(0,4,'Portaria','',14); rot(-46,22,'Pedreira','paredões, 2 reagentes',14); rot(46,26,'Charco','árvores mortas, 2 reagentes',14);
  rot(0,58,'Encruzilhada','mercador',14); rot(-82,82,'Menires','',14); rot(82,85,'Encapuzados','',14); rot(0,99,'Calvário','',13);
  s.push(`<text x="${M}" y="40" fill="#e6ddc4" font-size="24">Ritual Reversal — vista superior conceitual (v6)</text>`);
  s.push(`<text x="${M}" y="66" fill="#a9b0bf" font-size="14">Promontório da catedral cercado pelo desfiladeiro em U; floresta a oeste, ao sul e a leste. Altares numerados, reagentes em vermelho, nascimentos em amarelo (caça) e vermelho (culto).</text>`);
  s.push('</svg>'); fs.writeFileSync(path.join(out,'mapa.svg'),s.join('\n')); }
console.log('pranchas em',out);
