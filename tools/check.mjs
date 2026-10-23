import {readFile,access} from 'node:fs/promises';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const data=JSON.parse(await readFile('site/catalog.json','utf8'));
assert.equal(data.products.length,37);assert.equal(new Set(data.products.map(p=>p.id)).size,37);
for(const p of data.products){assert.ok(p.name);assert.ok(p.price===null||Number.isFinite(p.price)&&p.price>=0);assert.match(p.image,/^assets\/[a-zA-Z0-9_-]+\.(jpg|png|webp)$/);await access('site/'+p.image);}
const html=await readFile('site/index.html','utf8');
for(const match of html.matchAll(/(?:href|src)="([^"#]+)"/g)){const url=match[1];if(!/^(https?:|data:)/.test(url))await access('site/'+url);}
for(const script of ['site/app.js','site/tryon.js']){const check=spawnSync(process.execPath,['--check',script],{encoding:'utf8'});assert.equal(check.status,0,check.stderr);}
const wearable=data.products.filter(p=>p.tryOnEnabled);assert.ok(wearable.length>0);for(const p of wearable){assert.ok(['Cadenas','Aretes','Pulseras'].includes(p.category));assert.ok(p.tryOnImage||p.tryOnCrop?.length===4);}
assert.equal(data.products.find(p=>p.name==='Rose').available,false);assert.equal(data.products.find(p=>p.name==='Aurora Heart').price,null);
console.log('Web: JSON, 37 fotos, referencias locales y sintaxis JavaScript correctos.');
