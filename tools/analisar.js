#!/usr/bin/env node
// Junta os registros de partida e compara com as metas do documento de balanceamento v2.
// Uso:  node tools/analisar.js [pasta ou registro.json]        (padrão: ./logs)
//       node tools/analisar.js logs --csv saida.csv
'use strict';
const fs=require('fs'), path=require('path');

const args=process.argv.slice(2);
const dir=args.find(a=>!a.startsWith('--'))||path.join(__dirname,'..','logs');
const csvOut=args.includes('--csv')?args[args.indexOf('--csv')+1]:null;
const Sim=require('../shared/sim.js');
const CACA_NOME={selou:'selou (Farol)',perdeu_disputa:'chegou a tempo e perdeu o círculo',chegou_tarde:'chegou tarde demais',perto_sem_entrar:'chegou perto e não entrou',altar_errado:'foi a outro altar e não chegou',nunca_chegou:'nunca chegou perto',aberto:'noite acabou antes'};
const CONV={}, SAIDA={}, VERSOES={}, DIFS={}, DISP={}, LANCES={}, ISCA=[];
// v6.4 (da análise de design de 28/10): o que decide a noite. Primeira queda, resgates, a chance de selar pelo tempo que faltava e
// pela distância do Caçador quando o ritual foi localizado, vitória por classe e por altar, e o intervalo entre rituais.
const DEC={noites:0,primeira:{n:0,venceu:0,porLado:{H:[0,0],C:[0,0]}},quedas:0,reanim:0,exec:0,sangrou:0,loc:[],classes:{},altares:{},entre:[],consagrar:[]};
const NOITE={n:0,culto:0,fendas:0,farois:0,duracao:[],verdade:[],semVerdade:0,maiorSemPista:[],pistas:[],lanterna:0,desmasc:0,localizarFim:[],labs:{}};

const METAS={
  chegada:[.45,.50,'Caçador chega ao círculo a tempo'],
  sobFogo:[.50,.70,'Tempo selando sob fogo'],
  contato:[120,240,'Primeiro contato (s)'],
  cancel:[.25,.40,'Selamentos interrompidos'],
  completos:[.40,.60,'Rituais que viram Fenda'],
};
const med=a=>{ if(!a.length) return null; const s=[...a].sort((x,y)=>x-y),m=s.length>>1; return s.length%2?s[m]:(s[m-1]+s[m])/2; };
const avg=a=>a.length?a.reduce((x,y)=>x+y,0)/a.length:null;
const pct=v=>v==null?'—':(v*100).toFixed(0)+'%';
const seg=v=>v==null?'—':`${Math.floor(v/60)}:${String(Math.round(v%60)).padStart(2,'0')}`;
const num=(v,d=1)=>v==null?'—':v.toFixed(d);

if(!fs.existsSync(dir)){ console.error(`Pasta ou arquivo não encontrado: ${dir}`); process.exit(1); }
// aceita uma pasta de registros ou um registro só (o JSON baixado no fim da noite)
const umArquivo=fs.statSync(dir).isFile(), base=umArquivo?path.dirname(dir):dir;
const files=umArquivo?[path.basename(dir)]:fs.readdirSync(dir).filter(f=>f.endsWith('.json'));
if(!files.length){ console.error(`Nenhum registro .json em ${dir}. Jogue uma partida ou rode: node tools/simular.js 20`); process.exit(1); }

const A={ partidas:0, rodadas:0, humanos:0, rituais:{1:{n:0,completos:0,selados:0,chegou:0,restante:[]},2:{n:0,completos:0,selados:0,chegou:0,restante:[]}},
  seloStart:0, seloStop:0, causas:{}, contato:[], sobFogo:[], quedas:0, quedaEq:{}, morteEq:{}, mortes:0, execucoes:0, sangrou:0, reanim:0,
  chamariz:0, chamarizVisto:0, chamarizPurgado:0, purgas:0, transfer:0, habilidades:{}, drops:{ess:0,selo:0},
  vitorias:{cultoDaVez:0,total:0}, desempates:0, empates:0, classes:{} };

