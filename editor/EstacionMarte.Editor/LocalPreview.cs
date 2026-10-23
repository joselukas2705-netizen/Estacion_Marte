using System.Net;
using System.Net.Sockets;
using System.Text;
namespace EstacionMarte.Editor;
sealed class LocalPreview : IDisposable
{
    private readonly TcpListener listener;
    private readonly CancellationTokenSource stop=new();
    private readonly string root;
    public string Url { get; }
    public LocalPreview(string directory)
    {
        root=Path.GetFullPath(directory);listener=new TcpListener(IPAddress.Loopback,0);listener.Start();Url=$"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";_ = Listen();
    }
    private async Task Listen(){while(!stop.IsCancellationRequested){try{var client=await listener.AcceptTcpClientAsync(stop.Token);_ = Serve(client);}catch(OperationCanceledException){break;}catch(ObjectDisposedException){break;}}}
    private async Task Serve(TcpClient client)
    {
        using(client)try
        {
            client.ReceiveTimeout=5000;using var stream=client.GetStream();using var reader=new StreamReader(stream,Encoding.ASCII,false,1024,true);
            var line=await reader.ReadLineAsync(stop.Token);if(line is null)return;
            var parts=line.Split(' ');if(parts.Length<2)return;
            var relative=Uri.UnescapeDataString(parts[1].Split('?')[0]);if(relative=="/")relative="/index.html";
            var file=Path.GetFullPath(Path.Combine(root,relative.TrimStart('/').Replace('/',Path.DirectorySeparatorChar)));
            var exists=file.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&File.Exists(file);
            var content=exists?await File.ReadAllBytesAsync(file,stop.Token):Encoding.UTF8.GetBytes("No encontrado");
            var type=Path.GetExtension(file).ToLowerInvariant() switch{".html"=>"text/html; charset=utf-8",".css"=>"text/css",".js"=>"text/javascript",".json"=>"application/json",".jpg" or ".jpeg"=>"image/jpeg",".webp"=>"image/webp",".png"=>"image/png",".svg"=>"image/svg+xml",".pdf"=>"application/pdf",_=>"application/octet-stream"};
            var headers=Encoding.ASCII.GetBytes($"HTTP/1.1 {(exists?"200 OK":"404 Not Found")}\r\nContent-Type: {type}\r\nContent-Length: {content.Length}\r\nCache-Control: no-cache\r\nConnection: close\r\n\r\n");await stream.WriteAsync(headers,stop.Token);await stream.WriteAsync(content,stop.Token);
        }catch(Exception e) when(e is IOException or OperationCanceledException or ArgumentException){ }
    }
    public void Dispose(){stop.Cancel();listener.Stop();stop.Dispose();}
}
