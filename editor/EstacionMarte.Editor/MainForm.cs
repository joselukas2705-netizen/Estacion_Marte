using System.Diagnostics;
using System.Text.Json;
using EstacionMarte.Core;

namespace EstacionMarte.Editor;
sealed class PublishSettings
{
    public string Repository {get;set;}="https://github.com/joselukas2705-netizen/Estacion_Marte.git";
    public string ExportDirectory {get;set;}=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Estacion Marte - Web");
}
sealed class MainForm : Form
{
    readonly string workspace,site;
    readonly Catalog catalog;
    readonly DataGridView grid=new();
    readonly TextBox search=Theme.Input(), repo=Theme.Input(), export=Theme.Input();
    readonly Label status=Theme.Note(""), count=Theme.Note("");
    readonly Dictionary<string,TextBox> brandFields=[];
    readonly FlowLayoutPanel actionPanel=new(){AutoSize=true,Dock=DockStyle.Top,Padding=new Padding(0,12,0,0)};
    readonly PublishSettings settings;
    LocalPreview? preview;
    bool dirty,busy,loaded;

    public MainForm(bool smoke=false)
    {
        Text="Estación Marte · Editor de catálogo";StartPosition=FormStartPosition.CenterScreen;Size=new Size(1180,820);MinimumSize=new Size(960,700);Font=new Font("Segoe UI",10);BackColor=Theme.Paper;ForeColor=Theme.Ink;
        workspace=smoke?Path.Combine(Path.GetTempPath(),"EstacionMarte-Smoke-"+Guid.NewGuid().ToString("N")):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EstacionMarte","MiCatalogo");
        site=Path.Combine(workspace,"site");
        if(!File.Exists(Path.Combine(site,"catalog.json"))) CopyTemplate();
        catalog=CatalogStore.Load(site);UpgradeWebTemplate();CatalogStore.Validate(catalog,site);
        var config=Path.Combine(workspace,"publicacion.local.json");
        try {settings=File.Exists(config)?JsonSerializer.Deserialize<PublishSettings>(File.ReadAllText(config))??new():new();}catch{settings=new();}
        if(string.IsNullOrWhiteSpace(settings.Repository))settings.Repository="https://github.com/joselukas2705-netizen/Estacion_Marte.git";
        var header=new Panel{Dock=DockStyle.Top,Height=107,Padding=new Padding(28,20,28,10),BackColor=Theme.Ink};
        var title=Theme.Heading("Estación Marte",25);title.ForeColor=Theme.Paper;title.Location=new Point(28,15);header.Controls.Add(title);
        var subtitle=Theme.Note("TU CATÁLOGO, A TU MANERA");subtitle.ForeColor=Color.FromArgb(210,191,154);subtitle.Location=new Point(31,63);subtitle.Font=new Font("Segoe UI",9);header.Controls.Add(subtitle);
        var save=Theme.Button("Guardar cambios",(_,_)=>Guard(SaveAll),true);save.BackColor=Theme.Gold;save.FlatAppearance.BorderSize=0;header.Controls.Add(save);header.Resize+=(_,_)=>save.Location=new Point(header.ClientSize.Width-save.Width-28,28);
        var tabs=new TabControl{Dock=DockStyle.Fill,Font=new Font("Segoe UI",11),Padding=new Point(25,12)};
        tabs.TabPages.Add(BuildProducts());tabs.TabPages.Add(BuildBrand());tabs.TabPages.Add(BuildPublish());
        var bottom=new Panel{Dock=DockStyle.Bottom,Height=45,Padding=new Padding(25,10,25,5),BackColor=Color.FromArgb(238,230,217)};status.Dock=DockStyle.Fill;status.MaximumSize=Size.Empty;status.AutoEllipsis=true;bottom.Controls.Add(status);
        Controls.Add(tabs);Controls.Add(bottom);Controls.Add(header);loaded=true;RefreshGrid();SetStatus("Tu catálogo está listo. Los cambios se guardan en este equipo.");
        FormClosing+=(_,e)=>{if(busy){e.Cancel=true;return;}if(dirty){var answer=MessageBox.Show("Tienes cambios sin guardar. ¿Quieres guardarlos antes de salir?","Estación Marte",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);if(answer==DialogResult.Cancel)e.Cancel=true;else if(answer==DialogResult.Yes){try{SaveAll();}catch(Exception ex){MessageBox.Show(ex.Message);e.Cancel=true;}}}if(!e.Cancel)preview?.Dispose();};
        if(smoke) Shown+=(_,_)=>{var timer=new System.Windows.Forms.Timer{Interval=1200};timer.Tick+=(_,_)=>{timer.Stop();CatalogStore.Save(site,catalog);File.WriteAllText(Path.Combine(Path.GetTempPath(),"estacion-marte-smoke.txt"),"OK "+catalog.Products.Count);dirty=false;Close();};timer.Start();};
    }
    void CopyTemplate()
    {
        var template=Path.Combine(AppContext.BaseDirectory,"plantilla","site");if(!Directory.Exists(template))throw new DirectoryNotFoundException("Falta la carpeta plantilla/site junto a la aplicación. Extrae el ZIP completo.");
        foreach(var file in Directory.EnumerateFiles(template,"*",SearchOption.AllDirectories)){var to=Path.Combine(site,Path.GetRelativePath(template,file));Directory.CreateDirectory(Path.GetDirectoryName(to)!);File.Copy(file,to,true);}
    }
    void UpgradeWebTemplate()
    {
        var template=Path.Combine(AppContext.BaseDirectory,"plantilla","site");
        foreach(var file in new[]{"index.html","styles.css","tryon.css","app.js","tryon.js","favicon.svg"})
        {
            var from=Path.Combine(template,file);if(File.Exists(from))File.Copy(from,Path.Combine(site,file),true);
        }
        // Seed fitting metadata only for original, unchanged photos from older catalogs.
        using var original=JsonDocument.Parse(File.ReadAllText(Path.Combine(site,"catalog.json")));
        var existing=original.RootElement.GetProperty("products").EnumerateArray().ToDictionary(p=>p.GetProperty("id").GetString()!,p=>p.TryGetProperty("tryOnEnabled",out _));
        var defaults=CatalogStore.Load(template).Products.ToDictionary(p=>p.Id);
        bool changed=false;
        foreach(var product in catalog.Products)
            if(existing.TryGetValue(product.Id,out var configured)&&!configured&&defaults.TryGetValue(product.Id,out var seed)&&product.Image==seed.Image&&product.Category==seed.Category)
            {
                product.TryOnEnabled=seed.TryOnEnabled;product.TryOnImage=seed.TryOnImage;product.TryOnCrop=seed.TryOnCrop;changed=true;
            }
        if(changed)CatalogStore.Save(site,catalog);
    }
    TabPage Page(string title)=>new(title){BackColor=Theme.Paper,Padding=new Padding(25)};
    TabPage BuildProducts()
    {
        var page=Page("Mi catálogo");
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=125,FlowDirection=FlowDirection.TopDown,WrapContents=false};
        top.Controls.Add(Theme.Heading("Cada pieza cuenta una historia."));
        var toolbar=new FlowLayoutPanel{AutoSize=true};search.Width=250;search.PlaceholderText="Buscar por nombre o categoría…";search.TextChanged+=(_,_)=>RefreshGrid();toolbar.Controls.Add(search);toolbar.Controls.Add(Theme.Button("+ Nuevo producto",(_,_)=>EditProduct(null),true));toolbar.Controls.Add(Theme.Button("Editar",(_,_)=>EditProduct(Selected())));toolbar.Controls.Add(Theme.Button("Duplicar",(_,_)=>Duplicate()));toolbar.Controls.Add(Theme.Button("Eliminar",(_,_)=>Delete()));top.Controls.Add(toolbar);
        grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.MultiSelect=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.RowHeadersVisible=false;grid.AutoGenerateColumns=false;grid.BackgroundColor=Theme.Paper;grid.BorderStyle=BorderStyle.None;grid.GridColor=Theme.Line;grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(236,226,210),ForeColor=Theme.Ink,Font=new Font("Segoe UI",10,FontStyle.Bold),Padding=new Padding(6)};grid.ColumnHeadersHeight=42;grid.RowTemplate.Height=43;grid.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Theme.Paper,ForeColor=Theme.Ink,SelectionBackColor=Color.FromArgb(228,211,179),SelectionForeColor=Theme.Ink,Padding=new Padding(6)};grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(243,237,226);grid.CellDoubleClick+=(_,e)=>{if(e.RowIndex>=0)EditProduct(Selected());};
        grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Producto",DataPropertyName="Name",AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill,FillWeight=150});
        grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Categoría",DataPropertyName="Category",Width=120});grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Precio S/",DataPropertyName="Price",Width=100,DefaultCellStyle=new(){Format="0.00",NullValue="Consultar"}});grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Acabado",DataPropertyName="Finish",Width=110});grid.Columns.Add(new DataGridViewCheckBoxColumn{HeaderText="Disponible",DataPropertyName="Available",Width=120});grid.Columns.Add(new DataGridViewCheckBoxColumn{HeaderText="Favorito",DataPropertyName="Featured",Width=85});
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=64,Padding=new Padding(0,15,0,0)};footer.Controls.Add(count);footer.Controls.Add(Theme.Button("Vista previa de mi web",(_,_)=>Guard(Preview)));
        page.Controls.Add(grid);page.Controls.Add(footer);page.Controls.Add(top);return page;
    }
    TabPage BuildBrand()
    {
        var page=Page("Mi marca");var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
        panel.Controls.Add(Theme.Heading("Tu esencia, en cada detalle."));panel.Controls.Add(Theme.Note("Personaliza el texto de portada y los canales de contacto. El diseño conserva los tonos del catálogo."));
        Add("name","Nombre de la marca",catalog.Brand.Name);Add("tagline","Frase al pie de la web",catalog.Brand.Tagline);Add("heroTitle","Título de portada (una línea por frase)",catalog.Brand.HeroTitle,true);Add("heroText","Descripción de portada",catalog.Brand.HeroText,true);Add("whatsapp","WhatsApp con código de país, solo dígitos (opcional)",catalog.Brand.Whatsapp);Add("instagram","Usuario de Instagram, sin @",catalog.Brand.Instagram);
        panel.Controls.Add(Theme.Note("Si dejas WhatsApp vacío, tus clientes podrán copiar su consulta y escribirte por Instagram. La moneda del catálogo es el sol peruano (PEN)."));panel.Controls.Add(Theme.Button("Guardar mi marca",(_,_)=>Guard(SaveAll),true));panel.Controls.Add(Theme.Button("Abrir mis copias de seguridad",(_,_)=>Guard(()=>{Directory.CreateDirectory(Path.Combine(workspace,"copias"));Open(Path.Combine(workspace,"copias"));})));
        page.Controls.Add(panel);return page;
        void Add(string key,string label,string value,bool multi=false){var input=Theme.Input(value,multi);input.TextChanged+=(_,_)=>Changed();brandFields[key]=input;Theme.Field(panel,label,input);}
    }
    TabPage BuildPublish()
    {
        var page=Page("Publicar web");var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
        panel.Controls.Add(Theme.Heading("De tu catálogo al mundo."));panel.Controls.Add(Theme.Note("1. Edita y guarda.  2. Genera tu web.  3. Publícala en GitHub.\nUna vez conectado el repositorio, Render actualizará la web con cada publicación."));
        export.Text=settings.ExportDirectory;repo.Text=settings.Repository;Theme.Field(panel,"Carpeta donde se generará el código de la web",export);panel.Controls.Add(Theme.Button("Elegir carpeta…",(_,_)=>{using var dialog=new FolderBrowserDialog{Description="Elige una carpeta vacía o una exportación anterior de Estación Marte",UseDescriptionForTitle=true,ShowNewFolderButton=true};if(dialog.ShowDialog()==DialogResult.OK)export.Text=dialog.SelectedPath;}));
        Theme.Field(panel,"URL del repositorio de GitHub (HTTPS)",repo);repo.PlaceholderText="https://github.com/tu-usuario/estacion-marte.git";
        panel.Controls.Add(Theme.Note("Para la primera publicación, crea un repositorio vacío en tu GitHub. Git debe estar instalado en este equipo. La autenticación se realiza mediante Git Credential Manager; la aplicación no te pide ni guarda contraseñas o tokens."));
        actionPanel.Controls.Add(Theme.Button("Generar código web",(_,_)=>Guard(()=>{Generate();SetStatus("Web generada. Ya puedes subir esta carpeta a GitHub.");Open(export.Text);}),true));actionPanel.Controls.Add(Theme.Button("Publicar en GitHub",async(_,_)=>await PublishAsync()));actionPanel.Controls.Add(Theme.Button("Ver antes de publicar",(_,_)=>Guard(Preview)));panel.Controls.Add(actionPanel);
        panel.Controls.Add(Theme.Heading("Conecta Render una sola vez",18));panel.Controls.Add(Theme.Note("En Render: New → Blueprint → conecta tu repositorio → Deploy Blueprint.\nEl render.yaml incluido configura un Static Site con la carpeta site y despliegues automáticos.\nTambién puedes crear un Static Site: Build command = echo listo; Publish directory = site.\nPublicar en GitHub confirma el envío del código; el resultado del despliegue se consulta en Render."));
        var links=new FlowLayoutPanel{AutoSize=true};links.Controls.Add(Theme.Button("Abrir Render",(_,_)=>Open("https://dashboard.render.com/")));links.Controls.Add(Theme.Button("Crear repositorio",(_,_)=>Open("https://github.com/new")));links.Controls.Add(Theme.Button("Instalar Git",(_,_)=>Open("https://git-scm.com/downloads/win")));panel.Controls.Add(links);
        panel.Controls.Add(Theme.Note("La web funciona como catálogo con consulta de pedidos. No incluye cobros ni pagos en línea. Los cambios de este editor se conservan localmente con copias de seguridad."));page.Controls.Add(panel);return page;
    }
    Product? Selected()=>grid.CurrentRow?.DataBoundItem as Product;
    void RefreshGrid(){if(!loaded)return;var query=search.Text.Trim();grid.DataSource=catalog.Products.Where(p=>query.Length==0||$"{p.Name} {p.Category} {p.Finish}".Contains(query,StringComparison.CurrentCultureIgnoreCase)).ToList();count.Text=$"{catalog.Products.Count} productos   ·   {catalog.Products.Count(p=>p.Available)} disponibles     ";}
    void EditProduct(Product? existing)
    {
        Guard(()=>{using var dialog=new ProductForm(site,existing?.Clone()??new Product());if(dialog.ShowDialog(this)!=DialogResult.OK)return;if(existing is null)catalog.Products.Add(dialog.Product);else catalog.Products[catalog.Products.IndexOf(existing)]=dialog.Product;Changed();RefreshGrid();});
    }
    void Duplicate(){var p=Selected();if(p is null)return;var copy=p.Clone();copy.Id="em-"+Guid.NewGuid().ToString("N")[..10];copy.Name+=" (copia)";catalog.Products.Add(copy);Changed();RefreshGrid();}
    void Delete(){var p=Selected();if(p is null)return;if(MessageBox.Show($"¿Eliminar «{p.Name}» del catálogo? Podrás recuperar los datos desde las copias de seguridad después de guardar.","Eliminar producto",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;catalog.Products.Remove(p);Changed();RefreshGrid();}
    void Changed(){if(!loaded)return;dirty=true;SetStatus("Hay cambios pendientes. Guarda antes de cerrar.");}
    void SetStatus(string value){status.Text=value;}
    void SaveAll()
    {
        catalog.Brand.Name=brandFields["name"].Text.Trim();catalog.Brand.Tagline=brandFields["tagline"].Text.Trim();catalog.Brand.HeroTitle=brandFields["heroTitle"].Text.Replace("\r","").Trim();catalog.Brand.HeroText=brandFields["heroText"].Text.Trim();catalog.Brand.Whatsapp=brandFields["whatsapp"].Text.Trim();catalog.Brand.Instagram=brandFields["instagram"].Text.Trim().TrimStart('@');
        CatalogStore.Save(site,catalog);SaveSettings();dirty=false;SetStatus("Cambios guardados. Se creó una copia de seguridad del catálogo anterior.");
    }
    void SaveSettings(){settings.ExportDirectory=export.Text.Trim();settings.Repository=repo.Text.Trim();File.WriteAllText(Path.Combine(workspace,"publicacion.local.json"),JsonSerializer.Serialize(settings));}
    void Generate(){SaveAll();if(string.IsNullOrWhiteSpace(export.Text))throw new InvalidOperationException("Elige una carpeta para generar la web.");SiteExporter.Export(site,export.Text.Trim());}
    void Preview(){SaveAll();preview??=new LocalPreview(site);Open(preview.Url);SetStatus("Vista previa abierta. Guarda tus cambios y recarga el navegador para verlos.");}
    async Task PublishAsync()
    {
        if(busy)return;try{Generate();if(!GitPublisher.IsGitHubUrl(repo.Text.Trim()))throw new InvalidOperationException("Configura la URL HTTPS de tu repositorio de GitHub.");busy=true;actionPanel.Enabled=false;UseWaitCursor=true;await GitPublisher.Publish(export.Text.Trim(),repo.Text.Trim(),new Progress<string>(SetStatus));}
        catch(Exception ex){SetStatus("La publicación no se completó.");MessageBox.Show(ex.Message+"\n\nSi GitHub solicita acceso, inicia sesión con Git Credential Manager. Si el repositorio tiene cambios de otro equipo, sincronízalo antes de volver a publicar.","Publicación de Estación Marte",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        finally{busy=false;actionPanel.Enabled=true;UseWaitCursor=false;}
    }
    static void Open(string target)=>Process.Start(new ProcessStartInfo(target){UseShellExecute=true});
    void Guard(Action action){try{action();}catch(Exception ex){MessageBox.Show(ex.Message,"Estación Marte",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
}