for(const f of files){
  let J; try{ J=JSON.parse(fs.readFileSync(path.join(base,f),'utf8')); }catch(e){ console.error(`Registro ilegível, ignorado: ${f}`); continue; }
  const ev=J.events||[]; if(!ev.length) continue;
  const emAndamento={};
  const fechar=R=>{ const b=A.rituais[R.n]; b.n++; if(R.chegou){ b.chegou++; if(R.restante!=null) b.restante.push(R.restante); } };
  A.partidas++;
  for(const e of ev){
    switch(e.ev){
      case 'round_start': A.rodadas++; (e.teams||[]).forEach(t=>{ A.classes[t.cls]=(A.classes[t.cls]||0)+1; if(t.human) A.humanos++; }); break;
      case 'ritual_start': emAndamento[e.altar]={n:Math.min(2,Math.max(1,e.channelers||1)),chegou:false,restante:null}; break;
      case 'hunter_arrive': { const R=emAndamento[e.altar]; if(R&&!R.chegou){ R.chegou=true; R.restante=e.remaining; } break; }
      case 'ritual_complete': case 'sealed': { if(e.ev==='ritual_complete') A.ritCompl=(A.ritCompl||0)+1; else A.ritSel=(A.ritSel||0)+1;
        const R=emAndamento[e.altar]; if(R){ if(e.canalizadores) R.n=Math.min(2,e.canalizadores); fechar(R); delete emAndamento[e.altar]; } break; }
      case 'round_end': Object.values(emAndamento).forEach(fechar); for(const k in emAndamento) delete emAndamento[k]; break;
      case 'seal_start': A.seloStart++; break;
      case 'seal_stop': A.seloStop++; A.causas[e.cause]=(A.causas[e.cause]||0)+1; break;
      case 'first_contact': A.contato.push(e.t); break;
      case 'down': A.quedas++; A.quedaEq[e.team]=(A.quedaEq[e.team]||0)+1; break;
      case 'death': A.morteEq[e.team]=(A.morteEq[e.team]||0)+1; A.mortes++; if(e.how==='exec') A.execucoes++; if(e.how==='bleed') A.sangrou++; break;
      case 'revive': A.reanim++; break;
      case 'decoy': A.chamariz++; break;
      case 'decoy_spotted': A.chamarizVisto++; break;
      case 'purge': A.purgas++; if(e.decoy) A.chamarizPurgado++; break;
      case 'transfer': A.transfer++; break;
      case 'ability': A.habilidades[e.feitico]=(A.habilidades[e.feitico]||0)+1; break;
      case 'pickup': A.drops[e.kind]=(A.drops[e.kind]||0)+1; break;
    }
  }
  for(const r of Object.values(J.stats||{})) if(r&&r.fracaoSobFogo!=null) A.sobFogo.push(r.fracaoSobFogo);
  // noite única: relatório de playtest (o mesmo que o jogo mostra no fim) e quem venceu
  if(!(J.scores&&J.scores.A)&&J.scores&&J.scores.B){ const M=Sim.relatorio(ev,1), I=M.investigacao; NOITE.n++; if(J.scores.B.done>=2) NOITE.culto++;
    NOITE.fendas+=M.fendas; NOITE.farois+=M.farois; NOITE.duracao.push(M.duracao); NOITE.pistas.push(I.pistasNovas); NOITE.maiorSemPista.push(I.maiorIntervaloSemPista);
    if(I.primeiraVerdade!=null) NOITE.verdade.push(I.primeiraVerdade); else NOITE.semVerdade++;
    NOITE.lanterna+=M.ferramentas.lanterna; NOITE.desmasc+=M.engano.chamarizesDesmascarados;
    M.rituais.forEach(R=>{ if(R.localizarAoFim!=null) NOITE.localizarFim.push(R.localizarAoFim); CONV[R.caca]=(CONV[R.caca]||0)+1; (R.cultoSaiu||[]).forEach(m=>SAIDA[m]=(SAIDA[m]||0)+1);
      // a disputa no círculo (registro 3 em diante): o que cada lado fez, separado pelo desfecho
      if(R.disputa&&(R.caca==='selou'||R.caca==='perdeu_disputa')){ const D=DISP[R.caca]||(DISP[R.caca]={n:0,v:{}}), H=R.disputa.caca, C=R.disputa.culto; D.n++;
        const soma=(k,v)=>D.v[k]=(D.v[k]||0)+(v||0);
        soma('cacaCirculo',H.circulo); soma('cacaSelando',H.selando); soma('cacaSobFogo',H.sobFogo); soma('cacaAtacando',H.atacando); soma('cacaDano',H.dano); soma('cacaQuedas',H.quedas);
        soma('cultoCirculo',C.circulo); soma('cultoCanal',C.canal); soma('cultoAtacando',C.atacando); soma('cultoDano',C.dano); soma('cultoQuedas',C.quedas);
        soma('intSelo',R.interrupcoes&&R.interrupcoes.selo); soma('intCanal',R.interrupcoes&&R.interrupcoes.canal); } });
    (M.lances||[]).forEach(x=>LANCES[x.tipo]=(LANCES[x.tipo]||0)+1); if(M.engano.segundosPresosEmChamariz!=null) ev.filter(e=>e.ev==='purge'&&e.decoy&&e.isca!=null).forEach(e=>ISCA.push(e.isca));
    { const cultoVenceu=J.scores.B.done>=2; DEC.noites++;
      // primeira queda: quem derrubou primeiro venceu a noite? (o caído é de um lado; quem derrubou, do outro)
      const q=ev.find(e=>e.ev==='down'); if(q){ const derrubou=q.team==='H'?'C':'H', ganhou=(derrubou==='C')===cultoVenceu; DEC.primeira.n++; if(ganhou) DEC.primeira.venceu++; DEC.primeira.porLado[derrubou][0]++; if(ganhou) DEC.primeira.porLado[derrubou][1]++; }
      DEC.quedas+=ev.filter(e=>e.ev==='down').length; DEC.reanim+=ev.filter(e=>e.ev==='revive').length; DEC.exec+=ev.filter(e=>e.ev==='death'&&e.how==='exec').length; DEC.sangrou+=ev.filter(e=>e.ev==='death'&&e.how==='bleed').length;
      // cada ritual: quanto faltava e a que distância estava a caça na localização (registros da v6.4 em diante) e como acabou
      const aberto={}; let fimAnt=null;
      for(const e of ev){
        if(e.ev==='ritual_start'){ aberto[e.altar]={loc:null}; if(fimAnt!=null){ DEC.entre.push(e.t-fimAnt); fimAnt=null; } }
        if(e.ev==='consecrate'&&fimAnt!=null&&DEC._ultimaConsagracao!==fimAnt){ DEC.consagrar.push(e.t-fimAnt); DEC._ultimaConsagracao=fimAnt; }
        if(e.ev==='localized'&&aberto[e.altar]) aberto[e.altar].loc=e;
        if(e.ev==='sealed'||e.ev==='ritual_complete'){ const R=aberto[e.altar]; const A_=DEC.altares[e.altar]||(DEC.altares[e.altar]={selou:0,fenda:0}); A_[e.ev==='sealed'?'selou':'fenda']++;
          if(R&&R.loc&&R.loc.restante!=null) DEC.loc.push({restante:R.loc.restante,dist:R.loc.distCaca,selou:e.ev==='sealed'}); delete aberto[e.altar]; fimAnt=e.t; } }
      const ini=ev.find(e=>e.ev==='round_start'); (ini&&ini.teams||[]).forEach(t=>{ const C=DEC.classes[t.cls]||(DEC.classes[t.cls]={n:0,venceu:0}); C.n++; if((t.team==='C')===cultoVenceu) C.venceu++; }); }
    const v=J.versao?`v${J.versao} (registro ${J.registro})`:'anterior à v5.6'; VERSOES[v]=(VERSOES[v]||0)+1; const dd=J.dificuldade||'não registrada'; DIFS[dd]=(DIFS[dd]||0)+1;
    const lab=Sim.labDe(J.lab), mud=Object.keys(Sim.LAB_OPCOES).filter(k=>lab[k]!==Sim.labDe()[k]).map(k=>`${k}=${lab[k]}`).join(',')||'padrão'; NOITE.labs[mud]=(NOITE.labs[mud]||0)+1; }
  const S=J.scores||{};
  if(S.A&&S.B){ A.vitorias.total++; if(S.A.done===S.B.done){ if(S.A.lastT!==S.B.lastT||Math.abs(S.A.maxP-S.B.maxP)>.005) A.desempates++; else A.empates++; } }
}

