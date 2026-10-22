#!/usr/bin/env node
// Teste de fumaça das telas do Unity sem o Unity: compila os scripts do jogo contra o UnityEngine e o IMGUI de mentira
// (tools/cs/UnityFalso.cs e IMGUIFalso.cs) e roda o roteiro de tools/cs/TesteTelas.cs: título, menus, uma noite inteira
// de cada lado, resumo, resultado, relatório, replay, perfil, tutorial, desafio e Arsenal, com as duas passadas do IMGUI
// a cada quadro. Falha com exceção ou com grupo de layout (Begin/End) mal fechado.
// Uso: node tools/unity-telas.js
'use strict';
const fs=require('fs'), path=require('path'), os=require('os'), {execFileSync}=require('child_process');
const ROOT=path.join(__dirname,'..'), UN=path.join(ROOT,'unity','Assets','RitualReversal'), CS=path.join(__dirname,'cs'), S=path.join(UN,'Scripts');
const fora=/^(JogadorFPS|GerarTextura|MapaDados|OlharParaCamera)\.cs$/;
const arqs=[...fs.readdirSync(S).filter(f=>f.endsWith('.cs')&&!fora.test(f)).map(f=>path.join(S,f)),
  ...fs.readdirSync(path.join(S,'Visual')).filter(f=>f.endsWith('.cs')&&f!=='Pos.cs').map(f=>path.join(S,'Visual',f)),
  ...fs.readdirSync(path.join(S,'Simulacao')).filter(f=>f.endsWith('.cs')).map(f=>path.join(S,'Simulacao',f))];
const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'unity-telas-')), EXE=path.join(tmp,'telas.exe');
try{ execFileSync('mcs',['-nowarn:414,618,169,649,219,162,168,1717','-out:'+EXE,'-main:RitualReversal.PartidaLocal',path.join(CS,'UnityFalso.cs'),path.join(CS,'IMGUIFalso.cs'),path.join(CS,'TesteTelas.cs'),...arqs],{stdio:['ignore','pipe','pipe']}); }
catch(e){ console.error('as telas não compilaram contra o Unity de mentira:\n'+String(e.stdout||'')+String(e.stderr||'')); process.exit(2); }
const t0=Date.now();
try{ execFileSync('mono',[EXE,path.join(UN,'Resources')],{stdio:['ignore','inherit','inherit'],cwd:tmp,timeout:1500000}); console.log('TELAS SEM ERRO ('+((Date.now()-t0)/1000).toFixed(0)+' s)'); }
catch(e){ console.error('PROBLEMAS nas telas (acima)'); process.exit(1); }
