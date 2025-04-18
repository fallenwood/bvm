using System.Net;
using System.Text;
using Bvm;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var platform = PlatformDetector.Detect();

if (platform == Platform.Unknown) {
  Console.Error.WriteLine("Unsupported platform");
  return;
}

var fileSystemManager = new FileSystemManager(
  Path.GetDirectoryName(Environment.ProcessPath)!,
  Path.GetTempPath(),
  "");

var httpClient = await GetHttpClient(fileSystemManager);
var downloadClient = new DownloadClient(httpClient);

var downloadManager = new DownloadManager(downloadClient, platform);

Commands.Setup(platform, downloadManager, fileSystemManager);

var app = ConsoleAppFramework.ConsoleApp.Create();

app.Add("use", Commands.UseAsync);
app.Add("list", Commands.ListAsync);
app.Add("install", Commands.InstallAsync);
app.Add("uninstall", Commands.Uninstall);

await app.RunAsync(args);

async Task<HttpClient> GetHttpClient(IFileSystemManager fileSystemManager) {
  var config = await fileSystemManager.ReadConfigAsync();
  WebProxy? proxy = null;

  if (!string.IsNullOrWhiteSpace(config.Proxy)) {
    proxy = new WebProxy(config.Proxy);
  }

  var httpClient = new HttpClient(new HttpClientHandler {
    Proxy = proxy,
  });

  httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36 Edg/132.0.0.0");

  return httpClient;
}