// rituais por número de canalizadores: completos/selados vêm dos totais, distribuídos pelo que se sabe
const totRit=A.rituais[1].n+A.rituais[2].n, totCheg=A.rituais[1].chegou+A.rituais[2].chegou;
const medido={
  chegada: totRit?totCheg/totRit:null,
  sobFogo: avg(A.sobFogo),
  contato: avg(A.contato),
  cancel: A.seloStart?A.seloStop/A.seloStart:null,
  completos: totRit?(A.ritCompl||0)/totRit:null,
};
const fmtMeta={chegada:pct,sobFogo:pct,contato:v=>num(v,0),cancel:pct,completos:pct};
const status=(k,v)=>{ if(v==null) return '—'; const [lo,hi]=METAS[k]; return v<lo?'abaixo':v>hi?'acima':'na meta'; };

const L=[];
const linha=(a,b,c,d)=>L.push([a.padEnd(36),String(b).padStart(9),String(c==null?'':c).padStart(18),String(d==null?'':d).padStart(9)].join('  ').trimEnd());
L.push(`\nRitual Reversal — ${A.partidas} ${A.partidas===1?'partida':'partidas'}, ${A.rodadas} rodadas, ${A.humanos} participações humanas`);
L.push(`Pasta: ${dir}\n`);
L.push('METAS DO DOCUMENTO v2'); L.push('-'.repeat(78));
linha('Métrica','Medido','Meta','Situação');
for(const k of Object.keys(METAS)){ const [lo,hi,nome]=METAS[k], f=fmtMeta[k];
  linha(nome, f(medido[k]), `${f(lo)} a ${f(hi)}`, status(k,medido[k])); }

