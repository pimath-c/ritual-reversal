#!/usr/bin/env node
// Desenha os ícones e a tela de abertura do app Android a partir do sigilo do jogo (o mesmo da tela inicial).
// Uso: NODE_PATH=$(npm root -g) node tools/icones-android.js   (precisa do Playwright; os PNGs ficam no repositório)
'use strict';
const fs=require('fs'), path=require('path');
const {chromium}=require('playwright');
const ROOT=path.join(__dirname,'..'), RES=path.join(ROOT,'mobile','android','app','src','main','res');
const cli=fs.readFileSync(path.join(ROOT,'client','index.html'),'utf8');
const SIGIL=cli.match(/const SIGIL=`(<svg[^`]*)`/)[1].replace('class="sigil" ','');
const FUNDO='#0b0a0e';
const icone=(tam,redondo)=>`<div style="width:${tam}px;height:${tam}px;border-radius:${redondo?'50%':'22%'};overflow:hidden;background:radial-gradient(circle at 50% 42%,#2a1018,${FUNDO} 72%);display:grid;place-items:center">
  <div style="width:${tam*.78}px;height:${tam*.78}px">${SIGIL}</div></div>`;
// adaptativo: o Android corta até um círculo de 66% do quadro; o sigilo fica dentro dessa área
const frente=tam=>`<div style="width:${tam}px;height:${tam}px;display:grid;place-items:center"><div style="width:${tam*.56}px;height:${tam*.56}px">${SIGIL}</div></div>`;
const abertura=(w,h)=>{ const s=Math.min(w,h)*.34; return `<div style="width:${w}px;height:${h}px;background:radial-gradient(ellipse at 50% 40%,#1e0c14,#050507 70%);display:flex;flex-direction:column;align-items:center;justify-content:center;gap:${s*.12}px">
  <div style="width:${s}px;height:${s}px">${SIGIL}</div><div style="font:${s*.2}px Georgia,serif;color:#e8e0d0;letter-spacing:.04em">Ritual Reversal</div></div>`; };
(async()=>{
  const browser=await chromium.launch(); const page=await browser.newPage();
  const png=async(html,w,h,arq,transp)=>{ await page.setViewportSize({width:w,height:h});
    await page.setContent(`<html><body style="margin:0;background:transparent">${html}</body></html>`);
    await page.screenshot({path:arq,omitBackground:!!transp,clip:{x:0,y:0,width:w,height:h}}); };
  const dens={mdpi:1,hdpi:1.5,xhdpi:2,xxhdpi:3,xxxhdpi:4};
  for(const [d,k] of Object.entries(dens)){ const dir=path.join(RES,'mipmap-'+d), t=Math.round(48*k), f=Math.round(108*k);
    await png(icone(t,false),t,t,path.join(dir,'ic_launcher.png'),true);
    await png(icone(t,true),t,t,path.join(dir,'ic_launcher_round.png'),true);
    await png(frente(f),f,f,path.join(dir,'ic_launcher_foreground.png'),true); }
  for(const dir of fs.readdirSync(RES).filter(d=>d.startsWith('drawable'))){ const arq=path.join(RES,dir,'splash.png'); if(!fs.existsSync(arq)) continue;
    const b=fs.readFileSync(arq), w=b.readUInt32BE(16), h=b.readUInt32BE(20); await png(abertura(w,h),w,h,arq,false); }
  fs.writeFileSync(path.join(RES,'values','ic_launcher_background.xml'),`<?xml version="1.0" encoding="utf-8"?>\n<resources>\n    <color name="ic_launcher_background">${FUNDO}</color>\n</resources>\n`);
  await page.setViewportSize({width:512,height:512}); await page.setContent(`<body style="margin:0">${icone(512,false)}</body>`);
  await page.screenshot({path:path.join(ROOT,'mobile','icone-512.png'),omitBackground:true,clip:{x:0,y:0,width:512,height:512}});
  await browser.close(); console.log('ícones e abertura gerados');
})().catch(e=>{ console.error(e); process.exit(1); });
