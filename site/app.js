'use strict';
const $ = (s, root = document) => root.querySelector(s);
let catalog, products = [], currentProduct, limit = 8, category = 'Todos', selection = {};
const money = value => value == null ? 'Consultar' : new Intl.NumberFormat('es-PE', {style:'currency',currency:'PEN'}).format(value);
const normalize = value => String(value).normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
let toastTimer;
function toast(message) { $('#toast').textContent = message; $('#toast').classList.add('visible'); clearTimeout(toastTimer); toastTimer = setTimeout(() => $('#toast').classList.remove('visible'), 3200); }
function safeImage(path) { return typeof path === 'string' && /^assets\/[a-zA-Z0-9_./-]+\.(webp|png|jpe?g)$/i.test(path) && !path.includes('..') ? path : 'assets/cover.webp'; }
function loadSelection() { try {const stored = JSON.parse(localStorage.getItem('em-selection-v1') || '{}'); if(stored && typeof stored === 'object') for(const p of products) {const qty=stored[p.id]; if(Number.isInteger(qty) && qty>0 && qty<=99 && p.available) selection[p.id]=qty;}} catch {} updateCount(); }
function updateCount() { $('#selection-count').textContent = Object.values(selection).reduce((a,b)=>a+b,0); try { localStorage.setItem('em-selection-v1', JSON.stringify(selection)); } catch {} }
function addProduct(p) { if(!p.available) return; selection[p.id] = Math.min(99,(selection[p.id] || 0)+1); updateCount(); toast(`${p.name} se añadió a tu selección`); }
function renderProducts() {
 const query=normalize($('#search').value), finish=$('#finish').value;
 let filtered=products.filter(p=>(category==='Todos'||p.category===category)&&(finish==='Todos'||p.finish===finish)&&normalize([p.name,p.category,p.style,p.material].join(' ')).includes(query));
 const sort=$('#sort').value;
 filtered.sort((a,b)=>sort==='price-asc' ? (a.price??Infinity)-(b.price??Infinity) : sort==='price-desc' ? (b.price??-Infinity)-(a.price??-Infinity) : sort==='name' ? a.name.localeCompare(b.name,'es') : Number(b.featured)-Number(a.featured));
 $('#product-grid').replaceChildren();
 for(const p of filtered.slice(0,limit)) {
  const article=document.createElement('article'); article.className='product-card';
  article.innerHTML='<button class="product-image-button"><img loading="lazy" width="400" height="500"><span class="product-badge"></span><span class="product-quick" aria-hidden="true">↗</span></button><div class="product-meta"><button class="product-title"><h3></h3></button><div class="price"></div></div><p><span class="finish-dot"></span><span class="product-info"></span></p>';
  $('img',article).src=safeImage(p.image); $('img',article).alt=`${p.category}: ${p.name}, ${p.finish.toLowerCase()}`;
  const badge=$('.product-badge',article); badge.textContent=p.available ? (p.featured?'FAVORITO':'') : 'AGOTADO'; badge.hidden=!badge.textContent; badge.classList.toggle('sold',!p.available);
  $('h3',article).textContent=p.name; $('.price',article).textContent=money(p.price); $('.product-info',article).textContent=`${p.category} · ${p.finish}`;
  $('.finish-dot',article).classList.toggle('silver',p.finish==='Plateado');
  for(const button of article.querySelectorAll('button')) {button.setAttribute('aria-label',`Ver ${p.name}, ${p.category}, ${p.finish}`);button.addEventListener('click',()=>openProduct(p));}
  $('#product-grid').append(article);
 }
 $('#results-count').textContent=`${filtered.length} ${filtered.length===1?'pieza para descubrir':'piezas para descubrir'}`;
 $('#empty').hidden=filtered.length>0; $('#load-more').hidden=filtered.length<=limit;
}
function openProduct(p) {
 currentProduct=p; $('#detail-image').src=safeImage(p.image); $('#detail-image').alt=p.name;
 $('#detail-category').textContent=p.category.toUpperCase(); $('#detail-name').textContent=p.name; $('#detail-price').textContent=money(p.price);
 $('#detail-description').textContent=p.description || 'Un pequeño detalle para hacerlo tuyo.'; $('#detail-material').textContent=p.material||'Consultar material';
 $('#detail-style').textContent=p.style||'Consultar'; $('#detail-finish').textContent=p.finish;
 $('#detail-availability').textContent=p.available?'Disponible para consultar':'Agotado'; $('#detail-add').disabled=!p.available;
 $('#detail-add').firstChild.textContent=p.available?'Añadir a mi selección ':'Agotado ';
 $('#detail-tryon').hidden=!p.tryOnEnabled || !p.available; $('#product-dialog').showModal();
}
function renderSelection() {
 const items=products.filter(p=>selection[p.id]); $('#selection-items').replaceChildren(); $('#selection-summary').hidden=items.length===0;
 if(!items.length) { const empty=document.createElement('div');empty.className='selection-empty';empty.innerHTML='<span>✧</span><h3>Aquí empieza algo bonito.</h3><p>Explora el catálogo y añade<br>las piezas que más te gusten.</p><button class="button outline">Descubrir la colección ↗</button>'; $('button',empty).onclick=()=>{$('#selection-dialog').close();$('#catalogo').scrollIntoView({behavior:'smooth'});};$('#selection-items').append(empty);return; }
 let total=0, consult=false;
 for(const p of items) {
  if(p.price==null) consult=true; else total+=p.price*selection[p.id];
  const row=document.createElement('div'); row.className='selection-item';row.innerHTML='<img width="70" height="90"><div><h3></h3><p></p><div class="quantity"><button aria-label="Quitar una unidad">−</button><span></span><button aria-label="Añadir una unidad">+</button></div></div><button class="remove-item" aria-label="Eliminar producto">×</button>';
  $('img',row).src=safeImage(p.image);$('img',row).alt=p.name;$('h3',row).textContent=p.name;$('p',row).textContent=`${p.finish} · ${money(p.price)}`;$('.quantity span',row).textContent=selection[p.id];
  const buttons=row.querySelectorAll('button');buttons[0].onclick=()=>changeQty(p,-1);buttons[1].onclick=()=>changeQty(p,1);buttons[2].onclick=()=>{delete selection[p.id];updateCount();renderSelection();};$('#selection-items').append(row);
 }
 $('#subtotal').textContent=money(total)+(consult?' + por consultar':''); $('#selection-note').textContent=consult?'Hay piezas con precio por consultar. El envío y la disponibilidad se confirman al conversar.':'El envío y la disponibilidad se confirman al conversar.';
 $('#send-order').firstChild.textContent=catalog.brand.whatsapp?'Consultar por WhatsApp ':'Copiar mi consulta ';
}
function changeQty(p,delta) {const qty=(selection[p.id]||0)+delta;if(qty<=0)delete selection[p.id];else selection[p.id]=Math.min(99,qty);updateCount();renderSelection();}
function orderText() {return `¡Hola, ${catalog.brand.name}! Me gustaría consultar estas piezas:\n\n`+products.filter(p=>selection[p.id]).map(p=>`• ${selection[p.id]} × ${p.name} (${p.category}, ${p.finish}) — ${money(p.price)} [${p.id}]`).join('\n')+'\n\nSubtotal estimado: '+$('#subtotal').textContent+'\n¿Me confirman disponibilidad, envío y total, por favor?';}
function chooseCategory(value) { category=value;limit=8;for(const button of $('#categories').querySelectorAll('button')){const active=button.dataset.category===value;button.classList.toggle('selected',active);button.setAttribute('aria-pressed',active);}renderProducts(); }
for(const button of document.querySelectorAll('[data-close]')) button.onclick=()=>document.getElementById(button.dataset.close).close();
for(const dialog of document.querySelectorAll('dialog')) dialog.addEventListener('click',event=>{if(event.target===dialog){const r=dialog.getBoundingClientRect();if(event.clientX<r.left||event.clientX>r.right||event.clientY<r.top||event.clientY>r.bottom)dialog.close();}});
$('#open-selection').onclick=()=>{renderSelection();$('#selection-dialog').showModal();}; $('#detail-add').onclick=()=>addProduct(currentProduct);
$('#categories').onclick=e=>{const button=e.target.closest('button');if(button)chooseCategory(button.dataset.category);};
for(const id of ['search','finish','sort']) $('#'+id).addEventListener(id==='search'?'input':'change',()=>{limit=8;renderProducts();});
$('#load-more').onclick=()=>{limit+=12;renderProducts();};
$('#clear-filters').onclick=()=>{$('#search').value='';$('#finish').value='Todos';chooseCategory('Todos');};
$('#show-gifts').onclick=()=>{$('#search').value='';$('#finish').value='Todos';chooseCategory('Adicionales');$('#catalogo').scrollIntoView({behavior:'smooth'});};
$('#send-order').onclick=async()=>{const text=orderText();if(catalog.brand.whatsapp){window.open('https://wa.me/'+catalog.brand.whatsapp+'?text='+encodeURIComponent(text),'_blank','noopener');}else{try {await navigator.clipboard.writeText(text);toast('Consulta copiada. Pégala en nuestro chat de Instagram.');}catch {const box=document.createElement('textarea');box.value=text;box.style.cssText='width:100%;height:180px';box.setAttribute('aria-label','Texto de tu consulta para copiar');$('#selection-summary').append(box);box.select();toast('Copia este texto y envíalo por Instagram.');}}};
$('#year').textContent=new Date().getFullYear();
async function initialize() {try {const response=await fetch('catalog.json',{cache:'no-cache'});if(!response.ok)throw new Error('No se pudo leer el catálogo');catalog=await response.json(); products=catalog.products;const brand=catalog.brand;brand.whatsapp=String(brand.whatsapp||'').replace(/\D/g,'');brand.instagram=String(brand.instagram||'estacionmarte').replace(/[^a-zA-Z0-9_.]/g,'');
 const title=$('#hero-title');title.replaceChildren();const lines=brand.heroTitle.split('\n');title.append(document.createTextNode(lines[0]),document.createElement('br'));const em=document.createElement('em');em.textContent=lines.slice(1).join(' ')||'lo dice todo.';title.append(em);$('#hero-text').textContent=brand.heroText;$('#tagline').textContent=brand.tagline;document.title=brand.name+' · Catálogo de accesorios';
 $('#instagram-link').href='https://www.instagram.com/'+brand.instagram+'/';$('#instagram-link').textContent='@'+brand.instagram+' ↗';$('#selection-contact').href=$('#instagram-link').href;$('#contact-link').href=brand.whatsapp?'https://wa.me/'+brand.whatsapp:$('#instagram-link').href;$('#contact-link').firstChild.textContent=brand.whatsapp?'Escríbenos por WhatsApp ':'Escríbenos por Instagram ';loadSelection();renderProducts();window.dispatchEvent(new CustomEvent('marte:catalog',{detail:catalog}));
 } catch(error) {$('#results-count').textContent='No pudimos cargar la colección.';$('#empty').hidden=false;$('#empty h3').textContent='El catálogo estará de vuelta pronto.';$('#empty p').textContent='Puedes ver nuestras piezas en el PDF o escribirnos por Instagram.';$('#clear-filters').hidden=true;console.error(error);} }
document.addEventListener('DOMContentLoaded',initialize,{once:true});

$('#detail-tryon').onclick=()=>{ $('#product-dialog').close(); window.dispatchEvent(new CustomEvent('marte:tryon',{detail:currentProduct})); };
$('#open-tryon').onclick=()=>{if(catalog)window.dispatchEvent(new CustomEvent('marte:tryon',{detail:null}));};
window.addEventListener('marte:tryon-add',event=>{const product=products.find(p=>p.id===event.detail);if(product)addProduct(product);});
