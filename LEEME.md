# Estación Marte

Web de catálogo y aplicación de Windows en C# para mantenerla sin editar código. La paleta conserva los marrones, dorados ocre y fondos claros del PDF AGOSTO.

## Abrir la aplicación

En la entrega de Documentos → Estacion Marte, abre `Editor Windows/EstacionMarte.Editor.exe`. La distribución incluye .NET: no necesitas instalarlo para usarla. Conserva toda la carpeta junto al ejecutable. Para entregarla al cliente, comparte `EstacionMarte-Editor-Windows.zip` de la carpeta ZIP para compartir y pídele extraer el ZIP antes de abrir el programa.

La primera vez se copia la plantilla a `%LOCALAPPDATA%/EstacionMarte/MiCatalogo/site`. Las siguientes aperturas conservan esos cambios. Las copias anteriores de `catalog.json` se guardan en `MiCatalogo/copias`. Para restaurar una copia, cierra la app y sustituye `site/catalog.json` por la copia elegida. Para trasladar el trabajo a otra computadora, copia toda la carpeta `MiCatalogo` a la misma ubicación del nuevo equipo; incluye tus fotos.

1. En **Mi catálogo**, crea, edita, duplica o elimina piezas. Cambia la foto, el material, la categoría, el acabado, el precio y la disponibilidad. El precio puede quedar como **Consultar**. Un favorito aparece primero en la web.
2. En **Mi marca**, configura el título de portada, su descripción, la frase del pie, Instagram y WhatsApp. Para Perú, WhatsApp es `51` seguido del número, sin `+` ni espacios. No se ha inventado un número de contacto.
3. **Guardar cambios** crea una copia de seguridad. **Vista previa** abre la web con un servidor local de la aplicación; guarda y recarga para ver nuevos cambios. La vista previa se cierra al salir de la app.
4. En **Publicar web**, elige una carpeta vacía y pulsa **Generar código web**. El resultado contiene `site/`, `render.yaml` y las instrucciones del repositorio. La app puede regenerar su propia carpeta de exportación y protege carpetas con archivos ajenos.
5. Crea un repositorio **vacío** en GitHub. Introduce su URL HTTPS y pulsa **Publicar en GitHub**. Necesitas Git para Windows y su inicio de sesión mediante Git Credential Manager. La aplicación no guarda contraseñas ni tokens. No uses esta opción para un repositorio con trabajo ajeno. No se fuerza el historial: si hay cambios remotos, sincronízalos primero.

El flujo normal del cliente es **editar → guardar → publicar en GitHub**. El botón de publicación vuelve a generar la web antes de enviarla.

## Configurar Render una sola vez

En [Render](https://dashboard.render.com/), selecciona **New → Blueprint**, conecta el repositorio de GitHub y usa el `render.yaml` incluido. Configura un sitio estático, carpeta `site`, y despliegue automático por commit.

Alternativamente, usa **New → Static Site**, selecciona el repositorio y la rama `main`, establece **Build command** en `echo listo` y **Publish directory** en `site`.

Render desplegará un nuevo sitio tras cada push a la rama conectada. La aplicación confirma el envío a GitHub; el estado y la URL del despliegue se comprueban en Render. [Documentación oficial](https://render.com/docs/static-sites) y [campos de render.yaml](https://render.com/docs/blueprint-spec).

La entrega incluye la carpeta Web para GitHub y Render y el ZIP EstacionMarte-Web-GitHub.zip, con una exportación preparada. El repositorio indicado es `https://github.com/joselukas2705-netizen/Estacion_Marte.git`. La configuración de Render está preparada; el despliegue requiere conectar el repositorio a la cuenta de Render.

## Contenido del PDF

Se importaron 37 productos con fotos reales y precios en soles. **Rose** se importó agotado por la marca SOLD OUT de la página 4. **Aurora Heart** no tiene un precio claramente asignado en la página 11; se dejó como Consultar. Revisa disponibilidad y precios antes de publicar, ya que proceden de un catálogo de agosto sin año indicado. La caja de lujo se importó a S/20 como adicional.

Las promociones del PDF no se aplican automáticamente. El PDF original se puede abrir desde la web. La selección de productos prepara una consulta por WhatsApp; si no está configurado, permite copiarla y abrir Instagram. El subtotal es orientativo: no incluye envío ni descuentos. No hay pasarela de pagos ni cobros en línea.

## Probador virtual con foto

Pulsa **Abrir probador virtual** en el catálogo o **Probármela** en una ficha habilitada. Sube un JPG, PNG o WebP de hasta 15 MB. Usa una foto frontal con buena luz y deja visible la zona de la joya. Arrastra la pieza o usa los controles de posición, tamaño y giro. En aretes puedes activar el par y ajustar su separación. Descarga tu prueba como PNG si quieres conservarla.

La foto se procesa solo en memoria del navegador: no se envía al servidor ni se guarda en almacenamiento del sitio. Al quitarla o cerrar el probador, se descarta. Solo el PNG que decidas descargar queda guardado en tu equipo.

Es una simulación manual 2D: no detecta automáticamente el rostro o el cuerpo, no calcula medidas reales y no genera una foto realista con IA. Las joyas no se adaptan a la perspectiva ni a lo que deba quedar detrás del cabello o la ropa. El recorte automático de fotos puede perder reflejos o conservar sombras; se puede ajustar su limpieza. La medida, el acabado y el ajuste reales pueden variar.

Se habilitaron inicialmente **Corazón geo**, **Heart dorado**, **Baby heart** y **Brac love**. Para añadir más: edita el producto en C#, activa **Probador virtual 2D** y, de preferencia, selecciona **PNG para probador** con una imagen transparente de esa misma pieza. Para aretes, usa un solo arete; la web crea el par. Se admiten cadenas, aretes y pulseras. No se debe activar en sets ni adicionales. Al actualizar la app, se refresca la plantilla web y se mantienen tus productos y fotos.

Repositorio indicado para publicar: `https://github.com/joselukas2705-netizen/Estacion_Marte.git`. El editor puede conectar una exportación nueva con la rama main existente conservando el resto del código del repositorio. No hace push forzado.

## Desarrollo

La web no necesita instalar paquetes ni compilarse. Desde la raíz: `node tools/preview.mjs`, luego visita `http://127.0.0.1:4173`. Verifica referencias y datos con `node tools/check.mjs`.

Para desarrollar la aplicación necesitas el SDK de .NET 10 en Windows:

```powershell
dotnet build editor/EstacionMarte.Editor/EstacionMarte.Editor.csproj -c Release
dotnet run --project editor/EstacionMarte.Verify -- .
dotnet publish editor/EstacionMarte.Editor/EstacionMarte.Editor.csproj -c Release -r win-x64 --self-contained true -o output/Editor-Windows
```

El proyecto Core administra catálogo, validaciones, copias, exportación y Git. Editor implementa la interfaz de Windows y la vista previa local. Verify comprueba el guardado, la exportación y el envío Git a un repositorio local temporal sin publicar datos externos.

`tools/import_catalog.py` conserva el mapeo del PDF como referencia de la importación inicial. Requiere PyMuPDF y Pillow; usarlo de nuevo sobrescribe los datos iniciales, por eso no forma parte del flujo del cliente. El navegador carga las fuentes DM Sans e Italiana desde Google Fonts y tiene fuentes locales de respaldo.
