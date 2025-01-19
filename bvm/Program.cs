using Bvm;
using System;
using System.CommandLine;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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

var downloadManager = new DownloadManager(httpClient, platform);

var commands = new Commands(platform, downloadManager, fileSystemManager);
var rootCommand = new RootCommand("Simple Bun/Deno version manager");
rootCommand.AddGlobalOption(commands.DistributionOption);

rootCommand.AddCommand(commands.UseCommand());
rootCommand.AddCommand(commands.ListCommand());
rootCommand.AddCommand(commands.InstallCommand());
rootCommand.AddCommand(commands.UninstallCommand());

rootCommand.SetHandler(async () =>
  {
    var config = await fileSystemManager.ReadConfigAsync();

    Console.WriteLine($"proxy         = {config.Proxy}");
    Console.WriteLine($"node registry = {config.NodeRegistry}");
    Console.WriteLine($"npm  registry = {config.NpmRegistry}");
    Console.WriteLine($"bun  version  = {config.BunVersion}");
    Console.WriteLine($"deno version  = {config.DenoVersion}");
    Console.WriteLine($"node version  = {config.NodeVersion}");
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
