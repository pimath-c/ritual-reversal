#!/usr/bin/env node
// As telas do navegador com os mesmos nomes das fotos do Unity (tools/unity-telas.js com FOTOS=), para pôr lado a lado:
// título, laboratório, menus, entrada, HUD, pausa, ajustes da pausa, caderno, frases, planta, resumo, resultado,
// relatório, Rever a noite, escolha dos altares, HUD do culto, tutorial e Arsenal, em 1920×1080, com as fontes do jogo
// (IM Fell English, Barlow Condensed, Big Shoulders Display e IBM Plex) servidas de unity/Assets/RitualReversal/Resources/Fontes no lugar do Google Fonts.
// Uso: THREE_DIR=<three@0.128.0> NODE_PATH=$(npm root -g) node tools/telas-navegador.js [pasta] [regex]
'use strict';
const fs=require('fs'), path=require('path'), os=require('os');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), OUT=path.resolve(process.argv[2]||path.join(ROOT,'dist','telas','navegador')), FILTRO=new RegExp(process.argv[3]||'.');
const THREE_DIR=process.env.THREE_DIR, FONTES=path.join(ROOT,'unity','Assets','RitualReversal','Resources','Fontes');
fs.mkdirSync(OUT,{recursive:true});
const CSS_FONTES=[['IM Fell English','normal',400,'IMFellEnglish-Regular'],['IM Fell English','italic',400,'IMFellEnglish-Italic'],
  ...[[400,'Regular'],[500,'Medium'],[600,'SemiBold'],[700,'Bold']].map(([w,n])=>['Barlow Condensed','normal',w,'BarlowCondensed-'+n]),
  ...[[400,'Regular'],[800,'ExtraBold'],[900,'Black']].map(([w,n])=>['Big Shoulders Display','normal',w,'BigShouldersDisplay-'+n]),   // v6.3, direção Nanquim
  ...[[400,'Regular'],[500,'Medium']].map(([w,n])=>['IBM Plex Mono','normal',w,'IBMPlexMono-'+n]),
  ...[[400,'Regular'],[500,'Medium'],[600,'SemiBold'],[700,'Bold']].map(([w,n])=>['IBM Plex Sans Condensed','normal',w,'IBMPlexSansCondensed-'+n])]
  .map(([f,s,w,a])=>`@font-face{font-family:'${f}';font-style:${s};font-weight:${w};src:url(https://fontes.local/${a}.ttf) format('truetype');}`).join('\n');
