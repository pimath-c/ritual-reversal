#!/usr/bin/env node
// Vitrine das animações da primeira pessoa: abre o jogo no Chromium, fixa a arma e um clipe (client/arma.js) e fotografa
// o clipe quadro a quadro, da câmera do jogador e de lado; monta folhas de quadros para conferir mãos, contato e
// atravessamento sem jogar uma partida.
// Uso: THREE_DIR=/caminho/node_modules/three NODE_PATH=$(npm root -g) node tools/vitrine.js [pasta] [filtro]
//   filtro: expressão regular sobre o nome das sequências (ex.: "carabina-recarga|sacar"); sem filtro, todas.
'use strict';
const fs=require('fs'), path=require('path');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), OUT=process.argv[2]||path.join(ROOT,'dist','vitrine'), FILTRO=process.argv[3]||'';
const THREE_DIR=process.env.THREE_DIR, N=+process.env.QUADROS||16;
const SEQ=[]; const VISTAS_ENV=process.env.VISTAS?process.env.VISTAS.split(','):null;
const add=(nome,o,quadros,vistas)=>SEQ.push({nome,o,quadros:quadros===1?1:(+process.env.QUADROS||quadros||N),vistas:VISTAS_ENV||vistas||['jogador','lado']});
add('carabina-pronta',{arma:'carabina',clipe:'pronta'},1,['jogador','lado','cima','frente','direita','perto']);
add('carabina-recarga-vazia',{arma:'carabina',clipe:'recarga',vazia:true});
add('carabina-recarga-tatica',{arma:'carabina',clipe:'recarga',vazia:false,n:3,carga:5});
add('carabina-sacar',{arma:'carabina',clipe:'sacar'},10);
add('carabina-inspecao',{arma:'carabina',clipe:'inspecao'},12);
add('carabina-ajuste',{arma:'carabina',clipe:'ajuste'},6,['jogador']);
add('carabina-corrida',{arma:'carabina',clipe:'corrida'},1,['jogador','lado']);
add('carabina-selar',{arma:'carabina',clipe:'selar'},1,['jogador','lado']);
add('revolver-pronta',{arma:'revolver',clipe:'pronta'},1,['jogador','lado','cima','frente','direita','perto']);
add('revolver-recarga-vazia',{arma:'revolver',clipe:'recarga',vazia:true});
add('revolver-recarga-tatica',{arma:'revolver',clipe:'recarga',vazia:false,carga:5});
add('revolver-sacar',{arma:'revolver',clipe:'sacar'},10);
add('revolver-inspecao',{arma:'revolver',clipe:'inspecao'},12);
add('revolver-corrida',{arma:'revolver',clipe:'corrida'},1,['jogador','lado']);
add('cetro-pronta',{arma:'cetro',clipe:'pronta',fervor:100},1,['jogador','lado','frente','perto']);
add('cetro-sacar',{arma:'cetro',clipe:'sacar',fervor:100},10);
add('cetro-inspecao',{arma:'cetro',clipe:'inspecao',fervor:100},10);
add('cetro-canalizar',{arma:'cetro',clipe:'canalizar',fervor:100},1,['jogador','lado']);
add('cetro-corrida',{arma:'cetro',clipe:'corrida',fervor:100},1,['jogador']);
fs.mkdirSync(OUT,{recursive:true});
(async()=>{
  const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
  const page=await browser.newPage({viewport:{width:1280,height:720}});
  const erros=[]; page.on('pageerror',e=>erros.push('pageerror: '+e.message)); page.on('console',m=>{ if(m.type()==='error'&&!/api\/info|ERR_FAILED/.test(m.text())) erros.push('console: '+m.text()); });
  await page.route('**/*',r=>{ const u=r.request().url();
    if(THREE_DIR&&u.includes('cdn.jsdelivr.net/npm/three@0.128.0/')) return r.fulfill({path:path.join(THREE_DIR,u.split('three@0.128.0/')[1]),contentType:'application/javascript'});
    if(u.startsWith('file:')) return r.continue(); if(u.includes('fonts.g')) return r.abort(); return r.continue(); });
  await page.goto('file://'+(process.env.HTML||path.join(ROOT,'dist','ritual-reversal.html'))+'#autotest');
  await page.waitForFunction(()=>window.__T,null,{timeout:120000});
  await page.addStyleTag({content:'#scr,#hud,#feed,#legendas,#bussola,#anuncio,#lance{display:none!important}'});
  await page.evaluate(()=>{ __T.SET.qual=__T.SET.qual||'alto'; __T.startLocal(); });
  await page.waitForFunction(()=>__T.game&&__T.game.phase==='intro',null,{timeout:30000});
  await page.evaluate(()=>__T.sendAct({type:'ready'}));
  await page.waitForFunction(()=>__T.game.phase==='play',null,{timeout:30000});
  // um lugar com luz de velas e uma parede ao fundo; ninguém por perto
  const lugar={x:+process.env.VX||-4,z:+process.env.VZ||52,yaw:+process.env.VYAW||.4,pitch:+process.env.VPITCH||-.05};
  const fixar=()=>page.evaluate(L=>{ const m=__T.game.actors.find(a=>a.cid==='local'); Object.assign(m,{x:L.x,z:L.z,invuln:999,hp:m.maxHp,lantern:true}); m.inp.yaw=L.yaw; m.inp.pitch=L.pitch; __T.LOOK.yaw=L.yaw; __T.LOOK.pitch=L.pitch;
    for(const a of __T.game.actors) if(a!==m){ a.x=a.team==='H'?-125:125; a.z=a.team==='H'?-8:8; } },lugar);
  await fixar(); await page.waitForTimeout(1200);
  if(process.env.ESTUDIO) await page.evaluate(()=>{ window.__ESTUDIO=true; });
  const quadro=()=>page.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(()=>r()))));
  const pg2=await browser.newPage(); let nFolhas=0;
  // folha de quadros (gravada logo que o clipe termina): até 3 colunas, com o instante de cada quadro
  async function gravarFolha(F){ const cols=Math.min(+process.env.COLUNAS||3,F.imgs.length), lin=Math.ceil(F.imgs.length/cols), w=F.imgs.length===1?1280:640, h=Math.round(w*(F.nome.endsWith('-jogador')&&!process.env.INTEIRO?470/980:9/16));
    const b64=await pg2.evaluate(async([F,cols,lin,w,h])=>{ const c=document.createElement('canvas'); c.width=cols*w; c.height=lin*h; const x=c.getContext('2d'); x.fillStyle='#000'; x.fillRect(0,0,c.width,c.height);
      for(let i=0;i<F.imgs.length;i++){ const im=new Image(); im.src='data:image/png;base64,'+F.imgs[i].png; await im.decode(); const cx=(i%cols)*w, cy=Math.floor(i/cols)*h; x.drawImage(im,cx,cy,w,h);
        x.fillStyle='rgba(0,0,0,.6)'; x.fillRect(cx,cy,92,26); x.fillStyle='#fff'; x.font='18px monospace'; x.fillText(F.imgs[i].t+' s',cx+6,cy+19); }
      return c.toDataURL('image/png').split(',')[1]; },[F,cols,lin,w,h]);
    fs.writeFileSync(path.join(OUT,F.nome+'.png'),Buffer.from(b64,'base64')); }
  for(const S of SEQ){ if(FILTRO&&!new RegExp(FILTRO).test(S.nome)) continue;
    for(const vista of S.vistas){ const imgs=[];
      for(let i=0;i<S.quadros;i++){ const t=S.quadros===1?0:i/(S.quadros-1);
        await fixar();
        await page.evaluate(([o,t,v])=>{ const A=__T.ARMA; if(!A.S.forca||A.S.forca.arma!==o.arma||A.S.forca.clipe!==o.clipe||A.S.forca.vazia!==o.vazia||A.S.forca.vista!==(v==='jogador'?null:v)) A.forcar(Object.assign({},o,{t,vista:v==='jogador'?null:v,estudio:!!window.__ESTUDIO})); else { A.S.forca.t=t; } },[S.o,t,vista]);
        await quadro(); await quadro();
        const clip=vista==='jogador'&&!process.env.INTEIRO?{x:300,y:250,width:980,height:470}:undefined;
        imgs.push({t:(t*(await page.evaluate(()=>__T.ARMA.S.est.dur))).toFixed(2),png:(await page.screenshot({type:'png',clip})).toString('base64')}); }
      await gravarFolha({nome:`${S.nome}-${vista}`,imgs}); nFolhas++; process.stdout.write(`\r${S.nome} ${vista}            `); } }
  await page.evaluate(()=>__T.ARMA.forcar(null));
  console.log(`\n${nFolhas} folhas em ${OUT}`); if(erros.length){ console.log('ERROS:\n'+[...new Set(erros)].join('\n')); process.exitCode=1; } else console.log('nenhum erro no console');
  await browser.close();
})().catch(e=>{ console.error(e); process.exit(1); });
