import {readFile,writeFile} from 'node:fs/promises';
const file='site/catalog.json',catalog=JSON.parse(await readFile(file,'utf8'));
const prepared={
 'em-001':[.31,.10,.69,.62],
 'em-009':[.1,0,.72,.82],
 'em-016':[.27,.34,.19,.22],
 'em-028':[.16,.15,.7,.74]
};
for(const p of catalog.products){p.tryOnEnabled=Boolean(prepared[p.id]);p.tryOnImage='';p.tryOnCrop=prepared[p.id]||null;}
await writeFile(file,JSON.stringify(catalog,null,2)+'\n');
console.log('Probador inicial: 4 piezas con encuadre preparado; ampliable desde el editor.');
