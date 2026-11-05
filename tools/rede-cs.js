#!/usr/bin/env node
// Testa o cliente online do Unity (unity/Assets/RitualReversal/Scripts/Rede.cs) contra o servidor de verdade:
// sobe server/server.js numa porta livre, compila tools/cs/TesteRede.cs com o Mono e joga um pedaço de partida
// com dois clientes (sala, lobby, código, fases, entradas, snapshots, previsão, reconexão).
// Precisa do Mono (mcs e mono) e de npm install em server/. Uso: node tools/rede-cs.js
'use strict';
const fs=require('fs'), path=require('path'), os=require('os'), {execFileSync,spawn}=require('child_process');
const RAIZ=path.join(__dirname,'..'), U=path.join(RAIZ,'unity','Assets','RitualReversal');
const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'rr-rede-')), exe=path.join(tmp,'teste.exe');
const sim=fs.readdirSync(path.join(U,'Scripts','Simulacao')).filter(f=>f.endsWith('.cs')).map(f=>path.join(U,'Scripts','Simulacao',f));
execFileSync('mcs',['-nowarn:219','-out:'+exe,path.join(U,'Scripts','Rede.cs'),...sim,path.join(__dirname,'cs','TesteRede.cs')],{stdio:'inherit'});
const porta=String(8700+Math.floor(Math.random()*200));
const srv=spawn(process.execPath,[path.join(RAIZ,'server','server.js')],{env:Object.assign({},process.env,{PORT:porta}),stdio:'ignore'});
setTimeout(()=>{
  let codigo=1;
  try{ execFileSync('mono',[exe,path.join(U,'Resources','simulacao.json'),porta],{stdio:'inherit',timeout:120000,env:Object.assign({},process.env,{LANG:'C.UTF-8'})}); codigo=0; }
  catch(e){ codigo=1; }
  srv.kill(); fs.rmSync(tmp,{recursive:true,force:true}); process.exit(codigo);
},1500);
