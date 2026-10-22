#!/usr/bin/env node
// As telas do navegador com os mesmos nomes das fotos do Unity (tools/unity-telas.js com FOTOS=), para pôr lado a lado:
// título, laboratório, menus, entrada, HUD, pausa, ajustes da pausa, caderno, frases, planta, resumo, resultado,
// relatório, Rever a noite, escolha dos altares, HUD do culto, tutorial e Arsenal, em 1920×1080, com as fontes do jogo
// (IM Fell English e Barlow Condensed) servidas de unity/Assets/RitualReversal/Resources/Fontes no lugar do Google Fonts.
// Uso: THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/telas-navegador.js [pasta] [regex]
'use strict';
const fs=require('fs'), path=require('path'), os=require('os');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), OUT=path.resolve(process.argv[2]||path.join(ROOT,'dist','telas','navegador')), FILTRO=new RegExp(process.argv[3]||'.');
const THREE_DIR=process.env.THREE_DIR, FONTES=path.join(ROOT,'unity','Assets','RitualReversal','Resources','Fontes');
fs.mkdirSync(OUT,{recursive:true});
const CSS_FONTES=[['IM Fell English','normal',400,'IMFellEnglish-Regular'],['IM Fell English','italic',400,'IMFellEnglish-Italic'],
  ...[[400,'Regular'],[500,'Medium'],[600,'SemiBold'],[700,'Bold']].map(([w,n])=>['Barlow Condensed','normal',w,'BarlowCondensed-'+n])]
  .map(([f,s,w,a])=>`@font-face{font-family:'${f}';font-style:${s};font-weight:${w};src:url(https://fontes.local/${a}.ttf) format('truetype');}`).join('\n');
