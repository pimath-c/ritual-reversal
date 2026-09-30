#!/usr/bin/env node
// Posta um arquivo de patch notes (Markdown simples) num canal do Discord por webhook.
// "# Título" vira o título, o parágrafo seguinte a descrição, e cada "## Seção" um campo do cartão.
// Uso: DISCORD_WEBHOOK=https://discord.com/api/webhooks/... node tools/postar-discord.js notas/v5.md [--testar]
// --testar só mostra o que seria enviado. O link do webhook nunca fica no repositório.
'use strict';
const fs=require('fs');
const arq=process.argv[2], testar=process.argv.includes('--testar'), url=process.env.DISCORD_WEBHOOK;
if(!arq) throw new Error('diga qual arquivo de notas postar');
const linhas=fs.readFileSync(arq,'utf8').split('\n');
let titulo='', desc=[], campos=[], atual=null;
for(const l of linhas){
  if(l.startsWith('# ')) titulo=l.slice(2).trim();
  else if(l.startsWith('## ')){ atual={name:l.slice(3).trim(),value:[]}; campos.push(atual); }
  else if(atual) { if(l.trim()) atual.value.push(l.replace(/^- /,'• ')); }
  else if(l.trim()) desc.push(l.trim());
}
const embed={title:titulo.slice(0,256),description:desc.join('\n').slice(0,4096),color:0xa3203a,
  fields:campos.map(c=>({name:c.name.slice(0,256),value:c.value.join('\n').slice(0,1024)})),
  footer:{text:'Ritual Reversal'},timestamp:new Date().toISOString()};
const total=embed.title.length+embed.description.length+embed.fields.reduce((s,f)=>s+f.name.length+f.value.length,0);
if(embed.fields.length>25||total>6000) throw new Error(`cartão grande demais para o Discord (${embed.fields.length} seções, ${total} caracteres)`);
const corpo={username:'Ritual Reversal',embeds:[embed]};
if(testar||!url){ console.log(JSON.stringify(corpo,null,2)); console.log(`\n${total} caracteres, ${embed.fields.length} seções`+(url?'':' (sem DISCORD_WEBHOOK: nada enviado)')); process.exit(0); }
fetch(url+(url.includes('?')?'&':'?')+'wait=true',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(corpo)})
  .then(async r=>{ if(!r.ok) throw new Error(`Discord respondeu ${r.status}: ${await r.text()}`); console.log('postado no Discord'); })
  .catch(e=>{ console.error(e.message); process.exit(1); });