(async()=>{
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  // CELULAR=1: um celular deitado (914 × 411 pixels de CSS a 2,625×, como o roteiro de celular do Unity) com o toque ligado
  const CEL=!!process.env.CELULAR;
  const page=await browser.newPage(CEL?{viewport:{width:914,height:411},deviceScaleFactor:2.625,isMobile:true,hasTouch:true}:{viewport:{width:1920,height:1080}});
  // a interface é o que importa aqui: o mundo vai na qualidade Mínimo, que o swiftshader desenha a tempo
  await page.addInitScript(c=>{ try{ if(!localStorage.getItem('rr-cfg')) localStorage.setItem('rr-cfg',JSON.stringify(Object.assign({qual:'minimo',autoRes:false},c?{toque:true}:{}))); }catch(e){} },CEL);
  page.setDefaultTimeout(120000);
  const erros=[];
  let ultimaFoto='nada'; page.on('pageerror',e=>erros.push('pageerror (depois de '+ultimaFoto+'): '+e.message+' '+String(e.stack||'').split('\n').slice(1,3).join(' | ')));
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
  if(CEL){ await roteiroCelular(page,ev=>page.evaluate(s=>__T.ev(s),ev),OUT,erros); await browser.close(); console.log(erros.length?'ERROS:\n  '+erros.join('\n  '):'celular sem erro'); process.exit(erros.length?1:0); }
  const fotos=[];
  const foto=async(nome,espera=600)=>{ ultimaFoto=nome; await page.waitForTimeout(espera); if(!FILTRO.test(nome)) return;   // espera mesmo a que não fotografa: a tela seguinte depende dela
  await page.screenshot({path:path.join(OUT,nome+'.png')}); fotos.push(nome); };
  const ev=s=>page.evaluate(s=>__T.ev(s),s);
  // MEDIR=1: as caixas (getBoundingClientRect) das peças de cada tela, para conferir o layout do Unity número a número
  const medir=async(nome,sels)=>{ if(!process.env.MEDIR||!FILTRO.test(nome)) return; const r=await page.evaluate(ss=>ss.map(s=>[...document.querySelectorAll(s)].slice(0,8).map(e=>{ const b=e.getBoundingClientRect(); return s+' '+[b.x,b.y,b.width,b.height].map(v=>Math.round(v*10)/10).join(','); })).flat(),sels); console.log('medidas '+nome+':\n  '+r.join('\n  ')); };
  // título e os menus (as telas do menu são as mesmas variáveis que os botões ligam)
  await foto('titulo',1500);
  await page.evaluate(()=>{ const d=document.querySelector('details.lab'); if(d) d.open=true; }); await foto('titulo-laboratorio');
  await page.evaluate(()=>{ const d=document.querySelector('details.lab'); if(d) d.open=false; });
  for(const [nome,v] of [['menu-aprender','aprenderAberto'],['menu-como','comoJogarAberto'],['menu-perfil','perfilAberto'],['menu-novidades','novidadesAberto'],['menu-ajustes','settingsOpen']]){
    await ev(v+"=true; screenKey=''"); await foto(nome); await medir(nome,['#scrBody','#scrBody h2','#scrBody .lede','#scrBody h3','#scrBody table','#scrBody table tr','#scrBody th','.conqs','.cq','.btnrow']); await ev(v+"=false; screenKey=''"); }
  // uma noite como Caçador
  await page.evaluate(()=>{ __T.startLocal({papel:'H'}); });
  await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000}); await foto('intro',1200);
  await page.evaluate(()=>{ __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  // sem o mouse capturado o jogo mostra a pausa: para o HUD, finge o mouse preso
  await ev("locked=true; paused=false; screenKey=''; scr.classList.remove('on'); for(let i=0;i<60;i++) Sim.step(game,1/30)");
  await foto('hud',2500);
  // as legendas dos sons, iguais às do roteiro do Unity: rumo zerado e quatro sons em volta da câmera
  await ev("LOOK.yaw=0; { const P=camera.position; legenda('shot',P.x+14,P.z-14); legenda('corvo',P.x-6,P.z+8); legenda('collect',P.x+1,P.z+1); legenda('presage',null,null,'Presságio: um ritual vai começar','rit'); }");
  await foto('legendas',700); await ev("limparLegendas()");
  // a vitrine das animações (F9): o painel do arma.js na recarga vazia da carabina
  await ev("ARMA.painel(true)"); await foto('vitrine',900); await medir('vitrine',['#vitrineArma','#vitrineArma > b','#vitrineArma > span','#vitrineArma > div','#vitrineArma button','#vitR','#vitLoop','#vitT']); await ev("ARMA.painel()");
  await ev("locked=false; paused=true; screenKey=''"); await foto('pausa',900); await medir('pausa',['#scrBody','#scrBody h2','#scrBody p','.cola','.cola span','.cola kbd','.btnrow']);
  await ev("openSettings()"); await foto('pausa-ajustes',900);
  await ev("settingsOpen=false; locked=true; paused=false; screenKey=''; scr.classList.remove('on'); CAD.aberto=true"); await foto('caderno',1200);
  await ev("CAD.aberto=false; FL.aberto=true; FL.t=4; desenharFalas()"); await foto('falas',900);
  await ev("FL.aberto=false; abrirChatJogo(); $('#iChatJogo').value='estou indo'"); await foto('chat-jogo',800); await medir('chat-jogo',['#chatJogo','#iChatJogo']);
  await ev("fecharChatJogo(); { const n=NPCS.find(x=>x.id==='ermitao'), m=me(); m.x=n.x+1; m.z=n.z; m.obolos=5; } lojaAberta='ermitao'; locked=false; screenKey=''"); await foto('loja',1200); await medir('loja',['#scrBody','#scrBody h2','#scrBody .lede','.loja','.loja .item','.loja .item b','.loja .item span','.loja .item button','.btnrow']);
  await ev("lojaAberta=null; locked=true; screenKey=''; scr.classList.remove('on')");
  await ev("FL.aberto=false; $('#tabMap').style.display='grid'"); await foto('planta',1200);
  await ev("$('#tabMap').style.display='none'; for(let i=0;i<1500&&game.phase==='play';i++) Sim.step(game,.05)"); await foto('hud-depois',1500);
  // a noite acelerada até o resumo, o resultado, o relatório e o Rever a noite
  // amostra a noite como o laço do jogo (amostrarNoite), para o Rever a noite ter as posições
  for(let k=0;k<60;k++){ const ph=await ev("for(let i=0;i<900&&game.phase==='play';i++){ Sim.step(game,1/30); if(i%15===0) amostrarNoite(); } game.phase"); await page.waitForTimeout(300); if(ph!=='play') break; }
  await ev("locked=false; screenKey=''");
  if(await page.evaluate(()=>__T.game.phase==='summary')){
    await foto('resumo',1500);
    await page.evaluate(()=>{ __T.sendAct({type:'ready'}); }); await page.waitForFunction(()=>__T.game.phase==='final',null,{timeout:30000}); await foto('resultado',2500);
    if(await page.waitForSelector('#bRel',{timeout:60000}).catch(()=>null)){ await page.click('#bRel'); await foto('relatorio',1500); await page.evaluate(()=>{ document.querySelector('#scr').scrollTop=0; }); await foto('relatorio-topo',300); await page.evaluate(()=>{ const b=[...document.querySelectorAll('#scrBody button')].find(b=>/Voltar/.test(b.textContent)); if(b) b.click(); }); await page.waitForTimeout(600); }
    const rep=await page.waitForSelector('#bRep1,#bRep',{timeout:30000}).catch(()=>null); if(rep){ await rep.click(); await page.waitForTimeout(800); await page.evaluate(()=>{ for(const b of document.querySelectorAll('.repCtl .opts button')) if(/8x|8×/.test(b.textContent)) b.click(); }); await foto('replay',5000); await medir('replay',['#scrBody','#scrBody h2','#scrBody .lede','#repSvg','.repCtl','.repLeg','.repLeg span','#repEv','#repEv li','.btnrow']); }
  } else erros.push('a noite acelerada não chegou ao resumo');
  // uma noite como Cultista: a escolha dos altares e o HUD do culto
  await ev("locked=false; startLocal({papel:'C'}); screenKey=''");
  await page.waitForFunction(()=>__T.game&&__T.game.phase==='pick',null,{timeout:30000}); await foto('escolha',1500);
  await page.evaluate(()=>{ __T.sendAct({type:'pickRandom'}); __T.sendAct({type:'pickConfirm'}); });
  await page.waitForFunction(()=>__T.game.phase==='intro',null,{timeout:30000}); await page.evaluate(()=>{ __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await ev("locked=true; paused=false; screenKey=''; scr.classList.remove('on'); for(let i=0;i<200;i++) Sim.step(game,.05)"); await foto('hud-culto',2500);
  // o tutorial
  await ev("locked=false; startLocal({papel:'H',dificuldade:'treino',tutorial:true}); screenKey=''");
  await page.waitForFunction(()=>__T.game&&__T.game.phase!=='pick',null,{timeout:30000});
  await page.evaluate(()=>{ if(__T.game.phase==='intro') __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await ev("locked=true; paused=false; screenKey=''; scr.classList.remove('on'); for(let i=0;i<100;i++) Sim.step(game,.05)"); await foto('tutorial',2500);
  // a sala online e a reconexão, com o lobby que o servidor mandaria (o anfitrião sozinho, três bots, nada no chat)
  await ev("locked=false; game=null; V=null; MODE='online'; meCid='c1'; NET.code='KQXT'; NET.chat=[]; NET.lobby={t:'lobby',code:'KQXT',host:'c1',inGame:false,publica:false,dif:'normal',lab:null,slots:[0,1,2,3].map(i=>({team:i<2?'A':'B',name:i===0?'Teste':Sim.BOT_NAMES[i<2?'A':'B'][i%2],cid:i===0?'c1':null}))}; screenKey=''");
  await foto('lobby',1500); await medir('lobby',['#scrBody','#scrBody h2','.bigcode','#scrBody .lede','.slots','.slot','#scrBody .fine','.difOnline','details.lab','#chatLista','.chat .linha','#iChat','#bChat','.btnrow']);
  await ev("RECON.ativo=true; RECON.ate=performance.now()+58000; RECON.tentativas=0; screenKey=''"); await foto('reconexao',1200); await medir('reconexao',['#scrBody','#scrBody h2','#scrBody p','.btnrow']);
  await ev("RECON.ativo=false; MODE=null; NET.lobby=null; screenKey=''");
  // o Arsenal
  await ev("locked=false; sairParaMenu()"); await page.waitForTimeout(800); await ev("startArsenal()"); await foto('arsenal',3000);   // pelo menu, como o jogador await medir('arsenal',['#arsenal','#arsenal h2','#arsenal .col','#arsenal .txt','#arsenal .abas','#arsenal .abas button','#arsenal .skin','#arsenal .skin .of','#arsenal .skin b','#arsenal .skin p','#arsenal .acoes','#arsenal .rot','#arsenal .sair']);
  console.log(fotos.length+' telas em '+OUT+': '+fotos.join(', '));
  if(erros.length){ console.log('ERROS:'); [...new Set(erros)].slice(0,20).forEach(e=>console.log(' ',e)); }
  await browser.close(); process.exit(erros.length?1:0);
})().catch(e=>{ console.error(e); process.exit(1); });
// a noite de Caçador no celular, como o RoteiroCelular do Unity: o dedo empurra o direcional (fica na foto), depois a Pausa
async function roteiroCelular(page,ev,OUT,erros){
  await page.evaluate(()=>{ __T.startLocal({papel:'H',dificuldade:'facil'}); });
  await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000}); await page.evaluate(()=>{ __T.sendAct({type:'ready'}); });
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  await ev("locked=true; paused=false; screenKey=''; scr.classList.remove('on'); for(let i=0;i<20;i++) Sim.step(game,.05)"); await page.waitForTimeout(800);
  // o dedo: encosta em (150, 300) e sobe 50 px
  await page.touchscreen.tap; const cdp=await page.context().newCDPSession(page);
  await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:150,y:300,id:1}]}); await page.waitForTimeout(100);
  await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:150,y:250,id:1}]}); await page.waitForTimeout(1500);
  await page.screenshot({path:require('path').join(OUT,'celular.png')});
  const sels=['#top','#strip','#vitals','#kit','#sensor','#tarefas','#feed','#net','#bussola','#tqStick','#tqFire','#tqUse','#tqAbil','#tqReload','#tqLant','#tqSens','#tqFlare','#tqCorre','#tqAgacha','#tqTopo .tq','#legendas','#mira'];
  console.log('medidas celular:\n  '+(await page.evaluate(ss=>ss.map(s=>[...document.querySelectorAll(s)].slice(0,6).map(e=>{ const b=e.getBoundingClientRect(); return s+' '+[b.x,b.y,b.width,b.height].map(v=>Math.round(v*10)/10).join(','); })).flat(),sels)).join('\n  '));
  await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]}); await page.waitForTimeout(200);
  await ev("unlock(); screenKey=''"); await page.waitForTimeout(900); await page.screenshot({path:require('path').join(OUT,'celular-pausa.png')});
  console.log('medidas celular-pausa:\n  '+(await page.evaluate(()=>['#scrBody','#scrBody h2','#scrBody p','.cola','.btnrow'].map(s=>{ const e=document.querySelector(s); if(!e) return s+' -'; const b=e.getBoundingClientRect(); return s+' '+[b.x,b.y,b.width,b.height].map(v=>Math.round(v*10)/10).join(','); }))).join('\n  '));
}
