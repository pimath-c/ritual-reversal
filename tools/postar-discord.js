#!/usr/bin/env node
// Posta um arquivo de patch notes (Markdown simples) num canal do Discord por webhook.
// "# Título" vira o título, o parágrafo seguinte a descrição, e cada "## Seção" um campo do cartão.
// Notas longas viram vários cartões em sequência (o Discord aceita 1024 caracteres por campo e 6000 por mensagem):
// seção grande continua no campo seguinte, cortada no fim de uma linha, nunca no meio de uma frase.
// Uso: DISCORD_WEBHOOK=https://discord.com/api/webhooks/... node tools/postar-discord.js notas/v5.md [--testar]
// --testar só mostra o que seria enviado. O link do webhook nunca fica no repositório.
'use strict';
const fs=require('fs');
const arq=process.argv[2], testar=process.argv.includes('--testar'), url=process.env.DISCORD_WEBHOOK||process.env.DISCORD_WEBHOOK_URL;
if(!arq) throw new Error('diga qual arquivo de notas postar');
const CAMPO=1024, MSG=5800, CAMPOS=25, DESC=4096;

// Markdown do GitHub para o do Discord: tabela vira lista ("a — b — c"), lista aninhada ganha outro marcador
function linhaDiscord(l){
  if(/^\s*\|[\s:|-]+\|\s*$/.test(l)) return null;                       // separador da tabela
  if(/^\s*\|.*\|\s*$/.test(l)) return '• '+l.trim().slice(1,-1).split('|').map(c=>c.trim()).join(' — ');
  return l.replace(/^- /,'• ').replace(/^\s{2,}- /,'  ◦ ');
}
// junta linhas em pedaços de até n caracteres, sem cortar uma linha (uma linha maior que n é cortada numa palavra)
function pedacos(linhas,n){
  const out=[]; let cur='';
  for(let l of linhas){
    while(l.length>n){ const k=l.lastIndexOf(' ',n-1)>n/2?l.lastIndexOf(' ',n-1):n-1; if(cur){ out.push(cur); cur=''; } out.push(l.slice(0,k)+'…'); l='…'+l.slice(k).trimStart(); }
    if(cur&&cur.length+1+l.length>n){ out.push(cur); cur=''; }
    cur=cur?cur+'\n'+l:l;
  }
  if(cur) out.push(cur); return out;
}

// as notas quebram linha a cada 120 colunas: junta a continuação de um parágrafo ou item na linha anterior
const novoItem=l=>/^\s*([-*]|\d+\.|\|)\s/.test(l)||/^\s*\|/.test(l)||l.startsWith('#');
const linhas=[];
for(const l of fs.readFileSync(arq,'utf8').split('\n')){
  const ant=linhas.length?linhas[linhas.length-1]:'';
  if(l.trim()&&ant.trim()&&!novoItem(l)&&!ant.startsWith('#')&&!/^\s*\|/.test(ant)) linhas[linhas.length-1]=ant+' '+l.trim();
  else linhas.push(l);
}
let titulo='', desc=[], secoes=[], atual=null;
for(const l of linhas){
  if(l.startsWith('# ')) titulo=l.slice(2).trim();
  else if(l.startsWith('## ')){ atual={name:l.slice(3).trim(),value:[]}; secoes.push(atual); }
  else if(atual){ const d=linhaDiscord(l); if(d!=null&&d.trim()) atual.value.push(d); }
  else if(l.trim()) desc.push(l.trim());
}
titulo=titulo.slice(0,240);
const campos=[];
for(const s of secoes){ const ps=pedacos(s.value.length?s.value:['—'],CAMPO); ps.forEach((v,i)=>campos.push({name:(i?s.name+' (cont.)':s.name).slice(0,256),value:v})); }
const descricao=pedacos(desc,DESC)[0]||'';

// distribui os campos em cartões; cada cartão vai numa mensagem
const cartoes=[]; let c=null, tam=0;
const novo=()=>{ c={title:titulo,color:0xa3203a,fields:[]}; tam=titulo.length+20; if(!cartoes.length&&descricao){ c.description=descricao; tam+=descricao.length; } cartoes.push(c); };
novo();
for(const f of campos){ const t=f.name.length+f.value.length; if(c.fields.length>=CAMPOS||(c.fields.length&&tam+t>MSG)) novo(); c.fields.push(f); tam+=t; }
cartoes.forEach((k,i)=>{ if(cartoes.length>1) k.title=`${titulo} (${i+1}/${cartoes.length})`; if(i===cartoes.length-1){ k.footer={text:'Ritual Reversal'}; k.timestamp=new Date().toISOString(); } });
const mensagens=cartoes.map(k=>({username:'Ritual Reversal',embeds:[k]}));
for(const m of mensagens){ const k=m.embeds[0], t=k.title.length+(k.description||'').length+k.fields.reduce((s,f)=>s+f.name.length+f.value.length,0);
  if(t>6000||k.fields.some(f=>f.value.length>CAMPO)) throw new Error(`cartão grande demais para o Discord (${t} caracteres)`); }

if(testar||!url){
  console.log(JSON.stringify(mensagens,null,2));
  console.log(`\n${mensagens.length} mensage${mensagens.length>1?'ns':'m'}, ${campos.length} campos`+(url?'':' (sem DISCORD_WEBHOOK: nada enviado)'));
  process.exit(0);
}
(async()=>{
  for(const m of mensagens){
    let r;
    for(let tent=0;tent<4;tent++){                                          // 429: espera o que o Discord pedir e tenta de novo
      r=await fetch(url+(url.includes('?')?'&':'?')+'wait=true',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(m)});
      if(r.status!==429) break; const j=await r.json().catch(()=>({})); await new Promise(f=>setTimeout(f,1000*(j.retry_after||2)));
    }
    if(!r.ok) throw new Error(`Discord respondeu ${r.status}: ${await r.text()}`);
    await new Promise(f=>setTimeout(f,700));                              // em ordem e sem bater no limite do webhook
  }
  console.log(`postado no Discord (${mensagens.length} mensage${mensagens.length>1?'ns':'m'})`);
})().catch(e=>{ console.error(e.message); process.exit(1); });
