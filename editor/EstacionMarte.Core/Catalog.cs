using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace EstacionMarte.Core;

public sealed class Catalog
{
    public int SchemaVersion { get; set; } = 1;
    public Brand Brand { get; set; } = new();
    public List<Product> Products { get; set; } = [];
}
public sealed class Brand
{
    public string Name { get; set; } = "Estación Marte";
    public string Tagline { get; set; } = "Pequeños detalles. Tu propio universo.";
    public string HeroTitle { get; set; } = "Un detalle que\nlo dice todo.";
    public string HeroText { get; set; } = "Cadenas, aretes y pulseras para acompañarte en cada versión de ti.";
    public string Whatsapp { get; set; } = "";
    public string Instagram { get; set; } = "estacionmarte";
    public string Currency { get; set; } = "PEN";
}
public sealed class Product
{
    public string Id { get; set; } = "em-" + Guid.NewGuid().ToString("N")[..10];
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Aretes";
    public decimal? Price { get; set; }
    public string Finish { get; set; } = "Dorado";
    public string Material { get; set; } = "";
    public string Style { get; set; } = "";
    public string Image { get; set; } = "assets/cover.webp";
    public string Description { get; set; } = "";
    public bool Available { get; set; } = true;
    public bool Featured { get; set; }
    public int? SourcePage { get; set; }
    public bool TryOnEnabled { get; set; }
    public string TryOnImage { get; set; } = "";
    public double[]? TryOnCrop { get; set; }
    public Product Clone() => (Product)MemberwiseClone();
}
public static class CatalogStore
{
    public static readonly string[] Categories = ["Cadenas", "Aretes", "Pulseras", "Sets", "Adicionales"];
    public static readonly string[] Finishes = ["Dorado", "Plateado", "Otro"];
    public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static Catalog Load(string site) => JsonSerializer.Deserialize<Catalog>(File.ReadAllText(Path.Combine(site, "catalog.json")), Json) ?? throw new InvalidDataException("Catálogo vacío.");
    public static string AssetPath(string site, string relative)
    {
        if (!Regex.IsMatch(relative, @"^assets/[a-zA-Z0-9_./-]+\.(webp|png|jpe?g)$", RegexOptions.IgnoreCase) || relative.Contains("..")) throw new InvalidDataException("La foto debe estar dentro de assets/ y ser JPG, PNG o WebP.");
        return Path.GetFullPath(Path.Combine(site, relative.Replace('/', Path.DirectorySeparatorChar)));
    }
    public static void Validate(Catalog catalog, string site)
    {
        if (catalog.SchemaVersion != 1) throw new InvalidDataException("Versión de catálogo no compatible.");
        if (string.IsNullOrWhiteSpace(catalog.Brand.Name) || string.IsNullOrWhiteSpace(catalog.Brand.HeroTitle)) throw new InvalidDataException("Escribe el nombre de la marca y el título de portada.");
        if (catalog.Brand.Whatsapp.Length > 0 && !Regex.IsMatch(catalog.Brand.Whatsapp, @"^[0-9]{10,15}$")) throw new InvalidDataException("WhatsApp: usa de 10 a 15 dígitos con código de país, sin + ni espacios.");
        if (!Regex.IsMatch(catalog.Brand.Instagram, @"^[A-Za-z0-9_.]{1,30}$")) throw new InvalidDataException("Escribe el usuario de Instagram sin @ ni enlaces.");
        var ids = new HashSet<string>();
        foreach (var p in catalog.Products)
        {
            if (!Regex.IsMatch(p.Id, @"^[a-zA-Z0-9_-]+$") || !ids.Add(p.Id)) throw new InvalidDataException("Hay identificadores de producto inválidos o repetidos.");
            if (string.IsNullOrWhiteSpace(p.Name)) throw new InvalidDataException("Todos los productos necesitan un nombre.");
            if (!Categories.Contains(p.Category) || !Finishes.Contains(p.Finish)) throw new InvalidDataException($"Revisa categoría y acabado de {p.Name}.");
            if (p.Price is < 0 or > 1000000) throw new InvalidDataException($"El precio de {p.Name} debe estar entre 0 y 1 000 000.");
            if (!File.Exists(AssetPath(site, p.Image))) throw new InvalidDataException($"No se encuentra la foto de {p.Name}.");
            if (p.TryOnEnabled && p.Category is not ("Cadenas" or "Aretes" or "Pulseras")) throw new InvalidDataException($"El probador de {p.Name} solo admite cadenas, aretes y pulseras.");
            if (!string.IsNullOrEmpty(p.TryOnImage) && !File.Exists(AssetPath(site,p.TryOnImage))) throw new InvalidDataException($"No se encuentra la foto del probador de {p.Name}.");
            if (p.TryOnCrop is { } crop && (crop.Length!=4 || crop.Any(v=>!double.IsFinite(v)||v<0||v>1) || crop[2]<=0 || crop[3]<=0 || crop[0]+crop[2]>1.00001 || crop[1]+crop[3]>1.00001)) throw new InvalidDataException($"El recorte del probador de {p.Name} no es válido.");
        }
    }
    public static void Save(string site, Catalog catalog)
    {
        Validate(catalog, site);
        var file = Path.Combine(site, "catalog.json");
        if (File.Exists(file))
        {
            var backup = Path.Combine(Directory.GetParent(site)!.FullName, "copias"); Directory.CreateDirectory(backup);
            File.Copy(file, Path.Combine(backup, $"catalog-{DateTime.Now:yyyyMMdd-HHmmss-ffff}.json"));
        }
        File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(catalog, Json));
        File.Move(file + ".tmp", file, true);
    }
    public static string ImportPhoto(string site, string source)
    {
        var extension = Path.GetExtension(source).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp")) throw new InvalidDataException("Selecciona una imagen JPG, PNG o WebP.");
        if (new FileInfo(source).Length > 15 * 1024 * 1024) throw new InvalidDataException("La foto supera 15 MB. Usa una versión más ligera.");
        var relative = $"assets/custom-{Guid.NewGuid():N}{extension}";
        File.Copy(source, AssetPath(site, relative)); return relative;
    }
}
public static class SiteExporter
{
    public const string Marker = ".estacion-marte-generated";
    public const string Blueprint = """
services:
  - type: web
    name: estacion-marte
    runtime: static
    buildCommand: echo "Catalogo Estacion Marte listo"
    staticPublishPath: site
    autoDeployTrigger: commit
    headers:
      - path: /catalog.json
        name: Cache-Control
        value: no-cache
      - path: /*
        name: X-Content-Type-Options
        value: nosniff
      - path: /*
        name: Referrer-Policy
        value: strict-origin-when-cross-origin
""";
    public static void Export(string sourceSite, string destination)
    {
        sourceSite = Path.GetFullPath(sourceSite); destination = Path.GetFullPath(destination);
        if (destination.Equals(sourceSite, StringComparison.OrdinalIgnoreCase) || destination.StartsWith(sourceSite + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || sourceSite.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Elige una carpeta de exportación separada de tu catálogo de trabajo.");
        CatalogStore.Validate(CatalogStore.Load(sourceSite), sourceSite);
        if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any() && !File.Exists(Path.Combine(destination, Marker))) throw new InvalidOperationException("La carpeta tiene otros archivos. Elige una carpeta vacía o una exportación anterior de Estación Marte.");
        Directory.CreateDirectory(Path.Combine(destination,"site","assets"));
        File.WriteAllText(Path.Combine(destination, Marker), "Estación Marte · web generada por el editor\n");
        foreach (var name in new[] { "index.html", "styles.css", "tryon.css", "app.js", "tryon.js", "favicon.svg", "catalog.json" }) File.Copy(Path.Combine(sourceSite,name),Path.Combine(destination,"site",name),true);
        foreach (var asset in Directory.EnumerateFiles(Path.Combine(sourceSite,"assets")))
        {
            if ((File.GetAttributes(asset) & FileAttributes.ReparsePoint) != 0) throw new IOException("No se exportan enlaces simbólicos.");
            if (new[] {".webp",".jpg",".jpeg",".png",".pdf"}.Contains(Path.GetExtension(asset).ToLowerInvariant())) File.Copy(asset,Path.Combine(destination,"site","assets",Path.GetFileName(asset)),true);
        }
        File.WriteAllText(Path.Combine(destination,"render.yaml"),Blueprint+"\n");
        File.WriteAllText(Path.Combine(destination,".gitignore"),".DS_Store\nThumbs.db\n*.local.json\n.tools/\ntmp/\n**/bin/\n**/obj/\noutput/\n.vs/\n*.user\n");
        File.WriteAllText(Path.Combine(destination,".gitattributes"),"* text=auto eol=lf\n*.pdf binary\n*.jpg binary\n*.jpeg binary\n*.png binary\n*.webp binary\n");
        File.WriteAllText(Path.Combine(destination,"README.md"), "# Estación Marte\n\nCatálogo estático generado desde el editor C#.\n\n## Render\nEn Render, selecciona New > Blueprint y conecta este repositorio. El archivo render.yaml configura el sitio. También puedes usar New > Static Site: Build command `echo listo`, Publish directory `site`. Activa el despliegue al hacer push a main.\n\nEdita el catálogo desde la aplicación, vuelve a generar en esta misma carpeta y pulsa Publicar en GitHub. Render desplegará el nuevo commit.\n\nLos pedidos se consultan por WhatsApp o Instagram; no se procesan pagos en la web.\n");
    }
}
public static class GitPublisher
{
    public static bool IsGitHubUrl(string url) => Regex.IsMatch(url, @"^https://github\.com/[A-Za-z0-9_-]+/[A-Za-z0-9_.-]+(?:\.git)?/?$", RegexOptions.IgnoreCase) && !url.Contains("..");
    private static string Canonical(string url) => url.TrimEnd('/').Replace(".git", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
    public static async Task<string> Run(string directory, params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory=directory, UseShellExecute=false, RedirectStandardOutput=true, RedirectStandardError=true, CreateNoWindow=true };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        using var process=Process.Start(start) ?? throw new IOException("No se pudo iniciar Git.");
        var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try { await process.WaitForExitAsync(timeout.Token); } catch(OperationCanceledException) {process.Kill(true);throw new IOException("Git tardó demasiado. Revisa tu conexión e inicio de sesión.");}
        var output=await stdout+await stderr;
        if(process.ExitCode!=0) throw new IOException(output.Length>0?output:"Git no pudo completar la operación.");
        return output;
    }
    public static async Task Publish(string directory, string url, IProgress<string>? progress = null)
    {
        if (!IsGitHubUrl(url)) throw new InvalidOperationException("Usa la URL HTTPS de GitHub, por ejemplo https://github.com/tu-usuario/estacion-marte.git");
        if (!File.Exists(Path.Combine(directory,SiteExporter.Marker))) throw new InvalidOperationException("Primero genera la web desde el editor.");
        if (!Directory.Exists(Path.Combine(directory,".git"))) await Run(directory,"init","-b","main");
        if (!string.Equals((await Run(directory,"rev-parse","--show-toplevel")).Trim().Replace('/',Path.DirectorySeparatorChar), Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("La exportación debe tener su propio repositorio.");
        if ((await Run(directory,"branch","--show-current")).Trim()!="main") throw new InvalidOperationException("El repositorio de publicación debe estar en la rama main.");
        var remotes=(await Run(directory,"remote")).Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if(remotes.Contains("origin"))
        {
            var actual=(await Run(directory,"remote","get-url","origin")).Trim();
            if(Canonical(actual)!=Canonical(url)) throw new InvalidOperationException("La carpeta ya está conectada a otro repositorio. Elige otra carpeta de exportación.");
        }
        else await Run(directory,"remote","add","origin",url);
        bool hasCommit;
        try { await Run(directory,"rev-parse","--verify","HEAD");hasCommit=true; } catch(IOException) { hasCommit=false; }
        if(!hasCommit && !string.IsNullOrWhiteSpace(await Run(directory,"ls-remote","origin","refs/heads/main")))
        {
            progress?.Report("Conectando con el historial existente del repositorio…");
            var snapshot=Path.Combine(Path.GetTempPath(),"EstacionMarte-Publicacion-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(snapshot);
            var generated=new[]{"site","render.yaml","README.md",".gitignore",".gitattributes",SiteExporter.Marker};
            void CopyGenerated(string from,string to)
            {
                foreach(var entry in generated)
                {
                    var item=Path.Combine(from,entry);
                    if(Directory.Exists(item))foreach(var file in Directory.EnumerateFiles(item,"*",SearchOption.AllDirectories))
                    {
                        var target=Path.Combine(to,Path.GetRelativePath(from,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target,true);
                    }
                    else if(File.Exists(item))File.Copy(item,Path.Combine(to,entry),true);
                }
            }
            CopyGenerated(directory,snapshot);
            await Run(directory,"fetch","origin","main");
            await Run(directory,"checkout","-f","-B","main","FETCH_HEAD");
            CopyGenerated(snapshot,directory);
            Directory.Delete(snapshot,true);
        }
        progress?.Report("Preparando los cambios…");
        await Run(directory,"add","--","site","render.yaml","README.md",".gitignore",".gitattributes",SiteExporter.Marker);
        if(!string.IsNullOrWhiteSpace(await Run(directory,"diff","--cached","--name-only"))) await Run(directory,"-c","user.name=Estacion Marte Editor","-c","user.email=editor@estacionmarte.local","commit","-m","Actualizar catalogo Estacion Marte");
        progress?.Report("Publicando en GitHub. Completa el acceso en el navegador si Git lo solicita…");
        await Run(directory,"push","-u","origin","HEAD:main");
        progress?.Report("Publicado en GitHub. Render iniciará el despliegue si ya está conectado.");
    }
}
