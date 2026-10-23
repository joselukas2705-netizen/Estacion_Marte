"""One-time import from the owner's supplied August PDF; no PDF text is executed."""
from pathlib import Path
import json, shutil, argparse
import pymupdf as fitz
from PIL import Image, ImageDraw
import io

root = Path(__file__).resolve().parents[1]
assets = root / 'site/assets'
assets.mkdir(parents=True, exist_ok=True)
parser=argparse.ArgumentParser(description='Importar el catálogo AGOSTO de Estación Marte')
parser.add_argument('--pdf',required=True)
parser.add_argument('--logo',required=True)
args=parser.parse_args()
doc = fitz.open(args.pdf)
gold = 'Acero con doble baño en oro 18k'
rodio = 'Acero con 2 baños en rodio'
# Verified visually against the supplied PDF. Page 4 Rose is marked SOLD OUT.
rows = [
('Corazón geo','Cadenas',17.5,'Dorado','Acero inoxidable','Minimalista',46,3,True),
('Katty','Cadenas',23.5,'Dorado','Acero inoxidable','Cute',48,3,True),
('Rose','Cadenas',18.5,'Dorado','Acero inoxidable','Flores · Giratorio',56,4,False),
('G-dimd','Cadenas',21.5,'Dorado','Acero inoxidable','Flores · Giratorio',54,4,True),
('G-clasid','Cadenas',21.5,'Dorado','Acero inoxidable','Flores · Giratorio',55,4,True),
('Tortugin','Cadenas',40.9,'Dorado',gold,'Cute, Y2K',61,5,True),
('Rubi','Cadenas',43.9,'Dorado',gold,'Clásico',62,5,True),
('3perla','Cadenas',35.5,'Dorado','Acero enchapado en oro 18k','Clásico',68,6,True),
('Heart','Cadenas',43.9,'Dorado',gold,'Romántico',66,6,True),
('Corazoo','Cadenas',43.9,'Dorado',gold,'Romántico',67,6,True),
('Heart','Cadenas',43.9,'Plateado',rodio,'Romántico',72,7,True),
('Lovy','Cadenas',45.9,'Plateado',rodio+', con circón incrustado','Romántico',73,7,True),
('Fresita','Aretes',23.5,'Dorado',gold,'Cute, Y2K',83,9,True),
('Sadny','Aretes',35.5,'Dorado',gold,'Romántico',84,9,True),
('Alas','Aretes',28.5,'Dorado',gold,'Cute, Y2K',88,10,True),
('Baby heart','Aretes',24.9,'Dorado','Acero enchapado en oro 18k','Romántico',89,10,True),
('Mini aros','Aretes',19.5,'Dorado',gold,'Clásico',93,11,True),
('Doble destello','Aretes',14.9,'Dorado','Acero enchapado en oro 18k','Romántico',94,11,True),
('Aurora Heart','Aretes',None,'Dorado','','',95,11,True),
('Gotas','Aretes',28.5,'Dorado',gold,'Clásico',99,12,True),
('Cora bom','Aretes',23.5,'Dorado',gold,'Romántico',100,12,True),
('Escudo de corazón','Aretes',23.5,'Dorado',gold,'Romántico',104,13,True),
('Cori','Aretes',15,'Dorado',gold,'Romántico',105,13,True),
('Mariquita','Aretes',23.5,'Dorado',gold,'Cute, Y2K',109,14,True),
('Panda','Aretes',28.5,'Dorado',gold,'Cute, Y2K',110,14,True),
('Canoa','Aretes',41.9,'Plateado',rodio,'Clásico',114,15,True),
('Heart one','Aretes',35.5,'Plateado',rodio,'Romántico',115,15,True),
('Brac love','Pulseras',35.5,'Dorado','Acero enchapado en oro 18k','Romántico',126,17,True),
('Sadny','Pulseras',34.9,'Dorado','Acero enchapado en oro 18k','Clásico',130,18,True),
('Clade','Pulseras',36.5,'Dorado',gold,'Clásico',131,18,True),
('Cattito','Sets',17.5,'Dorado','Acero inoxidable','Clásico',140,20,True),
('Heart bomb','Sets',68.5,'Dorado',gold,'Romántico',141,20,True),
('Baby love','Sets',45.5,'Dorado',gold,'Delicado',147,21,True),
('Baby glowy','Sets',45.5,'Dorado',gold,'Clásico',146,21,True),
('Heart bomb','Sets',68.5,'Plateado',rodio,'Romántico',152,22,True),
('Caja de lujo con rosa eterna','Adicionales',20,'Otro','','Romántico',157,23,True),
('Joyero de anillos','Adicionales',10.5,'Otro','Cuero ecológico','Romántico',156,23,True)
]
def extract(xref, name, width=1200):
    data=doc.extract_image(xref)
    im=Image.open(io.BytesIO(data['image'])).convert('RGB')
    im.thumbnail((width,width*2))
    im.save(assets/name,'JPEG' if name.endswith('.jpg') else 'WEBP',quality=88)
    return im
products=[]
sheet=Image.new('RGB',(1200,((len(rows)+5)//6)*215),'#eee')
for i,(name,category,price,finish,material,style,xref,page,available) in enumerate(rows):
    filename=f'product-{i+1:02}.jpg'
    im=extract(xref,filename,1000)
    im.thumbnail((190,180))
    x,y=(i%6)*200,(i//6)*215
    sheet.paste(im,(x,y))
    ImageDraw.Draw(sheet).text((x,y+183),f'{i+1}. {name}',fill='black')
    products.append(dict(id=f'em-{i+1:03}',name=name,category=category,price=price,finish=finish,material=material,style=style,image='assets/'+filename,description='',available=available,featured=i in [0,8,15,19,27,32],sourcePage=page))
extract(135,'hero.webp',1400)
extract(78,'editorial.webp',1000)
extract(162,'gift.webp',800)
extract(36,'cover.webp',800)
if Path(args.logo).resolve() != (assets/'logo.jpg').resolve():
    shutil.copyfile(args.logo,assets/'logo.jpg')
shutil.copyfile(args.pdf,assets/'catalogo-agosto.pdf')
catalog=dict(schemaVersion=1,brand=dict(name='Estación Marte',tagline='Pequeños detalles. Tu propio universo.',heroTitle='Un detalle que\nlo dice todo.',heroText='Cadenas, aretes y pulseras para acompañarte en cada versión de ti.',whatsapp='',instagram='estacionmarte',currency='PEN'),products=products)
(root/'site/catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
sheet.save(root/'tmp/pdfs/products.jpg')
print(f'Imported {len(products)} products. Aurora Heart has no printed price: left empty for consultation.')