L.push('\nRITUAL POR NÚMERO DE CANALIZADORES'); L.push('-'.repeat(78));
linha('','Sozinho','Em dupla','');
linha('Rituais iniciados', A.rituais[1].n, A.rituais[2].n, '');
linha('Caçador chegou ao círculo', A.rituais[1].n?pct(A.rituais[1].chegou/A.rituais[1].n):'—', A.rituais[2].n?pct(A.rituais[2].chegou/A.rituais[2].n):'—','');
linha('Tempo de ritual restante na chegada', num(avg(A.rituais[1].restante))+' s', num(avg(A.rituais[2].restante))+' s','');

L.push('\nDISPUTA NO CÍRCULO'); L.push('-'.repeat(78));
linha('Tentativas de selamento', A.seloStart, '', '');
linha('Interrompidas', `${A.seloStop} (${pct(medido.cancel)})`, '', '');
for(const [c,n] of Object.entries(A.causas).sort((a,b)=>b[1]-a[1])) linha(`   por ${c}`, n, A.seloStop?pct(n/A.seloStop):'', '');
linha('Rituais completos (Fenda)', A.ritCompl||0, '', '');
linha('Rituais selados (Farol)', A.ritSel||0, '', '');

L.push('\nCOMBATE E DEDUÇÃO'); L.push('-'.repeat(78));
linha('Quedas', A.quedas, A.rodadas?num(A.quedas/A.rodadas)+' por rodada':'', '');
const qh=A.quedaEq.H||0, qc=A.quedaEq.C||0, maior=qh+qc?Math.max(qh,qc)/(qh+qc):null;
linha('   Caçadores derrubados', qh, qh+qc?pct(qh/(qh+qc)):'', maior==null?'':maior>.6?'acima de 60%':'na meta');
linha('   Cultistas derrubados', qc, qh+qc?pct(qc/(qh+qc)):'', '');
linha('Mortes', A.mortes, `${A.execucoes} execuções, ${A.sangrou} sangraram`, '');
linha('Reanimações', A.reanim, A.quedas?pct(A.reanim/A.quedas)+' das quedas':'', '');
linha('Primeiro contato (mediana)', seg(med(A.contato)), A.contato.length?`${seg(Math.min(...A.contato))} a ${seg(Math.max(...A.contato))}`:'', '');
linha('Chamarizes plantados', A.chamariz, `${A.chamarizVisto} desmascarados pela lanterna`, '');
linha('Altares purgados', A.purgas, `${A.chamarizPurgado} eram chamariz`, '');
linha('Transferências', A.transfer, '', '');
linha('Essência Profana / Selo de Luz', `${A.drops.ess||0} / ${A.drops.selo||0}`, '', '');

L.push('\nUSO DE FEITIÇOS'); L.push('-'.repeat(78));
for(const [c,n] of Object.entries(A.habilidades).sort((a,b)=>b[1]-a[1])) linha(c, n, num(n/Math.max(1,A.rodadas))+' por rodada', '');

if(A.vitorias.total){ L.push('\nRESULTADOS'); L.push('-'.repeat(78));
  linha('Partidas com placar', A.vitorias.total, '', '');
  linha('Decididas no desempate', A.desempates, pct(A.desempates/A.vitorias.total), '');
  linha('Empate exato (iria ao Ritual Final)', A.empates, pct(A.empates/A.vitorias.total), ''); }

