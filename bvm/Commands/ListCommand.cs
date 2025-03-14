namespace Bvm;

using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Bvm.Models;
using Microsoft.Extensions.Logging;

public partial class Commands {
  public Command ListCommand() {
    var command = new Command(
      name: "list",
      description: "List all installed versions of bun");

    var allOptions = new Option<bool>(
      aliases: ["--all", "-a"],
      description: "List all available versions");

    command.AddOption(allOptions);
    command.SetHandler(
      async (all, distribution) => {
        var versionManagerHandler = this.GetVersionManagerHandler(distribution);
        var installedReleases = versionManagerHandler.GetInstalledReleases(this.fileSystemManager);
        
        var installedTags = installedReleases.Select(r => r.TagName).ToHashSet();
        var releases = new List<Release>(installedReleases.Count);

        if (all) {
          var config = await this.fileSystemManager.ReadConfigAsync();

          var remoteReleases = await versionManagerHandler.RetrieveReleasesAsync(this.downloadManager.Client, platform, config.NodeRegistry);

          releases.AddRange(remoteReleases);
        } else {
          releases.AddRange(installedReleases);
        }

        foreach (var release in releases.OrderBy(e => e.CreatedAt)) {
          var installed = installedTags.Contains(release.TagName) ? "*" : string.Empty;
          Logger.Instance.LogInformation($"{release.Name} {installed}");
        }
      },
      allOptions,
      this.DistributionOption);

    return command;
  }
}
