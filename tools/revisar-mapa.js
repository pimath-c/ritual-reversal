#!/usr/bin/env node
// v6.5f: auditoria do mapa na simulação: frestas entre peças (0,04 a 0,85 m: parecem passagem e não são), peça dentro de
// outra, peças que se atravessam, pontos do jogo onde não cabe um boneco e bolsões livres sem ligação com o resto.
// Uso: node tools/revisar-mapa.js [catedral|asilo]
'use strict';
const Sim=require('../shared/sim.js'); const MAPA=process.argv[2]||'catedral'; Sim.usarMapa(MAPA);
const W=Sim.WALLS, out=[]; const nome=w=>`${w.kind}(${w.x1.toFixed(1)}..${w.x2.toFixed(1)},${w.z1.toFixed(1)}..${w.z2.toFixed(1)} h${w.h})`;
const nat=k=>/arvore_f|espinheiro|limite|abismo|lago|penhasco|paredao/.test(k);
// frestas: duas caixas a menos de 0,85 m, com face paralela em mais de 0,6 m (parece passagem e não é / prende)
const fr={};
for(let i=0;i<W.length;i++) for(let j=i+1;j<W.length;j++){ const a=W[i],b=W[j]; if(nat(a.kind)&&nat(b.kind)) continue;
  const gx=Math.max(b.x1-a.x2,a.x1-b.x2), gz=Math.max(b.z1-a.z2,a.z1-b.z2), ox=Math.min(a.x2,b.x2)-Math.max(a.x1,b.x1), oz=Math.min(a.z2,b.z2)-Math.max(a.z1,b.z1);
  let g=null; if(gx>.04&&gx<.85&&oz>.6) g=gx; if(gz>.04&&gz<.85&&ox>.6) g=gz; if(g==null) continue;
  const k=a.kind+'|'+b.kind; (fr[k]=fr[k]||[]).push(`${g.toFixed(2)} m entre ${nome(a)} e ${nome(b)}`); }
// peça inteira dentro de outra (o visual de uma some ou atravessa a outra)
const dentro=[];
for(const a of W) for(const b of W){ if(a===b||a.kind===b.kind) continue; if(a.x1>=b.x1-.01&&a.x2<=b.x2+.01&&a.z1>=b.z1-.01&&a.z2<=b.z2+.01&&a.h<=b.h+.01) dentro.push(`${nome(a)} dentro de ${nome(b)}`); }
// sobreposição parcial grande entre peças de tipos diferentes (fora natureza): uma atravessa a outra
const cruza={};
for(let i=0;i<W.length;i++) for(let j=i+1;j<W.length;j++){ const a=W[i],b=W[j]; if(a.kind===b.kind) continue; if(nat(a.kind)!==nat(b.kind)&&!/arvore_f/.test(a.kind+b.kind)) {} 
  const ox=Math.min(a.x2,b.x2)-Math.max(a.x1,b.x1), oz=Math.min(a.z2,b.z2)-Math.max(a.z1,b.z1); if(ox<.3||oz<.3) continue;
  const ar=ox*oz, mn=Math.min((a.x2-a.x1)*(a.z2-a.z1),(b.x2-b.x1)*(b.z2-b.z1)); if(ar<mn*.15) continue;
  const k=[a.kind,b.kind].sort().join('×'); (cruza[k]=cruza[k]||[]).push(`${nome(a)} × ${nome(b)} (${(ar/mn*100).toFixed(0)}%)`); }
// pontos do jogo onde não cabe um boneco
const pts=[]; const P=(n,x,z)=>pts.push([n,x,z]);
(Sim.ALTARS||[]).forEach(a=>P('altar '+a.name,a.x,a.z)); (Sim.REAG||[]).forEach(p=>P('reagente',p[0],p[1])); (Sim.ERVA_PTS||[]).forEach(p=>P('erva',p[0],p[1]));
(Sim.PISTA_PTS||[]).forEach(p=>P('pista',p[0],p[1])); (Sim.SENTINELA_PTS||[]).forEach(p=>P('sentinela',p[0],p[1])); (Sim.NPCS||[]).forEach(n=>P('npc '+n.id,n.x,n.z));
if(Sim.SPAWN) for(const k in Sim.SPAWN) P('nascimento '+k,Sim.SPAWN[k].x,Sim.SPAWN[k].z);
(Sim.LUGARES||[]).forEach(l=>{ if(l.x!=null) P('lugar '+(l.nome||l.n||''),l.x,l.z); });
const bloq=pts.filter(([n,x,z])=>!Sim.livre({x,z},.4)).map(([n,x,z])=>`${n} em (${x},${z})`);
// bolsões: células livres (r .4) em grade de 0,5 m, componentes conexas; as que não ligam à maior são lugares sem acesso
const H=Sim.HALF, c=.5, nx=Math.ceil(H.x*2/c), nz=Math.ceil(H.z*2/c), comp=new Int32Array(nx*nz).fill(-1), livre=new Uint8Array(nx*nz);
for(let i=0;i<nx;i++) for(let j=0;j<nz;j++){ const x=-H.x+(i+.5)*c, z=-H.z+(j+.5)*c; livre[j*nx+i]=Sim.livre({x,z},.4)?1:0; }
let nc=0; const tam=[], amostra=[];
for(let s=0;s<nx*nz;s++){ if(!livre[s]||comp[s]>=0) continue; const fila=[s]; comp[s]=nc; let n=0; while(fila.length){ const q=fila.pop(); n++; const i=q%nx, j=(q-i)/nx;
    for(const [di,dj] of [[1,0],[-1,0],[0,1],[0,-1]]){ const a=i+di,b=j+dj; if(a<0||b<0||a>=nx||b>=nz) continue; const r=b*nx+a; if(livre[r]&&comp[r]<0){ comp[r]=nc; fila.push(r); } } }
  tam.push(n); amostra.push([-H.x+(s%nx+.5)*c, -H.z+(Math.floor(s/nx)+.5)*c]); nc++; }
const mai=tam.indexOf(Math.max(...tam)); const bols=tam.map((n,k)=>[n,k]).filter(([n,k])=>k!==mai&&n*c*c>=1).sort((a,b)=>b[0]-a[0]).map(([n,k])=>`${(n*c*c).toFixed(1)} m² perto de (${amostra[k][0].toFixed(1)},${amostra[k][1].toFixed(1)})${Sim.zoneAt?' '+Sim.zoneAt(...amostra[k]):''}`);
console.log(`== ${MAPA}: ${W.length} peças`);
console.log(`-- frestas (0,04 a 0,85 m):`); for(const k in fr) console.log(`  ${k}: ${fr[k].length}\n    `+fr[k].slice(0,6).join('\n    '));
console.log(`-- peça dentro de outra: ${dentro.length}\n  `+dentro.slice(0,30).join('\n  '));
console.log(`-- peças que se atravessam:`); for(const k in cruza) console.log(`  ${k}: ${cruza[k].length}\n    `+cruza[k].slice(0,4).join('\n    '));
console.log(`-- pontos bloqueados: ${bloq.length}\n  `+bloq.join('\n  '));
console.log(`-- bolsões livres sem ligação com o resto (≥1 m²): ${bols.length}\n  `+bols.slice(0,40).join('\n  '));
