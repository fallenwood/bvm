namespace Bvm;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using Bvm.Models;

public partial class Commands {
  private const string LatestVersion = "latest";

  public Command InstallCommand() {
    var command = new Command(
      name: "install",
      description: "Install a specific version of bun");

    var argument = new Argument<string?>(name: "version");

    var forceOption = new Option<bool>(
      aliases: ["--force", "-f"],
      description: "Force the installation");

    command.AddOption(forceOption);
    command.AddArgument(argument);

    command.SetHandler(
      async (version, force, distribution) => {
        if (version is null) {
          Console.WriteLine("Please provide a version to install");
          return;
        }

        var config = await this.fileSystemManager.ReadConfigAsync();

        distribution = this.NormalizeDistribution(distribution);

        List<Release> releases = distribution switch {
          Distribution.Bun => await this.downloadManager.RetriveBunReleasesAsync(),
          Distribution.Deno => await this.downloadManager.RetriveDenoReleasesAsync(),
          Distribution.Node => await this.downloadManager.RetrieveNodejsReleasesAsync(config.NodeRegistry),
          _ => throw new InvalidDistributionException(distribution!),
        };

        Release? release;

        if (string.Equals(LatestVersion, version)) {
          release = releases.OrderByDescending(e => e.CreatedAt).FirstOrDefault();
        } else {
          version = distribution switch {
            Distribution.Bun => this.NormalizeBunTag(version),
            Distribution.Deno => this.NormalizeDenoTag(version),
            Distribution.Node => this.NormalizeNodeTag(version),
            _ => throw new InvalidDistributionException(distribution!),
          };

          release = releases.FirstOrDefault(e => string.Equals(e.TagName, version));
        }

        if (release is null) {
          Console.WriteLine($"Version {version} not found");
          return;
        }

        var installedReleases = this.fileSystemManager.GetInstalledBunReleases();
        var installedTags = installedReleases.Select(r => r.TagName).ToHashSet();
        if (installedTags.Contains(release.TagName)) {
          if (force) {
            Console.Error.WriteLine($"Version {version} already installed, but force option is set");
            this.fileSystemManager.RemoveBun(release.TagName);
          } else {
            Console.WriteLine($"Version {version} already installed");
            return;
          }
        }

        var tag = distribution switch {
          Distribution.Bun => this.NormalizeBunTag(release.TagName),
          Distribution.Deno => this.NormalizeDenoDirectoryName(release.TagName),
          Distribution.Node => this.NormalizeNodeDirectoryName(release.TagName),
          _ => throw new InvalidDistributionException(distribution!),
        };

        var extractDirectory = Path.Join(this.fileSystemManager.CurrentPath, tag);
        var tmpCompressedFile = await this.downloadManager.DownloadReleaseAsync(distribution!, release.DownloadUrl, this.fileSystemManager);

        if (release.DownloadUrl.EndsWith(".zip")) {
          this.fileSystemManager.ExtractZipFile(tmpCompressedFile, extractDirectory);
        } else if (release.DownloadUrl.EndsWith(".tar.gz")) {
          this.fileSystemManager.ExtractTarGzipFile(tmpCompressedFile, extractDirectory);
        } else {
          throw new NotImplementedException();
        }
      },
      argument,
      forceOption,
      this.DistributionOption);

    return command;
  }
}
