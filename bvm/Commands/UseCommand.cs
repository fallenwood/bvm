namespace Bvm;

using System.Linq;
using Microsoft.Extensions.Logging;
using Bvm.Models;

public static partial class Commands {
  /// <summary>
  /// Use a specific version
  /// </summary>
  /// <param name="tag"></param>
  /// <param name="distribution">-d, bun/node/deno/tailwindcss</param>
  /// <param name="all">Copy or link all assets, only works for nodejs on windows</param>
  /// <param name="silent"></param>
  /// <returns></returns>
  /// <exception cref="InvalidDistributionException"></exception>
  public static async Task UseAsync(
    [ConsoleAppFramework.Argument] string tag,
    string distribution = "bun",
    bool all = false,
    bool silent = true
  ) {

    if (silent) {
      Logger.Instance.Silent();
    }

    if (string.IsNullOrEmpty(tag)) {
      Logger.Instance.LogError("Please provide a tag to use");
      return;
    }

    var config = await Commands.fileSystemManager.ReadConfigAsync();
    distribution = Commands.NormalizeDistribution(distribution) ?? string.Empty;

    var versionManagerHandler = Commands.GetVersionManagerHandler(distribution);
    var directoryName = versionManagerHandler.NormalizeDirectoryName(tag);
    var installed = versionManagerHandler.GetInstalledReleases(Commands.fileSystemManager);
    var release = installed.FirstOrDefault(r => string.Equals(r.TagName, directoryName));

    if (release is null) {
      Logger.Instance.LogError("Version {version} not found", directoryName);
      return;
    }

    if (distribution == Distribution.Bun) {
      config.BunVersion = directoryName;
    } else if (distribution == Distribution.Deno) {
      config.DenoVersion = directoryName;
    } else if (distribution == Distribution.Node) {
      config.NodeVersion = directoryName;
    } else if (distribution == Distribution.Tailwind) {
      config.TailwindVersion = directoryName;
    } else {
      throw new InvalidDistributionException(distribution!);
    }

    versionManagerHandler.CopyOrLink(Commands.fileSystemManager, platform, directoryName, all);

    await Commands.fileSystemManager.WriteConfigAsync(config);
  }
}
