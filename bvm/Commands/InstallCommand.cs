namespace Bvm;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bvm.Models;
using Microsoft.Extensions.Logging;

public static partial class Commands {
  private const string LatestVersion = "latest";

  /// <summary>
  /// Install a specific version
  /// </summary>
  /// <param name="version"></param>
  /// <param name="distribution">-d, bun/node/deno/tailwindcss</param>
  /// <param name="force">-f, Force the installation</param>
  /// <param name="silent"></param>
  /// <returns></returns>
  public static async Task InstallAsync(
    [ConsoleAppFramework.Argument] string version,
    string distribution = "bun",
    bool force = false,
    bool silent = true
  ) {
    if (silent) {
      Logger.Instance.Silent();
    }

    if (version is null) {
      Logger.Instance.LogError("Please provide a version to install");
      return;
    }

    var config = await Commands.fileSystemManager.ReadConfigAsync();

    distribution = Commands.NormalizeDistribution(distribution) ?? string.Empty;

    var versionManagerHandler = Commands.GetVersionManagerHandler(distribution);

    List<Release> releases = await downloadManager.RetrieveReleasesAsync(versionManagerHandler, config.NodeRegistry);

    Release? release;

    if (string.Equals(LatestVersion, version)) {
      release = releases.OrderByDescending(e => e.CreatedAt).FirstOrDefault();
    } else {
      version = versionManagerHandler.NormalizeTag(version);
      release = releases.FirstOrDefault(e => string.Equals(e.TagName, version));
    }

    if (release is null) {
      Logger.Instance.LogError("Version {version} not found", version);
      return;
    }

    var installedReleases = versionManagerHandler.GetInstalledReleases(Commands.fileSystemManager);
    var installedTags = installedReleases.Select(r => r.TagName).ToHashSet();
    if (installedTags.Contains(release.TagName)) {
      if (force) {
        Logger.Instance.LogWarning("Version {version} already installed, but force option is set", version);
        versionManagerHandler.Remove(Commands.fileSystemManager, release.TagName);
      } else {
        Logger.Instance.LogWarning("Version {version} already installed", version);
        return;
      }
    }

    var tag = versionManagerHandler.NormalizeDirectoryName(release.TagName);

    var extractDirectory = Path.Join(Commands.fileSystemManager.CurrentPath, tag);
    var tmpCompressedFile = await Commands.downloadManager.DownloadReleaseAsync(versionManagerHandler, distribution!, release.DownloadUrl, Commands.fileSystemManager);

    // Only fails on mac
    var donwloadUrl = release.DownloadUrl.Trim();

    if (donwloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
      Commands.fileSystemManager.ExtractZipFile(tmpCompressedFile, extractDirectory);
    } else if (donwloadUrl.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)) {
      Commands.fileSystemManager.ExtractTarGzipFile(tmpCompressedFile, extractDirectory);
    } else {
      var targetFilePath = Path.Combine(extractDirectory, Path.GetFileName(tmpCompressedFile));
      if (!Directory.Exists(extractDirectory)) {
        Directory.CreateDirectory(extractDirectory);
      }
      Commands.fileSystemManager.CopyOnly(tmpCompressedFile, targetFilePath);
    }
  }
}
