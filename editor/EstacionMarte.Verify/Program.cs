using EstacionMarte.Core;
using System.Security.Cryptography;
using System.Text.Json;

var root=Path.GetFullPath(args.FirstOrDefault()??".");
if(args.Length==3&&args[1]=="--export") {SiteExporter.Export(Path.Combine(root,"site"),Path.GetFullPath(args[2]));Console.WriteLine("Web exportada en "+Path.GetFullPath(args[2]));return;}
int checks=0;
void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;Console.WriteLine("OK "+message);}
void Reject(Action action,string message){try{action();}catch(Exception e) when(e is InvalidDataException or InvalidOperationException){checks++;Console.WriteLine("OK "+message);return;}throw new Exception(message);}
var source=Path.Combine(root,"site");var catalog=CatalogStore.Load(source);CatalogStore.Validate(catalog,source);
Check(catalog.Products.Count==37,"37 productos importados");Check(!catalog.Products.Single(p=>p.Name=="Rose").Available,"Agotado conservado del PDF");Check(catalog.Products.Single(p=>p.Name=="Aurora Heart").Price is null,"Precio ausente sin inventar");
Check(catalog.Products.Count(p=>p.TryOnEnabled)==4,"4 piezas habilitadas para el probador inicial");
var temp=Path.Combine(root,"tmp","verification-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
var work=Path.Combine(temp,"work","site");Directory.CreateDirectory(work);
foreach(var file in Directory.EnumerateFiles(source,"*",SearchOption.AllDirectories)){var target=Path.Combine(work,Path.GetRelativePath(source,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target);}
catalog.Brand.Whatsapp="51999999999";catalog.Products[0].Name="Prueba <script> & ñ";catalog.Products[0].Price=19.95m;CatalogStore.Save(work,catalog);
var roundtrip=CatalogStore.Load(work);Check(roundtrip.Products[0].Name==catalog.Products[0].Name&&roundtrip.Products[0].Price==19.95m,"Edición y JSON con caracteres especiales");Check(Directory.GetFiles(Path.Combine(temp,"work","copias")).Length==1,"Copia de seguridad al guardar");
Check(roundtrip.Products[0].TryOnEnabled&&roundtrip.Products[0].TryOnCrop!.SequenceEqual(catalog.Products[0].TryOnCrop!),"Configuración del probador conservada al guardar");
var savedCrop=catalog.Products[0].TryOnCrop;catalog.Products[0].TryOnCrop=[.9,0,.5,1];Reject(()=>CatalogStore.Validate(catalog,work),"Rechazo de recortes fuera de la imagen");catalog.Products[0].TryOnCrop=savedCrop;
Reject(()=>CatalogStore.AssetPath(work,"assets/../../secret.jpg"),"Bloqueo de rutas fuera de assets");catalog.Products[0].Price=-1;Reject(()=>CatalogStore.Save(work,catalog),"Rechazo de precios negativos");catalog.Products[0].Price=19.95m;
catalog.Brand.Whatsapp="+51 abc";Reject(()=>CatalogStore.Validate(catalog,work),"Validación de WhatsApp");catalog.Brand.Whatsapp="51999999999";CatalogStore.Save(work,catalog);
var export=Path.Combine(temp,"export");SiteExporter.Export(work,export);Check(File.Exists(Path.Combine(export,"render.yaml"))&&File.Exists(Path.Combine(export,"site","index.html")),"Exportación lista para Render");
Check(File.Exists(Path.Combine(export,"site","tryon.js"))&&File.Exists(Path.Combine(export,"site","tryon.css")),"Probador incluido en la web exportada");
Check(SHA256.HashData(File.ReadAllBytes(Path.Combine(work,"assets","product-01.jpg"))).SequenceEqual(SHA256.HashData(File.ReadAllBytes(Path.Combine(export,"site","assets","product-01.jpg")))),"Fotos exportadas sin alterar");
catalog.Products[0].Price=21.25m;CatalogStore.Save(work,catalog);SiteExporter.Export(work,export);Check(CatalogStore.Load(Path.Combine(export,"site")).Products[0].Price==21.25m,"Regeneración actualiza el catálogo");
var unrelated=Path.Combine(temp,"other");Directory.CreateDirectory(unrelated);File.WriteAllText(Path.Combine(unrelated,"keep.txt"),"keep");Reject(()=>SiteExporter.Export(work,unrelated),"Protección de carpetas ajenas");Reject(()=>SiteExporter.Export(work,Path.Combine(work,"export")),"Protección del catálogo original");
Check(!GitPublisher.IsGitHubUrl("https://evil.com/user/repo.git")&&GitPublisher.IsGitHubUrl("https://github.com/user/repo.git"),"Destino de publicación restringido a GitHub HTTPS");
var bare=Path.Combine(temp,"remote.git");Directory.CreateDirectory(bare);await GitPublisher.Run(bare,"init","--bare");await GitPublisher.Run(export,"init","-b","main");await GitPublisher.Run(export,"add",".");await GitPublisher.Run(export,"-c","user.name=Verification","-c","user.email=verify@example.invalid","commit","-m","Verify exported catalog");await GitPublisher.Run(export,"remote","add","origin",bare);await GitPublisher.Run(export,"push","-u","origin","main");
var remoteJson=await GitPublisher.Run(bare,"show","main:site/catalog.json");Check(JsonSerializer.Deserialize<Catalog>(remoteJson,CatalogStore.Json)!.Products[0].Price==21.25m,"Git recibe exactamente los datos exportados (remoto local de prueba)");
Console.WriteLine($"{checks} verificaciones correctas. No se publicaron datos en servicios externos.");
