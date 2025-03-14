namespace Bvm;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bvm.Models;
using Microsoft.Extensions.Logging;

public sealed partial class BunVersionManager : IVersionManagerHandler {
  private const string BunWindowsAmd64Archive = "bun-windows-x64.zip";
  private const string BunLinuxAmd64Archive = "bun-linux-x64.zip";
  private const string BunLinuxAarch64Archive = "bun-linux-aarch64.zip";
  private const string BunMacAmd64Archive = "bun-darwin-x64.zip";

  private const string BunWindowsAmd64SubPath = "bun-windows-x64";
  private const string BunLinuxAmd64SubPath = "bun-linux-x64";
  private const string BunLinuxAarch64SubPath = "bun-linux-aarch64";
  private const string BunMacAmd64SubPath = "bun-darwin-x64";
  private const string BunWindowsExecutable = "bun.exe";
  private const string BunXnixExecutable = "bun";


  [GeneratedRegex(@"bun-v(\d+\.\d+\.\d+)", RegexOptions.Compiled)]
  private static partial Regex BunDirectoryRegexGenerator();

  private static readonly Regex BunDirectoryRegex = BunDirectoryRegexGenerator();

  public async Task<List<Release>> RetrieveReleasesAsync(
    IDownloadClient downloadClient,
    Platform platform,
    string? registry) {
    var page = 1;
    var pageSize = 100;

    var releases = await this.RetriveBunReleasesAsync(downloadClient, platform, page, pageSize);
    var tmpReleases = releases;

    while (tmpReleases.Count >= pageSize) {
      page++;
      tmpReleases = await this.RetriveBunReleasesAsync(downloadClient, platform, page, pageSize);
      releases.AddRange(tmpReleases);
    }

    return releases;
  }

  private async Task<List<Release>> RetriveBunReleasesAsync(
    IDownloadClient downloadClient,
    Platform platform,
    int page,
    int pageSize) {
    var that = this as IVersionManagerHandler;
    var releases = await that.RetrieveGithubReleasesAsync(
      downloadClient,
      platform,
      Distribution.Bun,
      "oven-sh",
      "bun",
      page,
      pageSize);

    return releases;
  }

  public bool IsPlatformMatch(Platform platform, string name, string distribution) {
    return platform switch {
      Platform.WindowsAmd64 => string.Equals(name, BunWindowsAmd64Archive, StringComparison.OrdinalIgnoreCase),
      Platform.LinuxAmd64 => string.Equals(name, BunLinuxAmd64Archive, StringComparison.OrdinalIgnoreCase),
      Platform.LinuxAarch64 => string.Equals(name, BunLinuxAarch64Archive, StringComparison.OrdinalIgnoreCase),
      Platform.MacAmd64 => string.Equals(name, BunMacAmd64Archive, StringComparison.OrdinalIgnoreCase),
      _ => false,
    };
  }

  public string NormalizeTag(string tag) {
    if (tag.StartsWith("bun-")) {
      return tag;
    }

    if (tag.StartsWith("v")) {
      return $"bun-{tag}";
    }

    return $"bun-v{tag}";
  }

  public string NormalizeDirectoryName(string tag) {
    return this.NormalizeTag(tag);
  }

  public void CopyOrLink(IFileSystemManager fileSystemManager, Platform platform, string tag, bool all) {
    var exe = platform switch {
      Platform.WindowsAmd64 => BunWindowsExecutable,
      Platform.LinuxAmd64 => BunXnixExecutable,
      Platform.LinuxAarch64 => BunXnixExecutable,
      Platform.MacAmd64 => BunXnixExecutable,
      _ => throw new InvalidPlatformException(platform),
    };

    var subpath = platform switch {
      Platform.WindowsAmd64 => BunWindowsAmd64SubPath,
      Platform.LinuxAmd64 => BunLinuxAmd64SubPath,
      Platform.LinuxAarch64 => BunLinuxAarch64SubPath,
      Platform.MacAmd64 => BunMacAmd64SubPath,
      _ => throw new InvalidPlatformException(platform),
    };

    var source = Path.Combine(fileSystemManager.CurrentPath, tag, subpath, exe);
    var destination = Path.Combine(fileSystemManager.CurrentPath, exe);

    fileSystemManager.LinkOnly(source, destination);
  }

  public List<Release> GetInstalledReleases(IFileSystemManager fileSystemManager) {
    return fileSystemManager.GetInstalledReleases(BunDirectoryRegex);
  }

  public void Remove(IFileSystemManager fileSystemManager, string tag) {
    var source = Path.Combine(fileSystemManager.CurrentPath, tag);

    if (Directory.Exists(source)) {
      Directory.Delete(source, recursive: true);
    } else {
      Logger.Instance.LogError("Directory {directory} not found", source);
    }
  }
}
