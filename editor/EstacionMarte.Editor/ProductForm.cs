using System.Globalization;
using EstacionMarte.Core;
namespace EstacionMarte.Editor;
sealed class ProductForm : Form
{
    public Product Product {get;}
    readonly TextBox name,material,style,description;
    readonly ComboBox category,finish;
    readonly NumericUpDown price;
    readonly CheckBox consult,available,featured,tryOnEnabled;
    readonly PictureBox photo;
    readonly string site;
    public ProductForm(string site,Product product)
    {
        this.site=site;Product=product;Text="Estación Marte · Editar producto";Size=new Size(900,790);MinimumSize=new Size(800,680);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=Theme.Paper;ForeColor=Theme.Ink;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Padding=new Padding(25)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));
        var form=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};form.Controls.Add(Theme.Heading("Un detalle especial.",23));
        name=Theme.Input(product.Name);name.Width=390;Theme.Field(form,"Nombre del producto",name);
        category=new ComboBox{Width=220,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(0,5,0,18)};category.Items.AddRange(CatalogStore.Categories);category.SelectedItem=product.Category;Theme.Field(form,"Categoría",category);
        var prices=new FlowLayoutPanel{AutoSize=true,Margin=new Padding(0,5,0,18)};price=new NumericUpDown{Width=130,DecimalPlaces=2,Maximum=1000000,Value=product.Price??0,Increment=.5m};consult=new CheckBox{Text="Precio por consultar",Checked=product.Price is null,AutoSize=true,Margin=new Padding(15,3,0,0)};consult.CheckedChanged+=(_,_)=>price.Enabled=!consult.Checked;price.Enabled=!consult.Checked;prices.Controls.Add(price);prices.Controls.Add(consult);Theme.Field(form,"Precio en soles (S/)",prices);
        finish=new ComboBox{Width=220,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(0,5,0,18)};finish.Items.AddRange(CatalogStore.Finishes);finish.SelectedItem=product.Finish;Theme.Field(form,"Acabado",finish);
        material=Theme.Input(product.Material);material.Width=390;Theme.Field(form,"Material",material);style=Theme.Input(product.Style);style.Width=390;Theme.Field(form,"Estilo",style);description=Theme.Input(product.Description,true);description.Width=390;Theme.Field(form,"Descripción (opcional)",description);
        available=new CheckBox{Text="Disponible para consultar",Checked=product.Available,AutoSize=true,Margin=new Padding(0,5,0,10)};featured=new CheckBox{Text="Mostrar como favorito",Checked=product.Featured,AutoSize=true,Margin=new Padding(0,5,0,20)};form.Controls.Add(available);form.Controls.Add(featured);
        tryOnEnabled=new CheckBox{Text="Activar probador virtual 2D",Checked=product.TryOnEnabled,AutoSize=true,Margin=new Padding(0,0,0,15)};form.Controls.Add(tryOnEnabled);
        var buttons=new FlowLayoutPanel{AutoSize=true};buttons.Controls.Add(Theme.Button("Guardar producto",(_,_)=>Save(),true));buttons.Controls.Add(Theme.Button("Cancelar",(_,_)=>{DialogResult=DialogResult.Cancel;Close();}));form.Controls.Add(buttons);
        var visual=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(15,0,0,0)};visual.Controls.Add(Theme.Note("FOTO DEL PRODUCTO"));photo=new PictureBox{Width=265,Height=345,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(235,227,213),Margin=new Padding(0,0,0,20)};visual.Controls.Add(photo);visual.Controls.Add(Theme.Button("Cambiar foto…",(_,_)=>Import()));visual.Controls.Add(Theme.Note("JPG, PNG o WebP · hasta 15 MB.\nRecomendamos una foto vertical, nítida y sin texto.\n\nLas fotos WebP pueden verse en la web aunque Windows no muestre aquí una miniatura."));
        visual.Controls.Add(Theme.Button("PNG para probador…",(_,_)=>ImportTryOn()));visual.Controls.Add(Theme.Button("Usar foto de producto",(_,_)=>{Product.TryOnImage="";Product.TryOnCrop=null;MessageBox.Show("El probador intentará quitar el fondo de la foto original. Revisa el resultado en la vista previa.");}));visual.Controls.Add(Theme.Note("Para el probador: un PNG transparente de la joya ofrece el mejor resultado. En aretes, usa un solo arete; la web permite mostrar el par. Solo cadenas, aretes y pulseras."));
        if(product.SourcePage is not null)visual.Controls.Add(Theme.Note($"Referencia del PDF: página {product.SourcePage}."));
        visual.AutoScroll=true;foreach(Control control in visual.Controls)if(control is Label)control.MaximumSize=new Size(265,0);
        layout.Controls.Add(form,0,0);layout.Controls.Add(visual,1,0);Controls.Add(layout);LoadPhoto();FormClosed+=(_,_)=>photo.Image?.Dispose();
    }
    void LoadPhoto(){photo.Image?.Dispose();photo.Image=null;try{using var stream=File.OpenRead(CatalogStore.AssetPath(site,Product.Image));using var image=Image.FromStream(stream);photo.Image=new Bitmap(image);}catch(ArgumentException){} }
    void Import(){using var dialog=new OpenFileDialog{Title="Selecciona la foto del producto",Filter="Imágenes|*.jpg;*.jpeg;*.png;*.webp"};if(dialog.ShowDialog()!=DialogResult.OK)return;try{Product.Image=CatalogStore.ImportPhoto(site,dialog.FileName);Product.TryOnCrop=null;LoadPhoto();}catch(Exception ex){MessageBox.Show(ex.Message,"Foto del producto");}}
    void ImportTryOn(){using var dialog=new OpenFileDialog{Title="Selecciona un PNG transparente de la joya",Filter="PNG transparente|*.png"};if(dialog.ShowDialog()!=DialogResult.OK)return;try{using var bitmap=new Bitmap(dialog.FileName);bool transparent=false;for(int y=0;y<bitmap.Height&&!transparent;y++)for(int x=0;x<bitmap.Width&&!transparent;x++)transparent=bitmap.GetPixel(x,y).A<200;if(!transparent)throw new InvalidDataException("Este PNG no tiene transparencia. Usa una imagen sin fondo.");Product.TryOnImage=CatalogStore.ImportPhoto(site,dialog.FileName);Product.TryOnCrop=null;tryOnEnabled.Checked=true;MessageBox.Show("Imagen del probador guardada. Revisa cómo queda desde Vista previa.");}catch(Exception ex){MessageBox.Show(ex.Message,"Imagen del probador");}}
    void Save(){if(string.IsNullOrWhiteSpace(name.Text)){MessageBox.Show("Escribe un nombre para el producto.");name.Focus();return;}if(tryOnEnabled.Checked && category.SelectedItem?.ToString() is not ("Cadenas" or "Aretes" or "Pulseras")){MessageBox.Show("Activa el probador solo en cadenas, aretes o pulseras.");return;}Product.Name=name.Text.Trim();Product.Category=category.SelectedItem?.ToString()??"Aretes";Product.Finish=finish.SelectedItem?.ToString()??"Dorado";Product.Price=consult.Checked?null:price.Value;Product.Material=material.Text.Trim();Product.Style=style.Text.Trim();Product.Description=description.Text.Trim();Product.Available=available.Checked;Product.Featured=featured.Checked;Product.TryOnEnabled=tryOnEnabled.Checked;DialogResult=DialogResult.OK;Close();}
}
