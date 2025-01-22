namespace Bvm;

using Bvm.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public interface IFileSystemManager {
  public string CurrentPath { get; }
  public string TmpPath { get; }
  public List<Release> GetInstalledBunReleases();
  public List<Release> GetInstalledDenoReleases();
  public List<Release> GetInstalledNodeReleases();
  public Task WriteConfigAsync(Config config);
  public Task<Config> ReadConfigAsync();
  public void CopyOrLinkBun(string tag);
  public void CopyOrLinkDeno(string tag);
  public void CopyOrLinkNode(string tag, bool all);
  public void RemoveBun(string tag);
  public void RemoveDeno(string tag);
  public void RemoveNode(string tag);
  public void ExtractZipFile(string zipFilePath, string extractPath);
  public void ExtractTarGzipFile(string gzipFilePath, string extractPath);
}

public partial class FileSystemManager(
  Platform platform,
  string currentPath,
  string tmpPath,
#pragma warning disable CS9113 // Parameter is unread.
  string profilePath)
#pragma warning restore CS9113 // Parameter is unread.
  : IFileSystemManager {
  private const string ConfigFileName = ".config.ini";
  private const string BunWindowsAmd64SubPath = "bun-windows-x64";
  private const string BunLinuxAmd64SubPath = "bun-linux-x64";
  private const string BunLinuxAarch64SubPath = "bun-linux-aarch64";
  private const string BunMacAmd64SubPath = "bun-darwin-x64";

  private const string BunWindowsExecutable = "bun.exe";
  private const string BunXnixExecutable = "bun";

  private const string DenoWindowsExecutable = "deno.exe";
  private const string DenoXnixExecutable = "deno";

  private const string NodeWindowsExecutable = "node.exe";
  private const string NodeXnixExecutable = "node";
  private const string NodeWindowsAmd64SubPath = "{0}-win-x64";
  private const string NodeLinuxAmd64SubPath = "{0}-linux-x64";
  private const string NodeLinuxAarch64SubPath = "{0}-linux-arm64";
  private const string NodeMacAmd64SubPath = "{0}-darwin-x64";

  private Config? cachedConfig = null;

  public string CurrentPath => currentPath;

  public string TmpPath => tmpPath;

  [GeneratedRegex(@"bun-v(\d+\.\d+\.\d+)", RegexOptions.Compiled)]
  private static partial Regex BunDirectory();

  [GeneratedRegex(@"deno-v(\d+\.\d+\.\d+)", RegexOptions.Compiled)]
  private static partial Regex DenoDirectory();

  [GeneratedRegex(@"node-v(\d+\.\d+\.\d+)", RegexOptions.Compiled)]
  private static partial Regex NodeDirectory();

  public List<Release> GetInstalledBunReleases() =>
    this.GetInstalledReleases(BunDirectory());

  public List<Release> GetInstalledDenoReleases() =>
    this.GetInstalledReleases(DenoDirectory());

  public List<Release> GetInstalledNodeReleases() =>
    this.GetInstalledReleases(NodeDirectory());

  public List<Release> GetInstalledReleases(Regex regex) {
    var directoes = Directory.GetDirectories(currentPath)
      .Select(fullDirectory => {
        var directory = Path.GetFileName(fullDirectory);
        var match = regex.Match(directory);

        if (!match.Success) {
          return null;
        }

        return new Release(TagName: match.Groups[0].Value);
      })
      .Where(e => e != null);

    return directoes.ToList()!;
  }

  public async Task WriteConfigAsync(Config config) {
    var configPath = Path.Combine(currentPath, ConfigFileName);
    var sb = new StringBuilder();

    if (!string.IsNullOrEmpty(config.Proxy)) {
      sb.AppendLine($"{Config.ProxyKey}={config.Proxy}");
    }

    if (!string.IsNullOrEmpty(config.NodeRegistry)) {
      sb.AppendLine($"{Config.NodeRegistryKey}={config.NodeRegistry}");
    }

    if (!string.IsNullOrEmpty(config.NpmRegistry)) {
      sb.AppendLine($"{Config.NpmRegistryKey}={config.NpmRegistry}");
    }

    if (!string.IsNullOrEmpty(config.BunVersion)) {
      sb.AppendLine($"{Config.BunVersionKey}={config.BunVersion}");
    }

    if (!string.IsNullOrEmpty(config.DenoVersion)) {
      sb.AppendLine($"{Config.DenoVersionKey}={config.DenoVersion}");
    }

    if (!string.IsNullOrEmpty(config.NodeVersion)) {
      sb.AppendLine($"{Config.NodeVersionKey}={config.NodeVersion}");
    }

    await File.WriteAllTextAsync(configPath, sb.ToString());
  }

  public async Task<Config> ReadConfigAsync() {
    if (this.cachedConfig != null) {
      return this.cachedConfig;
    }

    var configPath = Path.Combine(currentPath, ConfigFileName);
    var config = new Config();

    if (!File.Exists(configPath)) {
      this.cachedConfig = config;
      return config;
    }

    var lines = await File.ReadAllLinesAsync(configPath);

    foreach (var line in lines) {
      var parts = line.Split('=', 2);

      if (parts.Length != 2) {
        throw new System.Exception($"Invalid config file: {line}");
      }

      switch (parts[0]) {
        case Config.ProxyKey:
          config.Proxy = parts[1];
          break;
        case Config.BunVersionKey:
          config.BunVersion = parts[1];
          break;
        case Config.DenoVersionKey:
          config.DenoVersion = parts[1];
          break;
        case Config.NodeRegistryKey:
          config.NodeRegistry = parts[1];
          break;
        case Config.NpmRegistryKey:
          config.NpmRegistry = parts[1];
          break;
        case Config.NodeVersionKey:
          config.NodeVersion = parts[1];
          break;
      }
    }

    this.cachedConfig = config;
    return config;
  }

  public void CopyOrLinkBun(string tag) {
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

    var source = Path.Combine(currentPath, tag, subpath, exe);
    var destination = Path.Combine(currentPath, exe);

    LinkOnly(source, destination);
  }

  public void CopyOrLinkDeno(string tag) {
    var exe = platform switch {
      Platform.WindowsAmd64 => DenoWindowsExecutable,
      Platform.LinuxAmd64 => DenoXnixExecutable,
      Platform.LinuxAarch64 => DenoXnixExecutable,
      Platform.MacAmd64 => DenoXnixExecutable,
      _ => throw new InvalidPlatformException(platform),
    };

    var source = Path.Combine(currentPath, tag, exe);
    var destination = Path.Combine(currentPath, exe);

    LinkOnly(source, destination);
  }

  public void CopyOrLinkNode(string tag, bool all) {
    if (platform == Platform.WindowsAmd64) {
      var subpath = string.Format(NodeWindowsAmd64SubPath, tag);

      if (all) {
        var sources = Directory.GetFileSystemEntries(Path.Combine(currentPath, tag, subpath));

        foreach (var source in sources) {
          var filename = Path.GetFileName(source);
          if (filename == NodeWindowsExecutable) {
            continue;
          }

          var destination = Path.Combine(currentPath, Path.GetFileName(source));
          CopyOnly(source, destination);
        }
      }

      var exeSource = Path.Combine(currentPath, tag, subpath, NodeWindowsExecutable);
      var exeDestination = Path.Combine(currentPath, NodeWindowsExecutable);

      LinkOnly(exeSource, exeDestination);
    } else if (platform == Platform.LinuxAarch64 || platform == Platform.LinuxAmd64) {
      var subpath = platform switch {
        Platform.LinuxAarch64 => string.Format(NodeLinuxAarch64SubPath, tag),
        Platform.LinuxAmd64 => string.Format(NodeLinuxAmd64SubPath, tag),
        Platform.MacAmd64 => string.Format(NodeMacAmd64SubPath, tag),
        _ => throw new InvalidPlatformException(platform),
      };

      var sources = Directory.GetFileSystemEntries(Path.Combine(currentPath, tag, subpath, "bin"));

      foreach (var source in sources) {
        var destination = Path.Combine(currentPath, Path.GetFileName(source));
        LinkOnly(source, destination);
      }
    } else {
      throw new InvalidPlatformException(platform);
    }
  }

  public void CopyDirectories(string source, string destination) {
    var dir = new DirectoryInfo(source);

    var dirs = dir.GetDirectories();

    Directory.CreateDirectory(destination);

    foreach (var file in dir.GetFiles()) {
      string targetFilePath = Path.Combine(destination, file.Name);
      if (File.Exists(targetFilePath)) {
        File.Delete(targetFilePath);
      }

      file.CopyTo(targetFilePath);
    }

    foreach (var subDir in dirs) {
      string newDestinationDir = Path.Combine(destination, subDir.Name);
      CopyDirectories(subDir.FullName, newDestinationDir);
    }
  }

  public void LinkOnly(string source, string destination) {
    if (Directory.Exists(destination)) {
      Directory.Delete(destination, recursive: true);
    }

    if (File.Exists(destination)) {
      File.Delete(destination);
    }

    if (Directory.Exists(source)) {
      Directory.CreateSymbolicLink(destination, source);
    } else {
      File.CreateSymbolicLink(destination, source);
    }
  }

  public void CopyOnly(string source, string destination) {
    if (Directory.Exists(destination)) {
      Directory.Delete(destination, recursive: true);
    }

    if (File.Exists(destination)) {
      File.Delete(destination);
    }

    if (Directory.Exists(source)) {
      CopyDirectories(source, destination);
    } else {
      File.Copy(source, destination);
    }
  }

  public void RemoveBun(string tag) {
    var source = Path.Combine(currentPath, tag);

    if (Directory.Exists(source)) {
      Directory.Delete(source, recursive: true);
    } else {
      Logger.Instance.LogError($"Directory {source} not found");
    }
  }

  public void RemoveDeno(string tag) {
    var filename = $"deno-{tag}";
    var source = Path.Combine(currentPath, filename);

    if (Directory.Exists(source)) {
      Directory.Delete(source, recursive: true);
    } else {
      Logger.Instance.LogError($"Directory {source} not found");
    }
  }

  public void RemoveNode(string tag) {
    var filename = $"node-{tag}";
    var source = Path.Combine(currentPath, filename);

    if (Directory.Exists(source)) {
      Directory.Delete(source, recursive: true);
    } else {
      Logger.Instance.LogError($"Directory {source} not found");
    }
  }

  public void ExtractZipFile(string zipFilePath, string extractPath) {
    ZipFile.ExtractToDirectory(zipFilePath, extractPath);
  }

  public void ExtractTarGzipFile(string gzipFilePath, string extractPath) {
    TarGzFile.ExtractToDirectory(gzipFilePath, extractPath);
  }
}
