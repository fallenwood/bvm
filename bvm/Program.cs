using Bvm;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Net;

var platform = PlatformDetector.Detect();

if (platform == Platform.Unknown) {
  Console.Error.WriteLine("Unsupported platform");
  return;
}

var fileSystemManager = new FileSystemManager(
  platform,
  Path.GetDirectoryName(Environment.ProcessPath)!,
  Path.GetTempPath(),
  "");

var httpClient = await GetHttpClient(fileSystemManager);
var downloadClient = new DownloadClient(httpClient);

var downloadManager = new DownloadManager(downloadClient, platform);

var commands = new Commands(platform, downloadManager, fileSystemManager);
var rootCommand = new RootCommand("Simple Bun/Deno version manager");
rootCommand.AddGlobalOption(commands.DistributionOption);
rootCommand.AddGlobalOption(commands.SilentOption);

rootCommand.AddCommand(commands.UseCommand());
rootCommand.AddCommand(commands.ListCommand());
rootCommand.AddCommand(commands.InstallCommand());
rootCommand.AddCommand(commands.UninstallCommand());

rootCommand.SetHandler(async () =>
  {
    var config = await fileSystemManager.ReadConfigAsync();

    Logger.Instance.LogInformation($"proxy         = {config.Proxy}");
    Logger.Instance.LogInformation($"node registry = {config.NodeRegistry}");
    // Logger.Instance.LogInformation($"npm  registry = {config.NpmRegistry}");
    Logger.Instance.LogInformation($"bun  version  = {config.BunVersion}");
    Logger.Instance.LogInformation($"deno version  = {config.DenoVersion}");
    Logger.Instance.LogInformation($"node version  = {config.NodeVersion}");
  });

await rootCommand.InvokeAsync(args);

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
