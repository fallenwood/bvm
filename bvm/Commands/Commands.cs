namespace Bvm;

using System.CommandLine;
using Bvm.Models;

public partial class Commands(
  Platform platform,
  IDownloadManager downloadManager,
  IFileSystemManager fileSystemManager) {
  private readonly Platform platform = platform;
  private readonly IDownloadManager downloadManager = downloadManager;
  private readonly IFileSystemManager fileSystemManager = fileSystemManager;

  public Option<string?> DistributionOption { get; } = new Option<string?>(
    aliases: ["--distribution", "-d"],
    description: "Select the distribution to install",
    getDefaultValue: () => "bun");

  public Option<bool> SilentOption { get; } = new Option<bool>(
    aliases: ["--silent", "-s"],
    description: "Silent mode",
    getDefaultValue: () => false);

  public string? NormalizeDistribution(string? distribution) {
    if (distribution == "nodejs") {
      return Distribution.Node;
    }

    return distribution;
  }

  public IVersionManagerHandler GetVersionManagerHandler(string? distribution) {
    IVersionManagerHandler versionManagerHandler = distribution switch {
      Distribution.Bun => new BunVersionManager(),
      Distribution.Deno => new DenoVersionManager(),
      Distribution.Node => new NodeJSVersionManager(),
      Distribution.Tailwind => new TailwindVersionManager(),
      _ => throw new InvalidDistributionException(distribution!),
    };
    return versionManagerHandler;
  }
}