if(NOITE.n){ L.push('\nNOITE ÚNICA (relatório de playtest)'); L.push('-'.repeat(78));
  linha('Noites', NOITE.n, Object.entries(NOITE.labs).map(([k,v])=>`${k}: ${v}`).join('; '), '');
  linha('Vitórias dos Cultistas', `${NOITE.culto} (${pct(NOITE.culto/NOITE.n)})`, '', '');
  linha('Fendas / Faróis por noite', `${num(NOITE.fendas/NOITE.n)} / ${num(NOITE.farois/NOITE.n)}`, '', '');
  linha('Duração da noite (mediana)', seg(med(NOITE.duracao)), '', '');
  linha('Primeira verdade da caça (mediana)', seg(med(NOITE.verdade)), NOITE.semVerdade?`${NOITE.semVerdade} noites sem nenhuma`:'', '');
  linha('Pistas novas por noite', num(avg(NOITE.pistas)), '', '');
  linha('Maior trecho sem pista (mediana)', seg(med(NOITE.maiorSemPista)), '', '');
  linha('Da localização ao fim do ritual', num(med(NOITE.localizarFim),0)+' s', 'mediana', '');
  linha('Lanterna em altar desperto', NOITE.lanterna, `${NOITE.desmasc} Chamarizes desmascarados`, '');
  linha('Versões dos registros', Object.entries(VERSOES).map(([k,v])=>`${k}: ${v}`).join('; '), '', '');
  linha('Dificuldade dos bots', Object.entries(DIFS).map(([k,v])=>`${k}: ${v}`).join('; '), '', '');
  const totR=Object.values(CONV).reduce((a,b)=>a+b,0);
  if(totR&&!CONV.undefined){ L.push('\nO QUE A CAÇA FEZ EM CADA RITUAL'); L.push('-'.repeat(78));
    for(const k of ['selou','perdeu_disputa','chegou_tarde','perto_sem_entrar','altar_errado','nunca_chegou','aberto']) if(CONV[k]) linha(CACA_NOME[k], CONV[k], pct(CONV[k]/totR), ''); }
  const totS=Object.values(SAIDA).reduce((a,b)=>a+b,0);
  if(totS){ L.push('\nPOR QUE UM CULTISTA SAIU DO CÍRCULO COM O RITUAL EM ANDAMENTO'); L.push('-'.repeat(78));
    for(const [k,v] of Object.entries(SAIDA).sort((a,b)=>b[1]-a[1])) linha(k, v, pct(v/totS), ''); }
  if(DISP.selou||DISP.perdeu_disputa){ L.push('\nA DISPUTA NO CÍRCULO: MÉDIA POR RITUAL (a menos de 12 m, depois que a caça chegou)'); L.push('-'.repeat(78));
    const S=DISP.selou||{n:0,v:{}}, P=DISP.perdeu_disputa||{n:0,v:{}}, m=(D,k,d=1)=>D.n?num((D.v[k]||0)/D.n,d):'—';
    linha('', 'Selou', 'Perdeu o círculo', ''); linha('Rituais', S.n, P.n, '');
    for(const [k,nome,d] of [['cacaCirculo','Caça no círculo (s)'],['cacaSelando','Caça selando (s)'],['cacaSobFogo','   selando sob fogo (s)'],['cacaAtacando','Caça atirando (s)'],['cacaDano','Dano causado pela caça',0],['cacaQuedas','Caçadores derrubados',2],
      ['cultoCirculo','Culto no círculo (s)'],['cultoCanal','Culto canalizando (s)'],['cultoAtacando','Culto atacando (s)'],['cultoDano','Dano causado pelo culto',0],['cultoQuedas','Cultistas derrubados',2],
      ['intSelo','Selos interrompidos',2],['intCanal','Canalizações interrompidas',2]]) linha(nome, m(S,k,d), m(P,k,d), ''); }
  const totL=Object.values(LANCES).reduce((a,b)=>a+b,0);
  if(totL||ISCA.length){ L.push('\nLANCES DA NOITE'); L.push('-'.repeat(78));
    const NL={selo_limite:'selo no último instante',achou_cedo:'achou o ritual antes do coro',defesa:'defendeu o ritual até a Fenda',resgate:'reanimou depois de impedir a execução',transferencia:'Transferência que virou Fenda',chamariz:'Chamariz que prendeu a caça'};
    for(const [k,v] of Object.entries(LANCES).sort((a,b)=>b[1]-a[1])) linha(NL[k]||k, v, num(v/NOITE.n,2)+' por noite', '');
    if(ISCA.length) linha('Chamariz queimado: caça presa (s)', num(med(ISCA),0), `mediana de ${ISCA.length}`, ''); } }

