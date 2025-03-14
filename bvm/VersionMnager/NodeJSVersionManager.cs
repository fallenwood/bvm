namespace Bvm;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bvm.Models;
using Microsoft.Extensions.Logging;

public sealed partial class NodeJSVersionManager : IVersionManagerHandler {
  private const string NodeDistUri = "{0}/dist";
  private const string NodeExeUri = "{0}/dist/{1}/{2}";
  private const string NodeJsOrg = "https://nodejs.org/";

  private const string NodeWindowsAmd64Archive = "node-{0}-win-x64.zip";
  private const string NodeLinuxAmd64Archive = "node-{0}-linux-x64.tar.gz";
  private const string NodeLinuxAarch64Archive = "node-{0}-linux-arm64.tar.gz";
  private const string NodeMacAmd64Archive = "node-{0}-darwin-x64.tar.gz ";
  private const string NodeWindowsExecutable = "node.exe";
  private const string NodeXnixExecutable = "node";
  private const string NodeWindowsAmd64SubPath = "{0}-win-x64";
  private const string NodeLinuxAmd64SubPath = "{0}-linux-x64";
  private const string NodeLinuxAarch64SubPath = "{0}-linux-arm64";
  private const string NodeMacAmd64SubPath = "{0}-darwin-x64";

  [GeneratedRegex(@"<a href=""(v\d+\.\d+\.\d+)/"">(v\d+\.\d+\.\d+)/</a>\s+([\w|-]+\s+\d+:\d+)\s+-")]
  private static partial Regex NodeJsVersionRegexGenerator();

  [GeneratedRegex(@"node-v(\d+\.\d+\.\d+)", RegexOptions.Compiled)]
  private static partial Regex NodeDirectoryRegexGenerator();

  private static readonly Regex NodeJsVersionRegex = NodeJsVersionRegexGenerator();
  private static readonly Regex NodeDirectoryRegex = NodeDirectoryRegexGenerator();

  public async Task<List<Release>> RetrieveReleasesAsync(
    IDownloadClient downloadClient,
    Platform platform,
    string? registry) {
    if (string.IsNullOrWhiteSpace(registry)) {
      registry = NodeJsOrg;
    }

    var uri = string.Format(NodeDistUri, registry);

    var httpResponse = await downloadClient.GetAsync(uri);
    httpResponse.EnsureSuccessStatusCode();

    var responseStream = await httpResponse.Content.ReadAsStreamAsync();

    using var streamReader = new StreamReader(responseStream);

    var releases = new List<Release>();

    while (true) {
      var line = await streamReader.ReadLineAsync();
      if (line == null) {
        break;
      }

      var match = NodeJsVersionRegex.Match(line);
      if (match.Success) {
        var tag = match.Groups[1].Value;
        var name = match.Groups[2].Value;

        var nodeExe = platform switch {
          Platform.WindowsAmd64 => string.Format(NodeWindowsAmd64Archive, tag),
          Platform.LinuxAmd64 => string.Format(NodeLinuxAmd64Archive, tag),
          Platform.LinuxAarch64 => string.Format(NodeLinuxAarch64Archive, tag),
          Platform.MacAmd64 => string.Format(NodeMacAmd64Archive, tag),
          _ => throw new InvalidPlatformException(platform),
        };

        var downloadUri = string.Format(NodeExeUri, registry, tag, nodeExe);

        var timestamp = DateTime.Parse(match.Groups[3].Value);

        releases.Add(new Release(
          Name: name,
          TagName: tag,
          downloadUri,
          timestamp,
          timestamp));
      }
    }

    return releases;
  }

  public bool IsPlatformMatch(Platform platform, string name, string distribution) {
    return true;
  }

  public string NormalizeTag(string tag) {
    if (tag.StartsWith("v")) {
      return tag;
    }

    if (tag.StartsWith("node-")) {
      return tag[5..];
    }

    return $"v{tag}";
  }

  public string NormalizeDirectoryName(string tag) {
    if (tag.StartsWith("node-")) {
      return tag;
    }

    if (tag.StartsWith("v")) {
      return $"node-{tag}";
    }

    return $"node-v{tag}";
  }

  public void CopyOrLink(IFileSystemManager fileSystemManager, Platform platform, string tag, bool all) {
    if (platform == Platform.WindowsAmd64) {
      var subpath = string.Format(NodeWindowsAmd64SubPath, tag);

      if (all) {
        var sources = Directory.GetFileSystemEntries(Path.Combine(fileSystemManager.CurrentPath, tag, subpath));

        foreach (var source in sources) {
          var filename = Path.GetFileName(source);
          if (filename == NodeWindowsExecutable) {
            continue;
          }

          var destination = Path.Combine(fileSystemManager.CurrentPath, Path.GetFileName(source));
          fileSystemManager.CopyOnly(source, destination);
        }
      }

      var exeSource = Path.Combine(fileSystemManager.CurrentPath, tag, subpath, NodeWindowsExecutable);
      var exeDestination = Path.Combine(fileSystemManager.CurrentPath, NodeWindowsExecutable);

      fileSystemManager.LinkOnly(exeSource, exeDestination);
    } else if (platform == Platform.LinuxAarch64 || platform == Platform.LinuxAmd64) {
      var subpath = platform switch {
        Platform.LinuxAarch64 => string.Format(NodeLinuxAarch64SubPath, tag),
        Platform.LinuxAmd64 => string.Format(NodeLinuxAmd64SubPath, tag),
        Platform.MacAmd64 => string.Format(NodeMacAmd64SubPath, tag),
        _ => throw new InvalidPlatformException(platform),
      };

      var sources = Directory.GetFileSystemEntries(Path.Combine(fileSystemManager.CurrentPath, tag, subpath, "bin"));

      foreach (var source in sources) {
        var destination = Path.Combine(fileSystemManager.CurrentPath, Path.GetFileName(source));
        fileSystemManager.LinkOnly(source, destination);
      }
    } else {
      throw new InvalidPlatformException(platform);
    }
  }

  public List<Release> GetInstalledReleases(IFileSystemManager fileSystemManager) {
    return fileSystemManager.GetInstalledReleases(NodeDirectoryRegex);
  }

  public void Remove(IFileSystemManager fileSystemManager, string tag) {
    var filename = $"node-{tag}";
    var source = Path.Combine(fileSystemManager.CurrentPath, filename);

    if (Directory.Exists(source)) {
      Directory.Delete(source, recursive: true);
    } else {
      Logger.Instance.LogError("Directory {directory} not found", source);
    }
  }
}
