namespace Bvm;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Bvm.Models;

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
        var installedReleases = distribution switch {
          Distribution.Bun => this.fileSystemManager.GetInstalledBunReleases(),
          Distribution.Deno => this.fileSystemManager.GetInstalledDenoReleases(),
          Distribution.Node => this.fileSystemManager.GetInstalledNodeReleases(),
          _ => throw new InvalidDistributionException(distribution!),
        };

        var installedTags = installedReleases.Select(r => r.TagName).ToHashSet();
        var releases = new List<Release>(installedReleases.Count);

        if (all) {
          var config = await this.fileSystemManager.ReadConfigAsync();

          var remoteReleases = distribution switch {
            Distribution.Bun => await this.downloadManager.RetriveBunReleasesAsync(),
            Distribution.Deno => await this.downloadManager.RetriveDenoReleasesAsync(),
            Distribution.Node => await this.downloadManager.RetrieveNodejsReleasesAsync(config.NodeRegistry),
            _ => throw new InvalidDistributionException(distribution!),
          };

          releases.AddRange(remoteReleases);
        } else {
          releases.AddRange(installedReleases);
        }

        foreach (var release in releases.OrderBy(e => e.CreatedAt)) {
          var installed = installedTags.Contains(release.TagName) ? "*" : string.Empty;
          Console.WriteLine($"{release.Name} {installed}");
        }
      },
      allOptions,
      this.DistributionOption);

    return command;
  }
}
