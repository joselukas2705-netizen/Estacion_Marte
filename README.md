# Estación Marte

Catálogo web de joyería con los tonos del PDF original, 37 productos y editor de Windows en C#.

- **Web:** HTML, CSS y JavaScript, sin compilación ni dependencias de producción.
- **Probador virtual:** foto local, superposición 2D, controles de posición/tamaño/giro, par de aretes y descarga PNG. La foto no se transmite ni se persiste. Hay 4 piezas inicialmente habilitadas; se pueden añadir otras desde el editor, preferiblemente con un PNG transparente.
- **Editor:** .NET 10 / Windows Forms; productos, fotos, precios, stock, textos, copias de seguridad, vista previa y publicación en GitHub.
- **Render:** conectar este repositorio como Blueprint para usar `render.yaml`. Alternativa: Static Site, build `echo listo`, carpeta `site`, rama `main`.

## Vista previa y verificación

```sh
node tools/preview.mjs
node tools/check.mjs
```

Visita `http://127.0.0.1:4173`. Para desarrollar el editor, usa el SDK .NET 10 en Windows:

```powershell
dotnet build editor/EstacionMarte.Editor/EstacionMarte.Editor.csproj -c Release
dotnet run --project editor/EstacionMarte.Verify -- .
dotnet publish editor/EstacionMarte.Editor/EstacionMarte.Editor.csproj -c Release -r win-x64 --self-contained true -o output/Editor-Windows
```

Consulta [LEEME.md](LEEME.md) para el flujo del cliente. La web prepara consultas de compra por WhatsApp/Instagram; no procesa pagos. El probador es orientativo: no calcula medidas reales ni reemplaza una prueba física.
