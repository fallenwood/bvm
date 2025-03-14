namespace Bvm;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bvm.Models;
using Microsoft.Extensions.Logging;

public sealed partial class DenoVersionManager : IVersionManagerHandler {
  private const string DenoWindowsAmd64Archive = "deno-x86_64-pc-windows-msvc.zip";
  private const string DenoLinuxAmd64Archive = "deno-x86_64-unknown-linux-gnu.zip";
  private const string DenoLinuxAarch64Archive = "deno-aarch64-unknown-linux-gnu.zip";
  private const string DenoOsXAmd64Archive = "deno-x86_64-apple-darwin.zip";
  private const string DenoWindowsExecutable = "deno.exe";
  private const string DenoXnixExecutable = "deno";


  [GeneratedRegex(@"deno-v(\d+\.\d+\.\d+)", RegexOptions.Compiled)]
  private static partial Regex DenoDirectoryRegexGenerator();

  private static readonly Regex DenoDirectoryRegex = DenoDirectoryRegexGenerator();

  public async Task<List<Release>> RetrieveReleasesAsync(
    IDownloadClient downloadClient,
    Platform platform,
    string? registry) {
    var page = 1;
    var pageSize = 100;

    var releases = await this.RetrieveDenoReleasesAsync(downloadClient, platform, page, pageSize);
    var tmpReleases = releases;

    while (tmpReleases.Count >= pageSize) {
      page++;
      tmpReleases = await this.RetrieveDenoReleasesAsync(downloadClient, platform, page, pageSize);
      releases.AddRange(tmpReleases);
    }

    return releases;
  }

  private async Task<List<Release>> RetrieveDenoReleasesAsync(
    IDownloadClient downloadClient,
    Platform platform,
    int page,
    int pageSize) {
    var that = this as IVersionManagerHandler;
    var releases = await that.RetrieveGithubReleasesAsync(downloadClient, platform, Distribution.Deno, "denoland", "deno", page, pageSize);
    return releases;
  }

  public bool IsPlatformMatch(Platform platform, string name, string distribution) {
    return platform switch {
      Platform.WindowsAmd64 => string.Equals(name, DenoWindowsAmd64Archive, StringComparison.OrdinalIgnoreCase),
      Platform.LinuxAmd64 => string.Equals(name, DenoLinuxAmd64Archive, StringComparison.OrdinalIgnoreCase),
      Platform.LinuxAarch64 => string.Equals(name, DenoLinuxAarch64Archive, StringComparison.OrdinalIgnoreCase),
      Platform.MacAmd64 => string.Equals(name, DenoOsXAmd64Archive, StringComparison.OrdinalIgnoreCase),
      _ => false,
    };
  }

  public string NormalizeTag(string tag) {
    if (tag.StartsWith("v")) {
      return tag;
    }

    if (tag.StartsWith("deno-")) {
      return tag[5..];
    }

    return $"v{tag}";
  }

  public string NormalizeDirectoryName(string tag) {
    if (tag.StartsWith("deno-")) {
      return tag;
    }

    if (tag.StartsWith("v")) {
      return $"deno-{tag}";
    }

    return $"deno-v{tag}";
  }

  public void CopyOrLink(IFileSystemManager fileSystemManager, Platform platform, string tag, bool all) {
    var exe = platform switch {
      Platform.WindowsAmd64 => DenoWindowsExecutable,
      Platform.LinuxAmd64 => DenoXnixExecutable,
      Platform.LinuxAarch64 => DenoXnixExecutable,
      Platform.MacAmd64 => DenoXnixExecutable,
      _ => throw new InvalidPlatformException(platform),
    };

    var source = Path.Combine(fileSystemManager.CurrentPath, tag, exe);
    var destination = Path.Combine(fileSystemManager.CurrentPath, exe);

    fileSystemManager.LinkOnly(source, destination);
  }

  public List<Release> GetInstalledReleases(IFileSystemManager fileSystemManager) {
    return fileSystemManager.GetInstalledReleases(DenoDirectoryRegex);
  }

  public void Remove(IFileSystemManager fileSystemManager, string tag) {
    var filename = $"deno-{tag}";
    var source = Path.Combine(fileSystemManager.CurrentPath, filename);

    if (Directory.Exists(source)) {
      Directory.Delete(source, recursive: true);
    } else {
      Logger.Instance.LogError("Directory {directory} not found", source);
    }
  }
}
