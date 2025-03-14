namespace Bvm;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using Bvm.Models;
using Microsoft.Extensions.Logging;

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
      async (version, force, distribution, silent) => {
        if (silent) {
          Logger.Instance.Silent();
        }

        if (version is null) {
          Logger.Instance.LogError("Please provide a version to install");
          return;
        }

        var config = await this.fileSystemManager.ReadConfigAsync();

        distribution = this.NormalizeDistribution(distribution);

        var versionManagerHandler = this.GetVersionManagerHandler(distribution);

        List<Release> releases = await downloadManager.RetrieveReleasesAsync(versionManagerHandler, config.NodeRegistry);
        
        Release? release;

        if (string.Equals(LatestVersion, version)) {
          release = releases.OrderByDescending(e => e.CreatedAt).FirstOrDefault();
        } else {
          version = versionManagerHandler.NormalizeTag(version);
          release = releases.FirstOrDefault(e => string.Equals(e.TagName, version));
        }

        if (release is null) {
          Logger.Instance.LogError($"Version {version} not found");
          return;
        }

        var installedReleases = versionManagerHandler.GetInstalledReleases(this.fileSystemManager);
        var installedTags = installedReleases.Select(r => r.TagName).ToHashSet();
        if (installedTags.Contains(release.TagName)) {
          if (force) {
            Logger.Instance.LogWarning($"Version {version} already installed, but force option is set");
            versionManagerHandler.Remove(this.fileSystemManager, release.TagName);
          } else {
            Logger.Instance.LogWarning($"Version {version} already installed");
            return;
          }
        }

        var tag = versionManagerHandler.NormalizeDirectoryName(release.TagName);

        var extractDirectory = Path.Join(this.fileSystemManager.CurrentPath, tag);
        var tmpCompressedFile = await this.downloadManager.DownloadReleaseAsync(versionManagerHandler, distribution!, release.DownloadUrl, this.fileSystemManager);

        // Only fails on mac
        var donwloadUrl = release.DownloadUrl.Trim();

        if (donwloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
          this.fileSystemManager.ExtractZipFile(tmpCompressedFile, extractDirectory);
        } else if (donwloadUrl.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)) {
          this.fileSystemManager.ExtractTarGzipFile(tmpCompressedFile, extractDirectory);
        } else {
          var targetFilePath = Path.Combine(extractDirectory, Path.GetFileName(tmpCompressedFile));
          if (!Directory.Exists(extractDirectory)) {
            Directory.CreateDirectory(extractDirectory);
          }
          this.fileSystemManager.CopyOnly(tmpCompressedFile, targetFilePath);
        }
      },
      argument,
      forceOption,
      this.DistributionOption,
      this.SilentOption);

    return command;
  }
}
