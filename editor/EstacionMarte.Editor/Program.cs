using EstacionMarte.Core;
namespace EstacionMarte.Editor;
internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => MessageBox.Show(e.Exception.Message,"Estación Marte",MessageBoxButtons.OK,MessageBoxIcon.Error);
        try { Application.Run(new MainForm(args.Contains("--smoke"))); }
        catch (Exception e) { MessageBox.Show(e.Message,"No se pudo abrir el editor",MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }
}
