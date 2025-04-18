namespace Bvm;

using Bvm.Models;

public static partial  class Commands {
  static Platform platform = Platform.Unknown;
  static IDownloadManager downloadManager = null!;
  static IFileSystemManager fileSystemManager = null!;

  public static void Setup(
    Platform platform,
    IDownloadManager downloadManager,
    IFileSystemManager fileSystemManager
  ) {
    Commands.platform = platform;
    Commands.downloadManager = downloadManager;
    Commands.fileSystemManager = fileSystemManager;
  }

  public static string? NormalizeDistribution(string? distribution) {
    if (distribution == "nodejs") {
      return Distribution.Node;
    }

    return distribution;
  }

  public static IVersionManagerHandler GetVersionManagerHandler(string? distribution) {
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
