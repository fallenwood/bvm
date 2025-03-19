namespace Bvm;

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bvm.Models;

public interface IFileSystemManager {
  public string CurrentPath { get; }
  public string TmpPath { get; }
  public List<Release> GetInstalledReleases(Regex regex);
  public Task WriteConfigAsync(Config config);
  public Task<Config> ReadConfigAsync();
  public void LinkOnly(string source, string destination);
  public void CopyOnly(string source, string destination);
  public void ExtractZipFile(string zipFilePath, string extractPath);
  public void ExtractTarGzipFile(string gzipFilePath, string extractPath);
  public void CopyFile(string sourceFilePath, string distPath);
}

public partial class FileSystemManager(
  string currentPath,
  string tmpPath,
#pragma warning disable CS9113 // Parameter is unread.
  string profilePath)
#pragma warning restore CS9113 // Parameter is unread.
  : IFileSystemManager {
  private const string ConfigFileName = ".config.ini";

  private Config? cachedConfig = null;

  public string CurrentPath => currentPath;

  public string TmpPath => tmpPath;

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

    if (!string.IsNullOrEmpty(config.TailwindVersion)) {
      sb.AppendLine($"{Config.TailwindVersionKey}={config.TailwindVersion}");
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
        case Config.TailwindVersionKey:
          config.TailwindVersion = parts[1];
          break;
      }
    }

    this.cachedConfig = config;
    return config;
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

  public void ExtractZipFile(string zipFilePath, string extractPath) {
    ZipFile.ExtractToDirectory(zipFilePath, extractPath);
  }

  public void ExtractTarGzipFile(string gzipFilePath, string extractPath) {
    TarGzFile.ExtractToDirectory(gzipFilePath, extractPath);
  }

  public void CopyFile(string sourceFilePath, string distPath) {
    if (File.Exists(distPath)) {
      File.Delete(distPath);
    }

    File.Copy(sourceFilePath, distPath, true);
  }
}
