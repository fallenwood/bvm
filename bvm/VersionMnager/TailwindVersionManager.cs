namespace Bvm;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bvm.Models;
using Microsoft.Extensions.Logging;

public sealed partial class TailwindVersionManager : IVersionManagerHandler {
  private const string TailwindWindowsAmd64Exe = "tailwindcss-windows-x64.exe";
  private const string TailwindLinuxAmd64Exe = "tailwindcss-linux-x64";
  private const string TailwindLinuxAarch64Exe = "tailwindcss-linux-arm64";
  private const string TailwindMacAmd64Exe = "tailwindcss-macos-x64";
  private const string TailwindMacAarch64Exe = "tailwindcss-macos-arm64";

  private const string TailwindWindowsTargetExe = "tailwindcss.exe";
  private const string TailwindXnixTargetExe = "tailwindcss";

  [GeneratedRegex("tw-v(\\d+\\.\\d+\\.\\d+)")]
  public static partial Regex TailwindDirectoryRegexGenerator();

  private static readonly Regex TailwindDirectoryRegex = TailwindDirectoryRegexGenerator();

  public async Task<List<Release>> RetrieveReleasesAsync(
    IDownloadClient downloadClient,
    Platform platform,
    string? registry) {
    var page = 1;
    var pageSize = 100;

    var releases = await this.RetriveTailwindReleasesAsync(downloadClient, platform, page, pageSize);
    var tmpReleases = releases;

    while (tmpReleases.Count >= pageSize) {
      page++;
      tmpReleases = await this.RetriveTailwindReleasesAsync(downloadClient, platform, page, pageSize);
      releases.AddRange(tmpReleases);
    }

    return releases;
  }

  private async Task<List<Release>> RetriveTailwindReleasesAsync(
    IDownloadClient downloadClient,
    Platform platform,
    int page,
    int pageSize) {
    var that = this as IVersionManagerHandler;
    var releases = await that.RetrieveGithubReleasesAsync(
      downloadClient,
      platform,
      Distribution.Bun,
      "tailwindlabs",
      "tailwindcss",
      page,
      pageSize);

    return releases;
  }

  public bool IsPlatformMatch(Platform platform, string name, string distribution) {
    return platform switch {
      Platform.WindowsAmd64 => string.Equals(name, TailwindWindowsAmd64Exe, StringComparison.OrdinalIgnoreCase),
      Platform.LinuxAmd64 => string.Equals(name, TailwindLinuxAmd64Exe, StringComparison.OrdinalIgnoreCase),
      Platform.LinuxAarch64 => string.Equals(name, TailwindLinuxAarch64Exe, StringComparison.OrdinalIgnoreCase),
      Platform.MacAmd64 => string.Equals(name, TailwindMacAmd64Exe, StringComparison.OrdinalIgnoreCase),
      _ => false,
    };
  }

  public string NormalizeTag(string tag) {
    if (tag.StartsWith("v")) {
      return tag;
    }

    if (tag.StartsWith("tw-")) {
      return tag[3..];
    }

    return $"v{tag}";
  }

  public string NormalizeDirectoryName(string tag) {
    if (tag.StartsWith("tw-")) {
      return tag;
    }

    if (tag.StartsWith("v")) {
      return $"tw-{tag}";
    }

    return $"tw-v{tag}";
  }

  public void CopyOrLink(IFileSystemManager fileSystemManager, Platform platform, string tag, bool all) {
    var exe = platform switch {
      Platform.WindowsAmd64 => TailwindWindowsAmd64Exe,
      Platform.LinuxAmd64 => TailwindLinuxAmd64Exe,
      Platform.LinuxAarch64 => TailwindLinuxAarch64Exe,
      Platform.MacAmd64 => TailwindMacAmd64Exe,
      _ => throw new InvalidPlatformException(platform),
    };

    var targetExe = platform switch {
      Platform.WindowsAmd64 => TailwindWindowsTargetExe,
      Platform.LinuxAmd64 or  Platform.LinuxAarch64 or Platform.MacAmd64 => TailwindXnixTargetExe,
      _ => throw new InvalidPlatformException(platform),
    };

    var source = Path.Combine(fileSystemManager.CurrentPath, tag, exe);
    var destination = Path.Combine(fileSystemManager.CurrentPath, targetExe);

    fileSystemManager.LinkOnly(source, destination);
  }

  public List<Release> GetInstalledReleases(IFileSystemManager fileSystemManager) {
    return fileSystemManager.GetInstalledReleases(TailwindDirectoryRegex);
  }

  public void Remove(IFileSystemManager fileSystemManager, string tag) {
    var filename = $"tw-{tag}";
    var source = Path.Combine(fileSystemManager.CurrentPath, filename);

    if (Directory.Exists(source)) {
      Directory.Delete(source, recursive: true);
    } else {
      Logger.Instance.LogError("Directory {directory} not found", source);
    }
  }
}