(async()=>{
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const page=await browser.newPage({viewport:{width:1920,height:1080}});
  // a interface é o que importa aqui: o mundo vai na qualidade Mínimo, que o swiftshader desenha a tempo
  await page.addInitScript(()=>{ try{ if(!localStorage.getItem('rr-cfg')) localStorage.setItem('rr-cfg',JSON.stringify({qual:'minimo',autoRes:false})); }catch(e){} });
  page.setDefaultTimeout(120000);
  const erros=[];
  page.on('pageerror',e=>erros.push('pageerror: '+e.message));
  await page.route('**/*',r=>{ const u=r.request().url();
    if(THREE_DIR&&u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')){ const rel=u.split('three@0.128.0/')[1]; return r.fulfill({path:path.join(THREE_DIR,rel),contentType:'application/javascript'}); }
    if(u.includes('fonts.googleapis.com')) return r.fulfill({body:CSS_FONTES,contentType:'text/css'});
    if(u.startsWith('https://fontes.local/')) return r.fulfill({path:path.join(FONTES,u.slice(21)),contentType:'font/ttf'});
    if(u.startsWith('file:')) return r.continue();
    if(u.includes('fonts.g')) return r.abort();
    return r.continue(); });
  // o jogo vive numa função: um eval dentro dela (só nesta cópia da página) alcança o estado das telas
  const html=fs.readFileSync(process.env.HTML||path.join(ROOT,'dist','ritual-reversal.html'),'utf8'), PAG=path.join(fs.mkdtempSync(path.join(os.tmpdir(),'telas-')),'pagina.html');
  if(!html.includes('window.__T={')) throw new Error('a página não tem o __T do autotest');
  fs.writeFileSync(PAG,html.replace('window.__T={','window.__T={ev:s=>eval(s),'));
  await page.goto('file://'+PAG+'#autotest');
  await page.waitForFunction(()=>window.__T,null,{timeout:120000});
  await page.evaluate(()=>document.fonts.ready);
  const fotos=[];
  const foto=async(nome,espera=600)=>{ if(!FILTRO.test(nome)) return; await page.waitForTimeout(espera); await page.screenshot({path:path.join(OUT,nome+'.png')}); fotos.push(nome); };
  const ev=s=>page.evaluate(s=>__T.ev(s),s);
  // título e os menus (as telas do menu são as mesmas variáveis que os botões ligam)
  await foto('titulo',1500);
  await page.evaluate(()=>{ const d=document.querySelector('details.lab'); if(d) d.open=true; }); await foto('titulo-laboratorio');
  await page.evaluate(()=>{ const d=document.querySelector('details.lab'); if(d) d.open=false; });
  for(const [nome,v] of [['menu-aprender','aprenderAberto'],['menu-como','comoJogarAberto'],['menu-perfil','perfilAberto'],['menu-novidades','novidadesAberto'],['menu-ajustes','settingsOpen']]){
    await ev(v+"=true; screenKey=''"); await foto(nome); await ev(v+"=false; screenKey=''"); }
  // uma noite como Caçador
  await page.evaluate(()=>{ __T.startLocal({papel:'H'}); });
  await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000}); await foto('intro',1200);
  await page.evaluate(()=>{ __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  // sem o mouse capturado o jogo mostra a pausa: para o HUD, finge o mouse preso
  await ev("locked=true; paused=false; screenKey=''; for(let i=0;i<60;i++) Sim.step(game,1/30)");
  await foto('hud',2500);
  await ev("locked=false; paused=true; screenKey=''"); await foto('pausa',900);
  await ev("openSettings()"); await foto('pausa-ajustes',900);
  await ev("settingsOpen=false; locked=true; paused=false; screenKey=''; CAD.aberto=true"); await foto('caderno',1200);
  await ev("CAD.aberto=false; FL.aberto=true; FL.t=4"); await foto('falas',900);
  await ev("FL.aberto=false; $('#tabMap').style.display='grid'"); await foto('planta',1200);
  await ev("$('#tabMap').style.display='none'; for(let i=0;i<6000&&game.phase==='play';i++) Sim.step(game,.05)"); await foto('hud-depois',1500);
  // a noite acelerada até o resumo, o resultado, o relatório e o Rever a noite
  for(let k=0;k<60;k++){ const ph=await page.evaluate(()=>{ const g=__T.game; if(g.phase==='play') for(let i=0;i<900&&g.phase==='play';i++) __T.Sim.step(g,1/30); return g.phase; }); await page.waitForTimeout(300); if(ph!=='play') break; }
  await ev("locked=false; screenKey=''");
  if(await page.evaluate(()=>__T.game.phase==='summary')){
    await foto('resumo',1500);
    await page.evaluate(()=>{ __T.sendAct({type:'ready'}); }); await page.waitForFunction(()=>__T.game.phase==='final',null,{timeout:30000}); await foto('resultado',2500);
    if(await page.$('#bRel')){ await page.click('#bRel'); await foto('relatorio',1500); await page.evaluate(()=>{ const b=[...document.querySelectorAll('#scrBody button')].find(b=>/Voltar/.test(b.textContent)); if(b) b.click(); }); await page.waitForTimeout(600); }
    const rep=await page.$('#bRep'); if(rep){ await rep.click(); await page.waitForTimeout(800); await page.evaluate(()=>{ for(const b of document.querySelectorAll('.repCtl .opts button')) if(/8x|8×/.test(b.textContent)) b.click(); }); await foto('replay',5000); }
  } else erros.push('a noite acelerada não chegou ao resumo');
  // uma noite como Cultista: a escolha dos altares e o HUD do culto
  await ev("locked=false; startLocal({papel:'C'}); screenKey=''");
  await page.waitForFunction(()=>__T.game&&__T.game.phase==='pick',null,{timeout:30000}); await foto('escolha',1500);
  await page.evaluate(()=>{ __T.sendAct({type:'pickRandom'}); __T.sendAct({type:'pickConfirm'}); });
  await page.waitForFunction(()=>__T.game.phase==='intro',null,{timeout:30000}); await page.evaluate(()=>{ __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await ev("locked=true; paused=false; screenKey=''; for(let i=0;i<200;i++) Sim.step(game,.05)"); await foto('hud-culto',2500);
  // o tutorial
  await ev("locked=false; startLocal({papel:'H',dificuldade:'treino',tutorial:true}); screenKey=''");
  await page.waitForFunction(()=>__T.game&&__T.game.phase!=='pick',null,{timeout:30000});
  await page.evaluate(()=>{ if(__T.game.phase==='intro') __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await ev("locked=true; paused=false; screenKey=''; for(let i=0;i<100;i++) Sim.step(game,.05)"); await foto('tutorial',2500);
  // o Arsenal
  await ev("locked=false; startArsenal()"); await foto('arsenal',3000);
  console.log(fotos.length+' telas em '+OUT+': '+fotos.join(', '));
  if(erros.length){ console.log('ERROS:'); [...new Set(erros)].slice(0,20).forEach(e=>console.log(' ',e)); }
  await browser.close(); process.exit(erros.length?1:0);
})().catch(e=>{ console.error(e); process.exit(1); });
