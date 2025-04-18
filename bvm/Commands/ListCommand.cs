namespace Bvm;

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Bvm.Models;

public static partial class Commands {
  /// <summary>
  /// List all installed versions of the specified distribution.
  /// </summary>
  /// <param name="distribution">-d, bun/node/deno/tailwindcss</param>
  /// <param name="all">List all available versions</param>
  /// <param name="silent">Slient mode</param>
  /// <returns></returns>
  public static async Task ListAsync(
    string distribution = "bun",
    bool all = false,
    bool silent = true) {
    var versionManagerHandler = Commands.GetVersionManagerHandler(distribution);
    var installedReleases = versionManagerHandler.GetInstalledReleases(Commands.fileSystemManager);

    var installedTags = installedReleases.Select(r => r.TagName).ToHashSet();
    var releases = new List<Release>(installedReleases.Count);

    if (all) {
      var config = await Commands.fileSystemManager.ReadConfigAsync();

      var remoteReleases = await versionManagerHandler.RetrieveReleasesAsync(Commands.downloadManager.Client, platform, config.NodeRegistry);

      releases.AddRange(remoteReleases);
    } else {
      releases.AddRange(installedReleases);
    }

    foreach (var release in releases.OrderBy(e => e.CreatedAt)) {
      var installed = installedTags.Contains(release.TagName) ? "*" : string.Empty;
      Logger.Instance.LogInformation($"{release.Name} {installed}");
    }
  }
}