if(DEC.noites){ L.push('\nO QUE DECIDE A NOITE (v6.4)'); L.push('-'.repeat(78));
  const P=DEC.primeira; linha('Quem derrubou primeiro venceu', P.n?`${P.venceu}/${P.n}`:'—', P.n?pct(P.venceu/P.n):'', '');
  for(const [k,nome] of [['H','   a caça derrubou primeiro'],['C','   o culto derrubou primeiro']]) if(P.porLado[k][0]) linha(nome, `${P.porLado[k][1]}/${P.porLado[k][0]}`, pct(P.porLado[k][1]/P.porLado[k][0])+' venceu', '');
  linha('Quedas reanimadas', `${DEC.reanim}/${DEC.quedas}`, DEC.quedas?pct(DEC.reanim/DEC.quedas):'', '');
  linha('   executadas / sangraram', `${DEC.exec} / ${DEC.sangrou}`, '', '');
  linha('Entre um ritual e o próximo', num(med(DEC.entre),0)+' s', `mediana de ${DEC.entre.length}`, '');
  linha('   até a próxima consagração', num(med(DEC.consagrar),0)+' s', 'mediana', '');
  if(DEC.loc.length){ L.push('\nCHANCE DE SELAR PELO QUE FALTAVA QUANDO O RITUAL FOI LOCALIZADO'); L.push('-'.repeat(78));
    const faixa=(lista,campo,cortes,un)=>{ for(let i=0;i<=cortes.length;i++){ const a=i?cortes[i-1]:-Infinity,b=i<cortes.length?cortes[i]:Infinity; const R=lista.filter(x=>x[campo]!=null&&x[campo]>=a&&x[campo]<b);
      if(R.length) linha(`   ${i?a:'<'+(i<cortes.length?b:'')}${i?(b===Infinity?'+':'–'+b):''} ${un}`, `${R.filter(x=>x.selou).length}/${R.length}`, pct(R.filter(x=>x.selou).length/R.length)+' selados', ''); } };
    linha('Tempo restante do ritual','','',''); faixa(DEC.loc,'restante',[10,20,30],'s');
    linha('Caçador mais perto (linha reta)','','',''); faixa(DEC.loc.filter(x=>x.dist>=0),'dist',[30,60,100],'m');
    const nv=DEC.loc.filter(x=>x.dist<0); if(nv.length) linha('   nenhum Caçador de pé', `${nv.filter(x=>x.selou).length}/${nv.length}`, '', ''); }
  L.push('\nVITÓRIA POR CLASSE (noites em que a classe jogou)'); L.push('-'.repeat(78));
  for(const [k,C] of Object.entries(DEC.classes).sort((a,b)=>b[1].n-a[1].n)) linha(k, `${C.venceu}/${C.n}`, pct(C.venceu/C.n), '');
  L.push('\nRITUAIS POR ALTAR (selados / Fendas)'); L.push('-'.repeat(78));
  for(const [k,X] of Object.entries(DEC.altares).sort()) linha(k, `${X.selou} / ${X.fenda}`, pct(X.selou/(X.selou+X.fenda))+' selados', '');
  L.push('(Ainda não medido: tempo de cada jogador sem nenhuma ação útil.)'); }

const fora=Object.keys(METAS).filter(k=>medido[k]!=null&&status(k,medido[k])!=='na meta');
L.push('\n'+(fora.length?`Fora da meta: ${fora.map(k=>METAS[k][2]).join('; ')}.`:'Todas as métricas principais estão dentro da meta.'));
if(A.partidas<10) L.push(`Atenção: ${A.partidas} ${A.partidas===1?'partida é':'partidas são'} pouca amostra. Trate como indício, não como conclusão.`);
L.push('');
console.log(L.join('\n'));

if(csvOut){
  const linhas=[['metrica','medido','meta_min','meta_max','situacao']];
  for(const k of Object.keys(METAS)) linhas.push([METAS[k][2],medido[k]??'',METAS[k][0],METAS[k][1],status(k,medido[k])]);
  fs.writeFileSync(csvOut,linhas.map(r=>r.join(',')).join('\n')); console.log(`CSV salvo em ${csvOut}\n`);
}
