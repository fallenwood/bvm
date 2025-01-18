using Bvm;
using Bvm.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public interface IFileSystemManager {
  public string CurrentPath { get; }
  public string TmpPath { get; }
  public List<Release> GetInstalledBunReleases();
  public List<Release> GetInstalledDenoReleases();
  public Task WriteConfigAsync(Config config);
  public Task<Config> ReadConfigAsync();
  public void CopyOrLinkBun(string tag);
  public void CopyOrLinkDeno(string tag);
  public void RemoveBun(string tag);
  public void RemoveDeno(string tag);
}

public partial class FileSystemManager(
  Platform platform,
  string currentPath,
  string tmpPath,
  string profilePath)
  : IFileSystemManager {
  private const string ConfigFileName = ".config.ini";
  private const string BunWindowsAmd64SubPath = "bun-windows-x64";
  private const string BunLinuxAmd64SubPath = "bun-linux-x64";
  private const string BunLinuxAarch64SubPath = "bun-linux-aarch64";
  private const string BunWindowsExecutable = "bun.exe";
  private const string BunXnixExecutable = "bun";

  private const string DenoWindowsExecutable = "deno.exe";
  private const string DenoXnixExecutable = "deno";

  public string CurrentPath => currentPath;

  public string TmpPath => tmpPath;

  [GeneratedRegex(@"bun-v(\d+\.\d+\.\d+)", RegexOptions.Compiled)]
  private static partial Regex BunDirectory();

  [GeneratedRegex(@"deno-v(\d+\.\d+\.\d+)", RegexOptions.Compiled)]
  private static partial Regex DenoDirectory();

  public List<Release> GetInstalledBunReleases() =>
    this.GetInstalledReleases(BunDirectory());

  public List<Release> GetInstalledDenoReleases() =>
    this.GetInstalledReleases(DenoDirectory());

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
    if (!string.IsNullOrEmpty(config.BunVersion)) {
      sb.AppendLine($"{Config.BunVersionKey}={config.BunVersion}");
    }

    await File.WriteAllTextAsync(configPath, sb.ToString());
  }

  public async Task<Config> ReadConfigAsync() {
    var configPath = Path.Combine(currentPath, ConfigFileName);
    var config = new Config();

    if (!File.Exists(configPath)) {
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
      }
    }

    return config;
  }

  public void CopyOrLinkBun(string tag) {
    var source = platform switch {
      Platform.WindowsAmd64 => Path.Combine(currentPath, tag, BunWindowsAmd64SubPath, BunWindowsExecutable),
      Platform.LinuxAmd64 => Path.Combine(currentPath, tag, BunLinuxAmd64SubPath, BunXnixExecutable),
      Platform.LinuxAarch64 => Path.Combine(currentPath, tag, BunLinuxAarch64SubPath, BunXnixExecutable),
      _ => throw new InvalidPlatformException(platform),
    };

    var destination = platform switch {
      Platform.WindowsAmd64 => Path.Combine(currentPath, BunWindowsExecutable),
      Platform.LinuxAmd64 => Path.Combine(currentPath, BunXnixExecutable),
      Platform.LinuxAarch64 => Path.Combine(currentPath, BunXnixExecutable),
      _ => throw new InvalidPlatformException(platform),
    };

    CopyOrLink(source, destination);
  }

  public void CopyOrLinkDeno(string tag) {
    var source = platform switch {
      Platform.WindowsAmd64 => Path.Combine(currentPath, tag, DenoWindowsExecutable),
      Platform.LinuxAmd64 => Path.Combine(currentPath, tag, DenoXnixExecutable),
      Platform.LinuxAarch64 => Path.Combine(currentPath, tag, DenoXnixExecutable),
      _ => throw new System.Exception("Unsupported platform"),
    };

    var destination = platform switch {
      Platform.WindowsAmd64 => Path.Combine(currentPath, DenoWindowsExecutable),
      Platform.LinuxAmd64 => Path.Combine(currentPath, DenoXnixExecutable),
      Platform.LinuxAarch64 => Path.Combine(currentPath, DenoXnixExecutable),
      _ => throw new System.Exception("Unsupported platform"),
    };

    CopyOrLink(source, destination);
  }

  public void CopyOrLink(string source, string destination) {
    if (File.Exists(destination)) {
      File.Delete(destination);
    }

    if (platform == Platform.WindowsAmd64) {
      File.Copy(source, destination);
    } else {
      File.CreateSymbolicLink(destination, source);
    }
  }

  public void RemoveBun(string tag) {
    var source = Path.Combine(currentPath, tag);

    if (Directory.Exists(source)) {
      Directory.Delete(source, recursive: true);
    } else {
      Console.WriteLine($"Directory {source} not found");
    }
  }

  public void RemoveDeno(string tag) {
    var filename = $"deno-{tag}";
    var source = Path.Combine(currentPath, filename);

    if (Directory.Exists(source)) {
      Directory.Delete(source, recursive: true);
    } else {
      Console.WriteLine($"Directory {source} not found");
    }
  }
}
