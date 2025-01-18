using System.CommandLine;

namespace Bvm;

public partial class Commands(
  Platform platform,
  IDownloadManager downloadManager,
  IFileSystemManager fileSystemManager) {
  private readonly Platform platform = platform;
  private readonly IDownloadManager downloadManager = downloadManager;
  private readonly IFileSystemManager fileSystemManager = fileSystemManager;

  public Option<string?> DistributionOption { get; } = new Option<string?>(
    aliases: ["--distribution", "-d"],
    description: "Select the distribution to install",
    getDefaultValue: () => "bun");

  public string NormalizeBunTag(string tag) {
    if (tag.StartsWith("bun-")) {
      return tag;
    }

    if (tag.StartsWith("v")) {
      return $"bun-{tag}";
    }

    return $"bun-v{tag}";
  }

  public string NormalizeDenoTag(string tag) {
    if (tag.StartsWith("v")) {
      return tag;
    }

    if (tag.StartsWith("deno-")) {
      return tag[5..];
    }

    return $"v{tag}";
  }

  public string NormalizeDenoDirectoryName(string tag) {
    if (tag.StartsWith("deno-")) {
      return tag;
    }

    if (tag.StartsWith("v")) {
      return $"deno-{tag}";
    }

    return $"deno-v{tag}";
  }
}
