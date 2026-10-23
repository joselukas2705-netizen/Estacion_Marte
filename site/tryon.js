/* Photo fitting stays entirely in browser memory. No photo is uploaded or persisted. */
(() => {
 'use strict';
 const el = id => document.getElementById(id);
 const dialog = el('tryon-dialog'), canvas = el('tryon-canvas'), ctx = canvas.getContext('2d');
 const thumb = el('tryon-cutout'), thumbCtx = thumb.getContext('2d');
 const fileInput = el('tryon-file');
 let catalog, product, photo=null, source=null, cutout=null, sourceHasAlpha=false;
 let x=.5,y=.55,size=.35,rotation=0,pair=false,gap=.4,drag=null,loadSequence=0,photoSequence=0;
 let rawPixels=null,rawWidth=0,rawHeight=0;
 const controls = ['tryon-size','tryon-rotation','tryon-x','tryon-y','tryon-pair','tryon-gap','tryon-reset'];
 const price = amount => amount==null?'Precio por consultar':new Intl.NumberFormat('es-PE',{style:'currency',currency:'PEN'}).format(amount);
 const status = text => { el('tryon-status').textContent=text; };
 const allowed = p => p?.tryOnEnabled && p.available && ['Cadenas','Aretes','Pulseras'].includes(p.category);
 const asset = path => typeof path==='string' && /^assets\/[a-zA-Z0-9_./-]+\.(png|webp|jpe?g)$/i.test(path) && !path.includes('..') ? path : '';
 function loadImage(url) { return new Promise((resolve,reject)=>{const image=new Image();image.onload=()=>resolve(image);image.onerror=()=>reject(new Error('No pudimos abrir esta imagen.'));image.src=url;}); }
 function enablePhotoControls() {
  for(const id of controls) el(id).disabled=!photo;
  el('tryon-download').disabled=!photo||!cutout;
  el('tryon-photo-prompt').hidden=Boolean(photo);
  el('tryon-change-photo').hidden=!photo;el('tryon-remove-photo').hidden=!photo;
 }
 function resetPosition() {
  x=.5;y=product?.category==='Aretes'?.35:.57;
  size=product?.category==='Aretes'?.07:product?.category==='Pulseras'?.24:cutout?Math.max(.05,Math.min(.65,.36*canvas.height/canvas.width*cutout.width/cutout.height)):.25;
  rotation=0;pair=false;gap=.4;
  syncControls();draw();
 }
 function syncControls() {
  el('tryon-size').value=Math.round(size*100);el('tryon-size-value').value=Math.round(size*100)+'%';
  el('tryon-rotation').value=rotation;el('tryon-rotation-value').value=rotation+'°';
  el('tryon-x').value=Math.round(x*100);el('tryon-y').value=Math.round(y*100);el('tryon-pair').checked=pair;el('tryon-gap').value=Math.round(gap*100);
  el('tryon-gap-control').hidden=!pair;
 }
 function silhouette() {
  const w=canvas.width,h=canvas.height;ctx.fillStyle='#e8e0d3';ctx.fillRect(0,0,w,h);
  ctx.fillStyle='#d8cbb8';ctx.beginPath();ctx.ellipse(w*.5,h*.29,w*.18,h*.2,0,0,Math.PI*2);ctx.fill();
  ctx.fillRect(w*.43,h*.43,w*.14,h*.16);ctx.beginPath();ctx.ellipse(w*.5,h*.94,w*.43,h*.39,0,Math.PI,Math.PI*2);ctx.fill();
 }
 function drawJewel(target,positionX,positionY,mirror=false) {
  if(!cutout)return;
  const width=canvas.width*size,height=width*cutout.height/cutout.width;
  target.save();target.translate(positionX*canvas.width,positionY*canvas.height);target.rotate(rotation*Math.PI/180);target.scale(mirror?-1:1,1);
  target.drawImage(cutout,-width/2,-height/2,width,height);target.restore();
 }
 function draw(withNote=false) {
  ctx.clearRect(0,0,canvas.width,canvas.height);
  if(photo)ctx.drawImage(photo,0,0,canvas.width,canvas.height);else silhouette();
  if(photo&&cutout){if(pair&&product.category==='Aretes'){drawJewel(ctx,x-gap/2,y);drawJewel(ctx,x+gap/2,y,true);}else drawJewel(ctx,x,y);}
  if(withNote){const h=Math.max(34,canvas.height*.033);ctx.fillStyle='#f8f5eff0';ctx.fillRect(0,canvas.height-h,canvas.width,h);ctx.fillStyle='#493321';ctx.font=`${Math.max(11,h*.36)}px Arial`;ctx.textAlign='center';ctx.fillText('Estación Marte · Simulación orientativa · '+product.name,canvas.width/2,canvas.height-h*.35);}
 }
 function median(values) {values.sort((a,b)=>a-b);return values[Math.floor(values.length/2)];}
 function makeCutout() {
  if(!rawPixels)return;
  const cleaned=document.createElement('canvas');cleaned.width=rawWidth;cleaned.height=rawHeight;
  const cleanedCtx=cleaned.getContext('2d'), image=new ImageData(new Uint8ClampedArray(rawPixels),rawWidth,rawHeight), pixels=image.data;
  const samples=[[],[],[]];
  for(let j=0;j<rawHeight;j++)for(let i=0;i<rawWidth;i++)if((i<3||j<3||i>rawWidth-4||j>rawHeight-4)&&pixels[(j*rawWidth+i)*4+3]>200){const k=(j*rawWidth+i)*4;for(let c=0;c<3;c++)samples[c].push(pixels[k+c]);}
  const bg=samples.map(v=>v.length?median(v):240), strength=Number(el('tryon-cleanup').value);
  let left=rawWidth,top=rawHeight,right=0,bottom=0,visible=0;
  const keep=new Uint8Array(rawWidth*rawHeight);
  for(let j=0;j<rawHeight;j++)for(let i=0;i<rawWidth;i++){
   const n=j*rawWidth+i,k=n*4;
   if(!sourceHasAlpha){
    const d=Math.hypot(pixels[k]-bg[0],pixels[k+1]-bg[1],pixels[k+2]-bg[2]);
    const max=Math.max(pixels[k],pixels[k+1],pixels[k+2]),min=Math.min(pixels[k],pixels[k+1],pixels[k+2]);
    const colorfulness=max-min;
    const edge=Math.max(d,colorfulness*1.65);
    const alpha=Math.max(0,Math.min(1,(edge-strength)/15));
    const neutralBackground=product.finish==='Dorado' && colorfulness<21 && min>95;
    pixels[k+3]=neutralBackground?0:Math.round(pixels[k+3]*alpha);
   }
   if(pixels[k+3]>24)keep[n]=1;
  }
  // Retain meaningful connected regions, removing dust and isolated background noise.
  const seen=new Uint8Array(keep.length),queue=new Int32Array(keep.length), minArea=Math.max(4,keep.length*.00004);
  for(let start=0;start<keep.length;start++)if(keep[start]&&!seen[start]){
   let head=0,tail=1;queue[0]=start;seen[start]=1;
   while(head<tail){const n=queue[head++],i=n%rawWidth,j=Math.floor(n/rawWidth);for(const next of [i>0?n-1:-1,i<rawWidth-1?n+1:-1,j>0?n-rawWidth:-1,j<rawHeight-1?n+rawWidth:-1])if(next>=0&&keep[next]&&!seen[next]){seen[next]=1;queue[tail++]=next;}}
   if(tail<minArea){for(let q=0;q<tail;q++)pixels[queue[q]*4+3]=0;}
  }
  for(let j=0;j<rawHeight;j++)for(let i=0;i<rawWidth;i++)if(pixels[(j*rawWidth+i)*4+3]>24){left=Math.min(left,i);right=Math.max(right,i);top=Math.min(top,j);bottom=Math.max(bottom,j);visible++;}
  if(visible<10){cutout=null;thumbCtx.clearRect(0,0,thumb.width,thumb.height);status('El recorte quedó vacío. Baja «Limpiar el fondo» para recuperar la joya.');enablePhotoControls();draw();return;}
  cleanedCtx.putImageData(image,0,0);
  cutout=document.createElement('canvas');cutout.width=right-left+1;cutout.height=bottom-top+1;
  cutout.getContext('2d').drawImage(cleaned,left,top,cutout.width,cutout.height,0,0,cutout.width,cutout.height);
  thumbCtx.clearRect(0,0,thumb.width,thumb.height);const fit=Math.min((thumb.width-24)/cutout.width,(thumb.height-16)/cutout.height);thumbCtx.drawImage(cutout,(thumb.width-cutout.width*fit)/2,(thumb.height-cutout.height*fit)/2,cutout.width*fit,cutout.height*fit);
  status(photo?'Joya lista. Arrástrala o usa los controles.':'Elige tu foto para comenzar.');enablePhotoControls();draw();
 }
 async function selectProduct(p) {
  product=p;source=null;cutout=null;rawPixels=null;el('tryon-product').value=p.id;el('tryon-price').textContent=price(p.price)+' · '+p.finish;
  el('tryon-pair-options').hidden=p.category!=='Aretes';el('tryon-cleanup').value=45;el('tryon-cleanup-value').value='45';resetPosition();enablePhotoControls();status('Preparando tu joya…');
  const sequence=++loadSequence;
  try{
   const url=asset(p.tryOnImage||p.image);if(!url)throw new Error('La foto de esta pieza no está disponible.');
   const image=await loadImage(url);if(sequence!==loadSequence||!dialog.open)return;source=image;
   const crop=Array.isArray(p.tryOnCrop)&&p.tryOnCrop.length===4&&!p.tryOnImage?p.tryOnCrop:[0,0,1,1];
   const temp=document.createElement('canvas'),scale=Math.min(1,650/(image.naturalWidth*crop[2]),850/(image.naturalHeight*crop[3]));
   temp.width=Math.max(1,Math.round(image.naturalWidth*crop[2]*scale));temp.height=Math.max(1,Math.round(image.naturalHeight*crop[3]*scale));
   const tempCtx=temp.getContext('2d',{willReadFrequently:true});tempCtx.drawImage(image,image.naturalWidth*crop[0],image.naturalHeight*crop[1],image.naturalWidth*crop[2],image.naturalHeight*crop[3],0,0,temp.width,temp.height);
   const data=tempCtx.getImageData(0,0,temp.width,temp.height);rawPixels=data.data;rawWidth=temp.width;rawHeight=temp.height;
   sourceHasAlpha=false;for(let i=3;i<rawPixels.length;i+=4)if(rawPixels[i]<200){sourceHasAlpha=true;break;}
   el('tryon-cleanup').disabled=sourceHasAlpha;el('tryon-cleanup').closest('details').hidden=sourceHasAlpha;makeCutout();
  }catch(error){if(sequence===loadSequence){status(error.message);enablePhotoControls();}}
 }
 async function openTryon(preferred) {
  if(!catalog)return;
  const options=catalog.products.filter(allowed);if(!options.length){el('toast').textContent='Estamos preparando las piezas para el probador.';return;}
  el('tryon-product').replaceChildren();for(const p of options){const option=document.createElement('option');option.value=p.id;option.textContent=p.name+' · '+p.category+' · '+p.finish;el('tryon-product').append(option);}
  if(!dialog.open)dialog.showModal();await selectProduct(options.find(p=>p.id===preferred?.id)||options[0]);
 }
 function clearPhoto() {photoSequence++;photo=null;canvas.width=900;canvas.height=1125;fileInput.value='';enablePhotoControls();draw();}
 fileInput.addEventListener('change',async()=>{
  const file=fileInput.files?.[0];if(!file)return;
  if(!['image/jpeg','image/png','image/webp'].includes(file.type)){status('Usa una foto JPG, PNG o WebP. Si está en HEIC, expórtala como JPG.');fileInput.value='';return;}
  if(file.size>15*1024*1024){status('La foto supera 15 MB. Elige una versión más ligera.');fileInput.value='';return;}
  const token=++photoSequence,url=URL.createObjectURL(file);status('Abriendo tu foto en este navegador…');
  try{const image=await loadImage(url);if(token!==photoSequence||!dialog.open)return;if(image.naturalWidth<100||image.naturalHeight<100)throw new Error('Elige una foto de al menos 100 × 100 píxeles.');
   const fit=Math.min(1,1200/image.naturalWidth,1600/image.naturalHeight);canvas.width=Math.round(image.naturalWidth*fit);canvas.height=Math.round(image.naturalHeight*fit);photo=image;
   enablePhotoControls();resetPosition();status('Foto lista. Arrastra la joya para colocarla sobre ti.');
  }catch(error){if(token===photoSequence)status(error.message);}finally{URL.revokeObjectURL(url);fileInput.value='';}
 });
 for(const id of ['tryon-upload','tryon-change-photo'])el(id).onclick=()=>fileInput.click();
 el('tryon-remove-photo').onclick=()=>{clearPhoto();status('Tu foto se quitó del probador.');};
 el('tryon-product').onchange=()=>{const p=catalog.products.find(p=>p.id===el('tryon-product').value&&allowed(p));if(p)selectProduct(p);};
 for(const id of ['tryon-size','tryon-rotation','tryon-x','tryon-y','tryon-gap'])el(id).oninput=()=>{size=Number(el('tryon-size').value)/100;rotation=Number(el('tryon-rotation').value);x=Number(el('tryon-x').value)/100;y=Number(el('tryon-y').value)/100;gap=Number(el('tryon-gap').value)/100;syncControls();draw();};
 el('tryon-pair').onchange=()=>{pair=el('tryon-pair').checked;syncControls();draw();};el('tryon-reset').onclick=resetPosition;
 el('tryon-cleanup').oninput=()=>{el('tryon-cleanup-value').value=el('tryon-cleanup').value;makeCutout();};
 canvas.addEventListener('pointerdown',event=>{if(!photo||!cutout)return;const r=canvas.getBoundingClientRect();drag={id:event.pointerId,offsetX:x-(event.clientX-r.left)/r.width,offsetY:y-(event.clientY-r.top)/r.height};canvas.setPointerCapture(event.pointerId);});
 canvas.addEventListener('pointermove',event=>{if(!drag||drag.id!==event.pointerId)return;const r=canvas.getBoundingClientRect();x=Math.max(0,Math.min(1,(event.clientX-r.left)/r.width+drag.offsetX));y=Math.max(0,Math.min(1,(event.clientY-r.top)/r.height+drag.offsetY));syncControls();draw();});
 for(const name of ['pointerup','pointercancel','lostpointercapture'])canvas.addEventListener(name,()=>{drag=null;});
 canvas.addEventListener('keydown',event=>{if(!photo||!cutout)return;const delta=event.shiftKey?.03:.01;const move={ArrowLeft:[-delta,0],ArrowRight:[delta,0],ArrowUp:[0,-delta],ArrowDown:[0,delta]}[event.key];if(!move)return;event.preventDefault();x=Math.max(0,Math.min(1,x+move[0]));y=Math.max(0,Math.min(1,y+move[1]));syncControls();draw();});
 el('tryon-download').onclick=()=>{if(!photo||!cutout)return;draw(true);canvas.toBlob(blob=>{draw();if(!blob)return;const url=URL.createObjectURL(blob),link=document.createElement('a');link.href=url;link.download='estacion-marte-'+product.id+'-mi-prueba.png';link.click();setTimeout(()=>URL.revokeObjectURL(url),10000);},'image/png');};
 el('tryon-add').onclick=()=>{if(product?.available)window.dispatchEvent(new CustomEvent('marte:tryon-add',{detail:product.id}));};
 dialog.addEventListener('close',()=>{loadSequence++;clearPhoto();source=null;cutout=null;rawPixels=null;thumbCtx.clearRect(0,0,thumb.width,thumb.height);status('');});
 window.addEventListener('marte:catalog',event=>{catalog=event.detail;el('open-tryon').hidden=!catalog.products.some(allowed);});
 window.addEventListener('marte:tryon',event=>openTryon(event.detail));
 enablePhotoControls();draw();
})();
